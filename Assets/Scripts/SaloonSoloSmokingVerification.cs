#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifySoloSmokingAccess()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            var host=players[Connection.LocalId];bool sandbox=Sandbox;Sandbox=false;int checks=0;
            void Check(bool ok,string message)
            {checks++;if(!ok){VerificationFinished=true;Debug.LogError("SALOON_SOLO_SMOKING FAIL "+message);throw new InvalidOperationException(message);}Debug.Log("SALOON_SOLO_SMOKING_CHECK "+message);}
            try
            {
                foreach(bool early in new[]{true,false})
                {
                    foreach(var p in players.Values){p.chosenRoom=0;p.companion=null;p.planSelected=false;p.hp=100;p.busyUntil=0;p.busyAction="";p.move=Vector2.zero;p.smokeAt=-100;p.puffs=0;p.cigaretteBuff=false;}
                    rounds.Start(players.Keys,Time.time,early?30:1,30,0);phase=rounds.Phase;PositionEveryone(true);
                    OnCommand(host.id,new Command{kind="planReady",x=3,name="",planRound=rounds.Round,planVersion=host.planVersion+1});
                    Check(rounds.IsReady(host.id)&&host.companion==null,"solo smoking locks through the real plan command");
                    if(early)foreach(var p in players.Values.Where(p=>p!=host))rounds.SetReady(p.id,true);
                    else yield return new WaitForSeconds(1.1f);
                    TickServer();Broadcast();UpdateCamera();yield return null;
                    Check(phase==Phase.Turns&&host.room==3&&host.seat<0&&host.controller.enabled,"solo smoker reaches deck after "+(early?"early start":"timer expiry"));
                    Check(players.Values.Count(p=>p.room==3)==1&&SaloonSmokingArea.Contains(host.body.transform.position),"no companion is recruited and the player is physically outside");
                    Check(host.cigaretteBuff&&PrivateSnapshot(host.id).players.Length==1,"solo entry retains the buff and private outdoor snapshot");
                    OnCommand(host.id,new Command{kind="smoke"});
                    Check(host.puffs==1&&!rounds.HasSpent(host.id),"solo smoker can take a drag without spending an indoor action");
                }
                foreach(int room in new[]{1,2})
                {
                    foreach(var p in players.Values){p.chosenRoom=0;p.companion=null;}
                    rounds.Start(players.Keys,Time.time,30,30,0);phase=rounds.Phase;PositionEveryone(true);
                    host.chosenRoom=room;foreach(var id in players.Keys)rounds.SetReady(id,true);
                    TickServer();Check(host.room==0,"unmatched "+SaloonStations.Name(room)+" still falls back to Bar");
                }
                VerificationFinished=VerificationPassed=true;Debug.Log("SALOON_SOLO_SMOKING PASS "+checks+" checks");
            }
            finally{Sandbox=sandbox;}
        }
    }
}
#endif
