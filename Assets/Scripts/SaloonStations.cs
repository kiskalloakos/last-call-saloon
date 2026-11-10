using UnityEngine;
namespace Saloon
{
    public static class SaloonStations
    {
        public static Vector3 Position(bool gather,int room,int slot)
        {
            if(gather){float a=slot*Mathf.PI*2/5;return new Vector3(Mathf.Sin(a)*1.25f,.08f,-.5f+Mathf.Cos(a)*1.25f);}
            if(room==3)return SaloonSmokingArea.Center+new Vector3(slot==0?-.9f:.9f,0,0);
            if(room==0)return new Vector3(-5+slot*1.5f,.35f,2.05f);
            return new Vector3((room==1?-5:5)+(slot==0?-1.04f:1.04f),.08f,-3.8f);
        }
        public static float Facing(bool gather,int room,int slot)
        {
            var position=Position(gather,room,slot);
            var center=room==3&&!gather?SaloonSmokingArea.Center:gather?new Vector3(0,.08f,-.5f):room==0?position+Vector3.forward:new Vector3(room==1?-5:5,.08f,-3.8f);
            return Quaternion.LookRotation(center-position).eulerAngles.y;
        }
        public static string Name(int room)=>room==1?"Coin Flip":room==2?"Blackjack":room==3?"have a smoke":"Bar";
    }
}
