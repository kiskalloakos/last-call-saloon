using UnityEngine;

namespace Saloon
{
    // Original alpha cue: sharp ignition, descending body, metal and short room reflections.
    public static class SaloonGunAudio
    {
        static AudioClip shot,reload;
        public static AudioClip Reload=>reload!=null?reload:reload=CreateReload();
        static AudioClip CreateReload()
        {
            const int rate=22050;var data=new float[rate*3];var random=new System.Random(871);
            // Cylinder latch, ejector, six cartridge clicks, then the cylinder locks shut.
            foreach(float at in new[]{.02f,.34f,.70f,.98f,1.26f,1.54f,1.82f,2.10f,2.65f})
            {
                int start=(int)(at*rate);
                for(int i=0;i<rate*.18f&&start+i<data.Length;i++)
                {
                    float t=i/(float)rate,n=(float)random.NextDouble()*2-1;
                    data[start+i]+=(n*.25f+Mathf.Sin(2*Mathf.PI*1650*t)*.18f+Mathf.Sin(2*Mathf.PI*890*t)*.12f)*Mathf.Exp(-t*42);
                }
            }
            var clip=AudioClip.Create("Revolver cylinder and cartridge reload",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        public static AudioClip Shot=>shot!=null?shot:shot=Create();
        static AudioClip Create()
        {
            const int rate=44100;var dry=new float[(int)(rate*.8f)];var output=new float[dry.Length];
            var random=new System.Random(944);float low=0,previous=0,peak=0;
            for(int i=0;i<dry.Length;i++)
            {
                float t=i/(float)rate,n=(float)random.NextDouble()*2-1;
                low=Mathf.Lerp(low,n,.16f);float high=n-previous;previous=n;
                float crack=high*Mathf.Exp(-t*210)*.7f;
                float body=(low*.8f+Mathf.Sin(2*Mathf.PI*(125*t-42*t*t))*.55f)*Mathf.Exp(-t*26);
                float blast=n*Mathf.Exp(-t*48)*.42f;
                float metal=(Mathf.Sin(2*Mathf.PI*1830*t)+Mathf.Sin(2*Mathf.PI*2710*t))*.045f*Mathf.Exp(-t*19);
                dry[i]=crack+body+blast+metal;
            }
            var echoes=new[]{new Vector2(.057f,.20f),new Vector2(.109f,.12f),new Vector2(.177f,.06f)};
            for(int i=0;i<output.Length;i++)
            {
                float sample=dry[i];
                foreach(var echo in echoes)
                {int source=i-(int)(echo.x*rate);if(source>=0)sample+=dry[source]*echo.y;}
                output[i]=sample;peak=Mathf.Max(peak,Mathf.Abs(sample));
            }
            for(int i=0;i<output.Length;i++)output[i]=output[i]/Mathf.Max(.01f,peak)*.94f;
            shot=AudioClip.Create("Saloon revolver — crack, body and room",output.Length,1,rate,false);shot.SetData(output,0);return shot;
        }
    }
}
