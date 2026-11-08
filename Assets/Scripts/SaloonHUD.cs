using System;
using System.Linq;
using Unity.Services.Vivox;
using UnityEngine;

namespace Saloon
{
    public class SaloonHUD : MonoBehaviour
    {
        public SaloonGame Game;
        GUIStyle title,subtitle,body,small,button,field,roleStyle;
        readonly Color ink=new Color(.94f,.88f,.76f), gold=new Color(.90f,.65f,.30f), muted=new Color(.65f,.62f,.55f);
        Vector2 scroll;
        bool online;
        float disconnectCopiedUntil;
        Texture2D coinTexture;

        void Styles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){fontSize=48,fontStyle=FontStyle.Bold,normal={textColor=ink}};
            subtitle=new GUIStyle(GUI.skin.label){fontSize=13,normal={textColor=gold}};
            body=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true,normal={textColor=ink}};
            small=new GUIStyle(body){fontSize=13,normal={textColor=muted}};
            button=new GUIStyle(GUI.skin.button){fontSize=16,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(15,15,6,6),normal={textColor=ink},hover={textColor=gold}};
            field=new GUIStyle(GUI.skin.textField){fontSize=17,padding=new RectOffset(10,10,8,8)};
            roleStyle=new GUIStyle(title){fontSize=32,alignment=TextAnchor.MiddleCenter};
            coinTexture=new Texture2D(32,32,TextureFormat.RGBA32,false);
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)coinTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(16-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(16,16)))));
            coinTexture.Apply();
        }

        void OnGUI()
        {
            Styles();float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            if(!Game.InRoom){MainMenu();return;}
            if(Game.HitFlash>0)Panel(new Rect(0,0,1280,720),new Color(.8f,.08f,.02f,Game.HitFlash));
            Gameplay();if(Game.MenuOpen)Pause();
        }

        void Panel(Rect rect,Color color)
        {
            var previous=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=previous;
        }
        void Text(float x,float y,float width,float height,string text,GUIStyle style=null)=>GUI.Label(new Rect(x,y,width,height),text,style??body);
        bool Button(float x,float y,float width,float height,string label)=>GUI.Button(new Rect(x,y,width,height),label,button);

        void MainMenu()
        {
            Panel(new Rect(0,0,630,720),new Color(.045f,.036f,.029f,.94f));
            Panel(new Rect(64,69,50,3),gold);
            Text(64,94,500,68,"LAST CALL",title);
            Text(67,157,500,26,"4–5 PLAYERS  /  BETA "+Application.version,subtitle);
            Text(67,199,485,55,"Everyone has a revolver.\nOnly one badge tells the truth.");
            Text(67,271,120,25,"YOUR NAME",subtitle);
            Game.PlayerName=GUI.TextField(new Rect(67,300,480,40),Game.PlayerName,20,field);
            if(Button(67,359,480,44,"SOLO PRACTICE")){Game.SaveName();Game.HostLan(true);}
            if(Button(67,418,233,38,online?"Local / LAN":"LOCAL / LAN"))online=false;
            if(Button(314,418,233,38,online?"ONLINE ROOM":"Online room"))online=true;
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
            Text(727,547,440,30,"TALK FIRST. DRAW CAREFULLY.",subtitle);
            Text(727,583,420,65,"4: Sheriff, Deputy, Outlaw, Renegade.\n5: one extra Outlaw. Lobbies hold up to 8; larger match rules come later.",body);
        }

        void Gameplay()
        {
            var view=Game.View;if(view==null){Text(40,30,650,100,Game.Connection.Status);return;}
            var sheriff=view.players.FirstOrDefault(p=>p.sheriff);
            if(view.phase!=Phase.Lobby&&sheriff!=null)
            {
                Panel(new Rect(26,22,330,88),new Color(.04f,.035f,.03f,.9f));
                Text(44,32,294,24,sheriff.alive?"SHERIFF":"SHERIFF · ELIMINATED",subtitle);
                Text(44,61,294,28,sheriff.name+(sheriff.id==Game.LocalId?" (YOU)":""),body);
            }
            Panel(new Rect(452,22,376,88),new Color(.04f,.035f,.03f,.82f));
            string phaseLabel=view.phase==Phase.Lobby?"WAITING FOR PLAYERS":view.phase==Phase.Discussion?$"ROUND {view.round}  /  DISCUSSION":view.phase==Phase.Turns?$"ROUND {view.round}  /  ROOM ACTIONS":view.phase==Phase.Choosing?"CHOOSE ROOMS":view.phase==Phase.Regroup?"REGROUPING":view.phase==Phase.Aftermath?"LET THE DUST SETTLE":"MATCH FINISHED";
            var centered=new GUIStyle(subtitle){alignment=TextAnchor.MiddleCenter};
            Text(468,32,344,24,phaseLabel,centered);
            string actionLabel=Game.Self?.alive==false?"Eliminated":view.duel!=null&&view.duel.stage!="playing"?"Coin toss":view.duel?.waitingForOpening==true?"Waiting for opening":view.roomTurn?.spent==true?"Action spent":view.roomTurn!=null&&!view.roomTurn.yourTurn?"Waiting for turn":view.actionAvailable?"One action":"Action spent";
            string turnLabel=view.phase==Phase.Lobby?$"{view.players.Length} / {view.lobbyCapacity}":view.phase==Phase.Turns?(SaloonStations.Name(Game.CameraPlayer?.room??0)+" · "+(Game.IsSpectating?"Spectating":actionLabel)):"";
            string status=view.phase==Phase.Discussion?view.players.Count(p=>p.alive&&p.ready)+" / "+view.players.Count(p=>p.alive)+" READY":$"{TimeSpan.FromSeconds(view.remaining):mm\\:ss}"+(turnLabel.Length>0?"  ·  "+turnLabel:"");
            Text(468,61,344,28,status,new GUIStyle(body){alignment=TextAnchor.MiddleCenter});
            Panel(new Rect(1074,22,180,88),new Color(.04f,.035f,.03f,.82f));
            string health=new string('●',Mathf.Clamp(Game.CameraPlayer?.health??view.health,0,12))+new string('○',Mathf.Max(0,Mathf.Min(12,Game.tuning.health)-(Game.CameraPlayer?.health??view.health)));
            Text(1092,33,150,26,health,subtitle);
            Text(1092,65,150,26,Game.IsSpectating?"PLAYER POV":$"{view.ammo} / 6"+(view.carriedBeer>0?"  ·  Beer":""),small);
            if(!Game.MenuOpen&&Game.Self!=null&&Game.Self.alive)
            {
                Text(630,344,24,28,"·",body);
                Text(430,545,420,32,Game.InteractionHint,new GUIStyle(body){alignment=TextAnchor.MiddleCenter});
            }
            if(!Game.MenuOpen)
            {
                if(!Game.IsSpectating&&!string.IsNullOrEmpty(view.safeReport))Text(28,120,360,45,"LAST INSPECTION\n"+view.safeReport,small);
                Text(350,590,580,30,view.objectiveNotice,new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
                var hintStyle=new GUIStyle(small){fontSize=13,wordWrap=false};hintStyle.normal.textColor=new Color(.79f,.75f,.66f,.65f);
                Text(28,650,1220,22,SaloonRules.Hint(view,Game.Self),hintStyle);
            }
            string notice=Game.Connection.WaitingForSnapshot?"Connection delayed — waiting for host ("+Game.Connection.SnapshotAge.ToString("0")+"s)":Game.Self?.alive==false&&!Game.Self.observer?"ELIMINATED · Entering spectator mode shortly":Game.IsSpectating?"":view.phase==Phase.Aftermath?"":view.busyRemaining>0?$"{view.busyRemaining:0.0}s":view.notice;
            if(!string.IsNullOrEmpty(notice))Text(370,620,540,45,notice,new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
            string voiceLabel=Game.Voice.Transmitting?"●  Speaking":Game.Voice.Muted?"Microphone muted":Game.Voice.Connected?"V  Talk":"Voice offline";
            Text(28,674,300,25,voiceLabel,Game.Voice.Transmitting?subtitle:small);
            Text(980,674,274,25,"TAB  Role    ESC  Menu",new GUIStyle(small){alignment=TextAnchor.MiddleRight});
            if(!Game.MenuOpen&&Game.Self?.alive==true)
            {
                if(view.phase==Phase.Discussion||view.phase==Phase.Choosing)Invitations();
                if(view.phase==Phase.Choosing||(view.phase==Phase.Discussion&&Game.RoomPlannerOpen))RoomChoices();
                if(view.phase==Phase.Turns&&view.duel!=null)DuelCard();
                if(view.phase==Phase.Turns&&view.roomTurn!=null)RoomTurnCard();
            }
            if(Game.IsSpectating&&!Game.MenuOpen)
            {
                Panel(new Rect(430,128,420,80),new Color(.045f,.035f,.028f,.98f));
                Text(450,140,380,25,"SPECTATING  /  "+Game.CameraPlayer.name,subtitle);
                if(Button(450,168,180,32,"← PREVIOUS"))Game.CycleSpectator(-1);
                if(Button(650,168,180,32,"NEXT →"))Game.CycleSpectator(1);
                Text(28,650,1100,22,"← / →  Switch player POV  ·  Dead players stay on their own voice channel",small);
            }
            if(Game.Self!=null&&!Game.Self.alive&&!Game.IsSpectating&&view.phase!=Phase.Finished&&Game.ShowEliminationOverlay)
            {Panel(new Rect(390,280,500,100),new Color(.05f,.035f,.03f,.95f));Text(420,298,440,70,"ELIMINATED\nYour voice is separated from living players.",roleStyle);}
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
                Text(400,460,500,30,"Esc → lobby to start the next match",subtitle);
            }
        }

        void RoleCard(Role role)
        {
            var guide=SaloonRules.Guide(role,Game.View.players.Length);
            var help=new GUIStyle(body){fontSize=15,richText=true};
            Panel(new Rect(218,122,844,546),new Color(.045f,.035f,.028f,.99f));
            Text(248,140,784,45,role.ToString().ToUpperInvariant(),roleStyle);
            Text(250,194,780,44,guide.Goal,body);
            Text(250,250,355,24,"WHAT TO PRESS ON YOUR TURN",subtitle);
            Text(645,250,385,24,guide.PlanTitle,subtitle);
            Text(250,279,355,222,guide.Actions+"\n\n<b>Q</b> draw, aim at the head + <b>left-click</b> shoot.\n<b>R</b> reload.\n<b>E</b> take a beer at Bar (free).\n<b>B / G</b> drink / give your beer.\n<b>Space</b> pass your turn.",help);
            Text(645,279,385,222,guide.Plan,help);
            Text(250,514,780,22,"HOW YOU WIN",subtitle);
            Text(250,538,780,43,guide.Win,help);
            Text(250,590,780,36,guide.Warning,new GUIStyle(help){normal={textColor=gold}});
            Text(250,637,780,22,"Headshots only. F stand / sit, WASD dodge in your room. One action per round. Release Tab to close.",small);
        }

        void RoomChoices()
        {
            Game.RoomChooserRenderFrames++;
            var self=Game.Self;Panel(new Rect(870,132,384,480),new Color(.045f,.035f,.028f,.95f));
            Text(890,148,344,28,"DISCUSS & PLAN",subtitle);
            Text(890,183,344,44,"Pick your room and optional companion. The round starts when everyone is ready.",small);
            var roomStyle=new GUIStyle(button){fontSize=14,padding=new RectOffset(4,4,6,6)};
            for(int room=0;room<3;room++)
                if(GUI.Button(new Rect(890+room*116,238,112,36),(Game.PlannedRoom==room?"✓ ":"")+SaloonStations.Name(room),roomStyle))Game.ChooseRoom(room,Game.PlannedCompanion);
            if(Button(890,286,344,30,string.IsNullOrEmpty(Game.PlannedCompanion)?"✓ Go alone":"Go alone"))Game.ChooseRoom(Game.PlannedRoom,"");
            int row=0;
            foreach(var player in Game.View.players.Where(p=>p.id!=self.id&&p.alive))
            {
                bool invited=player.companion==self.id;
                string label=(Game.PlannedCompanion==player.id?"✓ ":"")+player.name+(invited?" · invites you":" · "+SaloonStations.Name(player.chosenRoom));
                if(Button(890,328+row++*37,344,31,label))Game.ChooseRoom(invited?player.chosenRoom:Game.PlannedRoom,player.id);
            }
            Text(890,484,344,42,Game.PlanPending?"Saving your plan with the host… You can keep choosing.":"Changing your plan clears Ready. Hold right mouse to look around.",small);
            if(Button(890,548,344,42,Game.PlanReady?"✓ READY · CLICK TO UNREADY":"READY · START WHEN ALL READY"))Game.ToggleReady();
        }

        void Invitations()
        {
            if(Game.RoomPlannerOpen||Game.View.phase==Phase.Choosing)return;
            int row=0;
            foreach(var invite in Game.View.players.Where(p=>p.alive&&p.companion==Game.LocalId))
            {
                float y=(string.IsNullOrEmpty(Game.View.safeReport)?145:185)+row++*64;Panel(new Rect(28,y,345,56),new Color(.045f,.035f,.028f,.75f));
                Text(42,y+6,315,20,invite.name+" wants to talk to you",subtitle);
                Text(42,y+28,315,20,SaloonStations.Name(invite.chosenRoom)+"  ·  P to plan",small);
            }
        }

        void DuelCard()
        {
            var duel=Game.View.duel;bool choosing=duel.stage=="choosing",flipping=duel.stage=="flipping";
            Panel(new Rect(430,125,420,choosing?160:100),new Color(.045f,.035f,.028f,.90f));
            Text(450,137,380,24,choosing?"CHOOSE A SIDE  /  "+duel.remaining.ToString("0"):flipping?"LET THE COIN DECIDE":duel.result+"  /  "+duel.firstName+" opens",subtitle);
            if(choosing)
            {
                Text(450,166,380,28,"You: "+duel.side+" · "+duel.opponent+": "+(duel.side=="HEADS"?"TAILS":"HEADS"),small);
                if(Button(450,202,180,38,"HEADS"))Game.ChooseCoin(0);
                if(Button(650,202,180,38,"TAILS"))Game.ChooseCoin(1);
                Text(450,250,380,24,"No choice? Assigned sides flip automatically.",small);
            }
            else if(flipping)
            {
                float width=8+Mathf.Abs(Mathf.Cos(Time.unscaledTime*24))*30;var previous=GUI.color;GUI.color=gold;
                GUI.DrawTexture(new Rect(640-width/2,174,width,36),coinTexture);GUI.color=previous;
            }
            else Text(450,170,380,45,duel.waitingForOpening?duel.firstName+" has the opening · "+duel.remaining.ToString("0")+"s":duel.yourTurn?"Your move · Q to draw, click to shoot, Space to pass":"Action spent · hear their response",body);
        }

        void RoomTurnCard()
        {
            var turn=Game.View.roomTurn;Panel(new Rect(430,125,420,105),new Color(.045f,.035f,.028f,.90f));
            Text(450,137,380,24,string.IsNullOrEmpty(turn.currentId)?"ROOM ACTIONS COMPLETE":turn.yourTurn?"YOUR TURN  /  "+turn.remaining.ToString("0")+"s":turn.currentName+"  /  "+turn.remaining.ToString("0")+"s",subtitle);
            Text(450,169,380,50,string.Join("  →  ",turn.order),small);
        }

        void Pause()
        {
            Panel(new Rect(0,0,1280,720),new Color(0,0,0,.65f));
            Panel(new Rect(260,30,760,660),new Color(.055f,.045f,.034f,.98f));
            Text(295,50,600,50,"LAST CALL  /  BETA",roleStyle);
            scroll=GUI.BeginScrollView(new Rect(290,115,700,500),scroll,new Rect(0,0,670,2000));
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
                if(Button(0,y,640,42,$"DEAL ROLES & START {size}-PLAYER MATCH"))
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
            GUI.EndScrollView();
            if(Button(295,630,330,40,"RETURN TO SALOON"))Game.MenuOpen=false;
            if(Button(645,630,330,40,"LEAVE ROOM"))Game.Leave();
        }
    }
}
