using UnityEngine;
using System.Linq;
using System.Text;

namespace Saloon
{
    public class SaloonAvatar : MonoBehaviour
    {
        SaloonGame game;
        PlayerView state;
        GameObject model,gun,firstPersonGun,mug,firstPersonMug;
        TextMesh label,healthDots;
        AudioSource audioSource,drinkAudio;
        bool wasDrinking;int lastGift;GameObject handoffMug;
        int lastShot;
        bool local;
        Vector3 target;
        float snapshotAt,shotAt=-10,drawAmount;
        Light flash,thirdFlash;
        float muzzleUntil;
        SaloonCharacterMotion motion;
        SaloonSpeechMouth speechMouth;
        Vector3 walkingVelocity;
        Transform rightArm,rightForearm,rightHand,gunSpin,firstGunSpin;
        Quaternion handBasis;Vector3 handGrip;
        SaloonGunGrip gunGrip;
        SaloonHitReaction hitReaction;
        int lastHit;
        public bool DamageFlashVisible=>hitReaction!=null&&hitReaction.FlashVisible;
        public float LastVisualHitAt=>hitReaction?.LastHitAt??-10;
        int shownHealth=-1,shownMaxHealth=-1;
        SaloonRagdoll ragdoll;
        public SaloonRagdoll DeathBody=>ragdoll;
        public bool CorpseVisible=>state!=null&&!state.alive&&!state.observer&&model.activeSelf;

        public void Initialize(SaloonGame owner,PlayerView initial,int colorIndex)
        {
            game=owner;state=initial;drawAmount=initial.drawn?1:0;target=new Vector3(initial.x,initial.y,initial.z);transform.position=target;
            var prefab=Resources.Load<GameObject>(SaloonCharacterMotion.ResourcePath);
            if(prefab==null)throw new System.InvalidOperationException("Prepare the Western Gunman character from the Saloon menu.");
            model=Instantiate(prefab,transform);model.name="Western Gunman";
            motion=model.GetComponent<SaloonCharacterMotion>();motion.Initialize(initial.seated);
            speechMouth=model.GetComponent<SaloonSpeechMouth>();speechMouth.PhaseOffset=(ulong.Parse(initial.id)%17)*.71f;speechMouth.Initialize();
            rightArm=motion.Bone(HumanBodyBones.RightUpperArm);rightForearm=motion.Bone(HumanBodyBones.RightLowerArm);rightHand=motion.Bone(HumanBodyBones.RightHand);
            var middle=motion.Bone(HumanBodyBones.RightMiddleProximal);
            var thumb=motion.Bone(HumanBodyBones.RightThumbProximal);
            handBasis=Quaternion.Inverse(rightHand.rotation)*Quaternion.LookRotation(middle.position-rightHand.position,rightHand.forward);
            handGrip=rightHand.InverseTransformPoint((middle.position+thumb.position)*.5f);
            gunGrip=new SaloonGunGrip(motion);hitReaction=new SaloonHitReaction(model);lastHit=initial.hits;
            gun=MakeGun(transform);gunSpin=gun.transform.Find("Spin pivot");
            mug=SaloonArt.Model("Mug",null,new Vector3(.14f,.17f,.13f));mug.transform.SetParent(transform,false);mug.transform.localPosition=new Vector3(.28f,1.02f,.33f);mug.SetActive(false);
            var nameObject=new GameObject("Name");nameObject.transform.SetParent(transform);nameObject.transform.localPosition=new Vector3(0,2.05f,0);
            label=nameObject.AddComponent<TextMesh>();label.characterSize=.015f;label.fontSize=48;label.anchor=TextAnchor.MiddleCenter;
            var healthObject=new GameObject("Health dots");healthObject.transform.SetParent(transform,false);healthObject.transform.localPosition=new Vector3(0,1.85f,0);
            healthDots=healthObject.AddComponent<TextMesh>();healthDots.characterSize=.014f;healthDots.fontSize=48;healthDots.anchor=TextAnchor.MiddleCenter;healthDots.richText=true;
            if(!game.Hosting&&initial.id!=game.LocalId)
            {
                var collider=gameObject.AddComponent<CapsuleCollider>();collider.center=Vector3.up*.9f;collider.height=1.8f;collider.radius=.3f;
                gameObject.AddComponent<SaloonBody>().id=ulong.Parse(initial.id);
            }
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=1;audioSource.minDistance=2;audioSource.maxDistance=45;
            drinkAudio=gameObject.AddComponent<AudioSource>();drinkAudio.clip=SaloonBeerAudio.Drinking;drinkAudio.minDistance=.8f;drinkAudio.maxDistance=7;drinkAudio.rolloffMode=AudioRolloffMode.Linear;
            thirdFlash=MakeMuzzle(gun.transform);
            lastGift=initial.giftSequence;lastShot=initial.shots;
        }

