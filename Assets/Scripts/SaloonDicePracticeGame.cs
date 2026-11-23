using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public bool DicePractice { get; private set; }
        public int DicePracticeHands { get; private set; }
        public bool DicePracticeHelp { get; private set; }
        public bool DicePracticeReview { get; private set; }
        public void ShowDicePracticeHelp(){if(DicePractice){DicePracticeHelp=true;Broadcast();}}
        public void CloseDicePracticeHelp(){DicePracticeHelp=false;Broadcast();}
        public void ContinueDicePractice()
        {
            if(!DicePractice||!DicePracticeReview||!duels.TryGetValue(1,out var d))return;
            DicePracticeReview=false;d.combatAt=Time.time+DiceResultSeconds;d.firstUntil=d.combatAt+TableOpeningSeconds;
            rounds.EnsureDeadline(d.firstUntil+Mathf.Max(10,tuning.turnSeconds)+1);Broadcast();
        }
        void HoldDicePracticeTimers()
        {
            if(!DicePractice||phase!=Phase.Turns||!duels.TryGetValue(1,out var d)||d.bluff==null)return;
            if(!d.revealed||DicePracticeHelp||DicePracticeReview)rounds.EnsureDeadline(Time.time+Mathf.Max(10,tuning.turnSeconds));
            if(DicePracticeHelp){d.throwUntil+=Time.deltaTime;d.readyAt+=Time.deltaTime;d.choiceUntil+=Time.deltaTime;d.judgmentUntil+=Time.deltaTime;d.nextInputAt+=Time.deltaTime;}
            if(d.revealed&&(DicePracticeHelp||DicePracticeReview))
            {d.combatAt=Time.time+DiceResultSeconds;d.firstUntil=d.combatAt+TableOpeningSeconds;}
        }
        public void StartDicePractice()
        {
            // Offline snapshots and commands; never turn a connected match into a test.
            if(InRoom&&!Connection.Solo)return;
            HostLan(true);if(!Connection.Solo)return;
            TrimBots(2);DicePractice=true;DicePracticeHands=0;Connection.Accepting=false;
            NewDicePracticeHand();DicePracticeHelp=true;Broadcast();
        }
        public void NewDicePracticeHand()
        {
            if(!DicePractice||!Connection.Solo||players.Count!=2)return;
            DicePracticeHelp=DicePracticeReview=false;ResetHeist();outcome=pendingOutcome="";matchResults=null;ResetCombatFeedback();
            foreach(var p in players.Values)
            {
                p.hp=tuning.health;p.ammo=tuning.cylinder;p.deathAt=-1;p.deathDirection=Vector3.zero;
                p.role=p.bot?Role.Renegade:Role.Sheriff;p.name=p.bot?"Dice partner":PlayerName;
                p.rematchVote=false;p.shots=p.hits=p.headshots=p.reloads=0;p.shotAt=p.hitAt=p.confirmedAt=-100;
                p.drawn=p.fireAfterDraw=p.chargingShot=p.cigaretteBuff=false;p.pendingShot=null;p.pendingChargeShot=null;
                p.busyAction="";p.busyUntil=0;p.move=Vector2.zero;p.nextShot=0;p.lastAction=p.lastStance=-100;
                p.beer=0;p.beerHealing=0;p.beerChoiceUntil=p.pourStarted=0;
                p.chosenRoom=1;p.planSelected=true;p.companion=players.Values.First(other=>other!=p).id;
            }
            if(DicePracticeHands==0)rounds.Start(players.Keys,Time.time,60,60,0);
            else
            {
                // Advance the round token so delayed commands from a prior hand stay invalid.
                rounds.BeginAftermath(Time.time-10);rounds.Tick(Time.time,id=>true);rounds.Tick(Time.time+4,id=>true);
            }
            DicePracticeHands++;foreach(var id in players.Keys)rounds.SetReady(id,true);
            rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
            roleIntroStarted=float.NegativeInfinity;helloSent=true;MenuOpen=ShowRole=RoomPlannerOpen=false;
            spectateId=shownViewpoint=null;cameraCut=true;Broadcast();
        }
    }
}
