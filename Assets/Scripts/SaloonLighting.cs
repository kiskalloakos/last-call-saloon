using UnityEngine;
using UnityEngine.Rendering;

namespace Saloon
{
    public static class SaloonLighting
    {
        public const string RootName="Lantern lighting v3";
        public static void Apply(GameObject world)
        {
            // Saved prototype scenes contain the original box lamps and floating lights.
            foreach(var child in world.GetComponentsInChildren<Transform>(true))
                if(child.name=="Hanging lantern"||child.name=="Lantern stem"||child.name=="Warm lamplight")
                {child.gameObject.SetActive(false);Remove(child.gameObject);}
            var sun=world.transform.Find("Evening sun");
            if(sun!=null){var light=sun.GetComponent<Light>();if(light!=null)light.enabled=false;}
            // Cool skylight and warm bounce separate faces, timber and colored props.
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.78f,.84f,.94f);
            RenderSettings.ambientEquatorColor=new Color(.72f,.66f,.53f);
            RenderSettings.ambientGroundColor=new Color(.40f,.32f,.23f);
            RenderSettings.fogColor=new Color(.72f,.52f,.35f);
            RenderSettings.fogDensity=.0012f;
            var existing=world.transform.Find(RootName);
            if(existing!=null){RefreshLighting(existing);return;}
            var old=world.transform.Find("Lantern lighting v2");
            if(old!=null){old.gameObject.SetActive(false);Remove(old.gameObject);}
            var root=new GameObject(RootName).transform;root.SetParent(world.transform,false);
            var iron=SaloonWorld.Material("Lantern blackened iron",new Color(.095f,.075f,.055f),.32f);iron.SetFloat("_Metallic",.6f);
            var brass=SaloonWorld.Material("Lantern burnished brass",new Color(.46f,.30f,.13f),.4f);brass.SetFloat("_Metallic",.65f);
            var glass=SaloonWorld.Material("Lantern glowing amber glass",new Color(.95f,.73f,.40f),.35f);
            glass.EnableKeyword("_EMISSION");glass.SetColor("_EmissionColor",new Color(1f,.69f,.30f)*.85f);
            Physics.SyncTransforms();
            foreach(var position in new[]{new Vector3(-3,3.25f,.15f),new Vector3(3,3.25f,.15f),new Vector3(-2,3.25f,5),new Vector3(6.5f,3.25f,4),
                new Vector3(-5,3.25f,-3.8f),new Vector3(5,3.25f,-3.8f)})
            {
                var lamp=Lantern(root,position,iron,brass,glass,false);
                float start=position.y+.31f;
                float ceiling=Physics.Raycast(new Vector3(position.x,start,position.z),Vector3.up,out var hit,1)?hit.point.y:4;
                float length=ceiling-start;
                Piece(lamp,"Ceiling suspension",new Vector3(0,.31f+length/2,0),new Vector3(.025f,length,.025f),iron);
                Piece(lamp,"Ceiling rose",new Vector3(0,ceiling-position.y-.015f,0),new Vector3(.14f,.03f,.14f),iron);
            }
            foreach(var position in new[]{new Vector3(-6.6f,2.15f,5.95f),new Vector3(3.35f,2.15f,5.95f)})
            {
                var lamp=Lantern(root,position,iron,brass,glass,true);
                Piece(lamp,"Sconce backplate",new Vector3(0,.02f,.25f),new Vector3(.14f,.36f,.04f),iron);
                Piece(lamp,"Sconce arm",new Vector3(0,.23f,.13f),new Vector3(.035f,.035f,.28f),brass);
            }
            RefreshLighting(root);
            SaloonSmokingArea.SetLayer(root,SaloonSmokingArea.IndoorLayer);
        }
        static void RefreshLighting(Transform root)
        {
            foreach(var light in root.GetComponentsInChildren<Light>(true))
            {
                if(light.type!=LightType.Point)continue;
                bool wall=light.name=="Wall lantern";
                light.color=new Color(1,.88f,.69f);
                light.intensity=wall?2.2f:3.0f;light.range=wall?6.2f:8.5f;
            }
            var fill=root.Find("Soft daylight fill");
            if(fill==null)
            {
                fill=new GameObject("Soft daylight fill").transform;fill.SetParent(root,false);
                fill.gameObject.AddComponent<Light>();
            }
            fill.rotation=Quaternion.Euler(48,-32,0);
            var daylight=fill.GetComponent<Light>();daylight.type=LightType.Directional;
            daylight.color=new Color(1,.92f,.80f);daylight.intensity=.5f;
            daylight.shadows=LightShadows.None;
            daylight.cullingMask=(1<<SaloonSmokingArea.IndoorLayer)|(1<<SaloonViewmodel.Layer);
        }

        static Transform Lantern(Transform parent,Vector3 position,Material iron,Material brass,Material glass,bool wall)
        {
            var root=new GameObject(wall?"Wall lantern":"Pendant lantern").transform;root.SetParent(parent,false);root.localPosition=position;
            // The luminous chamber and point light share a center. The open frame
            // casts no shadow on its own bulb, avoiding a bright ceiling/dark room.
            Piece(root,"Luminous glass chamber",Vector3.zero,new Vector3(.22f,.31f,.22f),glass);
            foreach(float x in new[]{-.135f,.135f})foreach(float z in new[]{-.135f,.135f})
                Piece(root,"Iron corner frame",new Vector3(x,0,z),new Vector3(.023f,.39f,.023f),iron);
            foreach(float y in new[]{-.19f,.19f})
                Piece(root,"Brass chamber rim",new Vector3(0,y,0),new Vector3(.31f,.045f,.31f),brass);
            Piece(root,"Lantern hood",new Vector3(0,.235f,0),new Vector3(.36f,.045f,.36f),iron);
            Piece(root,"Hood crown",new Vector3(0,.279f,0),new Vector3(.19f,.045f,.19f),iron);
            Piece(root,"Lantern base",new Vector3(0,-.235f,0),new Vector3(.27f,.045f,.27f),iron);
            var light=root.gameObject.AddComponent<Light>();light.type=LightType.Point;
            light.color=new Color(1f,.86f,.67f);light.intensity=wall?1.7f:2.35f;light.range=wall?5.2f:7.5f;
            light.shadows=!wall&&(position.z<0||Mathf.Abs(position.z)<1)?LightShadows.Soft:LightShadows.None;light.renderMode=LightRenderMode.Auto;
            light.cullingMask=1<<SaloonSmokingArea.IndoorLayer;
            return root;
        }
        static void Piece(Transform parent,string name,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=size;
            var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            var collider=go.GetComponent<Collider>();collider.enabled=false;Remove(collider);
        }
        static void Remove(Object item){if(Application.isPlaying)Object.Destroy(item);else Object.DestroyImmediate(item);}
    }
}
