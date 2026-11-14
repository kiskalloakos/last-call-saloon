#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyPlanningDraft()
        {
            verifying=true;AutomatedInput=true;VerificationPassed=VerificationFinished=false;
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;throw new InvalidOperationException(text);}Debug.Log("SALOON_PLANNING_CHECK "+text);}
            bool sandbox=Sandbox;Sandbox=false;
            try
            {
                var host=players[Connection.LocalId];var peer=players.Values.First(p=>p!=host);
                foreach(var p in players.Values){p.hp=60;p.chosenRoom=0;p.companion=null;p.planSelected=false;}
                rounds.Start(players.Keys,Time.time,30,30,6);phase=rounds.Phase;PositionEveryone(true);ResetPlanDraft();Broadcast();
                var stale=PrivateSnapshot(host.id);
                ChooseRoom(1,"");ChooseRoom(1,peer.id.ToString());ApplySnapshot(stale);
                Check(PlannedRoom==1&&PlannedCompanion==peer.id.ToString(),"older snapshots cannot erase rapid room and companion edits");
                ToggleReady();Broadcast();
                Check(!PlanReady&&!PlanPending&&host.notice.Contains("Waiting"),"unaccepted invitation explains why it is not locked");
                var invitation=InvitationsFor(peer).Single(i=>i.fromId==host.id.ToString());
                OnCommand(peer.id,new Command{kind="joinInvite",x=1,name=host.id.ToString(),invitationVersion=invitation.planVersion,planVersion=peer.planVersion+1});Broadcast();
                Check(PlanReady&&rounds.IsReady(peer.id),"one acceptance locks both players and updates the sender");
                ToggleReady();Broadcast();Check(!PlanReady&&!rounds.IsReady(peer.id)&&!PlanPending,"unlock clears both reservations and the optimistic state");
                ChooseRoom(2,"");var intermediate=PrivateSnapshot(host.id);ChooseRoom(1,"");ApplySnapshot(intermediate);
                Check(PlannedRoom==1,"delayed intermediate snapshot cannot undo the latest room");
                ToggleReady();Broadcast();Check(PlanReady&&host.chosenRoom==1,"Ready submits the complete latest room choice");
                VerifyRoomReservations();
                // Exercise automatic reassignment after an idle discussion in the actual game.
                foreach(var p in players.Values){p.chosenRoom=0;p.companion=null;}
                rounds.Start(players.Keys,Time.time-31,30,30,6);phase=rounds.Phase;TickServer();
                Check(phase==Phase.Turns&&players.Values.Where(p=>p.Alive&&(p.room==1||p.room==2)).GroupBy(p=>p.room).All(g=>g.Count()>=2),"expired discussion prevents solo minigames while allowing solo Bar and smoking");
                rounds.Start(players.Keys,Time.time,30,30,6);phase=rounds.Phase;PositionEveryone(true);Broadcast();
                RoomPlannerOpen=true;MenuOpen=false;ShowRole=false;
                if(!Application.isBatchMode)
                {
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"SaloonPlanner-live-plans.png"));
                    yield return null;
                }
            }
            finally{Sandbox=sandbox;}
            Debug.Log("SALOON_PLANNING PASS");VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
