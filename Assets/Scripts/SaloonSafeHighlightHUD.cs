using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        void SafeInteractionPrompt()
        {
            var marker=Game.SafeHighlight;
            if(marker==null||!marker.Highlighted)return;
            var point=Game.ViewCamera.WorldToScreenPoint(marker.Anchor);
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            float x=(point.x-(Screen.width-1280*scale)/2)/scale;
            float y=(Screen.height-point.y-(Screen.height-720*scale)/2)/scale;
            bool onScreen=point.z>0&&x>30&&x<1250&&y>80&&y<650;
            var rect=new Rect(onScreen?Mathf.Clamp(x-172,24,912):468,onScreen?Mathf.Clamp(y-88,200,440):455,344,76);
            var amber=new Color(1,.73f,.32f);var cream=new Color(.98f,.94f,.84f);
            Paper(()=>
            {
                Fill(rect,new Color(.035f,.045f,.04f,.97f));Outline(rect,amber);
                Fill(new Rect(rect.x+12,rect.y+13,48,48),amber);
                Text(rect.x+12,rect.y+13,48,48,marker.Key,new GUIStyle(title){fontSize=30,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(.09f,.08f,.05f)}});
                Text(rect.x+73,rect.y+13,258,25,marker.Action,new GUIStyle(body){fontSize=18,wordWrap=false,normal={textColor=cream}});
                Text(rect.x+73,rect.y+41,258,30,onScreen?marker.Status:"Safe nearby · turn to face it",new GUIStyle(small){fontSize=13,normal={textColor=cream}});
                if(onScreen&&y>rect.yMax+5)
                {
                    Fill(new Rect(x-1,rect.yMax,2,y-rect.yMax),amber);
                    Fill(new Rect(x-3,y-3,6,6),amber);
                }
            });
        }
    }
}
