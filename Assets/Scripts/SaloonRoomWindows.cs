using System.Linq;
using UnityEngine;

namespace Saloon
{
    // Retained for the separate training map's booth windows.
    public sealed class SaloonRoomWindow : MonoBehaviour
    {
        public int room,layoutVersion;
        public Renderer outside,inside;
        public BoxCollider barrier;
    }

    public static class SaloonRoomWindows
    {
        public const float FrontZ=-1.7f,Bottom=.08f,Top=2.85f,Width=3.8f,RoomHeight=4.1f;
        public const string RootName="Solid minigame room walls";
        const int LayoutVersion=6;
        public static void Apply()
        {
            var world=GameObject.Find("Saloon World");if(world==null)return;
            foreach(var old in world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Room screen"||t.name=="Room doorway lintel"||
                ((t.name=="Door visual"||t.name=="Room door")&&Mathf.Abs(t.position.z-FrontZ)<.8f&&Mathf.Abs(t.position.x)>3)).ToArray())
            {if(old==null)continue;old.gameObject.SetActive(false);Remove(old.gameObject);}
            foreach(var wallPart in world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Room back"||t.name=="Room side"))
            {
                var position=wallPart.position;position.y=RoomHeight/2;wallPart.position=position;
                var scale=wallPart.localScale;scale.y=RoomHeight;wallPart.localScale=scale;
            }
            SealRoomPerimeter(world);
            SaloonExteriorWindows.Apply(world);
            var glass=world.transform.Find("One-way room windows");
            if(glass!=null){glass.gameObject.SetActive(false);Remove(glass.gameObject);}
            var existing=world.transform.Find(RootName);
            if(existing!=null)
            {
                var panes=existing.GetComponentsInChildren<SaloonRoomWall>(true);
                if(panes.Length==2&&panes.All(p=>p.layoutVersion==LayoutVersion))return;
                existing.name="Retired room wall layout";existing.gameObject.SetActive(false);Remove(existing.gameObject);
            }
            var wall=world.transform.Find("Room back").GetComponent<Renderer>().sharedMaterial;
            var root=new GameObject(RootName).transform;root.SetParent(world.transform,false);
            var interior=world.transform.Find("Western interior");
            var timber=interior.GetComponentsInChildren<Renderer>().First(r=>r.sharedMaterial.name.Contains("Walnut joinery")).sharedMaterial;
            for(int room=1;room<=2;room++)
            {
                float x=room==1?-5:5;
                var panel=new GameObject(SaloonStations.Name(room)+" solid plaster wall");panel.transform.SetParent(root,false);
                panel.transform.localPosition=new Vector3(x,RoomHeight/2,FrontZ);
                var boundary=panel.AddComponent<SaloonRoomWall>();boundary.room=room;boundary.layoutVersion=LayoutVersion;
                boundary.barrier=panel.AddComponent<BoxCollider>();boundary.barrier.size=new Vector3(4.18f,RoomHeight,.18f);
                var face=Decoration("Continuous plaster",panel.transform.position,new Vector3(4.18f,RoomHeight,.18f),wall);
                boundary.surface=face.GetComponent<Renderer>();
                foreach(float side in new[]{-1f,1f})
                {
                    Decoration("Front wall walnut dado",new Vector3(x,.52f,FrontZ+side*.12f),new Vector3(4.18f,1.04f,.045f),timber);
                    Decoration("Front wall chair rail",new Vector3(x,1.09f,FrontZ+side*.14f),new Vector3(4.18f,.075f,.09f),timber);
                }
            }
            SaloonSmokingArea.SetLayer(root,SaloonSmokingArea.IndoorLayer);
            GameObject Decoration(string name,Vector3 position,Vector3 size,Material material)
            {
                var go=SaloonWorld.Box(name,position,size,material,root);
                var collider=go.GetComponent<Collider>();collider.enabled=false;Remove(collider);return go;
            }
        }
        static void SealRoomPerimeter(GameObject world)
        {
            // Upgrade saved worlds too: no corridor beside or behind either room.
            if(world.transform.Find("Room perimeter infill")!=null)return;
            var root=new GameObject("Room perimeter infill").transform;root.SetParent(world.transform,false);
            var wall=world.transform.Find("Room back").GetComponent<Renderer>().sharedMaterial;
            foreach(float side in new[]{-1f,1f})
            {
                SaloonWorld.Box("Room outer wall infill",new Vector3(side*7.5f,RoomHeight/2,-4.35f),
                    new Vector3(1,RoomHeight,5.3f),wall,root);
                SaloonWorld.Box("Room rear wall infill",new Vector3(side*5,RoomHeight/2,-6.35f),
                    new Vector3(4,RoomHeight,1.3f),wall,root);
            }
            SaloonSmokingArea.SetLayer(root,SaloonSmokingArea.IndoorLayer);
        }
        static void Remove(Object obj){if(Application.isPlaying)Object.Destroy(obj);else Object.DestroyImmediate(obj);}
        public static Vector3 ClipShotTrail(Vector3 origin,Vector3 direction,float distance)
        {
            var end=origin+direction*distance;
            foreach(var hit in Physics.RaycastAll(origin,direction,distance,~0,QueryTriggerInteraction.Ignore))
            {
                if((hit.collider.GetComponent<SaloonRoomWall>()==null&&hit.collider.GetComponent<SaloonRoomWindow>()==null&&hit.collider.name!=SaloonExteriorWindows.PaneName)||hit.distance>=distance)continue;
                distance=hit.distance;end=hit.point;
            }
            return end;
        }
    }
}
