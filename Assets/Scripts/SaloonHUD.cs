using System;
using System.Linq;
using Unity.Services.Vivox;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD : MonoBehaviour
    {
        public SaloonGame Game;
        GUIStyle title,subtitle,body,small,button,field,roleStyle;
        readonly Color ink=new Color(.92f,.94f,.95f), gold=new Color(.56f,.77f,.79f), muted=new Color(.64f,.70f,.74f);
        bool cluesOpen;
        Vector2 scroll;
        float menuContentHeight=1400;
        bool online;
        float disconnectCopiedUntil,noticeAt;
        string lastNotice="";
        Vector2 evidenceScroll;
        Texture2D coinTexture;

        void Styles()
        {
            if(title!=null)return;
            BuildTheme();
            coinTexture=new Texture2D(32,32,TextureFormat.RGBA32,false);
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)coinTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(16-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(16,16)))));
            coinTexture.Apply();
        }

        void OnGUI()
        {
            Styles();GUI.skin=flatSkin;float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            if(!Game.InRoom){MainMenu();return;}
            if(Game.HitFlash>0)Panel(new Rect(0,0,1280,720),new Color(.8f,.08f,.02f,Game.HitFlash));
            Gameplay();if(Game.MenuOpen)Pause();
        }

        void Panel(Rect rect,Color color)
        {
            if(rect.width>=1280){Fill(rect,color.r>.5f?color:paper);return;}
            if(rect.height<=3){Fill(rect,gold);return;}
            Fill(rect,paper);
        }
        void Text(float x,float y,float width,float height,string text,GUIStyle style=null)
        {bool enabled=GUI.enabled;GUI.enabled=true;GUI.Label(new Rect(x,y,width,height),text,style??body);GUI.enabled=enabled;}
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
            Panel(new Rect(0,0,630,720),new Color(.045f,.036f,.029f,.94f));
            Panel(new Rect(64,69,50,3),gold);
            Text(64,94,500,68,"LAST CALL",title);
            Text(67,157,500,26,"4–5 PLAYERS  /  BETA "+Application.version,subtitle);
            Text(67,199,485,55,"Everyone has a revolver.\nOnly one badge tells the truth.");
            Text(67,271,120,25,"YOUR NAME",subtitle);
            Game.PlayerName=GUI.TextField(new Rect(67,300,480,40),Game.PlayerName,20,field);
            if(Button(67,359,480,44,"SOLO PRACTICE",true)){Game.SaveName();Game.HostLan(true);}
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
            var sheriff=view.players.FirstOrDefault(p=>p.sheriff);
            if(view.phase!=Phase.Lobby&&sheriff!=null)
            {
                Text(28,24,280,18,sheriff.alive?"SHERIFF":"SHERIFF · ELIMINATED",subtitle);
                Text(28,45,280,24,sheriff.name+(sheriff.id==Game.LocalId?" (you)":""),small);
            }
            string phaseLabel=view.phase==Phase.Lobby?"LOBBY":view.phase==Phase.Discussion?"DISCUSSION":view.phase==Phase.Turns?"ROOM ACTIONS":view.phase==Phase.Regroup?"REGROUP":view.phase==Phase.Aftermath?"AFTERMATH":view.phase==Phase.Finished?"MATCH FINISHED":"CHOOSE ROOMS";
            Text(460,24,360,20,"ROUND "+view.round+"  /  "+phaseLabel,new GUIStyle(subtitle){alignment=TextAnchor.MiddleCenter});
            string status=view.phase==Phase.Discussion?view.players.Count(p=>p.alive&&p.ready)+" / "+view.players.Count(p=>p.alive)+" ready":view.phase==Phase.Lobby?view.players.Length+" players":TimeSpan.FromSeconds(view.remaining).ToString(@"mm\:ss");
            Text(460,46,360,22,status,new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
            Vitals();
            if(view.phase!=Phase.Discussion)cluesOpen=false;
            if(!Game.MenuOpen&&Game.Self!=null&&Game.Self.alive)
            {
                Text(630,344,24,28,"·",new GUIStyle(body){normal={textColor=Color.white}});
                if(!string.IsNullOrEmpty(Game.InteractionHint)){Text(430,623,420,32,Game.InteractionHint,new GUIStyle(body){alignment=TextAnchor.MiddleCenter});}
            }
            if(!Game.MenuOpen)
            {
                if(!string.IsNullOrEmpty(view.objectiveNotice)&&view.phase!=Phase.Discussion){Panel(new Rect(28,126,345,85),paper);Text(44,140,313,58,view.objectiveNotice,body);}

            }
            string notice=Game.Connection.WaitingForSnapshot?"Connection delayed — waiting for host ("+Game.Connection.SnapshotAge.ToString("0")+"s)":Game.Self?.alive==false&&!Game.Self.observer?"ELIMINATED · Entering spectator mode shortly":Game.IsSpectating?"":view.phase==Phase.Aftermath?"":view.busyRemaining>0?$"{view.busyRemaining:0.0}s":view.notice;
            if(notice!=lastNotice){lastNotice=notice;noticeAt=Time.unscaledTime;}
            if(!string.IsNullOrEmpty(notice)&&(Time.unscaledTime-noticeAt<4||Game.Connection.WaitingForSnapshot||view.busyRemaining>0||Game.Self?.alive==false)){Text(412,585,416,32,notice,new GUIStyle(small){alignment=TextAnchor.MiddleCenter});}
            string voiceLabel=Game.Voice.Transmitting?"●  Speaking":Game.Voice.Muted?"Microphone muted":Game.Voice.Connected?"V  Talk":"Voice offline";
            Text(28,688,300,20,voiceLabel,Game.Voice.Transmitting?subtitle:small);
            Text(980,688,274,20,view.ownRole==Role.Sheriff.ToString()?"Tab  Role & clues     Esc  Menu":"Tab  Role     Esc  Menu",new GUIStyle(small){alignment=TextAnchor.MiddleRight});
            if(!Game.MenuOpen&&!Game.ShowRole&&Game.Self?.alive==true)
            {
                if(view.phase==Phase.Discussion&&view.ownRole!=Role.Sheriff.ToString())EvidenceCard();
                if(view.phase==Phase.Discussion||view.phase==Phase.Choosing)Invitations();
                if(view.phase==Phase.Choosing||(view.phase==Phase.Discussion&&Game.RoomPlannerOpen))RoomChoices();
                if(view.phase==Phase.Turns&&view.duel!=null)DuelCard();
                if(view.phase==Phase.Turns&&view.roomTurn!=null)RoomTurnCard();
            }
            if(Game.IsSpectating&&!Game.MenuOpen)
            {

                Text(450,635,380,22,"SPECTATING  /  "+Game.CameraPlayer.name,subtitle);
                if(Button(450,665,180,27,"← PREVIOUS"))Game.CycleSpectator(-1);
                if(Button(650,665,180,27,"NEXT →"))Game.CycleSpectator(1);

            }
            if(Game.Self!=null&&!Game.Self.alive&&!Game.IsSpectating&&view.phase!=Phase.Finished&&Game.ShowEliminationOverlay)
            {Panel(new Rect(390,280,500,110),paper);Text(420,294,440,42,"ELIMINATED",new GUIStyle(roleStyle){fontSize=28});Text(420,343,440,30,"Your voice is separated from living players.",small);}
            if(Game.ShowRole&&!Game.MenuOpen)
            {
                Enum.TryParse(view.ownRole,out Role role);
                RoleCard(role);
            }
            if(view.phase==Phase.Finished&&!Game.MenuOpen)
            {
                Panel(new Rect(350,195,580,320),new Color(.04f,.035f,.03f,.97f));
                Text(375,220,530,80,view.outcome,new GUIStyle(roleStyle){fontSize=26,wordWrap=true});
                int row=0;foreach(var p in view.players)Text(400,310+row++*30,500,28,$"{p.name}  /  {p.revealedRole}  /  {(p.alive?"survived":"eliminated")}",small);
                Text(400,474,500,30,"Esc → lobby to start the next match",subtitle);
            }
        }

        void Vitals()
        {
            var view=Game.View;int hp=Mathf.Clamp(Game.CameraPlayer?.health??view.health,0,12);
            int max=Mathf.Clamp(Game.CameraPlayer?.maxHealth??Game.tuning.health,1,12);
            Text(28,632,220,18,Game.IsSpectating?"SPECTATING · HEALTH":"HEALTH",subtitle);
            for(int i=0;i<max;i++)
            {
                var rect=new Rect(28+i*35,657,28,7);Fill(rect,new Color(.16f,.19f,.22f));
                if(i<hp)Fill(rect,hp==1?new Color(.91f,.54f,.43f):new Color(.68f,.85f,.76f));
            }
            Text(28+max*35+5,648,90,25,hp+" / "+max,small);
            if(!Game.IsSpectating)
            {
                Text(1080,632,174,18,"AMMO",new GUIStyle(subtitle){alignment=TextAnchor.MiddleRight});
                Text(1080,650,174,30,view.ammo+" / 6"+(view.carriedBeer>0?"   ·   Beer":""),new GUIStyle(body){fontSize=22,alignment=TextAnchor.MiddleRight});
            }
        }

        void EvidenceCard()
        {
            var clues=Game.View.evidence??Array.Empty<string>();
            if(Button(28,92,220,28,"Clues  ·  "+clues.Length+(cluesOpen?"   −":"   +")))cluesOpen=!cluesOpen;
            if(!cluesOpen)return;
            float viewport=Mathf.Min(240,Mathf.Max(60,clues.Length*96));
            Panel(new Rect(28,128,280,viewport+54),paper);
            Text(40,138,250,18,"ONLY YOU CAN SEE THESE",subtitle);
            evidenceScroll=GUI.BeginScrollView(new Rect(40,166,256,viewport),evidenceScroll,new Rect(0,0,240,Mathf.Max(60,clues.Length*96)));
            if(clues.Length==0)Text(0,0,230,60,"No clues yet. Watch the safe, or inspect it as Sheriff.",small);
            for(int i=0;i<clues.Length;i++)
            {
                Text(0,i*96,230,80,clues[clues.Length-1-i],new GUIStyle(body){fontSize=14});
                Fill(new Rect(0,i*96+88,230,1),rule);
            }
            GUI.EndScrollView();
        }

        void RoleCard(Role role)
        {
            var guide=SaloonRules.Guide(role,Game.View.players.Length);
            var help=new GUIStyle(body){fontSize=15,richText=true};
            Panel(new Rect(218,122,844,546),new Color(.045f,.035f,.028f,.99f));
            Text(248,140,784,45,role.ToString().ToUpperInvariant(),roleStyle);
            Text(250,194,780,44,guide.Goal,body);
            Text(250,250,355,24,"WHAT TO PRESS ON YOUR TURN",subtitle);
            Text(645,250,385,24,role==Role.Sheriff?"YOUR CLUES · PRIVATE":guide.PlanTitle,subtitle);
            Text(250,279,355,222,guide.Actions+"\n\n<b>Q</b> draw, aim at the head + <b>left-click</b> shoot.\n<b>R</b> reload.\n<b>E</b> take a beer at Bar (free).\n<b>B / G</b> drink / give your beer.\n<b>Space</b> pass your turn.",help);
            if(role==Role.Sheriff)
            {
                var clues=Game.View.evidence??Array.Empty<string>();
                if(clues.Length==0)Text(645,279,385,222,"No clues yet.\n\nAt the Bar, press C on your turn to inspect the safe. Your findings appear here.\n\nYou can also notice someone working the safe by watching them.",help);
                else
                {
                    evidenceScroll=GUI.BeginScrollView(new Rect(645,279,385,222),evidenceScroll,new Rect(0,0,362,clues.Length*80));
                    for(int i=0;i<clues.Length;i++)
                    {
                        Text(0,i*80,352,70,clues[clues.Length-1-i],help);
                        Fill(new Rect(0,i*80+74,352,1),rule);
                    }
                    GUI.EndScrollView();
                }
            }
            else Text(645,279,385,222,guide.Plan,help);
            Text(250,514,780,22,"HOW YOU WIN",subtitle);
            Text(250,538,780,43,guide.Win,help);
            Text(250,590,780,36,guide.Warning,new GUIStyle(help){normal={textColor=gold}});
            Text(250,637,780,22,"Headshots only. F stand / sit, WASD dodge in your room. One action per round. Release Tab to close.",small);
        }

        void RoomChoices()
        {
            Game.RoomChooserRenderFrames++;var self=Game.Self;
            const float x=1000,w=254;
            Text(x,92,w,22,"YOUR PLAN",subtitle);
            Text(x,120,w,20,"Room",small);
            for(int room=0;room<3;room++)
                if(Button(x+room*86,144,82,29,new[]{"Bar","West","East"}[room],Game.PlannedRoom==room))Game.ChooseRoom(room,Game.PlannedCompanion);
            Text(x,187,w,20,"Partner",small);
            if(Button(x,211,w,26,"Go alone",string.IsNullOrEmpty(Game.PlannedCompanion)))Game.ChooseRoom(Game.PlannedRoom,"");
            int row=0;
            foreach(var player in Game.View.players.Where(p=>p.id!=self.id&&p.alive))
            {
                bool invited=player.companion==self.id;
                string label=player.name+(invited?" · invites you":"");
                if(Button(x,243+row++*31,w,27,label,Game.PlannedCompanion==player.id))Game.ChooseRoom(invited?player.chosenRoom:Game.PlannedRoom,player.id);
            }
            float y=251+row*31;
            if(Button(x,y,w,34,Game.PlanReady?"Ready · click to undo":"Ready",Game.PlanReady))Game.ToggleReady();
            Text(x,y+40,w,35,Game.PlanPending?"Saving…":"Everyone must be ready.",small);
        }

        void Invitations()
        {
            if(Game.RoomPlannerOpen||Game.View.phase==Phase.Choosing)return;
            int row=0;
            foreach(var invite in Game.View.players.Where(p=>p.alive&&p.companion==Game.LocalId).Take(1))
            {
                float y=548+row++*52;Panel(new Rect(28,y,345,56),new Color(.045f,.035f,.028f,.75f));
                Text(42,y+6,315,20,invite.name+" invites you",subtitle);
                Text(42,y+28,315,20,SaloonStations.Name(invite.chosenRoom)+"  ·  P to plan",small);
            }
        }

        void DuelCard()
        {
            var duel=Game.View.duel;const float x=1000;
            bool choosing=duel.stage=="choosing",flipping=duel.stage=="flipping";
            Text(x,92,254,22,choosing?"CHOOSE YOUR SIDE":flipping?"COIN TOSS":duel.firstName+" opens",subtitle);
            if(choosing)
            {
                Text(x,119,254,22,"Against "+duel.opponent+" · "+duel.remaining.ToString("0")+"s",small);
                if(Button(x,150,122,29,"Heads",duel.side=="HEADS"))Game.ChooseCoin(0);
                if(Button(x+132,150,122,29,"Tails",duel.side=="TAILS"))Game.ChooseCoin(1);
            }
            else if(flipping)
            {
                float width=4+Mathf.Abs(Mathf.Cos(Time.unscaledTime*24))*20;var previous=GUI.color;GUI.color=gold;
                GUI.DrawTexture(new Rect(x+127-width/2,124,width,24),coinTexture);GUI.color=previous;
            }
            else Text(x,119,254,50,duel.waitingForOpening?"Waiting for opening · "+duel.remaining.ToString("0")+"s":duel.yourTurn?"Your move · Q draw · Space pass":"Action spent",small);
        }

        void RoomTurnCard()
        {
            var turn=Game.View.roomTurn;
            Text(1000,92,254,22,string.IsNullOrEmpty(turn.currentId)?"ROOM COMPLETE":turn.yourTurn?"YOUR TURN":turn.currentName,subtitle);
            Text(1000,118,254,35,string.IsNullOrEmpty(turn.currentId)?"":turn.yourTurn?"Act or press Space to pass · "+turn.remaining.ToString("0")+"s":"Waiting · "+turn.remaining.ToString("0")+"s",small);
        }

        void Pause()
        {
            Panel(new Rect(0,0,1280,720),paper);
            Panel(new Rect(260,30,760,660),new Color(.055f,.045f,.034f,.98f));
            Text(295,50,600,50,"LAST CALL  /  BETA",roleStyle);
            scroll=GUI.BeginScrollView(new Rect(290,115,700,500),scroll,new Rect(0,0,670,menuContentHeight));
            Text(0,0,660,40,!string.IsNullOrEmpty(Game.Connection.RoomCode)?"ROOM CODE  "+Game.Connection.RoomCode:"LAN  /  Host shares their local IP and port "+Game.Port,subtitle);
            int y=45;
            if(Game.View!=null)foreach(var p in Game.View.players){Text(0,y,650,30,p.name+(p.bot?"  [BOT]":"")+(Game.View.phase==Phase.Turns?"  /  "+SaloonStations.Name(p.room):"")+(p.sheriff?"  [SHERIFF]":"")+(!p.alive?"  [ELIMINATED]":""));y+=31;}
            if(Game.Hosting)
            {
                bool editable=Game.View!=null&&(Game.View.phase==Phase.Lobby||Game.View.phase==Phase.Finished);
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
                GUI.enabled=Game.CanStartMatch;
                if(Button(0,y,640,42,$"DEAL ROLES & START {size}-PLAYER MATCH",true))
                {Game.StartMatch();if(Game.CurrentPhase==Phase.Discussion)Game.MenuOpen=false;}
                y+=55;GUI.enabled=editable;
                Text(0,y,650,32,"Discussion ends when every living player is Ready.",small);y+=40;
                Text(0,y,650,26,$"Shared action window: {Game.tuning.turnSeconds:0} seconds",small);y+=28;
                Game.tuning.turnSeconds=Mathf.Round(GUI.HorizontalSlider(new Rect(0,y,640,25),Game.tuning.turnSeconds,5,60));y+=33;
                Text(0,y,650,26,$"Round limit: {Game.tuning.roundLimit}",small);y+=28;
                Game.tuning.roundLimit=Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(0,y,640,25),Game.tuning.roundLimit,1,12));y+=33;
                Text(0,y,650,26,$"Shared beer supply: {Game.tuning.sharedBeers}",small);y+=28;
                Game.tuning.sharedBeers=Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(0,y,640,25),Game.tuning.sharedBeers,0,16));y+=32;
                GUI.enabled=true;y+=8;
            }
            else if(Game.View!=null)
            {
                Text(0,y,650,52,Game.View.botsEnabled?$"Bots enabled · fill to {Game.View.botMatchSize} players. The host starts the match.":"No bots · 4 or 5 human players required. The host starts the match.",small);y+=62;
            }
            Text(0,y,650,100,"Mouse  Look / aim  ·  Q  Draw / holster  ·  Hold right mouse during planning\nWASD  Move in your room  ·  F  Stand / sit  ·  Click  Headshots only\nR  Reload  ·  E  Beer  ·  B  Drink  ·  G  Give beer  ·  H  Safe  ·  X  Escape  ·  C  Inspect\nSpace  Ready / pass  ·  ← / →  Spectate  ·  Hold V  Talk  ·  M  Mute  ·  Hold Tab  Role",small);y+=110;
            Text(0,y,650,28,"VOICE",subtitle);y+=31;
            GUI.enabled=!Game.Voice.Busy;
            if(Button(0,y,310,38,Game.Voice.Connected?"VOICE CONNECTED":"CONNECT VOICE"))Game.Voice.Connect();
            if(Button(325,y,315,38,"MICROPHONE ECHO TEST"))Game.Voice.EchoTest();
            GUI.enabled=true;y+=49;
            Text(0,y,640,75,Game.Voice.Status,small);y+=80;
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
            Text(0,y,640,120,"Law: defend the money through the round cap or remove all criminals.\nOutlaws: only Outlaws can rob and escape with the money.\nRenegade: eliminate Outlaws first, then Law; be the last alive.\nSheriff death does not end the match. A disconnect aborts it.",small);
            menuContentHeight=y+130;
            GUI.EndScrollView();
            if(Button(295,630,330,40,"RETURN TO SALOON"))Game.MenuOpen=false;
            if(Button(645,630,330,40,"LEAVE ROOM"))Game.Leave();
        }
    }
}
