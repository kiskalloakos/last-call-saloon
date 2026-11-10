#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyRoundCompletion()
        {
            verifying=true;AutomatedInput=true;VerificationPassed=VerificationFinished=false;
            void Check(bool ok,string text)
            {
                if(!ok){VerificationFinished=true;throw new InvalidOperationException("Round completion: "+text);}
                Debug.Log("SALOON_COMPLETION_CHECK "+text);
            }
            yield return null;
            var host=players[Connection.LocalId];var smoker=players.Values.First(p=>p.bot);
            void Setup()
            {
                ResetHeist();outcome="";
                foreach(var p in players.Values){p.hp=tuning.health;p.deathAt=-1;p.drawn=false;p.role=Role.Deputy;p.chosenRoom=0;p.companion=null;p.busyUntil=0;p.busyAction="";p.beer=0;p.lastAction=-100;}
                host.role=Role.Sheriff;smoker.role=Role.Outlaw;smoker.chosenRoom=3;
                rounds.Start(players.Keys,Time.time,30,30,1,1,1);phase=rounds.Phase;
                foreach(var p in players.Values)rounds.SetReady(p.id,true);TickServer();
                Check(PrivateSnapshot(host.id).actionsRemaining==players.Count-1,"remaining-action count excludes the smoker");
                Check(phase==Phase.Turns&&smoker.room==3&&rounds.Deadline-Time.time>=30,"smoking and indoor turns begin with a full fallback deadline");
            }
            void Pass(Participant p){p.lastAction=-100;OnCommand(p.id,new Command{kind="pass"});}
            Setup();Broadcast();yield return new WaitForSeconds(.25f);Check(View.actionsRemaining==players.Count-1,"client receives the live unspent-action count");var queue=roomTurns[0].order.Select(id=>players[id]).ToArray();
            foreach(var p in queue.Take(queue.Length-1))Pass(p);
            TickServer();Check(PrivateSnapshot(host.id).actionsRemaining==1,"one unspent action is replicated");Check(phase==Phase.Turns,"an indoor player who has not acted still gets their turn");
            Pass(queue.Last());TickServer();
            Check(phase==Phase.Aftermath&&!rounds.HasSpent(smoker.id),"all indoor passes immediately end actions without waiting on the smoker");
            Check(rounds.Deadline-Time.time<=3.01f,"only the three-second aftermath remains");
            Broadcast();yield return null;Check(View.phase==Phase.Aftermath&&View.remaining<=3.01f,"clients receive aftermath instead of the stale action timer");
            Setup();queue=roomTurns[0].order.Select(id=>players[id]).ToArray();
            foreach(var p in queue.Take(queue.Length-1))Pass(p);
            var last=queue.Last();last.hp=tuning.health-1;last.beer=1;last.lastAction=-100;
            float duration=tuning.drinkSeconds;tuning.drinkSeconds=.4f;OnCommand(last.id,new Command{kind="drink"});TickServer();
            Check(phase==Phase.Turns&&last.busyAction=="drink","early completion never cuts off the last player's drink");
            yield return new WaitForSeconds(.55f);
            Check(last.hp==tuning.health&&phase==Phase.Aftermath,"the last drink heals before automatically starting aftermath");tuning.drinkSeconds=duration;
            Setup();foreach(var p in roomTurns[0].order.Select(id=>players[id]))Pass(p);
            smoker.hp=0;TickServer();Check(phase==Phase.Aftermath,"eliminated players cannot delay completed actions either");
            yield return VerifyCombatFeedback();
            VerificationPassed=VerificationFinished=true;Debug.Log("SALOON_COMPLETION PASS");
        }
        IEnumerator VerifyCombatFeedback()
        {
            void Check(bool ok,string text){if(!ok)throw new InvalidOperationException("Combat feedback: "+text);Debug.Log("SALOON_COMBAT_FEEDBACK_CHECK "+text);}
            var host=players[Connection.LocalId];var target=players.Values.First(p=>p.bot);
            void Window()
            {
                ResetHeist();outcome="";
                foreach(var p in players.Values){p.hp=tuning.health;p.deathAt=-1;p.role=Role.Deputy;p.chosenRoom=0;p.companion=null;p.busyUntil=0;p.busyAction="";p.lastAction=-100;p.ammo=tuning.cylinder;p.drawn=false;p.headshots=p.reloads=0;p.confirmedAt=-100;}
                host.role=Role.Sheriff;target.role=Role.Outlaw;
                rounds.Start(players.Keys,Time.time,30,30,1);phase=rounds.Phase;
                foreach(var p in players.Values)rounds.SetReady(p.id,true);TickServer();
                var turn=roomTurns[0];turn.order=new[]{host.id}.Concat(players.Keys.Where(id=>id!=host.id)).ToArray();turn.index=0;turn.until=Time.time+30;
            }
            Window();Fire(host);Check(host.notice.Contains("holstered")&&!rounds.HasSpent(host.id),"holstered trigger explains Q without consuming the action");
            host.lastAction=-100;OnCommand(host.id,new Command{kind="reload"});Check(host.notice.Contains("Draw"),"reload explains the holstered weapon");
            host.drawn=true;host.lastAction=-100;OnCommand(host.id,new Command{kind="reload"});Check(host.notice.Contains("already full")&&!rounds.HasSpent(host.id),"unneeded reload explains the full cylinder");
            var direction=Eye(target)-Eye(host);host.yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;host.pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;host.nextShot=0;Physics.SyncTransforms();Fire(host);
            var wire=Snapshot.FromWireJson(PrivateSnapshot(host.id).ToWireJson());
            Check(string.IsNullOrEmpty(host.notice)&&string.IsNullOrEmpty(target.notice),"a confirmed hit emits no headshot or damage toast");
            Check(wire.hitSequence==1&&wire.confirmedHealth==tuning.health-1&&wire.confirmedMaxHealth==tuning.health&&!wire.confirmedFriendly,"confirmed headshot and resulting HP survive the wire");
            Check(PrivateSnapshot(target.id).hitSequence==0,"only the shooter gets their confirmation event");
            Broadcast();yield return new WaitForSeconds(.25f);
            var muzzle=ViewCamera.transform.Find("Low-poly revolver").GetComponentsInChildren<MeshFilter>(true).First(m=>m.sharedMesh!=null&&m.sharedMesh.name=="Crossed muzzle flame");
            Check(muzzle.sharedMesh.vertexCount==18,"muzzle flash has intersecting flame planes");
            Check(SaloonWeaponPose.Offset(.035f).z<-.07f&&Quaternion.Angle(Quaternion.identity,SaloonWeaponPose.Rotation(.035f))>18,"shot kick drives a stronger barrel rise and backward impulse");
            Check(wire.actionBlockReason.Contains("Action spent")&&wire.actionsRemaining==players.Count-1,"spent-action explanation and countdown agree");
            TakeDamage(host);Broadcast();Check(HealthDelta==-1&&HealthDeltaUntil>Time.unscaledTime,"local damage raises a readable HP loss");host.hp++;Broadcast();Check(HealthDelta==1,"healing raises a readable HP gain");
            Window();host.drawn=true;host.ammo=2;float duration=tuning.reloadSeconds;tuning.reloadSeconds=.5f;host.lastAction=-100;OnCommand(host.id,new Command{kind="reload"});
            wire=Snapshot.FromWireJson(PrivateSnapshot(host.id).ToWireJson());
            Check(wire.players.First(p=>p.id==host.id.ToString()).reloading&&wire.players.First(p=>p.id==host.id.ToString()).reloads==1&&wire.actionBlockReason.Contains("Reloading"),"reload event and busy explanation replicate");
            foreach(var p in players.Values.Where(p=>p!=host))rounds.TrySpend(p.id,Time.time);
            wire=PrivateSnapshot(host.id);Check(wire.actionsRemaining==0&&wire.actionsInProgress==1,"zero actions left distinguishes the last reload still finishing");
            var clip=SaloonGunAudio.Reload;var samples=new float[clip.samples];clip.GetData(samples,0);
            Check(clip!=SaloonGunAudio.Shot&&clip.length>SaloonGunAudio.Shot.length&&samples.Any(v=>Mathf.Abs(v)>.05f)&&samples.All(v=>!float.IsNaN(v)&&Mathf.Abs(v)<=1),"reload has a distinct audible cylinder and cartridge sound");
            Broadcast();yield return new WaitForSeconds(.65f);Check(host.ammo==tuning.cylinder&&phase==Phase.Aftermath,"reload finishes before aftermath and replenishes the cylinder");tuning.reloadSeconds=duration;
            Check(Resources.Load<Font>("Fonts/Rye-Regular")!=null&&Resources.Load<Font>("Fonts/BarlowSemiCondensed-Medium")!=null,"custom display and body fonts are bundled");
            var stepsObject=new GameObject("Footstep verification");var steps=stepsObject.AddComponent<SaloonFootsteps>();
            steps.Tick(Vector3.zero,true,false,true,true,false);
            for(int i=1;i<=8;i++)steps.Tick(Vector3.right*i*.2f,true,false,true,true,false);
            int count=steps.StepsPlayed;Check(count>=2,"distance-based walking produces footsteps");
            steps.Tick(Vector3.right*1.6f,true,false,true,true,false);
            steps.Tick(Vector3.right*1.8f,true,true,true,true,false);
            steps.Tick(Vector3.right*2,true,false,false,true,false);
            steps.Tick(Vector3.right*2.2f,false,false,true,true,false);
            steps.Tick(Vector3.right*20,true,false,true,true,false);
            Check(steps.StepsPlayed==count,"idle, sitting, stale movement, death and teleports produce no footsteps");
            var wood=SaloonFootsteps.Create(false,20);var dirt=SaloonFootsteps.Create(true,20);
            var a=new float[wood.samples];var b=new float[dirt.samples];wood.GetData(a,0);dirt.GetData(b,0);
            Check(a.All(v=>!float.IsNaN(v)&&Mathf.Abs(v)<=1)&&a.Any(v=>Mathf.Abs(v)>.1f)&&!a.SequenceEqual(b),"wood and dirt have distinct valid footstep waveforms");
            Destroy(wood);Destroy(dirt);Destroy(stepsObject);
            Window();host.drawn=false;host.lastAction=-100;int before=host.shots;
            OnCommand(host.id,new Command{kind="draw"});
            Check(host.busyUntil-Time.time>.20f&&host.busyUntil-Time.time<.30f&&SaloonWeaponPose.DrawSeconds==.25f,"draw and visual raise share a quarter-second reaction window");
            OnCommand(host.id,new Command{kind="fire"});OnCommand(host.id,new Command{kind="fire"});
            Check(host.fireAfterDraw&&host.shots==before,"rapid Q and clicks buffer one shot at the bar without firing instantly");
            yield return new WaitForSeconds(.10f);Check(host.shots==before,"buffer respects the short reaction window");
            yield return new WaitForSeconds(.20f);Check(host.shots==before+1&&rounds.HasSpent(host.id),"buffered bar shot fires once as soon as the draw finishes");
            Window();host.lastAction=-100;OnCommand(host.id,new Command{kind="pass"});
            var responder=players[roomTurns[0].order[roomTurns[0].index]];responder.lastAction=-100;before=responder.shots;
            OnCommand(responder.id,new Command{kind="draw"});OnCommand(responder.id,new Command{kind="fire"});
            Check(responder.fireAfterDraw,"responding bar players also retain quick trigger input");
            yield return new WaitForSeconds(.30f);Check(responder.shots==before+1,"responding player fires after the short draw");
            Window();host.lastAction=-100;before=host.shots;OnCommand(host.id,new Command{kind="draw"});OnCommand(host.id,new Command{kind="fire"});
            rounds.TrySpend(host.id,Time.time);yield return new WaitForSeconds(.30f);
            Check(host.shots==before&&!host.fireAfterDraw,"queued trigger cannot bypass an action that was already spent");
            Check(Resources.LoadAll<AudioClip>("Audio/Footsteps").Length==5,"five pre-produced wooden footstep variations are bundled");
            yield return VerifyTableDodgeAfterShot();
            Debug.Log("SALOON_COMBAT_FEEDBACK PASS");
        }
        IEnumerator VerifyTableDodgeAfterShot()
        {
            void Check(bool ok,string text){if(!ok)throw new InvalidOperationException("Table dodge: "+text);Debug.Log("SALOON_TABLE_DODGE_CHECK "+text);}
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            foreach(int room in new[]{1,2})foreach(bool hostFirst in new[]{true,false})
            {
                ResetHeist();outcome="";
                foreach(var p in players.Values){p.hp=3;p.deathAt=-1;p.role=Role.Deputy;p.chosenRoom=0;p.companion=null;p.busyAction="";p.busyUntil=0;p.lastAction=-100;p.lastStance=-100;p.nextShot=0;p.ammo=6;p.drawn=false;}
                host.role=Role.Sheriff;peer.role=Role.Outlaw;host.chosenRoom=peer.chosenRoom=room;
                rounds.Start(players.Keys,Time.time,30,30,1);phase=rounds.Phase;
                foreach(var id in players.Keys)rounds.SetReady(id,true);TickServer();
                OnCommand(host.id,new Command{kind="stance"});Check(host.seat>=0,"room "+room+" keeps the seat locked during the minigame");host.lastStance=-100;
                var d=duels[room];d.revealed=true;d.flipping=true;d.first=hostFirst?host.id:peer.id;d.firstUntil=Time.time+20;
                foreach(var actor in new[]{players[d.first],players[d.first==host.id?peer.id:host.id]})
                {
                    actor.drawn=true;actor.yaw=SaloonStations.Facing(false,room,actor==host?0:1);actor.pitch=-65;actor.lastAction=-100;
                    int shots=actor.shots;OnCommand(actor.id,new Command{kind="fire"});
                    Check(actor.shots==shots+1&&rounds.HasSpent(actor.id),"room "+room+" spends the shooter action");
                    OnCommand(actor.id,new Command{kind="stance"});
                    Check(actor.seat<0&&actor.controller.enabled&&CanWalk(actor),"room "+room+" allows F immediately after shooting, on either seat and either turn");
                    Check(SaloonMovement.InRoom(actor.body.transform.position,room),"standing never leaves the assigned room");
                    Vector3 before=actor.body.transform.position;bool moved=false;
                    foreach(var direction in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down})
                    {
                        float until=Time.time+.13f;
                        while(Time.time<until){OnCommand(actor.id,new Command{kind="input",x=direction.x,z=direction.y,yaw=0,pitch=0,poseVersion=actor.poseVersion});yield return null;}
                        if(Vector3.Distance(before,actor.body.transform.position)>.10f){moved=true;break;}
                    }
                    OnCommand(actor.id,new Command{kind="input",poseVersion=actor.poseVersion});
                    if(!moved)Debug.Log("DODGE_DIAGNOSTIC before="+before+" after="+actor.body.transform.position+" phase="+phase+" canWalk="+CanWalk(actor)+" flags="+actor.controller.collisionFlags+" colliders="+string.Join(",",Physics.OverlapCapsule(actor.body.transform.position+Vector3.up*.3f,actor.body.transform.position+Vector3.up*1.5f,.3f).Select(c=>c.name)));
                    Check(moved&&rounds.HasSpent(actor.id),"room "+room+" permits real dodge movement after the action is spent");
                }
            }
            Debug.Log("SALOON_TABLE_DODGE PASS");
        }
    }
}
#endif
