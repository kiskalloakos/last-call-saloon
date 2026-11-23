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
        public IEnumerator VerifyWalkthrough()
        {
            VerificationFinished=VerificationPassed=false;AutomatedInput=true;
            var original=tuning;bool oldBots=BotsEnabled;int oldSize=BotMatchSize;
            string oldName=PlayerName;PlayerName="Learner";
            bool hadSeen=PlayerPrefs.HasKey(TutorialSeenKey);string seen=PlayerPrefs.GetString(TutorialSeenKey,"");
            var originalAmbient=RenderSettings.ambientLight;var originalAmbientMode=RenderSettings.ambientMode;
            var originalBackground=ViewCamera.backgroundColor;
            int checks=0;
            void Check(bool ok,string message)
            {if(!ok)throw new InvalidOperationException("SALOON_WALKTHROUGH FAIL "+message);checks++;Debug.Log("SALOON_WALKTHROUGH_CHECK "+message);}
            void Do(string kind,float x=0){var p=WalkthroughHost;p.lastAction=-100;OnCommand(p.id,new Command{kind=kind,x=x});}
            void Input(Vector3 direction,bool crouch=false,float? lookPitch=null)
            {
                var p=WalkthroughHost;float facing=direction.sqrMagnitude>.001f?Quaternion.LookRotation(direction).eulerAngles.y:p.yaw;
                yaw=facing;pitch=lookPitch??0;
                OnCommand(p.id,new Command{kind="input",x=0,z=direction.sqrMagnitude>.001f?1:0,yaw=facing,pitch=lookPitch??0,crouch=crouch,poseVersion=p.poseVersion});
            }
            void Walk()
            {
                var p=WalkthroughHost;
                if(p.seat>=0){Do("stance");return;}
                if(!TutorialGoal.HasValue){Input(Vector3.zero);return;}
                var direction=Vector3.ProjectOnPlane(TutorialGoal.Value-p.body.transform.position,Vector3.up);
                Input(direction.magnitude>.45f?direction.normalized:Vector3.zero);
            }
            void ShootTraining(bool accurate)
            {
                var p=WalkthroughHost;if(p.busyUntil>Time.time)return;
                if(!p.drawn){Do("draw");return;}
                if(!rounds.CanAct(p.id,Time.time))return;
                CaptureCombatFrames();var target=WalkthroughPeer;
                var direction=target.body.transform.TransformPoint(target.head.center)-Eye(p);
                p.yaw=yaw=Quaternion.LookRotation(direction).eulerAngles.y;
                p.pitch=pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
                p.move=Vector2.zero;
                if(accurate&&!p.chargingShot)
                {OnCommand(p.id,new Command{kind="chargeShot",shotRound=rounds.Round,poseVersion=p.poseVersion,shotSequence=p.shotChargeSequence+1});return;}
                if(accurate&&Time.time-p.shotChargeAt<AccurateShotSeconds)return;
                OnCommand(p.id,new Command{kind="fire",accurateShot=accurate,shotSequence=p.shotChargeSequence,shotRound=rounds.Round,poseVersion=p.poseVersion});
            }
            string directory=Path.Combine(Path.GetTempPath(),"lastcall-walkthrough");Directory.CreateDirectory(directory);
            try
            {
                OpenTutorial();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"welcome.png"));yield return null;
                BeginTutorial();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"overview.png"));yield return null;
                Check(TutorialWalkthrough&&TutorialActive&&Connection.Solo&&!MenuOpen,"the default tutorial starts playing without a chapter screen");
                Check(trainingMap.Root.activeSelf&&!GameObject.Find("Saloon World")&&!GameObject.Find("Saloon entrance sunset"),"the new training map replaces the normal saloon shell and front sunset");
                Check(trainingMap.Root.GetComponentsInChildren<SaloonRoomWindow>().Length==2,"the training map has real one-way booth windows");
                Check(trainingMap.Root.GetComponentsInChildren<Light>().Count(l=>l.type==LightType.Spot)==1&&trainingMap.ActionLight.type==LightType.Spot&&
                    trainingMap.Root.GetComponentsInChildren<Light>().Where(l=>l!=trainingMap.ActionLight).All(l=>l.cullingMask==1<<SaloonSmokingArea.OutdoorLayer),
                    "one spotlight guides indoor actions and the sunset light affects only outdoor scenery");
                var vista=trainingMap.SmokingDeck.Find("Wild West sunset overlook");
                Check(vista!=null&&vista.GetComponentsInChildren<Renderer>().Length>50&&
                    vista.GetComponentsInChildren<Renderer>().All(r=>r.gameObject.layer==SaloonSmokingArea.OutdoorLayer)&&
                    vista.GetComponentsInChildren<Collider>().All(c=>!c.enabled),"the tutorial deck reuses the game's sunset vista without adding collision obstacles");
                Check(trainingMap.Root.GetComponentsInChildren<TextMesh>().Length==0,"inactive stations have no floating signs");
                Check(!Connection.Manager.IsListening&&!Voice.Connected,"the walkthrough starts no network listener or voice connection");
                var mapRoot=trainingMap.Root;
                var start=WalkthroughHost.body.transform.position;var step=WalkthroughStep;
                MenuOpen=true;TutorialHelpOpen=true;TickWalkthrough();
                Check(WalkthroughStep==step&&WalkthroughHost.body.transform.position==start,"opening help preserves the live route and position");
                TutorialHelpOpen=false;MenuOpen=false;
                float started=Time.realtimeSinceStartup,nextCapture=0,stuckAt=Time.realtimeSinceStartup;
                var lastPosition=WalkthroughHost.body.transform.position;
                WalkthroughBeat? previous=null;
                bool capturedBeer=false,capturedCards=false,testedWrongPlans=false,testedSpectator=false,capturedSunset=false,checkedReturn=false;
                while(!WalkthroughFinished&&Time.realtimeSinceStartup-started<260)
                {
                    var p=WalkthroughHost;var beat=WalkthroughStep;
                    if(previous!=beat)
                    {
                        Debug.Log("SALOON_WALKTHROUGH_BEAT "+beat+" seconds="+(Time.realtimeSinceStartup-started).ToString("F1"));
                        if(previous.HasValue)Check(!MenuOpen,"the route continues automatically after "+previous.Value);
                        previous=beat;nextCapture=Time.time+.3f;stuckAt=Time.realtimeSinceStartup;
                        if(beat==WalkthroughBeat.Pass||beat==WalkthroughBeat.Quick)
                        {
                            Input(Vector3.zero);yield return new WaitForEndOfFrame();
                            ScreenCapture.CaptureScreenshot(Path.Combine(directory,beat==WalkthroughBeat.Pass?"action-rule.png":"turn-to-target.png"));
                            yield return null;
                        }
                    }
                    if(nextCapture>0&&Time.time>=nextCapture)
                    {ScreenCapture.CaptureScreenshot(Path.Combine(directory,beat+".png"));nextCapture=0;}
                    CheckNoMenu();
                    if(trainingMap.ActionLight.enabled==(p.room==3)||!trainingMap.ActionLight.gameObject.activeInHierarchy||trainingMap.ActionLight.transform.position.y>3.3f)
                        throw new InvalidOperationException("SALOON_WALKTHROUGH FAIL missing or obstructed action light in "+beat);
                    if(walkthroughPath.Count>0&&Vector3.Distance(trainingMap.FocusPoint,walkthroughPath.Peek())>.01f)
                        throw new InvalidOperationException("SALOON_WALKTHROUGH FAIL the light skipped a reachable waypoint in "+beat);
                    if(beat==WalkthroughBeat.Heal&&walkthroughPath.Count==0)
                    {
                        if(Vector3.Distance(trainingMap.FocusPoint,SaloonBeerTaps.Position)>.01f)
                            throw new InvalidOperationException("SALOON_WALKTHROUGH FAIL beer light points away from tray");
                        if(!capturedBeer&&NearBeerTray(p))
                        {ScreenCapture.CaptureScreenshot(Path.Combine(directory,"beer-action.png"));capturedBeer=true;}
                    }
                    if(beat==WalkthroughBeat.Blackjack&&walkthroughTable&&View.duel?.canHit==true&&!capturedCards)
                    {ScreenCapture.CaptureScreenshot(Path.Combine(directory,"cards-action.png"));capturedCards=true;}
                    switch(beat)
                    {
                        case WalkthroughBeat.Walk:Walk();break;
                        case WalkthroughBeat.Crouch:Input(Vector3.zero,true);break;
                        case WalkthroughBeat.Quick:ShootTraining(false);break;
                        case WalkthroughBeat.Accurate:ShootTraining(true);break;
                        case WalkthroughBeat.Reload:Input(Vector3.zero);if(p.busyUntil==0){if(!p.drawn)Do("draw");else Do("reload");}break;
                        case WalkthroughBeat.Pass:Input(Vector3.zero);Do("pass");break;
                        case WalkthroughBeat.Heal:
                        case WalkthroughBeat.Boost:
                            if(NearBeerTray(p)&&p.room==0){Input(Vector3.zero);if(p.busyUntil==0)Do("interact");}else Walk();break;
                        case WalkthroughBeat.Plan:
                            if(!testedWrongPlans)
                            {
                                foreach(int otherRoom in new[]{1,2,3})
                                {
                                    ChooseRoom(otherRoom,"");ToggleReady();Broadcast();yield return null;
                                    Check(phase==Phase.Discussion&&!PlanReady&&!PlanPending&&RoomPlannerOpen&&p.notice.Contains("Choose Bar"),
                                        "locking "+SaloonStations.Name(otherRoom)+" stays in planning with a correctable choice");
                                }
                                LeaveForRoom(3);Broadcast();yield return null;
                                Check(phase==Phase.Discussion&&!rounds.IsReady(p.id)&&!PlanPending,"Go alone cannot bypass the training plan guard");
                                OnCommand(p.id,new Command{kind="ready",x=1});Broadcast();yield return null;
                                Check(phase==Phase.Discussion&&!rounds.IsReady(p.id),"a direct ready command cannot lock the wrong training room");
                                ScreenCapture.CaptureScreenshot(Path.Combine(directory,"plan-correction.png"));
                                // Reproduce a plan already locked by the old build, including its room transfer.
                                var origin=tutorialStartPosition;p.chosenRoom=3;p.planSelected=true;rounds.SetReady(p.id,true);
                                rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);TickWalkthrough();
                                Check(WalkthroughStep==WalkthroughBeat.Plan&&phase==Phase.Discussion&&p.room==0&&p.seat<0&&
                                    FlatDistance(p.body.transform.position,origin)<.01f&&trainingMap.Root==mapRoot&&RoomPlannerOpen,
                                    "an already-invalid plan safely returns to the same training map and planning checkpoint");
                                testedWrongPlans=true;
                            }
                            ChooseRoom(0,"");ToggleReady();break;
                        case WalkthroughBeat.Coin:
                        case WalkthroughBeat.Blackjack:
                            if(!walkthroughTable){Walk();break;}
                            Input(Vector3.zero);
                            int room=beat==WalkthroughBeat.Coin?1:2;var d=duels[room];
                            if(room==1&&!d.flipping)ChooseCoin(0);
                            if(room==2&&View.duel?.canHit==true)ChooseBlackjack("stand");
                            if(d.revealed&&Time.time>=d.combatAt&&DuelAllows(p))Do("pass");break;
                        case WalkthroughBeat.Smoke:
                            if(p.room==3)
                            {
                                Input(Vector3.zero);
                                if(!capturedSunset)
                                {
                                    yield return new WaitForSeconds(.2f);
                                    Check((ViewCamera.cullingMask&(1<<SaloonSmokingArea.OutdoorLayer))!=0&&!trainingMap.ActionLight.enabled&&
                                        Vector3.Dot(ViewCamera.transform.forward,SaloonSunset.Direction)>.95f,
                                        "walking onto the deck reveals the sunset and faces the arrival view toward the sun");
                                    Check(!Physics.Raycast(Eye(p),SaloonSunset.Direction,15,~((1<<8)|(1<<SaloonRagdoll.Layer)),QueryTriggerInteraction.Ignore),
                                        "the low sun has a clear view from the deck entrance");
                                    ScreenCapture.CaptureScreenshot(Path.Combine(directory,"smoking-sunset.png"));
                                    yield return new WaitForSeconds(.2f);capturedSunset=true;
                                }
                                Do("smoke");
                            }
                            else Walk();break;
                        case WalkthroughBeat.Sheriff:
                        case WalkthroughBeat.Deputy:
                        case WalkthroughBeat.Outlaw:
                            if(AtSafe(p)){Input(Vector3.zero);if(p.busyUntil==0)Do(beat==WalkthroughBeat.Outlaw?"rob":"inspect");}else Walk();break;
                        case WalkthroughBeat.Escape:Input(Vector3.zero);if(phase==Phase.Discussion){Do("escape");Do("ready",1);}break;
                        case WalkthroughBeat.Renegade:ShowRole=true;TickWalkthrough();ShowRole=false;ShootTraining(true);break;
                        case WalkthroughBeat.Spectate:
                            if(!IsSpectating&&Time.realtimeSinceStartup-stuckAt>SaloonRagdoll.ObserverDelay+2)
                                throw new InvalidOperationException("SALOON_WALKTHROUGH FAIL death camera did not enter spectator view");
                            if(IsSpectating&&!testedSpectator)
                            {
                                yield return new WaitForSeconds(.2f);
                                yield return new WaitForEndOfFrame();
                                Check(!Self.alive&&Self.observer&&CameraPlayer.alive,"practice elimination automatically enters a living player's view");
                                var watchedPlayer=CameraPlayer;
                                var watchedEye=new Vector3(watchedPlayer.x,watchedPlayer.y+SaloonMovement.EyeHeight(watchedPlayer.seated,watchedPlayer.crouched),watchedPlayer.z);
                                Check(Vector3.Distance(ViewCamera.transform.position,watchedEye)<.25f,"the rendered camera leaves the corpse and reaches the watched player's eyes");
                                Check(GetComponent<SaloonHUD>().WalkthroughSpectatorControlsVisible&&Cursor.lockState!=CursorLockMode.Locked,
                                    "spectator Previous and Next controls are visible with an unlocked pointer");
                                Check(View.players.Count(b=>b.alive)>=2&&CameraPlayer.z<3,"the spectator exercise has multiple living targets in front of the bar");
                                ScreenCapture.CaptureScreenshot(Path.Combine(directory,"spectator-controls.png"));
                                yield return new WaitForSeconds(.25f);
                                string watched=ViewpointId;CycleSpectator(-1);
                                Check(ViewpointId!=watched,"Previous changes the tutorial spectator target");
                                CycleSpectator(1);Check(ViewpointId==watched,"Next changes back to the original spectator target");
                                testedSpectator=true;CycleSpectator(1);
                            }
                            break;
                        case WalkthroughBeat.Exit:Walk();break;
                    }
                    if(beat==WalkthroughBeat.Boost&&p.room==0&&!checkedReturn)
                    {
                        Check((ViewCamera.cullingMask&(1<<SaloonSmokingArea.OutdoorLayer))==0&&trainingMap.ActionLight.enabled,
                            "returning indoors hides the sunset and restores the focused action light");checkedReturn=true;
                    }
                    if((p.body.transform.position-lastPosition).sqrMagnitude>.08f)
                    {lastPosition=p.body.transform.position;stuckAt=Time.realtimeSinceStartup;}
                    bool walking=TutorialGoal.HasValue&&(beat==WalkthroughBeat.Walk||beat==WalkthroughBeat.Heal||beat==WalkthroughBeat.Boost||
                        beat==WalkthroughBeat.Sheriff||beat==WalkthroughBeat.Exit||
                        (beat==WalkthroughBeat.Coin||beat==WalkthroughBeat.Blackjack)&&!walkthroughTable||beat==WalkthroughBeat.Smoke&&p.room!=3);
                    if(walking&&p.busyUntil==0&&Time.realtimeSinceStartup-stuckAt>7)
                        throw new InvalidOperationException("SALOON_WALKTHROUGH FAIL blocked path in "+beat+" at "+p.body.transform.position+" goal="+TutorialGoal+" notice="+p.notice);
                    yield return null;
                }
                void CheckNoMenu(){if(MenuOpen)throw new InvalidOperationException("Unexpected lecture screen during "+WalkthroughStep);}
                Check(WalkthroughFinished,"all required interactions complete on the continuous route");
                Check(Time.realtimeSinceStartup-started<300&&WalkthroughSeconds<300,"the physical route and real action waits finish in under five minutes");
                Check(trainingMap.Root==mapRoot,"every station uses the same training map without rebuilding the match");
                Check(capturedBeer&&capturedCards,"the action light follows waypoints and reaches the real beer and card stations");
                Check(players.Values.Where(b=>b.bot).All(b=>b.shots==0),"training opponents never attack");
                Check(MenuOpen,"walking to the exit opens only the final completion screen");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"complete.png"));yield return null;
                Debug.Log("SALOON_WALKTHROUGH_TIME seconds="+(Time.realtimeSinceStartup-started).ToString("F1"));
                EndTutorial();while(TutorialClosing||InRoom)yield return null;
                Check(GameObject.Find("Saloon World")!=null&&GameObject.Find("Saloon entrance sunset")!=null&&!TutorialWalkthrough&&trainingMap==null,"exiting restores the normal map and front sunset");
                Check(RenderSettings.ambientLight==originalAmbient&&RenderSettings.ambientMode==originalAmbientMode&&ViewCamera.backgroundColor==originalBackground,
                    "exiting restores normal match lighting and background");
                Check(ReferenceEquals(tuning,original)&&BotsEnabled==oldBots&&BotMatchSize==oldSize,"exiting restores practice settings");
                OpenTutorial();BeginTutorial();BeginTutorialTopic(Array.FindIndex(SaloonTutorialLessons.All,l=>l.Task==TutorialTask.Heal));
                Check(!TutorialWalkthrough&&TutorialActive&&TutorialLesson.Task==TutorialTask.Heal&&MenuOpen,"a help topic can still open its separate practice example");
                Check(GameObject.Find("Saloon World")!=null,"switching to topic practice restores its correct map");
                EndTutorial();while(TutorialClosing||InRoom)yield return null;
                VerificationPassed=true;Debug.Log("SALOON_WALKTHROUGH PASS checks="+checks+" previews="+directory);
            }
            finally
            {
                if(hadSeen)PlayerPrefs.SetString(TutorialSeenKey,seen);else PlayerPrefs.DeleteKey(TutorialSeenKey);
                PlayerPrefs.Save();VerificationFinished=true;
                PlayerName=oldName;
            }
        }
    }
}
#endif
