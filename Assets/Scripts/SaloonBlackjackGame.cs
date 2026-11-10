using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        const float BlackjackChoiceSeconds=20,BlackjackDealSeconds=1.5f,BlackjackDealerSeconds=6;
        bool TableOpening(Participant p)=>p.Alive&&phase==Phase.Turns&&duels.TryGetValue(p.room,out var d)&&(d.blackjack!=null||p.room==1)&&d.revealed&&d.first==p.id&&Time.time<d.firstUntil&&rounds.CanAct(p.id,Time.time);
        bool TableLocksSeat(Participant p)=>phase==Phase.Turns&&duels.TryGetValue(p.room,out var d)&&(d.blackjack!=null||p.room==1)&&!d.revealed;
        void DealBlackjack(Duel d)
        {
            d.blackjack=new SaloonBlackjack(new System.Random(UnityEngine.Random.Range(0,int.MaxValue)));
            d.revealed=d.flipping=false;d.first=0;d.firstUntil=0;
            foreach(ulong id in new[]{d.a,d.b}){players[id].pitch=26;players[id].poseVersion++;players[id].fireAfterDraw=false;}
            d.handNumber++;d.readyAt=Time.time+BlackjackDealSeconds;
            d.choiceUntil=d.readyAt+BlackjackChoiceSeconds;d.nextCardAt=d.readyAt+BlackjackDealerSeconds;d.nextInputAt=d.readyAt;
            d.revealAt=0;d.result="";
        }
        void PlayBlackjack(Participant p,string action)
        {
            if(phase!=Phase.Turns||!p.Alive||!duels.TryGetValue(p.room,out var d)||d.blackjack==null||d.revealed||d.blackjack.Complete||
                (p.id!=d.a&&p.id!=d.b)||Time.time<d.readyAt||Time.time<d.nextInputAt)return;
            var hand=d.blackjack;
            if(action=="deal")
            {
                if(p.id!=d.dealer||hand.PlayerTurn)return;
                hand.DealerStep();d.nextCardAt=Time.time+BlackjackDealerSeconds;
            }
            else
            {
                if(p.id==d.dealer||!hand.PlayerTurn||Time.time>=d.choiceUntil||(action!="hit"&&action!="stand"))return;
                if(action=="hit")hand.Hit();else hand.Stand();
                d.nextCardAt=Time.time+BlackjackDealerSeconds;
            }
            d.nextInputAt=Time.time+.65f;
            if(!hand.Complete&&!hand.PlayerTurn&&hand.DealerTotal>=17)hand.DealerStep();
            ResolveBlackjack(d);
        }
        void ResolveBlackjack(Duel d)
        {
            var hand=d.blackjack;
            if(d.revealed||!hand.Complete||Time.time<d.readyAt)return;
            if(hand.Winner==0)
            {
                if(d.revealAt==0)d.revealAt=Time.time+3;
                if(Time.time>=d.revealAt)DealBlackjack(d);
                return;
            }
            ulong player=d.dealer==d.a?d.b:d.a;
            RevealBlackjack(d,hand.Winner>0?player:d.dealer,
                hand.PlayerTotal>21?"Player busts":hand.DealerTotal>21?"Dealer busts":
                hand.Player.Count==2&&hand.PlayerTotal==21?"Player blackjack":
                hand.Dealer.Count==2&&hand.DealerTotal==21?"Dealer blackjack":"Hand won");
        }
        void TickBlackjack(Duel d)
        {
            if(d.revealed)return;
            rounds.EnsureDeadline(Time.time+Mathf.Max(10,tuning.turnSeconds));
            ulong player=d.dealer==d.a?d.b:d.a;
            if(!players[d.a].Alive||!players[d.b].Alive)
            {RevealBlackjack(d,players[d.a].Alive?d.a:d.b,"Opponent left the table");return;}
            if(Time.time<d.readyAt)return;
            var hand=d.blackjack;
            if(hand.Complete){ResolveBlackjack(d);return;}
            if(hand.PlayerTurn)
            {
                if(Time.time>=d.choiceUntil){hand.Stand();d.nextCardAt=Time.time+BlackjackDealerSeconds;}
                else if(!verifying&&players[player].bot&&Time.time>=d.nextInputAt)
                {PlayBlackjack(players[player],hand.PlayerTotal<17?"hit":"stand");d.nextInputAt=Time.time+1.2f;}
            }
            else if(hand.DealerTotal>=17)hand.DealerStep();
            else if((!verifying&&players[d.dealer].bot&&Time.time>=d.nextInputAt+1)||Time.time>=d.nextCardAt)
            {hand.DealerStep();d.nextInputAt=Time.time+.65f;d.nextCardAt=Time.time+BlackjackDealerSeconds;}
            ResolveBlackjack(d);
        }
        void RevealBlackjack(Duel d,ulong winner,string reason)
        {
            d.first=winner;d.revealed=true;d.flipping=true;
            d.firstUntil=Time.time+Mathf.Min(6,tuning.turnSeconds*.4f);
            d.result=reason+" · "+players[winner].name+" opens";
            foreach(ulong id in new[]{d.a,d.b})
            {
                var p=players[id];p.pitch=0;p.poseVersion++;p.lastAction=-100;
                p.notice=id==winner?"You won blackjack. Q to draw, click to shoot, or Space to pass.":players[winner].name+" won blackjack and has the opening.";
            }
            Broadcast();
        }
        DuelView PrivateBlackjack(Participant p,Duel d,bool opening)
        {
            var hand=d.blackjack;bool hidden=hand.PlayerTurn&&!d.revealed;
            ulong player=d.dealer==d.a?d.b:d.a;
            var dealerHand=hand.Dealer.Select((card,index)=>hidden&&index==1?-1:card).ToArray();
            string Cards(System.Collections.Generic.IEnumerable<int> cards)=>string.Join("  ",cards.Select(card=>card<0?"[hidden]":SaloonBlackjack.Label(card)));
            return new DuelView{game="blackjack",stage=d.revealed?"playing":hand.Complete&&hand.Winner==0?"redealing":"blackjack",
                opponent=players[p.id==d.a?d.b:d.a].name,isDealer=p.id==d.dealer,
                dealerId=d.dealer.ToString(),playerId=player.ToString(),handNumber=d.handNumber,
                dealerName=players[d.dealer].name,playerName=players[player].name,
                // Only the visible card IDs travel over the network; the hole card is -1.
                dealerHand=dealerHand,playerHand=hand.Player.ToArray(),dealerCards=Cards(dealerHand),playerCards=Cards(hand.Player),
                playerTotal=hand.PlayerTotal,dealerTotal=hidden?-1:hand.DealerTotal,
                canHit=!d.revealed&&hand.PlayerTurn&&p.id==player&&Time.time>=d.readyAt,
                canDeal=!d.revealed&&!hand.Complete&&!hand.PlayerTurn&&hand.DealerTotal<17&&p.id==d.dealer&&Time.time>=d.readyAt,
                firstId=d.revealed?d.first.ToString():"",firstName=d.revealed?players[d.first].name:"",result=d.result,
                yourTurn=d.revealed&&DuelAllows(p)&&rounds.CanAct(p.id,Time.time),waitingForOpening=opening&&p.id!=d.first,
                remaining=Mathf.Max(0,(d.revealed?(opening?d.firstUntil:rounds.Deadline):hand.Complete?d.revealAt:hand.PlayerTurn?d.choiceUntil:d.nextCardAt)-Time.time)};
        }
        DuelView PublicBlackjackTable()
        {
            if(phase!=Phase.Turns||!duels.TryGetValue(2,out var d)||d.blackjack==null)return null;
            var table=PrivateBlackjack(players[d.a],d,false);
            table.canHit=table.canDeal=table.yourTurn=table.waitingForOpening=table.isDealer=false;
            table.opponent="";return table;
        }
        public void ChooseBlackjack(string action)=>Connection.Send(new Command{kind="blackjack",name=action});
    }
}
