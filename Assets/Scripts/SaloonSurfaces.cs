using System.Collections.Generic;
using UnityEngine;

namespace Saloon
{
    // World-space projection keeps scanned grain at a consistent size on stretched
    // primitives, imported furniture and the combined interior meshes alike.
    public static class SaloonSurfaces
    {
        public static void Apply(Transform root)
        {
            var shader=Resources.Load<Shader>("Shaders/SaloonSurface");
            if(shader==null)return;
            var replacements=new Dictionary<string,Material>();
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var materials=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];if(source==null||source.shader==shader)continue;
                    string name=source.name.Replace(" (Instance)","");
                    string scan=null;Color tint=Color.white;float scale=.5f,polish=0,relief=.4f;
                    if(name=="Warm limewash") {scan="LimePlaster";tint=new Color(.88f,.79f,.65f);scale=.65f;relief=.3f;}
                    else if(name=="Counter oak"||name=="Walnut joinery"||name=="Bar access walnut"||name=="Retro dark timber"||name=="Retro floorboards"||name=="Honey oak"||name=="Dark walnut"||name=="Sunset deck cedar"||name=="Weathered deck posts"||Furniture(name))
                    {
                        bool outside=name=="Sunset deck cedar"||name=="Weathered deck posts";
                        bool dark=name=="Walnut joinery"||name=="Bar access walnut"||name=="Retro dark timber"||name=="Dark walnut"||name=="Weathered deck posts";
                        bool floor=name=="Retro floorboards";
                        scan=outside||floor?"WeatheredWood":"CounterWood";
                        tint=dark?new Color(.34f,.23f,.16f):outside?new Color(.73f,.60f,.46f):floor?new Color(.68f,.49f,.32f):new Color(.85f,.64f,.41f);
                        scale=outside||floor?.42f:.65f;polish=outside||floor?0:.07f;
                    }
                    else if(name=="Sunset sand") {scan="DesertGravel";scale=.25f;tint=new Color(.84f,.61f,.43f);relief=.55f;}
                    if(scan==null)continue;
                    string key=name+"|"+scan;
                    if(!replacements.TryGetValue(key,out var material))
                    {
                        var color=Resources.Load<Texture2D>("Art/Surfaces/"+scan+"_Color");
                        var normal=Resources.Load<Texture2D>("Art/Surfaces/"+scan+"_NormalGL");
                        var roughness=Resources.Load<Texture2D>("Art/Surfaces/"+scan+"_Roughness");
                        if(color==null||normal==null||roughness==null)continue;
                        material=new Material(shader){name="Scanned "+name,color=tint};
                        material.mainTexture=color;material.SetTexture("_NormalMap",normal);material.SetTexture("_Roughness",roughness);
                        material.SetFloat("_Scale",scale);material.SetFloat("_NormalStrength",relief);material.SetFloat("_Polish",polish);
                        replacements.Add(key,material);SaloonWorld.GeneratedMaterials.Add(material);
                    }
                    materials[i]=material;changed=true;
                }
                if(changed)renderer.sharedMaterials=materials;
            }
        }
        static bool Furniture(string name)
        {
            if(name.Contains("glass"))return false;
            return name.StartsWith("Beta Chair ")||name.StartsWith("Beta Table ")||name.StartsWith("Beta Shelf ")||name.StartsWith("Beta Door ")||name.StartsWith("Beta Window ");
        }
    }
}
