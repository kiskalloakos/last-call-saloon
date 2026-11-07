using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Saloon
{
    public static class SaloonSnapshotVerification
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static void Run()
        {
            void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Snapshot stream: "+message);}
            var random=new System.Random(1234);var noise=new byte[6000];random.NextBytes(noise);
            var source=new Snapshot{phase=Phase.Choosing,round=2,ownRole="Deputy",notice=Convert.ToBase64String(noise),
                players=new[]{new PlayerView{id="1",name="Árvíztűrő 漢字",alive=true,companion="0"}}};
            var packed=SaloonSnapshotWire.Encode(source,out int legacy);
            int count=(packed.Length+SaloonSnapshotWire.ChunkBytes-1)/SaloonSnapshotWire.ChunkBytes;
            Check(count>2,"fixture exercises several datagrams");
            var receiver=new SaloonSnapshotWire.Receiver();
            Snapshot Part(uint seq,int part)
            {
                int offset=part*SaloonSnapshotWire.ChunkBytes,length=Math.Min(SaloonSnapshotWire.ChunkBytes,packed.Length-offset);
                return receiver.Accept(seq,part,count,packed.Length,packed.Skip(offset).Take(length).ToArray());
            }
            // Lose an early state, then deliver the following state in reverse packet order.
            for(int i=1;i<count;i++)Check(Part(100,i)==null,"incomplete state is not applied");
            Snapshot latest=null;
            for(int i=count-1;i>=0;i--)latest=Part(101,i)??latest;
            Check(latest!=null&&latest.phase==Phase.Choosing&&latest.players[0].name==source.players[0].name&&latest.notice==source.notice,"newer complete state recovers despite loss/reordering and preserves UTF-8");
            Check(latest.duel==null&&latest.roomTurn==null,"compressed state preserves absent panel flags");
            Check(Part(100,0)==null&&Part(101,0)==null,"late and duplicate updates cannot roll state backwards");
            Check(Part(102,0)==null&&Part(102,0)==null,"duplicate chunks do not complete partial state");
            for(int i=0;i<count;i++)latest=Part(103,i)??latest;
            Check(receiver.LastSequence==103,"later state overtakes an unfinished update");
            receiver.Reset();
            for(int i=0;i<count;i++)Part(uint.MaxValue,i);
            for(int i=0;i<count;i++)Part(0,i);
            Check(receiver.LastSequence==0,"sequence wrap remains monotonic");
            bool rejected=false;
            try{receiver.Accept(1,0,99,1,new byte[1]);}catch(InvalidDataException){rejected=true;}
            Check(rejected,"malformed metadata is rejected before allocation");
            // Independent zlib/raw-deflate fixture verifies the decoder's portable wire format.
            byte[] fixture=Convert.FromBase64String("q1YqyEmsTC0qVrKKjtVRKshILE5VsjLRUSrKL81LUbIy1FHKyy/JTAYKKhXkF5UkJuWkKtUCAA==");
            var json=SaloonSnapshotWire.Decode(fixture);
            Check(json.phase==Phase.Choosing&&json.round==1,"portable deflate fixture decodes");
            Debug.Log("SALOON_SNAPSHOT_STREAM PASS lost/reordered/duplicate/stale updates, UTF-8, flags, bounds and sequence wrap");
        }
#endif
    }
}
