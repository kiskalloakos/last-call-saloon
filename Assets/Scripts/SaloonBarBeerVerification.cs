#if UNITY_EDITOR
using System;
using System.Collections;
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
            var others=players.Values.Where(p=>p!=host&&p!=peer).ToArray();
            ResetHeist();outcome="";
            foreach(var p in players.Values){p.hp=tuning.health;p.role=Role.Deputy;p.beer=0;p.beerPickupRound=-1;p.drawn=false;p.busyAction="";p.busyUntil=0;p.lastAction=-100;p.chosenRoom=0;p.companion=null;}
            host.role=Role.Sheriff;peer.role=Role.Outlaw;
            for(int i=0;i<others.Length;i++)others[i].chosenRoom=i<2?2:3;
            rounds.Start(players.Keys,Time.time,30,30,1,1,1);phase=rounds.Phase;
            foreach(var p in players.Values)rounds.SetReady(p.id,true);TickServer();
            Check(!duels.ContainsKey(0)&&roomTurns[0].order.Length==2,"two bar players get a turn queue without a coin toss");
            Check(PrivateSnapshot(host.id).duel==null&&PrivateSnapshot(peer.id).duel==null,"bar clients receive no private minigame");
            ClaimCoin(host,0);Check(!duels.ContainsKey(0),"coin commands at the bar do nothing");
            int supply=beers,ammo=host.ammo;Interact(host);Interact(peer);
            Check(host.beer==1&&peer.beer==1&&beers==supply-2,"full-health players can take one beer each at the bar");
            OnCommand(host.id,new Command{kind="drink"});
            Check(host.hp==tuning.health&&host.beer==1&&!rounds.HasSpent(host.id),"full-health drinking preserves beer and action within the round");
            foreach(var p in players.Values)rounds.TrySpend(p.id,Time.time);TickServer();
            Check(phase==Phase.Aftermath&&host.beer==1,"beer remains through the round's final effects");
            yield return new WaitForSeconds(3.2f);
            Check(phase==Phase.Regroup&&players.Values.All(p=>p.beer==0),"regroup leaves every unused beer behind");
            Check(host.hp==tuning.health&&host.ammo==ammo&&beers==supply-2,"cleanup preserves health, ammo and shared supply");
            var wire=Snapshot.FromWireJson(PrivateSnapshot(host.id).ToWireJson());
            Check(wire.carriedBeer==0&&wire.players.All(p=>p.carriedBeer==0),"cleared beer reaches clients in the snapshot");
            yield return new WaitForSeconds(1.1f);
            Check(phase==Phase.Discussion&&rounds.Round==2,"next round opens normally");
            host.chosenRoom=peer.chosenRoom=1;foreach(var p in others)p.chosenRoom=0;
            foreach(var p in players.Values)rounds.SetReady(p.id,true);TickServer();
            Check(duels.ContainsKey(1)&&!duels.ContainsKey(0),"coin flip stays in its dedicated room");
            var duel=duels[1];ClaimCoin(host,0);duel.revealAt=Time.time-1;TickDuels();
            var first=players[duel.first];var second=first==host?peer:host;
            void Shoot(Participant from,Participant to)
            {
                var direction=Eye(to)-Eye(from);from.yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;from.pitch=0;
                from.drawn=true;from.nextShot=0;from.busyUntil=0;Fire(from);
                from.lastAction=-100;OnCommand(from.id,new Command{kind="draw"});
            }
            Shoot(first,second);Shoot(second,first);
            Check(!host.drawn&&!peer.drawn&&host.shots>0&&peer.shots>0,"both coin players can shoot and holster after the toss");
            Broadcast();yield return new WaitForSeconds(.2f);
            var localMug=ViewCamera.transform.Find("Mug visual");
            var remoteMug=avatars[peer.id.ToString()].transform.Find("Mug visual");
            Check(localMug!=null&&!localMug.gameObject.activeInHierarchy&&remoteMug!=null&&!remoteMug.gameObject.activeInHierarchy,"holstering next round never restores either player's old mug");
            // A single player at the bar also gets a normal turn; returning there may take a fresh drink.
            rounds.BeginAftermath(Time.time-4);rounds.Tick(Time.time,id=>players[id].Alive);
            rounds.Tick(rounds.Deadline,id=>players[id].Alive);phase=rounds.Phase;PositionEveryone(true);
            foreach(var p in players.Values){p.room=p==host?0:3;rounds.SetReady(p.id,true);}
            rounds.Tick(Time.time,id=>players[id].Alive);phase=rounds.Phase;SetupDuels(false);
            Check(!duels.ContainsKey(0)&&roomTurns[0].order.SequenceEqual(new[]{host.id}),"solo bar player has an ordinary immediately available turn");
            Interact(host);Check(host.beer==1,"returning to the bar permits a fresh beer in the new round");
            VerificationPassed=VerificationFinished=true;Debug.Log("SALOON_BAR_BEER PASS");
        }
    }
}
#endif
