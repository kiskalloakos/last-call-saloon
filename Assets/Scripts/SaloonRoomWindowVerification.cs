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
        public IEnumerator VerifyRoomWindows()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string text)
            {
                if(!ok){VerificationFinished=true;throw new InvalidOperationException("Room windows: "+text);}
                Debug.Log("SALOON_WINDOW_CHECK "+text);
            }
            yield return new WaitForSeconds(.1f);
            yield return VerifyDiscussionHealth();
            var legacy=FindObjectsByType<SaloonRoomWindow>();
            legacy[0].layoutVersion=0;legacy[0].barrier.size=new Vector3(2.5f,2.1f,.12f);
            SaloonRoomWindows.Apply();
            var windows=FindObjectsByType<SaloonRoomWindow>();
            Check(windows.Length==2&&windows.All(p=>p.barrier.size.x>=3.8f),"saved room layouts are upgraded instead of keeping the old doorway");
            SaloonRoomWindows.Apply();Check(FindObjectsByType<SaloonRoomWindow>().Length==2,"reapplying the room layout creates no duplicate glass");
            Check(windows.Length==2,"both minigame rooms have viewing panes");
            Check(GameObject.Find("Saloon World").GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Room screen").All(t=>!t.gameObject.activeInHierarchy),"old solid front screens are removed");
            Check(SaloonStations.Name(0)=="Bar"&&RoomCapacities()[0]==5&&SaloonStations.Position(false,0,0)==new Vector3(-5,.35f,2.05f),"Bar name, capacity and stations stay unchanged");
            Check(GameObject.Find("Saloon World/Bar front").GetComponent<Collider>().enabled,"existing Bar counter remains in place");
            var probe=new GameObject("Window collision probe");var controller=probe.AddComponent<CharacterController>();controller.height=.6f;controller.radius=.18f;controller.skinWidth=.02f;
            foreach(var window in windows)
            {
                Check(window.barrier.enabled&&!window.barrier.isTrigger&&window.barrier.size.x>=2.5f,"wide pane is a permanent solid collider in "+SaloonStations.Name(window.room));
                var center=window.transform.position;
                foreach(int side in new[]{1,-1})
                {
                    var start=center+Vector3.forward*.9f*side;var direction=Vector3.back*side;
                    Physics.SyncTransforms();
                    Check(Physics.Raycast(start,direction,out var hit,1.8f,~0,QueryTriggerInteraction.Ignore)&&hit.collider==window.barrier,"bullet ray hits glass from side "+side);
                    var end=SaloonRoomWindows.ClipShotTrail(start,direction,20);
                    Check(Mathf.Abs(end.z-SaloonRoomWindows.FrontZ)<.07f,"shot trail stops on glass from side "+side);
                    controller.enabled=false;probe.transform.position=start;controller.enabled=true;controller.Move(direction*1.8f);
                    Check((probe.transform.position.z-center.z)*side>.19f,"character controller cannot cross glass from side "+side);
                }
                foreach(float x in new[]{-1.75f,1.4f,1.75f})
                {
                    var start=center+new Vector3(x,0,.9f);Physics.SyncTransforms();
                    Check(Physics.Raycast(start,Vector3.back,out var formerDoor,1.8f,~0,QueryTriggerInteraction.Ignore)&&formerDoor.collider==window.barrier,"full-width glass seals the former doorway and edges");
                    controller.enabled=false;probe.transform.position=start;controller.enabled=true;controller.Move(Vector3.back*1.8f);
                    Check(probe.transform.position.z>center.z+.15f,"there is no walkable doorway beside the glass");
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
            foreach(var p in players.Values){p.hp=3;p.chosenRoom=0;p.companion=null;p.drawn=false;p.busyUntil=0;}
            bots[0].chosenRoom=bots[1].chosenRoom=2;bots[2].chosenRoom=bots[3].chosenRoom=1;
            rounds.Start(players.Keys,Time.time,30,30,6);foreach(var id in players.Keys)rounds.SetReady(id,true);
            rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
            var duel=duels[2];duel.blackjack=new SaloonBlackjack(new System.Random(0));
            var watching=Snapshot.FromWireJson(PrivateSnapshot(host.id).ToWireJson());
            Check(watching.duel==null&&watching.blackjackTable?.dealerHand[1]==-1,"Bar viewer receives visible table state with a hidden hole card");
            Check(!watching.blackjackTable.canHit&&!watching.blackjackTable.canDeal&&!watching.blackjackTable.yourTurn,"watching players receive no blackjack controls or opening permissions");
            Broadcast();yield return new WaitForSeconds(1.7f);
            Check(BlackjackTable.Visible&&BlackjackTable.CardCount==4&&BlackjackTable.HiddenCount==1,"outside viewer sees the physical cards through the window");
            Check(BlackjackTable.GetComponentsInChildren<SaloonBlackjackTarget>().All(t=>!t.GetComponent<Collider>().enabled),"outside viewer cannot click another room's cards");
            externalCamera=true;RefreshViewpoint();
            WindowPreview(new Vector3(5,1.75f,1.2f),new Vector3(5,1.55f,-3.8f),72,"outside");
            WindowPreview(new Vector3(5,1.7f,-3.25f),new Vector3(5,1.7f,-1),78,"inside");
            externalCamera=false;RefreshViewpoint();

            Check(SaloonPrivacy.ShareActivity(Phase.Turns,2,0)&&SaloonPrivacy.VoiceChannel(VoiceRoom,Phase.Turns,2,true)==SaloonPrivacy.VoiceChannel(VoiceRoom,Phase.Turns,0,true),"room occupants retain indoor sounds and the same positional voice channel");
            var indoorListener=bots[0];var barSource=host;
            Check(PrivateSnapshot(indoorListener.id).players.Any(p=>p.id==barSource.id.ToString()),"indoor listeners still receive saloon sound sources");

            // Same logical room on both sides: the physical pane, rather than the room rule,
            // must be what prevents these otherwise valid headshots.
            duels.Clear();roomTurns.Clear();
            var victim=bots[0];var pane=windows.First(w=>w.room==2);
            foreach(var p in new[]{host,victim}){p.room=0;p.seat=-1;p.controller.enabled=false;p.drawn=true;p.ammo=6;p.nextShot=0;p.lastAction=-100;}
            host.role=Role.Sheriff;victim.role=Role.Outlaw;
            host.body.transform.position=pane.transform.position+new Vector3(0,-1.62f,.9f);
            victim.body.transform.position=pane.transform.position+new Vector3(0,-1.62f,-.9f);
            foreach(var p in new[]{host,victim})UpdatePlayerColliders(p);
            foreach(var shooter in new[]{host,victim})
            {
                var other=shooter==host?victim:host;var direction=Eye(other)-Eye(shooter);shooter.yaw=Quaternion.LookRotation(direction).eulerAngles.y;shooter.pitch=0;
                Check(Target(shooter,65)?.collider==pane.barrier,"host targeting reaches glass before the opponent");
                int hp=other.hp,shots=shooter.shots;OnCommand(shooter.id,new Command{kind="fire"});
                Check(shooter.shots==shots+1&&other.hp==hp&&pane.barrier.enabled,"glass absorbs a real shot and stays intact");
            }
            rounds.BeginAftermath(Time.time);phase=rounds.Phase;Broadcast();yield return null;
            Check(!BlackjackTable.Visible&&View.blackjackTable==null,"visible world table clears after the action phase");
            VerificationPassed=VerificationFinished=true;Debug.Log("SALOON_WINDOWS PASS");
        }
        IEnumerator VerifyRoomHearing()
        {
            var host=players[Connection.LocalId];var source=players.Values.Last(p=>p.bot);
            foreach(var p in players.Values){p.chosenRoom=0;p.companion=null;p.hp=3;p.busyUntil=0;}
            host.chosenRoom=2;rounds.Start(players.Keys,Time.time,30,30,6);foreach(var id in players.Keys)rounds.SetReady(id,true);
            rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);Broadcast();yield return null;
            var turn=roomTurns[0];turn.index=Array.IndexOf(turn.order,source.id);
            source.drawn=true;source.yaw=0;source.pitch=60;source.ammo=6;source.nextShot=0;source.lastAction=-100;
            int before=source.shots;OnCommand(source.id,new Command{kind="fire"});Broadcast();
            var audio=avatars[source.id.ToString()].GetComponents<AudioSource>();
            bool playing=source.shots==before+1&&audio.Any(a=>a.isPlaying&&a.maxDistance>Vector3.Distance(Eye(host),Eye(source)));
            if(!playing){VerificationFinished=true;throw new InvalidOperationException("Enclosed rooms must still receive indoor gunshot audio.");}
            Debug.Log("SALOON_WINDOW_CHECK enclosed room still plays a saloon gunshot through the glass");
            yield return null;
        }
        void CheckWindowRendering(SaloonRoomWindow pane,Action<bool,string> check)
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
                check(visible>50,"outside render sees the room through the pane ("+visible+" marker pixels)");
                ViewCamera.transform.SetPositionAndRotation(center+Vector3.back*.85f,Quaternion.identity);
                pane.outside.enabled=pane.inside.enabled=false;int baseline=WindowMarkerPixels(false);
                pane.outside.enabled=pane.inside.enabled=true;int blocked=WindowMarkerPixels(false);
                check(baseline>50&&blocked==0,"inside render cannot see the saloon through the pane ("+blocked+" / "+baseline+" marker pixels)");
            }
            finally
            {
                pane.outside.enabled=pane.inside.enabled=true;RenderSettings.fog=fog;ViewCamera.fieldOfView=fov;ViewCamera.transform.SetPositionAndRotation(position,rotation);
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
