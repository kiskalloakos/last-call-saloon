using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Saloon
{
    // A separate runtime map. No normal-match geometry is edited or reused as its shell.
    public sealed class SaloonTrainingMap : IDisposable
    {
        public readonly GameObject Root;
        public Collider SafeCollider { get; private set; }
        public Light ActionLight { get; private set; }
        public Transform SmokingDeck { get; private set; }
        public Vector3 FocusPoint { get; private set; }
        public static readonly Vector3 Start=new Vector3(0,.08f,-5.5f);
        public static readonly Vector3 Target=new Vector3(0,.08f,-7.5f);
        public static readonly Vector3 Deck=new Vector3(5.8f,.08f,6.8f);
        public static readonly Vector3 Exit=new Vector3(0,.08f,-8.1f);
        readonly List<GameObject> hidden=new List<GameObject>();
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly Color ambient;
        readonly AmbientMode ambientMode;
        readonly bool fog;
        readonly Camera camera;
        readonly Color background;

        public SaloonTrainingMap(Camera viewCamera)
        {
            camera=viewCamera;background=camera.backgroundColor;camera.backgroundColor=new Color(.035f,.045f,.055f);
            ambient=RenderSettings.ambientLight;ambientMode=RenderSettings.ambientMode;fog=RenderSettings.fog;
            foreach(string name in new[]{"Saloon World","have a smoke · sunset deck","Saloon entrance sunset"})
            {var old=GameObject.Find(name);if(old!=null){hidden.Add(old);old.SetActive(false);}}
            Root=new GameObject("Last Call · Training Saloon");
            SmokingDeck=new GameObject("Training sunset deck").transform;SmokingDeck.SetParent(Root.transform);
            SmokingDeck.position=new Vector3(5.8f,0,6.8f);
            var oak=Mat("Training honey oak",new Color(.49f,.31f,.17f));
            oak.mainTexture=Resources.Load<Texture2D>("Art/LowPoly/Textures/WoodPlanks");
            var cream=Mat("Training ivory plaster",new Color(.84f,.79f,.65f));
            var teal=Mat("Training teal",new Color(.09f,.29f,.27f));
            var walnut=Mat("Training walnut",new Color(.19f,.11f,.075f));
            var gold=Mat("Training brass",new Color(.92f,.62f,.23f));
            Box("Training saloon floor",new Vector3(0,-.08f,-1.3f),new Vector3(18,.16f,16),oak);
            foreach(float x in new[]{-8.8f,8.8f})
                Box("Outer saloon wall",new Vector3(x,1.7f,x>0?-2.4f:-1.3f),new Vector3(.18f,3.4f,x>0?13.8f:16),cream);
            Box("Back wall",new Vector3(-2.8f,1.7f,5.25f),new Vector3(11.8f,3.4f,.18f),cream);
            Box("Entry wall left",new Vector3(-5.1f,1.7f,-9.15f),new Vector3(7.4f,3.4f,.18f),teal);
            Box("Entry wall right",new Vector3(5.1f,1.7f,-9.15f),new Vector3(7.4f,3.4f,.18f),teal);
            foreach(float x in new[]{-8.7f,-2.75f,2.75f,8.7f})
                Box("Heavy timber post",new Vector3(x,1.85f,-1.2f),new Vector3(.22f,3.7f,.22f),walnut);
            foreach(float z in new[]{-8.9f,-1.2f,5.1f})
                Box("Ceiling cross beam",new Vector3(0,3.5f,z),new Vector3(17.6f,.25f,.22f),walnut);
            Box("Saloon roof",new Vector3(0,4.35f,-2.4f),new Vector3(18.2f,.16f,14),walnut);
            Box("Back bar roof",new Vector3(-2.85f,4.35f,5.6f),new Vector3(11.9f,.16f,2.2f),walnut);
            var counter=Box("Shared bar counter",new Vector3(-2, .65f,3.8f),new Vector3(9,1.3f,.8f),teal);
            counter.GetComponent<Renderer>().enabled=false;
            var bar=SaloonArt.Model("Bar",Root.transform,new Vector3(9.2f,1.43f,1.1f));bar.transform.position=new Vector3(-2,0,3.8f);
            var shelf=SaloonArt.Model("Shelf",Root.transform,new Vector3(5.2f,2.3f,.45f));shelf.transform.position=new Vector3(-1.8f,0,4.95f);
            Box("Bar brass rail",new Vector3(-2,.3f,3.25f),new Vector3(8.8f,.05f,.05f),gold);
            var drawer=SaloonArt.BuildCashDrawer(Root.transform);SafeCollider=drawer.GetComponent<Collider>();
            Box("Range backstop",new Vector3(0,1.25f,-8.65f),new Vector3(3,2.5f,.22f),teal);
            foreach(int room in new[]{1,2})
            {
                float x=room==1?-5:5;
                Box("Game booth rear",new Vector3(x,1.65f,-5.65f),new Vector3(4,3.3f,.16f),cream);
                Box("Game booth outer wall",new Vector3(room==1?-7:7,1.65f,-3.7f),new Vector3(.16f,3.3f,4),cream);
                Window(room,x,walnut,teal);
                foreach(float seatX in new[]{x-1.04f,x+1.04f})Chair(new Vector3(seatX,0,-3.8f),seatX<x?90:270,oak,teal);
            }
            // Both booths open onto the central aisle. The deck has its own connected doorway.
            var cedar=Mat("Sunset deck cedar",new Color(.38f,.24f,.15f));
            var posts=Mat("Weathered deck posts",new Color(.21f,.14f,.11f));
            OutdoorBox("Connected smoking veranda",new Vector3(5.5f,-.08f,7),new Vector3(7,.16f,5),posts);
            for(int i=0;i<14;i++)OutdoorBox("Weathered cedar plank",new Vector3(5.5f,.019f,4.64f+i*.36f),new Vector3(6.95f,.025f,.345f),cedar,false);
            OutdoorBox("Saloon side privacy wall",new Vector3(3.1f,1.7f,6.8f),new Vector3(.18f,3.4f,4.4f),cedar);
            foreach(float y in new[]{.4f,.78f})
            {
                OutdoorBox("Sunset facing rail",new Vector3(8.8f,y,6.8f),new Vector3(.11f,.11f,5),cedar);
                OutdoorBox("Open overlook rail",new Vector3(5.9f,y,9.3f),new Vector3(5.7f,.11f,.11f),cedar);
            }
            foreach(float z in new[]{4.5f,6.8f,9.3f})OutdoorBox("Overlook post",new Vector3(8.8f,.52f,z),new Vector3(.15f,1.04f,.15f),posts);
            foreach(float x in new[]{3.2f,5.9f})OutdoorBox("Overlook post",new Vector3(x,.52f,9.3f),new Vector3(.15f,1.04f,.15f),posts);
            OutdoorBox("Quiet cedar bench",new Vector3(3.8f,.46f,7),new Vector3(.52f,.12f,2.8f),cedar);
            foreach(float z in new[]{5.95f,8.05f})OutdoorBox("Bench support",new Vector3(3.8f,.2f,z),new Vector3(.4f,.4f,.15f),posts);
            SaloonSunset.Build(SmokingDeck);OwnMaterials(SmokingDeck);SaloonSurfaces.Apply(SmokingDeck);
            SaloonSmokingArea.SetLayer(SmokingDeck,SaloonSmokingArea.OutdoorLayer);
            // Only generated materials/meshes belong to this runtime map; imported assets remain shared.
            OwnMaterials(SmokingDeck);
            foreach(var filter in SmokingDeck.GetComponentsInChildren<MeshFilter>())
                if(filter.sharedMesh.name=="Wind-carved sandstone"||filter.gameObject.name=="Desert fading into the horizon")meshes.Add(filter.sharedMesh);
            // A little cool ambient keeps obstacles readable. Only the current action gets a lamp.
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.12f,.14f,.17f);RenderSettings.fog=false;
            var lamp=new GameObject("Current action spotlight");lamp.transform.SetParent(Root.transform);
            ActionLight=lamp.AddComponent<Light>();ActionLight.type=LightType.Spot;
            ActionLight.color=new Color(1,.81f,.51f);ActionLight.intensity=5;
            ActionLight.range=8;ActionLight.spotAngle=62;ActionLight.innerSpotAngle=36;
            ActionLight.renderMode=LightRenderMode.ForcePixel;ActionLight.shadows=LightShadows.Soft;
        }

        Material Mat(string name,Color color)
        {var mat=new Material(Shader.Find("Standard")){name=name,color=color};mat.SetFloat("_Glossiness",.1f);materials.Add(mat);return mat;}
        void OwnMaterials(Transform root)
        {
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
                foreach(var mat in renderer.sharedMaterials)if(mat!=null&&!materials.Contains(mat))materials.Add(mat);
        }
        GameObject Box(string name,Vector3 position,Vector3 size,Material mat,bool solid=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(Root.transform,false);
            go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
            if(!solid){var c=go.GetComponent<Collider>();c.enabled=false;UnityEngine.Object.Destroy(c);}return go;
        }
        GameObject OutdoorBox(string name,Vector3 position,Vector3 size,Material mat,bool solid=true)
        {var go=Box(name,position,size,mat,solid);go.transform.SetParent(SmokingDeck,true);return go;}
        void Chair(Vector3 position,float yaw,Material wood,Material fabric)
        {
            var root=new GameObject("Training table chair").transform;root.SetParent(Root.transform);root.position=position;root.rotation=Quaternion.Euler(0,yaw,0);
            var seat=Box("Chair seat",Vector3.zero,new Vector3(.48f,.1f,.48f),fabric,false);seat.transform.SetParent(root,false);seat.transform.localPosition=Vector3.up*.46f;
            var back=Box("Chair back",Vector3.zero,new Vector3(.48f,.55f,.09f),wood,false);back.transform.SetParent(root,false);back.transform.localPosition=new Vector3(0,.75f,-.22f);
            foreach(float x in new[]{-.18f,.18f})foreach(float z in new[]{-.18f,.18f})
            {var leg=Box("Chair leg",Vector3.zero,new Vector3(.06f,.45f,.06f),wood,false);leg.transform.SetParent(root,false);leg.transform.localPosition=new Vector3(x,.225f,z);}
        }
        void Window(int room,float x,Material frame,Material inside)
        {
            const float z=SaloonRoomWindows.FrontZ;
            foreach(float edge in new[]{x-1.95f,x+1.95f})Box("Training glass frame",new Vector3(edge,1.3f,z),new Vector3(.1f,2.6f,.16f),frame);
            foreach(float y in new[]{.08f,2.6f})Box("Training glass frame",new Vector3(x,y,z),new Vector3(4,.1f,.16f),frame);
            var glass=new GameObject("Training privacy window");glass.transform.SetParent(Root.transform);glass.transform.position=new Vector3(x,1.34f,z);
            var window=glass.AddComponent<SaloonRoomWindow>();window.room=room;
            window.barrier=glass.AddComponent<BoxCollider>();window.barrier.size=new Vector3(3.8f,2.5f,.12f);
            Renderer Face(string name,float offset,float yaw,Material mat)
            {
                var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name=name;quad.transform.SetParent(glass.transform,false);
                quad.transform.localPosition=Vector3.forward*offset;quad.transform.localRotation=Quaternion.Euler(0,yaw,0);quad.transform.localScale=new Vector3(3.8f,2.5f,1);
                var c=quad.GetComponent<Collider>();c.enabled=false;UnityEngine.Object.Destroy(c);var r=quad.GetComponent<Renderer>();r.sharedMaterial=mat;return r;
            }
            window.outside=Face("Bar cannot see room",.065f,180,inside);window.inside=Face("Room cannot see Bar",-.065f,0,inside);
        }
        public void Focus(Vector3 point,Vector3 viewer,bool outdoors)
        {
            FocusPoint=point;
            ActionLight.enabled=!outdoors;
            // Stay below the roof beams; offset toward the player to light the object's front.
            var toward=Vector3.ProjectOnPlane(viewer-point,Vector3.up).normalized;
            ActionLight.transform.position=new Vector3(point.x,3.18f,point.z)+toward*(point.y>.5f?1.25f:0);
            ActionLight.transform.rotation=Quaternion.LookRotation(point-ActionLight.transform.position);
        }
        public static Vector3 Clamp(Vector3 point)
        {point.x=Mathf.Clamp(point.x,-8.45f,8.45f);point.z=Mathf.Clamp(point.z,-8.8f,9);return point;}
        public void Dispose()
        {
            Root.SetActive(false);UnityEngine.Object.Destroy(Root);foreach(var mat in materials)UnityEngine.Object.Destroy(mat);
            foreach(var mesh in meshes)UnityEngine.Object.Destroy(mesh);
            foreach(var old in hidden)if(old!=null)old.SetActive(true);
            RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=ambientMode;RenderSettings.fog=fog;
            if(camera!=null)camera.backgroundColor=background;
        }
    }
}
