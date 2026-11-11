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
        Renderer nameRenderer,healthRenderer;
        AudioSource audioSource,drinkAudio,reloadAudio;
        int lastReload;
        bool wasDrinking;int lastGift;GameObject handoffMug;
        int lastShot;
        bool local;
        Vector3 target;
        float snapshotAt,shotAt=-10,drawAmount;
        Light flash,thirdFlash;
        float muzzleUntil;
        SaloonCharacterMotion motion;
        SaloonSmokingVisual smoking;
        public SaloonSmokingVisual SmokingVisual=>smoking;
        SaloonSpeechMouth speechMouth;
        Vector3 walkingVelocity;
        float lookYaw,lookPitch;
        Transform rightArm,rightForearm,rightHand,gunSpin,firstGunSpin;
        Quaternion handBasis;Vector3 handGrip;
        SaloonGunGrip gunGrip;
        SaloonHitReaction hitReaction;
        int lastHit;
        public bool DamageFlashVisible=>hitReaction!=null&&hitReaction.FlashVisible;
        public float LastVisualHitAt=>hitReaction?.LastHitAt??-10;
        int shownHealth=-1,shownMaxHealth=-1;
        SaloonFootsteps footsteps;
        SaloonRagdoll ragdoll;
        public SaloonRagdoll DeathBody=>ragdoll;
        public bool CorpseVisible=>state!=null&&!state.alive&&model.activeSelf;

        public void Initialize(SaloonGame owner,PlayerView initial,int colorIndex)
        {
            footsteps=gameObject.AddComponent<SaloonFootsteps>();
            game=owner;state=initial;drawAmount=initial.drawn?1:0;target=new Vector3(initial.x,initial.y,initial.z);transform.position=target;
            transform.rotation=Quaternion.Euler(0,initial.bodyYaw,0);lookYaw=initial.yaw;lookPitch=initial.pitch;
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
            smoking=gameObject.AddComponent<SaloonSmokingVisual>();smoking.Initialize(game,motion);
            gunGrip=new SaloonGunGrip(motion);hitReaction=new SaloonHitReaction(model);lastHit=initial.hits;
            gun=MakeGun(transform);gunSpin=gun.transform.Find("Spin pivot");
            mug=SaloonArt.Model("Mug",null,new Vector3(.14f,.17f,.13f));mug.transform.SetParent(transform,false);mug.transform.localPosition=new Vector3(.28f,1.02f,.33f);mug.SetActive(false);
            var nameObject=new GameObject("Name");nameObject.transform.SetParent(transform);nameObject.transform.localPosition=new Vector3(0,2.05f,0);
            label=nameObject.AddComponent<TextMesh>();label.characterSize=.012f;label.fontSize=48;label.anchor=TextAnchor.LowerCenter;label.alignment=TextAlignment.Center;label.lineSpacing=.85f;SaloonWorldText.Attach(label);nameRenderer=label.GetComponent<Renderer>();
            var healthObject=new GameObject("Health dots");healthObject.transform.SetParent(transform,false);healthObject.transform.localPosition=new Vector3(0,1.85f,0);
            healthDots=healthObject.AddComponent<TextMesh>();healthDots.characterSize=.013f;healthDots.fontSize=48;healthDots.anchor=TextAnchor.MiddleCenter;healthDots.alignment=TextAlignment.Center;healthDots.richText=true;SaloonWorldText.Attach(healthDots);healthRenderer=healthDots.GetComponent<Renderer>();
            if(!game.Hosting&&initial.id!=game.LocalId)
            {
                var collider=gameObject.AddComponent<CapsuleCollider>();collider.center=Vector3.up*.9f;collider.height=1.8f;collider.radius=.3f;
                gameObject.AddComponent<SaloonBody>().id=ulong.Parse(initial.id);
            }
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=1;audioSource.minDistance=2;audioSource.maxDistance=45;
            drinkAudio=gameObject.AddComponent<AudioSource>();drinkAudio.playOnAwake=false;drinkAudio.clip=SaloonBeerAudio.Drinking;drinkAudio.minDistance=.8f;drinkAudio.maxDistance=7;drinkAudio.rolloffMode=AudioRolloffMode.Linear;
            thirdFlash=MakeMuzzle(gun.transform);
            reloadAudio=gameObject.AddComponent<AudioSource>();reloadAudio.playOnAwake=false;reloadAudio.minDistance=1;reloadAudio.maxDistance=12;reloadAudio.rolloffMode=AudioRolloffMode.Linear;
            lastReload=initial.reloads;
            lastGift=initial.giftSequence;lastShot=initial.shots;
        }

        public void Apply(PlayerView next,bool isLocal)
        {
            var nextPosition=new Vector3(next.x,next.y,next.z);
            bool placed=next.poseVersion!=state.poseVersion||(nextPosition-target).sqrMagnitude>2;
            if(placed){transform.rotation=Quaternion.Euler(0,next.bodyYaw,0);lookYaw=next.yaw;lookPitch=next.pitch;}
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
            if(!next.alive&&ragdoll==null)
            {
                model.SetActive(true);ragdoll=model.AddComponent<SaloonRagdoll>();
                ragdoll.Launch(new Vector3(next.deathX,next.deathY,next.deathZ));
            }
            var hitbox=GetComponent<CapsuleCollider>();if(hitbox!=null)
            {
                hitbox.height=next.crouched?SaloonMovement.CrouchingHeight:SaloonMovement.StandingHeight;
                hitbox.center=Vector3.up*(hitbox.height*.5f);hitbox.enabled=next.alive;
            }
            drinkAudio.spatialBlend=isLocal?0:1;drinkAudio.volume=isLocal?.5f:.32f;
            if(next.drinking&&!wasDrinking)
            {
                drinkAudio.pitch=Mathf.Clamp(drinkAudio.clip.length/Mathf.Max(.2f,next.drinkDuration),.25f,4);
                drinkAudio.time=Mathf.Clamp01(1-next.drinkRemaining/Mathf.Max(.2f,next.drinkDuration))*(drinkAudio.clip.length-.01f);drinkAudio.Play();
            }
            if(!next.drinking&&wasDrinking)drinkAudio.Stop();wasDrinking=next.drinking;
            if(next.giftSequence>lastGift)
            {
                lastGift=next.giftSequence;audioSource.PlayOneShot(SaloonBeerAudio.Handoff,.25f);StartCoroutine(Handoff(next.giftTarget,next.giftRemaining));
            }
            target=new Vector3(next.x,next.y,next.z);
            SetViewpoint(isLocal);
            label.text=next.name+(!next.alive?"  ·  eliminated":"")+(next.sheriff&&string.IsNullOrEmpty(next.revealedRole)?"\nSHERIFF":"")+(!string.IsNullOrEmpty(next.revealedRole)?"\n"+next.revealedRole:"");
            label.color=!next.alive?new Color(.61f,.57f,.51f):next.sheriff?new Color(1,.77f,.3f):new Color(.94f,.86f,.69f);
            if(next.health!=shownHealth||next.maxHealth!=shownMaxHealth)
            {
                shownHealth=next.health;shownMaxHealth=next.maxHealth;
                int count=SaloonHealth.Segments,shots=SaloonHealth.ShotsToKill(next.health);
                var dots=new StringBuilder(count*32);
                for(int i=0;i<count;i++)
                {
                    dots.Append(i<shots?(shots==1?"<color=#FF6757>●</color>":"<color=#F1E4CB>●</color>"):"<color=#6D665C>●</color>");
                }
                dots.Append(shots==1?"<color=#FF6757>  ":"<color=#F1E4CB>  ").Append(SaloonHealth.Percentage(next.health)).Append("</color>");
                healthDots.text=dots.ToString();
            }
            reloadAudio.spatialBlend=isLocal?0:1;reloadAudio.volume=isLocal?.55f:.35f;
            if(next.reloads>lastReload&&next.reloading)
            {
                reloadAudio.clip=SaloonGunAudio.Reload;reloadAudio.pitch=Mathf.Clamp(reloadAudio.clip.length/Mathf.Max(.2f,next.reloadDuration),.25f,4);
                reloadAudio.time=Mathf.Clamp01(1-next.reloadRemaining/Mathf.Max(.2f,next.reloadDuration))*(reloadAudio.clip.length-.01f);reloadAudio.Play();
            }
            if(!next.reloading||!next.alive)reloadAudio.Stop();lastReload=next.reloads;
            bool holdingBeer=next.alive&&next.room!=3&&!next.drawn&&((next.carriedBeer>0&&next.room!=0)||next.drinking);
            mug.SetActive(!local&&holdingBeer);
            if(firstPersonMug!=null)firstPersonMug.SetActive(local&&holdingBeer);
            if(next.shots>lastShot)
            {
                audioSource.spatialBlend=local?0:1;audioSource.pitch=1+(next.shots%3-1)*.015f;
                audioSource.PlayOneShot(SaloonGunAudio.Shot,local?.8f:.65f);shotAt=Time.time;muzzleUntil=Time.time+.09f;
                var tracer=new GameObject("Shot trail");tracer.layer=SaloonSmokingArea.IndoorLayer;var line=tracer.AddComponent<LineRenderer>();
                line.sharedMaterial=SaloonWorld.Material("Shot trail",new Color(1,.7f,.3f));line.startWidth=.018f;line.endWidth=.003f;line.positionCount=2;
                var eye=target+Vector3.up*SaloonMovement.EyeHeight(next.seated,next.crouched);var dir=Quaternion.Euler(next.pitch,next.yaw,0)*Vector3.forward;
                var weapon=local&&firstPersonGun!=null?firstPersonGun:gun;
                line.SetPosition(0,weapon.transform.TransformPoint(new Vector3(0,.14f,.21f)-SaloonWeaponPose.Grip));line.SetPosition(1,SaloonRoomWindows.ClipShotTrail(eye,dir,20));Destroy(tracer,.055f);
            }
            lastShot=next.shots;
        }

        public void SetViewpoint(bool isViewpoint)
        {
            local=isViewpoint;
            model.SetActive(!local||!state.alive);
            label.gameObject.SetActive(!state.observer&&(!local||!state.alive));healthDots.gameObject.SetActive(!local&&state.alive);
            if(local&&state.alive&&firstPersonGun==null)
            {
                firstPersonGun=MakeGun(game.ViewCamera.transform);firstGunSpin=firstPersonGun.transform.Find("Spin pivot");
                firstPersonMug=SaloonArt.Model("Mug",null,new Vector3(.14f,.17f,.13f));firstPersonMug.transform.SetParent(game.ViewCamera.transform,false);
                flash=MakeMuzzle(firstPersonGun.transform);
                SaloonViewmodel.Attach(game.ViewCamera,firstPersonGun);
            }
        }

        void LateUpdate()
        {
            if(state==null)return;
            footsteps.Tick(transform.position,state.alive,state.seated,Time.time-snapshotAt<.25f,local,state.room==3);
            hitReaction.Tick();
            speechMouth.Tick(game.Voice.SpeechLevel(state.voiceId),state.alive&&!state.observer&&!state.drinking,Time.unscaledDeltaTime);
            if(!state.alive)
            {
                smoking.Pose(state,local,100);
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
            float lookBlend=1-Mathf.Exp(-Time.deltaTime*25);
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(0,state.bodyYaw,0),lookBlend);
            lookYaw=Mathf.LerpAngle(lookYaw,state.yaw,lookBlend);lookPitch=Mathf.Lerp(lookPitch,state.pitch,lookBlend);
            float progress=1-Mathf.Clamp01((state.drinkRemaining-(Time.time-snapshotAt))/Mathf.Max(.01f,state.drinkDuration));
            float lift=state.drinking?Mathf.SmoothStep(0,1,Mathf.Min(progress/.2f,(1-progress)/.2f)):0;
            float headYaw=Mathf.DeltaAngle(transform.eulerAngles.y,lookYaw);
            motion.Pose(state.seated,Time.time-snapshotAt<.25f?walkingVelocity:Vector3.zero,lift,SaloonWeaponPose.Kick(Time.time-shotAt),lookPitch,headYaw,state.crouched);
            // Follow the posed head, so sitting or wearing a hat cannot bury the health label.
            var healthPosition=motion.Bone(HumanBodyBones.Head).position+Vector3.up*.43f;
            if(label!=null){label.transform.position=healthPosition;label.transform.rotation=game.ViewCamera.transform.rotation;}
            if(healthDots!=null){healthDots.transform.position=healthPosition;healthDots.transform.rotation=game.ViewCamera.transform.rotation;}
            float distance=Vector3.Distance(game.ViewCamera.transform.position,healthPosition);
            float labelScale=Mathf.Min(1.25f,distance/3);
            label.transform.localScale=healthDots.transform.localScale=Vector3.one*labelScale;
            // Align the actual text edges, including multiline role labels and font metrics.
            float nameOffset=(healthRenderer.localBounds.max.y-nameRenderer.localBounds.min.y+.022f)*labelScale;
            label.transform.position=healthPosition+game.ViewCamera.transform.up*nameOffset;
            float elapsed=Time.time-shotAt;
            bool finishingShot=state.shots>0&&elapsed<SaloonWeaponPose.Duration;
            drawAmount=Mathf.MoveTowards(drawAmount,(state.alive&&state.drawn)||finishingShot?1:0,Time.deltaTime/SaloonWeaponPose.DrawSeconds);
            float spin=SaloonWeaponPose.Flip(elapsed);
            var shotRotation=SaloonWeaponPose.Rotation(elapsed);var shotOffset=SaloonWeaponPose.Offset(elapsed);
            var drawOffset=SaloonWeaponPose.DrawOffset(drawAmount);var drawRotation=SaloonWeaponPose.DrawRotation(drawAmount);
            var aimRotation=Quaternion.Euler(lookPitch,lookYaw,0)*shotRotation*drawRotation;
            gun.transform.rotation=aimRotation;
            float crouchDrop=motion.CrouchingBlend*SaloonMovement.CrouchDrop;
            gun.transform.localPosition=new Vector3(.25f,(state.seated?.91f:1.20f)-crouchDrop,.44f)+shotOffset+drawOffset;
            gunSpin.localRotation=Quaternion.Euler(spin,0,0);
            bool weaponVisible=state.room!=3&&!state.drinking&&(drawAmount>.001f||finishingShot);
            if(weaponVisible)
                gunGrip.Pose(gun.transform,aimRotation,drawAmount);
            else gunGrip.Release();
            mug.transform.localPosition=Vector3.Lerp(new Vector3(.28f,state.seated?.80f:1.02f-(state.crouched?SaloonMovement.CrouchDrop:0),.33f),new Vector3(.06f,state.seated?.96f:1.30f-crouchDrop,.38f),lift);
            // +Y is the mug's rim. Negative pitch brings the rim toward the drinker (-Z).
            mug.transform.localRotation=Quaternion.Euler(-65*lift,0,-10*lift);
            if(lift>0)
            {
                var sipOrigin=motion.Mouth-mug.transform.rotation*(Vector3.up*.155f);
                mug.transform.position=Vector3.Lerp(transform.TransformPoint(new Vector3(.28f,state.seated?.80f:1.02f-(state.crouched?SaloonMovement.CrouchDrop:0),.33f)),sipOrigin,lift);
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
            smoking.Pose(state,local,state.smokeAge+Mathf.Max(0,Time.time-snapshotAt));
            gun.SetActive(!local&&weaponVisible);
            AnimateMuzzle(thirdFlash,elapsed);if(flash!=null)AnimateMuzzle(flash,elapsed);
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
                Vector3 from=local?game.ViewCamera.transform.TransformPoint(new Vector3(.23f,-.30f,.43f)):transform.TransformPoint(new Vector3(.28f,state.seated?.80f:1.02f-(state.crouched?SaloonMovement.CrouchDrop:0),.33f));
                Vector3 to=targetId==game.LocalId?game.ViewCamera.transform.TransformPoint(new Vector3(.23f,-.30f,.43f)):new Vector3(recipient.x,recipient.y,recipient.z)+Quaternion.Euler(0,recipient.yaw,0)*new Vector3(.28f,recipient.seated?.80f:1.02f-(recipient.crouched?SaloonMovement.CrouchDrop:0),.33f);
                float amount=Mathf.SmoothStep(0,1,elapsed/duration);offered.transform.position=Vector3.Lerp(from,to,amount)+Vector3.up*Mathf.Sin(amount*Mathf.PI)*.13f;
                offered.transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(0,recipient.yaw,0),amount);
                yield return null;
            }
            if(offered!=null)Destroy(offered);handoffMug=null;
        }

        static void AnimateMuzzle(Light light,float elapsed)
        {
            float fade=1-Mathf.Clamp01(elapsed/.09f);
            light.intensity=6*fade*fade;
            light.transform.localScale=Vector3.one*(.6f+.6f*fade);
            light.transform.localRotation=Quaternion.Euler(0,0,(int)(elapsed*120)*37);
        }
        static Light MakeMuzzle(Transform parent)
        {
            var go=new GameObject("Muzzle flash");go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(0,.14f,.21f)-SaloonWeaponPose.Grip;
            var mesh=new Mesh{name="Crossed muzzle flame"};var vertices=new Vector3[18];var triangles=new int[48];var colors=new Color[18];
            // Two intersecting bursts stay visible from the shooter's camera and beside the barrel.
            for(int plane=0;plane<2;plane++)
            {
                int offset=plane*9;colors[offset]=new Color(1,1,.86f);
                for(int i=0;i<8;i++)
                {
                    float angle=i*Mathf.PI/4,radius=i%2==0?.115f:.036f;
                    var point=new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0);
                    vertices[offset+i+1]=plane==0?point:new Vector3(point.x,0,point.y+.035f);
                    colors[offset+i+1]=new Color(1,.46f,.08f,.75f);
                    int t=plane*24+i*3;triangles[t]=offset;triangles[t+1]=offset+i+1;triangles[t+2]=offset+(i+1)%8+1;
                }
            }
            mesh.vertices=vertices;mesh.triangles=triangles;mesh.colors=colors;mesh.RecalculateNormals();go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=new Material(Shader.Find("Sprites/Default")){color=Color.white};
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=3.2f;light.color=new Color(1,.67f,.3f);light.intensity=6;
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

        public void Silence(){if(audioSource!=null)audioSource.Stop();if(drinkAudio!=null)drinkAudio.Stop();if(reloadAudio!=null)reloadAudio.Stop();}

        void OnDestroy(){if(handoffMug!=null)Destroy(handoffMug);if(firstPersonGun!=null)Destroy(firstPersonGun);if(firstPersonMug!=null)Destroy(firstPersonMug);}
    }
}
