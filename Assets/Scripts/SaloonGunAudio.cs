using UnityEngine;

namespace Saloon
{
    public static class SaloonGunAudio
    {
        public static AudioClip Reload=>SaloonLibraryAudio.Reload;
        public static AudioClip Shot=>SaloonLibraryAudio.RevolverShot;
        public static AudioClip Cock=>SaloonCombatAudio.Mechanism;
    }
}
