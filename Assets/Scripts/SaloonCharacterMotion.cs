using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    // Authored joint animation for the pack's Humanoid rig. Gameplay snapshots drive it;
    // no root motion moves the host-owned player, and ragdoll physics can take over.
    public sealed class SaloonCharacterMotion : MonoBehaviour
    {
        public const string ResourcePath="Art/Western/Gunman";
        public const float HeadYawLimit=65,HeadPitchLimit=60;
        // Keep small looks on the neck; turn the body only past its comfortable range
        // or while walking. The host replicates this separately from camera/aim yaw.
        public static float FollowBody(float bodyYaw,float lookYaw,bool walking,float delta)
        {
            if(walking)bodyYaw=Mathf.MoveTowardsAngle(bodyYaw,lookYaw,180*Mathf.Max(0,delta));
            float offset=Mathf.DeltaAngle(bodyYaw,lookYaw);
            return Mathf.Repeat(lookYaw-Mathf.Clamp(offset,-HeadYawLimit,HeadYawLimit),360);
        }
        readonly Dictionary<HumanBodyBones,Transform> humanoid=new Dictionary<HumanBodyBones,Transform>();
        Transform[] joints;
        Vector3[] positions;
        Quaternion[] rotations;
        Vector3 leftFoot,rightFoot;
        Vector3 mouthOffset;
        Quaternion leftFootRotation,rightFootRotation;
        float sitting,walking,step,crouching;
        Vector3 walkDirection=Vector3.forward;
        bool initialized;
        float hitAt=-10;
        Vector3 impactDirection;
        public float HitAmount { get; private set; }
        public void Hit(Vector3 direction,float age=0)
        {impactDirection=direction.sqrMagnitude>.01f?direction.normalized:-transform.forward;hitAt=Time.time-Mathf.Max(0,age);}
        public void ClearHit(){hitAt=-10;HitAmount=0;}
        public string CurrentAnimation { get; private set; }="Idle";
        public float SittingBlend=>sitting;
        public float CrouchingBlend=>crouching;
        public float WalkingBlend=>walking;
        public Transform Bone(HumanBodyBones bone)=>humanoid.TryGetValue(bone,out var joint)?joint:null;
        public Vector3 Mouth=>Bone(HumanBodyBones.Head).TransformPoint(mouthOffset);

        public void Initialize(bool seated)
        {
            if(initialized)return;
            var animator=GetComponentInChildren<Animator>(true);
            if(animator==null||animator.avatar==null||!animator.avatar.isValid||!animator.avatar.isHuman)
                throw new InvalidOperationException("Western Gunman needs its imported Humanoid avatar.");
            foreach(HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if(bone==HumanBodyBones.LastBone)continue;
                var joint=animator.GetBoneTransform(bone);if(joint!=null)humanoid[bone]=joint;
            }
            animator.applyRootMotion=false;animator.enabled=false;
            joints=GetComponentsInChildren<Transform>(true).Where(t=>t!=transform).ToArray();
            positions=joints.Select(t=>t.localPosition).ToArray();rotations=joints.Select(t=>t.localRotation).ToArray();
            leftFoot=transform.InverseTransformPoint(Bone(HumanBodyBones.LeftFoot).position);
            rightFoot=transform.InverseTransformPoint(Bone(HumanBodyBones.RightFoot).position);
            leftFootRotation=Quaternion.Inverse(transform.rotation)*Bone(HumanBodyBones.LeftFoot).rotation;
            rightFootRotation=Quaternion.Inverse(transform.rotation)*Bone(HumanBodyBones.RightFoot).rotation;
            mouthOffset=Bone(HumanBodyBones.Head).InverseTransformVector(transform.forward*.095f-transform.up*.035f);
            sitting=seated?1:0;initialized=true;
        }

        public void Pose(bool seated,Vector3 velocity,float drinking,float kick,float lookPitch,float lookYaw=0,bool crouched=false)
        {
            if(!initialized)Initialize(seated);
            for(int i=0;i<joints.Length;i++){joints[i].localPosition=positions[i];joints[i].localRotation=rotations[i];}
            float delta=Mathf.Min(Time.deltaTime,.05f);
            sitting=Mathf.MoveTowards(sitting,seated?1:0,delta/.3f);
            crouching=Mathf.MoveTowards(crouching,crouched&&!seated?1:0,delta/.15f);
            float speed=Mathf.Min(3.5f,velocity.magnitude);
            walking=Mathf.MoveTowards(walking,seated?0:Mathf.Clamp01(speed/.8f),delta*6);
            if(speed>.1f)
            {
                var direction=transform.InverseTransformDirection(velocity);direction.y=0;
                walkDirection=Vector3.Lerp(walkDirection,direction.normalized,1-Mathf.Exp(-delta*12));
            }
            step+=delta*Mathf.Lerp(.8f,1.45f,speed/3.5f)*Mathf.PI*2;
            float breath=Mathf.Sin(Time.time*1.8f)*.004f;
            float hitTime=Time.time-hitAt;
            HitAmount=hitTime<0||hitTime>.70f?0:hitTime<.055f?Mathf.SmoothStep(0,1,hitTime/.055f):Mathf.Pow(1-Mathf.Clamp01((hitTime-.055f)/.645f),2);
            var hips=Bone(HumanBodyBones.Hips);
            var hipLocal=transform.InverseTransformPoint(hips.position);
            // A real seated pose: lower the pelvis and solve bent knees, retaining scale.
            hipLocal.y=Mathf.Lerp(hipLocal.y,.57f,sitting)+breath+.012f*walking*Mathf.Cos(step*2);
            hipLocal.y-=SaloonMovement.CrouchDrop*crouching;
            hipLocal.z-=.025f*sitting+.10f*crouching;
            hips.position=transform.TransformPoint(hipLocal);
            hips.position+=impactDirection*(.025f*HitAmount)-Vector3.up*(.018f*HitAmount);
            Rotate(HumanBodyBones.Spine,transform.right,3*sitting-7*kick-3*drinking);
            float headYaw=Mathf.Clamp(lookYaw,-HeadYawLimit,HeadYawLimit)*(1-drinking);
            float headPitch=Mathf.Clamp(lookPitch,-HeadPitchLimit,HeadPitchLimit)*(1-drinking);
            var lookRight=Quaternion.AngleAxis(headYaw,transform.up)*transform.right;
            float neckShare=Bone(HumanBodyBones.Neck)!=null?.25f:0;
            Rotate(HumanBodyBones.Neck,transform.up,headYaw*neckShare);
            Rotate(HumanBodyBones.Head,transform.up,headYaw*(1-neckShare));
            Rotate(HumanBodyBones.Neck,lookRight,headPitch*neckShare);
            Rotate(HumanBodyBones.Head,lookRight,headPitch*(1-neckShare));
            Rotate(HumanBodyBones.Head,transform.right,-9*drinking);
            var hitAxis=Vector3.Cross(Vector3.up,impactDirection).normalized;
            Rotate(HumanBodyBones.Spine,hitAxis,13*HitAmount);
            Rotate(HumanBodyBones.Chest,hitAxis,10*HitAmount);
            Rotate(HumanBodyBones.Head,hitAxis,-6*HitAmount);
            AnimateLeg(true,leftFoot,leftFootRotation,step);
            AnimateLeg(false,rightFoot,rightFootRotation,step+Mathf.PI);
            AnimateArm(true,step+Mathf.PI);
            AnimateArm(false,step);
            CurrentAnimation=HitAmount>.01f?"Hit":drinking>.01f?"Drinking":kick>.001f?"Shooting":seated?"Sitting":crouching>.5f?"Crouching":walking>.05f?"Walking":"Idle";
        }

        void Rotate(HumanBodyBones bone,Vector3 axis,float degrees)
        {var joint=Bone(bone);if(joint!=null)joint.rotation=Quaternion.AngleAxis(degrees,axis)*joint.rotation;}

        void AnimateLeg(bool left,Vector3 rest,Quaternion footRotation,float cycle)
        {
            float stride=Mathf.Sin(cycle)*.23f*walking*(1-sitting)*(1-.45f*crouching);
            var goal=rest+walkDirection*stride+Vector3.up*(Mathf.Max(0,Mathf.Cos(cycle))*.10f*walking*(1-sitting));
            goal=Vector3.Lerp(goal,new Vector3(left?-.13f:.13f,rest.y,.37f),sitting);
            var thigh=Bone(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);
            var knee=Bone(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg);
            var ankle=Bone(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
            SaloonWeaponPose.Reach(thigh,knee,ankle,transform.TransformPoint(goal),transform.forward);
            ankle.rotation=transform.rotation*footRotation;
        }

        void AnimateArm(bool left,float cycle)
        {
            float side=left?-1:1;
            var goal=new Vector3(side*.28f,Mathf.Lerp(.91f,.75f,sitting),Mathf.Lerp(.015f,.25f,sitting));
            goal.y-=SaloonMovement.CrouchDrop*crouching;
            goal.z+=Mathf.Sin(cycle)*.14f*walking*(1-sitting);
            goal.y+=.025f*walking*Mathf.Cos(cycle);
            var upper=Bone(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
            var lower=Bone(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
            var hand=Bone(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
            SaloonWeaponPose.Reach(upper,lower,hand,transform.TransformPoint(goal),transform.right*side*.4f-transform.forward*.2f);
        }
    }
}
