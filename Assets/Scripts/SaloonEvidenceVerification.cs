#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyEvidence()
        {
            verifying=true;AutomatedInput=true;VerificationPassed=VerificationFinished=false;
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;Debug.LogError("SALOON_EVIDENCE FAIL "+text);throw new InvalidOperationException(text);}Debug.Log("SALOON_EVIDENCE_CHECK "+text);}
            var witness=players[Connection.LocalId];var worker=players.Values.First(p=>p.bot);var outsider=players.Values.First(p=>p!=worker&&p!=witness);
            void Actions()
            {
                rounds.Start(players.Keys,Time.time,30,30,6,1,1);foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;roomTurns.Clear();
                foreach(var p in players.Values){p.controller.enabled=false;p.hp=3;p.room=1;p.busyUntil=0;p.busyAction="";p.lastAction=-100;}
                witness.room=worker.room=0;witness.role=Role.Sheriff;worker.role=Role.Outlaw;
            }
            ResetHeist();Actions();
            witness.body.transform.position=new Vector3(0,10,0);worker.body.transform.position=new Vector3(0,10,3);
            witness.seat=worker.seat=-1;witness.yaw=180;witness.pitch=0;worker.busyAction="rob";worker.busyUntil=Time.time+100;
            yield return new WaitForSeconds(.9f);
            Check(witness.evidence.Count==0,"looking away gives no clue");
            witness.yaw=0;yield return new WaitForSeconds(.2f);witness.yaw=180;yield return null;witness.yaw=0;yield return new WaitForSeconds(.2f);
            Check(witness.evidence.Count==0,"brief separate glances do not accumulate into a witness clue");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,11.65f,1.5f);wall.transform.localScale=new Vector3(2,3,.3f);
            yield return new WaitForSeconds(.9f);Check(witness.evidence.Count==0,"a wall blocks witnessing");Destroy(wall);yield return null;
            witness.hp=0;yield return new WaitForSeconds(.9f);Check(witness.evidence.Count==0,"dead players collect no clues");witness.hp=3;
            yield return new WaitForSeconds(.9f);
            Check(witness.evidence.Count==1&&witness.evidence[0].Contains(worker.name)&&witness.evidence[0].Contains("did not see whether"),"watching real safe work records one named observation without claiming completion");
            yield return new WaitForSeconds(.9f);Check(witness.evidence.Count==1,"continued watching does not spam duplicate clues");
            Check(outsider.evidence.Count==0&&PrivateSnapshot(outsider.id).evidence.Length==0&&PrivateSnapshot(worker.id).evidence.Length==0,"other rooms and the worker receive no witness notebook");
            worker.busyAction="";worker.busyUntil=0;InspectSafe(witness);
            Check(witness.safeReport.Contains("No tool marks")&&safeProgress==0,"interrupted work leaves no completed-work inspection marks");
            Actions();CompleteRobbery(worker);InspectSafe(witness);
            Check(witness.safeReport.Contains("New tool marks")&&witness.safeReport.Contains("round 1"),"inspection compares completed safe work with the previous check");
            Check(!rounds.CanAct(witness.id,Time.time),"inspection still costs the Sheriff's action");
            string report=witness.safeReport;CompleteRobbery(worker);
            Check(witness.safeReport==report,"later safe work never updates an old report automatically");
            Actions();InspectSafe(witness);Actions();InspectSafe(witness);
            Check(witness.safeReport.Contains("No new tool marks"),"another check without progress reports no new marks");
            var wire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(witness.id),out _));
            Check(wire.evidence.SequenceEqual(witness.evidence),"private notebook survives the actual compressed snapshot wire");
            Check(PrivateSnapshot(outsider.id).safeReport==""&&PrivateSnapshot(outsider.id).evidence.Length==0,"inspection clues remain private too");
            for(int i=0;i<10;i++)NoteEvidence(witness,"Test clue "+i);
            Check(witness.evidence.Count==6&&witness.evidence[0]=="Test clue 4","notebook keeps only the six latest clues");
            ResetHeist();Check(players.Values.All(p=>p.evidence.Count==0&&p.safeWatch.Count==0)&&lastSafeWorkRound==0,"new match resets every clue and observation timer");
            NoteEvidence(witness,"Round 1: you saw "+worker.name+" working the safe. You did not see whether they finished.");
            NoteEvidence(witness,"Round 2 check · tampered with · money inside. New tool marks since your last check (round 1).");
            rounds.Start(players.Keys,Time.time,30,30,6,1,1);phase=rounds.Phase;PositionEveryone(true);MenuOpen=false;Broadcast();
            yield return new WaitForSeconds(15);
            Debug.Log("SALOON_EVIDENCE PASS");VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
