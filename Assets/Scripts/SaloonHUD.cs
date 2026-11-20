using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD : MonoBehaviour
    {
        public SaloonGame Game;
        public bool ShowsCigaretteBuff=>Game.Self?.alive==true&&!Game.IsSpectating&&Game.View.cigaretteBuff;
        GUIStyle title,subtitle,body,small,button,roleStyle;
        readonly Color ink=new Color(.19f,.13f,.09f), gold=new Color(.54f,.27f,.12f), muted=new Color(.39f,.31f,.23f);
        float noticeAt;
        string lastNotice="";

        void Styles()
        {
            if(title!=null)return;
            BuildTheme();
        }

        void OnGUI()
        {
            Styles();worldHud=false;GUI.skin=flatSkin;float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            if(Game.Connection.Reconnecting)
            {
                ReconnectMenu();
                return;
            }
            if(Game.GraphicsMenuOpen){GraphicsMenu();return;}
            if(!Game.InRoom){MainMenu();return;}
            if(Game.MenuOpen){InRoomMenu();return;}
            if(Game.RoleIntroActive&&!Game.MenuOpen)
            {
                // Only expose the world as the black screen fades away.
                if(Game.RoleIntroElapsed>SaloonGame.RoleIntroDuration-.8f)
                {bool enabled=GUI.enabled;GUI.enabled=false;Gameplay();GUI.enabled=enabled;}
                RoleIntro();return;
            }
            if(!Game.ShowRole)storyIntelPage=0;
            CombatOverlay();
            Gameplay();BartenderArrivalNotice();
            if(!Game.ShowRole&&!Game.RoomPlannerOpen&&Game.View?.phase!=Phase.Choosing&&!string.IsNullOrEmpty(Game.View?.reconnectNotice))
            {Panel(new Rect(290,650,700,50),paper);Text(310,660,660,30,Game.View.reconnectNotice,small);}
        }

        void Panel(Rect rect,Color color)
        {
            if(rect.width>=1280){Fill(rect,color);return;}
            if(rect.height<=3){Fill(rect,gold);return;}
            Fill(new Rect(rect.x+2,rect.y+3,rect.width,rect.height),new Color(.20f,.12f,.06f,.12f));
            Fill(rect,paper);Outline(rect,rule);
            Fill(new Rect(rect.x+5,rect.y+5,rect.width-10,1),new Color(1,.97f,.86f,.6f));
        }
        void Text(float x,float y,float width,float height,string text,GUIStyle style=null)
        {
            bool enabled=GUI.enabled;GUI.enabled=true;var shown=new GUIStyle(style??body);
            if(worldHud)
            {
                shown.normal.textColor=style==small?new Color(.92f,.87f,.76f):new Color(1,.95f,.82f);
                var shadow=new GUIStyle(shown);shadow.normal.textColor=new Color(.14f,.07f,.03f,.8f);PassiveText(shadow);
                GUI.Label(new Rect(x+1,y+2,width,height),text,shadow);
            }
            PassiveText(shown);
            GUI.Label(new Rect(x,y,width,height),text,shown);GUI.enabled=enabled;
        }
        static float WrappedHeight(string text,float width,GUIStyle style)
            =>string.IsNullOrEmpty(text)?0:Mathf.Ceil(style.CalcHeight(new GUIContent(text),width))+2;

        float WrappedText(float x,float y,float width,string text,GUIStyle style)
        {
            float height=WrappedHeight(text,width,style);
            if(height>0)Text(x,y,width,height,text,style);
            return height;
        }

        bool Button(float x,float y,float width,float height,string label,bool selected=false)
        {
            selected|=label.StartsWith("✓ ");label=label.Replace("✓ ","");
            var rect=new Rect(x,y,width,height);
            bool enabled=GUI.enabled;GUI.enabled=true;
            bool clicked=GUI.Button(rect,label,!enabled?disabledButton:selected?selectedButton:button);GUI.enabled=enabled;
            if(Event.current.type==EventType.Repaint)Outline(rect,selected?gold:rule);
            return clicked&&enabled;
        }

        void Gameplay()
        {
            var view=Game.View;if(view==null){Text(40,30,650,100,Game.Connection.Status);return;}
            if(Game.ShowRole&&!Game.MenuOpen)
            {
                Enum.TryParse(view.ownRole,out Role role);
                RoleCard(role);
                return;
            }
            if(view.phase==Phase.Finished&&!Game.MenuOpen&&!Game.ShowRole)
            {
                ShowMatchResults(view);
                return;
            }
            bool roomChoicesOpen=!Game.MenuOpen&&!Game.ShowRole&&Game.Self?.alive==true&&
                (view.phase==Phase.Choosing||view.phase==Phase.Discussion&&Game.RoomPlannerOpen);
            bool showVitals=!Game.ShowRole&&!roomChoicesOpen&&!Game.BartenderMenuVisible;
            worldHud=true;
            var sheriff=view.players.FirstOrDefault(p=>p.sheriff);
            if(view.phase!=Phase.Lobby&&sheriff!=null)
            {
                Graphic(new Rect(31,24,43,43),badgeIcon,new Color(.97f,.75f,.34f));
                Text(84,23,250,20,sheriff.alive?"THE SHERIFF":"SHERIFF · ELIMINATED",subtitle);
                Text(84,45,250,24,sheriff.name+(sheriff.id==Game.LocalId?"  /  YOU":""),new GUIStyle(body){fontSize=15,wordWrap=false});
            }
            string phaseLabel=view.phase==Phase.Lobby?"LOBBY":view.phase==Phase.Discussion?"DISCUSSION":view.phase==Phase.Turns?"ROOM ACTIONS":view.phase==Phase.Regroup?"REGROUP":view.phase==Phase.Aftermath?"AFTERMATH":view.phase==Phase.Finished?"MATCH FINISHED":"CHOOSE ROOMS";
            Text(430,22,420,22,"ROUND "+view.round+"  /  "+phaseLabel,new GUIStyle(title){fontSize=16,alignment=TextAnchor.MiddleCenter});
            string status=view.phase==Phase.Discussion?TimeSpan.FromSeconds(Mathf.Ceil(view.remaining)).ToString(@"mm\:ss")+" · "+view.players.Count(p=>p.alive&&p.ready)+" / "+view.players.Count(p=>p.alive)+" locked":view.phase==Phase.Lobby?view.players.Length+" players":(view.phase==Phase.Turns?(view.actionsRemaining==1?"1 player has an action left":view.actionsRemaining+" players have actions left")+" · ":"")+TimeSpan.FromSeconds(view.remaining).ToString(@"mm\:ss");
            if(view.duel==null)Text(430,47,420,22,status,new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
            if(view.posseStage>0&&view.phase!=Phase.Finished)
            {
                Fill(new Rect(520,78,240,2),new Color(.97f,.65f,.33f,.7f));
                string pressure=view.posseStage==4?"POSSE ARRIVED":view.posseRoundsRemaining==1?"POSSE APPROACHING · FINAL ROUND":"POSSE APPROACHING · "+view.posseRoundsRemaining+" ROUNDS LEFT";
                Text(402,82,476,22,pressure,new GUIStyle(subtitle){alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(1,.72f,.38f)}});
                Text(402,107,476,31,SaloonPosse.Dialogue(view.posseStage),new GUIStyle(small){fontSize=12,alignment=TextAnchor.MiddleCenter});
            }
            if(view.phase==Phase.Turns&&view.duel==null)
            {
                string why=view.actionsRemaining==0&&view.actionsInProgress>0?"Finishing "+view.actionsInProgress+" timed action(s)…":view.actionBlockReason;
                if(!string.IsNullOrEmpty(why)){Text(407,155,466,36,why,new GUIStyle(small){alignment=TextAnchor.MiddleCenter});}
            }
            if(view.phase==Phase.Discussion&&!Game.MenuOpen&&!Game.ShowRole&&!roomChoicesOpen)
                Text(430,82,420,26,"Right-click to plan. Press Space to lock or unlock.",new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
            IncomingInvitationNotice();
            if(!Game.MenuOpen&&Game.Voice!=null)
                Text(890,22,354,26,Game.Voice.MicrophoneStatus,new GUIStyle(small){alignment=TextAnchor.MiddleRight});
            if(showVitals){Vitals();CombatHitMarker();}
            BartenderMoodMeter();
            if(!Game.MenuOpen&&showVitals&&Game.Self?.alive==true)
            {
                if(!Game.OutsideForSmoke&&!Game.PlayingTableGame)
                {var aim=new Color(.95f,.91f,.83f,.65f);Fill(new Rect(634,359,4,1),aim);Fill(new Rect(643,359,4,1),aim);Fill(new Rect(640,353,1,4),aim);Fill(new Rect(640,362,1,4),aim);}
                InteractionPrompt();
            }
            if(!Game.MenuOpen)
            {
                if(!string.IsNullOrEmpty(view.objectiveNotice)&&view.phase!=Phase.Discussion)Text(36,107,300,58,view.objectiveNotice,body);

            }
            string notice=Game.Connection.WaitingForSnapshot?"Connection delayed — waiting for host ("+Game.Connection.SnapshotAge.ToString("0")+"s)":Game.Self?.alive==false&&!Game.Self.observer?"ELIMINATED · Entering spectator mode shortly":Game.IsSpectating?"":view.phase==Phase.Aftermath?"":view.busyRemaining>0?$"{view.busyRemaining:0.0}s":view.notice;
            if(notice!=lastNotice){lastNotice=notice;noticeAt=Time.unscaledTime;}
            if(!roomChoicesOpen&&!Game.BartenderMenuVisible&&!Game.PlayingTableGame&&!string.IsNullOrEmpty(notice)&&
                (Time.unscaledTime-noticeAt<4||Game.Connection.WaitingForSnapshot||view.busyRemaining>0||Game.Self?.alive==false))
            {
                var style=new GUIStyle(small){alignment=TextAnchor.UpperCenter};
                float bottom=string.IsNullOrEmpty(Game.InteractionHint)?607:574;
                WrappedText(404,bottom-WrappedHeight(notice,472,style),472,notice,style);
            }
            if(roomChoicesOpen)Paper(RoomChoices);
            MinigameControls();
            BartenderPanel();
            if(Game.IsSpectating&&!Game.MenuOpen)
            {

                Text(450,635,380,22,"SPECTATING  /  "+Game.CameraPlayer.name,subtitle);
                if(Button(450,665,180,27,"← PREVIOUS"))Game.CycleSpectator(-1);
                if(Button(650,665,180,27,"NEXT →"))Game.CycleSpectator(1);

            }
            if(Game.Self!=null&&!Game.Self.alive&&!Game.IsSpectating&&view.phase!=Phase.Finished&&Game.ShowEliminationOverlay)
            {worldHud=false;Panel(new Rect(390,280,500,110),paper);Text(420,294,440,42,"ELIMINATED",new GUIStyle(roleStyle){fontSize=28});Text(420,343,440,30,"Your voice is separated from living players.",small);}
        }

        void Vitals()
        {
            var view=Game.View;int hp=SaloonHealth.Clamp(Game.CameraPlayer?.health??view.health);
            var red=new Color(.95f,.30f,.23f);var healthy=new Color(.70f,.83f,.49f);
            var healthColor=hp<=SaloonHealth.RevolverDamage?red:hp<=2*SaloonHealth.RevolverDamage?new Color(1,.71f,.30f):healthy;
            string role=Game.IsSpectating?Game.CameraPlayer.name:(view.ownRole??"Stranger");
            Text(40,615,170,35,role.ToUpperInvariant(),new GUIStyle(title){fontSize=22,alignment=TextAnchor.MiddleLeft});
            Text(210,615,100,35,SaloonHealth.Percentage(hp),new GUIStyle(title){fontSize=30,alignment=TextAnchor.MiddleRight});
            const float gap=5,width=(270-gap*(SaloonHealth.Segments-1))/SaloonHealth.Segments;
            for(int i=0;i<SaloonHealth.Segments;i++)
            {
                var segment=new Rect(40+i*(width+gap),654,width,16);
                Fill(segment,new Color(.95f,.86f,.68f,.22f));
                if(!Game.IsSpectating&&Game.HealthDelta<0&&Time.unscaledTime<Game.HealthDeltaUntil)
                {
                    float age=2.5f-(Game.HealthDeltaUntil-Time.unscaledTime);
                    float ghost=hp-Game.HealthDelta*(1-Mathf.SmoothStep(0,1,(age-.16f)/.36f));
                    var lost=segment;lost.width*=Mathf.Clamp01((ghost-i*SaloonHealth.RevolverDamage)/SaloonHealth.RevolverDamage);
                    Fill(lost,new Color(1,.79f,.56f,.85f));
                }
                segment.width*=Mathf.Clamp01((hp-i*SaloonHealth.RevolverDamage)/(float)SaloonHealth.RevolverDamage);
                Fill(segment,healthColor);
            }
            if(ShowsCigaretteBuff)
                Text(40,676,430,22,"CIGARETTE BUFF · NEXT SHOT OR BEER x2",subtitle);
            if(Game.OutsideForSmoke)
            {
                Text(1000,615,240,24,"TAKE A BREATHER",new GUIStyle(title){fontSize=20,alignment=TextAnchor.MiddleRight});
                Text(1000,650,240,24,"Left-click  /  Take a drag",new GUIStyle(small){alignment=TextAnchor.MiddleRight});
            }
            else if(!Game.IsSpectating&&view.UnlimitedAmmo)
            {
                var ammoStyle=new GUIStyle(body){fontSize=21,alignment=TextAnchor.MiddleRight};
                const string label="Unlimited bullets";
                float labelWidth=Mathf.Ceil(ammoStyle.CalcSize(new GUIContent(label)).x);
                Graphic(new Rect(1240-labelWidth-26,621,23,43),cartridgeIcon,new Color(.98f,.76f,.37f));
                Text(1240-labelWidth,621,labelWidth,43,label,ammoStyle);
            }
            else if(!Game.IsSpectating)
            {
                int capacity=Game.tuning.cylinder;
                float start=1240-((capacity-1)*24+23);
                for(int i=0;i<capacity;i++)
                {
                    bool loaded=i<view.ammo;
                    Graphic(new Rect(start+i*24,618,23,43),loaded?cartridgeIcon:emptyCartridgeIcon,
                        loaded?new Color(.98f,.76f,.37f):new Color(.96f,.88f,.72f,.65f));
                }
                if(view.carriedBeer>0)Text(1010,665,240,20,"BEER +"+SaloonHealth.BeerHealingAmount(view.beerHealing,view.cigaretteBuff)+"%  /  B drink · G offer",new GUIStyle(small){alignment=TextAnchor.MiddleRight});
            }
        }

        void RoleCard(Role role)=>RoleStoryMap(role);

    }
}
