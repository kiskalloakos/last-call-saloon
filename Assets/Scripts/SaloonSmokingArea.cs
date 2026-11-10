using UnityEngine;

namespace Saloon
{
    public static class SaloonSmokingArea
    {
        public const int IndoorLayer=12,OutdoorLayer=13;
        public static readonly Vector3 Center=new Vector3(28,.08f,-12);
        public static Vector3 Clamp(Vector3 point)
        {point.x=Mathf.Clamp(point.x,25.1f,30.9f);point.z=Mathf.Clamp(point.z,-14.8f,-9.2f);return point;}
        public static bool Contains(Vector3 point)=>point.x>24&&point.x<32&&point.z>-16&&point.z<-8;
        public static void SetLayer(Transform root,int layer)
        {foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))renderer.gameObject.layer=layer;}
        public static void ApplyCamera(Camera camera,bool outdoors)
        {
            int mask=camera.cullingMask&~((1<<IndoorLayer)|(1<<OutdoorLayer));
            camera.cullingMask=mask|(1<<(outdoors?OutdoorLayer:IndoorLayer));
        }
        public static void Apply()
        {
            var world=GameObject.Find("Saloon World");if(world!=null)SetLayer(world.transform,IndoorLayer);
            if(GameObject.Find("have a smoke · placeholder yard")!=null)return;
            var root=new GameObject("have a smoke · placeholder yard").transform;root.position=Center-Vector3.up*.08f;
            var wood=SaloonWorld.Material("Smoking yard placeholder wood",new Color(.30f,.22f,.15f));
            var sand=SaloonWorld.Material("Smoking yard placeholder ground",new Color(.49f,.37f,.24f));
            var dark=SaloonWorld.Material("Smoking yard placeholder trim",new Color(.18f,.14f,.11f));
            Box("Placeholder desert",root,new Vector3(0,-.20f,0),new Vector3(50,.15f,50),sand);
            Box("Placeholder outdoor deck",root,new Vector3(0,-.04f,0),new Vector3(8,.16f,8),sand);
            Box("Privacy fence · saloon side",root,new Vector3(-3.7f,1.7f,0),new Vector3(.18f,3.4f,8),wood);
            foreach(float z in new[]{-3.7f,3.7f})Box("Privacy fence",root,new Vector3(0,1.7f,z),new Vector3(7.6f,3.4f,.18f),wood);
            for(int i=0;i<12;i++)
            {
                float p=-3.65f+i*.66f;
                Box("Fence post",root,new Vector3(-3.58f,1.7f,p),new Vector3(.10f,3.5f,.08f),dark);
                foreach(float z in new[]{-3.58f,3.58f})Box("Fence post",root,new Vector3(p,1.7f,z),new Vector3(.08f,3.5f,.10f),dark);
            }
            Box("Low outdoor boundary",root,new Vector3(3.7f,.35f,0),new Vector3(.14f,.7f,8),wood);
            Box("Placeholder bench",root,new Vector3(0,.46f,-2.8f),new Vector3(2.8f,.12f,.5f),wood);
            foreach(float x in new[]{-1.05f,1.05f})Box("Bench support",root,new Vector3(x,.2f,-2.8f),new Vector3(.15f,.4f,.4f),dark);
            var sign=new GameObject("have a smoke sign");sign.transform.SetParent(root,false);sign.transform.localPosition=new Vector3(-3.46f,2.5f,0);sign.transform.localRotation=Quaternion.Euler(0,-90,0);
            var text=sign.AddComponent<TextMesh>();text.text="have a smoke\nTalk. Take a drag. Relax.";text.characterSize=.013f;text.fontSize=64;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.94f,.84f,.65f);SaloonWorldText.Attach(text);
            SetLayer(root,OutdoorLayer);
        }
        static void Box(string name,Transform root,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}
