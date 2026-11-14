using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void VerifyRoomReservations()
        {
            void Check(bool ok,string text){if(!ok)throw new InvalidOperationException("Room reservation: "+text);Debug.Log("SALOON_RESERVATION_CHECK "+text);}
            bool sandbox=Sandbox;Sandbox=false;
            try
            {
                SaloonRoomReservationVerification.Run();
                var host=players[Connection.LocalId];var peers=players.Values.Where(p=>p!=host).Take(2).ToArray();
                foreach(var p in players.Values){p.hp=60;p.chosenRoom=0;p.companion=null;p.planSelected=false;}
                rounds.Start(players.Keys,Time.time,30,30,6);phase=rounds.Phase;PositionEveryone(true);Broadcast();
                OnCommand(host.id,new Command{kind="room",x=2,name=peers[0].id.ToString(),planVersion=host.planVersion+1});
                var invitation=InvitationsFor(peers[0]).Single(i=>i.fromId==host.id.ToString());
                OnCommand(peers[0].id,new Command{kind="joinInvite",x=2,name=host.id.ToString(),invitationVersion=invitation.planVersion,planVersion=peers[0].planVersion+1});
                Check(rounds.IsReady(host.id)&&rounds.IsReady(peers[0].id),"accepting locks both partners in one transaction");
                OnCommand(peers[1].id,new Command{kind="planReady",x=2,planVersion=peers[1].planVersion+1});
                Check(!rounds.IsReady(peers[1].id)&&peers[1].notice.Contains("full"),"third player receives a clear full-room rejection");
                OnCommand(peers[1].id,new Command{kind="room",x=1,name=peers[0].id.ToString(),planVersion=peers[1].planVersion+1});
                Check(peers[1].companion==null&&peers[1].notice.Contains("locked"),"a locked partner cannot be invited elsewhere");
                var snapshot=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(peers[1].id),out _));
                Check(snapshot.players.Single(p=>p.id==host.id.ToString()).chosenRoom==2&&snapshot.players.Single(p=>p.id==host.id.ToString()).companion==peers[0].id.ToString(),"public plan includes selected room and partner over the wire");
                Check(snapshot.players.Single(p=>p.id==host.id.ToString()).ready,"public plan identifies locked players");
                OnCommand(host.id,new Command{kind="ready",x=0,planVersion=host.planVersion+1});
                Check(!rounds.IsReady(host.id)&&!rounds.IsReady(peers[0].id)&&!peers[0].companion.HasValue,"unlock releases both partners");
                OnCommand(peers[1].id,new Command{kind="planReady",x=2,planVersion=peers[1].planVersion+1});
                Check(rounds.IsReady(peers[1].id),"released room becomes available immediately");
            }
            finally{Sandbox=sandbox;}
            Debug.Log("SALOON_RESERVATIONS PASS");
        }
#endif
    }
}
