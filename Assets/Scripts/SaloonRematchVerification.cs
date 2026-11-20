#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyRematchAndReconnect()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            var peers=new List<SaloonConnection>();
            var states=new Dictionary<SaloonConnection,Snapshot>();
            void Check(bool ok,string message)
            {
                if(!ok){VerificationFinished=true;Debug.LogError("SALOON_REMATCH FAIL "+message);throw new InvalidOperationException(message);}
                Debug.Log("SALOON_REMATCH_CHECK "+message);
            }
            IEnumerator Wait(Func<bool> condition,string message,float seconds=10)
            {
                float until=Time.realtimeSinceStartup+seconds;
                while(!condition()&&Time.realtimeSinceStartup<until)yield return null;
                Check(condition(),message);
            }
            try
            {
                for(int i=0;i<3;i++)
                {
                    var peer=new GameObject("Rematch peer").AddComponent<SaloonConnection>();peers.Add(peer);
                    peer.Initialize();peer.AutoReconnect=true;peer.SnapshotReceived+=s=>states[peer]=s;
                    peer.StartLan(false,"127.0.0.1",ushort.Parse(Port));
                }
                yield return Wait(()=>HumanCount==4&&peers.All(p=>states.ContainsKey(p)),"four real UDP players share the lobby");
                StartMatch();
                yield return Wait(()=>peers.All(p=>states[p].phase==Phase.Discussion),"match starts for everyone");
                Check(peers.All(p=>(states[p].matchResults?.Length??0)==0),"the full results roster stays private until the match ends");
                var peer0=peers[0];ulong id=peer0.LocalId;var original=players[id];
                original.hp=40;original.ammo=3;original.cigaretteBuff=true;original.evidence.Add("Reconnect evidence");
                var originalRole=original.role;var originalPosition=original.body.transform.position;
                Connection.Manager.DisconnectClient(peer0.Manager.LocalClientId,"Reconnect verification");
                yield return Wait(()=>original.disconnectedUntil>0,"disconnected place is reserved");
                Check(MatchActive&&players[id]==original,"disconnect does not abort or remove the participant");
                yield return Wait(()=>peer0.Connected&&original.disconnectedUntil==0&&states[peer0].ownId==id.ToString()&&states[peer0].health==40&&states[peer0].cigaretteBuff&&peer0.SnapshotAge<2,"automatic reconnect reclaims the reserved place");
                Check(peer0.LocalId==id&&players[id]==original&&original.role==originalRole&&original.hp==40&&original.ammo==3&&original.evidence.Contains("Reconnect evidence")&&original.body.transform.position==originalPosition,"reconnect preserves identity, role, health, inventory, evidence and position");
                Check(HumanCount==4,"reconnect creates no duplicate player");
                var sheriff=players.Values.Single(p=>p.role==Role.Sheriff);
                sheriff.hp=0;
                Finish("LAW WINS — POSSE ARRIVED");
                yield return Wait(()=>peers.All(p=>states[p].phase==Phase.Finished),"results reach every player");
                Check(peers.All(p=>states[p].matchResults.Length==4&&states[p].matchResults.Count(entry=>entry.winner)==2
                    &&states[p].matchResults.Any(entry=>entry.role==Role.Sheriff&&!entry.alive&&entry.winner)
                    &&states[p].matchResults.Any(entry=>entry.role==Role.Deputy&&entry.alive&&entry.winner)),
                    "every client receives the whole cast and both Law winners, including the dead Sheriff");
                peer0.Send(new Command{kind="rematch"});
                yield return Wait(()=>players[id].rematchVote,"client vote reaches host");
                peer0.Send(new Command{kind="rematch"});
                yield return Wait(()=>!players[id].rematchVote,"client can withdraw vote");
                Act("rematch");foreach(var peer in peers)peer.Send(new Command{kind="rematch"});
                yield return Wait(()=>phase==Phase.Discussion&&peers.All(p=>states[p].phase==Phase.Discussion),"unanimous vote starts another match for the same group");
                Check(players.Values.All(p=>!p.rematchVote)&&HumanCount==4,"new match clears all votes and keeps the group");
                Check(peers.All(p=>(states[p].matchResults?.Length??0)==0),"the rematch clears the old results roster");
                Finish("RETURN TEST");yield return Wait(()=>states[peer0].phase==Phase.Finished,"client receives second results");
                peer0.Send(new Command{kind="lobby"});
                yield return Wait(()=>phase==Phase.Lobby&&peers.All(p=>states[p].phase==Phase.Lobby),"one return action sends everyone to the same lobby");
                Check(players.Values.All(p=>p.role==Role.Unknown&&p.hp==tuning.health&&!p.cigaretteBuff)&&HumanCount==4,"lobby resets roles and revives the same players");
                StartMatch();peer0.AutoReconnect=false;
                Connection.Manager.DisconnectClient(peer0.Manager.LocalClientId,"Expiry verification");
                yield return Wait(()=>original.disconnectedUntil>0,"second disconnect reserves the same stable identity");
                Check(!CanStartMatch,"reserved seats cannot be replaced by starting a match");
                original.disconnectedUntil=Time.realtimeSinceStartup-1;TickServer();
                Check(phase==Phase.Finished&&outcome.Contains("RECONNECT WINDOW EXPIRED")&&!players.ContainsKey(id),"expired reservation aborts match and releases the seat");
                Check(PrivateSnapshot(Connection.LocalId).matchResults.Length==4&&matchResults.Any(entry=>entry.id==id.ToString())
                    &&matchResults.All(entry=>!entry.winner),"the aborted match shows the departed player and has no winners");
                Debug.Log("SALOON_REMATCH PASS");VerificationPassed=VerificationFinished=true;
            }
            finally
            {
                foreach(var peer in peers)
                {var cleanup=peer.Disconnect();if(peer.Manager!=null)Destroy(peer.Manager.gameObject);Destroy(peer.gameObject);}
            }
        }
    }
}
#endif
