using UnityEngine;
using System.Linq;

namespace Saloon
{
    // Pose the arm first, then attach the weapon. Never force or stretch the wrist
    // to reach an independently positioned revolver.
    public sealed class SaloonGunGrip
    {
        public const string Shape="SaloonGunHold";
        public static readonly Vector3 PalmInWeapon=new Vector3(0,.04f,0);
        const float LiftEnd=.5f,HandClearance=.215f,ElbowClearance=.15f;
        readonly Transform body,chest,upper,lower,hand;
        readonly Quaternion wristRest,handToGun,upperFrame;
        readonly Vector3 mount,hingeInLower;
        readonly SkinnedMeshRenderer[] skins;
        readonly int[] shapes;
        public Quaternion WristRest=>wristRest;
        public Vector3 Mount=>hand.TransformPoint(mount);

        public SaloonGunGrip(SaloonCharacterMotion motion)
        {
            body=motion.transform;chest=motion.Bone(HumanBodyBones.Chest);
            upper=motion.Bone(HumanBodyBones.RightUpperArm);lower=motion.Bone(HumanBodyBones.RightLowerArm);hand=motion.Bone(HumanBodyBones.RightHand);
            wristRest=hand.localRotation;
            var middle=motion.Bone(HumanBodyBones.RightMiddleProximal);var thumb=motion.Bone(HumanBodyBones.RightThumbProximal);
            var forward=(middle.position-hand.position).normalized;
            var up=Vector3.Cross(forward,thumb.position-hand.position).normalized;
            var gunFrame=Quaternion.LookRotation(forward,up);
            handToGun=Quaternion.Inverse(hand.rotation)*gunFrame;
            mount=hand.InverseTransformPoint((middle.position+thumb.position)*.5f);
            var hinge=-(gunFrame*Vector3.right);
            hingeInLower=Quaternion.Inverse(lower.rotation)*hinge;
            upperFrame=Quaternion.Inverse(upper.rotation)*Quaternion.LookRotation(lower.position-upper.position,hinge);
            skins=motion.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s=>s.sharedMesh.GetBlendShapeIndex(Shape)>=0).ToArray();
            shapes=skins.Select(s=>s.sharedMesh.GetBlendShapeIndex(Shape)).ToArray();
        }

        public void Pose(Transform weapon,Quaternion aim,float amount)
        {
            float blend=Mathf.SmoothStep(0,1,amount);
            var outward=(body.right+body.forward).normalized;
            float Depth(Vector3 point)=>Vector3.Dot(point-chest.position,outward);
            float HandDepth()=>Mathf.Min(Depth(hand.position),Depth(Mount));
            float lift=Mathf.SmoothStep(0,1,amount/LiftEnd);
            // Start at the walking or idle pose before guiding the arm past the coat.
            float handDepth=Mathf.Lerp(Mathf.Min(HandDepth(),HandClearance),HandClearance,lift);
            float elbowDepth=Mathf.Lerp(Mathf.Min(Depth(lower.position),ElbowClearance),ElbowClearance,lift);
            // Lift from the right hip before turning toward the aim direction.
            float turn=Mathf.SmoothStep(0,1,(amount-LiftEnd)/(1-LiftEnd));
            var drawAim=Quaternion.Slerp(body.rotation,aim,turn);
            var wantedHand=drawAim*Quaternion.Inverse(handToGun);
            var wantedLower=wantedHand*Quaternion.Inverse(wristRest);
            var forearmDirection=(wantedLower*hand.localPosition).normalized;
            var normal=(wantedLower*hingeInLower).normalized;
            var upperDirection=Quaternion.AngleAxis(-62,normal)*forearmDirection;
            var wantedUpper=Quaternion.LookRotation(upperDirection,normal)*Quaternion.Inverse(upperFrame);
            var restingForearm=lower.rotation;
            upper.rotation=Quaternion.Slerp(upper.rotation,wantedUpper,lift);
            lower.rotation=Quaternion.Slerp(restingForearm,wantedLower,lift);
            hand.localRotation=wristRest;
            MoveElbowOutward(outward,Mathf.Max(handDepth-HandDepth(),elbowDepth-Depth(lower.position)));
            for(int i=0;i<skins.Length;i++)skins[i].SetBlendShapeWeight(shapes[i],blend*100);
            var rotation=hand.rotation*handToGun;
            weapon.SetPositionAndRotation(Mount-rotation*PalmInWeapon,rotation);
        }

        void MoveElbowOutward(Vector3 outward,float clearance)
        {
            if(clearance<=0)return;

            // Pivot the upper arm to move the elbow and hand together. Preserve the
            // forearm's world rotation, so the wrist and revolver keep their aim.
            var arm=lower.position-upper.position;
            var direction=arm.normalized;
            float amount=Mathf.Min(1,Vector3.Dot(direction,outward)+clearance/arm.magnitude);
            var tangent=Vector3.ProjectOnPlane(direction,outward).normalized;
            if(tangent==Vector3.zero)tangent=-body.up;
            var corrected=outward*amount+tangent*Mathf.Sqrt(1-amount*amount);
            var forearm=lower.rotation;
            upper.rotation=Quaternion.FromToRotation(direction,corrected)*upper.rotation;
            lower.rotation=forearm;
        }

        public void Release(){for(int i=0;i<skins.Length;i++)skins[i].SetBlendShapeWeight(shapes[i],0);}
    }
}
