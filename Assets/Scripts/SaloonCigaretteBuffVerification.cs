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
        public IEnumerator VerifyCigaretteBuff()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string text)
            {if(!ok){VerificationFinished=true;throw new InvalidOperationException("Cigarette buff: "+text);}Debug.Log("SALOON_CIGARETTE_CHECK "+text);}
            yield return null;
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            var hud=GetComponent<SaloonHUD>();
            void Reset()
            {
                pendingOutcome=outcome="";ResetHeist();
                foreach(var p in players.Values)
                {
                    p.hp=100;p.deathAt=-1;p.role=Role.Outlaw;p.cigaretteBuff=false;p.beer=0;p.shots=p.hits=p.headshots=0;p.ammo=6;
                    p.chosenRoom=0;p.companion=null;p.busyAction="";p.busyUntil=0;p.lastAction=-100;p.nextShot=0;p.move=Vector2.zero;p.smokeAt=-100;
                }
                host.role=Role.Sheriff;
                rounds.Start(players.Keys,Time.time,30,120,0);phase=rounds.Phase;PositionEveryone(true);
            }
            void Ready()
            {
                if(host.chosenRoom==3&&players.Values.Count(p=>p.chosenRoom==3)==1)
                    players.Values.First(p=>p!=host&&p!=peer).chosenRoom=3;
                foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(Time.time,id=>players[id].Alive);phase=rounds.Phase;PositionEveryone(false);Physics.SyncTransforms();
            }
            void Inside()
            {
                rounds.Start(players.Keys,Time.time,30,120,0);phase=rounds.Phase;PositionEveryone(true);
                host.chosenRoom=peer.chosenRoom=1;host.companion=peer.id;peer.companion=host.id;Ready();duels.Clear();roomTurns.Clear();
                host.drawn=true;host.nextShot=0;host.lastAction=-100;
            }
            void Aim(Vector3 at)
            {var d=at-Eye(host);host.yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;host.pitch=-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;Physics.SyncTransforms();}
            Snapshot Wire(Participant p)=>SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(p.id),out _));
            Reset();host.chosenRoom=3;Broadcast();
            Check(!host.cigaretteBuff&&!Wire(host).cigaretteBuff,"choosing the smoking area grants nothing before actual entry");
            Ready();Check(host.room==3&&host.cigaretteBuff&&host.puffs==0,"entering the smoking area grants the buff without taking a drag");
            host.drawn=true;Fire(host);Check(host.cigaretteBuff&&host.shots==0&&host.ammo==6,"rejected outdoor firing preserves the buff and ammunition");
            TrySmoke(host);host.smokeAt=Time.time-5;TrySmoke(host);
            Check(host.cigaretteBuff&&host.puffs==2,"repeated drags keep one buff without stacking");
            Inside();Check(host.cigaretteBuff,"regroup and the next room preserve an unused buff");
            Check(Wire(host).cigaretteBuff&&!Wire(peer).cigaretteBuff,"compressed snapshots reveal the buff only to its owner");
            Check(typeof(PlayerView).GetField("cigaretteBuff")==null,"public avatar records have no buff field");
            host.drawn=false;Fire(host);Check(host.cigaretteBuff&&host.shots==0,"holstered trigger preserves the buff");host.drawn=true;
            host.ammo=0;Fire(host);Check(host.cigaretteBuff,"empty cylinder preserves the buff");host.ammo=6;
            host.nextShot=Time.time+10;Fire(host);Check(host.cigaretteBuff,"cooldown rejection preserves the buff");host.nextShot=0;
            Aim(Eye(peer));Fire(host);
            Check(peer.hp==60&&peer.hits==1&&host.hp==100&&host.ammo==5&&host.shots==1&&!host.cigaretteBuff,"buffed headshot deals 40 points with one bullet, action and hit event");
            var confirmation=Wire(host);Check(confirmation.confirmedHealth==60&&!confirmation.cigaretteBuff,"hit result and buff consumption replicate together");
            Inside();Aim(Eye(peer));Fire(host);Check(peer.hp==40&&host.ammo==4,"following shot returns to normal 20-point damage");
            Reset();host.chosenRoom=3;Ready();Inside();host.pitch=-75;host.yaw=0;Fire(host);
            Check(!host.cigaretteBuff&&peer.hp==100&&host.shots==1,"a fired miss consumes the buff");
            Reset();host.chosenRoom=3;Ready();Inside();Aim(peer.body.transform.position+peer.hitbox.center);Fire(host);
            Check(!host.cigaretteBuff&&peer.hp==100&&peer.hits==0&&host.shots==1,"a body hit consumes the buff and retains the headshots-only rule");
            Reset();host.chosenRoom=3;Ready();Inside();rounds.TrySpend(host.id,Time.time);Fire(host);
            Check(host.cigaretteBuff&&host.shots==0,"spent action cannot consume a buff without firing");
            Reset();host.chosenRoom=3;Ready();Inside();peer.role=Role.Deputy;peer.cigaretteBuff=true;Aim(Eye(peer));Fire(host);
            Check(peer.hp==60&&host.hp==80&&!host.cigaretteBuff&&peer.cigaretteBuff,"friendly target takes 40, shooter penalty stays 20 and target keeps its own buff");
            Reset();host.chosenRoom=3;Ready();Inside();peer.hp=30;Aim(Eye(peer));Fire(host);
            Check(peer.hp==0&&!peer.Alive&&Wire(host).confirmedKill,"buffed lethal hit clamps health at zero and confirms elimination");
            Reset();host.chosenRoom=3;Ready();Inside();host.hp=20;TakeDamage(host);Broadcast();
            Check(!host.cigaretteBuff&&!hud.ShowsCigaretteBuff,"death clears the buff and its HUD indicator");
            peer.cigaretteBuff=true;yield return new WaitForSeconds(SaloonRagdoll.ObserverDelay+.15f);Broadcast();spectateId=peer.id.ToString();RefreshViewpoint();
            Check(IsSpectating&&!View.cigaretteBuff&&!hud.ShowsCigaretteBuff&&Wire(peer).cigaretteBuff,"spectators cannot inherit or display the followed player's private buff");
            ReopenLobby();Check(players.Values.All(p=>!p.cigaretteBuff),"returning to the lobby clears buffs");
            foreach(var p in players.Values)p.cigaretteBuff=true;StartMatch();Check(players.Values.All(p=>!p.cigaretteBuff),"new match clears every old buff");
            Reset();host.chosenRoom=3;Ready();Inside();Broadcast();MenuOpen=false;ShowRole=false;
            yield return new WaitForSeconds(.15f);
            Check(hud.ShowsCigaretteBuff&&View.cigaretteBuff,"living owner sees the ready-buff HUD indicator indoors");
            Aim(Eye(peer));yield return new WaitForEndOfFrame();
            string path=Path.Combine(Path.GetTempPath(),"SaloonCigaretteBuff-HUD.png");ScreenCapture.CaptureScreenshot(path);yield return null;
            // Buffered draw must consume at actual firing time, not at the trigger press.
            host.drawn=false;host.lastAction=-100;OnCommand(host.id,new Command{kind="draw"});OnCommand(host.id,new Command{kind="fire"});
            Check(host.fireAfterDraw&&host.cigaretteBuff&&host.shots==0,"buffered trigger retains the buff until the draw completes");
            yield return new WaitForSeconds(tuning.drawSeconds+.15f);
            Check(!host.cigaretteBuff&&host.shots==1&&peer.hp==60,"buffered shot consumes one buff and deals 40 points");
            Broadcast();Check(!hud.ShowsCigaretteBuff,"HUD indicator disappears after use");
            Debug.Log("SALOON_CIGARETTE PASS entry, persistence, one-shot damage, misses, rejection, privacy, death, reset and buffered draw; preview="+path);
            VerificationFinished=VerificationPassed=true;
        }
    }
}
#endif
