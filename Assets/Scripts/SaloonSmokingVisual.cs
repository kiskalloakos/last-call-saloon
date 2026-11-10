using UnityEngine;

namespace Saloon
{
    public sealed class SaloonSmokingVisual : MonoBehaviour
    {
        SaloonGame game;SaloonCharacterMotion motion;
        SaloonCigarette third,first;
        GameObject firstHand;
        ParticleSystem thirdExhale,firstExhale;
        Material skin;
        Quaternion basis;Vector3 grip;
        public float Lift { get; private set; }
        public bool FirstPersonVisible=>first!=null&&first.gameObject.activeInHierarchy;
        public bool ThirdPersonVisible=>third!=null&&third.gameObject.activeInHierarchy;
        public float EmberHeat=>first!=null&&FirstPersonVisible?first.Heat:third!=null?third.Heat:0;
        public Vector3 CigarettePosition=>ThirdPersonVisible?third.transform.position:first.transform.position;
        public void Initialize(SaloonGame owner,SaloonCharacterMotion character)
        {
            game=owner;motion=character;
            var hand=motion.Bone(HumanBodyBones.RightHand);var thumb=motion.Bone(HumanBodyBones.RightThumbProximal);var index=motion.Bone(HumanBodyBones.RightIndexProximal)??motion.Bone(HumanBodyBones.RightThumbProximal);var middle=motion.Bone(HumanBodyBones.RightMiddleProximal);
            var forward=(middle.position-hand.position).normalized;var up=Vector3.Cross(forward,thumb.position-hand.position).normalized;
            basis=Quaternion.Inverse(hand.rotation)*Quaternion.LookRotation(forward,up);
            grip=hand.InverseTransformPoint((index.position+middle.position)*.5f);
        }
        void Build()
        {
            third=SaloonCigarette.Create(transform,"Third-person burning cigarette");
            first=SaloonCigarette.Create(game.ViewCamera.transform,"First-person burning cigarette");
            firstHand=new GameObject("First-person smoking hand");firstHand.transform.SetParent(first.transform,false);firstHand.transform.localScale=Vector3.one*.8f;
            skin=new Material(Shader.Find("Standard")){name="Low-poly smoking hand",color=new Color(.62f,.42f,.28f)};skin.SetFloat("_Glossiness",.08f);
            Finger("Palm",new Vector3(.01f,-.045f,-.01f),new Vector3(.067f,.068f,.026f),new Vector3(15,0,-12));
            Finger("Wrist",new Vector3(.035f,-.11f,-.018f),new Vector3(.057f,.085f,.04f),new Vector3(10,0,-20));
            Finger("Index · cigarette pinch",new Vector3(-.003f,-.009f,.024f),new Vector3(.014f,.016f,.047f),new Vector3(0,65,5));
            Finger("Middle · cigarette pinch",new Vector3(.008f,-.024f,.024f),new Vector3(.014f,.016f,.048f),new Vector3(0,65,5));
            Finger("Ring finger",new Vector3(.025f,-.043f,.005f),new Vector3(.015f,.035f,.02f),new Vector3(40,0,-10));
            Finger("Little finger",new Vector3(.043f,-.049f,.002f),new Vector3(.014f,.029f,.019f),new Vector3(45,0,-10));
            Finger("Thumb",new Vector3(-.023f,-.038f,.004f),new Vector3(.021f,.04f,.019f),new Vector3(25,15,35));
            thirdExhale=SaloonCigarette.MakeSmoke(transform,"Third-person exhaled smoke");
            firstExhale=SaloonCigarette.MakeSmoke(game.ViewCamera.transform,"First-person exhaled smoke");
        }
        void Finger(string name,Vector3 at,Vector3 size,Vector3 rotation)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(firstHand.transform,false);go.transform.localPosition=at;go.transform.localScale=size;go.transform.localRotation=Quaternion.Euler(rotation);
            go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=skin;
        }
        public void Pose(PlayerView state,bool local,float age)
        {
            bool outside=state.alive&&state.room==3&&SaloonPrivacy.Separated(game.View.phase);
            if(outside&&third==null)Build();
            if(third==null)return;
            first.gameObject.SetActive(outside&&local);third.gameObject.SetActive(outside&&!local);
            firstExhale.gameObject.SetActive(outside&&local);thirdExhale.gameObject.SetActive(outside&&!local);
            if(!outside){Lift=0;return;}
            foreach(var mesh in motion.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {int shape=mesh.sharedMesh.GetBlendShapeIndex(SaloonGunGrip.Shape);if(shape>=0)mesh.SetBlendShapeWeight(shape,65);}
            Lift=SaloonSmoking.Lift(age);
            var rest=transform.TransformPoint(new Vector3(.30f,state.seated?.75f:1.0f,.28f));
            var mouth=motion.Mouth+transform.forward*.018f+transform.right*.015f;
            third.transform.position=Vector3.Lerp(rest,mouth,Lift);
            third.transform.rotation=transform.rotation*Quaternion.Euler(Mathf.Lerp(-25,8,Lift),-22,0);
            var hand=motion.Bone(HumanBodyBones.RightHand);var upper=motion.Bone(HumanBodyBones.RightUpperArm);var elbow=motion.Bone(HumanBodyBones.RightLowerArm);
            var rotation=Quaternion.LookRotation(third.transform.right,third.transform.up)*Quaternion.Inverse(basis);hand.rotation=rotation;
            var pinch=third.transform.TransformPoint(new Vector3(0,-.007f,.024f));
            SaloonWeaponPose.Reach(upper,elbow,hand,pinch-hand.TransformVector(grip),-transform.up*.85f+transform.right*.35f);hand.rotation=rotation;
            foreach(var finger in new[]{HumanBodyBones.RightIndexIntermediate,HumanBodyBones.RightMiddleIntermediate,HumanBodyBones.RightRingIntermediate,HumanBodyBones.RightLittleIntermediate})
            {var joint=motion.Bone(finger);if(joint!=null)joint.localRotation*=Quaternion.Euler(0,0,35);}
            first.transform.localPosition=Vector3.Lerp(new Vector3(.24f,-.22f,.42f),new Vector3(.07f,-.09f,.21f),Lift);
            first.transform.localRotation=Quaternion.Euler(Mathf.Lerp(-20,-8,Lift),Mathf.Lerp(-40,-20,Lift),12);
            third.Burn(state.puffs,age);first.Burn(state.puffs,age);
            thirdExhale.transform.position=motion.Mouth+transform.forward*.05f;thirdExhale.transform.rotation=Quaternion.LookRotation(transform.forward+Vector3.up*.25f);
            firstExhale.transform.localPosition=new Vector3(.01f,-.08f,.12f);firstExhale.transform.localRotation=Quaternion.Euler(-10,0,0);
            foreach(var smoke in new[]{thirdExhale,firstExhale})
            {if(smoke.gameObject.activeInHierarchy&&!smoke.isPlaying)smoke.Play();var emission=smoke.emission;emission.rateOverTime=SaloonSmoking.Exhale(age)*20;var main=smoke.main;main.startSpeed=new ParticleSystem.MinMaxCurve(.18f,.42f);main.startSize=new ParticleSystem.MinMaxCurve(.06f,.10f);main.startColor=new Color(.8f,.82f,.8f,1);
                var size=smoke.sizeOverLifetime;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.8f,1,3.5f));}
        }
        void OnDestroy()
        {
            if(first!=null)Destroy(first.gameObject);if(firstExhale!=null)Destroy(firstExhale.gameObject);if(skin!=null)Destroy(skin);
        }
    }
}
