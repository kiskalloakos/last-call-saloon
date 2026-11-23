using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        static readonly Color PlannerInk=new Color(.94f,.90f,.80f),PlannerMuted=new Color(.67f,.69f,.62f),PlannerAccent=new Color(.42f,.63f,.82f);
        Vector2 plannerPlayersScroll;

        GUIStyle PlannerStyle(int size,bool muted=false)
            =>new GUIStyle(body){fontSize=size,richText=false,wordWrap=true,normal={textColor=muted?PlannerMuted:PlannerInk}};

        bool PlannerAction(float x,float y,float width,float height,string text,bool primary=false)
            =>GUI.Button(new Rect(x,y,width,height),text,primary?plannerPrimary:plannerButton);

        string PlanDescription(PlayerView person)
        {
            if(person.id==Game.LocalId&&Game.View.toiletDuty)return "Solo toilet mission · Bar return reserved";
            if(person.escaping)return "Attempting escape · Bar";
            if(!person.planSelected&&!person.ready)return "No room selected";
            var partner=Game.View.players.FirstOrDefault(p=>p.id==person.companion);
            string room=Game.RoomName(person.chosenRoom);
            return person.ready?room+(partner==null?"":" with "+partner.name)+" · Locked"
                :partner!=null?"Invited "+partner.name+" to "+room:"Choosing "+room;
        }
        void IncomingInvitationNotice()
        {
            if(Game.View.phase!=Phase.Discussion||Game.MenuOpen||Game.ShowRole||Game.RoomPlannerOpen)return;
            float y=145;
            foreach(var person in Game.View.players.Where(p=>p.alive))
            {
                y+=WrappedText(908,y,338,person.name+(person.id==Game.LocalId?" (you)":""),new GUIStyle(body){fontSize=14});
                y+=WrappedText(908,y,338,PlanDescription(person),new GUIStyle(small){fontSize=12})+8;
            }
            if(Game.IncomingInvitations.Length>0)
                Text(280,145,600,28,"New invitation · Open room plans to accept.",new GUIStyle(body){fontSize=16,alignment=TextAnchor.MiddleCenter});
        }

        void RoomChoices()
        {
            Game.RoomChooserRenderFrames++;
            var self=Game.Self;if(self==null)return;
            var living=Game.View.players.Where(p=>p.alive).OrderByDescending(p=>p.id==self.id)
                .ThenByDescending(p=>Game.IncomingInvitations.Any(i=>i.fromId==p.id)).ToArray();
            int selected=Game.PlannedRoom;
            bool hasChoice=self.planSelected||Game.PlanPending||Game.PlanReady;
            bool trainingPlan=Game.TutorialWalkthrough&&Game.WalkthroughStep==SaloonGame.WalkthroughBeat.Plan;
            var panel=new Rect(108,88,1064,596);
            const float left=136,roomColumn=460,planX=652,planWidth=492;
            Fill(new Rect(panel.x+4,panel.y+5,panel.width,panel.height),new Color(0,0,0,.22f));
            Fill(panel,new Color(.055f,.07f,.065f,.98f));
            Fill(new Rect(panel.x,panel.y,panel.width,3),PlannerAccent);
            WrappedText(left,111,460,"Choose your room",PlannerStyle(27));
            if(PlannerAction(1064,112,80,32,"Close"))Game.RoomPlannerOpen=false;
            Fill(new Rect(624,162,1,492),new Color(1,1,1,.12f));
            if(!string.IsNullOrEmpty(Game.View.escapeWarning))
                WrappedText(left,145,1008,Game.View.escapeWarning,new GUIStyle(PlannerStyle(15)){normal={textColor=new Color(.98f,.45f,.40f)}});
            const float gridTop=174,roomGap=12,roomHeight=110,roomWidth=(roomColumn-roomGap)/2;
            for(int room=0;room<4;room++)
            {
                float x=left+(room%2)*(roomWidth+roomGap),y=gridTop+(room/2)*(roomHeight+roomGap);
                int capacity=Game.View.roomCapacities?[room]??(room==0?SaloonRules.BarCapacity:2);
                int locked=living.Count(p=>p.chosenRoom==room&&p.ready),interested=living.Count(p=>p.chosenRoom==room&&p.planSelected&&!p.ready);
                bool full=locked>=capacity,own=selected==room&&hasChoice;
                bool enabled=GUI.enabled;GUI.enabled=enabled&&!Game.View.toiletDuty&&(!full||(self.ready&&self.chosenRoom==room));
                var style=new GUIStyle(plannerButton){alignment=TextAnchor.UpperLeft,padding=new RectOffset(13,10,10,4),fontSize=20};
                var card=new Rect(x,y,roomWidth,roomHeight);
                if(GUI.Button(card,"",style))Game.ChooseRoom(room,"");
                GUI.enabled=enabled;
                if(Game.View.medicalRoom>0&&room==Game.View.medicalRoom)MedicalRoomEvent(card);
                if(room==0&&!string.IsNullOrEmpty(Game.View.escapeWarning))EscapeRoomEvent(card);
                if(own||trainingPlan&&room==0)Outline(new Rect(card.x+3,card.y+3,card.width-6,card.height-6),PlannerAccent);
                WrappedText(x+13,y+15,roomWidth-62,Game.RoomName(room),PlannerStyle(23));
                if(room==1&&Game.View.medicalRoom!=1)WrappedText(x+13,y+51,roomWidth-26,"This round · winner opens",PlannerStyle(13,true));
                if(room==3)WrappedText(x+13,y+51,roomWidth-26,"Out the back",PlannerStyle(14,true));
                WrappedText(x+13,y+83,roomWidth-26,full&&!own?"Full":locked+" / "+capacity+" locked"+(interested>0?" · "+interested+" choosing":""),PlannerStyle(14,true));
            }
            Fill(new Rect(left,429,roomColumn,1),new Color(1,1,1,.12f));
            if(hasChoice)WrappedText(left,446,roomColumn,Game.PlanReady?"YOUR LOCKED PLAN":"YOUR CHOICE",PlannerStyle(14,true));
            WrappedText(left,472,roomColumn,hasChoice?Game.RoomName(selected):"",PlannerStyle(23));
            var partner=living.FirstOrDefault(p=>p.id==Game.PlannedCompanion);
            string detail=Game.View.toiletDuty?"Solo toilet mission · returns to Bar":Game.PlanPending?"Saving your plan…":partner!=null?(Game.PlanReady?"With ":"Invitation sent to ")+partner.name:
                "";
            WrappedText(left,516,roomColumn,detail,PlannerStyle(15,true));
            bool invitingCompanion=!string.IsNullOrEmpty(Game.PlannedCompanion);
            string action=Game.PlanReady?"Unlock choice":invitingCompanion?"Cancel invitation":"Lock choice";
            bool wasEnabled=GUI.enabled;GUI.enabled=wasEnabled&&hasChoice&&!Game.View.toiletDuty;
            if(PlannerAction(left,568,roomColumn,44,action,true))
            {
                if(Game.PlanReady)Game.ToggleReady();
                else if(invitingCompanion)Game.ChooseRoom(selected,"");
                else Game.LeaveForRoom(selected);
            }
            GUI.enabled=wasEnabled;
            if(Game.View.moneyBag)
            {
                if(!Game.View.toiletDuty&&PlannerAction(left,622,roomColumn,32,Game.View.escapePlanned?"Cancel escape attempt":"Attempt escape at Bar"))Game.ChooseEscape(!Game.View.escapePlanned);
            }
            WrappedText(planX,118,370,"Players & invitations",PlannerStyle(21));
            GUI.enabled=wasEnabled&&!Game.View.toiletDuty;
            DrawPlayerPlans(new Rect(planX,174,planWidth,480),living,self,selected,hasChoice);GUI.enabled=wasEnabled;
        }

        void EscapeRoomEvent(Rect card)
        {
            float pulse=Game.Graphics?.Current.reducedMotion==true?.5f:.5f+.5f*Mathf.Sin(Time.unscaledTime*2.2f);
            var danger=new Color(.98f,.45f,.40f);
            Fill(card,new Color(.22f+.04f*pulse,.08f,.09f));
            Outline(card,new Color(danger.r,danger.g,danger.b,.6f+.3f*pulse));
            var label=PlannerStyle(14);label.normal.textColor=danger;
            WrappedText(card.x+13,card.y+51,card.width-26,"Escape attempt",label);
            Fill(new Rect(card.xMax-32,card.y+18,5,17),danger);
            Fill(new Rect(card.xMax-32,card.y+39,5,5),danger);
        }

        void MedicalRoomEvent(Rect card)
        {
            bool reduced=Game.Graphics?.Current.reducedMotion==true;
            float pulse=reduced?.5f:.5f+.5f*Mathf.Sin(Time.unscaledTime*2.2f);
            var mint=new Color(.32f,.88f,.72f);
            Fill(card,Color.Lerp(new Color(.055f,.17f,.16f),new Color(.075f,.22f,.20f),pulse*.65f));
            for(int i=3;i>0;i--)
                Outline(new Rect(card.x-i*2,card.y-i*2,card.width+i*4,card.height+i*4),new Color(mint.r,mint.g,mint.b,.035f*pulse));
            Outline(card,new Color(mint.r,mint.g,mint.b,.45f+.35f*pulse));
            float x=card.xMax-46,y=card.y+17;
            Outline(new Rect(x+9,y,14,7),mint);
            Fill(new Rect(x,y+6,32,24),mint);
            Fill(new Rect(x+2,y+8,28,20),new Color(.06f,.20f,.18f));
            Fill(new Rect(x+13,y+11,6,14),mint);
            Fill(new Rect(x+9,y+15,14,6),mint);
            var label=PlannerStyle(14);label.normal.textColor=mint;
            WrappedText(card.x+13,card.y+51,card.width-26,"Medical supplies",label);
        }

        void DrawPlayerPlans(Rect area,PlayerView[] living,PlayerView self,int selected,bool hasChoice)
        {
            const float inset=12,verticalInset=10,actionWidth=110,rowGap=6;
            float rowWidth=area.width-14,textWidth=rowWidth-actionWidth-3*inset;
            var nameStyle=PlannerStyle(17);
            var planStyle=PlannerStyle(15,true);
            var invitations=Game.IncomingInvitations;
            var rows=living.Select(person=>
            {
                var invitation=invitations.FirstOrDefault(i=>i.fromId==person.id);
                string name=person.name+(person.id==self.id?" (you)":"");
                string plan=invitation!=null?"Invites you to "+Game.RoomName(invitation.room):PlanDescription(person);
                float height=Mathf.Max(72,2*verticalInset+WrappedHeight(name,textWidth,nameStyle)+WrappedHeight(plan,textWidth,planStyle));
                return (Name:name,Plan:plan,Invitation:invitation,Height:height);
            }).ToArray();
            float total=rows.Sum(row=>row.Height)+Mathf.Max(0,rows.Length-1)*rowGap;
            int capacity=Game.View.roomCapacities?[selected]??(selected==0?SaloonRules.BarCapacity:2);
            bool hasSeats=hasChoice&&living.Count(p=>p.ready&&p.chosenRoom==selected)+2<=capacity;
            plannerPlayersScroll=GUI.BeginScrollView(area,plannerPlayersScroll,new Rect(0,0,rowWidth,total));
            float y=0;
            for(int i=0;i<rows.Length;i++)
            {
                var row=rows[i];var person=living[i];
                Fill(new Rect(0,y,rowWidth,row.Height),new Color(.09f,.11f,.10f));
                if(person.id==self.id||row.Invitation!=null)Fill(new Rect(0,y,2,row.Height),PlannerAccent);
                float nameHeight=WrappedText(inset,y+verticalInset,textWidth,row.Name,nameStyle);
                WrappedText(inset,y+verticalInset+nameHeight,textWidth,row.Plan,planStyle);
                float actionX=rowWidth-inset-actionWidth,actionY=y+(row.Height-34)/2;
                if(row.Invitation!=null)
                {
                    if(PlannerAction(actionX,actionY,actionWidth,34,"Accept invite",true))Game.JoinInvitation(row.Invitation.fromId);
                }
                else if(hasSeats&&person.id!=self.id&&!person.ready&&!Game.PlanReady&&Game.PlannedCompanion!=person.id)
                {
                    if(PlannerAction(actionX,actionY,actionWidth,34,person.bot?"Go with":"Invite"))
                    {if(person.bot)Game.LeaveForRoom(selected,person.id);else Game.ChooseRoom(selected,person.id);}
                }
                y+=row.Height+rowGap;
            }
            GUI.EndScrollView();
        }
    }
}
