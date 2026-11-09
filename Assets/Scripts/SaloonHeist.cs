using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        int safeProgress;
        bool verifying;
        static readonly int[] PracticeRoomCycle={0,1,1,2};
        void ResetHeist()
        {
            safeProgress=0;lastSafeWorkRound=0;pendingOutcome="";
            foreach(var p in players.Values){p.bag=false;p.escapeRound=0;p.botThink=0;p.botAimAt=-1;p.giftUntil=0;p.inspectedProgress=-1;p.safeReport="";p.evidence.Clear();p.safeWatch.Clear();}
        }
        void BeginRobbery(Participant p)
        {
            if(!SaloonRules.CanRob(p.role)){p.notice="Only Outlaws can rob or collect the money.";return;}
            if(phase!=Phase.Turns||p.room!=0){p.notice="The safe is at the bar. Work it during room actions.";return;}
            if(p.bag){p.notice="You already carry the money.";return;}
            if(!SpendAction(p,tuning.robberySeconds))return;
            p.busyAction="rob";p.busyUntil=Time.time+tuning.robberySeconds;p.notice="Working quietly — a shot will interrupt you.";
            // No global notification. Only attentive, living bar witnesses can collect a private clue.
        }
        void InspectSafe(Participant p)
        {
            if(phase!=Phase.Turns||p.room!=0||p.role!=Role.Sheriff){p.notice="Only the Sheriff can inspect the safe, at the bar.";return;}
            if(!SpendAction(p))return;
            int previous=p.inspectedProgress;
            p.inspectedProgress=safeProgress;
            string state=players.Values.Any(x=>x.Alive&&x.bag)?"money missing":safeProgress==3?"open · money inside":safeProgress==0?"sealed · money inside":"tampered with · money inside";
            string marks=safeProgress==0?"No tool marks.":previous<0?"Tool marks from round "+lastSafeWorkRound+".":safeProgress>previous?"New tool marks since your last check (round "+lastSafeWorkRound+").":"No new tool marks since your last check.";
            p.safeReport="Round "+rounds.Round+" check · "+state+". "+marks;
            NoteEvidence(p,p.safeReport);p.notice="Inspection complete. Hold Tab to read your clues.";
        }
        void CompleteRobbery(Participant p)
        {
            if(!SaloonRules.CanRob(p.role)||!p.Alive||p.room!=0||phase!=Phase.Turns)return;
            if(players.Values.Any(x=>x.bag&&x.Alive)){p.notice="Nothing recovered from the safe.";return;}
            safeProgress=Mathf.Min(3,safeProgress+1);lastSafeWorkRound=rounds.Round;
            if(safeProgress==3){p.bag=true;p.notice="You collected the money. X to declare escape on a later action.";}
            else p.notice="Safe work completed: "+safeProgress+" / 3.";
        }
        void DeclareEscape(Participant p)
        {
            if(!SaloonRules.CanRob(p.role)){p.notice="Only Outlaws can escape with the money.";return;}
            if(phase!=Phase.Turns||p.room!=0||!p.bag){p.notice="Carry the money at the bar to declare an escape.";return;}
            if(p.escapeRound>0){p.notice="Escape is already pending. Survive the next action phase at the bar.";return;}
            if(rounds.Round>=rounds.Limit){p.notice="Too late: escape needs another response round.";return;}
            if(!SpendAction(p))return;
            p.escapeRound=rounds.Round;p.notice="Escape pending. Keep the bag and choose the bar next round.";
        }
        void ResolveEscapes()
        {
            var escaped=players.Values.FirstOrDefault(p=>SaloonRules.CanRob(p.role)&&p.Alive&&p.bag&&p.room==0&&p.escapeRound>0&&rounds.Round>p.escapeRound);
            if(escaped!=null)QueueFinish("OUTLAWS WIN — MONEY ESCAPED");
        }
        string HeistTimeout()
        {
            var living=players.Values.Where(p=>p.Alive).Select(p=>p.role).ToArray();var winner=SaloonRules.Winner(living);
            return winner!=""?winner:living.Any(r=>r==Role.Sheriff||r==Role.Deputy)?"LAW WINS — MONEY DEFENDED":"DRAW — NO OBJECTIVE COMPLETED";
        }

        bool Enemies(Participant a,Participant b)
        {
            if(a.role==Role.Renegade)return a!=b&&(b.role==Role.Outlaw||!players.Values.Any(p=>p.Alive&&p.role==Role.Outlaw));
            if(a.role==Role.Outlaw)return b.role!=Role.Outlaw;
            return b.role==Role.Outlaw||b.role==Role.Renegade;
        }
        void TickPracticeBots()
        {
            if(!Sandbox)return;
            var bots=players.Values.Where(p=>p.bot&&p.Alive).OrderBy(p=>p.id).ToArray();
            foreach(var p in bots)
            {
                if(phase==Phase.Choosing||phase==Phase.Discussion)
                {
                    // Keep a stable, varied plan throughout discussion; rotate it each round.
                    // One bot tends the bar, two can meet, and one leaves a room for a human.
                    int choice=PracticeRoomCycle[(Array.IndexOf(bots,p)+rounds.Round-1)%PracticeRoomCycle.Length];
                    if(rounds.Round%2==0&&choice>0)choice=3-choice;
                    var invite=players.Values.FirstOrDefault(x=>!x.bot&&x.Alive&&x.companion==p.id&&x.chosenRoom>0);
                    p.chosenRoom=p.bag?0:invite?.chosenRoom??choice;p.companion=p.chosenRoom>0?invite?.id:null;
                    rounds.SetReady(p.id,true);
                    continue;
                }
                if(phase==Phase.Turns&&duels.TryGetValue(p.room,out var duel)&&!duel.flipping&&Time.time>=duel.choiceUntil-CoinChoiceSeconds+1)
                    ClaimCoin(p,UnityEngine.Random.Range(0,2));
                if(phase==Phase.Turns&&!DuelAllows(p))continue;
                if(phase!=Phase.Turns||!rounds.CanAct(p.id,Time.time)||p.busyUntil>Time.time||Time.time<p.botThink)continue;
                if(p.botThink==0){p.botThink=Time.time+1.2f+(p.id%5)*.4f;continue;}
                p.lastAction=-100;
                var targets=players.Values.Where(x=>x.Alive&&x.room==p.room&&Enemies(p,x)).OrderByDescending(x=>x.bag||x.escapeRound>0).ThenBy(x=>x.hp).ToArray();
                var target=targets.FirstOrDefault();
                if(p.hp==1&&p.beer>0){OnCommand(p.id,new Command{kind="drink"});continue;}
                bool law=p.role==Role.Sheriff||p.role==Role.Deputy;
                if(SaloonRules.CanRob(p.role)&&p.bag&&p.escapeRound==0&&p.room==0&&rounds.Round<rounds.Limit){DeclareEscape(p);continue;}
                bool shoot=target!=null&&(law||p.role==Role.Renegade||target.bag||target.escapeRound>0||target.hp==1||p.room!=0||p.bag);
                if(shoot)
                {
                    // Commit to a head position before firing, giving a moving player time to dodge.
                    if(p.botAimAt<0){p.botAim=Eye(target);p.botAimAt=Time.time+.3f;}
                    var direction=p.botAim-Eye(p);p.yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
                    p.pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
                    if(!p.drawn){OnCommand(p.id,new Command{kind="draw"});continue;}
                    if(Time.time<p.botAimAt)continue;
                    p.botAimAt=-1;
                    if(p.ammo==0)OnCommand(p.id,new Command{kind="reload"});else OnCommand(p.id,new Command{kind="fire"});
                }
                else if(SaloonRules.CanRob(p.role)&&p.room==0&&!p.bag&&!players.Values.Any(x=>x.Alive&&x.bag))BeginRobbery(p);
                else if(p.room==0&&p.hp<tuning.health&&p.beer==0&&beers>0)OnCommand(p.id,new Command{kind="interact"});
                else if(p.hp<tuning.health&&p.beer>0)OnCommand(p.id,new Command{kind="drink"});
                else OnCommand(p.id,new Command{kind="pass"});
            }
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        System.Collections.IEnumerator VerifyHeist()
        {
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;Debug.LogError("SALOON_HEIST FAIL "+text);throw new InvalidOperationException(text);}Debug.Log("SALOON_HEIST_CHECK "+text);}
            var witness=players[Connection.LocalId];var robber=players.Values.First(p=>p.bot);var outsider=players.Values.First(p=>p.bot&&p!=robber);
            var remaining=players.Values.Where(p=>p!=witness&&p!=robber&&p!=outsider).ToArray();var otherRobber=remaining[0];var renegade=remaining[1];
            witness.role=Role.Sheriff;robber.role=otherRobber.role=Role.Outlaw;outsider.role=Role.Deputy;renegade.role=Role.Renegade;
            void Window()
            {
                pendingOutcome="";
                foreach(var p in players.Values){p.chosenRoom=0;p.companion=null;p.hp=3;p.deathAt=-1;p.controller.enabled=false;p.hitbox.enabled=true;}
                rounds.Start(players.Keys,Time.time-1,1,30,6,1,1);foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);roomTurns.Clear(); // Isolate heist commands; group authorization has its own fixture.
            }
            ResetHeist();tuning.robberySeconds=.2f;Window();
            foreach(var role in new[]{Role.Sheriff,Role.Deputy,Role.Renegade})
            {
                witness.role=role;witness.lastAction=-100;witness.busyUntil=0;witness.busyAction="";
                OnCommand(witness.id,new Command{kind="rob"});
                Check(witness.busyAction==""&&safeProgress==0&&rounds.CanAct(witness.id,Time.time),role+" cannot start robbery or spend an action on H");
                CompleteRobbery(witness);Check(safeProgress==0&&!witness.bag,role+" cannot complete or collect money through a stale robbery");
                witness.bag=true;witness.lastAction=-100;OnCommand(witness.id,new Command{kind="escape"});
                Check(witness.escapeRound==0&&rounds.CanAct(witness.id,Time.time)&&!PrivateSnapshot(witness.id).escapePending,role+" cannot declare escape even with injected money state");
                witness.bag=false;
            }
            witness.role=Role.Sheriff;outsider.room=otherRobber.room=1;
            BeginRobbery(otherRobber);Check(rounds.CanAct(otherRobber.id,Time.time)&&safeProgress==0,"Outlaw side-room work rejected without spending action");otherRobber.room=0;
            int eventsBefore=events.Count;robber.drawn=true;BeginRobbery(robber);
            Check(robber.busyAction=="rob"&&robber.drawn,"silent robbery preserves the visible weapon pose");
            var observer=PrivateSnapshot(witness.id).players.First(p=>p.id==robber.id.ToString());
            Check(!observer.robbing&&!observer.moneyBag&&!observer.busy,"same-room observer receives no robbery animation or busy flag");
            Check(PrivateSnapshot(robber.id).players.First(p=>p.id==robber.id.ToString()).robbing,"worker receives their own private progress state");
            var hidden=PrivateSnapshot(outsider.id);Check(!hidden.players.First(p=>p.id==robber.id.ToString()).robbing&&hidden.safeProgress==-1,"other rooms receive no robbery state or progress");
            Check(events.Count==eventsBefore,"robbery creates no global event");
            BeginRobbery(robber);Check(!rounds.CanAct(robber.id,Time.time),"robbery consumes the single action");
            yield return new WaitForSeconds(.3f);Check(safeProgress==1,"timer completion advances one safe step");
            Window();BeginRobbery(robber);witness.drawn=true;witness.yaw=90;witness.pitch=0;witness.nextShot=0;witness.lastAction=-100;Fire(witness);
            Check(robber.hp==2&&robber.busyAction=="","real shot interrupts the working player");
            yield return new WaitForSeconds(.3f);Check(safeProgress==1,"interruption grants no safe progress");
            Window();BeginRobbery(robber);yield return new WaitForSeconds(.3f);Check(safeProgress==2,"progress persists across action windows");
            Window();BeginRobbery(robber);yield return new WaitForSeconds(.3f);Check(safeProgress==3&&robber.bag,"third completed work action collects the money");
            outsider.room=1;Check(!PrivateSnapshot(outsider.id).players.First(p=>p.id==robber.id.ToString()).moneyBag,"bag possession is not broadcast outside the room");
            Check(!PrivateSnapshot(witness.id).players.First(p=>p.id==robber.id.ToString()).moneyBag,"bag possession is also hidden from roommates");
            Window();BeginRobbery(otherRobber);Check(otherRobber.busyAction=="rob"&&!rounds.CanAct(otherRobber.id,Time.time),"Outlaw empty-safe probing costs a full action instead of leaking money state for free");
            yield return new WaitForSeconds(.3f);Check(!otherRobber.bag&&otherRobber.notice=="Nothing recovered from the safe.","empty-safe attempt reveals no carrier identity");
            Window();robber.role=Role.Outlaw;DeclareEscape(robber);ResolveEscapes();Check(phase==Phase.Turns&&robber.escapeRound==1,"escape does not win in the declaration round");
            foreach(var p in players.Values)rounds.TrySpend(p.id,Time.time);TickServer();
            yield return new WaitForSeconds(4.15f);Check(rounds.Round==2,"escape crosses aftermath and regroup into a full response round");
            foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);roomTurns.Clear();
            foreach(var role in new[]{Role.Sheriff,Role.Deputy,Role.Renegade})
            {
                robber.role=role;ResolveEscapes();Check(phase==Phase.Turns&&pendingOutcome=="",role+" cannot resolve an escape or win with injected money state");
            }
            robber.role=Role.Outlaw;
            foreach(var p in players.Values)rounds.TrySpend(p.id,Time.time);TickServer();
            Check(phase==Phase.Aftermath&&pendingOutcome=="OUTLAWS WIN — MONEY ESCAPED","escape result waits for aftermath");
            yield return new WaitForSeconds(3.15f);
            Check(phase==Phase.Finished&&outcome=="OUTLAWS WIN — MONEY ESCAPED","surviving next action phase resolves faction escape win");
            Window();robber.bag=true;robber.escapeRound=1;robber.chosenRoom=1;PositionEveryone(false);Check(robber.escapeRound==0,"leaving the bar cancels escape");
            Check(SaloonRules.Winner(new[]{Role.Deputy,Role.Outlaw})=="","Sheriff death is not an Outlaw victory");
            Check(SaloonRules.Winner(new[]{Role.Renegade})=="RENEGADE WINS — LAST STANDING"&&SaloonRules.Winner(new[]{Role.Outlaw,Role.Renegade})=="","Renegade wins only as the sole survivor");
            ResetHeist();Window();witness.role=Role.Sheriff;robber.role=Role.Outlaw;outsider.role=Role.Deputy;
            outsider.drawn=true;outsider.botThink=Time.time-1;outsider.ammo=6;
            foreach(var p in players.Values.Where(p=>p.bot&&p!=outsider))p.botThink=Time.time+100;
            int shots=outsider.shots;TickPracticeBots();yield return new WaitForSeconds(.32f);TickPracticeBots();Check(outsider.shots==shots+1&&!rounds.CanAct(outsider.id,Time.time),"practice Law bot aims and fires a legal real shot instead of passing");
            Window();robber.role=Role.Outlaw;robber.botThink=Time.time-1;
            foreach(var p in players.Values.Where(p=>p.bot&&p!=robber))p.botThink=Time.time+100;
            TickPracticeBots();Check(robber.busyAction=="rob","practice criminal works the safe toward victory");
            ResetHeist();Window();renegade.drawn=true;renegade.ammo=6;renegade.nextShot=0;renegade.botThink=Time.time-1;
            foreach(var p in players.Values.Where(p=>p.bot&&p!=renegade))p.botThink=Time.time+100;
            int lawHealth=witness.hp+outsider.hp;int outlawHealth=robber.hp+otherRobber.hp;shots=renegade.shots;
            TickPracticeBots();yield return new WaitForSeconds(.32f);TickPracticeBots();Check(renegade.shots==shots+1&&robber.hp+otherRobber.hp==outlawHealth-1&&witness.hp+outsider.hp==lawHealth&&renegade.busyAction!="rob","practice Renegade attacks Outlaws first and never robs");
            Window();robber.hp=otherRobber.hp=0;robber.hitbox.enabled=otherRobber.hitbox.enabled=false;
            renegade.drawn=true;renegade.ammo=6;renegade.nextShot=0;renegade.botThink=Time.time-1;outsider.botThink=Time.time+100;
            lawHealth=witness.hp+outsider.hp;shots=renegade.shots;
            TickPracticeBots();yield return new WaitForSeconds(.32f);TickPracticeBots();Check(renegade.shots==shots+1&&witness.hp+outsider.hp==lawHealth-1,"practice Renegade turns on the Law after all Outlaws are dead");
            tuning.robberySeconds=3;Debug.Log("SALOON_HEIST PASS");
        }
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        System.Collections.IEnumerator VerifyBeerGift()
        {
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;Debug.LogError("SALOON_BEER FAIL "+text);throw new InvalidOperationException(text);}Debug.Log("SALOON_BEER_CHECK "+text);}
            var giver=players[Connection.LocalId];var recipient=players.Values.First(p=>p.bot);
            void Window()
            {
                pendingOutcome="";
                foreach(var p in players.Values){p.chosenRoom=0;p.companion=null;p.hp=3;p.deathAt=-1;p.hitbox.enabled=true;}
                rounds.Start(players.Keys,Time.time-1,1,30,6,1,1);foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);roomTurns.Clear();
                giver.beer=1;giver.yaw=90;giver.pitch=0;giver.lastAction=-100;recipient.beer=0;recipient.hp=1;recipient.yaw=-90;
            }
            tuning.handoffSeconds=.15f;tuning.drinkSeconds=.4f;Window();Give(giver);
            Check(giver.beer==0&&recipient.busyAction=="giftDrink"&&!rounds.CanAct(giver.id,Time.time),"handoff spends giver action and beer");
            Check(rounds.CanAct(recipient.id,Time.time),"receiving a gift does not spend recipient action");
            Check(!PrivateSnapshot(recipient.id).players.First(p=>p.id==recipient.id.ToString()).drinking,"recipient waits for beer arrival");
            yield return new WaitForSeconds(.23f);
            Check(PrivateSnapshot(recipient.id).players.First(p=>p.id==recipient.id.ToString()).drinking,"recipient begins synchronized first/third person drinking after arrival");
            yield return new WaitForSeconds(.4f);Check(recipient.hp==2&&recipient.busyAction=="","received beer heals one HP after drinking completes");
            Window();Give(giver);yield return new WaitForSeconds(.2f);
            var shooter=players.Values.First(p=>p.bot&&p!=recipient);shooter.drawn=true;shooter.ammo=6;shooter.nextShot=0;
            var direction=Eye(recipient)-Eye(shooter);shooter.yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;shooter.pitch=0;
            Fire(shooter);Check(recipient.hp==0&&recipient.busyAction=="","shot interrupts gifted beer");
            yield return new WaitForSeconds(.5f);Check(recipient.hp==0,"interrupted gift never heals or revives");
            var sound=SaloonBeerAudio.Drinking;var wave=new float[sound.samples];sound.GetData(wave,0);
            Check(wave.All(v=>!float.IsNaN(v)&&Mathf.Abs(v)<=1)&&wave.Any(v=>Mathf.Abs(v)>.02f),"procedural drinking sound contains valid audible samples");
            Check(SaloonBeerAudio.Handoff.length>.1f,"handoff clink generated");
            tuning.handoffSeconds=.65f;tuning.drinkSeconds=2.5f;
