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
        public IEnumerator VerifyHitReactionsAndGrip()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string message)
            {
                if(!ok){VerificationFinished=true;Debug.LogError("SALOON_COMBAT FAIL "+message);throw new InvalidOperationException(message);}
                Debug.Log("SALOON_COMBAT_CHECK "+message);
            }
            string directory=Path.Combine(Path.GetTempPath(),"SaloonPrototype-EditorPreview");Directory.CreateDirectory(directory);
            var host=players[Connection.LocalId];var hero=players.Values.First(p=>p.bot);var others=players.Values.Where(p=>p!=host&&p!=hero).ToArray();
            var avatar=avatars[hero.id.ToString()];var motion=avatar.GetComponentInChildren<SaloonCharacterMotion>(true);
            var upper=motion.Bone(HumanBodyBones.RightUpperArm);var lower=motion.Bone(HumanBodyBones.RightLowerArm);var hand=motion.Bone(HumanBodyBones.RightHand);
            var middle=motion.Bone(HumanBodyBones.RightMiddleProximal);var thumb=motion.Bone(HumanBodyBones.RightThumbProximal);
            var wristRest=hand.localRotation;var middleRest=middle.localRotation;float upperLength=Vector3.Distance(upper.position,lower.position),lowerLength=Vector3.Distance(lower.position,hand.position);
            var weapon=avatar.transform.Find("Low-poly revolver");
            void Window()
            {
                pendingOutcome="";outcome="";
                foreach(var p in players.Values){p.hp=60;p.hits=0;p.hitAt=-10;p.deathAt=-1;p.beer=0;p.hitbox.enabled=true;p.busyUntil=0;p.busyAction="";p.drawn=false;p.ammo=6;p.shots=0;p.lastAction=-100;p.nextShot=0;p.chosenRoom=0;p.companion=null;}
                host.role=Role.Sheriff;hero.role=Role.Outlaw;others[0].role=Role.Deputy;others[1].role=Role.Outlaw;others[2].role=Role.Renegade;
                host.chosenRoom=hero.chosenRoom=1;
                rounds.Start(players.Keys,Time.time,0,120,6);foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
                var duel=duels[1];duel.claimed=duel.flipping=duel.revealed=true;duel.first=hero.id;duel.firstUntil=Time.time+30;duel.result="HEADS";Broadcast();
            }
            IEnumerator Capture(string name,bool close=true,bool profile=false)
            {
                if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)yield break;
                externalCamera=true;MenuOpen=false;ShowRole=false;RefreshViewpoint();ViewCamera.fieldOfView=close?45:65;
                var rotation=Quaternion.Euler(0,hero.yaw,0);
                var center=hero.body.transform.position+rotation*new Vector3(.17f,hero.seat>=0?1.12f:1.35f,.22f);
                externalPosition=center+rotation*(close?(profile?new Vector3(.8f,.08f,.12f):new Vector3(-.10f,.12f,.80f)):new Vector3(.7f,.15f,1.7f));
                externalRotation=Quaternion.LookRotation(center-externalPosition);
                yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"combat-"+name+".png"));yield return null;
            }
            yield return VerifyDrawClearance(Check);
            Window();yield return new WaitForSeconds(.4f);
            var lightObject=new GameObject("Combat preview light");var previewLight=lightObject.AddComponent<Light>();previewLight.type=LightType.Point;previewLight.range=5;previewLight.intensity=1.8f;lightObject.transform.position=hero.body.transform.position+Vector3.up*1.8f+Forward(hero)*1.1f;
            OnCommand(hero.id,new Command{kind="draw"});Broadcast();yield return new WaitForSeconds(tuning.drawSeconds+.1f);
            foreach(float angle in new[]{-35f,0,35f})
            {
                hero.pitch=angle;Broadcast();yield return new WaitForSeconds(.15f);
                Check(Quaternion.Angle(hand.localRotation,wristRest)<.1f,"aim "+angle+" preserves the native wrist rotation");
                Check(Mathf.Abs(Vector3.Distance(upper.position,lower.position)-upperLength)<.001f&&Mathf.Abs(Vector3.Distance(lower.position,hand.position)-lowerLength)<.001f,"aim never stretches the upper arm or forearm");
                Check(Quaternion.Angle(middle.localRotation,middleRest)<.1f,"grip does not twist the mitten finger joints");
                Check(Vector3.Distance(weapon.TransformPoint(SaloonGunGrip.PalmInWeapon),(middle.position+thumb.position)*.5f)<.005f,"revolver stays at the palm grip");
                Check(Vector3.Dot(weapon.forward,Forward(hero))>.995f,"revolver points along the player's aim");
            }
            hero.pitch=0;Broadcast();yield return new WaitForSeconds(.1f);yield return Capture("01-seated-grip");yield return Capture("02-seated-grip-profile",true,true);
            hero.seat=-1;hero.controller.enabled=false;hero.body.transform.position=new Vector3(0,.08f,-5.5f);hero.yaw=0;hero.poseVersion++;Broadcast();yield return new WaitForSeconds(.4f);
            yield return Capture("03-standing-grip");yield return Capture("04-standing-grip-profile",true,true);
            Check(Quaternion.Angle(hand.localRotation,wristRest)<.1f,"standing grip also leaves the wrist neutral");
            Window();yield return new WaitForSeconds(.4f);
            var spine=motion.Bone(HumanBodyBones.Spine);var resting=spine.rotation;var bodyPosition=hero.body.transform.position;
            var aim=Eye(hero)-Eye(host);host.yaw=Mathf.Atan2(aim.x,aim.z)*Mathf.Rad2Deg;host.pitch=0;host.drawn=true;
            duels[1].first=host.id;host.lastAction=-100;OnCommand(host.id,new Command{kind="fire"});Broadcast();yield return new WaitForSeconds(.08f);
            Check(hero.hp==40&&hero.hits==1&&host.shots==1,"a real legal shot records one authoritative hit and 20% health damage");
            Check(motion.HitAmount>.5f&&Quaternion.Angle(resting,spine.rotation)>7&&avatar.DamageFlashVisible,"the victim visibly flinches and flashes after a non-lethal shot");
            var wire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(host.id),out _)).players.Single(p=>p.id==hero.id.ToString());
            Check(wire.hits==1&&wire.hitAge<.3f&&Vector3.Dot(new Vector3(wire.hitX,wire.hitY,wire.hitZ),Forward(host))>.99f,"hit sequence, age and direction survive the network wire");
            float started=avatar.LastVisualHitAt;Broadcast();Broadcast();
            Check(Mathf.Abs(avatar.LastVisualHitAt-started)<.005f,"duplicate snapshots do not restart the hit animation");
            yield return Capture("05-shot-flinch",false);
            yield return new WaitForSeconds(.75f);
            Check(motion.HitAmount==0&&!avatar.DamageFlashVisible&&Quaternion.Angle(resting,spine.rotation)<1.5f,"the hit animation settles back to the original pose");
            Check(hero.body.transform.position==bodyPosition,"hit reactions never move authoritative seats or collision bodies");
            yield return Capture("06-recovered",false);
            hero.hp=60;TakeDamage(hero,Vector3.right);Broadcast();yield return new WaitForSeconds(.08f);
            Check(hero.hits==2&&motion.HitAmount>.5f,"another hit restarts the directional reaction");yield return Capture("07-side-flinch",false);
            yield return new WaitForSeconds(.75f);hero.hp=20;TakeDamage(hero,-Forward(hero));Broadcast();yield return new WaitForSeconds(.1f);
            Check(!hero.Alive&&avatar.DeathBody!=null&&avatar.DeathBody.BodyCount==11&&avatar.CorpseVisible,"lethal damage hands over to the existing ragdoll");
            externalCamera=false;ViewCamera.fieldOfView=78;RefreshViewpoint();
            Destroy(lightObject);
            Debug.Log("SALOON_COMBAT PASS native wrist and arm lengths, palm attachment, seated/standing aim, real shot flinch/flash, hit wire, duplicate handling, recovery and lethal ragdoll; previews="+directory);
            VerificationPassed=VerificationFinished=true;
        }

        IEnumerator VerifyDrawClearance(Action<bool,string> check)
        {
            var model=Instantiate(Resources.Load<GameObject>(SaloonCharacterMotion.ResourcePath));
            model.SetActive(false);
            model.transform.rotation=Quaternion.Euler(0,137,0);
            var weapon=new GameObject("Draw clearance probe");
            try
            {
                var motion=model.GetComponent<SaloonCharacterMotion>();motion.Initialize(false);
                var grip=new SaloonGunGrip(motion);
                var chest=motion.Bone(HumanBodyBones.Chest);
                var shoulder=motion.Bone(HumanBodyBones.RightUpperArm);
                var elbow=motion.Bone(HumanBodyBones.RightLowerArm);
                var wrist=motion.Bone(HumanBodyBones.RightHand);
                var outward=(model.transform.right+model.transform.forward).normalized;
                float upperLength=Vector3.Distance(shoulder.position,elbow.position),lowerLength=Vector3.Distance(elbow.position,wrist.position);
                float Depth(Vector3 point)=>Vector3.Dot(point-chest.position,outward);
                foreach(var stance in new[]{"standing","seated","crouched","walking"})
                {
                    bool seated=stance=="seated",crouched=stance=="crouched";
                    var velocity=stance=="walking"?model.transform.forward*2:Vector3.zero;
                    for(float until=Time.time+.4f;Time.time<until;)
                    {
                        motion.Pose(seated,velocity,0,0,0,0,crouched);
                        yield return null;
                    }
                    float closestHand=1,closestElbow=1,longestStep=0,lengthError=0,aimError=0;
                    string longestStepPose="";
                    int gaitSamples=stance=="walking"?8:1;
                    for(int gait=0;gait<gaitSamples;gait++)
                    {
                        // Hold this point of the gait cycle while sampling the draw.
                        var shoulderRest=shoulder.localRotation;
                        var elbowRest=elbow.localRotation;
                        var wristRest=wrist.localRotation;
                        foreach(float yaw in new[]{-65f,-45f,0,45f,65f})foreach(float pitch in new[]{-75f,-35f,0,35f,75f})
                        {
                            var aim=model.transform.rotation*Quaternion.Euler(pitch,yaw,0);
                            var previous=Vector3.zero;
                            for(int frame=0;frame<=50;frame++)
                            {
                                float amount=frame/50f;
                                shoulder.localRotation=shoulderRest;elbow.localRotation=elbowRest;wrist.localRotation=wristRest;
                                grip.Pose(weapon.transform,aim*SaloonWeaponPose.DrawRotation(amount),amount);
                                if(amount>=.45f)
                                {
                                    closestHand=Mathf.Min(closestHand,Depth(wrist.position),Depth(grip.Mount));
                                    closestElbow=Mathf.Min(closestElbow,Depth(elbow.position));
                                }
                                float step=frame==0?0:Vector3.Distance(previous,grip.Mount);
                                if(step>longestStep){longestStep=step;longestStepPose=$"yaw {yaw}, pitch {pitch}, draw {amount:F2}";}
                                previous=grip.Mount;
                                lengthError=Mathf.Max(lengthError,Mathf.Abs(Vector3.Distance(shoulder.position,elbow.position)-upperLength),Mathf.Abs(Vector3.Distance(elbow.position,wrist.position)-lowerLength));
                            }
                            aimError=Mathf.Max(aimError,Quaternion.Angle(weapon.transform.rotation,aim));
                        }
                        if(gait+1<gaitSamples)for(float until=Time.time+.1f;Time.time<until;)
                        {
                            motion.Pose(seated,velocity,0,0,0,0,crouched);
                            yield return null;
                        }
                    }
                    check(closestHand>.198f&&closestElbow>.14f,$"{stance} draw clears the coat at extreme aim angles (hand {closestHand:F3}, elbow {closestElbow:F3})");
                    check(longestStep<.065f&&lengthError<.001f&&aimError<.1f,$"{stance} draw is continuous and preserves arm lengths and final aim (step {longestStep:F3} at {longestStepPose}, error {aimError:F2}°)");
                }
            }
            finally{Destroy(model);Destroy(weapon);}
        }
    }
}
#endif
