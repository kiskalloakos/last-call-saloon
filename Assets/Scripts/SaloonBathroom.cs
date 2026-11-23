using System.Linq;
using UnityEngine;

namespace Saloon
{
    public static class SaloonBathroom
    {
        public const int Room=4,Layer=17;
        public const string RootName="Isolated solo bathroom";
        public static readonly Vector3 Center=new Vector3(0,0,40);
        public static Vector3 Clamp(Vector3 p)=>new Vector3(Mathf.Clamp(p.x,-1.35f,1.35f),p.y,Mathf.Clamp(p.z,38.45f,41.45f));
        public static bool Contains(Vector3 p)=>p.x>-2&&p.x<2&&p.z>37.8f&&p.z<42.2f;
        public static void ApplyCamera(Camera camera,bool inside)
        {
            int mask=camera.cullingMask&~(1<<Layer);
            if(inside)mask&=~((1<<SaloonSmokingArea.IndoorLayer)|(1<<SaloonSmokingArea.OutdoorLayer)|(1<<SaloonSunset.EntranceLayer));
            camera.cullingMask=inside?mask|(1<<Layer):mask;
        }
        public static void Apply(GameObject world)
        {
            if(GameObject.Find(RootName)!=null)return;
            var root=new GameObject(RootName).transform;root.position=Center;
            var timber=world.transform.Find("Western interior").GetComponentsInChildren<Renderer>().First(r=>r.sharedMaterial.name.Contains("Walnut joinery")).sharedMaterial;
            var plaster=world.transform.Find("Room back").GetComponent<Renderer>().sharedMaterial;
            Box("Bathroom floor",new Vector3(0,0,0),new Vector3(3.6f,.12f,4.2f),timber);
            Box("Bathroom ceiling",new Vector3(0,3.1f,0),new Vector3(3.6f,.18f,4.2f),timber);
            foreach(float x in new[]{-1.8f,1.8f})
            {
                Box("Bathroom side wall",new Vector3(x,1.55f,0),new Vector3(.18f,3.1f,4.2f),plaster);
                Box("Bathroom walnut dado",new Vector3(x+(x<0?.11f:-.11f),.55f,0),new Vector3(.055f,1,4.1f),timber);
            }
            foreach(float z in new[]{-2.1f,2.1f})Box("Bathroom end wall",new Vector3(0,1.55f,z),new Vector3(3.6f,3.1f,.18f),plaster);
            Box("Closed bathroom door",new Vector3(0,1.2f,-1.98f),new Vector3(1.15f,2.4f,.08f),timber);
            var porcelain=SaloonWorld.Material("Bathroom basin porcelain",new Color(.84f,.82f,.69f),.55f);
            Box("Bathroom basin",new Vector3(-1.42f,.88f,.75f),new Vector3(.42f,.18f,.62f),porcelain);
            Box("Bathroom basin pedestal",new Vector3(-1.5f,.42f,.75f),new Vector3(.2f,.84f,.18f),porcelain);
            var lamp=Box("Bathroom lantern",new Vector3(0,2.73f,-.15f),new Vector3(.2f,.3f,.2f),SaloonWorld.Material("Bathroom amber glass",new Color(.94f,.69f,.34f)));
            var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=5;light.intensity=.55f;light.color=new Color(1,.82f,.64f);light.cullingMask=1<<Layer;
            var sign=new GameObject("Solo mission sign").transform;sign.SetParent(root,false);sign.localPosition=new Vector3(0,2.55f,-1.91f);sign.localRotation=Quaternion.Euler(0,180,0);
            var text=sign.gameObject.AddComponent<TextMesh>();text.text="TOILET DUTY\nUnclog · heal · return";text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=64;text.characterSize=.028f;text.color=new Color(.97f,.83f,.56f);SaloonWorldText.Attach(text);
            SaloonSmokingArea.SetLayer(root,Layer);
            GameObject Box(string name,Vector3 position,Vector3 scale,Material material)=>SaloonWorld.Box(name,Center+position,scale,material,root);
        }
    }
}
