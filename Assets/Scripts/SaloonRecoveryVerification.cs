#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        // Real transport packets are discarded by the relay, without modifying UTP.
        sealed class RecoveryLink : IDisposable
        {
            readonly UdpClient front=new UdpClient(new IPEndPoint(IPAddress.Loopback,0));
            readonly UdpClient back=new UdpClient(new IPEndPoint(IPAddress.Loopback,0));
            readonly IPEndPoint host;
            IPEndPoint client;
            public float BlackoutUntil;
            public int Dropped;
            public ushort Port=>(ushort)((IPEndPoint)front.Client.LocalEndPoint).Port;
            public RecoveryLink(ushort port){host=new IPEndPoint(IPAddress.Loopback,port);}
            public void Pump()
            {
                for(int i=0;i<256&&front.Available>0;i++)
                {
                    IPEndPoint from=null;var bytes=front.Receive(ref from);client=from;
                    if(Time.realtimeSinceStartup<BlackoutUntil)Dropped++;else back.Send(bytes,bytes.Length,host);
                }
                for(int i=0;i<256&&back.Available>0;i++)
                {
                    IPEndPoint from=null;var bytes=back.Receive(ref from);
                    if(Time.realtimeSinceStartup<BlackoutUntil)Dropped++;else if(client!=null)front.Send(bytes,bytes.Length,client);
                }
            }
            public void Dispose(){front.Dispose();back.Dispose();}
        }

        public IEnumerator VerifyLanRecovery()
        {
            verifying=true;AutomatedInput=true;VerificationPassed=VerificationFinished=false;
            var peers=new List<SaloonConnection>();var links=new List<RecoveryLink>();
            var states=new Dictionary<SaloonConnection,Snapshot>();
            void Check(bool ok,string message)
            {
                if(!ok){VerificationFinished=true;Debug.LogError("SALOON_RECOVERY FAIL "+message);throw new InvalidOperationException(message);}
                Debug.Log("SALOON_RECOVERY_CHECK "+message);
            }
            IEnumerator Wait(Func<bool> condition,string message,float seconds=8)
            {
                float until=Time.realtimeSinceStartup+seconds;
                while(!condition()&&Time.realtimeSinceStartup<until){foreach(var link in links)link.Pump();yield return null;}
                Check(condition(),message);
            }
            try
            {
                for(int i=0;i<3;i++)
                {
                    var link=new RecoveryLink(ushort.Parse(Port));links.Add(link);
                    var peer=new GameObject("Recovery test peer").AddComponent<SaloonConnection>();peers.Add(peer);peer.Initialize();
                    peer.SnapshotReceived+=s=>states[peer]=s;peer.StartLan(false,"127.0.0.1",link.Port);
                }
                yield return Wait(()=>HumanCount==4&&peers.All(p=>p.Connected),"four players connect through independent UDP links");
                StartMatch();
                yield return Wait(()=>peers.All(p=>states.TryGetValue(p,out var s)&&s.phase==Phase.Discussion),"discussion reaches all four players");
                var interrupted=peers[0];var linkDown=links[0];
                linkDown.BlackoutUntil=Time.realtimeSinceStartup+13;
                interrupted.Send(new Command{kind="room",x=1});
                interrupted.Send(new Command{kind="room",x=2,name=peers[1].LocalId.ToString()});
                yield return Wait(()=>Time.realtimeSinceStartup>=linkDown.BlackoutUntil,"13-second blackout completes",15);
                Check(linkDown.Dropped>0&&peers.All(p=>p.Connected)&&HumanCount==4&&phase==Phase.Discussion,"all players remain in the match throughout the blackout");
                yield return Wait(()=>states.TryGetValue(interrupted,out var s)&&s.players.Any(p=>p.id==interrupted.LocalId.ToString()&&p.chosenRoom==2&&p.companion==peers[1].LocalId.ToString()),"queued room and companion choices reach the host after recovery");
                Check(interrupted.SnapshotAge<2,"fresh state resumes after the link recovers");
                foreach(var peer in peers){peer.Send(new Command{kind="room",x=0});peer.Send(new Command{kind="ready",x=1});}
                ChooseRoom(0,"");ToggleReady();
                yield return Wait(()=>phase==Phase.Turns&&peers.All(p=>states[p].phase==Phase.Turns),"all four players enter room actions after Ready");
                foreach(ulong actor in roomTurns[0].order)
                {
                    (actor==Connection.LocalId?Connection:peers.Single(p=>p.LocalId==actor)).Send(new Command{kind="pass"});
                    yield return Wait(()=>rounds.HasSpent(actor),"player "+actor+" receives and completes their turn");
                }
                Check(peers.All(p=>p.Connected)&&HumanCount==4,"all four players stay connected through the recovered round");
                Debug.Log("SALOON_RECOVERY PASS");VerificationPassed=VerificationFinished=true;
            }
            finally
            {
                foreach(var peer in peers){peer.Manager.Shutdown();Destroy(peer.Manager.gameObject);Destroy(peer.gameObject);}
                foreach(var link in links)link.Dispose();
            }
        }
    }
}
#endif
