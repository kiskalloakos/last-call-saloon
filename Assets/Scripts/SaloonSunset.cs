using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Saloon
{
    public static class SaloonSunset
    {
        public static readonly Vector3 Direction=new Vector3(1,.065f,.18f).normalized;
        public static void Build(Transform yard)
        {
            var vista=new GameObject("Wild West sunset overlook").transform;vista.SetParent(yard,false);
            var sky=GameObject.CreatePrimitive(PrimitiveType.Sphere);sky.name="Apricot and violet sunset sky";
            sky.transform.SetParent(vista,false);sky.transform.localScale=Vector3.one*230;RemoveCollider(sky);
            var skyMaterial=new Material(Resources.Load<Shader>("Shaders/SaloonSunset")){name="Desert sunset sky"};
            skyMaterial.SetVector("_Center",vista.position);skyMaterial.SetVector("_SunDirection",Direction);
            var skyRenderer=sky.GetComponent<Renderer>();skyRenderer.sharedMaterial=skyMaterial;
            skyRenderer.shadowCastingMode=ShadowCastingMode.Off;skyRenderer.receiveShadows=false;
            var dust=Mat("Sunset sand",new Color(.58f,.33f,.19f));
            var far=Mat("Distant violet mesas",new Color(.26f,.19f,.29f));
            var middle=Mat("Dusk terracotta mesas",new Color(.43f,.23f,.24f));
            var near=Mat("Warm sandstone",new Color(.56f,.29f,.17f));
            var cactus=Mat("Desert sage silhouettes",new Color(.13f,.20f,.16f));
            var rock=Mat("Desert ochre rocks",new Color(.39f,.25f,.17f));
            // Broad, gently rising desert; decorative terrain has no gameplay collision.
            var ground=new GameObject("Desert fading into the horizon");ground.transform.SetParent(vista,false);
            var groundMesh=new Mesh();var vertices=new List<Vector3>();var triangles=new List<int>();
            const int segments=64;
            vertices.Add(new Vector3(0,-.12f,0));
            for(int ring=1;ring<=5;ring++)for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,r=ring*21;
                vertices.Add(new Vector3(Mathf.Cos(a)*r,-.15f+Mathf.Sin(a*5+ring)*.11f*ring,Mathf.Sin(a)*r));
            }
            for(int i=0;i<segments;i++){triangles.Add(0);triangles.Add(1+(i+1)%segments);triangles.Add(1+i);}
            for(int ring=1;ring<5;ring++)for(int i=0;i<segments;i++)
            {
                int a=1+(ring-1)*segments+i,b=1+(ring-1)*segments+(i+1)%segments,c=a+segments,d=b+segments;
                triangles.AddRange(new[]{a,b,c,b,d,c});
            }
            groundMesh.SetVertices(vertices);groundMesh.SetTriangles(triangles,0);groundMesh.RecalculateNormals();
            ground.AddComponent<MeshFilter>().sharedMesh=groundMesh;ground.AddComponent<MeshRenderer>().sharedMaterial=dust;
            // Leave a saddle between the mesas for the setting sun.
            Mesa(vista,new Vector3(86,-.8f,-36),new Vector3(25,13,23),far,11);
            Mesa(vista,new Vector3(91,-.6f,51),new Vector3(26,11,27),far,27);
            Mesa(vista,new Vector3(60,-.5f,-28),new Vector3(17,9,16),middle,7);
            Mesa(vista,new Vector3(72,-.5f,43),new Vector3(18,8,20),middle,14);
            Mesa(vista,new Vector3(38,-.5f,-35),new Vector3(12,7,15),near,19);
            Mesa(vista,new Vector3(52,-.5f,38),new Vector3(10,9,12),near,23);
            Mesa(vista,new Vector3(-45,-.5f,45),new Vector3(24,12,26),middle,31);
            Mesa(vista,new Vector3(-62,-.5f,-43),new Vector3(28,15,22),far,37);
            var random=new System.Random(817);
            for(int i=0;i<28;i++)
            {
                float angle=(float)random.NextDouble()*Mathf.PI*2,radius=9+(float)random.NextDouble()*33;
                var p=new Vector3(Mathf.Cos(angle)*radius,-.13f,Mathf.Sin(angle)*radius);
                if(i%3==0)Cactus(vista,p,1.5f+(float)random.NextDouble()*2.1f,cactus);
                else Mesa(vista,p,new Vector3(.3f+(float)random.NextDouble()*.6f,.25f+(float)random.NextDouble()*.45f,.45f),rock,i+50);
            }
            var sunset=new GameObject("Last light over the desert");sunset.transform.SetParent(vista,false);
            sunset.transform.rotation=Quaternion.LookRotation(-Direction);
            var light=sunset.AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.47f,.23f);
            light.intensity=1.4f;light.shadows=LightShadows.Soft;light.cullingMask=1<<SaloonSmokingArea.OutdoorLayer;
            SaloonSmokingArea.SetLayer(vista,SaloonSmokingArea.OutdoorLayer);
        }
        static Material Mat(string name,Color color)=>SaloonWorld.Material(name,color);
        static void RemoveCollider(GameObject go)
        {var collider=go.GetComponent<Collider>();if(collider!=null){collider.enabled=false;Object.Destroy(collider);}}
        static void Mesa(Transform parent,Vector3 position,Vector3 size,Material material,int seed)
        {
            var random=new System.Random(seed);const int sides=9;
            var rings=new Vector3[4,sides];float[] widths={1,.73f,.66f,.64f},heights={0,.22f,.86f,1};
            for(int i=0;i<sides;i++)
            {
                float angle=(i+(float)random.NextDouble()*.25f)*Mathf.PI*2/sides,r=.85f+(float)random.NextDouble()*.15f;
                for(int j=0;j<4;j++)rings[j,i]=Vector3.Scale(new Vector3(Mathf.Cos(angle)*r*widths[j],heights[j],Mathf.Sin(angle)*r*widths[j]),size);
            }
            var vertices=new List<Vector3>();var triangles=new List<int>();
            void Triangle(Vector3 a,Vector3 b,Vector3 c){int start=vertices.Count;vertices.AddRange(new[]{a,b,c});triangles.AddRange(new[]{start,start+1,start+2});}
            for(int i=0;i<sides;i++)
            {
                int n=(i+1)%sides;
                for(int j=0;j<3;j++){Triangle(rings[j,i],rings[j+1,i],rings[j,n]);Triangle(rings[j,n],rings[j+1,i],rings[j+1,n]);}
                Triangle(Vector3.up*size.y,rings[3,n],rings[3,i]);
            }
            var mesh=new Mesh{name="Wind-carved sandstone"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
            var go=new GameObject("Sandstone butte");go.transform.SetParent(parent,false);go.transform.localPosition=position;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        static void Cactus(Transform parent,Vector3 p,float height,Material material)
        {
            var root=new GameObject("Saguaro silhouette").transform;root.SetParent(parent,false);root.localPosition=p;
            void Limb(Vector3 a,Vector3 b,float radius)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name="Saguaro limb";go.transform.SetParent(root,false);
                go.transform.localPosition=(a+b)*.5f;go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
                go.transform.localScale=new Vector3(radius*2,(b-a).magnitude*.5f,radius*2);go.GetComponent<Renderer>().sharedMaterial=material;RemoveCollider(go);
            }
            Limb(Vector3.zero,Vector3.up*height,.12f);
            foreach(float side in new[]{-1f,1f})
            {
                float y=height*(side<0?.40f:.59f);var elbow=new Vector3(side*height*.23f,y,0);
                Limb(Vector3.up*y,elbow,.085f);Limb(elbow,elbow+Vector3.up*height*.26f,.085f);
            }
        }
    }
}
