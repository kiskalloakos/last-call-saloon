using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Saloon
{
    public static class SaloonExteriorWindows
    {
        public const string RootName="Exterior saloon windows",PaneName="Exterior window pane";
        const float Width=1.8f,Sill=1.35f,Head=2.95f;
        public static void Apply(GameObject world)
        {
            var existing=world.transform.Find(RootName);
            if(existing!=null){ConfigureBreakables(existing);return;}
            var root=new GameObject(RootName).transform;root.SetParent(world.transform,false);
            var timber=world.transform.Find("Western interior").GetComponentsInChildren<Renderer>()
                .First(r=>r.sharedMaterial.name.Contains("Walnut joinery")).sharedMaterial;
            var glass=SaloonWorld.Material("Exterior window glass",new Color(.72f,.85f,.83f,.07f),.85f);
            glass.SetFloat("_Mode",3);glass.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHABLEND_ON");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=(int)RenderQueue.Transparent;
            // Cut every layer of the thick outer room wall as well as the saloon shell.
            // Dado and chair rails are below the sill and stay intact.
            foreach(var source in world.GetComponentsInChildren<Transform>(true).Where(t=>
                t.name=="West wall"||t.name=="East wall"||t.name=="Room outer wall infill"||t.name=="Room side"&&Mathf.Abs(t.position.x)>6.5f).ToArray())
            {
                var bounds=source.GetComponent<Renderer>().bounds;var material=source.GetComponent<Renderer>().sharedMaterial;
                float[] centers=source.name=="West wall"||source.name=="East wall"?new[]{-3.7f,.6f,4.3f}:new[]{-3.7f};
                float from=bounds.min.z;
                foreach(float z in centers)
                {
                    Segment(from,z-Width/2,bounds.min.y,bounds.max.y);
                    Segment(z-Width/2,z+Width/2,bounds.min.y,Sill);
                    Segment(z-Width/2,z+Width/2,Head,bounds.max.y);
                    from=z+Width/2;
                }
                Segment(from,bounds.max.z,bounds.min.y,bounds.max.y);
                source.gameObject.SetActive(false);
                void Segment(float start,float end,float bottom,float top)
                {
                    if(end-start<.001f)return;
                    SaloonWorld.Box("Exterior wall section",new Vector3(bounds.center.x,(bottom+top)/2,(start+end)/2),new Vector3(bounds.size.x,top-bottom,end-start),material,root);
                }
            }
            foreach(float side in new[]{-1f,1f})
            {
                Window(side,7.84f,.6f,"Bar front",.12f);
                Window(side,7.84f,4.3f,"Bar rear",.12f);
                Window(side,6.87f,-3.7f,side<0?"Coin Flip":"Blackjack",1.25f);
            }
            SaloonSmokingArea.SetLayer(root,SaloonSmokingArea.IndoorLayer);
            ConfigureBreakables(root);
            void Window(float side,float inner,float z,string room,float depth)
            {
                var frame=new GameObject((side<0?"West ":"East ")+room+" exterior window").transform;frame.SetParent(root,false);
                float middle=(Sill+Head)/2,x=side*inner;
                Part("Window sill",new Vector3(side*(inner+depth/2),Sill,z),new Vector3(depth+.18f,.12f,Width+.22f),timber);
                Part("Window lintel",new Vector3(side*(inner+depth/2),Head,z),new Vector3(depth,.12f,Width+.22f),timber);
                foreach(float edge in new[]{-1f,1f})
                    Part("Window jamb",new Vector3(side*(inner+depth/2),middle,z+edge*Width/2),new Vector3(depth,Head-Sill,.12f),timber);
                Part("Window mullion",new Vector3(x,middle,z),new Vector3(.09f,Head-Sill,.045f),timber);
                Part("Window crossbar",new Vector3(x,middle,z),new Vector3(.09f,.045f,Width),timber);
                var pane=Part(PaneName,new Vector3(x+side*.045f,middle,z),new Vector3(.025f,Head-Sill,Width),glass);
                pane.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                // Intact glass stops bullets. A separate movement boundary survives shattering.
                var barrier=pane.AddComponent<BoxCollider>();barrier.size=new Vector3(3.2f,1,1);
                GameObject Part(string name,Vector3 position,Vector3 size,Material material)
                {
                    var part=SaloonWorld.Box(name,position,size,material,frame);var collider=part.GetComponent<Collider>();collider.enabled=false;
                    if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);
                    return part;
                }
            }
        }
        static void ConfigureBreakables(Transform root)
        {
            int id=1;
            foreach(var pane in root.GetComponentsInChildren<Renderer>().Where(p=>p.name==PaneName).OrderBy(p=>p.transform.parent.name,System.StringComparer.Ordinal))
            {
                var prop=pane.GetComponent<SaloonBreakable>()??pane.gameObject.AddComponent<SaloonBreakable>();prop.id=id++;prop.window=true;
                if(pane.transform.Find("Window movement boundary")!=null)continue;
                var boundary=new GameObject("Window movement boundary");boundary.transform.SetParent(pane.transform,false);boundary.layer=SaloonMovement.SolidLayer;
                boundary.AddComponent<BoxCollider>().size=pane.GetComponent<BoxCollider>().size;
            }
        }

    }
}
