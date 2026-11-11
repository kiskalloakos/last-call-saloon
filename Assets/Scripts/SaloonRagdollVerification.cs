#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public System.Collections.IEnumerator VerifyEditorRagdoll()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string message)
            {
                if(!ok){VerificationFinished=true;Debug.LogError("SALOON_RAGDOLL FAIL "+message);throw new InvalidOperationException(message);}
                Debug.Log("SALOON_RAGDOLL_CHECK "+message);
            }
            string directory=Path.Combine(Path.GetTempPath(),"SaloonPrototype-EditorPreview");Directory.CreateDirectory(directory);
            var host=players[Connection.LocalId];var victim=players.Values.First(p=>p.bot);var others=players.Values.Where(p=>p!=host&&p!=victim).ToArray();
            void Window(int room=0)
            {
                pendingOutcome="";outcome="";externalCamera=false;
                foreach(var p in players.Values){p.hp=60;p.deathAt=-1;p.deathDirection=Vector3.zero;p.hitbox.enabled=true;p.busyUntil=0;p.drawn=false;p.shots=0;p.nextShot=0;p.lastAction=-100;p.ammo=6;p.chosenRoom=0;p.companion=null;}
                host.role=Role.Sheriff;victim.role=Role.Outlaw;others[0].role=Role.Outlaw;others[1].role=Role.Deputy;others[2].role=Role.Renegade;
                host.chosenRoom=victim.chosenRoom=room;
                others[0].chosenRoom=others[1].chosenRoom=room==0?1:0;others[2].chosenRoom=room==0?2:0;
                rounds.Start(players.Keys,Time.time,0,120,6);foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
                if(duels.TryGetValue(room,out var duel))
                {duel.claimed=duel.flipping=duel.revealed=true;duel.first=host.id;duel.result="HEADS";duel.firstUntil=Time.time+30;}
                Broadcast();
            }
            void Shoot(Participant shooter,Participant target)
            {
                if(duels.TryGetValue(shooter.room,out var duel))duel.first=shooter.id;
                else
                {
                    var turn=roomTurns[shooter.room];turn.order=new[]{shooter.id}.Concat(turn.order.Where(id=>id!=shooter.id)).ToArray();turn.index=0;turn.until=Time.time+30;
                }
                target.hp=20;shooter.drawn=true;shooter.nextShot=0;
                var direction=Eye(target)-Eye(shooter);shooter.yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;shooter.pitch=0;
                Fire(shooter);Broadcast();
            }
            System.Collections.IEnumerator Capture(string name,Vector3 center,Vector3? offset=null)
            {
                externalCamera=true;externalPosition=center+(offset??new Vector3(.5f,1.3f,-3));externalRotation=Quaternion.LookRotation(center-externalPosition);
                yield return null;
                WindowPreview(externalPosition,center,ViewCamera.fieldOfView,name);
                yield return null;
            }
            Window();yield return new WaitForSeconds(.2f);Shoot(host,victim);yield return new WaitForSeconds(.08f);
            var avatar=avatars[victim.id.ToString()];var rag=avatar.DeathBody;
            Check(!victim.Alive&&rag!=null&&rag.BodyCount==11&&avatar.CorpseVisible,"a real lethal shot starts an eleven-body jointed ragdoll");
            var original=rag.Center;var wire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(host.id),out _)).players.Single(p=>p.id==victim.id.ToString());
            Check(!wire.observer&&wire.deathAge>=0&&new Vector3(wire.deathX,wire.deathY,wire.deathZ).sqrMagnitude>.9f,"death direction and observer phase survive the network snapshot");
            yield return new WaitForSeconds(.3f);
            Check(Vector3.Distance(original,rag.Center)>.25f,"ragdoll is thrown away from its seat");
            yield return Capture("death-01-airborne",rag.Center);
            yield return new WaitForSeconds(1.7f);
            Debug.Log("SALOON_RAGDOLL_GROUND center="+rag.Center+" speed="+rag.Speed);
            Check(rag.Center.y<.65f&&rag.Speed<1&&avatar.CorpseVisible&&!ObserverReady(victim),"body lands and rests before spectator mode");
            yield return Capture("death-02-on-floor",rag.Center);
            yield return new WaitForSeconds(1.1f);Broadcast();yield return null;
            Check(ObserverReady(victim)&&avatar.CorpseVisible&&rag.Settled&&rag.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"body remains on the ground after observer mode with resting physics and no blocking colliders");
            var resting=rag.Center;Broadcast();yield return new WaitForSeconds(.2f);
            Check(avatar.CorpseVisible&&Vector3.Distance(resting,rag.Center)<.01f,"later snapshots preserve the fallen pose");
            yield return Capture("death-03-body-retained",rag.Center);
            var corpseState=PrivateSnapshot(host.id).players.Single(p=>p.id==victim.id.ToString());
            var lateAvatar=new GameObject("Late observer corpse probe").AddComponent<SaloonAvatar>();
            lateAvatar.Initialize(this,corpseState,0);lateAvatar.Apply(corpseState,false);
            Check(corpseState.observer&&lateAvatar.CorpseVisible&&lateAvatar.DeathBody!=null,"receiving an already-spectating player still creates their corpse");
            Destroy(lateAvatar.gameObject);

            Window();yield return new WaitForSeconds(.2f);Shoot(victim,host);yield return new WaitForSeconds(.15f);
            Check(!Self.alive&&!IsSpectating&&!View.actionAvailable&&avatars[LocalId].CorpseVisible,"local death shows the body and locks actions before observer mode");
            Check(Vector3.Distance(ViewCamera.transform.position,Eye(host))>.7f,"death camera pulls out to follow the ragdoll");
            WindowPreview(ViewCamera.transform.position,ViewCamera.transform.position+ViewCamera.transform.forward,ViewCamera.fieldOfView,"death-local-camera");
            yield return new WaitForSeconds(SaloonRagdoll.ObserverDelay+.1f);Broadcast();yield return null;
            Check(IsSpectating&&CameraPlayer.alive&&avatars[LocalId].CorpseVisible,"observer POV starts while the local body remains visible");
            var localCorpse=avatars[LocalId].DeathBody;var corpsePosition=localCorpse.Center;
            string previous=ViewpointId;CycleSpectator(1);Broadcast();yield return null;
            Check(IsSpectating&&ViewpointId!=previous&&CameraPlayer.alive&&avatars[LocalId].CorpseVisible,"switching spectator targets keeps the corpse visible");
            var oldSeat=host.body.transform.position;foreach(var p in players.Values.Where(p=>p.Alive))p.chosenRoom=0;
            PositionEveryone(true);Broadcast();yield return null;
            Check(avatars[LocalId].CorpseVisible&&Vector3.Distance(corpsePosition,localCorpse.Center)<.01f,"regrouping leaves the dead body in its original resting place");
            PositionEveryone(false);Broadcast();yield return null;
            Check(players.Values.Any(p=>p.Alive&&p.body.transform.position==oldSeat)&&avatars[LocalId].CorpseVisible,"the fallen body remains visible while its former seat can be reused");
            Window(1);yield return new WaitForSeconds(.2f);Shoot(host,victim);yield return new WaitForSeconds(2.4f);
            rag=avatars[victim.id.ToString()].DeathBody;
            Check(rag!=null&&rag.Center.y<.65f&&rag.Speed<1,"side-room ragdoll also lands on the floor after hitting the room wall");
            yield return Capture("death-04-side-room-floor",rag.Center,new Vector3(-1,1.5f,1));
            externalCamera=false;phase=Phase.Finished;StartMatch();yield return new WaitForSeconds(.2f);
            Check(players.Values.All(p=>p.Alive)&&avatars.Values.All(a=>a.DeathBody==null&&a.GetComponentsInChildren<Rigidbody>(true).Length==0),"new match restores living models and removes ragdoll physics");
            Debug.Log("SALOON_RAGDOLL PASS");VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
