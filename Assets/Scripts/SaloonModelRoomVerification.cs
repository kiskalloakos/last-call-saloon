#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyModelRoomFixes()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string message)
            {
                if(!ok){VerificationFinished=true;Debug.LogError("SALOON_MODEL_ROOM FAIL "+message);throw new InvalidOperationException(message);}
                Debug.Log("SALOON_MODEL_ROOM_CHECK "+message);
            }
            yield return null;
            SaloonSnapshotVerification.Run();
            Finish("HEAD PREVIEW");ReopenLobby();
            var hero=players.Values.First(p=>p.bot);
            hero.controller.enabled=false;hero.body.transform.position=new Vector3(0,.08f,-5.5f);hero.controller.enabled=true;
            hero.yaw=hero.bodyYaw=hero.pitch=0;hero.move=Vector2.zero;hero.poseVersion++;Broadcast();
            yield return new WaitForSeconds(.4f);
            var avatar=avatars[hero.id.ToString()];var motion=avatar.GetComponentInChildren<SaloonCharacterMotion>(true);
            var head=motion.Bone(HumanBodyBones.Head);var chest=motion.Bone(HumanBodyBones.Chest);var hips=motion.Bone(HumanBodyBones.Hips);
            var restHead=head.rotation;var restChest=chest.rotation;var restHips=hips.rotation;
            var position=hero.body.transform.position;
            externalCamera=true;MenuOpen=false;ShowRole=false;RefreshViewpoint();ViewCamera.fieldOfView=38;
            externalPosition=position+new Vector3(.2f,1.45f,2.5f);externalRotation=Quaternion.LookRotation(position+Vector3.up*1.25f-externalPosition);
            string directory=Path.Combine(Path.GetTempPath(),"SaloonModelRoomFixes");Directory.CreateDirectory(directory);
            IEnumerator Capture(string name)
            {
                yield return null;
                if(Application.isEditor&&Application.isBatchMode)
                    WindowPreview(externalPosition,externalPosition+externalRotation*Vector3.forward,ViewCamera.fieldOfView,"head-"+name);
                else {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));}
                yield return null;
            }
            yield return Capture("01-neutral");
            IEnumerator Look(float yaw,float pitch)
            {
                OnCommand(hero.id,new Command{kind="input",yaw=yaw,pitch=pitch,poseVersion=hero.poseVersion});
                TickServer();Broadcast();yield return new WaitForSeconds(.25f);
                Check(Mathf.Abs(Mathf.DeltaAngle(0,hero.bodyYaw))<.01f&&Quaternion.Angle(restChest,chest.rotation)<.1f&&Quaternion.Angle(restHips,hips.rotation)<.1f,"head gesture leaves standing torso and hips still ("+yaw+", "+pitch+")");
                Check(Quaternion.Angle(restHead,head.rotation)>20,"head follows mouse look with the weapon holstered");
                Check(Vector3.Distance(hero.body.transform.position,position)<.01f,"nodding does not move the player");
            }
            yield return Look(0,35);yield return Capture("02-nod-down");
            yield return Look(0,-35);yield return Capture("03-nod-up");
            yield return Look(40,0);yield return Capture("04-shake-right");
            yield return Look(-40,0);yield return Capture("05-shake-left");
            var wire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(Connection.LocalId),out _));
            var remote=wire.players.Single(p=>p.id==hero.id.ToString());
            Check(Mathf.Abs(Mathf.DeltaAngle(remote.yaw,-40))<.01f&&remote.bodyYaw==0,"snapshot carries head look separately from body facing");
            Check(Mathf.Abs(Mathf.DeltaAngle(SaloonCharacterMotion.FollowBody(350,10,false,.1f),350))<.01f,"looking across 360 degrees does not spin the body");
            OnCommand(hero.id,new Command{kind="input",yaw=120,poseVersion=hero.poseVersion});TickServer();Broadcast();yield return new WaitForSeconds(.3f);
            Check(Mathf.Abs(Mathf.DeltaAngle(hero.bodyYaw,hero.yaw)-SaloonCharacterMotion.HeadYawLimit)<.01f,"body follows a large turn without twisting the neck beyond its limit");
            OnCommand(hero.id,new Command{kind="input",yaw=120,z=1,poseVersion=hero.poseVersion});
            yield return new WaitForSeconds(.2f);hero.move=Vector2.zero;
            Check(Mathf.Abs(Mathf.DeltaAngle(hero.bodyYaw,hero.yaw))<SaloonCharacterMotion.HeadYawLimit-10,"walking turns the body toward the movement controls");

            StartMatch();yield return new WaitForSeconds(.4f);
            float seatedYaw=hero.bodyYaw;
            var seatedChest=chest.rotation;var seatedHips=hips.rotation;
            OnCommand(hero.id,new Command{kind="input",yaw=seatedYaw+35,pitch=-30,poseVersion=hero.poseVersion});TickServer();Broadcast();yield return new WaitForSeconds(.25f);
            Check(Mathf.Abs(Mathf.DeltaAngle(hero.bodyYaw,seatedYaw))<.01f&&Quaternion.Angle(seatedChest,chest.rotation)<.1f&&Quaternion.Angle(seatedHips,hips.rotation)<.1f,"seated players can nod and shake without turning their torso or chair");
            yield return Capture("06-seated-gesture");

            VerificationFinished=VerificationPassed=false;
            var windows=FindObjectsByType<SaloonRoomWindow>();
            Check(windows.Length==2,"both room windows are present in the player");
            foreach(var pane in windows)
            {
                Check(pane.outside.sharedMaterial.shader==Resources.Load<Shader>("Shaders/SaloonWindowGlass")&&pane.outside.sharedMaterial.shader.isSupported,"window uses the included glass shader in "+SaloonStations.Name(pane.room));
                CheckWindowRendering(pane,Check);
                Check(pane.barrier.enabled&&!pane.barrier.isTrigger,"glass remains a solid shot and movement barrier");
            }
            externalCamera=true;RefreshViewpoint();
            WindowPreview(new Vector3(-5,1.70f,2.05f),new Vector3(-5,1.5f,-3.8f),68,"build-left-room");
            WindowPreview(new Vector3(5,1.70f,2.05f),new Vector3(5,1.5f,-3.8f),68,"build-right-room");
            externalCamera=false;RefreshViewpoint();ViewCamera.fieldOfView=78;
            VerificationPassed=VerificationFinished=true;
            Debug.Log("SALOON_MODEL_ROOM PASS platform="+Application.platform+" graphics="+SystemInfo.graphicsDeviceType+" previews="+directory);
        }
    }
}
#endif
