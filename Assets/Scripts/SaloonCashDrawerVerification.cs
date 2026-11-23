#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyCashDrawer()
        {
            verifying=AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            int checks=0;
            void Check(bool ok,string message)
            {
                checks++;
                if(!ok){VerificationFinished=true;throw new InvalidOperationException("Cash drawer: "+message);}
                Debug.Log("SALOON_DRAWER_CHECK "+message);
            }
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p!=host);
            void Place(Participant p,Vector3 position)
            {p.controller.enabled=false;p.seat=-1;p.body.transform.position=position;p.move=Vector2.zero;p.poseVersion++;UpdatePlayerColliders(p);Physics.SyncTransforms();}
            void Window(Role role=Role.Outlaw)
            {
                ResetHeist();pendingOutcome=outcome="";duels.Clear();roomTurns.Clear();
                foreach(var p in players.Values)
                {p.hp=100;p.deathAt=-1;p.role=Role.Outlaw;p.chosenRoom=p.room=0;p.companion=null;p.busyAction="";p.busyUntil=0;p.lastAction=-100;p.nextShot=0;p.ammo=6;p.combatHistory.Clear();p.drawn=false;}
                host.role=role;
                rounds.Start(players.Keys,Time.time,30,30,0);foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(Time.time,id=>players[id].Alive);phase=rounds.Phase;PositionEveryone(false);
                Place(host,SafeApproach);host.lastAction=-100;
            }
            void NextDiscussion()
            {
                rounds.BeginAftermath(Time.time-10);rounds.Tick(Time.time,id=>players[id].Alive);
                rounds.Tick(Time.time+4,id=>players[id].Alive);phase=rounds.Phase;
            }
            void StartActions()
            {
                foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(Mathf.Max(Time.time,rounds.DiscussionHeldUntil),id=>players[id].Alive);phase=rounds.Phase;PositionEveryone(false);BeginPlannedEscapes();
            }
            // Logic fixtures do not repeatedly build and destroy rendered corpses.
            Connection.SnapshotReceived-=ApplySnapshot;
            try
            {
                Check(GameObject.Find("Cash drawer")!=null&&GameObject.Find("Safe visual")==null,"cash drawer replaces the standalone safe in a saved scene");
                Check(!SaloonBeerTaps.WithinReach(SafeApproach,tuning.interactionDistance),"drawer users cannot also drink from the separated beer tray");
                Check(!AtSafe(new Vector3(SaloonBeerTaps.Position.x,.08f,2.05f)),"beer customers use a separate counter position from the drawer");
                Check(GameObject.Find("Bartender")==null&&GetComponents<MonoBehaviour>().All(c=>!c.GetType().Name.Contains("Bartender")),"bartender character and behaviour are absent");
                foreach(var role in new[]{Role.Sheriff,Role.Deputy,Role.Outlaw,Role.Renegade,Role.Swindler})
                {
                    Window(role);InspectSafe(host);
                    Check(host.hp==100&&host.busyAction=="inspect"&&rounds.HasSpent(host.id),role+" can inspect at full health as one action");
                    var publicView=PrivateSnapshot(peer.id).players.Single(p=>p.id==host.id.ToString());
                    Check(publicView.checkingDrawer&&publicView.busy&&!publicView.robbing,"inspection publishes only shared checking activity");
                    yield return new WaitForSeconds(SaloonRules.DrawerInteractionSeconds+.1f);
                    Check(host.safeReport.Contains("money present")&&host.busyUntil==0,"two-second inspection completes with a private report");
                    Check(string.IsNullOrEmpty(PrivateSnapshot(peer.id).safeReport),"inspection report never goes to another recipient");
                }
                Window();Place(host,new Vector3(0,.08f,-2));BeginRobbery(host);
                Check(!rounds.HasSpent(host.id)&&host.busyUntil==0,"distant theft is rejected without action loss");
                Place(host,SafeApproach);BeginRobbery(host);Place(host,new Vector3(0,.08f,-2));TickSafeWork(host);
                Check(!host.bag&&host.busyUntil==0&&rounds.HasSpent(host.id),"walking away interrupts a check and keeps its action spent");
                Window();Place(peer,SafeApproach+Vector3.right*.4f);peer.lastAction=-100;
                BeginRobbery(host);BeginRobbery(peer);
                var theftView=PrivateSnapshot(players.Values.Last().id).players.Single(p=>p.id==host.id.ToString());
                Check(theftView.checkingDrawer&&theftView.busy&&!theftView.robbing&&!theftView.moneyBag,"theft has the same public check state and no carrier label");
                yield return new WaitForSeconds(SaloonRules.DrawerInteractionSeconds+.1f);
                Check(players.Values.All(p=>!p.bag)&&safeProgress==1,"competing first checks complete only one shared step and give no money");
                CompleteRobbery(host);CompleteRobbery(peer);
                Check(safeProgress==1&&players.Values.All(p=>!p.bag),"duplicate completions cannot rush the shared round limit");
                var idleOutlaw=players.Values.Last(p=>p!=host&&p!=peer);idleOutlaw.botThink=Time.time-1;
                foreach(var p in players.Values.Where(p=>p.bot&&p!=idleOutlaw))p.botThink=Time.time+100;
                Check(!BotWantsSafe(idleOutlaw),"bots stop walking to a drawer already worked this round");
                TickPracticeBots();Check(rounds.HasSpent(idleOutlaw.id),"an idle Outlaw bot passes instead of repeatedly attempting a locked theft step");
                var inspector=players.Values.First(p=>p!=host&&p!=peer);inspector.role=Role.Deputy;
                Place(inspector,SafeApproach+Vector3.left*.4f);InspectSafe(inspector);
                yield return new WaitForSeconds(SaloonRules.DrawerInteractionSeconds+.1f);
                Check(inspector.safeReport.Contains("money present")&&inspector.safeReport.Contains("tampered"),"partial theft leaves money present and a private tampering clue");
                for(int step=2;step<=SaloonRules.DrawerWorkSteps;step++)
                {
                    NextDiscussion();StartActions();Place(host,SafeApproach);Place(peer,SafeApproach+Vector3.right*.4f);
                    host.lastAction=peer.lastAction=-100;
                    var worker=step==2?peer:host;var other=step==2?host:peer;
                    BeginRobbery(worker);BeginRobbery(other);
                    yield return new WaitForSeconds(SaloonRules.DrawerInteractionSeconds+.1f);
                    Check(safeProgress==step&&players.Values.Count(p=>p.bag)==(step==3?1:0),"round "+rounds.Round+" completes step "+step+" with at most one carrier");
                }
                Check(rounds.Round==3&&host.bag&&!peer.bag,"teammates share work across three rounds and the third worker collects the money");
                var carrier=players.Values.Single(p=>p.bag);
                var bagWire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(carrier.id),out _));
                Check(bagWire.moneyBag&&bagWire.safeProgress==SaloonRules.DrawerWorkSteps,"completed three-step theft survives compressed snapshot serialization");
                DeclareEscape(carrier);Check(!carrier.escapePlanned&&carrier.escapeRound==0,"an action-phase declaration cannot create a late escape");
                NextDiscussion();DeclareEscape(carrier);
                Check(rounds.Round==4&&carrier.escapePlanned&&!rounds.IsReady(carrier.id),"first escape can be planned in round four, never round two");
                var defender=players.Values.First(p=>p!=carrier);
                var warningWire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(defender.id),out _));
                Check(!warningWire.escapePlanned&&warningWire.escapeWarning.Contains(carrier.name)&&warningWire.players.Single(p=>p.id==carrier.id.ToString()).escaping,"escape announcement reaches defenders during discussion through the real wire format");
                foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(Time.time,id=>players[id].Alive);
                Check(rounds.Phase==Phase.Discussion&&rounds.DiscussionHeldUntil>=Time.time+9.9f,"everyone locking cannot skip ten seconds of escape planning response");
                MenuOpen=ShowRole=false;RoomPlannerOpen=true;roleIntroStarted=float.NegativeInfinity;
                foreach(var p in players.Values)p.name=p==host?"Preview player":"Practice player "+p.id;
                ApplySnapshot(PrivateSnapshot(host.id));yield return null;if(!Application.isBatchMode)yield return new WaitForEndOfFrame();
                string preview=Path.Combine(Path.GetTempPath(),"lastcall-cash-drawer");Directory.CreateDirectory(preview);
                ScreenCapture.CaptureScreenshot(Path.Combine(preview,"EscapePlanning.png"));yield return null;RoomPlannerOpen=false;
                StartActions();
                Check(carrier.escapeRound==rounds.Round&&players.Values.All(p=>!rounds.HasSpent(p.id)),"escape starts with the carrier and every defender action unused");
                Check(PrivateSnapshot(peer.id).escapeWarning.Contains(carrier.name),"all recipients receive the public escape warning");
                TakeDamage(carrier,Vector3.back,SaloonHealth.RevolverDamage);
                Check(carrier.Alive&&carrier.bag&&carrier.escapeRound==0,"one nonlethal headshot cancels escape without taking away the money");
                ResolveEscapes();Check(pendingOutcome=="","interrupted attempt cannot resolve as a win");
                NextDiscussion();DeclareEscape(carrier);StartActions();
                var carrierWire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(carrier.id),out _));
                Check(carrierWire.escapePending&&carrierWire.actionAvailable&&carrierWire.actionsRemaining==players.Count&&carrierWire.actionsInProgress==0,"wire grants the escaping carrier a normal action without a phantom busy action");
                foreach(var p in players.Values.Where(p=>p!=carrier))
                {p.drawn=true;p.pitch=-89;p.nextShot=0;p.lastAction=-100;Fire(p);}
                TickServer();
                Check(phase==Phase.Turns&&PrivateSnapshot(carrier.id).actionAvailable&&players.Values.Where(p=>p!=carrier).All(p=>p.shots>0)&&carrier.escapeRound==rounds.Round,"real defender misses leave the escaping carrier its action");
                int shots=carrier.shots;carrier.drawn=true;carrier.pitch=-89;carrier.nextShot=0;carrier.lastAction=-100;Fire(carrier);TickServer();
                Check(carrier.shots==shots+1&&phase==Phase.Finished&&outcome=="OUTLAWS WIN — MONEY ESCAPED"&&pendingOutcome=="","carrier can shoot after all misses and go straight to victory without aftermath");
                var resultWire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(peer.id),out _));
                Check(resultWire.phase==Phase.Finished&&resultWire.outcome==outcome&&resultWire.matchResults.Length==players.Count&&resultWire.matchResults.All(p=>p.winner==SaloonRules.CanRob(p.role)),"immediate escape results replicate the complete roster and identify Outlaw winners");
                ApplySnapshot(PrivateSnapshot(host.id));yield return null;if(!Application.isBatchMode)yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(preview,"EscapeVictory.png"));yield return null;
                void HealingEscape()
                {
                    Window();host.bag=true;host.bagRound=rounds.Round;NextDiscussion();DeclareEscape(host);StartActions();
                    host.hp=60;MoveToBeerTrayForVerification(host);host.lastAction=-100;DrinkBeer(host);
                    foreach(var p in players.Values.Where(p=>p!=host))rounds.TrySpend(p.id,Time.time);
                }
                float drinkSeconds=tuning.drinkSeconds;tuning.drinkSeconds=.4f;
                HealingEscape();TickServer();
                Check(phase==Phase.Turns&&host.busyAction=="drink"&&host.escapeRound==rounds.Round,"escaping carrier may heal and results wait for the drink");
                yield return new WaitForSeconds(.55f);
                Check(host.hp==80&&phase==Phase.Finished&&outcome=="OUTLAWS WIN — MONEY ESCAPED","carrier healing completes before the immediate escape victory");
                HealingEscape();TakeDamage(host,Vector3.back,SaloonHealth.RevolverDamage);TickServer();
                Check(phase==Phase.Turns&&host.busyAction=="drink"&&host.escapeRound==0,"headshot interruption preserves a carrier drink already underway");
                yield return new WaitForSeconds(.55f);
                Check(host.hp==60&&phase==Phase.Aftermath&&outcome==""&&pendingOutcome=="","interrupted escape finishes healing and ends normally without a false victory");
                tuning.drinkSeconds=drinkSeconds;
                Window();host.bag=true;safeProgress=SaloonRules.DrawerWorkSteps;TakeDamage(host,Vector3.back,SaloonHealth.Maximum);
                Check(!host.bag&&safeProgress==0&&drawerRevision==1,"eliminating a carrier returns the money to the drawer");
                Window(Role.Sheriff);InspectSafe(host);
                yield return new WaitForSeconds(SaloonRules.DrawerInteractionSeconds+.1f);
                int revision=host.inspectedRevision;drawerRevision+=2;safeProgress=0;
                CompleteInspection(host);Check(host.inspectedRevision>revision&&host.safeReport.Contains("changed since"),"inspection detects a theft and return even when money is present again");
                Window(Role.Sheriff);peer.bag=true;peer.bagRound=rounds.Round;NextDiscussion();
                var guard=players.Values.First(p=>p.bot&&p!=peer);guard.role=Role.Deputy;
                PlanPracticeBots();
                Check(peer.escapePlanned&&rounds.IsReady(peer.id),"bot carrier announces escape during discussion");
                Check(guard.chosenRoom==0&&guard.companion==null&&rounds.IsReady(guard.id),"defending bot chooses Bar in response to the public warning");
                StartActions();duels.Clear();roomTurns.Clear();
                Place(guard,new Vector3(0,.08f,0));Place(peer,new Vector3(0,.08f,1.5f));
                foreach(var p in players.Values.Where(p=>p!=guard&&p!=peer))Place(p,new Vector3(-5,.08f,-.5f));
                guard.hp=peer.hp=100;guard.botThink=Time.time-1;guard.drawn=true;guard.busyUntil=0;guard.nextShot=0;guard.lastAction=-100;
                TickPracticeBots();yield return new WaitForSeconds(.35f);TickPracticeBots();
                Check(peer.hp==80&&peer.bag&&peer.escapeRound==0&&rounds.HasSpent(guard.id),"defending bot lands a real shot and interrupts escape without killing the carrier");
                Window(Role.Sheriff);
            }
            finally { Connection.SnapshotReceived+=ApplySnapshot; }
            // Leave one normal rendered frame and a usable picture of the new counter.
            MenuOpen=RoomPlannerOpen=ShowRole=false;roleIntroStarted=float.NegativeInfinity;
            Place(host,new Vector3(-1.4f,.08f,1.5f));host.yaw=yaw=20;host.pitch=pitch=22;cameraCut=true;
            Broadcast();yield return null;if(!Application.isBatchMode)yield return new WaitForEndOfFrame();
            string directory=Path.Combine(Path.GetTempPath(),"lastcall-cash-drawer");Directory.CreateDirectory(directory);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"CashDrawer.png"));yield return null;
            Debug.Log("SALOON_DRAWER PASS "+checks+" checks; preview="+directory);
            VerificationFinished=VerificationPassed=true;
        }
    }
}
#endif
