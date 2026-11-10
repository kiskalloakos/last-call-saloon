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
        float actions,regroup;
        public bool IsReady(ulong id)=>ready.Contains(id);
        public bool HasSpent(ulong id)=>spent.ContainsKey(id);
        public void SetReady(ulong id,bool value)
        {
            if(Phase!=Phase.Discussion||!order.Contains(id))return;
            if(value){if(ready.Add(id))readyOrder[id]=++nextReady;}
            else {ready.Remove(id);readyOrder.Remove(id);}
        }
        // Legacy duration/limit arguments remain compatible with saved tuning.
        // Discussion is Ready-driven and rounds have no cap.
        public void Start(IEnumerable<ulong> ids,float now,float discussion,float actions,int limit,float choosing=15,float regroup=3)
        {
            order=ids.Distinct().ToArray();if(order.Length==0)throw new ArgumentException("Players required");
            this.actions=Math.Max(1,actions);this.regroup=Math.Max(1,regroup);
            Round=1;spent.Clear();ready.Clear();readyOrder.Clear();nextReady=0;Phase=Phase.Discussion;Deadline=0;
        }
        public bool CanAct(ulong id,float now)=>Phase==Phase.Turns&&order.Contains(id)&&!HasSpent(id)&&now<Deadline;
        public void DelayDeadline(float seconds){if(Phase==Phase.Turns)Deadline+=Math.Max(0,seconds);}
        public void EnsureDeadline(float until){if(Phase==Phase.Turns)Deadline=Math.Max(Deadline,until);}
        public void BeginAftermath(float now){Phase=Phase.Aftermath;Deadline=now+3;}
        public bool TrySpend(ulong id,float now,float duration=0)
        {if(!CanAct(id,now))return false;spent[id]=now+Math.Max(0,duration);return true;}
        public void Tick(float now,Func<ulong,bool> alive,Func<ulong,bool> hasAction=null)
        {
            if(Phase==Phase.Discussion)
            {
                var living=order.Where(alive).ToArray();
                if(living.Length==0||living.Any(id=>!ready.Contains(id)))return;
                spent.Clear();Phase=Phase.Turns;Deadline=now+actions;return;
            }
            if(Phase==Phase.Turns)
            {
                // Discussion still needs every living player. Only playable actions hold up Turns.
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
            {Round++;ready.Clear();readyOrder.Clear();nextReady=0;Phase=Phase.Discussion;Deadline=0;}
        }
        public Dictionary<ulong,int> AssignRooms(IEnumerable<ulong> ids,Func<ulong,int> room,int[] limits=null)
        {
            limits=limits??new[]{5,2,2,2};
            var result=new Dictionary<ulong,int>();var capacity=new int[limits.Length];
            // Ready receipt order is authoritative. Invitations never reserve another seat.
            foreach(var id in ids.OrderBy(id=>readyOrder.TryGetValue(id,out var ticket)?ticket:long.MaxValue).ThenBy(id=>id))
            {
                int choice=room(id);
                if(choice<1||choice>=limits.Length||capacity[choice]>=limits[choice]){result[id]=0;continue;}
                result[id]=choice;capacity[choice]++;
            }
            return result;
        }
    }
}
