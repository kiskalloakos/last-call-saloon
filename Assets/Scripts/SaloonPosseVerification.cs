using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public System.Collections.IEnumerator VerifyPosseArrival()
        {
            verifying=true;AutomatedInput=true;VerificationPassed=VerificationFinished=false;
            void Check(bool ok,string text)
            {
                if(!ok){VerificationFinished=true;throw new InvalidOperationException("Posse: "+text);}
                Debug.Log("SALOON_POSSE_CHECK "+text);
            }
            var host=players[Connection.LocalId];var robber=players.Values.First(p=>p.id!=host.id);
            void Reset()
            {
                ResetHeist();outcome="";duels.Clear();roomTurns.Clear();
                foreach(var p in players.Values)
                {
                    p.hp=60;p.deathAt=-1;p.role=Role.Deputy;p.chosenRoom=0;p.room=0;p.busyUntil=0;p.busyAction="";
                    p.lastAction=-100;p.hitbox.enabled=true;p.companion=null;
                }
                host.role=Role.Sheriff;robber.role=Role.Outlaw;
                rounds.Start(players.Keys,Time.time,30,30,1,1,1);phase=rounds.Phase;PositionEveryone(true);
            }
            void Ready()
            {
                foreach(var p in players.Values.Where(p=>p.Alive))rounds.SetReady(p.id,true);
                TickServer();Check(phase==Phase.Turns,"all living players start round "+rounds.Round);
                // Isolate global deadline from the room minigames, which have separate fixtures.
                duels.Clear();roomTurns.Clear();
            }
            void EndActions()
            {
                foreach(var p in players.Values.Where(p=>p.Alive))rounds.TrySpend(p.id,Time.time);
                TickServer();Check(phase==Phase.Aftermath,"round "+rounds.Round+" retains aftermath");
            }
            void NextRound()
            {
                rounds.BeginAftermath(Time.time-4);TickServer();
                Check(phase==Phase.Regroup,"completed round regroups without a terminal result");
                rounds.Tick(rounds.Deadline,id=>players[id].Alive);phase=rounds.Phase;
                Check(phase==Phase.Discussion,"next round waits for Ready");
            }
            void ReachFinalRound()
            {
                for(int n=1;n<SaloonPosse.ArrivalRound;n++)
                {
                    Ready();EndActions();Check(string.IsNullOrEmpty(pendingOutcome),"no arrival before round ten");
                    NextRound();
                    var wire=Snapshot.FromWireJson(PrivateSnapshot(host.id).ToWireJson());
                    Check(wire.posseStage==(n<7?0:n-6),"host warning reaches clients after round "+n);
                    Check(wire.posseRoundsRemaining==(n<7?0:10-n),"client countdown leaves three complete rounds");
                }
                Ready();
            }
            Reset();ReachFinalRound();
            robber.bag=true;DeclareEscape(robber);
            Check(robber.escapeRound==0&&rounds.CanAct(robber.id,Time.time)&&robber.notice.Contains("too late"),"round ten refuses an impossible escape without spending the action");
            EndActions();Check(pendingOutcome=="LAW WINS — POSSE ARRIVED"&&outcome=="","arrival queues Law victory after final actions");
            var final=PrivateSnapshot(host.id);Check(final.posseStage==4&&final.posseRoundsRemaining==0&&final.players.All(p=>string.IsNullOrEmpty(p.revealedRole)),"arrival snapshot preserves hidden roles until aftermath finishes");
            Broadcast();yield return new WaitForSeconds(3.15f);
            Check(phase==Phase.Finished&&outcome=="LAW WINS — POSSE ARRIVED","posse ends the match after effects");
            Check(PrivateSnapshot(host.id).players.All(p=>!string.IsNullOrEmpty(p.revealedRole)),"arrival reveals roles on the results screen");
            Reset();Check(PrivateSnapshot(host.id).posseStage==0,"new match clears the posse countdown");
            ReachFinalRound();robber.bag=true;robber.escapeRound=9;
            EndActions();Check(pendingOutcome=="OUTLAWS WIN — MONEY ESCAPED","round nine escape completes on round ten before the posse");
            Reset();ReachFinalRound();
            foreach(var p in players.Values)p.role=Role.Outlaw;host.role=Role.Renegade;
            EndActions();Check(pendingOutcome=="DRAW — POSSE CAPTURED THE SURVIVORS","no surviving Law and no completed escape means capture, not a criminal win");
            Reset();ReachFinalRound();QueueFinish("RENEGADE WINS — LAST STANDING");ResolveRoundEnding();
            Check(pendingOutcome=="RENEGADE WINS — LAST STANDING","arrival never overrides a terminal elimination result");
            VerificationPassed=VerificationFinished=true;Debug.Log("SALOON_POSSE PASS");
        }
#endif
    }
}
