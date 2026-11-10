using System.Collections.Generic;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        readonly Color paper=new Color(.91f,.85f,.72f,.97f),rule=new Color(.47f,.36f,.23f,.45f);
        readonly List<Texture2D> uiTextures=new List<Texture2D>();
        GUISkin flatSkin;
        GUIStyle selectedButton,disabledButton;
        Font headingFont,bodyFont;
        bool worldHud;
        Texture2D badgeIcon,cartridgeIcon;

        Texture2D Swatch(Color color)
        {
            var texture=new Texture2D(1,1){hideFlags=HideFlags.HideAndDontSave};
            texture.SetPixel(0,0,color);texture.Apply();uiTextures.Add(texture);return texture;
        }
        static void ClearScaledBackgrounds(GUIStyleState state)
        {
#if UNITY_EDITOR
            state.scaledBackgrounds=null;
#endif
        }
        void FlatStates(GUIStyle style,Texture2D normal,Texture2D hover,Texture2D active,Color text)
        {
            style.border=new RectOffset();
            foreach(var state in new[]{style.normal,style.focused,style.onNormal,style.onFocused}){state.background=normal;state.textColor=text;ClearScaledBackgrounds(state);}
            foreach(var state in new[]{style.hover,style.onHover}){state.background=hover;state.textColor=text;ClearScaledBackgrounds(state);}
            foreach(var state in new[]{style.active,style.onActive}){state.background=active;state.textColor=text;ClearScaledBackgrounds(state);}
        }
        // Labels inherit hover states from Unity's skin, including in copied HUD styles.
        // Keep every state identical so only buttons visually react to the pointer.
        static void PassiveText(GUIStyle style)
        {
            Color color=style.normal.textColor;
            foreach(var state in new[]{style.normal,style.hover,style.active,style.focused,style.onNormal,style.onHover,style.onActive,style.onFocused})
            {state.textColor=color;state.background=null;ClearScaledBackgrounds(state);}
        }
        void BuildTheme()
        {
            var blank=Swatch(new Color(.82f,.74f,.60f,.8f));var hover=Swatch(new Color(.95f,.90f,.79f));
            var accent=Swatch(new Color(.53f,.19f,.12f));var accentHover=Swatch(new Color(.65f,.25f,.15f));var line=Swatch(rule);
            flatSkin=Instantiate(GUI.skin);flatSkin.hideFlags=HideFlags.HideAndDontSave;
            headingFont=Resources.Load<Font>("Fonts/Rye-Regular");
            bodyFont=Resources.Load<Font>("Fonts/BarlowSemiCondensed-Medium");
            if(headingFont==null||bodyFont==null)throw new System.InvalidOperationException("Bundled HUD fonts are missing.");
            flatSkin.font=bodyFont;
            badgeIcon=Icon(true);cartridgeIcon=Icon(false);
            title=new GUIStyle(GUI.skin.label){font=headingFont,fontSize=42,fontStyle=FontStyle.Normal,normal={textColor=ink}};
            body=new GUIStyle(GUI.skin.label){font=bodyFont,fontSize=17,wordWrap=true,normal={textColor=ink}};
            subtitle=new GUIStyle(body){fontSize=13,fontStyle=FontStyle.Normal,normal={textColor=gold}};
            small=new GUIStyle(body){fontSize=14,normal={textColor=muted}};
            roleStyle=new GUIStyle(title){fontSize=34,alignment=TextAnchor.MiddleCenter};
            button=new GUIStyle{font=bodyFont,fontSize=16,fontStyle=FontStyle.Normal,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(10,10,4,4),clipping=TextClipping.Clip};
            FlatStates(button,blank,hover,hover,ink);
            selectedButton=new GUIStyle(button);FlatStates(selectedButton,accent,accentHover,accentHover,new Color(1,.94f,.8f));
            disabledButton=new GUIStyle(button);FlatStates(disabledButton,Swatch(paper),Swatch(paper),Swatch(paper),muted);
            field=new GUIStyle(GUI.skin.textField){font=bodyFont,fontSize=18,padding=new RectOffset(12,12,8,8)};
            FlatStates(field,blank,blank,hover,ink);
            flatSkin.settings.cursorColor=gold;flatSkin.settings.selectionColor=new Color(.44f,.34f,.18f);
            FlatStates(flatSkin.horizontalSlider,line,line,line,ink);flatSkin.horizontalSlider.fixedHeight=3;flatSkin.horizontalSlider.margin=new RectOffset(0,0,10,10);
            FlatStates(flatSkin.horizontalSliderThumb,accent,accent,accent,ink);flatSkin.horizontalSliderThumb.fixedWidth=12;flatSkin.horizontalSliderThumb.fixedHeight=16;
            FlatStates(flatSkin.verticalScrollbar,line,line,line,ink);flatSkin.verticalScrollbar.fixedWidth=5;
            FlatStates(flatSkin.verticalScrollbarThumb,accent,accent,accent,ink);flatSkin.verticalScrollbarThumb.fixedWidth=5;
            foreach(var style in new[]{flatSkin.verticalScrollbarUpButton,flatSkin.verticalScrollbarDownButton,flatSkin.horizontalScrollbarLeftButton,flatSkin.horizontalScrollbarRightButton})
            {style.fixedHeight=style.fixedWidth=0;FlatStates(style,line,line,line,ink);}
            flatSkin.scrollView=new GUIStyle();
        }
        Texture2D Icon(bool badge)
        {
            const int size=96;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=(x+.5f)/size,v=(y+.5f)/size;bool solid;
                if(badge)
                {
                    float dx=u-.5f,dy=v-.5f,r=Mathf.Sqrt(dx*dx+dy*dy),angle=Mathf.Atan2(dy,dx);
                    solid=r<.29f+.16f*Mathf.Cos(6*angle);
                    if(r>.10f&&r<.13f)solid=false;
                }
                else solid=(u>.33f&&u<.67f&&v>.14f&&v<.70f)||(new Vector2((u-.5f)*2,v-.70f).magnitude<.17f&&v>=.70f)||(u>.29f&&u<.71f&&v>.07f&&v<.12f);
                texture.SetPixel(x,y,solid?Color.white:Color.clear);
            }
            texture.Apply();uiTextures.Add(texture);return texture;
        }
        void Graphic(Rect rect,Texture2D texture,Color color)
        {var previous=GUI.color;GUI.color=color;GUI.DrawTexture(rect,texture);GUI.color=previous;}
        void Paper(System.Action draw)
        {bool prior=worldHud;worldHud=false;draw();worldHud=prior;}
        void Fill(Rect rect,Color color)
        {var previous=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=previous;}
        void Outline(Rect r,Color c)
        {Fill(new Rect(r.x,r.y,r.width,1),c);Fill(new Rect(r.x,r.yMax-1,r.width,1),c);Fill(new Rect(r.x,r.y,1,r.height),c);Fill(new Rect(r.xMax-1,r.y,1,r.height),c);}
        void OnDestroy()
        {
            foreach(var texture in uiTextures)if(texture!=null)Destroy(texture);
            if(flatSkin!=null)Destroy(flatSkin);if(coinTexture!=null)Destroy(coinTexture);
        }
    }
}
