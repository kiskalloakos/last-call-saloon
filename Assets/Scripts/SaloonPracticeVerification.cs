using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        System.Collections.IEnumerator VerifyPracticeAndFairTurns()
        {
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;Debug.LogError("SALOON_PRACTICE FAIL "+text);throw new InvalidOperationException(text);}Debug.Log("SALOON_PRACTICE_CHECK "+text);}
            var host=players[Connection.LocalId];var bots=players.Values.Where(p=>p.bot).OrderBy(p=>p.id).ToArray();var peer=bots[0];
            void Discussion()
            {
                pendingOutcome="";outcome="";
                foreach(var p in players.Values){p.hp=3;p.deathAt=-1;p.hitbox.enabled=true;p.bag=false;p.escapeRound=0;p.role=Role.Outlaw;p.nextShot=0;p.ammo=6;p.lastAction=-100;p.busyUntil=0;}
                host.role=Role.Sheriff;
                rounds.Start(players.Keys,Time.time,0,30,6,1,1);phase=rounds.Phase;PositionEveryone(true);
            }
            Discussion();TickPracticeBots();
            Check(bots.Any(p=>p.chosenRoom==0)&&bots.Any(p=>p.chosenRoom==1)&&bots.Any(p=>p.chosenRoom==2),"bots independently plan for bar and both side rooms");
            var initial=bots.Select(p=>p.chosenRoom).ToArray();TickPracticeBots();
            Check(bots.Select(p=>p.chosenRoom).SequenceEqual(initial)&&bots.All(p=>rounds.IsReady(p.id)),"bot plans stay stable while waiting Ready");
            OnCommand(host.id,new Command{kind="room",x=2,name=peer.id.ToString()});TickPracticeBots();
            Check(peer.chosenRoom==2&&peer.companion==host.id,"bot accepts a human invitation instead of its default plan");
            OnCommand(host.id,new Command{kind="room",x=0});TickPracticeBots();
            Check(peer.chosenRoom==initial[0]&&!peer.companion.HasValue,"withdrawn invitation restores varied plan");
            peer.bag=true;OnCommand(host.id,new Command{kind="room",x=2,name=peer.id.ToString()});TickPracticeBots();
            Check(peer.chosenRoom==0&&!peer.companion.HasValue,"money carrier keeps the bar objective");peer.bag=false;
            OnCommand(host.id,new Command{kind="room",x=0});OnCommand(host.id,new Command{kind="ready",x=1});TickServer();
            Check(phase==Phase.Turns&&players.Values.Any(p=>p.room==1)&&players.Values.Any(p=>p.room==2),"varied bot plans survive actual Ready room assignment");
            rounds.BeginAftermath(Time.time-4);rounds.Tick(Time.time,id=>true);PositionEveryone(true);
            rounds.Tick(Time.time+2,id=>true);phase=rounds.Phase;TickPracticeBots();
            Check(rounds.Round==2&&!rounds.IsReady(host.id)&&!bots.Select(p=>p.chosenRoom).SequenceEqual(initial),"next discussion resets human Ready and rotates bot plans");

            Discussion();foreach(var p in players.Values){p.chosenRoom=0;rounds.SetReady(p.id,true);}
            rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
            var turn=roomTurns[0];turn.order=bots.Select(p=>p.id).Concat(new[]{host.id}).ToArray();turn.index=0;
            Check(turn.seconds>=10,"crowded bar gives each actor at least ten seconds");
            // Reproduce a late host frame after the original shared phase deadline.
            rounds.Start(players.Keys,Time.time-100,0,1,6);foreach(var id in players.Keys)rounds.SetReady(id,true);
            rounds.Tick(Time.time-100,id=>true);phase=rounds.Phase;turn.until=Time.time-1;
            AdvanceRoomTurn(0);
            Check(turn.index==1&&rounds.HasSpent(bots[0].id)&&bots.Skip(1).All(p=>!rounds.HasSpent(p.id))&&!rounds.HasSpent(host.id),"late shared deadline times out only the current actor");
            while(turn.order[turn.index]!=host.id){turn.until=Time.time-1;AdvanceRoomTurn(0);}
            Check(DuelAllows(host)&&PrivateSnapshot(host.id).actionAvailable&&turn.until-Time.time>=9.9f,"last human in bar order receives a fresh usable turn");
            host.lastAction=-100;OnCommand(host.id,new Command{kind="pass"});
            Check(rounds.HasSpent(host.id)&&turn.index==turn.order.Length,"human can spend the final queued action");

            void Shot(Role shooter,Role target,int hp=3,int targetHp=3)
            {
                Discussion();host.role=shooter;peer.role=target;host.hp=hp;peer.hp=targetHp;
                host.chosenRoom=peer.chosenRoom=1;host.companion=peer.id;peer.companion=host.id;
                foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);duels.Clear();
                var direction=Eye(peer)-Eye(host);host.yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;host.pitch=0;host.drawn=true;
                Fire(host);
            }
            foreach(var pair in new[]{(Role.Deputy,Role.Sheriff),(Role.Sheriff,Role.Deputy),(Role.Outlaw,Role.Outlaw)})
            {
                Shot(pair.Item1,pair.Item2);
                Check(host.hp==2&&peer.hp==2&&host.ammo==5&&!rounds.CanAct(host.id,Time.time),pair+" friendly shot costs shooter and target one HP, one bullet and one action");
                host.nextShot=0;Fire(host);Check(host.hp==2&&peer.hp==2,"rejected second shot cannot apply another penalty");
            }
            foreach(var pair in new[]{(Role.Sheriff,Role.Outlaw),(Role.Deputy,Role.Renegade),(Role.Outlaw,Role.Deputy),(Role.Renegade,Role.Sheriff),(Role.Renegade,Role.Outlaw)})
            {
                Shot(pair.Item1,pair.Item2);Check(host.hp==3&&peer.hp==2,pair+" enemy shot has no friendly-fire penalty");
            }
            Shot(Role.Deputy,Role.Sheriff,1,1);
            Check(!host.Alive&&!peer.Alive&&!host.hitbox.enabled&&!peer.hitbox.enabled&&!host.drawn&&host.hp==0&&peer.hp==0,"lethal friendly shot eliminates both players cleanly");
            Broadcast();yield return null;
            Debug.Log("SALOON_PRACTICE PASS");
        }
#endif
    }
}
