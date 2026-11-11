using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        int plannerRoom=-1,plannerRound=-1,plannerPose=-1;
        Vector2 invitationScroll,companyScroll;
        static readonly Color PlannerInk=new Color(.94f,.90f,.80f),PlannerMuted=new Color(.67f,.69f,.62f),PlannerGold=new Color(.91f,.69f,.40f);

        void PlannerText(float x,float y,float width,float height,string text,int size=15,bool muted=false)
            =>Text(x,y,width,height,text,new GUIStyle(body){fontSize=size,normal={textColor=muted?PlannerMuted:PlannerInk}});

        bool PlannerAction(float x,float y,float width,float height,string text,bool primary=false)
            =>GUI.Button(new Rect(x,y,width,height),text,primary?plannerPrimary:plannerButton);

        void IncomingInvitationNotice()
        {
            if(Game.View.phase!=Phase.Discussion||Game.MenuOpen||Game.ShowRole||Game.RoomPlannerOpen)return;
            var invitations=Game.IncomingInvitations;
            float y=Game.View.posseStage>0?145:111;
            foreach(var invitation in invitations.Take(3))
            {
                Text(280,y,720,25,invitation.fromName+" invites you to "+SaloonStations.Name(invitation.room)+" · Right-click to join",new GUIStyle(body){fontSize=15,alignment=TextAnchor.MiddleCenter});
                y+=26;
            }
        }

        void RoomChoices()
        {
            Game.RoomChooserRenderFrames++;
            var self=Game.Self;if(self==null)return;
            if(plannerRound!=Game.View.round||plannerPose!=self.poseVersion)
            {plannerRound=Game.View.round;plannerPose=self.poseVersion;plannerRoom=-1;companyScroll=invitationScroll=Vector2.zero;}
            var people=Game.View.players.Where(p=>p.id!=self.id&&p.alive).ToArray();
            var invitations=Game.IncomingInvitations;
            const float left=824,top=100,width=432,padding=22,inner=388;
            float inviteHeight=invitations.Length==0?0:26+Mathf.Min(invitations.Length,2)*66+12;
            float bodyTop=top+82+inviteHeight;
            float contentHeight=plannerRoom<0?208:76+Mathf.Min(people.Length,4)*42+50;
            float height=Mathf.Min(584,82+inviteHeight+contentHeight+70),footer=top+height-66;
            Fill(new Rect(left+4,top+5,width,height),new Color(0,0,0,.22f));
            Fill(new Rect(left,top,width,height),new Color(.055f,.07f,.065f,.98f));
            Fill(new Rect(left,top,3,height),PlannerGold);
            PlannerText(left+padding,top+17,290,39,"Where to?",27);
            if(PlannerAction(left+342,top+20,68,29,"Close"))Game.RoomPlannerOpen=false;

            if(invitations.Length>0)
            {
                PlannerText(left+padding,top+74,inner,21,"YOU'RE INVITED",12,true);
                float visible=Mathf.Min(invitations.Length,2)*66;
                invitationScroll=GUI.BeginScrollView(new Rect(left+padding,top+100,inner,visible),invitationScroll,new Rect(0,0,inner-10,invitations.Length*66));
                for(int i=0;i<invitations.Length;i++)
                {
                    var invitation=invitations[i];float y=i*66;
                    Fill(new Rect(0,y,inner-10,60),new Color(.115f,.14f,.12f));
                    bool together=Game.PlannedCompanion==invitation.fromId&&Game.PlannedRoom==invitation.room;
                    PlannerText(12,y+5,262,21,invitation.fromName+(together?" is joining you at":" invited you to"),13,true);
                    PlannerText(12,y+25,262,28,SaloonStations.Name(invitation.room),20);
                    if(PlannerAction(inner-106,y+12,86,35,together?"Let's go":"Join",true))Game.JoinInvitation(invitation.fromId);
                }
                GUI.EndScrollView();
            }

            if(plannerRoom<0)
            {
                PlannerText(left+padding,bodyTop,inner,24,"Choose a destination",15,true);
                string[] descriptions={"Stay with the crowd","Call it. Face your opponent.","Play a hand. Read your rival.","Step outside. Talk in private."};
                for(int room=0;room<4;room++)
                {
                    float x=left+padding+(room%2)*200,y=bodyTop+29+(room/2)*83;
                    var style=new GUIStyle(plannerButton){alignment=TextAnchor.UpperLeft,padding=new RectOffset(13,10,12,4),fontSize=19};
                    if(GUI.Button(new Rect(x,y,188,73),SaloonStations.Name(room),style)){plannerRoom=room;companyScroll=Vector2.zero;}
                    PlannerText(x+13,y+40,165,28,descriptions[room],12,true);
                }
            }
            else
            {
                if(PlannerAction(left+padding,bodyTop,82,28,"‹ Rooms"))plannerRoom=-1;
                if(plannerRoom>=0)
                {
                    PlannerText(left+120,bodyTop-3,290,35,SaloonStations.Name(plannerRoom),23);
                    PlannerText(left+padding,bodyTop+38,inner,24,"Who are you going with?",15,true);
                    float listTop=bodyTop+69,listHeight=Mathf.Max(42,footer-58-listTop);
                    companyScroll=GUI.BeginScrollView(new Rect(left+padding,listTop,inner,listHeight),companyScroll,new Rect(0,0,inner-10,people.Length*42));
                    for(int i=0;i<people.Length;i++)
                    {
                        var person=people[i];
                        string action=(person.bot?"Go with ":"Invite ")+person.name;
                        if(PlannerAction(0,i*42,inner-10,36,action))
                        {
                            if(person.bot)Game.LeaveForRoom(plannerRoom,person.id);
                            else Game.ChooseRoom(plannerRoom,person.id);
                        }
                    }
                    GUI.EndScrollView();
                    if(PlannerAction(left+padding,footer-48,inner,38,"Go on your own",true))Game.LeaveForRoom(plannerRoom);
                }
            }
            Fill(new Rect(left+padding,footer-1,inner,1),new Color(1,1,1,.1f));
            string status;
            var partner=people.FirstOrDefault(p=>p.id==Game.PlannedCompanion);
            if(Game.PlanPending)status="Sending your choice…";
            else if(Game.PlanReady)status="Ready for "+SaloonStations.Name(Game.PlannedRoom)+(partner==null?"":" with "+partner.name)+".";
            else if(Game.View.notice?.Contains("can't join")==true||Game.View.notice?.Contains("is full")==true||Game.View.notice?.StartsWith("That invitation changed")==true)status=Game.View.notice;
            else if(partner!=null&&!partner.bot)status="Invited "+partner.name+" to "+SaloonStations.Name(Game.PlannedRoom)+". Their reply will appear above.";
            else status=plannerRoom>=0?"Pick someone to join you, or go on your own.":"Join an invitation, or choose where to go.";
            PlannerText(left+padding,footer+11,inner,45,status,14,true);
        }
    }
}
