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
                foreach(var p in players.Values){p.controller.enabled=false;p.hp=60;p.room=1;p.busyUntil=0;p.busyAction="";p.lastAction=-100;}
                witness.room=worker.room=0;witness.role=Role.Sheriff;worker.role=Role.Outlaw;
            }
            ResetHeist();Actions();
            witness.body.transform.position=SafeApproach+Vector3.back*1.1f;worker.body.transform.position=SafeApproach;
            witness.seat=worker.seat=-1;witness.yaw=180;witness.pitch=0;worker.busyAction="rob";worker.busyUntil=Time.time+100;
            yield return new WaitForSeconds(.9f);
            Check(witness.evidence.Count==0,"looking away gives no clue");
            witness.yaw=0;yield return new WaitForSeconds(.2f);witness.yaw=180;yield return null;witness.yaw=0;yield return new WaitForSeconds(.2f);
            Check(witness.evidence.Count==0,"brief separate glances do not accumulate into a witness clue");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=SafeApproach+new Vector3(0,1.2f,-.55f);wall.transform.localScale=new Vector3(2,3,.3f);
            yield return new WaitForSeconds(.9f);Check(witness.evidence.Count==0,"a wall blocks witnessing");Destroy(wall);yield return null;
            witness.hp=0;yield return new WaitForSeconds(.9f);Check(witness.evidence.Count==0,"dead players collect no clues");witness.hp=60;
            yield return new WaitForSeconds(.9f);
            Check(witness.evidence.Count==1&&witness.evidence[0].Contains(worker.name)&&witness.evidence[0].Contains("did not see what they did inside"),"watching real safe work records one named observation without claiming completion");
            yield return new WaitForSeconds(.9f);Check(witness.evidence.Count==1,"continued watching does not spam duplicate clues");
            Check(outsider.evidence.Count==0&&PrivateSnapshot(outsider.id).evidence.Length==0&&PrivateSnapshot(worker.id).evidence.Length==0,"other rooms and the worker receive no witness notebook");
            // A legitimate inspection must produce the same witness description.
            Actions();worker.role=Role.Deputy;worker.busyAction="inspect";worker.busyUntil=Time.time+100;
            witness.evidence.Clear();witness.safeWatch.Clear();
            yield return new WaitForSeconds(.9f);
            Check(witness.evidence.Count==1&&witness.evidence[0].Contains("did not see what they did inside"),"Law inspection is witnessed exactly like theft without certifying a role");
            worker.busyAction="";worker.busyUntil=0;witness.body.transform.position=SafeApproach+Vector3.left*.25f;Physics.SyncTransforms();InspectSafe(witness);
            yield return new WaitForSeconds(SaloonRules.DrawerInteractionSeconds+.1f);
            Check(witness.safeReport.Contains("money present")&&safeProgress==0,"an interrupted check leaves the money in the drawer");
            Actions();worker.role=Role.Outlaw;safeProgress=SaloonRules.DrawerWorkSteps-1;lastSafeWorkRound=0;CompleteRobbery(worker);InspectSafe(witness);
            yield return new WaitForSeconds(SaloonRules.DrawerInteractionSeconds+.1f);
            Check(witness.safeReport.Contains("money missing")&&witness.safeReport.Contains("changed since"),"inspection compares drawer changes with the previous check");
            Check(!rounds.CanAct(witness.id,Time.time),"inspection still costs the observer's action");
            ShowRole=true;Broadcast();SyncPointer();
            Check(NeedsPointer&&Cursor.lockState==CursorLockMode.None,"Tab makes room-action clues scrollable with a free cursor");
            ShowRole=false;
            string report=witness.safeReport;ReturnDrawerMoney(worker);worker.bag=false;
            Check(witness.safeReport==report,"later drawer changes never update an old report automatically");
            Actions();InspectSafe(witness);yield return new WaitForSeconds(SaloonRules.DrawerInteractionSeconds+.1f);
            Actions();InspectSafe(witness);yield return new WaitForSeconds(SaloonRules.DrawerInteractionSeconds+.1f);
            Check(witness.safeReport.Contains("No change since"),"another check without a state change reports no change");
            var wire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(witness.id),out _));
            Check(wire.evidence.SequenceEqual(witness.evidence),"private notebook survives the actual compressed snapshot wire");
            Check(PrivateSnapshot(outsider.id).safeReport==""&&PrivateSnapshot(outsider.id).evidence.Length==0,"inspection clues remain private too");
            for(int i=0;i<10;i++)NoteEvidence(witness,"Test clue "+i);
            Check(witness.evidence.Count==6&&witness.evidence[0]=="Test clue 4","notebook keeps only the six latest clues");
            ResetHeist();Check(players.Values.All(p=>p.evidence.Count==0&&p.safeWatch.Count==0)&&lastSafeWorkRound==0,"new match resets every clue and observation timer");
            NoteEvidence(witness,"Round 1: you saw "+worker.name+" checking the cash drawer. You did not see what they did inside.");
            NoteEvidence(witness,"Round 2 drawer check · money missing. The drawer changed since your last inspection.");
            rounds.Start(players.Keys,Time.time,30,30,6,1,1);phase=rounds.Phase;PositionEveryone(true);MenuOpen=false;Broadcast();
            float previewUntil=Time.unscaledTime+1;bool captured=false;
            while(Time.unscaledTime<previewUntil)
            {
                if(ShowRole&&!captured){ScreenCapture.CaptureScreenshot("/tmp/saloon-sheriff-tab.png");captured=true;}
                yield return null;
            }
            Debug.Log("SALOON_EVIDENCE PASS");VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
