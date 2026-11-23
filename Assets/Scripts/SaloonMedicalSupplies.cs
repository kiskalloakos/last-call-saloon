using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        int medicalRoom,medicalRound;
        // After the quiet first round: 50% no event, 25% Coin Flip, 25% Blackjack.
        internal static int MedicalEventForRound(int round,float roll)=>round<2||roll<.5f?0:roll<.75f?1:2;
        void UpdateMedicalOpportunity()
        {
            if(TutorialActive||phase!=Phase.Discussion||medicalRound==rounds.Round)return;
            medicalRound=rounds.Round;medicalRoom=MedicalEventForRound(rounds.Round,Random.value);
        }
        bool MedicalChoice(Participant p)=>medicalRound>1&&p.room==medicalRoom&&medicalRound==rounds.Round&&TableOpening(p)&&
            !duels[p.room].medicalDeclined;
        void UseMedicalSupplies(Participant p)
        {
            if(!MedicalChoice(p)){p.notice="Only the featured room's winner can use supplies during their opening.";return;}
            if(p.hp>=tuning.health){KeepMedicalOpening(p);return;}
            if(!SpendAction(p))return;
            p.hp=Mathf.Min(tuning.health,p.hp+20);
            p.notice="Medical supplies: +20% health, capped at 100%. Action spent; your opponent may respond.";
        }
        void KeepMedicalOpening(Participant p)
        {
            if(!MedicalChoice(p))return;
            duels[p.room].medicalDeclined=true;
            p.notice="Opening kept. Supplies declined; use your action before the opening expires.";
        }
        public void ChooseMedicalSupplies(bool heal)=>Connection.Send(new Command{kind=heal?"medicalSupplies":"keepOpening"});
    }
}
