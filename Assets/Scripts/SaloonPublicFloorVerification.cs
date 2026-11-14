#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyPublicFloor()
        {
            verifying=true;AutomatedInput=true;VerificationPassed=VerificationFinished=false;
            int checks=0;
            void Check(bool ok,string message)
            {checks++;if(!ok){VerificationFinished=true;Debug.LogError("SALOON_PUBLIC_FLOOR FAIL "+message);throw new InvalidOperationException(message);}}
            var host=players[Connection.LocalId];var peers=players.Values.Where(p=>p!=host).ToArray();
            bool sandbox=Sandbox;Sandbox=false;
            try
            {
                void Assign(int[] sideChoices,int hostRoom=0,bool expired=false)
                {
                    ResetHeist();outcome="";duels.Clear();roomTurns.Clear();host.name="Preview player";
                    var deck=SaloonRules.Deck.ToList();deck.Remove(Role.Outlaw);int i=0;
                    foreach(var p in players.Values)
                    {p.hp=100;p.deathAt=-1;p.role=p==host?Role.Outlaw:deck[i++];p.busyAction="";p.busyUntil=0;p.beer=0;p.beerChoiceUntil=0;p.move=Vector2.zero;p.companion=null;}
                    rounds.Start(players.Keys,Time.time-(expired?31:0),30,180,0);phase=rounds.Phase;PositionEveryone(true);
                    host.chosenRoom=hostRoom;host.planSelected=true;host.bag=true;host.escapeRound=1;
                    for(int n=0;n<peers.Length;n++){peers[n].chosenRoom=sideChoices[n];peers[n].planSelected=true;}
                    foreach(var id in players.Keys)if(!expired||id==host.id)rounds.SetReady(id,true);
                    TickServer();Broadcast();
                }
                Assign(new[]{1,1,3,3});
                Check(phase==Phase.Turns&&host.room==0&&players.Values.Count(p=>p.Alive&&p.room==0)==1,"explicit Bar choice can start alone");
                Check(peers[0].room==1&&peers[1].room==1&&peers[2].room==3&&peers[3].room==3,"a solo Bar leaves both side-room pairs in their chosen destinations");
                Check(host.escapeRound==1&&View.actionAvailable,"solo Bar preserves the carrier's escape and available action");
                Check(!View.players.Any(p=>p.id==peers[2].id.ToString()||p.id==peers[3].id.ToString()),"private smoking activity stays separate from the solo Bar");
                Assign(new[]{1,2,3,3});
                Check(host.room==0&&peers[0].room==0&&peers[1].room==0,"only unmatched side-room occupants fall back to the Bar");
                Check(peers[2].room==3&&peers[3].room==3&&players.Values.Where(p=>p.room==3).All(p=>p.chosenRoom==3),"fallback never recruits anyone into the private smoking area");
                foreach(bool expired in new[]{false,true})
                {
                    Assign(new[]{0,0,0,0},3,expired);
                    Check(phase==Phase.Turns&&host.room==3&&host.companion==null&&host.seat<0,"solo smoking reaches the deck through "+(expired?"discussion timeout":"everyone locking early"));
                    Check(SaloonSmokingArea.Contains(host.body.transform.position)&&host.cigaretteBuff,"solo smoker is outside and receives the entry buff");
                    Check(PrivateSnapshot(host.id).players.Length==1,"solo smoking preserves outdoor privacy");
                }
                Assign(new[]{1,1,2,2});
                host.bag=false;host.escapeRound=0;host.drawn=false;
                host.controller.enabled=false;host.seat=-1;host.body.transform.position=new Vector3(2,.08f,1.45f);
                host.crouched=host.crouchHeld=false;host.yaw=yaw=0;host.pitch=pitch=0;host.poseVersion++;host.vertical=0;
                UpdatePlayerColliders(host);host.controller.enabled=true;Physics.SyncTransforms();
                Check(SaloonMovement.InRoom(new Vector3(0,.08f,-2.4f),0)&&SaloonMovement.InRoom(new Vector3(0,.08f,-6.4f),0),"Bar movement includes the back of the center table and entrance aisle");
                Check(!SaloonMovement.InRoom(new Vector3(-5,.08f,-3.8f),0)&&!SaloonMovement.InRoom(new Vector3(5,.08f,-3.8f),0),"public walking area excludes both private minigame interiors");
                IEnumerator WalkTo(Vector3 target)
                {
                    float until=Time.time+4;
                    while(Time.time<until&&Vector3.ProjectOnPlane(target-host.body.transform.position,Vector3.up).magnitude>.12f)
                    {
                        var direction=Vector3.ProjectOnPlane(target-host.body.transform.position,Vector3.up).normalized;
                        OnCommand(host.id,new Command{kind="input",x=direction.x,z=direction.z,yaw=0,pitch=0,poseVersion=host.poseVersion});yield return null;
                    }
                    OnCommand(host.id,new Command{kind="input",poseVersion=host.poseVersion});
                    Check(Vector3.ProjectOnPlane(target-host.body.transform.position,Vector3.up).magnitude<.18f,"real collision movement reaches table-route point "+target+"; actual="+host.body.transform.position);
                }
                foreach(var target in new[]{new Vector3(2,.08f,-2.4f),new Vector3(-2,.08f,-2.4f),new Vector3(-2,.08f,1.45f),new Vector3(2,.08f,1.45f)})
                    yield return WalkTo(target);
                Check(host.room==0&&!rounds.HasSpent(host.id),"walking a complete loop around the center table preserves Bar assignment and action");
                yield return WalkTo(new Vector3(2,.08f,-2.4f));yield return WalkTo(new Vector3(0,.08f,-2.4f));yield return WalkTo(new Vector3(0,.08f,-6.1f));
                var door=FindAnyObjectByType<SaloonEntranceDoor>();Check(door!=null&&door.GetComponentsInChildren<BoxCollider>().Length==2,"outside entrance has two closed physical door leaves");
                Physics.SyncTransforms();
                Check(Physics.RaycastAll(new Vector3(0,1.5f,-6.1f),Vector3.back,1.8f).Any(h=>h.collider.GetComponentInParent<SaloonEntranceDoor>()==door),"closed entrance physically blocks the outside opening");
                host.yaw=yaw=180;host.pitch=pitch=0;host.lastAction=-100;OnCommand(host.id,new Command{kind="interact"});
                Check(host.room==0&&host.body.transform.position.z>-6.55f&&!rounds.HasSpent(host.id),"E at the entrance does not enter, exit, or spend an action");
                var directory=Path.Combine(Path.GetTempPath(),"LastCall-PublicFloor");Directory.CreateDirectory(directory);
                MenuOpen=ShowRole=false;cameraCut=true;Broadcast();UpdateCamera();yield return null;yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,"ClosedEntrance.png"));yield return null;
                externalCamera=true;externalPosition=new Vector3(2.3f,3.3f,-4.8f);externalRotation=Quaternion.LookRotation(new Vector3(0,.4f,-.5f)-externalPosition);
                yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"CenterTable.png"));yield return null;
                externalCamera=false;RefreshViewpoint();
                Debug.Log("SALOON_PUBLIC_FLOOR PASS "+checks+" checks; screenshots="+directory);
                VerificationPassed=VerificationFinished=true;
            }
            finally{Sandbox=sandbox;}
        }
    }
}
#endif
