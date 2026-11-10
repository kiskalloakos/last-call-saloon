using System.Linq;
using UnityEngine;

namespace Saloon
{
    public static class SaloonTableFurniture
    {
        public const float Surface=.803f;
        public static Transform Build(int room,string name,Material oak,Material rail,Material felt,Material brass)
        {
            float center=room==1?-5:5;var world=GameObject.Find("Saloon World");
            if(world!=null)
            {
                foreach(var table in world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="tableRound"&&Mathf.Abs(t.position.x-center)<.2f))
                {
                    foreach(var r in table.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                    foreach(var c in table.GetComponentsInChildren<Collider>(true))c.enabled=false;
                }
                int first=10+room*2;
                foreach(var chair in world.GetComponentsInChildren<InteractionPoint>(true))
                    if(chair.kind==InteractionKind.Seat&&(chair.index==first||chair.index==first+1))
                    {
                        int slot=chair.index-first;chair.transform.position=SaloonStations.Position(false,room,slot)-Vector3.up*.08f;
                        chair.sitYaw=SaloonStations.Facing(false,room,slot);chair.transform.rotation=Quaternion.Euler(0,chair.sitYaw,0);
                    }
                foreach(var prop in world.GetComponentsInChildren<Transform>(true).Where(t=>t.name.EndsWith(" visual")&&(t.name.StartsWith("Mug")||t.name.StartsWith("Plate"))&&Vector3.Distance(t.position,new Vector3(center,.76f,-3.8f))<.65f))prop.gameObject.SetActive(false);
            }
            var root=new GameObject(name).transform;root.position=new Vector3(center,0,-3.8f);
            Box("Walnut table",root,new Vector3(0,.715f,0),new Vector3(1.46f,.10f,1.77f),oak);
            Box("Leather padded rail",root,new Vector3(0,.768f,0),new Vector3(1.44f,.035f,1.75f),rail);
            Box("Brass baize border",root,new Vector3(0,.787f,0),new Vector3(1.34f,.008f,1.65f),brass);
            Box("Playing felt",root,new Vector3(0,.793f,0),new Vector3(1.32f,.008f,1.63f),felt);
            foreach(float x in new[]{-.5f,.5f})foreach(float z in new[]{-.66f,.66f})Box("Turned table leg",root,new Vector3(x,.35f,z),new Vector3(.095f,.7f,.095f),oak);
            var solid=root.gameObject.AddComponent<BoxCollider>();solid.center=new Vector3(0,.73f,0);solid.size=new Vector3(1.46f,.13f,1.77f);
            SaloonSmokingArea.SetLayer(root,SaloonSmokingArea.IndoorLayer);return root;
        }
        public static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;
            var collider=go.GetComponent<Collider>();collider.enabled=false;
            if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);
            go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
    }
}
