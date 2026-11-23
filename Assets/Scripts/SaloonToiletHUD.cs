using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        void ToiletDutyOffer()
        {
            worldHud=false;
            Fill(new Rect(0,0,1280,720),new Color(0,0,0,.55f));
            Panel(new Rect(320,205,640,300),paper);
            Text(350,229,580,40,"TOILET TROUBLE",new GUIStyle(title){fontSize=30,alignment=TextAnchor.MiddleCenter});
            Text(360,283,560,40,"Congratulations. You're on toilet duty.",new GUIStyle(body){alignment=TextAnchor.MiddleCenter,fontSize=20});
            Text(365,335,550,68,"Accept a safe solo bathroom mission. Eight pumps in 12 seconds for a 20% heal, then return to Bar. Keep your combat action.",new GUIStyle(body){alignment=TextAnchor.MiddleCenter,wordWrap=true});
            if(Button(375,430,260,44,"ACCEPT · +20% HEAL"))Game.ChooseToiletDuty(true);
            if(Button(655,430,250,44,"DENY"))Game.ChooseToiletDuty(false);
        }
    }
}
