using System;
using System.Linq;
using Saloon;

static class BluffDiceVerification
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static void Main()
    {
        for(int a=1;a<=6;a++)for(int b=1;b<=6;b++)for(int c=1;c<=6;c++)
        {
            var h=new[]{a,b,c};int sum=a+b+c,evens=(a%2==0?1:0)+(b%2==0?1:0)+(c%2==0?1:0);
            bool[] expected={a==b||b==c||a==c,a==6||b==6||c==6,(a==6?1:0)+(b==6?1:0)+(c==6?1:0)>=2,sum>=11,a!=b&&b!=c&&a!=c,a==1||b==1||c==1,evens>=2,evens<=1,a==5||b==5||c==5,sum<=9,a==4||b==4||c==4,a==b&&b==c};
            for(int claim=0;claim<expected.Length;claim++)Check(SaloonBluffDice.Evaluate(claim,h)==expected[claim],"every possible roll evaluates each claim correctly");
        }
        int trueClaims=0,speakerA=0;var seen=new bool[SaloonBluffDice.Claims.Length];
        for(int seed=0;seed<5000;seed++)
        {
            var game=new SaloonBluffDice(new Random(seed));seen[game.ClaimId]=true;
            if(game.Truth)trueClaims++;if(game.Speaker==0)speakerA++;
            Check(game.Hand().Length==3&&game.Hand().All(n=>n>=1&&n<=6)&&game.Winner==-1,"one secret three-dice roll");
            var copy=game.Hand();copy[0]=99;Check(game.Hand()[0]!=99,"private snapshot copies never mutate host dice");
            foreach(bool believe in new[]{false,true})
            {
                var trial=new SaloonBluffDice(new Random(seed));
                Check(trial.Judge(believe)&&trial.Complete&&trial.Winner==(believe==trial.Truth?trial.Listener:trial.Speaker),"correct judgment wins listener opening; wrong judgment wins speaker opening");
                int winner=trial.Winner;Check(!trial.Judge(!believe)&&trial.Winner==winner&&trial.Revision==1,"judgment resolves only once");
                trial.Forfeit(winner);Check(trial.Winner==winner,"late forfeit cannot replace winner");
            }
            foreach(int loser in new[]{0,1})
            {var trial=new SaloonBluffDice(new Random(seed));trial.Forfeit(loser);Check(trial.Winner==1-loser&&!trial.Believed.HasValue,"forfeit preserves absent judgment");}
        }
        Check(trueClaims>2250&&trueClaims<2750,"true and false claims remain balanced");
        Check(speakerA>2250&&speakerA<2750,"speaker role is fair");Check(seen.All(n=>n),"all spoken claims occur");
        Console.WriteLine("PASS: "+checks+" Bluff Dice rules checks.");
    }
}
