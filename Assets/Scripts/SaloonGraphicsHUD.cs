using System;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        SaloonGraphics.Options graphicsDraft;
        bool displayWasPending;

        void OpenGraphics()
        {
            graphicsDraft=Game.Graphics.Current;
            displayWasPending=false;
            Game.MenuOpen=true;
            Game.GraphicsMenuOpen=true;
        }

        void GraphicsMenu()
        {
            var graphics=Game.Graphics;
            if(displayWasPending&&!graphics.AwaitingConfirmation)graphicsDraft=graphics.Current;
            displayWasPending=graphics.AwaitingConfirmation;

            MenuBackdrop(!Game.InRoom,true);
            var panel=new Rect(214,44,852,632);
            MenuFrame(panel);
            const float left=246,width=788,controlsX=530,controlsWidth=504;
            if(graphics.AwaitingConfirmation)
            {
                MenuText(left,222,width,"Keep these display settings?",36,heading:true);
                MenuText(left,298,width,$"The previous settings return in {Mathf.CeilToInt(graphics.ConfirmationRemaining)} seconds.",21,muted:true);
                if(MenuButton(new Rect(left,373,384,48),"Keep changes",true))graphics.Keep();
                if(MenuButton(new Rect(left+404,373,384,48),"Revert")){graphics.Revert();graphicsDraft=graphics.Current;}
                MenuText(left,440,width,"Press Escape to revert and close this menu.",17,muted:true);
                return;
            }

            MenuText(left,74,width,"Graphics",37,heading:true);
            MenuText(left,127,width,"These settings apply to this computer.",18,muted:true);
            MenuDivider(left,166,width);

            GraphicsLabel(left,181,"Quality");
            const float presetWidth=160;
            for(int i=0;i<SaloonGraphics.PresetNames.Length;i++)
            {
                string name=SaloonGraphics.PresetNames[i];
                string label=name[0]+name.Substring(1).ToLowerInvariant();
                if(MenuButton(new Rect(controlsX+i*(presetWidth+12),181,presetWidth,36),label,graphicsDraft.preset==i))
                    graphicsDraft.preset=i;
            }
            string qualityInfo=graphicsDraft.preset==0?"Lower texture detail. No shadows or anti-aliasing.":
                graphicsDraft.preset==1?"Full texture detail. Hard shadows.":"Soft shadows and 4× anti-aliasing.";
            MenuText(controlsX,224,controlsWidth,qualityInfo,16,muted:true);

            GraphicsLabel(left,255,"Resolution");
            var resolutions=graphics.Resolutions;
            int resolution=Array.IndexOf(resolutions,new Vector2Int(graphicsDraft.width,graphicsDraft.height));
            int nextResolution=GraphicsStepper(controlsX,255,controlsWidth,$"{graphicsDraft.width} × {graphicsDraft.height}",resolution,resolutions.Length);
            if(nextResolution>=0){graphicsDraft.width=resolutions[nextResolution].x;graphicsDraft.height=resolutions[nextResolution].y;}

            GraphicsLabel(left,308,"Display mode");
            graphicsDraft.windowed=GraphicsSwitch(controlsX,308,controlsWidth,"Windowed","Borderless",graphicsDraft.windowed);

            GraphicsLabel(left,361,"VSync");
            graphicsDraft.vsync=GraphicsSwitch(controlsX,361,controlsWidth,"On","Off",graphicsDraft.vsync);

            GraphicsLabel(left,414,"Frame rate limit");
            int frame=Array.IndexOf(SaloonGraphics.FrameLimits,graphicsDraft.frameLimit);
            string limit=graphicsDraft.vsync?"Monitor refresh rate":graphicsDraft.frameLimit==0?"Unlimited":$"{graphicsDraft.frameLimit} FPS";
            frame=GraphicsStepper(controlsX,414,controlsWidth,limit,frame,SaloonGraphics.FrameLimits.Length,!graphicsDraft.vsync);
            graphicsDraft.frameLimit=SaloonGraphics.FrameLimits[Mathf.Max(0,frame)];
            MenuText(controlsX,456,controlsWidth,graphicsDraft.vsync?"Turn VSync off to use your frame rate limit.":"Performance depends on your computer and resolution.",16,muted:true);

            GraphicsLabel(left,489,"Reduced motion");
            graphicsDraft.reducedMotion=GraphicsSwitch(controlsX,489,controlsWidth,"On","Off",graphicsDraft.reducedMotion);
            if(Application.isEditor)MenuText(left,545,width,"Resolution and display mode take effect in the standalone game.",16,muted:true);
            MenuDivider(left,584,width);
            if(MenuButton(new Rect(left,604,180,43),"Defaults"))graphicsDraft=graphics.Defaults();
            if(MenuButton(new Rect(650,604,180,43),"Back")){graphicsDraft=graphics.Current;Game.GraphicsMenuOpen=false;}
            if(MenuButton(new Rect(850,604,184,43),"Apply",true)){graphics.Apply(graphicsDraft);graphicsDraft=graphics.Current;}
        }

        void GraphicsLabel(float x,float y,string label)=>MenuText(x,y+7,250,label,19);

        bool GraphicsSwitch(float x,float y,float width,string first,string second,bool firstSelected)
        {
            float half=(width-12)/2;
            if(MenuButton(new Rect(x,y,half,36),first,firstSelected))return true;
            if(MenuButton(new Rect(x+half+12,y,half,36),second,!firstSelected))return false;
            return firstSelected;
        }

        int GraphicsStepper(float x,float y,float width,string value,int index,int options,bool enabled=true)
        {
            if(MenuButton(new Rect(x,y,48,36),"‹",enabled:enabled))index=(Mathf.Max(0,index)+options-1)%options;
            if(MenuButton(new Rect(x+width-48,y,48,36),"›",enabled:enabled))index=(Mathf.Max(0,index)+1)%options;
            var center=new Rect(x+56,y,width-112,36);
            Fill(center,MenuSurface);
            Text(center.x,center.y,center.width,center.height,value,MenuStyle(18,enabled?MenuInk:MenuMuted,TextAnchor.MiddleCenter));
            return index;
        }
    }
}
