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
        public IEnumerator VerifyBluffDice()
        {
            verifying=AutomatedInput=true;VerificationFinished=VerificationPassed=false;int checks=0;
            void Check(bool ok,string text){checks++;if(!ok){VerificationFinished=true;throw new InvalidOperationException("Bluff Dice: "+text);}Debug.Log("SALOON_BLUFF_CHECK "+text);}
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);var outsider=players.Values.First(p=>p!=host&&p!=peer);var smoker=players.Values.Last(p=>p.bot);
            string folder=Path.Combine(Path.GetTempPath(),"LastCall-BluffDice");Directory.CreateDirectory(folder);
            void Discussion(int round=2)
            {
                DicePractice=DicePracticeHelp=DicePracticeReview=false;ResetHeist();outcome=pendingOutcome="";duels.Clear();roomTurns.Clear();
                foreach(var p in players.Values)
                {p.bot=p.id!=Connection.LocalId;p.hp=100;p.deathAt=-1;p.role=Role.Outlaw;p.chosenRoom=0;p.companion=null;p.busyAction="";p.busyUntil=0;p.move=Vector2.zero;p.lastAction=p.lastStance=-100;p.drawn=false;p.nextShot=0;p.ammo=6;p.shots=0;p.shotAt=p.confirmedAt=-100;p.cigaretteBuff=false;}
                host.role=Role.Sheriff;host.name="You";peer.name="Bluff partner";
                rounds.Start(players.Keys,Time.time,60,60,0);
                while(rounds.Round<round){rounds.BeginAftermath(Time.time-10);rounds.Tick(Time.time,id=>true);rounds.Tick(Time.time+4,id=>true);}
                phase=rounds.Phase;PositionEveryone(true);MenuOpen=ShowRole=RoomPlannerOpen=false;roleIntroStarted=float.NegativeInfinity;helloSent=true;
            }
            void Start()
            {
                host.chosenRoom=peer.chosenRoom=1;host.companion=peer.id;peer.companion=host.id;smoker.chosenRoom=3;
                foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
            }
            Participant Speaker(Duel d)=>players[d.bluff.Speaker==0?d.a:d.b];
            Participant Listener(Duel d)=>players[d.bluff.Listener==0?d.a:d.b];
            void SetHand(Duel d,Participant speaker,bool truth)
            {
                for(int seed=0;seed<1000;seed++){var h=new SaloonBluffDice(new System.Random(seed),speaker.id==d.a?0:1);if(h.Truth==truth){d.bluff=h;return;}}
                throw new InvalidOperationException("No matching roll");
            }
            Duel Setup(bool hostSpeaks=true,bool truth=true,bool throwDice=true)
            {Discussion();Start();var d=duels[1];SetHand(d,hostSpeaks?host:peer,truth);if(throwDice)Send(Speaker(d),d,"throw");return d;}
            void Ready(Duel d){d.readyAt=0;d.choiceUntil=Time.time-1;d.judgmentUntil=Time.time+BluffJudgeSeconds;}
            void Send(Participant p,Duel d,string action,int round=-1,int revision=-1)
            {OnCommand(p.id,new Command{kind="bluff",name=action,planRound=round<0?rounds.Round:round,bluffRevision=revision<0?d.bluff.Revision:revision});}
            Snapshot Wire(Participant p)=>SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(p.id),out _));
            IEnumerator Capture(string name){Broadcast();yield return null;if(!Application.isBatchMode)yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return null;}
            yield return new WaitForSeconds(.1f);
            for(int round=1;round<=6;round++)
            {
                Discussion(round);Broadcast();
                Check(Wire(host).coinRoomGame==(round%2==0?"bluff":"coin")&&Wire(peer).coinRoomGame==Wire(host).coinRoomGame,"announced game is shared before voting in round "+round);
                Check(RoomName(1)==(round%2==0?"Bluff Dice":"Coin Flip"),"room plans show the new game name in round "+round);
                Start();Check((duels[1].bluff!=null)==(round%2==0),"West room assignment follows its announced rotation in round "+round);
            }
            var thrown=Setup(throwDice:false);Broadcast();yield return null;
            Check(PlayingDice&&NeedsPointer&&Wire(host).duel.stage=="waiting"&&!Wire(host).duel.bluffCanJudge&&Wire(host).duel.yourDice.Length==0&&Wire(host).duel.bluffClaim==""&&!Wire(host).duel.bluffTruthKnown,"speaker waits with a free pointer and no exposed roll or claim before throwing");
            yield return Capture("manual-throw-ready");yield return new WaitForSeconds(DiceRollSeconds+.1f);
            Check(!thrown.bluffThrown&&Wire(host).duel.stage=="waiting"&&!DiceTable.Throwing&&DiceTable.VisibleDice==0,"human dice never roll automatically after the former roll duration");
            Send(peer,thrown,"throw");Send(outsider,thrown,"throw");Send(host,thrown,"throw",round:1);Send(host,thrown,"throw",revision:7);Send(peer,thrown,"believe");
            Check(!thrown.bluffThrown&&!thrown.revealed,"listener, outsider, stale throw commands and pre-throw judgments are rejected");
            ThrowBluffDice();yield return null;
            Check(thrown.bluffThrown&&Mathf.Abs(thrown.readyAt-Time.time-DiceRollSeconds)<.2f&&Mathf.Abs(thrown.choiceUntil-thrown.readyAt-BluffSpeakSeconds)<.01f,"speaker's throw control starts the roll and subsequent speaking timer");
            float readyAt=thrown.readyAt;Send(host,thrown,"throw");Send(peer,thrown,"doubt");
            Check(thrown.readyAt==readyAt&&!thrown.revealed,"repeated clicks cannot reroll or restart the throw, and judgment waits for speaking");
            Broadcast();yield return null;
            Check(Wire(host).duel.stage=="rolling"&&Wire(host).duel.remaining<=DiceRollSeconds&&DiceTable.Throwing&&DiceTable.VisibleDice==0,"throw begins with a closed cup and its own synchronized countdown");
            yield return Capture("cup-shake");yield return new WaitForSeconds(1.35f);
            Check(DiceTable.Throwing&&DiceTable.VisibleDice==3&&DiceTable.ThrowHeight>.10f,"speaker's three dice leave the cup, tumble and bounce above the baize");
            yield return Capture("dice-thrown");
            while(Time.time<thrown.readyAt+.1f)yield return null;
            Check(!DiceTable.Throwing&&DiceTable.VisibleDice==3&&Mathf.Abs(DiceTable.ThrowHeight-.088f)<.001f,"dice settle before the eight-second speaking interval");
            var rendered=DiceTable.transform.Find("Bluff Dice cups and dice");
            var cover=rendered.Find("Oxblood leather playing surface").GetComponent<Renderer>().bounds;
            Check(Mathf.Abs(cover.size.x-1.32f)<.001f&&Mathf.Abs(cover.size.z-1.63f)<.001f,"leather surface stays aligned with the furniture when seats rotate the dice board");
            var hand=thrown.bluff.Hand();
            for(int i=0;i<3;i++)
            {
                var die=rendered.Find("Thrown ivory die "+(i+1));
                Check(Vector3.Dot(die.Find("Face "+hand[i]).up,Vector3.up)>.999f,"settled physical die "+(i+1)+" matches its authoritative face");
            }
            yield return Capture("dice-settled");
            for(int face=1;face<=6;face++)
            {
                var faceView=Wire(host);faceView.duel.stage="speaking";faceView.duel.yourDice=new[]{face,face,face};
                DiceTable.Apply(faceView,faceView.players.First(p=>p.id==host.id.ToString()));
                Check(Vector3.Dot(rendered.Find("Thrown ivory die 1").Find("Face "+face).up,Vector3.up)>.999f,"physical face "+face+" points up for its matching snapshot");
            }
            thrown=Setup(false);Broadcast();yield return null;yield return new WaitForSeconds(1.4f);
            Check(DiceTable.Throwing&&DiceTable.VisibleDice==0,"listener's animated throw never creates the speaker's secret dice");
            yield return Capture("hidden-throw");
            Discussion();RoomPlannerOpen=true;yield return Capture("planning");RoomPlannerOpen=false;Start();var d=duels[1];SetHand(d,host,false);Send(host,d,"throw");
            Check(Mathf.Abs(d.choiceUntil-d.readyAt-BluffSpeakSeconds)<.01f&&Mathf.Abs(d.judgmentUntil-d.choiceUntil-BluffJudgeSeconds)<.01f,"eight seconds for speaking, then six seconds for one judgment");
            peer.bot=false;var spoken=Wire(host);var listening=Wire(peer);
            Check(spoken.duel.bluffClaim==d.bluff.Claim&&spoken.duel.bluffTruthKnown&&!spoken.duel.bluffClaimTrue&&spoken.duel.yourDice.SequenceEqual(d.bluff.Hand()),"speaker alone receives their exact claim, truth and three secret dice");
            Check(listening.duel.bluffClaim==""&&!listening.duel.bluffTruthKnown&&!listening.duel.bluffClaimTrue&&listening.duel.yourDice.Length==0&&listening.duel.opponentDice.Length==0,"human listener receives no claim, truth or secret dice before reveal");
            Check(spoken.diceTable.bluffClaim==""&&!spoken.diceTable.bluffTruthKnown&&spoken.diceTable.yourDice.Length==0&&spoken.diceTable.revealedDiceA.Length==0&&spoken.diceTable.revealedDiceB.Length==0,"shared table view has no secret statement or roll");
            Check(Wire(outsider).diceTable==null&&Wire(outsider).duel==null&&Wire(smoker).diceTable==null,"other rooms receive no private game state");
            Send(peer,d,"believe");Check(!d.revealed&&!PrivateDuel(peer).bluffCanJudge,"listener cannot skip the speaking interval");
            d.readyAt=0;yield return Capture("speaker");Check(DiceTable.Visible&&DiceTable.VisibleDice==3&&!CoinTable.Visible&&PlayingDice&&NeedsPointer,"speaker sees only their three dice and remains at the shared table");
            var mural=DiceTable.transform.Find("Bluff Dice wall stencil");
            Check(mural.gameObject.activeSelf&&mural.GetComponent<Renderer>().sharedMaterial.mainTexture==Resources.Load<Texture2D>("Art/RoomStencils/BluffDice"),"Bluff Dice retains the exterior cup-and-dice mural");
            var before=host.body.transform.position;OnCommand(host.id,new Command{kind="stance"});OnCommand(host.id,new Command{kind="draw"});OnCommand(host.id,new Command{kind="pass"});
            Check(host.seat>=0&&host.body.transform.position==before&&!host.drawn&&!rounds.HasSpent(host.id),"standing, weapons and spending remain locked until the reveal");
            Ready(d);Send(host,d,"believe");Send(outsider,d,"doubt");Send(peer,d,"raise");Send(peer,d,"call");Send(peer,d,"believe",round:1);Send(peer,d,"doubt",revision:7);
            OnCommand(peer.id,new Command{kind="dice",name="raise"});OnCommand(peer.id,new Command{kind="coin",x=0});OnCommand(peer.id,new Command{kind="blackjack",name="hit"});
            Check(!d.revealed&&d.bluff.Revision==0,"speaker, outsider, old bid commands, other games and stale inputs cannot judge");
            foreach(bool hostSpeaks in new[]{true,false})foreach(bool truth in new[]{true,false})foreach(bool believe in new[]{true,false})
            {
                d=Setup(hostSpeaks,truth);Ready(d);var listener=Listener(d);var speaker=Speaker(d);Send(listener,d,believe?"believe":"doubt");
                Check(d.revealed&&d.first==(believe==truth?listener.id:speaker.id),"judgment winner: speaker="+hostSpeaks+", truth="+truth+", believe="+believe);
                var view=Wire(listener).duel;Check(view.bluffTruthKnown&&view.bluffClaimTrue==truth&&view.bluffClaim==d.bluff.Claim&&view.opponentDice.SequenceEqual(d.bluff.Hand()),"reveal explains the statement and exposes only the speaker's actual three dice");
                Check(!DuelAllows(host)&&!DuelAllows(peer)&&!TableLocksSeat(host)&&Mathf.Abs(d.firstUntil-d.combatAt-10)<.01f&&!rounds.HasSpent(host.id)&&!rounds.HasSpent(peer.id),"normal result pause and ten-second opening preserve both combat actions");
                int revision=d.bluff.Revision;Send(listener,d,believe?"doubt":"believe");Check(d.bluff.Revision==revision,"duplicate or changed judgment never awards a second opening");
            }
            d=Setup(false,true);peer.bot=false;Ready(d);var hidden=Wire(host);Check(hidden.duel.bluffClaim==""&&!hidden.duel.bluffTruthKnown&&!hidden.duel.bluffClaimTrue,"even a true human statement stays hidden from the listener");
            DiceTable.Apply(hidden,hidden.players.First(p=>p.id==host.id.ToString()));Check(DiceTable.VisibleDice==0,"listener rendering contains no hidden roll");
            yield return Capture("human-listener");
            peer.bot=true;Broadcast();Check(Wire(host).duel.bluffBotCaption&&Wire(host).duel.bluffClaim==d.bluff.Claim&&!Wire(host).duel.bluffTruthKnown,"bot caption supplies its spoken sentence without revealing truth");
            yield return Capture("listener");Send(host,d,"doubt");yield return Capture("reveal");
            Check(Mathf.Abs(Mathf.DeltaAngle(ViewCamera.transform.eulerAngles.x,26))<.1f,"seated reveal camera keeps the cup and physical dice in view");
            yield return new WaitForSeconds(.7f);yield return Capture("cup-revealed");
            Check(DiceTable.transform.Find("Bluff Dice cups and dice").Cast<Transform>().First(t=>t.name=="Leather dice shaker"&&t.gameObject.activeSelf).localPosition.x<-.4f,"cup lifts and moves aside to expose the roll");
            Check(DiceTable.VisibleDice==3,"reveal opens just the speaker's cup, with three dice total");
            host.lastStance=-100;OnCommand(host.id,new Command{kind="stance"});Check(host.seat<0,"both players may stand during the result pause");
            var first=players[d.first];var second=players[d.first==d.a?d.b:d.a];d.combatAt=Time.time-1;
            Check(DuelAllows(first)&&!DuelAllows(second),"winner alone receives the first combat action");
            first.lastAction=-100;OnCommand(first.id,new Command{kind="pass"});Check(rounds.HasSpent(first.id)&&DuelAllows(second),"passing immediately releases the normal response");
            d=Setup();Ready(d);d.judgmentUntil=Time.time-1;Send(Listener(d),d,"believe");Check(!d.revealed,"late judgment is rejected before the timeout tick");
            TickBluffDice(d);Check(d.revealed&&d.bluffTimedOut&&d.first==Speaker(d).id&&!d.bluff.Believed.HasValue,"idle listener forfeits to speaker without an invented answer");
            d.combatAt=Time.time-2;d.firstUntil=Time.time-1;TickDuels();Check(rounds.HasSpent(d.first)&&DuelAllows(players[d.first==d.a?d.b:d.a]),"normal opening expiry releases the opponent");
            d=Setup();peer.hp=0;TickBluffDice(d);Check(d.revealed&&d.first==host.id,"eliminated participant cannot stall the minigame");
            d=Setup(throwDice:false);d.throwUntil=Time.time-1;Send(host,d,"throw");Check(!d.bluffThrown,"late multiplayer throws are rejected");TickBluffDice(d);
            Check(d.revealed&&d.first==Listener(d).id&&!Wire(host).duel.bluffTruthKnown&&Wire(host).duel.yourDice.Length==0&&Wire(host).duel.result.Contains("No throw"),"idle multiplayer speaker forfeits without an automatic roll or invented dice reveal");
            d=Setup(false,true);Ready(d);Send(host,d,"believe");d.combatAt=Time.time-1;host.role=Role.Sheriff;peer.role=Role.Renegade;
            var aim=Eye(peer)-Eye(host);host.yaw=Quaternion.LookRotation(aim).eulerAngles.y;host.pitch=0;int hp=peer.hp;
            OnCommand(host.id,new Command{kind="draw"});OnCommand(host.id,new Command{kind="fire"});host.busyUntil=Time.time-.01f;TickServer();
            Check(host.shots==1&&peer.hp==hp-SaloonHealth.RevolverDamage&&DuelAllows(peer),"real opening shot uses unchanged damage and releases response");
            d=Setup(false,true);Ready(d);Send(host,d,"believe");d.combatAt=Time.time-1;host.hp=60;medicalRound=rounds.Round;medicalRoom=1;int stock=beers;
            Check(MedicalChoice(host)&&!MedicalChoice(peer),"medical opportunity follows the new game's winner");UseMedicalSupplies(host);
            Check(host.hp==80&&beers==stock&&rounds.HasSpent(host.id)&&DuelAllows(peer),"medical heal retains normal cost and beer independence");
            d=Setup();outsider.hp=0;outsider.deathAt=Time.time;yield return new WaitForSeconds(SaloonRagdoll.ObserverDelay+.1f);var spectator=Wire(outsider);
            Check(spectator.diceTable!=null&&spectator.diceTable.bluffClaim==""&&!spectator.diceTable.bluffTruthKnown&&spectator.diceTable.yourDice.Length==0,"spectators never receive the speaker's private prompt or roll");
            DiceTable.Apply(spectator,spectator.players.First(p=>p.id==outsider.id.ToString()));Check(DiceTable.VisibleDice==0,"spectator sees the closed speaker cup");
            Discussion(3);Start();Broadcast();yield return null;Check(PlayingCoin&&CoinTable.Visible&&!DiceTable.Visible,"next odd round still plays Coin Flip");
            Discussion();host.chosenRoom=1;foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
            Check(host.room==0&&!duels.ContainsKey(1),"unmatched solo plan still falls back to Bar");
            d=Setup();host.bot=true;verifying=false;d.choiceUntil=Time.time+.2f;d.nextInputAt=d.choiceUntil+.2f;float end=Time.time+3;
            while(!d.revealed&&Time.time<end)yield return null;
            host.bot=false;verifying=true;Check(d.revealed&&d.bluff.Believed.HasValue,"bot listener makes one blind real-time judgment");
            d=Setup(false,throwDice:false);verifying=false;d.nextInputAt=Time.time-1;TickBluffDice(d);verifying=true;
            Check(d.bluffThrown&&Wire(host).duel.stage=="rolling"&&Wire(host).duel.yourDice.Length==0,"bot speaker throws its own hidden dice through the normal authorized command");
            VerifyBlackjackFlow();
            StartDicePractice();d=duels[1];Check(DicePractice&&Connection.Solo&&players.Count==2&&DicePracticeHelp&&NeedsPointer,"offline two-player practice waits on clear instructions");
            Check(Speaker(d)==host&&Listener(d).bot&&!d.bluffThrown,"first practice hand lets the human throw their own dice");
            int initial=d.bluff.Revision;HoldDicePracticeTimers();TickBluffDice(d);Send(host,d,"throw");
            Check(d.bluff.Revision==initial&&!d.revealed&&!d.bluffThrown,"instruction screen blocks throws and bot activity");
            CloseDicePracticeHelp();d.throwUntil=Time.time-20;TickBluffDice(d);
            Check(!d.revealed&&!d.bluffThrown&&PrivateDuel(host).stage=="waiting","practice waits indefinitely for the human throw");
            Send(host,d,"throw");Check(d.bluffThrown,"practice accepts the human's throw after an unlimited wait");
            NewDicePracticeHand();d=duels[1];Check(Speaker(d).bot&&Listener(d)==host,"second practice hand alternates to listening");Send(Speaker(d),d,"throw");
            d.readyAt=0;d.choiceUntil=Time.time-1;d.judgmentUntil=Time.time-20;TickBluffDice(d);
            Check(!d.revealed&&PrivateDuel(host).bluffCanJudge,"practice judgment has no timeout");
            Send(host,d,"believe");d.combatAt=d.firstUntil=Time.time-20;HoldDicePracticeTimers();TickDuels();
            Check(DicePracticeReview&&NeedsPointer&&!DuelAllows(host)&&!rounds.HasSpent(d.first),"practice holds the reveal without consuming the opening");
            yield return Capture("practice-review");ContinueDicePractice();Check(!DicePracticeReview&&d.combatAt>Time.time&&Mathf.Abs(d.firstUntil-d.combatAt-10)<.01f,"continue restores the normal reaction pause and opening");
            int priorRound=rounds.Round;host.ammo=1;peer.hp=0;NewDicePracticeHand();d=duels[1];
            Check(rounds.Round==priorRound+1&&Speaker(d)==host&&CoinRoomGame=="bluff"&&players.Values.All(p=>p.hp==100&&p.ammo==6&&!rounds.HasSpent(p.id)),"new hand alternates speaker and restores health, ammo and actions");
            Ready(d);Send(host,d,"throw",round:priorRound);Send(Listener(d),d,"believe",round:priorRound);Check(!d.revealed&&!d.bluffThrown,"old practice hand throws and judgments are rejected");
            rounds.BeginAftermath(Time.time-20);phase=rounds.Phase;TickServer();Check(phase==Phase.Aftermath,"practice waits for a new-hand choice after combat");
            NewDicePracticeHand();d=duels[1];Send(Speaker(d),d,"throw");d.readyAt=0;d.choiceUntil=Time.time-1;yield return Capture("practice-listener");
            VerificationFinished=VerificationPassed=true;Debug.Log("SALOON_BLUFF PASS "+checks+" checks; previews="+folder);
        }
    }
}
#endif
