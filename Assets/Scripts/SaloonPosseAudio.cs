using UnityEngine;

namespace Saloon
{
    // Local ambience follows the host's public snapshot; dropped snapshots cannot lose the warning.
    public sealed class SaloonPosseAudio : MonoBehaviour
    {
        public SaloonGame Game;
        AudioSource horses,shots;
        AudioClip hoofbeats,distantShot;
        int stage;
        float nextShot;
        void Awake()
        {
            horses=gameObject.AddComponent<AudioSource>();horses.playOnAwake=false;horses.loop=true;horses.spatialBlend=0;horses.volume=0;
            shots=gameObject.AddComponent<AudioSource>();shots.playOnAwake=false;shots.spatialBlend=0;
        }
        void Update()
        {
            var view=Game!=null?Game.View:null;
            int current=view==null||view.phase==Phase.Lobby?0:view.posseStage;
            if(current==0){horses.Stop();shots.Stop();stage=0;return;}
            if(hoofbeats==null)hoofbeats=CreateHoofbeats();
            if(distantShot==null)distantShot=CreateDistantShot();
            if(current!=stage)
            {
                stage=current;horses.clip=hoofbeats;horses.pitch=.85f+stage*.07f;
                horses.volume=.035f+stage*.025f;horses.Play();nextShot=Time.unscaledTime+1;
            }
            if(view.phase==Phase.Finished){horses.Stop();shots.Stop();return;}
            if(Time.unscaledTime>=nextShot)
            {
                shots.PlayOneShot(distantShot,.08f+stage*.025f);
                nextShot=Time.unscaledTime+Mathf.Max(5,17-stage*3);
            }
        }
        static AudioClip CreateHoofbeats()
        {
            const int rate=22050;var data=new float[rate*2];var random=new System.Random(710);
            foreach(float beat in new[]{.02f,.18f,.32f,.51f,.88f,1.04f,1.18f,1.37f,1.76f,1.92f})
            {
                int start=(int)(beat*rate);
                for(int i=0;i<rate*.12f&&start+i<data.Length;i++)
                {
                    float t=i/(float)rate;
                    data[start+i]+=(Mathf.Sin(2*Mathf.PI*105*t)*.6f+((float)random.NextDouble()*2-1)*.3f)*Mathf.Exp(-t*45);
                }
            }
            var clip=AudioClip.Create("Approaching posse hoofbeats",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        static AudioClip CreateDistantShot()
        {
            var original=SaloonGunAudio.Shot;var data=new float[original.samples];original.GetData(data,0);float low=0;
            for(int i=0;i<data.Length;i++){low=Mathf.Lerp(low,data[i],.045f);data[i]=low;}
            var clip=AudioClip.Create("Posse distant warning shot",data.Length,1,original.frequency,false);clip.SetData(data,0);return clip;
        }
        void OnDestroy()
        {
            if(hoofbeats!=null)Destroy(hoofbeats);
            if(distantShot!=null)Destroy(distantShot);
        }
    }
}
