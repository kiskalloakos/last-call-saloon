using System.Collections.Generic;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        readonly Color paper=new Color(.075f,.09f,.105f),rule=new Color(.20f,.24f,.27f);
        readonly List<Texture2D> uiTextures=new List<Texture2D>();
        GUISkin flatSkin;
        GUIStyle selectedButton,disabledButton;
        Font headingFont;

        Texture2D Swatch(Color color)
        {
            var texture=new Texture2D(1,1){hideFlags=HideFlags.HideAndDontSave};
            texture.SetPixel(0,0,color);texture.Apply();uiTextures.Add(texture);return texture;
        }
        void FlatStates(GUIStyle style,Texture2D normal,Texture2D hover,Texture2D active,Color text)
        {
            style.border=new RectOffset();
            foreach(var state in new[]{style.normal,style.focused,style.onNormal,style.onFocused}){state.background=normal;state.textColor=text;state.scaledBackgrounds=null;}
            foreach(var state in new[]{style.hover,style.onHover}){state.background=hover;state.textColor=text;state.scaledBackgrounds=null;}
            foreach(var state in new[]{style.active,style.onActive}){state.background=active;state.textColor=text;state.scaledBackgrounds=null;}
        }
        void BuildTheme()
        {
            var blank=Swatch(new Color(.10f,.12f,.14f));var hover=Swatch(new Color(.16f,.20f,.23f));
            var accent=Swatch(new Color(.14f,.23f,.25f));var accentHover=Swatch(new Color(.19f,.29f,.31f));var line=Swatch(rule);
            flatSkin=Instantiate(GUI.skin);flatSkin.hideFlags=HideFlags.HideAndDontSave;
            headingFont=null;
            title=new GUIStyle(GUI.skin.label){font=GUI.skin.font,fontSize=38,fontStyle=FontStyle.Bold,normal={textColor=ink}};
            body=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true,normal={textColor=ink}};
            subtitle=new GUIStyle(body){fontSize=12,fontStyle=FontStyle.Bold,normal={textColor=gold}};
            small=new GUIStyle(body){fontSize=12,normal={textColor=muted}};
            roleStyle=new GUIStyle(title){fontSize=34,alignment=TextAnchor.MiddleCenter};
            button=new GUIStyle{font=GUI.skin.font,fontSize=13,fontStyle=FontStyle.Normal,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(10,10,4,4),clipping=TextClipping.Clip};
            FlatStates(button,blank,hover,hover,ink);
            selectedButton=new GUIStyle(button);FlatStates(selectedButton,accent,accentHover,accentHover,ink);
            disabledButton=new GUIStyle(button);FlatStates(disabledButton,Swatch(paper),Swatch(paper),Swatch(paper),muted);
            field=new GUIStyle(GUI.skin.textField){fontSize=17,padding=new RectOffset(12,12,8,8)};
            FlatStates(field,blank,blank,hover,ink);
            flatSkin.settings.cursorColor=gold;flatSkin.settings.selectionColor=new Color(.24f,.38f,.40f);
            FlatStates(flatSkin.horizontalSlider,line,line,line,ink);flatSkin.horizontalSlider.fixedHeight=3;flatSkin.horizontalSlider.margin=new RectOffset(0,0,10,10);
            FlatStates(flatSkin.horizontalSliderThumb,accent,accentHover,accent,ink);flatSkin.horizontalSliderThumb.fixedWidth=12;flatSkin.horizontalSliderThumb.fixedHeight=16;
            FlatStates(flatSkin.verticalScrollbar,line,line,line,ink);flatSkin.verticalScrollbar.fixedWidth=5;
            FlatStates(flatSkin.verticalScrollbarThumb,accent,accentHover,accent,ink);flatSkin.verticalScrollbarThumb.fixedWidth=5;
            foreach(var style in new[]{flatSkin.verticalScrollbarUpButton,flatSkin.verticalScrollbarDownButton,flatSkin.horizontalScrollbarLeftButton,flatSkin.horizontalScrollbarRightButton})
            {style.fixedHeight=style.fixedWidth=0;FlatStates(style,line,line,line,ink);}
            flatSkin.scrollView=new GUIStyle();
        }
        void Fill(Rect rect,Color color)
        {var previous=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=previous;}
        void Outline(Rect r,Color c)
        {Fill(new Rect(r.x,r.y,r.width,1),c);Fill(new Rect(r.x,r.yMax-1,r.width,1),c);Fill(new Rect(r.x,r.y,1,r.height),c);Fill(new Rect(r.xMax-1,r.y,1,r.height),c);}
        void OnDestroy()
        {
            foreach(var texture in uiTextures)if(texture!=null)Destroy(texture);
            if(flatSkin!=null)Destroy(flatSkin);if(coinTexture!=null)Destroy(coinTexture);if(headingFont!=null)Destroy(headingFont);
        }
    }
}
