using UnityEngine;
using System.Linq;
using System.Text;

namespace Saloon
{
    public class SaloonAvatar : MonoBehaviour
    {
        SaloonGame game;
        PlayerView state;
        GameObject model,gun,firstPersonGun,mug,firstPersonMug,robberyHands,moneyBag;
        TextMesh label,healthDots;
        AudioSource audioSource,drinkAudio;
        bool wasDrinking;int lastGift;GameObject handoffMug;
        int lastShot;
        bool local;
        Vector3 target;
        float recoil,snapshotAt,shotAt=-10;
        Light flash;
        Animation locomotion;
        Transform rightArm,rightForearm,rightHand;
        int shownHealth=-1,shownMaxHealth=-1;

        public void Initialize(SaloonGame owner,PlayerView initial,int colorIndex)
        {
            game=owner;state=initial;target=new Vector3(initial.x,initial.y,initial.z);transform.position=target;
            var mat=SaloonWorld.Material("Coat "+initial.id,SaloonWorld.PlayerColors[colorIndex%5]);
            mat.mainTexture=Resources.Load<Texture2D>("Art/CharacterSkin");mat.color=Color.Lerp(mat.color,Color.white,.65f);
            model=SaloonWorld.Prop("Art/Character/characterMedium",Vector3.zero,1.72f,0,mat,false);
            model.transform.SetParent(transform,false);model.transform.localPosition=Vector3.zero;
            var bones=model.GetComponentsInChildren<Transform>();
            rightArm=bones.FirstOrDefault(t=>t.name=="RightArm");rightForearm=bones.FirstOrDefault(t=>t.name=="RightForeArm");rightHand=bones.FirstOrDefault(t=>t.name=="RightHand");
            var skeleton=model.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Root");
            if(skeleton!=null)
            {
                foreach(var existing in model.GetComponentsInChildren<Animation>())
                    if(existing.gameObject!=skeleton.gameObject){existing.Stop();existing.enabled=false;}
                foreach(var animator in model.GetComponentsInChildren<Animator>())animator.enabled=false;
                locomotion=skeleton.GetComponent<Animation>();if(locomotion==null)locomotion=skeleton.gameObject.AddComponent<Animation>();
                // The skinned mesh is a sibling of the armature, not its child.
                locomotion.cullingType=AnimationCullingType.AlwaysAnimate;
                foreach(string name in new[]{"idle","run"})
                {
                    var clip=Resources.LoadAll<AnimationClip>("Art/Animations/"+name).Where(c=>!c.name.StartsWith("__preview__")).OrderByDescending(c=>c.length).FirstOrDefault();
                    if(clip==null)continue;locomotion.AddClip(clip,name);locomotion[name].wrapMode=WrapMode.Loop;
                    if(name=="run")locomotion[name].speed=.65f;
                }
                if(locomotion.GetClip("idle")!=null)locomotion.Play("idle");
            }
            gun=MakeGun(transform);gun.transform.localPosition=new Vector3(.27f,1.1f,.36f);
            mug=SaloonArt.Model("Mug",null,new Vector3(.14f,.17f,.13f));mug.transform.SetParent(transform,false);mug.transform.localPosition=new Vector3(.28f,1.02f,.33f);mug.SetActive(false);
            robberyHands=new GameObject("Safe work hands");robberyHands.transform.SetParent(transform,false);
            var hands=SaloonWorld.Material("Safe work gloves",new Color(.45f,.27f,.14f));
            foreach(float x in new[]{-.17f,.17f})
            {
                var arm=GameObject.CreatePrimitive(PrimitiveType.Cube);arm.transform.SetParent(robberyHands.transform,false);arm.transform.localPosition=new Vector3(x,0,.12f);arm.transform.localScale=new Vector3(.065f,.07f,.3f);arm.GetComponent<Renderer>().sharedMaterial=hands;arm.GetComponent<Collider>().enabled=false;Destroy(arm.GetComponent<Collider>());
            }
            moneyBag=GameObject.CreatePrimitive(PrimitiveType.Sphere);moneyBag.name="Money pouch";moneyBag.transform.SetParent(transform,false);moneyBag.transform.localPosition=new Vector3(.25f,1.05f,.4f);moneyBag.transform.localScale=new Vector3(.18f,.24f,.16f);moneyBag.GetComponent<Renderer>().sharedMaterial=SaloonWorld.Material("Money pouch leather",new Color(.26f,.12f,.035f));moneyBag.GetComponent<Collider>().enabled=false;Destroy(moneyBag.GetComponent<Collider>());
            robberyHands.SetActive(false);moneyBag.SetActive(false);
            var nameObject=new GameObject("Name");nameObject.transform.SetParent(transform);nameObject.transform.localPosition=new Vector3(0,2.05f,0);
            label=nameObject.AddComponent<TextMesh>();label.characterSize=.022f;label.fontSize=48;label.anchor=TextAnchor.MiddleCenter;
            var healthObject=new GameObject("Health dots");healthObject.transform.SetParent(transform,false);healthObject.transform.localPosition=new Vector3(0,1.85f,0);
            healthDots=healthObject.AddComponent<TextMesh>();healthDots.characterSize=.018f;healthDots.fontSize=48;healthDots.anchor=TextAnchor.MiddleCenter;healthDots.richText=true;
            if(!game.Hosting&&initial.id!=game.LocalId)
            {
                var collider=gameObject.AddComponent<CapsuleCollider>();collider.center=Vector3.up*.9f;collider.height=1.8f;collider.radius=.3f;
                gameObject.AddComponent<SaloonBody>().id=ulong.Parse(initial.id);
            }
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=1;audioSource.minDistance=2;audioSource.maxDistance=45;
            drinkAudio=gameObject.AddComponent<AudioSource>();drinkAudio.clip=SaloonBeerAudio.Drinking;drinkAudio.minDistance=.8f;drinkAudio.maxDistance=7;drinkAudio.rolloffMode=AudioRolloffMode.Linear;
            lastGift=initial.giftSequence;lastShot=initial.shots;
        }

