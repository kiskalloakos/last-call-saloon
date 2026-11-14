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
        public IEnumerator VerifySafeHighlight()
        {
            verifying=true;AutomatedInput=true;VerificationPassed=VerificationFinished=false;
            int checks=0;
            void Check(bool ok,string message)
            {checks++;if(!ok){VerificationFinished=true;Debug.LogError("SALOON_SAFE_HIGHLIGHT FAIL "+message);throw new InvalidOperationException(message);}}
            var host=players[Connection.LocalId];var marker=SafeHighlight;
            string directory=Path.Combine(Path.GetTempPath(),"LastCall-SafeHighlight");Directory.CreateDirectory(directory);
            var safe=GameObject.Find("Safe visual").transform;
            var originals=safe.GetComponentsInChildren<MeshRenderer>();
            var materials=originals.Select(r=>r.sharedMaterials).ToArray();
            var colors=materials.Select(row=>row.Select(m=>m.color).ToArray()).ToArray();
            void Publish(){Broadcast();marker.Refresh();}
            void Place(Vector3 position)
            {host.controller.enabled=false;host.seat=-1;host.body.transform.position=position;host.move=Vector2.zero;UpdatePlayerColliders(host);Physics.SyncTransforms();}
            void Window(Role role)
            {
                ResetHeist();outcome="";roomTurns.Clear();duels.Clear();MenuOpen=ShowRole=GraphicsMenuOpen=false;
                HitFlash=hitKick=0;HealthDelta=0;previousHp=100;host.name="Preview player";
                var deck=SaloonRules.Deck.ToList();deck.Remove(role);int i=0;
                foreach(var p in players.Values)
                {p.hp=100;p.deathAt=-1;p.role=p==host?role:deck[i++];p.chosenRoom=p.room=0;p.companion=null;p.busyAction="";p.busyUntil=0;p.botThink=Time.time+100;p.beer=0;p.beerChoiceUntil=0;}
                rounds.Start(players.Keys,Time.time,300,300,0);foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(Time.time,id=>true);phase=Phase.Turns;PositionEveryone(false);Place(SafeApproach);Publish();
            }
            var shader=Resources.Load<Shader>("Shaders/SaloonSafeHighlight");
            Check(shader!=null&&shader.isSupported,"bundled depth-tested highlight shader is supported");
            foreach(var role in new[]{Role.Outlaw,Role.Sheriff})
            {
                Window(role);string key=role==Role.Outlaw?"H":"C";
                Check(marker.Highlighted&&marker.Key==key&&marker.OverlayCount>0,role+" receives a highlighted physical safe with the correct key");
                Check(marker.Status=="Press "+key+" to interact",role+" receives an actionable prompt");
                Check(safe.GetComponentsInChildren<Transform>().Where(t=>t.name=="Safe proximity highlight").All(t=>t.GetComponent<Collider>()==null),"visual highlight adds no gameplay colliders");
                externalCamera=true;externalPosition=SaloonArt.SafePosition+new Vector3(-1.9f,1.8f,-2.6f);
                externalRotation=Quaternion.LookRotation(safe.GetComponent<Collider>().bounds.center-externalPosition);
                UpdateCamera();yield return null;yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,role+"-near.png"));yield return null;
                externalCamera=false;RefreshViewpoint();
                var direction=safe.GetComponent<Collider>().bounds.center-Eye(host);
                yaw=host.yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
                pitch=host.pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
                cameraCut=true;Publish();UpdateCamera();yield return null;yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,role+"-first-person.png"));yield return null;
                Place(new Vector3(-4,.08f,0));Publish();
                Check(!marker.Highlighted&&marker.Key=="",role+" loses highlight and key when outside reach");
                Place(SafeApproach);Publish();
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=SaloonArt.SafePosition+new Vector3(0,1,-.65f);
                wall.transform.localScale=new Vector3(1.5f,2,.08f);Physics.SyncTransforms();marker.Refresh();
                Check(!marker.Highlighted,role+" receives no highlight through an obstruction");Destroy(wall);yield return null;Physics.SyncTransforms();marker.Refresh();
                Check(marker.Highlighted,role+" highlight returns once access is clear");
                MenuOpen=true;marker.Refresh();Check(!marker.Highlighted,"pause menu hides the safe highlight");MenuOpen=false;
                ShowRole=true;marker.Refresh();Check(!marker.Highlighted,"Tab hides the safe highlight");ShowRole=false;
                GraphicsMenuOpen=true;marker.Refresh();Check(!marker.Highlighted,"graphics menu hides the safe highlight");GraphicsMenuOpen=false;
                host.hp=0;Publish();Check(!marker.Highlighted,"dead players cannot receive an interaction highlight");host.hp=100;
                phase=Phase.Discussion;Publish();Check(!marker.Highlighted,"discussion does not advertise a room-action interaction");
                phase=Phase.Finished;Publish();Check(!marker.Highlighted,"match completion clears the highlight");phase=Phase.Turns;
            }
            foreach(var role in new[]{Role.Deputy,Role.Renegade})
            {Window(role);Check(!marker.Highlighted&&marker.Key=="",role+" receives no unauthorized safe key");}
            Window(Role.Outlaw);host.bag=true;Publish();Check(!marker.Highlighted,"money carrier is not invited to collect the bag again");
            Window(Role.Outlaw);BeginRobbery(host);Publish();
            Check(marker.Highlighted&&marker.Key=="H"&&marker.Status.Contains("stay beside"),"active safe work keeps its highlight and tells the player to stay nearby");
            host.busyAction="";host.busyUntil=0;Publish();
            Check(marker.Highlighted&&marker.Key=="H"&&marker.Status!="Press H to interact","spent action retains the safe location with an unavailable status");
            for(int i=0;i<originals.Length;i++)
                Check(originals[i].sharedMaterials.SequenceEqual(materials[i])&&materials[i].Select(m=>m.color).SequenceEqual(colors[i]),"original safe material identities and colors remain unchanged");
            marker.enabled=false;Check(!marker.Highlighted&&safe.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Safe proximity highlight").All(r=>!r.enabled),"disabling the component clears every overlay");
            marker.enabled=true;MenuOpen=true;marker.Refresh();
            externalCamera=false;RefreshViewpoint();
            Debug.Log("SALOON_SAFE_HIGHLIGHT PASS "+checks+" checks; screenshots="+directory);
            VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
