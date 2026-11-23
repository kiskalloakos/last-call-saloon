using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public const int ToiletPumpCount=8;
        public const float ToiletPumpInterval=.4f,ToiletMissionSeconds=12;
        int toiletPumps,toiletSequence,localToiletSequence,toiletRound;
        float toiletPumpAt=-100,toiletDeadline;
        ulong? toiletWorker;
        bool toiletCompleted;
        void ResetToiletDuty()
        {
            toiletRound=0;toiletWorker=null;toiletCompleted=false;toiletPumps=toiletSequence=0;toiletPumpAt=-100;toiletDeadline=0;
            foreach(var p in players.Values)p.toiletOffer=p.toiletDuty=false;
        }
        void UpdateToiletDuty()
        {
            if(TutorialActive||phase!=Phase.Discussion||toiletRound==rounds.Round)return;
            foreach(var p in players.Values)p.toiletOffer=p.toiletDuty=false;
            toiletRound=rounds.Round;toiletCompleted=false;toiletPumps=toiletSequence=0;toiletPumpAt=-100;toiletDeadline=0;
            var eligible=players.Values.Where(p=>p.Alive&&p.hp<tuning.health).OrderBy(p=>p.id).ToArray();
            toiletWorker=eligible.Length==0?(ulong?)null:eligible[Random.Range(0,eligible.Length)].id;
            if(!toiletWorker.HasValue)return;
            var worker=players[toiletWorker.Value];worker.toiletOffer=true;rounds.SetReady(worker.id,false);
            if(worker.bot)AnswerToiletDuty(worker,true);
        }
        void AnswerToiletDuty(Participant p,bool accept)
        {
            if(!p.Alive||phase!=Phase.Discussion||toiletRound!=rounds.Round||toiletWorker!=p.id||!p.toiletOffer)return;
            p.toiletOffer=false;
            if(p.hp>=tuning.health){p.notice="Already at full health. Choose your normal room plan.";return;}
            if(!accept){p.notice="Toilet duty declined. Choose your normal room plan.";return;}
            ReleasePair(p);SetRoomPlan(p,0,null);p.escapePlanned=false;p.toiletDuty=true;rounds.SetReady(p.id,true);
            p.notice="Solo toilet mission accepted. Unclog safely for +20% health, then return to Bar with your combat action.";
        }
        void StartToiletMission()
        {
            if(phase!=Phase.Turns||toiletRound!=rounds.Round||!toiletWorker.HasValue||!players.TryGetValue(toiletWorker.Value,out var p)||!p.toiletDuty||!p.Alive)return;
            if(p.hp>=tuning.health){p.toiletDuty=false;return;}
            // Keep the shared clock running, with a small combat window on short rounds.
            toiletDeadline=Mathf.Min(Time.time+ToiletMissionSeconds,Mathf.Max(Time.time,rounds.Deadline-3));
            p.controller.enabled=false;p.room=SaloonBathroom.Room;p.seat=-1;p.body.transform.position=SaloonToilet.Approach;
            p.crouched=p.crouchHeld=false;p.move=Vector2.zero;p.vertical=0;p.drawn=false;p.fireAfterDraw=false;p.pendingShot=null;CancelShotCharge(p);
            p.busyAction="";p.busyUntil=0;p.yaw=p.bodyYaw=0;p.pitch=42;p.poseVersion++;p.controller.enabled=true;UpdatePlayerColliders(p);p.combatHistory.Clear();
            p.notice="Safe solo mission. Left click eight times to unclog before the timer ends. Your combat action is kept.";
        }
        bool NearToilet(Participant p)=>p.room==SaloonBathroom.Room&&SaloonToilet.InReach(p.body.transform.position);
        void BeginToiletWork(Participant p)
        {
            if(!p.Alive||phase!=Phase.Turns||!NearToilet(p)||!p.toiletDuty||toiletRound!=rounds.Round||toiletWorker!=p.id||toiletCompleted||Time.time>=toiletDeadline||rounds.HasSpent(p.id))return;
            p.busyAction="toilet";p.busyUntil=toiletDeadline;p.drawn=false;p.move=Vector2.zero;
            var direction=SaloonToilet.Position-p.body.transform.position;p.bodyYaw=p.yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;p.pitch=42;p.poseVersion++;
            p.notice="Left click to pump the plunger. Combat action kept.";
        }
        void PumpToilet(Participant p,Command cmd)
        {
            if(!p.Alive||phase!=Phase.Turns||toiletRound!=rounds.Round||cmd.planRound!=rounds.Round||toiletWorker!=p.id||!p.toiletDuty||toiletCompleted||!NearToilet(p)||
                cmd.toiletSequence<=toiletSequence||Time.time>=toiletDeadline)return;
            toiletSequence=cmd.toiletSequence;
            if(p.busyAction!="toilet")BeginToiletWork(p);
            if(p.busyAction!="toilet"||Time.time-toiletPumpAt<ToiletPumpInterval)return;
            toiletPumpAt=Time.time;toiletPumps++;p.notice="Plunger strokes: "+toiletPumps+" / "+ToiletPumpCount;
            if(toiletPumps>=ToiletPumpCount)CompleteToiletWork(p);
        }
        bool CanPumpToilet=>Self?.alive==true&&Self.room==SaloonBathroom.Room&&View.phase==Phase.Turns&&View.toiletDuty&&SaloonToilet.InReach(new Vector3(Self.x,Self.y,Self.z));
        void SendToiletPump()=>Connection.Send(new Command{kind="toiletPump",planRound=View.round,toiletSequence=++localToiletSequence});
        void ReturnFromToilet(Participant p,string notice)
        {
            p.toiletDuty=false;p.busyAction="";p.busyUntil=0;p.drawn=false;p.move=Vector2.zero;p.fireAfterDraw=false;p.pendingShot=null;CancelShotCharge(p);
            p.controller.enabled=false;p.room=0;p.seat=100+Mathf.Max(0,p.beerStation);p.body.transform.position=SaloonStations.Position(false,0,Mathf.Max(0,p.beerStation));
            p.yaw=p.bodyYaw=SaloonStations.Facing(false,0,Mathf.Max(0,p.beerStation));p.pitch=0;p.poseVersion++;p.vertical=0;p.lastAction=-100;p.botThink=Time.time+1;
            UpdatePlayerColliders(p);if(p.Alive)Stand(p);p.combatHistory.Clear();p.notice=notice;
        }
        void CancelToiletWork(Participant p)=>ReturnFromToilet(p,"Toilet mission ended without healing. Returned to Bar; combat action kept.");
        void TickToiletWork(Participant p)
        {
            if(p.room!=SaloonBathroom.Room)return;
            if(!p.Alive||phase!=Phase.Turns||toiletRound!=rounds.Round||!p.toiletDuty||Time.time>=toiletDeadline)
            {CancelToiletWork(p);return;}
            if(p.busyAction=="toilet"&&!NearToilet(p)){CancelToiletWork(p);return;}
            if(p.bot&&Time.time-toiletPumpAt>=.6f)PumpToilet(p,new Command{planRound=rounds.Round,toiletSequence=toiletSequence+1});
        }
        void CompleteToiletWork(Participant p)
        {
            if(!p.Alive||phase!=Phase.Turns||p.busyAction!="toilet"||toiletPumps<ToiletPumpCount||!p.toiletDuty||toiletWorker!=p.id||toiletRound!=rounds.Round||toiletCompleted||!NearToilet(p)||Time.time>=toiletDeadline)return;
            toiletCompleted=true;p.hp=Mathf.Min(tuning.health,p.hp+20);
            ReturnFromToilet(p,"Toilet unclogged. +20% health. Returned to Bar; combat action available.");
        }
        public void ChooseToiletDuty(bool accept)=>Connection.Send(new Command{kind="toiletDuty",x=accept?1:0,planRound=View.round,planVersion=++nextPlanVersion});
    }
}
