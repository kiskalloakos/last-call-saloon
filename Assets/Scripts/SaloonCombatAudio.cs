using UnityEngine;

namespace Saloon
{
    public static class SaloonCombatAudio
    {
        static AudioClip mechanism,thud;
        public static AudioClip Mechanism=>mechanism!=null?mechanism:mechanism=Resources.Load<AudioClip>("Audio/Combat/impactMetal_light_003");
        public static AudioClip Thud=>thud!=null?thud:thud=Resources.Load<AudioClip>("Audio/Footsteps/footstep_wood_004");

        public static void Play(Transform owner,Vector3 position,AudioClip clip,float volume,float pitch=1,float delay=0,bool local=false)
        {
            if(clip==null)return;
            var sound=new GameObject("Combat sound");sound.layer=SaloonSmokingArea.IndoorLayer;
            sound.transform.SetParent(owner);sound.transform.position=position;
            var source=sound.AddComponent<AudioSource>();source.playOnAwake=false;
            source.clip=clip;source.volume=volume;source.pitch=pitch;source.spatialBlend=local?0:1;
            source.minDistance=1;source.maxDistance=24;source.rolloffMode=AudioRolloffMode.Linear;
            source.PlayDelayed(delay);Object.Destroy(sound,delay+clip.length/Mathf.Max(.1f,pitch)+.1f);
        }
    }
}
