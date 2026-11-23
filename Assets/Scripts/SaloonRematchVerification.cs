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
                if(!condition())foreach(var state in states)Debug.Log("SALOON_REMATCH_STATE connected="+state.Key.Connected+" status="+state.Key.Status+" id="+state.Value.ownId+" role="+state.Value.ownRole+" bag="+state.Value.moneyBag+" plan="+state.Value.escapePlanned+" pending="+state.Value.escapePending+" phase="+state.Value.phase);
                Check(condition(),message);
            }
            try
            {
                for(int i=0;i<7;i++)
                {
                    var peer=new GameObject("Rematch peer").AddComponent<SaloonConnection>();peers.Add(peer);
                    peer.Initialize();peer.AutoReconnect=true;peer.SnapshotReceived+=s=>states[peer]=s;
                    peer.StartLan(false,"127.0.0.1",ushort.Parse(Port));
                }
                yield return Wait(()=>HumanCount==8&&peers.All(p=>states.ContainsKey(p)),"eight real UDP players share the lobby");
                StartMatch();
                yield return Wait(()=>peers.All(p=>states[p].phase==Phase.Discussion),"match starts for everyone");
                int originalMedicalRoom=medicalRoom;
                Check(originalMedicalRoom==0&&peers.All(p=>states[p].medicalRoom==originalMedicalRoom),"all eight clients receive a quiet first round with no medical event");
                Check(peers.All(p=>(states[p].matchResults?.Length??0)==0),"the full results roster stays private until the match ends");
                var peer0=peers[0];ulong id=peer0.LocalId;var original=players[id];
                void GiveRole(Role role)
                {
                    if(original.role==role)return;
                    var other=players.Values.First(p=>p.role==role);other.role=original.role;original.role=role;
                }
                players.Values.Single(p=>SaloonRules.IsIndependent(p.role)).role=Role.Swindler;
                GiveRole(Role.Swindler);original.personalVictory=true;
                original.hp=40;original.ammo=3;original.cigaretteBuff=true;original.evidence.Add("Reconnect evidence");
                var originalRole=original.role;var originalPosition=original.body.transform.position;
                Connection.Manager.DisconnectClient(peer0.Manager.LocalClientId,"Reconnect verification");
                yield return Wait(()=>original.disconnectedUntil>0,"disconnected place is reserved");
                Check(MatchActive&&players[id]==original,"disconnect does not abort or remove the participant");
                yield return Wait(()=>peer0.Connected&&original.disconnectedUntil==0&&states[peer0].ownId==id.ToString()&&states[peer0].health==40&&states[peer0].cigaretteBuff&&peer0.SnapshotAge<2,"automatic reconnect reclaims the reserved place");
                Check(peer0.LocalId==id&&players[id]==original&&original.role==originalRole&&original.hp==40&&original.ammo==3&&original.evidence.Contains("Reconnect evidence")&&original.body.transform.position==originalPosition,"reconnect preserves identity, role, health, inventory, evidence and position");
                Check(HumanCount==8,"reconnect creates no duplicate player");
                Check(medicalRoom==originalMedicalRoom&&states[peer0].medicalRoom==originalMedicalRoom,"reconnecting cannot reroll or lose the featured medical room");
                yield return Wait(()=>states[peer0].personalVictory,"reconnect preserves the Swindler's earned personal victory");
                Check(peers.Where(p=>p!=peer0).All(p=>!states[p].personalVictory),"Swindler victory stays private across reconnect");
                // Partial work and the per-round cap survive a reconnect as well.
                GiveRole(Role.Outlaw);original.personalVictory=false;
                safeProgress=1;drawerRevision=1;lastSafeWorkRound=rounds.Round;Broadcast();
                Connection.Manager.DisconnectClient(peer0.Manager.LocalClientId,"Partial drawer reconnect");
                yield return Wait(()=>original.disconnectedUntil>0,"partial-work disconnect reserves the same player");
                yield return Wait(()=>peer0.Connected&&original.disconnectedUntil==0&&states[peer0].safeProgress==1,"reconnect retains the first shared theft step");
                Check(lastSafeWorkRound==rounds.Round&&players.Values.All(p=>!p.bag),"reconnect preserves round cap and cannot grant early money");
                original.bag=true;original.bagRound=rounds.Round-1;safeProgress=SaloonRules.DrawerWorkSteps;drawerRevision=SaloonRules.DrawerWorkSteps;
                original.safeReport="Reconnect drawer report";original.inspectedProgress=1;original.inspectedRevision=1;
                DeclareEscape(original);Broadcast();
                Check(original.escapePlanned,"carrier can plan escape before reconnect: phase="+phase+" round="+rounds.Round+" notice="+original.notice);
                Connection.Manager.DisconnectClient(peer0.Manager.LocalClientId,"Planned escape reconnect");
                yield return Wait(()=>original.disconnectedUntil>0,"carrier's escape plan reserves its disconnected seat");
                yield return Wait(()=>peer0.Connected&&original.disconnectedUntil==0&&states[peer0].moneyBag&&states[peer0].escapePlanned,"reconnect retains money and the announced escape plan");
                Check(original.bag&&original.bagRound==rounds.Round-1&&drawerRevision==SaloonRules.DrawerWorkSteps&&original.inspectedRevision==1&&states[peer0].safeReport=="Reconnect drawer report","drawer state and private inspection survive reconnect without duplicating money");
                yield return Wait(()=>peers.All(p=>states[p].escapeWarning.Contains(original.name)),"every client receives the escape warning before room lock");
                foreach(var person in players.Values){person.chosenRoom=0;person.companion=null;rounds.SetReady(person.id,true);}
                yield return Wait(()=>Time.time>=rounds.DiscussionHeldUntil,"escape warning leaves time for room changes",12);
                TickServer();rounds.EnsureDeadline(Time.time+120);Broadcast();
                yield return Wait(()=>phase==Phase.Turns&&states[peer0].escapePending,"planned escape starts after reconnect with fresh actions");
                Check(!rounds.HasSpent(id)&&states[peer0].actionAvailable&&players.Values.Where(p=>p!=original).All(p=>!rounds.HasSpent(p.id)),"reconnected carrier and every defender retain their normal action");
                OnCommand(id,new Command{kind="pass"});Broadcast();
                yield return Wait(()=>!states[peer0].actionAvailable,"carrier spends its normal action before reconnect");
                Connection.Manager.DisconnectClient(peer0.Manager.LocalClientId,"Active escape reconnect");
                yield return Wait(()=>original.disconnectedUntil>0,"active escape reserves the same carrier");
                yield return Wait(()=>peer0.Connected&&original.disconnectedUntil==0&&states[peer0].escapePending&&peer0.SnapshotAge<2,"active escape and public warning survive reconnect");
                Check(original.bag&&rounds.HasSpent(id)&&!states[peer0].actionAvailable&&players.Values.Count(p=>p.bag)==1,"reconnect cannot refund a spent action or duplicate the money");
                var fallenDeputy=players.Values.First(p=>p.role==Role.Deputy);
                fallenDeputy.hp=0;
                Finish("LAW WINS");
                yield return Wait(()=>peers.All(p=>states[p].phase==Phase.Finished),"results reach every player");
                Check(peers.All(p=>states[p].matchResults.Length==8&&states[p].matchResults.Count(entry=>entry.winner)==4
                    &&states[p].matchResults.Any(entry=>entry.role==Role.Sheriff&&entry.alive&&entry.winner)
                    &&states[p].matchResults.Any(entry=>entry.role==Role.Deputy&&!entry.alive&&entry.winner)),
                    "every client receives the whole cast and all Law winners, including a dead Deputy");
                peer0.Send(new Command{kind="rematch"});
                yield return Wait(()=>players[id].rematchVote,"client vote reaches host");
                peer0.Send(new Command{kind="rematch"});
                yield return Wait(()=>!players[id].rematchVote,"client can withdraw vote");
                Act("rematch");foreach(var peer in peers)peer.Send(new Command{kind="rematch"});
                yield return Wait(()=>phase==Phase.Discussion&&peers.All(p=>states[p].phase==Phase.Discussion),"unanimous vote starts another match for the same group");
                Check(players.Values.All(p=>!p.rematchVote)&&HumanCount==8,"new match clears all votes and keeps the group");
                Check(safeProgress==0&&drawerRevision==0&&lastSafeWorkRound==0&&players.Values.All(p=>!p.bag&&!p.escapePlanned&&p.escapeRound==0&&!p.personalVictory&&p.inspectedRevision==-1&&p.safeReport==""),"rematch clears money, escape, inspection and personal-win state");
                Check(peers.All(p=>(states[p].matchResults?.Length??0)==0),"the rematch clears the old results roster");
                Finish("RETURN TEST");yield return Wait(()=>states[peer0].phase==Phase.Finished,"client receives second results");
                peer0.Send(new Command{kind="lobby"});
                yield return Wait(()=>phase==Phase.Lobby&&peers.All(p=>states[p].phase==Phase.Lobby),"one return action sends everyone to the same lobby");
                Check(players.Values.All(p=>p.role==Role.Unknown&&p.hp==tuning.health&&!p.cigaretteBuff)&&HumanCount==8,"lobby resets roles and revives the same players");
                StartMatch();peer0.AutoReconnect=false;
                Connection.Manager.DisconnectClient(peer0.Manager.LocalClientId,"Expiry verification");
                yield return Wait(()=>original.disconnectedUntil>0,"second disconnect reserves the same stable identity");
                Check(!CanStartMatch,"reserved seats cannot be replaced by starting a match");
                original.disconnectedUntil=Time.realtimeSinceStartup-1;TickServer();
                Check(phase==Phase.Finished&&outcome.Contains("RECONNECT WINDOW EXPIRED")&&!players.ContainsKey(id),"expired reservation aborts match and releases the seat");
                Check(PrivateSnapshot(Connection.LocalId).matchResults.Length==8&&matchResults.Any(entry=>entry.id==id.ToString())
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
