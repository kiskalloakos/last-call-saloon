#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    // Opt-in packaged-player test: one host plus three to seven LAN or Relay clients.
    public sealed class SaloonLanMatchVerification : MonoBehaviour
    {
        public SaloonGame Game;
        int count,index,round,completedMask,initialHealth;
        int expectedAmmoAfterShot;
        bool host,online,codeWritten,started,connected,planSent,stanceSent,stood,moved,actionSent,actionSeen,drawSent,beerSent,reported,privacy;
        float began,roomAt,nextCommand,quitAt,drawAt,discussionAt,outageSeconds,walkAt;Vector3 origin;
        int beerRoute;float beerAisleX;
        string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int n=Array.IndexOf(args,key);return n>=0&&n+1<args.Length?args[n+1]:fallback;}
        void Start()
        {
            Application.logMessageReceived+=OnLog;
            host=Arg("-saloonTest","").EndsWith("host");count=int.Parse(Arg("-saloonPlayers","4"));index=int.Parse(Arg("-saloonIndex","0"));
            if(!SaloonRules.SupportedMatchSize(count)){Report(false,"match requires four to eight players");return;}
            online=Arg("-saloonOnline","0")=="1";
            outageSeconds=float.Parse(Arg("-saloonOutageSeconds","0"),System.Globalization.CultureInfo.InvariantCulture);
            Game.AutomatedInput=true;Game.PlayerName="LAN-"+index;Game.Port=Arg("-saloonPort","18130");Game.Address=Arg("-saloonAddress","127.0.0.1");
            Game.tuning.roundLimit=3;Game.tuning.turnSeconds=20;Game.tuning.regroupSeconds=1;began=Time.unscaledTime;
            Game.SetBotsEnabled(false);
            if(online){Game.JoinCode=Arg("-saloonCode","");Game.Cloud(host);}
            else if(host)Game.HostLan(false);else Game.JoinLan();
        }
        void OnLog(string message,string trace,LogType type)
        {
            // The services SDK logs handled 429 responses as exceptions before the
            // connection's bounded retry runs. All other runtime exceptions fail.
            if(type==LogType.Exception&&!(online&&Game.Connection.Busy&&message.Contains("request_rate_limited")))
                Report(false,"runtime exception: "+message);
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
        void Report(bool ok,string reason)
        {
            if(reported)return;reported=true;quitAt=Time.unscaledTime+(host?4:1);
            Debug.Log("SALOON_LAN_MATCH "+(ok?"PASS":"FAIL")+" index="+index+" players="+count+" role="+Game.View?.ownRole+" "+reason);
            if(!ok){Application.Quit(10);enabled=false;}
        }
        void Update()
        {
            if(reported){if(Time.unscaledTime>=quitAt)Application.Quit(0);return;}
            if(Time.unscaledTime-began>105+outageSeconds+(online?75:0)){Report(false,"timeout phase="+Game.View?.phase);return;}
            if(online&&host&&!codeWritten&&Game.InRoom&&!string.IsNullOrEmpty(Game.Connection.RoomCode))
            {File.WriteAllText(Arg("-saloonCodeFile",""),Game.Connection.RoomCode);codeWritten=true;}
            if(Game.InRoom)connected=true;
            if(connected&&!Game.InRoom){Report(false,"connection closed before match results");return;}
            Game.MenuOpen=false;
            if(host&&!started&&Game.View?.players?.Length==count&&Game.View.players.All(p=>p.name.StartsWith("LAN-")))
            {Game.StartMatch();started=true;}
            var view=Game.View;var self=Game.Self;if(view==null||self==null)return;
            if(view.phase==Phase.Lobby)return;
            if(!Game.InRoom||(!host&&Game.Connection.SnapshotAge>8+outageSeconds)){Report(false,"connection/state stream lost");return;}
            if(view.players.Length!=count||view.players.Any(p=>p.bot)||!self.alive){Report(false,"wrong roster or unexpected elimination");return;}
            if(view.phase==Phase.Discussion&&view.round==4)
            {
                bool ok=privacy&&completedMask==14&&string.IsNullOrEmpty(view.outcome)&&SaloonAutomation.RoleLabelsValid(view);
                Report(ok,"three rounds complete and round four started beyond the legacy cap; standing/movement, paired coin/blackjack/headshots, beer healing; outcome="+view.outcome);return;
            }
            if(view.phase==Phase.Discussion)
            {
                if(round!=view.round)
                {
                    round=view.round;planSent=stanceSent=stood=moved=actionSent=actionSeen=drawSent=beerSent=false;roomAt=0;nextCommand=0;
                    discussionAt=Time.unscaledTime;
                    Debug.Log("SALOON_LAN_MATCH_PHASE index="+index+" round="+round+" Discussion");
                }
                if(!privacy)
                {
                    privacy=view.ownRole!="Unknown"&&view.players.Count(p=>p.sheriff)==1&&SaloonAutomation.RoleLabelsValid(view);
                    if(!privacy){Report(false,"private roles/Sheriff roster invalid");return;}
                }
                int room=round==2&&index<4?(index<2?1:2):0;
                string companion=room==0?"":view.players.FirstOrDefault(p=>p.name=="LAN-"+(index^1))?.id;
                if(room>0&&companion==null)return;
                if(!planSent){Game.ChooseRoom(room,companion);planSent=true;return;}
                // Leave the discussion open while the external relay interrupts one peer.
                if(index==1&&round==1&&outageSeconds>0&&Time.unscaledTime-discussionAt<outageSeconds+3)return;
                if(self.chosenRoom==room&&self.companion==(companion??"")&&!self.ready&&!Game.PlanReady)Game.ToggleReady();
                return;
            }
            if(view.phase==Phase.Turns)
            {
                if(roomAt==0)
                {
                    roomAt=Time.unscaledTime;initialHealth=self.health;beerRoute=0;beerAisleX=Mathf.Floor(self.x)+.5f;
                    expectedAmmoAfterShot=view.ammo-(view.UnlimitedAmmo?0:1);
                    Debug.Log("SALOON_LAN_MATCH_PHASE index="+index+" round="+round+" Turns room="+self.room);
                }
                int expected=round==2&&index<4?(index<2?1:2):0;
                if(self.room!=expected){Report(false,"wrong room assignment");return;}
                if(round==1)
                {
                    if(self.seated&&!stanceSent&&Time.unscaledTime>=nextCommand){Game.Act("stance");stanceSent=true;nextCommand=Time.unscaledTime+.3f;return;}
                    if(self.seated&&stanceSent&&Time.unscaledTime>=nextCommand){stanceSent=false;return;}
                    if(!self.seated&&!stood){stood=true;walkAt=Time.unscaledTime;origin=new Vector3(self.x,self.y,self.z);}
                    if(stood)
                    {
                        if(Vector3.ProjectOnPlane(new Vector3(self.x,self.y,self.z)-origin,Vector3.up).magnitude>.65f)moved=true;
                        // Give each walker time after standing, and try another direction
                        // when furniture or another player blocks the first path.
                        float angle=(index+Mathf.Floor((Time.unscaledTime-walkAt)/.6f))*45;
                        var direction=moved?Vector3.zero:Quaternion.Euler(0,angle,0)*Vector3.back*.7f;
                        Game.Connection.Send(new Command{kind="input",x=direction.x,z=direction.z,yaw=0,pitch=0,poseVersion=self.poseVersion});
                    }
                    if(view.actionAvailable&&moved&&Time.unscaledTime-roomAt>2&&!actionSent){Game.Act("pass");actionSent=true;}
                    if(actionSent&&view.notice.StartsWith("Passed."))actionSeen=true;
                }
                else if(round==2&&index<4)
                {
                    if(view.duel?.stage=="choosing")Game.ChooseCoin(index%2);
                    // Play through the real, slower blackjack decisions instead of waiting for human timeouts.
                    if(Time.unscaledTime>=nextCommand)
                    {
                        if(view.duel?.canHit==true){Game.ChooseBlackjack("stand");nextCommand=Time.unscaledTime+.3f;}
                        else if(view.duel?.canDeal==true){Game.ChooseBlackjack("deal");nextCommand=Time.unscaledTime+.3f;}
                    }
                    var target=view.players.FirstOrDefault(p=>p.name=="LAN-"+(index^1));if(target==null)return;
                    var direction=new Vector3(target.headX-self.x,target.headY-self.y-SaloonMovement.EyeHeight(self.seated,self.crouched),target.headZ-self.z);
                    float yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
                    Game.Connection.Send(new Command{kind="input",yaw=yaw,pitch=pitch,poseVersion=self.poseVersion});
                    if(view.actionAvailable&&!self.drawn&&!drawSent){Game.Act("draw");drawSent=true;drawAt=Time.unscaledTime;}
                    if(view.actionAvailable&&self.drawn&&view.busyRemaining<=0&&Time.unscaledTime-drawAt>.85f&&!actionSent)
                    {
                        actionSent=Game.ShootForVerification(target.id);
                    }
                    if(actionSent&&self.shots==1&&view.ammo==expectedAmmoAfterShot)actionSeen=true;
                }
                else if(round==3&&initialHealth<Game.tuning.health)
                {
                    if(view.actionAvailable&&!beerSent&&Time.unscaledTime>=nextCommand)
                    {
                        if(self.seated){Game.Act("stance");nextCommand=Time.unscaledTime+.2f;}
                        else if(!SaloonBeerTaps.WithinReach(new Vector3(self.x,self.y,self.z),Game.tuning.interactionDistance))
                        {
                            // Walk through a stool gap and along the public aisle; a direct
                            // diagonal to the tray can run into another player or stool.
                            var current=new Vector3(self.x,self.y,self.z);
                            var waypoint=beerRoute==0?new Vector3(beerAisleX,self.y,1.35f):new Vector3(SaloonBeerTaps.Position.x,self.y,1.35f);
                            if(Vector3.ProjectOnPlane(waypoint-current,Vector3.up).magnitude<.15f)beerRoute=1;
                            var direction=Vector3.ProjectOnPlane(waypoint-current,Vector3.up).normalized;
                            var local=Quaternion.Euler(0,-self.yaw,0)*direction;
                            Game.Connection.Send(new Command{kind="input",x=local.x,z=local.z,yaw=self.yaw,pitch=self.pitch,poseVersion=self.poseVersion});
                        }
                        else {Game.Act("interact");beerSent=actionSent=true;}
                    }
                    if(actionSent&&self.health==SaloonHealth.AfterBeer(initialHealth))actionSeen=true;
                }
                else
                {
                    if(view.actionAvailable&&!actionSent){Game.Act("pass");actionSent=true;}
                    if(actionSent&&view.notice.StartsWith("Passed."))actionSeen=true;
                }
            }
            // A final action may arrive in the same snapshot that advances to aftermath.
            if(actionSent)
            {
                if(round==1||(round==2&&index>=4)||(round==3&&initialHealth>=SaloonHealth.Maximum))actionSeen|=view.notice.StartsWith("Passed.");
                else if(round==2)actionSeen|=self.shots==1&&view.ammo==expectedAmmoAfterShot;
                else if(round==3)actionSeen|=self.health==SaloonHealth.AfterBeer(initialHealth);
            }
            if(view.phase==Phase.Aftermath&&(completedMask&(1<<round))==0)
            {
                bool ok=actionSeen&&(round!=1||(stood&&moved))&&(round!=2||index>=4||self.health<SaloonHealth.Maximum);
                if(!ok){Report(false,"round "+round+" action/movement/health not acknowledged health="+self.health+" action="+actionSeen+" stood="+stood+" moved="+moved+" sent="+actionSent+" beer="+view.carriedBeer+" position="+new Vector3(self.x,self.y,self.z)+" seated="+self.seated+" route="+beerRoute+" notice="+view.notice);return;}
                completedMask|=1<<round;Debug.Log("SALOON_LAN_MATCH_ROUND PASS index="+index+" round="+round+" health="+self.health);
            }
        }
    }
}
#endif
