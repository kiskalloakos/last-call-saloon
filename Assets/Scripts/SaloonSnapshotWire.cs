using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Saloon
{
    // State is replaceable: a missing update must never hold later updates behind it.
    // Each datagram stays below NGO's non-fragmented message limit, including headers.
    public static class SaloonSnapshotWire
    {
        public const int ChunkBytes=1000,HeaderBytes=12,MaxPackedBytes=16000,MaxJsonBytes=65536;
        public static byte[] Encode(Snapshot snapshot,out int legacyBytes)
        {
            string json=snapshot.ToWireJson();legacyBytes=Encoding.Unicode.GetByteCount(json);
            var bytes=Encoding.UTF8.GetBytes(json);
            if(bytes.Length>MaxJsonBytes)throw new InvalidDataException("Snapshot JSON exceeds limit.");
            using(var output=new MemoryStream())
            {
                using(var deflate=new DeflateStream(output,CompressionLevel.Fastest,true))deflate.Write(bytes,0,bytes.Length);
                var packed=output.ToArray();
                if(packed.Length>MaxPackedBytes)throw new InvalidDataException("Compressed snapshot exceeds limit.");
                return packed;
            }
        }
        public static Snapshot Decode(byte[] packed)
        {
            if(packed==null||packed.Length==0||packed.Length>MaxPackedBytes)throw new InvalidDataException("Invalid snapshot size.");
            using(var input=new MemoryStream(packed))
            using(var deflate=new DeflateStream(input,CompressionMode.Decompress))
            using(var output=new MemoryStream())
            {
                var buffer=new byte[4096];int read;
                while((read=deflate.Read(buffer,0,buffer.Length))>0)
                {
                    if(output.Length+read>MaxJsonBytes)throw new InvalidDataException("Expanded snapshot exceeds limit.");
                    output.Write(buffer,0,read);
                }
                return Snapshot.FromWireJson(new UTF8Encoding(false,true).GetString(output.ToArray()));
            }
        }
        static bool Newer(uint value,uint previous)=>unchecked((int)(value-previous))>0;

        public sealed class Receiver
        {
            sealed class Assembly
            {
                public byte[] data;
                public bool[] parts;
                public int received;
            }
            readonly Dictionary<uint,Assembly> pending=new Dictionary<uint,Assembly>();
            public uint LastSequence { get; private set; }
            bool applied;
            public void Reset(){pending.Clear();applied=false;LastSequence=0;}
            public Snapshot Accept(uint sequence,int part,int count,int total,byte[] payload)
            {
                if(applied&&!Newer(sequence,LastSequence))return null;
                if(total<=0||total>MaxPackedBytes||count!=(total+ChunkBytes-1)/ChunkBytes||part<0||part>=count||
                    payload==null||payload.Length!=Math.Min(ChunkBytes,total-part*ChunkBytes))
                    throw new InvalidDataException("Invalid snapshot chunk.");
                if(!pending.TryGetValue(sequence,out var assembly))
                {
                    if(pending.Count>=2)
                    {
                        uint oldest=sequence;
                        foreach(uint key in pending.Keys)if(Newer(oldest,key))oldest=key;
                        if(oldest==sequence)return null;
                        pending.Remove(oldest);
                    }
                    assembly=new Assembly{data=new byte[total],parts=new bool[count]};pending.Add(sequence,assembly);
                }
                if(assembly.data.Length!=total||assembly.parts.Length!=count)throw new InvalidDataException("Snapshot chunk metadata changed.");
                if(assembly.parts[part])return null;
                Buffer.BlockCopy(payload,0,assembly.data,part*ChunkBytes,payload.Length);assembly.parts[part]=true;assembly.received++;
                if(assembly.received!=count)return null;
                var snapshot=Decode(assembly.data);
                if(snapshot==null||snapshot.players==null)throw new InvalidDataException("Snapshot has no players.");
                LastSequence=sequence;applied=true;
                var expired=new List<uint>();
                foreach(uint key in pending.Keys)if(!Newer(key,sequence))expired.Add(key);
                foreach(uint key in expired)pending.Remove(key);
                return snapshot;
            }
        }
    }
}
