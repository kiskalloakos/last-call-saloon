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
        public bool Enabled=true,Muted;
        public bool Connected { get; private set; }
        public bool Busy { get; private set; }
        public bool MicrophoneAllowed { get; private set; }
        public string Status="Voice connects when you enter the saloon. Hold V to speak; M mutes your microphone.";
        public string Channel=>channel;
        public string MicrophoneName=>Connected?VivoxService.Instance.EffectiveInputDevice?.DeviceName??"No microphone detected":"Not connected";
        public bool Transmitting=>transmitting;
        string channel="";
        bool deadChannel,transmitting,autoAttempted,verificationMode;
        float nextPosition;
        int generation;
        Task operation=Task.CompletedTask;
        CancellationTokenSource echoCancellation;

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
            VivoxService.Instance.MuteInputDevice();
            if(!VivoxService.Instance.IsLoggedIn)
                await VivoxService.Instance.LoginAsync(new LoginOptions{DisplayName=Game.PlayerName,EnableTTS=false,ParticipantUpdateFrequency=ParticipantPropertyUpdateFrequency.FivePerSecond});
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
                deadChannel=Game.Self!=null&&!Game.Self.alive;
                await JoinLifeChannel(deadChannel);CheckCurrent(version);
                Connected=true;Enabled=true;
                Status=MicrophoneAllowed?"Proximity voice ready — hold V to speak; M toggles microphone mute.":"Voice connected for listening; microphone permission is off.";
                Debug.Log("SALOON_VOICE CONNECTED "+(deadChannel?"dead":"positional"));
            }
            catch(OperationCanceledException) { }
            catch(Exception e){Status="Voice connection: "+e.Message;Debug.LogWarning(Status);}
            finally{Busy=false;}
        }

        async Task JoinLifeChannel(bool dead)
        {
            channel=Game.VoiceRoom+(dead?"-dead":"-alive");
            var options=new ChannelOptions{MakeActiveChannelUponJoining=false};
            if(dead)await VivoxService.Instance.JoinGroupChannelAsync(channel,ChatCapability.AudioOnly,options);
            else await VivoxService.Instance.JoinPositionalChannelAsync(channel,ChatCapability.AudioOnly,new Channel3DProperties(10,2,1,AudioFadeModel.LinearByDistance),options);
            await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.Single,channel);
            VivoxService.Instance.MuteInputDevice();transmitting=false;
            UpdatePosition();
        }

        Task RequestMicrophone()
        {
            var completion=new TaskCompletionSource<bool>();StartCoroutine(Request());return completion.Task;
            IEnumerator Request(){yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);completion.TrySetResult(true);}
        }

        void CheckCurrent(int version)
        {if(version!=generation)throw new OperationCanceledException();}

        void Update()
        {
            if(!autoAttempted&&!Application.isBatchMode&&!Game.AutomatedInput&&Game.InRoom&&Game.Self!=null&&!string.IsNullOrEmpty(Game.VoiceRoom))
            {autoAttempted=true;Connect();}
            if(!Connected||Busy)return;
            bool alive=Game.Self!=null&&Game.Self.alive;
            if(deadChannel==alive){operation=SwitchLifeChannel(!alive);return;}
            if(!verificationMode)
            {
                if(!Game.MenuOpen&&Input.GetKeyDown(KeyCode.M))Muted=!Muted;
                bool talk=MicrophoneAllowed&&Enabled&&!Muted&&!Game.MenuOpen&&Input.GetKey(KeyCode.V);
                if(talk!=transmitting)
                {
                    transmitting=talk;
                    if(talk)VivoxService.Instance.UnmuteInputDevice();else VivoxService.Instance.MuteInputDevice();
                }
            }
            if(Time.unscaledTime>=nextPosition){UpdatePosition();nextPosition=Time.unscaledTime+.1f;}
        }

        void UpdatePosition()
        {
            if(deadChannel||string.IsNullOrEmpty(channel)||Game.ViewCamera==null)return;
            var t=Game.ViewCamera.transform;
            VivoxService.Instance.Set3DPosition(t.position,t.position,t.forward,t.up,channel);
        }

        async Task SwitchLifeChannel(bool dead)
        {
            Busy=true;int version=generation;
            try
            {
                VivoxService.Instance.MuteInputDevice();transmitting=false;
                await VivoxService.Instance.LeaveChannelAsync(channel);CheckCurrent(version);
                deadChannel=dead;
                await JoinLifeChannel(dead);CheckCurrent(version);
                Status=dead?"Eliminated players' voice — living players cannot hear you.":"Proximity voice ready — hold V to speak.";
                Debug.Log("SALOON_VOICE CHANNEL "+(dead?"dead":"positional"));
            }
            catch(OperationCanceledException) { }
            catch(Exception e){Connected=false;Status="Voice channel: "+e.Message;Debug.LogWarning(Status);}
            finally{Busy=false;}
        }

        public void EchoTest()
        {
            if(Busy)return;
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
                VivoxService.Instance.UnmuteInputDevice();Status="Echo test: speak now — only you hear this, for eight seconds.";
                await Task.Delay(8000,echoCancellation.Token);
                CheckCurrent(version);Status="Microphone test finished — hold V to speak in the saloon.";
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
            ++generation;Enabled=false;echoCancellation?.Cancel();
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
            Status="Voice connects when you enter the saloon. Hold V to speak.";
        }
    }
}
