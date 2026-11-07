using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    // Explicit command-line harness for multi-process integration checks, never active in normal play.
    public class SaloonAutomation : MonoBehaviour
    {
        public SaloonGame Game;
        string mode;
        float started,nextCommand,nextMove,liveStarted=-1;
        float quitAfter;
        int resultCode;
        bool dealt,reported,captured,privacyChecked;
        string preview;
        Vector3 initial;
        bool haveInitial,moved;
        int trackedPose=-1;
        bool roomRequested,roomAcknowledged,voiceStarted;
        bool RoomTest=>mode!=null&&mode.StartsWith("rooms-");
        void Start()
        {
            var args=Environment.GetCommandLineArgs();
            mode=Arg(args,"-saloonTest");
            if(string.IsNullOrEmpty(mode))return;
            preview=Arg(args,"-saloonPreview");
            Game.Port=Arg(args,"-saloonPort")??"17877";
            Game.PlayerName=Arg(args,"-saloonName")??("Test-"+mode);
            Game.tuning.discussionSeconds=1;Game.tuning.turnSeconds=mode.StartsWith("preview")?8:1;Game.tuning.roundLimit=mode=="functional"?6:1;
            if(RoomTest){Game.tuning.discussionSeconds=30;Game.tuning.turnSeconds=5;}
            started=Time.unscaledTime;
            if(mode=="preview")return;
            Game.AutomatedInput=!mode.StartsWith("preview");
            if(mode=="host"||mode=="practice-host"||(RoomTest&&mode.EndsWith("host")))Game.HostLan(false);
            else if(mode=="solo"||mode=="preview-solo"||mode=="functional")Game.HostLan(true);
            else Game.JoinLan();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(mode=="functional")StartCoroutine(Game.VerifyGameplay());
#endif
        }
        static string Arg(string[] args,string key){int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
        void Update()
        {
            if(string.IsNullOrEmpty(mode))return;
            if(mode=="functional")return;
            if(reported){if(Time.unscaledTime>=quitAfter)Application.Quit(resultCode);return;}
            if(mode.StartsWith("preview"))
            {
                if(mode=="preview-solo"&&Time.unscaledTime-started>2&&Game.Self!=null&&!Game.Self.drawn)Game.Act("draw");
                if(!captured&&Time.unscaledTime-started>4){ScreenCapture.CaptureScreenshot(preview);captured=true;}
                if(Time.unscaledTime-started>7)Application.Quit(0);
                return;
            }
            if((mode=="practice-host"||RoomTest)&&Game.Hosting&&Game.PlayerCount==2&&!dealt&&Game.CurrentPhase==Phase.Lobby)
            {Game.StartPractice();dealt=true;}
            if(Game.Hosting&&Game.PlayerCount==5&&!dealt&&Game.CurrentPhase==Phase.Lobby){Game.StartMatch();dealt=true;}
            var self=Game.Self;
            if(self!=null)
            {
                if(RoomTest)
                {
                    if(mode.Contains("voice")&&!voiceStarted){voiceStarted=true;_ = Game.Voice.ConnectForVerification();}
                    int desired=Game.Hosting?2:1;
                    if(Game.View.phase==Phase.Choosing&&!roomRequested)
                    {
                        var peer=Game.View.players.First(p=>p.id!=self.id&&!p.name.StartsWith("Practice"));
                        Game.ChooseRoom(desired,peer.id);roomRequested=true;
                    }
                    if(roomRequested&&self.chosenRoom==desired)roomAcknowledged=true;
                    if(Game.View.phase==Phase.Turns)
                    {
                        bool ok=roomAcknowledged&&Game.RoomChooserRenderFrames>0&&self.room==desired&&(!mode.Contains("voice")||Game.Voice.Connected);
                        Debug.Log("SALOON_ROOMS "+(ok?"PASS":"FAIL")+" mode="+mode+" room="+self.room+" acknowledged="+roomAcknowledged+" chooserFrames="+Game.RoomChooserRenderFrames+" voice="+Game.Voice.Connected);
                        reported=true;resultCode=ok?0:8;quitAfter=Time.unscaledTime+(Game.Hosting?1:.5f);
                    }
                    if(Time.unscaledTime-started>90){Debug.LogError("SALOON_ROOMS FAIL timeout "+mode);Application.Quit(9);}
                    return;
                }
                if(!privacyChecked&&Game.View.phase==Phase.Discussion)
                {
                    privacyChecked=true;
                    bool secret=Game.View.players.All(p=>string.IsNullOrEmpty(p.revealedRole))&&Game.View.players.Count(p=>p.sheriff)==1&&
                        string.IsNullOrEmpty(Game.View.ally);
                    Debug.Log("SALOON_PRIVACY "+(secret?"PASS":"FAIL")+" mode="+mode);
                    if(!secret)Application.Quit(5);
                }
                if(!haveInitial||trackedPose!=self.poseVersion){initial=new Vector3(self.x,self.y,self.z);haveInitial=true;trackedPose=self.poseVersion;}
                if(Vector3.Distance(initial,new Vector3(self.x,self.y,self.z))>.15f)moved=true;
                if(Time.unscaledTime>=nextCommand&&Game.View.phase==Phase.Turns)
                {
                    // Through the same wire protocol as human controls.
                    if(!self.drawn)Game.Act("draw");else Game.Act("fire");
                    nextCommand=Time.unscaledTime+.7f;
                }
                if(Game.View.phase==Phase.Turns&&Time.unscaledTime>=nextMove)
                {
                    if(liveStarted<0)liveStarted=Time.unscaledTime;
                    Game.Connection.Send(new Command{kind="input",x=0,z=Time.unscaledTime-liveStarted<1?-1:0,yaw=0,pitch=0});
                    nextMove=Time.unscaledTime+.04f;
                }
                if(Game.View.phase==Phase.Finished&&!reported)
                {
                    reported=true;
                    bool valid=!moved&&privacyChecked&&Game.View.players.Length==5&&Game.View.ownRole!="Unknown"&&Game.View.players.All(p=>!string.IsNullOrEmpty(p.revealedRole));
                    Debug.Log("SALOON_SMOKE "+(valid?"PASS":"FAIL")+" mode="+mode+" players="+Game.View.players.Length+" role="+Game.View.ownRole+" outcome="+Game.View.outcome+" movement="+moved);
                    // Let the host's final reliable snapshot leave the transport before shutdown.
                    resultCode=valid?0:3;quitAfter=Time.unscaledTime+(Game.Hosting?1f:.25f);
                }
            }
            if(Time.unscaledTime-started>55){Debug.LogError("SALOON_SMOKE FAIL: timed out waiting for five players/match completion");Application.Quit(4);}
        }
    }
}
