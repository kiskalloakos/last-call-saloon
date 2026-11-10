using System.Linq;

namespace Saloon
{
    public partial class SaloonGame
    {
        int[] RoomCapacities()
        {
            var capacities=new[]{5,2,2,2};
            foreach(var dead in players.Values.Where(p=>!p.Alive&&!p.gatherStation&&!ObserverReady(p)))capacities[dead.room]--;
            return capacities;
        }
        bool AcceptPlanCommand(Participant p,Command cmd)
        {
            if(cmd.planVersion>0&&cmd.planVersion<p.planVersion)return false;
            if(cmd.planVersion>0)p.planVersion=cmd.planVersion;
            return true;
        }
        void SetRoomPlan(Participant p,int room,ulong? companion)
        {
            // Re-sending an unchanged plan must not move a player to the back of the line.
            if(p.chosenRoom!=room||p.companion!=companion)rounds.SetReady(p.id,false);
            p.chosenRoom=room;p.companion=companion;
        }
        bool TryReady(Participant p,bool ready)
        {
            if(!ready){rounds.SetReady(p.id,false);p.notice="Seat released — you can edit your plan.";return true;}
            if(rounds.IsReady(p.id))return true;
            if(p.chosenRoom>0)
            {
                int capacity=RoomCapacities()[p.chosenRoom];
                int reserved=players.Values.Count(other=>other!=p&&other.Alive&&other.chosenRoom==p.chosenRoom&&rounds.IsReady(other.id));
                if(reserved>=capacity)
                {p.notice=SaloonStations.Name(p.chosenRoom)+" is full. Choose another room, or wait for a seat to be released.";return false;}
            }
            rounds.SetReady(p.id,true);p.notice=SaloonStations.Name(p.chosenRoom)+" seat reserved — waiting for the table.";return true;
        }
        bool PlanFull(Participant p)
        {
            if(p.chosenRoom<=0||rounds.IsReady(p.id))return false;
            int reserved=0;foreach(var other in players.Values)if(other!=p&&other.Alive&&other.chosenRoom==p.chosenRoom&&rounds.IsReady(other.id))reserved++;
            return reserved>=RoomCapacities()[p.chosenRoom];
        }
    }
}
