using System;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        static readonly Color StoryGold=new Color(.98f,.73f,.35f),StoryCream=new Color(.94f,.90f,.80f),
            StoryGrey=new Color(.57f,.60f,.59f),StoryNight=new Color(.055f,.075f,.073f),StoryQuiet=new Color(.40f,.46f,.44f);
        Texture2D storyAtlas,storyGreyAtlas,storyDisc;
        SaloonStoryMap storyMap;
        Snapshot storySnapshot;

        void EnsureStoryArt()
        {
            if(storyDisc!=null)return;
            storyAtlas=Resources.Load<Texture2D>("UI/StoryMilestones");
            if(storyAtlas!=null)
            {
                // Desaturate once, rather than tinting the completed art brown.
                var pixels=storyAtlas.GetPixels32();
                for(int i=0;i<pixels.Length;i++)
                {
                    var p=pixels[i];byte grey=(byte)((p.r*30+p.g*59+p.b*11)/100);
                    pixels[i]=new Color32(grey,grey,grey,p.a);
                }
                storyGreyAtlas=new Texture2D(storyAtlas.width,storyAtlas.height,TextureFormat.RGBA32,false)
                    {hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                storyGreyAtlas.SetPixels32(pixels);storyGreyAtlas.Apply(false,true);uiTextures.Add(storyGreyAtlas);
            }
            storyDisc=new Texture2D(128,128,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear};
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
                storyDisc.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(63.5f-Vector2.Distance(new Vector2(x,y),new Vector2(63.5f,63.5f)))));
            storyDisc.Apply(false,true);uiTextures.Add(storyDisc);
        }

        void StoryText(float x,float y,float w,float h,string text,int size,Color color,bool center=false,bool heading=false)
            =>Text(x,y,w,h,text,new GUIStyle(heading?title:body){fontSize=size,richText=false,wordWrap=true,
                alignment=center?TextAnchor.MiddleCenter:TextAnchor.UpperLeft,normal={textColor=color}});

        void StoryLine(Vector2 a,Vector2 b,Color color,float thickness=2)
        {
            if(Event.current.type!=EventType.Repaint)return;
            var matrix=GUI.matrix;
            GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(a.x,a.y,0),
                Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);
            Fill(new Rect(0,-thickness/2,Vector2.Distance(a,b),thickness),color);GUI.matrix=matrix;
        }

        void StoryTrail(StoryMilestone from,StoryMilestone to)
        {
            var start=new Vector2(from.X,from.Y);var end=new Vector2(to.X,to.Y);
            var a=start+Vector2.right*(end.x-start.x)*.48f;var b=end-Vector2.right*(end.x-start.x)*.48f;
            Color color=to.State==StoryState.Complete?new Color(.42f,.47f,.45f):
                to.State==StoryState.Next?new Color(.70f,.51f,.26f):new Color(.23f,.29f,.27f);
            var previous=start;
            for(int i=1;i<=36;i++)
            {
                float t=i/36f,u=1-t;var point=u*u*u*start+3*u*u*t*a+3*u*t*t*b+t*t*t*end;
                if(to.State!=StoryState.Later||i%3!=0)StoryLine(previous,point,color,to.State==StoryState.Next?2.5f:1.5f);
                previous=point;
            }
        }

        void StoryNode(StoryMilestone node)
        {
            bool done=node.State==StoryState.Complete,next=node.State==StoryState.Next;
            Color color=done?StoryGrey:next?StoryGold:StoryQuiet;
            float x=node.X,y=node.Y;
            if(next)
            {
                float pulse=.13f+.05f*Mathf.Sin(Time.unscaledTime*2.5f);
                Graphic(new Rect(x-52,y-52,104,104),storyDisc,new Color(StoryGold.r,StoryGold.g,StoryGold.b,pulse));
                Fill(new Rect(x-32,y-70,64,20),StoryGold);
                StoryText(x-32,y-70,64,20,"NEXT",12,StoryNight,true);
            }
            Graphic(new Rect(x-46,y-46,92,92),storyDisc,color);
            Graphic(new Rect(x-43,y-43,86,86),storyDisc,StoryNight);
            var atlas=done?storyGreyAtlas:storyAtlas;
            if(atlas!=null)
            {
                var previous=GUI.color;GUI.color=done?new Color(.65f,.68f,.67f):next?Color.white:new Color(.46f,.49f,.46f);
                GUI.DrawTextureWithTexCoords(new Rect(x-41,y-41,82,82),atlas,new Rect((node.Art%4)*.25f,node.Art<4?.5f:0,.25f,.5f));
                GUI.color=previous;
            }
            if(done)
            {
                Graphic(new Rect(x+23,y+23,24,24),storyDisc,new Color(.65f,.71f,.68f));
                StoryLine(new Vector2(x+29,y+35),new Vector2(x+33,y+39),StoryNight,2.5f);
                StoryLine(new Vector2(x+33,y+39),new Vector2(x+41,y+30),StoryNight,2.5f);
            }
            Fill(new Rect(x-81,y+48,162,88),StoryNight);
            StoryText(x-81,y+48,162,24,node.Title,17,done?StoryGrey:StoryCream,true);
            StoryText(x-81,y+73,162,17,done?"COMPLETED":next?"YOUR NEXT MOVE":node.Optional?"OPTIONAL":"AHEAD",11,color,true);
            StoryText(x-81,y+92,162,42,node.Detail.Replace("Optional · ",""),13,
                done?StoryGrey:next?StoryCream:new Color(.75f,.79f,.73f),true);
        }

        void BartenderIntel(float y)
        {
            var view=Game.View;
            StoryText(959,y,220,24,"BARTENDER · PRIVATE",12,StoryGold);
            StoryText(959,y+27,220,79,!view.bartenderArrived?$"Not here yet. Arrives in round {SaloonRules.BartenderArrivalRound}, after five complete rounds. Save your one question.":view.bartenderUsed?view.bartenderQuestion+"\n"+view.bartenderAnswer:
                "One question per match. I beside him at the Bar; uses your action. Gunfire lowers his mood. Maybe also spends it.",14,StoryCream);
        }

        void StoryIntel(Role role)
        {
            var view=Game.View;
            Fill(new Rect(937,151,1,353),new Color(.40f,.46f,.39f,.3f));
            StoryText(959,152,220,24,"PRIVATE INTEL",13,StoryGold);
            if(role==Role.Sheriff)
            {
                var deputy=Array.Find(view.knownRoles??Array.Empty<KnownRole>(),p=>p.role=="Deputy");
                StoryText(959,187,220,43,"Your Deputy: "+(deputy?.name??"Unknown"),17,StoryCream);
                var clues=view.evidence??Array.Empty<string>();
                BartenderIntel(236);
                StoryText(959,351,220,24,"SAFE REPORTS & CLUES",12,StoryGrey);
                if(clues.Length==0)StoryText(959,380,220,115,"No clues yet.\n\nF to stand; WASD to the safe at the counter's right end. Press C beside it on your turn. Reports appear here.",15,StoryCream);
                else
                {
                    evidenceScroll=GUI.BeginScrollView(new Rect(955,378,230,121),evidenceScroll,new Rect(0,0,206,clues.Length*118));
                    for(int i=0;i<clues.Length;i++)
                    {StoryText(4,i*118,200,108,clues[clues.Length-i-1],15,StoryCream);Fill(new Rect(4,i*118+112,200,1),StoryQuiet);}
                    GUI.EndScrollView();
                }
            }
            else if(role==Role.Renegade)
            {
                var roles=view.knownRoles??Array.Empty<KnownRole>();
                for(int i=0;i<roles.Length;i++)
                {
                    StoryText(959,187+i*54,220,25,roles[i].name,17,StoryCream);
                    StoryText(959,212+i*54,220,20,roles[i].role.ToUpperInvariant(),12,StoryGrey);
                }
            }
            else if(role==Role.Outlaw)
            {
                var ally=Array.Find(view.knownRoles??Array.Empty<KnownRole>(),p=>p.role=="Outlaw"&&p.id!=view.ownId);
                StoryText(959,187,220,27,ally==null?"Riding solo":"Your fellow Outlaw",17,StoryCream);
                StoryText(959,220,220,52,ally==null?"You are the only Outlaw in this match.":ally.name+"\nEither Outlaw can escape to win for both.",14,StoryCream);
                BartenderIntel(276);
                StoryText(959,394,220,25,"THE ESCAPE WINDOW",12,StoryGold);
                StoryText(959,420,220,85,$"Three safe jobs; X on a later turn.\nBar again next round.\nDeclare by round {SaloonPosse.LastEscapeRound}.\nPosse arrives after round {SaloonPosse.ArrivalRound}.",13,StoryCream);
            }
            else
            {
                var sheriff=Array.Find(view.players??Array.Empty<PlayerView>(),p=>p.sheriff);
                StoryText(959,187,220,53,"Your Sheriff: "+(sheriff?.name??"Watch the public Sheriff marker"),17,StoryCream);
                BartenderIntel(251);
                StoryText(959,373,220,130,"Ask the Sheriff about safe inspections.\nE pours beer; E stops. Aim + G to offer healing.\nYou can still win if the Sheriff falls.",14,StoryCream);
            }
        }

        void RoleStoryMap(Role role)
        {
            EnsureStoryArt();
            if(storySnapshot!=Game.View){storySnapshot=Game.View;storyMap=SaloonStoryMap.Build(Game.View);}
            var map=storyMap;
            Fill(new Rect(0,0,1280,720),new Color(.015f,.022f,.021f,.78f));
            Fill(new Rect(64,42,1152,638),StoryNight);
            Outline(new Rect(64,42,1152,638),new Color(.34f,.37f,.29f));
            Fill(new Rect(64,42,1152,3),StoryGold);
            StoryText(87,60,800,43,role.ToString().ToUpperInvariant()+" / YOUR STORY",28,StoryCream,false,true);
            StoryText(87,104,1090,20,map.Caption,14,StoryGrey);
            StoryText(950,66,240,24,"ROUND "+Game.View.round+" / "+SaloonPosse.ArrivalRound,14,StoryGold,true);
            StoryText(950,91,240,24,map.Completed+" milestones reached",13,StoryGrey,true);
            // Faint contour trails give the graph a map-like field without adding a second bitmap.
            for(int row=0;row<5;row++)
                for(int col=0;col<12;col++)
                    Graphic(new Rect(112+col*69,171+row*70,2,2),storyDisc,new Color(.31f,.39f,.34f,.23f));
            foreach(var node in map.Nodes)foreach(int source in node.From)StoryTrail(map.Nodes[source],node);
            foreach(var node in map.Nodes)StoryNode(node);
            StoryIntel(role);
            Fill(new Rect(87,555,1105,84),new Color(.10f,.14f,.12f));
            Fill(new Rect(87,555,3,84),StoryGold);
            StoryText(106,568,64,20,"NEXT",12,StoryGold);
            StoryText(170,564,997,28,map.NextTitle,21,StoryCream);
            StoryText(170,598,997,38,map.NextDetail,15,StoryCream);
            StoryText(87,650,1105,22,"GREY + CHECK  completed     GOLD  next move     DOTTED TRAIL  ahead     /     One action per round. Headshots only. Release Tab to close.",13,StoryGrey);
        }
    }
}
