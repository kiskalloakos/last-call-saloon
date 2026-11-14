namespace Saloon
{
    public static class SaloonPrivacy
    {
        public static bool Separated(Phase phase)=>phase==Phase.Turns||phase==Phase.Aftermath;
        public static bool ShareActivity(Phase phase,int viewerRoom,int actorRoom)=>!Separated(phase)||(viewerRoom==3)==(actorRoom==3);
        public static bool SharedVoice(Phase phase)=>phase==Phase.Lobby||phase==Phase.Finished;
        public static bool VoiceEliminated(Phase phase,bool alive)=>!alive&&!SharedVoice(phase);
        public static bool VoicePaused(Phase phase,bool menuOpen,bool graphicsOpen)=>graphicsOpen||(menuOpen&&!SharedVoice(phase));
        public static string VoiceChannel(string match,Phase phase,int room,bool alive)=>match+(VoiceEliminated(phase,alive)?"-dead":Separated(phase)&&room==3?"-smoke":"-alive");
    }
}
