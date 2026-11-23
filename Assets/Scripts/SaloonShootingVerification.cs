#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyMultiplayerShooting()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            var peers=new List<SaloonConnection>();var states=new Dictionary<SaloonConnection,Snapshot>();
            void Check(bool ok,string message)
            {
                Debug.Log((ok?"SALOON_SHOOTING_CHECK ":"SALOON_SHOOTING FAIL ")+message);
                if(!ok){VerificationFinished=true;throw new InvalidOperationException(message);}
            }
            IEnumerator Wait(Func<bool> condition,string message)
            {
                float until=Time.realtimeSinceStartup+8;
                while(!condition()&&Time.realtimeSinceStartup<until)yield return null;
                Check(condition(),message);
            }
            try
            {
                for(int i=0;i<7;i++)
                {
                    var peer=new GameObject("Shooting UDP peer").AddComponent<SaloonConnection>();peers.Add(peer);
                    peer.Initialize();peer.SnapshotReceived+=s=>states[peer]=s;
                    peer.StartLan(false,"127.0.0.1",ushort.Parse(Port));
                }
                yield return Wait(()=>HumanCount==8&&peers.All(p=>p.Connected&&states.ContainsKey(p)),"eight real UDP players connect");
                StartMatch();
                var shooter=players[peers[0].LocalId];var second=players[peers[1].LocalId];var victim=players[peers[6].LocalId];
                var host=players[Connection.LocalId];
                void Place(Participant p,Vector3 position)
                {
                    p.controller.enabled=false;p.body.transform.position=position;p.move=Vector2.zero;p.vertical=0;
                    UpdatePlayerColliders(p);
                }
                void Window()
                {
                    ResetHeist();pendingOutcome=outcome="";duels.Clear();roomTurns.Clear();int index=0;
                    foreach(var p in players.Values)
                    {
                        p.hp=100;p.hits=p.shots=p.headshots=0;p.hitAt=-10;p.deathAt=-1;p.ammo=6;p.nextShot=0;
                        p.busyUntil=0;p.busyAction="";p.drawn=true;p.fireAfterDraw=false;p.pendingShot=null;CancelShotCharge(p);
                        p.cigaretteBuff=false;p.beer=0;p.beerChoiceUntil=0;p.seat=-1;p.crouched=p.crouchHeld=false;
                        p.room=p.chosenRoom=0;p.companion=null;p.poseVersion++;p.lastAction=-100;p.yaw=p.pitch=p.bodyYaw=0;
                        p.combatHistory.Clear();Place(p,new Vector3(-7+index++*1.8f,.08f,-6.3f));
                    }
                    shooter.role=Role.Sheriff;second.role=Role.Deputy;victim.role=Role.Outlaw;
                    rounds.Start(players.Keys,Time.time,1,120,0);foreach(var id in players.Keys)rounds.SetReady(id,true);
                    rounds.Tick(Time.time,id=>true);phase=rounds.Phase;
                    Place(shooter,new Vector3(-6,.08f,1));Place(second,new Vector3(-2,.08f,1));Place(victim,new Vector3(-4,.08f,1));
                    CaptureCombatFrames();Broadcast();
                }
                Vector3 Head(Participant p)=>p.body.transform.TransformPoint(p.head.center);
                Command Trigger(Participant p,Vector3 point,float time,bool accurate=false)
                {
                    var direction=point-Eye(p);var angles=Quaternion.LookRotation(direction).eulerAngles;
                    return new Command{kind="fire",shotAim=true,accurateShot=accurate,shotSequence=p.shotChargeSequence,yaw=angles.y,pitch=Mathf.DeltaAngle(0,angles.x),
                        shotTime=time,shotRound=rounds.Round,poseVersion=p.poseVersion};
                }
                Command Charge(Participant p)=>new Command{kind="chargeShot",shotSequence=p.shotChargeSequence+1,shotRound=rounds.Round,poseVersion=p.poseVersion};
                Command Shot(Participant p,Vector3 point,float time)
                {
                    // Rewind cases isolate precision/aim from timing, which is exercised
                    // below through real UDP charge, cancel and trigger commands.
                    OnCommand(p.id,Charge(p));p.shotChargeAt=Time.time-AccurateShotSeconds;
                    return Trigger(p,point,time,true);
                }
                IEnumerator Synced()
                {
                    yield return Wait(()=>peers.All(peer=>states[peer].players.Any(p=>p.id==peer.LocalId.ToString()&&p.poseVersion==players[peer.LocalId].poseVersion)&&
                        states[peer].players.All(view=>players.TryGetValue(ulong.Parse(view.id),out var p)&&view.poseVersion==p.poseVersion&&view.room==p.room)),"current shooting poses reach every client");
                }

                Window();yield return Synced();
                peers[0].Send(Trigger(shooter,Head(victim),Time.time,true));
                yield return new WaitForSeconds(.08f);
                Check(shooter.shots==0&&shooter.ammo==6&&!rounds.HasSpent(shooter.id),"accurate trigger cannot skip its host-authorized charge");
                shooter.cigaretteBuff=true;peers[0].Send(Charge(shooter));
                yield return Wait(()=>shooter.chargingShot,"real UDP begins the deliberate shot charge");
                var charging=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(shooter.id),out _)).players.First(p=>p.id==shooter.id.ToString());
                Check(charging.chargingShot&&charging.shotChargeSequence==shooter.shotChargeSequence,"charge state and animation sequence survive compressed snapshots");
                peers[0].Send(Trigger(shooter,Head(victim),Time.time,true));
                yield return Wait(()=>shooter.pendingChargeShot!=null,"an early final trigger waits for the half-second host timer");
                Check(Time.time-shooter.shotChargeAt<AccurateShotSeconds&&shooter.shots==0&&shooter.cigaretteBuff&&!rounds.HasSpent(shooter.id),"charging spends no ammo, action or cigarette buff");
                peers[0].Send(new Command{kind="cancelShot",shotSequence=shooter.shotChargeSequence});
                yield return Wait(()=>!shooter.chargingShot,"releasing the trigger cancels a queued deliberate shot");
                yield return new WaitForSeconds(AccurateShotSeconds+.05f);
                Check(shooter.shots==0&&shooter.ammo==6&&shooter.cigaretteBuff&&!rounds.HasSpent(shooter.id),"cancelled charge never fires later or consumes resources");
                peers[0].Send(Charge(shooter));yield return Wait(()=>shooter.chargingShot,"a fresh hold can charge again");
                yield return new WaitForSeconds(AccurateShotSeconds+.02f);
                var accurate=Trigger(shooter,Head(victim),Time.time,true);peers[0].Send(accurate);peers[0].Send(accurate);
                yield return Wait(()=>shooter.shots==1,"fully charged deliberate shot fires once");
                Check(victim.hp==60&&shooter.ammo==5&&!shooter.cigaretteBuff&&rounds.HasSpent(shooter.id),"accurate shot keeps normal buffed damage and one-bullet/action cost");

                Window();yield return Synced();shooter.cigaretteBuff=true;
                peers[0].Send(Trigger(shooter,Head(victim),Time.time));
                yield return Wait(()=>shooter.shots==1,"quick shot fires without a charge");
                Check(victim.hp==60&&shooter.ammo==5&&!shooter.cigaretteBuff&&rounds.HasSpent(shooter.id),"close-range quick shot has the same damage and resource cost as the accurate shot");
                var forward=new Ray(Vector3.zero,Vector3.forward);
                foreach(float movement in new[]{0f,.5f,1f})foreach(var sample in new[]{Vector2.zero,Vector2.right,Vector2.up,new Vector2(.6f,.8f)})
                {
                    var spread=SpreadShot(forward,QuickShotSpread(movement),sample);
                    Check(Vector3.Angle(forward.direction,spread.direction)<=QuickShotSpread(movement)+.001f,"quick spread stays within its movement-dependent cone");
                }
                Check(QuickShotSpread(1)>QuickShotSpread(0)&&Mathf.Tan(QuickShotSpread(0)*Mathf.Deg2Rad)*2<victim.head.radius,
                    "movement widens quick shots while a stationary two-metre center-head aim remains dependable");

                Window();yield return Synced();peers[0].Send(Charge(shooter));
                yield return Wait(()=>shooter.chargingShot,"charge begins before a quick-shot replacement");
                peers[0].Send(Trigger(shooter,Head(victim),Time.time));
                yield return Wait(()=>shooter.shots==1,"quick shot interrupts an unfinished accurate shot immediately");
                yield return new WaitForSeconds(AccurateShotSeconds+.05f);
                Check(!shooter.chargingShot&&shooter.pendingChargeShot==null&&shooter.shots==1&&shooter.ammo==5,"two firing modes cannot stack shots from the same hold");

                Window();yield return Synced();peers[0].Send(Charge(shooter));
                yield return Wait(()=>shooter.chargingShot,"charge begins before holstering");
                peers[0].Send(new Command{kind="draw"});
                yield return Wait(()=>!shooter.drawn&&!shooter.chargingShot,"Q holsters and cancels an unfinished charge");
                yield return new WaitForSeconds(AccurateShotSeconds+.05f);
                Check(shooter.shots==0&&shooter.ammo==6&&!rounds.HasSpent(shooter.id),"holstered charge never fires later or spends an action");

                Window();yield return Synced();
                Check(victim.head.radius>.07f&&victim.head.radius<.25f,"head volume is measured from the character mesh (radius="+victim.head.radius+")");
                shooter.yaw=180;shooter.pitch=55;shooter.lastAction=Time.time;
                var shot=Shot(shooter,Head(victim),states[peers[0]].serverTime);
                peers[0].Send(shot);peers[0].Send(shot);
                yield return Wait(()=>victim.hits==1,"click-time aim hits despite stale movement aim and action debounce");
                Check(victim.hp==80&&shooter.shots==1&&shooter.ammo==5&&rounds.HasSpent(shooter.id),"duplicate triggers spend exactly one action and one bullet");
                Check(shooter.yaw==180&&shooter.pitch==55,"a shot cannot overwrite newer movement/look input");

                Window();Place(shooter,new Vector3(-7,.08f,-6.3f));Place(host,new Vector3(-6,.08f,1));host.poseVersion++;
                CaptureCombatFrames();Broadcast();yield return Synced();
                shotKick=0;hitKick=1;damageCameraImpulse=new Vector3(4,0,0);
                var cameraAim=Quaternion.LookRotation(Head(victim)-Eye(host)).eulerAngles;
                yaw=cameraAim.y;pitch=Mathf.DeltaAngle(0,cameraAim.x)-4;
                Shoot();
                Check(host.shots==1&&victim.hp==80,"the actual trigger path includes camera kick rather than firing along a different ray");
                hitKick=shotKick=0;damageCameraImpulse=Vector3.zero;

                foreach(var stance in new[]{"seated","crouched","looking up","looking aside"})
                {
                    Window();victim.poseVersion++;
                    victim.seat=stance=="seated"?100:-1;
                    victim.crouchHeld=victim.crouched=stance=="crouched";victim.lastInput=Time.time;
                    victim.pitch=stance=="looking up"?-55:0;victim.yaw=stance=="looking aside"?60:0;
                    UpdatePlayerColliders(victim);CaptureCombatFrames();Broadcast();yield return Synced();
                    peers[0].Send(Shot(shooter,Head(victim),states[peers[0]].serverTime));
                    yield return Wait(()=>victim.hits==1,"the animated head remains hittable while "+stance);
                }

                foreach(bool reverse in new[]{false,true})
                {
                    Window();yield return Synced();
                    var a=Shot(shooter,Head(victim),states[peers[0]].serverTime);
                    var b=Shot(second,Head(victim),states[peers[1]].serverTime);
                    if(reverse){peers[1].Send(b);peers[0].Send(a);}else{peers[0].Send(a);peers[1].Send(b);}
                    yield return Wait(()=>victim.hits==2,"both near-simultaneous UDP headshots register (reverse="+reverse+")");
                    Check(victim.hp==60&&shooter.shots==1&&second.shots==1,"two shooters cause cumulative damage without an immunity window");
                    yield return Wait(()=>states[peers[6]].health==60&&states[peers[0]].hitSequence==1&&states[peers[1]].hitSequence==1,"damage and both confirmations reach the real clients");
                }

                Window();yield return Synced();var resting=Head(victim);
                TakeDamage(victim,Vector3.right);Broadcast();yield return new WaitForSeconds(.12f);
                CaptureCombatFrames();Broadcast();yield return null;yield return null;
                var avatar=avatars[victim.id.ToString()];var visible=avatar.RenderedHead;
                Check(Vector3.Distance(Head(victim),resting)>.025f,"authoritative head follows the visible flinch");
                var visibleShot=Shot(shooter,visible,avatar.RenderedServerTime);
                visibleShot.shotInterpolated=true;visibleShot.shotFromTime=avatar.RenderFromTime;visibleShot.shotToTime=avatar.RenderToTime;
                peers[0].Send(visibleShot);
                yield return Wait(()=>victim.hits==2,"a second shooter can hit the head they see during its flinch");
                Check(victim.hp==60,"the flinching target loses both hits' health");

                Window();yield return Synced();CaptureCombatFrames();
                float fromTime=Time.time;var fromHead=Head(victim);TakeDamage(victim,Vector3.right);
                yield return new WaitForSeconds(.11f);CaptureCombatFrames();
                float toTime=Time.time;var toHead=Head(victim);float between=(fromTime+toTime)*.5f;
                var displayedHead=(fromHead+toHead)*.5f;
                Check(Vector3.Distance(CombatFrameAt(victim,between).Head,displayedHead)>.025f,"the fixture has a nonlinear flinch between two published poses");
                var interpolated=Shot(shooter,displayedHead,between);
                interpolated.shotInterpolated=true;interpolated.shotFromTime=fromTime;interpolated.shotToTime=toTime;
                peers[0].Send(interpolated);
                yield return Wait(()=>victim.hits==2,"rewind matches the client's interpolated head, not a different point on the flinch curve");

                Window();yield return Synced();
                var oldShot=Shot(shooter,Head(victim),states[peers[0]].serverTime);
                yield return new WaitForSeconds(.08f);
                Place(victim,victim.body.transform.position+Vector3.forward*.8f);CaptureCombatFrames();
                var now=Head(victim);
                Check(RayTarget(shooter,new Ray(Eye(shooter),Quaternion.Euler(oldShot.pitch,oldShot.yaw,0)*Vector3.forward),65)?.collider!=victim.head,"the delayed shot really misses the present head position");
                peers[0].Send(oldShot);
                yield return Wait(()=>victim.hits==1,"bounded rewind hits the authoritative position shown to the shooter");
                Check(Vector3.Distance(Head(victim),now)<.001f&&Mathf.Abs(victim.body.transform.position.z-1.8f)<.001f,"rewind restores colliders and never moves the live player");

                Window();yield return Synced();yield return null;
                avatar=avatars[victim.id.ToString()];
                yield return new WaitForSeconds(.04f);
                // Save the actual last presented pose after waiting, then publish a
                // new target without allowing LateUpdate to present it yet.
                var presented=avatar.RenderedHead;var beforeRender=Shot(shooter,presented,avatar.RenderedServerTime);
                beforeRender.shotInterpolated=true;beforeRender.shotFromTime=avatar.RenderFromTime;beforeRender.shotToTime=avatar.RenderToTime;
                float presentedTime=avatar.RenderedServerTime;
                Place(victim,victim.body.transform.position+Vector3.forward*.8f);CaptureCombatFrames();Broadcast();
                Check(avatar.RenderedHead==presented&&avatar.RenderedServerTime==presentedTime,"an arriving snapshot cannot rewrite the pose/time pair of the last rendered head");
                peers[0].Send(beforeRender);
                yield return Wait(()=>victim.hits==1,"a click between snapshot receipt and the next render still hits the head that was shown");

                Window();yield return Synced();
                var beforeStance=Trigger(shooter,Head(victim),states[peers[0]].serverTime);
                yield return new WaitForSeconds(.04f);
                shooter.poseVersion++;Place(shooter,shooter.body.transform.position+Vector3.forward*.6f);CaptureCombatFrames();
                peers[0].Send(beforeStance);
                yield return Wait(()=>victim.hits==1,"a click from a recently acknowledged stance is not lost while standing confirmation is in flight");

                Window();yield return Synced();
                var body=victim.body.transform.TransformPoint(victim.hitbox.center);
                peers[0].Send(Shot(shooter,body,states[peers[0]].serverTime));
                yield return Wait(()=>shooter.shots==1,"body shot is fired and acknowledged");
                Check(victim.hp==100&&victim.hits==0,"lag compensation does not turn body hits into headshots");

                Window();Place(shooter,new Vector3(-5,.08f,-.6f));Place(victim,new Vector3(-5,.08f,-2.5f));CaptureCombatFrames();Broadcast();yield return Synced();
                peers[0].Send(Trigger(shooter,Head(victim),Time.time));
                yield return Wait(()=>shooter.shots==1,"shot toward a player behind glass is fired");
                Check(victim.hp==100&&shooter.shotSurface==SaloonImpactSurface.Glass,"rewind preserves solid glass and wall occlusion");

                Window();host.room=3;Place(host,SaloonSmokingArea.Center);CaptureCombatFrames();Broadcast();yield return Synced();yield return null;
                Check(!avatars.ContainsKey(victim.id.ToString()),"the smoking host has no visible indoor target avatar");
                peers[0].Send(Shot(shooter,Head(victim),states[peers[0]].serverTime));
                yield return Wait(()=>victim.hp==80,"head detection still works while the host cannot see the target");

                Window();yield return Synced();shooter.drawn=false;Broadcast();
                peers[0].Send(new Command{kind="draw"});
                peers[0].Send(Trigger(shooter,Head(victim),Time.time));
                yield return Wait(()=>shooter.fireAfterDraw,"a trigger during drawing is buffered");
                TakeDamage(shooter,Vector3.right);Broadcast();
                Check(shooter.busyAction=="draw"&&shooter.fireAfterDraw,"a nonlethal hit does not discard a queued trigger");
                yield return Wait(()=>victim.hits==1&&shooter.shots==1,"the buffered click fires once when drawing completes");

                Window();yield return Synced();
                foreach(var invalid in new[]{"old","future","round","pose","unknown pose","nan","span order","span future","span old"})
                {
                    var bad=Shot(shooter,Head(victim),Time.time);
                    if(invalid=="old")bad.shotTime-=1;if(invalid=="future")bad.shotTime+=1;
                    if(invalid=="round")bad.shotRound++;if(invalid=="pose")bad.poseVersion++;if(invalid=="nan")bad.yaw=float.NaN;
                    if(invalid=="unknown pose")bad.poseVersion-=100;
                    if(invalid.StartsWith("span"))
                    {
                        bad.shotInterpolated=true;bad.shotFromTime=bad.shotToTime=bad.shotTime;
                        if(invalid=="span order")bad.shotFromTime++;
                        if(invalid=="span future")bad.shotToTime++;
                        if(invalid=="span old")bad.shotFromTime-=2;
                    }
                    OnCommand(shooter.id,bad);
                    Check(shooter.shots==0&&shooter.ammo==6&&!rounds.HasSpent(shooter.id)&&victim.hp==100,"invalid "+invalid+" shot is rejected without spending ammunition");
                }
                Check(shooter.notice.Contains("out of date"),"rejected aim has a visible explanation");
                Debug.Log("SALOON_SHOOTING PASS click aim, duplicate protection, concurrent hits, animated heads, movement rewind, occlusion, hidden host targets, queued draw and invalid timestamps");
                VerificationFinished=VerificationPassed=true;
            }
            finally
            {
                foreach(var peer in peers)
                {peer.Manager.Shutdown();Destroy(peer.Manager.gameObject);Destroy(peer.gameObject);}
            }
        }
    }
}
#endif
