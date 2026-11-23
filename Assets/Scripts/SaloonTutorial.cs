using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        internal const string TutorialSeenKey="LastCall.Tutorial.SeenBuild";
        public static string TutorialBuildIdentity(string version,string guid)=>version+"|"+(string.IsNullOrEmpty(guid)?"unbuilt":guid);
        public static bool ShouldOfferTutorial(string seen,string build)=>seen!=build;
        public string TutorialBuild=>TutorialBuildIdentity(Application.version,Application.buildGUID);
        public bool TutorialWelcome { get; private set; }
        public bool TutorialActive { get; private set; }
        public bool TutorialHelpOpen { get; set; }
        public bool TutorialClosing { get; private set; }
        public int TutorialIndex { get; private set; }
        public bool TutorialTaskDone { get; private set; }
        public TutorialLesson TutorialLesson=>SaloonTutorialLessons.All[TutorialIndex];
        public readonly bool[] TutorialCompleted=new bool[SaloonTutorialLessons.All.Length];
        public Vector3? TutorialGoal { get; private set; }
        public string TutorialGoalLabel { get; private set; }="";
        public string TutorialProgress { get; private set; }="";
        Tuning beforeTutorialTuning;
        bool beforeTutorialBots;
        int beforeTutorialSize,tutorialWorkProgress;
        bool tutorialMoved,tutorialCrouched,tutorialReadRole,tutorialQuick,tutorialAccurateHit;
        Vector3 tutorialStartPosition;
        string tutorialFirstViewpoint;
        float tutorialShotAt,tutorialCompletedAt;

        void InitializeTutorial()
        {
            // Automated player runs must never be held by first-launch onboarding.
            TutorialWelcome=!Application.isEditor&&!Application.isBatchMode&&
                !Environment.GetCommandLineArgs().Contains("-saloonTest")&&
                ShouldOfferTutorial(PlayerPrefs.GetString(TutorialSeenKey,""),TutorialBuild);
        }

        public void OpenTutorial()
        {
            if(InRoom||Connection.Busy||TutorialClosing)return;
            TutorialWelcome=true;MenuOpen=true;SyncPointer();
        }

        void RememberTutorialBuild()
        {PlayerPrefs.SetString(TutorialSeenKey,TutorialBuild);PlayerPrefs.Save();}

        public void SkipTutorialWelcome()
        {if(!TutorialWelcome)return;RememberTutorialBuild();TutorialWelcome=false;MenuOpen=true;}

        bool StartTutorialSession()
        {
            if(InRoom||Connection.Busy||TutorialActive||TutorialClosing)return false;
            beforeTutorialTuning=tuning;beforeTutorialBots=BotsEnabled;beforeTutorialSize=BotMatchSize;
            tuning=new Tuning{discussionSeconds=3600,turnSeconds=3600,sharedBeers=8};
            BotsEnabled=true;BotMatchSize=5;TutorialActive=true;TutorialWelcome=false;
            Array.Clear(TutorialCompleted,0,TutorialCompleted.Length);
            StartSolo();
            if(!Connection.Solo){RestoreTutorialSettings();TutorialWelcome=true;return false;}
            return true;
        }

        public void BeginTutorialLibrary()
        {
            if(!StartTutorialSession())return;
            SelectTutorialLesson(0);
        }

        public void BeginTutorialTopic(int index)
        {
            TutorialHelpOpen=false;
            if(!TutorialActive&&!StartTutorialSession())return;
            RestoreWalkthrough();
            SelectTutorialLesson(index);
        }

        public void SelectTutorialLesson(int index)
        {
            if(!TutorialActive||TutorialClosing||!Connection.Solo||index<0||index>=SaloonTutorialLessons.All.Length)return;
            TutorialIndex=index;TutorialTaskDone=false;
            tutorialMoved=tutorialCrouched=tutorialReadRole=tutorialQuick=tutorialAccurateHit=false;
            tutorialWorkProgress=0;tutorialShotAt=-100;tutorialFirstViewpoint=null;
            TutorialGoal=null;TutorialGoalLabel="";TutorialProgress="";
            CancelShotCharge();ResetPlanDraft();spectateId=null;
            ReopenLobby();StartMatch();
            var host=players[Connection.LocalId];var bots=players.Values.Where(p=>p.bot).OrderBy(p=>p.id).ToArray();
            host.name=CleanName(PlayerName);host.role=Role.Sheriff;
            var roles=new[]{Role.Deputy,Role.Outlaw,Role.Outlaw,Role.Renegade};
            for(int i=0;i<bots.Length;i++){bots[i].role=roles[i];bots[i].name="Training "+roles[i]+" "+(i+1);}
            var task=TutorialLesson.Task;
            if(task==TutorialTask.Deputy){host.role=Role.Deputy;bots[0].role=Role.Sheriff;}
            if(task==TutorialTask.SafeWork||task==TutorialTask.Escape){host.role=Role.Outlaw;bots[1].role=Role.Sheriff;}
            if(task==TutorialTask.Renegade){host.role=Role.Renegade;bots[3].role=Role.Sheriff;}
            int room=task==TutorialTask.Smoke?3:task==TutorialTask.RoomPrivacy||task==TutorialTask.Coin?1:
                task==TutorialTask.BlackjackPlayer||task==TutorialTask.BlackjackDealer?2:0;
            foreach(var p in players.Values){p.chosenRoom=0;p.companion=null;rounds.SetReady(p.id,true);}
            host.chosenRoom=room;
            if(room==1||room==2){bots[1].chosenRoom=room;host.companion=bots[1].id;bots[1].companion=host.id;}
            if(task==TutorialTask.Plan)
            {rounds.SetReady(host.id,false);host.planSelected=false;}
            else
            {rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);}
            roleIntroStarted=float.NegativeInfinity;
            if(task==TutorialTask.Heal||task==TutorialTask.SmokeBeer)
            {
                host.hp=task==TutorialTask.Heal?60:40;host.cigaretteBuff=task==TutorialTask.SmokeBeer;
                SetTutorialGoal(SaloonBeerTaps.Position,"Shared beer tray");
            }
            if(task==TutorialTask.Reload)host.ammo=2;
            if(task==TutorialTask.QuickShot||task==TutorialTask.AccurateShot||task==TutorialTask.Renegade)
            {
                PlaceTutorialPlayer(host,new Vector3(1.8f,.08f,1.2f),90);
                PlaceTutorialPlayer(bots[1],new Vector3(3.8f,.08f,1.2f),270);
                if(task==TutorialTask.Renegade)bots[1].hp=20;
                SetTutorialGoal(bots[1].body.transform.position+Vector3.up*1.65f,"Outlaw training target");
            }
            if(task==TutorialTask.WatchRooms)
            {
                bots[1].room=1;PlaceTutorialPlayer(bots[1],SaloonStations.Position(false,1,0),90);
                bots[2].room=2;PlaceTutorialPlayer(bots[2],SaloonStations.Position(false,2,0),270);
                SetTutorialGoal(new Vector3(-5,1.4f,SaloonRoomWindows.FrontZ),"Painted room wall");
            }
            if(task==TutorialTask.RoomPrivacy)
            {duels.Clear();SetTutorialGoal(new Vector3(-5,1.4f,SaloonRoomWindows.FrontZ),"Solid room wall");}
            if(task==TutorialTask.Coin)duels[1].choiceUntil=Time.time+3600;
            if(task==TutorialTask.BlackjackPlayer||task==TutorialTask.BlackjackDealer)
            {
                var d=duels[2];d.dealer=task==TutorialTask.BlackjackDealer?host.id:bots[1].id;
                // Start with a playable hand so every learner gets an actual choice.
                int seed=0;
                do{d.blackjack=new SaloonBlackjack(new System.Random(seed++));}
                while(d.blackjack.Complete||d.blackjack.DealerTotal>=17||d.blackjack.PlayerTotal>=17);
                d.choiceUntil=Time.time+3600;
            }
            if(task==TutorialTask.Sheriff||task==TutorialTask.Deputy||task==TutorialTask.SafeWork)
            {
                if(task!=TutorialTask.SafeWork){safeProgress=SaloonRules.DrawerWorkSteps;lastSafeWorkRound=1;}
                SetTutorialGoal(SaloonArt.SafePosition+Vector3.up*.6f,"Cash drawer · Space, then walk here");
            }
            if(task==TutorialTask.Escape){host.bag=true;host.bagRound=rounds.Round-1;PrepareTutorialEscapePlanning();}
            if(task==TutorialTask.Spectate)TakeDamage(host,Vector3.back,SaloonHealth.Maximum);
            tutorialStartPosition=host.body.transform.position;
            CaptureCombatFrames();Broadcast();RoomPlannerOpen=false;MenuOpen=true;ShowRole=false;SyncPointer();
            if(task==TutorialTask.Finish)TutorialTaskDone=true;
        }

        void SetTutorialGoal(Vector3 point,string label){TutorialGoal=point;TutorialGoalLabel=label;}

        void PlaceTutorialPlayer(Participant p,Vector3 point,float facing)
        {
            p.controller.enabled=false;p.body.transform.position=point;p.seat=-1;p.gatherStation=false;
            p.move=Vector2.zero;p.yaw=p.bodyYaw=facing;p.pitch=0;p.poseVersion++;UpdatePlayerColliders(p);p.controller.enabled=true;
        }

        public void TryTutorialLesson()
        {if(!TutorialActive||TutorialClosing)return;MenuOpen=false;SyncPointer();}

        public void NextTutorialLesson()
        {
            if(!TutorialActive||TutorialClosing)return;
            if(TutorialIndex+1==SaloonTutorialLessons.All.Length)EndTutorial();else SelectTutorialLesson(TutorialIndex+1);
        }

        public async void EndTutorial()
        {
            if(TutorialClosing||!TutorialActive)return;
            TutorialClosing=true;RememberTutorialBuild();
            try{await LeaveAsync();}finally{TutorialClosing=false;}
        }

        void RestoreTutorialSettings()
        {
            RestoreWalkthrough();TutorialHelpOpen=false;
            if(beforeTutorialTuning!=null)
            {tuning=beforeTutorialTuning;BotsEnabled=beforeTutorialBots;BotMatchSize=beforeTutorialSize;beforeTutorialTuning=null;}
            TutorialActive=false;TutorialGoal=null;TutorialWelcome=false;
        }

        void PrepareTutorialEscapePlanning()
        {
            rounds.BeginAftermath(Time.time-10);rounds.Tick(Time.time,id=>players[id].Alive);
            rounds.Tick(Time.time+4,id=>players[id].Alive);phase=rounds.Phase;
            foreach(var p in players.Values){p.chosenRoom=0;p.companion=null;p.planSelected=true;rounds.SetReady(p.id,p.bot);}
            RoomPlannerOpen=false;
        }

        void AdvanceTutorialRound()
        {
            // Skip only the training wait. Keep the production action authorization and round number.
            rounds.BeginAftermath(Time.time-10);rounds.Tick(Time.time,id=>players[id].Alive);
            rounds.Tick(Time.time+4,id=>players[id].Alive);
            foreach(var id in players.Keys)rounds.SetReady(id,true);
            rounds.Tick(Time.time,id=>players[id].Alive);phase=rounds.Phase;
        }

        void HoldTutorialTimers(float delta)
        {
            foreach(var p in players.Values)if(p.busyUntil>0)p.busyUntil+=delta;
            foreach(var d in duels.Values)
            {d.choiceUntil+=delta;d.readyAt+=delta;d.nextInputAt+=delta;d.nextCardAt+=delta;
                if(d.revealAt>0)d.revealAt+=delta;if(d.combatAt>0)d.combatAt+=delta;if(d.firstUntil>0)d.firstUntil+=delta;}
        }

        void RecordTutorialShot(Participant p,ShotIntent intent)
        {
            if(!TutorialActive||p.id!=Connection.LocalId)return;
            tutorialQuick|=intent?.Quick==true;tutorialShotAt=Time.time;
        }

        void TickTutorial()
        {
            if(TutorialWalkthrough){TickWalkthrough();return;}
            if(!TutorialActive||TutorialClosing||MenuOpen||!players.TryGetValue(Connection.LocalId,out var p))return;
            if(TutorialTaskDone)
            {
                float feedback=TutorialLesson.Task==TutorialTask.Smoke?SaloonSmoking.Duration:1.5f;
                if(!ShowRole&&Time.unscaledTime-tutorialCompletedAt>=feedback){MenuOpen=true;SyncPointer();}
                return;
            }
            var task=TutorialLesson.Task;
            tutorialReadRole|=ShowRole;
            tutorialCrouched|=p.crouched;
            tutorialMoved|=(p.body.transform.position-tutorialStartPosition).sqrMagnitude>.4f;
            rounds.EnsureDeadline(Time.time+120);
            foreach(var d in duels.Values.Where(d=>d.revealed))
            {
                d.firstUntil=Time.time+120;
                if(d.first!=p.id&&Time.time>=d.combatAt&&!rounds.HasSpent(d.first))rounds.TrySpend(d.first,Time.time);
            }
            if(task==TutorialTask.SafeWork&&safeProgress>tutorialWorkProgress)
            {tutorialWorkProgress=safeProgress;if(!p.bag)AdvanceTutorialRound();}
            // A failed work attempt or an accidental wrong action must not trap the learner.
            bool retry=task==TutorialTask.SafeWork?!p.bag:
                task==TutorialTask.Heal||task==TutorialTask.SmokeBeer?p.hp<80:
                task==TutorialTask.Reload?p.ammo<tuning.cylinder:
                task==TutorialTask.Sheriff||task==TutorialTask.Deputy?string.IsNullOrEmpty(p.safeReport):
                false;
            if(retry&&rounds.HasSpent(p.id)&&p.busyUntil==0)AdvanceTutorialRound();
            if((task==TutorialTask.AccurateShot||task==TutorialTask.Renegade)&&rounds.HasSpent(p.id)&&
                Time.time-tutorialShotAt>1&&(task==TutorialTask.AccurateShot?!tutorialAccurateHit:p.headshots==0))
            {AdvanceTutorialRound();p.ammo=tuning.cylinder;}
            bool done=false;
            switch(task)
            {
                case TutorialTask.Move:done=tutorialMoved&&tutorialCrouched&&p.seat<0;break;
                case TutorialTask.Plan:done=phase==Phase.Turns&&p.room==0&&p.planSelected;break;
                case TutorialTask.Action:done=rounds.HasSpent(p.id);break;
                case TutorialTask.RoleCard:done=tutorialReadRole;break;
                case TutorialTask.Heal:done=p.hp>=80&&p.busyUntil==0;break;
                case TutorialTask.QuickShot:done=tutorialQuick;break;
                case TutorialTask.AccurateShot:done=tutorialAccurateHit;break;
                case TutorialTask.Reload:done=p.reloads>0&&p.busyUntil==0&&p.ammo==tuning.cylinder;break;
                case TutorialTask.WatchRooms:
                case TutorialTask.RoomPrivacy:
                    done=tutorialMoved&&p.seat<0&&Vector3.ProjectOnPlane(p.body.transform.position-TutorialGoal.Value,Vector3.up).magnitude<2.5f;break;
                case TutorialTask.Coin:done=duels[1].claimed&&duels[1].revealed&&rounds.HasSpent(p.id);break;
                case TutorialTask.BlackjackPlayer:
                case TutorialTask.BlackjackDealer:done=duels[2].revealed&&rounds.HasSpent(p.id);break;
                case TutorialTask.Smoke:done=p.puffs>0;break;
                case TutorialTask.SmokeBeer:done=p.hp>=80&&!p.cigaretteBuff&&p.busyUntil==0;break;
                case TutorialTask.Sheriff:
                case TutorialTask.Deputy:done=!string.IsNullOrEmpty(p.safeReport);break;
                case TutorialTask.SafeWork:TutorialProgress="Drawer work "+safeProgress+" / "+SaloonRules.DrawerWorkSteps+" · one step per round.";done=p.bag;break;
                case TutorialTask.Escape:done=phase==Phase.Turns&&p.escapeRound==rounds.Round&&p.escapeRound>0;break;
                case TutorialTask.Renegade:done=tutorialReadRole&&players.Values.Any(b=>b.bot&&b.role==Role.Outlaw&&!b.Alive);break;
                case TutorialTask.Spectate:
                    if(IsSpectating&&tutorialFirstViewpoint==null)tutorialFirstViewpoint=ViewpointId;
                    done=tutorialFirstViewpoint!=null&&ViewpointId!=tutorialFirstViewpoint;break;
            }
            if(!done)return;
            TutorialTaskDone=true;TutorialCompleted[TutorialIndex]=true;
            tutorialCompletedAt=Time.unscaledTime;CancelShotCharge();
        }
    }
}
