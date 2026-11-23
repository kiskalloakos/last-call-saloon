using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public enum WalkthroughBeat
        {Walk,Crouch,Quick,Accurate,Reload,Pass,Heal,Plan,Coin,Blackjack,Smoke,Boost,Sheriff,Deputy,Outlaw,Escape,Renegade,Spectate,Exit,Complete}
        public bool TutorialWalkthrough { get; private set; }
        public WalkthroughBeat WalkthroughStep { get; private set; }
        public string WalkthroughTitle { get; private set; }
        public string WalkthroughInstruction { get; private set; }
        public string WalkthroughContext { get; private set; }
        public string WalkthroughFeedback { get; private set; }="";
        public bool WalkthroughFinished=>TutorialWalkthrough&&WalkthroughStep==WalkthroughBeat.Complete;
        public float WalkthroughSeconds { get; private set; }
        public Vector3 WalkthroughFocus=>trainingMap.FocusPoint;
        public bool WalkthroughNeedsRole=>WalkthroughStep==WalkthroughBeat.Renegade&&!walkthroughReadRole;
        SaloonTrainingMap trainingMap;
        readonly Queue<Vector3> walkthroughPath=new Queue<Vector3>();
        Vector3? walkthroughDestination;
        float walkthroughFeedbackUntil,walkthroughClock;
        bool walkthroughTable,walkthroughReadRole,walkthroughEscaping;
        int walkthroughShots;
        Participant WalkthroughHost=>players[Connection.LocalId];
        Participant WalkthroughPeer=>players.Values.Where(p=>p.bot).OrderBy(p=>p.id).ElementAt(1);

        public void BeginTutorial()
        {
            if(!StartTutorialSession())return;
            TutorialWalkthrough=true;TutorialHelpOpen=false;TutorialTaskDone=false;
            trainingMap=new SaloonTrainingMap(ViewCamera);
            var p=WalkthroughHost;var bots=players.Values.Where(b=>b.bot).OrderBy(b=>b.id).ToArray();
            p.name=CleanName(PlayerName);p.role=Role.Sheriff;
            var roles=new[]{Role.Deputy,Role.Outlaw,Role.Outlaw,Role.Renegade};
            for(int i=0;i<bots.Length;i++)
            {bots[i].role=roles[i];bots[i].name="Training "+roles[i];bots[i].chosenRoom=0;PlaceTutorialPlayer(bots[i],new Vector3(-7+i*1.6f,.08f,4.7f),180);}
            foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;
            duels.Clear();roomTurns.Clear();p.room=0;
            PlaceTutorialPlayer(p,SaloonTrainingMap.Start,0);
            PlaceTutorialPlayer(WalkthroughPeer,SaloonTrainingMap.Target,0);
            roleIntroStarted=float.NegativeInfinity;RoomPlannerOpen=false;ShowRole=false;MenuOpen=false;
            WalkthroughSeconds=0;walkthroughClock=Time.unscaledTime;walkthroughReadRole=walkthroughEscaping=false;
            SetWalkthroughBeat(WalkthroughBeat.Walk);CaptureCombatFrames();Broadcast();SyncPointer();
        }

#if UNITY_EDITOR
        public void PreviewTutorialCheckpoint(WalkthroughBeat beat)
        {
            BeginTutorial();
            if(!TutorialWalkthrough)return;
            SetWalkthroughBeat(beat);
            if(beat==WalkthroughBeat.Smoke)
            {
                var p=WalkthroughHost;p.hp=80;p.room=3;p.cigaretteBuff=true;
                PlaceTutorialPlayer(p,SaloonTrainingMap.Deck,Quaternion.LookRotation(SaloonSunset.Direction).eulerAngles.y);
                UpdateWalkthroughGoal();Broadcast();SyncPointer();
            }
        }
