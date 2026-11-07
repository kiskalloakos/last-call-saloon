using System;
using UnityEngine;

namespace Saloon
{
    // Visual overlay: keeps the tested room geometry, interaction points and seat indices.
    public static class SaloonArt
    {
        const string Root="Art/LowPoly/";
        public static GameObject Model(string asset,Transform parent,Vector3 size)
        {
            var prefab=Resources.Load<GameObject>(Root+asset);
            if(prefab==null)throw new InvalidOperationException("Missing saloon artwork: "+asset);
            var holder=new GameObject(asset+" visual");
            var fit=new GameObject("Mesh fit");fit.transform.SetParent(holder.transform,false);
            var model=UnityEngine.Object.Instantiate(prefab,fit.transform);
            model.transform.localPosition=Vector3.zero;
            var renderers=model.GetComponentsInChildren<Renderer>();
            var bounds=new Bounds();bool first=true;
            foreach(var renderer in renderers)
            {
                if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];string key=source!=null?source.name:"Default";
                    var texture=Resources.Load<Texture2D>(Root+"Textures/"+asset+"_"+key);
                    bool furniture=asset=="Bar"||asset=="Table"||asset=="Chair"||asset=="Shelf"||asset=="Door"||asset=="Window";
                    if(texture==null&&furniture&&key!="glass")texture=Resources.Load<Texture2D>(Root+"Textures/WoodPlanks");
                    var color=texture!=null?Color.white:source!=null&&source.HasProperty("_Color")?source.color:new Color(.35f,.25f,.18f);
                    if(key=="glass")color=new Color(.17f,.23f,.28f);
                    var material=SaloonWorld.Material("Beta "+asset+" "+key,color,.12f);material.mainTexture=texture;
                    materials[i]=material;
                }
                renderer.sharedMaterials=materials;
            }
            var scale=new Vector3(size.x/Mathf.Max(.001f,bounds.size.x),size.y/Mathf.Max(.001f,bounds.size.y),size.z/Mathf.Max(.001f,bounds.size.z));
            // Imported meshes are instantiated at the parent's origin before bounds normalization.
            var origin=Vector3.zero;
            var center=bounds.center-origin;
            fit.transform.localScale=scale;
            fit.transform.localPosition=Vector3.Scale(new Vector3(-center.x,-bounds.min.y+origin.y,-center.z),scale);
            holder.transform.SetParent(parent,false);return holder;
        }

        public static void Apply(bool refresh=false)
        {
            var world=GameObject.Find("Saloon World");if(world==null)return;
            if(refresh)
            {
                var previous=world.transform.Find("Low-poly art overlay");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
                foreach(var child in world.GetComponentsInChildren<Transform>(true))
                    if(child!=null&&(child.name=="Chair visual"||child.name=="Table visual"||child.name=="Shelf visual"))UnityEngine.Object.DestroyImmediate(child.gameObject);
                SaloonWorld.GeneratedMaterials.Clear();
            }
            foreach(var label in world.GetComponentsInChildren<TextMesh>(true))label.gameObject.SetActive(false);
            if(world.transform.Find("Low-poly art overlay")!=null)return;
            var overlay=new GameObject("Low-poly art overlay");overlay.transform.SetParent(world.transform,false);
            var woodTexture=Resources.Load<Texture2D>(Root+"Textures/WoodPlanks");
            var floor=SaloonWorld.Material("Retro floorboards",new Color(.62f,.48f,.34f));floor.mainTexture=woodTexture;floor.mainTextureScale=new Vector2(.5f,6);
            var timber=SaloonWorld.Material("Retro dark timber",new Color(.34f,.25f,.19f));timber.mainTexture=woodTexture;timber.mainTextureScale=new Vector2(2,2);
            foreach(var child in world.GetComponentsInChildren<Transform>())
            {
                var renderer=child.GetComponent<Renderer>();
                if(renderer!=null&&child.name=="Floorboard")renderer.sharedMaterial=floor;
                else if(renderer!=null&&(child.name=="Ceiling beam"||child.name=="Wainscot"||child.name.StartsWith("Back room partition")))renderer.sharedMaterial=timber;
            }
            foreach(var child in world.GetComponentsInChildren<Transform>())
                if(child.name=="bench"||child.name=="Shared beer supply")Hide(child.gameObject);
            // Keep physical seat colliders and InteractionPoint components intact.
            foreach(var point in world.GetComponentsInChildren<InteractionPoint>())
            {
                if(point.kind!=InteractionKind.Seat||point.gameObject.name!="chair")continue;
                Hide(point.gameObject);var chair=Model("Chair",point.transform,new Vector3(.55f,1.0f,.55f));chair.transform.localRotation=Quaternion.Euler(0,180,0);
            }
            foreach(var child in world.GetComponentsInChildren<Transform>())
            {
                if(child.name=="tableRound")
                {
                    Hide(child.gameObject);Model("Table",child,Mathf.Abs(child.position.x)<1?new Vector3(1.8f,.75f,1.6f):new Vector3(.95f,.75f,.65f));
                }
                else if(child.name=="bookcaseOpen")
                {Hide(child.gameObject);Model("Shelf",child,new Vector3(2.8f,1.95f,.36f));}
                else if(child.name=="Bar front"||child.name=="Bar top"||child.name=="Bar panel")Hide(child.gameObject);
            }
            At("Bar",overlay.transform,new Vector3(-1.8f,0,3.8f),new Vector3(9,1.43f,1.4f));
            var safe=At("Safe",overlay.transform,new Vector3(-6.45f,.06f,5.75f),new Vector3(.85f,1.15f,.9f),180);
            var collider=safe.AddComponent<BoxCollider>();collider.center=Vector3.up*.575f;collider.size=new Vector3(.85f,1.15f,.9f);
            foreach(var pos in new[]{new Vector3(0,.76f,-.5f),new Vector3(-5,.76f,-3.8f),new Vector3(5,.76f,-3.8f),new Vector3(-2,1.44f,3.8f)})
            {
                At("Mug",overlay.transform,pos,new Vector3(.16f,.19f,.15f));
                At("Plate",overlay.transform,pos+Vector3.right*.36f,new Vector3(.23f,.025f,.23f));
            }
            At("Cup",overlay.transform,new Vector3(.6f,1.44f,3.8f),new Vector3(.11f,.14f,.11f));
            At("Mug",overlay.transform,new Vector3(1.6f,1.44f,3.7f),new Vector3(.18f,.23f,.17f));

        }

        static void Hide(GameObject go){foreach(var r in go.GetComponentsInChildren<Renderer>())r.enabled=false;}
        static GameObject At(string asset,Transform parent,Vector3 position,Vector3 size,float yaw=0)
        {
            // Normalize at origin, then place: avoids rotated parent bounds affecting the asset fit.
            var result=Model(asset,null,size);result.transform.SetParent(parent,true);result.transform.position=position;result.transform.rotation=Quaternion.Euler(0,yaw,0);return result;
        }
    }
}
