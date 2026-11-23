using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        const float ShotRewindSeconds=.5f,CombatHistorySeconds=1;
        public const float AccurateShotSeconds=.5f,QuickShotSpreadDegrees=1.25f,MovingQuickShotSpreadDegrees=3;
        bool localShotCharging,shotHoldArmed;
        float localShotChargeAt;
        int localShotSequence,localShotRound,localShotPose,triggerBlockedFrame=-1;
        public float ShotChargeProgress=>localShotCharging?Mathf.Clamp01((Time.time-localShotChargeAt)/AccurateShotSeconds):0;
        class ShotIntent
        {
            public Ray Ray;
            public float Time;
            public float FromTime,ToTime;
            public bool Interpolated;
            public bool Quick;
            public int Round,PoseVersion;
        }
        class CombatRig
        {
            public SaloonCharacterMotion Motion;
            public int Frame=-1,Hits;
            public int PoseVersion=int.MinValue;
            public bool Seated,Crouched;
        }
        struct CombatFrame
        {
            public float Time,Radius,BodyHeight;
            public Vector3 Head,Body,Eye;
            public int Round,Room,PoseVersion;
            public bool Alive;
        }

        void InitializeCombatRig(Participant p)
        {
            // This unrendered copy uses the same rig/animation as clients, including
            // crouching, drinking and flinches. It remains available when the host
            // cannot see this player (for example, while outside smoking).
            var rig=Instantiate(Resources.Load<GameObject>(SaloonCharacterMotion.ResourcePath),p.body.transform);
            rig.name="Server combat pose";rig.SetActive(false);
            var motion=rig.GetComponent<SaloonCharacterMotion>();motion.Initialize(false);
            p.combatRig=new CombatRig{Motion=motion};
        }

        void PoseCombatRig(Participant p)
        {
            var rig=p.combatRig;if(rig==null)return;
            bool seated=p.seat>=0;
            if(rig.PoseVersion!=p.poseVersion||rig.Seated!=seated||rig.Crouched!=p.crouched)
            {
                rig.Motion.SnapStance(seated,p.crouched);
                rig.PoseVersion=p.poseVersion;rig.Seated=seated;rig.Crouched=p.crouched;
            }
            if(p.hits<rig.Hits)rig.Motion.ClearHit();
            if(p.hits>rig.Hits)rig.Motion.Hit(p.hitDirection,Time.time-p.hitAt);
            rig.Hits=p.hits;
            rig.Motion.transform.rotation=Quaternion.Euler(0,p.bodyYaw,0);
            float progress=1-Mathf.Clamp01((p.busyUntil-Time.time)/Mathf.Max(.01f,tuning.drinkSeconds));
            bool drinking=p.busyAction=="drink";
            float lift=drinking?Mathf.SmoothStep(0,1,Mathf.Min(progress/.2f,(1-progress)/.2f)):0;
            var velocity=CanWalk(p)?Quaternion.Euler(0,p.yaw,0)*new Vector3(p.move.x,0,p.move.y)*tuning.walkSpeed:Vector3.zero;
            rig.Motion.Pose(p.seat>=0,velocity,lift,SaloonWeaponPose.Kick(Time.time-p.shotAt),p.pitch,
                Mathf.DeltaAngle(p.bodyYaw,p.yaw),p.crouched,rig.Frame==Time.frameCount?0:-1);
            rig.Frame=Time.frameCount;
            p.head.center=p.body.transform.InverseTransformPoint(rig.Motion.HeadHitCenter);
            p.head.radius=rig.Motion.HeadHitRadius;
        }

        CombatFrame CurrentCombatFrame(Participant p)=>new CombatFrame
        {
            Time=Time.time,Round=rounds.Round,Room=p.room,PoseVersion=p.poseVersion,Alive=p.Alive,
            Head=p.body.transform.TransformPoint(p.head.center),Radius=p.head.radius,
            Body=p.body.transform.TransformPoint(p.hitbox.center),BodyHeight=p.hitbox.height,Eye=Eye(p)
        };

        void CaptureCombatFrames()
        {
            foreach(var p in players.Values)
            {
                UpdatePlayerColliders(p);
                var frames=p.combatHistory;var frame=CurrentCombatFrame(p);
                if(frames.Count>0&&frames[frames.Count-1].Time==frame.Time)frames[frames.Count-1]=frame;
                else frames.Add(frame);
                while(frames.Count>1&&frames[1].Time<Time.time-CombatHistorySeconds)frames.RemoveAt(0);
            }
        }

        CombatFrame CombatFrameAt(Participant p,float time)
        {
            var frames=p.combatHistory;
            if(frames.Count==0)return CurrentCombatFrame(p);
            for(int i=frames.Count-1;i>0;i--)
            {
                var after=frames[i];var before=frames[i-1];
                if(time<before.Time)continue;
                // Never blend through a teleport, room transfer or stance change.
                if(after.Round!=before.Round||after.Room!=before.Room||after.PoseVersion!=before.PoseVersion)
                    return time<after.Time?before:after;
                float blend=Mathf.InverseLerp(before.Time,after.Time,time);
                before.Head=Vector3.Lerp(before.Head,after.Head,blend);
                before.Body=Vector3.Lerp(before.Body,after.Body,blend);
                before.Eye=Vector3.Lerp(before.Eye,after.Eye,blend);
                before.BodyHeight=Mathf.Lerp(before.BodyHeight,after.BodyHeight,blend);
                before.Radius=Mathf.Lerp(before.Radius,after.Radius,blend);
                return before;
            }
            return frames[0];
        }

        void UpdateRevolverInput(bool allowed,bool usedLeftClick)
        {
            allowed=allowed&&triggerBlockedFrame!=Time.frameCount&&View?.phase==Phase.Turns&&Self?.alive==true&&Self.drawn&&View.actionAvailable;
            if(!allowed||localShotCharging&&(View.round!=localShotRound||Self.poseVersion!=localShotPose||Self.busy))
            {CancelShotCharge();return;}
            // A quick shot replaces a charge; both mouse buttons can never fire together.
            if(Input.GetMouseButtonDown(1)){CancelShotCharge();Shoot();return;}
            if(!Input.GetMouseButton(0)){CancelShotCharge();return;}
            if(Input.GetMouseButtonDown(0)&&!usedLeftClick)shotHoldArmed=true;
            if(shotHoldArmed&&!localShotCharging&&!Self.busy)
            {
                localShotCharging=true;localShotChargeAt=Time.time;localShotRound=View.round;localShotPose=Self.poseVersion;
                localShotSequence=Mathf.Max(localShotSequence,Self.shotChargeSequence)+1;
                Connection.Send(new Command{kind="chargeShot",shotSequence=localShotSequence,shotRound=View.round,poseVersion=Self.poseVersion});
            }
            if(localShotCharging&&Time.time-localShotChargeAt>=AccurateShotSeconds)
            {Shoot(true);localShotCharging=false;shotHoldArmed=false;}
        }

        void CancelShotCharge()
        {
            shotHoldArmed=false;
            if(!localShotCharging)return;
            localShotCharging=false;
            Connection.Send(new Command{kind="cancelShot",shotSequence=localShotSequence});
        }

        void OnApplicationFocus(bool focused){if(!focused)CancelShotCharge();}
        void OnApplicationPause(bool paused){if(paused)CancelShotCharge();}

        bool CanChargeShot(Participant p)=>p.Alive&&p.drawn&&p.room!=3&&Time.time>=p.nextShot&&ActionBlockReason(p)=="";

        void BeginShotCharge(Participant p,Command command)
        {
            if(command.shotRound!=rounds.Round||command.poseVersion!=p.poseVersion||command.shotSequence<=p.shotChargeSequence)return;
            if(!CanChargeShot(p)){p.notice=ActionBlockReason(p);return;}
            p.chargingShot=true;p.shotChargeAt=Time.time;p.shotChargeRound=rounds.Round;p.shotChargePose=p.poseVersion;
            p.shotChargeSequence=command.shotSequence;p.pendingChargeShot=null;
        }

        void CancelShotCharge(Participant p){p.chargingShot=false;p.pendingChargeShot=null;}

        void TickShotCharge(Participant p)
        {
            if(!p.chargingShot)return;
            if(!CanChargeShot(p)||p.shotChargeRound!=rounds.Round||p.shotChargePose!=p.poseVersion||Time.time-p.shotChargeAt>2)
            {CancelShotCharge(p);return;}
            if(p.pendingChargeShot!=null&&Time.time-p.shotChargeAt>=AccurateShotSeconds)
            {
                var intent=p.pendingChargeShot;CancelShotCharge(p);Fire(p,intent);
            }
        }

        public static float QuickShotSpread(float movement)=>Mathf.Lerp(QuickShotSpreadDegrees,MovingQuickShotSpreadDegrees,Mathf.Clamp01(movement));

        static Ray SpreadShot(Ray aim,float degrees,Vector2 sample)
        {
            // Uniform disk spread stays inside the cone. Only the host samples it.
            var basis=Quaternion.LookRotation(aim.direction);
            var offset=Vector2.ClampMagnitude(sample,1)*Mathf.Tan(degrees*Mathf.Deg2Rad);
            return new Ray(aim.origin,basis*new Vector3(offset.x,offset.y,1).normalized);
        }

        public void Shoot(bool accurate=false)
        {
            if(Self==null||View==null)return;
            var rotation=Quaternion.Euler(pitch,yaw,0)*CameraImpact();
            var angles=rotation.eulerAngles;
            var ray=new Ray(ViewCamera.transform.position,rotation*Vector3.forward);
            var visible=avatars.Where(a=>a.Key!=LocalId&&a.Value.Hittable).Select(a=>a.Value)
                .OrderBy(a=>VisibleHeadDistance(a,ray)).FirstOrDefault();
            float visibleTime=visible?.RenderedServerTime??View.serverTime;
            Connection.Send(new Command{kind="fire",shotAim=true,accurateShot=accurate,shotSequence=localShotSequence,yaw=angles.y,pitch=Mathf.DeltaAngle(0,angles.x),
                shotTime=visibleTime,shotInterpolated=true,shotFromTime=visible?.RenderFromTime??visibleTime,
                shotToTime=visible?.RenderToTime??visibleTime,shotRound=View.round,poseVersion=Self.poseVersion});
        }

        static float VisibleHeadDistance(SaloonAvatar avatar,Ray ray)
        {
            var offset=avatar.RenderedHead-ray.origin;float along=Vector3.Dot(offset,ray.direction);
            if(along>=0&&along<=65&&(offset-ray.direction*along).sqrMagnitude<=avatar.HeadRadius*avatar.HeadRadius)return along;
            return along>0?1000+Vector3.Angle(ray.direction,offset):float.PositiveInfinity;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool ShootForVerification(string targetId)
        {
            if(!avatars.TryGetValue(targetId,out var avatar)||!avatar.Hittable)return false;
            // Drive the real trigger/aim serialization path in packaged tests.
            // Camera-impact behavior is checked separately by the shooting suite.
            hitKick=shotKick=0;damageCameraImpulse=Vector3.zero;
            var aim=Quaternion.LookRotation(avatar.RenderedHead-ViewCamera.transform.position).eulerAngles;
            yaw=aim.y;pitch=Mathf.DeltaAngle(0,aim.x);Shoot();return true;
        }
#endif

        CombatFrame ShotFrame(Participant p,ShotIntent intent)
        {
            if(!intent.Interpolated)return CombatFrameAt(p,intent.Time);
            var from=CombatFrameAt(p,intent.FromTime);var to=CombatFrameAt(p,intent.ToTime);
            if(from.Round!=to.Round||from.Room!=to.Room||from.PoseVersion!=to.PoseVersion)return CombatFrameAt(p,intent.Time);
            float blend=Mathf.InverseLerp(intent.FromTime,intent.ToTime,intent.Time);
            from.Head=Vector3.Lerp(from.Head,to.Head,blend);from.Body=Vector3.Lerp(from.Body,to.Body,blend);
            from.Eye=Vector3.Lerp(from.Eye,to.Eye,blend);from.BodyHeight=Mathf.Lerp(from.BodyHeight,to.BodyHeight,blend);
            from.Radius=Mathf.Lerp(from.Radius,to.Radius,blend);
            return from;
        }

        void ReceiveShot(Participant p,Command command)
        {
            if(!p.Alive){p.notice="You are out. You can watch the match.";return;}
            if(command.accurateShot&&(!p.chargingShot||command.shotSequence!=p.shotChargeSequence||
                p.shotChargeRound!=rounds.Round||p.shotChargePose!=p.poseVersion))
            {p.notice="Hold left-click to prepare an accurate shot.";return;}
            var intent=new ShotIntent{Time=Time.time,Round=rounds.Round,PoseVersion=p.poseVersion,
                Ray=new Ray(Eye(p),Forward(p)),Quick=!command.accurateShot};
            if(command.shotAim)
            {
                if(!Finite(command.yaw)||!Finite(command.pitch)||!Finite(command.shotTime)||Mathf.Abs(command.pitch)>89||
                    command.shotTime>Time.time+.05f||command.shotTime<Time.time-ShotRewindSeconds||
                    command.shotRound!=rounds.Round||command.poseVersion>p.poseVersion)
                {p.notice="Your aim is out of date. Aim and shoot again.";return;}
                if(command.shotInterpolated&&(!Finite(command.shotFromTime)||!Finite(command.shotToTime)||
                    command.shotFromTime>command.shotTime||command.shotTime>command.shotToTime||
                    command.shotToTime>Time.time+.05f||command.shotFromTime<Time.time-CombatHistorySeconds))
                {p.notice="Your aim is out of date. Aim and shoot again.";return;}
                CaptureCombatFrames();
                intent=new ShotIntent{Time=command.shotTime,Round=command.shotRound,PoseVersion=p.poseVersion,
                    Interpolated=command.shotInterpolated,FromTime=command.shotFromTime,ToTime=command.shotToTime,Quick=!command.accurateShot};
                var frame=ShotFrame(p,intent);
                bool knownPose=frame.Round==rounds.Round&&frame.Room==p.room&&frame.PoseVersion==command.poseVersion;
                if(command.poseVersion!=p.poseVersion&&!knownPose)
                {p.notice="Your aim is out of date. Aim and shoot again.";return;}
                intent.Ray=new Ray(knownPose?frame.Eye:Eye(p),
                    Quaternion.Euler(command.pitch,command.yaw%360,0)*Vector3.forward);
            }
            if(command.accurateShot)
            {
                if(!CanChargeShot(p)){CancelShotCharge(p);p.notice=ActionBlockReason(p);return;}
                // Jitter can deliver the final trigger slightly before the host's timer.
                // Keep the final aim, but never fire before the full half-second charge.
                if(Time.time-p.shotChargeAt<AccurateShotSeconds)
                {if(p.pendingChargeShot==null)p.pendingChargeShot=intent;return;}
            }
            CancelShotCharge(p);
            if(p.drawn&&p.busyAction=="draw"&&CanFinishDrawShot(p))
            {
                // The completion may be due this frame, before TickServer has run.
                if(!p.fireAfterDraw){p.fireAfterDraw=true;p.pendingShot=intent;}
                return;
            }
            if(p.busyUntil>Time.time){p.notice=ActionBlockReason(p);return;}
            Fire(p,intent);
        }

        RaycastHit? ShotTarget(Participant shooter,ShotIntent intent,Ray ray)
        {
            CaptureCombatFrames();
            if(intent==null)return RayTarget(shooter,ray,65);
            var restored=new List<(Participant Player,Vector3 Head,float Radius,Vector3 Body,float Height)>();
            try
            {
                foreach(var p in players.Values.Where(p=>p!=shooter&&p.Alive&&p.room==shooter.room))
                {
                    var frame=ShotFrame(p,intent);
                    if(!frame.Alive||frame.Round!=rounds.Round||frame.Room!=p.room)continue;
                    restored.Add((p,p.head.center,p.head.radius,p.hitbox.center,p.hitbox.height));
                    p.head.center=p.body.transform.InverseTransformPoint(frame.Head);p.head.radius=frame.Radius;
                    p.hitbox.center=p.body.transform.InverseTransformPoint(frame.Body);p.hitbox.height=frame.BodyHeight;
                }
                return RayTarget(shooter,ray,65);
            }
            finally
            {
                foreach(var saved in restored)
                {
                    saved.Player.head.center=saved.Head;saved.Player.head.radius=saved.Radius;
                    saved.Player.hitbox.center=saved.Body;saved.Player.hitbox.height=saved.Height;
                }
                Physics.SyncTransforms();
            }
        }
    }
}
