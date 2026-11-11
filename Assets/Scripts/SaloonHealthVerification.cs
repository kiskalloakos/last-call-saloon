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
        public IEnumerator VerifyPercentageHealth()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string text)
            {if(!ok){VerificationFinished=true;throw new InvalidOperationException("Percentage health: "+text);}Debug.Log("SALOON_HEALTH_CHECK "+text);}
            yield return null;
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            Check(tuning.health==100&&players.Values.All(p=>p.hp==100),"saved scene, lobby and new match start at 100%");
            foreach(int hp in new[]{0,1,19,20,21,40,41,60,80,99,100})
            {
                int hits=0,remaining=hp;while(remaining>0){remaining=SaloonHealth.AfterShot(remaining);hits++;}
                Check(hits==SaloonHealth.ShotsToKill(hp),hp+"% displays the exact number of lethal hits, including partial segments");
            }
            Check(SaloonHealth.AfterShot(7)==0&&SaloonHealth.AfterShot(0)==0&&SaloonHealth.AfterBeer(95)==100&&SaloonHealth.AfterBeer(0)==0,"damage clamps at zero, healing caps at 100%, beer never revives");
            void Window(bool reset)
            {
                pendingOutcome=outcome="";ResetHeist();
                foreach(var p in players.Values)
                {
                    if(reset){p.hp=100;p.deathAt=-1;p.hits=p.shots=p.headshots=0;p.ammo=tuning.cylinder;}
                    p.role=Role.Outlaw;p.drawn=false;p.chosenRoom=0;p.companion=null;p.busyAction="";p.busyUntil=0;p.lastAction=-100;p.nextShot=0;p.move=Vector2.zero;
                }
                host.role=Role.Sheriff;
                rounds.Start(players.Keys,Time.time,30,120,0);foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(Time.time,id=>players[id].Alive);phase=rounds.Phase;PositionEveryone(false);duels.Clear();roomTurns.Clear();
            }
            void Shoot()
            {
                var d=Eye(peer)-Eye(host);host.yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;host.pitch=-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;
                host.drawn=true;Physics.SyncTransforms();OnCommand(host.id,new Command{kind="fire"});
            }
            Window(true);
            for(int shot=1;shot<=5;shot++)
            {
                if(shot>1)Window(false);
                Shoot();int expected=100-shot*20;
                Check(peer.hp==expected&&peer.hits==shot&&host.shots==shot&&host.hp==100&&host.ammo==6-shot,"legal headshot "+shot+" leaves "+expected+"% and spends exactly one bullet");
                var wire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(host.id),out _));
                var victim=wire.players.Single(p=>p.id==peer.id.ToString());
                Check(victim.health==expected&&victim.maxHealth==100&&wire.confirmedHealth==expected&&wire.confirmedMaxHealth==100&&wire.confirmedKill==(shot==5),"public health and private hit confirmation agree after shot "+shot+" over the compressed wire");
                Check(peer.Alive==(shot<5)&&peer.head.enabled==(shot<5),"only the fifth hit eliminates and disables hitboxes");
                host.nextShot=0;Shoot();Check(peer.hp==expected&&host.shots==shot,"repeated trigger cannot bypass the action limit");
            }
            Window(true);peer.role=Role.Deputy;Shoot();Check(host.hp==80&&peer.hp==80,"friendly fire removes exactly 20 percentage points from both players");
            Window(true);peer.hp=7;Shoot();Check(peer.hp==0&&!peer.Alive,"a hit at partial health eliminates without negative HP");
            Window(true);host.hp=20;peer.hp=20;peer.role=Role.Deputy;Shoot();
            Check(host.hp==0&&peer.hp==0&&!host.Alive&&!peer.Alive,"lethal friendly fire resolves both deaths at zero");
            Window(true);host.beer=1;host.beerHealing=SaloonHealth.BeerHealing;host.lastAction=-100;OnCommand(host.id,new Command{kind="drink"});
            Check(host.hp==100&&host.beer==1&&!rounds.HasSpent(host.id),"full-health drinking preserves beer and action");
            float duration=tuning.drinkSeconds;tuning.drinkSeconds=.1f;
            foreach(int hp in new[]{20,80,95})
            {
                Window(true);host.hp=hp;host.beer=1;host.beerHealing=SaloonHealth.BeerHealing;OnCommand(host.id,new Command{kind="drink"});
                Check(host.hp==hp&&host.busyAction=="drink","beer does not heal before the drinking timer completes");
                yield return new WaitForSeconds(.2f);
                Check(host.hp==Math.Min(100,hp+20)&&host.beer==0&&host.busyAction=="","completed drink heals 20 points, capped at 100%, from "+hp+"%");
            }
            foreach(int hp in new[]{20,80,95,100})
            {
                Window(true);peer.hp=hp;peer.busyAction="giftDrink";peer.drinkHealing=SaloonHealth.BeerHealing;peer.drinkReady=Time.time;peer.busyUntil=Time.time+.1f;
                yield return new WaitForSeconds(.2f);
                Check(peer.hp==Math.Min(100,hp+20)&&peer.busyAction=="","gifted beer heals once and respects the 100% cap from "+hp+"%");
                TickServer();Check(peer.hp==Math.Min(100,hp+20),"completed gifted beer cannot heal a second time");
            }
            tuning.drinkSeconds=duration;
            Window(true);host.hp=80;peer.hp=40;Broadcast();MenuOpen=false;
            yield return VerifyDiscussionHealth();
            host.hp=20;TakeDamage(host);Broadcast();yield return new WaitForSeconds(SaloonRagdoll.ObserverDelay+.15f);Broadcast();
            Check(IsSpectating&&CameraPlayer!=null&&CameraPlayer.health==players[ulong.Parse(CameraPlayer.id)].hp&&CameraPlayer.maxHealth==100,"spectator HUD follows the observed player's exact percentage");
            // Full HUD capture includes the bottom-left percentage and five segments.
            host.hp=80;host.deathAt=-1;host.role=Role.Renegade;peer.hp=40;Broadcast();yield return new WaitForSeconds(.1f);yield return new WaitForEndOfFrame();
            string preview=Path.Combine(Path.GetTempPath(),"SaloonHealth-HUD.png");ScreenCapture.CaptureScreenshot(preview);yield return null;
            Check(Self.health==80&&CameraPlayer.health==80,"local HUD uses the same percentage as the replicated player");
            Debug.Log("SALOON_HEALTH PASS five-hit elimination, partial health, friendly fire, healing, wire and visible labels; preview="+preview);
            VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
