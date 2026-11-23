#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifySunsetOverlook()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string message)
            {
                if(!ok){VerificationFinished=true;Debug.LogError("SALOON_SUNSET FAIL "+message);throw new InvalidOperationException(message);}
                Debug.Log("SALOON_SUNSET_CHECK "+message);
            }
            yield return null;
            tuning.turnSeconds=120;
            var hero=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            rounds.Start(players.Keys,Time.time,30,120,6);phase=rounds.Phase;
            foreach(var p in players.Values){p.chosenRoom=p==hero||p==peer?3:0;p.companion=null;rounds.SetReady(p.id,true);}
            rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);Broadcast();MenuOpen=false;
            yield return new WaitForSeconds(.4f);
            Check(hero.room==3&&peer.room==3&&hero.seat<0,"two smokers arrive standing at the overlook");
            Check(Mathf.Abs(Mathf.DeltaAngle(hero.yaw,90))<.01f&&Mathf.Abs(Mathf.DeltaAngle(peer.yaw,90))<.01f,"both arrival views face the sunset");
            var root=GameObject.Find("have a smoke · sunset deck");
            Check(root!=null&&GameObject.Find("have a smoke · placeholder yard")==null,"sunset deck replaces the placeholder yard");
            var vista=root.transform.Find("Wild West sunset overlook");
            Check(vista!=null&&vista.GetComponentsInChildren<Renderer>().Length>50,"mesas, desert, cacti and sky form the overlook");
            Check(vista.GetComponentsInChildren<Collider>().All(c=>!c.enabled),"distant scenery cannot interfere with player collision");
            Check(vista.GetComponentsInChildren<Renderer>().All(r=>r.gameObject.layer==SaloonSmokingArea.OutdoorLayer),"sunset scenery stays on the outdoor visibility layer");
            var sky=vista.Find("Apricot and violet sunset sky").GetComponent<Renderer>();
            Check(sky.sharedMaterial.shader.isSupported&&sky.sharedMaterial.shader==Resources.Load<Shader>("Shaders/SaloonSunset"),"sunset shader is included and supported by the graphics device");
            Check((ViewCamera.cullingMask&(1<<SaloonSmokingArea.IndoorLayer))==0,"outdoor view excludes the saloon");
            var sun=vista.GetComponentInChildren<Light>();
            Check(sun.cullingMask==(1<<SaloonSmokingArea.OutdoorLayer)&&GameObject.Find("Evening sun").GetComponent<Light>().cullingMask!=(~0),"sunset lighting is restricted to the outdoor scene");
            var audioProbe=new GameObject("Outdoor footstep test");var footsteps=audioProbe.AddComponent<SaloonFootsteps>();
            Vector3 pos=Vector3.zero;
            footsteps.Tick(pos,true,false,true,true,false);
            for(int i=0;i<4;i++){pos.x+=.35f;footsteps.Tick(pos,true,false,true,true,false);}
            int steps=footsteps.StepsPlayed;Check(steps>0,"indoor walking still produces wooden footsteps");
            for(int i=0;i<20;i++){pos.x+=.35f;footsteps.Tick(pos,true,false,true,i%2==0,true);}
            Check(footsteps.StepsPlayed==steps&&!audioProbe.GetComponent<AudioSource>().isPlaying,"local and remote outside walking is silent and stops any previous step");
            for(int i=0;i<4;i++){pos.x+=.35f;footsteps.Tick(pos,true,false,true,true,false);}
            Check(footsteps.StepsPlayed>steps,"wooden footsteps resume after returning inside");Destroy(audioProbe);
            SaloonSmokingArea.Apply();SaloonSmokingArea.Apply();
            Check(root.GetComponentsInChildren<Light>().Count(l=>l.name=="Last light over the desert")==1&&GameObject.Find("Saloon entrance sunset").GetComponentsInChildren<Light>().Length==1,"reapplying the area does not duplicate either sunset or its lights");
            Check(root.GetComponentsInChildren<InteractionPoint>().Count(p=>p.kind==InteractionKind.Seat)==2,"back bench has exactly two usable seats after repeated application");
            var arrival=hero.body.transform.position;var peerArrival=peer.body.transform.position;
            hero.lastStance=-100;OnCommand(hero.id,new Command{kind="stance"});
            Check(hero.seat<0&&hero.notice.Contains("bench"),"Space requires an empty bench spot nearby");
            IEnumerator WalkToBench(Participant p,float z)
            {
                var target=SaloonSmokingArea.Center+new Vector3(-2.15f,0,z);float until=Time.time+4;
                while(Time.time<until&&Vector3.ProjectOnPlane(target-p.body.transform.position,Vector3.up).magnitude>.12f)
                {
                    var direction=Vector3.ProjectOnPlane(target-p.body.transform.position,Vector3.up).normalized;
                    OnCommand(p.id,new Command{kind="input",x=direction.z*-1,z=direction.x,yaw=90,pitch=0,poseVersion=p.poseVersion});yield return null;
                }
                OnCommand(p.id,new Command{kind="input",yaw=90,poseVersion=p.poseVersion});
                Check(Vector3.ProjectOnPlane(target-p.body.transform.position,Vector3.up).magnitude<.2f,"real walking reaches the back bench");
            }
            yield return WalkToBench(hero,-.75f);
            hero.lastStance=-100;OnCommand(hero.id,new Command{kind="stance"});Broadcast();yield return null;
            Check(hero.seat==1040&&!hero.controller.enabled&&PrivateSnapshot(hero.id).players.First(p=>p.id==hero.id.ToString()).seated,"Space seats the smoker on the bench and replicates seated posture");
            Check(Mathf.Abs(Mathf.DeltaAngle(hero.yaw,90))<.01f&&!rounds.HasSpent(hero.id),"seated smoker faces the sunset without spending an action");
            yield return WalkToBench(peer,-.75f);
            peer.lastStance=-100;OnCommand(peer.id,new Command{kind="stance"});
            Check(peer.seat<0,"another smoker cannot occupy the same bench spot");
            yield return WalkToBench(peer,.75f);
            peer.lastStance=-100;OnCommand(peer.id,new Command{kind="stance"});
            Check(peer.seat==1041&&peer.seat!=hero.seat,"both smokers can sit in separate bench spots");
            int puffs=hero.puffs;hero.smokeAt=-100;OnCommand(hero.id,new Command{kind="smoke"});
            Check(hero.puffs==puffs+1&&hero.seat==1040&&!rounds.HasSpent(hero.id),"seated smoker can take a drag without leaving the bench or spending an action");
            externalCamera=true;externalPosition=SaloonSmokingArea.Center+new Vector3(2,1.6f,0);externalRotation=Quaternion.LookRotation(SaloonSmokingArea.Center+new Vector3(-2.9f,1,0)-externalPosition);RefreshViewpoint();Broadcast();yield return null;
            SmokingImage("back-bench");externalCamera=false;RefreshViewpoint();
            hero.lastStance=-100;OnCommand(hero.id,new Command{kind="stance"});
            Check(hero.seat<0&&hero.controller.enabled&&SaloonMovement.InRoom(hero.body.transform.position,3),"Space stands up safely and restores walking on the back deck");
            foreach(var p in new[]{hero,peer})
            {p.controller.enabled=false;p.body.transform.position=p==hero?arrival:peerArrival;p.seat=-1;p.controller.enabled=true;p.poseVersion++;UpdatePlayerColliders(p);}
            Broadcast();yield return null;
            TrySmoke(hero);hero.smokeAt=Time.time-1;Broadcast();yield return new WaitForSeconds(.1f);
            var previous=RenderTexture.active;var oldTarget=ViewCamera.targetTexture;
            var target=new RenderTexture(1280,900,24);var texture=new Texture2D(1280,900,TextureFormat.RGB24,false);
            try
            {
                ViewCamera.targetTexture=target;ViewCamera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1280,900),0,0);texture.Apply();
                var pixels=texture.GetPixels32();
                int warm=pixels.Count(c=>c.r>150&&c.r>c.g*1.25f&&c.g>c.b*1.15f);
                int sunPixels=pixels.Count(c=>c.r>240&&c.g>205&&c.b>115&&c.b<210);
                Check(warm>50000&&sunPixels>300,"sunset renders warm sky and a visible low sun (warm="+warm+", sun="+sunPixels+")");
                string file=Path.Combine(Path.GetTempPath(),"SaloonSunset-overlook.png");File.WriteAllBytes(file,texture.EncodeToPNG());Debug.Log("SALOON_SUNSET_IMAGE "+file);
            }
            finally{ViewCamera.targetTexture=oldTarget;RenderTexture.active=previous;Destroy(texture);target.Release();Destroy(target);}
            Check(avatars[LocalId].SmokingVisual.FirstPersonVisible,"cigarette remains visible against the sunset");
            var start=hero.body.transform.position;float until=Time.time+.5f;
            while(Time.time<until){OnCommand(hero.id,new Command{kind="input",z=1,yaw=90,poseVersion=hero.poseVersion});yield return null;}
            OnCommand(hero.id,new Command{kind="input",yaw=90,poseVersion=hero.poseVersion});
            Check(Vector3.Distance(start,hero.body.transform.position)>.15f&&SaloonMovement.InRoom(hero.body.transform.position,3),"outside walking still works within the deck boundary (distance="+Vector3.Distance(start,hero.body.transform.position)+", phase="+phase+")");
            PositionEveryone(true);Broadcast();yield return null;
            SaloonSmokingArea.ApplyCamera(ViewCamera,false);
            Check((ViewCamera.cullingMask&(1<<SaloonSmokingArea.OutdoorLayer))==0&&(ViewCamera.cullingMask&(1<<SaloonSunset.EntranceLayer))!=0,"returning inside hides the private deck while retaining the front sunset");
            var front=GameObject.Find("Saloon entrance sunset");
            Check(front.GetComponentsInChildren<Renderer>().All(r=>r.gameObject.layer==SaloonSunset.EntranceLayer)&&front.GetComponentsInChildren<Collider>().All(c=>!c.enabled),"front sunset has scenery only on its own visibility layer");
            Check(front.GetComponentInChildren<Light>().cullingMask==(1<<SaloonSunset.EntranceLayer),"front sunset light cannot alter indoor or smoking-deck lighting");
            var frontSky=front.GetComponentInChildren<Renderer>();
            var frontDirection=frontSky.sharedMaterial.GetVector("_SunDirection");
            Check(Vector3.Dot(front.transform.TransformDirection(SaloonSunset.Direction),new Vector3(frontDirection.x,frontDirection.y,frontDirection.z))>.99f,"front scenery rotates the same sunset toward the entrance");
            hero.room=0;hero.controller.enabled=false;hero.body.transform.position=new Vector3(0,.08f,-6.1f);hero.seat=-1;hero.controller.enabled=true;hero.yaw=yaw=180;hero.pitch=pitch=0;hero.poseVersion++;
            peer.room=3;peer.controller.enabled=false;peer.body.transform.position=peerArrival;peer.controller.enabled=true;
            hero.lastAction=-100;OnCommand(hero.id,new Command{kind="room",x=3});
            Check(hero.room==0,"room actions cannot enter the back deck; smoking requires the discussion plan");
            var indoor=Snapshot.FromWireJson(PrivateSnapshot(hero.id).ToWireJson());
            Check(indoor.players.All(p=>p.id!=peer.id.ToString())&&SaloonPrivacy.VoiceChannel(VoiceRoom,Phase.Turns,0,true)!=SaloonPrivacy.VoiceChannel(VoiceRoom,Phase.Turns,3,true),"front view receives no smoker avatar, animation or private conversation");
            Check(!SaloonMovement.InRoom(peerArrival,0)&&!SaloonMovement.InRoom(hero.body.transform.position,3),"front entrance and private back deck have separate walking bounds");
            var door=SaloonEntranceDoor.Build();SaloonEntranceDoor.Build();Physics.SyncTransforms();
            Check(door.GetComponentsInChildren<BoxCollider>().Length==2&&Physics.RaycastAll(new Vector3(0,1.7f,-6.1f),Vector3.back,1.8f).Any(h=>h.collider.GetComponentInParent<SaloonEntranceDoor>()==door),"sunset view keeps a full-height entrance barrier and two door leaves");
            Broadcast();MenuOpen=ShowRole=false;cameraCut=true;UpdateCamera();yield return null;
            SmokingImage("front-entrance");
            VerificationPassed=VerificationFinished=true;Debug.Log("SALOON_SUNSET PASS");
        }
    }
}
#endif
