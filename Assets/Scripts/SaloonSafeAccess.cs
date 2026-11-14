using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public const float SafeReach=1.35f;
        static Vector3 SafeApproach=>SaloonArt.SafePosition+Vector3.back*1.05f;
        Collider safeAccessCollider;
        readonly Dictionary<ulong,Queue<Vector3>> botSafeRoutes=new Dictionary<ulong,Queue<Vector3>>();

        bool AtSafe(Vector3 position)
        {
            var offset=position-SaloonArt.SafePosition;
            if(Mathf.Abs(offset.y)>1||Vector3.ProjectOnPlane(offset,Vector3.up).sqrMagnitude>SafeReach*SafeReach)return false;
            if(safeAccessCollider==null)safeAccessCollider=GameObject.Find("Safe visual")?.GetComponent<Collider>();
            if(safeAccessCollider==null||!safeAccessCollider.enabled)return false;
            // A nearby wall/counter still blocks access. Players do not block each other's hands.
            var origin=position+Vector3.up*.9f;var direction=safeAccessCollider.bounds.center-origin;
            return !Physics.RaycastAll(origin,direction.normalized,direction.magnitude,~(1<<SaloonRagdoll.Layer),QueryTriggerInteraction.Ignore)
                .Any(hit=>hit.collider!=safeAccessCollider&&hit.collider.GetComponentInParent<SaloonBody>()==null);
        }

        bool AtSafe(Participant person)=>person.Alive&&person.room==0&&AtSafe(person.body.transform.position);
        public bool NearSafe=>Self?.alive==true&&Self.room==0&&AtSafe(new Vector3(Self.x,Self.y,Self.z));

        void TickSafeWork(Participant person)
        {
            if(person.busyAction!="rob"||AtSafe(person))return;
            person.busyAction="";person.busyUntil=0;
            rounds.SetCompletion(person.id,Time.time);
            person.notice="Safe work interrupted. Stay next to the safe while working.";
        }

        bool BotWantsSafe(Participant person)=>person.role==Role.Outlaw&&person.room==0&&!person.bag&&
            rounds.CanAct(person.id,Time.time)&&!players.Values.Any(p=>p.Alive&&p.bag)&&
            !players.Values.Any(p=>p.Alive&&p.room==0&&Enemies(person,p)&&(p.escapeRound>0||p.hp<=SaloonHealth.RevolverDamage));

        void WalkBotToSafe(Participant person)
        {
            if(AtSafe(person)){person.move=Vector2.zero;person.botMoveWorld=Vector3.zero;return;}
            var position=person.body.transform.position;
            if(!botSafeRoutes.TryGetValue(person.id,out var route)||route.Count==0||Vector3.Distance(position,route.Peek())>2)
            {
                if(Time.time<person.botRoamAt){person.move=Vector2.zero;person.botMoveWorld=Vector3.zero;return;}
                botSafeRoutes[person.id]=route=FindSafeRoute(person);
            }
            while(route.Count>1&&Vector3.ProjectOnPlane(route.Peek()-position,Vector3.up).magnitude<.09f)route.Dequeue();
            var target=route.Count>0?route.Peek():position;
            var direction=Vector3.ProjectOnPlane(target-position,Vector3.up).normalized;
            var path=SafeSegmentClear(person,position,target)?direction:Vector3.zero;
            if(path==Vector3.zero){botSafeRoutes.Remove(person.id);person.botRoamAt=Time.time+.5f;}
            person.botMoveWorld=path;person.yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;person.pitch=0;
            var local=Quaternion.Euler(0,-person.yaw,0)*path;
            OnCommand(person.id,new Command{kind="input",x=local.x*.7f,z=local.z*.7f,yaw=person.yaw,pitch=0,poseVersion=person.poseVersion});
        }

        bool SafeSegmentClear(Participant person,Vector3 from,Vector3 to)
        {
            var delta=Vector3.ProjectOnPlane(to-from,Vector3.up);
            return delta.sqrMagnitude<.0001f||!Physics.CapsuleCastAll(from+Vector3.up*.4f,from+Vector3.up*1.45f,.31f,
                delta.normalized,delta.magnitude,~((1<<8)|(1<<SaloonRagdoll.Layer)),QueryTriggerInteraction.Ignore)
                .Any(hit=>hit.collider.GetComponentInParent<SaloonBody>()?.id!=person.id);
        }

        Queue<Vector3> FindSafeRoute(Participant person)
        {
            // Sample the actual Bar colliders once per route: stools and discussion
            // chairs leave narrow, staggered gaps that a straight steering line misses.
            const int columns=59,rows=17;const float step=.25f;
            var points=new Vector3[columns*rows];var open=new bool[points.Length];
            var previous=Enumerable.Repeat(-1,points.Length).ToArray();var pending=new Queue<int>();
            var position=person.body.transform.position;int start=-1;float nearest=float.MaxValue;
            Physics.SyncTransforms();
            for(int z=0;z<rows;z++)for(int x=0;x<columns;x++)
            {
                int index=z*columns+x;var point=new Vector3(-7.25f+x*step,.08f,-1.1f+z*step);points[index]=point;
                open[index]=!Physics.OverlapCapsule(point+Vector3.up*.4f,point+Vector3.up*1.45f,.31f,
                    ~((1<<8)|(1<<SaloonRagdoll.Layer)),QueryTriggerInteraction.Ignore)
                    .Any(c=>c.GetComponentInParent<SaloonBody>()?.id!=person.id);
                float distance=(point-position).sqrMagnitude;
                if(open[index]&&distance<nearest&&SafeSegmentClear(person,position,point)){start=index;nearest=distance;}
            }
            var route=new Queue<Vector3>();if(start<0)return route;
            previous[start]=start;pending.Enqueue(start);int goal=-1;
            while(pending.Count>0)
            {
                int current=pending.Dequeue();
                if(AtSafe(points[current])){goal=current;break;}
                int x=current%columns,z=current/columns;
                foreach(var next in new[]{x>0?current-1:-1,x+1<columns?current+1:-1,z>0?current-columns:-1,z+1<rows?current+columns:-1})
                {
                    if(next<0||!open[next]||previous[next]>=0||!SafeSegmentClear(person,points[current],points[next]))continue;
                    previous[next]=current;pending.Enqueue(next);
                }
            }
            if(goal<0)return route;
            var reverse=new List<Vector3>();
            for(int index=goal;;index=previous[index]){reverse.Add(points[index]);if(index==start)break;}
            reverse.Reverse();foreach(var point in reverse)route.Enqueue(point);return route;
        }
    }
}
