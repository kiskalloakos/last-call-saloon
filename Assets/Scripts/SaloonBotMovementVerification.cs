#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyBotMovement()
        {
            AutomatedInput=true;VerificationPassed=VerificationFinished=false;verifying=false;
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;Debug.LogError("SALOON_BOT_MOVEMENT FAIL "+text);throw new InvalidOperationException(text);}Debug.Log("SALOON_BOT_MOVEMENT_CHECK "+text);}
            var host=players[Connection.LocalId];var bots=players.Values.Where(p=>p.bot).OrderBy(p=>p.id).ToArray();
            void Window(bool allBar=false)
            {
                pendingOutcome=outcome="";
                foreach(var p in players.Values){p.hp=3;p.deathAt=-1;p.busyAction="";p.busyUntil=0;p.beer=0;p.nextShot=0;p.lastAction=-100;p.ammo=6;p.chosenRoom=0;p.companion=null;}
                for(int i=0;i<bots.Length;i++)bots[i].chosenRoom=allBar?0:PracticeRoomCycle[i];
                rounds.Start(players.Keys,Time.time,0,120,6);foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
                foreach(var bot in bots)bot.botThink=Time.time+100;
            }
            IEnumerator Observe(float seconds,Dictionary<ulong,Vector3> origins,Dictionary<ulong,float> distances,HashSet<ulong> walking)
            {
                float until=Time.time+seconds;
                while(Time.time<until)
                {
                    foreach(var bot in bots)
                    {
                        if(!SaloonMovement.InRoom(bot.body.transform.position,bot.room))Check(false,"bot remains inside assigned room");
                        if(bot.seat>=0)continue;
                        if(!origins.ContainsKey(bot.id))origins[bot.id]=bot.body.transform.position;
                        distances[bot.id]=Mathf.Max(distances.GetValueOrDefault(bot.id),Vector3.Distance(origins[bot.id],bot.body.transform.position));
                        var motion=avatars[bot.id.ToString()].GetComponentInChildren<SaloonCharacterMotion>();
                        if(motion.WalkingBlend>.4f)walking.Add(bot.id);
                    }
                    yield return null;
                }
            }
            var origins=new Dictionary<ulong,Vector3>();var distances=new Dictionary<ulong,float>();var walking=new HashSet<ulong>();
            Window();yield return Observe(3,origins,distances,walking);
            Check(bots.All(p=>p.seat<0&&p.controller.enabled),"all bots stand automatically in the bar and both side rooms");
            Check(bots.All(p=>distances.GetValueOrDefault(p.id)>.3f),"every bot walks after standing rather than only moving to leave its chair");
            Check(bots.All(p=>walking.Contains(p.id)),"all remote bot avatars animate walking from replicated movement");
            Check(bots.All(p=>!rounds.HasSpent(p.id)&&p.ammo==6),"roaming does not spend actions or ammunition");
            Window(true);Check(roomTurns[0].order.Length==5,"bar test uses the real five-player turn queue");
            roomTurns[0].order=new[]{host.id}.Concat(bots.Select(p=>p.id)).ToArray();roomTurns[0].index=0;
            origins.Clear();distances.Clear();walking.Clear();yield return Observe(2.5f,origins,distances,walking);
            Check(bots.All(p=>!DuelAllows(p)&&distances.GetValueOrDefault(p.id)>.3f),"all bots dodge while waiting for the human's bar turn");
            foreach(var bot in bots)rounds.TrySpend(bot.id,Time.time);
            origins=bots.ToDictionary(p=>p.id,p=>p.body.transform.position);distances.Clear();walking.Clear();yield return Observe(1.5f,origins,distances,walking);
            Check(bots.All(p=>distances.GetValueOrDefault(p.id)>.15f),"bots keep moving after spending their action");

            Window(true);roomTurns.Clear();duels.Clear();var victim=bots[0];host.role=Role.Sheriff;victim.role=Role.Outlaw;
            void Place(Participant p,Vector3 pos){p.controller.enabled=false;p.body.transform.position=pos;p.seat=-1;p.poseVersion++;p.move=Vector2.zero;p.vertical=0;UpdatePlayerColliders(p);p.controller.enabled=true;}
            Place(host,new Vector3(4,.08f,0));Place(victim,new Vector3(6,.08f,0));
            victim.botMoveWorld=Vector3.forward;victim.botRoamAt=Time.time+1;host.drawn=true;
            var aim=Eye(victim)-Eye(host);host.yaw=Mathf.Atan2(aim.x,aim.z)*Mathf.Rad2Deg;host.pitch=0;
            Check(Target(host,65)?.collider==victim.head,"the original head position is a legal shot before the dodge");
            yield return new WaitForSeconds(.25f);Fire(host);
            Check(victim.hp==3&&host.ammo==5&&rounds.HasSpent(host.id),"autonomous bot movement dodges a real shot aimed before the sidestep");
            victim.role=Role.Sheriff;host.role=Role.Outlaw;host.hp=2;victim.botThink=Time.time-1;int shots=victim.shots;
            float fireUntil=Time.time+2;
            while(victim.shots==shots&&Time.time<fireUntil)yield return null;
            Check(victim.shots==shots+1&&host.hp==1&&rounds.HasSpent(victim.id),"moving bot still draws, aims and spends one legal headshot action");
            victim.busyAction="drink";victim.busyUntil=Time.time+1;yield return null;
            Check(victim.move==Vector2.zero,"bots pause their feet while drinking");
            victim.busyUntil=0;victim.busyAction="";
            rounds.BeginAftermath(Time.time);phase=rounds.Phase;var positions=bots.ToDictionary(p=>p.id,p=>p.body.transform.position);yield return new WaitForSeconds(.2f);
            Check(bots.All(p=>p.move==Vector2.zero&&Vector3.ProjectOnPlane(p.body.transform.position-positions[p.id],Vector3.up).sqrMagnitude<.001f),"bots stop roaming when room actions end");
            rounds.Start(players.Keys,Time.time,0,120,6);phase=rounds.Phase;PositionEveryone(true);yield return new WaitForSeconds(.2f);
            Check(bots.All(p=>p.seat>=0&&p.move==Vector2.zero),"bots return to seated discussion");
            verifying=true;Debug.Log("SALOON_BOT_MOVEMENT PASS");VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
