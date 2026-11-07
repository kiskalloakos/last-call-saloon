using UnityEngine;
namespace Saloon
{
    public static class SaloonStations
    {
        public static Vector3 Position(bool gather,int room,int slot)
        {
            if(gather){float a=slot*Mathf.PI*2/5;return new Vector3(Mathf.Sin(a)*1.25f,.08f,-.5f+Mathf.Cos(a)*1.25f);}
            if(room==0)return new Vector3(-3+slot*1.5f,.35f,2.05f);
            return new Vector3((room==1?-5:5)+(slot==0?-.75f:.75f),.08f,-3.8f);
        }
        public static float Facing(bool gather,int room,int slot)
        {
            var position=Position(gather,room,slot);
            var center=gather?new Vector3(0,.08f,-.5f):room==0?position+Vector3.forward:new Vector3(room==1?-5:5,.08f,-3.8f);
            return Quaternion.LookRotation(center-position).eulerAngles.y;
        }
        public static string Name(int room)=>room==1?"West room":room==2?"East room":"Bar";
    }
}
