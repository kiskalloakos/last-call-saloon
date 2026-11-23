#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
#if UNITY_EDITOR
        public IEnumerator VerifyRoomWindows()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string text)
            {
                if(!ok){VerificationFinished=true;throw new InvalidOperationException("Room walls: "+text);}
                Debug.Log("SALOON_WINDOW_CHECK "+text);
            }
            yield return new WaitForSeconds(.1f);
            yield return VerifyDiscussionHealth();
            VerifyExteriorWindows(Check);
            var legacy=FindObjectsByType<SaloonRoomWall>();
            legacy[0].layoutVersion=0;legacy[0].barrier.size=new Vector3(2.5f,2.1f,.12f);
            SaloonRoomWindows.Apply();
            var walls=FindObjectsByType<SaloonRoomWall>();
            Check(walls.Length==2&&walls.All(p=>p.barrier.size.x>=3.8f),"saved room layouts are upgraded instead of keeping the old doorway");
            SaloonRoomWindows.Apply();Check(FindObjectsByType<SaloonRoomWall>().Length==2,"reapplying the room layout creates no duplicate walls");
            Check(walls.Length==2,"both minigame rooms have solid front walls");
            Check(GameObject.Find("Saloon World").GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Room screen").All(t=>!t.gameObject.activeInHierarchy),"old solid front screens are removed");
            Check(SaloonStations.Name(0)=="Bar"&&RoomCapacities()[0]==SaloonRules.BarCapacity&&SaloonStations.Position(false,0,0)==new Vector3(-5,.35f,2.05f),"Bar supports eight stations facing the walls");
            Check(GameObject.Find("Saloon World/Bar front").GetComponent<Collider>().enabled,"existing Bar counter remains in place");
            for(int slot=0;slot<SaloonRules.BarCapacity;slot++)
            {
                var station=SaloonStations.Position(false,0,slot);
                var forward=Quaternion.Euler(0,SaloonStations.Facing(false,0,slot),0)*Vector3.forward;
                foreach(float x in new[]{-5f,5f})
                {
                    var toward=Vector3.ProjectOnPlane(new Vector3(x,0,SaloonRoomWindows.FrontZ)-station,Vector3.up);
                    Check(Vector3.Angle(forward,toward)<90,"Bar slot "+slot+" starts facing both minigame walls");
                }
            }
            var probe=new GameObject("Window collision probe");var controller=probe.AddComponent<CharacterController>();controller.height=.6f;controller.radius=.18f;controller.skinWidth=.02f;
            foreach(var window in walls)
            {
                Check(window.barrier.enabled&&!window.barrier.isTrigger&&window.barrier.size.x>=2.5f,"full-height wall is a permanent solid collider in "+SaloonStations.Name(window.room));
                var center=window.transform.position;
                foreach(int side in new[]{1,-1})
                {
                    var start=center+Vector3.forward*.9f*side;var direction=Vector3.back*side;
                    Physics.SyncTransforms();
                    Check(Physics.Raycast(start,direction,out var hit,1.8f,~0,QueryTriggerInteraction.Ignore)&&hit.collider==window.barrier,"bullet ray hits wall from side "+side);
                    var end=SaloonRoomWindows.ClipShotTrail(start,direction,20);
                    Check(Mathf.Abs(end.z-SaloonRoomWindows.FrontZ)<.11f,"shot trail stops on wall from side "+side);
                    controller.enabled=false;probe.transform.position=start;controller.enabled=true;controller.Move(direction*1.8f);
                    Check((probe.transform.position.z-center.z)*side>.19f,"character controller cannot cross wall from side "+side);
                }
                foreach(float x in new[]{-1.75f,1.4f,1.75f})
                {
                    var start=center+new Vector3(x,0,.9f);Physics.SyncTransforms();
                    Check(Physics.Raycast(start,Vector3.back,out var formerDoor,1.8f,~0,QueryTriggerInteraction.Ignore)&&formerDoor.collider==window.barrier,"full-width wall seals the former doorway and edges");
                    controller.enabled=false;probe.transform.position=start;controller.enabled=true;controller.Move(Vector3.back*1.8f);
                    Check(probe.transform.position.z>center.z+.15f,"there is no walkable doorway beside the wall");
                }
                Check(Physics.Raycast(new Vector3(center.x,3.85f,center.z+.9f),Vector3.back,1.8f,~0,QueryTriggerInteraction.Ignore),"front header seals the space below the ceiling");
                probe.SetActive(false);
                CheckWindowRendering(window,Check);
                probe.SetActive(true);
            }
            probe.SetActive(false);Destroy(probe);

            yield return VerifyRoomHearing();

            // Render the same physical table for the person watching from the Bar.
            var host=players[Connection.LocalId];var bots=players.Values.Where(p=>p.bot).ToArray();
            foreach(var p in players.Values){p.hp=60;p.chosenRoom=0;p.companion=null;p.drawn=false;p.busyUntil=0;}
            bots[0].chosenRoom=bots[1].chosenRoom=2;bots[2].chosenRoom=bots[3].chosenRoom=1;
            rounds.Start(players.Keys,Time.time,30,30,6);foreach(var id in players.Keys)rounds.SetReady(id,true);
            rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
            var duel=duels[2];duel.blackjack=new SaloonBlackjack(new System.Random(0));
            var watching=Snapshot.FromWireJson(PrivateSnapshot(host.id).ToWireJson());
            Check(watching.duel==null&&watching.blackjackTable==null&&watching.coinTable==null,"Bar viewer receives no hidden room table state");
            Check(watching.blackjackTable==null,"watching players receive no blackjack controls or opening permissions");
            Broadcast();yield return new WaitForSeconds(1.7f);
            Check(!BlackjackTable.Visible,"Bar viewer has no physical cards to inspect through the opaque wall");
            Check(BlackjackTable.transform.Find("Blackjack cards")==null||BlackjackTable.transform.Find("Blackjack cards").GetComponentsInChildren<Collider>().All(c=>!c.enabled),"outside viewer has no clickable table controls");
            externalCamera=true;RefreshViewpoint();
            WindowPreview(new Vector3(5,1.75f,1.2f),new Vector3(5,1.55f,-3.8f),72,"outside");
            WindowPreview(new Vector3(5,1.7f,-3.25f),new Vector3(5,1.7f,-1),78,"inside");
            externalCamera=false;RefreshViewpoint();

            Check(SaloonPrivacy.ShareActivity(Phase.Turns,2,0)&&SaloonPrivacy.VoiceChannel(VoiceRoom,Phase.Turns,2,true)==SaloonPrivacy.VoiceChannel(VoiceRoom,Phase.Turns,0,true),"room occupants retain indoor sounds and the same positional voice channel");
            var indoorListener=bots[0];var barSource=host;
            Check(PrivateSnapshot(indoorListener.id).players.Any(p=>p.id==barSource.id.ToString()),"indoor listeners still receive saloon sound sources");

            // Same logical room on both sides: the physical wall, rather than the room rule,
            // must be what prevents these otherwise valid headshots.
            duels.Clear();roomTurns.Clear();
            var victim=bots[0];var pane=walls.First(w=>w.room==2);
            foreach(var p in new[]{host,victim}){p.room=0;p.seat=-1;p.controller.enabled=false;p.drawn=true;p.ammo=6;p.nextShot=0;p.lastAction=-100;}
            host.role=Role.Sheriff;victim.role=Role.Outlaw;
            host.body.transform.position=pane.transform.position+new Vector3(0,-1.62f,.9f);
            victim.body.transform.position=pane.transform.position+new Vector3(0,-1.62f,-.9f);
            foreach(var p in new[]{host,victim})UpdatePlayerColliders(p);
            foreach(var shooter in new[]{host,victim})
            {
                var other=shooter==host?victim:host;var direction=Eye(other)-Eye(shooter);shooter.yaw=Quaternion.LookRotation(direction).eulerAngles.y;shooter.pitch=0;
                Check(Target(shooter,65)?.collider==pane.barrier,"host targeting reaches wall before the opponent");
                int hp=other.hp,shots=shooter.shots;OnCommand(shooter.id,new Command{kind="fire"});
                Check(shooter.shots==shots+1&&other.hp==hp&&pane.barrier.enabled,"wall absorbs a real shot and stays intact");
            }
            rounds.BeginAftermath(Time.time);phase=rounds.Phase;Broadcast();yield return null;
            Check(!BlackjackTable.Visible&&View.blackjackTable==null,"visible world table clears after the action phase");
            VerificationPassed=VerificationFinished=true;Debug.Log("SALOON_WINDOWS PASS");
        }
        void VerifyExteriorWindows(Action<bool,string> check)
        {
            var world=GameObject.Find("Saloon World");SaloonRoomWindows.Apply();SaloonRoomWindows.Apply();
            var root=world.transform.Find(SaloonExteriorWindows.RootName);
            var panes=root.GetComponentsInChildren<BoxCollider>().Where(c=>c.enabled&&c.name==SaloonExteriorWindows.PaneName).ToArray();
            check(panes.Length==6,"four Bar and two minigame exterior windows are installed without duplicates");
            check(world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="West wall"||t.name=="East wall"||t.name=="Room outer wall infill"||t.name=="Room side"&&Mathf.Abs(t.position.x)>6.5f).All(t=>!t.gameObject.activeSelf),"saved solid outer walls are replaced by window openings through every wall layer");
            var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);marker.name="Exterior window visibility probe";
            marker.GetComponent<Collider>().enabled=false;marker.transform.localScale=new Vector3(.35f,.65f,.65f);
            var material=new Material(Shader.Find("Unlit/Color")){color=Color.green};marker.GetComponent<Renderer>().sharedMaterial=material;
            var cameraPosition=ViewCamera.transform.position;var cameraRotation=ViewCamera.transform.rotation;float fov=ViewCamera.fieldOfView;int mask=ViewCamera.cullingMask;
            var movementProbe=new GameObject("Exterior glass collision probe");var controller=movementProbe.AddComponent<CharacterController>();controller.height=.6f;controller.radius=.18f;controller.skinWidth=.02f;
            try
            {
                SaloonSmokingArea.ApplyCamera(ViewCamera,false);
                foreach(var pane in panes)
                {
                    var center=pane.bounds.center;float side=Mathf.Sign(center.x);var direction=Vector3.right*side;
                    var inside=center-direction*.65f;marker.transform.position=center+direction*2.5f;marker.layer=SaloonSunset.EntranceLayer;
                    ViewCamera.transform.SetPositionAndRotation(inside,Quaternion.LookRotation(direction));ViewCamera.fieldOfView=70;
                    check(WindowMarkerPixels(true)>150,"exterior scenery is visible through "+pane.transform.parent.name);
                    marker.layer=SaloonSmokingArea.OutdoorLayer;
                    check(WindowMarkerPixels(true)==0,"private back-deck objects stay invisible through "+pane.transform.parent.name);
                    marker.SetActive(false);Physics.SyncTransforms();
                    var first=Physics.RaycastAll(inside,direction,4,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).First();
                    check(first.collider==pane&&!pane.isTrigger,"exterior glass stops bullet rays in "+pane.transform.parent.name);
                    check(Mathf.Abs(SaloonRoomWindows.ClipShotTrail(inside,direction,20).x-center.x)<.1f,"shot trail ends at the exterior glass");
                    controller.enabled=false;movementProbe.transform.position=inside;controller.enabled=true;controller.Move(direction*2);
                    check((movementProbe.transform.position.x-center.x)*side<-.1f,"exterior window cannot be walked through");
                    WindowPreview(center-direction*1.8f,center,78,pane.transform.parent.name.Replace(' ','-'));
                    marker.SetActive(true);
                }
                check((ViewCamera.cullingMask&(1<<SaloonSmokingArea.OutdoorLayer))==0,"all exterior views retain the smoking visibility boundary");
            }
            finally
            {
                marker.SetActive(false);movementProbe.SetActive(false);Destroy(marker);Destroy(material);Destroy(movementProbe);
                ViewCamera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);ViewCamera.fieldOfView=fov;ViewCamera.cullingMask=mask;
            }
        }
        IEnumerator VerifyRoomHearing()
        {
            var host=players[Connection.LocalId];var source=players.Values.Last(p=>p.bot);
            foreach(var p in players.Values){p.chosenRoom=0;p.companion=null;p.hp=60;p.busyUntil=0;}
            host.chosenRoom=players.Values.First(p=>p!=host&&p!=source).chosenRoom=2;rounds.Start(players.Keys,Time.time,30,30,6);foreach(var id in players.Keys)rounds.SetReady(id,true);
            rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);Broadcast();yield return null;
            source.drawn=true;source.yaw=0;source.pitch=60;source.ammo=6;source.nextShot=0;source.lastAction=-100;
            int before=source.shots;OnCommand(source.id,new Command{kind="fire"});Broadcast();
            var audio=avatars[source.id.ToString()].GetComponents<AudioSource>();
            bool playing=source.shots==before+1&&audio.Any(a=>a.isPlaying&&a.maxDistance>Vector3.Distance(Eye(host),Eye(source)));
            playing&=avatars[source.id.ToString()].GetComponent<AudioLowPassFilter>().enabled;
            if(!playing){VerificationFinished=true;throw new InvalidOperationException("Enclosed rooms must still receive indoor gunshot audio.");}
            Debug.Log("SALOON_WINDOW_CHECK enclosed room still plays a saloon gunshot through the wall");
            yield return null;
        }
