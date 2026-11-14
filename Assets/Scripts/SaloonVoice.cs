using System;
using System.Collections;
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
        public bool MicrophoneAllowed { get; private set; }
        public string Status="Voice connects when you enter the saloon. Hold V to speak, T toggles open mic, M mutes.";
        public string Channel=>channel;
        public string MicrophoneName=>Connected?VivoxService.Instance.EffectiveInputDevice?.DeviceName??"No microphone detected":"Not connected";
        public bool Transmitting=>transmitting;
        public string MicrophoneStatus=>!Connected?"VOICE OFFLINE":Busy?"VOICE CONNECTING":!MicrophoneAllowed?"MIC PERMISSION OFF":Muted?"MIC MUTED · M":MicrophonePaused?"MIC PAUSED IN MENU":OpenMicrophone?"OPEN MIC ON · T / M":transmitting?"MIC ON · V":"HOLD V · TALK / T · OPEN MIC";
        bool MicrophonePaused=>SaloonPrivacy.VoicePaused(Game.View?.phase??Phase.Lobby,Game.MenuOpen,Game.GraphicsMenuOpen);
        bool Eliminated=>SaloonPrivacy.VoiceEliminated(Game.View?.phase??Phase.Lobby,Game.Self?.alive==true);
        string ChannelStatus=>deadChannel?"Eliminated players' voice — living players cannot hear you.":outdoorChannel?"Private outside voice — only the people having a smoke can hear you; indoor audio is isolated.":"Indoor proximity voice — nearby players can hear you.";
        public string ControlHint=>OpenMicrophone?"Open mic selected. T switches back to push-to-talk; M mutes. Pause/settings menus during a match pause your mic; lobby and results keep voice on.":"Hold V to talk. T switches open mic on; M mutes. Pause/settings menus during a match pause your mic; lobby and results keep voice on.";
        string channel="";
        bool deadChannel,outdoorChannel,transmitting,autoAttempted,verificationMode;
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
            if(Busy||Connected||Game.Self==null||string.IsNullOrEmpty(Game.VoiceRoom))return;
            operation=ConnectInternal(true);
        }

        // Exercises real cloud voice sessions without unmuting a user's microphone.
        internal Task ConnectForVerification()
        {
            verificationMode=true;
            return operation=ConnectInternal(false);
        }

        async Task Prepare(bool requestMicrophone,int version)
        {
            if(string.IsNullOrEmpty(Application.cloudProjectId))throw new InvalidOperationException("This build is missing its Unity cloud project link.");
            if(requestMicrophone)
            {
                bool mac=Application.platform==RuntimePlatform.OSXPlayer||Application.platform==RuntimePlatform.OSXEditor;
                if(mac&&!Application.HasUserAuthorization(UserAuthorization.Microphone))await RequestMicrophone();
                MicrophoneAllowed=!mac||Application.HasUserAuthorization(UserAuthorization.Microphone);
            }
            CheckCurrent(version);
            await SaloonConnection.EnsureServices();CheckCurrent(version);
            if(VivoxService.Instance.InitializationState!=VivoxInitializationState.Initialized)await VivoxService.Instance.InitializeAsync();
            CheckCurrent(version);
            VivoxService.Instance.MuteInputDevice();VivoxService.Instance.MuteOutputDevice();
            if(!VivoxService.Instance.IsLoggedIn)
                await VivoxService.Instance.LoginAsync(new LoginOptions{DisplayName=Game.PlayerName,EnableTTS=false,ParticipantUpdateFrequency=ParticipantPropertyUpdateFrequency.TenPerSecond});
            CheckCurrent(version);
            VivoxService.Instance.MuteInputDevice();
            VivoxService.Instance.EnableAcousticEchoCancellation();
        }

        async Task ConnectInternal(bool requestMicrophone)
        {
            if(Busy||Connected||!Game.InRoom)return;
            Busy=true;int version=generation;
            try
            {
                Status="Connecting proximity voice...";
                await Prepare(requestMicrophone,version);
                deadChannel=Eliminated;
                await VivoxService.Instance.LeaveAllChannelsAsync();CheckCurrent(version);
                await JoinLifeChannel(deadChannel,!deadChannel&&Game.OutsideForSmoke);CheckCurrent(version);
                Connected=true;Enabled=true;
                Status=MicrophoneAllowed?ChannelStatus:"Voice connected for listening; microphone permission is off.";
                Debug.Log("SALOON_VOICE CONNECTED "+(deadChannel?"dead":outdoorChannel?"smoke":"positional"));
            }
            catch(OperationCanceledException) { }
            catch(Exception e){Status="Voice connection: "+e.Message;Debug.LogWarning(Status);}
            finally{Busy=false;}
        }

        string DesiredChannel=>SaloonPrivacy.VoiceChannel(Game.VoiceRoom,Game.View?.phase??Phase.Lobby,Game.Self?.room??0,Game.Self?.alive==true);

        async Task JoinLifeChannel(bool dead,bool outdoors)
        {
            deadChannel=dead;outdoorChannel=!dead&&outdoors;
            channel=Game.VoiceRoom+(dead?"-dead":outdoorChannel?"-smoke":"-alive");
            var options=new ChannelOptions{MakeActiveChannelUponJoining=false};
            if(dead||outdoorChannel)await VivoxService.Instance.JoinGroupChannelAsync(channel,ChatCapability.AudioOnly,options);
            else await VivoxService.Instance.JoinPositionalChannelAsync(channel,ChatCapability.AudioOnly,new Channel3DProperties(10,2,1,AudioFadeModel.LinearByDistance),options);
            await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.Single,channel);
            VivoxService.Instance.MuteInputDevice();transmitting=false;
            UpdatePosition();
            if(channel==DesiredChannel)VivoxService.Instance.UnmuteOutputDevice();
        }

        Task RequestMicrophone()
        {
            var completion=new TaskCompletionSource<bool>();StartCoroutine(Request());return completion.Task;
            IEnumerator Request(){yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);completion.TrySetResult(true);}
        }

        void CheckCurrent(int version)
        {if(version!=generation)throw new OperationCanceledException();}

        public void ValidateArea()
        {
            if(!Connected||channel==DesiredChannel)return;
            VivoxService.Instance.MuteInputDevice();VivoxService.Instance.MuteOutputDevice();transmitting=false;
        }

        internal static bool WantsTransmission(bool openMic,bool pushToTalk,bool microphoneMuted,bool menuOpen)
            =>!microphoneMuted&&!menuOpen&&(openMic||pushToTalk);

        void ApplyTransmission()
        {
            if(!Connected||Busy||verificationMode)return;
            bool talk=MicrophoneAllowed&&Enabled&&channel==DesiredChannel&&WantsTransmission(OpenMicrophone,Input.GetKey(KeyCode.V),Muted,MicrophonePaused);
            if(talk==transmitting)return;
            transmitting=talk;
            if(talk)VivoxService.Instance.UnmuteInputDevice();else VivoxService.Instance.MuteInputDevice();
        }

        void Update()
        {
            if(!autoAttempted&&!Game.Connection.Solo&&!Application.isBatchMode&&!Game.AutomatedInput&&Game.InRoom&&Game.Self!=null&&!string.IsNullOrEmpty(Game.VoiceRoom))
            {autoAttempted=true;Connect();}
            if(!Connected||Busy)return;
            bool alive=!Eliminated;
            if(channel!=DesiredChannel){operation=SwitchLifeChannel(!alive,alive&&Game.OutsideForSmoke);return;}
            if(!verificationMode)
            {
                if(!MicrophonePaused)
                {
                    if(Input.GetKeyDown(KeyCode.M))Muted=!Muted;
                    if(Input.GetKeyDown(KeyCode.T))OpenMicrophone=!OpenMicrophone;
                }
                ApplyTransmission();
            }
            if(Time.unscaledTime>=nextPosition){UpdatePosition();nextPosition=Time.unscaledTime+.1f;}
            UpdateVoiceIdentity();
        }

        void UpdatePosition()
        {
            if(deadChannel||outdoorChannel||string.IsNullOrEmpty(channel)||Game.ViewCamera==null)return;
            var t=Game.ViewCamera.transform;
            VivoxService.Instance.Set3DPosition(t.position,t.position,t.forward,t.up,channel);
        }

        async Task SwitchLifeChannel(bool dead,bool outdoors)
        {
            Busy=true;int version=generation;
            try
            {
                VivoxService.Instance.MuteInputDevice();VivoxService.Instance.MuteOutputDevice();transmitting=false;
                await VivoxService.Instance.LeaveChannelAsync(channel);CheckCurrent(version);
                deadChannel=dead;
                await JoinLifeChannel(dead,outdoors);CheckCurrent(version);
                Status=ChannelStatus;
                Debug.Log("SALOON_VOICE CHANNEL "+(dead?"dead":outdoorChannel?"smoke":"positional"));
            }
            catch(OperationCanceledException) { }
            catch(Exception e){Connected=false;Status="Voice channel: "+e.Message;Debug.LogWarning(Status);}
            finally{Busy=false;}
        }

        public void EchoTest()
        {
            if(Busy||(Connected&&channel!=DesiredChannel))return;
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
                await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.Single,echo);
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
                        await VivoxService.Instance.LeaveChannelAsync(echo);
                        if(Connected&&version==generation)await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.Single,channel);
                    }
                    catch(Exception e){Debug.LogWarning("Echo cleanup: "+e.Message);}
                }
                echoCancellation.Dispose();echoCancellation=null;Busy=false;
            }
        }

        public async void CycleMicrophone()
        {
            if(!Connected||Busy)return;
            var devices=VivoxService.Instance.AvailableInputDevices.ToArray();if(devices.Length==0)return;
            int current=Array.FindIndex(devices,d=>d.DeviceID==VivoxService.Instance.ActiveInputDevice?.DeviceID);
            try{await devices[(current+1)%devices.Length].SetActiveDeviceAsync();Status="Microphone: "+MicrophoneName;}
            catch(Exception e){Status="Microphone selection: "+e.Message;}
        }

        public async Task Leave()
        {
            ++generation;Enabled=false;openMicrophone=false;echoCancellation?.Cancel();
            try{await operation;}catch(Exception e){Debug.LogWarning(e.Message);}
            // LAN/Editor tests may never initialize cloud services; don't resolve Vivox in that case.
            if(UnityServices.State==ServicesInitializationState.Initialized)
            {
                try
                {
                    var vivox=VivoxService.Instance;
                    if(vivox!=null&&vivox.InitializationState==VivoxInitializationState.Initialized&&vivox.IsLoggedIn)
                    {vivox.MuteInputDevice();await vivox.LeaveAllChannelsAsync();}
                }
                catch(Exception e){Debug.LogWarning("Voice cleanup: "+e.Message);}
            }
            Connected=false;channel="";transmitting=false;Busy=false;autoAttempted=false;verificationMode=false;MicrophoneAllowed=false;
            nextIdentity=0;
            Status="Voice connects when you enter the saloon. Hold V to speak, T toggles open mic, M mutes.";
        }
    }
}
