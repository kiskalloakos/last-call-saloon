using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        bool hasPlanDraft,draftReady;
        int draftRoom,draftRound=-1;
        Phase draftPhase;
        string draftCompanion="";
        public int PlannedRoom=>hasPlanDraft?draftRoom:Self?.chosenRoom??0;
        public string PlannedCompanion=>hasPlanDraft?draftCompanion:Self?.companion??"";
        public bool PlanReady=>hasPlanDraft?draftReady:Self?.ready==true;
        public bool PlanPending=>hasPlanDraft&&Self!=null&&(Self.chosenRoom!=draftRoom||(Self.companion??"")!=draftCompanion||Self.ready!=draftReady);

        void ResetPlanDraft(){hasPlanDraft=draftReady=false;draftRound=-1;draftRoom=0;draftCompanion="";}
        void SyncPlanDraft(Snapshot snapshot,PlayerView self)
        {
            if(self==null||!self.alive||(snapshot.phase!=Phase.Discussion&&snapshot.phase!=Phase.Choosing))
            {ResetPlanDraft();return;}
            if(draftRound!=snapshot.round||draftPhase!=snapshot.phase)
            {ResetPlanDraft();draftRound=snapshot.round;draftPhase=snapshot.phase;}
            // Keep a draft for the whole discussion. Even a matching older acknowledgement
            // must not let a later in-flight snapshot undo a newer click (West/East/West).
            if(!hasPlanDraft){draftRoom=self.chosenRoom;draftCompanion=self.companion??"";draftReady=self.ready;}
        }
        void EditRoom(int room,string companion)
        {
            if(Self?.alive!=true||(View.phase!=Phase.Discussion&&View.phase!=Phase.Choosing)||room<0||room>2)return;
            draftRoom=room;draftCompanion=companion??"";draftReady=false;hasPlanDraft=true;draftRound=View.round;draftPhase=View.phase;
            Debug.Log("SALOON_PLAN send round="+View.round+" room="+room+" companion="+draftCompanion+" host="+Hosting);
            Connection.Send(new Command{kind="room",x=draftRoom,name=draftCompanion});
        }
        void EditReady()
        {
            if(Self?.alive!=true||View.phase!=Phase.Discussion)return;
            int room=PlannedRoom;string companion=PlannedCompanion;bool ready=!PlanReady;
            draftRoom=room;draftCompanion=companion;draftReady=ready;hasPlanDraft=true;draftRound=View.round;draftPhase=View.phase;
            Connection.Send(new Command{kind="ready",x=ready?1:0});
        }
    }
}
