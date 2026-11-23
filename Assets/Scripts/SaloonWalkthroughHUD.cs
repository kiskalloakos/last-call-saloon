using System;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        string tutorialSearch="";
        int helpTopic;
        Vector2 helpTopicsScroll,helpTextScroll;
        float helpTextHeight;
        public bool WalkthroughSpectatorControlsVisible { get; private set; }

        void WalkthroughGameplay()
        {
            worldHud=false;
            WalkthroughSpectatorControlsVisible=false;
            if(Game.RoomPlannerOpen){Paper(RoomChoices);return;}
            worldHud=true;
            Vitals();
            worldHud=false;
            if(Game.WalkthroughStep==SaloonGame.WalkthroughBeat.Spectate)
            {
                WalkthroughSpectatorControlsVisible=true;
                string watching=Game.IsSpectating?"Watching "+Game.CameraPlayer.name:"Starting spectator view…";
                WrappedText(360,565,560,watching,MenuStyle(20,MenuInk,TextAnchor.UpperCenter));
                if(MenuButton(new Rect(410,610,220,46),"← Previous player",enabled:Game.IsSpectating))Game.CycleSpectator(-1);
                if(MenuButton(new Rect(650,610,220,46),"Next player →",enabled:Game.IsSpectating))Game.CycleSpectator(1);
                return;
            }
            bool aiming=Game.WalkthroughStep==SaloonGame.WalkthroughBeat.Quick||
                Game.WalkthroughStep==SaloonGame.WalkthroughBeat.Accurate||Game.WalkthroughStep==SaloonGame.WalkthroughBeat.Renegade;
            if(aiming)
            {
                float charge=Game.ShotChargeProgress;
                float gap=Mathf.Lerp(7,2,charge);
                Crosshair(gap);
                if(charge>0){Fill(new Rect(622,380,36,2),MenuStroke);Fill(new Rect(622,380,36*charge,2),MenuAccent);}
                CombatHitMarker();
            }
            if(Game.View.duel?.stage!="result")MinigameControls();
        }

        void WalkthroughOverlay()
        {
            worldHud=false;
            if(Game.RoomPlannerOpen)
            {
                Fill(new Rect(108,688,1064,30),MenuBase);
                MenuText(120,691,1040,"Choose Bar. Press F to lock.",17);return;
            }
            if(Game.WalkthroughStep==SaloonGame.WalkthroughBeat.Pass)
            {
                Fill(new Rect(258,200,764,316),new Color(.025f,.035f,.04f,.94f));
                TutorialRule(288,223,704,36);
                MenuText(288,282,704,"Each player gets ONE action per round. A missed shot counts.",22);
                MenuText(288,325,330,"Shoot (left / right click)\nReload (R)\nDrink beer (E)\nCheck the drawer (C)",21);
                MenuText(654,325,338,"Check drawer (C)\nTake money (H)\nPlan escape next discussion (X)\nPass (F)",21);
                MenuText(288,441,704,"Moving (WASD), aiming (mouse), drawing (Q), and minigame choices are free.",17,muted:true);
                WrappedText(288,480,704,"F · Pass your action to continue",MenuStyle(23,MenuAccent,TextAnchor.UpperCenter));
                return;
            }
            string instruction=Game.WalkthroughInstruction,hint=Game.WalkthroughContext;
            var self=Game.Self;var goal=Game.TutorialGoal;var camera=Game.ViewCamera;
            if(Game.WalkthroughStep==SaloonGame.WalkthroughBeat.Spectate)
            {
                instruction=Game.IsSpectating?"Left / Right arrow keys · Switch player.":
                    "Practice elimination · Spectator view in "+Mathf.Max(1,Mathf.CeilToInt(SaloonRagdoll.ObserverDelay-(self?.deathAge??0)))+"…";
                hint=Game.IsSpectating?"Use the arrow keys or click Previous / Next to continue. Your mic reaches eliminated players only.":
                    "You will watch through another player's eyes. This short death-camera view ends automatically.";
            }
            bool aiming=Game.WalkthroughStep==SaloonGame.WalkthroughBeat.Quick||
                Game.WalkthroughStep==SaloonGame.WalkthroughBeat.Accurate||Game.WalkthroughStep==SaloonGame.WalkthroughBeat.Renegade;
            if(goal.HasValue&&self?.alive==true&&!Game.PlayingTableGame)
            {
                var position=new Vector3(self.x,self.y,self.z);
                if(!aiming)
                {
                    if(self.seated)instruction="Space · Stand up. Then follow the light.";
                    else
                    {
                        instruction="WASD · Follow the light.";
                        if(self.room==3&&Game.WalkthroughStep==SaloonGame.WalkthroughBeat.Boost)instruction="WASD · Return inside to the bar.";
                        switch(Game.WalkthroughStep)
                        {
                            case SaloonGame.WalkthroughBeat.Heal:
                            case SaloonGame.WalkthroughBeat.Boost:
                                if(self.room==0&&SaloonBeerTaps.WithinReach(position,Game.tuning.interactionDistance))instruction="E · Drink a beer from the lit tray.";break;
                            case SaloonGame.WalkthroughBeat.Sheriff:
                            case SaloonGame.WalkthroughBeat.Deputy:
                                if(Game.NearSafe)instruction="C · Check the lit cash drawer.";break;
                            case SaloonGame.WalkthroughBeat.Outlaw:
                                if(Game.NearSafe)instruction="H · Work the drawer. Stay close.";break;
                            case SaloonGame.WalkthroughBeat.Smoke:
                                if(self.room==3)instruction="Left-click · Take a drag.";break;
                        }
                    }
                }
                else if(Game.WalkthroughNeedsRole)instruction="Hold Tab · Find the Outlaw's role.";
                else if(!self.drawn)instruction="Q · Draw your revolver.";
                else if(Game.WalkthroughStep==SaloonGame.WalkthroughBeat.Quick)instruction="Aim at the head. Right-click · Fire a quick shot.";
                // Directions appear only when the single light is outside the player's view.
                var to=Game.WalkthroughFocus-camera.transform.position;
                float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up),Vector3.ProjectOnPlane(to,Vector3.up),Vector3.up);
                var screen=camera.WorldToViewportPoint(Game.WalkthroughFocus);
                bool vertical=(aiming||Game.WalkthroughFocus.y>.5f)&&(screen.y<.04f||screen.y>.96f);
                if(screen.z<=0||screen.x<.06f||screen.x>.94f||vertical)
                {
                    string turn=Mathf.Abs(angle)>140?"Turn around":angle>25?"Turn right":angle<-25?"Turn left":screen.y>.96f?"Look up":"Look down";
                    hint=instruction;instruction=self.seated?"Space · Stand up. Then "+turn.ToLowerInvariant()+".":turn+" · Find the light.";
                }
            }
            var duel=Game.View.duel;
            if(Game.PlayingTableGame&&duel!=null&&(duel.stage=="result"||duel.stage=="playing"))
                instruction=Game.View.actionAvailable?"F · Pass your action.":"Wait for your opponent's action.";
            if(Game.View.busyRemaining>0)
                instruction=(self.drinking?"Drinking":self.reloading?"Reloading":"Checking the drawer")+"… "+Game.View.busyRemaining.ToString("0.0")+" s";
            if(Game.WalkthroughFeedback.StartsWith("New training round"))hint=Game.WalkthroughFeedback;
            var main=MenuStyle(24,MenuInk,TextAnchor.UpperCenter);main.fontStyle=FontStyle.Bold;
            var note=MenuStyle(17,MenuMuted,TextAnchor.UpperCenter);
            const float width=800;
            float h=WrappedHeight(instruction,width-40,main),detail=WrappedHeight(hint,width-40,note);
            Fill(new Rect(240,26,width,h+detail+31),new Color(.025f,.035f,.04f,.88f));
            WrappedText(260,39,width-40,instruction,main);
            WrappedText(260,45+h,width-40,hint,note);
            WrappedText(920,685,330,"Esc · Pause     F1 · Help",MenuStyle(15,new Color(.7f,.71f,.7f,.65f),TextAnchor.UpperRight));
        }

        void WalkthroughPauseScreen()
        {
            MenuBackdrop(false,true);MenuFrame(new Rect(344,170,592,380));
            bool done=Game.WalkthroughFinished;
            MenuText(380,205,520,done?"YOU'RE READY":"TAKE A BREATHER",32,heading:true);
            MenuText(380,264,520,done?"You tried the essentials. Full rules and topic practice remain available in Help topics.":
                "Your walkthrough stays here. Resume when you are ready.",21,muted:true);
            if(MenuButton(new Rect(380,346,520,48),done?"Back to main menu":"Resume walkthrough",true,!Game.TutorialClosing))
            {if(done)Game.EndTutorial();else Game.MenuOpen=false;}
            if(MenuButton(new Rect(380,408,254,44),"Help topics",enabled:!Game.TutorialClosing))Game.TutorialHelpOpen=true;
            if(MenuButton(new Rect(648,408,252,44),"Leave tutorial",enabled:!Game.TutorialClosing))Game.EndTutorial();
            if(!done)MenuText(380,495,520,"Enter or Esc · Resume",17,muted:true);
        }

        void TutorialHelpScreen()
        {
            MenuBackdrop(!Game.InRoom,true);MenuFrame(new Rect(36,30,1208,660));
            MenuText(64,56,300,"HELP TOPICS",29,heading:true);
            MenuText(390,58,790,"Find an answer. Try it when you need it.",23,muted:true);
            string query=MenuField(new Rect(64,108,284,40),tutorialSearch,60);
            if(query!=tutorialSearch)
            {
                tutorialSearch=query;helpTopicsScroll=Vector2.zero;helpTextScroll=Vector2.zero;helpTextHeight=0;
                int match=Array.FindIndex(SaloonTutorialLessons.All,l=>l.Title.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0);
                if(match<0)match=Array.FindIndex(SaloonTutorialLessons.All,l=>l.Details.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0);
                if(match>=0)helpTopic=match;
            }
            helpTopicsScroll=GUI.BeginScrollView(new Rect(64,162,300,414),helpTopicsScroll,new Rect(0,0,280,SaloonTutorialLessons.All.Length*45));
            float row=0;
            for(int i=0;i<SaloonTutorialLessons.All.Length;i++)
            {
                var topic=SaloonTutorialLessons.All[i];
                if(tutorialSearch.Length>0&&(topic.Title+" "+topic.Details).IndexOf(tutorialSearch,StringComparison.OrdinalIgnoreCase)<0)continue;
                if(MenuButton(new Rect(0,row,280,40),topic.Title,i==helpTopic))
                {helpTopic=i;helpTextScroll=Vector2.zero;helpTextHeight=0;}row+=45;
            }
            if(row==0)MenuText(0,0,280,"No topics match. Try 'beer', 'drawer', or 'shoot'.",19,muted:true);
            GUI.EndScrollView();
            const float left=394,width=810;
            var lesson=SaloonTutorialLessons.All[helpTopic];
            MenuText(left,110,width,lesson.Title,30,heading:true);
            helpTextScroll=GUI.BeginScrollView(new Rect(left,167,width,409),helpTextScroll,new Rect(0,0,width-18,helpTextHeight));
            float y=0;
            if(lesson.Task==TutorialTask.Action)
            {
                TutorialRule(0,0,width-32,34);y=60;
                y+=MenuText(0,y,width-32,SaloonTutorialLessons.ActionMeaning,22)+20;
                y+=TutorialList(0,y,width-32,"These use your action",SaloonTutorialLessons.Actions)+20;
                y+=TutorialList(0,y,width-32,"These are free",SaloonTutorialLessons.FreeActions)+20;
            }
            foreach(string paragraph in lesson.Details.Split(new[]{"\n\n"},StringSplitOptions.None))
                y+=MenuText(0,y,width-32,paragraph,21)+20;
            helpTextHeight=y;GUI.EndScrollView();
            MenuDivider(64,594,1140);
            if(MenuButton(new Rect(64,615,284,44),Game.TutorialWalkthrough?"Back to walkthrough":"Back"))Game.TutorialHelpOpen=false;
            MenuText(left,626,440,"Try opens a separate practice example.",17,muted:true);
            if(MenuButton(new Rect(940,615,264,44),"Try this topic",true,!Game.TutorialClosing))Game.BeginTutorialTopic(helpTopic);
        }
    }
}