        public void Apply(PlayerView next,bool isLocal)
        {
            var nextPosition=new Vector3(next.x,next.y,next.z);
            bool placed=next.poseVersion!=state.poseVersion||(nextPosition-target).sqrMagnitude>2;
            walkingVelocity=placed?Vector3.zero:(nextPosition-target)/Mathf.Max(.03f,Time.time-snapshotAt);
            if(placed)transform.position=nextPosition;
            if(next.hits<lastHit||(!state.alive&&next.alive)){hitReaction.Clear();motion.ClearHit();}
            if(next.hits>lastHit&&next.hitAge<.70f)
            {
                motion.Hit(new Vector3(next.hitX,next.hitY,next.hitZ),next.hitAge);hitReaction.Trigger(next.hitAge);
            }
            lastHit=next.hits;
            state=next;local=isLocal;snapshotAt=Time.time;
            if(!next.alive||next.observer||next.drinking){speechMouth.Silence();gunGrip.Release();}
            if(next.alive&&ragdoll!=null){ragdoll.Restore();ragdoll=null;}
            if(!next.alive&&!next.observer&&ragdoll==null)
            {
                model.SetActive(true);ragdoll=model.AddComponent<SaloonRagdoll>();
                ragdoll.Launch(new Vector3(next.deathX,next.deathY,next.deathZ));
            }
            if(next.observer&&ragdoll!=null)ragdoll.Hide();
            var hitbox=GetComponent<CapsuleCollider>();if(hitbox!=null)hitbox.enabled=next.alive;
            drinkAudio.spatialBlend=isLocal?0:1;drinkAudio.volume=isLocal?.35f:.28f;
            if(next.drinking&&!wasDrinking){drinkAudio.pitch=Mathf.Clamp(2.5f/Mathf.Max(.2f,next.drinkDuration),.5f,3);drinkAudio.Play();}
            if(!next.drinking&&wasDrinking)drinkAudio.Stop();wasDrinking=next.drinking;
            if(next.giftSequence>lastGift)
            {
                lastGift=next.giftSequence;audioSource.PlayOneShot(SaloonBeerAudio.Handoff,.25f);StartCoroutine(Handoff(next.giftTarget,next.giftRemaining));
            }
            target=new Vector3(next.x,next.y,next.z);
            SetViewpoint(isLocal);
            label.text=next.name+(!next.alive?"  ·  eliminated":"")+(next.sheriff?"\nSHERIFF":"")+(!string.IsNullOrEmpty(next.revealedRole)?"\n"+next.revealedRole:"");
            label.color=!next.alive?new Color(.61f,.57f,.51f):next.sheriff?new Color(1,.77f,.3f):new Color(.94f,.86f,.69f);
            if(next.health!=shownHealth||next.maxHealth!=shownMaxHealth)
            {
                shownHealth=next.health;shownMaxHealth=next.maxHealth;
                int count=Mathf.Clamp(next.maxHealth,1,12);
                var dots=new StringBuilder(count*32);
                for(int i=0;i<count;i++)
                {
                    if(i>0)dots.Append("  ");
                    dots.Append(i<next.health?"<color=#F1E4CB>●</color>":"<color=#6D665C>●</color>");
                }
                healthDots.text=dots.ToString();
            }
            bool holdingBeer=next.alive&&!next.drawn&&(next.carriedBeer>0||next.drinking);
            mug.SetActive(!local&&holdingBeer);
            if(firstPersonMug!=null)firstPersonMug.SetActive(local&&holdingBeer);
            if(next.shots>lastShot)
            {
                audioSource.spatialBlend=local?0:1;audioSource.pitch=1+(next.shots%3-1)*.015f;
                audioSource.PlayOneShot(SaloonGunAudio.Shot,local?.8f:.65f);shotAt=Time.time;muzzleUntil=Time.time+.075f;
                var tracer=new GameObject("Shot trail");var line=tracer.AddComponent<LineRenderer>();
                line.sharedMaterial=SaloonWorld.Material("Shot trail",new Color(1,.7f,.3f));line.startWidth=.018f;line.endWidth=.003f;line.positionCount=2;
                var eye=target+Vector3.up*(next.seated?1.35f:1.65f);var dir=Quaternion.Euler(next.pitch,next.yaw,0)*Vector3.forward;
                var weapon=local&&firstPersonGun!=null?firstPersonGun:gun;
                line.SetPosition(0,weapon.transform.TransformPoint(new Vector3(0,.14f,.21f)-SaloonWeaponPose.Grip));line.SetPosition(1,eye+dir*20);Destroy(tracer,.055f);
            }
            lastShot=next.shots;
        }

