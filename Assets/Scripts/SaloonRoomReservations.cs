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
            if(Time.time>=rounds.Deadline)return false;
            if(cmd.planRound>0&&cmd.planRound!=rounds.Round)return false;
            if(cmd.planVersion>0&&cmd.planVersion<p.planVersion)return false;
            if(cmd.planVersion>0)p.planVersion=cmd.planVersion;
            return true;
        }
        RoomInvitation[] InvitationsFor(Participant recipient)
        {
            if(!recipient.Alive||rounds.IsReady(recipient.id)||(phase!=Phase.Discussion&&phase!=Phase.Choosing))return System.Array.Empty<RoomInvitation>();
            return players.Values.Where(p=>p!=recipient&&p.Alive&&!rounds.IsReady(p.id)&&p.companion==recipient.id&&HasPlanSpace(p.chosenRoom,p,recipient))
                .Select(p=>new RoomInvitation{fromId=p.id.ToString(),fromName=p.name,room=p.chosenRoom,planVersion=p.planVersion}).ToArray();
        }
        bool HasPlanSpace(int room,Participant first,Participant second=null)
        {
            int reserved=players.Values.Count(p=>p.Alive&&p!=first&&p!=second&&p.chosenRoom==room&&rounds.IsReady(p.id));
            return reserved+(second==null?1:2)<=RoomCapacities()[room];
        }
        bool CanSelectPlan(Participant p,int room,ulong? companion)
        {
            Participant partner=null;
            if(companion.HasValue)
            {
                if(!players.TryGetValue(companion.Value,out partner)||!partner.Alive||partner==p)
                {p.notice="That player is no longer available. Choose another plan.";return false;}
                if(rounds.IsReady(partner.id)&&(partner.companion!=p.id||partner.chosenRoom!=room))
                {p.notice=partner.name+" has already locked a plan. Choose an available player.";return false;}
            }
            if(!HasPlanSpace(room,p,partner))
            {p.notice=SaloonStations.Name(room)+" is full. Choose another room.";return false;}
            return true;
        }
        void AcceptRoomInvitation(Participant p,Command cmd,bool ready=false)
        {
            if((phase!=Phase.Discussion&&phase!=Phase.Choosing)||!p.Alive||!AcceptPlanCommand(p,cmd))return;
            if(!ulong.TryParse(cmd.name,out var fromId)||!players.TryGetValue(fromId,out var sender)||sender==p||!sender.Alive||
                sender.companion!=p.id||sender.chosenRoom!=cmd.x||sender.planVersion!=cmd.invitationVersion||rounds.IsReady(p.id)||rounds.IsReady(sender.id))
            {p.notice="That invitation changed. Check your latest invitations.";return;}
            if(!CanSelectPlan(p,sender.chosenRoom,sender.id))return;
            SetRoomPlan(p,sender.chosenRoom,sender.id);
            if(ready){TryReady(p,true);return;}
            p.notice="Invitation selected. Lock your choice to confirm both players.";
        }
        void ReleasePair(Participant p)
        {
            if(p.companion.HasValue&&players.TryGetValue(p.companion.Value,out var other)&&other.companion==p.id)
            {
                rounds.SetReady(other.id,false);other.companion=null;
                other.notice=p.name+" changed the shared plan. Choose or lock a new plan.";
            }
            rounds.SetReady(p.id,false);p.companion=null;
        }
        void SetRoomPlan(Participant p,int room,ulong? companion)
        {
            if(p.chosenRoom!=room||p.companion!=companion)ReleasePair(p);
            p.chosenRoom=room;p.companion=companion;p.planSelected=true;
        }
        bool TryReady(Participant p,bool ready)
        {
            if(!ready){ReleasePair(p);p.notice="Plan unlocked — choose a room or invite someone.";return true;}
            if(rounds.IsReady(p.id))return true;
            if(!CanSelectPlan(p,p.chosenRoom,p.companion))return false;
            Participant partner=null;
            if(p.companion.HasValue)
            {
                partner=players[p.companion.Value];
                if(partner.companion!=p.id||partner.chosenRoom!=p.chosenRoom)
                {p.notice="Waiting for "+partner.name+" to accept your invitation.";return false;}
            }
            // Capacity and consent are checked before reserving either place.
            p.planSelected=true;rounds.SetReady(p.id,true);
            if(partner!=null)
            {
                partner.planSelected=true;rounds.SetReady(partner.id,true);
                partner.notice="Locked with "+p.name+" · "+SaloonStations.Name(p.chosenRoom)+".";
            }
            p.notice="Locked · "+SaloonStations.Name(p.chosenRoom)+(partner==null?" · no partner selected.":" with "+partner.name+".");
            return true;
        }
        bool PlanFull(Participant p)=>!rounds.IsReady(p.id)&&!HasPlanSpace(p.chosenRoom,p);
    }
}
