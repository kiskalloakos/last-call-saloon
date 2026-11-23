#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        IEnumerator VerifyToiletAudio()
        {
            int checks=0;
            void Check(bool ok,string message)
            {checks++;if(!ok){VerificationFinished=true;throw new InvalidOperationException("Toilet audio: "+message);}Debug.Log("SALOON_TOILET_AUDIO_CHECK "+message);}
            foreach(string name in new[]{"Plunger","Gurgle","Flush"})
            {
                var clip=Resources.Load<AudioClip>("Audio/Toilet/"+name);Check(clip!=null&&clip.channels==1&&clip.length>.3f,"recorded mono "+name+" loads");
                var samples=new float[clip.samples];clip.GetData(samples,0);float peak=samples.Max(v=>Mathf.Abs(v));Check(peak>.03f&&peak<.95f,name+" contains audible unclipped samples");
            }
            Check(Resources.Load<TextAsset>("Audio/Toilet/License").text.Contains("CC0 1.0"),"source credits and recording licenses are included");
            var original=PrivateSnapshot(Connection.LocalId);var probe=new GameObject("Solo mission audio verification");probe.transform.position=SaloonToilet.Position;
            var audio=probe.AddComponent<SaloonToiletAudio>();audio.Initialize(this);var sources=probe.GetComponents<AudioSource>();
            var pump=sources.Single(s=>s.clip.name=="Plunger");var bubble=sources.Single(s=>s.clip.name=="Gurgle");var finish=sources.Single(s=>s.clip.name=="Flush");
            void State(int count,int room=SaloonBathroom.Room,float age=0,bool working=true,bool complete=false,Phase phase=Phase.Turns)
            {
                var snapshot=Snapshot.FromWireJson(original.ToWireJson());snapshot.round=900;snapshot.phase=phase;
                var self=snapshot.players.First(p=>p.id==LocalId);self.room=room;self.observer=false;self.alive=true;snapshot.players=new[]{self};
                if(room==SaloonBathroom.Room){self.x=SaloonToilet.Approach.x;self.y=.08f;self.z=SaloonToilet.Approach.z;}
                snapshot.toiletPumps=count;snapshot.toiletPumpAge=age;snapshot.toiletInUse=working;snapshot.toiletCompleted=complete;snapshot.toiletDuty=room==SaloonBathroom.Room;snapshot.toiletRemaining=12;
                View=snapshot;audio.Apply(snapshot);
            }
            Connection.SnapshotReceived-=ApplySnapshot;
            try
            {
                Check(sources.All(s=>!s.playOnAwake&&!s.loop&&!s.isPlaying&&s.spatialBlend==1),"mission audio starts silent with positional sources");
                State(3,age:.1f);Check(sources.All(s=>!s.isPlaying),"late joining a mission never replays old strokes");
                State(4);yield return new WaitForSeconds(.08f);Check(pump.isPlaying&&bubble.isPlaying&&!finish.isPlaying,"fresh accepted stroke plays suction and gurgle inside the solo room");
                float before=pump.time;State(4,age:.08f);yield return new WaitForSeconds(.04f);Check(pump.time>before,"duplicate snapshots do not restart stroke audio");
                State(5,age:2);Check(pump.time>before,"stale history is consumed without replay");
                State(6);State(6,working:false);Check(sources.All(s=>!s.isPlaying),"interrupted work stops pumping without flushing");
                State(7);State(8,working:false);yield return new WaitForSeconds(.18f);Check(finish.isPlaying,"completion starts a flush inside the private mission");
                float time=finish.time;State(8,room:0,working:false,complete:true);yield return new WaitForSeconds(.05f);
                Check(finish.isPlaying&&!finish.mute&&finish.spatialBlend==0&&finish.time>time,"only the worker's completion flush follows their immediate return to Bar");
                time=finish.time;State(8,room:0,age:.25f,working:false,complete:true);yield return new WaitForSeconds(.04f);Check(finish.time>time,"repeated completion snapshots never replay the flush");
                foreach(int room in new[]{0,1,2,3})
                {
                    State(0,room:room,working:false);State(1,room:room);yield return null;
                    Check(sources.All(s=>s.mute&&!s.isPlaying),"non-worker listener in room "+room+" cannot hear solo mission audio");
                }
                State(0);State(1);State(0,phase:Phase.Regroup,working:false);
                Check(sources.All(s=>!s.isPlaying),"regroup stops remaining solo Foley tails");
            }
            finally{Connection.SnapshotReceived+=ApplySnapshot;View=original;Destroy(probe);}
            Debug.Log("SALOON_TOILET_AUDIO PASS "+checks+" checks");
        }
    }
}
#endif
