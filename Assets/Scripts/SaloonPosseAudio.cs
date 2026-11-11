using UnityEngine;

namespace Saloon
{
    // Local ambience follows the host's public snapshot; dropped snapshots cannot lose the warning.
    public sealed class SaloonPosseAudio : MonoBehaviour
    {
        public SaloonGame Game;
        AudioSource horses,shots;
        AudioLowPassFilter distanceFilter;
        int stage;
        float nextShot;
        void Awake()
        {
            horses=gameObject.AddComponent<AudioSource>();horses.playOnAwake=false;horses.loop=true;horses.spatialBlend=0;horses.volume=0;
            shots=gameObject.AddComponent<AudioSource>();shots.playOnAwake=false;shots.spatialBlend=0;
            distanceFilter=gameObject.AddComponent<AudioLowPassFilter>();distanceFilter.cutoffFrequency=6500;distanceFilter.lowpassResonanceQ=1;
        }
        void Update()
        {
            var view=Game!=null?Game.View:null;
            int current=view==null||view.phase==Phase.Lobby||view.phase==Phase.Finished?0:Mathf.Clamp(view.posseStage,0,4);
            if(current==0){horses.Stop();shots.Stop();stage=0;return;}
            if(current!=stage)
            {
                stage=current;
                if(!horses.isPlaying)
                {horses.clip=SaloonLibraryAudio.Hoofbeats;horses.pitch=1;horses.volume=0;horses.Play();}
                nextShot=Time.unscaledTime+2;
            }
            horses.volume=Mathf.MoveTowards(horses.volume,.025f+stage*.035f,Time.unscaledDeltaTime*.08f);
            distanceFilter.cutoffFrequency=Mathf.Lerp(distanceFilter.cutoffFrequency,Mathf.Lerp(1500,6500,(stage-1)/3f),Time.unscaledDeltaTime*2);
            if(Time.unscaledTime>=nextShot)
            {
                shots.PlayOneShot(SaloonLibraryAudio.DistantShot,.10f+stage*.025f);
                nextShot=Time.unscaledTime+Mathf.Max(7,20-stage*3);
            }
        }
        void OnDisable()
        {
            if(horses!=null)horses.Stop();if(shots!=null)shots.Stop();stage=0;
        }
    }
}
