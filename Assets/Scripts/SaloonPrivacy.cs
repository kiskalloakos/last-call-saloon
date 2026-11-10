namespace Saloon
{
    public static class SaloonPrivacy
    {
        public static bool Separated(Phase phase)=>phase==Phase.Turns||phase==Phase.Aftermath;
        public static bool ShareActivity(Phase phase,int viewerRoom,int actorRoom)=>!Separated(phase)||(viewerRoom==3)==(actorRoom==3);
        public static string VoiceChannel(string match,Phase phase,int room,bool alive)=>match+(!alive?"-dead":Separated(phase)&&room==3?"-smoke":"-alive");
    }
}
