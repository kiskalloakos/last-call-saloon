using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using Unity.Services.Core;
using UnityEngine;

namespace Saloon
{
    public class SaloonVoice : MonoBehaviour
    {
        public SaloonGame Game;
        public bool Enabled=true;
        bool muted,openMicrophone;
        public bool Muted { get=>muted; set {muted=value;ApplyTransmission();} }
        // An explicit, session-only opt-in. Never reopen the mic on a later join.
        public bool OpenMicrophone { get=>openMicrophone; set {openMicrophone=value;ApplyTransmission();} }
        public bool Connected { get; private set; }
        public bool Busy { get; private set; }
        public bool Recovering { get; private set; }
        public bool MicrophoneAllowed { get; private set; }
        public string Status="Voice connects when you enter the saloon. Hold V to speak, T toggles open mic, M mutes.";
        public string Channel=>voiceChannels.Speech??"";
        internal string SpectatorChannel=>voiceChannels.Spectator??"";
        public string MicrophoneName=>Connected?VivoxService.Instance.EffectiveInputDevice?.DeviceName??"No microphone detected":"Not connected";
        public bool Transmitting=>transmitting;
        public string MicrophoneStatus=>Game.RoleIntroActive?"VOICE PAUSED · ROLE REVEAL":Recovering?"VOICE RECONNECTING":Busy?"VOICE CONNECTING":!Connected?"VOICE OFFLINE":!MicrophoneAllowed?"MIC PERMISSION OFF":Muted?"MIC MUTED · M":MicrophonePaused?"MIC PAUSED IN MENU":OpenMicrophone?"OPEN MIC ON · T / M":transmitting?"MIC ON · V":"HOLD V · TALK / T · OPEN MIC";
        bool MicrophonePaused=>Game.RoleIntroActive||SaloonPrivacy.VoicePaused(Game.View?.phase??Phase.Lobby,Game.MenuOpen,Game.GraphicsMenuOpen);
        bool Eliminated=>SaloonPrivacy.VoiceEliminated(Game.View?.phase??Phase.Lobby,Game.Self?.alive==true);
        string ChannelStatus=>SpectatorChannel!=""?"You hear the player you're watching and the voices around them. Your microphone uses eliminated-player chat.":Eliminated?"Eliminated players' voice — living players cannot hear you.":Game.Self?.room==SaloonBathroom.Room?"Private solo bathroom — you are isolated until returning to Bar.":Game.OutsideForSmoke?"Private outside voice — only the people having a smoke can hear you; indoor audio is isolated.":"Indoor proximity voice — voices through room walls are muffled.";
        public string ControlHint=>OpenMicrophone?"Open mic selected. T switches back to push-to-talk; M mutes. Pause/settings menus during a match pause your mic; lobby and results keep voice on.":"Hold V to talk. T switches open mic on; M mutes. Pause/settings menus during a match pause your mic; lobby and results keep voice on.";
        (string Speech,string Spectator) voiceChannels;
        bool transmitting,autoAttempted,verificationMode,reconnectWanted,microphoneRequested,leaving;
        IVivoxService observedService;
        float nextRetry;
        int failedAttempts;
        float nextPosition;
        float nextIdentity;
        int generation;
        Task operation=Task.CompletedTask;
        CancellationTokenSource echoCancellation;

        void UpdateVoiceIdentity()
        {
            var service=VivoxService.Instance;
            // Associate a voice participant with their game avatar by ID, never display name.
            string id=service.SignedInPlayerId;
            if(Game.Self!=null&&Game.Self.voiceId!=id&&!string.IsNullOrEmpty(id)&&Time.unscaledTime>=nextIdentity)
            {Game.Connection.Send(new Command{kind="voice",name=id});nextIdentity=Time.unscaledTime+1;}
        }

        public void Connect()
        {
            if(Busy||Connected||Recovering||leaving||Game.Self==null||string.IsNullOrEmpty(Game.VoiceRoom))return;
            reconnectWanted=true;Enabled=true;
            operation=ConnectInternal(true);
        }

        // Exercises real cloud voice sessions without unmuting a user's microphone.
        internal Task ConnectForVerification()
        {
            verificationMode=true;reconnectWanted=true;Enabled=true;
            return operation=ConnectInternal(false);
        }

