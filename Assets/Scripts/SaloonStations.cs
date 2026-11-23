using UnityEngine;
namespace Saloon
{
    public static class SaloonStations
    {
        // Seat interaction IDs must not overlap the eight bar stools or side-room chairs (12–15).
        public const int GatherSeatStart=20;
        public static float BarX(int slot)=>-5+slot;
        public static Vector3 Position(bool gather,int room,int slot)
        {
            if(gather){float a=slot*Mathf.PI*2/SaloonRules.LobbyCapacity;return new Vector3(Mathf.Sin(a)*1.25f,.08f,-.5f+Mathf.Cos(a)*1.25f);}
            if(room==SaloonBathroom.Room)return SaloonToilet.Approach;
            if(room==3)return SaloonSmokingArea.Center+new Vector3(0,0,slot==0?-.9f:.9f);
            if(room==0)return new Vector3(BarX(slot),.35f,2.05f);
            return new Vector3((room==1?-5:5)+(slot==0?-1.04f:1.04f),.08f,-3.8f);
        }
        public static float Facing(bool gather,int room,int slot)
        {
            if(!gather&&room==3)return 90; // Both smokers arrive facing the sunset.
            var position=Position(gather,room,slot);
            if(!gather&&room==0)
            {
                // Look between the two painted room walls when the Bar is chosen.
                var left=Vector3.ProjectOnPlane(new Vector3(-5,0,SaloonRoomWindows.FrontZ)-position,Vector3.up).normalized;
                var right=Vector3.ProjectOnPlane(new Vector3(5,0,SaloonRoomWindows.FrontZ)-position,Vector3.up).normalized;
                return Quaternion.LookRotation(left+right).eulerAngles.y;
            }
            var center=room==3&&!gather?SaloonSmokingArea.Center:gather?new Vector3(0,.08f,-.5f):new Vector3(room==1?-5:5,.08f,-3.8f);
            return Quaternion.LookRotation(center-position).eulerAngles.y;
        }
        public static string Name(int room)=>room==SaloonBathroom.Room?"Toilet duty":room==1?"Coin Flip":room==2?"Blackjack":room==3?"Have a Smoke":"Bar";
    }
}
