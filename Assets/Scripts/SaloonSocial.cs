using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    [Serializable]
    public class DuelView
    {
        public string stage,opponent,headsName,tailsName,firstName,firstId,side,result;
        public string game,dealerName,playerName,dealerCards,playerCards;
        public int playerTotal,dealerTotal;
        public bool isDealer,canHit,canDeal;
        public string dealerId,playerId;
        public int handNumber;
        public int[] playerHand,dealerHand;
        public float remaining;
        public bool yourTurn,waitingForOpening;
        public bool canChooseCoin;
        public string coinAId,coinBId,headsId;
        public float coinElapsed,coinDuration;
        public int coinRound;
    }

    [Serializable]
    public class RoomTurnView
    {
        public string currentId,currentName;
        public string[] order;
        public float remaining;
        public bool yourTurn,spent;
    }

    public partial class SaloonGame
    {
        const float CoinChoiceSeconds=3,CoinFlipSeconds=1.2f;
        class Duel
        {
            public ulong a,b,heads,first;
            public float choiceUntil,revealAt,firstUntil;
            public float coinChoiceSeconds=CoinChoiceSeconds,coinFlipSeconds=CoinFlipSeconds;
            public bool claimed,flipping,revealed;
            public string result;
            public SaloonBlackjack blackjack;
            public ulong dealer;
            public float nextCardAt,nextInputAt,readyAt;
            public int handNumber;
        }
        readonly Dictionary<int,Duel> duels=new Dictionary<int,Duel>();
        class RoomTurn
        {
            public ulong[] order;
            public int index;
            public float until,seconds;
        }
        readonly Dictionary<int,RoomTurn> roomTurns=new Dictionary<int,RoomTurn>();
        public bool RoomPlannerOpen;
        public bool NeedsPointer=>View!=null&&(ShowRole||View.phase==Phase.Finished||(View.phase==Phase.Discussion&&RoomPlannerOpen)||View.phase==Phase.Choosing||(View.phase==Phase.Turns&&(View.duel?.stage=="choosing"||PlayingTableGame)));
        public float HitFlash { get; private set; }
        float eliminatedAt=-10;
        public bool ShowEliminationOverlay=>View?.phase!=Phase.Aftermath&&Self?.observer==true;
        float shotKick,hitKick;
        int previousHp=-1,previousShots=-1;
        public int HealthDelta { get; private set; }
        public float HealthDeltaUntil { get; private set; }
        void SetupDuels(bool gather)
        {
            duels.Clear();roomTurns.Clear();if(gather)return;
            foreach(var group in players.Values.Where(p=>p.Alive).GroupBy(p=>p.room))
            {
                if(group.Key==3)continue;
                var pair=group.OrderBy(p=>p.id).ToArray();
                // The bar always uses ordinary turns, even with just one or two players.
                if(group.Key==0||pair.Length>2)
                {
                    float seconds=Mathf.Max(10,tuning.turnSeconds/pair.Length);
                    roomTurns[group.Key]=new RoomTurn{order=pair.OrderBy(p=>UnityEngine.Random.value).Select(p=>p.id).ToArray(),seconds=seconds,until=Time.time+seconds};
                    rounds.EnsureDeadline(Time.time+seconds*pair.Length+1);continue;
                }
                if(pair.Length!=2)continue;
                foreach(var p in pair)
                {
                    var other=pair.First(x=>x.id!=p.id);var direction=other.body.transform.position-p.body.transform.position;
                    p.yaw=Quaternion.LookRotation(direction).eulerAngles.y;p.pitch=0;p.poseVersion++;
                }
                var duel=new Duel{a=pair[0].id,b=pair[1].id,heads=pair[UnityEngine.Random.Range(0,2)].id,choiceUntil=Time.time+CoinChoiceSeconds};
                duels[group.Key]=duel;
                if(group.Key==1)
                {
                    duel.coinChoiceSeconds=8;duel.coinFlipSeconds=2.2f;duel.choiceUntil=Time.time+duel.coinChoiceSeconds;
                    foreach(var seated in pair){seated.pitch=26;seated.poseVersion++;}
                }
                if(group.Key==2)
                {
                    duel.dealer=pair[UnityEngine.Random.Range(0,2)].id;
                    DealBlackjack(duel);
                }
            }
            if(duels.Count>0)rounds.DelayDeadline(duels.Values.Max(d=>d.coinChoiceSeconds+d.coinFlipSeconds));
        }
        void ClaimCoin(Participant p,int side)
        {
            if(phase!=Phase.Turns||p.room!=1||!p.Alive||!duels.TryGetValue(p.room,out var duel)||duel.blackjack!=null||duel.claimed||duel.flipping||Time.time>=duel.choiceUntil)return;
            if(side!=0&&side!=1)return;
            duel.heads=side==0?p.id:(p.id==duel.a?duel.b:duel.a);duel.claimed=true;StartFlip(duel);
        }
        void StartFlip(Duel duel)
        {duel.flipping=true;duel.revealAt=Time.time+duel.coinFlipSeconds;}
        void TickDuels()
        {
            if(phase!=Phase.Turns)return;
            foreach(var room in roomTurns.Keys)AdvanceRoomTurn(room);
            foreach(var duel in duels.Values)
            {
                if(duel.blackjack!=null)TickBlackjack(duel);
                if(duel.blackjack==null&&!duel.flipping&&Time.time>=duel.choiceUntil)StartFlip(duel);
                if(duel.blackjack==null&&duel.flipping&&!duel.revealed&&Time.time>=duel.revealAt)
                {
                    bool heads=UnityEngine.Random.Range(0,2)==0;duel.result=heads?"HEADS":"TAILS";
                    duel.first=heads?duel.heads:(duel.heads==duel.a?duel.b:duel.a);duel.revealed=true;
                    duel.firstUntil=Time.time+Mathf.Min(6,tuning.turnSeconds*.4f);
                    if(players[duel.a].room==1)
                    {
                        foreach(ulong id in new[]{duel.a,duel.b}){players[id].pitch=0;players[id].poseVersion++;players[id].lastAction=-100;}
                        players[duel.first].notice="You won the coin toss. Q to draw, click to shoot, or Space to pass.";
                        Broadcast();
                    }
                }
                if(duel.revealed&&rounds.CanAct(duel.first,Time.time)&&
                    (!players[duel.first].Alive||Time.time>=duel.firstUntil))
                {rounds.TrySpend(duel.first,Time.time);players[duel.first].notice="Opening passed. Your opponent may respond.";}
            }
        }
        bool DuelAllows(Participant p)
        {
            if(roomTurns.TryGetValue(p.room,out var turn))return turn.index<turn.order.Length&&turn.order[turn.index]==p.id;
            if(!duels.TryGetValue(p.room,out var duel))return true;
            if(!duel.revealed)return false;
            return p.id==duel.first||!players[duel.first].Alive||!rounds.CanAct(duel.first,Time.time);
        }
        void AdvanceRoomTurn(int room)
        {
            if(phase!=Phase.Turns||!roomTurns.TryGetValue(room,out var turn))return;
            while(turn.index<turn.order.Length)
            {
                ulong id=turn.order[turn.index];
                // A late frame must not let the shared deadline erase everyone's remaining turn.
                // Only the current actor may time out; the next actor gets a fresh full window.
                rounds.EnsureDeadline(Mathf.Max(Time.time,turn.until)+turn.seconds*(turn.order.Length-turn.index-1)+1);
                if(players[id].Alive&&!rounds.HasSpent(id)&&Time.time<turn.until)return;
                if(players[id].Alive&&!rounds.HasSpent(id))
                {rounds.TrySpend(id,Time.time);players[id].notice="Passed — your turn timed out.";}
                turn.index++;turn.until=Time.time+turn.seconds;
            }
        }
        RoomTurnView PrivateRoomTurn(Participant p)
        {
            if(phase!=Phase.Turns||!p.Alive||!roomTurns.TryGetValue(p.room,out var turn))return null;
            bool active=turn.index<turn.order.Length;ulong id=active?turn.order[turn.index]:0;
            return new RoomTurnView{currentId=active?id.ToString():"",currentName=active?players[id].name:"",
                order=turn.order.Select(x=>players[x].name).ToArray(),yourTurn=active&&id==p.id,spent=!rounds.CanAct(p.id,Time.time),
                remaining=active?Mathf.Max(0,turn.until-Time.time):0};
        }
        DuelView PrivateDuel(Participant p)
        {
            if(phase!=Phase.Turns||!p.Alive||!duels.TryGetValue(p.room,out var d))return null;
            bool opening=d.revealed&&players[d.first].Alive&&rounds.CanAct(d.first,Time.time);
            if(d.blackjack!=null)return PrivateBlackjack(p,d,opening);
            return new DuelView{game="coin",coinAId=d.a.ToString(),coinBId=d.b.ToString(),headsId=d.heads.ToString(),coinRound=rounds.Round,coinDuration=d.coinFlipSeconds,
                coinElapsed=d.revealed?d.coinFlipSeconds:d.flipping?Mathf.Clamp(d.coinFlipSeconds-(d.revealAt-Time.time),0,d.coinFlipSeconds):0,canChooseCoin=!d.flipping&&!d.claimed&&Time.time<d.choiceUntil,
                stage=!d.flipping?"choosing":!d.revealed?"flipping":"playing",
                opponent=players[p.id==d.a?d.b:d.a].name,headsName=players[d.heads].name,
                tailsName=players[d.heads==d.a?d.b:d.a].name,side=p.id==d.heads?"HEADS":"TAILS",result=d.result??"",
                firstId=d.revealed?d.first.ToString():"",firstName=d.revealed?players[d.first].name:"",
                yourTurn=d.revealed&&DuelAllows(p)&&rounds.CanAct(p.id,Time.time),
                waitingForOpening=opening&&p.id!=d.first,
                remaining=Mathf.Max(0,(!d.flipping?d.choiceUntil:!d.revealed?d.revealAt:opening?d.firstUntil:rounds.Deadline)-Time.time)};
        }
        DuelView PublicCoinTable()
        {
            if(phase!=Phase.Turns||!duels.TryGetValue(1,out var d)||d.blackjack!=null)return null;
            var table=PrivateDuel(players[d.a]);if(table==null)return null;
            table.canChooseCoin=table.yourTurn=table.waitingForOpening=false;table.side="";table.opponent="";return table;
        }
        public void ChooseCoin(int side)=>Connection.Send(new Command{kind="coin",x=side});
        public void ToggleRoomPlanner()
        {if(View?.phase!=Phase.Discussion)return;RoomPlannerOpen=!RoomPlannerOpen;SyncPointer();}
        void SyncPointer(){if(ShowRole||(View?.phase==Phase.Discussion&&RoomPlannerOpen)){UnlockCursor();return;}if(!MenuOpen&&NeedsPointer&&Self?.alive==true&&Input.GetMouseButton(1))LockCursor();else if(MenuOpen||NeedsPointer||Self?.alive==false)UnlockCursor();else LockCursor();}
        void ApplyImpactFeedback(PlayerView self)
        {
            if(self==null)return;
            if(self.health<=0&&previousHp!=0){eliminatedAt=Time.unscaledTime;cameraCut=true;}
            if(self.health>0)eliminatedAt=-10;
            if(View.phase==Phase.Lobby||(View.round==1&&View.phase==Phase.Discussion))HealthDeltaUntil=0;
            if(previousHp>=0&&self.health!=previousHp&&!(View.round==1&&View.phase==Phase.Discussion))
            {HealthDelta=self.health-previousHp;HealthDeltaUntil=Time.unscaledTime+2.5f;}
            if(previousHp>=0&&self.health<previousHp){hitKick=1;HitFlash=.22f;}
            if(previousShots>=0&&self.shots>previousShots)shotKick=1;
            previousHp=self.health;previousShots=self.shots;
        }
        Quaternion CameraImpact()
        {
            float t=Time.unscaledTime;
            var offset=new Vector3(-shotKick*1.4f+Mathf.Sin(t*91)*hitKick*1.6f,Mathf.Sin(t*113)*(shotKick*.18f+hitKick*.9f),Mathf.Sin(t*87)*hitKick*.8f);
            shotKick=Mathf.MoveTowards(shotKick,0,Time.unscaledDeltaTime*9);
            hitKick=Mathf.MoveTowards(hitKick,0,Time.unscaledDeltaTime*4.5f);
            HitFlash=Mathf.MoveTowards(HitFlash,0,Time.unscaledDeltaTime*.9f);
            return Quaternion.Euler(offset);
        }
    }
}
