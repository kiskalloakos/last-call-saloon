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
                    var source=materials[i];if(source==null)continue;
                    string name=source.name.Replace(" (Instance)","").Replace("Scanned ","");
                    string scan=null;Color tint=Color.white;float scale=.5f,polish=0,relief=.4f;
                    if(name=="Warm limewash") {scan="LimePlaster";tint=new Color(1f,.94f,.80f);scale=.65f;relief=.3f;}
                    else if(name=="Counter oak"||name=="Walnut joinery"||name=="Bar access walnut"||name=="Retro dark timber"||name=="Retro floorboards"||name=="Honey oak"||name=="Dark walnut"||name=="Sunset deck cedar"||name=="Weathered deck posts"||Furniture(name))
                    {
                        bool outside=name=="Sunset deck cedar"||name=="Weathered deck posts";
                        bool dark=name=="Walnut joinery"||name=="Bar access walnut"||name=="Retro dark timber"||name=="Dark walnut"||name=="Weathered deck posts";
                        bool floor=name=="Retro floorboards";
                        scan=outside||floor?"WeatheredWood":"CounterWood";
                        tint=dark?new Color(.56f,.34f,.19f):outside?new Color(.95f,.73f,.45f):floor?new Color(.88f,.62f,.36f):new Color(1f,.77f,.45f);
                        scale=outside||floor?.42f:.65f;polish=outside||floor?0:.07f;
                    }
                    else if(name=="Sunset sand") {scan="DesertGravel";scale=.25f;tint=new Color(1f,.76f,.50f);relief=.55f;}
                    if(scan==null)
                    {
                        var accent=Accent(name);if(!accent.HasValue||source.color==accent.Value)continue;
                        if(!replacements.TryGetValue(name,out var colored))
                        {
                            colored=new Material(source){name=source.name,color=accent.Value};
                            replacements.Add(name,colored);SaloonWorld.GeneratedMaterials.Add(colored);
                        }
                        materials[i]=colored;changed=true;continue;
                    }
                    if(source.shader==shader&&source.color==tint)continue;
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
        static Color? Accent(string name)
        {
            switch(name)
            {
                case "Bottle green":return new Color(.12f,.48f,.22f);
                case "Bottle amber":return new Color(.72f,.32f,.065f);
                case "Bottle blue":return new Color(.10f,.43f,.58f);
                case "Bottle paper":return new Color(.96f,.86f,.62f);
                case "Oxblood leather":return new Color(.46f,.10f,.065f);
                case "Polished aged brass":return new Color(.85f,.58f,.20f);
                case "Smoky backbar mirror":return new Color(.39f,.58f,.57f);
                default:return null;
            }
        }

        static bool Furniture(string name)
        {
            if(name.Contains("glass"))return false;
            return name.StartsWith("Beta Chair ")||name.StartsWith("Beta Table ")||name.StartsWith("Beta Shelf ")||name.StartsWith("Beta Door ")||name.StartsWith("Beta Window ");
        }
    }
}
