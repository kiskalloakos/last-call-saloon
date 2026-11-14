using System;

namespace Saloon
{
    public static class SaloonBartenderMood
    {
        public const int LossPerShot=10;
        public static int FromShots(int shots)=>100-Math.Min(10,Math.Max(0,shots))*LossPerShot;
        public static string Label(int mood)=>mood>=80?"CALM":mood>=50?"IRRITATED":mood>0?"ANGRY":"FED UP";
        // A useless answer is independent of the target's role. Useful answers never lie.
        public static string Answer(Role target,int mood,float roll)
        {
            mood=Math.Max(0,Math.Min(100,mood));
            if(mood==0||(mood<100&&roll>=mood/100f))return "Maybe";
            return target==Role.Outlaw?"Yes":"No";
        }
    }
}
