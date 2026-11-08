using UnityEngine;
using System.Linq;

namespace Saloon
{
    public static class SaloonMovement
    {
        public const int SolidLayer=10;
        public static Vector3 ClampToRoom(Vector3 p,int room)
        {
            if(room==1){p.x=Mathf.Clamp(p.x,-6.65f,-3.35f);p.z=Mathf.Clamp(p.z,-5.35f,-2.02f);}
            else if(room==2){p.x=Mathf.Clamp(p.x,3.35f,6.65f);p.z=Mathf.Clamp(p.z,-5.35f,-2.02f);}
            else {p.x=Mathf.Clamp(p.x,-7.4f,7.4f);p.z=Mathf.Clamp(p.z,-1.25f,6.4f);}
            return p;
        }
        public static bool InRoom(Vector3 p,int room)=>(ClampToRoom(p,room)-p).sqrMagnitude<.0001f;
    }

    public partial class SaloonGame
    {
        bool CanWalk(Participant p)=>p.Alive&&p.seat<0&&(phase==Phase.Lobby||phase==Phase.Turns);

        // Movement runs independently of turn/action authorization, just like human input.
        void TickBotMovement(Participant p)
        {
            if(!p.Alive||phase!=Phase.Turns)
            {p.move=Vector2.zero;p.botMoveWorld=Vector3.zero;p.botRoamAt=0;return;}
            if(Time.time<p.botRoamAt&&p.seat>=0)return;
            if(p.seat>=0)
            {
                Stand(p);p.botRoamAt=Time.time+.45f;
                if(p.seat>=0)return;
                p.botRoamAt=0;
            }
            if(p.busyUntil>Time.time&&(p.busyAction=="rob"||p.busyAction=="drink"||p.busyAction=="giftDrink"||p.busyAction=="give"))
            {p.move=Vector2.zero;p.botMoveWorld=Vector3.zero;p.botRoamAt=0;return;}
            var threat=players.Values.Where(x=>x!=p&&x.Alive&&x.room==p.room&&Enemies(p,x))
                .OrderBy(x=>(x.body.transform.position-p.body.transform.position).sqrMagnitude).FirstOrDefault();
            var toward=threat==null?Vector3.forward:Vector3.ProjectOnPlane(threat.body.transform.position-p.body.transform.position,Vector3.up).normalized;
            // Keep the weapon facing the opponent while stepping sideways. Never change a committed aim.
            if(threat!=null&&p.botAimAt<0&&p.busyUntil<=Time.time)
            {p.yaw=Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg;p.pitch=0;}
            bool blocked=p.botMoveWorld.sqrMagnitude>.01f&&!BotPathClear(p,p.botMoveWorld);
            if(Time.time>=p.botRoamAt||blocked)
            {
                p.botMoveWorld=Vector3.zero;
                p.botRoamAt=Time.time+UnityEngine.Random.Range(.55f,1.1f);
                if(blocked||UnityEngine.Random.value>.18f)
                {
                    var sideways=Vector3.Cross(Vector3.up,toward)*(UnityEngine.Random.value<.5f?-1:1);
                    foreach(float angle in new[]{0f,180f,45f,-45f,135f,-135f,90f,-90f})
                    {
                        var direction=Quaternion.Euler(0,angle,0)*sideways;
                        if(!BotPathClear(p,direction))continue;
                        p.botMoveWorld=direction;break;
                    }
                    if(p.botMoveWorld==Vector3.zero)p.botRoamAt=Time.time+.2f;
                }
                else p.botRoamAt=Time.time+UnityEngine.Random.Range(.2f,.4f);
            }
            var local=Quaternion.Euler(0,-p.yaw,0)*p.botMoveWorld;
            OnCommand(p.id,new Command{kind="input",x=local.x*.7f,z=local.z*.7f,yaw=p.yaw,pitch=p.pitch,poseVersion=p.poseVersion});
        }

        bool BotPathClear(Participant p,Vector3 direction)
        {
            var position=p.body.transform.position;
            if(!SaloonMovement.InRoom(position+direction*.65f,p.room))return false;
            Physics.SyncTransforms();
            var hits=Physics.CapsuleCastAll(position+Vector3.up*.4f,position+Vector3.up*1.45f,.31f,direction,.65f,
                ~((1<<8)|(1<<SaloonRagdoll.Layer)),QueryTriggerInteraction.Ignore);
            return !hits.Any(hit=>hit.collider.GetComponentInParent<SaloonBody>()?.id!=p.id);
        }

        void UpdatePlayerColliders(Participant p)
        {
            float headHeight=(p.seat>=0?1.35f:1.65f)-.04f;
            p.head.center=Vector3.up*headHeight;p.head.radius=.13f;p.head.enabled=p.Alive;
            float bodyHeight=headHeight-.135f;
            p.hitbox.height=bodyHeight;p.hitbox.center=Vector3.up*(bodyHeight*.5f);p.hitbox.enabled=p.Alive;
            p.solid.height=bodyHeight;p.solid.center=p.hitbox.center;p.solid.enabled=p.Alive&&p.seat>=0&&phase==Phase.Turns;
        }

        void ToggleStance(Participant p)
        {
            if(phase!=Phase.Turns){p.notice="Stand and walk during room actions. Discussion stays at the table.";return;}
            if(p.seat>=0){Stand(p);return;}
            InteractionPoint nearest=null;float best=1.35f;
            foreach(var point in FindObjectsByType<InteractionPoint>())
            {
                if(point.kind!=InteractionKind.Seat||!SaloonMovement.InRoom(point.transform.position,p.room))continue;
                float distance=Vector3.Distance(Vector3.ProjectOnPlane(point.transform.position-p.body.transform.position,Vector3.up),Vector3.zero);
                if(distance>=best||players.Values.Any(other=>other!=p&&other.Alive&&other.seat>=0&&Vector3.ProjectOnPlane(other.body.transform.position-point.transform.position,Vector3.up).magnitude<.55f))continue;
                nearest=point;best=distance;
            }
            if(nearest==null){p.notice="Move closer to an empty chair or stool, then press F to sit.";return;}
            p.controller.enabled=false;p.body.transform.position=nearest.transform.position+Vector3.up*(nearest.gameObject.name=="stoolBar"?.35f:.08f);
            p.seat=1000+nearest.index;p.yaw=nearest.sitYaw;p.pitch=0;p.poseVersion++;p.move=Vector2.zero;p.vertical=0;
            p.notice="Seated. F to stand and dodge.";UpdatePlayerColliders(p);
        }
    }
}
