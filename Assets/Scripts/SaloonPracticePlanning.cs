using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        void PlanPracticeBots()
        {
            if(!Sandbox||(phase!=Phase.Discussion&&phase!=Phase.Choosing)||Time.time>=rounds.Deadline)return;
            var bots=players.Values.Where(p=>p.bot&&p.Alive).OrderBy(p=>p.id).ToArray();
            var humans=players.Values.Where(p=>!p.bot&&p.Alive).OrderBy(p=>p.id).ToArray();
            bool BotSpace(int room,Participant first,Participant second)=>HasPlanSpace(room,first,second)&&
                !bots.Any(b=>b!=first&&b!=second&&!rounds.IsReady(b.id)&&b.chosenRoom==room&&humans.Any(h=>h.id==b.companion&&!rounds.IsReady(h.id)));
            // Locked plans stay stable, exactly like human reservations.
            foreach(var human in humans.Where(p=>!rounds.IsReady(p.id)&&p.companion.HasValue))
            {
                var bot=bots.FirstOrDefault(p=>p.id==human.companion&&!p.bag&&!rounds.IsReady(p.id));
                if(bot==null||!HasPlanSpace(human.chosenRoom,human,bot))continue;
                SetRoomPlan(bot,human.chosenRoom,human.id);TryReady(bot,true);
            }
            // A lone human's locked destination is never an invitation. Only explicit
            // partner requests above can recruit a bot; unmatched choices fall back to Bar.
            foreach(var human in humans.Where(p=>!rounds.IsReady(p.id)&&!p.companion.HasValue))
            {
                var bot=bots.FirstOrDefault(p=>!p.bag&&!rounds.IsReady(p.id)&&p.companion==human.id)
                    ??bots.FirstOrDefault(p=>!p.bag&&!rounds.IsReady(p.id)&&!humans.Any(h=>h.id==p.companion));
                if(bot==null)continue;
                int preferred=bot.companion==human.id?bot.chosenRoom:1+(rounds.Round-1+Array.IndexOf(humans,human))%3;
                int room=Enumerable.Range(0,3).Select(n=>1+(preferred-1+n)%3).FirstOrDefault(r=>BotSpace(r,bot,human));
                if(room>0)SetRoomPlan(bot,room,human.id);
            }
            var available=bots.Where(p=>!rounds.IsReady(p.id)&&!humans.Any(h=>h.id==p.companion&&!rounds.IsReady(h.id))).ToArray();
            for(int i=0;i<available.Length;i++)
            {
                var bot=available[i];
                if(rounds.IsReady(bot.id))continue;
                var peer=available.Skip(i+1).FirstOrDefault(p=>!rounds.IsReady(p.id)&&!p.bag);
                int room=0;
                if(!bot.bag&&peer!=null)
                {
                    int preferred=PracticeRoomCycle[(i+rounds.Round-1)%PracticeRoomCycle.Length];
                    room=Enumerable.Range(0,3).Select(n=>1+(Math.Max(1,preferred)-1+n)%3).FirstOrDefault(r=>BotSpace(r,bot,peer));
                }
                if(room>0)
                {
                    SetRoomPlan(bot,room,peer.id);SetRoomPlan(peer,room,bot.id);TryReady(bot,true);
                }
                else {SetRoomPlan(bot,0,null);TryReady(bot,true);}
            }
        }
    }
}
