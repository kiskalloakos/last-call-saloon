using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        System.Collections.IEnumerator VerifyReleaseChanges()
        {
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;Debug.LogError("SALOON_RELEASE FAIL "+text);throw new InvalidOperationException(text);}Debug.Log("SALOON_RELEASE_CHECK "+text);}
            var host=players[Connection.LocalId];var dead=players.Values.First(p=>p.bot);var neighbour=players.Values.First(p=>p.bot&&p!=dead);
            foreach(var p in players.Values){p.hp=3;p.hitbox.enabled=true;p.beer=0;p.chosenRoom=0;p.companion=null;}
            host.role=Role.Outlaw;dead.role=Role.Outlaw;
            PositionEveryone(true);var deadPosition=dead.body.transform.position;var neighbourPosition=neighbour.body.transform.position;
            dead.hp=0;dead.hitbox.enabled=false;PositionEveryone(true);
            Check(dead.body.transform.position==deadPosition&&neighbour.body.transform.position==neighbourPosition,"elimination preserves corpse and discussion seat identities");
            dead.hp=3;dead.hitbox.enabled=true;PositionEveryone(false);deadPosition=dead.body.transform.position;dead.hp=0;dead.hitbox.enabled=false;
            PositionEveryone(true);PositionEveryone(false);
            Check(dead.body.transform.position==deadPosition&&players.Values.Where(p=>p.Alive).All(p=>Vector3.Distance(p.body.transform.position,deadPosition)>.5f),"dead bar seat stays occupied across regroup without overlaps");
            foreach(var p in players.Values){p.hp=3;p.hitbox.enabled=true;}
            rounds.Start(players.Keys,Time.time-1,1,30,6,1,1);rounds.Tick(Time.time,id=>true);rounds.Tick(Time.time+1.1f,id=>true);phase=rounds.Phase;PositionEveryone(false);roomTurns.Clear();
            Check(players.Values.All(p=>string.IsNullOrEmpty(PrivateSnapshot(p.id).ally)&&PrivateSnapshot(p.id).players.All(v=>string.IsNullOrEmpty(v.revealedRole))),"no role receives an ally name or another hidden role");
            host.beer=0;host.hp=1;host.beerPickupRound=-1;host.lastAction=-100;int supply=beers;Interact(host);
            Check(host.beer==1&&beers==supply-1&&rounds.CanAct(host.id,Time.time),"beer pickup is free preparation");
            Interact(host);Check(host.beer==1&&beers==supply-1,"duplicate beer pickup cannot drain supply");
            tuning.drinkSeconds=.2f;host.lastAction=-100;OnCommand(host.id,new Command{kind="drink"});
            Check(host.busyAction=="drink"&&host.beer==0&&!rounds.CanAct(host.id,Time.time),"same-round self-drinking spends the action");
            yield return new WaitForSeconds(.3f);Check(host.hp==2,"self-drinking heals after taking beer in the same round");tuning.drinkSeconds=2.5f;
            rounds.Start(players.Keys,Time.time-1,1,30,6,1,1);rounds.Tick(Time.time,id=>true);rounds.Tick(Time.time+1.1f,id=>true);phase=rounds.Phase;PositionEveryone(false);roomTurns.Clear();
            host.drawn=true;host.yaw=90;host.pitch=0;host.nextShot=0;host.ammo=6;Fire(host);Broadcast();
            yield return new WaitForSeconds(.4f);
            var weapon=ViewCamera.transform.Find("Low-poly revolver");
            Check(weapon!=null&&weapon.gameObject.activeInHierarchy&&Quaternion.Angle(weapon.localRotation,Quaternion.identity)>30,"accepted shot triggers the first-person revolver spin");
            Check(avatars[host.id.ToString()].GetComponents<AudioSource>().Any(a=>a.isPlaying),"accepted shot plays the new gunshot");
            var clip=SaloonGunAudio.Shot;var data=new float[clip.samples];clip.GetData(data,0);
            Check(data.All(x=>!float.IsNaN(x)&&Mathf.Abs(x)<=1)&&data.Max(x=>Mathf.Abs(x))>.9f,"gunshot waveform is finite with unclipped headroom");
#if UNITY_EDITOR
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../Documentation/RevolverSpinPreview.png"));
#endif
            Check(SaloonRules.Hint(PrivateSnapshot(host.id),PrivateSnapshot(host.id).players.First(p=>p.id==host.id.ToString())).Length>0,"private contextual beta hint is available");
            Debug.Log("SALOON_RELEASE PASS");
        }
#endif
    }
}
