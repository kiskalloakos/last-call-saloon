using System;
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

        void Styles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){fontSize=48,fontStyle=FontStyle.Bold,normal={textColor=ink}};
            subtitle=new GUIStyle(GUI.skin.label){fontSize=13,normal={textColor=gold}};
            body=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true,normal={textColor=ink}};
            small=new GUIStyle(body){fontSize=13,normal={textColor=muted}};
            button=new GUIStyle(GUI.skin.button){fontSize=16,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(15,15,10,10),normal={textColor=ink},hover={textColor=gold}};
            field=new GUIStyle(GUI.skin.textField){fontSize=17,padding=new RectOffset(10,10,8,8)};
            roleStyle=new GUIStyle(title){fontSize=32,alignment=TextAnchor.MiddleCenter};
        }

        void OnGUI()
        {
            Styles();float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            if(!Game.InRoom){MainMenu();return;}
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
            Text(67,157,500,26,"A FIVE-PLAYER SALOON PLAYTEST",subtitle);
            Text(67,199,485,55,"Everyone has a revolver.\nOnly one badge tells the truth.");
            Text(67,271,120,25,"YOUR NAME",subtitle);
            Game.PlayerName=GUI.TextField(new Rect(67,300,480,40),Game.PlayerName,20,field);
            if(Button(67,359,480,44,"SOLO PRACTICE  /  explore & test weapons")){Game.SaveName();Game.HostLan(true);}
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
            Text(67,632,480,72,Game.Connection.Status,small);
            Text(727,547,440,30,"TALK FIRST. DRAW CAREFULLY.",subtitle);
            Text(727,583,420,65,"Sheriff • Deputy • Two Outlaws • Renegade\nPrivate roles. Real-time action. No turns or dice.",body);
        }

        void Gameplay()
        {
            var view=Game.View;if(view==null){Text(40,30,650,100,Game.Connection.Status);return;}
            Panel(new Rect(26,22,330,104),new Color(.04f,.035f,.03f,.88f));
            Text(43,32,300,26,"LAST CALL  /  "+(view.sandbox?"PRACTICE":"PRIVATE PLAYTEST"),subtitle);
            Text(43,62,300,28,view.phase==Phase.Lobby?"Waiting for five players":view.phase==Phase.Introductions?"INTRODUCTIONS — WEAPONS LOCKED":view.phase==Phase.Live?"WEAPONS LIVE":"MATCH FINISHED",small);
            Text(43,89,300,26,view.phase==Phase.Lobby?$"{view.players.Length} / 5 players   •   Esc for lobby":$"{TimeSpan.FromSeconds(view.remaining):mm\\:ss}   •   Hold Tab for your role",small);
            Panel(new Rect(954,22,300,76),new Color(.04f,.035f,.03f,.88f));
            Text(974,35,270,26,$"HEALTH  {view.health} / {Game.tuning.health}     AMMO  {view.ammo} / 6",body);
            Text(974,66,270,26,$"BEER  {view.carriedBeer} carried   /   {view.beers} at the bar",small);
            if(!Game.MenuOpen&&Game.Self!=null&&Game.Self.alive)
            {
                Text(630,344,30,35,"+",body);
                Text(470,392,380,28,Game.InteractionHint,new GUIStyle(body){alignment=TextAnchor.MiddleCenter});
            }
            Panel(new Rect(0,653,1280,67),new Color(.035f,.03f,.025f,.9f));
            Text(35,664,1220,24,"WASD  move     Q  draw / holster     Click  fire     R  reload     E  interact     B  drink     G  offer beer     V  talk     Esc  menu",small);
            Text(35,689,1210,25,view.busyRemaining>0?$"Hands occupied  /  {view.busyRemaining:0.0}s":view.notice,small);
            string voiceLabel=Game.Voice.Connected?(Game.Voice.Muted?"MIC MUTED  /  M to unmute":!Game.Voice.MicrophoneAllowed?"LISTENING ONLY  /  Esc for microphone settings":Game.Voice.Transmitting?"TRANSMITTING  /  release V to stop":"PROXIMITY VOICE  /  hold V to talk"):Game.Voice.Busy?"CONNECTING VOICE...":"VOICE DISCONNECTED  /  Esc to retry";
            Text(40,609,1000,28,voiceLabel,subtitle);
            if(Game.Self!=null&&!Game.Self.alive&&view.phase!=Phase.Finished)
            {Panel(new Rect(390,280,500,100),new Color(.05f,.035f,.03f,.95f));Text(420,298,440,70,"ELIMINATED\nYour voice is separated from living players.",roleStyle);}
            if(Game.ShowRole&&!Game.MenuOpen)
            {
                Panel(new Rect(365,205,550,245),new Color(.045f,.035f,.028f,.98f));
                Text(390,231,500,50,view.ownRole.ToUpperInvariant(),roleStyle);
                Enum.TryParse(view.ownRole,out Role role);
                Text(405,300,470,65,SaloonRules.Objective(role));
                if(!string.IsNullOrEmpty(view.ally))Text(405,374,470,45,"Fellow Outlaw: "+view.ally,subtitle);
            }
            if(view.phase==Phase.Finished&&!Game.MenuOpen)
            {
                Panel(new Rect(350,195,580,320),new Color(.04f,.035f,.03f,.97f));
                Text(375,220,530,45,view.outcome,roleStyle);
                int row=0;foreach(var p in view.players)Text(400,280+row++*30,500,28,$"{p.name}  /  {p.revealedRole}  /  {(p.alive?"survived":"eliminated")}",small);
                Text(400,460,500,30,"Esc → lobby to start the next match",subtitle);
            }
        }

        void Pause()
        {
            Panel(new Rect(0,0,1280,720),new Color(0,0,0,.65f));
            Panel(new Rect(260,30,760,660),new Color(.055f,.045f,.034f,.98f));
            Text(295,50,600,50,"SALOON LOBBY",roleStyle);
            scroll=GUI.BeginScrollView(new Rect(290,115,700,500),scroll,new Rect(0,0,670,900));
            Text(0,0,660,40,!string.IsNullOrEmpty(Game.Connection.RoomCode)?"ROOM CODE  "+Game.Connection.RoomCode:"LAN  /  Host shares their local IP and port "+Game.Port,subtitle);
            int y=45;
            if(Game.View!=null)foreach(var p in Game.View.players){Text(0,y,650,30,p.name+(p.sheriff?"  [SHERIFF]":"")+(!p.alive?"  [ELIMINATED]":""));y+=31;}
            if(Game.Hosting)
            {
                y+=12;GUI.enabled=Game.View!=null&&(Game.View.phase==Phase.Lobby||Game.View.phase==Phase.Finished);
                Text(0,y,650,26,$"Opening weapon lock: {Game.tuning.openingSeconds:0} seconds",small);y+=28;
                Game.tuning.openingSeconds=Mathf.Round(GUI.HorizontalSlider(new Rect(0,y,640,25),Game.tuning.openingSeconds,0,60));y+=33;
                Text(0,y,650,26,$"Match limit: {Game.tuning.matchSeconds/60:0} minutes",small);y+=28;
                Game.tuning.matchSeconds=Mathf.Round(GUI.HorizontalSlider(new Rect(0,y,640,25),Game.tuning.matchSeconds/60,2,12))*60;y+=33;
                Text(0,y,650,26,$"Shared beer supply: {Game.tuning.sharedBeers}",small);y+=28;
                Game.tuning.sharedBeers=Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(0,y,640,25),Game.tuning.sharedBeers,0,16));y+=32;
                GUI.enabled=GUI.enabled&&Game.PlayerCount==5;
                if(Button(0,y,640,42,"DEAL ROLES & START MATCH")){Game.StartMatch();Game.MenuOpen=false;}
                GUI.enabled=Game.View!=null&&(Game.View.phase==Phase.Lobby||Game.View.phase==Phase.Finished);y+=52;
                if(Button(0,y,640,42,"START PRACTICE / FILL EMPTY SEATS")){Game.StartPractice();Game.MenuOpen=false;}
                y+=52;
                if(Game.View!=null&&Game.View.sandbox)
                {if(Button(0,y,640,34,"REMOVE PRACTICE TARGETS / REOPEN LOBBY"))Game.ClearPracticeTargets();y+=44;}
                GUI.enabled=true;y+=8;
            }
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
            Text(0,y,640,120,"Sheriff & Deputy: remove both Outlaws and the Renegade.\nOutlaws: kill the Sheriff.\nRenegade: be the sole survivor, Sheriff last.\nTimeout is a draw; a mid-match disconnect aborts the match.",small);
            GUI.EndScrollView();
            if(Button(295,630,330,40,"RETURN TO SALOON"))Game.MenuOpen=false;
            if(Button(645,630,330,40,"LEAVE ROOM"))Game.Leave();
        }
    }
}
