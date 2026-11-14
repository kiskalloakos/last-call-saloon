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
                foreach(var p in players.Values){p.hp=60;p.deathAt=-1;p.hitbox.enabled=true;p.bag=false;p.escapeRound=0;p.role=Role.Outlaw;p.nextShot=0;p.ammo=6;p.lastAction=-100;p.busyUntil=0;}
                host.role=Role.Sheriff;
                rounds.Start(players.Keys,Time.time,30,30,6,1,1);phase=rounds.Phase;PositionEveryone(true);
            }
            Discussion();TickPracticeBots();
            var offer=InvitationsFor(host).First();
            var initial=bots.Select(p=>p.chosenRoom).ToArray();TickPracticeBots();
            Check(bots.Select(p=>p.chosenRoom).SequenceEqual(initial),"bot plans stay stable while waiting for consent");
            AcceptRoomInvitation(host,new Command{kind="joinInvite",name=offer.fromId,x=offer.room,invitationVersion=offer.planVersion},true);TickServer();
            Check(phase==Phase.Turns&&host.room==offer.room&&players[ulong.Parse(offer.fromId)].room==host.room,"accepting a bot invitation locks and assigns both players together");
            Check(players.Values.Where(p=>p.Alive&&(p.room==1||p.room==2)).GroupBy(p=>p.room).All(g=>g.Count()>=2),"practice assignments allow solo Bar and smoking but never solo minigames");

            Discussion();foreach(var p in players.Values){p.chosenRoom=0;rounds.SetReady(p.id,true);}
            rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
            Check(!roomTurns.ContainsKey(0)&&players.Values.All(p=>DuelAllows(p)&&PrivateSnapshot(p.id).actionAvailable),"all Bar actions are available concurrently");
            host.role=Role.Outlaw;host.bag=true;DeclareEscape(host);
            Check(host.escapeRound==rounds.Round&&rounds.HasSpent(host.id)&&bots.All(p=>!rounds.HasSpent(p.id)),"escape can be declared before any other Bar player acts");
            Check(PrivateSnapshot(host.id).roomTurn==null&&bots.All(p=>PrivateSnapshot(p.id).actionAvailable),"declaring escape creates no queue or wait for the other players");

            void Shot(Role shooter,Role target,int hp=60,int targetHp=60)
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
                Check(host.hp==40&&peer.hp==40&&host.ammo==5&&!rounds.CanAct(host.id,Time.time),pair+" friendly shot costs shooter and target 20% health, one bullet and one action");
                host.nextShot=0;Fire(host);Check(host.hp==40&&peer.hp==40,"rejected second shot cannot apply another penalty");
            }
            foreach(var pair in new[]{(Role.Sheriff,Role.Outlaw),(Role.Deputy,Role.Renegade),(Role.Outlaw,Role.Deputy),(Role.Renegade,Role.Sheriff),(Role.Renegade,Role.Outlaw)})
            {
                Shot(pair.Item1,pair.Item2);Check(host.hp==60&&peer.hp==40,pair+" enemy shot has no friendly-fire penalty");
            }
            Shot(Role.Deputy,Role.Sheriff,20,20);
            Check(!host.Alive&&!peer.Alive&&!host.hitbox.enabled&&!peer.hitbox.enabled&&!host.drawn&&host.hp==0&&peer.hp==0,"lethal friendly shot eliminates both players cleanly");
            Broadcast();yield return null;
            Debug.Log("SALOON_PRACTICE PASS");
        }
#endif
    }
}
