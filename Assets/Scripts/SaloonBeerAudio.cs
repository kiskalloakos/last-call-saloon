using UnityEngine;
namespace Saloon
{
    // Original lightweight procedural alpha sounds: no external samples or licensing dependencies.
    public static class SaloonBeerAudio
    {
        static AudioClip drinking,clink;
        public static AudioClip Drinking=>drinking!=null?drinking:drinking=Make(true);
        public static AudioClip Handoff=>clink!=null?clink:clink=Make(false);
        static AudioClip Make(bool gulp)
        {
            const int rate=22050;int length=(int)(rate*(gulp?2.5f:.35f));var data=new float[length];var random=new System.Random(gulp?819:820);float low=0;
            for(int i=0;i<length;i++)
            {
                float t=i/(float)rate;float noise=(float)random.NextDouble()*2-1;low=Mathf.Lerp(low,noise,.13f);
                if(gulp)
                {
                    float pulse=t%.65f;float envelope=Mathf.Sin(Mathf.Clamp01(pulse/.24f)*Mathf.PI)*Mathf.Exp(-pulse*6);
                    data[i]=(low*.7f+Mathf.Sin(2*Mathf.PI*(180*t-22*pulse*pulse))*.15f)*envelope;
                }
                else data[i]=(Mathf.Sin(t*2*Mathf.PI*1700)*.35f+Mathf.Sin(t*2*Mathf.PI*2780)*.2f+noise*.08f)*Mathf.Exp(-t*24);
            }
            var clip=AudioClip.Create(gulp?"Beer gulp":"Mug handoff",length,1,rate,false);clip.SetData(data,0);return clip;
        }
    }
}
