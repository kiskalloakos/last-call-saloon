using System;
using System.Linq;
using Unity.Services.Vivox;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD : MonoBehaviour
    {
        public SaloonGame Game;
        public bool ShowsCigaretteBuff=>Game.Self?.alive==true&&!Game.IsSpectating&&Game.View.cigaretteBuff;
        GUIStyle title,subtitle,body,small,button,field,roleStyle;
        readonly Color ink=new Color(.19f,.13f,.09f), gold=new Color(.54f,.27f,.12f), muted=new Color(.39f,.31f,.23f);
        Vector2 scroll;
        float menuContentHeight=1400;
        bool online;
        float disconnectCopiedUntil,noticeAt;
        string lastNotice="";
        Vector2 evidenceScroll;

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
                Panel(new Rect(290,230,700,220),paper);
                Text(320,255,640,70,"Connection lost — reconnecting",roleStyle);
                Text(320,330,640,45,"Your place is reserved · "+Mathf.CeilToInt(Game.Connection.ReconnectRemaining)+"s remaining",subtitle);
                if(Button(320,390,640,40,"LEAVE ROOM"))Game.Leave();
                return;
            }
            if(Game.GraphicsMenuOpen){GraphicsMenu();return;}
            if(!Game.InRoom){MainMenu();return;}
            if(Game.RoleIntroActive&&!Game.MenuOpen)
            {
                // Only expose the world as the black screen fades away.
                if(Game.RoleIntroElapsed>SaloonGame.RoleIntroDuration-.8f)
                {bool enabled=GUI.enabled;GUI.enabled=false;Gameplay();GUI.enabled=enabled;}
                RoleIntro();return;
            }
            if(Game.HitFlash>0)Panel(new Rect(0,0,1280,720),new Color(.8f,.08f,.02f,Game.HitFlash));
            Gameplay();BartenderArrivalNotice();if(Game.MenuOpen)Paper(Pause);
            if(!string.IsNullOrEmpty(Game.View?.reconnectNotice))
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
        bool Button(float x,float y,float width,float height,string label,bool selected=false)
        {
            selected|=label.StartsWith("✓ ");label=label.Replace("✓ ","");
            var rect=new Rect(x,y,width,height);
            bool enabled=GUI.enabled;GUI.enabled=true;
            bool clicked=GUI.Button(rect,label,!enabled?disabledButton:selected?selectedButton:button);GUI.enabled=enabled;
            if(Event.current.type==EventType.Repaint)Outline(rect,selected?gold:rule);
            return clicked&&enabled;
        }

        void MainMenu()
        {
            if(Button(1000,32,220,38,"GRAPHICS SETTINGS"))OpenGraphics();
            Panel(new Rect(0,0,630,720),new Color(.045f,.036f,.029f,.94f));
            Panel(new Rect(64,69,50,3),gold);
            Text(64,94,500,68,"LAST CALL",title);
            Text(67,157,500,26,"4–5 PLAYERS  /  BETA "+Application.version,subtitle);
            Text(67,199,485,55,"Everyone has a revolver.\nOnly one badge tells the truth.");
            Text(67,271,120,25,"YOUR NAME",subtitle);
            Game.PlayerName=GUI.TextField(new Rect(67,300,480,40),Game.PlayerName,20,field);
            if(Button(67,359,480,44,"SOLO PRACTICE",true)){Game.SaveName();Game.StartSolo();}
            if(Button(67,418,233,38,"LOCAL / LAN",!online))online=false;
            if(Button(314,418,233,38,"ONLINE ROOM",online))online=true;
            if(!online)
            {
                Text(67,471,310,20,"HOST ADDRESS",subtitle);Text(398,471,145,20,"UDP PORT",subtitle);
                Game.Address=GUI.TextField(new Rect(67,496,310,38),Game.Address,64,field);
                Game.Port=GUI.TextField(new Rect(398,496,149,38),Game.Port,5,field);
                GUI.enabled=!Game.Connection.Busy;
                if(Button(67,549,233,42,"HOST LAN")){Game.SaveName();Game.HostLan(false);}
                if(Button(314,549,233,42,"JOIN LAN")){Game.SaveName();Game.JoinLan();}
                GUI.enabled=true;
                Text(67,601,150,25,"HOST WITH",subtitle);
                if(Button(219,596,156,32,(Game.BotsEnabled?"":"✓ ")+"NO BOTS"))Game.SetBotsEnabled(false);
                if(Button(389,596,158,32,(Game.BotsEnabled?"✓ ":"")+"BOTS"))Game.SetBotsEnabled(true);
            }
            else
            {
                Text(67,456,480,22,"ROOM CODE",subtitle);
                Game.JoinCode=GUI.TextField(new Rect(67,486,480,38),Game.JoinCode,12,field).ToUpperInvariant();
                GUI.enabled=!Game.Connection.Busy;
                if(Button(67,541,233,42,"CREATE ROOM")){Game.SaveName();Game.Cloud(true);}
                if(Button(314,541,233,42,"JOIN CODE")){Game.SaveName();Game.Cloud(false);}
                GUI.enabled=true;
                Text(67,589,480,32,"Private room codes work across different networks.",small);
            }
            bool details=!string.IsNullOrEmpty(Game.Connection.LastDisconnectDetails);
            Text(67,641,480,details?42:65,Game.Connection.Status,small);
            if(details&&GUI.Button(new Rect(67,686,480,28),Time.unscaledTime<disconnectCopiedUntil?"DISCONNECT DETAILS COPIED":"COPY DISCONNECT DETAILS",new GUIStyle(button){fontSize=12,padding=new RectOffset(4,4,2,2)}))
            {GUIUtility.systemCopyBuffer=Game.Connection.LastDisconnectDetails;disconnectCopiedUntil=Time.unscaledTime+2;}
            Fill(new Rect(700,520,520,160),paper);
            Text(727,547,440,30,"TALK FIRST. DRAW CAREFULLY.",subtitle);
            Text(727,583,420,65,"4: Sheriff, Deputy, Outlaw, Renegade.\n5: one extra Outlaw. Lobbies hold up to 8; larger match rules come later.",body);
        }

        void Gameplay()
        {
            var view=Game.View;if(view==null){Text(40,30,650,100,Game.Connection.Status);return;}
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
            if(view.phase==Phase.Discussion&&!Game.MenuOpen&&!Game.ShowRole)
                Text(430,82,420,26,Game.RoomPlannerOpen?"Right-click · Close planner":"Right-click · Plan your room    Space · Lock / unlock",new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
            IncomingInvitationNotice();
            if(!Game.MenuOpen&&Game.Voice!=null)
                Text(890,22,354,26,Game.Voice.MicrophoneStatus,new GUIStyle(small){alignment=TextAnchor.MiddleRight});
            Vitals();
            BartenderMoodMeter();
            if(!Game.MenuOpen&&Game.Self!=null&&Game.Self.alive)
            {
                if(!Game.OutsideForSmoke&&!Game.PlayingTableGame)
                {var aim=new Color(.95f,.91f,.83f,.65f);Fill(new Rect(634,359,4,1),aim);Fill(new Rect(643,359,4,1),aim);Fill(new Rect(640,353,1,4),aim);Fill(new Rect(640,362,1,4),aim);}
                if(!Game.PlayingTableGame&&Game.SafeHighlight?.Highlighted!=true&&!string.IsNullOrEmpty(Game.InteractionHint)){Text(430,623,420,32,Game.InteractionHint,new GUIStyle(body){alignment=TextAnchor.MiddleCenter});}
                SafeInteractionPrompt();
            }
            if(!Game.MenuOpen)
            {
                if(!string.IsNullOrEmpty(view.objectiveNotice)&&view.phase!=Phase.Discussion)Text(36,107,300,58,view.objectiveNotice,body);

            }
            string notice=Game.Connection.WaitingForSnapshot?"Connection delayed — waiting for host ("+Game.Connection.SnapshotAge.ToString("0")+"s)":Game.Self?.alive==false&&!Game.Self.observer?"ELIMINATED · Entering spectator mode shortly":Game.IsSpectating?"":view.phase==Phase.Aftermath?"":view.busyRemaining>0?$"{view.busyRemaining:0.0}s":view.notice;
            if(notice!=lastNotice){lastNotice=notice;noticeAt=Time.unscaledTime;}
            if(!Game.PlayingTableGame&&!string.IsNullOrEmpty(notice)&&(Time.unscaledTime-noticeAt<4||Game.Connection.WaitingForSnapshot||view.busyRemaining>0||Game.Self?.alive==false)){Text(404,580,472,32,notice,new GUIStyle(small){alignment=TextAnchor.MiddleCenter});}
            if(!Game.MenuOpen&&!Game.ShowRole&&Game.Self?.alive==true)
            {
                if(view.phase==Phase.Choosing||(view.phase==Phase.Discussion&&Game.RoomPlannerOpen))Paper(RoomChoices);
            }
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
            if(Game.ShowRole&&!Game.MenuOpen)
            {
                Enum.TryParse(view.ownRole,out Role role);
                Paper(()=>RoleCard(role));
            }
            if(view.phase==Phase.Finished&&!Game.MenuOpen&&!Game.ShowRole)
            {
                worldHud=false;
                Panel(new Rect(350,140,580,470),new Color(.04f,.035f,.03f,.97f));
                Text(375,165,530,80,view.outcome,new GUIStyle(roleStyle){fontSize=26,wordWrap=true});
                int row=0;foreach(var p in view.players)Text(400,255+row++*30,500,28,$"{p.name}  /  {p.revealedRole}  /  {(p.alive?"survived":"eliminated")}",small);
                Text(375,420,530,30,$"Rematch votes: {view.rematchVotes} / {view.rematchRequired}",subtitle);
                if(Button(375,460,530,42,view.ownRematchVote?"WITHDRAW REMATCH VOTE":"VOTE FOR REMATCH",view.ownRematchVote))Game.Act("rematch");
                if(Button(375,515,530,42,"RETURN EVERYONE TO LOBBY"))Game.Act("lobby");
                Text(375,565,530,30,"All players must vote to start another match.",small);
            }
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

        void Pause()
        {
            Panel(new Rect(0,0,1280,720),paper);
            Panel(new Rect(260,30,760,660),new Color(.055f,.045f,.034f,.98f));
            Text(295,50,460,50,"LAST CALL  /  BETA",roleStyle);
            if(Button(775,55,200,38,"GRAPHICS SETTINGS"))OpenGraphics();
            scroll=GUI.BeginScrollView(new Rect(290,115,700,500),scroll,new Rect(0,0,670,menuContentHeight));
            Text(0,0,660,40,!string.IsNullOrEmpty(Game.Connection.RoomCode)?"ROOM CODE  "+Game.Connection.RoomCode:"LAN  /  Host shares their local IP and port "+Game.Port,subtitle);
            int y=45;
            if(Game.View!=null)foreach(var p in Game.View.players){Text(0,y,650,30,p.name+(p.bot?"  [BOT]":"")+(Game.View.phase==Phase.Turns?"  /  "+SaloonStations.Name(p.room):"")+(p.sheriff?"  [SHERIFF]":"")+(!p.alive?"  [ELIMINATED]":""));y+=31;}
            if(Game.Hosting)
            {
                bool editable=Game.View?.phase==Phase.Lobby;
                y+=12;GUI.enabled=editable;
                Text(0,y,650,25,$"{Game.HumanCount} human players / {SaloonRules.LobbyCapacity} lobby places",subtitle);y+=31;
                if(Button(0,y,310,35,(Game.BotsEnabled?"":"✓ ")+"NO BOTS"))Game.SetBotsEnabled(false);
                if(Button(325,y,315,35,(Game.BotsEnabled?"✓ ":"")+"WITH BOTS"))Game.SetBotsEnabled(true);
                y+=45;
                if(Game.BotsEnabled)
                {
                    Text(0,y,650,25,"Fill empty seats when the match starts:",small);y+=29;
                    if(Button(0,y,310,35,(Game.BotMatchSize==4?"✓ ":"")+"4 PLAYERS"))Game.SetBotMatchSize(4);
                    if(Button(325,y,315,35,(Game.BotMatchSize==5?"✓ ":"")+"5 PLAYERS"))Game.SetBotMatchSize(5);
                    y+=45;
                }
                int size=Game.PlannedMatchSize;
                Text(0,y,650,42,size==4?"4 players: Sheriff, Deputy, 1 Outlaw, Renegade.":size==5?"5 players: Sheriff, Deputy, 2 Outlaws, Renegade.":"Matches need 4 or 5 players. Rules for 6–8 come later.",small);y+=46;
                Text(0,y,650,40,Game.LobbyStartStatus,small);y+=44;
                GUI.enabled=Game.View?.phase==Phase.Lobby&&Game.CanStartMatch;
                if(Button(0,y,640,42,$"DEAL ROLES & START {size}-PLAYER MATCH",true))
                {Game.StartMatch();if(Game.CurrentPhase==Phase.Discussion)Game.MenuOpen=false;}
                y+=55;GUI.enabled=editable;
                Text(0,y,650,32,"Discussion ends on the timer, or early when everyone locks in.",small);y+=40;
                Text(0,y,650,26,$"Shared action window: {Game.tuning.turnSeconds:0} seconds",small);y+=28;
                Game.tuning.turnSeconds=Mathf.Round(GUI.HorizontalSlider(new Rect(0,y,640,25),Game.tuning.turnSeconds,5,60));y+=33;
                Text(0,y,650,26,$"Posse: warning after round {SaloonPosse.WarningRound} · arrives after round {SaloonPosse.ArrivalRound}",small);y+=33;
                Text(0,y,650,26,$"Shared beer supply: {Game.tuning.sharedBeers}",small);y+=28;
                Game.tuning.sharedBeers=Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(0,y,640,25),Game.tuning.sharedBeers,0,16));y+=32;
                GUI.enabled=true;y+=8;
            }
            else if(Game.View!=null)
            {
                Text(0,y,650,52,Game.View.botsEnabled?$"Bots enabled · fill to {Game.View.botMatchSize} players. The host starts the match.":"No bots · 4 or 5 human players required. The host starts the match.",small);y+=62;
            }
            Text(0,y,650,100,"Mouse  Look / aim  ·  Q  Draw / holster  ·  Right-click  Discussion planner\nWASD  Move  ·  Hold Ctrl  Crouch  ·  F  Stand / sit  ·  Click  Headshots only\nR  Reload  ·  E  Pour / stop  ·  B  Drink  ·  G  Give beer  ·  H  Safe  ·  X  Escape  ·  C  Inspect\nSpace  Ready / pass  ·  ← / →  Spectate  ·  V  Talk  ·  T  Open mic  ·  M  Mute  ·  Tab  Role",small);y+=110;
            Text(0,y,650,28,"VOICE",subtitle);y+=31;
            GUI.enabled=!Game.Voice.Busy;
            if(Button(0,y,310,38,Game.Voice.Connected?"VOICE CONNECTED":"CONNECT VOICE"))Game.Voice.Connect();
            if(Button(325,y,315,38,"MICROPHONE ECHO TEST"))Game.Voice.EchoTest();
            GUI.enabled=true;y+=49;
            Text(0,y,640,50,Game.Voice.Status,small);y+=55;
            Text(0,y,640,50,Game.Voice.ControlHint,small);y+=55;
            if(Button(0,y,640,36,Game.Voice.OpenMicrophone?"OPEN MIC ON  /  click for push-to-talk (T)":"PUSH-TO-TALK  /  click to enable open mic (T)",Game.Voice.OpenMicrophone))Game.Voice.OpenMicrophone=!Game.Voice.OpenMicrophone;
            y+=46;
            GUI.enabled=Game.Voice.Connected&&!Game.Voice.Busy;
            if(Button(0,y,640,36,"MICROPHONE: "+Game.Voice.MicrophoneName+"  /  click to change"))Game.Voice.CycleMicrophone();
            y+=46;
            if(Button(0,y,310,34,Game.Voice.Muted?"UNMUTE MICROPHONE":"MUTE MICROPHONE"))Game.Voice.Muted=!Game.Voice.Muted;
            y+=44;GUI.enabled=true;
            if(Game.Voice.Connected&&VivoxService.Instance.ActiveChannels.TryGetValue(Game.Voice.Channel,out var participants))
            {
                foreach(var p in participants)
                {
                    Text(0,y,430,28,p.DisplayName+(p.SpeechDetected?"  • speaking":""),small);
                    if(!p.IsSelf&&Button(440,y,190,28,p.IsMuted?"Unmute":"Mute")){if(p.IsMuted)p.UnmutePlayerLocally();else p.MutePlayerLocally();}
                    y+=34;
                }
            }
            Text(0,y,640,80,"Revolver: TimDev — CC BY 4.0; adapted model/texture.\nSource: opengameart.org/content/colt-lowpoly-revolver\nLicense: creativecommons.org/licenses/by/4.0/",small);y+=95;
            Text(0,y,640,85,"Recorded sound effects: okieactor (revolver shot), Dredile (reload), Q.K. (drinking), Max_Headroom (horses), Resaural (distant gunshot). CC0 1.0 / Freesound. Full sources: Audio/Recorded/License.txt.",small);y+=95;
            Text(0,y,640,120,$"Law: stop the escape and survive until the posse, or eliminate all criminals.\nOutlaws: only Outlaws can rob and escape with the money.\nRenegade: eliminate Outlaws first, then Law; be the last alive.\nSheriff death does not end the match. Posse arrives after round {SaloonPosse.ArrivalRound}. Disconnected places are held for 20 seconds.",small);
            menuContentHeight=y+130;
            GUI.EndScrollView();
            if(Button(295,630,330,40,"RETURN TO SALOON"))Game.MenuOpen=false;
            if(Button(645,630,330,40,"LEAVE ROOM"))Game.Leave();
        }
    }
}
