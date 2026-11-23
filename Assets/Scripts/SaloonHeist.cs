using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        // Three shared theft steps; at most one completes per round.
        int safeProgress,drawerRevision;
        bool verifying;
        static readonly int[] PracticeRoomCycle={0,1,1,2,3};
        void ResetHeist()
        {
            ResetToiletDuty();ResetBreakables();
            safeProgress=drawerRevision=lastSafeWorkRound=0;medicalRoom=medicalRound=0;pendingOutcome="";
            botSafeRoutes.Clear();
            foreach(var p in players.Values)
            {p.storyProgress=StoryProgress.None;p.personalVictory=false;p.storySeenDead.Clear();p.bag=false;p.bagRound=0;p.escapeRound=0;p.escapePlanned=false;p.botThink=0;p.botAimAt=-1;p.giftUntil=0;p.inspectedProgress=p.inspectedRevision=-1;p.safeReport="";p.evidence.Clear();p.safeWatch.Clear();}
        }
        void BeginRobbery(Participant p)=>BeginDrawerCheck(p,true);
        void InspectSafe(Participant p)=>BeginDrawerCheck(p,false);
        void BeginDrawerCheck(Participant p,bool steal)
        {
            if(phase!=Phase.Turns||p.room!=0||!p.Alive||!SaloonRules.CanInspectSafe(p.role))
            {p.notice="Check the cash drawer at the Bar during room actions.";return;}
            if(steal&&!SaloonRules.CanRob(p.role)){p.notice="Only Outlaws can take the money.";return;}
            if(steal&&p.bag){p.notice="You already carry the money. Plan your escape next discussion.";return;}
            if(steal&&lastSafeWorkRound==rounds.Round){p.notice="The drawer was already worked this round. Try again next round.";return;}
            if(!AtSafe(p)){p.notice="Walk to the cash drawer beside the beer tray. C checks it; Outlaws can use H to work the drawer.";return;}
            if(!SpendAction(p,SaloonRules.DrawerInteractionSeconds))return;
            p.busyAction=steal?"rob":"inspect";p.drawerStarted=Time.time;p.busyUntil=Time.time+SaloonRules.DrawerInteractionSeconds;
            p.drawn=false;p.fireAfterDraw=false;p.pendingShot=null;CancelShotCharge(p);p.move=Vector2.zero;
            var direction=SaloonArt.SafePosition-p.body.transform.position;
            p.bodyYaw=p.yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;p.pitch=0;
            p.notice="Checking the cash drawer. Stay nearby for two seconds.";
        }
        void CompleteInspection(Participant p)
        {
            if(!p.Alive||p.room!=0||phase!=Phase.Turns||!AtSafe(p))return;
            string state=safeProgress>=SaloonRules.DrawerWorkSteps?"money missing":safeProgress>0?"money present · drawer tampered with":"money present";
            string change=p.inspectedRevision<0?"First inspection.":p.inspectedRevision!=drawerRevision?"The drawer changed since your last inspection.":"No change since your last inspection.";
            p.inspectedProgress=safeProgress;p.inspectedRevision=drawerRevision;
            p.safeReport="Round "+rounds.Round+" drawer check · "+state+". "+change;
            NoteEvidence(p,p.safeReport);p.notice="Drawer checked. Hold Tab to read your private report.";
        }
        void CompleteRobbery(Participant p)
        {
            if(!SaloonRules.CanRob(p.role)||!p.Alive||p.room!=0||phase!=Phase.Turns||!AtSafe(p))return;
            // Competing and empty-drawer attempts look identical and spend an action.
            if(players.Values.Any(x=>x.bag&&x.Alive)){p.notice="The drawer is empty.";return;}
            if(lastSafeWorkRound==rounds.Round){p.notice="The drawer was already worked this round. Try again next round.";return;}
            safeProgress++;drawerRevision++;lastSafeWorkRound=rounds.Round;
            foreach(var outlaw in players.Values.Where(x=>SaloonRules.CanRob(x.role)))outlaw.storyProgress|=StoryProgress.SafeWork;
            if(safeProgress<SaloonRules.DrawerWorkSteps){p.notice="Drawer work "+safeProgress+" / "+SaloonRules.DrawerWorkSteps+". Continue next round.";return;}
            p.bag=true;p.bagRound=rounds.Round;
            p.notice="Money taken. Choose Attempt escape during the next discussion.";
        }
        void DeclareEscape(Participant p)
        {
            if(p.toiletDuty){p.notice="The accepted solo mission replaces this round's escape plan.";return;}
            if(!p.Alive||!SaloonRules.CanRob(p.role)||!p.bag)
            {p.notice="Carry the money as an Outlaw to plan an escape.";return;}
            if(phase!=Phase.Discussion&&phase!=Phase.Choosing)
            {p.notice="Choose Attempt escape during discussion, before room actions start.";return;}
            if(p.bagRound>=rounds.Round){p.notice="Plan the escape in the discussion after taking the money.";return;}
            if(p.escapePlanned)return;
            SetRoomPlan(p,0,null);rounds.SetReady(p.id,false);p.planSelected=true;p.escapePlanned=true;
            rounds.HoldDiscussionUntil(Time.time+10);
            AddEvent(p.name+" plans to escape. Guard the Bar to stop the attempt.");
            p.notice="Escape announced. Everyone can change rooms before the attempt. Lock your Bar plan.";
        }
        void BeginPlannedEscapes()
        {
            if(phase!=Phase.Turns)return;
            foreach(var p in players.Values)
            {
                bool planned=p.escapePlanned;p.escapePlanned=false;p.escapeRound=0;
                if(!planned||!p.Alive||!SaloonRules.CanRob(p.role)||!p.bag||p.bagRound>=rounds.Round||p.room!=0||!rounds.IsReady(p.id))continue;
                p.escapeRound=rounds.Round;p.notice="Escape underway. You still have one action. Stay at Bar and avoid every headshot until actions end.";
                AddEvent(p.name+" is attempting to escape! One headshot stops the attempt.");
            }
        }
        void CancelEscapeOnHit(Participant p)
        {
            if(p.escapeRound<=0||p.escapeRound!=rounds.Round)return;
            p.escapeRound=0;
            p.notice="Escape interrupted. Keep the money and try again next discussion.";
            AddEvent(p.name+"'s escape was stopped.");
        }
        void ReturnDrawerMoney(Participant p)
        {
            if(!p.bag)return;
            safeProgress=0;drawerRevision++;lastSafeWorkRound=rounds.Round;
        }
        void ResolveEscapes()
        {
            if((phase!=Phase.Turns&&phase!=Phase.Aftermath)||!string.IsNullOrEmpty(pendingOutcome))return;
            var escaped=players.Values.FirstOrDefault(p=>SaloonRules.CanRob(p.role)&&p.Alive&&p.bag&&p.room==0&&p.escapeRound==rounds.Round&&p.escapeRound>0);
            if(escaped==null)return;
            // Only Bar players can stop the escape. Let all of them, including
            // the carrier, finish their action before skipping to the results.
            if(phase==Phase.Turns&&players.Values.Any(p=>p.Alive&&p.room==0&&(!rounds.HasSpent(p.id)||p.busyUntil>Time.time)))return;
            Finish("OUTLAWS WIN — MONEY ESCAPED");
        }
        void ResolveRoundEnding(){ResolveEscapes();}
        bool Enemies(Participant a,Participant b)
        {
            if(a.role==Role.Renegade)return a!=b&&(b.role!=Role.Sheriff||!players.Values.Any(p=>p!=a&&p!=b&&p.Alive))&&
                (b.role==Role.Outlaw||!players.Values.Any(p=>p.Alive&&p.role==Role.Outlaw));
            if(a.role==Role.Outlaw)return b.role!=Role.Outlaw;
            return b.role==Role.Outlaw||b.role==Role.Renegade;
        }
        void TickPracticeBots()
        {
            if(!Sandbox)return;
            if(phase==Phase.Choosing||phase==Phase.Discussion){PlanPracticeBots();return;}
            var bots=players.Values.Where(p=>p.bot&&p.Alive).OrderBy(p=>p.id).ToArray();
            foreach(var p in bots)
            {
                if(p.room==SaloonBathroom.Room)continue;
                if(phase==Phase.Turns&&p.room==3){if(Time.time-p.smokeAt>6)TrySmoke(p);continue;}
                if(phase==Phase.Turns&&duels.TryGetValue(p.room,out var duel)&&!duel.flipping&&Time.time>=duel.choiceUntil-duel.coinChoiceSeconds+(p.room==1?3:1))
                    ClaimCoin(p,UnityEngine.Random.Range(0,2));
                if(phase==Phase.Turns&&!DuelAllows(p))continue;
                if(phase!=Phase.Turns||!rounds.CanAct(p.id,Time.time)||p.busyUntil>Time.time||Time.time<p.botThink)continue;
                if(p.botThink==0){p.botThink=Time.time+1.2f+(p.id%5)*.4f;continue;}
                p.lastAction=-100;
                if(p.role!=Role.Swindler&&p.hp<tuning.health&&MedicalChoice(p)){UseMedicalSupplies(p);continue;}
                // This practice AI cannot bluff. A Swindler presents a drawn gun
                // to invite suspicion, but does not shoot or heal away its objective.
                if(p.role==Role.Swindler)
                {
                    if(!p.drawn)OnCommand(p.id,new Command{kind="draw"});
                    else OnCommand(p.id,new Command{kind="pass"});
                    continue;
                }
                var targets=players.Values.Where(x=>x.Alive&&x.room==p.room&&Enemies(p,x)).OrderByDescending(x=>x.bag||x.escapeRound>0).ThenBy(x=>x.hp).ToArray();
                var target=targets.FirstOrDefault();
                if(BotWantsBeer(p)){if(NearBeerTray(p))OnCommand(p.id,new Command{kind="interact"});continue;}
                bool law=p.role==Role.Sheriff||p.role==Role.Deputy;

                bool shoot=target!=null&&(law||p.role==Role.Renegade||target.bag||target.escapeRound>0||target.hp<=SaloonHealth.RevolverDamage||p.room!=0||p.bag);
                if(shoot)
                {
                    // Commit to a head position before firing, giving a moving player time to dodge.
                    if(p.botAimAt<0){p.botAim=Eye(target);p.botAimAt=Time.time+.3f;}
                    var direction=p.botAim-Eye(p);p.yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
                    p.pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
                    if(!p.drawn){OnCommand(p.id,new Command{kind="draw"});continue;}
                    if(Time.time<p.botAimAt)continue;
                    p.botAimAt=-1;
                    if(!p.UnlimitedAmmo&&p.ammo<=0)OnCommand(p.id,new Command{kind="reload"});else OnCommand(p.id,new Command{kind="fire"});
                }
                else if(BotWantsSafe(p))
                {if(AtSafe(p))BeginRobbery(p);}
                else OnCommand(p.id,new Command{kind="pass"});
            }
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        System.Collections.IEnumerator VerifyHeist()
        {yield return VerifyCashDrawer();VerificationFinished=VerificationPassed=false;}

#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        System.Collections.IEnumerator VerifyBeerGift()
        {
            // Keep the existing automation entry point, exercising the replacement flow.
            yield return VerifyBarAndBeer();
            Debug.Log("SALOON_BEER PASS");
        }

#endif
    }
}
