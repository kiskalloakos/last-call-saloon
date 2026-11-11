using UnityEngine;

namespace Saloon
{
    // Distance-based steps follow visible, replicated movement rather than key presses.
    public sealed class SaloonFootsteps : MonoBehaviour
    {
        AudioSource source;
        AudioClip[] wood;
        Vector3 previous;
        float distance;
        int step;
        bool tracking;
        public int StepsPlayed { get; private set; }
        public static bool CanStep(bool alive,bool seated,bool fresh,float distance)=>alive&&!seated&&fresh&&distance>.0001f&&distance<.65f;
        void Awake()
        {
            source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.minDistance=.7f;source.maxDistance=9;source.rolloffMode=AudioRolloffMode.Linear;
            wood=Resources.LoadAll<AudioClip>("Audio/Footsteps");
            if(wood.Length==0)throw new System.InvalidOperationException("Wooden footstep samples are missing.");
        }
        public void Tick(Vector3 position,bool alive,bool seated,bool fresh,bool local,bool outside)
        {
            float moved=tracking?Vector3.ProjectOnPlane(position-previous,Vector3.up).magnitude:0;
            previous=position;tracking=true;
            if(outside){distance=0;source.Stop();return;}
            if(!CanStep(alive,seated,fresh,moved)){distance=0;if(!alive||seated||!fresh||moved>=.65f)source.Stop();return;}
            distance+=moved;
            if(distance<.78f)return;distance%=.78f;
            source.spatialBlend=local?0:1;source.volume=local?.17f:.23f;source.pitch=.97f+(step%3)*.025f;
            source.PlayOneShot(wood[step++%wood.Length]);StepsPlayed++;
        }
        public static AudioClip Create(bool outdoors,int seed)
        {
            if(!outdoors)
            {
                var recorded=Resources.LoadAll<AudioClip>("Audio/Footsteps");
                if(recorded.Length==0)throw new System.InvalidOperationException("Wooden footstep samples are missing.");
                return Instantiate(recorded[Mathf.Abs(seed)%recorded.Length]);
            }
            const int rate=22050;var data=new float[(int)(rate*.32f)];var random=new System.Random(seed);
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)rate,n=(float)random.NextDouble()*2-1;
                float thud=Mathf.Sin(2*Mathf.PI*82*t)*Mathf.Exp(-t*36)*.40f;
                data[i]=(thud+n*.13f*Mathf.Exp(-t*22))*(1-Mathf.Exp(-t*700));
            }
            var clip=AudioClip.Create(outdoors?"Boot on dirt":"Boot on saloon planks",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
    }
}
