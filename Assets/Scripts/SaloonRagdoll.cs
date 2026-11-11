using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    // Cosmetic physics only. The host still owns damage, elimination and observer timing.
    public sealed class SaloonRagdoll : MonoBehaviour
    {
        public const float ObserverDelay=3.2f;
        public const int Layer=9;
        readonly List<Rigidbody> bodies=new List<Rigidbody>();
        readonly List<Collider> colliders=new List<Collider>();
        readonly List<CharacterJoint> joints=new List<CharacterJoint>();
        Transform[] bones;
        Vector3[] positions;
        Quaternion[] rotations;
        int[] layers;
        Animation[] animations;
        bool[] animationEnabled;
        Rigidbody hips;
        float started;
        bool frozen;
        SaloonCharacterMotion character;
        public Vector3 Center=>hips!=null?hips.position:transform.position+Vector3.up*.4f;
        public int BodyCount=>bodies.Count;
        public float Speed=>hips!=null?hips.linearVelocity.magnitude:0;
        public bool Settled=>frozen;

        public void Launch(Vector3 direction)
        {
            started=Time.time;bones=GetComponentsInChildren<Transform>();character=GetComponent<SaloonCharacterMotion>();
            positions=bones.Select(t=>t.localPosition).ToArray();rotations=bones.Select(t=>t.localRotation).ToArray();layers=bones.Select(t=>t.gameObject.layer).ToArray();
            animations=GetComponentsInChildren<Animation>();animationEnabled=animations.Select(a=>a.enabled).ToArray();
            foreach(var animation in animations){animation.Stop();animation.enabled=false;}
            foreach(var animator in GetComponentsInChildren<Animator>())animator.enabled=false;
            Physics.IgnoreLayerCollision(Layer,Layer,true);
            Add("Hips","Spine",.15f,7);
            Add("Spine","Neck",.16f,6);
            Add("Head",null,.22f,3);
            foreach(string side in new[]{"Left","Right"})
            {
                Add(side+"Arm",side+"ForeArm",.065f,1.2f);
                Add(side+"ForeArm",side+"Hand",.055f,.8f);
                Add(side+"UpLeg",side+"Leg",.09f,2.8f);
                Add(side+"Leg",side+"Foot",.07f,1.8f);
            }
            foreach(var body in bodies)
            {
                var parent=body.transform.parent;
                while(parent!=null&&parent.GetComponent<Rigidbody>()==null)parent=parent.parent;
                if(parent==null)continue;
                var joint=body.gameObject.AddComponent<CharacterJoint>();joints.Add(joint);
                joint.connectedBody=parent.GetComponent<Rigidbody>();joint.enablePreprocessing=false;
                joint.enableProjection=true;joint.projectionDistance=.08f;joint.projectionAngle=15;
                joint.lowTwistLimit=new SoftJointLimit{limit=-35};joint.highTwistLimit=new SoftJointLimit{limit=35};
                joint.swing1Limit=new SoftJointLimit{limit=55};joint.swing2Limit=new SoftJointLimit{limit=40};
            }
            // Leave the chair cleanly instead of letting overlapping seat colliders explode the rig.
            var seats=FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None).Where(p=>p.kind==InteractionKind.Seat).SelectMany(p=>p.GetComponents<Collider>()).ToArray();
            var players=FindObjectsByType<SaloonBody>(FindObjectsSortMode.None).SelectMany(p=>p.GetComponents<Collider>()).ToArray();
            foreach(var own in colliders)
            {
                foreach(var seat in seats)Physics.IgnoreCollision(own,seat,true);
                foreach(var player in players)Physics.IgnoreCollision(own,player,true);
            }
            direction=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
            if(direction.sqrMagnitude<.1f)direction=-transform.forward;
            foreach(var body in bodies)
            {
                body.isKinematic=false;
                body.linearVelocity=direction*3.6f+Vector3.up*2.1f;
                body.angularVelocity=Vector3.Cross(Vector3.up,direction)*4+direction*.6f;
            }
        }

        void Add(string name,string end,float radius,float mass)
        {
            var bone=FindBone(name);var child=end==null?null:FindBone(end);
            if(bone==null)return;
            bone.gameObject.layer=Layer;
            var body=bone.gameObject.AddComponent<Rigidbody>();body.mass=mass;body.isKinematic=true;
            body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            body.linearDamping=.55f;body.angularDamping=1.3f;body.solverIterations=12;body.solverVelocityIterations=8;body.maxAngularVelocity=10;
            bodies.Add(body);if(name=="Hips")hips=body;
            var scale=bone.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            if(child==null)
            {
                var sphere=bone.gameObject.AddComponent<SphereCollider>();sphere.radius=radius/Mathf.Max(.0001f,Mathf.Max(scale.x,Mathf.Max(scale.y,scale.z)));
                sphere.center=bone.InverseTransformVector(Vector3.up*.1f);colliders.Add(sphere);return;
            }
            var delta=bone.InverseTransformPoint(child.position);var length=new Vector3(Mathf.Abs(delta.x),Mathf.Abs(delta.y),Mathf.Abs(delta.z));
            int axis=length.x>length.y&&length.x>length.z?0:length.y>length.z?1:2;
            var capsule=bone.gameObject.AddComponent<CapsuleCollider>();capsule.direction=axis;capsule.center=delta*.5f;
            float perpendicular=axis==0?Mathf.Max(scale.y,scale.z):axis==1?Mathf.Max(scale.x,scale.z):Mathf.Max(scale.x,scale.y);
            capsule.radius=radius/Mathf.Max(.0001f,perpendicular);
            capsule.height=Mathf.Max(capsule.radius*2,length[axis]+capsule.radius*2);colliders.Add(capsule);
        }

        Transform FindBone(string name)
        {
            if(character==null)return bones.FirstOrDefault(t=>t.name==name);
            string human=name.Replace("ForeArm","LowerArm").Replace("UpLeg","UpperLeg");
            if(human=="LeftArm"||human=="RightArm")human=human.Replace("Arm","UpperArm");
            if(human=="LeftLeg"||human=="RightLeg")human=human.Replace("Leg","LowerLeg");
            return System.Enum.TryParse(human,out HumanBodyBones id)?character.Bone(id):null;
        }

        void FixedUpdate()
        {
            // Keep the fallen pose for the rest of the match, independently of spectating.
            if(!frozen&&Time.time-started>2.2f&&Center.y<.55f&&Speed<.8f)Freeze();
        }
        void Freeze()
        {
            foreach(var body in bodies){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;}
            // A resting body stays visible without blocking seats, movement or interactions.
            foreach(var collider in colliders)collider.enabled=false;
            frozen=true;
        }
        public void Restore()
        {
            if(!frozen)Freeze();foreach(var collider in colliders){collider.enabled=false;Destroy(collider);}
            foreach(var joint in joints)Destroy(joint);foreach(var body in bodies)Destroy(body);
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];bones[i].gameObject.layer=layers[i];}
            for(int i=0;i<animations.Length;i++){animations[i].enabled=animationEnabled[i];if(animationEnabled[i]&&animations[i].GetClip("idle")!=null)animations[i].Play("idle");}
            Destroy(this);
        }
    }
}
