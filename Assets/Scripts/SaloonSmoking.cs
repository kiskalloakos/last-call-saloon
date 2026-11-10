using UnityEngine;
namespace Saloon
{
    public static class SaloonSmoking
    {
        public const float Duration=4;
        public static float Lift(float age)=>age<0||age>Duration?0:Mathf.SmoothStep(0,1,Mathf.Min(age/.65f,(2.65f-age)/.75f));
        public static float Inhale(float age)=>age>=.65f&&age<=1.75f?Mathf.Sin((age-.65f)/1.1f*Mathf.PI):0;
        public static float Exhale(float age)=>age>=1.95f&&age<=3.7f?Mathf.Sin((age-1.95f)/1.75f*Mathf.PI):0;
    }
    public partial class SaloonGame
    {
        void TrySmoke(Participant p)
        {
            if(!p.Alive||p.room!=3||!SaloonPrivacy.Separated(phase)||Time.time-p.smokeAt<SaloonSmoking.Duration)return;
            p.drawn=false;p.fireAfterDraw=false;p.puffs++;p.smokeAt=Time.time;p.notice="";
        }
    }
}
