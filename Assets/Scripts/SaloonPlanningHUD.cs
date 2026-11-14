using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        static readonly Color PlannerInk=new Color(.94f,.90f,.80f),PlannerMuted=new Color(.67f,.69f,.62f),PlannerGold=new Color(.91f,.69f,.40f);

        void PlannerText(float x,float y,float width,float height,string text,int size=15,bool muted=false)
            =>Text(x,y,width,height,text,new GUIStyle(body){fontSize=size,normal={textColor=muted?PlannerMuted:PlannerInk}});

        bool PlannerAction(float x,float y,float width,float height,string text,bool primary=false)
            =>GUI.Button(new Rect(x,y,width,height),text,primary?plannerPrimary:plannerButton);

        string PlanDescription(PlayerView person)
        {
            if(!person.planSelected&&!person.ready)return "No room selected";
            var partner=Game.View.players.FirstOrDefault(p=>p.id==person.companion);
            string room=SaloonStations.Name(person.chosenRoom);
            return person.ready?"LOCKED · "+room+(partner==null?" · no partner":" with "+partner.name)
                :partner!=null?"Inviting "+partner.name+" · "+room:"Choosing · "+room;
        }
        void IncomingInvitationNotice()
        {
            if(Game.View.phase!=Phase.Discussion||Game.MenuOpen||Game.ShowRole||Game.RoomPlannerOpen)return;
            float y=145;
            foreach(var person in Game.View.players.Where(p=>p.alive))
            {
                Text(908,y,350,20,person.name+(person.id==Game.LocalId?" (you)":""),new GUIStyle(body){fontSize=14});
                Text(908,y+19,350,27,PlanDescription(person),new GUIStyle(small){fontSize=12});y+=49;
            }
            if(Game.IncomingInvitations.Length>0)
                Text(280,145,600,28,"You have an invitation · Right-click to accept",new GUIStyle(body){fontSize=15,alignment=TextAnchor.MiddleCenter});
        }

        void RoomChoices()
        {
            Game.RoomChooserRenderFrames++;
            var self=Game.Self;if(self==null)return;
            var living=Game.View.players.Where(p=>p.alive).ToArray();
            var invitations=Game.IncomingInvitations;
            int selected=Game.PlannedRoom;
            const float left=120,top=100,width=1040,height=584;
            Fill(new Rect(left+4,top+5,width,height),new Color(0,0,0,.22f));
            Fill(new Rect(left,top,width,height),new Color(.055f,.07f,.065f,.98f));
            Fill(new Rect(left,top,3,height),PlannerGold);
            PlannerText(144,119,640,40,"Choose a room. Lock your plan.",27);
            if(PlannerAction(1068,122,68,29,"Close"))Game.RoomPlannerOpen=false;
            PlannerText(144,163,500,24,"Bar and smoking allow solo visits. Minigames need two players.",13,true);
            for(int room=0;room<4;room++)
            {
                float x=144+(room%2)*252,y=199+(room/2)*91;
                int capacity=Game.View.roomCapacities?[room]??(room==0?5:2);
                int locked=living.Count(p=>p.chosenRoom==room&&p.ready),interested=living.Count(p=>p.chosenRoom==room&&p.planSelected&&!p.ready);
                bool full=locked>=capacity,own=selected==room&&(self.planSelected||Game.PlanPending||Game.PlanReady);
                bool enabled=GUI.enabled;GUI.enabled=enabled&&(!full||(self.ready&&self.chosenRoom==room));
                var style=new GUIStyle(own?plannerPrimary:plannerButton){alignment=TextAnchor.UpperLeft,padding=new RectOffset(13,10,10,4),fontSize=19};
                if(GUI.Button(new Rect(x,y,240,79),(own?"✓ ":"")+SaloonStations.Name(room),style))Game.ChooseRoom(room,"");
                GUI.enabled=enabled;
                PlannerText(x+13,y+40,220,28,locked+" / "+capacity+" locked · "+interested+" choosing"+(full?" · FULL":""),12,true);
            }
            PlannerText(674,163,460,24,"EVERYONE'S PLAN",13,true);
            for(int i=0;i<living.Length;i++)
            {
                var person=living[i];float y=199+i*67;
                Fill(new Rect(674,y,462,60),person.ready?new Color(.13f,.19f,.14f):new Color(.09f,.11f,.10f));
                PlannerText(686,y+7,330,22,person.name+(person.id==self.id?" (you)":""),16);
                PlannerText(686,y+31,435,24,PlanDescription(person),12,!person.ready);
                bool available=person.id!=self.id&&!person.ready&&!Game.PlanReady;
                int capacity=Game.View.roomCapacities?[selected]??(selected==0?5:2);
                int reserved=living.Count(p=>p.ready&&p.chosenRoom==selected);
                bool inviting=Game.PlannedCompanion==person.id;
                if(available&&!inviting&&reserved+2<=capacity)
                    if(PlannerAction(1038,y+7,86,26,person.bot?"Go with":"Invite"))
                    {if(person.bot)Game.LeaveForRoom(selected,person.id);else Game.ChooseRoom(selected,person.id);}
            }
            PlannerText(144,390,490,25,invitations.Length>0?"INVITATIONS · ACCEPT TO LOCK BOTH PLAYERS":"Invite an available player, or lock your room choice.",13,true);
            for(int i=0;i<Mathf.Min(invitations.Length,3);i++)
            {
                var invitation=invitations[i];float y=426+i*48;
                PlannerText(144,y,376,40,invitation.fromName+" · "+SaloonStations.Name(invitation.room),15);
                if(PlannerAction(532,y-2,104,34,"Accept",true))Game.JoinInvitation(invitation.fromId);
            }
            Fill(new Rect(144,575,992,1),new Color(1,1,1,.1f));
            string status=Game.PlanPending?"Saving your plan…":Game.PlanReady?"Your plan is locked.":Game.View.notice;
            if(string.IsNullOrEmpty(status))status="Select a room, then lock your choice or invite someone.";
            PlannerText(144,588,764,42,status,14,true);
            string action=Game.PlanReady?"Unlock / change":!string.IsNullOrEmpty(Game.PlannedCompanion)?"Cancel invitation":"Lock choice";
            if(PlannerAction(930,591,206,42,action,true))
            {
                if(Game.PlanReady)Game.ToggleReady();
                else if(!string.IsNullOrEmpty(Game.PlannedCompanion))Game.ChooseRoom(selected,"");
                else Game.LeaveForRoom(selected);
            }
            PlannerText(144,644,992,24,"Space · lock / unlock    |    Everyone locked starts early. Otherwise, move when the discussion timer ends.",12,true);
        }
    }
}
