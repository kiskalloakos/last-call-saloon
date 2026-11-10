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
                int count=h.Player.Count+h.Dealer.Count,winner=h.Winner;
                h.Hit();h.Stand();h.DealerStep();
                Check(count==h.Player.Count+h.Dealer.Count&&winner==h.Winner,"resolved hand ignores further moves");
            }
            Check(playerWins>0&&dealerWins>0&&pushes>0&&naturals>0,"all outcome paths covered");
        }
    }
}
