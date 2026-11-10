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
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;Debug.LogError("SALOON_PLANNING FAIL "+text);throw new InvalidOperationException(text);}Debug.Log("SALOON_PLANNING_CHECK "+text);}
            var host=players[Connection.LocalId];var companion=players.Values.First(p=>p.bot);Broadcast();
            var stale=PrivateSnapshot(host.id);Check(stale.phase==Phase.Discussion&&Self.chosenRoom==0,"fixture starts with an unedited Bar plan");
            ChooseRoom(1,PlannedCompanion);
            Check(PlannedRoom==1&&Self.chosenRoom==0&&PlanPending,"room checkmark changes immediately before an acknowledgement");
            ChooseRoom(PlannedRoom,companion.id.ToString());
            Check(host.chosenRoom==1&&host.companion==companion.id&&PlannedCompanion==companion.id.ToString(),"rapid companion selection keeps the newly selected room on the host");
            ApplySnapshot(stale);
            Check(PlannedRoom==1&&PlannedCompanion==companion.id.ToString(),"an older snapshot cannot undo newer room or companion clicks");
            ToggleReady();Check(PlanReady&&rounds.IsReady(host.id),"Ready uses and immediately displays the latest plan");
            ApplySnapshot(stale);Check(PlanReady,"a stale unready snapshot cannot reverse the Ready button");
            ToggleReady();Check(!PlanReady&&!rounds.IsReady(host.id),"a second quick Ready click correctly unreadies");
            Broadcast();Check(!PlanPending,"matching host confirmation clears the saving notice");
            ChooseRoom(2,PlannedCompanion);var intermediate=PrivateSnapshot(host.id);
            ChooseRoom(1,PlannedCompanion);ApplySnapshot(intermediate);
            Check(PlannedRoom==1&&host.chosenRoom==1,"West/East/West keeps the final West choice despite a delayed East snapshot");
            ChooseRoom(PlannedRoom,"");Check(host.chosenRoom==1&&!host.companion.HasValue&&PlannedCompanion=="","Go alone preserves the current room");
            ToggleReady();ChooseRoom(2,PlannedCompanion);
            Check(!PlanReady&&!rounds.IsReady(host.id)&&host.chosenRoom==2,"editing the plan immediately clears Ready locally and on the host");
            Broadcast();Check(!PlanPending,"final host acknowledgement matches the displayed plan");
            var next=PrivateSnapshot(host.id);next.round++;next.players.Single(p=>p.id==LocalId).chosenRoom=0;ApplySnapshot(next);
            Check(PlannedRoom==0&&!PlanReady&&!PlanPending,"a new discussion discards the previous local draft");
            ResetPlanDraft();Broadcast();yield return null;
            VerifyRoomReservations();
            Debug.Log("SALOON_PLANNING PASS");VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
