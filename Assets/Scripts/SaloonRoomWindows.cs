using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Saloon
{
    // Two independently culled faces give a fixed viewing direction on every client.
    // The solid collider is shared by both faces and never depends on the viewer.
    public sealed class SaloonRoomWindow : MonoBehaviour
    {
        public int room,layoutVersion;
        public Renderer outside,inside;
        public BoxCollider barrier;
    }

    public static class SaloonRoomWindows
    {
        public const float FrontZ=-1.7f,Bottom=.08f,Top=2.85f,Width=3.8f,RoomHeight=4.1f;
        const int LayoutVersion=3;
        public static void Apply()
        {
            var world=GameObject.Find("Saloon World");if(world==null)return;
            var original=world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Room screen").ToArray();
            var wall=original.FirstOrDefault()?.GetComponent<Renderer>()?.sharedMaterial;
            foreach(var old in world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Room screen"||t.name=="Room doorway lintel"||
                ((t.name=="Door visual"||t.name=="Room door")&&Mathf.Abs(t.position.z-FrontZ)<.8f&&Mathf.Abs(t.position.x)>3)).ToArray())
            {if(old==null)continue;old.gameObject.SetActive(false);Remove(old.gameObject);}
            foreach(var wallPart in world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Room back"||t.name=="Room side"))
            {
                var position=wallPart.position;position.y=RoomHeight/2;wallPart.position=position;
                var scale=wallPart.localScale;scale.y=RoomHeight;wallPart.localScale=scale;
            }
            var existing=world.transform.Find("One-way room windows");
            if(existing!=null)
            {
                var panes=existing.GetComponentsInChildren<SaloonRoomWindow>(true);
                if(panes.Length==2&&panes.All(p=>p.layoutVersion==LayoutVersion))return;
                existing.name="Retired room window layout";existing.gameObject.SetActive(false);Remove(existing.gameObject);
            }
            if(wall==null)wall=SaloonWorld.Material("Room window plaster",new Color(.70f,.61f,.47f));
            var root=new GameObject("One-way room windows").transform;root.SetParent(world.transform,false);
            var timber=SaloonWorld.Material("Window walnut frame",new Color(.24f,.135f,.075f),.25f);
            timber.mainTexture=Resources.Load<Texture2D>("Art/LowPoly/Textures/WoodPlanks");
            var brass=SaloonWorld.Material("Window brass trim",new Color(.59f,.42f,.19f),.6f);
            var glassShader=Resources.Load<Shader>("Shaders/SaloonWindowGlass");
            if(glassShader==null)throw new System.InvalidOperationException("Missing room window glass shader.");
            var clear=new Material(glassShader){name="Glass · outside sees into room",color=new Color(.30f,.48f,.49f,.10f)};
            var privacy=SaloonWorld.Material("Glass · opaque from room interior",new Color(.10f,.17f,.18f),.94f);
            privacy.SetFloat("_Metallic",.65f);
            for(int room=1;room<=2;room++)
            {
                float x=room==1?-5:5;
                SaloonWorld.Box("Window lower wall",new Vector3(x,Bottom/2,FrontZ),new Vector3(4,Bottom,.18f),wall,root);
                SaloonWorld.Box("Window upper wall",new Vector3(x,(Top+RoomHeight)/2,FrontZ),new Vector3(4,RoomHeight-Top,.18f),wall,root);
                foreach(float edge in new[]{x-1.95f,x+1.95f})
                    SaloonWorld.Box("Window wall post",new Vector3(edge,(Bottom+Top)/2,FrontZ),new Vector3(.1f,Top-Bottom,.18f),wall,root);
                foreach(float edge in new[]{x-Width/2,x+Width/2})
                    SaloonWorld.Box("Walnut window upright",new Vector3(edge,(Bottom+Top)/2,FrontZ),new Vector3(.075f,Top-Bottom+.10f,.24f),timber,root);
                foreach(float height in new[]{Bottom,Top})
                {
                    SaloonWorld.Box("Walnut window crosspiece",new Vector3(x,height,FrontZ),new Vector3(Width+.10f,.075f,.24f),timber,root);
                    SaloonWorld.Box("Window brass beading",new Vector3(x,height,FrontZ+.129f),new Vector3(Width,.012f,.014f),brass,root);
                }
                var panel=new GameObject(SaloonStations.Name(room)+" one-way glass");panel.transform.SetParent(root,false);panel.transform.position=new Vector3(x,(Bottom+Top)/2,FrontZ);
                var window=panel.AddComponent<SaloonRoomWindow>();window.room=room;window.layoutVersion=LayoutVersion;
                window.barrier=panel.AddComponent<BoxCollider>();window.barrier.size=new Vector3(Width,Top-Bottom,.12f);window.barrier.isTrigger=false;
                window.outside=Face(panel.transform,"Outside · transparent",clear,.062f,180);
                window.inside=Face(panel.transform,"Inside · privacy glass",privacy,-.062f,0);
            }
            SaloonSmokingArea.SetLayer(root,SaloonSmokingArea.IndoorLayer);
        }
        static void Remove(Object obj){if(Application.isPlaying)Object.Destroy(obj);else Object.DestroyImmediate(obj);}
        static Renderer Face(Transform parent,string name,Material material,float z,float yaw)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=new Vector3(0,0,z);go.transform.localScale=new Vector3(Width,Top-Bottom,1);go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var collider=go.GetComponent<Collider>();collider.enabled=false;
            if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);
            var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return renderer;
        }
        public static Vector3 ClipShotTrail(Vector3 origin,Vector3 direction,float distance)
        {
            var end=origin+direction*distance;
            foreach(var hit in Physics.RaycastAll(origin,direction,distance,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.collider.GetComponent<SaloonRoomWindow>()==null||hit.distance>=distance)continue;
                distance=hit.distance;end=hit.point;
            }
            return end;
        }
    }
}
