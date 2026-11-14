using System;
using System.Collections.Generic;
using System.Linq;
namespace Saloon
{
    public sealed class SaloonRounds
    {
        public Phase Phase { get; private set; }=Phase.Lobby;
        public int Round { get; private set; }
        // Kept for snapshot compatibility: zero means unlimited.
        public int Limit => 0;
        public float Deadline { get; private set; }
        public ulong? Active => null;
        public IReadOnlyList<ulong> Order => order;
        ulong[] order=Array.Empty<ulong>();
        readonly Dictionary<ulong,float> spent=new Dictionary<ulong,float>();
        readonly HashSet<ulong> ready=new HashSet<ulong>();
        readonly Dictionary<ulong,long> readyOrder=new Dictionary<ulong,long>();
        long nextReady;
        float discussion,actions,regroup;
        public bool IsReady(ulong id)=>ready.Contains(id);
        public bool HasSpent(ulong id)=>spent.ContainsKey(id);
        public void SetReady(ulong id,bool value)
        {
            if(Phase!=Phase.Discussion||!order.Contains(id))return;
            if(value){if(ready.Add(id))readyOrder[id]=++nextReady;}
            else {ready.Remove(id);readyOrder.Remove(id);}
        }
        // Everyone locking in can end discussion early; the timer prevents an idle player stalling it.
        public void Start(IEnumerable<ulong> ids,float now,float discussion,float actions,int limit,float choosing=15,float regroup=3)
        {
            order=ids.Distinct().ToArray();if(order.Length==0)throw new ArgumentException("Players required");
            this.discussion=Math.Max(1,discussion);this.actions=Math.Max(1,actions);this.regroup=Math.Max(1,regroup);
            Round=1;spent.Clear();ready.Clear();readyOrder.Clear();nextReady=0;Phase=Phase.Discussion;Deadline=now+this.discussion;
        }
        public bool CanAct(ulong id,float now)=>Phase==Phase.Turns&&order.Contains(id)&&!HasSpent(id)&&now<Deadline;
        public void DelayDeadline(float seconds){if(Phase==Phase.Turns)Deadline+=Math.Max(0,seconds);}
        public void EnsureDeadline(float until){if(Phase==Phase.Turns)Deadline=Math.Max(Deadline,until);}
        public void BeginAftermath(float now){Phase=Phase.Aftermath;Deadline=now+3;}
        public bool TrySpend(ulong id,float now,float duration=0)
        {if(!CanAct(id,now))return false;spent[id]=now+Math.Max(0,duration);return true;}
        // Pouring reserves its follow-up sip/offer inside the same action.
        public void SetCompletion(ulong id,float until){if(spent.ContainsKey(id))spent[id]=until;}
        public void Tick(float now,Func<ulong,bool> alive,Func<ulong,bool> hasAction=null)
        {
            if(Phase==Phase.Discussion)
            {
                var living=order.Where(alive).ToArray();
                if(living.Length==0||(now<Deadline&&living.Any(id=>!ready.Contains(id))))return;
                spent.Clear();Phase=Phase.Turns;Deadline=now+actions;return;
            }
            if(Phase==Phase.Turns)
            {
                // Only playable actions hold up Turns.
                var living=order.Where(alive).Where(id=>hasAction==null||hasAction(id)).ToArray();
                if(living.Any(id=>spent.TryGetValue(id,out var end)&&end>now))return;
                if(now<Deadline&&living.Any(id=>!spent.ContainsKey(id)))return;
                BeginAftermath(now);return;
            }
            if(now<Deadline)return;
            if(Phase==Phase.Choosing){spent.Clear();Phase=Phase.Turns;Deadline=now+actions;}
            else if(Phase==Phase.Aftermath)
            {Phase=Phase.Regroup;Deadline=now+regroup;}
            else if(Phase==Phase.Regroup)
            {Round++;ready.Clear();readyOrder.Clear();nextReady=0;Phase=Phase.Discussion;Deadline=now+discussion;}
        }
        public Dictionary<ulong,int> AssignRooms(IEnumerable<ulong> ids,Func<ulong,int> room,int[] limits=null,bool preventSolo=false)
        {
            limits=limits??new[]{5,2,2,2};
            var result=new Dictionary<ulong,int>();var capacity=new int[limits.Length];
            // Lock order is authoritative; pair acceptance locks both seats together.
            foreach(var id in ids.OrderBy(id=>readyOrder.TryGetValue(id,out var ticket)?ticket:long.MaxValue).ThenBy(id=>id))
            {
                int choice=room(id);
                if(choice<1||choice>=limits.Length||capacity[choice]>=limits[choice]){result[id]=0;continue;}
                result[id]=choice;capacity[choice]++;
            }
            if(preventSolo)
            {
                // Only the two minigames require company. The Bar and smoking deck
                // both allow solo visits; never recruit another player to fill a room.
                foreach(var group in result.Where(p=>p.Value==1||p.Value==2).GroupBy(p=>p.Value).Where(g=>g.Count()==1).ToArray())
                    result[group.First().Key]=0;
            }
            return result;
        }
    }
}
