using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public const float BluffSpeakSeconds=8,BluffJudgeSeconds=6,DiceResultSeconds=3,DiceRollSeconds=2.4f,BluffThrowSeconds=15;
        internal static bool DiceRound(int round)=>round>0&&round%2==0;
        string CoinRoomGame=>DicePractice||!TutorialActive&&DiceRound(rounds.Round)?"bluff":"coin";
        public string RoomName(int room)=>room==1&&View?.coinRoomGame=="bluff"?"Bluff Dice":SaloonStations.Name(room);
        public bool PlayingDice=>View?.phase==Phase.Turns&&View.duel?.game=="bluff"&&(View.duel.stage=="waiting"||View.duel.stage=="rolling"||View.duel.stage=="speaking"||View.duel.stage=="judging");
        void DealBluffDice(Duel d)
        {
            // Let the human throw the first practice hand, then alternate roles.
            int speaker=DicePractice?(DicePracticeHands%2==1?(players[d.a].bot?1:0):(players[d.a].bot?0:1)):-1;
            d.bluff=new SaloonBluffDice(new System.Random(Random.Range(0,int.MaxValue)),speaker);
            d.bluffThrown=false;d.throwUntil=Time.time+BluffThrowSeconds;d.nextInputAt=Time.time+.9f;d.result="";
            rounds.EnsureDeadline(d.throwUntil+DiceRollSeconds+BluffSpeakSeconds+BluffJudgeSeconds+DiceResultSeconds+TableOpeningSeconds+Mathf.Max(10,tuning.turnSeconds)+1);
        }
        void ThrowBluffDice(Duel d)
        {
            d.bluffThrown=true;
            d.readyAt=Time.time+DiceRollSeconds;d.choiceUntil=d.readyAt+BluffSpeakSeconds;d.judgmentUntil=d.choiceUntil+BluffJudgeSeconds;
            d.nextInputAt=d.choiceUntil+1.2f;d.result="";
            rounds.EnsureDeadline(d.judgmentUntil+DiceResultSeconds+TableOpeningSeconds+Mathf.Max(10,tuning.turnSeconds)+1);
        }
        void PlayBluffDice(Participant p,Command command)
        {
            if(DicePracticeHelp||phase!=Phase.Turns||!p.Alive||p.room!=1||!duels.TryGetValue(1,out var d)||d.bluff==null||d.revealed||
                command.planRound!=rounds.Round||command.bluffRevision!=d.bluff.Revision)return;
            if(command.name=="throw")
            {
                if(!d.bluffThrown&&p.id==(d.bluff.Speaker==0?d.a:d.b)&&(DicePractice||Time.time<d.throwUntil))
                {ThrowBluffDice(d);Broadcast();}
                return;
            }
            if(!d.bluffThrown||Time.time<d.choiceUntil||!DicePractice&&Time.time>=d.judgmentUntil||
                p.id!=(d.bluff.Listener==0?d.a:d.b)||(command.name!="believe"&&command.name!="doubt"))return;
            if(d.bluff.Judge(command.name=="believe"))RevealBluffDice(d);
        }
        void TickBluffDice(Duel d)
        {
            if(d.revealed||DicePracticeHelp)return;
            if(!players[d.a].Alive||!players[d.b].Alive)
            {d.bluff.Forfeit(players[d.a].Alive?1:0);RevealBluffDice(d);return;}
            if(!d.bluffThrown)
            {
                if(!DicePractice&&Time.time>=d.throwUntil)
                {d.bluffTimedOut=true;d.bluff.Forfeit(d.bluff.Speaker);RevealBluffDice(d);return;}
                ulong speaker=d.bluff.Speaker==0?d.a:d.b;
                if(!verifying&&players[speaker].bot&&Time.time>=d.nextInputAt)
                    PlayBluffDice(players[speaker],new Command{planRound=rounds.Round,bluffRevision=d.bluff.Revision,name="throw"});
                return;
            }
            if(Time.time<d.choiceUntil)return;
            if(!DicePractice&&Time.time>=d.judgmentUntil)
            {d.bluffTimedOut=true;d.bluff.Forfeit(d.bluff.Listener);RevealBluffDice(d);return;}
            ulong listener=d.bluff.Listener==0?d.a:d.b;
            if(!verifying&&players[listener].bot&&Time.time>=d.nextInputAt)
            {
                // A bot cannot read speech or see the speaker's roll. It makes a blind judgment.
                PlayBluffDice(players[listener],new Command{planRound=rounds.Round,bluffRevision=d.bluff.Revision,name=Random.value<.5f?"believe":"doubt"});
            }
        }
        void RevealBluffDice(Duel d)
        {
            d.first=d.bluff.Winner==0?d.a:d.b;d.revealed=true;if(DicePractice)DicePracticeReview=true;
            d.combatAt=Time.time+DiceResultSeconds;d.firstUntil=d.combatAt+TableOpeningSeconds;
            d.result=d.bluffTimedOut?(d.bluffThrown?"No judgment in time. The speaker wins.":"No throw in time. The listener wins."):!d.bluff.Believed.HasValue?"Opponent left the table.":
                "The claim was "+(d.bluff.Truth?"true":"false")+". "+(d.first==(d.bluff.Listener==0?d.a:d.b)?"The listener judged correctly.":"The speaker fooled the listener.");
            foreach(ulong id in new[]{d.a,d.b}){players[id].pitch=0;players[id].poseVersion++;players[id].lastAction=-100;}
            rounds.EnsureDeadline(d.firstUntil+Mathf.Max(10,tuning.turnSeconds)+1);Broadcast();
        }
        DuelView PrivateBluffDice(Participant p,Duel d,bool opening,bool publicTable=false)
        {
            var hand=d.bluff;ulong speaker=hand.Speaker==0?d.a:d.b,listener=hand.Listener==0?d.a:d.b;
            bool isSpeaker=p.id==speaker&&!publicTable,botCaption=p.id==listener&&!publicTable&&players[speaker].bot;
            bool known=d.bluffThrown&&(isSpeaker||d.revealed),reveal=d.bluffThrown&&d.revealed;
            return new DuelView{game="bluff",stage=d.revealed?(Time.time<d.combatAt?"result":"playing"):!d.bluffThrown?"waiting":Time.time<d.readyAt?"rolling":Time.time<d.choiceUntil?"speaking":"judging",
                opponent=players[p.id==d.a?d.b:d.a].name,coinAId=d.a.ToString(),coinBId=d.b.ToString(),coinRound=rounds.Round,
                yourDice=isSpeaker&&d.bluffThrown?hand.Hand():System.Array.Empty<int>(),opponentDice=reveal&&!isSpeaker&&!publicTable?hand.Hand():System.Array.Empty<int>(),
                revealedDiceA=reveal&&hand.Speaker==0?hand.Hand():System.Array.Empty<int>(),revealedDiceB=reveal&&hand.Speaker==1?hand.Hand():System.Array.Empty<int>(),
                bluffSpeakerId=speaker.ToString(),bluffListenerId=listener.ToString(),bluffSpeakerName=players[speaker].name,bluffIsSpeaker=isSpeaker,
                bluffClaim=known||d.bluffThrown&&botCaption?hand.Claim:"",bluffTruthKnown=known,bluffClaimTrue=known&&hand.Truth,
                bluffRevision=hand.Revision,bluffCanJudge=!publicTable&&d.bluffThrown&&!d.revealed&&!DicePracticeHelp&&p.id==listener&&Time.time>=d.choiceUntil&&(DicePractice||Time.time<d.judgmentUntil),
                bluffBotCaption=botCaption,bluffJudged=hand.Believed.HasValue,bluffBelieved=hand.Believed.GetValueOrDefault(),bluffTimedOut=d.bluffTimedOut,
                result=d.result,firstId=d.revealed?d.first.ToString():"",firstName=d.revealed?players[d.first].name:"",
                yourTurn=!publicTable&&d.revealed&&DuelAllows(p)&&rounds.CanAct(p.id,Time.time),waitingForOpening=!publicTable&&opening&&p.id!=d.first,
                remaining=Mathf.Max(0,(d.revealed?(Time.time<d.combatAt?d.combatAt:opening?d.firstUntil:rounds.Deadline):!d.bluffThrown?d.throwUntil:Time.time<d.readyAt?d.readyAt:Time.time<d.choiceUntil?d.choiceUntil:d.judgmentUntil)-Time.time)};
        }
        DuelView PublicDiceTable()=>phase==Phase.Turns&&duels.TryGetValue(1,out var d)&&d.bluff!=null?PrivateBluffDice(players[d.a],d,false,true):null;
        public void ThrowBluffDice()
        {
            var d=View?.duel;if(DicePracticeHelp||d?.game!="bluff"||d.stage!="waiting"||!d.bluffIsSpeaker)return;
            Connection.Send(new Command{kind="bluff",name="throw",planRound=View.round,bluffRevision=d.bluffRevision});
        }
        public void ChooseBluff(bool believe)
        {
            var d=View?.duel;if(d?.game!="bluff")return;
            Connection.Send(new Command{kind="bluff",name=believe?"believe":"doubt",planRound=View.round,bluffRevision=d.bluffRevision});
        }
    }
}
