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
            var existing=GameObject.Find("have a smoke · sunset deck");
            if(existing!=null){SaloonSurfaces.Apply(existing.transform);return;}
            var placeholder=GameObject.Find("have a smoke · placeholder yard");
            if(placeholder!=null){placeholder.SetActive(false);Object.Destroy(placeholder);}
            var root=new GameObject("have a smoke · sunset deck").transform;root.position=Center-Vector3.up*.08f;
            var wood=SaloonWorld.Material("Sunset deck cedar",new Color(.38f,.24f,.15f));
            var dark=SaloonWorld.Material("Weathered deck posts",new Color(.21f,.14f,.11f));
            // Retain the original walkable footprint and room clamps.
            Box("Solid outdoor deck",root,new Vector3(0,-.04f,0),new Vector3(8,.16f,8),dark);
            for(int i=0;i<22;i++)Box("Weathered cedar plank",root,new Vector3(0,.047f,-3.82f+i*.36f),new Vector3(7.95f,.025f,.345f),wood,false);
            Box("Saloon side privacy wall",root,new Vector3(-3.98f,1.7f,0),new Vector3(.18f,3.4f,8),wood);
            foreach(float z in new[]{-3.86f,3.86f})
            {
                foreach(float y in new[]{.40f,.78f})Box("Open overlook rail",root,new Vector3(0,y,z),new Vector3(8,.11f,.11f),wood);
                foreach(float x in new[]{-3.85f,0,3.85f})Box("Overlook post",root,new Vector3(x,.52f,z),new Vector3(.15f,1.04f,.15f),dark);
            }
            foreach(float y in new[]{.40f,.78f})Box("Sunset facing rail",root,new Vector3(3.86f,y,0),new Vector3(.11f,.11f,7.8f),wood);
            Box("Sunset rail center post",root,new Vector3(3.86f,.52f,0),new Vector3(.15f,1.04f,.15f),dark);
            Box("Quiet cedar bench",root,new Vector3(-2.9f,.46f,0),new Vector3(.52f,.12f,2.8f),wood);
            foreach(float z in new[]{-1.05f,1.05f})Box("Bench support",root,new Vector3(-2.9f,.2f,z),new Vector3(.4f,.4f,.15f),dark);
            var sign=new GameObject("have a smoke sign");sign.transform.SetParent(root,false);sign.transform.localPosition=new Vector3(-3.86f,2.4f,0);sign.transform.localRotation=Quaternion.Euler(0,-90,0);
            var text=sign.AddComponent<TextMesh>();text.text="Have a Smoke\nStay a while. Watch the sun go down.";text.characterSize=.011f;text.fontSize=64;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.94f,.84f,.65f);SaloonWorldText.Attach(text);
            if(world!=null)foreach(var light in world.GetComponentsInChildren<Light>())light.cullingMask&=~(1<<OutdoorLayer);
            SaloonSunset.Build(root);SaloonSurfaces.Apply(root);SetLayer(root,OutdoorLayer);
        }
        static void Box(string name,Transform root,Vector3 position,Vector3 size,Material material,bool solid=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid){var collider=go.GetComponent<Collider>();collider.enabled=false;if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);}
        }
    }
}
