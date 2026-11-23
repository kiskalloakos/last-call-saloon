#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyMedicalSupplies()
        {
            verifying=AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            int checks=0;
            void Check(bool ok,string message)
            {checks++;if(!ok){VerificationFinished=true;throw new InvalidOperationException("Medical supplies: "+message);}Debug.Log("SALOON_MEDICAL_CHECK "+message);}
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p!=host);
            void Command(Participant p,string kind){p.lastAction=-100;OnCommand(p.id,new Command{kind=kind});}
            void Window(int room=1,Role role=Role.Sheriff)
            {
                ResetHeist();pendingOutcome=outcome="";
                foreach(var p in players.Values)
                {p.hp=100;p.deathAt=-1;p.role=Role.Outlaw;p.chosenRoom=0;p.companion=null;p.busyAction="";p.busyUntil=0;p.lastAction=-100;p.drawn=false;}
                host.role=role;host.hp=60;host.chosenRoom=peer.chosenRoom=room;host.companion=peer.id;peer.companion=host.id;
                rounds.Start(players.Keys,Time.time,60,60,0);
                rounds.BeginAftermath(Time.time-10);rounds.Tick(Time.time,id=>true);rounds.Tick(Time.time+4,id=>true);
                foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
                medicalRoom=room;medicalRound=rounds.Round;
            }
            void Reveal(int room)
            {
                var d=duels[room];
                if(d.bluff!=null){d.bluff.Forfeit(d.a==host.id?1:0);RevealBluffDice(d);}
                else if(room==1){ClaimCoin(host,0);d.revealAt=Time.time-1;TickDuels();d.first=host.id;}
                else RevealBlackjack(d,host.id,"Verification hand won");
                d.combatAt=Time.time-.01f;d.firstUntil=Time.time+10;
            }
            Connection.SnapshotReceived-=ApplySnapshot;
            try
            {
                phase=Phase.Finished;StartMatch();int featured=medicalRoom;
                Check(featured==0&&medicalRound==1,"round one never has a medical event");
                foreach(float roll in new[]{0f,.499f,.5f,.749f,.75f,1f})
                    Check(MedicalEventForRound(1,roll)==0,"every first-round roll stays quiet: "+roll);
                Check(MedicalEventForRound(2,0)==0&&MedicalEventForRound(2,.499f)==0,"later rounds can have no event");
                Check(MedicalEventForRound(2,.5f)==1&&MedicalEventForRound(2,.749f)==1&&MedicalEventForRound(2,.75f)==2&&MedicalEventForRound(2,1)==2,"later event rolls can select either room");
                for(int i=0;i<10;i++){UpdateMedicalOpportunity();PrivateSnapshot(host.id);}
                Check(medicalRoom==featured,"snapshots and repeated updates cannot reroll an announced opportunity");
                var publicView=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(peer.id),out _));
                Check(publicView.medicalRoom==featured&&!publicView.medicalChoice,"planning recipients learn the same room without a reward action");
                rounds.BeginAftermath(Time.time-10);rounds.Tick(Time.time,id=>true);rounds.Tick(Time.time+4,id=>true);phase=rounds.Phase;UpdateMedicalOpportunity();
                Check(medicalRound==2&&medicalRoom>=0&&medicalRoom<=2,"next discussion rolls once for no event or one medical room");
                foreach(int room in new[]{1,2})
                foreach(var role in new[]{Role.Sheriff,Role.Deputy,Role.Outlaw,Role.Renegade,Role.Swindler})
                {
                    Window(room,role);int stock=beers;host.cigaretteBuff=true;
                    Command(host,"medicalSupplies");Check(host.hp==60&&!rounds.HasSpent(host.id),"unresolved game cannot grant supplies in room "+room+" / "+role);
                    Reveal(room);var loser=PrivateSnapshot(peer.id);
                    Check(PrivateSnapshot(host.id).medicalChoice&&!loser.medicalChoice&&!loser.medicalCanHeal,"only the winner receives the choice");
                    Command(peer,"medicalSupplies");Check(peer.hp==100&&!rounds.HasSpent(peer.id),"loser cannot claim the winner's supplies");
                    Command(host,"medicalSupplies");
                    Check(host.hp==80&&rounds.HasSpent(host.id)&&DuelAllows(peer)&&!rounds.HasSpent(peer.id),"healing costs exactly one action and releases the opponent for "+role+" / "+room);
                    Check(beers==stock&&host.cigaretteBuff,"medical healing uses no beer and neither consumes nor doubles the smoking boost");
                    Command(host,"medicalSupplies");Check(host.hp==80&&!PrivateSnapshot(host.id).medicalChoice,"duplicate healing cannot grant a second reward");
                    var own=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(host.id),out _));
                    Check(own.health==80&&!own.actionAvailable&&!own.medicalChoice,"healed health and spent action survive the compressed wire");
                }
                Window();Reveal(1);host.hp=95;Command(host,"medicalSupplies");Check(host.hp==100,"supplies cap health at 100 percent");
                foreach(int room in new[]{1,2})
                foreach(var role in new[]{Role.Sheriff,Role.Deputy,Role.Outlaw,Role.Renegade,Role.Swindler})
                {
                    Window(room,role);Reveal(room);host.hp=100;float openingUntil=duels[room].firstUntil;TickDuels();
                    var own=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(host.id),out _));
                    Check(duels[room].medicalDeclined&&!own.medicalChoice&&!own.medicalCanHeal,"full health automatically keeps the opening without a prompt for "+role+" / "+room);
                    Check(host.hp==100&&!rounds.HasSpent(host.id)&&DuelAllows(host)&&!DuelAllows(peer)&&duels[room].firstUntil==openingUntil,"automatic keep preserves health, the action, opening priority and timer");
                    host.hp=60;Command(host,"medicalSupplies");Check(host.hp==60&&!rounds.HasSpent(host.id),"automatically declined supplies cannot be reclaimed after taking damage");
                }
                Window();Reveal(1);host.hp=100;Command(host,"medicalSupplies");Check(duels[1].medicalDeclined&&!rounds.HasSpent(host.id),"a full-health healing request also keeps the opening for a stale client");
                Window();Reveal(1);
                Command(host,"keepOpening");Check(!MedicalChoice(host)&&!rounds.HasSpent(host.id)&&DuelAllows(host),"keeping the opening is free and declines supplies");
                host.hp=60;Command(host,"medicalSupplies");Check(host.hp==60&&!rounds.HasSpent(host.id),"declined supplies cannot be reclaimed after choosing the opening");
                Command(host,"pass");Check(rounds.HasSpent(host.id)&&DuelAllows(peer),"normal actions remain available after keeping the opening");
                Window();Reveal(1);medicalRoom=0;Command(host,"medicalSupplies");Check(host.hp==60&&!rounds.HasSpent(host.id)&&!PrivateSnapshot(host.id).medicalChoice,"a no-event round offers no supplies or reward choice");
                Window();Reveal(1);medicalRoom=2;Command(host,"medicalSupplies");Check(host.hp==60&&!rounds.HasSpent(host.id),"a winner in the other room cannot use featured supplies");
                Window();Reveal(1);duels[1].firstUntil=Time.time-1;Command(host,"medicalSupplies");TickDuels();Check(host.hp==60&&rounds.HasSpent(host.id)&&DuelAllows(peer),"expired choice passes normally without granting healing");
                Window();Reveal(1);host.hp=0;Command(host,"medicalSupplies");Check(host.hp==0&&!rounds.HasSpent(host.id),"medical supplies cannot revive an eliminated winner");
                ResetHeist();Check(medicalRoom==0&&medicalRound==0,"new match or lobby clears the old featured opportunity");
                Window(2);Reveal(2);
            }
            finally{Connection.SnapshotReceived+=ApplySnapshot;}
            host.name="Preview player";helloSent=true;
            roleIntroStarted=float.NegativeInfinity;MenuOpen=ShowRole=RoomPlannerOpen=false;Broadcast();yield return null;
            Check(View.medicalChoice&&NeedsPointer,"winner choice is clickable after the minigame");
            string folder=Path.Combine(Path.GetTempPath(),"lastcall-medical-supplies");Directory.CreateDirectory(folder);
            if(!Application.isBatchMode)yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"WinnerChoice.png"));yield return null;
            phase=Phase.Finished;StartMatch();host.name="Preview player";roleIntroStarted=float.NegativeInfinity;MenuOpen=false;RoomPlannerOpen=true;Broadcast();yield return null;
            if(!Application.isBatchMode)yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"RoundOnePlanning.png"));yield return null;
            rounds.BeginAftermath(Time.time-10);rounds.Tick(Time.time,id=>true);rounds.Tick(Time.time+4,id=>true);phase=rounds.Phase;
            medicalRound=rounds.Round;medicalRoom=2;RoomPlannerOpen=true;Broadcast();yield return null;
            if(!Application.isBatchMode)yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Planning.png"));yield return null;
            medicalRoom=0;Broadcast();yield return null;
            if(!Application.isBatchMode)yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"NoEventPlanning.png"));yield return null;
            Debug.Log("SALOON_MEDICAL PASS "+checks+" checks; previews="+folder);VerificationFinished=VerificationPassed=true;
        }
    }
}
#endif
