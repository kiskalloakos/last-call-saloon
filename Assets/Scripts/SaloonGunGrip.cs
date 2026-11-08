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
        readonly Transform upper,lower,hand;
        readonly Quaternion wristRest,handToGun,upperFrame;
        readonly Vector3 mount,hingeInLower;
        readonly SkinnedMeshRenderer[] skins;
        readonly int[] shapes;
        public Quaternion WristRest=>wristRest;
        public Vector3 Mount=>hand.TransformPoint(mount);

        public SaloonGunGrip(SaloonCharacterMotion motion)
        {
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
            var wantedHand=aim*Quaternion.Inverse(handToGun);
            var wantedLower=wantedHand*Quaternion.Inverse(wristRest);
            var forearmDirection=(wantedLower*hand.localPosition).normalized;
            var normal=(wantedLower*hingeInLower).normalized;
            var upperDirection=Quaternion.AngleAxis(-62,normal)*forearmDirection;
            var wantedUpper=Quaternion.LookRotation(upperDirection,normal)*Quaternion.Inverse(upperFrame);
            upper.rotation=Quaternion.Slerp(upper.rotation,wantedUpper,blend);
            lower.rotation=Quaternion.Slerp(lower.rotation,wantedLower,blend);
            hand.localRotation=wristRest;
            for(int i=0;i<skins.Length;i++)skins[i].SetBlendShapeWeight(shapes[i],blend*100);
            var rotation=hand.rotation*handToGun;
            weapon.SetPositionAndRotation(Mount-rotation*PalmInWeapon,rotation);
        }
        public void Release(){for(int i=0;i<skins.Length;i++)skins[i].SetBlendShapeWeight(shapes[i],0);}
    }
}
