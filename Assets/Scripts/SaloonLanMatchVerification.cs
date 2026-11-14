#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    // Opt-in packaged-player test: one host plus three/four independent client processes.
    public sealed class SaloonLanMatchVerification : MonoBehaviour
    {
        public SaloonGame Game;
        int count,index,round,completedMask,initialHealth;
        bool host,started,connected,planSent,stanceSent,stood,moved,actionSent,actionSeen,drawSent,beerSent,reported,privacy;
        float began,roomAt,nextCommand,quitAt,drawAt,discussionAt,outageSeconds;Vector3 origin;
        string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int n=Array.IndexOf(args,key);return n>=0&&n+1<args.Length?args[n+1]:fallback;}
        void Start()
        {
            Application.logMessageReceived+=OnLog;
            host=Arg("-saloonTest","").EndsWith("host");count=int.Parse(Arg("-saloonPlayers","4"));index=int.Parse(Arg("-saloonIndex","0"));
            outageSeconds=float.Parse(Arg("-saloonOutageSeconds","0"),System.Globalization.CultureInfo.InvariantCulture);
            Game.AutomatedInput=true;Game.PlayerName="LAN-"+index;Game.Port=Arg("-saloonPort","18130");Game.Address=Arg("-saloonAddress","127.0.0.1");
            Game.tuning.roundLimit=3;Game.tuning.turnSeconds=20;Game.tuning.regroupSeconds=1;began=Time.unscaledTime;
            if(host){Game.SetBotsEnabled(false);Game.HostLan(false);}else Game.JoinLan();
        }
        void OnLog(string message,string trace,LogType type){if(type==LogType.Exception)Report(false,"runtime exception: "+message);}
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
            if(Time.unscaledTime-began>105+outageSeconds){Report(false,"timeout phase="+Game.View?.phase);return;}
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
                if(roomAt==0){roomAt=Time.unscaledTime;initialHealth=self.health;Debug.Log("SALOON_LAN_MATCH_PHASE index="+index+" round="+round+" Turns room="+self.room);}
                int expected=round==2&&index<4?(index<2?1:2):0;
                if(self.room!=expected){Report(false,"wrong room assignment");return;}
                if(round==1)
                {
                    if(self.seated&&!stanceSent&&Time.unscaledTime>=nextCommand){Game.Act("stance");stanceSent=true;nextCommand=Time.unscaledTime+.3f;return;}
                    if(self.seated&&stanceSent&&Time.unscaledTime>=nextCommand){stanceSent=false;return;}
                    if(!self.seated&&!stood){stood=true;origin=new Vector3(self.x,self.y,self.z);}
                    if(stood)
                    {
                        if(Vector3.ProjectOnPlane(new Vector3(self.x,self.y,self.z)-origin,Vector3.up).magnitude>.25f)moved=true;
                        Game.Connection.Send(new Command{kind="input",x=Time.unscaledTime-roomAt<1.4f?-.7f:0,z=Time.unscaledTime-roomAt<1.4f?-.7f:0,yaw=0,pitch=0,poseVersion=self.poseVersion});
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
                    var direction=new Vector3(target.x-self.x,target.y-self.y,target.z-self.z);
                    float yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
                    Game.Connection.Send(new Command{kind="input",yaw=yaw,pitch=pitch,poseVersion=self.poseVersion});
                    if(view.actionAvailable&&!self.drawn&&!drawSent){Game.Act("draw");drawSent=true;drawAt=Time.unscaledTime;}
                    if(view.actionAvailable&&self.drawn&&view.busyRemaining<=0&&Time.unscaledTime-drawAt>.85f&&!actionSent){Game.Act("fire");actionSent=true;}
                    if(actionSent&&self.shots==1&&view.ammo==5)actionSeen=true;
                }
                else if(round==3&&initialHealth<Game.tuning.health)
                {
                    if(view.actionAvailable&&!beerSent){Game.Act("interact");beerSent=true;nextCommand=Time.unscaledTime+.25f;}
                    if(view.pouring&&view.pourElapsed>=2.65f&&Time.unscaledTime>=nextCommand){Game.Act("stopPour");nextCommand=Time.unscaledTime+.25f;}
                    if(view.carriedBeer==1&&!actionSent&&Time.unscaledTime>=nextCommand){Game.Act("drink");actionSent=true;nextCommand=Time.unscaledTime+.4f;}
                    if(actionSent&&self.health==SaloonHealth.AfterBeer(initialHealth))actionSeen=true;
                    if(actionSent&&!actionSeen&&view.beerChoiceRemaining>0&&view.carriedBeer==1&&view.busyRemaining<=0&&Time.unscaledTime>=nextCommand)
                    {Game.Act("drink");nextCommand=Time.unscaledTime+.4f;}
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
                else if(round==2)actionSeen|=self.shots==1&&view.ammo==5;
                else if(round==3)actionSeen|=self.health==SaloonHealth.AfterBeer(initialHealth);
            }
            if(view.phase==Phase.Aftermath&&(completedMask&(1<<round))==0)
            {
                bool ok=actionSeen&&(round!=1||(stood&&moved))&&(round!=2||index>=4||self.health<SaloonHealth.Maximum);
                if(!ok){Report(false,"round "+round+" action/movement/health not acknowledged health="+self.health+" action="+actionSeen+" stood="+stood+" moved="+moved+" sent="+actionSent+" beer="+view.carriedBeer+" notice="+view.notice);return;}
                completedMask|=1<<round;Debug.Log("SALOON_LAN_MATCH_ROUND PASS index="+index+" round="+round+" health="+self.health);
            }
        }
    }
}
#endif
