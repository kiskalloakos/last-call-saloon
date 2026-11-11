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
            VerifyDiscussionInvitations();
            VerifyPracticePlanningFlow();
            // Capture the same planner people use, including a received invitation.
            phase=Phase.Discussion;RoomPlannerOpen=true;MenuOpen=false;ShowRole=false;Broadcast();
            if(!Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"SaloonPlanner-invitations.png"));
                yield return null;
            }
            Debug.Log("SALOON_PLANNING PASS");VerificationPassed=VerificationFinished=true;
        }

        void VerifyPracticePlanningFlow()
        {
            void Check(bool ok,string text)
            {if(!ok){VerificationFinished=true;throw new InvalidOperationException("Practice planner: "+text);}Debug.Log("SALOON_PRACTICE_PLANNER_CHECK "+text);}
            Sandbox=true;
            var host=players[Connection.LocalId];var bots=players.Values.Where(p=>p.bot).OrderBy(p=>p.id).ToArray();
            void Discussion()
            {
                pendingOutcome=outcome="";
                foreach(var p in players.Values){p.hp=60;p.deathAt=-1;p.bag=false;p.chosenRoom=0;p.companion=null;p.move=Vector2.zero;}
                rounds.Start(players.Keys,Time.time,30,30,6);phase=rounds.Phase;PositionEveryone(true);Broadcast();
            }
            Discussion();TickPracticeBots();Broadcast();
            var offer=IncomingInvitations.FirstOrDefault();
            Check(offer!=null&&offer.room>0&&players[ulong.Parse(offer.fromId)].bot,"a real practice bot invites the human to a named destination without a human request");
            Check(bots.Count(p=>p.chosenRoom==offer.room&&rounds.IsReady(p.id))==1,"bot invitation leaves the second seat open for its recipient");
            TickPracticeBots();Broadcast();
            Check(IncomingInvitations.Any(i=>i.fromId==offer.fromId&&i.room==offer.room),"bot invitation stays stable while the human is deciding");
            JoinInvitation(offer.fromId);Broadcast();
            Check(PlanReady&&rounds.IsReady(host.id)&&host.chosenRoom==offer.room&&!PlanPending,"Join applies the invitation and readiness in one acknowledged command");
            TickServer();
            Check(phase==Phase.Turns&&host.room==offer.room&&players[ulong.Parse(offer.fromId)].room==host.room,"accepting a bot invitation enters that room with the inviter");

            Discussion();
            foreach(var bot in bots.Take(2)){SetRoomPlan(bot,1,null);TryReady(bot,true);}
            var requested=bots.Last();
            OnCommand(host.id,new Command{kind="room",x=1,name=requested.id.ToString(),planVersion=host.planVersion+1});
            Check(!PlanFull(host),"two ready bots do not show a human's selected Coin Flip room as full");
            Broadcast();LeaveForRoom(1,requested.id.ToString());Broadcast();
            Check(PlanReady&&rounds.IsReady(host.id)&&requested.chosenRoom==1&&requested.companion==host.id&&rounds.IsReady(requested.id),"Go with a practice player reassigns the bots and readies the requested pair");
            Check(bots.Count(p=>p.chosenRoom==1)==1,"unrelated bots yield the destination instead of blocking the human");
            TickServer();
            Check(phase==Phase.Turns&&host.room==1&&requested.room==1,"the previously full room actually starts Coin Flip for the requested pair");

            Discussion();var reserved=bots.Take(2).ToArray();
            try
            {
                foreach(var p in reserved){p.bot=false;SetRoomPlan(p,2,null);TryReady(p,true);}
                Broadcast();LeaveForRoom(2);Broadcast();
                Check(!PlanReady&&RoomPlannerOpen&&host.notice.Contains("full"),"a genuinely full human room explains the rejection and reopens the choices");
                Check(reserved.All(p=>rounds.IsReady(p.id)&&p.chosenRoom==2),"practice helpers never displace human reservations");
            }
            finally{foreach(var p in reserved)p.bot=true;}
            Discussion();TickPracticeBots();Broadcast();
            Check(IncomingInvitations.Length>0,"the final preview contains an actual bot-generated invitation");
            Debug.Log("SALOON_PRACTICE_PLANNER PASS");
        }

        void VerifyDiscussionInvitations()
        {
            void Check(bool ok,string text)
            {if(!ok){VerificationFinished=true;throw new InvalidOperationException("Discussion invitations: "+text);}Debug.Log("SALOON_INVITATION_CHECK "+text);}
            var host=players[Connection.LocalId];var sender=players.Values.First(p=>p.bot);var outsider=players.Values.First(p=>p!=host&&p!=sender);
            foreach(var p in players.Values){p.hp=60;p.chosenRoom=0;p.companion=null;}
            rounds.Start(players.Keys,Time.time,30,30,6);phase=rounds.Phase;PositionEveryone(true);Broadcast();
            void Invite(Participant p,int room,string target)
            {OnCommand(p.id,new Command{kind="room",x=room,name=target,planVersion=p.planVersion+1});}
            Invite(sender,3,LocalId);
            var received=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(host.id),out _));
            var request=received.invitations.Single();
            Check(request.fromId==sender.id.ToString()&&request.fromName==sender.name&&request.room==3&&request.planVersion==sender.planVersion,"recipient receives the inviter and requested room over the compressed wire");
            Check(PrivateSnapshot(outsider.id).invitations.Length==0&&PrivateSnapshot(sender.id).invitations.Length==0,"unrelated players cannot see another person's invitations");
            Check(received.players.Where(p=>p.id!=LocalId).All(p=>p.chosenRoom==-1&&p.companion==""),"public room and partner choices remain redacted");
            Broadcast();AcceptInvitation(sender.id.ToString());
            Check(PlannedRoom==3&&PlannedCompanion==sender.id.ToString()&&host.chosenRoom==3&&host.companion==sender.id,"one accept click selects both the room and companion locally and on the host");
            Check(!rounds.IsReady(host.id)&&!PlanReady,"accepting an invitation does not reserve a seat or ready the player automatically");
            Broadcast();Invite(sender,1,LocalId);AcceptInvitation(sender.id.ToString());
            Check(host.chosenRoom==3&&host.notice.Contains("changed"),"host rejects a stale invitation if the sender changed rooms before the click arrived");
            Broadcast();AcceptInvitation(sender.id.ToString());Broadcast();
            Check(host.chosenRoom==1&&PlannedRoom==1&&!PlanPending,"latest invitation can be accepted after the stale one was rejected");
            Invite(sender,1,"");Broadcast();
            Check(IncomingInvitations.Length==0,"withdrawn invitation disappears from the next snapshot");
            int version=host.planVersion;AcceptInvitation(sender.id.ToString());
            Check(host.planVersion==version,"clicking an unavailable invitation sends no plan change");
            Invite(sender,3,LocalId);Invite(outsider,2,LocalId);Broadcast();
            Check(IncomingInvitations.Length==2&&IncomingInvitations.Select(i=>i.fromId).Distinct().Count()==2,"multiple incoming invitations keep their own sender and room");
            sender.hp=0;
            Check(PrivateSnapshot(host.id).invitations.All(i=>i.fromId!=sender.id.ToString()),"eliminated players cannot leave an active invitation");
            sender.hp=60;phase=Phase.Turns;
            Check(PrivateSnapshot(host.id).invitations.Length==0,"invitations clear when room actions begin");
            phase=Phase.Discussion;Broadcast();
            Check(SaloonStations.Name(3)=="Have a Smoke","smoking destination uses the same capitalization as the other rooms");
        }
    }
}
#endif
