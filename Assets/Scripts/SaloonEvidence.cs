using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        int lastSafeWorkRound;

        void NoteEvidence(Participant witness,string clue)
        {
            if(witness.evidence.Contains(clue))return;
            witness.evidence.Add(clue);
            if(witness.evidence.Count>6)witness.evidence.RemoveAt(0);
        }

        bool CanWitnessSafeWork(Participant witness,Participant worker)
        {
            if(witness==worker||!witness.Alive||!worker.Alive||witness.room!=0||worker.room!=0)return false;
            var direction=Eye(worker)-Eye(witness);
            if(direction.sqrMagnitude>144||direction.sqrMagnitude<.001f||Vector3.Dot(Forward(witness),direction.normalized)<.8f)return false;
            var hits=Physics.RaycastAll(Eye(witness),direction.normalized,direction.magnitude,
                ~((1<<SaloonRagdoll.Layer)|(1<<SaloonMovement.SolidLayer)),QueryTriggerInteraction.Ignore);
            Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                var body=hit.collider.GetComponentInParent<SaloonBody>();
                if(body!=null&&(body.id==witness.id||(players.TryGetValue(body.id,out var p)&&(!p.Alive||hit.collider==p.controller))))continue;
                return body!=null&&body.id==worker.id;
            }
            return true;
        }

        void TickEvidence()
        {
            if(phase!=Phase.Turns){foreach(var p in players.Values)p.safeWatch.Clear();return;}
            var workers=players.Values.Where(p=>p.busyAction=="rob"&&p.busyUntil>Time.time&&SaloonRules.CanRob(p.role)).ToArray();
            if(workers.Length>0)Physics.SyncTransforms();
            foreach(var witness in players.Values)
            {
                foreach(var id in witness.safeWatch.Keys.Where(id=>!workers.Any(p=>p.id==id)).ToArray())witness.safeWatch.Remove(id);
                foreach(var worker in workers)
                {
                    if(!CanWitnessSafeWork(witness,worker)){witness.safeWatch.Remove(worker.id);continue;}
                    witness.safeWatch.TryGetValue(worker.id,out float watched);
                    watched+=Mathf.Min(Time.deltaTime,.1f);witness.safeWatch[worker.id]=watched;
                    if(watched>=.75f)NoteEvidence(witness,"Round "+rounds.Round+": you saw "+worker.name+" working the safe. You did not see whether they finished.");
                }
            }
        }
    }
}
