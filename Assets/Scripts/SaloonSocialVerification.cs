using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        System.Collections.IEnumerator VerifySocialChanges()
        {
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;Debug.LogError("SALOON_SOCIAL FAIL "+text);throw new InvalidOperationException(text);}Debug.Log("SALOON_SOCIAL_CHECK "+text);}
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);var outsider=players.Values.First(p=>p.bot&&p!=peer);
            void Discussion()
            {
                pendingOutcome="";
                foreach(var p in players.Values){p.hp=3;p.deathAt=-1;p.hitbox.enabled=true;p.beer=0;p.chosenRoom=0;p.companion=null;p.lastAction=-100;p.nextShot=0;}
                rounds.Start(players.Keys,Time.time,30,30,6,1,1);phase=rounds.Phase;PositionEveryone(true);
            }
            void Actions(bool pair)
            {
                Discussion();if(pair){host.chosenRoom=peer.chosenRoom=1;host.companion=peer.id;peer.companion=host.id;}
                host.role=Role.Sheriff;peer.role=Role.Outlaw;
                rounds.Start(players.Keys,Time.time-3,1,30,6,1,1);foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);if(!pair)roomTurns.Clear();
            }
            void Aim(Participant from,Participant to)
            {var dir=Eye(to)-Eye(from);from.yaw=Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg;from.pitch=0;from.drawn=true;from.busyUntil=0;from.ammo=6;from.nextShot=0;}
            Discussion();OnCommand(peer.id,new Command{kind="room",x=1,name=host.id.ToString()});
            var invite=PrivateSnapshot(host.id).players.First(p=>p.id==peer.id.ToString());
            Check(invite.companion==""&&invite.chosenRoom==-1,"discussion choices and partners stay private to their owner");
            Broadcast();yield return new WaitForSeconds(.15f);
#if UNITY_EDITOR
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../Documentation/InvitationPreview.png"));
            yield return new WaitForEndOfFrame();
#endif
            OnCommand(peer.id,new Command{kind="room",x=2});
            Check(PrivateSnapshot(host.id).players.All(p=>p.companion!=host.id.ToString()),"withdrawn or changed invitation disappears");
            Actions(true);var duel=duels[1];
            Check(PrivateSnapshot(host.id).duel.side!=PrivateSnapshot(peer.id).duel.side&&PrivateSnapshot(outsider.id).duel==null,"opposite random sides and room-private coin state");
            Aim(host,peer);int before=host.shots;Fire(host);
            Check(host.shots==before&&rounds.CanAct(host.id,Time.time),"shooting before the coin is rejected without spending action");
            host.drawn=false;host.lastAction=-100;OnCommand(host.id,new Command{kind="draw"});Check(!host.drawn,"drawing before the coin is host-blocked");
            Broadcast();yield return new WaitForSeconds(.15f);
#if UNITY_EDITOR
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../Documentation/CoinChoicePreview.png"));
#endif
            OnCommand(host.id,new Command{kind="coin",x=0});OnCommand(peer.id,new Command{kind="coin",x=0});
            Check(duel.heads==host.id&&duel.claimed&&duel.flipping,"first side claim locks ownership against competing claims");
            Check(PrivateSnapshot(host.id).duel.firstId=="","coin winner is not leaked before reveal");
            yield return new WaitForSeconds(duel.coinFlipSeconds+.1f);Check(duel.revealed,"server coin reveals after flip animation");
            var first=players[duel.first];var second=first==host?peer:host;Aim(second,first);before=second.shots;Fire(second);
            Check(second.shots==before&&rounds.CanAct(second.id,Time.time),"loser cannot bypass opening with a fire command");
            Aim(first,second);int hp=second.hp;Fire(first);Check(second.hp==hp-1,"coin winner can draw and land the opening shot");
            int shots=first.shots;first.nextShot=0;Fire(first);Check(first.shots==shots,"opening does not grant an extra shot");
            Check(PrivateSnapshot(second.id).duel.yourTurn,"opponent gets the choice to respond after the opening");
            Aim(second,first);Fire(second);Check(first.hp==2,"response shot follows the opening");
            Broadcast();yield return new WaitForSeconds(.025f);
            Check(HitFlash>0,"real local damage triggers the hit flash");
            var muzzle=ViewCamera.transform.Find("Low-poly revolver/Muzzle flash");
            Check(muzzle!=null&&muzzle.gameObject.activeInHierarchy,"accepted local shot shows muzzle burst and light");
#if UNITY_EDITOR
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../Documentation/MuzzleImpactPreview.png"));
#endif
            Actions(true);duel=duels[1];duel.choiceUntil=Time.time-1;TickDuels();Check(duel.flipping,"no-input sides auto-flip");
            yield return new WaitForSeconds(duel.coinFlipSeconds+.1f);duel.firstUntil=Time.time-1;TickDuels();
            second=players[duel.first==host.id?peer.id:host.id];Check(!rounds.CanAct(duel.first,Time.time)&&DuelAllows(second),"silent opener automatically passes to prevent a stall");
            Actions(true);duel=duels[1];duel.choiceUntil=Time.time-1;TickDuels();yield return new WaitForSeconds(duel.coinFlipSeconds+.1f);
            first=players[duel.first];second=first==host?peer:host;first.hp=0;Check(DuelAllows(second),"dead opener cannot block the surviving player");
            VerifyBlackjackFlow();
            Actions(false);host.role=Role.Sheriff;peer.role=Role.Outlaw;ResetHeist();int eventsBefore=events.Count;
            InspectSafe(host);string report=host.safeReport;safeProgress=3;peer.bag=true;
            Check(PrivateSnapshot(host.id).safeReport==report&&report.Contains("money inside"),"inspection is a last-known report, not live theft telemetry");
            Actions(false);peer.bag=true;safeProgress=3;InspectSafe(host);
            Check(host.safeReport.Contains("money missing")&&!rounds.CanAct(host.id,Time.time),"Sheriff inspection finds missing money and spends one action");
            Check(players.Values.Where(p=>p!=host).All(p=>string.IsNullOrEmpty(PrivateSnapshot(p.id).safeReport)&&PrivateSnapshot(p.id).safeProgress==-1),"inspection report and safe progress remain Sheriff-private");
            InspectSafe(peer);Check(rounds.CanAct(peer.id,Time.time),"non-Sheriff inspection rejected without spending action");
            Check(events.Count==eventsBefore,"inspection and concealed theft create no global event");
            Debug.Log("SALOON_SOCIAL PASS");
        }
#endif
    }
}
