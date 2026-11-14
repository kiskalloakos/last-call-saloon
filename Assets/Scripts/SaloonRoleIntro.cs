using System;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public const float RoleIntroDuration=5.5f;
        float roleIntroStarted=float.NegativeInfinity;
        public Role IntroRole { get; private set; }
        public float RoleIntroElapsed=>Time.unscaledTime-roleIntroStarted;
        public bool RoleIntroActive=>InRoom&&Self?.alive==true&&View?.phase!=Phase.Lobby&&View?.phase!=Phase.Finished&&RoleIntroElapsed<RoleIntroDuration;

        void UpdateRoleIntro(Snapshot next)
        {
            if(next.phase==Phase.Lobby||next.phase==Phase.Finished)
            {roleIntroStarted=float.NegativeInfinity;IntroRole=Role.Unknown;return;}
            // Snapshot refreshes, later rounds and reconnects must not replay the opening.
            bool opening=next.phase==Phase.Discussion&&next.round==1&&
                (View==null||View.phase==Phase.Lobby||View.phase==Phase.Finished);
            if(!opening||!Enum.TryParse(next.ownRole,out Role role)||role==Role.Unknown)return;
            IntroRole=role;roleIntroStarted=Time.unscaledTime;ShowRole=false;
        }
    }

    public partial class SaloonHUD
    {
        void RoleIntro()
        {
            worldHud=false;
            float t=Game.RoleIntroElapsed;
            float exit=Mathf.SmoothStep(0,1,Mathf.InverseLerp(SaloonGame.RoleIntroDuration-.8f,SaloonGame.RoleIntroDuration,t));
            // Cover the actual viewport, including the margins on ultrawide displays.
            var matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;
            Fill(new Rect(0,0,Screen.width,Screen.height),new Color(0,0,0,1-exit));
            GUI.matrix=matrix;
            float reveal=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1.5f,t))*(1-exit);
            float detail=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.2f,2.1f,t))*(1-exit);
            var amber=new Color(.90f,.67f,.36f,reveal);
            var cream=new Color(.97f,.92f,.82f,reveal);
            float drift=Mathf.Lerp(9,0,Mathf.SmoothStep(0,1,t/4.7f));
            IntroText(220,212+drift,840,30,"Y O U   A R E   T H E",16,amber,false);
            IntroText(100,260+drift,1080,100,Game.IntroRole.ToString().ToUpperInvariant(),68,cream,true);
            float lineWidth=112*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,2,t));
            Fill(new Rect(640-lineWidth/2,386,lineWidth,1),new Color(amber.r,amber.g,amber.b,detail));
            string purpose=Game.IntroRole==Role.Sheriff?"Wear the badge. Protect the money. Stop the escape.":
                Game.IntroRole==Role.Deputy?"Stand with the Sheriff. Keep the money safe.":
                Game.IntroRole==Role.Outlaw?"Take the money. Make it out alive.":
                "You know every secret. Be the last one standing.";
            IntroText(250,410,780,58,purpose,23,new Color(.78f,.75f,.68f,detail),false);
            IntroText(340,620,600,25,"HOLD TAB ANYTIME TO REVIEW YOUR ROLE",12,new Color(.52f,.49f,.43f,detail),false);
        }

        void IntroText(float x,float y,float width,float height,string value,int size,Color color,bool heading)
        {
            Text(x,y,width,height,value,new GUIStyle(heading?title:body){fontSize=size,
                alignment=TextAnchor.MiddleCenter,wordWrap=true,richText=false,normal={textColor=color}});
        }
    }
}
