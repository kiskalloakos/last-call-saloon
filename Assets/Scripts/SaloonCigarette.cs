using System.Collections.Generic;
using UnityEngine;

namespace Saloon
{
    public sealed class SaloonCigarette : MonoBehaviour
    {
        readonly List<Object> owned=new List<Object>();
        Transform paper,ash,ember;
        Material glow;
        Light light;
        ParticleSystem wisp;
        public float Heat { get; private set; }
        public Vector3 Tip=>ember.position;
        public static SaloonCigarette Create(Transform parent,string name)
        {
            var root=new GameObject(name);root.transform.SetParent(parent,false);
            var cigarette=root.AddComponent<SaloonCigarette>();cigarette.Build();return cigarette;
        }
        Material Mat(string name,Color color)
        {var mat=new Material(Shader.Find("Standard")){name=name,color=color};mat.SetFloat("_Glossiness",.05f);owned.Add(mat);return mat;}
        void Build()
        {
            var filter=Mat("Cork cigarette filter",new Color(.67f,.37f,.13f));
            var white=Mat("Cigarette paper",new Color(.91f,.88f,.77f));
            var grey=Mat("Burnt ash",new Color(.22f,.22f,.20f));
            glow=Mat("Burning cigarette ember",new Color(.74f,.16f,.025f));glow.EnableKeyword("_EMISSION");
            Segment("Low-poly filter",.024f,.0045f,filter).localPosition=Vector3.forward*.012f;
            paper=Segment("Low-poly paper",.05f,.0044f,white);
            ash=Segment("Burning ash",.007f,.0045f,grey);
            ember=Segment("Glowing ember",.0025f,.0046f,glow);
            light=new GameObject("Ember glow").AddComponent<Light>();light.transform.SetParent(ember,false);light.type=LightType.Point;light.color=new Color(1,.24f,.035f);light.range=.22f;light.shadows=LightShadows.None;
            wisp=MakeSmoke(ember,"Cigarette wisp");
            Burn(0,0);
        }
        Transform Segment(string name,float length,float radius,Material mat)
        {
            const int sides=8;var vertices=new Vector3[sides*2+2];var triangles=new List<int>();
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides;float x=Mathf.Cos(a)*radius,y=Mathf.Sin(a)*radius;
                vertices[i]=new Vector3(x,y,-.5f);vertices[sides+i]=new Vector3(x,y,.5f);
                int j=(i+1)%sides;
                triangles.AddRange(new[]{i,j,sides+j,i,sides+j,sides+i,2*sides,j,i,2*sides+1,sides+i,sides+j});
            }
            vertices[2*sides]=new Vector3(0,0,-.5f);vertices[2*sides+1]=new Vector3(0,0,.5f);
            var mesh=new Mesh{name=name,vertices=vertices,triangles=triangles.ToArray()};mesh.RecalculateNormals();owned.Add(mesh);
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.localScale=new Vector3(1,1,length);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;return go.transform;
        }
        public void Burn(int puffs,float age)
        {
            float inhaling=SaloonSmoking.Inhale(age);
            float burnt=Mathf.Min(.024f,Mathf.Max(0,puffs)*.002f);
            float length=.05f-burnt;
            paper.localScale=new Vector3(1,1,length);paper.localPosition=Vector3.forward*(.024f+length*.5f);
            ash.localPosition=Vector3.forward*(.024f+length+.0035f);
            ember.localPosition=Vector3.forward*(.024f+length+.007f);
            Heat=.3f+inhaling*3.5f;
            glow.SetColor("_EmissionColor",new Color(1,.14f,.015f)*Heat);light.intensity=.12f+inhaling*.8f;
            var emission=wisp.emission;emission.rateOverTime=.8f+inhaling*2;
        }
        public static ParticleSystem MakeSmoke(Transform parent,string name)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localRotation=Quaternion.Euler(-90,0,0);
            var particles=go.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.playOnAwake=false;main.loop=true;main.startLifetime=new ParticleSystem.MinMaxCurve(1.5f,2.8f);main.startSpeed=new ParticleSystem.MinMaxCurve(.035f,.10f);main.startSize=new ParticleSystem.MinMaxCurve(.02f,.04f);
            main.startColor=new Color(.72f,.73f,.72f,.7f);main.scalingMode=ParticleSystemScalingMode.Shape;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=90;
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.004f;
            var emission=particles.emission;emission.rateOverTime=0;
            var velocity=particles.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.y=new ParticleSystem.MinMaxCurve(.055f);
            var size=particles.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.4f,1,4));
            var colors=particles.colorOverLifetime;colors.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.75f,.76f,.74f),0),new GradientColorKey(new Color(.70f,.72f,.70f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.5f,.12f),new GradientAlphaKey(0,1)});colors.color=gradient;
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Billboard;
            var texture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Soft smoke puff"};var pixels=new Color[1024];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/16;pixels[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2));}
            texture.SetPixels(pixels);texture.Apply();renderer.sharedMaterial=new Material(Resources.Load<Shader>("Shaders/SaloonWorldText")){name="Cigarette smoke",mainTexture=texture};
            var cleanup=go.AddComponent<SmokeResources>();cleanup.texture=texture;cleanup.material=renderer.sharedMaterial;
            particles.Play();return particles;
        }
        void OnEnable(){if(wisp!=null)wisp.Play();}
        void OnDestroy(){foreach(var obj in owned)if(obj!=null)Destroy(obj);}
    }
    public sealed class SmokeResources : MonoBehaviour
    {
        public Texture2D texture;public Material material;
        void OnDestroy(){if(texture!=null)Destroy(texture);if(material!=null)Destroy(material);}
    }
}
