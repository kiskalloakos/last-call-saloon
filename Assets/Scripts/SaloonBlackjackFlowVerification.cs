using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void VerifyBlackjackFlow()
        {
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;throw new InvalidOperationException("Blackjack flow: "+text);}Debug.Log("SALOON_BLACKJACK_CHECK "+text);}
            SaloonBlackjackVerification.Run();
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);var outsider=players.Values.First(p=>p!=host&&p!=peer);
            foreach(var p in players.Values){p.hp=60;p.chosenRoom=0;p.companion=null;p.drawn=false;p.lastAction=-100;p.nextShot=0;p.busyUntil=0;}
            void Setup(int room)
            {
                foreach(var p in players.Values){p.lastAction=-100;p.busyUntil=0;p.drawn=false;p.fireAfterDraw=false;}
                host.chosenRoom=peer.chosenRoom=room;host.companion=peer.id;peer.companion=host.id;
                rounds.Start(players.Keys,Time.time,30,30,6);
                foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
            }
            Setup(1);Check(duels[1].blackjack==null&&PrivateDuel(host).stage=="choosing","West keeps coin flip");
            Setup(2);var d=duels[2];
            Check(d.blackjack!=null&&(d.dealer==host.id||d.dealer==peer.id),"East deals blackjack with one randomly assigned dealer");
            Check(d.readyAt-Time.time>=3&&d.choiceUntil-d.readyAt>=44.99f&&!PrivateDuel(host).canHit,"initial deal is paced before a full forty-five-second decision window");
            var dealer=players[d.dealer];var player=players[d.dealer==d.a?d.b:d.a];
            SaloonBlackjack Find(Func<SaloonBlackjack,bool> predicate)
            {
                for(int seed=0;seed<10000;seed++){var h=new SaloonBlackjack(new System.Random(seed));if(predicate(h))return h;}
                throw new InvalidOperationException("No test hand found");
            }
            d.blackjack=Find(h=>!h.Complete&&h.PlayerTotal<10);d.nextInputAt=d.readyAt=0;
            var view=PrivateDuel(player);var wire=Snapshot.FromWireJson(PrivateSnapshot(player.id).ToWireJson());
            Check(view.canHit&&!PrivateDuel(dealer).canHit&&view.dealerTotal==-1&&view.dealerCards.Contains("[hidden]"),"only player can act; hole card is hidden");
            Check(wire.duel.dealerHand[1]==-1&&wire.duel.playerHand.Length==2&&wire.duel.game=="blackjack"&&wire.duel.canHit&&wire.duel.dealerTotal==-1&&PrivateDuel(outsider)==null,"private blackjack state survives network serialization");
            var seatedPosition=player.body.transform.position;
            OnCommand(player.id,new Command{kind="stance"});Stand(dealer);TickBotMovement(dealer);
            Check(player.seat>=0&&dealer.seat>=0&&player.body.transform.position==seatedPosition,"both players stay seated through the hand");
            int cards=d.blackjack.Player.Count;
            OnCommand(dealer.id,new Command{kind="blackjack",name="hit"});OnCommand(outsider.id,new Command{kind="blackjack",name="hit"});
            OnCommand(player.id,new Command{kind="coin",x=0});OnCommand(player.id,new Command{kind="blackjack",name="invalid"});
            Check(d.blackjack.Player.Count==cards&&!d.flipping,"dealer, outsider, coin and invalid commands rejected");
            OnCommand(player.id,new Command{kind="draw"});
            Check(!player.drawn&&!DuelAllows(player)&&!DuelAllows(dealer)&&rounds.CanAct(player.id,Time.time),"combat locked without consuming an action");
            OnCommand(player.id,new Command{kind="blackjack",name="hit"});
            Check(d.blackjack.Player.Count==cards+1,"player hit deals one card");
            Check(d.readyAt-Time.time>1.4f&&d.choiceUntil-d.readyAt>=44.99f&&!PrivateDuel(player).canHit,"each hit waits for the card and renews the decision time");
            OnCommand(player.id,new Command{kind="blackjack",name="hit"});
            Check(d.blackjack.Player.Count==cards+1,"duplicate rapid input is throttled");
            d.readyAt=d.nextInputAt=0;d.blackjack=Find(h=>!h.Complete);d.choiceUntil=Time.time-1;TickBlackjack(d);
            Check(!d.blackjack.PlayerTurn,"idle player automatically stands");
            Check(PrivateDuel(player).dealerTotal>=0,"hole card revealed after stand");
            for(int i=0;i<30&&!d.blackjack.Complete;i++){d.readyAt=d.nextCardAt=0;TickBlackjack(d);}
            Check(d.blackjack.Complete,"dealer draws to 17 and finishes");
            d.readyAt=0;d.blackjack=Find(h=>h.Complete&&h.Winner==1);d.revealed=false;TickBlackjack(d);
            Check(d.revealed&&d.first==player.id&&PrivateDuel(player).stage=="result"&&!DuelAllows(player)&&!DuelAllows(dealer),"winner is shown during the reaction pause with weapons locked for both players");
            Check(d.combatAt-Time.time>=3.9f&&d.firstUntil-d.combatAt>=9.99f&&!TableLocksSeat(dealer),"result gives four seconds to prepare and a separate ten-second opening (delay="+(d.combatAt-Time.time)+", opening="+(d.firstUntil-d.combatAt)+", locked="+TableLocksSeat(dealer)+")");
            player.drawn=false;player.busyUntil=0;player.lastAction=-100;
            OnCommand(player.id,new Command{kind="draw"});OnCommand(player.id,new Command{kind="fire"});OnCommand(player.id,new Command{kind="pass"});
            Check(!player.drawn&&!player.fireAfterDraw&&rounds.CanAct(player.id,Time.time),"draw, fire and pass cannot bypass the result pause or spend the winner's action");
            d.combatAt=Time.time-1;
            Check(DuelAllows(player)&&!DuelAllows(dealer),"blackjack winner receives exclusive opening after the result pause");
            player.role=Role.Sheriff;dealer.role=Role.Outlaw;player.drawn=false;player.busyUntil=0;player.nextShot=0;player.ammo=6;player.lastAction=-100;
            var aim=Eye(dealer)-Eye(player);player.yaw=Quaternion.LookRotation(aim).eulerAngles.y;player.pitch=0;
            dealer.lastAction=-100;OnCommand(dealer.id,new Command{kind="draw"});
            Check(!dealer.drawn,"blackjack loser cannot draw before the opening is used");
            int shots=player.shots,hp=dealer.hp;
            OnCommand(player.id,new Command{kind="draw"});OnCommand(player.id,new Command{kind="fire"});
            Check(player.drawn&&player.fireAfterDraw&&player.shots==shots,"winner may Q and click after the result pause; shot waits for the normal draw (drawn="+player.drawn+", queued="+player.fireAfterDraw+", shots="+player.shots+", before="+shots+", busy="+player.busyUntil+", now="+Time.time+", available="+rounds.CanAct(player.id,Time.time)+", allowed="+DuelAllows(player)+", last="+player.lastAction+")");
            player.busyUntil=Time.time-.01f;TickServer();
            Check(player.shots==shots+1,"queued opening shot fires when the draw finishes (time="+Time.time+", busy="+player.busyUntil+", queued="+player.fireAfterDraw+")");
            Check(dealer.hp==hp-SaloonHealth.RevolverDamage&&DuelAllows(dealer),"queued opening shot lands and releases the opponent's response");
            Setup(2);d=duels[2];d.readyAt=0;d.blackjack=Find(h=>h.Complete&&h.Winner==1);d.revealed=false;TickBlackjack(d);
            d.combatAt=Time.time-1;
            var opener=players[d.first];var responder=players[d.first==d.a?d.b:d.a];
            OnCommand(opener.id,new Command{kind="pass"});
            Check(!rounds.CanAct(opener.id,Time.time)&&DuelAllows(responder),"winner can pass immediately to release the response");
            Setup(2);d=duels[2];d.readyAt=0;d.blackjack=Find(h=>h.Complete&&h.Winner==1);d.revealed=false;TickBlackjack(d);
            d.combatAt=Time.time-2;d.firstUntil=Time.time-1;TickDuels();
            Check(!rounds.CanAct(d.first,Time.time)&&DuelAllows(players[d.first==d.a?d.b:d.a]),"opening timeout lets opponent respond");
            Setup(2);d=duels[2];d.readyAt=0;
            d.blackjack=Find(h=>{if(h.Complete)return h.Winner==0;h.Stand();while(!h.Complete)h.DealerStep();return h.Winner==0;});
            ulong sameDealer=d.dealer;TickBlackjack(d);
            Check(!d.revealed&&PrivateDuel(host).stage=="redealing"&&d.revealAt-Time.time>=4.9f,"push keeps the cards visible for five seconds and awards no opening");
            d.revealAt=Time.time-1;TickBlackjack(d);
            Check(d.dealer==sameDealer&&d.revealAt==0&&!d.revealed,"push redeals with same roles");
            Check(rounds.Deadline>=Time.time+Mathf.Max(10,tuning.turnSeconds)-.1f,"minigame preserves combat action time");
            Debug.Log("SALOON_BLACKJACK PASS");
        }
#endif
    }
}
