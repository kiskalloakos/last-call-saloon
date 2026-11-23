using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        Vector2 tutorialArticleScroll,tutorialChapterScroll;
        int tutorialShownLesson=-1;
        float tutorialArticleHeight;

        void TutorialRule(float x,float y,float width,int size)
        {
            var style=MenuStyle(size,MenuAccent,heading:true);style.fontStyle=FontStyle.Bold;
            WrappedText(x,y,width,SaloonTutorialLessons.ActionRule,style);
        }

        void TutorialWelcomeScreen()
        {
            MenuBackdrop(true,true);MenuFrame(new Rect(270,145,740,430));
            MenuText(308,183,664,"LEARN BY PLAYING",38,heading:true);
            MenuText(308,246,664,"A new training saloon. One continuous route.",25);
            MenuText(308,294,664,"Shoot, heal, play the tables, and try every role.\nFollow short prompts as you move. Designed for under five minutes.",21,muted:true);
            if(MenuButton(new Rect(308,382,664,54),"Start walkthrough",true))Game.BeginTutorial();
            if(MenuButton(new Rect(308,450,324,44),"Help topics"))Game.TutorialHelpOpen=true;
            if(MenuButton(new Rect(648,450,324,44),"Skip tutorial"))Game.SkipTutorialWelcome();
            MenuText(308,521,664,"Enter: start. Esc: skip. Replay from the main menu any time.",17,muted:true);
        }

        void TutorialLessonScreen()
        {
            if(tutorialShownLesson!=Game.TutorialIndex)
            {tutorialShownLesson=Game.TutorialIndex;tutorialArticleScroll=Vector2.zero;tutorialArticleHeight=0;}
            MenuBackdrop(false,true);MenuFrame(new Rect(36,30,1208,660));
            Fill(new Rect(37,32,282,657),MenuSurface);MenuDivider(319,31,1);
            MenuText(58,53,240,"HELP TOPICS",26,heading:true);
            MenuText(58,98,240,"Find a topic. Try it.",18,muted:true);
            tutorialChapterScroll=GUI.BeginScrollView(new Rect(54,133,250,453),tutorialChapterScroll,
                new Rect(0,0,230,SaloonTutorialLessons.All.Length*45));
            for(int i=0;i<SaloonTutorialLessons.All.Length;i++)
            {
                string label=(Game.TutorialCompleted[i]?"✓  ":"")+SaloonTutorialLessons.All[i].Title;
                if(MenuButton(new Rect(0,i*45,230,40),label,Game.TutorialIndex==i,!Game.TutorialClosing))
                    Game.SelectTutorialLesson(i);
            }
            GUI.EndScrollView();
            if(MenuButton(new Rect(58,614,240,46),Game.TutorialClosing?"Leaving…":"Exit tutorial",enabled:!Game.TutorialClosing))Game.EndTutorial();

            const float x=350,width=858;
            var lesson=Game.TutorialLesson;
            MenuText(x,53,width,lesson.Group.ToUpperInvariant(),16,muted:true);
            MenuText(x,82,width,lesson.Title,32,heading:true);
            string objective=Game.TutorialTaskDone?"STEP COMPLETE · "+lesson.Objective:lesson.Objective;
            float objectiveHeight=MenuText(x,133,width,objective,20);
            float articleY=Mathf.Max(204,145+objectiveHeight);
            MenuDivider(x,articleY-16,width);
            var viewport=new Rect(x,articleY,width,573-articleY);
            tutorialArticleScroll=GUI.BeginScrollView(viewport,tutorialArticleScroll,new Rect(0,0,width-20,tutorialArticleHeight));
            float y=0,textWidth=width-32;
            if(lesson.Task==TutorialTask.Action)
            {
                TutorialRule(0,y,textWidth,37);y+=64;
                y+=MenuText(0,y,textWidth,SaloonTutorialLessons.ActionMeaning,22)+24;
                y+=TutorialList(0,y,textWidth,"These use your action",SaloonTutorialLessons.Actions)+22;
                y+=TutorialList(0,y,textWidth,"These do not use your action",SaloonTutorialLessons.FreeActions)+22;
            }
            foreach(string paragraph in lesson.Details.Split(new[]{"\n\n"},System.StringSplitOptions.None))
                y+=MenuText(0,y,textWidth,paragraph,21)+20;
            tutorialArticleHeight=y;
            GUI.EndScrollView();
            MenuDivider(x,591,width);
            bool final=lesson.Task==TutorialTask.Finish;
            if(MenuButton(new Rect(x,614,132,46),"← Back",enabled:Game.TutorialIndex>0&&!Game.TutorialClosing))
                Game.SelectTutorialLesson(Game.TutorialIndex-1);
            if(MenuButton(new Rect(x+146,614,180,46),Game.TutorialTaskDone?"Retry exercise":"Try this",!Game.TutorialTaskDone,
                !final&&!Game.TutorialClosing))
            {
                if(Game.TutorialTaskDone)Game.SelectTutorialLesson(Game.TutorialIndex);
                Game.TryTutorialLesson();
            }
            string next=final?"Finish tutorial":Game.TutorialTaskDone?"Next topic →":"Next topic →";
            if(MenuButton(new Rect(x+width-252,614,252,46),next,Game.TutorialTaskDone,!Game.TutorialClosing))Game.NextTutorialLesson();
        }

        float TutorialList(float x,float y,float width,string heading,string[] items)
        {
            float start=y;y+=MenuText(x,y,width,heading,25,heading:true)+14;
            foreach(string item in items)y+=MenuText(x+10,y,width-10,"• "+item,21)+10;
            return y-start;
        }

        void TutorialOverlay()
        {
            // Keep the full planner and physical minigame controls unobstructed.
            if(Game.RoomPlannerOpen)
            {
                Fill(new Rect(108,688,1064,30),MenuBase);
                MenuText(120,691,1040,"TUTORIAL · Select Bar. Press F to lock. Esc / F1: instructions and chapters.",17);
                return;
            }
            const float x=30,y=102,width=330;
            var objectiveStyle=MenuStyle(19,MenuInk);
            string objective=Game.TutorialTaskDone?"STEP COMPLETE. Esc opens the instructions. Continue with Next lesson.":Game.TutorialLesson.Objective;
            string progress=Game.TutorialProgress;
            if(Game.TutorialGoal.HasValue&&Game.Self!=null)
            {
                var self=Game.Self;
                float distance=Vector3.ProjectOnPlane(new Vector3(self.x,self.y,self.z)-Game.TutorialGoal.Value,Vector3.up).magnitude;
                progress=Game.TutorialGoalLabel+" · "+distance.ToString("0.0")+" m"+(string.IsNullOrEmpty(progress)?"":"\n"+progress);
            }
            float textHeight=WrappedHeight(objective,width-28,objectiveStyle);
            float progressHeight=WrappedHeight(progress,width-28,MenuStyle(17,MenuAccent));
            float height=91+textHeight+progressHeight;
            Fill(new Rect(x,y,width,height),new Color(MenuBase.r,MenuBase.g,MenuBase.b,.94f));
            Outline(new Rect(x,y,width,height),MenuStroke);
            MenuText(x+14,y+12,width-28,"TUTORIAL · "+(Game.TutorialIndex+1)+" / "+SaloonTutorialLessons.All.Length,15,muted:true);
            WrappedText(x+14,y+40,width-28,objective,objectiveStyle);
            WrappedText(x+14,y+48+textHeight,width-28,progress,MenuStyle(17,MenuAccent));
            MenuText(x+14,y+height-28,width-28,"Esc / F1 · Instructions and chapters",16,muted:true);
            if(Game.TutorialLesson.Task==TutorialTask.Action)TutorialRule(410,115,780,33);
            TutorialDestinationMarker();
        }

        void TutorialDestinationMarker()
        {
            if(!Game.TutorialGoal.HasValue)return;
            var screen=Game.ViewCamera.WorldToScreenPoint(Game.TutorialGoal.Value);
            if(screen.z<=0)return;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            float x=(screen.x-(Screen.width-1280*scale)/2)/scale;
            float y=(Screen.height-screen.y-(Screen.height-720*scale)/2)/scale;
            if(x<18||x>1262||y<85||y>650)return;
            Outline(new Rect(x-10,y-10,20,20),MenuAccent);
            Fill(new Rect(x-3,y-3,6,6),MenuAccent);
        }
    }
}
