#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyEndlessMatch()
        {
            verifying=AutomatedInput=true;VerificationPassed=VerificationFinished=false;
            int checks=0;
            void Check(bool ok,string message)
            {checks++;if(!ok){VerificationFinished=true;throw new InvalidOperationException("Endless match: "+message);}Debug.Log("SALOON_ENDLESS_CHECK "+message);}
            var host=players[Connection.LocalId];var carrier=players.Values.First(p=>p!=host);
            Connection.SnapshotReceived-=ApplySnapshot;
            try
            {
                ResetHeist();outcome="";duels.Clear();roomTurns.Clear();
                foreach(var p in players.Values)
                {p.hp=100;p.deathAt=-1;p.role=Role.Deputy;p.chosenRoom=p.room=0;p.companion=null;p.busyAction="";p.busyUntil=0;p.lastAction=-100;}
                host.role=Role.Sheriff;carrier.role=Role.Outlaw;
                rounds.Start(players.Keys,Time.time,30,30,0);phase=rounds.Phase;
                for(int n=1;n<=25;n++)
                {
                    foreach(var id in players.Keys)rounds.SetReady(id,true);
                    TickServer();Check(phase==Phase.Turns,"round "+n+" starts normally");
                    foreach(var id in players.Keys)rounds.TrySpend(id,Time.time);
                    TickServer();Check(phase==Phase.Aftermath&&pendingOutcome=="","round "+n+" has no automatic ending");
                    rounds.BeginAftermath(Time.time-4);TickServer();
                    rounds.Tick(rounds.Deadline,id=>players[id].Alive);phase=rounds.Phase;
                    Check(phase==Phase.Discussion&&rounds.Round==n+1,"round "+(n+1)+" remains playable");
                }
                Check(GetComponents<MonoBehaviour>().All(c=>!c.GetType().Name.Contains("Posse")),"no countdown audio component is installed");
                Check(Resources.Load<AudioClip>("Audio/Recorded/Hoofbeats")==null&&Resources.Load<AudioClip>("Audio/Recorded/DistantShot")==null,"arrival-only recordings are no longer bundled");
                var wire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(host.id),out _));
                Check(wire.round==26&&!wire.ToWireJson().ToLowerInvariant().Contains("posse"),"late-round snapshots have no arrival or countdown state");
                carrier.bag=true;carrier.bagRound=rounds.Round-1;safeProgress=SaloonRules.DrawerWorkSteps;
                DeclareEscape(carrier);Check(carrier.escapePlanned,"round 26 accepts an escape plan");
                foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(rounds.DiscussionHeldUntil,id=>players[id].Alive);phase=rounds.Phase;PositionEveryone(false);BeginPlannedEscapes();
                Check(carrier.escapeRound==26&&!rounds.HasSpent(carrier.id),"late escape starts and preserves the carrier action");
                Check(players.Values.Where(p=>p!=carrier).All(p=>!rounds.HasSpent(p.id)),"late escape leaves every defender a fresh action");
                rounds.Tick(rounds.Deadline,id=>players[id].Alive);phase=rounds.Phase;ResolveRoundEnding();
                Check(phase==Phase.Finished&&outcome=="OUTLAWS WIN — MONEY ESCAPED","round 26 escape wins through normal rules");
            }
            finally{Connection.SnapshotReceived+=ApplySnapshot;}
            Broadcast();yield return null;
            Debug.Log("SALOON_ENDLESS PASS "+checks+" checks");VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
