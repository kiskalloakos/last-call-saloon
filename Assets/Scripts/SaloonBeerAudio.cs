using UnityEngine;
namespace Saloon
{
    // Drinking uses a CC0 recording; the existing short handoff cue is separate.
    public static class SaloonBeerAudio
    {
        static AudioClip clink;
        public static AudioClip Drinking=>SaloonLibraryAudio.Drinking;
        public static AudioClip Handoff=>clink!=null?clink:clink=MakeHandoff();
        static AudioClip MakeHandoff()
        {
            const int rate=22050;var data=new float[(int)(rate*.35f)];var random=new System.Random(820);
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)rate,noise=(float)random.NextDouble()*2-1;
                data[i]=(Mathf.Sin(t*2*Mathf.PI*1700)*.35f+Mathf.Sin(t*2*Mathf.PI*2780)*.2f+noise*.08f)*Mathf.Exp(-t*24);
            }
            var clip=AudioClip.Create("Mug handoff",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
    }
}