        public void SetViewpoint(bool isViewpoint)
        {
            local=isViewpoint;
            model.SetActive(!state.observer&&(!local||!state.alive));
            label.gameObject.SetActive(!state.observer&&(!local||!state.alive));healthDots.gameObject.SetActive(!local&&state.alive);
            if(local&&state.alive&&firstPersonGun==null)
            {
                firstPersonGun=MakeGun(game.ViewCamera.transform);firstGunSpin=firstPersonGun.transform.Find("Spin pivot");
                firstPersonMug=SaloonArt.Model("Mug",null,new Vector3(.14f,.17f,.13f));firstPersonMug.transform.SetParent(game.ViewCamera.transform,false);
                flash=MakeMuzzle(firstPersonGun.transform);
            }
        }

        void LateUpdate()
        {
            if(state==null)return;
            hitReaction.Tick();
            speechMouth.Tick(game.Voice.SpeechLevel(state.voiceId),state.alive&&!state.observer&&!state.drinking,Time.unscaledDeltaTime);
            if(!state.alive)
            {
                gun.SetActive(false);mug.SetActive(false);
                if(firstPersonGun!=null)firstPersonGun.SetActive(false);
                if(firstPersonMug!=null)firstPersonMug.SetActive(false);
                if(ragdoll!=null&&!state.observer)
                {
                    label.transform.position=ragdoll.Center+Vector3.up*.65f;
                    label.transform.rotation=game.ViewCamera.transform.rotation;
                }
                return;
            }
            transform.position=Vector3.Lerp(transform.position,target,1-Mathf.Exp(-Time.deltaTime*22));
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(0,state.yaw,0),Time.deltaTime*18);
            float progress=1-Mathf.Clamp01((state.drinkRemaining-(Time.time-snapshotAt))/Mathf.Max(.01f,state.drinkDuration));
            float lift=state.drinking?Mathf.SmoothStep(0,1,Mathf.Min(progress/.2f,(1-progress)/.2f)):0;
            motion.Pose(state.seated,Time.time-snapshotAt<.25f?walkingVelocity:Vector3.zero,lift,SaloonWeaponPose.Kick(Time.time-shotAt),state.drawn?state.pitch:0);
            if(label!=null){label.transform.localPosition=new Vector3(0,state.seated?1.60f:2.05f,0);label.transform.rotation=game.ViewCamera.transform.rotation;}
            if(healthDots!=null){healthDots.transform.localPosition=new Vector3(0,state.seated?1.46f:1.85f,0);healthDots.transform.rotation=game.ViewCamera.transform.rotation;}
            float elapsed=Time.time-shotAt;
            bool finishingShot=state.shots>0&&elapsed<SaloonWeaponPose.Duration;
            drawAmount=Mathf.MoveTowards(drawAmount,(state.alive&&state.drawn)||finishingShot?1:0,Time.deltaTime/SaloonWeaponPose.DrawSeconds);
            float spin=SaloonWeaponPose.Flip(elapsed);
            var shotRotation=SaloonWeaponPose.Rotation(elapsed);var shotOffset=SaloonWeaponPose.Offset(elapsed);
            var drawOffset=SaloonWeaponPose.DrawOffset(drawAmount);var drawRotation=SaloonWeaponPose.DrawRotation(drawAmount);
            gun.transform.localRotation=Quaternion.Euler(state.pitch,0,0)*shotRotation*drawRotation;
            gun.transform.localPosition=new Vector3(.25f,state.seated?.91f:1.20f,.44f)+shotOffset+drawOffset;
            gunSpin.localRotation=Quaternion.Euler(spin,0,0);
            bool weaponVisible=!state.drinking&&(drawAmount>.001f||finishingShot);
            if(weaponVisible)
                gunGrip.Pose(gun.transform,transform.rotation*Quaternion.Euler(state.pitch,0,0)*shotRotation*drawRotation,drawAmount);
            else gunGrip.Release();
            mug.transform.localPosition=Vector3.Lerp(new Vector3(.28f,state.seated?.80f:1.02f,.33f),new Vector3(.06f,state.seated?.96f:1.30f,.38f),lift);
            // +Y is the mug's rim. Negative pitch brings the rim toward the drinker (-Z).
            mug.transform.localRotation=Quaternion.Euler(-65*lift,0,-10*lift);
            if(lift>0)
            {
                var sipOrigin=motion.Mouth-mug.transform.rotation*(Vector3.up*.155f);
                mug.transform.position=Vector3.Lerp(transform.TransformPoint(new Vector3(.28f,state.seated?.80f:1.02f,.33f)),sipOrigin,lift);
            }
            if(mug.activeInHierarchy&&rightArm!=null&&rightForearm!=null&&rightHand!=null)
            {
                var rotation=Quaternion.LookRotation(mug.transform.up,mug.transform.right)*Quaternion.Inverse(handBasis);
                rightHand.rotation=rotation;
                var grip=mug.transform.TransformPoint(new Vector3(.08f,.08f,0));
                SaloonWeaponPose.Reach(rightArm,rightForearm,rightHand,grip-rightHand.TransformVector(handGrip),transform.right*.4f-transform.forward*.2f);
                rightHand.rotation=rotation;
            }
            if(firstPersonMug!=null)
            {
                firstPersonMug.transform.localPosition=Vector3.Lerp(new Vector3(.23f,-.30f,.43f),new Vector3(.08f,-.16f,.30f),lift);
                firstPersonMug.transform.localRotation=Quaternion.Euler(-65*lift,0,-10*lift);
            }
            gun.SetActive(!local&&weaponVisible);
            thirdFlash.gameObject.SetActive(!local&&weaponVisible&&Time.time<muzzleUntil);
            if(firstPersonGun!=null)
            {
                firstPersonGun.SetActive(local&&weaponVisible);
                firstPersonGun.transform.localPosition=new Vector3(.22f,-.25f,.40f)+shotOffset+drawOffset;
                firstPersonGun.transform.localRotation=shotRotation*drawRotation;
                firstGunSpin.localRotation=Quaternion.Euler(spin,0,0);
                flash.gameObject.SetActive(local&&weaponVisible&&Time.time<muzzleUntil);
            }
        }

