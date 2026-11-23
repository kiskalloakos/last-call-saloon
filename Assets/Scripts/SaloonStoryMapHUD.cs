using System;
using System.Collections.Generic;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        static readonly Color StoryGold=new Color(.98f,.73f,.35f),StoryCream=new Color(.94f,.90f,.80f),
            StoryGrey=new Color(.69f,.73f,.71f),StoryNight=new Color(.055f,.075f,.073f),StoryQuiet=new Color(.48f,.54f,.52f);
        Texture2D storyAtlas,storyGreyAtlas,storyDisc;
        SaloonStoryMap storyMap;
        Snapshot storySnapshot;
        int storyIntelPage;

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

        GUIStyle StoryStyle(int size,Color color,TextAnchor alignment=TextAnchor.UpperLeft,bool heading=false)
            =>new GUIStyle(heading?title:body){fontSize=size,richText=false,wordWrap=true,alignment=alignment,normal={textColor=color}};

        void StoryLine(Vector2 a,Vector2 b,Color color,float thickness=2)
        {
            if(Event.current.type!=EventType.Repaint)return;
            var matrix=GUI.matrix;
            GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(a.x,a.y,0),
                Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);
            Fill(new Rect(0,-thickness/2,Vector2.Distance(a,b),thickness),color);GUI.matrix=matrix;
        }

        void StoryTrail(Vector2 start,Vector2 end,StoryState state)
        {
            var a=start+Vector2.right*(end.x-start.x)*.48f;var b=end-Vector2.right*(end.x-start.x)*.48f;
            Color color=state==StoryState.Complete?new Color(.42f,.47f,.45f):
                state==StoryState.Next?new Color(.70f,.51f,.26f):new Color(.30f,.35f,.33f);
            var previous=start;
            for(int i=1;i<=36;i++)
            {
                float t=i/36f,u=1-t;var point=u*u*u*start+3*u*u*t*a+3*u*t*t*b+t*t*t*end;
                if(state!=StoryState.Later||i%3!=0)StoryLine(previous,point,color,state==StoryState.Next?2.5f:1.5f);
                previous=point;
            }
        }

        const float StoryLabelWidth=148;
        const float StoryLabelOffset=43;

        string StoryNodeStatus(StoryMilestone node)
            =>node.State==StoryState.Complete?"Done":node.State==StoryState.Next?"Next":node.Optional?"Optional":"";

        void StoryNode(StoryMilestone node,Vector2 position,GUIStyle labelStyle,GUIStyle statusStyle)
        {
            bool done=node.State==StoryState.Complete,next=node.State==StoryState.Next;
            Color color=done?StoryGrey:next?StoryGold:StoryQuiet;
            float x=position.x,y=position.y;
            if(next)
            {
                Graphic(new Rect(x-42,y-42,84,84),storyDisc,new Color(StoryGold.r,StoryGold.g,StoryGold.b,.15f));
            }
            Graphic(new Rect(x-38,y-38,76,76),storyDisc,color);
            Graphic(new Rect(x-36,y-36,72,72),storyDisc,StoryNight);
            var atlas=done?storyGreyAtlas:storyAtlas;
            if(atlas!=null)
            {
                var previous=GUI.color;GUI.color=done?new Color(.65f,.68f,.67f):next?Color.white:new Color(.46f,.49f,.46f);
                GUI.DrawTextureWithTexCoords(new Rect(x-34,y-34,68,68),atlas,new Rect((node.Art%4)*.25f,node.Art<4?.5f:0,.25f,.5f));
                GUI.color=previous;
            }
            if(done)
            {
                Graphic(new Rect(x+19,y+19,22,22),storyDisc,new Color(.65f,.71f,.68f));
                StoryLine(new Vector2(x+24,y+30),new Vector2(x+28,y+34),StoryNight,2.5f);
                StoryLine(new Vector2(x+28,y+34),new Vector2(x+36,y+26),StoryNight,2.5f);
            }
            string status=StoryNodeStatus(node);
            float left=x-StoryLabelWidth/2,top=y+StoryLabelOffset;
            float height=WrappedHeight(node.Title,StoryLabelWidth,labelStyle)+WrappedHeight(status,StoryLabelWidth,statusStyle);
            Fill(new Rect(left,top,StoryLabelWidth,height),StoryNight);
            labelStyle.normal.textColor=done?StoryGrey:StoryCream;
            statusStyle.normal.textColor=next?StoryGold:StoryGrey;
            top+=WrappedText(left,top,StoryLabelWidth,node.Title,labelStyle);
            WrappedText(left,top,StoryLabelWidth,status,statusStyle);
        }

        void StoryIntel(Role role,Rect panel)
        {
            var view=Game.View;
            var lines=new List<(string Text,bool Heading)>();
            void Add(string text,bool heading=false)=>lines.Add((text,heading));
            void SafeClues()
            {
                Add("Drawer reports",true);
                var clues=view.evidence??Array.Empty<string>();
                if(clues.Length==0)Add("No clues yet. Press C by the Bar cash drawer to check it.");
                for(int i=clues.Length-1;i>=0;i--)Add(clues[i]);
            }

            if(role==Role.Sheriff)
            {
                Add("Your Deputies are hidden",true);
                Add("Listen to claims, compare clues, and decide whom to trust. A badge claim is not proof.");
                SafeClues();
            }
            else if(SaloonRules.IsIndependent(role))
            {
                Add("You work alone",true);
                Add(role==Role.Swindler?"Make the Sheriff or a hidden Deputy deal the lethal shot. An Outlaw kill does not count.":
                    "Hidden roles are not revealed to you. Listen, watch, and stop the money from escaping.");
                SafeClues();
                if(role==Role.Swindler)Add(view.personalVictory?"Personal victory earned. The remaining players can finish their match.":"Your personal win does not end the other players' match.");
            }
            else if(role==Role.Outlaw)
            {
                var allies=Array.FindAll(view.knownRoles??Array.Empty<KnownRole>(),p=>p.role=="Outlaw"&&p.id!=view.ownId);
                Add(allies.Length==0?"You are the only Outlaw.":allies.Length==1?"Your partner":"Your teammates",allies.Length>0);
                foreach(var ally in allies)Add(ally.name);
                if(allies.Length>0)Add("Any Outlaw can win for the whole team.");
                if(view.safeProgress>=0)Add(view.safeProgress<SaloonRules.DrawerWorkSteps?"Drawer work "+view.safeProgress+" / "+SaloonRules.DrawerWorkSteps:"Money is missing from the drawer.");
                SafeClues();
                Add("Plan escape during discussion, then lock your Bar choice.");
            }
            else if(role==Role.Deputy)
            {
                var sheriff=Array.Find(view.players??Array.Empty<PlayerView>(),p=>p.sheriff);
                Add("Your Sheriff",true);
                Add(sheriff?.name??"Look for the Sheriff badge.");
                SafeClues();
                Add("When injured, press E near the shared beer tray at the Bar.");
                Add("Keep the Sheriff alive. Their death ends the match for the Law.");
            }

            const float inset=16;
            Fill(panel,new Color(.084f,.105f,.097f));
            float left=panel.x+inset,width=panel.width-2*inset;
            float top=panel.y+inset;
            top+=WrappedText(left,top,width,"Private information",StoryStyle(20,StoryCream))+12;
            Fill(new Rect(left,top-6,width,1),new Color(1,1,1,.10f));
            var style=StoryStyle(17,StoryCream);
            var heights=new float[lines.Count];
            float total=0;
            for(int i=0;i<lines.Count;i++)
            {
                var line=lines[i];
                heights[i]=WrappedHeight(line.Text,width,style)+(line.Heading?2:6);
                total+=heights[i];
            }
            float available=panel.yMax-inset-top;
            var pages=new List<(int First,int End)>();
            if(total>available)
            {
                available-=36;
                int start=0;
                float used=0;
                for(int i=0;i<lines.Count;i++)
                {
                    float needed=heights[i];
                    if(lines[i].Heading&&i+1<lines.Count)needed+=heights[i+1];
                    if(i>start&&used+needed>available)
                    {
                        pages.Add((start,i));
                        start=i;
                        used=0;
                    }
                    used+=heights[i];
                }
                pages.Add((start,lines.Count));
            }
            else pages.Add((0,lines.Count));

            storyIntelPage=Mathf.Clamp(storyIntelPage,0,pages.Count-1);
            if(pages.Count>1)
            {
                var evt=Event.current;
                if(evt.type==EventType.ScrollWheel&&panel.Contains(evt.mousePosition))
                {
                    storyIntelPage=Mathf.Clamp(storyIntelPage+(evt.delta.y>0?1:-1),0,pages.Count-1);
                    evt.Use();
                }
                float navY=panel.yMax-inset-26;
                bool enabled=GUI.enabled;
                GUI.enabled=enabled&&storyIntelPage>0;
                if(PlannerAction(left,navY,73,26,"Back"))storyIntelPage--;
                GUI.enabled=enabled&&storyIntelPage<pages.Count-1;
                if(PlannerAction(left+width-73,navY,73,26,"Next"))storyIntelPage++;
                GUI.enabled=enabled;
                WrappedText(left+76,navY+3,width-152,$"{storyIntelPage+1} of {pages.Count}",StoryStyle(15,StoryGrey,TextAnchor.UpperCenter));
            }
            var page=pages[storyIntelPage];
            float y=top;
            for(int i=page.First;i<page.End;i++)
            {
                var line=lines[i];
                style.normal.textColor=line.Heading?StoryGold:StoryCream;
                WrappedText(left,y,width,line.Text,style);
                y+=heights[i];
            }
        }

        void RoleStoryMap(Role role)
        {
            EnsureStoryArt();
            if(storySnapshot!=Game.View)
            {
                if(storySnapshot?.ownRole!=Game.View.ownRole||storySnapshot?.ownId!=Game.View.ownId)storyIntelPage=0;
                storySnapshot=Game.View;
                storyMap=SaloonStoryMap.Build(Game.View);
            }
            var map=storyMap;
            var guiMatrix=GUI.matrix;
            GUI.matrix=Matrix4x4.identity;
            Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.015f,.022f,.021f,.84f));
            GUI.matrix=guiMatrix;
            var panel=new Rect(56,32,1168,656);
            const float margin=32,sectionGap=16,sidebarWidth=282;
            float left=panel.x+margin,right=panel.xMax-margin,width=right-left;
            float sidebarX=right-sidebarWidth;
            Fill(panel,StoryNight);
            Outline(panel,new Color(.34f,.37f,.29f));
            Fill(new Rect(panel.x,panel.y,panel.width,3),StoryGold);

            float headerY=panel.y+22;
            float captionY=headerY+WrappedText(left,headerY,sidebarX-left,role==Role.Unknown?"Your role":role.ToString(),
                StoryStyle(32,StoryCream,heading:true))+3;
            float headerEnd=captionY+WrappedText(left,captionY,sidebarX-left,map.Caption,StoryStyle(19,StoryCream));
            WrappedText(sidebarX,headerY+8,sidebarWidth,$"Round {Game.View.round}",
                StoryStyle(17,StoryGold,TextAnchor.UpperRight));

            const float closeWidth=182;
            const string controls="You have one action per round. Q draws. Hold left-click for 0.5 s for accuracy; right-click fires instantly with more spread. Only headshots do damage.";
            const string close="Release Tab to close.";
            var footerStyle=StoryStyle(16,StoryGrey);
            float controlsWidth=width-closeWidth-sectionGap;
            float footerHeight=Mathf.Max(WrappedHeight(controls,controlsWidth,footerStyle),WrappedHeight(close,closeWidth,footerStyle));
            float footerY=panel.yMax-18-footerHeight;
            WrappedText(left,footerY,controlsWidth,controls,footerStyle);
            WrappedText(right-closeWidth,footerY,closeWidth,close,StoryStyle(16,StoryGrey,TextAnchor.UpperRight));

            const float nextInset=16,tagWidth=62;
            var nextTitleStyle=StoryStyle(22,StoryCream);
            var nextDetailStyle=StoryStyle(18,StoryCream);
            var nextControlStyle=StoryStyle(16,StoryGrey);
            float nextWidth=width-2*nextInset;
            float nextTitleHeight=WrappedHeight(map.NextTitle,nextWidth-tagWidth,nextTitleStyle);
            float nextContentHeight=WrappedHeight(map.NextDetail,nextWidth,nextDetailStyle)+WrappedHeight(map.NextControl,nextWidth,nextControlStyle);
            float nextHeight=2*nextInset+nextTitleHeight+nextContentHeight+4+(string.IsNullOrEmpty(map.NextControl)?0:4);
            float nextY=footerY-sectionGap-nextHeight;
            Fill(new Rect(left,nextY,width,nextHeight),new Color(.10f,.14f,.12f));
            Fill(new Rect(left,nextY,3,nextHeight),StoryGold);
            float nextTextX=left+nextInset,nextTextY=nextY+nextInset;
            WrappedText(nextTextX,nextTextY+3,tagWidth,"NEXT",StoryStyle(15,StoryGold));
            nextTextY+=WrappedText(nextTextX+tagWidth,nextTextY,nextWidth-tagWidth,map.NextTitle,nextTitleStyle)+4;
            nextTextY+=WrappedText(nextTextX,nextTextY,nextWidth,map.NextDetail,nextDetailStyle)+4;
            WrappedText(nextTextX,nextTextY,nextWidth,map.NextControl,nextControlStyle);

            float contentY=headerEnd+sectionGap;
            float contentHeight=nextY-sectionGap-contentY;
            StoryGraph(map,new Rect(left,contentY,sidebarX-left-24,contentHeight));
            StoryIntel(role,new Rect(sidebarX,contentY,sidebarWidth,contentHeight));
        }

        void StoryGraph(SaloonStoryMap map,Rect area)
        {
            if(map.Nodes.Count==0)return;
            var labelStyle=StoryStyle(18,StoryCream,TextAnchor.UpperCenter);
            var statusStyle=StoryStyle(14,StoryGold,TextAnchor.UpperCenter);
            float labelHeight=0;
            foreach(var node in map.Nodes)
            {
                float height=WrappedHeight(node.Title,StoryLabelWidth,labelStyle)+WrappedHeight(StoryNodeStatus(node),StoryLabelWidth,statusStyle);
                labelHeight=Mathf.Max(labelHeight,height);
            }
            float xInset=StoryLabelWidth/2+6;
            var positions=new Vector2[map.Nodes.Count];
            for(int i=0;i<positions.Length;i++)
            {
                var node=map.Nodes[i];
                positions[i]=new Vector2(Mathf.Lerp(area.xMin+xInset,area.xMax-xInset,Mathf.InverseLerp(145,820,node.X)),
                    Mathf.Lerp(area.yMin+44,area.yMax-StoryLabelOffset-labelHeight-4,Mathf.InverseLerp(198,415,node.Y)));
            }
            for(int i=0;i<positions.Length;i++)
                foreach(int source in map.Nodes[i].From)StoryTrail(positions[source],positions[i],map.Nodes[i].State);
            for(int i=0;i<positions.Length;i++)
                StoryNode(map.Nodes[i],positions[i],labelStyle,statusStyle);
        }
    }
}
