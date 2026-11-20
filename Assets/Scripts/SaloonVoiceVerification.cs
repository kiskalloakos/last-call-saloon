#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using Unity.Services.Vivox;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        // Opt-in packaged regression. No microphone permission request or live capture.
        public IEnumerator VerifyVoiceControls()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string message)
            {
                if(!ok){VerificationFinished=true;Debug.LogError("SALOON_VOICE_CONTROLS FAIL "+message);Application.Quit(8);throw new InvalidOperationException(message);}
                Debug.Log("SALOON_VOICE_CONTROLS_CHECK "+message);
            }
            Check(!Voice.OpenMicrophone,"new session starts in push-to-talk");
            Check(!SaloonVoice.WantsTransmission(false,false,false,false)&&SaloonVoice.WantsTransmission(false,true,false,false),"push-to-talk follows press and release");
            Check(SaloonVoice.WantsTransmission(true,false,false,false)&&!SaloonVoice.WantsTransmission(false,false,false,false),"open mic works without V and switching it off stops transmission");
            for(int bits=0;bits<16;bits++)
            {
                bool open=(bits&1)!=0,push=(bits&2)!=0,mute=(bits&4)!=0,menu=(bits&8)!=0;
                Check(SaloonVoice.WantsTransmission(open,push,mute,menu)==(!mute&&!menu&&(open||push)),"mute/menu/input combination "+bits);
            }
            foreach(Phase p in Enum.GetValues(typeof(Phase)))
            {
                string indoor=SaloonPrivacy.VoiceChannel("test",p,0,true),outside=SaloonPrivacy.VoiceChannel("test",p,3,true);
                Check((indoor!=outside)==SaloonPrivacy.Separated(p)&&SaloonPrivacy.VoiceChannel("test",p,3,false)==(SaloonPrivacy.SharedVoice(p)?"test-alive":"test-dead"),"smoking and eliminated isolation during "+p);
            }
            foreach(Phase p in Enum.GetValues(typeof(Phase)))
            {
                Check(SaloonPrivacy.VoicePaused(p,true,false)==!SaloonPrivacy.SharedVoice(p),"setup/results keep microphone controls available during "+p);
                Check(SaloonPrivacy.VoicePaused(p,false,true),"graphics settings pause microphone during "+p);
            }
            MenuOpen=false;
            var connect=Voice.ConnectForVerification();float deadline=Time.realtimeSinceStartup+40;
            while(!connect.IsCompleted&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(connect.IsCompleted&&Voice.Connected,"live Vivox connects");
            var service=VivoxService.Instance;
            void OnlyChannel(string suffix)
            {
                Check(Voice.Channel==VoiceRoom+suffix&&service.ActiveChannels.Count==1&&service.ActiveChannels.ContainsKey(Voice.Channel),"exactly one live channel "+suffix);
                Check(!Voice.Transmitting&&service.IsInputDeviceMuted,"verification never captures the physical microphone");
            }
            IEnumerator SpectatorChannels(string suffix)
            {
                string playback=VoiceRoom+suffix;
                float until=Time.realtimeSinceStartup+30;
                while((Voice.SpectatorChannel!=playback||Voice.Busy||service.IsOutputDeviceMuted)&&Time.realtimeSinceStartup<until)yield return null;
                Check(Voice.Channel==VoiceRoom+"-dead"&&Voice.SpectatorChannel==playback&&service.ActiveChannels.Count==2&&
                    service.ActiveChannels.ContainsKey(Voice.Channel)&&service.ActiveChannels.ContainsKey(playback)&&!service.IsOutputDeviceMuted,
                    "spectator hears "+suffix+" alongside eliminated chat");
                Check(service.TransmittingChannels.SequenceEqual(new[]{Voice.Channel})&&service.IsInputDeviceMuted,
                    "spectator microphone is routed to eliminated chat only");
            }
            OnlyChannel("-alive");Voice.OpenMicrophone=true;
            var hero=players[Connection.LocalId];
            rounds.Start(players.Keys,Time.time,30,60,6);
            foreach(var p in players.Values){p.chosenRoom=p==hero?3:0;p.companion=null;rounds.SetReady(p.id,true);}
            rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);Broadcast();
            Check(service.IsInputDeviceMuted&&service.IsOutputDeviceMuted,"moving outside mutes both directions before the asynchronous switch");
            deadline=Time.realtimeSinceStartup+30;
            while((Voice.Channel!=VoiceRoom+"-smoke"||Voice.Busy)&&Time.realtimeSinceStartup<deadline)yield return null;
            OnlyChannel("-smoke");Check(Voice.OpenMicrophone,"open mic selection survives the private outdoor transition");
            var snapshot=PrivateSnapshot(hero.id);
            Check(snapshot.players.All(p=>p.room==3)&&snapshot.blackjackTable==null,"smoker receives no indoor players or table audio events");
            rounds.Start(players.Keys,Time.time,30,60,6);phase=rounds.Phase;PositionEveryone(true);Broadcast();
            Check(service.IsOutputDeviceMuted,"regroup mutes outdoor reception immediately");
            deadline=Time.realtimeSinceStartup+30;
            while((Voice.Channel!=VoiceRoom+"-alive"||Voice.Busy)&&Time.realtimeSinceStartup<deadline)yield return null;
            OnlyChannel("-alive");Check(Voice.OpenMicrophone,"open mic selection survives return indoors");
            hero.hp=0;Broadcast();deadline=Time.realtimeSinceStartup+30;
            while((Voice.Channel!=VoiceRoom+"-dead"||Voice.Busy)&&Time.realtimeSinceStartup<deadline)yield return null;
            OnlyChannel("-dead");Voice.Muted=true;
            Check(service.IsInputDeviceMuted,"mute overrides open mic");
            hero.deathAt=Time.time-SaloonRagdoll.ObserverDelay;Broadcast();
            Check(IsSpectating&&CameraPlayer.alive,"eliminated player is watching a living player");
            yield return SpectatorChannels("-alive");
            var previous=ViewpointId;
            bool AvatarAudioIs(string id,float spatialBlend)=>avatars[id].GetComponents<AudioSource>().All(source=>source.spatialBlend==spatialBlend);
            Check(AvatarAudioIs(previous,0),"watched player's game sounds use the first-person mix");
            CycleSpectator(1);yield return null;
            Check(ViewpointId!=previous&&!Voice.Busy&&!service.IsOutputDeviceMuted,"changing between indoor viewpoints keeps proximity playback active");
            Check(AvatarAudioIs(previous,1)&&AvatarAudioIs(ViewpointId,0),"changing viewpoints also changes the first-person game audio mix");

            var smoker=players.Values.First(p=>p!=hero);
            rounds.Start(players.Keys,Time.time,30,60,6);
            foreach(var p in players.Values){p.chosenRoom=p==smoker?3:0;rounds.SetReady(p.id,true);}
            rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);Broadcast();
            Check(View.players.All(p=>p.room!=3)&&Voice.SpectatorChannel==VoiceRoom+"-alive","indoor spectators stay isolated from the smoking deck");
            hero.room=3;Broadcast();
            Check(CameraPlayer?.room==3&&service.IsOutputDeviceMuted,"an outdoor spectator viewpoint mutes indoor playback before switching");
            yield return SpectatorChannels("-smoke");

            Finish("VOICE REMATCH TEST");deadline=Time.realtimeSinceStartup+30;
            while((Voice.Channel!=VoiceRoom+"-alive"||Voice.Busy)&&Time.realtimeSinceStartup<deadline)yield return null;
            OnlyChannel("-alive");Check(!Self.alive&&Voice.SpectatorChannel=="","eliminated player rejoins shared proximity for results voting");
            ReopenLobby();Broadcast();yield return null;
            OnlyChannel("-alive");Check(MenuOpen&&Voice.OpenMicrophone,"host setup retains proximity and open mic preference");
            var leave=Voice.Leave();while(!leave.IsCompleted)yield return null;
            Check(!Voice.OpenMicrophone&&!Voice.Transmitting&&service.ActiveChannels.Count==0,"leave clears open mic and all channels");
            VerificationFinished=VerificationPassed=true;Debug.Log("SALOON_VOICE_CONTROLS PASS");Application.Quit(0);
        }
    }
}
#endif
