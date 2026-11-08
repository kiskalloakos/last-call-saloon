using UnityEngine;

namespace Saloon
{
    public static class SaloonWeaponPose
    {
        public static readonly Vector3 Grip=new Vector3(0,.035f,-.135f);
        public static readonly Vector3 Trigger=new Vector3(0,.035f,.045f);
        public const float FlipStart=.16f,FlipSeconds=.95f,Duration=1.16f,DrawSeconds=.35f;
        static float Ease(float t){t=Mathf.Clamp01(t);return t*t*t*(t*(t*6-15)+10);}
        public static float Kick(float elapsed)
        {
            if(elapsed<0)return 0;
            return elapsed<.035f?Ease(elapsed/.035f):1-Ease((elapsed-.035f)/.21f);
        }
        // A continuous backward roll with zero velocity and acceleration at either end.
        public static float Flip(float elapsed)=>-360*Ease((elapsed-FlipStart)/FlipSeconds);
        static float Flourish(float elapsed)
        {
            float progress=Ease((elapsed-FlipStart)/FlipSeconds);
            float arc=Mathf.Sin(progress*Mathf.PI);return arc*arc;
        }
        public static Vector3 Offset(float elapsed)=>new Vector3(.012f*Flourish(elapsed),-.022f*Flourish(elapsed),-.045f*Kick(elapsed));
        public static Quaternion Rotation(float elapsed)=>Quaternion.Euler(-12*Kick(elapsed),-2*Flourish(elapsed),-1.5f*Kick(elapsed)+6*Flourish(elapsed));
        public static Vector3 DrawOffset(float amount)=>new Vector3(0,-.34f,-.025f)*(1-Ease(amount));
        public static Quaternion DrawRotation(float amount)=>Quaternion.Euler(14*(1-Ease(amount)),0,0);

        public static void Reach(Transform shoulder,Transform elbow,Transform wrist,Vector3 target,Vector3 bend)
        {
            float upper=Vector3.Distance(shoulder.position,elbow.position),lower=Vector3.Distance(elbow.position,wrist.position);
            Vector3 delta=target-shoulder.position;float distance=Mathf.Clamp(delta.magnitude,.01f,upper+lower-.005f);
            Vector3 direction=delta.normalized,plane=Vector3.ProjectOnPlane(bend,direction).normalized;
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            Vector3 joint=shoulder.position+direction*along+plane*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            shoulder.rotation=Quaternion.FromToRotation(elbow.position-shoulder.position,joint-shoulder.position)*shoulder.rotation;
            elbow.rotation=Quaternion.FromToRotation(wrist.position-elbow.position,target-elbow.position)*elbow.rotation;
            // Seated characters have a compressed Y scale; refine in world space afterward.
            for(int i=0;i<6;i++)
            {
                elbow.rotation=Quaternion.FromToRotation(wrist.position-elbow.position,target-elbow.position)*elbow.rotation;
                shoulder.rotation=Quaternion.FromToRotation(wrist.position-shoulder.position,target-shoulder.position)*shoulder.rotation;
            }
        }
    }
}
