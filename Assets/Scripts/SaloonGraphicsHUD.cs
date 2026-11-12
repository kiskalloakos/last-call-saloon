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
            graphicsDraft=Game.Graphics.Current;displayWasPending=false;
            Game.MenuOpen=true;Game.GraphicsMenuOpen=true;
        }

        void GraphicsMenu()
        {
            var graphics=Game.Graphics;
            if(displayWasPending&&!graphics.AwaitingConfirmation)graphicsDraft=graphics.Current;
            displayWasPending=graphics.AwaitingConfirmation;
            Panel(new Rect(220,35,840,650),paper);
            Text(255,55,770,50,"GRAPHICS SETTINGS",roleStyle);
            Text(255,110,770,30,"Settings apply to this computer. The match continues while this menu is open.",small);
            if(graphics.AwaitingConfirmation)
            {
                Text(275,220,730,60,"KEEP THESE DISPLAY SETTINGS?",subtitle);
                Text(275,285,730,60,"Reverting in "+Mathf.CeilToInt(graphics.ConfirmationRemaining)+" seconds.",body);
                if(Button(275,390,350,45,"KEEP CHANGES"))graphics.Keep();
                if(Button(645,390,350,45,"REVERT")){graphics.Revert();graphicsDraft=graphics.Current;}
                Text(275,460,730,30,"Escape also reverts and closes this menu.",small);
                return;
            }
            Text(255,163,220,30,"QUALITY",subtitle);
            for(int i=0;i<3;i++)
                if(Button(495+i*175,158,165,38,SaloonGraphics.PresetNames[i],graphicsDraft.preset==i))graphicsDraft.preset=i;
            string detail=graphicsDraft.preset==0?"No shadows or anti-aliasing; reduced texture detail.":
                graphicsDraft.preset==1?"Hard shadows, full texture detail, no anti-aliasing.":"Soft shadows, full texture detail, 4x anti-aliasing.";
            Text(495,203,520,42,detail,small);
            Text(255,265,220,30,"RESOLUTION",subtitle);
            var resolutions=graphics.Resolutions;
            int resolution=Array.IndexOf(resolutions,new Vector2Int(graphicsDraft.width,graphicsDraft.height));
            if(Button(495,258,48,38,"<"))resolution=(Mathf.Max(0,resolution)+resolutions.Length-1)%resolutions.Length;
            Text(555,264,395,30,graphicsDraft.width+" × "+graphicsDraft.height,body);
            if(Button(972,258,48,38,">"))resolution=(Mathf.Max(0,resolution)+1)%resolutions.Length;
            if(resolution>=0){graphicsDraft.width=resolutions[resolution].x;graphicsDraft.height=resolutions[resolution].y;}
            Text(255,325,220,30,"DISPLAY MODE",subtitle);
            if(Button(495,318,255,38,"WINDOWED",graphicsDraft.windowed))graphicsDraft.windowed=true;
            if(Button(765,318,255,38,"BORDERLESS",!graphicsDraft.windowed))graphicsDraft.windowed=false;
            Text(255,385,220,30,"VSYNC",subtitle);
            if(Button(495,378,255,38,"OFF",!graphicsDraft.vsync))graphicsDraft.vsync=false;
            if(Button(765,378,255,38,"ON",graphicsDraft.vsync))graphicsDraft.vsync=true;
            Text(255,445,220,30,"FRAME RATE LIMIT",subtitle);
            GUI.enabled=!graphicsDraft.vsync;
            int frame=Array.IndexOf(SaloonGraphics.FrameLimits,graphicsDraft.frameLimit);
            if(Button(495,438,48,38,"<"))frame=(Mathf.Max(0,frame)+SaloonGraphics.FrameLimits.Length-1)%SaloonGraphics.FrameLimits.Length;
            if(Button(972,438,48,38,">"))frame=(Mathf.Max(0,frame)+1)%SaloonGraphics.FrameLimits.Length;
            graphicsDraft.frameLimit=SaloonGraphics.FrameLimits[Mathf.Max(0,frame)];
            Text(555,444,395,30,graphicsDraft.vsync?"MONITOR REFRESH RATE":graphicsDraft.frameLimit==0?"UNLIMITED":graphicsDraft.frameLimit+" FPS",body);
            GUI.enabled=true;
            Text(255,500,770,60,graphicsDraft.vsync?"VSync controls frame rate while enabled. Your chosen limit is used when VSync is off.":
                "60 FPS is the default limit. Actual performance depends on your PC, resolution and quality.",small);
            if(Application.isEditor)Text(255,554,770,28,"Editor: quality and frame limit apply; display mode and resolution require a player.",small);
            if(Button(255,615,240,40,"DEFAULTS"))graphicsDraft=graphics.Defaults();
            if(Button(510,615,240,40,"CANCEL / BACK")){graphicsDraft=graphics.Current;Game.GraphicsMenuOpen=false;}
            if(Button(765,615,255,40,"APPLY",true)){graphics.Apply(graphicsDraft);graphicsDraft=graphics.Current;}
        }
    }
}
