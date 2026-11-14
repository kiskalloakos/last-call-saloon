using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using UnityEngine;

namespace Saloon
{
    // Explicit command-line integration harness; it never runs during normal play.
    public class SaloonCloudVerification : MonoBehaviour
    {
        public SaloonGame Game;
        string mode;

        async void Start()
        {
            var args=Environment.GetCommandLineArgs();mode=Arg(args,"-saloonCloudTest");
            if(string.IsNullOrEmpty(mode))return;
            Game.AutomatedInput=true;Game.PlayerName=mode;
            Game.Port=Arg(args,"-saloonPort")??"17888";
            try
            {
                if(mode=="relay-host"||mode=="relay-client")
                {
                    bool host=mode=="relay-host";
                    Game.JoinCode=Arg(args,"-saloonCode")??"";Game.Cloud(host);
                    await Until(()=>Game.InRoom&&!string.IsNullOrEmpty(Game.Connection.RoomCode),45,"Relay connection");
                    if(host)File.WriteAllText(Arg(args,"-saloonCodeFile"),Game.Connection.RoomCode);
                    await Until(()=>Game.View?.players?.Length==2,45,"Relay player replication");
                    Debug.Log("SALOON_RELAY PASS "+mode+" players=2");
                    await Task.Delay(host?1800:600);
                }
                else
                {
                    bool host=mode=="voice-host";
                    if(host)Game.HostLan(false);else Game.JoinLan();
                    await Until(()=>Game.InRoom&&Game.Self!=null&&Game.View.players.Length==2,40,"LAN voice room");
                    await Game.Voice.ConnectForVerification();
                    if(!Game.Voice.Connected)throw new Exception(Game.Voice.Status);
                    VivoxService.Instance.MuteInputDevice();
                    VivoxService.Instance.UnmuteOutputDevice();VivoxService.Instance.SetOutputDeviceVolume(-20);
                    await Until(()=>VivoxService.Instance.ActiveChannels.TryGetValue(Game.Voice.Channel,out var participants)&&participants.Count>=2,35,"Two live Vivox participants");
                    Debug.Log("SALOON_VOICE_JOIN PASS "+mode);
                    if(host)
                    {
                        string wave=Arg(args,"-saloonWave");
                        VivoxService.Instance.StartAudioInjection(wave);
                        await Until(()=>VivoxService.Instance.IsInjectingAudio,5,"Audio injection start");
                        Debug.Log("SALOON_VOICE_INJECTION started");
                        await Task.Delay(12000);
                        if(VivoxService.Instance.IsInjectingAudio)VivoxService.Instance.StopAudioInjection();
                        string stopped=Arg(args,"-saloonStopFile");
                        if(!string.IsNullOrEmpty(stopped))File.WriteAllText(stopped,"Audio injection stopped");
                        // Keep the participant connected while the receiver checks silence,
                        // so reception can be checked independently of channel departure.
                        await Task.Delay(1800);
                        Debug.Log("SALOON_VOICE_AUDIO PASS injected test speech");
                    }
                    else
                    {
                        double peak=0;
                        await Until(()=>
                        {
                            if(VivoxService.Instance.ActiveChannels.TryGetValue(Game.Voice.Channel,out var participants))
                                peak=Math.Max(peak,participants.Where(p=>!p.IsSelf).Select(p=>p.AudioEnergy).DefaultIfEmpty(0).Max());
                            return peak>.001;
                        },20,"Decoded remote voice audio");
                        Debug.Log("SALOON_VOICE_AUDIO PASS received energy="+peak.ToString("0.0000"));
                        var speaker=VivoxService.Instance.ActiveChannels[Game.Voice.Channel].First(p=>!p.IsSelf);
                        string stopped=Arg(args,"-saloonStopFile");
                        if(!string.IsNullOrEmpty(stopped))await Until(()=>File.Exists(stopped),20,"Sender stops injection");
                        await Until(()=>speaker.AudioEnergy<=.00001,5,"Injected speech stops while participant stays connected");
                        Debug.Log("SALOON_VOICE_STOP PASS silent connected participant");
                    }
                }
                await Game.Voice.Leave();await Game.Connection.Disconnect();
                Application.Quit(0);
            }
            catch(Exception e)
            {
                Debug.LogError("SALOON_CLOUD FAIL "+mode+": "+e.Message);
                Application.Quit(8);
            }
        }

        static async Task Until(Func<bool> condition,int seconds,string step)
        {
            var deadline=DateTime.UtcNow.AddSeconds(seconds);
            while(!condition())
            {
                if(DateTime.UtcNow>deadline)throw new TimeoutException(step);
                await Task.Delay(100);
            }
        }
        static string Arg(string[] args,string key){int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
    }
}