        async Task Prepare(bool requestMicrophone,int version)
        {
            if(string.IsNullOrEmpty(Application.cloudProjectId))throw new InvalidOperationException("This build is missing its Unity cloud project link.");
            if(requestMicrophone)
            {
                bool mac=Application.platform==RuntimePlatform.OSXPlayer||Application.platform==RuntimePlatform.OSXEditor;
                if(mac&&!microphoneRequested&&!Application.HasUserAuthorization(UserAuthorization.Microphone))
                {microphoneRequested=true;await RequestMicrophone();}
                MicrophoneAllowed=!mac||Application.HasUserAuthorization(UserAuthorization.Microphone);
            }
            CheckCurrent(version);
            await SaloonConnection.EnsureServices();CheckCurrent(version);
            if(VivoxService.Instance.InitializationState!=VivoxInitializationState.Initialized)await VivoxService.Instance.InitializeAsync();
            CheckCurrent(version);
            ObserveService(VivoxService.Instance);
            VivoxService.Instance.MuteInputDevice();VivoxService.Instance.MuteOutputDevice();
            if(!VivoxService.Instance.IsLoggedIn)
                await VivoxService.Instance.LoginAsync(new LoginOptions{DisplayName=Game.PlayerName,EnableTTS=false,ParticipantUpdateFrequency=ParticipantPropertyUpdateFrequency.TenPerSecond});
            CheckCurrent(version);
            VivoxService.Instance.MuteInputDevice();
            VivoxService.Instance.EnableAcousticEchoCancellation();
            await VivoxService.Instance.EnableAutoVoiceActivityDetectionAsync();CheckCurrent(version);
        }

        async Task ConnectInternal(bool requestMicrophone)
        {
            if(Busy||Connected||Recovering||leaving||!Game.InRoom)return;
            Busy=true;int version=generation;
            try
            {
                Status="Connecting proximity voice...";
                await Prepare(requestMicrophone,version);
                await JoinVoiceChannels(DesiredChannels,version);CheckCurrent(version);
                Connected=true;Enabled=true;
                failedAttempts=0;
                Status=MicrophoneAllowed?ChannelStatus:"Voice connected for listening; microphone permission is off.";
                Debug.Log("SALOON_VOICE CONNECTED "+Channel);
            }
            catch(OperationCanceledException){if(version==generation)ConnectionLost("Voice connection interrupted.");}
            catch(Exception e){if(version==generation)ConnectionLost("Voice connection: "+e.Message);}
            finally{Busy=false;}
        }

        (string Speech,string Spectator) DesiredChannels
        {
            get
            {
                var phase=Game.View?.phase??Phase.Lobby;
                var self=Game.Self;
                var speech=SaloonPrivacy.VoiceChannel(Game.VoiceRoom,phase,self?.room??0,self?.alive==true);
                // Use the same roster and area restrictions as the spectator camera.
                var target=Eliminated&&Game.IsSpectating?Game.CameraPlayer:null;
                var spectator=target==null?"":SaloonPrivacy.VoiceChannel(Game.VoiceRoom,phase,target.room,true);
                return (speech,spectator);
            }
        }

        static bool IsPositional(string channel)=>channel.EndsWith("-alive",StringComparison.Ordinal);

        static Task JoinChannel(string channel)
        {
            var options=new ChannelOptions{MakeActiveChannelUponJoining=false};
            return IsPositional(channel)
                ?VivoxService.Instance.JoinPositionalChannelAsync(channel,ChatCapability.AudioOnly,new Channel3DProperties(10,2,1,AudioFadeModel.LinearByDistance),options)
                :VivoxService.Instance.JoinGroupChannelAsync(channel,ChatCapability.AudioOnly,options);
        }

        async Task JoinVoiceChannels((string Speech,string Spectator) desired,int version)
        {
            var service=VivoxService.Instance;
            ClearVoicePlayback();
            await service.LeaveAllChannelsAsync();CheckCurrent(version);
            // Vivox requires the positional channel to be joined before any group channel.
            if(desired.Spectator!="")
            {
                await JoinChannel(desired.Spectator);CheckCurrent(version);
            }
            await JoinChannel(desired.Speech);CheckCurrent(version);
            await service.SetChannelTransmissionModeAsync(TransmissionMode.Single,desired.Speech);CheckCurrent(version);
            voiceChannels=desired;
            RefreshListener();
        }

