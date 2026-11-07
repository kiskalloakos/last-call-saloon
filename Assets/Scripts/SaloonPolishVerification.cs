using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        System.Collections.IEnumerator VerifyPolishChanges()
        {
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;Debug.LogError("SALOON_POLISH FAIL "+text);throw new InvalidOperationException(text);}Debug.Log("SALOON_POLISH_CHECK "+text);}
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            void Window(int limit=6)
            {
                pendingOutcome="";outcome="";ResetHeist();
                foreach(var p in players.Values){p.hp=3;p.role=Role.Deputy;p.hitbox.enabled=true;p.chosenRoom=0;p.companion=null;p.lastAction=-100;p.nextShot=0;p.ammo=6;p.shots=0;}
                rounds.Start(players.Keys,Time.time-3,1,30,limit,1,1);rounds.Tick(Time.time-2,id=>true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
            }
            void Aim(Participant from,Participant to)
            {var dir=Eye(to)-Eye(from);from.yaw=Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg;from.pitch=0;from.drawn=true;from.busyUntil=0;from.ammo=6;from.nextShot=0;}
            void Pass(Participant p){p.lastAction=-100;OnCommand(p.id,new Command{kind="pass"});}
            Window();host.role=Role.Sheriff;peer.role=Role.Outlaw;
            var turn=roomTurns[0];Check(turn.order.Length==5&&turn.order.Distinct().Count()==5,"bar queue contains every player exactly once");
            // Put Sheriff last so the early-fire and one-action checks are deterministic.
            turn.order=turn.order.Where(id=>id!=host.id).Concat(new[]{host.id}).ToArray();
            Aim(host,peer);Fire(host);Check(host.shots==0&&host.ammo==6,"Sheriff cannot fire before their bar turn");
            host.drawn=false;host.lastAction=-100;OnCommand(host.id,new Command{kind="draw"});Check(!host.drawn,"Sheriff cannot draw out of turn either");
            Broadcast();yield return new WaitForSeconds(.15f);
#if UNITY_EDITOR
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../Documentation/BarOrderPreview.png"));yield return new WaitForEndOfFrame();
#endif
            turn.until=Time.time-1;var timedOut=turn.order[turn.index];TickDuels();Check(!rounds.CanAct(timedOut,Time.time)&&turn.index==1,"idle bar turn passes automatically");
            while(turn.order[turn.index]!=host.id)Pass(players[turn.order[turn.index]]);
            Aim(host,peer);Fire(host);Check(host.shots==1&&peer.hp==2,"Sheriff gets exactly one legal bar shot on their turn");
            host.nextShot=0;Fire(host);Check(host.shots==1&&host.ammo==5,"repeated Sheriff clicks cannot shoot the rest of the bar");
            Check(PrivateSnapshot(host.id).roomTurn.spent,"bar HUD distinguishes a spent action from a pending turn");
            var position=host.body.transform.position;TickServer();Check(phase==Phase.Aftermath&&host.body.transform.position==position&&host.drawn,"last action enters aftermath without moving or holstering");
            Broadcast();yield return new WaitForSeconds(.4f);
            var gun=ViewCamera.transform.Find("Low-poly revolver");
            Check(phase==Phase.Aftermath&&gun!=null&&gun.gameObject.activeInHierarchy,"last actor can see their post-shot spin during aftermath");
            Check(players.Values.All(p=>!PrivateSnapshot(p.id).actionAvailable),"aftermath locks every action");
#if UNITY_EDITOR
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../Documentation/AftermathPreview.png"));
#endif
            yield return new WaitForSeconds(2.75f);Check(phase==Phase.Regroup,"non-final round regroups only after three seconds");

            Window(1);host.role=Role.Outlaw;host.hp=1;peer.role=Role.Sheriff;
            turn=roomTurns[0];turn.order=new[]{host.id,peer.id}.Concat(players.Keys.Where(id=>id!=host.id&&id!=peer.id)).ToArray();
            Broadcast();yield return new WaitForSeconds(.1f);
            Aim(host,peer);Fire(host);Aim(peer,host);Fire(peer);
            Check(!host.Alive&&phase==Phase.Aftermath&&pendingOutcome=="LAW WINS","lethal final shot queues the result instead of displaying it immediately");
            Check(PrivateSnapshot(host.id).players.All(p=>string.IsNullOrEmpty(p.revealedRole)),"roles and results remain hidden until animations finish");
            Broadcast();yield return new WaitForSeconds(.25f);
            Check(gun.gameObject.activeInHierarchy&&!ShowEliminationOverlay,"dying shooter retains their final gun animation without an elimination overlay");
            Check(host.body.transform.position==position,"dead player retains their seat during terminal aftermath");
            yield return new WaitForSeconds(2.9f);Check(phase==Phase.Finished&&outcome=="LAW WINS","terminal result appears after three seconds");
            Check(PrivateSnapshot(host.id).players.All(p=>!string.IsNullOrEmpty(p.revealedRole)),"terminal result reveals roles after the delay");
            Debug.Log("SALOON_POLISH PASS");
        }
#endif
    }
}