        public void Apply(PlayerView next,bool isLocal)
        {
            state=next;local=isLocal;snapshotAt=Time.time;
            drinkAudio.spatialBlend=isLocal?0:1;drinkAudio.volume=isLocal?.35f:.28f;
            if(next.drinking&&!wasDrinking){drinkAudio.pitch=Mathf.Clamp(2.5f/Mathf.Max(.2f,next.drinkDuration),.5f,3);drinkAudio.Play();}
            if(!next.drinking&&wasDrinking)drinkAudio.Stop();wasDrinking=next.drinking;
            if(next.giftSequence>lastGift)
            {
                lastGift=next.giftSequence;audioSource.PlayOneShot(SaloonBeerAudio.Handoff,.25f);StartCoroutine(Handoff(next.giftTarget,next.giftRemaining));
            }
            target=new Vector3(next.x,next.y,next.z);
            model.SetActive(!local);label.gameObject.SetActive(!local);
            healthDots.gameObject.SetActive(!local);
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
            robberyHands.SetActive(!local&&next.alive&&next.robbing);
            moneyBag.SetActive(!local&&next.alive&&(next.moneyBag||next.robbing));
            gun.SetActive(!local&&next.alive&&next.drawn);
            bool holdingBeer=next.alive&&!next.drawn&&(next.carriedBeer>0||next.drinking);
            mug.SetActive(!local&&holdingBeer);
            if(local&&firstPersonGun==null)
            {
                firstPersonGun=MakeGun(game.ViewCamera.transform);
                firstPersonMug=SaloonArt.Model("Mug",null,new Vector3(.14f,.17f,.13f));firstPersonMug.transform.SetParent(game.ViewCamera.transform,false);
                flash=firstPersonGun.AddComponent<Light>();flash.type=LightType.Point;flash.range=3;flash.color=new Color(1,.6f,.2f);flash.intensity=0;
            }
            if(firstPersonGun!=null)firstPersonGun.SetActive(local&&next.alive&&next.drawn);
            if(firstPersonMug!=null)firstPersonMug.SetActive(local&&holdingBeer);
            if(next.shots>lastShot)
            {
                audioSource.spatialBlend=local?0:1;audioSource.pitch=1+(next.shots%3-1)*.015f;
                audioSource.PlayOneShot(SaloonGunAudio.Shot,local?.8f:.65f);recoil=1;shotAt=Time.time;
                var tracer=new GameObject("Shot trail");var line=tracer.AddComponent<LineRenderer>();
                line.sharedMaterial=SaloonWorld.Material("Shot trail",new Color(1,.7f,.3f));line.startWidth=.018f;line.endWidth=.003f;line.positionCount=2;
                var eye=target+Vector3.up*(next.seated?1.35f:1.65f);var dir=Quaternion.Euler(next.pitch,next.yaw,0)*Vector3.forward;
                line.SetPosition(0,eye+dir*.4f);line.SetPosition(1,eye+dir*20);Destroy(tracer,.055f);
            }
            lastShot=next.shots;
        }

