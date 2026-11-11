using System;
using System.Collections.Generic;
using System.Linq;

namespace Saloon
{
    public partial class SaloonGame
    {
        void PlanPracticeBots()
        {
            if(!Sandbox||(phase!=Phase.Discussion&&phase!=Phase.Choosing))return;
            var bots=players.Values.Where(p=>p.bot&&p.Alive).OrderBy(p=>p.id).ToArray();
            var humans=players.Values.Where(p=>!p.bot&&p.Alive).OrderBy(p=>p.id).ToArray();
            var limits=RoomCapacities();var used=new int[4];
            var plans=new Dictionary<ulong,(int room,ulong? companion)>();
            foreach(var human in humans)if(rounds.IsReady(human.id))used[human.chosenRoom]++;

            // Give a human and their invited bot space together before autonomous bot plans.
            foreach(var human in humans)
            {
                var invited=bots.FirstOrDefault(b=>!b.bag&&b.id==human.companion&&!plans.ContainsKey(b.id));
                if(invited==null)continue;
                int room=human.chosenRoom,needed=rounds.IsReady(human.id)?1:2;
                if(room>0&&used[room]+needed>limits[room])continue;
                plans[invited.id]=(room,human.id);used[room]+=needed;
            }

            // One stable invitation per human, with an open place for its recipient.
            for(int i=0;i<humans.Length;i++)
            {
                var human=humans[i];if(human.companion.HasValue||rounds.IsReady(human.id)||bots.Length==0)continue;
                int room=1+(rounds.Round-1+i)%3;
                var inviter=Enumerable.Range(0,bots.Length).Select(n=>bots[(rounds.Round-1+i+n)%bots.Length])
                    .FirstOrDefault(b=>!b.bag&&!plans.ContainsKey(b.id));
                if(inviter==null||used[room]+2>limits[room])continue;
                plans[inviter.id]=(room,human.id);used[room]+=2;
            }

            foreach(var bot in bots)
            {
                if(plans.ContainsKey(bot.id))continue;
                int room=bot.bag?0:PracticeRoomCycle[(Array.IndexOf(bots,bot)+rounds.Round-1)%PracticeRoomCycle.Length];
                if(rounds.Round%2==0&&room>0&&room<3)room=3-room;
                if(room>0&&used[room]>=limits[room])room=0;
                plans[bot.id]=(room,null);used[room]++;
            }
            // Apply every destination before checking readiness, so old bot reservations
            // cannot reject a new invitation partway through the replan.
            foreach(var bot in bots){var plan=plans[bot.id];SetRoomPlan(bot,plan.room,plan.companion);}
            foreach(var bot in bots)TryReady(bot,true);
        }

        void YieldPracticeSeats(Participant human,int room,int capacity)
        {
            if(!Sandbox||human.bot)return;
            int humans=players.Values.Count(p=>p!=human&&!p.bot&&p.Alive&&p.chosenRoom==room&&rounds.IsReady(p.id));
            int keep=Math.Max(0,capacity-humans-1);
            var bots=players.Values.Where(p=>p.bot&&p.Alive&&p.chosenRoom==room&&rounds.IsReady(p.id))
                .OrderByDescending(p=>human.companion==p.id&&p.companion==human.id).ThenBy(p=>p.id).ToArray();
            foreach(var bot in bots.Skip(keep)){SetRoomPlan(bot,0,null);TryReady(bot,true);}
        }
    }
}
