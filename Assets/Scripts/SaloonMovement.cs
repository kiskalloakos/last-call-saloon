using UnityEngine;
using System.Linq;

namespace Saloon
{
    public static class SaloonMovement
    {
        public const int SolidLayer=10;
        public const float BarPublicBackZ=3.05f;
        public const float StandingHeight=1.8f,CrouchingHeight=1.17f,CrouchDrop=.63f,CrouchSpeed=.45f;
        public static float EyeHeight(bool seated,bool crouched)=>seated?1.35f:crouched?1.02f:1.65f;
        public static Vector3 ClampToRoom(Vector3 p,int room)
        {
            if(room==3)return SaloonSmokingArea.Clamp(p);
            if(room==SaloonBathroom.Room)return SaloonBathroom.Clamp(p);
            if(room==1){p.x=Mathf.Clamp(p.x,-6.65f,-3.35f);p.z=Mathf.Clamp(p.z,-5.35f,-2.02f);}
            else if(room==2){p.x=Mathf.Clamp(p.x,3.35f,6.65f);p.z=Mathf.Clamp(p.z,-5.35f,-2.02f);}
            else
            {
                // The public floor includes the center aisle and entrance, around the
                // discussion table. The rooms meet the outer and entrance walls.
                var bar=new Vector3(Mathf.Clamp(p.x,-7.4f,7.4f),p.y,Mathf.Clamp(p.z,-1.25f,BarPublicBackZ));
                var aisle=new Vector3(Mathf.Clamp(p.x,-2.65f,2.65f),p.y,Mathf.Clamp(p.z,-6.55f,BarPublicBackZ));
                var nearest=(bar-p).sqrMagnitude<(aisle-p).sqrMagnitude?bar:aisle;
                p=nearest;
            }
            return p;
        }
        public static bool InRoom(Vector3 p,int room)=>(ClampToRoom(p,room)-p).sqrMagnitude<.0001f;
    }

    public partial class SaloonGame
    {
        bool CanWalk(Participant p)=>p.Alive&&p.seat<0&&!TableLocksSeat(p)&&(phase==Phase.Lobby||phase==Phase.Turns);

        // Movement runs independently of turn/action authorization, just like human input.
        void TickBotMovement(Participant p)
        {
            if(p.room==3||p.room==SaloonBathroom.Room){p.move=Vector2.zero;p.botMoveWorld=Vector3.zero;return;}
            if(TableLocksSeat(p)){p.move=Vector2.zero;p.botMoveWorld=Vector3.zero;return;}
            if(!p.Alive||phase!=Phase.Turns)
            {p.move=Vector2.zero;p.botMoveWorld=Vector3.zero;p.botRoamAt=0;return;}
            if(Time.time<p.botRoamAt&&p.seat>=0)return;
            if(p.seat>=0)
            {
                Stand(p);p.botRoamAt=Time.time+.45f;
                if(p.seat>=0)return;
                p.botRoamAt=0;
            }
            if(p.busyUntil>Time.time&&(p.busyAction=="rob"||p.busyAction=="drink"||p.busyAction=="toilet"))
            {p.move=Vector2.zero;p.botMoveWorld=Vector3.zero;p.botRoamAt=0;return;}
            if(BotWantsBeer(p)){WalkBotToBeer(p);return;}
            if(BotWantsSafe(p)){WalkBotToSafe(p);return;}
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

        bool BotWantsBeer(Participant p)=>p.Alive&&p.room==0&&p.hp<=2*SaloonHealth.RevolverDamage&&beers>0&&
            phase==Phase.Turns&&rounds.CanAct(p.id,Time.time)&&p.busyUntil<=Time.time;

        void WalkBotToBeer(Participant p)
        {
            var toward=Vector3.ProjectOnPlane(SaloonBeerTaps.Position-p.body.transform.position,Vector3.up).normalized;
            var direction=NearBeerTray(p)?Vector3.zero:toward;
            if(direction!=Vector3.zero&&!BotPathClear(p,direction))
            {
                direction=Vector3.zero;
                foreach(float angle in new[]{45f,-45f,90f,-90f})
                {var candidate=Quaternion.Euler(0,angle,0)*toward;if(BotPathClear(p,candidate)){direction=candidate;break;}}
            }
            p.botMoveWorld=direction;p.botRoamAt=0;
            var local=Quaternion.Euler(0,-p.yaw,0)*direction;
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

        void UpdateCrouch(Participant p)
        {
            bool requested=p.crouchHeld&&Time.time-p.lastInput<=.3f&&CanWalk(p);
            if(p.seat>=0||!p.Alive)p.crouched=false;
            else if(requested)p.crouched=true;
            else if(p.crouched&&CanStandUpright(p))p.crouched=false;
            float height=p.crouched?SaloonMovement.CrouchingHeight:SaloonMovement.StandingHeight;
            if(!Mathf.Approximately(p.controller.height,height))
            {
                // Keep the feet in place when changing capsule height.
                p.controller.height=height;p.controller.center=Vector3.up*(height*.5f);
            }
        }

        bool CanStandUpright(Participant p)
        {
            Physics.SyncTransforms();
            float radius=p.controller.radius-p.controller.skinWidth;
            var position=p.body.transform.position;
            var overlaps=Physics.OverlapCapsule(position+Vector3.up*(radius+.04f),
                position+Vector3.up*(SaloonMovement.StandingHeight-radius),radius,
                ~((1<<8)|(1<<SaloonRagdoll.Layer)),QueryTriggerInteraction.Ignore);
            return !overlaps.Any(c=>c.GetComponentInParent<SaloonBody>()?.id!=p.id);
        }

        void UpdatePlayerColliders(Participant p)
        {
            PoseCombatRig(p);
            p.head.enabled=p.Alive;
            float bodyHeight=Mathf.Max(.5f,p.head.center.y-p.head.radius-.01f);
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
            if(nearest==null){p.notice=p.room==3?"Move closer to an empty bench spot, then press Space to sit.":"Move closer to an empty chair or stool, then press Space to sit.";return;}
            p.controller.enabled=false;p.body.transform.position=nearest.transform.position+Vector3.up*(nearest.gameObject.name=="stoolBar"?.35f:.08f);
            p.crouched=p.crouchHeld=false;UpdateCrouch(p);
            p.seat=1000+nearest.index;p.yaw=p.bodyYaw=nearest.sitYaw;p.pitch=0;p.poseVersion++;p.move=Vector2.zero;p.vertical=0;
            p.notice="";UpdatePlayerColliders(p);
        }
    }
}
