#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyTutorial()
        {
            VerificationFinished=VerificationPassed=false;AutomatedInput=true;
            var originalTuning=tuning;bool originalBots=BotsEnabled;int originalSize=BotMatchSize;
            string seen=PlayerPrefs.GetString(TutorialSeenKey,"");bool hadSeen=PlayerPrefs.HasKey(TutorialSeenKey);
            int checks=0;
            void Check(bool ok,string message)
            {
                if(!ok){VerificationFinished=true;Debug.LogError("SALOON_TUTORIAL FAIL "+message);throw new InvalidOperationException(message);}
                checks++;Debug.Log("SALOON_TUTORIAL_CHECK "+message);
            }
            Participant Host()=>players[Connection.LocalId];
            void Lesson(TutorialTask task)
            {
                SelectTutorialLesson(Array.FindIndex(SaloonTutorialLessons.All,l=>l.Task==task));
                Check(TutorialActive&&Connection.Solo&&!Connection.Manager.IsListening&&PlayerCount==5&&MenuOpen,
                    task+" starts an isolated five-player scenario with instructions");
                TryTutorialLesson();
            }
            void Do(string kind,string name=null,float x=0)
            {var p=Host();p.lastAction=-100;OnCommand(p.id,new Command{kind=kind,name=name,x=x});Broadcast();TickTutorial();}
            IEnumerator Until(Func<bool> condition,string message,float seconds=8)
            {
                float end=Time.realtimeSinceStartup+seconds;
                while(!condition()&&Time.realtimeSinceStartup<end)yield return null;
                Check(condition(),message);
            }
            void Headshot(bool quick)
            {
                var p=Host();var target=players.Values.First(b=>b.bot&&b.role==Role.Outlaw);
                CaptureCombatFrames();Physics.SyncTransforms();p.drawn=true;p.nextShot=0;
                var ray=new Ray(Eye(p),(target.body.transform.TransformPoint(target.head.center)-Eye(p)).normalized);
                Fire(p,new ShotIntent{Ray=ray,Time=Time.time,Quick=quick,Round=rounds.Round,PoseVersion=p.poseVersion});
                Broadcast();TickTutorial();
            }
            string directory=Path.Combine(Path.GetTempPath(),"lastcall-tutorial");Directory.CreateDirectory(directory);
            IEnumerator Capture(string name)
            {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));yield return null;}
            try
            {
                Check(ShouldOfferTutorial("",TutorialBuild),"a new install offers the tutorial");
                Check(!ShouldOfferTutorial(TutorialBuild,TutorialBuild),"the same acknowledged build does not offer it again");
                Check(ShouldOfferTutorial("older-build",TutorialBuild),"a new build offers it again");
                Check(TutorialBuildIdentity("1","a")!=TutorialBuildIdentity("1","b"),"different binaries with the same version get separate onboarding");
                Check(SaloonTutorialLessons.All.Select(l=>l.Task).Distinct().Count()==Enum.GetValues(typeof(TutorialTask)).Length,
                    "all tutorial tasks have one chapter");
                Check(SaloonTutorialLessons.Actions.Length==8&&SaloonTutorialLessons.ActionRule=="1 ACTION = 1 ROUND",
                    "the prominent action rule includes all eight action categories");
                OpenTutorial();yield return Capture("welcome");SkipTutorialWelcome();
                Check(!TutorialWelcome&&!InRoom&&PlayerPrefs.GetString(TutorialSeenKey)==TutorialBuild,"Skip acknowledges this build without creating a match");
                OpenTutorial();BeginTutorialLibrary();
                Check(TutorialActive&&!TutorialWelcome&&tuning!=originalTuning,"replay starts with separate tuning");
                Lesson(TutorialTask.Move);Do("stance");
                var p=Host();p.move=Vector2.right;p.lastInput=Time.time;p.crouchHeld=true;
                yield return Until(()=>TutorialTaskDone,"standing, walking and crouching complete the movement exercise");
                Check(players.Values.Where(b=>b.bot).All(b=>b.shots==0),"tutorial opponents never attack");

                Lesson(TutorialTask.Plan);ChooseRoom(0,"");ToggleReady();
                yield return Until(()=>TutorialTaskDone,"real plan and Ready commands start room actions");
                Lesson(TutorialTask.Action);MenuOpen=true;yield return Capture("actions");TryTutorialLesson();Do("pass");
                Check(TutorialTaskDone,"passing spends the round action and completes the action lesson");
                Lesson(TutorialTask.RoleCard);ShowRole=true;TickTutorial();ShowRole=false;
                Check(TutorialTaskDone,"reading the actual role card completes the information lesson");

                foreach(var task in new[]{TutorialTask.Heal,TutorialTask.SmokeBeer})
                {
                    Lesson(task);MenuOpen=true;yield return Capture(task.ToString());TryTutorialLesson();
                    PlaceTutorialPlayer(Host(),new Vector3(SaloonBeerTaps.Position.x,.08f,2.1f),0);Do("interact");
                    Check(Host().busyAction=="drink"&&beers==7,"beer uses one shared drink and starts timed healing");
                    float finish=Host().busyUntil;HoldTutorialTimers(2);Check(Mathf.Abs(Host().busyUntil-finish-2)<.001f,"reading instructions preserves the remaining drink time");Host().busyUntil-=2;
                    yield return Until(()=>TutorialTaskDone,task+" finishes the actual healing action");
                    Check(Host().hp==80,"training beer produces the expected normal or boosted health");
                }
                Lesson(TutorialTask.QuickShot);Headshot(true);Check(TutorialTaskDone,"a real quick shot completes the quick-shot exercise");
                Lesson(TutorialTask.AccurateShot);Headshot(false);Check(TutorialTaskDone,"an accurate headshot completes the aiming exercise");
                Lesson(TutorialTask.Reload);Do("draw");yield return new WaitForSeconds(.3f);Do("reload");
                yield return Until(()=>TutorialTaskDone,"reload completes with six bullets");

                foreach(var task in new[]{TutorialTask.WatchRooms,TutorialTask.RoomPrivacy})
                {
                    Lesson(task);Do("stance");
                    PlaceTutorialPlayer(Host(),new Vector3(-5,.08f,task==TutorialTask.WatchRooms?-.8f:-2.2f),0);
                    Broadcast();TickTutorial();Check(TutorialTaskDone,task+" requires movement toward the correct side of the window");
                }
                Lesson(TutorialTask.Coin);ChooseCoin(0);var coin=duels[1];coin.revealAt=Time.time-1;TickServer();
                coin.combatAt=Time.time-1;TickTutorial();Do("pass");Check(TutorialTaskDone,"coin choice, result and legal pass complete the minigame");
                foreach(var task in new[]{TutorialTask.BlackjackPlayer,TutorialTask.BlackjackDealer})
                {
                    Lesson(task);var d=duels[2];int limit=0;
                    while(!d.revealed&&limit++<100)
                    {
                        d.readyAt=d.nextInputAt=Time.time-1;
                        var actor=d.blackjack.PlayerTurn?(d.dealer==d.a?players[d.b]:players[d.a]):players[d.dealer];
                        PlayBlackjack(actor,d.blackjack.PlayerTurn?"stand":"deal");
                        d.readyAt=Time.time-1;
                        if(d.blackjack.PlayerHighCard>=0)d.revealAt=Time.time-1;
                        ResolveBlackjack(d);yield return null;
                    }
                    Check(d.revealed,task+" resolves through real blackjack decisions");
                    d.combatAt=Time.time-1;TickTutorial();Do("pass");Check(TutorialTaskDone,task+" completes after the opening and response");
                }
                Lesson(TutorialTask.Smoke);Do("smoke");Check(TutorialTaskDone&&Host().cigaretteBuff,"an actual drag completes the outside lesson with one visit boost");
                int shots=Host().shots;Do("fire");Check(Host().shots==shots,"weapons remain unavailable outside");
                foreach(var task in new[]{TutorialTask.Sheriff,TutorialTask.Deputy})
                {
                    Lesson(task);PlaceTutorialPlayer(Host(),SafeApproach,0);Do("inspect");
                    yield return Until(()=>TutorialTaskDone&&Host().safeReport.Contains("money missing"),task+" receives a private drawer report after the check");
                }
                Lesson(TutorialTask.SafeWork);PlaceTutorialPlayer(Host(),SafeApproach,0);
                for(int step=1;step<=SaloonRules.DrawerWorkSteps;step++)
                {
                    int completed=step;Do("rob");yield return Until(()=>safeProgress>=completed,"drawer step "+step+" completes through a real action");
                    Check(Host().bag==(step==SaloonRules.DrawerWorkSteps),"training gives money only after the third step");
                }
                Check(TutorialTaskDone&&Host().bag,"three steps across separate training rounds collect the money");
                Lesson(TutorialTask.Escape);Do("escape");
                Check(Host().escapePlanned&&!rounds.HasSpent(Host().id),"escape is planned for the next action phase without spending the discussion");
                Do("ready",null,1);
                yield return Until(()=>Host().escapeRound==rounds.Round&&Host().escapeRound>0,"escape begins with a public response window",12);
                Check(TutorialTaskDone&&!rounds.HasSpent(Host().id)&&players.Values.Where(p=>p!=Host()).All(p=>!rounds.HasSpent(p.id)),"escape preserves actions for both the carrier and defenders");
                float escapeDeadline=rounds.Deadline;rounds.Tick(escapeDeadline,id=>players[id].Alive);phase=rounds.Phase;ResolveRoundEnding();
                Check(Host().bag&&phase==Phase.Finished&&outcome=="OUTLAWS WIN — MONEY ESCAPED","surviving the complete response window wins with the money");
                Lesson(TutorialTask.Renegade);ShowRole=true;TickTutorial();ShowRole=false;Headshot(false);
                Check(TutorialTaskDone&&Host().UnlimitedAmmo,"Renegade identifies roles and eliminates the Outlaw target");
                Lesson(TutorialTask.Spectate);Host().deathAt=Time.time-10;Broadcast();TickTutorial();CycleSpectator(1);TickTutorial();
                Check(TutorialTaskDone,"cycling the actual spectator view completes the elimination lesson");
                Lesson(TutorialTask.Finish);MenuOpen=true;yield return Capture("finish");
                EndTutorial();yield return Until(()=>!TutorialActive&&!InRoom&&!TutorialClosing,"finishing tears down the tutorial session");
                Check(ReferenceEquals(tuning,originalTuning)&&BotsEnabled==originalBots&&BotMatchSize==originalSize,"finishing restores every practice setting");
                OpenTutorial();BeginTutorialLibrary();EndTutorial();
                yield return Until(()=>!TutorialActive&&!InRoom&&!TutorialClosing,"an early tutorial exit also tears down the session");
                Check(ReferenceEquals(tuning,originalTuning)&&PlayerCount==0&&View==null,"early exit restores settings and clears players and snapshots");
                VerificationPassed=true;Debug.Log("SALOON_TUTORIAL PASS checks="+checks+" previews="+directory);
            }
            finally
            {
                if(hadSeen)PlayerPrefs.SetString(TutorialSeenKey,seen);else PlayerPrefs.DeleteKey(TutorialSeenKey);
                PlayerPrefs.Save();VerificationFinished=true;
            }
        }
    }
}
#endif
