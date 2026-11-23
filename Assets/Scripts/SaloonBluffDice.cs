using System;
using System.Linq;

namespace Saloon
{
    // One speaker, three secret dice and one truth judgment. Claims are balanced true/false.
    public sealed class SaloonBluffDice
    {
        readonly int[] dice;
        public int Speaker { get; }
        public int Listener=>1-Speaker;
        public int ClaimId { get; }
        public string Claim=>Claims[ClaimId];
        public bool Truth=>Evaluate(ClaimId,dice);
        public int Winner { get; private set; }=-1;
        public int Revision { get; private set; }
        public bool? Believed { get; private set; }
        public bool Complete=>Winner>=0;
        public static readonly string[] Claims={"At least two of my dice match.","I have at least one six.","I have at least two sixes.","My dice add up to 11 or more.","All three of my dice are different.","I have at least one one.","At least two of my dice are even.","At least two of my dice are odd.","I have at least one five.","My dice add up to 9 or less.","I have at least one four.","All three of my dice match."};
        public SaloonBluffDice(Random random,int speaker=-1)
        {
            dice=Enumerable.Range(0,3).Select(_=>random.Next(1,7)).ToArray();Speaker=speaker<0?random.Next(2):speaker;
            bool truth=random.Next(2)==0;
            var candidates=Enumerable.Range(0,Claims.Length).Where(id=>Evaluate(id,dice)==truth).ToArray();ClaimId=candidates[random.Next(candidates.Length)];
        }
        public int[] Hand()=>(int[])dice.Clone();
        public static bool Evaluate(int claim,int[] hand)
        {
            switch(claim)
            {
                case 0:return hand.Distinct().Count()<3;
                case 1:return hand.Contains(6);
                case 2:return hand.Count(n=>n==6)>=2;
                case 3:return hand.Sum()>=11;
                case 4:return hand.Distinct().Count()==3;
                case 5:return hand.Contains(1);
                case 6:return hand.Count(n=>n%2==0)>=2;
                case 7:return hand.Count(n=>n%2==1)>=2;
                case 8:return hand.Contains(5);
                case 9:return hand.Sum()<=9;
                case 10:return hand.Contains(4);
                case 11:return hand.Distinct().Count()==1;
                default:throw new ArgumentOutOfRangeException(nameof(claim));
            }
        }
        public bool Judge(bool believe)
        {
            if(Complete)return false;
            Believed=believe;Winner=believe==Truth?Listener:Speaker;Revision++;return true;
        }
        public void Forfeit(int loser){if(!Complete){Winner=1-loser;Revision++;}}
    }
}
