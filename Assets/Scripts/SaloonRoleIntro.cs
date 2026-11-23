using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public const float PersonalRoleIntroDuration=5.5f;
        public const float MatchupIntroDuration=9;
        public const float RoleIntroDuration=PersonalRoleIntroDuration+MatchupIntroDuration;
        float roleIntroStarted=float.NegativeInfinity;
        public Role IntroRole { get; private set; }
        public Role[] IntroMatchupRoles { get; private set; }=Array.Empty<Role>();
        public float RoleIntroElapsed=>Time.unscaledTime-roleIntroStarted;
        public bool RoleIntroActive=>InRoom&&Self?.alive==true&&View?.phase!=Phase.Lobby&&View?.phase!=Phase.Finished&&RoleIntroElapsed<RoleIntroDuration;

        void UpdateRoleIntro(Snapshot next)
        {
            if(next.phase==Phase.Lobby||next.phase==Phase.Finished)
            {roleIntroStarted=float.NegativeInfinity;IntroRole=Role.Unknown;IntroMatchupRoles=Array.Empty<Role>();return;}
            // Snapshot refreshes, later rounds and reconnects must not replay the opening.
            bool opening=next.phase==Phase.Discussion&&next.round==1&&
                (View==null||View.phase==Phase.Lobby||View.phase==Phase.Finished);
            if(!opening||!Enum.TryParse(next.ownRole,out Role role)||role==Role.Unknown||
                !SaloonRules.SupportedMatchSize(next.players?.Length??0))return;
            // Build anonymous role slots from the public deck, never from player IDs,
            // names, avatar positions, or recipient-specific revealed roles.
            IntroMatchupRoles=SaloonRules.MatchupFor(next.players.Length,role);
            IntroRole=role;roleIntroStarted=Time.unscaledTime;ShowRole=false;
        }
    }

    public partial class SaloonHUD
    {
        void RoleIntro()
        {
            worldHud=false;
            float t=Game.RoleIntroElapsed;
            if(t>=SaloonGame.PersonalRoleIntroDuration)
            {MatchupIntro();return;}
            float exit=Mathf.SmoothStep(0,1,Mathf.InverseLerp(SaloonGame.PersonalRoleIntroDuration-.8f,SaloonGame.PersonalRoleIntroDuration,t));
            // The original reveal fades into the anonymous matchup instead of the world.
            if(exit>0)MatchupIntro();
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
                Game.IntroRole==Role.Swindler?"Make the Law kill you. Win through suspicion.":
                "Trust your instincts. Be the last one standing.";
            IntroText(250,410,780,58,purpose,23,new Color(.78f,.75f,.68f,detail),false);
            IntroText(340,620,600,25,"HOLD TAB ANYTIME TO REVIEW YOUR ROLE",12,new Color(.52f,.49f,.43f,detail),false);
        }

        void MatchupIntro()
        {
            var roles=Game.IntroMatchupRoles;
            if(roles.Length==0)return;
            float exit=Mathf.SmoothStep(0,1,Mathf.InverseLerp(SaloonGame.RoleIntroDuration-.8f,SaloonGame.RoleIntroDuration,Game.RoleIntroElapsed));
            var previousColor=GUI.color;
            GUI.color=new Color(previousColor.r,previousColor.g,previousColor.b,previousColor.a*(1-exit));
            var matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;
            MatchupFill(new Rect(0,0,Screen.width,Screen.height),Color.black);
            GUI.matrix=matrix;
            var panel=new Rect(76,54,1128,612);
            MatchupFill(panel,StoryNight);MatchupOutline(panel,StoryQuiet);
            MatchupFill(new Rect(panel.x,panel.y,panel.width,3),StoryGold);
            ResultText(new Rect(105,78,1070,48),"YOUR MATCHUP",36,StoryCream,true);
            int allies=roles.Skip(1).Count(r=>SaloonRules.Allies(Game.IntroRole,r));
            int enemies=roles.Length-1-allies;
            int hidden=roles.Skip(1).Count(r=>!MatchupIdentityKnown(r));
            string allySummary=allies==0?"No allies":allies==1?"1 ally":allies+" allies";
            string opposition=Game.IntroRole==Role.Swindler?enemies+" other players":enemies+(enemies==1?" enemy":" enemies");
            ResultText(new Rect(105,132,1070,30),allySummary+"  /  "+opposition,22,StoryCream);
            ResultText(new Rect(105,166,1070,28),hidden==0?"You already know every role. Decide whom to trust with that knowledge.":
                "Discover "+hidden+(hidden==1?" hidden identity.":" hidden identities.")+" Your role knowledge is available with Tab.",17,StoryGrey);

            if(resultPortraits==null)resultPortraits=Resources.Load<Texture2D>("UI/ResultPortraits");
            if(matchupGreyPortraits==null&&resultPortraits!=null)BuildGreyMatchupPortraits();
            bool compact=roles.Length>5;
            const float gap=16;
            float cardWidth=compact?244:198,cardHeight=compact?164:324;
            int columns=compact?4:roles.Length;
            for(int i=0;i<roles.Length;i++)
            {
                int row=i/columns,column=i%columns,inRow=Mathf.Min(columns,roles.Length-row*columns);
                float left=(1280-(inRow*cardWidth+Mathf.Max(0,inRow-1)*gap))*.5f;
                MatchupRoleCard(new Rect(left+column*(cardWidth+gap),209+row*(cardHeight+gap),cardWidth,cardHeight),roles[i],i==0,compact);
            }
            ResultText(new Rect(115,570,1050,28),"Anonymous role portraits — their order does not identify any player.",17,StoryGrey);
            ResultText(new Rect(115,606,1050,28),"YOU: "+Game.IntroRole.ToString().ToUpperInvariant()+"  /  DISCUSSION BEGINS SHORTLY",15,StoryGold);
            GUI.color=previousColor;
        }

        Texture2D matchupGreyPortraits;

        void BuildGreyMatchupPortraits()
        {
            // The imported atlas is not CPU-readable. Copy it once for the muted cards.
            var previous=RenderTexture.active;
            var copy=RenderTexture.GetTemporary(resultPortraits.width,resultPortraits.height,0);
            try
            {
                Graphics.Blit(resultPortraits,copy);RenderTexture.active=copy;
                matchupGreyPortraits=new Texture2D(copy.width,copy.height,TextureFormat.RGBA32,false)
                    {hideFlags=HideFlags.HideAndDontSave};
                matchupGreyPortraits.ReadPixels(new Rect(0,0,copy.width,copy.height),0,0);
                var pixels=matchupGreyPortraits.GetPixels32();
                for(int i=0;i<pixels.Length;i++)
                {
                    var color=pixels[i];byte grey=(byte)((color.r*299+color.g*587+color.b*114)/1000);
                    pixels[i]=new Color32(grey,grey,grey,color.a);
                }
                matchupGreyPortraits.SetPixels32(pixels);matchupGreyPortraits.Apply(false,true);
                uiTextures.Add(matchupGreyPortraits);
            }
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(copy);}
        }

        bool MatchupIdentityKnown(Role role)=>role==Role.Sheriff||SaloonRules.KnowsRole(Game.IntroRole,role);

        void MatchupFill(Rect area,Color color)
        {color.a*=GUI.color.a;Fill(area,color);}

        void MatchupOutline(Rect area,Color color)
        {color.a*=GUI.color.a;Outline(area,color);}

        void MatchupRoleCard(Rect card,Role role,bool self,bool compact)
        {
            bool ally=!self&&SaloonRules.Allies(Game.IntroRole,role);
            Color accent=self?StoryGold:new Color(.37f,.39f,.38f);
            Color text=self?StoryCream:new Color(.64f,.66f,.64f);
            MatchupFill(card,self?new Color(.16f,.15f,.11f):new Color(.085f,.095f,.09f));MatchupOutline(card,accent);
            MatchupFill(new Rect(card.x,card.y,card.width,3),accent);
            ResultText(new Rect(card.x+8,card.y+8,card.width-16,28),role==Role.Unknown?"Independent":role.ToString(),21,text,true);
            float portraitSize=compact?96:182;
            var portrait=new Rect(card.x+8,card.y+44,portraitSize,portraitSize);
            if(resultPortraits!=null)
                GUI.DrawTextureWithTexCoords(portrait,self?resultPortraits:matchupGreyPortraits,new Rect((role==Role.Unknown?3:Mathf.Clamp((int)role-1,0,3))*.25f,.5f,.25f,.5f));
            if(!self)MatchupFill(portrait,new Color(0,0,0,.24f));
            string relationship=self?"YOU":ally?"YOUR ALLY":Game.IntroRole==Role.Swindler?"OTHER PLAYER":"YOUR ENEMY";
            string identity=self?"Your role":role==Role.Unknown?"Renegade or Swindler":MatchupIdentityKnown(role)?role==Role.Sheriff?"Public identity":"Identity known":"Find this player";
            var relation=compact?new Rect(card.x+110,card.y+50,card.width-118,34):new Rect(card.x+8,card.y+238,card.width-16,30);
            var knowledge=compact?new Rect(card.x+110,card.y+90,card.width-118,54):new Rect(card.x+8,card.y+276,card.width-16,34);
            ResultText(relation,relationship,17,self?StoryGold:text,true);
            ResultText(knowledge,identity,16,text);
        }

        void IntroText(float x,float y,float width,float height,string value,int size,Color color,bool heading)
        {
            Text(x,y,width,height,value,new GUIStyle(heading?title:body){fontSize=size,
                alignment=TextAnchor.MiddleCenter,wordWrap=true,richText=false,normal={textColor=color}});
        }
    }
}
