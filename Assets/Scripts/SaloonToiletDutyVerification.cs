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
        public IEnumerator VerifyToiletDuty()
        {
            verifying=AutomatedInput=true;VerificationFinished=VerificationPassed=false;int checks=0;
            void Check(bool ok,string message)
            {checks++;if(!ok){VerificationFinished=true;throw new InvalidOperationException("Toilet mission: "+message);}Debug.Log("SALOON_TOILET_CHECK "+message);}
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p!=host);
            var roles=new[]{Role.Sheriff,Role.Deputy,Role.Outlaw,Role.Renegade,Role.Swindler};
            void Offer(Role role=Role.Sheriff,int hp=60)
            {
                ResetHeist();outcome=pendingOutcome="";duels.Clear();roomTurns.Clear();
                foreach(var p in players.Values){p.hp=100;p.deathAt=-1;p.role=Role.Outlaw;p.chosenRoom=p.room=0;p.companion=null;p.busyAction="";p.busyUntil=0;p.move=Vector2.zero;p.lastAction=-100;p.drawn=false;p.nextShot=0;p.ammo=6;p.shots=0;p.shotAt=-100;p.confirmedAt=-100;}
                host.role=role;host.hp=hp;host.cigaretteBuff=true;
                rounds.Start(players.Keys,Time.time,60,180,0);phase=rounds.Phase;PositionEveryone(true);
                toiletRound=rounds.Round;toiletWorker=host.id;host.toiletOffer=true;
            }
            void Cmd(Participant p,string kind,float value=0,int round=0)
            {p.lastAction=-100;OnCommand(p.id,new Command{kind=kind,x=value,planRound=round});}
            void Turns()
            {foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);}
            void Stroke(Participant p,int sequence=0,int round=0)
            {OnCommand(p.id,new Command{kind="toiletPump",planRound=round==0?rounds.Round:round,toiletSequence=sequence==0?toiletSequence+1:sequence});}
            void Clear(Participant p)
            {for(int i=0;i<ToiletPumpCount&&toiletPumps<ToiletPumpCount;i++){toiletPumpAt=Time.time-ToiletPumpInterval-.01f;Stroke(p);}}
            Snapshot Wire(Participant p)=>SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(p.id),out _));
            void Place(Participant p,Vector3 position)
            {p.controller.enabled=false;p.seat=-1;p.body.transform.position=position;p.move=Vector2.zero;p.poseVersion++;p.controller.enabled=true;UpdatePlayerColliders(p);Physics.SyncTransforms();}
            yield return new WaitForSeconds(.1f);
            var bathroom=GameObject.Find(SaloonBathroom.RootName);var toilet=bathroom.transform.Find(SaloonToilet.RootName);SaloonToilet.Apply();
            Check(bathroom.transform.parent==null&&Vector3.Distance(SaloonBathroom.Center,Vector3.zero)>25&&GameObject.Find("Saloon World").transform.Find(SaloonToilet.RootName)==null,"bathroom is a separate off-Bar scene root with no attached toilet");
            Check(bathroom.GetComponentsInChildren<Renderer>().All(r=>r.gameObject.layer==SaloonBathroom.Layer)&&bathroom.GetComponentsInChildren<Transform>().Count(t=>t.name==SaloonToilet.RootName)==1,"isolated room and toilet use one private render layer with no duplicates");
            Check(SaloonMovement.InRoom(SaloonToilet.Approach,SaloonBathroom.Room)&&!SaloonMovement.InRoom(SaloonToilet.Approach,0)&&!SaloonMovement.InRoom(new Vector3(9,.08f,2.4f),0),"bathroom bounds are separate and the former Bar extension is inaccessible");
            foreach(var direction in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
                Check(Physics.Raycast(SaloonBathroom.Center+Vector3.up*1.6f,direction,3,1<<SaloonBathroom.Layer),"solo room has a solid enclosing wall toward "+direction);
            Offer();var roster=players.Values.OrderBy(p=>p.id).ToArray();
            for(int i=0;i<roster.Length;i++){roster[i].role=roles[i%5];roster[i].hp=60;}
            var random=UnityEngine.Random.state;UnityEngine.Random.InitState(947);var counts=roster.ToDictionary(p=>p.id,p=>0);int repeats=0;ulong? previous=null;
            for(int i=0;i<1000;i++){ResetToiletDuty();UpdateToiletDuty();counts[toiletWorker.Value]++;if(toiletWorker==previous)repeats++;previous=toiletWorker;}
            UnityEngine.Random.state=random;
            Check(counts.Values.All(n=>n>100&&n<300)&&repeats>0,"uniform selection includes all injured roles, humans, bots and consecutive repeats");
            Offer();foreach(var p in roster){p.hp=100;rounds.SetReady(p.id,true);}toiletRound=0;UpdateToiletDuty();
            Check(toiletWorker==null&&roster.All(p=>!p.toiletOffer&&!p.toiletDuty&&rounds.IsReady(p.id)),"an all-healthy round has no offer and does not disturb plans");
            Offer();peer.hp=0;bool valid=true;
            for(int i=0;i<200;i++){ResetToiletDuty();UpdateToiletDuty();valid&=toiletWorker==host.id;}
            Check(valid,"healthy and eliminated players are excluded when only one injured player is eligible");
            Offer(hp:99);toiletRound=0;UpdateToiletDuty();Check(toiletWorker==host.id&&host.toiletOffer,"99-percent health is eligible");
            host.hp=100;Cmd(host,"toiletDuty",1);Check(!host.toiletDuty&&!host.toiletOffer,"a now-healthy player cannot accept an old offer");
            Offer();Check(Wire(host).toiletOffer&&!Wire(peer).toiletOffer,"only the selected player receives the private offer");
            Cmd(host,"room",SaloonBathroom.Room);Check(host.chosenRoom==0&&!CanSelectPlan(peer,SaloonBathroom.Room,null),"bathroom cannot be selected, invited to or reserved as a normal room");
            Check(!TryReady(host,true),"pending offer requires an accept or reject decision");
            host.chosenRoom=peer.chosenRoom=2;host.companion=peer.id;peer.companion=host.id;
            Check(!TryReady(peer,true),"a partner cannot skip the selected player's decision");ReleasePair(host);
            Cmd(peer,"toiletDuty",1);Cmd(host,"toiletDuty",1,99);Check(host.toiletOffer&&!peer.toiletDuty,"another player or stale round cannot accept the mission");
            Cmd(host,"toiletDuty",0);Cmd(host,"toiletDuty",1);Check(!host.toiletDuty&&host.hp==60&&CanSelectPlan(host,3,null),"rejecting the mission keeps normal planning and grants no heal");
            foreach(var role in roles)
            {
                Offer(role);host.chosenRoom=peer.chosenRoom=2;host.companion=peer.id;peer.companion=host.id;Cmd(host,"toiletDuty",1);
                Check(host.toiletDuty&&host.chosenRoom==0&&!host.companion.HasValue&&!peer.companion.HasValue&&rounds.IsReady(host.id),role+" acceptance reserves the solo mission and releases the prior partner");
                Check(host.room==0&&host.hp==60&&!CanSelectPlan(host,2,null)&&!TryReady(host,false),"discussion stays shared until the mission starts, without an early heal");
                Turns();float deadline=rounds.Deadline;int ammo=host.ammo,beer=beers,hits=host.hits;
                Check(host.room==SaloonBathroom.Room&&NearToilet(host)&&!rounds.HasSpent(host.id)&&rounds.CanAct(host.id,Time.time),"accepted worker starts alone at the toilet without spending an action");
                var own=Wire(host);var other=Wire(peer);
                Check(own.players.Length==1&&own.players[0].id==host.id.ToString()&&!other.players.Any(p=>p.id==host.id.ToString()),"private mission snapshots isolate worker and Bar players in both directions");
                Check(!own.actionAvailable&&own.toiletRemaining>11&&own.toiletRemaining<=ToiletMissionSeconds&&other.toiletRemaining==0,"private countdown is twelve seconds and combat controls are disabled only inside");
                TakeDamage(host,Vector3.back,100);foreach(string command in new[]{"draw","reload","fire","rob","inspect","drink","pass"})Cmd(host,command);
                Check(host.hp==60&&host.hits==hits&&host.ammo==ammo&&beers==beer&&!rounds.HasSpent(host.id),"mission ignores damage and rejects weapons, beer and combat spending");
                Cmd(host,"interact");Check(host.busyAction=="toilet"&&host.hp==60&&!rounds.HasSpent(host.id),"equipping the plunger consumes no action and grants no automatic healing");
                CompleteToiletWork(host);Stroke(peer);Stroke(host,round:99);Check(toiletPumps==0&&host.hp==60,"waiting, direct completion, another player and stale input cannot progress or heal");
                Stroke(host);int sequence=toiletSequence;Stroke(host,sequence);Stroke(host);
                Check(toiletPumps==1&&!rounds.HasSpent(host.id)&&Wire(host).toiletSequence==toiletSequence&&Wire(peer).toiletSequence==0,"duplicate and rapid clicks are rejected while private reconnect sequence is preserved");
                foreach(var p in players.Values.Where(p=>p!=host))rounds.TrySpend(p.id,Time.time);
                rounds.Tick(Time.time,id=>players[id].Alive,id=>players[id].room!=3);
                Check(rounds.Phase==Phase.Turns,"the worker still has a same-round action after other players act");
                int pose=host.poseVersion;Clear(host);Stroke(host);CompleteToiletWork(host);
                Check(host.room==0&&host.hp==80&&!host.toiletDuty&&toiletCompleted&&rounds.CanAct(host.id,Time.time)&&Wire(host).actionAvailable,"eight strokes heal twenty once and immediately return to Bar with the action available");
                Check(host.poseVersion>pose&&SaloonMovement.InRoom(host.body.transform.position,0)&&host.cigaretteBuff&&beers==beer&&rounds.Deadline==deadline,"return moves safely to Bar, preserves cigarette buff and beer, and never extends the round");
                Cmd(host,"pass");Check(rounds.HasSpent(host.id),"returned player can use their retained action in this same round");
            }
            Offer(hp:95);Cmd(host,"toiletDuty",1);Turns();Clear(host);Check(host.hp==100,"healing caps at full health");
            TakeDamage(host);Check(host.hp==80,"normal damage applies immediately after returning to Bar");
            Offer();Cmd(host,"toiletDuty",1);Turns();float end=rounds.Deadline;toiletDeadline=Time.time-.01f;Stroke(host);TickToiletWork(host);
            Check(host.room==0&&host.hp==60&&!rounds.HasSpent(host.id)&&rounds.CanAct(host.id,Time.time)&&rounds.Deadline==end,"idle timeout returns without healing, action spending or round delay");
            Offer();Cmd(host,"toiletDuty",1);Turns();Stroke(host);toiletDeadline=Time.time-.01f;TickToiletWork(host);CompleteToiletWork(host);
            Check(host.room==0&&host.hp==60&&!rounds.HasSpent(host.id),"partial work times out without healing or consuming the combat action");
            Offer();Cmd(host,"toiletDuty",1);rounds.Start(players.Keys,Time.time,60,4,0);Turns();yield return new WaitForSeconds(1.15f);TickServer();
            Check(host.room==0&&host.hp==60&&rounds.CanAct(host.id,Time.time),"short action phases reserve time for combat after the mission times out");
            Offer();Cmd(host,"toiletDuty",1);Turns();Clear(host);
            Place(host,new Vector3(-2,.08f,-4.5f));Place(peer,new Vector3(-2,.08f,-6));host.drawn=true;host.ammo=6;host.nextShot=0;
            var aim=peer.body.transform.TransformPoint(peer.head.center)-Eye(host);host.yaw=Mathf.Atan2(aim.x,aim.z)*Mathf.Rad2Deg;host.pitch=-Mathf.Atan2(aim.y,new Vector2(aim.x,aim.z).magnitude)*Mathf.Rad2Deg;Physics.SyncTransforms();Fire(host);
            Check(rounds.HasSpent(host.id)&&host.shots>0&&host.ammo==5&&peer.hp==60,"a healed player can fire a real boosted headshot after returning to Bar");
            Offer();Cmd(host,"toiletDuty",1);Turns();phase=Phase.Finished;TickToiletWork(host);
            Check(host.room==0&&host.hp==60&&!host.toiletDuty,"match end cancels protection without awarding healing");
            Offer();Cmd(host,"toiletDuty",1);ResetHeist();Check(toiletWorker==null&&toiletDeadline==0&&!host.toiletDuty&&!host.toiletOffer,"new matches clear offers, mission timers and completion state");
            Check(SaloonPrivacy.VoiceChannel("test",Phase.Turns,4,true)!=SaloonPrivacy.VoiceChannel("test",Phase.Turns,0,true)&&SaloonPrivacy.VoiceChannel("test",Phase.Turns,4,true)!=SaloonPrivacy.VoiceChannel("test",Phase.Turns,3,true),"solo mission voice uses its own channel");
            for(int a=0;a<5;a++)for(int b=0;b<5;b++)Check(SaloonPrivacy.ShareActivity(Phase.Turns,a,b)==(a==b||a<3&&b<3),"activity isolation "+a+" / "+b);
            Offer();Cmd(host,"toiletDuty",1);Turns();host.name="Mission player";helloSent=true;roleIntroStarted=float.NegativeInfinity;MenuOpen=ShowRole=RoomPlannerOpen=false;Broadcast();yield return null;
            int mask=ViewCamera.cullingMask;
            Check((mask&(1<<SaloonBathroom.Layer))!=0&&(mask&(1<<SaloonSmokingArea.IndoorLayer))==0&&(mask&(1<<SaloonSmokingArea.OutdoorLayer))==0,"mission camera renders only the isolated room environment");
            string folder=Path.Combine(Path.GetTempPath(),"LastCall-ToiletMission");Directory.CreateDirectory(folder);
            if(!Application.isBatchMode)yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"safe-mission.png"));yield return null;
            for(int i=0;i<ToiletPumpCount;i++){Stroke(host);Broadcast();yield return new WaitForSeconds(ToiletPumpInterval+.06f);}
            Check(host.room==0&&host.hp==80&&rounds.CanAct(host.id,Time.time),"real paced pumping completes the safe mission and preserves combat availability");
            Broadcast();yield return null;Check((ViewCamera.cullingMask&(1<<SaloonBathroom.Layer))==0&&(ViewCamera.cullingMask&(1<<SaloonSmokingArea.IndoorLayer))!=0,"Bar visibility is restored on automatic return");
            if(!Application.isBatchMode)yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"returned-to-bar.png"));yield return null;
            Offer();host.toiletOffer=false;toiletWorker=peer.id;peer.hp=60;peer.toiletOffer=true;AnswerToiletDuty(peer,true);Turns();float until=Time.time+7;
            while(Time.time<until&&!toiletCompleted)yield return null;
            Check(toiletCompleted&&peer.hp==80&&peer.room==0&&!rounds.HasSpent(peer.id),"assigned bot completes the isolated mission and returns with its action too");
            yield return VerifyToiletAudio();
            VerificationFinished=VerificationPassed=true;Debug.Log("SALOON_TOILET PASS "+checks+" checks; previews="+folder);
        }
    }
}
#endif
