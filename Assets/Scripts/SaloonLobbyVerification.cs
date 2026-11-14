#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        // Real UDP clients exercise approval, reliable commands and private snapshots.
        // They share this Editor process; no player builds are required.
        public IEnumerator VerifyEditorLobby()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=false;VerificationPassed=false;
            var clients=new List<SaloonConnection>();
            var snapshots=new Dictionary<SaloonConnection,Snapshot>();
            void Cleanup()
            {
                foreach(var c in clients)
                {
                    if(c==null)continue;
                    c.Manager.Shutdown();Destroy(c.Manager.gameObject);Destroy(c.gameObject);
                }
                clients.Clear();
            }
            void Check(bool ok,string message)
            {
                if(ok){Debug.Log("SALOON_LOBBY_CHECK "+message);return;}
                Cleanup();VerificationFinished=true;Debug.LogError("SALOON_LOBBY FAIL "+message);throw new InvalidOperationException(message);
            }
            IEnumerator Wait(Func<bool> condition,string message)
            {
                float until=Time.realtimeSinceStartup+8;
                while(!condition()&&Time.realtimeSinceStartup<until)yield return null;
                Check(condition(),message);
            }
            SaloonConnection Connect()
            {
                var c=new GameObject("Lobby verification peer").AddComponent<SaloonConnection>();
                c.Initialize();clients.Add(c);c.SnapshotReceived+=s=>snapshots[c]=s;
                c.StartLan(false,"127.0.0.1",ushort.Parse(Port));return c;
            }
            void Deck(int count)
            {
                Check(players.Count==count&&players.Values.Count(p=>p.role==Role.Sheriff)==1&&players.Values.Count(p=>p.role==Role.Deputy)==1&&players.Values.Count(p=>p.role==Role.Renegade)==1&&players.Values.Count(p=>p.role==Role.Outlaw)==count-3,count+"-player match has the exact requested roles");
            }
            IEnumerator PlayRound(int count)
            {
                yield return Wait(()=>clients.All(c=>c.Connected),"LAN clients finish their connection handshake");
                foreach(var c in clients)c.Send(new Command{kind="voice",name="editor-voice-"+c.LocalId});
                yield return Wait(()=>clients.All(c=>players[c.LocalId].voiceId=="editor-voice-"+c.LocalId),"voice identities arrive over reliable LAN commands");
                StartMatch();Deck(count);
                yield return Wait(()=>clients.All(c=>snapshots.TryGetValue(c,out var s)&&s.phase==Phase.Discussion&&s.players.Length==count),count+"-player discussion reaches all LAN clients");
                Check(clients.All(c=>snapshots[c].ownRole==players[c.LocalId].role.ToString()&&snapshots[c].players.Count(p=>p.sheriff)==1&&snapshots[c].players.All(p=>SaloonRules.KnowsRole(players[c.LocalId].role,players[ulong.Parse(p.id)].role)?p.revealedRole==players[ulong.Parse(p.id)].role.ToString():p.revealedRole=="")&&!snapshots[c].botsEnabled),"private roles and no-bots mode survive the wire");
                Check(clients.All(c=>snapshots[c].players.Where(p=>p.id!=LocalId).All(p=>p.voiceId=="editor-voice-"+p.id)),"all LAN clients map each voice ID to the correct avatar");
                var victim=players[clients[0].LocalId];TakeDamage(victim,Vector3.forward);Broadcast();
                yield return Wait(()=>clients.All(c=>snapshots[c].players.Single(p=>p.id==victim.id.ToString()).hits==1),"every LAN client receives the victim's hit sequence");
                Check(clients.All(c=>snapshots[c].players.Single(p=>p.id==victim.id.ToString()).hitZ>.99f),"every LAN client receives the hit direction");
                foreach(var c in clients){c.Send(new Command{kind="room",x=0});c.Send(new Command{kind="ready",x=1});}
                Connection.Send(new Command{kind="room",x=0});Connection.Send(new Command{kind="ready",x=1});
                yield return Wait(()=>phase==Phase.Turns&&clients.All(c=>snapshots.TryGetValue(c,out var s)&&s.phase==Phase.Turns),"Ready starts room actions for every LAN client");
                Check(players.Values.All(p=>p.room==0&&p.seat>=100)&&players.Values.Select(p=>p.seat).Distinct().Count()==count,"every player has a separate bar seat");
                Check(!roomTurns.ContainsKey(0)&&players.Values.All(p=>PrivateSnapshot(p.id).actionAvailable),"every Bar player can act immediately");
                var walker=players.Values.Last();var walkerConnection=walker.id==Connection.LocalId?Connection:clients.First(c=>c.LocalId==walker.id);
                walkerConnection.Send(new Command{kind="stance"});
                yield return Wait(()=>walker.seat<0,"a LAN player can stand before acting");
                Vector3 start=walker.body.transform.position;float walkingUntil=Time.time+.3f;
                while(Time.time<walkingUntil){walkerConnection.Send(new Command{kind="input",z=-1,yaw=0,pitch=0,poseVersion=walker.poseVersion});yield return null;}
                walkerConnection.Send(new Command{kind="input",poseVersion=walker.poseVersion});
                Check(Vector3.Distance(start,walker.body.transform.position)>.2f,"LAN player moves through host-authoritative collision");
                yield return Wait(()=>clients.All(c=>!snapshots[c].players.Single(p=>p.id==walker.id.ToString()).seated),"all LAN clients receive the standing pose");
                for(int i=0;i<count;i++)
                {
                    ulong actor=players.Keys.Reverse().ElementAt(i);
                    Check(PrivateSnapshot(actor).actionAvailable,"player "+actor+" can act without waiting at the Bar");
                    (actor==Connection.LocalId?Connection:clients.First(c=>c.LocalId==actor)).Send(new Command{kind="pass"});
                    yield return Wait(()=>rounds.HasSpent(actor),"LAN pass spends only that player's action");
                }
                Check(clients.All(c=>c.Connected)&&Connection.Manager.ConnectedClientsIds.Count==count,"all "+count+" players stay connected through the round");
                Finish("LOBBY VERIFICATION: ROUND COMPLETE");
            }

            Check(Hosting&&HumanCount==1&&!BotsEnabled,"LAN starts with one human and no bots");
            Check(!CanStartMatch,"one human cannot start a no-bots match");
            for(int n=2;n<=4;n++){Connect();int expected=n;yield return Wait(()=>HumanCount==expected,"human "+n+" joins over UDP");}
            Check(CanStartMatch,"four humans can start without filling a fifth place");
            MenuOpen=true;Broadcast();yield return new WaitForEndOfFrame();
            string directory=Path.Combine(Path.GetTempPath(),"SaloonPrototype-EditorPreview");Directory.CreateDirectory(directory);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"lobby-four-no-bots.png"));
            yield return PlayRound(4);
            Connect();yield return Wait(()=>HumanCount==5&&phase==Phase.Lobby,"fifth human reopens the lobby after a match");
            Check(CanStartMatch,"five humans can start without bots");yield return PlayRound(5);
            for(int n=6;n<=8;n++)
            {
                Connect();int expected=n;yield return Wait(()=>HumanCount==expected,"human "+n+" joins within the eight-place lobby");
                Check(!CanStartMatch,"undefined "+n+"-player deck cannot start");StartMatch();Check(phase==Phase.Lobby,"unsupported start keeps everyone in the lobby");
            }
            yield return Wait(()=>clients.All(c=>snapshots.TryGetValue(c,out var s)&&s.players.Length==8&&s.lobbyCapacity==8),"eight-player lobby roster reaches all peers");
            Check(players.Values.Select(p=>p.body.transform.position).Distinct().Count()==8,"eight lobby participants have separate spawn positions");
            var ninth=Connect();yield return Wait(()=>!ninth.Manager.IsListening,"ninth player is rejected");
            Check(ninth.Status.Contains("full")&&HumanCount==8&&clients.Where(c=>c!=ninth).All(c=>c.Connected),"full-lobby rejection preserves all eight existing connections");
            Cleanup();yield return Wait(()=>HumanCount==1,"disconnecting test clients leaves only the host");
            SetBotsEnabled(true);SetBotMatchSize(4);
            Check(PlayerCount==1&&CanStartMatch,"bots wait until Start so friends can join first");
            MenuOpen=true;Broadcast();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"lobby-four-with-bots.png"));
            StartMatch();Deck(4);Check(HumanCount==1&&players.Values.Count(p=>p.bot)==3,"four-player bot mode fills exactly three missing places");
            Finish("LOBBY VERIFICATION: BOTS");SetBotsEnabled(false);
            Check(phase==Phase.Lobby&&PlayerCount==1&&!CanStartMatch,"turning bots off removes them and prevents an underfilled start");
            StartMatch();Check(PlayerCount==1&&phase==Phase.Lobby,"no-bots Start never silently fills places");
            SetBotsEnabled(true);SetBotMatchSize(5);StartMatch();Deck(5);
            Check(players.Values.Count(p=>p.bot)==4,"five-player bot mode fills exactly four missing places");
            Finish("LOBBY VERIFICATION: BOT REPLACEMENT");Connect();
            yield return Wait(()=>HumanCount==2&&phase==Phase.Lobby,"a friend can join after a practice match");
            Check(PlayerCount==5&&players.Values.Count(p=>p.bot)==3,"a joining human replaces one bot");
            SetBotMatchSize(4);StartMatch();Deck(4);
            Check(HumanCount==2&&players.Values.Count(p=>p.bot)==2,"switching from five to four trims only surplus bots");
            Finish("LOBBY VERIFICATION COMPLETE");Cleanup();
            Debug.Log("SALOON_LOBBY PASS real LAN four/five-player rounds, exact roles, eight-place capacity, ninth rejection, bot settings and replacement; previews="+directory);
            VerificationPassed=true;VerificationFinished=true;
        }
    }
}
#endif
