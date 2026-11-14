using UnityEngine;
using UnityEngine.Rendering;

namespace Saloon
{
    public static class SaloonLighting
    {
        public const string RootName="Lantern lighting v2";
        public static void Apply(GameObject world)
        {
            // Saved prototype scenes contain the original box lamps and floating lights.
            foreach(var child in world.GetComponentsInChildren<Transform>(true))
                if(child.name=="Hanging lantern"||child.name=="Lantern stem"||child.name=="Warm lamplight")
                {child.gameObject.SetActive(false);Remove(child.gameObject);}
            var sun=world.transform.Find("Evening sun");
            if(sun!=null){var light=sun.GetComponent<Light>();if(light!=null)light.enabled=false;}
            // A neutral bounced-light approximation keeps faces and dark timber readable.
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.53f,.55f,.59f);
            RenderSettings.ambientEquatorColor=new Color(.43f,.40f,.35f);
            RenderSettings.ambientGroundColor=new Color(.28f,.25f,.22f);
            RenderSettings.fogDensity=.0015f;
            if(world.transform.Find(RootName)!=null)return;
            var root=new GameObject(RootName).transform;root.SetParent(world.transform,false);
            var iron=SaloonWorld.Material("Lantern blackened iron",new Color(.095f,.075f,.055f),.32f);iron.SetFloat("_Metallic",.6f);
            var brass=SaloonWorld.Material("Lantern burnished brass",new Color(.46f,.30f,.13f),.4f);brass.SetFloat("_Metallic",.65f);
            var glass=SaloonWorld.Material("Lantern glowing amber glass",new Color(.95f,.73f,.40f),.35f);
            glass.EnableKeyword("_EMISSION");glass.SetColor("_EmissionColor",new Color(1f,.69f,.30f)*.85f);
            foreach(var position in new[]{new Vector3(-3,2.85f,0),new Vector3(3,2.85f,-3),new Vector3(-2,2.85f,5),new Vector3(6.5f,2.85f,4)})
            {
                var lamp=Lantern(root,position,iron,brass,glass,false);
                Piece(lamp,"Ceiling suspension",new Vector3(0,.57f,0),new Vector3(.025f,.65f,.025f),iron);
            }
            foreach(var position in new[]{new Vector3(-6.6f,2.15f,5.95f),new Vector3(3.35f,2.15f,5.95f),new Vector3(-3.62f,2.05f,-5.22f),new Vector3(6.38f,2.05f,-5.22f)})
            {
                var lamp=Lantern(root,position,iron,brass,glass,true);
                Piece(lamp,"Sconce backplate",new Vector3(0,.02f,.25f),new Vector3(.14f,.36f,.04f),iron);
                Piece(lamp,"Sconce arm",new Vector3(0,.23f,.13f),new Vector3(.035f,.035f,.28f),brass);
            }
            SaloonSmokingArea.SetLayer(root,SaloonSmokingArea.IndoorLayer);
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
            light.shadows=!wall&&Mathf.Abs(position.z)<.01f?LightShadows.Soft:LightShadows.None;light.renderMode=LightRenderMode.Auto;
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