        Task RequestMicrophone()
        {
            var completion=new TaskCompletionSource<bool>();StartCoroutine(Request());return completion.Task;
            IEnumerator Request(){yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);completion.TrySetResult(true);}
        }

        void CheckCurrent(int version)
        {if(version!=generation||Recovering)throw new OperationCanceledException();}

        void ObserveService(IVivoxService service)
        {
            if(observedService==service)return;
            StopObservingService();observedService=service;
            service.ConnectionRecovering+=OnConnectionRecovering;
            service.ConnectionRecovered+=OnConnectionRecovered;
            service.ConnectionFailedToRecover+=OnConnectionFailedToRecover;
        }

        void StopObservingService()
        {
            if(observedService==null)return;
            observedService.ConnectionRecovering-=OnConnectionRecovering;
            observedService.ConnectionRecovered-=OnConnectionRecovered;
            observedService.ConnectionFailedToRecover-=OnConnectionFailedToRecover;
            observedService=null;
        }

        void OnConnectionRecovering()
        {
            Recovering=true;MuteForSwitch();echoCancellation?.Cancel();
            Status="Voice connection interrupted. Reconnecting...";
        }

        void OnConnectionRecovered()
        {
            Recovering=false;RefreshListener();
            Status=Connected?ChannelStatus:"Voice connection restored. Rejoining voice...";
        }

        void OnConnectionFailedToRecover()
        {
            Recovering=false;ConnectionLost("Voice connection lost.");
        }

        void ConnectionLost(string reason)
        {
            if(observedService?.InitializationState==VivoxInitializationState.Initialized)MuteForSwitch();
            Connected=false;voiceChannels=default;
            float delay=Mathf.Min(30,5*Mathf.Pow(2,Mathf.Min(failedAttempts++,3)));
            nextRetry=Time.unscaledTime+delay;
            Status=reason+(reconnectWanted&&!leaving?" Retrying in "+delay+" seconds.":"");
            Debug.LogWarning("SALOON_VOICE "+Status);
        }

        bool ChannelsActive=>observedService?.IsLoggedIn==true&&!string.IsNullOrEmpty(Channel)&&
            observedService.ActiveChannels.ContainsKey(Channel)&&(SpectatorChannel==""||observedService.ActiveChannels.ContainsKey(SpectatorChannel));

        public void ValidateArea()
        {
            if(!Connected||(!Game.RoleIntroActive&&voiceChannels==DesiredChannels))return;
            MuteForSwitch();
        }

        internal void RefreshListener()=>nextPosition=0;

        void MuteForSwitch()
        {
            VivoxService.Instance.MuteInputDevice();VivoxService.Instance.MuteOutputDevice();transmitting=false;
            foreach(var audio in voicePlayback.Values)if(audio!=null)audio.mute=true;
        }

        internal static bool WantsTransmission(bool openMic,bool pushToTalk,bool microphoneMuted,bool menuOpen)
            =>!microphoneMuted&&!menuOpen&&(openMic||pushToTalk);

        void ApplyTransmission()
        {
            if(!Connected||Busy||Recovering||verificationMode)return;
            bool talk=MicrophoneAllowed&&Enabled&&voiceChannels==DesiredChannels&&WantsTransmission(OpenMicrophone,Input.GetKey(KeyCode.V),Muted,MicrophonePaused);
            var service=VivoxService.Instance;
            if(talk==transmitting&&service.IsInputDeviceMuted==!talk)return;
            if(talk)service.UnmuteInputDevice();else service.MuteInputDevice();
            transmitting=talk&&!service.IsInputDeviceMuted;
        }

        void Update()
        {
            if(!leaving&&!autoAttempted&&!Game.Connection.Solo&&!Application.isBatchMode&&!Game.AutomatedInput&&Game.InRoom&&Game.Self!=null&&!string.IsNullOrEmpty(Game.VoiceRoom))
            {autoAttempted=true;Connect();}
            if(!Connected&&!Busy&&!Recovering&&!leaving&&Enabled&&reconnectWanted&&Game.InRoom&&Game.Self!=null&&!string.IsNullOrEmpty(Game.VoiceRoom)&&Time.unscaledTime>=nextRetry)
            {operation=ConnectInternal(!verificationMode);return;}
            if(!Connected||Busy||Recovering||!Enabled)return;
            if(!ChannelsActive){ConnectionLost("Voice channel disconnected.");return;}
            if(!verificationMode)
            {
                bool mac=Application.platform==RuntimePlatform.OSXPlayer||Application.platform==RuntimePlatform.OSXEditor;
                MicrophoneAllowed=!mac||Application.HasUserAuthorization(UserAuthorization.Microphone);
            }
            var desired=DesiredChannels;
            if(voiceChannels!=desired){operation=SwitchVoiceChannels(desired);return;}
            if(!verificationMode&&!MicrophonePaused)
            {
                if(Input.GetKeyDown(KeyCode.M))Muted=!Muted;
                if(Input.GetKeyDown(KeyCode.T))OpenMicrophone=!OpenMicrophone;
            }
            UpdateVoiceIdentity();
        }