#endif
        void CheckWindowRendering(SaloonRoomWall pane,Action<bool,string> check)
        {
            var greenMaterial=new Material(Shader.Find("Unlit/Color")){color=new Color(.02f,1,.02f)};
            var redMaterial=new Material(Shader.Find("Unlit/Color")){color=new Color(1,.02f,.02f)};
            var green=GameObject.CreatePrimitive(PrimitiveType.Cube);green.name="Inside visibility marker";green.transform.localScale=Vector3.one*.4f;
            var red=GameObject.CreatePrimitive(PrimitiveType.Cube);red.name="Outside visibility marker";red.transform.localScale=Vector3.one*.4f;
            green.GetComponent<Renderer>().sharedMaterial=greenMaterial;red.GetComponent<Renderer>().sharedMaterial=redMaterial;
            green.GetComponent<Collider>().enabled=false;red.GetComponent<Collider>().enabled=false;
            var outsideName=new GameObject("Outside name marker");var name=outsideName.AddComponent<TextMesh>();name.text="OUTSIDE PLAYER";name.color=Color.red;name.fontSize=64;name.characterSize=.008f;name.anchor=TextAnchor.MiddleCenter;
            SaloonWorldText.Attach(name);
            var center=pane.transform.position;green.transform.position=center+Vector3.back*1.4f;red.transform.position=center+Vector3.forward*1.2f;
            outsideName.transform.position=red.transform.position+Vector3.up*.3f;
            var position=ViewCamera.transform.position;var rotation=ViewCamera.transform.rotation;float fov=ViewCamera.fieldOfView;bool fog=RenderSettings.fog;
            try
            {
                RenderSettings.fog=false;ViewCamera.fieldOfView=45;
                ViewCamera.transform.SetPositionAndRotation(center+Vector3.forward*.85f,Quaternion.Euler(0,180,0));
                int visible=WindowMarkerPixels(true);
                pane.surface.enabled=false;int outsideBaseline=WindowMarkerPixels(true);
                pane.surface.enabled=true;
                check(outsideBaseline>50&&visible==0,"outside render cannot see into the room ("+visible+" / "+outsideBaseline+" marker pixels)");
                var barEye=SaloonStations.Position(false,0,pane.room==1?0:4)+Vector3.up*1.35f;
                ViewCamera.transform.SetPositionAndRotation(barEye,Quaternion.LookRotation(green.transform.position-barEye));
                int fromBar=WindowMarkerPixels(true);
                check(fromBar==0,"Bar station cannot see inside "+SaloonStations.Name(pane.room)+" ("+fromBar+" marker pixels)");
                ViewCamera.transform.SetPositionAndRotation(center+Vector3.back*.85f,Quaternion.identity);
                pane.surface.enabled=false;int baseline=WindowMarkerPixels(false);
                pane.surface.enabled=true;int blocked=WindowMarkerPixels(false);
                check(baseline>50&&blocked==0,"inside render cannot see the saloon through the wall ("+blocked+" / "+baseline+" marker pixels)");
            }
            finally
            {
                pane.surface.enabled=true;RenderSettings.fog=fog;ViewCamera.fieldOfView=fov;ViewCamera.transform.SetPositionAndRotation(position,rotation);
                green.SetActive(false);red.SetActive(false);outsideName.SetActive(false);Destroy(green);Destroy(red);Destroy(outsideName);Destroy(greenMaterial);Destroy(redMaterial);
            }
        }
        int WindowMarkerPixels(bool green)
        {
            var previous=RenderTexture.active;var oldTarget=ViewCamera.targetTexture;
            var target=new RenderTexture(256,256,24);var texture=new Texture2D(256,256,TextureFormat.RGB24,false);
            try
            {
                ViewCamera.targetTexture=target;ViewCamera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,256,256),0,0);texture.Apply();
                return texture.GetPixels32().Count(c=>green?c.g>210&&c.r<70&&c.b<70:c.r>210&&c.g<70&&c.b<70);
            }
            finally{ViewCamera.targetTexture=oldTarget;RenderTexture.active=previous;Destroy(texture);target.Release();Destroy(target);}
        }
        void WindowPreview(Vector3 position,Vector3 lookAt,float fov,string name)
        {
            ViewCamera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(lookAt-position));ViewCamera.fieldOfView=fov;
            var previous=RenderTexture.active;var oldTarget=ViewCamera.targetTexture;
            var target=new RenderTexture(1280,900,24);var texture=new Texture2D(1280,900,TextureFormat.RGB24,false);
            try
            {
                ViewCamera.targetTexture=target;ViewCamera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1280,900),0,0);texture.Apply();
                string path=Path.Combine(Path.GetTempPath(),"SaloonWindows-"+name+".png");File.WriteAllBytes(path,texture.EncodeToPNG());Debug.Log("SALOON_WINDOW_IMAGE "+path);
            }
            finally{ViewCamera.targetTexture=oldTarget;RenderTexture.active=previous;Destroy(texture);target.Release();Destroy(target);}
        }
    }
}
#endif
