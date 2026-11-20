using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        static readonly Color PlannerInk=new Color(.94f,.90f,.80f),PlannerMuted=new Color(.67f,.69f,.62f),PlannerGold=new Color(.91f,.69f,.40f);
        Vector2 plannerInvitationScroll,plannerPlayersScroll;

        GUIStyle PlannerStyle(int size,bool muted=false)
            =>new GUIStyle(body){fontSize=size,richText=false,wordWrap=true,normal={textColor=muted?PlannerMuted:PlannerInk}};

        bool PlannerAction(float x,float y,float width,float height,string text,bool primary=false)
            =>GUI.Button(new Rect(x,y,width,height),text,primary?plannerPrimary:plannerButton);

        string PlanDescription(PlayerView person)
        {
            if(!person.planSelected&&!person.ready)return "No room selected";
            var partner=Game.View.players.FirstOrDefault(p=>p.id==person.companion);
            string room=SaloonStations.Name(person.chosenRoom);
            return person.ready?room+(partner==null?"":" with "+partner.name)+" (locked)"
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
                Text(280,145,600,28,"Right-click to see your invitations.",new GUIStyle(body){fontSize=16,alignment=TextAnchor.MiddleCenter});
        }

        void RoomChoices()
        {
            Game.RoomChooserRenderFrames++;
            var self=Game.Self;if(self==null)return;
            var living=Game.View.players.Where(p=>p.alive).ToArray();
            var invitations=Game.IncomingInvitations;
            int selected=Game.PlannedRoom;
            var panel=new Rect(108,88,1064,596);
            const float inset=28,gap=24;
            float left=panel.x+inset,right=panel.xMax-inset,width=right-left;
            const float roomColumn=488;
            float planX=left+roomColumn+gap,planWidth=right-planX;
            Fill(new Rect(panel.x+4,panel.y+5,panel.width,panel.height),new Color(0,0,0,.22f));
            Fill(panel,new Color(.055f,.07f,.065f,.98f));
            Fill(new Rect(panel.x,panel.y,3,panel.height),PlannerGold);
            float top=panel.y+inset;
            top+=WrappedText(left,top,roomColumn,"Choose a room",PlannerStyle(27))+10;
            if(PlannerAction(right-80,panel.y+inset,80,32,"Close"))Game.RoomPlannerOpen=false;
            WrappedText(left,top,roomColumn,"Minigames need two players.",PlannerStyle(16,true));
            float gridTop=top+32;
            const float roomGap=12,roomHeight=74;
            const float roomWidth=(roomColumn-roomGap)/2;
            for(int room=0;room<4;room++)
            {
                float x=left+(room%2)*(roomWidth+roomGap),y=gridTop+(room/2)*(roomHeight+roomGap);
                int capacity=Game.View.roomCapacities?[room]??(room==0?5:2);
                int locked=living.Count(p=>p.chosenRoom==room&&p.ready),interested=living.Count(p=>p.chosenRoom==room&&p.planSelected&&!p.ready);
                bool full=locked>=capacity,own=selected==room&&(self.planSelected||Game.PlanPending||Game.PlanReady);
                bool enabled=GUI.enabled;GUI.enabled=enabled&&(!full||(self.ready&&self.chosenRoom==room));
                var style=new GUIStyle(own?plannerPrimary:plannerButton){alignment=TextAnchor.UpperLeft,padding=new RectOffset(13,10,10,4),fontSize=19};
                if(GUI.Button(new Rect(x,y,roomWidth,roomHeight),(own?"✓ ":"")+SaloonStations.Name(room),style))Game.ChooseRoom(room,"");
                GUI.enabled=enabled;
                WrappedText(x+13,y+39,roomWidth-26,full?"Full":locked+" of "+capacity+" locked. "+interested+" choosing.",PlannerStyle(15,true));
            }
            var helpStyle=PlannerStyle(15,true);
            string help=Game.View.phase==Phase.Discussion?"Press Space to lock or unlock. Right-click to close.":"Select Lock choice when you are ready.";
            float helpY=panel.yMax-18-WrappedHeight(help,width,helpStyle);
            WrappedText(left,helpY,width,help,helpStyle);
            const float actionWidth=206,actionHeight=42;
            float statusWidth=width-actionWidth-gap;
            string status=Game.PlanPending?"Saving your plan…":Game.PlanReady?"Your plan is locked.":Game.View.notice;
            var statusStyle=PlannerStyle(16,true);
            float statusHeight=WrappedHeight(status,statusWidth,statusStyle);
            float footerY=helpY-12-Mathf.Max(actionHeight,statusHeight);
            Fill(new Rect(left,footerY-12,width,1),new Color(1,1,1,.14f));
            WrappedText(left,footerY+Mathf.Max(0,(actionHeight-statusHeight)/2),statusWidth,status,statusStyle);
            bool invitingCompanion=!string.IsNullOrEmpty(Game.PlannedCompanion);
            string action=Game.PlanReady?"Unlock / change":invitingCompanion?"Cancel invitation":selected==3?"Go alone":"Lock choice";
            if(PlannerAction(right-actionWidth,footerY,actionWidth,actionHeight,action,true))
            {
                if(Game.PlanReady)Game.ToggleReady();
                else if(invitingCompanion)Game.ChooseRoom(selected,"");
                else Game.LeaveForRoom(selected);
            }
            float bodyBottom=footerY-18;
            WrappedText(planX,top,planWidth,"Player plans",PlannerStyle(17,true));
            DrawPlayerPlans(new Rect(planX,gridTop,planWidth,bodyBottom-gridTop),living,self,selected);

            float invitesY=gridTop+2*roomHeight+roomGap+15;
            string inviteHint=invitations.Length>0?"Accept an invite to lock both plans.":"Lock your room choice or invite another player.";
            invitesY+=WrappedText(left,invitesY,roomColumn,inviteHint,PlannerStyle(16,true))+8;
            if(invitations.Length>0)DrawPlannerInvitations(new Rect(left,invitesY,roomColumn,bodyBottom-invitesY),invitations);
        }

        void DrawPlayerPlans(Rect area,PlayerView[] living,PlayerView self,int selected)
        {
            const float inset=12,verticalInset=6,actionWidth=86,rowGap=4;
            float rowWidth=area.width-14;
            float nameWidth=rowWidth-actionWidth-3*inset,planWidth=rowWidth-2*inset;
            var nameStyle=PlannerStyle(17);
            var planStyle=PlannerStyle(15);
            var rows=living.Select(person=>
            {
                string name=person.name+(person.id==self.id?" (you)":"");
                string plan=PlanDescription(person);
                float height=Mathf.Max(62,2*verticalInset+WrappedHeight(name,nameWidth,nameStyle)+WrappedHeight(plan,planWidth,planStyle));
                return (Name:name,Plan:plan,Height:height);
            }).ToArray();
            float total=rows.Sum(row=>row.Height)+Mathf.Max(0,rows.Length-1)*rowGap;
            int capacity=Game.View.roomCapacities?[selected]??(selected==0?5:2);
            bool hasSeats=living.Count(p=>p.ready&&p.chosenRoom==selected)+2<=capacity;
            plannerPlayersScroll=GUI.BeginScrollView(area,plannerPlayersScroll,new Rect(0,0,rowWidth,total));
            float y=0;
            for(int i=0;i<rows.Length;i++)
            {
                var row=rows[i];
                var person=living[i];
                Fill(new Rect(0,y,rowWidth,row.Height),person.ready?new Color(.13f,.19f,.14f):new Color(.09f,.11f,.10f));
                float nameHeight=WrappedText(inset,y+verticalInset,nameWidth,row.Name,nameStyle);
                planStyle.normal.textColor=person.ready?PlannerInk:PlannerMuted;
                WrappedText(inset,y+verticalInset+nameHeight,planWidth,row.Plan,planStyle);
                if(hasSeats&&person.id!=self.id&&!person.ready&&!Game.PlanReady&&Game.PlannedCompanion!=person.id)
                {
                    if(PlannerAction(rowWidth-inset-actionWidth,y+8,actionWidth,27,person.bot?"Go with":"Invite"))
                    {if(person.bot)Game.LeaveForRoom(selected,person.id);else Game.ChooseRoom(selected,person.id);}
                }
                y+=row.Height+rowGap;
            }
            GUI.EndScrollView();
        }

        void DrawPlannerInvitations(Rect area,RoomInvitation[] invitations)
        {
            const float buttonWidth=104,rowGap=4;
            float rowWidth=area.width-14,textWidth=rowWidth-buttonWidth-16;
            var style=PlannerStyle(16);
            var rows=invitations.Select(invitation=>
            {
                string text=invitation.fromName+" · "+SaloonStations.Name(invitation.room);
                return (Text:text,Height:Mathf.Max(32,WrappedHeight(text,textWidth,style)+4));
            }).ToArray();
            plannerInvitationScroll=GUI.BeginScrollView(area,plannerInvitationScroll,new Rect(0,0,rowWidth,rows.Sum(row=>row.Height)+(rows.Length-1)*rowGap));
            float y=0;
            for(int i=0;i<rows.Length;i++)
            {
                float textHeight=WrappedHeight(rows[i].Text,textWidth,style);
                WrappedText(0,y+(rows[i].Height-textHeight)/2,textWidth,rows[i].Text,style);
                if(PlannerAction(rowWidth-buttonWidth,y,buttonWidth,32,"Accept",true))Game.JoinInvitation(invitations[i].fromId);
                y+=rows[i].Height+rowGap;
            }
            GUI.EndScrollView();
        }
    }
}
