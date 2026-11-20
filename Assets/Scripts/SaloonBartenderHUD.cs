using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        void DrawBartenderMood(Rect area)
        {
            var view=Game.View;int mood=Mathf.Clamp(view.bartenderMood,0,100);
            bool previous=worldHud;worldHud=false;
            Fill(area,new Color(.055f,.075f,.065f,.96f));
            var color=Color.Lerp(new Color(.94f,.32f,.20f),new Color(.81f,.72f,.38f),mood/100f);
            const float inset=14;
            float left=area.x+inset,width=area.width-2*inset,top=area.y+10;
            top+=WrappedText(left,top,width,$"Bartender mood  {mood} / 100",new GUIStyle(subtitle){fontSize=15,normal={textColor=color}})+5;
            Fill(new Rect(left,top,width,7),new Color(.22f,.23f,.19f));
            Fill(new Rect(left,top,width*mood/100f,7),color);
            WrappedText(left,top+14,width,SaloonBartenderMood.Label(mood)+"\n"+mood+"% chance of a clear answer",
                new GUIStyle(small){fontSize=15,normal={textColor=new Color(.93f,.88f,.75f)}});
            worldHud=previous;
        }
        void BartenderMoodMeter()
        {
            if(Game.MenuOpen||Game.ShowRole||Game.BartenderMenuVisible||Game.View?.bartenderArrived!=true||Game.View.phase!=Phase.Turns||Game.Self?.alive!=true||Game.Self.room!=0||Game.View.bartenderMood<0)return;
            DrawBartenderMood(new Rect(990,77,254,100));
        }

        void BartenderArrivalNotice()
        {
            var view=Game.View;
            if(Game.MenuOpen||Game.ShowRole||Game.RoomPlannerOpen||Game.BartenderMenuVisible||view?.bartenderArrived!=true||
                view.bartenderArrivalRemaining<=0||view.phase==Phase.Choosing||view.phase==Phase.Finished)return;
            bool previous=worldHud;worldHud=false;
            const string heading="Bartender arrived";
            string detail=view.ownRole==nameof(Role.Renegade)?"You already know every role.":"You can ask one question this match.";
            var headingStyle=new GUIStyle(title){fontSize=23,wordWrap=true,alignment=TextAnchor.UpperCenter,normal={textColor=new Color(.98f,.84f,.57f)}};
            var detailStyle=new GUIStyle(small){fontSize=16,alignment=TextAnchor.UpperCenter,normal={textColor=new Color(.92f,.87f,.76f)}};
            float headingHeight=WrappedHeight(heading,368,headingStyle);
            float detailHeight=WrappedHeight(detail,368,detailStyle);
            Fill(new Rect(440,78,400,29+headingHeight+detailHeight),new Color(.055f,.075f,.065f,.97f));
            Fill(new Rect(440,78,400,2),new Color(.94f,.72f,.36f));
            WrappedText(456,90,368,heading,headingStyle);
            WrappedText(456,95+headingHeight,368,detail,detailStyle);
            worldHud=previous;
        }

        void BartenderPanel()
        {
            if(!Game.BartenderMenuVisible)return;
            worldHud=false;
            var view=Game.View;
            var targets=view.players.Where(p=>p.id!=view.ownId&&p.alive).ToArray();
            const string heading="Ask the bartender";
            const string instructions="You can ask one question per match. It uses your action.\nThe answer can be Yes, No, or Maybe.\nEach shot at Bar lowers his mood by 10.";
            const string reminder="Hold Tab to read this answer later.";
            const float inset=28,panelWidth=592,textWidth=panelWidth-2*inset;
            const float buttonHeight=36,rowGap=8,moodHeight=100;
            var headingStyle=new GUIStyle(title){fontSize=24,wordWrap=true};
            var questionStyle=new GUIStyle(body){fontSize=20,richText=false};
            var answerStyle=new GUIStyle(title){fontSize=36,wordWrap=true};
            float headingHeight=WrappedHeight(heading,textWidth,headingStyle);
            float instructionsHeight=WrappedHeight(instructions,textWidth,body);
            float contentHeight=view.bartenderUsed
                ?WrappedHeight(view.bartenderQuestion,textWidth,questionStyle)+WrappedHeight(view.bartenderAnswer,textWidth,answerStyle)+WrappedHeight(reminder,textWidth,body)+16
                :targets.Length*(buttonHeight+rowGap)+(view.actionAvailable?0:WrappedHeight(view.actionBlockReason,textWidth,body));
            float height=2*inset+headingHeight+instructionsHeight+moodHeight+contentHeight+buttonHeight+48;
            var panel=new Rect((1280-panelWidth)/2,(720-height)/2,panelWidth,height);
            Panel(panel,paper);
            Fill(new Rect(panel.x+1,panel.y+1,panel.width-2,panel.height-2),new Color(.91f,.85f,.72f,1));
            float x=panel.x+inset,y=panel.y+inset;
            y+=WrappedText(x,y,textWidth,heading,headingStyle)+8;
            y+=WrappedText(x,y,textWidth,instructions,body)+12;
            DrawBartenderMood(new Rect(x,y,textWidth,moodHeight));
            y+=moodHeight+16;
            if(view.bartenderUsed)
            {
                y+=WrappedText(x,y,textWidth,view.bartenderQuestion,questionStyle)+6;
                y+=WrappedText(x,y,textWidth,view.bartenderAnswer,answerStyle)+10;
                WrappedText(x,y,textWidth,reminder,body);
            }
            else
            {
                bool enabled=GUI.enabled;
                GUI.enabled=enabled&&view.actionAvailable;
                foreach(var target in targets)
                {
                    if(Button(x,y,textWidth,buttonHeight,"Is "+target.name+" an Outlaw?"))Game.AskBartender(target.id);
                    y+=buttonHeight+rowGap;
                }
                GUI.enabled=enabled;
                if(!view.actionAvailable)WrappedText(x,y,textWidth,view.actionBlockReason,body);
            }
            if(Button(x,panel.yMax-inset-buttonHeight,textWidth,buttonHeight,"Close (I)"))Game.CloseBartender();
        }
    }
}
