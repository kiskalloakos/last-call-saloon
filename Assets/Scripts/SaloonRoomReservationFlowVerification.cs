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
            SaloonRoomReservationVerification.Run();
            var host=players[Connection.LocalId];var peers=players.Values.Where(p=>p.bot).Take(2).ToArray();
            foreach(var p in players.Values){p.hp=60;p.chosenRoom=0;p.companion=null;}
            rounds.Start(players.Keys,Time.time,30,30,6);phase=rounds.Phase;PositionEveryone(true);Broadcast();
            ChooseRoom(2,""); // Select first, ready last: clicking a room alone reserves nothing.
            foreach(var peer in peers)
            {OnCommand(peer.id,new Command{kind="room",x=2});OnCommand(peer.id,new Command{kind="ready",x=1});}
            var stale=PrivateSnapshot(host.id);
            ToggleReady();
            Check(!rounds.IsReady(host.id)&&rounds.IsReady(peers[0].id)&&rounds.IsReady(peers[1].id),"third Ready is rejected without disturbing the first two seats");
            Check(host.notice.Contains("full"),"full-room rejection explains how to choose another room");
            ApplySnapshot(stale);Check(PlanReady,"older acknowledgement cannot erase the pending Ready request");
            Broadcast();Check(!PlanReady&&!PlanPending,"latest rejection clears the optimistic Ready button and saving notice");
            OnCommand(peers[0].id,new Command{kind="ready",x=0});ToggleReady();Broadcast();
            Check(rounds.IsReady(host.id)&&PlanReady,"released seat can be reserved by the next requester");
            OnCommand(peers[0].id,new Command{kind="room",x=2,name=host.id.ToString()});
            host.companion=peers[0].id;
            OnCommand(peers[0].id,new Command{kind="ready",x=1});
            var assigned=rounds.AssignRooms(players.Values.Where(p=>p.Alive).Select(p=>p.id),id=>players[id].chosenRoom);
            Check(assigned[host.id]==2&&assigned[peers[1].id]==2&&assigned[peers[0].id]==0,"mutual invitations cannot displace earlier reservations");
            OnCommand(host.id,new Command{kind="ready",x=1});
            Check(rounds.AssignRooms(players.Keys,id=>players[id].chosenRoom)[host.id]==2,"duplicate Ready keeps a reserved seat");
            ChooseRoom(1,"");Broadcast();
            Check(!rounds.IsReady(host.id),"changing rooms releases the previous seat");
            OnCommand(peers[0].id,new Command{kind="ready",x=1});
            Check(rounds.IsReady(peers[0].id),"released seat is immediately available to another player");
            Check(SaloonStations.Name(1)=="Coin Flip"&&SaloonStations.Name(2)=="Blackjack","planner and assignment names describe their minigames");
            Debug.Log("SALOON_RESERVATIONS PASS");
        }
#endif
    }
}
