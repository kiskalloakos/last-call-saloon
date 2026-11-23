using System.Linq;
using Unity.Services.Vivox;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        enum FrontPage { Home, Practice, Lan, Online }
        enum RoomPage { Room, Voice, Controls, Credits, Rules }

        static readonly Color MenuBase=new Color(.063f,.058f,.047f);
        static readonly Color MenuSurface=new Color(.115f,.105f,.083f);
        static readonly Color MenuStroke=new Color(.29f,.25f,.18f);
        static readonly Color MenuInk=new Color(.98f,.925f,.81f);
        static readonly Color MenuMuted=new Color(.76f,.71f,.61f);
        static readonly Color MenuAccent=new Color(.96f,.71f,.39f);
        GUIStyle menuField,menuButton,menuPrimary,menuDisabled;
        Texture2D menuArtwork,menuShade;
        FrontPage frontPage;
        RoomPage roomPage;
        Vector2 roomMenuScroll,frontStatusScroll,roomRosterScroll;
        float roomRosterHeight;
        float roomMenuHeight,disconnectCopiedUntil;

        void BuildMenuStyles()
        {
            var surface=Swatch(MenuSurface);
            var hover=Swatch(new Color(.19f,.16f,.12f));
            var amber=Swatch(new Color(.84f,.58f,.30f));
            var bright=Swatch(MenuAccent);
            var quiet=Swatch(new Color(.095f,.084f,.067f));
            menuButton=new GUIStyle(button){fontSize=18,wordWrap=true};
            FlatStates(menuButton,surface,hover,hover,MenuInk);
            menuPrimary=new GUIStyle(menuButton);
            FlatStates(menuPrimary,amber,bright,bright,MenuBase);
            menuDisabled=new GUIStyle(menuButton);
            FlatStates(menuDisabled,quiet,quiet,quiet,new Color(.57f,.52f,.44f));
            menuField=new GUIStyle(GUI.skin.textField){font=bodyFont,fontSize=19,padding=new RectOffset(13,13,10,8)};
            FlatStates(menuField,quiet,quiet,hover,MenuInk);
            menuArtwork=Resources.Load<Texture2D>("UI/MainMenuArt");
            menuShade=new Texture2D(128,1,TextureFormat.RGBA32,false)
                {hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
            for(int x=0;x<menuShade.width;x++)
            {
                float alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.28f,.69f,x/(menuShade.width-1f)));
                menuShade.SetPixel(x,0,new Color(MenuBase.r,MenuBase.g,MenuBase.b,alpha));
            }
            menuShade.Apply(false,true);
            uiTextures.Add(menuShade);
        }

        GUIStyle MenuStyle(int size,Color color,TextAnchor anchor=TextAnchor.UpperLeft,bool heading=false)
            =>new GUIStyle(heading?title:body){fontSize=size,richText=false,wordWrap=true,alignment=anchor,normal={textColor=color}};

        float MenuText(float x,float y,float width,string value,int size=17,bool muted=false,bool heading=false)
            =>WrappedText(x,y,width,value,MenuStyle(size,muted?MenuMuted:MenuInk,heading:heading));

        bool MenuButton(Rect rect,string label,bool primary=false,bool enabled=true)
        {
            bool active=GUI.enabled&&enabled;
            bool previous=GUI.enabled;
            GUI.enabled=true;
            bool clicked=GUI.Button(rect,label,active?(primary?menuPrimary:menuButton):menuDisabled);
            GUI.enabled=previous;
            if(Event.current.type==EventType.Repaint)
                Outline(rect,active&&primary?MenuAccent:active&&rect.Contains(Event.current.mousePosition)?MenuAccent:MenuStroke);
            return clicked&&active;
        }

        string MenuField(Rect rect,string value,int maxLength)
        {
            string updated=GUI.TextField(rect,value,maxLength,menuField);
            if(Event.current.type==EventType.Repaint)Outline(rect,rect.Contains(Event.current.mousePosition)?MenuAccent:MenuStroke);
            return updated;
        }

        void MenuBackdrop(bool artwork,bool dim=false)
        {
            var matrix=GUI.matrix;
            GUI.matrix=Matrix4x4.identity;
            var screen=new Rect(0,0,Screen.width,Screen.height);
            if(artwork)
            {
                Fill(screen,MenuBase);
                if(menuArtwork!=null)GUI.DrawTexture(screen,menuArtwork,ScaleMode.ScaleAndCrop);
                Fill(screen,new Color(.055f,.036f,.021f,.21f));
                GUI.DrawTexture(screen,menuShade,ScaleMode.StretchToFill);
            }
            if(dim)Fill(screen,new Color(.033f,.027f,.019f,.79f));
            GUI.matrix=matrix;
        }

        void MenuFrame(Rect panel)
        {
            Fill(new Rect(panel.x+4,panel.y+6,panel.width,panel.height),new Color(0,0,0,.38f));
            Fill(panel,MenuBase);
            Outline(panel,MenuStroke);
            Fill(new Rect(panel.x,panel.y,panel.width,2),MenuAccent);
        }

        void MenuDivider(float x,float y,float width)=>Fill(new Rect(x,y,width,1),MenuStroke);

        void MainMenu()
        {
            MenuBackdrop(true);
            roomPage=RoomPage.Room;
            roomMenuScroll=Vector2.zero;
            const float x=76,width=426;
            bool home=frontPage==FrontPage.Home,busy=Game.Connection.Busy;
            float brandY=home?101:62;
            MenuText(x,brandY,515,"LAST CALL",72,heading:true);
            Fill(new Rect(x,brandY+95,64,2),MenuAccent);
            MenuText(x,brandY+109,width,home?"A social deduction game for 4–8 players.":
                frontPage==FrontPage.Practice?"Set up a local match with bots.":
                frontPage==FrontPage.Lan?"Play on the same local network.":"Play with a private room code.",20,muted:true);
            WrappedText(1000,657,216,Application.version,MenuStyle(15,MenuInk,TextAnchor.UpperRight));

            float nameY=home?262:frontPage==FrontPage.Practice?198:218;
            MenuText(x,nameY,width,"Player name",16,muted:true);
            Game.PlayerName=MenuField(new Rect(x,nameY+26,width,45),Game.PlayerName,20);
            if(home)
            {
                if(MenuButton(new Rect(x,356,width,54),"Solo practice",true,!busy))
                    frontPage=FrontPage.Practice;
                MenuText(x,435,width,"Play with friends",16,muted:true);
                if(MenuButton(new Rect(x,461,207,46),"Local network",enabled:!busy))frontPage=FrontPage.Lan;
                if(MenuButton(new Rect(x+219,461,207,46),"Online room",enabled:!busy))frontPage=FrontPage.Online;
                if(MenuButton(new Rect(x,527,207,44),"Tutorial",enabled:!busy))Game.OpenTutorial();
                if(MenuButton(new Rect(x+219,527,207,44),"Graphics settings",enabled:!busy))OpenGraphics();
            }
            else if(frontPage==FrontPage.Practice)DrawPracticeSetup(x,width,busy);
            else
            {
                if(MenuButton(new Rect(x,310,207,42),"Local network",frontPage==FrontPage.Lan,!busy))frontPage=FrontPage.Lan;
                if(MenuButton(new Rect(x+219,310,207,42),"Online room",frontPage==FrontPage.Online,!busy))frontPage=FrontPage.Online;
                if(frontPage==FrontPage.Lan)
                {
                    MenuText(x,373,288,"Host address",16,muted:true);
                    MenuText(x+302,373,124,"Port",16,muted:true);
                    Game.Address=MenuField(new Rect(x,398,288,42),Game.Address,64);
                    Game.Port=MenuField(new Rect(x+302,398,124,42),Game.Port,5);
                    if(MenuButton(new Rect(x,452,207,43),"Host LAN",true,!busy)){Game.SaveName();Game.HostLan(false);}
                    if(MenuButton(new Rect(x+219,452,207,43),"Join LAN",enabled:!busy)){Game.SaveName();Game.JoinLan();}
                    if(MenuButton(new Rect(x,509,width,38),Game.BotsEnabled?"Fill empty seats with bots: On":"Fill empty seats with bots: Off",enabled:!busy))
                        Game.SetBotsEnabled(!Game.BotsEnabled);
                }
                else
                {
                    MenuText(x,373,width,"Room code",16,muted:true);
                    Game.JoinCode=MenuField(new Rect(x,398,width,42),Game.JoinCode,12).ToUpperInvariant();
                    if(MenuButton(new Rect(x,452,207,43),"Create room",true,!busy)){Game.SaveName();Game.Cloud(true);}
                    if(MenuButton(new Rect(x+219,452,207,43),"Join room",enabled:!busy)){Game.SaveName();Game.Cloud(false);}
                }
                if(MenuButton(new Rect(x,558,132,38),"←  Back",enabled:!busy))frontPage=FrontPage.Home;
            }
            DrawFrontStatus(x,frontPage==FrontPage.Practice?644:home?597:610,width);
        }

        void DrawPracticeSetup(float x,float width,bool busy)
        {
            bool enabled=GUI.enabled;GUI.enabled=enabled&&!busy;
            MenuText(x,286,width,"Number of bots",17);
            int selected=Mathf.Clamp(Game.BotMatchSize,SaloonRules.MinimumPlayers,SaloonRules.LobbyCapacity);
            float choiceWidth=(width-4*6)/5;
            for(int count=SaloonRules.MinimumPlayers;count<=SaloonRules.LobbyCapacity;count++)
                if(MenuButton(new Rect(x+(count-SaloonRules.MinimumPlayers)*(choiceWidth+6),314,choiceWidth,36),
                    (count-1).ToString(),selected==count))Game.SetBotMatchSize(count);
            MenuText(x,362,width,$"You + {selected-1} bots · {selected} players total",17,muted:true);
            MenuText(x,405,width,$"Discussion time  {Game.tuning.discussionSeconds:0} seconds",17);
            Game.tuning.discussionSeconds=Mathf.Round(GUI.HorizontalSlider(new Rect(x,435,width,20),Game.tuning.discussionSeconds,5,120));
            DrawActionSettings(x,466,width);
            if(MenuButton(new Rect(x,590,132,44),"←  Back"))frontPage=FrontPage.Home;
            if(MenuButton(new Rect(x+144,590,width-144,44),"Start practice",true))
            {Game.SaveName();Game.StartSolo();}
            GUI.enabled=enabled;
        }

        void DrawFrontStatus(float x,float y,float width)
        {
            var connection=Game.Connection;
            string status=connection.Status;
            if(status=="Choose solo practice, LAN, or an online room.")status="";
            bool details=!string.IsNullOrEmpty(connection.LastDisconnectDetails);
            if(!details&&string.IsNullOrEmpty(status))return;
            // Give long network messages room to scroll inside the title screen.
            var style=MenuStyle(16,MenuMuted);
            float statusHeight=WrappedHeight(status,width-14,style);
            float visible=Mathf.Min(statusHeight,Mathf.Min(details?48:66,710-y-(details?36:0)));
            if(statusHeight>visible)
            {
                frontStatusScroll=GUI.BeginScrollView(new Rect(x,y,width,visible),frontStatusScroll,new Rect(0,0,width-14,statusHeight));
                WrappedText(0,0,width-14,status,style);
                GUI.EndScrollView();
            }
            else WrappedText(x,y,width-14,status,style);
            if(details&&MenuButton(new Rect(x,y+visible+4,212,32),Time.unscaledTime<disconnectCopiedUntil?"Details copied":"Copy disconnect details"))
            {
                GUIUtility.systemCopyBuffer=connection.LastDisconnectDetails;
                disconnectCopiedUntil=Time.unscaledTime+2;
            }
        }

        void ReconnectMenu()
        {
            MenuBackdrop(false,true);
            var panel=new Rect(362,234,556,242);
            MenuFrame(panel);
            MenuText(394,266,492,"Reconnecting",34,heading:true);
            MenuText(394,331,492,$"Your seat is held for {Mathf.CeilToInt(Game.Connection.ReconnectRemaining)} seconds.",20,muted:true);
            if(MenuButton(new Rect(394,404,492,42),"Leave room"))Game.Leave();
        }

        void InRoomMenu()
        {
            frontPage=FrontPage.Home;
            MenuBackdrop(false,true);
            bool lobby=Game.View?.phase==Phase.Lobby;
            var panel=new Rect(64,45,1152,630);
            MenuFrame(panel);
            Fill(new Rect(panel.x+1,panel.y+2,232,panel.height-3),new Color(.093f,.081f,.061f));
            Fill(new Rect(296,74,1,572),MenuStroke);
            MenuText(90,84,190,lobby?"Lobby":"Game menu",29,heading:true);
            RoomNav(RoomPage.Room,146,lobby?"Room":"Match");
            RoomNav(RoomPage.Voice,199,"Voice");
            if(MenuButton(new Rect(90,252,180,44),"Graphics"))OpenGraphics();
            RoomNav(RoomPage.Controls,305,"Controls");
            RoomNav(RoomPage.Rules,358,"How to win");
            RoomNav(RoomPage.Credits,411,"Credits");
            if(MenuButton(new Rect(90,546,180,46),lobby?"Look around":"Resume game",!lobby))Game.MenuOpen=false;
            if(MenuButton(new Rect(90,603,180,42),"Leave room"))Game.Leave();

            if(roomPage==RoomPage.Room)DrawRoomPage(new Rect(328,77,854,570),lobby);
            else DrawRoomMenuTab(new Rect(328,77,854,570));
        }

        void RoomNav(RoomPage page,float y,string label)
        {
            if(!MenuButton(new Rect(90,y,180,44),label,roomPage==page)||roomPage==page)return;
            roomPage=page;
            roomMenuScroll=Vector2.zero;
            roomMenuHeight=0;
        }

        void DrawRoomPage(Rect content,bool lobby)
        {
            var view=Game.View;
            float x=content.x,y=content.y;
            MenuText(x,y,540,lobby?"Multiplayer room":"Current match",34,heading:true);
            if(view==null){MenuText(x,y+64,content.width,"Joining room…",20,muted:true);return;}
            string code=Game.Connection.RoomCode;
            string label=Game.Connection.Solo?"Solo practice":string.IsNullOrEmpty(code)?"Local network":"Room code  "+code;
            MenuText(x,y+54,670,label,21);
            if(!string.IsNullOrEmpty(code)&&MenuButton(new Rect(content.xMax-130,y+50,130,36),"Copy code"))
                GUIUtility.systemCopyBuffer=code;
            string instruction=Game.Connection.Solo?"Practice with bots.":string.IsNullOrEmpty(code)?
                $"Share the host’s local address and port {Game.Port}.":"Share the code with the players you want to invite.";
            MenuText(x,y+85,780,instruction,17,muted:true);
            MenuDivider(x,y+120,content.width);

            const float rosterWidth=410;
            float right=x+rosterWidth+24,rightWidth=content.xMax-right;
            MenuText(x,y+141,rosterWidth,lobby?$"Players  {view.players.Length} of {SaloonRules.LobbyCapacity}":"Players",22);
            var rosterArea=new Rect(x,y+180,rosterWidth,content.height-180);
            float rowWidth=rosterWidth-16;
            roomRosterScroll=GUI.BeginScrollView(rosterArea,roomRosterScroll,new Rect(0,0,rowWidth,roomRosterHeight));
            float rowY=0;
            foreach(var person in view.players)
            {
                string name=person.name+(person.id==Game.LocalId?" (you)":"");
                var metadata=person.disconnected?"Reconnecting":!person.alive?"Dead":person.bot?"Bot":person.sheriff&&!lobby?"Sheriff":"";
                var metaStyle=MenuStyle(15,MenuMuted,TextAnchor.UpperRight);
                float metaWidth=string.IsNullOrEmpty(metadata)?0:Mathf.Ceil(metaStyle.CalcSize(new GUIContent(metadata)).x)+4;
                float nameWidth=rowWidth-28-(metaWidth==0?0:metaWidth+12);
                var nameStyle=MenuStyle(17,person.alive?MenuInk:MenuMuted);
                string location=view.phase==Phase.Turns?Game.RoomName(person.room):"";
                var locationStyle=MenuStyle(15,MenuMuted);
                float nameHeight=WrappedHeight(name,nameWidth,nameStyle);
                float labelsHeight=nameHeight+WrappedHeight(location,nameWidth,locationStyle);
                float metaHeight=WrappedHeight(metadata,metaWidth,metaStyle);
                float rowHeight=Mathf.Max(42,Mathf.Max(labelsHeight,metaHeight)+12);
                Fill(new Rect(0,rowY,rowWidth,rowHeight),MenuSurface);
                WrappedText(14,rowY+6,nameWidth,name,nameStyle);
                WrappedText(14,rowY+6+nameHeight,nameWidth,location,locationStyle);
                if(metaWidth>0)
                    WrappedText(rowWidth-metaWidth-14,rowY+(rowHeight-metaHeight)/2,metaWidth,metadata,metaStyle);
                rowY+=rowHeight+6;
            }
            roomRosterHeight=rowY;
            GUI.EndScrollView();
            if(lobby&&Game.Hosting)DrawMatchSetup(right,y+141,rightWidth);
            else
            {
                MenuText(right,y+141,rightWidth,lobby?"Match setup":$"Round {view.round}",24);
                string detail=lobby?(view.botsEnabled?$"The host can fill the room to {view.botMatchSize} players with bots.":"The match needs 4–8 players."):
                    "The match continues while this menu is open.";
                float infoY=y+190+MenuText(right,y+190,rightWidth,detail,19,muted:true)+12;
                if(lobby)infoY+=MenuText(right,infoY,rightWidth,"The host starts the match.",18,muted:true)+16;
                MenuText(right,infoY,rightWidth,view.reconnectNotice,15,muted:true);
            }
        }

        void DrawMatchSetup(float x,float y,float width)
        {
            const float inset=20;
            var statusStyle=MenuStyle(16,MenuMuted);
            string status=Game.LobbyStartStatus;
            float startOffset=Mathf.Max(358,313+WrappedHeight(status,width-2*inset,statusStyle)+10);
            var panel=new Rect(x,y-8,width,startOffset+69);
            Fill(panel,MenuSurface);
            Outline(panel,MenuStroke);
            x+=inset;width-=2*inset;
            MenuText(x,y+9,width,"Match setup",23);
            if(MenuButton(new Rect(x,y+51,width,38),Game.BotsEnabled?"Fill empty seats with bots: On":"Fill empty seats with bots: Off"))
                Game.SetBotsEnabled(!Game.BotsEnabled);
            if(Game.BotsEnabled)
            {
                MenuText(x,y+101,width,"Fill to",16,muted:true);
                float sizeWidth=(width-4*6)/5;
                for(int count=SaloonRules.MinimumPlayers;count<=SaloonRules.LobbyCapacity;count++)
                    if(MenuButton(new Rect(x+(count-SaloonRules.MinimumPlayers)*(sizeWidth+6),y+126,sizeWidth,34),count.ToString(),Game.BotMatchSize==count))Game.SetBotMatchSize(count);
            }
            else MenuText(x,y+106,width,"A match needs 4–8 players.",16,muted:true);

            DrawActionSettings(x,y+173,width);
            MenuDivider(x,y+300,width);
            WrappedText(x,y+313,width,status,statusStyle);
            bool start=Game.CanStartMatch;
            if(MenuButton(new Rect(x,y+startOffset,width,45),start?$"Start {Game.PlannedMatchSize}-player match":"Start match",true,start))
            {
                Game.StartMatch();
                if(Game.CurrentPhase==Phase.Discussion)Game.MenuOpen=false;
            }
        }

        void DrawActionSettings(float x,float y,float width)
        {
            MenuText(x,y,width,$"Action time  {Game.tuning.turnSeconds:0} seconds",17);
            Game.tuning.turnSeconds=Mathf.Round(GUI.HorizontalSlider(new Rect(x,y+30,width,20),Game.tuning.turnSeconds,5,60));
            MenuText(x,y+61,width,$"Shared beer supply  {Game.tuning.sharedBeers}",17);
            Game.tuning.sharedBeers=Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(x,y+91,width,20),Game.tuning.sharedBeers,0,16));
        }

        void DrawRoomMenuTab(Rect content)
        {
            MenuText(content.x,content.y,content.width,roomPage==RoomPage.Rules?"How to win":roomPage.ToString(),34,heading:true);
            var area=new Rect(content.x,content.y+64,content.width,content.height-64);
            float width=area.width-16;
            roomMenuScroll=GUI.BeginScrollView(area,roomMenuScroll,new Rect(0,0,width,roomMenuHeight));
            switch(roomPage)
            {
                case RoomPage.Voice: roomMenuHeight=DrawVoiceSettings(width);break;
                case RoomPage.Controls: roomMenuHeight=DrawControls(width);break;
                case RoomPage.Rules: roomMenuHeight=DrawRules(width);break;
                case RoomPage.Credits: roomMenuHeight=DrawCredits(width);break;
            }
            GUI.EndScrollView();
        }

        float DrawVoiceSettings(float width)
        {
            var voice=Game.Voice;
            float y=MenuText(0,0,width,voice.Status,18,muted:true)+18;
            float half=(width-12)/2;
            if(MenuButton(new Rect(0,y,half,42),voice.Connected?"Voice connected":"Connect voice",!voice.Connected,!voice.Busy))voice.Connect();
            if(MenuButton(new Rect(half+12,y,half,42),"Microphone test",enabled:!voice.Busy))voice.EchoTest();
            y+=62;
            MenuText(0,y,width,"Microphone mode",20);
            y+=36;
            if(MenuButton(new Rect(0,y,half,42),"Push-to-talk",!voice.OpenMicrophone))voice.OpenMicrophone=false;
            if(MenuButton(new Rect(half+12,y,half,42),"Open mic",voice.OpenMicrophone))voice.OpenMicrophone=true;
            y+=52;
            bool sharedVoice=SaloonPrivacy.SharedVoice(Game.View?.phase??Phase.Lobby);
            y+=MenuText(0,y,width,sharedVoice?"Your mic stays active in the lobby and on the results screen.":
                "Opening this menu pauses your mic during a match.",17,muted:true)+17;
            if(MenuButton(new Rect(0,y,width,42),"Change microphone",enabled:voice.Connected&&!voice.Busy))voice.CycleMicrophone();
            y+=48;
            y+=MenuText(0,y,width,voice.MicrophoneName,16,muted:true)+12;
            if(MenuButton(new Rect(0,y,250,40),voice.Muted?"Unmute microphone":"Mute microphone",enabled:voice.Connected&&!voice.Busy))voice.Muted=!voice.Muted;
            y+=58;
            if(voice.Connected)
            {
                var channels=VivoxService.Instance.ActiveChannels.Where(pair=>pair.Key==voice.Channel||pair.Key==voice.SpectatorChannel);
                foreach(var player in channels.SelectMany(pair=>pair.Value).GroupBy(p=>p.PlayerId))
                {
                    var participant=player.First();
                    bool muted=player.All(p=>p.IsMuted);
                    float nameHeight=MenuText(0,y+8,width-148,participant.DisplayName+(player.Any(p=>p.SpeechDetected)?"  ·  Speaking":""),17);
                    if(!participant.IsSelf&&MenuButton(new Rect(width-128,y,128,36),muted?"Unmute":"Mute"))
                        foreach(var peer in player){if(muted)peer.UnmutePlayerLocally();else peer.MutePlayerLocally();}
                    y+=Mathf.Max(36,nameHeight+16)+8;
                }
            }
            return y;
        }

        float DrawControls(float width)
        {
            float column=(width-24)/2;
            float Bindings(float left,(string Key,string Action)[] bindings)
            {
                float y=0;
                foreach(var (key,action) in bindings)
                {
                    if(key=="R"&&Game.View?.UnlimitedAmmo==true)continue;
                    Fill(new Rect(left,y,column,42),MenuSurface);
                    WrappedText(left+12,y+10,130,key,MenuStyle(17,MenuAccent));
                    MenuText(left+148,y+10,column-160,action,17);
                    y+=47;
                }
                return y;
            }
            float leftHeight=Bindings(0,new[]{
                ("WASD","Move"),("Mouse","Look and aim"),("Ctrl","Hold to crouch"),("Space","Stand or sit"),("Q","Draw or holster"),
                ("Hold left-click","Accurate shot · 0.5 s"),("Right-click","Quick shot · more spread"),("R","Reload"),("Tab","View your role")
            });
            float rightHeight=Bindings(column+24,new[]{
                ("F","Lock plan or pass"),("E","Drink beer near the tray"),("C / H","Check drawer / take money"),
                ("X","Plan escape during discussion"),("← / →","Change spectator view"),("V / T","Talk or switch mic mode"),("M","Mute microphone"),
                ("Right-click","Plan during discussion")
            });
            float yEnd=Mathf.Max(leftHeight,rightHeight)+12;
            return yEnd+MenuText(0,yEnd,width,"Both shots use one bullet and one action, with equal headshot damage. Quick shots spread more while moving. Release left-click early to cancel.",17,muted:true);
        }

        float DrawRules(float width)
        {
            float y=0;
            void Rule(string heading,string description)
            {
                const float inset=16;
                var headingStyle=MenuStyle(22,MenuAccent);
                var descriptionStyle=MenuStyle(18,MenuInk);
                float textWidth=width-2*inset;
                float headingHeight=WrappedHeight(heading,textWidth,headingStyle);
                float height=2*inset+headingHeight+5+WrappedHeight(description,textWidth,descriptionStyle);
                Fill(new Rect(0,y,width,height),MenuSurface);
                WrappedText(inset,y+inset,textWidth,heading,headingStyle);
                WrappedText(inset,y+inset+headingHeight+5,textWidth,description,descriptionStyle);
                y+=height+12;
            }
            Rule("Sheriff and Deputies","Keep the Sheriff alive and defeat every Outlaw and any Renegade. Stop the money from escaping.");
            Rule("Outlaws","Use a drawer check to take the money, then help one Outlaw escape with it.");
            Rule("Independent · random each match","A secret 50/50 roll selects Renegade or Swindler. Renegade: discover rivals and be last alive. Swindler: earn a personal win by being killed by the Sheriff or a Deputy; others keep playing.");
            return y;
        }

        float DrawCredits(float width)
        {
            float y=MenuText(0,0,width,"Revolver model",22)+9;
            y+=MenuText(0,y,width,"TimDev. CC BY 4.0. Model and texture adapted.\nopengameart.org/content/colt-lowpoly-revolver\ncreativecommons.org/licenses/by/4.0/",18,muted:true)+26;
            y+=MenuText(0,y,width,"Recorded sound effects",22)+9;
            y+=MenuText(0,y,width,"okieactor (revolver shot), Dredile (reload), Q.K. (drinking), Max_Headroom (horses), and Resaural (distant shot). CC0 1.0 / Freesound. See Audio/Recorded/License.txt.",18,muted:true)+23;
            y+=MenuText(0,y,width,"Combat sounds",22)+9;
            return y+MenuText(0,y,width,"Kenney. CC0. See Audio/Combat/PROVENANCE.txt.",18,muted:true)+10;
        }
    }
}