        void LateUpdate()
        {
            if(!Connected||Busy||Recovering||!Enabled||voiceChannels!=DesiredChannels)return;
            if(Game.RoleIntroActive){MuteForSwitch();return;}
            var service=VivoxService.Instance;
            UpdateVoicePlayback();
            if(Time.unscaledTime>=nextPosition||service.IsOutputDeviceMuted)
            {
                // Follow the same camera and orientation as the world audio, after any spectator cut.
                string playback=SpectatorChannel!=""?SpectatorChannel:Channel;
                if(IsPositional(playback)&&Game.ViewCamera!=null)
                    service.Set3DPosition(Game.ViewCamera.gameObject,playback);
                nextPosition=Time.unscaledTime+.1f;
                if(service.IsOutputDeviceMuted)service.UnmuteOutputDevice();
            }
            ApplyTransmission();
        }

        readonly Dictionary<VivoxParticipant,AudioSource> voicePlayback=new Dictionary<VivoxParticipant,AudioSource>();
        internal static void ConfigureVoicePlayback(AudioSource audio,AudioLowPassFilter filter,bool muffled,bool silent,bool positional)
        {
            audio.mute=silent;audio.volume=muffled?.28f:1;audio.spatialBlend=positional?1:0;
            audio.rolloffMode=AudioRolloffMode.Linear;audio.minDistance=2;audio.maxDistance=10;
            filter.enabled=muffled;filter.cutoffFrequency=850;filter.lowpassResonanceQ=1;
        }
        void UpdateVoicePlayback()
        {
            var service=VivoxService.Instance;
            var active=service.ActiveChannels.Values.SelectMany(p=>p).Where(p=>!p.IsSelf).ToArray();
            foreach(var old in voicePlayback.Keys.Where(p=>!active.Contains(p)).ToArray())
            {old.DestroyVivoxParticipantTap();voicePlayback.Remove(old);}
            var listener=Game.CameraPlayer;
            foreach(var participant in active)
            {
                if(!voicePlayback.TryGetValue(participant,out var audio)||audio==null)
                {
                    var tap=participant.CreateVivoxParticipantTap("Room voice playback",true);
                    if(tap==null)continue;
                    audio=tap.GetComponent<AudioSource>();audio.mute=true;
                    tap.AddComponent<AudioLowPassFilter>();voicePlayback[participant]=audio;
                }
                bool positional=IsPositional(participant.ChannelName);
                var speaker=Game.View?.players?.FirstOrDefault(p=>p.voiceId==participant.PlayerId);
                bool muffled=positional&&listener!=null&&speaker!=null&&SaloonPrivacy.MuffledVoice(Game.View.phase,listener.room,speaker.room);
                ConfigureVoicePlayback(audio,audio.GetComponent<AudioLowPassFilter>(),muffled,
                    Game.RoleIntroActive||participant.IsMuted||(positional&&(listener==null||speaker==null)),positional);
                if(speaker!=null)audio.transform.position=new Vector3(speaker.x,speaker.y+1.5f,speaker.z);
            }
        }
        void ClearVoicePlayback()
        {
            foreach(var p in voicePlayback.Keys)p.DestroyVivoxParticipantTap();
            voicePlayback.Clear();
        }

        async Task SwitchVoiceChannels((string Speech,string Spectator) desired)
        {
            Busy=true;int version=generation;
            try
            {
                MuteForSwitch();
                await JoinVoiceChannels(desired,version);CheckCurrent(version);
                Status=ChannelStatus;
                Debug.Log("SALOON_VOICE CHANNEL "+Channel);
            }
            catch(OperationCanceledException){if(version==generation)ConnectionLost("Voice channel switch interrupted.");}
            catch(Exception e){if(version==generation)ConnectionLost("Voice channel: "+e.Message);}
            finally{Busy=false;}
        }

