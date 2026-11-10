using System;
using System.Linq;

namespace Saloon
{
    public static class SaloonRoomReservationVerification
    {
        public static void Run()
        {
            void Check(bool ok,string text){if(!ok)throw new InvalidOperationException("Room priority: "+text);}
            var r=new SaloonRounds();var ids=new ulong[]{1,2,3,4};
            r.Start(ids,0,30,30,2);
            r.SetReady(3,true);r.SetReady(2,true);r.SetReady(1,true);r.SetReady(4,true);
            foreach(int room in new[]{1,2,3})
            {
                var assigned=r.AssignRooms(ids,id=>room);
                Check(assigned[3]==room&&assigned[2]==room&&assigned[1]==0&&assigned[4]==0,"first two ready win regardless of player ID");
                var corpse=r.AssignRooms(ids,id=>room,new[]{5,1,1,1});
                Check(corpse[3]==room&&corpse.Count(p=>p.Value==room)==1,"corpse reduces room capacity");
            }
            r.SetReady(3,true);Check(r.AssignRooms(ids,id=>2)[3]==2,"duplicate Ready keeps priority");
            r.SetReady(3,false);r.SetReady(3,true);
            var rejoined=r.AssignRooms(ids,id=>2);
            Check(rejoined[2]==2&&rejoined[1]==2&&rejoined[3]==0,"unready releases priority and re-ready joins at end");
            r.Start(ids,0,30,30,2);r.SetReady(4,true);r.SetReady(1,true);r.SetReady(2,true);r.SetReady(3,true);
            Check(r.AssignRooms(ids,id=>2)[4]==2,"new match clears all old priorities");
            r.Tick(1,id=>true);foreach(var id in ids)r.TrySpend(id,1);r.Tick(1,id=>true);r.Tick(4,id=>true);r.Tick(7,id=>true);
            r.SetReady(3,true);r.SetReady(2,true);r.SetReady(1,true);r.SetReady(4,true);
            Check(r.AssignRooms(ids,id=>2)[4]==0,"new discussion resets reservation order");
        }
    }
}
