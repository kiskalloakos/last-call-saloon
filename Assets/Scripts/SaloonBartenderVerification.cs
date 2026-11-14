#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyBartender()
        {
            verifying=true;AutomatedInput=true;helloSent=true;VerificationPassed=VerificationFinished=false;
            int checks=0;
            void Check(bool ok,string message)
            {checks++;if(!ok){VerificationFinished=true;Debug.LogError("SALOON_BARTENDER FAIL "+message);throw new InvalidOperationException(message);}}
            var sheriff=players[Connection.LocalId];var targets=players.Values.Where(p=>p!=sheriff).ToArray();
            void Place(Vector3 position)
            {sheriff.controller.enabled=false;sheriff.seat=-1;sheriff.body.transform.position=position;sheriff.move=Vector2.zero;UpdatePlayerColliders(sheriff);Physics.SyncTransforms();}
            void Window(bool reset=true,int round=SaloonRules.BartenderArrivalRound)
            {
                if(reset)ResetHeist();pendingOutcome=outcome="";roomTurns.Clear();duels.Clear();
                MenuOpen=ShowRole=GraphicsMenuOpen=false;sheriff.name="Sheriff";
                int i=0;
                foreach(var p in players.Values)
                {p.role=p==sheriff?Role.Sheriff:new[]{Role.Outlaw,Role.Renegade,Role.Deputy,Role.Outlaw}[i++];p.hp=100;p.deathAt=-1;p.chosenRoom=p.room=0;p.companion=null;p.busyAction="";p.busyUntil=0;p.lastAction=-100;p.botThink=Time.time+100;p.drawn=false;p.beer=0;}
                rounds.Start(players.Keys,Time.time-(round-1)*606,300,300,0);
                while(rounds.Round<round)
                    for(int step=0;step<4;step++)rounds.Tick(rounds.Deadline,id=>true);
                foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(Time.time,id=>true);phase=Phase.Turns;PositionEveryone(false);Place(BartenderApproach);
                TickBartenderArrival();
                // Ordinary fixtures take place after the arrival toast has expired.
                if(bartenderArrived)bartenderArrivedAt=Time.time-10;
                Broadcast();
            }
            void Ask(string id){sheriff.lastAction=-100;OnCommand(sheriff.id,new Command{kind="bartender",name=id,x=BartenderApproach.x,z=BartenderApproach.z});}
            string directory=Path.Combine(Path.GetTempPath(),"LastCall-Bartender");Directory.CreateDirectory(directory);
            var npc=GetComponent<SaloonBartenderNPC>();
            for(int round=1;round<SaloonRules.BartenderArrivalRound;round++)
            {
                Window(round:round);npc.Refresh();
                Check(!bartenderArrived&&!npc.Visible&&GameObject.Find("Bartender")==null,"bartender character is absent throughout round "+round);
                ToggleBartender();Check(!CanQuestionBartender&&!BartenderMenuOpen,"early round "+round+" cannot open the question panel");
                foreach(var person in players.Values)
                {
                    person.controller.enabled=false;person.seat=-1;person.body.transform.position=BartenderApproach;Physics.SyncTransforms();
                    person.lastAction=-100;OnCommand(person.id,new Command{kind="bartender",name=players.Values.First(p=>p!=person).id.ToString()});
                    var early=PrivateSnapshot(person.id);
                    Check(!person.bartenderUsed&&!rounds.HasSpent(person.id)&&person.bartenderAnswer=="",person.role+" cannot ask or spend a question in round "+round);
                    Check(!early.bartenderArrived&&early.bartenderArrivalRemaining==0&&early.bartenderMood==-1&&early.bartenderBarShots==-1,"early snapshots expose no bartender, mood or arrival notification");
                }
            }
            sheriff.drawn=true;sheriff.ammo=6;sheriff.nextShot=0;sheriff.pitch=-75;sheriff.lastAction=-100;
            OnCommand(sheriff.id,new Command{kind="fire"});
            Check(rounds.HasSpent(sheriff.id)&&bartenderBarShots==0&&BartenderMood==100,"a real Bar shot before arrival does not affect the absent bartender");
            sheriff.yaw=yaw=0;sheriff.pitch=pitch=-8;cameraCut=true;Broadcast();UpdateCamera();
            yield return new WaitForSeconds(.25f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"BeforeArrival.png"));yield return null;
            // Cross the real round-clock boundary from round five into round six.
            rounds.BeginAftermath(Time.time-10);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;TickBartenderArrival();
            Check(rounds.Round==5&&!bartenderArrived,"round-five regroup still has no bartender");
            rounds.Tick(rounds.Deadline,id=>true);phase=rounds.Phase;TickBartenderArrival();Broadcast();npc.Refresh();
            Check(rounds.Round==6&&phase==Phase.Discussion&&bartenderArrived&&npc.Visible,"bartender arrives when discussion starts after five complete rounds");
            Check(players.Values.All(p=>{var v=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(p.id),out _));return v.bartenderArrived&&v.bartenderArrivalRemaining>0&&v.bartenderMood==100;}),"every recipient receives the serialized arrival notification and calm mood");
            int arrivals=events.Count(e=>e=="Bartender arrived");float arrivalAt=bartenderArrivedAt;
            TickBartenderArrival();TickBartenderArrival();
            Check(arrivals==1&&events.Count(e=>e=="Bartender arrived")==1&&bartenderArrivedAt==arrivalAt,"host emits one arrival event and never restarts its toast timer");
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"Arrival.png"));yield return null;
            sheriff.disconnectedUntil=Time.realtimeSinceStartup+10;OnJoined(sheriff.id);
            Check(bartenderArrived&&bartenderArrivedAt==arrivalAt,"reconnect preserves arrival without replaying its timer");
            ResetHeist();phase=Phase.Lobby;TickBartenderArrival();Broadcast();npc.Refresh();
            Check(!bartenderArrived&&!npc.Visible&&PrivateSnapshot(sheriff.id).bartenderArrivalRemaining==0,"new lobby hides the bartender and clears arrival even with an old round counter");
            Window();yield return null;
            Check(FindAnyObjectByType<SaloonBartenderNPC>()!=null&&GameObject.Find("Bartender")!=null,"physical bartender is present behind the counter");
            Check(players.Count==5&&!GameObject.Find("Bartender").GetComponentsInChildren<Collider>().Any(c=>c.enabled),"bartender is not a participant or combat collider");
            Check(GameObject.Find("Bartender").GetComponentInChildren<SaloonCharacterMotion>()==null&&GameObject.Find("CDmir Bartender")!=null,"bartender uses the licensed CDmir character rather than a player gunman");
            Check(NearBartender,"counter approach is reachable");
            var target=targets[0];
            Place(new Vector3(0,.08f,-.5f));Ask(target.id.ToString());
            Check(!sheriff.bartenderUsed&&!rounds.HasSpent(sheriff.id),"distant request with spoofed coordinates preserves question and action");
            Place(BartenderApproach+Vector3.up*2);Ask(target.id.ToString());
            Check(!sheriff.bartenderUsed,"wrong floor cannot question bartender");
            Place(BartenderApproach);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(BartenderApproach.x,1.65f,2.95f);wall.transform.localScale=new Vector3(2,2,.08f);Physics.SyncTransforms();Ask(target.id.ToString());
            Check(!sheriff.bartenderUsed&&!rounds.HasSpent(sheriff.id),"obstruction rejects question without spending");Destroy(wall);yield return null;Physics.SyncTransforms();
            foreach(var role in new[]{Role.Unknown,Role.Renegade})
            {sheriff.role=role;Ask(target.id.ToString());Check(!sheriff.bartenderUsed&&!rounds.HasSpent(sheriff.id),role+" cannot ask");}
            sheriff.role=Role.Sheriff;
            foreach(var testPhase in new[]{Phase.Lobby,Phase.Discussion,Phase.Choosing,Phase.Regroup,Phase.Aftermath,Phase.Finished})
            {phase=testPhase;Ask(target.id.ToString());Check(!sheriff.bartenderUsed&&!rounds.HasSpent(sheriff.id),testPhase+" cannot spend a question");}
            phase=Phase.Turns;
            sheriff.room=1;Ask(target.id.ToString());Check(!sheriff.bartenderUsed,"wrong room rejects question");sheriff.room=0;
            Ask(sheriff.id.ToString());Ask("not-an-id");Ask(ulong.MaxValue.ToString());Ask(null);
            Check(!sheriff.bartenderUsed&&!rounds.HasSpent(sheriff.id),"self and malformed or absent targets preserve question and action");
            target.hp=0;Ask(target.id.ToString());Check(!sheriff.bartenderUsed,"dead target is rejected");target.hp=100;
            sheriff.hp=0;Ask(target.id.ToString());Check(!sheriff.bartenderUsed,"dead Sheriff cannot ask");sheriff.hp=100;
            sheriff.busyUntil=Time.time+2;sheriff.busyAction="draw";Ask(target.id.ToString());Check(!sheriff.bartenderUsed,"busy Sheriff cannot ask");sheriff.busyUntil=0;sheriff.busyAction="";
            SpendAction(sheriff);Ask(target.id.ToString());Check(!sheriff.bartenderUsed,"spent round action does not consume question");
            Window();
            var eventsBefore=events.ToArray();var knownBefore=PrivateSnapshot(sheriff.id).knownRoles.Select(p=>p.id+":"+p.role).ToArray();
            Ask(target.id.ToString());
            Check(sheriff.bartenderUsed&&sheriff.bartenderAnswer=="Yes"&&rounds.HasSpent(sheriff.id),"Outlaw yields exactly Yes and consumes question plus one action");
            string question=sheriff.bartenderQuestion;
            Ask(targets[1].id.ToString());
            Check(sheriff.bartenderQuestion==question&&sheriff.bartenderAnswer=="Yes","duplicate or changed target cannot obtain another answer");
            Check(events.SequenceEqual(eventsBefore),"question and answer never enter public events");
            var own=PrivateSnapshot(sheriff.id);var packed=SaloonSnapshotWire.Encode(own,out _);var decoded=SaloonSnapshotWire.Decode(packed);
            Check(decoded.bartenderUsed&&decoded.bartenderQuestion==question&&decoded.bartenderAnswer=="Yes","private answer survives compressed snapshot serialization");
            Check(own.knownRoles.Select(p=>p.id+":"+p.role).SequenceEqual(knownBefore)&&own.players.First(p=>p.id==target.id.ToString()).revealedRole=="","question does not add exact role knowledge");
            Check(targets.All(p=>{var v=PrivateSnapshot(p.id);return !v.bartenderUsed&&v.bartenderQuestion==""&&v.bartenderAnswer==""&&!v.notice.Contains("Bartender:");}),"every other recipient receives no question or answer");
            sheriff.disconnectedUntil=Time.realtimeSinceStartup+10;OnJoined(sheriff.id);
            Check(sheriff.bartenderUsed&&sheriff.bartenderAnswer=="Yes","reconnection retains spent question and answer");
            Window(false);Ask(targets[1].id.ToString());
            Check(sheriff.bartenderQuestion==question&&sheriff.bartenderAnswer=="Yes"&&!rounds.HasSpent(sheriff.id),"new action window cannot refresh the match question");
            foreach(var role in new[]{Role.Deputy,Role.Renegade})
            {Window();target.role=role;Ask(target.id.ToString());Check(sheriff.bartenderAnswer=="No"&&sheriff.bartenderUsed,"non-Outlaw "+role+" yields only No");}
            // All four eligible participants ask in one match, with separate budgets.
            Window();
            var eligible=players.Values.Where(p=>SaloonRules.CanAskBartender(p.role)).ToArray();
            Check(eligible.Length==4,"Sheriff, Deputy and both Outlaws have individual question rights");
            var renegade=players.Values.Single(p=>p.role==Role.Renegade);
            foreach(var person in eligible)
            {
                person.controller.enabled=false;person.seat=-1;person.body.transform.position=BartenderApproach;Physics.SyncTransforms();
                Check(!person.bartenderUsed,"another player asking does not spend "+person.role+" question");
                person.lastAction=-100;OnCommand(person.id,new Command{kind="bartender",name=renegade.id.ToString()});
                var ownView=PrivateSnapshot(person.id);
                Check(person.bartenderUsed&&person.bartenderAnswer=="No"&&rounds.HasSpent(person.id),person.role+" can spend their own question and action");
                var wireView=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(ownView,out _));
                Check(wireView.bartenderUsed&&wireView.bartenderQuestion==person.bartenderQuestion&&wireView.bartenderAnswer=="No",person.role+" receives their own answer over the wire");
            }
            Check(!renegade.bartenderUsed&&!rounds.HasSpent(renegade.id),"everyone else asking leaves Renegade without a question or spent action");
            var saved=eligible.ToDictionary(p=>p.id,p=>p.bartenderQuestion);
            Window(false);
            foreach(var person in eligible)
            {
                person.controller.enabled=false;person.seat=-1;person.body.transform.position=BartenderApproach;Physics.SyncTransforms();
                person.lastAction=-100;OnCommand(person.id,new Command{kind="bartender",name=target.id.ToString()});
                Check(person.bartenderQuestion==saved[person.id]&&!rounds.HasSpent(person.id),person.role+" cannot ask again in a later action window");
                person.disconnectedUntil=Time.realtimeSinceStartup+10;OnJoined(person.id);
                Check(person.bartenderUsed&&person.bartenderQuestion==saved[person.id],person.role+" retains their one-use state on reconnect");
            }
            foreach(var role in new[]{Role.Deputy,Role.Outlaw,Role.Renegade})
            {
                Window();sheriff.role=role;Broadcast();ToggleBartender();
                bool allowed=SaloonRules.CanAskBartender(role);
                Check(BartenderMenuVisible==allowed&&CanQuestionBartender==allowed,role+" frontend access matches host eligibility");
                CloseBartender();
            }
            Window();Broadcast();ToggleBartender();Check(BartenderMenuVisible&&NeedsPointer,"I interaction opens target panel and frees pointer");
            CloseBartender();Check(!BartenderMenuVisible&&!NeedsPointer,"closing target panel restores normal input without spending");
            Check(!sheriff.bartenderUsed&&!rounds.HasSpent(sheriff.id),"opening and cancelling menu are free");
            // Capture the actual first-person target picker and answer for visual review.
            sheriff.yaw=yaw=0;sheriff.pitch=pitch=-8;cameraCut=true;Broadcast();UpdateCamera();ToggleBartender();
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"Question.png"));yield return null;
            Ask(target.id.ToString());Broadcast();
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"Answer.png"));yield return null;
            CloseBartender();ShowRole=true;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"PrivateIntel.png"));yield return null;
            ShowRole=false;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"Bartender.png"));yield return null;
            foreach(var role in new[]{Role.Deputy,Role.Outlaw})
            {
                Window();sheriff.role=role;Ask(target.id.ToString());Broadcast();ShowRole=true;
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,role+"-PrivateIntel.png"));yield return null;
            }
            // The displayed probability, host answer and shot accounting share one rule.
            for(int shots=0;shots<=12;shots++)
            {
                int mood=SaloonBartenderMood.FromShots(shots);int useful=0;
                for(int roll=0;roll<100;roll++)
                {
                    string yes=SaloonBartenderMood.Answer(Role.Outlaw,mood,(roll+.5f)/100f);
                    string no=SaloonBartenderMood.Answer(Role.Deputy,mood,(roll+.5f)/100f);
                    Check(yes=="Maybe"?no=="Maybe":yes=="Yes"&&no=="No","mood never lies or leaks a role through Maybe");
                    if(yes!="Maybe")useful++;
                }
                Check(useful==mood,"useful answer probability equals mood after "+shots+" shots");
            }
            Check(SaloonBartenderMood.FromShots(int.MaxValue)==0&&SaloonBartenderMood.FromShots(-1)==100,"mood clamps safely at both limits");
            Window();
            void Shoot(Participant person,int room)
            {
                person.room=room;person.drawn=true;person.ammo=6;person.nextShot=0;person.lastAction=-100;person.pitch=-75;person.yaw=0;
                OnCommand(person.id,new Command{kind="fire"});
            }
            sheriff.drawn=false;OnCommand(sheriff.id,new Command{kind="fire"});
            Check(BartenderMood==100&&bartenderBarShots==0,"holstered trigger does not affect mood");
            sheriff.lastAction=-100;sheriff.drawn=true;sheriff.ammo=0;OnCommand(sheriff.id,new Command{kind="fire"});
            Check(BartenderMood==100&&bartenderBarShots==0,"empty trigger does not affect mood");
            int beforeShots=sheriff.shots;Shoot(sheriff,0);
            Check(sheriff.shots==beforeShots+1&&bartenderBarShots==1&&BartenderMood==90,"a real missed Bar shot lowers mood by ten");
            Shoot(sheriff,0);Check(bartenderBarShots==1,"a rejected second shot cannot lower mood");
            Window(false);Shoot(sheriff,1);
            Check(bartenderBarShots==1&&BartenderMood==90,"side-room gunfire leaves mood unchanged");
            Window(false);Shoot(target,0);
            Check(target.bot&&bartenderBarShots==2&&BartenderMood==80,"bot Bar gunfire also lowers the shared mood");
            sheriff.disconnectedUntil=Time.realtimeSinceStartup+10;OnJoined(sheriff.id);
            Check(BartenderMood==80,"reconnect cannot restore bartender mood");
            foreach(var person in players.Values.Where(p=>p.room!=3))
            {
                var snapshot=PrivateSnapshot(person.id);var wire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(snapshot,out _));
                Check(wire.bartenderMood==80&&wire.bartenderBarShots==2,"indoor recipients share serialized mood and shot count");
            }
            sheriff.room=3;
            Check(PrivateSnapshot(sheriff.id).bartenderMood==-1&&PrivateSnapshot(sheriff.id).bartenderBarShots==-1,"smoking separation hides live indoor mood changes");
            sheriff.room=0;Window(false);
            Check(BartenderMood==80&&bartenderBarShots==2,"later action windows do not regenerate mood");
            for(int i=2;i<10;i++){Window(false);Shoot(sheriff,0);}
            Check(bartenderBarShots==10&&BartenderMood==0,"ten accepted Bar shots leave the bartender fed up");
            Window(false);Ask(target.id.ToString());
            string maybeQuestion=sheriff.bartenderQuestion;
            Check(sheriff.bartenderUsed&&sheriff.bartenderAnswer=="Maybe"&&rounds.HasSpent(sheriff.id),"fed-up bartender gives Maybe and still spends the player's sole question and action");
            Ask(targets[1].id.ToString());
            Check(sheriff.bartenderQuestion==maybeQuestion&&sheriff.bartenderAnswer=="Maybe","useless answer cannot be rerolled or changed");
            var maybeView=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(sheriff.id),out _));
            Check(maybeView.bartenderAnswer=="Maybe"&&targets.All(p=>PrivateSnapshot(p.id).bartenderAnswer==""),"Maybe survives serialization and remains recipient-private");
            Broadcast();sheriff.yaw=yaw=0;sheriff.pitch=pitch=-8;cameraCut=true;UpdateCamera();ToggleBartender();
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"FedUp-Maybe.png"));yield return null;
            CloseBartender();ShowRole=false;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"FedUp-Bartender.png"));yield return null;
            ShowRole=false;ResetHeist();
            Check(players.Values.All(p=>!p.bartenderUsed&&p.bartenderQuestion==""&&p.bartenderAnswer==""),"new match resets all bartender knowledge");
            Check(BartenderMood==100&&bartenderBarShots==0,"new match restores a calm bartender and clears Bar shots");
            Broadcast();npc.Refresh();Check(!bartenderArrived&&!npc.Visible&&!CanQuestionBartender,"new match hides the bartender until the next scheduled arrival");
            Debug.Log("SALOON_BARTENDER PASS "+checks+" checks; screenshots="+directory);
            VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
