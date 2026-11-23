using UnityEngine;

namespace Saloon
{
    public static class SaloonToilet
    {
        public const string RootName="Bathroom toilet";
        public static readonly Vector3 Position=SaloonBathroom.Center+Vector3.forward*.95f;
        public static readonly Vector3 Approach=Position+Vector3.back*.95f+Vector3.up*.08f;
        public static bool InReach(Vector3 position)=>Vector3.ProjectOnPlane(position-Approach,Vector3.up).magnitude<=1;
        public static void Present(bool working,int pumps,float pumpAge)
        {
            var root=GameObject.Find(SaloonBathroom.RootName)?.transform.Find(RootName);
            if(root!=null)root.GetComponent<SaloonToiletVisual>().Show(working,pumps,pumpAge);
        }
        public static void Apply()
        {
            var world=GameObject.Find("Saloon World");if(world==null)return;
            SaloonBathroom.Apply(world);var bathroom=GameObject.Find(SaloonBathroom.RootName).transform;if(bathroom.Find(RootName)!=null)return;
            var root=new GameObject(RootName).transform;root.SetParent(bathroom,false);root.position=Position;
            var porcelain=SaloonWorld.Material("Toilet ivory porcelain",new Color(.87f,.84f,.70f),.6f);
            var timber=SaloonWorld.Material("Toilet walnut seat",new Color(.22f,.12f,.065f));
            var rubber=SaloonWorld.Material("Toilet plunger rubber",new Color(.34f,.11f,.065f));
            Part("Toilet pedestal",PrimitiveType.Cube,new Vector3(0,.22f,0),new Vector3(.38f,.44f,.5f),porcelain);
            Part("Toilet bowl",PrimitiveType.Sphere,new Vector3(0,.46f,-.1f),new Vector3(.65f,.36f,.85f),porcelain);
            Part("Walnut seat rim",PrimitiveType.Cylinder,new Vector3(0,.61f,-.1f),new Vector3(.65f,.035f,.8f),timber);
            var water=SaloonWorld.Material("Clogged toilet water",new Color(.24f,.17f,.055f),.8f);
            Part("Bowl water",PrimitiveType.Cylinder,new Vector3(0,.65f,-.1f),new Vector3(.46f,.012f,.61f),water);
            var waste=SaloonWorld.Material("Toilet waste",new Color(.19f,.085f,.025f),.1f);
            var clog=new GameObject("Visible clog").transform;clog.SetParent(root,false);clog.localPosition=new Vector3(0,.68f,0);
            for(int i=0;i<4;i++)
            {
                var lump=Part("Waste",PrimitiveType.Sphere,new Vector3(-.12f+i*.075f,.69f+(i%2)*.025f,-.18f+(i%2)*.13f),new Vector3(.16f,.09f,.22f),waste);
                lump.transform.SetParent(clog,true);lump.transform.localRotation=Quaternion.Euler(0,i*43,0);
            }
            Part("Flush cistern",PrimitiveType.Cube,new Vector3(0,.92f,.38f),new Vector3(.7f,.65f,.27f),porcelain);
            Part("Cistern lid",PrimitiveType.Cube,new Vector3(0,1.26f,.38f),new Vector3(.76f,.06f,.32f),porcelain);
            Part("Flush handle",PrimitiveType.Cube,new Vector3(.26f,1.07f,.20f),new Vector3(.18f,.06f,.07f),timber);
            var plunger=new GameObject("Working plunger").transform;plunger.SetParent(root,false);
            var cup=Part("Plunger cup",PrimitiveType.Sphere,Vector3.zero,new Vector3(.28f,.18f,.28f),rubber);cup.transform.SetParent(plunger,false);
            var handle=Part("Plunger handle",PrimitiveType.Cylinder,new Vector3(0,.44f,0),new Vector3(.035f,.45f,.035f),timber);handle.transform.SetParent(plunger,false);
            var visual=root.gameObject.AddComponent<SaloonToiletVisual>();visual.Initialize(plunger,clog,water);
            var solid=root.gameObject.AddComponent<BoxCollider>();solid.center=new Vector3(0,.55f,.1f);solid.size=new Vector3(.76f,1.1f,1.05f);
            SaloonSmokingArea.SetLayer(root,SaloonBathroom.Layer);
            GameObject Part(string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
            {
                var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=scale;
                go.GetComponent<Renderer>().sharedMaterial=material;var collider=go.GetComponent<Collider>();collider.enabled=false;
                if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);return go;
            }
        }
    }
    public sealed class SaloonToiletVisual : MonoBehaviour
    {
        Transform plunger,clog;
        Material water;
        bool working;
        float pumpAt=-100;
        public void Initialize(Transform tool,Transform waste,Material liquid)
        {plunger=tool;clog=waste;water=liquid;Show(false,0,100);}
        public void Show(bool active,int pumps,float age)
        {
            working=active;pumpAt=Time.time-age;
            float remaining=1-Mathf.Clamp01(pumps/(float)SaloonGame.ToiletPumpCount);
            clog.gameObject.SetActive(remaining>0);clog.localScale=Vector3.one*Mathf.Sqrt(remaining);clog.localPosition=new Vector3(0,.68f-(1-remaining)*.06f,0);
            water.color=Color.Lerp(new Color(.31f,.54f,.58f),new Color(.24f,.17f,.055f),remaining);
            Update();
        }
        void Update()
        {
            if(plunger==null)return;
            float stroke=working?Mathf.Sin(Mathf.Clamp01((Time.time-pumpAt)/SaloonGame.ToiletPumpInterval)*Mathf.PI)*.23f:0;
            plunger.localPosition=working?new Vector3(0,.81f-stroke,-.1f):new Vector3(-.58f,.09f,.15f);
        }
    }
}
