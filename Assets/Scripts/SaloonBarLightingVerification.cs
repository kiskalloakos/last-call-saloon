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
        public IEnumerator VerifyBarLighting()
        {
            verifying=AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            int checks=0;
            void Check(bool ok,string message)
            {checks++;if(!ok){VerificationFinished=true;throw new InvalidOperationException("Bar lighting: "+message);}Debug.Log("SALOON_BAR_LIGHTING_CHECK "+message);}
            yield return new WaitForSeconds(.2f);
            MenuOpen=ShowRole=false;
            var lights=GameObject.Find("Saloon World").transform.Find(SaloonLighting.RootName).GetComponentsInChildren<Light>(true);
            var original=lights.Select(l=>(l.enabled,l.intensity,l.range,l.color,l.cullingMask)).ToArray();
            var glass=lights.First(l=>l.name=="Pendant lantern").transform.Find("Luminous glass chamber").GetComponent<Renderer>().sharedMaterial;
            var emission=glass.GetColor("_EmissionColor");bool emissive=glass.IsKeywordEnabled("_EMISSION");
            var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;var probe=RenderSettings.ambientProbe;
            var position=ViewCamera.transform.position;var rotation=ViewCamera.transform.rotation;float fov=ViewCamera.fieldOfView;
            var point=new Vector3(0,1.7f,-2.4f);var look=new Vector3(-.8f,1.3f,3.6f);
            string folder=Path.Combine(Path.GetTempPath(),"LastCall-BarLighting");Directory.CreateDirectory(folder);
            Color32[] Capture(string name,Vector3 eye,Vector3 target)
            {
                ViewCamera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));ViewCamera.fieldOfView=100;
                var previous=RenderTexture.active;var old=ViewCamera.targetTexture;
                var image=new RenderTexture(1280,900,24);var texture=new Texture2D(1280,900,TextureFormat.RGB24,false);
                try
                {
                    ViewCamera.targetTexture=image;ViewCamera.Render();RenderTexture.active=image;
                    texture.ReadPixels(new Rect(0,0,1280,900),0,0);texture.Apply();
                    File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());return texture.GetPixels32();
                }
                finally{ViewCamera.targetTexture=old;RenderTexture.active=previous;Destroy(texture);image.Release();Destroy(image);}
            }
            float Luminance(Color32[] pixels)=>pixels.Average(c=>(.2126f*c.r+.7152f*c.g+.0722f*c.b)/255);
            Connection.SnapshotReceived-=ApplySnapshot;
            try
            {
                Check(BarLighting!=null&&GameObject.Find("Saloon World").transform.Find(SaloonBarLighting.RootName).GetComponentsInChildren<Light>().Length==2,"only two window light sources are created");
                BarLighting.enabled=false;var before=Capture("before",point,look);BarLighting.enabled=true;
                bool sawBarLighting=false;
                void Observe(Camera camera)
                {
                    if(camera!=ViewCamera)return;
                    sawBarLighting=lights.Where(l=>l.name=="Pendant lantern"&&l.transform.position.z>SaloonRoomWindows.FrontZ).All(l=>l.enabled&&l.GetComponentsInChildren<Renderer>().All(r=>r.enabled))
                        &&glass.IsKeywordEnabled("_EMISSION")&&glass.GetColor("_EmissionColor").maxColorComponent>.1f
                        &&lights.Where(l=>l.name=="Pendant lantern"&&l.transform.position.z>SaloonRoomWindows.FrontZ&&l.enabled).All(l=>l.intensity<1.3f)
                        &&lights.Where(l=>l.name=="Wall lantern").All(l=>l.intensity<1f)
                        &&lights.Where(l=>l.type==LightType.Directional).All(l=>l.enabled&&l.intensity<=.2f);
                }
                Camera.onPreCull+=Observe;
                Color32[] early;
                try{BarLighting.SetRound(1,true);early=Capture("round-01",point,look);}
                finally{Camera.onPreCull-=Observe;}
                Check(sawBarLighting,"all Bar fixtures and light sources stay enabled at reduced brightness");

                Check(BarLighting.Dusk==0,"round one starts at sunset");
                BarLighting.SetRound(10,true);var middle=Capture("round-10",point,look);
                Check(BarLighting.Dusk>0&&BarLighting.Dusk<1,"middle rounds show an intermediate dusk");
                BarLighting.SetRound(20,true);var late=Capture("round-20",point,look);
                Check(BarLighting.Dusk==1,"round twenty reaches the night profile");
                BarLighting.SetRound(50,true);var later=Capture("round-50",point,look);
                Check(late.SequenceEqual(later),"late rounds retain night without a lighting reset or match cutoff");
                float oldLuma=Luminance(before),earlyLuma=Luminance(early),middleLuma=Luminance(middle),lateLuma=Luminance(late);
                Check(earlyLuma<oldLuma*.85f,"sunset Bar is dimmer than the old flood lighting ("+oldLuma+" -> "+earlyLuma+")");
                Check(earlyLuma>middleLuma&&middleLuma>lateLuma&&lateLuma>.18f,"Bar darkens progressively without becoming black ("+earlyLuma+", "+middleLuma+", "+lateLuma+")");
                foreach(int room in new[]{1,2})
                {
                    float x=room==1?-5:5;var eye=new Vector3(x,1.7f,-2.35f);var target=new Vector3(x,1.6f,-4.8f);
                    BarLighting.enabled=false;var baseline=Capture("room-"+room+"-original",eye,target);BarLighting.enabled=true;
                    BarLighting.SetRound(1,true);var atSunset=Capture("room-"+room+"-round-01",eye,target);
                    BarLighting.SetRound(20,true);var atNight=Capture("room-"+room+"-round-20",eye,target);
                    Check(baseline.SequenceEqual(atSunset)&&baseline.SequenceEqual(atNight),SaloonStations.Name(room)+" renders identically to its original lighting at early and late rounds");
                }
                var deck=SaloonSmokingArea.Center+Vector3.up*1.65f;
                BarLighting.enabled=false;var deckBefore=Capture("smoking-original",deck,deck+Vector3.right*10);BarLighting.enabled=true;
                Check(deckBefore.SequenceEqual(Capture("smoking-round-20",deck,deck+Vector3.right*10)),"private smoking sunset remains unchanged");
                Check(lights.Select(l=>(l.enabled,l.intensity,l.range,l.color,l.cullingMask)).SequenceEqual(original)&&RenderSettings.ambientLight==ambient&&RenderSettings.ambientMode==mode&&RenderSettings.ambientProbe.Equals(probe)&&glass.GetColor("_EmissionColor")==emission&&glass.IsKeywordEnabled("_EMISSION")==emissive,"every render restores the original room lights, glass glow and ambient state");
                var snapshot=Snapshot.FromWireJson(PrivateSnapshot(Connection.LocalId).ToWireJson());snapshot.round=12;ApplySnapshot(snapshot);
                Check(Mathf.Abs(BarLighting.TargetDusk-11/19f)<.0001f,"real snapshot round drives the client lighting target");
                BarLighting.SetRound(1,true);BarLighting.SetRound(20);
                yield return new WaitForSeconds(.1f);
                Check(BarLighting.Dusk>0&&BarLighting.Dusk<1,"round changes blend smoothly rather than flashing to the next profile");
                BarLighting.SetRound(1);
                Check(BarLighting.Dusk==0&&BarLighting.TargetDusk==0,"a new match resets sunset immediately");
            }
            finally
            {
                Connection.SnapshotReceived+=ApplySnapshot;BarLighting.enabled=true;BarLighting.SetRound(View?.round??1,true);
                ViewCamera.transform.SetPositionAndRotation(position,rotation);ViewCamera.fieldOfView=fov;
            }
            VerificationFinished=VerificationPassed=true;Debug.Log("SALOON_BAR_LIGHTING PASS "+checks+" checks; previews="+folder);
        }
    }
}
#endif
