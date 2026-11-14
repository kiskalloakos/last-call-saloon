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
        public IEnumerator VerifyWesternCharacter()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string message)
            {
                if(!ok){VerificationFinished=true;Debug.LogError("SALOON_WESTERN FAIL "+message);throw new InvalidOperationException(message);}
                Debug.Log("SALOON_WESTERN_CHECK "+message);
            }
            string directory=Path.Combine(Path.GetTempPath(),"SaloonPrototype-EditorPreview");Directory.CreateDirectory(directory);
            var hero=players.Values.First(p=>p.bot);
            var avatar=avatars[hero.id.ToString()];var motion=avatar.GetComponentInChildren<SaloonCharacterMotion>(true);
            Check(motion!=null&&avatars.Values.All(a=>a.GetComponentInChildren<SaloonCharacterMotion>(true)!=null),"every player uses the Western Gunman");
            var reference=motion.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r=>r.sharedMaterials).ToArray();
            Check(avatars.Values.All(a=>a.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r=>r.sharedMaterials).SequenceEqual(reference)),"all players have identical original materials");
            IEnumerator Capture(string name,bool frameHero=true)
            {
                externalCamera=true;MenuOpen=false;ShowRole=false;RefreshViewpoint();
                if(frameHero)
                {
                    var center=hero.body.transform.position+Vector3.up*.85f;
                    externalPosition=center+Quaternion.Euler(0,hero.yaw,0)*new Vector3(1.7f,.25f,2.5f);
                    externalRotation=Quaternion.LookRotation(center-externalPosition);
                }
                yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"western-"+name+".png"));yield return null;
            }
            Finish("WESTERN PREVIEW");ReopenLobby();
            hero.controller.enabled=false;hero.body.transform.SetPositionAndRotation(new Vector3(0,.08f,-5.5f),Quaternion.identity);hero.controller.enabled=true;hero.yaw=0;hero.pitch=0;hero.move=Vector2.zero;hero.poseVersion++;Broadcast();
            yield return new WaitForSeconds(.4f);
            Check(motion.CurrentAnimation=="Idle"&&motion.SittingBlend==0,"standing idle restores the uncompressed rig");
            yield return Capture("01-standing");
            var leg=motion.Bone(HumanBodyBones.LeftUpperLeg);var before=leg.rotation;var origin=hero.body.transform.position;float legSwing=0,walkingBlend=0;
            for(int i=0;i<14;i++)
            {OnCommand(hero.id,new Command{kind="input",z=1,yaw=0,poseVersion=hero.poseVersion});yield return new WaitForSeconds(.04f);legSwing=Mathf.Max(legSwing,Quaternion.Angle(before,leg.rotation));walkingBlend=Mathf.Max(walkingBlend,motion.WalkingBlend);}
            Check(Vector3.Distance(origin,hero.body.transform.position)>.5f&&walkingBlend>.8f&&legSwing>5,"real host movement animates walking legs");
            yield return Capture("02-walking");hero.move=Vector2.zero;hero.lastInput=-10;
            StartMatch();yield return new WaitForSeconds(.4f);
            var thigh=motion.Bone(HumanBodyBones.RightUpperLeg);var knee=motion.Bone(HumanBodyBones.RightLowerLeg);var foot=motion.Bone(HumanBodyBones.RightFoot);
            Check(motion.SittingBlend>.99f&&Vector3.Angle(thigh.position-knee.position,foot.position-knee.position)<145&&motion.transform.localScale==Vector3.one,"sitting bends the knees without squashing the model");
            yield return Capture("03-sitting");
            foreach(var p in players.Values){p.chosenRoom=0;OnCommand(p.id,new Command{kind="ready",x=1});}TickServer();
            hero.yaw=180;hero.pitch=-8;hero.lastAction=-100;OnCommand(hero.id,new Command{kind="draw"});Broadcast();yield return new WaitForSeconds(tuning.drawSeconds+.15f);
            yield return Capture("04-revolver-grip");
            hero.nextShot=0;hero.lastAction=-100;OnCommand(hero.id,new Command{kind="fire"});Broadcast();yield return new WaitForSeconds(.04f);
            Check(hero.shots==1&&motion.CurrentAnimation=="Shooting","real shot drives the new rig's recoil (shots="+hero.shots+", pose="+motion.CurrentAnimation+")");yield return Capture("05-shooting");
            yield return new WaitForSeconds(.32f);yield return Capture("06-revolver-flip");yield return new WaitForSeconds(.9f);
            rounds.Start(players.Keys,Time.time,0,60,6);foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
            hero.yaw=180;hero.drawn=false;hero.hp=40;hero.beer=1;hero.beerHealing=SaloonHealth.BeerHealing;hero.lastAction=-100;OnCommand(hero.id,new Command{kind="drink"});Broadcast();
            yield return new WaitForSeconds(tuning.drinkSeconds*.3f);
            Check(motion.CurrentAnimation=="Drinking"&&hero.busyAction=="drink","real drink lifts the mug and animates the character");yield return Capture("07-drinking");
            var mug=avatar.GetComponentsInChildren<Transform>().First(t=>t.name=="Mug visual");
            Check(Vector3.Distance(mug.TransformPoint(Vector3.up*.155f),motion.Mouth)<.07f,"the mug rim reaches the mouth on this skeleton");
            yield return new WaitForSeconds(tuning.drinkSeconds+.1f);
            Check(hero.hp==60&&!PrivateSnapshot(hero.id).players.Single(p=>p.id==hero.id.ToString()).drinking,"drinking completes and returns to the seated pose");
            externalCamera=false;RefreshViewpoint();
            Debug.Log("SALOON_WESTERN PASS same Gunman/materials, idle, host walking, bent-leg sitting, real draw/shot/flip and mug-to-mouth drinking; previews="+directory);
            VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
