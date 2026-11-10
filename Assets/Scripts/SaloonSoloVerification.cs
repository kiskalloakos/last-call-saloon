#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifySoloPractice()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string message)
            {
                if(!ok){VerificationFinished=true;throw new InvalidOperationException("Solo practice: "+message);}
                Debug.Log("SALOON_SOLO_CHECK "+message);
            }
            IEnumerator LeavePractice()
            {
                var task=LeaveAsync();while(!task.IsCompleted)yield return null;
                Check(!task.IsFaulted&&!InRoom&&View==null&&!Connection.Solo&&PlayerCount==0,"leaving solo clears the local session and returns to the menu");
            }
            yield return new WaitForSeconds(.1f);
            using(var occupied=new UdpClient(new IPEndPoint(IPAddress.Any,0)))
            {
                Port=((IPEndPoint)occupied.Client.LocalEndPoint).Port.ToString();
                SetBotsEnabled(false);StartSolo();
                yield return new WaitForSeconds(.2f);
                Check(InRoom&&Hosting&&Connection.Solo&&phase==Phase.Discussion&&!MenuOpen,"Solo Practice starts even when the selected UDP port is occupied");
                Check(!Connection.Manager.IsListening&&!Connection.Manager.IsServer,"solo starts without a network listener");
                Check(PlayerCount==5&&HumanCount==1&&BotsEnabled&&Sandbox&&Self?.alive==true,"No Bots lobby preference still produces one human and four practice opponents");
                Check(View.players.Length==5&&View.ownRole!="Unknown"&&Self.name==CleanName(PlayerName),"the normal private snapshot stream supplies roles, roster, and player name");
                StartSolo();Check(PlayerCount==5&&phase==Phase.Discussion,"repeated Solo Practice calls do not reset or duplicate the match");
                ChooseRoom(1,"");ToggleReady();
                yield return new WaitForSeconds(.15f);
                Check(Self.chosenRoom==1&&Self.ready,"room and Ready commands work through the local host path");
                var peer=players.Values.First(p=>p.bot);peer.chosenRoom=1;
                foreach(var id in players.Keys)rounds.SetReady(id,true);
                yield return null;Broadcast();
                Check(phase==Phase.Turns&&Self.room==1&&CoinTable.Visible,"solo can enter the physical coin-table minigame");
                ChooseCoin(0);Broadcast();Check(duels[1].claimed&&duels[1].heads==Connection.LocalId,"coin table commands reach the local authoritative game");
                yield return LeavePractice();
                Port="invalid";SetBotMatchSize(4);HostLan(true);
                yield return new WaitForSeconds(.1f);
                Check(Connection.Solo&&InRoom&&PlayerCount==4&&phase==Phase.Discussion,"solo restarts with four players despite invalid LAN settings");
                yield return LeavePractice();
            }
            // Leaving solo must also allow an ordinary real LAN session afterwards.
            ushort available;
            using(var probe=new UdpClient(new IPEndPoint(IPAddress.Loopback,0)))available=(ushort)((IPEndPoint)probe.Client.LocalEndPoint).Port;
            Port=available.ToString();SetBotsEnabled(false);HostLan(false);
            yield return new WaitForSeconds(.15f);
            Check(!Connection.Solo&&Connection.Manager.IsListening&&Hosting&&InRoom&&phase==Phase.Lobby,"LAN hosting still starts after leaving solo");
            var remote=new GameObject("Solo-to-LAN verification peer").AddComponent<SaloonConnection>();remote.Initialize();
            try
            {
                Snapshot received=null;remote.SnapshotReceived+=state=>received=state;
                remote.StartLan(false,"127.0.0.1",available);
                float until=Time.realtimeSinceStartup+8;
                while((!remote.Connected||received==null)&&Time.realtimeSinceStartup<until)yield return null;
                Check(remote.Connected&&HumanCount==2&&received?.phase==Phase.Lobby,"a real UDP client joins and receives snapshots after solo teardown");
                remote.Send(new Command{kind="hello",name="LAN after solo"});
                until=Time.realtimeSinceStartup+3;
                while(players[remote.LocalId].name!="LAN after solo"&&Time.realtimeSinceStartup<until)yield return null;
                Check(players[remote.LocalId].name=="LAN after solo","real network commands still reach the host");
            }
            finally
            {
                remote.Manager.Shutdown();Destroy(remote.Manager.gameObject);Destroy(remote.gameObject);
            }
            VerificationPassed=VerificationFinished=true;Debug.Log("SALOON_SOLO PASS");
        }
    }
}
#endif
