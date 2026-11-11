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
        public IEnumerator VerifyBarAndBeer()
        {
            verifying=true;AutomatedInput=true;VerificationPassed=VerificationFinished=false;
            void Check(bool ok,string text)
            {
                if(!ok){VerificationFinished=true;throw new InvalidOperationException("Bar/beer: "+text);}
                Debug.Log("SALOON_BAR_BEER_CHECK "+text);
            }
            yield return null;
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            MenuOpen=false;Broadcast();
            Check(View.phase==Phase.Discussion&&!RoomPlannerOpen&&!NeedsPointer,"discussion begins with ordinary mouse look");
            ToggleRoomPlanner();Check(RoomPlannerOpen&&NeedsPointer,"planner opens with a usable cursor");
            ToggleRoomPlanner();Check(!RoomPlannerOpen&&!NeedsPointer,"planner closes and returns to mouse look");
            float drink=tuning.drinkSeconds,handoff=tuning.handoffSeconds;
            void Window()
            {
                ResetHeist();pendingOutcome=outcome="";beers=tuning.sharedBeers;
                foreach(var p in players.Values)
                {
                    p.hp=80;p.deathAt=-1;p.role=p==host?Role.Sheriff:Role.Outlaw;
                    p.beer=0;p.beerHealing=0;p.beerChoiceUntil=0;p.beerPickupRound=-1;
                    p.drawn=false;p.busyAction="";p.busyUntil=0;p.lastAction=-100;p.nextShot=0;
                    p.chosenRoom=0;p.companion=null;p.ammo=6;p.cigaretteBuff=false;
                }
                rounds.Start(players.Keys,Time.time,30,120,0);foreach(var p in players.Values)rounds.SetReady(p.id,true);
                rounds.Tick(Time.time,id=>players[id].Alive);phase=rounds.Phase;PositionEveryone(false);
                roomTurns[0].order=new[]{host.id}.Concat(players.Keys.Where(id=>id!=host.id)).ToArray();
                roomTurns[0].index=0;roomTurns[0].until=Time.time+30;
            }
            void Cmd(string kind){OnCommand(host.id,new Command{kind=kind});}
            void Pour(float seconds)
            {
                host.lastAction=-100;Cmd("interact");host.pourStarted=Time.time-seconds;Cmd("stopPour");
            }
            void Aim(Participant from,Participant to)
            {
                var d=Eye(to)-Eye(from);from.yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
                from.pitch=-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;Physics.SyncTransforms();
            }
            foreach(var sample in new[]{(0f,0),(1f,5),(5f,5),(5.01f,10),(10f,10),(11f,15),(12f,15),(15f,15),(17f,20),(20f,20),(99f,20)})
                Check(SaloonBeerPour.RoundHealing(sample.Item1)==sample.Item2,"raw "+sample.Item1+" rounds up to "+sample.Item2+"%");
            Check(SaloonBeerPour.Healing(3)==20&&SaloonBeerPour.Healing(4.5f)==10&&SaloonBeerPour.Healing(float.NaN)==0,"perfect pour heals 20%, overflow wastes beer, invalid values cannot score");
            Window();host.hp=100;int stock=beers;Cmd("interact");
            Check(host.beer==0&&host.busyAction==""&&!rounds.HasSpent(host.id)&&beers==stock,"full-health spectator cannot pour or consume stock/action");
            Stand(host);Check(CanWalk(host),"full-health bar spectator can stand and walk");
            Window();OnCommand(peer.id,new Command{kind="interact"});
            Check(peer.busyAction==""&&beers==tuning.sharedBeers&&!rounds.HasSpent(peer.id),"waiting player cannot start a pour before their turn");
            host.room=1;Cmd("interact");Check(host.busyAction==""&&beers==tuning.sharedBeers,"side rooms cannot pour beer");
            Window();beers=0;Cmd("interact");Check(!rounds.HasSpent(host.id),"empty supply preserves action");
            Window();Aim(host,peer);host.drawn=true;Cmd("fire");
            Check(peer.hp==60&&host.ammo==5&&rounds.HasSpent(host.id)&&beers==tuning.sharedBeers,"skipping beer preserves the option to spend the turn shooting");
            Window();Cmd("interact");
            Check(host.busyAction=="pour"&&host.beer==0&&host.hp==80&&beers==tuning.sharedBeers-1&&rounds.HasSpent(host.id),"pour spends exactly one stock and one action without healing");
            int seat=host.seat;Cmd("stance");Check(host.seat==seat,"pouring keeps the player at the tap");
            Cmd("fire");Cmd("drink");Cmd("give");
            Check(host.busyAction=="pour"&&host.ammo==6&&host.hp==80,"weapons and consumption cannot bypass active pour");
            TakeDamage(host);Check(host.hp==60&&host.busyAction=="pour","non-lethal hit does not interrupt pouring");
            host.pourStarted=Time.time-1.7f;Cmd("stopPour");
            Check(host.beer==1&&host.beerHealing==15&&host.hp==60,"partial pour becomes a 15% mug without automatic drinking");
            var wire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(host.id),out _));
            Check(wire.beerHealing==15&&wire.players.Single(p=>p.id==host.id.ToString()).beerHealing==15&&wire.beerChoiceRemaining>0,"exact mug healing and choice window survive compressed snapshots");
            Cmd("drink");Check(host.busyAction=="drink"&&host.drinkHealing==15&&host.beer==0,"drink is a continuation of the already spent action");
            TakeDamage(host);Check(host.hp==40&&host.busyAction=="drink","non-lethal headshot preserves self-drink");
            host.busyUntil=Time.time-.01f;TickServer();Check(host.hp==55,"finished sip heals exactly the poured 15 points after damage");
            TickServer();Check(host.hp==55,"sip never heals twice");
            host.lastAction=-100;Cmd("fire");Cmd("interact");Check(host.ammo==6&&beers==tuning.sharedBeers-1,"pour cannot be followed by another shot or pour");
            foreach(int healing in new[]{5,10,15,20})
            {
                Window();Pour((healing-1)/20f*SaloonBeerPour.FullSeconds);Aim(host,peer);Cmd("give");
                Check(peer.busyAction=="giftDrink"&&peer.drinkHealing==healing&&host.beer==0&&!rounds.HasSpent(peer.id),"gift keeps "+healing+"% healing and recipient's action");
                TakeDamage(peer);Check(peer.hp==60&&peer.busyAction=="giftDrink","gift survives damage including its handoff");
                peer.busyUntil=Time.time-.01f;TickServer();Check(peer.hp==60+healing,"recipient heals exactly "+healing+" points");
            }
            Window();host.hp=95;Pour(3);Cmd("drink");host.busyUntil=Time.time-.01f;TickServer();Check(host.hp==100,"perfect pour respects maximum health");
            Window();host.hp=20;Pour(3);Cmd("drink");TakeDamage(host);TickServer();Check(host.hp==0&&host.busyAction=="","lethal hit eliminates without revival");
            Window();host.hp=20;Cmd("interact");TakeDamage(host);TickServer();Check(host.hp==0&&host.beer==0&&host.busyAction=="","death during pour cannot create a beer");
            Window();Pour(.15f);Cmd("leaveBeer");
            Check(host.hp==80&&host.beer==0&&rounds.HasSpent(host.id),"leaving poured beer spends action without healing");
            Window();Pour(3);host.beerChoiceUntil=Time.time-.01f;TickServer();
            Check(host.hp==80&&host.beer==0,"unclaimed beer is left behind without forced drinking");
            Window();Cmd("interact");host.pourStarted=Time.time-10;host.busyUntil=Time.time-.01f;TickServer();
            Check(host.beer==1&&host.beerHealing==10&&host.busyAction=="","unattended tap closes at overflow limit");
            Window();Pour(3);peer.busyUntil=Time.time+10;Aim(host,peer);Cmd("give");
            Check(host.beer==1&&host.beerHealing==20&&host.beerChoiceUntil>0,"invalid gift retains the mug and healing");
            Window();foreach(var p in players.Values.Where(p=>p!=host))rounds.TrySpend(p.id,Time.time);
            Pour(3);TickServer();Check(phase==Phase.Turns,"last action keeps the round open while choosing drink or offer");
            Cmd("drink");TickServer();Check(phase==Phase.Turns&&host.hp==80,"last sip keeps the round open before healing");
            host.busyUntil=Time.time-.01f;rounds.SetCompletion(host.id,Time.time);TickServer();
            Check(host.hp==100&&phase==Phase.Aftermath,"last sip applies its healing before aftermath");
            Window();Pour(3);PositionEveryone(true);
            Check(players.Values.All(p=>p.beer==0&&p.beerChoiceUntil==0),"regroup clears beer, quality and continuation state");
            // Exercise the actual world controls with center-of-view rays; no UI cursor is involved.
            Ray CounterRay(int station,string target)
            {
                Physics.SyncTransforms();var at=BeerTaps.transform.Find("Pouring station "+(station+1)).GetComponentsInChildren<Transform>(true).First(t=>t.name==target).position;
                return new Ray(Eye(host),(at-Eye(host)).normalized);
            }
            Window();MenuOpen=false;Broadcast();
            Check(!NeedsPointer,"Bar keeps normal mouse look without holding right mouse");
            bool automated=AutomatedInput;AutomatedInput=false;SyncPointer();AutomatedInput=automated;
            Check(Cursor.lockState==CursorLockMode.Locked,"Bar locks the mouse for ordinary first-person look");
            var pourRay=CounterRay(host.beerStation,"Tap handle");
            Check(BeerTaps.ActionForRay(pourRay)=="interact","aim ray selects the physical pour control on the countertop");
            Check(BeerTaps.ActionForRay(CounterRay(peer.beerStation,"Tap handle"))==null,"cannot operate another player's tap");
            host.drawn=true;Broadcast();Check(BeerTaps.ActionForRay(pourRay)==null,"drawn weapon takes priority over tap clicks");
            host.drawn=false;host.hp=100;Broadcast();Check(BeerTaps.ActionForRay(pourRay)==null,"full-health spectators cannot click into a pour");
            host.hp=80;Broadcast();float oldYaw=host.yaw,oldPitch=host.pitch;int oldPose=host.poseVersion;
            Cmd(BeerTaps.ActionForRay(pourRay));Broadcast();
            Check(host.busyAction=="pour"&&host.yaw==oldYaw&&host.pitch==oldPitch&&host.poseVersion==oldPose,"world pour starts without turning or grabbing the camera");
            Check(BeerTaps.ActionForRay(pourRay)=="stopPour","same physical control stops a running pour");
            OnCommand(host.id,new Command{kind="pourFlow",x=1.5f});float beforeFlow=Time.time-host.pourStarted;
            host.pourTick=Time.time-.2f;TickBeer(host);
            Check(Time.time-host.pourStarted>beforeFlow+.09f,"opening the tap increases the actual beer flow");
            OnCommand(host.id,new Command{kind="pourFlow",x=-99});Check(host.pourFlow==.35f,"tap flow is clamped by the host");
            OnCommand(host.id,new Command{kind="pourFlow",x=1});

            OnCommand(host.id,new Command{kind="input",yaw=37,pitch=12,poseVersion=host.poseVersion});
            Check(host.yaw==37&&host.pitch==12,"looking around continues normally during the pour");
            host.pourStarted=Time.time-2.4f;Broadcast();
            var stationRoot=BeerTaps.transform.Find("Pouring station "+(host.beerStation+1));
            var counterText=stationRoot.Find("Pour result on counter");
            Check(Vector3.Dot(-counterText.forward,Eye(host)-counterText.position)>0,"counter lettering faces the player instead of appearing mirrored");
            Check(stationRoot.Find("Beer level").localScale.y>.09f&&stationRoot.Find("Pouring stream").gameObject.activeSelf&&stationRoot.Find("Full line")!=null,"beer level, live stream and target line are physical objects on the bar");
            BeerTaps.Apply(PrivateSnapshot(peer.id));
            Check(stationRoot.Find("Pouring stream").gameObject.activeSelf&&BeerTaps.ActionForRay(pourRay)==null,"another player watches the live pour without gaining its controls");
            Broadcast();Cmd("stopPour");Broadcast();
            Check(BeerTaps.ActionForRay(CounterRay(host.beerStation,"Mug interaction"))=="drink","the finished mug can be clicked to drink");
            Check(BeerTaps.ActionForRay(CounterRay(host.beerStation,"LEAVE / SPACE"))=="leaveBeer","leaving the mug uses the counter control");
            Window();host.yaw=0;host.pitch=11;host.poseVersion++;Cmd("interact");host.pourStarted=Time.time-2.4f;
            foreach(var p in players.Values)p.shots=0;
            Broadcast();MenuOpen=false;nextBroadcast=Time.unscaledTime+5;
            yield return new WaitForSeconds(.25f);yield return new WaitForEndOfFrame();
            string path=Path.Combine(Path.GetTempPath(),"SaloonBeerPour.png");ScreenCapture.CaptureScreenshot(path);
            yield return null;
            Check(BeerTaps!=null&&View.pouring&&!NeedsPointer,"countertop pouring stays visible with ordinary mouse look");
            externalCamera=true;externalPosition=new Vector3(-1.5f,2.7f,-1.1f);
            externalRotation=Quaternion.LookRotation(new Vector3(-1.9f,1.9f,3.7f)-externalPosition);
            yield return null;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetTempPath(),"SaloonBeerStations.png"));yield return null;
            externalCamera=false;RefreshViewpoint();
            tuning.drinkSeconds=drink;tuning.handoffSeconds=handoff;
            VerificationPassed=VerificationFinished=true;Debug.Log("SALOON_BAR_BEER PASS preview="+path);
        }
    }
}
#endif
