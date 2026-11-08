using System;
using System.Collections.Generic;
using System.Linq;
namespace Saloon
{
    public sealed class SaloonRounds
    {
        public Phase Phase { get; private set; }=Phase.Lobby;
        public int Round { get; private set; }
        public int Limit { get; private set; }
        public float Deadline { get; private set; }
        public ulong? Active => null;
        public IReadOnlyList<ulong> Order => order;
        ulong[] order=Array.Empty<ulong>();
        readonly Dictionary<ulong,float> spent=new Dictionary<ulong,float>();
        readonly HashSet<ulong> ready=new HashSet<ulong>();
        float actions,regroup;
        public bool IsReady(ulong id)=>ready.Contains(id);
        public bool HasSpent(ulong id)=>spent.ContainsKey(id);
        public void SetReady(ulong id,bool value){if(Phase!=Phase.Discussion||!order.Contains(id))return;if(value)ready.Add(id);else ready.Remove(id);}
        // Legacy duration arguments remain compatible with saved tuning; discussion is Ready-driven.
        public void Start(IEnumerable<ulong> ids,float now,float discussion,float actions,int limit,float choosing=15,float regroup=3)
        {
            order=ids.Distinct().ToArray();if(order.Length==0)throw new ArgumentException("Players required");
            this.actions=Math.Max(1,actions);this.regroup=Math.Max(1,regroup);
            Limit=Math.Max(1,limit);Round=1;spent.Clear();ready.Clear();Phase=Phase.Discussion;Deadline=0;
        }
        public bool CanAct(ulong id,float now)=>Phase==Phase.Turns&&order.Contains(id)&&!HasSpent(id)&&now<Deadline;
        public void DelayDeadline(float seconds){if(Phase==Phase.Turns)Deadline+=Math.Max(0,seconds);}
        public void EnsureDeadline(float until){if(Phase==Phase.Turns)Deadline=Math.Max(Deadline,until);}
        public void BeginAftermath(float now){Phase=Phase.Aftermath;Deadline=now+3;}
        public bool TrySpend(ulong id,float now,float duration=0)
        {if(!CanAct(id,now))return false;spent[id]=now+Math.Max(0,duration);return true;}
        public void Tick(float now,Func<ulong,bool> alive)
        {
            if(Phase==Phase.Discussion)
            {
                var living=order.Where(alive).ToArray();
                if(living.Length==0||living.Any(id=>!ready.Contains(id)))return;
                spent.Clear();Phase=Phase.Turns;Deadline=now+actions;return;
            }
            if(Phase==Phase.Turns)
            {
                var living=order.Where(alive).ToArray();
                if(living.Any(id=>spent.TryGetValue(id,out var end)&&end>now))return;
                if(now<Deadline&&living.Any(id=>!spent.ContainsKey(id)))return;
                BeginAftermath(now);return;
            }
            if(now<Deadline)return;
            if(Phase==Phase.Choosing){spent.Clear();Phase=Phase.Turns;Deadline=now+actions;}
            else if(Phase==Phase.Aftermath)
            {if(Round>=Limit){Phase=Phase.Finished;return;}Phase=Phase.Regroup;Deadline=now+regroup;}
            else if(Phase==Phase.Regroup)
            {if(Round>=Limit){Phase=Phase.Finished;return;}Round++;ready.Clear();Phase=Phase.Discussion;Deadline=0;}
        }
        public static Dictionary<ulong,int> AssignRooms(IEnumerable<ulong> ids,Func<ulong,int> room,Func<ulong,ulong?> companion,int[] limits=null)
        {
            limits=limits??new[]{5,2,2};
            var all=ids.OrderBy(id=>id).ToArray();var result=all.ToDictionary(id=>id,id=>0);var used=new HashSet<ulong>();var capacity=new int[3];
            foreach(var id in all)
            {
                var other=companion(id);int choice=room(id);
                if(used.Contains(id)||choice<1||choice>2||!other.HasValue||other==id||!result.ContainsKey(other.Value))continue;
                if(companion(other.Value)!=id||room(other.Value)!=choice)continue;
                if(capacity[choice]+2>limits[choice]){used.Add(id);used.Add(other.Value);continue;}
                result[id]=result[other.Value]=choice;used.Add(id);used.Add(other.Value);capacity[choice]=2;
            }
            foreach(var id in all)
            {int choice=room(id);if(used.Contains(id)||choice<1||choice>2||capacity[choice]>=limits[choice])continue;result[id]=choice;capacity[choice]++;}
            return result;
        }
    }
}