        void LateUpdate()
        {
            if(state==null)return;
            bool moving=(transform.position-target).sqrMagnitude>.003f&&state.alive&&!state.seated;
            if(locomotion!=null)
            {
                string clip=moving?"run":"idle";
                if(locomotion.GetClip(clip)!=null&&!locomotion.IsPlaying(clip))locomotion.CrossFade(clip,.15f);
            }
            transform.position=Vector3.Lerp(transform.position,target,1-Mathf.Exp(-Time.deltaTime*22));
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(0,state.yaw,0),Time.deltaTime*18);
            model.transform.localRotation=Quaternion.Euler(state.alive?(state.robbing?12:state.drinking?-5:0):12,0,0);
            robberyHands.transform.localPosition=new Vector3(0,1.12f,.30f+Mathf.Sin(Time.time*8)*.07f);
            if(state.robbing)moneyBag.transform.localPosition=new Vector3(.25f,1.05f+Mathf.Sin(Time.time*8)*.035f,.4f);
            model.transform.localScale=new Vector3(1,state.seated?.78f:1,1);
            if(label!=null)label.transform.rotation=game.ViewCamera.transform.rotation;
            if(healthDots!=null)healthDots.transform.rotation=game.ViewCamera.transform.rotation;
            recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*5);
            float spin=360*Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-shotAt-.16f)/.72f));
            gun.transform.localRotation=Quaternion.Euler(state.pitch-recoil*25+spin,0,-recoil*8);
            float progress=1-Mathf.Clamp01((state.drinkRemaining-(Time.time-snapshotAt))/Mathf.Max(.01f,state.drinkDuration));
            float lift=state.drinking?Mathf.SmoothStep(0,1,Mathf.Min(progress/.2f,(1-progress)/.2f)):0;
            mug.transform.localPosition=Vector3.Lerp(new Vector3(.28f,state.seated?.80f:1.02f,.33f),new Vector3(.06f,state.seated?.96f:1.30f,.38f),lift);
            mug.transform.localRotation=Quaternion.Euler(65*lift,0,-10*lift);
            if(mug.activeInHierarchy&&rightArm!=null&&rightForearm!=null&&rightHand!=null)
            {
                var grip=mug.transform.TransformPoint(new Vector3(.09f,.05f,0));
                // Bend the existing skinned arm toward the mug after the idle animation.
                for(int i=0;i<5;i++)
                {
                    rightForearm.rotation=Quaternion.FromToRotation(rightHand.position-rightForearm.position,grip-rightForearm.position)*rightForearm.rotation;
                    rightArm.rotation=Quaternion.FromToRotation(rightHand.position-rightArm.position,grip-rightArm.position)*rightArm.rotation;
                }
            }
            if(firstPersonMug!=null)
            {
                firstPersonMug.transform.localPosition=Vector3.Lerp(new Vector3(.23f,-.30f,.43f),new Vector3(.08f,-.16f,.30f),lift);
                firstPersonMug.transform.localRotation=Quaternion.Euler(65*lift,0,-10*lift);
            }
            if(firstPersonGun!=null)
            {
                firstPersonGun.transform.localPosition=new Vector3(.23f,-.24f,.43f-recoil*.13f);
                firstPersonGun.transform.localRotation=Quaternion.Euler(-recoil*28+spin,0,-recoil*8);
                flash.intensity=recoil>.75f?2:0;
                if(game.View!=null&&game.View.busyRemaining>0)
                    firstPersonGun.transform.localRotation*=Quaternion.Euler(25,0,25);
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

        public static GameObject MakeGun(Transform parent)
        {
            var go=new GameObject("Low-poly revolver");go.transform.SetParent(parent,false);
            var visual=SaloonArt.Model("Revolver",null,new Vector3(.075f,.18f,.39f));
            visual.transform.SetParent(go.transform,false);visual.transform.localRotation=Quaternion.identity;
            return go;
        }

        void OnDestroy(){if(handoffMug!=null)Destroy(handoffMug);if(firstPersonGun!=null)Destroy(firstPersonGun);if(firstPersonMug!=null)Destroy(firstPersonMug);}
    }
}
