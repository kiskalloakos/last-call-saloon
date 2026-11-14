using System;
using System.Collections.Generic;
using System.Linq;

namespace Saloon
{
    // Seventeen normal rounds, then warnings during the final three full rounds.
    public static class SaloonPosse
    {
        public const int ArrivalRound=20,WarningRound=ArrivalRound-3,LastEscapeRound=ArrivalRound-1;
        public static int Stage(int round,Phase phase)
        {
            if(phase==Phase.Lobby||round<WarningRound)return 0;
            if(round>=ArrivalRound&&(phase==Phase.Aftermath||phase==Phase.Finished))return 4;
            if(phase==Phase.Finished)return 0;
            int next=round+(phase==Phase.Regroup?1:0);
            return Math.Max(0,Math.Min(3,next-WarningRound));
        }
        public static int RoundsRemaining(int round,Phase phase)
        {
            int stage=Stage(round,phase);
            return stage==0?0:Math.Max(0,4-stage);
        }
        public static bool Arrives(int round)=>round>=ArrivalRound;
        public static bool CanEscape(int round)=>round<ArrivalRound;
        public static string ArrivalOutcome(IEnumerable<Role> survivors)
        {
            var living=survivors.ToArray();
            string winner=SaloonRules.Winner(living);
            if(!string.IsNullOrEmpty(winner))return winner;
            return living.Any(r=>r==Role.Sheriff||r==Role.Deputy)?
                "LAW WINS — POSSE ARRIVED":"DRAW — POSSE CAPTURED THE SURVIVORS";
        }
        public static string Dialogue(int stage)
        {
            switch(stage)
            {
                case 1:return "\"Riders on the ridge! Three rounds till they surround us.\"";
                case 2:return "\"Hear those horses? Two rounds left.\"";
                case 3:return "\"They're at the edge of town. This is your last round!\"";
                case 4:return "\"The saloon is surrounded! Nobody else gets out.\"";
                default:return "";
            }
        }
    }
}
