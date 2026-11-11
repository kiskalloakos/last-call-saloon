using System.Linq;
using UnityEngine;

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
        RoomInvitation[] InvitationsFor(Participant recipient)
        {
            if(!recipient.Alive||(phase!=Phase.Discussion&&phase!=Phase.Choosing))return System.Array.Empty<RoomInvitation>();
            // Expose a request only to its intended recipient, never other room plans.
            return players.Values.Where(p=>p!=recipient&&p.Alive&&p.companion==recipient.id)
                .Select(p=>new RoomInvitation{fromId=p.id.ToString(),fromName=p.name,room=p.chosenRoom,planVersion=p.planVersion}).ToArray();
        }

        void AcceptRoomInvitation(Participant p,Command cmd,bool ready=false)
        {
            if((phase!=Phase.Discussion&&phase!=Phase.Choosing)||!p.Alive||(phase==Phase.Choosing&&Time.time>=rounds.Deadline)||!AcceptPlanCommand(p,cmd))return;
            if(!ulong.TryParse(cmd.name,out var fromId)||!players.TryGetValue(fromId,out var sender)||sender==p||!sender.Alive||
                sender.companion!=p.id||sender.chosenRoom!=cmd.x||sender.planVersion!=cmd.invitationVersion)
            {p.notice="That invitation changed. Check your latest invitations.";return;}
            SetRoomPlan(p,sender.chosenRoom,sender.id);
            if(Sandbox&&(!verifying||ready))PlanPracticeBots();
            if(ready){TryReady(p,true);return;}
            p.notice="Invitation joined. Choose Ready to reserve your seat.";
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
                YieldPracticeSeats(p,p.chosenRoom,capacity);
                int reserved=players.Values.Count(other=>other!=p&&other.Alive&&other.chosenRoom==p.chosenRoom&&rounds.IsReady(other.id));
                if(reserved>=capacity)
                {p.notice=SaloonStations.Name(p.chosenRoom)+" is full. Choose another room, or wait for a seat to be released.";return false;}
            }
            rounds.SetReady(p.id,true);p.notice=SaloonStations.Name(p.chosenRoom)+" seat reserved — waiting for the table.";return true;
        }
        bool PlanFull(Participant p)
        {
            if(p.chosenRoom<=0||rounds.IsReady(p.id))return false;
            int reserved=0;foreach(var other in players.Values)if(other!=p&&other.Alive&&(!Sandbox||p.bot||!other.bot)&&other.chosenRoom==p.chosenRoom&&rounds.IsReady(other.id))reserved++;
            return reserved>=RoomCapacities()[p.chosenRoom];
        }
    }
}
