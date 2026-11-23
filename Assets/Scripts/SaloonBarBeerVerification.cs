#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        void MoveToBeerTrayForVerification(Participant p)
        {
            p.controller.enabled=false;p.body.transform.position=new Vector3(SaloonBeerTaps.Position.x,.08f,2.05f);p.seat=-1;
            p.controller.enabled=true;UpdatePlayerColliders(p);Physics.SyncTransforms();
        }

        public IEnumerator VerifyBarAndBeer()
        {
            verifying=true;AutomatedInput=true;VerificationPassed=VerificationFinished=false;
            void Check(bool ok,string text)
            {if(!ok){VerificationFinished=true;throw new InvalidOperationException("Bar/beer: "+text);}Debug.Log("SALOON_BAR_BEER_CHECK "+text);}
            // The editor can start this coroutine at Time.time == 0. Expired
            // busy timers below must remain positive to exercise completion.
            yield return new WaitForSeconds(.1f);
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            float duration=tuning.drinkSeconds;
            void Place(Participant p,Vector3 position)
            {p.controller.enabled=false;p.body.transform.position=position;p.seat=-1;p.controller.enabled=true;UpdatePlayerColliders(p);Physics.SyncTransforms();}
            void Window()
            {
                ResetHeist();pendingOutcome=outcome="";beers=tuning.sharedBeers;
                foreach(var p in players.Values)
                {
                    p.hp=60;p.deathAt=-1;p.role=p==host?Role.Sheriff:Role.Outlaw;p.chosenRoom=0;p.companion=null;
                    p.beer=0;p.beerHealing=0;p.beerChoiceUntil=0;p.drawn=false;p.busyUntil=0;p.busyAction="";p.lastAction=-100;p.cigaretteBuff=false;
                }
                rounds.Start(players.Keys,Time.time,30,120,0);foreach(var p in players.Values)rounds.SetReady(p.id,true);
                rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
                Place(host,new Vector3(SaloonBeerTaps.Position.x,.08f,2.05f));Place(peer,new Vector3(SaloonBeerTaps.Position.x+1,.08f,2.05f));
                MenuOpen=false;Broadcast();
            }
            void Cmd(Participant p,string kind){p.lastAction=-100;OnCommand(p.id,new Command{kind=kind});}
            try
            {
                Window();
                foreach(var p in players.Values.Where(p=>p!=host))Place(p,new Vector3(6,.08f,-.5f));
                IEnumerator WalkTo(Vector3 target)
                {
                    float until=Time.time+3;
                    while(Time.time<until&&Vector3.ProjectOnPlane(target-host.body.transform.position,Vector3.up).magnitude>.06f)
                    {
                        var direction=Vector3.ProjectOnPlane(target-host.body.transform.position,Vector3.up).normalized;
                        OnCommand(host.id,new Command{kind="input",x=direction.x,z=direction.z,yaw=0,pitch=0,poseVersion=host.poseVersion});
                        yield return null;
                    }
                    OnCommand(host.id,new Command{kind="input",poseVersion=host.poseVersion});
                    Check(Vector3.ProjectOnPlane(target-host.body.transform.position,Vector3.up).magnitude<.12f,
                        "standing collision movement reaches bar route "+target+"; actual="+host.body.transform.position);
                }
                for(int slot=0;slot<SaloonRules.BarCapacity-1;slot++)
                {
                    float x=(SaloonStations.BarX(slot)+SaloonStations.BarX(slot+1))*.5f;
                    Place(host,new Vector3(x,.08f,1.35f));
                    // The fitted drawer projects from the counter here; stop in front of its face.
                    float approachZ=Mathf.Abs(x-SaloonArt.SafePosition.x)<.8f?2.35f:2.7f;
                    yield return WalkTo(new Vector3(x,.08f,approachZ));
                    yield return WalkTo(new Vector3(x,.08f,1.35f));
                }
                Place(host,new Vector3(-2.5f,.08f,1.35f));
                yield return WalkTo(new Vector3(-2.5f,.08f,2.7f));
                yield return WalkTo(new Vector3(SaloonBeerTaps.Position.x,.08f,2.7f));
                Check(NearBeerTray(host)&&!rounds.HasSpent(host.id),"walking between stools reaches the beer tray without spending an action");
                Cmd(host,"interact");Check(host.busyAction=="drink","E drinks a beer after physically walking to the counter");
                host.busyUntil=Time.time-.01f;TickServer();Check(host.hp==80,"beer heals after walking between the bar stools");

                Window();int supply=beers;Cmd(host,"interact");
                Check(host.busyAction=="drink"&&host.hp==60&&host.drinkHealing==20&&beers==supply-1&&rounds.HasSpent(host.id),"E takes one ready beer and starts drinking as one action");
                Cmd(host,"interact");Check(beers==supply-1,"duplicate E cannot consume another beer");
                var wire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(host.id),out _));
                Check(wire.players.First(p=>p.id==host.id.ToString()).drinking&&wire.carriedBeer==0&&!wire.pouring&&wire.beerChoiceRemaining==0,"snapshots contain immediate drinking without a carry or pour step");
                TakeDamage(host);Check(host.hp==40&&host.busyAction=="drink","a nonlethal shot preserves the drinking animation");
                host.busyUntil=Time.time-.01f;TickServer();Check(host.hp==60,"finishing the drink heals exactly 20 points after damage");
                TickServer();Check(host.hp==60,"one beer cannot heal twice");

                Window();host.cigaretteBuff=true;Cmd(host,"interact");
                Check(host.drinkHealing==40&&!host.cigaretteBuff,"the drinker's cigarette buff doubles a ready beer to 40 points");
                host.busyUntil=Time.time-.01f;TickServer();Check(host.hp==100,"buffed beer caps health at 100 percent");
                Window();host.hp=90;Cmd(host,"interact");host.busyUntil=Time.time-.01f;TickServer();Check(host.hp==100,"ordinary beer also respects maximum health");

                Window();host.hp=100;host.cigaretteBuff=true;supply=beers;Cmd(host,"interact");
                Check(beers==supply&&host.cigaretteBuff&&!rounds.HasSpent(host.id),"full health preserves stock, buff and action");
                Window();Place(host,new Vector3(-6,.08f,-5));supply=beers;Cmd(host,"interact");
                Check(beers==supply&&!rounds.HasSpent(host.id),"players must approach the shared tray to drink");
                Window();host.room=1;supply=beers;Cmd(host,"interact");Check(beers==supply&&!rounds.HasSpent(host.id),"side rooms cannot drink from the Bar tray");
                Window();beers=0;Cmd(host,"interact");Check(!rounds.HasSpent(host.id),"empty tray preserves the action");
                Window();beers=1;Cmd(host,"interact");Cmd(peer,"interact");
                Check(beers==0&&host.busyAction=="drink"&&peer.busyAction==""&&!rounds.HasSpent(peer.id),"two players cannot take the same last beer");

                Window();supply=beers;
                foreach(string removed in new[]{"give","stopPour","pourFlow","leaveBeer"})Cmd(host,removed);
                Check(beers==supply&&host.busyAction==""&&peer.busyAction==""&&!rounds.HasSpent(host.id),"removed pouring and offering commands cannot create actions or gifted beer");
                Window();Broadcast();yield return null;
                var tray=BeerTaps.transform.Find("Shared beer tray");
                Check(tray!=null&&tray.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("Ready beer "))==tuning.sharedBeers,"one shared tray holds the ready mugs");
                Check(!FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(t=>t.name.StartsWith("Pouring station ")||t.name=="Tap handle"||t.name=="Brass beer tower"||t.name=="Pouring instructions"),"old tap towers, signs and pouring stations are absent");
                Check(BeerTaps.HintForRay(new Ray(Eye(host),Vector3.forward)).StartsWith("E · Drink beer"),"nearby players receive one simple E prompt");
                Cmd(host,"interact");Broadcast();yield return null;
                Check(tray.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Ready beer "))==beers,"taking a beer removes one visible mug from the tray");
                Check(tray.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("BeerMug")||t.name=="CC0 Tankard"),"ready beers use the replacement CC0 tankard");

                Window();
                foreach(var p in players.Values.Where(p=>p!=host))Place(p,SaloonWorld.Spawns[(int)(p.id%(ulong)SaloonWorld.Spawns.Length)]);
                Place(host,new Vector3(SaloonBeerTaps.Position.x,.35f,2.05f));host.seat=100;host.controller.enabled=false;host.move=Vector2.zero;
                host.yaw=host.bodyYaw=0;host.pitch=7;host.poseVersion++;UpdatePlayerColliders(host);MenuOpen=true;Broadcast();yield return null;
                yield return new WaitForSeconds(.35f);
                CaptureBeerScene("Playing");
                var position=host.body.transform.position;var cameraPosition=ViewCamera.transform.position;var cameraRotation=ViewCamera.transform.rotation;
                var detailPosition=new Vector3(SaloonBeerTaps.Position.x+.4f,2.05f,2.9f);
                ViewCamera.transform.SetPositionAndRotation(detailPosition,Quaternion.LookRotation(SaloonBeerTaps.Position+Vector3.up*.12f-detailPosition));
                CaptureBeerScene("Tankard");ViewCamera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
                foreach(var p in players.Values)rounds.TrySpend(p.id,Time.time);
                TickServer();Broadcast();yield return null;
                Check(phase==Phase.Aftermath&&BeerTaps.Visible,"the shared tray stays visible after every action is done");
                Check(host.body.transform.position==position&&Vector3.Distance(ViewCamera.transform.position,cameraPosition)<.001f&&Quaternion.Angle(ViewCamera.transform.rotation,cameraRotation)<.01f,"waiting preserves the round's player position and camera (player "+position+" -> "+host.body.transform.position+", camera "+cameraPosition+" -> "+ViewCamera.transform.position+", angle "+Quaternion.Angle(ViewCamera.transform.rotation,cameraRotation)+")");
                Check(BeerTaps.HintForRay(new Ray(Eye(host),Vector3.forward))==""&&!View.actionAvailable,"waiting does not permit another drink or action");
                CaptureBeerScene("Waiting");
                rounds.BeginAftermath(Time.time-4);TickServer();Broadcast();
                Check(phase==Phase.Regroup&&cameraCut&&cameraVelocity==Vector3.zero,"regroup requests a single camera cut with no leftover movement");
                yield return null;
                var cameraPlayer=CameraPlayer;var eye=new Vector3(cameraPlayer.x,cameraPlayer.y+SaloonMovement.EyeHeight(cameraPlayer.seated,cameraPlayer.crouched),cameraPlayer.z);
                Check(Vector3.Distance(ViewCamera.transform.position,eye)<.001f&&!BeerTaps.Visible,"the discussion-table camera arrives immediately without sliding through the bar");
                CaptureBeerScene("Regroup");
                Window();Cmd(host,"interact");TakeDamage(host,Vector3.back,100);host.busyUntil=Time.time-.01f;TickServer();
                Check(!host.Alive&&host.hp==0&&host.busyAction=="","lethal damage cancels the drink and never revives a player");
                VerificationPassed=VerificationFinished=true;Debug.Log("SALOON_BAR_BEER PASS");
            }
            finally{tuning.drinkSeconds=duration;}
        }

        void CaptureBeerScene(string name)
        {
            string directory=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Documentation/BeerScenePreviews"));
            System.IO.Directory.CreateDirectory(directory);
            var oldTarget=ViewCamera.targetTexture;var oldActive=RenderTexture.active;
            var target=new RenderTexture(1280,800,24);var image=new Texture2D(1280,800,TextureFormat.RGB24,false);
            try
            {
                ViewCamera.targetTexture=target;ViewCamera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory,name+".png"),image.EncodeToPNG());
            }
            finally{ViewCamera.targetTexture=oldTarget;RenderTexture.active=oldActive;Destroy(image);target.Release();Destroy(target);}
        }
    }
}
#endif