#endif

        void RestoreWalkthrough()
        {
            if(trainingMap!=null){trainingMap.Dispose();trainingMap=null;}
            TutorialWalkthrough=false;walkthroughPath.Clear();
        }

        void RouteGoal(Vector3 destination,string label,params Vector3[] via)
        {
            walkthroughPath.Clear();foreach(var point in via)walkthroughPath.Enqueue(point);
            walkthroughDestination=destination;TutorialGoalLabel=label;UpdateWalkthroughGoal();
        }

        void UpdateWalkthroughGoal()
        {
            var p=WalkthroughHost;
            while(walkthroughPath.Count>0&&FlatDistance(p.body.transform.position,walkthroughPath.Peek())<.85f)walkthroughPath.Dequeue();
            TutorialGoal=walkthroughPath.Count>0?walkthroughPath.Peek():walkthroughDestination;
            if(WalkthroughStep==WalkthroughBeat.Smoke&&p.room==3)TutorialGoal=null;
            var position=p.body.transform.position;
            if(WalkthroughStep==WalkthroughBeat.Spectate&&CameraPlayer!=null)
                position=new Vector3(CameraPlayer.x,CameraPlayer.y,CameraPlayer.z);
            var focus=TutorialGoal??position+Quaternion.Euler(0,p.yaw,0)*Vector3.forward*1.5f;
            if(WalkthroughStep==WalkthroughBeat.Spectate)
                focus=SaloonBeerTaps.Position;
            // First light a reachable waypoint. At the station, light the object itself.
            if(walkthroughPath.Count==0)
            {
                switch(WalkthroughStep)
                {
                    case WalkthroughBeat.Heal:
                    case WalkthroughBeat.Boost:focus=SaloonBeerTaps.Position;break;
                    case WalkthroughBeat.Sheriff:
                    case WalkthroughBeat.Deputy:
                    case WalkthroughBeat.Outlaw:
                    case WalkthroughBeat.Escape:focus=SaloonArt.SafePosition+Vector3.up*.65f;break;
                    case WalkthroughBeat.Coin:focus=SaloonCoinTable.Center;break;
                    case WalkthroughBeat.Blackjack:focus=SaloonBlackjackTable.Center;break;
                }
            }
            trainingMap.Focus(focus,position,p.room==3);
        }

        public bool WalkthroughActorVisible(string id)=>!TutorialWalkthrough||
            WalkthroughStep==WalkthroughBeat.Spectate||id==Connection.LocalId.ToString()||
            id==WalkthroughPeer.id.ToString()&&(WalkthroughStep==WalkthroughBeat.Quick||WalkthroughStep==WalkthroughBeat.Accurate||
                WalkthroughStep==WalkthroughBeat.Renegade||walkthroughTable&&(WalkthroughStep==WalkthroughBeat.Coin||WalkthroughStep==WalkthroughBeat.Blackjack));

        static float FlatDistance(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;

        void RouteAction()
        {
            AdvanceTutorialRound();WalkthroughHost.lastAction=-100;
            WalkthroughHost.nextShot=0;WalkthroughHost.fireAfterDraw=false;CancelShotCharge();
        }

        void SetWalkthroughBeat(WalkthroughBeat beat)
        {
            var p=WalkthroughHost;
            WalkthroughStep=beat;
            walkthroughDestination=null;walkthroughPath.Clear();TutorialGoal=null;TutorialGoalLabel="";
            WalkthroughFeedback=beat==WalkthroughBeat.Walk?"":"Good. Keep going.";walkthroughFeedbackUntil=Time.unscaledTime+1.1f;
            WalkthroughTitle="";WalkthroughInstruction="";WalkthroughContext="";
            p.notice="";RoomPlannerOpen=false;
            switch(beat)
            {
                case WalkthroughBeat.Walk:
                    WalkthroughTitle="Make yourself at home";WalkthroughInstruction="WASD · Walk into the light.";
                    WalkthroughContext="Move the mouse to look. No one will attack you.";
                    RouteGoal(new Vector3(0,.08f,-.4f),"Walk here");break;
                case WalkthroughBeat.Crouch:
                    WalkthroughTitle="Stay hard to hit";WalkthroughInstruction="Hold Ctrl to crouch.";
                    WalkthroughContext="Moving and crouching do not use your action.";break;
                case WalkthroughBeat.Quick:
                    WalkthroughTitle="Try the revolver";WalkthroughInstruction="Q · Draw. Right-click · Fire a quick shot.";
                    WalkthroughContext="Only headshots cause damage. A quick shot has more spread.";
                    RouteGoal(SaloonTrainingMap.Target+Vector3.up*1.65f,"Training target");
                    walkthroughShots=p.shots;tutorialQuick=false;break;
                case WalkthroughBeat.Accurate:
                    RouteAction();tutorialAccurateHit=false;
                    WalkthroughTitle="Now take your time";WalkthroughInstruction="Aim at the head. Hold left-click for 0.5 seconds.";
                    WalkthroughContext="Hold still for accuracy. Release early to cancel. A miss still uses your action.";
                    RouteGoal(SaloonTrainingMap.Target+Vector3.up*1.65f,"Headshot target");break;
                case WalkthroughBeat.Reload:
                    RouteAction();WalkthroughTitle="Keep the cylinder ready";WalkthroughInstruction="R · Reload the drawn revolver.";
                    WalkthroughContext="Reloading uses your action. Wait for six bullets.";break;
                case WalkthroughBeat.Pass:
                    RouteAction();WalkthroughTitle="1 ACTION = 1 ROUND";WalkthroughInstruction="F · Pass this round's action.";
                    WalkthroughContext="Each player gets one action per round. Moving, aiming, drawing, and minigame choices are free.";break;
                case WalkthroughBeat.Heal:
                    RouteAction();p.hp=60;p.drawn=false;WalkthroughTitle="A beer can save you";
                    WalkthroughInstruction="Walk to the beer tray. Press E nearby.";
                    WalkthroughContext="Beer heals 20% when you finish. It uses one action and one shared beer.";
                    RouteGoal(new Vector3(SaloonBeerTaps.Position.x,.08f,2.1f),"Shared beer tray",new Vector3(0,.08f,-.3f));break;
                case WalkthroughBeat.Plan:
                    rounds.Start(players.Keys,Time.time,3600,3600,0);phase=rounds.Phase;
                    foreach(var b in players.Values.Where(b=>b.bot))rounds.SetReady(b.id,true);
                    p.planSelected=false;ResetPlanDraft();Broadcast();RoomPlannerOpen=false;
                    WalkthroughTitle="Decide where to go";WalkthroughInstruction="Right-click · Planner. Select Bar. Press F to lock.";
                    WalkthroughContext="During discussion, choose a room. Your plan is public.";break;
                case WalkthroughBeat.Coin:
                    RouteAction();walkthroughTable=false;
                    WalkthroughTitle="Call the coin";WalkthroughInstruction="Walk through the central aisle to Coin Flip.";
                    WalkthroughContext="These rooms are opaque in both directions. Nearby voices through their walls sound muffled; voices in your own room stay clear.";
                    RouteGoal(SaloonStations.Position(false,1,1),"Coin Flip",new Vector3(0,.08f,-3.5f));break;
                case WalkthroughBeat.Blackjack:
                    LeaveWalkthroughTable();RouteAction();walkthroughTable=false;
                    WalkthroughTitle="Try a hand of Blackjack";WalkthroughInstruction="Space · Stand. Walk to the other table.";
                    WalkthroughContext="Get close to 21 without going over. Hit takes a card. Stand keeps your total. Aces count as 1 or 11.";
                    RouteGoal(SaloonStations.Position(false,2,0),"Blackjack",new Vector3(0,.08f,-2.7f));break;
                case WalkthroughBeat.Smoke:
                    LeaveWalkthroughTable();RouteAction();
                    WalkthroughTitle="Step out for a smoke";WalkthroughInstruction="Space · Stand. Follow the light to the deck.";
                    WalkthroughContext="Outside chat is separate. You cannot watch, shoot, heal, or guard the cash drawer here.";
                    RouteGoal(SaloonTrainingMap.Deck,"Smoking deck",new Vector3(0,.08f,-2.7f),new Vector3(0,.08f,-.5f),new Vector3(7.3f,.08f,1.2f));break;
                case WalkthroughBeat.Boost:
                    RouteAction();p.hp=40;
                    WalkthroughTitle="Bring the boost back inside";WalkthroughInstruction="Return to the beer tray. Press E to heal.";
                    WalkthroughContext="One visit boosts your next shot OR beer to 40%. Extra drags do not stack.";
                    RouteGoal(new Vector3(SaloonBeerTaps.Position.x,.08f,2.1f),"Boosted beer",new Vector3(7.3f,.08f,1.2f),new Vector3(0,.08f,-.3f));break;
                case WalkthroughBeat.Sheriff:
                    RouteAction();p.role=Role.Sheriff;p.inspectedProgress=-1;p.safeReport="";safeProgress=SaloonRules.DrawerWorkSteps;
                    WalkthroughTitle="Put on the Sheriff's badge";WalkthroughInstruction="Walk to the cash drawer. Press C nearby.";
                    WalkthroughContext="Sheriff: protect the money. Your Deputies are hidden. Everyone knows you.";
                    RouteGoal(SafeApproach,"Cash drawer",new Vector3(2.4f,.08f,.4f));break;
                case WalkthroughBeat.Deputy:
                    RouteAction();p.role=Role.Deputy;p.safeReport="";players.Values.First(b=>b.bot).role=Role.Sheriff;
                    WalkthroughTitle="Now help as a Deputy";WalkthroughInstruction="C · Inspect the cash drawer as a Deputy.";
                    WalkthroughContext="Deputy: help the Sheriff. Share your private report. Shots can hurt allies; hit feedback does not reveal their roles.";
                    RouteGoal(SafeApproach,"Inspect the cash drawer");break;
                case WalkthroughBeat.Outlaw:
                    RouteAction();p.role=Role.Outlaw;safeProgress=lastSafeWorkRound=0;p.safeReport="";
                    WalkthroughTitle="Switch sides: take the money";WalkthroughInstruction="H · Work the drawer. Complete three steps.";
                    WalkthroughContext="Outlaw: kill the Sheriff, or steal and escape. Three shared steps in separate rounds take the money. A normal inspection looks the same.";
                    RouteGoal(SafeApproach,"Cash drawer");break;
                case WalkthroughBeat.Escape:
                    PrepareTutorialEscapePlanning();walkthroughEscaping=false;
                    WalkthroughTitle="Get the money out";WalkthroughInstruction="X · Choose escape, then F · Lock your plan.";
                    WalkthroughContext="The attempt starts with room actions. Stay at Bar; one headshot stops it. Your whole Outlaw team shares a successful escape.";break;
                case WalkthroughBeat.Renegade:
                    RouteAction();p.bag=false;p.escapeRound=0;p.role=Role.Renegade;walkthroughReadRole=false;
                    var target=WalkthroughPeer;target.hp=20;target.deathAt=-1;target.role=Role.Outlaw;target.room=0;
                    PlaceTutorialPlayer(target,new Vector3(2.4f,.08f,.9f),90);target.hitbox.enabled=target.head.enabled=true;
                    WalkthroughTitle="Try the lone Renegade";WalkthroughInstruction="Hold Tab to review your goal. Shoot the marked target.";
                    WalkthroughContext="Renegade: defeat everyone else, leaving the Sheriff until last. Be last alive. Discover hidden roles. You never reload.";
                    RouteGoal(target.body.transform.position+Vector3.up*1.65f,"Outlaw target");break;
                case WalkthroughBeat.Spectate:
                    // Give every spectator target a clear view, rather than watching idle bots behind the counter.
                    int observerSlot=0;
                    foreach(var bot in players.Values.Where(b=>b.bot).OrderBy(b=>b.id))
                    {
                        bot.hp=100;bot.deathAt=-1;bot.room=0;bot.drawn=false;bot.crouched=bot.crouchHeld=false;
                        var station=new Vector3(-4.2f+observerSlot++*1.6f,.08f,1.1f);
                        PlaceTutorialPlayer(bot,station,Quaternion.LookRotation(SaloonBeerTaps.Position-station).eulerAngles.y);
                    }
                    TakeDamage(p,Vector3.back,SaloonHealth.Maximum);tutorialFirstViewpoint=null;
                    WalkthroughTitle="Even elimination has a view";WalkthroughInstruction="Left / Right arrow keys · Switch player.";
                    WalkthroughContext="This is a practice elimination. Watch another player, then switch to continue. Beer cannot revive you.";break;
                case WalkthroughBeat.Exit:
                    p.hp=100;p.deathAt=-1;p.room=0;p.drawn=false;PlaceTutorialPlayer(p,new Vector3(-3,.08f,1.9f),180);
                    p.hitbox.enabled=p.head.enabled=true;RouteAction();spectateId=null;
                    WalkthroughTitle="You're ready";WalkthroughInstruction="Walk to the exit to finish.";
                    WalkthroughContext="V: talk. T: open mic. M: mute. Press F1 for full rules and topic practice.";
                    RouteGoal(SaloonTrainingMap.Exit,"Exit",new Vector3(0,.08f,-.3f),new Vector3(2.1f,.08f,-6.5f));break;
                case WalkthroughBeat.Complete:
                    WalkthroughTitle="Welcome to Last Call";WalkthroughInstruction="Walkthrough complete.";MenuOpen=true;break;
            }
            tutorialStartPosition=p.body.transform.position;walkthroughShots=p.shots;UpdateWalkthroughGoal();
            CaptureCombatFrames();Broadcast();SyncPointer();
        }

        void StartWalkthroughTable(int room)
        {
            var p=WalkthroughHost;var peer=WalkthroughPeer;
            RouteAction();duels.Clear();p.room=peer.room=room;
            PlaceTutorialPlayer(p,SaloonStations.Position(false,room,room==1?1:0),room==1?270:90);
            PlaceTutorialPlayer(peer,SaloonStations.Position(false,room,room==1?0:1),room==1?90:270);
            p.seat=100;peer.seat=101;p.controller.enabled=peer.controller.enabled=false;UpdatePlayerColliders(p);UpdatePlayerColliders(peer);
            SetupDuels(false);
            var d=duels[room];d.choiceUntil=Time.time+3600;
            if(room==2)
            {
                d.dealer=peer.id;
                int seed=0;do{d.blackjack=new SaloonBlackjack(new System.Random(seed++));}
                while(d.blackjack.Complete||d.blackjack.PlayerTotal<12||d.blackjack.PlayerTotal>17);
                d.readyAt=d.nextInputAt=Time.time+.7f;d.nextCardAt=Time.time+1;
            }
            walkthroughTable=true;TutorialGoal=null;walkthroughPath.Clear();walkthroughDestination=null;UpdateWalkthroughGoal();
            WalkthroughInstruction=room==1?"Choose Heads or Tails. After the result, press F when you can act.":"Choose Hit or Stand. After the hand, press F when you can act.";
            WalkthroughContext=room==1?"The winner gets the first action. The other player responds after that action or pass. No bonus actions.":
                "The dealer draws below 17. Over 21 loses. Ties deal again. The winner acts first. Your opponent will pass.";
            Broadcast();
        }

        void LeaveWalkthroughTable()
        {duels.Clear();WalkthroughPeer.room=0;WalkthroughHost.drawn=false;}

        void RecoverWalkthroughPlan()
        {
            var origin=tutorialStartPosition;
            duels.Clear();roomTurns.Clear();walkthroughTable=false;
            int botSlot=0;
            foreach(var person in players.Values)
            {
                person.room=person.chosenRoom=0;person.companion=null;person.planSelected=false;
                if(person.bot)PlaceTutorialPlayer(person,new Vector3(-7+botSlot++*1.6f,.08f,4.7f),180);
            }
            PlaceTutorialPlayer(WalkthroughHost,origin,0);
            SetWalkthroughBeat(WalkthroughBeat.Plan);
            WalkthroughHost.notice="Choose Bar for this exercise. The other rooms come next.";
            RoomPlannerOpen=true;Broadcast();SyncPointer();
        }

        void TickWalkthrough()
        {
            float delta=Time.unscaledTime-walkthroughClock;walkthroughClock=Time.unscaledTime;
            if(!TutorialActive||TutorialClosing||MenuOpen||TutorialHelpOpen||WalkthroughFinished)return;
            WalkthroughSeconds+=Mathf.Max(0,delta);
            var p=WalkthroughHost;var step=WalkthroughStep;
            if(step==WalkthroughBeat.Plan&&phase!=Phase.Discussion&&!(phase==Phase.Turns&&p.chosenRoom==0&&p.planSelected))
            {RecoverWalkthroughPlan();return;}
            if(Time.unscaledTime>walkthroughFeedbackUntil)WalkthroughFeedback="";
            UpdateWalkthroughGoal();rounds.EnsureDeadline(Time.time+120);
            // Walking between stations is the tutorial's only room transfer. Normal matches keep their planner transfers.
            if(!walkthroughTable||step!=WalkthroughBeat.Coin&&step!=WalkthroughBeat.Blackjack)
            {
                var pos=p.body.transform.position;int room=pos.x>3.2f&&pos.z>5.3f?3:pos.z<-1.8f&&pos.z>-5.65f?(pos.x<-3?1:pos.x>3?2:0):0;
                if(room!=p.room)
                {
                    if(room==3)
                    {
                        p.cigaretteBuff=true;
                        if(step==WalkthroughBeat.Smoke)
                        {p.yaw=p.bodyYaw=Quaternion.LookRotation(SaloonSunset.Direction).eulerAngles.y;p.pitch=0;p.poseVersion++;p.move=Vector2.zero;}
                    }
                    p.room=room;UpdateWalkthroughGoal();Broadcast();
                }
            }
            foreach(var d in duels.Values)
            {
                if(!d.flipping&&d.blackjack==null)d.choiceUntil=Time.time+8;
                if(d.revealed)
                {d.firstUntil=Time.time+120;if(d.first!=p.id&&Time.time>=d.combatAt&&!rounds.HasSpent(d.first))rounds.TrySpend(d.first,Time.time);}
            }
            bool done=false;
            switch(step)
            {
                case WalkthroughBeat.Walk:done=FlatDistance(p.body.transform.position,walkthroughDestination.Value)<.65f;break;
                case WalkthroughBeat.Crouch:done=p.crouched;break;
                case WalkthroughBeat.Quick:done=tutorialQuick&&p.shots>walkthroughShots;break;
                case WalkthroughBeat.Accurate:done=tutorialAccurateHit;break;
                case WalkthroughBeat.Reload:done=p.reloads>0&&p.ammo==6&&p.busyUntil==0;break;
                case WalkthroughBeat.Pass:done=rounds.HasSpent(p.id);break;
                case WalkthroughBeat.Heal:done=p.hp>=80&&p.busyUntil==0;break;
                case WalkthroughBeat.Plan:done=phase==Phase.Turns&&p.chosenRoom==0&&p.planSelected;break;
                case WalkthroughBeat.Coin:
                case WalkthroughBeat.Blackjack:
                    int room=step==WalkthroughBeat.Coin?1:2;
                    if(!walkthroughTable&&FlatDistance(p.body.transform.position,walkthroughDestination.Value)<1.25f)StartWalkthroughTable(room);
                    done=walkthroughTable&&duels[room].revealed&&rounds.HasSpent(p.id);break;
                case WalkthroughBeat.Smoke:
                    if(p.room==3)WalkthroughInstruction="Left-click · Take a drag. One visit gives one boost.";
                    done=p.puffs>0;break;
                case WalkthroughBeat.Boost:done=p.hp>=80&&!p.cigaretteBuff&&p.busyUntil==0;break;
                case WalkthroughBeat.Sheriff:
                case WalkthroughBeat.Deputy:done=!string.IsNullOrEmpty(p.safeReport);break;
                case WalkthroughBeat.Outlaw:done=p.bag;break;
                case WalkthroughBeat.Escape:
                    done=phase==Phase.Turns&&p.escapeRound==rounds.Round&&p.escapeRound>0;
                    if(done)walkthroughEscaping=true;
                    break;
                case WalkthroughBeat.Renegade:walkthroughReadRole|=ShowRole;done=walkthroughReadRole&&!WalkthroughPeer.Alive;break;
                case WalkthroughBeat.Spectate:
                    if(ObserverReady(p)&&!IsSpectating)Broadcast();
                    if(IsSpectating&&tutorialFirstViewpoint==null)tutorialFirstViewpoint=ViewpointId;
                    done=tutorialFirstViewpoint!=null&&ViewpointId!=tutorialFirstViewpoint;break;
                case WalkthroughBeat.Exit:done=FlatDistance(p.body.transform.position,walkthroughDestination.Value)<1;break;
            }
            if(done){SetWalkthroughBeat(step+1);return;}
            // Give another actual round after a wrong action or a miss, without a restart menu.
            bool repeat=step==WalkthroughBeat.Accurate||step==WalkthroughBeat.Heal||step==WalkthroughBeat.Boost||
                step==WalkthroughBeat.Reload||step==WalkthroughBeat.Sheriff||step==WalkthroughBeat.Deputy||step==WalkthroughBeat.Outlaw||
                step==WalkthroughBeat.Renegade||step==WalkthroughBeat.Escape&&!walkthroughEscaping;
            if(repeat&&phase==Phase.Turns&&rounds.HasSpent(p.id)&&p.busyUntil==0&&Time.time-tutorialShotAt>1)
            {
                RouteAction();if(!p.UnlimitedAmmo&&p.ammo==0)p.ammo=6;
                WalkthroughFeedback="New training round. Try again.";walkthroughFeedbackUntil=Time.unscaledTime+2;
                if(step==WalkthroughBeat.Accurate&&!WalkthroughPeer.Alive)
                {WalkthroughPeer.hp=100;WalkthroughPeer.deathAt=-1;WalkthroughPeer.head.enabled=WalkthroughPeer.hitbox.enabled=true;}
            }
        }
    }
}
