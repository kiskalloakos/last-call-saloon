using System;
using System.Collections.Generic;
using System.Linq;

namespace Saloon
{
    // Host-owned single-deck blackjack. No betting, splits, insurance or doubles.
    public sealed class SaloonBlackjack
    {
        readonly Stack<int> deck;
        readonly List<int> player=new List<int>(),dealer=new List<int>();
        public IReadOnlyList<int> Player=>player;
        public IReadOnlyList<int> Dealer=>dealer;
        public bool PlayerTurn { get; private set; }=true;
        public bool Complete { get; private set; }
        // 1 = player wins, -1 = dealer wins, 0 = push.
        public int Winner { get; private set; }
        public int PlayerHighCard { get; private set; }=-1;
        public int DealerHighCard { get; private set; }=-1;
        public int PlayerTotal=>Total(player);
        public int DealerTotal=>Total(dealer);
        public SaloonBlackjack(Random random)
        {
            var cards=Enumerable.Range(0,52).ToArray();
            for(int i=cards.Length-1;i>0;i--){int j=random.Next(i+1);int card=cards[i];cards[i]=cards[j];cards[j]=card;}
            deck=new Stack<int>(cards);
            player.Add(deck.Pop());dealer.Add(deck.Pop());player.Add(deck.Pop());dealer.Add(deck.Pop());
            bool pn=PlayerTotal==21,dn=DealerTotal==21;
            if(pn||dn)Finish(pn==dn?0:pn?1:-1);
        }
        public static int Total(IEnumerable<int> cards)
        {
            int total=0,aces=0;
            foreach(int card in cards){int rank=card%13+1;if(rank==1){aces++;total+=11;}else total+=Math.Min(rank,10);}
            while(total>21&&aces-->0)total-=10;
            return total;
        }
        public static string Label(int card)
        {int rank=card%13+1;return (rank==1?"A":rank==11?"J":rank==12?"Q":rank==13?"K":rank.ToString())+new[]{"C","D","H","S"}[card/13];}
        public static int HighCardRank(int card)=>card%13==0?14:card%13+1;
        public void BreakTie()
        {
            if(!Complete||Winner!=0)return;
            // Keep the blackjack hands intact. Equal ranks redraw only this pair;
            // suits never decide the opening and neither seat needs to act.
            do{PlayerHighCard=deck.Pop();DealerHighCard=deck.Pop();}
            while(HighCardRank(PlayerHighCard)==HighCardRank(DealerHighCard));
            Finish(HighCardRank(PlayerHighCard)>HighCardRank(DealerHighCard)?1:-1);
        }
        public void Hit()
        {
            if(Complete||!PlayerTurn)return;
            player.Add(deck.Pop());
            if(PlayerTotal>21)Finish(-1);else if(PlayerTotal==21)Stand();
        }
        public void Stand(){if(!Complete)PlayerTurn=false;}
        public void DealerStep()
        {
            if(Complete||PlayerTurn)return;
            if(DealerTotal<17)dealer.Add(deck.Pop());
            if(DealerTotal>=17)Finish(DealerTotal>21?1:PlayerTotal==DealerTotal?0:PlayerTotal>DealerTotal?1:-1);
        }
        void Finish(int winner){Winner=winner;Complete=true;PlayerTurn=false;}
    }
}
