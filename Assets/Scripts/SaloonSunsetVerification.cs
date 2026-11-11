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
            SaloonSmokingArea.Apply();Check(FindObjectsByType<Light>().Count(l=>l.name=="Last light over the desert")==1,"reapplying the area does not duplicate scenery or lights");
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
            Check((ViewCamera.cullingMask&(1<<SaloonSmokingArea.OutdoorLayer))==0,"returning inside hides the sunset scenery");
            VerificationPassed=VerificationFinished=true;Debug.Log("SALOON_SUNSET PASS");
        }
    }
}
#endif
