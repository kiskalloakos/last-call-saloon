using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        bool hasPlanDraft,draftReady;
        int draftRoom,draftRound=-1,draftVersion,nextPlanVersion;
        Phase draftPhase;
        string draftCompanion="";
        public int PlannedRoom=>hasPlanDraft?draftRoom:Self?.chosenRoom??0;
        public string PlannedCompanion=>hasPlanDraft?draftCompanion:Self?.companion??"";
        public bool PlanReady=>hasPlanDraft?draftReady:Self?.ready==true;
        public bool PlanPending=>hasPlanDraft&&Self!=null&&(Self.chosenRoom!=draftRoom||(Self.companion??"")!=draftCompanion||Self.ready!=draftReady);

        void ResetPlanDraft(){hasPlanDraft=draftReady=false;draftRound=-1;draftRoom=0;draftCompanion="";draftVersion=0;}
        void SyncPlanDraft(Snapshot snapshot,PlayerView self)
        {
            if(self==null||!self.alive||(snapshot.phase!=Phase.Discussion&&snapshot.phase!=Phase.Choosing))
            {ResetPlanDraft();return;}
            if(draftRound!=snapshot.round||draftPhase!=snapshot.phase)
            {ResetPlanDraft();draftRound=snapshot.round;draftPhase=snapshot.phase;}
            // Acknowledge only the latest edit, including a rejected full-room reservation.
            // Older snapshots must not undo a newer local click.
            if(self.planVersion>=draftVersion)
            {
                draftRoom=self.chosenRoom;draftCompanion=self.companion??"";draftReady=self.ready;hasPlanDraft=true;
                draftVersion=self.planVersion;nextPlanVersion=Mathf.Max(nextPlanVersion,self.planVersion);
            }
        }
        void EditRoom(int room,string companion)
        {
            if(Self?.alive!=true||(View.phase!=Phase.Discussion&&View.phase!=Phase.Choosing)||room<0||room>3)return;
            draftRoom=room;draftCompanion=companion??"";draftReady=false;hasPlanDraft=true;draftRound=View.round;draftPhase=View.phase;
            Debug.Log("SALOON_PLAN send round="+View.round+" room="+room+" companion="+draftCompanion+" host="+Hosting);
            draftVersion=++nextPlanVersion;
            Connection.Send(new Command{kind="room",x=draftRoom,name=draftCompanion,planVersion=draftVersion});
        }
        void EditReady()
        {
            if(Self?.alive!=true||View.phase!=Phase.Discussion)return;
            int room=PlannedRoom;string companion=PlannedCompanion;bool ready=!PlanReady;
            draftRoom=room;draftCompanion=companion;draftReady=ready;hasPlanDraft=true;draftRound=View.round;draftPhase=View.phase;
            draftVersion=++nextPlanVersion;
            Connection.Send(new Command{kind="ready",x=ready?1:0,planVersion=draftVersion});
        }
    }
}
