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
        public IEnumerator VerifyRoomActions()
        {
            verifying=true;AutomatedInput=true;VerificationPassed=VerificationFinished=false;
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;Debug.LogError("SALOON_ROOM_ACTIONS FAIL "+text);throw new InvalidOperationException(text);}Debug.Log("SALOON_ROOM_ACTIONS_CHECK "+text);}
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);float drinkSeconds=tuning.drinkSeconds;
            void Do(Participant p,string kind){p.lastAction=-100;OnCommand(p.id,new Command{kind=kind});}
            void Aim(Participant p,Vector3 at){var d=at-Eye(p);p.yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;p.pitch=-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;}
            void Window(int room=0)
            {
                pendingOutcome=outcome="";
                foreach(var p in players.Values){p.hp=60;p.deathAt=-1;p.hits=0;p.busyAction="";p.busyUntil=0;p.beer=0;p.nextShot=0;p.lastAction=-100;p.drawn=true;p.ammo=6;p.chosenRoom=0;p.companion=null;p.botAimAt=-1;}
                host.role=Role.Sheriff;peer.role=Role.Outlaw;host.chosenRoom=peer.chosenRoom=room;
                rounds.Start(players.Keys,Time.time,0,120,6);foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);duels.Clear();roomTurns.Clear();
            }
            void Place(Participant p,Vector3 position){p.controller.enabled=false;p.seat=-1;p.body.transform.position=position;p.poseVersion++;p.move=Vector2.zero;p.vertical=0;UpdatePlayerColliders(p);p.controller.enabled=true;}
            void Range(){Window();Place(host,new Vector3(-1,.08f,1.2f));Place(peer,new Vector3(1,.08f,1.2f));host.drawn=peer.drawn=true;}
            IEnumerator Walk(Participant p,float x,float z,float seconds)
            {
                float until=Time.time+seconds;
                while(Time.time<until){OnCommand(p.id,new Command{kind="input",x=x,z=z,yaw=0,pitch=0,poseVersion=p.poseVersion});yield return null;}
                OnCommand(p.id,new Command{kind="input",poseVersion=p.poseVersion});
            }
            Window();Do(host,"stance");Check(host.seat<0&&host.controller.enabled&&!rounds.HasSpent(host.id),"F stands up at the bar without spending an action");
            Vector3 initial=host.body.transform.position;yield return Walk(host,0,-1,.25f);Check(Vector3.Distance(initial,host.body.transform.position)>.25f,"WASD moves a standing player during room actions");
            Do(host,"pass");initial=host.body.transform.position;yield return Walk(host,1,0,.25f);Check(rounds.HasSpent(host.id)&&Vector3.Distance(initial,host.body.transform.position)>.25f,"movement remains available after spending the turn");
            Window(1);Do(host,"stance");Check(host.seat<0,"F stands up in a side room");
            initial=host.body.transform.position;yield return Walk(host,0,1,.25f);Check(Vector3.Distance(initial,host.body.transform.position)>.2f,"side-room walking uses real collision movement");
            Place(host,new Vector3(-6.6f,.08f,-4.8f));yield return Walk(host,-1,0,.4f);Check(SaloonMovement.InRoom(host.body.transform.position,1)&&host.room==1,"movement cannot leave the assigned side room");
            Window();Do(host,"stance");yield return new WaitForSeconds(.2f);Do(host,"stance");Check(host.seat>=0&&!host.controller.enabled&&!rounds.HasSpent(host.id),"F returns to a nearby empty stool for free");
            phase=Phase.Discussion;initial=host.body.transform.position;Do(host,"stance");OnCommand(host.id,new Command{kind="input",x=1,poseVersion=host.poseVersion});TickServer();Check(host.body.transform.position==initial&&host.seat>=0,"discussion remains seated");

            Range();Aim(host,peer.body.transform.position+peer.hitbox.center);var bodyHit=Target(host,65);Check(bodyHit?.collider==peer.hitbox,"body aiming ray reaches the body collider");Do(host,"fire");Check(peer.hp==60&&peer.hits==0&&host.ammo==5&&rounds.HasSpent(host.id),"body shots spend ammo and action without damage or hit effects");
            Range();peer.role=Role.Deputy;Aim(host,peer.body.transform.position+peer.hitbox.center);Do(host,"fire");Check(peer.hp==60&&host.hp==60,"an allied body shot has no friendly-fire penalty");
            Range();Aim(host,Eye(peer));Check(Target(host,65)?.collider==peer.head,"standing head aiming ray reaches only the head collider");Do(host,"fire");Check(peer.hp==40&&peer.hits==1,"a standing headshot deals exactly 20% health");
            Window(1);host.drawn=true;Aim(host,Eye(peer));Do(host,"fire");Check(peer.hp==40,"seated headshots still deal 20% health");
            Range();Aim(host,Eye(peer));Place(peer,peer.body.transform.position+Vector3.forward*.6f);Do(host,"fire");Check(peer.hp==60&&host.ammo==5,"a sidestep dodges a shot aimed at the previous head position");

            Range();host.role=Role.Outlaw;host.hp=40;peer.role=Role.Sheriff;peer.botThink=Time.time-1;
            foreach(var p in players.Values.Where(p=>p.bot&&p!=peer))p.botThink=Time.time+100;
            int beforeShots=peer.shots;TickPracticeBots();Check(peer.shots==beforeShots&&peer.botAimAt>Time.time,"practice bots commit to an aim point before shooting");
            Place(host,host.body.transform.position+Vector3.forward*.6f);yield return new WaitForSeconds(.32f);TickPracticeBots();Check(peer.shots==beforeShots+1&&host.hp==40,"practice bot fires at the committed point and a sidestep avoids damage");
            Range();Broadcast();yield return new WaitForSeconds(.25f);
            var headBone=avatars[peer.id.ToString()].GetComponentInChildren<SaloonCharacterMotion>().Bone(HumanBodyBones.Head);
            Check(Vector3.Distance(headBone.position,peer.head.bounds.center)<.2f,"standing head hitbox aligns with the visible character head");

            Range();host.hp=40;host.beer=1;host.beerHealing=SaloonHealth.BeerHealing;tuning.drinkSeconds=.45f;Do(host,"drink");float ends=host.busyUntil;Aim(peer,Eye(host));Do(peer,"fire");Check(host.hp==20&&host.busyAction=="drink"&&Mathf.Abs(host.busyUntil-ends)<.001f,"a non-lethal headshot preserves the original beer timer");
            yield return new WaitForSeconds(.52f);Check(host.hp==40&&host.busyAction==""&&host.beer==0,"40%, drink, shot to 20%, then beer finishes at 40%");
            Range();host.hp=40;host.busyAction="giftDrink";host.drinkHealing=SaloonHealth.BeerHealing;host.drinkReady=Time.time;host.busyUntil=Time.time+.4f;Aim(peer,Eye(host));Do(peer,"fire");yield return new WaitForSeconds(.5f);Check(host.hp==40&&host.busyAction=="","gifted beer also finishes after a non-lethal headshot");
            Range();host.hp=20;host.beer=1;host.beerHealing=SaloonHealth.BeerHealing;Do(host,"drink");Aim(peer,Eye(host));Do(peer,"fire");yield return new WaitForSeconds(.5f);Check(host.hp==0&&host.busyAction==""&&!host.head.enabled,"a lethal headshot cancels drinking and never revives the player");
            tuning.drinkSeconds=drinkSeconds;

            Window();Broadcast();MenuOpen=false;ShowRole=false;externalCamera=true;ViewCamera.fieldOfView=70;
            externalPosition=new Vector3(5.8f,2.4f,-.4f);externalRotation=Quaternion.LookRotation(new Vector3(-1.5f,1.4f,4.8f)-externalPosition);
            string directory=Path.Combine(Path.GetTempPath(),"SaloonPrototype-EditorPreview");Directory.CreateDirectory(directory);
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"room-actions-new-bar.png"));yield return null;
            Check(GameObject.Find("Saloon World").transform.Find("Western interior")!=null,"the new paneled counter, backbar and wall details load in the scene");
            externalCamera=false;RefreshViewpoint();ViewCamera.fieldOfView=78;
            Debug.Log("SALOON_ROOM_ACTIONS PASS");VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
