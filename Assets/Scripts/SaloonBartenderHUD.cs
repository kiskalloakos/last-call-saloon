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
            Text(area.x+12,area.y+7,area.width-24,21,"BARTENDER MOOD · "+mood+" / 100",new GUIStyle(subtitle){normal={textColor=color}});
            Fill(new Rect(area.x+12,area.y+34,area.width-24,7),new Color(.22f,.23f,.19f));
            Fill(new Rect(area.x+12,area.y+34,(area.width-24)*mood/100f,7),color);
            Text(area.x+12,area.y+48,area.width-24,36,SaloonBartenderMood.Label(mood)+" · "+mood+"% helpful\n"+view.bartenderBarShots+" Bar shots · keep my bar peaceful",new GUIStyle(small){fontSize=13,normal={textColor=new Color(.93f,.88f,.75f)}});
            worldHud=previous;
        }
        void BartenderMoodMeter()
        {
            if(Game.MenuOpen||Game.ShowRole||Game.View?.bartenderArrived!=true||Game.View.phase!=Phase.Turns||Game.Self?.alive!=true||Game.Self.room!=0||Game.View.bartenderMood<0)return;
            DrawBartenderMood(new Rect(990,77,254,89));
        }

        void BartenderArrivalNotice()
        {
            var view=Game.View;
            if(Game.MenuOpen||Game.ShowRole||view?.bartenderArrived!=true||view.bartenderArrivalRemaining<=0||view.phase==Phase.Finished)return;
            bool previous=worldHud;worldHud=false;
            Fill(new Rect(440,78,400,64),new Color(.055f,.075f,.065f,.97f));
            Fill(new Rect(440,78,400,2),new Color(.94f,.72f,.36f));
            Text(450,87,380,28,"Bartender arrived",new GUIStyle(title){fontSize=23,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(.98f,.84f,.57f)}});
            Text(450,118,380,20,"One question per player · Renegade excluded",new GUIStyle(small){alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(.92f,.87f,.76f)}});
            worldHud=previous;
        }

        void BartenderPanel()
        {
            if(!Game.BartenderMenuVisible)return;
            worldHud=false;var view=Game.View;
            Panel(new Rect(350,125,580,490),paper);
            Fill(new Rect(351,126,578,488),new Color(.91f,.85f,.72f,1));
            Text(378,145,524,36,"THE BARTENDER",new GUIStyle(title){fontSize=24});
            Text(378,188,524,60,"One question per player per match. Uses your round action.\nHe may answer Yes, No or Maybe. Maybe still spends your question.\nEvery Bar shot lowers his mood by 10. A quiet Bar keeps him helpful.",small);
            DrawBartenderMood(new Rect(378,258,524,87));
            if(view.bartenderUsed)
            {
                Text(378,367,524,52,view.bartenderQuestion,subtitle);
                Text(378,420,524,65,view.bartenderAnswer,new GUIStyle(title){fontSize=36});
                Text(378,510,524,48,view.bartenderAnswer=="Maybe"?"He won't give you anything useful. Your question is spent.\nHold Tab to read this answer later.":"Your question is spent. Hold Tab to read this answer later.",small);
            }
            else
            {
                int row=0;
                foreach(var target in view.players.Where(p=>p.id!=view.ownId&&p.alive))
                {
                    GUI.enabled=view.actionAvailable;
                    if(Button(378,355+row++*43,524,35,"Is "+target.name+" an Outlaw?"))Game.AskBartender(target.id);
                }
                GUI.enabled=true;
                if(!view.actionAvailable)Text(378,531,524,35,view.actionBlockReason,small);
            }
            if(Button(378,571,524,34,"CLOSE  /  I"))Game.CloseBartender();
        }
    }
}
