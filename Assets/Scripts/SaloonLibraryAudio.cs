using UnityEngine;

namespace Saloon
{
    // CC0 recordings and provenance live in Resources/Audio/Recorded.
    public static class SaloonLibraryAudio
    {
        static AudioClip reload,drinking,hoofbeats,distantShot,revolverShot;
        public static AudioClip RevolverShot=>revolverShot!=null?revolverShot:revolverShot=Load("RevolverShot");
        public static AudioClip Reload=>reload!=null?reload:reload=Load("Reload");
        public static AudioClip Drinking=>drinking!=null?drinking:drinking=Load("Drinking");
        public static AudioClip Hoofbeats=>hoofbeats!=null?hoofbeats:hoofbeats=Load("Hoofbeats");
        public static AudioClip DistantShot=>distantShot!=null?distantShot:distantShot=Load("DistantShot");
        static AudioClip Load(string name)=>Resources.Load<AudioClip>("Audio/Recorded/"+name)
            ??throw new System.InvalidOperationException("Missing recorded audio asset: Audio/Recorded/"+name);
    }
}
