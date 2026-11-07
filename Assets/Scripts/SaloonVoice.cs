using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using UnityEngine;

namespace Saloon
{
    public class SaloonVoice : MonoBehaviour
    {
        public SaloonGame Game;
        public bool Enabled,Muted;
        public bool Connected { get; private set; }
        public bool Busy { get; private set; }
        public string Status="Voice needs a linked Unity Services project; hold V to speak once connected.";
        string channel="";
        bool deadChannel,transmitting;
        float nextPosition;
        public string Channel=>channel;

        public async void Connect()
        {
            if(Busy||Connected||!Game.InRoom)return;
            Busy=true;
            try
            {
                if(string.IsNullOrEmpty(Application.cloudProjectId))throw new InvalidOperationException("Link the Unity Services project and enable Vivox first; see Documentation/CLOUD-SETUP.md.");
                if(Application.platform==RuntimePlatform.OSXPlayer||Application.platform==RuntimePlatform.OSXEditor)
                {
                    if(!Application.HasUserAuthorization(UserAuthorization.Microphone))
                    {
                        await RequestMicrophone();
                        if(!Application.HasUserAuthorization(UserAuthorization.Microphone))throw new InvalidOperationException("Microphone access was denied in macOS Privacy & Security.");
                    }
                }
                await SaloonConnection.EnsureServices();
                await VivoxService.Instance.InitializeAsync();
                if(!VivoxService.Instance.IsLoggedIn)await VivoxService.Instance.LoginAsync(new LoginOptions{DisplayName=Game.PlayerName});
                deadChannel=Game.Self!=null&&!Game.Self.alive;
                channel=Game.VoiceRoom+(deadChannel?"-dead":"-alive");
                if(deadChannel)await VivoxService.Instance.JoinGroupChannelAsync(channel,ChatCapability.AudioOnly);
                else await VivoxService.Instance.JoinPositionalChannelAsync(channel,ChatCapability.AudioOnly,new Channel3DProperties(10,2,1,AudioFadeModel.LinearByDistance));
                VivoxService.Instance.MuteInputDevice();Enabled=true;Connected=true;
                Status="Proximity voice ready — hold V to speak; M toggles microphone mute.";
            }
            catch(Exception e){Status="Voice setup: "+e.Message;Debug.LogWarning(Status);}
            finally{Busy=false;}
        }

        Task RequestMicrophone()
        {
            var completion=new TaskCompletionSource<bool>();StartCoroutine(Request());return completion.Task;
            IEnumerator Request(){yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);completion.SetResult(true);}
        }

        void Update()
        {
            if(!Connected||Busy)return;
            if(Input.GetKeyDown(KeyCode.M))Muted=!Muted;
            bool alive=Game.Self!=null&&Game.Self.alive;
            if(deadChannel==alive){SwitchLifeChannel(!alive);return;}
            bool talk=Enabled&&!Muted&&!Game.MenuOpen&&Input.GetKey(KeyCode.V);
            if(talk!=transmitting)
            {
                transmitting=talk;
                if(talk)VivoxService.Instance.UnmuteInputDevice();else VivoxService.Instance.MuteInputDevice();
            }
            if(!deadChannel&&Time.unscaledTime>=nextPosition&&Game.ViewCamera!=null)
            {
                var t=Game.ViewCamera.transform;
                VivoxService.Instance.Set3DPosition(t.position,t.position,t.forward,t.up,channel);
                nextPosition=Time.unscaledTime+.1f;
            }
        }

        async void SwitchLifeChannel(bool dead)
        {
            Busy=true;
            try
            {
                VivoxService.Instance.MuteInputDevice();transmitting=false;
                await VivoxService.Instance.LeaveChannelAsync(channel);
                deadChannel=dead;channel=Game.VoiceRoom+(dead?"-dead":"-alive");
                if(dead)await VivoxService.Instance.JoinGroupChannelAsync(channel,ChatCapability.AudioOnly);
                else await VivoxService.Instance.JoinPositionalChannelAsync(channel,ChatCapability.AudioOnly,new Channel3DProperties(10,2,1,AudioFadeModel.LinearByDistance));
                Status=dead?"Eliminated players' voice channel — living players cannot hear you.":"Proximity voice ready — hold V.";
            }
            catch(Exception e){Connected=false;Status=e.Message;}
            finally{Busy=false;}
        }

        public async void EchoTest()
        {
            if(Busy||Connected){Status="Leave the room before running the microphone test.";return;}
            if(string.IsNullOrEmpty(Application.cloudProjectId)){Status="Microphone test needs the linked Vivox project first.";return;}
            Busy=true;
            try
            {
                await RequestMicrophone();await SaloonConnection.EnsureServices();await VivoxService.Instance.InitializeAsync();
                if(!VivoxService.Instance.IsLoggedIn)await VivoxService.Instance.LoginAsync(new LoginOptions{DisplayName=Game.PlayerName});
                string echo="saloon-echo-"+Guid.NewGuid().ToString("N");
                await VivoxService.Instance.JoinEchoChannelAsync(echo,ChatCapability.AudioOnly);
                VivoxService.Instance.UnmuteInputDevice();Status="Echo test: speak now — listening for 8 seconds.";
                await Task.Delay(8000);VivoxService.Instance.MuteInputDevice();await VivoxService.Instance.LeaveChannelAsync(echo);Status="Echo test finished.";
            }
            catch(Exception e){Status=e.Message;}
            finally{Busy=false;}
        }

        public async Task Leave()
        {
            Enabled=false;
            if(!Connected)return;
            try{VivoxService.Instance.MuteInputDevice();await VivoxService.Instance.LeaveAllChannelsAsync();}
            catch(Exception e){Debug.LogWarning(e.Message);}
            Connected=false;channel="";transmitting=false;
        }
    }
}
