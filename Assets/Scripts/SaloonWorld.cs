using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Saloon
{
    public enum InteractionKind { Seat, Beer }
    public static class SaloonWorld
    {
        public static readonly Vector3[] Spawns = {
            new Vector3(-3, .08f, -3), new Vector3(3, .08f, -3), new Vector3(-5, .08f, 0),
            new Vector3(2, .08f, 0), new Vector3(6.5f, .08f, 3)
        };
        public static readonly Color[] PlayerColors = {
            new Color(.75f,.35f,.20f), new Color(.25f,.51f,.65f), new Color(.66f,.57f,.29f),
            new Color(.40f,.57f,.38f), new Color(.62f,.39f,.57f)
        };
        public static readonly List<Material> GeneratedMaterials = new List<Material>();
        static Transform root;
        static Material wood, dark, trim, brass, plaster, glass;

        public static Material Material(string name, Color color, float smoothness = .15f)
        {
            var mat = new Material(Shader.Find("Standard")) { name = name, color = color };
            mat.SetFloat("_Glossiness", smoothness);
            GeneratedMaterials.Add(mat);
            return mat;
        }

        public static GameObject Build()
        {
            var old = GameObject.Find("Saloon World");
            if (old != null) return old;
            GeneratedMaterials.Clear();
            root = new GameObject("Saloon World").transform;
            wood = Material("Honey oak", new Color(.40f,.235f,.115f));
            dark = Material("Dark walnut", new Color(.135f,.077f,.046f));
            trim = Material("Worn red trim", new Color(.30f,.075f,.046f));
            brass = Material("Aged brass", new Color(.66f,.44f,.16f), .55f);
            plaster = Material("Warm plaster", new Color(.66f,.52f,.34f));
            glass = Material("Bottle green", new Color(.12f,.24f,.14f), .7f);
            var sand = Material("Desert sand", new Color(.49f,.36f,.23f));
            Box("Desert", new Vector3(0,-.35f,0), new Vector3(120,.3f,120), sand);
            Box("Foundation", new Vector3(0,-.13f,0), new Vector3(16,.25f,14), dark);
            for (int i=0;i<32;i++)
                Box("Floorboard", new Vector3(-7.75f+i*.5f,0,0), new Vector3(.48f,.12f,14), wood);
            Box("Back wall", new Vector3(0,2,7), new Vector3(16,4,.22f), plaster);
            Box("West wall", new Vector3(-8,2,0), new Vector3(.22f,4,14), plaster);
            Box("East wall", new Vector3(8,2,0), new Vector3(.22f,4,14), plaster);
            Box("Entrance left", new Vector3(-4.75f,2,-7), new Vector3(6.5f,4,.22f), plaster);
            Box("Entrance right", new Vector3(4.75f,2,-7), new Vector3(6.5f,4,.22f), plaster);
            Box("Entrance lintel", new Vector3(0,3.6f,-7), new Vector3(3, .8f,.25f), dark);
            Box("Ceiling", new Vector3(0,4.1f,0), new Vector3(16,.2f,14), dark);
            for (int i=0;i<5;i++)
                Box("Ceiling beam",new Vector3(-6+i*3,3.85f,0),new Vector3(.22f,.3f,14),wood);
            foreach(float x in new[]{-7.8f,7.8f})
            {
                Box("Wainscot",new Vector3(x,.6f,0),new Vector3(.18f,1.1f,14),dark);
                Box("Chair rail",new Vector3(x,1.2f,0),new Vector3(.23f,.09f,14),brass);
            }
            Box("Back wainscot",new Vector3(0,.6f,6.8f),new Vector3(16,1.1f,.15f),dark);
            Box("Porch",new Vector3(0,-.06f,-8.5f),new Vector3(8,.15f,3),wood);
            Box("Porch roof",new Vector3(0,3.3f,-8.5f),new Vector3(8,.15f,3),dark);
            foreach(float x in new[]{-3.7f,3.7f}) Box("Porch post",new Vector3(x,1.6f,-9.7f),new Vector3(.18f,3.2f,.18f),wood);
            // Two conversation rooms, each open toward the saloon and screened from the bar.
            foreach(float center in new[]{-5f,5f})
            {
                Box("Room back",new Vector3(center,1.8f,-5.7f),new Vector3(4,3.6f,.18f),dark);
                Box("Room side",new Vector3(center-2,1.8f,-3.7f),new Vector3(.18f,3.6f,4),dark);
                Box("Room side",new Vector3(center+2,1.8f,-3.7f),new Vector3(.18f,3.6f,4),dark);
                Box("Room screen",new Vector3(center-.6f,1.8f,-1.7f),new Vector3(2.8f,3.6f,.18f),dark);
                Box("Room doorway lintel",new Vector3(center+1.4f,3.25f,-1.7f),new Vector3(1.2f,.7f,.18f),dark);
            }

            Box("Bar front",new Vector3(-1.8f,.65f,3.8f),new Vector3(8.5f,1.3f,1),dark);
            Box("Bar top",new Vector3(-1.8f,1.35f,3.8f),new Vector3(9,.16f,1.4f),wood);
            Box("Brass foot rail",new Vector3(-1.8f,.25f,2.98f),new Vector3(8.4f,.055f,.055f),brass);
            for(int i=0;i<6;i++) Box("Bar panel",new Vector3(-5.3f+i*1.4f,.7f,3.28f),new Vector3(1.18f,.9f,.04f),trim);
            Prop("Art/bookcaseOpen", new Vector3(-4,0,6.65f),1.95f,180,wood,true);
            Prop("Art/bookcaseOpen", new Vector3(-1.3f,0,6.65f),1.95f,180,wood,true);
            for(int shelf=0;shelf<3;shelf++) for(int i=0;i<13;i++)
                Bottle(new Vector3(-5.5f+i*.38f,.55f+shelf*.57f,6.22f));
            var tap = Box("Shared beer supply",new Vector3(1.6f,1.6f,3.7f),new Vector3(.32f,.4f,.32f),brass);
            var beer = tap.AddComponent<InteractionPoint>(); beer.kind=InteractionKind.Beer;
            for(int i=0;i<5;i++) Seat(new Vector3(-5+i*1.5f,0,2.05f),0,i,true);
            Prop("Art/tableRound",new Vector3(0,0,-.5f),.75f,0,wood,true);
            for(int i=0;i<5;i++)Seat(SaloonStations.Position(true,0,i)-Vector3.up*.08f,SaloonStations.Facing(true,0,i),5+i,false);
            foreach(int room in new[]{1,2})
            {
                var center=new Vector3(room==1?-5:5,0,-3.8f);Prop("Art/tableRound",center,.70f,0,wood,true);
                for(int slot=0;slot<2;slot++)Seat(SaloonStations.Position(false,room,slot)-Vector3.up*.08f,SaloonStations.Facing(false,room,slot),10+room*2+slot,false);
            }
            Prop("Art/WineBarrel",new Vector3(-6.8f,.06f,5.5f),1.15f,0,BarrelMaterial(),true);
            Prop("Art/WineBarrel",new Vector3(6.7f,.06f,-5.8f),1.15f,25,BarrelMaterial(),true);
            foreach(var pos in new[]{new Vector3(-3,3.1f,0),new Vector3(3,3.1f,-3),new Vector3(-2,3.1f,5),new Vector3(6.5f,3.1f,4)})
            {
                Box("Hanging lantern",pos,new Vector3(.4f,.25f,.4f),brass);
                Box("Lantern stem",pos+Vector3.up*.35f,new Vector3(.04f,.7f,.04f),dark);
                var lamp=new GameObject("Warm lamplight");lamp.transform.SetParent(root);lamp.transform.position=pos-Vector3.up*.2f;
                var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=10;light.intensity=2.4f;light.color=new Color(1,.67f,.35f);
            }
            var sun=new GameObject("Evening sun");sun.transform.SetParent(root);sun.transform.rotation=Quaternion.Euler(35,-35,0);
            var directional=sun.AddComponent<Light>();directional.type=LightType.Directional;directional.intensity=1.15f;directional.color=new Color(1,.76f,.51f);directional.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.33f,.28f,.23f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.39f,.29f,.20f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.008f;
            return root.gameObject;
        }

        static Material BarrelMaterial()
        {
            var mat=Material("Poly Haven barrel",Color.white);
            mat.mainTexture=Resources.Load<Texture2D>("Art/BarrelDiffuse");return mat;
        }

        static void Seat(Vector3 pos,float yaw,int index,bool bar)
        {
            var go=Prop(bar?"Art/stoolBar":"Art/chair",pos,bar?.86f:.95f,yaw,wood,true);
            var seat=go.AddComponent<InteractionPoint>();seat.kind=InteractionKind.Seat;seat.index=index;
            seat.sitPosition=pos+Vector3.up*(bar?.17f:-.10f);seat.sitYaw=yaw;
        }

        static void Bottle(Vector3 position)
        {
            Cylinder("Bottle",position+Vector3.up*.12f,new Vector3(.10f,.12f,.10f),glass);
            Cylinder("Bottle neck",position+Vector3.up*.27f,new Vector3(.045f,.055f,.045f),glass);
        }

        public static GameObject Prop(string resource,Vector3 position,float height,float yaw,Material material,bool collider)
        {
            var prefab=Resources.Load<GameObject>(resource);
            if(prefab==null) throw new System.InvalidOperationException("Missing imported model: "+resource);
            var holder=new GameObject(prefab.name);if(root!=null)holder.transform.SetParent(root);
            var model=Object.Instantiate(prefab,holder.transform);
            model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
            var renderers=model.GetComponentsInChildren<Renderer>();
            var bounds=new Bounds();bool first=true;
            foreach(var r in renderers)
            {
                if(r is SkinnedMeshRenderer skin)
                {
                    // FBX skinned bounds can include the armature import scale twice.
                    foreach(var vertex in skin.sharedMesh.vertices)
                    {
                        var world=skin.transform.TransformPoint(vertex);
                        if(first){bounds=new Bounds(world,Vector3.zero);first=false;}else bounds.Encapsulate(world);
                    }
                    skin.updateWhenOffscreen=true;
                }
                else {if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
                r.sharedMaterial=material;
            }
            float scale=height/Mathf.Max(.01f,bounds.size.y);
            model.transform.localScale*=scale;
            model.transform.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
            if(collider)
            {
                var box=holder.AddComponent<BoxCollider>();box.center=new Vector3(0,height*.5f,0);box.size=new Vector3(bounds.size.x*scale,height,bounds.size.z*scale);
            }
            holder.transform.position=position;holder.transform.rotation=Quaternion.Euler(0,yaw,0);
            return holder;
        }

        public static GameObject Box(string name,Vector3 position,Vector3 scale,Material material,Transform parent=null)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent!=null?parent:root);go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
        public static GameObject Cylinder(string name,Vector3 position,Vector3 scale,Material material,Transform parent=null)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent!=null?parent:root);go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
        static void Sign(string text,Vector3 position,float size,float yaw)
        {
            var go=new GameObject(text);go.transform.SetParent(root);go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,yaw,0);
            var label=go.AddComponent<TextMesh>();label.text=text;label.characterSize=size;label.fontSize=64;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.92f,.76f,.46f);
        }
    }
}
