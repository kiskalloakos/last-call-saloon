using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;

namespace Saloon
{
    public static class SaloonInterior
    {
        static Transform root;
        static Material oak,walnut,cream,brass,leather,mirror;

        public static void Apply(GameObject world)
        {
            if(world.transform.Find("Western interior")!=null)return;
            root=new GameObject("Western interior").transform;root.SetParent(world.transform,false);
            var texture=Resources.Load<Texture2D>("Art/LowPoly/Textures/WoodPlanks");
            oak=Wood("Counter oak",new Color(.47f,.28f,.14f),texture,new Vector2(2,1));
            walnut=Wood("Walnut joinery",new Color(.24f,.135f,.075f),texture,new Vector2(1,2));
            cream=SaloonWorld.Material("Warm limewash",new Color(.70f,.61f,.47f),.05f);
            brass=SaloonWorld.Material("Polished aged brass",new Color(.66f,.43f,.16f),.65f);brass.SetFloat("_Metallic",.65f);
            leather=SaloonWorld.Material("Oxblood leather",new Color(.23f,.065f,.038f),.28f);
            mirror=SaloonWorld.Material("Smoky backbar mirror",new Color(.32f,.40f,.39f),.88f);mirror.SetFloat("_Metallic",.8f);
            foreach(var child in world.GetComponentsInChildren<Transform>(true))
            {
                if(child==root||child.IsChildOf(root))continue;
                var renderer=child.GetComponent<Renderer>();
                if(child.name=="Bar visual"||child.name=="Shelf visual"||child.name=="Bottle"||child.name=="Bottle neck"||child.name=="Brass foot rail")
                    foreach(var r in child.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                if(renderer!=null&&(child.name.Contains("wall")||child.name.StartsWith("Room "))&&child.GetComponent<Collider>()!=null)renderer.sharedMaterial=cream;
                if(child.name=="stoolBar")
                {
                    foreach(var r in child.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=walnut;
                    Cylinder("Leather stool seat",child.position+Vector3.up*.875f,new Vector3(.43f,.035f,.43f),leather);
                }
            }
            Counter();Backbar();WallDetails();
            BatchGeometry();
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.41f,.36f,.29f);
            RenderSettings.fogDensity=.0035f;
        }

        static Material Wood(string name,Color tint,Texture texture,Vector2 tiling)
        {
            var mat=SaloonWorld.Material(name,tint,.26f);mat.mainTexture=texture;mat.mainTextureScale=tiling;return mat;
        }
        static GameObject Box(string name,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=size;
            var collider=go.GetComponent<Collider>();collider.enabled=false;Remove(collider);
            go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
        static GameObject Cylinder(string name,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=size;
            var collider=go.GetComponent<Collider>();collider.enabled=false;Remove(collider);
            go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
        static void Counter()
        {
            // Separate panels preserve grain scale instead of stretching a whole asset.
            Box("Counter carcass",new Vector3(-1.8f,.62f,3.8f),new Vector3(8.5f,1.24f,1),walnut);
            Box("Counter plinth",new Vector3(-1.8f,.12f,3.24f),new Vector3(8.65f,.22f,.15f),walnut);
            for(int i=0;i<6;i++)
            {
                float x=-5.35f+i*1.42f;
                Box("Raised oak panel",new Vector3(x,.68f,3.268f),new Vector3(1.19f,.87f,.055f),oak);
                Box("Panel inset",new Vector3(x,.68f,3.23f),new Vector3(.98f,.65f,.035f),walnut);
                Box("Panel field",new Vector3(x,.68f,3.205f),new Vector3(.89f,.56f,.025f),oak);
                Box("Counter stile",new Vector3(x-.68f,.69f,3.215f),new Vector3(.10f,1.18f,.13f),walnut);
            }
            Box("Counter apron",new Vector3(-1.8f,1.21f,3.20f),new Vector3(8.7f,.12f,.15f),walnut);
            Box("Counter top edge",new Vector3(-1.8f,1.335f,3.8f),new Vector3(9,.09f,1.4f),walnut);
            for(int plank=0;plank<4;plank++)
                Box("Counter top plank",new Vector3(-1.8f,1.405f,3.27f+plank*.35f),new Vector3(8.96f,.05f,.34f),oak);
            Box("Brass counter inlay",new Vector3(-1.8f,1.36f,3.092f),new Vector3(8.85f,.022f,.018f),brass);
            var rail=Cylinder("Brass foot rail",new Vector3(-1.8f,.27f,2.96f),new Vector3(.042f,4.25f,.042f),brass);rail.transform.localRotation=Quaternion.Euler(0,0,90);
            foreach(float x in new[]{-5.6f,-3.7f,-1.8f,.1f,2})
            {
                Box("Foot rail bracket",new Vector3(x,.24f,3.1f),new Vector3(.035f,.055f,.31f),brass);
                Cylinder("Rail socket",new Vector3(x,.17f,3.23f),new Vector3(.075f,.12f,.075f),brass);
            }
            Box("Tap drip tray",new Vector3(1.6f,1.444f,3.51f),new Vector3(.62f,.018f,.36f),brass);
            for(int i=0;i<2;i++)
            {
                float x=1.43f+i*.31f;
                Cylinder("Brass beer tower",new Vector3(x,1.64f,3.79f),new Vector3(.075f,.21f,.075f),brass);
                var spout=Cylinder("Tap spout",new Vector3(x,1.78f,3.68f),new Vector3(.045f,.12f,.045f),brass);spout.transform.localRotation=Quaternion.Euler(90,0,0);
                Box("Tap handle",new Vector3(x,1.91f,3.74f),new Vector3(.065f,.21f,.055f),walnut);
            }
        }
        static void Backbar()
        {
            var green=SaloonWorld.Material("Bottle green",new Color(.12f,.27f,.12f),.75f);
            var amber=SaloonWorld.Material("Bottle amber",new Color(.38f,.19f,.055f),.75f);
            var blue=SaloonWorld.Material("Bottle blue",new Color(.12f,.23f,.27f),.75f);
            var label=SaloonWorld.Material("Bottle paper",new Color(.80f,.72f,.52f),.05f);
            var colors=new[]{green,amber,blue};
            for(int bay=0;bay<3;bay++)
            {
                float center=-4.7f+bay*2.85f;
                Box("Backbar mirror",new Vector3(center,1.67f,6.63f),new Vector3(2.55f,1.85f,.035f),mirror);
                foreach(float x in new[]{center-1.35f,center+1.35f})Box("Backbar pillar",new Vector3(x,1.36f,6.50f),new Vector3(.14f,2.7f,.24f),walnut);
                Box("Backbar cornice",new Vector3(center,2.77f,6.47f),new Vector3(2.82f,.19f,.4f),walnut);
                Box("Backbar cabinet",new Vector3(center,.39f,6.45f),new Vector3(2.69f,.78f,.46f),walnut);
                for(int shelf=0;shelf<3;shelf++)
                {
                    float y=.78f+shelf*.60f;
                    Box("Bottle shelf",new Vector3(center,y,6.38f),new Vector3(2.64f,.075f,.48f),oak);
                    Box("Shelf brass edge",new Vector3(center,y+.025f,6.133f),new Vector3(2.65f,.018f,.018f),brass);
                    for(int bottle=0;bottle<7;bottle++)
                    {
                        float x=center-1.06f+bottle*.35f,height=.22f+((bay+bottle+shelf)%3)*.035f;
                        var material=colors[(bottle+bay+shelf)%colors.Length];var position=new Vector3(x,y+.04f,6.34f);
                        Cylinder("Backbar bottle",position+Vector3.up*(height*.5f),new Vector3(.105f,height*.5f,.105f),material);
                        Cylinder("Bottle label",position+Vector3.up*(height*.48f),new Vector3(.108f,height*.14f,.108f),label);
                        Cylinder("Bottle neck",position+Vector3.up*(height+.045f),new Vector3(.048f,.045f,.048f),material);
                        Cylinder("Bottle stopper",position+Vector3.up*(height+.095f),new Vector3(.052f,.012f,.052f),brass);
                    }
                }
            }
            Lamp(new Vector3(-6.6f,2.35f,5.95f));Lamp(new Vector3(3.35f,2.35f,5.95f));
        }
        static void WallDetails()
        {
            foreach(float center in new[]{-5f,5f})
            {
                Box("Room oak dado",new Vector3(center,.52f,-5.575f),new Vector3(3.8f,1.04f,.045f),walnut);
                Box("Room picture rail",new Vector3(center,1.09f,-5.53f),new Vector3(3.9f,.075f,.09f),oak);
                foreach(float x in new[]{center-1.87f,center+1.87f})
                {Box("Room side dado",new Vector3(x,.52f,-3.78f),new Vector3(.05f,1.04f,3.55f),walnut);Box("Room side cap",new Vector3(x,1.09f,-3.78f),new Vector3(.09f,.075f,3.55f),oak);}
                Picture(new Vector3(center,2.02f,-5.43f),center<0?new Color(.35f,.13f,.065f):new Color(.16f,.30f,.29f));
                Lamp(new Vector3(center+1.38f,2.0f,-5.35f));
            }
            foreach(float x in new[]{-7.74f,7.74f})
            {
                Box("Main wall chair rail",new Vector3(x,1.2f,2.4f),new Vector3(.12f,.075f,7.6f),oak);
                for(int i=0;i<7;i++)Box("Main wall panel stile",new Vector3(x,.60f,-.8f+i*1.10f),new Vector3(.12f,1.12f,.07f),walnut);
            }
        }
        static void Picture(Vector3 position,Color color)
        {
            var art=SaloonWorld.Material("Western wall print",color,.05f);
            Box("Picture frame",position,new Vector3(.93f,.64f,.075f),walnut);
            Box("Picture mat",position+Vector3.forward*.044f,new Vector3(.81f,.52f,.018f),cream);
            Box("Picture field",position+Vector3.forward*.058f,new Vector3(.72f,.43f,.012f),art);
            var sun=Cylinder("Print medallion",position+new Vector3(.18f,.045f,.072f),new Vector3(.17f,.006f,.17f),brass);sun.transform.localRotation=Quaternion.Euler(90,0,0);
        }
        static void Lamp(Vector3 position)
        {
            Box("Lamp backplate",position,new Vector3(.19f,.35f,.06f),brass);
            Box("Lamp shade",position+new Vector3(0,.06f,-.13f),new Vector3(.28f,.22f,.22f),walnut);
            var bulb=new GameObject("Warm wall lamp");bulb.transform.SetParent(root,false);bulb.transform.localPosition=position+new Vector3(0,-.10f,-.25f);
            var light=bulb.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.80f,.55f);light.intensity=.85f;light.range=4.2f;
        }
        static void Remove(Object obj){if(Application.isPlaying)Object.Destroy(obj);else Object.DestroyImmediate(obj);}
        static void BatchGeometry()
        {
            var pieces=root.GetComponentsInChildren<MeshFilter>();
            foreach(var group in pieces.GroupBy(p=>p.GetComponent<Renderer>().sharedMaterial))
            {
                var mesh=new Mesh{name="Western interior · "+group.Key.name};mesh.indexFormat=IndexFormat.UInt32;
                var instances=group.Select(p=>new CombineInstance{mesh=p.sharedMesh,transform=root.worldToLocalMatrix*p.transform.localToWorldMatrix}).ToArray();
                mesh.CombineMeshes(instances,true,true);
                var go=new GameObject(mesh.name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
                foreach(var piece in group)Remove(piece.gameObject);
            }
        }
    }
}
