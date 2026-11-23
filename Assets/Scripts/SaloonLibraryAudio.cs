using UnityEngine;

namespace Saloon
{
    // CC0 recordings and provenance live in Resources/Audio/Recorded.
    public static class SaloonLibraryAudio
    {
        static AudioClip reload,drinking,revolverShot;
        public static AudioClip RevolverShot=>revolverShot!=null?revolverShot:revolverShot=Load("RevolverShot");
        public static AudioClip Reload=>reload!=null?reload:reload=Load("Reload");
        public static AudioClip Drinking=>drinking!=null?drinking:drinking=Load("Drinking");
        static AudioClip Load(string name)=>Resources.Load<AudioClip>("Audio/Recorded/"+name)
            ??throw new System.InvalidOperationException("Missing recorded audio asset: Audio/Recorded/"+name);
    }
}
