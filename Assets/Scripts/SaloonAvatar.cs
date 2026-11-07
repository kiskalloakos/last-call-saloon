using UnityEngine;
using System.Linq;

namespace Saloon
{
    public class SaloonAvatar : MonoBehaviour
    {
        SaloonGame game;
        PlayerView state;
        GameObject model,gun,firstPersonGun;
        TextMesh label;
        AudioSource audioSource;
        static AudioClip shotSound;
        int lastShot;
        bool local;
        Vector3 target;
        float recoil;
        Light flash;
        Animation locomotion;

        public void Initialize(SaloonGame owner,PlayerView initial,int colorIndex)
        {
            game=owner;state=initial;target=new Vector3(initial.x,initial.y,initial.z);transform.position=target;
            var mat=SaloonWorld.Material("Coat "+initial.id,SaloonWorld.PlayerColors[colorIndex%5]);
            mat.mainTexture=Resources.Load<Texture2D>("Art/CharacterSkin");mat.color=Color.Lerp(mat.color,Color.white,.65f);
            model=SaloonWorld.Prop("Art/Character/characterMedium",Vector3.zero,1.72f,0,mat,false);
            model.transform.SetParent(transform,false);model.transform.localPosition=Vector3.zero;
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
            var nameObject=new GameObject("Name");nameObject.transform.SetParent(transform);nameObject.transform.localPosition=new Vector3(0,2.05f,0);
            label=nameObject.AddComponent<TextMesh>();label.characterSize=.035f;label.fontSize=48;label.anchor=TextAnchor.MiddleCenter;
            if(!game.Hosting&&initial.id!=game.LocalId)
            {
                var collider=gameObject.AddComponent<CapsuleCollider>();collider.center=Vector3.up*.9f;collider.height=1.8f;collider.radius=.3f;
                gameObject.AddComponent<SaloonBody>().id=ulong.Parse(initial.id);
            }
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=1;audioSource.minDistance=2;audioSource.maxDistance=45;
            if(shotSound==null)shotSound=CreateShot();
            lastShot=initial.shots;
        }

        public void Apply(PlayerView next,bool isLocal)
        {
            state=next;local=isLocal;target=new Vector3(next.x,next.y,next.z);
            model.SetActive(!local);label.gameObject.SetActive(!local&&next.alive);
            label.text=next.name+(next.sheriff?"\nSHERIFF":"")+(!string.IsNullOrEmpty(next.revealedRole)?"\n"+next.revealedRole:"");
            label.color=next.sheriff?new Color(1,.77f,.3f):new Color(.94f,.86f,.69f);
            gun.SetActive(!local&&next.alive&&next.drawn);
            if(local&&firstPersonGun==null)
            {
                firstPersonGun=MakeGun(game.ViewCamera.transform);
                flash=firstPersonGun.AddComponent<Light>();flash.type=LightType.Point;flash.range=3;flash.color=new Color(1,.6f,.2f);flash.intensity=0;
            }
            if(firstPersonGun!=null)firstPersonGun.SetActive(local&&next.alive&&next.drawn);
            if(next.shots>lastShot)
            {
                audioSource.PlayOneShot(shotSound,.55f);recoil=1;
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
            model.transform.localRotation=Quaternion.Euler(state.alive?0:85,0,0);
            model.transform.localScale=new Vector3(1,state.seated?.78f:1,1);
            if(label!=null)label.transform.rotation=game.ViewCamera.transform.rotation;
            recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*5);
            if(firstPersonGun!=null)
            {
                firstPersonGun.transform.localPosition=new Vector3(.23f,-.24f,.43f-recoil*.13f);
                firstPersonGun.transform.localRotation=Quaternion.Euler(-recoil*22,0,-recoil*5);
                flash.intensity=recoil>.75f?2:0;
                if(game.View!=null&&game.View.busyRemaining>0)
                    firstPersonGun.transform.localRotation*=Quaternion.Euler(25,0,25);
            }
        }

        public static GameObject MakeGun(Transform parent)
        {
            var go=new GameObject("Placeholder revolver");go.transform.SetParent(parent,false);
            var steel=SaloonWorld.Material("Revolver steel",new Color(.16f,.17f,.18f),.65f);
            var grip=SaloonWorld.Material("Revolver grip",new Color(.24f,.10f,.045f));
            Part("Frame",new Vector3(0,0,.04f),new Vector3(.08f,.10f,.22f),steel,go.transform,false);
            var barrel=Part("Barrel",new Vector3(0,.025f,.23f),new Vector3(.045f,.15f,.045f),steel,go.transform,true);barrel.transform.localRotation=Quaternion.Euler(90,0,0);
            var cylinder=Part("Cylinder",new Vector3(0,.005f,.04f),new Vector3(.095f,.065f,.095f),steel,go.transform,true);cylinder.transform.localRotation=Quaternion.Euler(90,0,0);
            var handle=Part("Wooden grip",new Vector3(0,-.095f,-.07f),new Vector3(.065f,.15f,.075f),grip,go.transform,false);handle.transform.localRotation=Quaternion.Euler(-20,0,0);
            Part("Front sight",new Vector3(0,.065f,.34f),new Vector3(.01f,.025f,.025f),steel,go.transform,false);
            return go;
        }

        static GameObject Part(string name,Vector3 position,Vector3 scale,Material mat,Transform parent,bool cylinder)
        {
            var go=cylinder?SaloonWorld.Cylinder(name,Vector3.zero,scale,mat,parent):SaloonWorld.Box(name,Vector3.zero,scale,mat,parent);
            go.transform.localPosition=position;go.GetComponent<Collider>().enabled=false;Object.Destroy(go.GetComponent<Collider>());return go;
        }
        static AudioClip CreateShot()
        {
            int count=16000;var samples=new float[count];var random=new System.Random(173);
            for(int i=0;i<count;i++)
            {
                float t=i/44100f;float decay=Mathf.Exp(-t*22);
                samples[i]=(float)(random.NextDouble()*2-1)*decay*.65f+Mathf.Sin(t*110*6.28f)*decay*.25f;
            }
            var clip=AudioClip.Create("Generated placeholder revolver shot",count,1,44100,false);clip.SetData(samples,0);return clip;
        }
        void OnDestroy(){if(firstPersonGun!=null)Destroy(firstPersonGun);}
    }
}
