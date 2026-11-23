using System;
using System.Linq;

namespace Saloon
{
    public static class SaloonBlackjackVerification
    {
        public static void Run()
        {
            void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Blackjack: "+message);}
            Check(SaloonBlackjack.Total(new[]{0,12})==21,"ace and face card");
            Check(SaloonBlackjack.Total(new[]{0,13,8})==21,"multiple aces soften");
            Check(SaloonBlackjack.Total(new[]{0,13,26,9})==13,"three aces soften");
            Check(SaloonBlackjack.Total(new[]{9,10,11})==30,"bust total remains above 21");
            Check(SaloonBlackjack.HighCardRank(0)>SaloonBlackjack.HighCardRank(12)&&SaloonBlackjack.HighCardRank(12)>SaloonBlackjack.HighCardRank(11)&&SaloonBlackjack.HighCardRank(11)>SaloonBlackjack.HighCardRank(10),"high card uses ace, king, queen, jack order");
            Check(SaloonBlackjack.HighCardRank(0)==SaloonBlackjack.HighCardRank(39),"suits do not break high-card ties");
            var equalRanks=new SaloonBlackjack(new Random(285));equalRanks.Stand();while(!equalRanks.Complete)equalRanks.DealerStep();
            Check(equalRanks.Winner==0,"equal-rank redraw fixture starts with a tied blackjack hand");
            equalRanks.BreakTie();
            Check(equalRanks.PlayerHighCard==27&&equalRanks.DealerHighCard==49&&equalRanks.Winner==-1,"equal fours redraw only the high-card pair, then jack beats two");
            int playerWins=0,dealerWins=0,pushes=0,naturals=0;
            for(int seed=0;seed<10000;seed++)
            {
                var h=new SaloonBlackjack(new Random(seed));
                if(h.Complete)
                {
                    naturals++;
                    Check(h.PlayerTotal==21||h.DealerTotal==21,"only naturals resolve at deal");
                    Check(h.Winner==(h.PlayerTotal==h.DealerTotal?0:h.PlayerTotal==21?1:-1),"natural beats any non-natural");
                }
                int steps=0;
                while(!h.Complete&&steps++<52)
                {
                    if(h.PlayerTurn){if(h.PlayerTotal<17)h.Hit();else h.Stand();}
                    else h.DealerStep();
                }
                Check(h.Complete,"every hand terminates");
                Check(h.Player.Concat(h.Dealer).Distinct().Count()==h.Player.Count+h.Dealer.Count,"single deck has no duplicate cards");
                if(h.PlayerTotal>21)Check(h.Winner==-1,"player bust loses");
                else if(h.DealerTotal>21)Check(h.Winner==1,"dealer bust loses");
                else if(h.Player.Count!=2||h.PlayerTotal!=21)
                    Check(h.Winner==(h.PlayerTotal==h.DealerTotal?0:h.PlayerTotal>h.DealerTotal?1:-1),"higher non-bust total wins");
                if(h.Winner>0)playerWins++;else if(h.Winner<0)dealerWins++;else pushes++;
                int playerTotal=h.PlayerTotal,dealerTotal=h.DealerTotal;
                int[] original=h.Player.Concat(h.Dealer).ToArray();
                bool tied=h.Winner==0;
                h.BreakTie();
                if(tied)
                {
                    Check(h.Complete&&!h.PlayerTurn&&h.Winner!=0,"push automatically produces a single winner");
                    Check(h.PlayerHighCard>=0&&h.DealerHighCard>=0&&h.PlayerHighCard!=h.DealerHighCard&&!original.Contains(h.PlayerHighCard)&&!original.Contains(h.DealerHighCard),"high cards come from the remaining deck");
                    Check(h.PlayerTotal==playerTotal&&h.DealerTotal==dealerTotal&&h.Player.Count+h.Dealer.Count==original.Length,"tiebreak preserves the original hands and totals");
                    Check(h.Winner==(SaloonBlackjack.HighCardRank(h.PlayerHighCard)>SaloonBlackjack.HighCardRank(h.DealerHighCard)?1:-1)&&SaloonBlackjack.HighCardRank(h.PlayerHighCard)!=SaloonBlackjack.HighCardRank(h.DealerHighCard),"high rank decides the opening with equal ranks redrawn");
                }
                else Check(h.PlayerHighCard==-1&&h.DealerHighCard==-1,"decisive blackjack hands need no high cards");
                int count=h.Player.Count+h.Dealer.Count,winner=h.Winner;
                int playerHigh=h.PlayerHighCard,dealerHigh=h.DealerHighCard;
                h.Hit();h.Stand();h.DealerStep();h.BreakTie();
                Check(count==h.Player.Count+h.Dealer.Count&&winner==h.Winner&&playerHigh==h.PlayerHighCard&&dealerHigh==h.DealerHighCard,"resolved hand ignores further moves and tiebreak attempts");
            }
            Check(playerWins>0&&dealerWins>0&&pushes>0&&naturals>0,"all outcome paths covered");
        }
    }
}
