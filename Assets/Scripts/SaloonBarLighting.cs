using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Saloon
{
    // Camera-scoped lighting preserves the two enclosed rooms and smoking deck,
    // including when a spectator changes rooms or an offscreen preview renders.
    public sealed class SaloonBarLighting : MonoBehaviour
    {
        public const string RootName="Bar window sunset lighting";
        public float Dusk { get; private set; }
        public float TargetDusk { get; private set; }
        SaloonGame game;
        Transform world;
        Light[] oldLights,windowLights;
        Material sky,lampGlass;
        Color glassEmission;
        bool glassWasEmissive;
        Light outsideSun;
        bool initialized,rendering;
        int lastRound;
        AmbientMode ambientMode;
        Color ambient,ambientSky,ambientEquator,ambientGround,skyZenith,skyHorizon;
        SphericalHarmonicsL2 ambientProbe;
        Vector4 sunDirection;
        float sunIntensity,sunStrength;
        LightState[] lightStates;
        struct LightState { public bool enabled;public float intensity,range;public Color color;public int mask; }

        public void Initialize(SaloonGame owner)
        {
            game=owner;world=GameObject.Find("Saloon World").transform;
            var lighting=world.Find(SaloonLighting.RootName);
            oldLights=lighting.GetComponentsInChildren<Light>(true);
            lightStates=new LightState[oldLights.Length];
            lampGlass=oldLights.First(l=>l.name=="Pendant lantern").transform.Find("Luminous glass chamber").GetComponent<Renderer>().sharedMaterial;
            var root=new GameObject(RootName).transform;root.SetParent(world,false);
            windowLights=new Light[2];
            for(int i=0;i<windowLights.Length;i++)
            {
                var source=new GameObject("Sunset through east window "+(i+1));source.transform.SetParent(root,false);
                float z=i==0?.6f:4.3f;source.transform.position=new Vector3(9,2.8f,z-.25f);
                source.transform.rotation=Quaternion.LookRotation(new Vector3(-4,.35f,z+3)-source.transform.position);
                var light=source.AddComponent<Light>();light.type=LightType.Spot;light.range=22;light.spotAngle=55;light.innerSpotAngle=38;
                light.shadows=LightShadows.Soft;light.shadowBias=.025f;light.shadowNormalBias=.1f;
                light.renderMode=LightRenderMode.ForcePixel;
                light.cullingMask=1|(1<<SaloonSmokingArea.IndoorLayer)|(1<<SaloonViewmodel.Layer);light.enabled=false;
                windowLights[i]=light;
            }
            var vista=GameObject.Find("Saloon entrance sunset").transform;
            sky=vista.GetComponentsInChildren<Renderer>().First(r=>r.sharedMaterial.shader.name=="Saloon/Desert Sunset").sharedMaterial;
            outsideSun=vista.GetComponentInChildren<Light>();initialized=true;SetRound(1,true);
        }
        public void SetRound(int round,bool immediate=false)
        {
            round=Mathf.Max(1,round);TargetDusk=Mathf.Clamp01((round-1)/19f);
            if(immediate||round<lastRound)Dusk=TargetDusk;
            lastRound=round;
        }
        void Update(){Dusk=Mathf.MoveTowards(Dusk,TargetDusk,Time.unscaledDeltaTime*.035f);}
        void OnEnable(){Camera.onPreCull+=BeforeCamera;Camera.onPostRender+=AfterCamera;}
        void OnDisable(){Restore();Camera.onPreCull-=BeforeCamera;Camera.onPostRender-=AfterCamera;}
        void OnDestroy(){Restore();}
        void BeforeCamera(Camera camera)
        {
            if(!initialized||rendering||game==null||game.TutorialActive||!world.gameObject.activeInHierarchy)return;
            if(camera!=game.ViewCamera&&camera!=game.ViewCamera.GetComponent<SaloonViewmodel>()?.Overlay)return;
            var p=game.ViewCamera.transform.position;
            if(SaloonSmokingArea.Contains(p)||p.x<-7.9f||p.x>11.65f||p.z<-7.1f||p.z>7.1f)return;
            if(p.z<SaloonRoomWindows.FrontZ&&p.z>-5.8f&&Mathf.Abs(p.x)>2.9f&&Mathf.Abs(p.x)<7.2f)return;
            rendering=true;
            glassEmission=lampGlass.GetColor("_EmissionColor");glassWasEmissive=lampGlass.IsKeywordEnabled("_EMISSION");
            lampGlass.EnableKeyword("_EMISSION");lampGlass.SetColor("_EmissionColor",new Color(.55f,.30f,.09f));
            ambientMode=RenderSettings.ambientMode;ambient=RenderSettings.ambientLight;ambientSky=RenderSettings.ambientSkyColor;
            ambientEquator=RenderSettings.ambientEquatorColor;ambientGround=RenderSettings.ambientGroundColor;ambientProbe=RenderSettings.ambientProbe;
            skyZenith=sky.GetColor("_Zenith");skyHorizon=sky.GetColor("_Horizon");sunDirection=sky.GetVector("_SunDirection");sunIntensity=outsideSun.intensity;sunStrength=sky.GetFloat("_SunStrength");
            for(int i=0;i<oldLights.Length;i++)
            {
                var light=oldLights[i];lightStates[i]=new LightState{enabled=light.enabled,intensity=light.intensity,range=light.range,color=light.color,mask=light.cullingMask};
                if(light.type==LightType.Directional)
                {light.intensity=.18f;light.color=new Color(1,.66f,.39f);}
                else if(light.name=="Pendant lantern"&&light.transform.position.z>SaloonRoomWindows.FrontZ)
                {light.color=new Color(1,.65f,.36f);light.intensity=Mathf.Lerp(.9f,1.15f,Dusk);light.range=6;light.cullingMask|=1;}
                else if(light.name=="Wall lantern")
                {light.color=new Color(1,.61f,.31f);light.intensity=Mathf.Lerp(.7f,.85f,Dusk);light.range=5;}
            }
            RenderSettings.ambientMode=AmbientMode.Flat;
            var bounce=Color.Lerp(new Color(.52f,.42f,.34f),new Color(.30f,.32f,.37f),Dusk);
            RenderSettings.ambientLight=bounce;var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(bounce);RenderSettings.ambientProbe=probe;
            foreach(var light in windowLights)
            {light.enabled=true;light.color=Color.Lerp(new Color(1,.56f,.28f),new Color(.38f,.43f,.67f),Dusk);light.intensity=Mathf.Lerp(5.2f,.28f,Dusk);}
            sky.SetColor("_Zenith",Color.Lerp(skyZenith,new Color(.025f,.035f,.10f),Dusk));
            sky.SetColor("_Horizon",Color.Lerp(skyHorizon,new Color(.12f,.075f,.13f),Dusk));
            var direction=sunDirection;direction.y=Mathf.Lerp(direction.y,-.22f,Dusk);sky.SetVector("_SunDirection",direction);
            sky.SetFloat("_SunStrength",Mathf.Lerp(sunStrength,0,Dusk));
            outsideSun.intensity=Mathf.Lerp(sunIntensity,.12f,Dusk);
        }
        void AfterCamera(Camera camera)
        {
            if(game!=null&&(camera==game.ViewCamera||camera==game.ViewCamera.GetComponent<SaloonViewmodel>()?.Overlay))Restore();
        }
        void Restore()
        {
            if(!rendering)return;rendering=false;
            if(lampGlass!=null)
            {lampGlass.SetColor("_EmissionColor",glassEmission);if(!glassWasEmissive)lampGlass.DisableKeyword("_EMISSION");}
            RenderSettings.ambientMode=ambientMode;RenderSettings.ambientLight=ambient;RenderSettings.ambientSkyColor=ambientSky;
            RenderSettings.ambientEquatorColor=ambientEquator;RenderSettings.ambientGroundColor=ambientGround;RenderSettings.ambientProbe=ambientProbe;
            for(int i=0;i<oldLights.Length;i++)if(oldLights[i]!=null)
            {var light=oldLights[i];var state=lightStates[i];light.enabled=state.enabled;light.intensity=state.intensity;light.range=state.range;light.color=state.color;light.cullingMask=state.mask;}
            foreach(var light in windowLights)if(light!=null)light.enabled=false;
            if(sky!=null){sky.SetColor("_Zenith",skyZenith);sky.SetColor("_Horizon",skyHorizon);sky.SetVector("_SunDirection",sunDirection);sky.SetFloat("_SunStrength",sunStrength);}
            if(outsideSun!=null)outsideSun.intensity=sunIntensity;
        }
    }
}