#if UNITY_EDITOR
            // Exercise actual snapshot delivery and rendered avatars at normal alpha timings.
            Window();Give(giver);yield return new WaitForSeconds(.3f);
            Check(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name=="Mug visual"&&t.parent==null),"snapshot delivery creates a travelling handoff mug");
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../Documentation/BeerHandoffPreview.png"));
            yield return new WaitForSeconds(1.2f);
            var remoteAvatar=avatars[recipient.id.ToString()];
            var remoteMug=remoteAvatar.transform.Find("Mug visual");
            Check(remoteMug!=null&&remoteMug.gameObject.activeInHierarchy&&remoteMug.localPosition.y>.95f&&remoteAvatar.GetComponents<AudioSource>().Any(a=>a.clip==sound&&a.isPlaying),"remote avatar raises mug and plays positional drinking audio");
            Check((remoteMug.localRotation*Vector3.up).z<-.8f,"third-person rim tips toward the drinker, not away");
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../Documentation/BeerThirdPersonPreview.png"));
            yield return new WaitForSeconds(1.9f);
            Window();giver.beer=0;giver.hp=1;recipient.beer=1;recipient.hp=3;recipient.yaw=-90;recipient.lastAction=-100;
            Give(recipient);yield return new WaitForSeconds(1.5f);
            var localMug=ViewCamera.transform.Find("Mug visual");
            Check(localMug!=null&&localMug.gameObject.activeInHierarchy&&localMug.localPosition.y>-.18f&&avatars[giver.id.ToString()].GetComponents<AudioSource>().Any(a=>a.clip==sound&&a.isPlaying&&a.spatialBlend==0),"local recipient sees raised first-person mug and hears drinking audio");
            Check((localMug.localRotation*Vector3.up).z<-.8f,"first-person rim tips toward the camera, not the mug base");
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../Documentation/BeerFirstPersonPreview.png"));
            yield return new WaitForSeconds(1.9f);Check(giver.hp==2,"bot-to-human gift heals after first-person animation");
#endif
            Debug.Log("SALOON_BEER PASS");
        }
#endif
    }
}
