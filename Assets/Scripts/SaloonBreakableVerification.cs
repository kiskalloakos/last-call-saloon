#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyBreakableProps()
        {
            verifying=AutomatedInput=true;VerificationFinished=VerificationPassed=false;int checks=0;
            var peers=new List<SaloonConnection>();var states=new Dictionary<SaloonConnection,Snapshot>();SaloonBreakable clientProp=null;
            void Check(bool ok,string message)
            {checks++;if(!ok){VerificationFinished=true;throw new InvalidOperationException("Breakable props: "+message);}Debug.Log("SALOON_BREAKABLE_CHECK "+message);}
            IEnumerator Wait(Func<bool> condition,string message)
            {
                float until=Time.realtimeSinceStartup+10;
                while(!condition()&&Time.realtimeSinceStartup<until)yield return null;
                Check(condition(),message);
            }
            void Place(Participant p,Vector3 position)
            {p.controller.enabled=false;p.seat=-1;p.body.transform.position=position;p.move=Vector2.zero;p.poseVersion++;UpdatePlayerColliders(p);Physics.SyncTransforms();}
            void Window()
            {
                foreach(var p in players.Values){p.hp=100;p.role=Role.Deputy;p.chosenRoom=0;p.companion=null;p.busyAction="";p.busyUntil=0;p.lastAction=-100;p.nextShot=0;p.drawn=true;p.ammo=6;}
                players[Connection.LocalId].role=Role.Sheriff;players[peers[0].LocalId].role=Role.Outlaw;
                rounds.Start(players.Keys,Time.time,30,30,0);foreach(var id in players.Keys)rounds.SetReady(id,true);TickServer();
                foreach(var p in players.Values)Place(p,new Vector3(-6+(int)p.id*.5f,.08f,-.5f));
            }
            void Aim(Participant p,Vector3 point)
            {var angle=Quaternion.LookRotation(point-Eye(p)).eulerAngles;p.yaw=angle.y;p.pitch=Mathf.DeltaAngle(0,angle.x);Physics.SyncTransforms();}
            try
            {
                for(int i=0;i<3;i++)
                {
                    var peer=new GameObject("Breakable UDP peer").AddComponent<SaloonConnection>();peers.Add(peer);peer.Initialize();peer.AutoReconnect=true;
                    peer.SnapshotReceived+=s=>states[peer]=s;peer.StartLan(false,"127.0.0.1",ushort.Parse(Port));
                }
                yield return Wait(()=>HumanCount==4&&peers.All(p=>p.Connected&&states.ContainsKey(p)),"four real UDP players connect");StartMatch();
                Check(breakableProps.Length==69&&breakableProps.Select(p=>p.id).Distinct().Count()==69,"six panes and 63 bottles have unique stable network IDs");
                Check(breakableProps.Count(p=>p.window)==6&&breakableProps.Count(p=>!p.window)==63,"all exterior panes and every backbar bottle are individual targets");
                SaloonArt.Apply();SaloonRoomWindows.Apply();
                Check(GameObject.Find("Saloon World").GetComponentsInChildren<SaloonBreakable>().Length==69,"reapplying saved-scene upgrades creates no duplicate breakables");
                foreach(var bottle in breakableProps.Where(p=>!p.window))
                    Check(bottle.GetComponentsInChildren<Renderer>().Length==4&&bottle.GetComponent<BoxCollider>().enabled,"bottle "+bottle.id+" keeps body, neck, label and stopper out of scenery batching");
                var shooter=players[peers[0].LocalId];var pane=breakableProps.First(p=>p.window);
                Window();var paneBounds=pane.GetComponent<Renderer>().bounds;var point=paneBounds.center+Vector3.up*.25f+Vector3.forward*.3f;
                float side=Mathf.Sign(point.x);Place(shooter,new Vector3(point.x-side,.08f,point.z));Aim(shooter,point);
                Check(Target(shooter,65)?.collider.GetComponent<SaloonBreakable>()==pane,"real shooting ray reaches intact glass before exterior scenery");
                var cameraPosition=ViewCamera.transform.position;var cameraRotation=ViewCamera.transform.rotation;float fov=ViewCamera.fieldOfView;
                WindowPreview(Eye(shooter),point,60,"breakable-window-before");
                shooter.drawn=false;Fire(shooter);Check(!pane.Broken&&brokenProps.Count==0,"holstered or otherwise rejected shots cannot destroy scenery");shooter.drawn=true;
                int ammo=shooter.ammo;peers[0].Send(new Command{kind="fire"});
                yield return Wait(()=>pane.Broken,"a real UDP shot shatters the window");
                Check(shooter.ammo==ammo-1&&rounds.HasSpent(shooter.id)&&shooter.shotSurface==SaloonImpactSurface.Glass,"breaking glass spends exactly one normal bullet and action");
                Check(!pane.GetComponent<Renderer>().enabled&&!pane.GetComponent<Collider>().enabled,"broken pane removes its intact surface and bullet collider");
                Check(Target(shooter,65)?.collider.GetComponentInParent<SaloonBreakable>()!=pane,"later bullets pass through the open pane");
                Check(pane.transform.Find("Window movement boundary").GetComponent<Collider>().enabled,"the room's movement boundary survives the broken pane");
                WindowPreview(Eye(shooter),point,60,"breakable-window-after");
                var fragments=GameObject.Find("Shattered window");var particles=fragments.GetComponent<ParticleSystem>();
                Check(particles.particleCount>0&&particles.GetComponent<ParticleSystemRenderer>().mesh.vertexCount==3,"window emits visible triangular glass fragments");
                Check(fragments.GetComponentInChildren<AudioSource>().clip.samples>1000,"shattering plays a positional glass crash");
                var bottleTarget=breakableProps.Where(p=>!p.window).OrderByDescending(p=>p.GetComponent<Collider>().bounds.center.y).First();
                clientProp=Instantiate(bottleTarget.gameObject,new Vector3(100,0,100),Quaternion.identity).GetComponent<SaloonBreakable>();
                Window();point=bottleTarget.GetComponent<Collider>().bounds.center;Place(shooter,new Vector3(point.x,.08f,2.05f));Aim(shooter,point);
                var hit=Target(shooter,65);
                Check(hit?.collider.GetComponentInParent<SaloonBreakable>()==bottleTarget,"bottles can be shot from the public Bar over the visible counter (hit "+hit?.collider.name+")");
                WindowPreview(Eye(shooter),point,32,"breakable-bottle-before");
                peers[0].Send(new Command{kind="fire"});yield return Wait(()=>bottleTarget.Broken,"a real UDP shot breaks an individual bottle");
                Check(bottleTarget.GetComponentsInChildren<Renderer>().All(r=>!r.enabled)&&!bottleTarget.GetComponent<Collider>().enabled,"bottle body, neck, label and stopper disappear together");
                Check(breakableProps.Count(p=>p.Broken)==2,"neighboring bottles and all other windows remain intact");
                WindowPreview(Eye(shooter),point,32,"breakable-bottle-after");
                ViewCamera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);ViewCamera.fieldOfView=fov;
                yield return Wait(()=>peers.All(p=>states[p].brokenProps?.Length==2),"all UDP clients receive both persistent broken prop IDs");
                var wire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(shooter.id),out _));
                Check(wire.brokenProps.SequenceEqual(brokenProps.OrderBy(id=>id)),"broken state survives compressed snapshot serialization");
                var savedConnection=Connection;var savedProps=breakableProps;var savedView=View;
                try
                {
                    Connection=peers[1];breakableProps=new[]{clientProp};View=null;ApplyBreakables(wire);
                    Check(clientProp.Broken&&!clientProp.GetComponent<Collider>().enabled&&clientProp.GetComponentsInChildren<Renderer>().All(r=>!r.enabled),"a fresh client reconstructs broken scenery from its first snapshot");
                    int effects=FindObjectsByType<ParticleSystem>().Length;View=wire;ApplyBreakables(wire);
                    Check(FindObjectsByType<ParticleSystem>().Length==effects,"repeated snapshots do not replay destruction effects");
                    ApplyBreakables(new Snapshot{brokenProps=Array.Empty<int>()});
                    Check(!clientProp.Broken&&clientProp.GetComponent<Collider>().enabled&&clientProp.GetComponentsInChildren<Renderer>().All(r=>r.enabled),"an empty new-match snapshot restores client visuals and colliders");
                }
                finally{Connection=savedConnection;breakableProps=savedProps;View=savedView;}
                var reconnect=peers[1];Connection.Manager.DisconnectClient(reconnect.Manager.LocalClientId,"Broken scenery reconnect check");
                yield return Wait(()=>players[reconnect.LocalId].disconnectedUntil>0,"disconnect reserves the same participant");
                yield return Wait(()=>reconnect.Connected&&players[reconnect.LocalId].disconnectedUntil==0&&states[reconnect].brokenProps?.Length==2,"reconnect retains both broken prop states");
                rounds.BeginAftermath(Time.time-4);TickServer();rounds.Tick(rounds.Deadline,id=>players[id].Alive);phase=rounds.Phase;
                Check(brokenProps.Count==2&&pane.Broken&&bottleTarget.Broken,"damage persists into the next round");
                Finish("OUTLAWS WIN — MONEY ESCAPED");StartMatch();Broadcast();
                yield return Wait(()=>peers.All(p=>states[p].brokenProps?.Length==0),"rematch clears broken state on every UDP client");
                Check(breakableProps.All(p=>!p.Broken&&p.GetComponent<Collider>().enabled&&p.GetComponentsInChildren<Renderer>().All(r=>r.enabled)),"new match repairs every bottle and pane");
                Debug.Log("SALOON_BREAKABLE PASS "+checks+" checks");VerificationFinished=VerificationPassed=true;
            }
            finally
            {
                if(clientProp!=null)Destroy(clientProp.gameObject);
                foreach(var peer in peers){peer.AutoReconnect=false;var task=peer.Disconnect();Destroy(peer.gameObject);}
            }
        }
    }
}
#endif