        System.Collections.IEnumerator Handoff(string targetId,float remaining)
        {
            var offered=SaloonArt.Model("Mug",null,new Vector3(.14f,.17f,.13f));handoffMug=offered;
            float duration=Mathf.Max(.05f,remaining);
            for(float elapsed=0;elapsed<duration;elapsed+=Time.deltaTime)
            {
                var recipient=game.View?.players?.FirstOrDefault(p=>p.id==targetId);
                if(recipient==null)break;
                Vector3 from=local?game.ViewCamera.transform.TransformPoint(new Vector3(.23f,-.30f,.43f)):transform.TransformPoint(new Vector3(.28f,state.seated?.80f:1.02f,.33f));
                Vector3 to=targetId==game.LocalId?game.ViewCamera.transform.TransformPoint(new Vector3(.23f,-.30f,.43f)):new Vector3(recipient.x,recipient.y,recipient.z)+Quaternion.Euler(0,recipient.yaw,0)*new Vector3(.28f,recipient.seated?.80f:1.02f,.33f);
                float amount=Mathf.SmoothStep(0,1,elapsed/duration);offered.transform.position=Vector3.Lerp(from,to,amount)+Vector3.up*Mathf.Sin(amount*Mathf.PI)*.13f;
                offered.transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(0,recipient.yaw,0),amount);
                yield return null;
            }
            if(offered!=null)Destroy(offered);handoffMug=null;
        }

        static Light MakeMuzzle(Transform parent)
        {
            var go=new GameObject("Muzzle flash");go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(0,.14f,.21f)-SaloonWeaponPose.Grip;
            var mesh=new Mesh{name="Low-poly muzzle burst"};var vertices=new Vector3[9];var triangles=new int[24];
            for(int i=0;i<8;i++)
            {
                float angle=i*Mathf.PI/4;float radius=i%2==0?.075f:.025f;
                vertices[i+1]=new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0);
                triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%8+1;
            }
            mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=new Material(Shader.Find("Sprites/Default")){color=new Color(1,.76f,.27f)};
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=2.5f;light.color=new Color(1,.65f,.2f);light.intensity=3;
            go.SetActive(false);return light;
        }

        public static GameObject MakeGun(Transform parent)
        {
            var go=new GameObject("Low-poly revolver");go.transform.SetParent(parent,false);
            var pivot=new GameObject("Spin pivot");pivot.transform.SetParent(go.transform,false);pivot.transform.localPosition=SaloonWeaponPose.Trigger;
            var visual=SaloonArt.Model("Revolver",null,new Vector3(.075f,.18f,.39f));
            visual.transform.SetParent(pivot.transform,false);visual.transform.localPosition=-SaloonWeaponPose.Grip-SaloonWeaponPose.Trigger;
            visual.transform.localRotation=Quaternion.identity;
            return go;
        }

        void OnDestroy(){if(handoffMug!=null)Destroy(handoffMug);if(firstPersonGun!=null)Destroy(firstPersonGun);if(firstPersonMug!=null)Destroy(firstPersonMug);}
    }
}
