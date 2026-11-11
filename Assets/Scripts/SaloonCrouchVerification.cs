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
        public IEnumerator VerifyCrouchAndWeapon()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string message)
            {
                if(!ok){VerificationFinished=true;Debug.LogError("SALOON_CROUCH FAIL "+message);throw new InvalidOperationException(message);}
                Debug.Log("SALOON_CROUCH_CHECK "+message);
            }
            yield return null;
            Finish("CROUCH PREVIEW");ReopenLobby();MenuOpen=false;
            var hero=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            void Place(Participant p,Vector3 position)
            {
                p.controller.enabled=false;p.seat=-1;p.crouched=p.crouchHeld=false;
                p.body.transform.position=position;p.yaw=p.bodyYaw=p.pitch=0;p.move=Vector2.zero;p.vertical=0;p.poseVersion++;
                UpdateCrouch(p);UpdatePlayerColliders(p);p.controller.enabled=true;
            }
            void InputPose(bool crouch,float move=0,int version=-1)
            {
                OnCommand(hero.id,new Command{kind="input",crouch=crouch,x=move,yaw=0,pitch=0,poseVersion=version<0?hero.poseVersion:version});
                UpdateCrouch(hero);UpdatePlayerColliders(hero);
            }
            Place(hero,new Vector3(0,.08f,-5.5f));Place(peer,new Vector3(2,.08f,-5.5f));
            var standingEye=Eye(hero);var feet=hero.body.transform.position;
            float settle=Time.time+.4f;
            while(Time.time<settle){InputPose(true);Broadcast();yield return null;}
            InputPose(true);
            Check(hero.crouched&&Mathf.Abs(hero.controller.height-SaloonMovement.CrouchingHeight)<.001f,"Control input lowers the host capsule");
            Check(Mathf.Abs(hero.controller.center.y-hero.controller.height*.5f)<.001f&&Mathf.Abs(hero.body.transform.position.y-feet.y)<.05f,"resizing the capsule keeps feet grounded");
            Check(Mathf.Abs(standingEye.y-Eye(hero).y-SaloonMovement.CrouchDrop)<.001f,"aim origin lowers with the crouched eyes");
            Check(Vector3.Distance(ViewCamera.transform.position,Eye(hero))<.06f,"first person camera follows crouched eyes");
            var snapshot=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(hero.id),out _));
            Check(snapshot.players.Single(p=>p.id==hero.id.ToString()).crouched,"crouch survives the network snapshot wire");
            var motion=avatars[LocalId].GetComponentInChildren<SaloonCharacterMotion>(true);
            Check(motion.CrouchingBlend>.95f&&motion.CurrentAnimation=="Crouching","remote character rig bends into the crouch pose");
            Check(Vector3.Distance(motion.Bone(HumanBodyBones.Head).position,hero.head.bounds.center)<.22f,"crouched head hitbox follows the visible head");
            InputPose(false);Check(!hero.crouched,"releasing Control stands in clear space");
            var oldHead=Eye(peer);peer.crouchHeld=true;peer.lastInput=Time.time;UpdateCrouch(peer);UpdatePlayerColliders(peer);
            void AimAt(Vector3 point)
            {
                var direction=point-Eye(hero);hero.yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
                hero.pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
            }
            AimAt(oldHead);var missed=Target(hero,5);
            Check(missed?.collider!=peer.head&&missed?.collider!=peer.hitbox,"crouching dodges a ray at the old standing head position");
            AimAt(Eye(peer));Check(Target(hero,5)?.collider==peer.head,"crouched heads remain hittable at their actual height");
            peer.crouchHeld=false;UpdateCrouch(peer);UpdatePlayerColliders(peer);hero.yaw=hero.pitch=0;
            IEnumerator Walk(bool crouched)
            {
                float until=Time.time+.25f;
                while(Time.time<until){InputPose(crouched,1);yield return null;}
                InputPose(crouched);
            }
            var start=hero.body.transform.position;yield return Walk(false);float uprightDistance=Vector3.Distance(start,hero.body.transform.position);
            Place(hero,start);yield return Walk(true);float crouchedDistance=Vector3.Distance(start,hero.body.transform.position);
            Check(crouchedDistance>uprightDistance*.25f&&crouchedDistance<uprightDistance*.65f,"crouch walking is slower than standing movement");
            InputPose(true);hero.lastInput=Time.time-1;UpdateCrouch(hero);Check(!hero.crouched,"stale input cannot leave crouch held");
            InputPose(false);InputPose(true,0,hero.poseVersion+1);Check(!hero.crouchHeld,"stale pose commands cannot change crouch");
            InputPose(true);
            var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.name="Crouch clearance test";
            ceiling.transform.position=hero.body.transform.position+Vector3.up*1.48f;ceiling.transform.localScale=new Vector3(1,.2f,1);
            InputPose(false);Check(hero.crouched,"release under a low ceiling keeps the capsule safely crouched");
            ceiling.SetActive(false);InputPose(false);Check(!hero.crouched,"standing resumes when overhead clearance opens");Destroy(ceiling);
            hero.seat=100;InputPose(true);Check(!hero.crouched,"seated players cannot crouch");hero.seat=-1;InputPose(false);

            Place(hero,new Vector3(0,.08f,-5.5f));hero.drawn=true;Broadcast();yield return new WaitForSeconds(.4f);
            var pass=ViewCamera.GetComponent<SaloonViewmodel>();
            Check(pass!=null&&pass.Overlay!=null&&(ViewCamera.cullingMask&(1<<SaloonViewmodel.Layer))==0,"held gun has an isolated render pass");
            var gun=ViewCamera.transform.Find("Low-poly revolver").gameObject;
            Check(gun.activeSelf&&gun.GetComponentsInChildren<Renderer>().All(r=>r.gameObject.layer==SaloonViewmodel.Layer),"only the held revolver uses the weapon layer");
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="Weapon clipping test wall";
            blocker.transform.position=ViewCamera.transform.position+ViewCamera.transform.forward*.22f;
            blocker.transform.rotation=ViewCamera.transform.rotation;blocker.transform.localScale=new Vector3(3,3,.04f);
            Check(Target(hero,5)?.collider==blocker.GetComponent<Collider>(),"the real shot ray still hits a close wall");
            Color32[] Render(bool visible,string name)
            {
                gun.SetActive(visible);
                var previous=RenderTexture.active;var oldTarget=ViewCamera.targetTexture;
                var target=new RenderTexture(640,450,24);var texture=new Texture2D(640,450,TextureFormat.RGB24,false);
                try
                {
                    ViewCamera.targetTexture=target;ViewCamera.Render();pass.RenderOverlay();RenderTexture.active=target;
                    texture.ReadPixels(new Rect(0,0,640,450),0,0);texture.Apply();
                    if(name!=null)File.WriteAllBytes(Path.Combine(Path.GetTempPath(),"SaloonCrouch-"+name+".png"),texture.EncodeToPNG());
                    return texture.GetPixels32();
                }
                finally{ViewCamera.targetTexture=oldTarget;RenderTexture.active=previous;Destroy(texture);target.Release();Destroy(target);}
            }
            int Difference(Color32[] a,Color32[] b)=>a.Zip(b,(x,y)=>Mathf.Abs(x.r-y.r)+Mathf.Abs(x.g-y.g)+Mathf.Abs(x.b-y.b)>30?1:0).Sum();
            int wallPixels=Difference(Render(false,null),Render(true,"wall"));
            Check(wallPixels>200,"revolver remains rendered against a wall in front of its muzzle ("+wallPixels+" pixels)");
            blocker.GetComponent<Renderer>().sharedMaterial=FindObjectsByType<SaloonRoomWindow>().First().outside.sharedMaterial;
            int glassPixels=Difference(Render(false,null),Render(true,"glass"));
            Check(glassPixels>200,"window glass cannot clip the held revolver ("+glassPixels+" pixels)");
            Check(pass.Overlay.clearFlags==CameraClearFlags.Depth&&pass.Overlay.cullingMask==(1<<SaloonViewmodel.Layer),"weapon pass clears depth and draws only its own layer");
            Check(pass.Overlay.depth>ViewCamera.depth&&pass.Overlay.enabled&&pass.Overlay.targetTexture==null,"weapon camera draws after the room and releases temporary capture targets");
            blocker.SetActive(false);Destroy(blocker);hero.drawn=false;InputPose(false);Broadcast();
            StartMatch();InputPose(true);Check(!hero.crouched,"match regroup clears crouching for seated discussion");
            VerificationPassed=VerificationFinished=true;Debug.Log("SALOON_CROUCH PASS");
        }
    }
}
#endif
