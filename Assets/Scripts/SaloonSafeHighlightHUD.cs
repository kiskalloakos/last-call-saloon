using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        void InteractionPrompt()
        {
            if(Game.PlayingTableGame)return;
            if(Game.SafeHighlight?.Highlighted==true)
            {
                SafeInteractionPrompt();
                return;
            }
            if(string.IsNullOrEmpty(Game.InteractionHint))return;
            var cream=new Color(.98f,.94f,.84f);
            var style=new GUIStyle(body){fontSize=18,richText=false,wordWrap=true,alignment=TextAnchor.MiddleCenter,normal={textColor=cream}};
            const float maxWidth=456;
            const float horizontalInset=20,verticalInset=12;
            var content=new GUIContent(Game.InteractionHint);
            float width=Mathf.Min(maxWidth,Mathf.Ceil(style.CalcSize(content).x)+2);
            float height=WrappedHeight(Game.InteractionHint,width,style);
            var rect=new Rect(640-width/2-horizontalInset,657-height-2*verticalInset,width+2*horizontalInset,height+2*verticalInset);
            Paper(()=>
            {
                Fill(rect,new Color(.035f,.045f,.04f,.96f));
                Outline(rect,new Color(.54f,.46f,.32f));
                Text(rect.x+horizontalInset,rect.y+verticalInset,width,height,Game.InteractionHint,style);
            });
        }

        void SafeInteractionPrompt()
        {
            var marker=Game.SafeHighlight;
            var point=Game.ViewCamera.WorldToScreenPoint(marker.Anchor);
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            float x=(point.x-(Screen.width-1280*scale)/2)/scale;
            float y=(Screen.height-point.y-(Screen.height-720*scale)/2)/scale;
            bool onScreen=point.z>0&&x>30&&x<1250&&y>80&&y<650;
            var amber=new Color(1,.73f,.32f);var cream=new Color(.98f,.94f,.84f);
            const float width=384,inset=14,keySize=48,gap=14;
            const float textLeft=inset+keySize+gap,textWidth=width-textLeft-inset;
            var style=new GUIStyle(body){fontSize=17,wordWrap=true,richText=false,normal={textColor=cream}};
            var actionStyle=new GUIStyle(style){fontSize=21};
            string status=onScreen?marker.Status:"Turn to face the safe.";
            float actionHeight=WrappedHeight(marker.Action,textWidth,actionStyle);
            float statusHeight=WrappedHeight(status,textWidth,style);
            float textHeight=actionHeight+3+statusHeight;
            float height=2*inset+Mathf.Max(keySize,textHeight);
            var rect=new Rect(onScreen?Mathf.Clamp(x-width/2,24,1256-width):(1280-width)/2,
                onScreen?Mathf.Clamp(y-height-12,200,520-height):455,width,height);
            Paper(()=>
            {
                Fill(rect,new Color(.035f,.045f,.04f,.97f));Outline(rect,amber);
                var key=new Rect(rect.x+inset,rect.center.y-keySize/2,keySize,keySize);
                Fill(key,amber);
                Text(key.x,key.y,key.width,key.height,marker.Key,new GUIStyle(title){fontSize=30,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(.09f,.08f,.05f)}});
                float textTop=rect.center.y-textHeight/2;
                Text(rect.x+textLeft,textTop,textWidth,actionHeight,marker.Action,actionStyle);
                Text(rect.x+textLeft,textTop+actionHeight+3,textWidth,statusHeight,status,style);
                if(onScreen&&y>rect.yMax+5)
                {
                    Fill(new Rect(x-1,rect.yMax,2,y-rect.yMax),amber);
                    Fill(new Rect(x-3,y-3,6,6),amber);
                }
            });
        }
    }
}