        public void EchoTest()
        {
            if(Game.RoleIntroActive||Busy||Recovering||leaving||(Connected&&voiceChannels!=DesiredChannels))return;
            operation=RunEchoTest();
        }

        async Task RunEchoTest()
        {
            Busy=true;int version=generation;string echo="saloon-echo-"+Guid.NewGuid().ToString("N");
            echoCancellation=new CancellationTokenSource();
            try
            {
                await Prepare(true,version);
                if(!MicrophoneAllowed)throw new InvalidOperationException("Allow microphone access in your operating-system settings first.");
                await VivoxService.Instance.JoinEchoChannelAsync(echo,ChatCapability.AudioOnly,new ChannelOptions{MakeActiveChannelUponJoining=false});CheckCurrent(version);
                await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.Single,echo);CheckCurrent(version);
                if(Game.RoleIntroActive||(Connected&&voiceChannels!=DesiredChannels))return;
                VivoxService.Instance.UnmuteOutputDevice();VivoxService.Instance.UnmuteInputDevice();Status="Echo test: speak now — only you hear this, for eight seconds.";
                await Task.Delay(8000,echoCancellation.Token);
                CheckCurrent(version);Status="Microphone test finished. "+ControlHint;
            }
            catch(OperationCanceledException) { }
            catch(Exception e){Status="Microphone test: "+e.Message;Debug.LogWarning(Status);}
            finally
            {
                if(VivoxService.Instance.IsLoggedIn)
                {
                    VivoxService.Instance.MuteInputDevice();transmitting=false;
                    try
                    {
                        if(!Recovering)
                        {
                            await VivoxService.Instance.LeaveChannelAsync(echo);
                            if(Connected&&version==generation&&!Recovering)await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.Single,Channel);
                        }
                        else if(Connected)ConnectionLost("Microphone test interrupted by a voice disconnect.");
                    }
                    catch(Exception e){Debug.LogWarning("Echo cleanup: "+e.Message);}
                }
                echoCancellation.Dispose();echoCancellation=null;Busy=false;
            }
        }

        public void CycleMicrophone()
        {
            if(!Connected||Busy||Recovering||leaving)return;
            operation=ChangeMicrophone();
        }

        async Task ChangeMicrophone()
        {
            var devices=VivoxService.Instance.AvailableInputDevices.ToArray();if(devices.Length==0)return;
            Busy=true;int version=generation;MuteForSwitch();
            int current=Array.FindIndex(devices,d=>d.DeviceID==VivoxService.Instance.ActiveInputDevice?.DeviceID);
            try{await devices[(current+1)%devices.Length].SetActiveDeviceAsync();CheckCurrent(version);RefreshListener();Status="Microphone: "+MicrophoneName;}
            catch(OperationCanceledException) { }
            catch(Exception e){Status="Microphone selection: "+e.Message;}
            finally{Busy=false;}
        }

        public async Task Leave()
        {
            ++generation;leaving=true;reconnectWanted=false;Enabled=false;openMicrophone=false;echoCancellation?.Cancel();
            if(Connected)MuteForSwitch();
            try{await operation;}catch(Exception e){Debug.LogWarning(e.Message);}
            // LAN/Editor tests may never initialize cloud services; don't resolve Vivox in that case.
            if(UnityServices.State==ServicesInitializationState.Initialized)
            {
                try
                {
                    var vivox=VivoxService.Instance;
                    if(vivox!=null&&vivox.InitializationState==VivoxInitializationState.Initialized&&vivox.IsLoggedIn)
                    {vivox.MuteInputDevice();vivox.MuteOutputDevice();if(!Recovering)await vivox.LeaveAllChannelsAsync();}
                }
                catch(Exception e){Debug.LogWarning("Voice cleanup: "+e.Message);}
            }
            ClearVoicePlayback();
            Connected=false;voiceChannels=default;transmitting=false;Busy=false;autoAttempted=false;verificationMode=false;MicrophoneAllowed=false;
            StopObservingService();Recovering=false;leaving=false;microphoneRequested=false;failedAttempts=0;nextRetry=0;nextIdentity=0;
            Status="Voice connects when you enter the saloon. Hold V to speak, T toggles open mic, M mutes.";
        }

        void OnDestroy(){ClearVoicePlayback();StopObservingService();}
    }
}
