using System;
using System.IO;
using System.Linq;
using Saloon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SaloonLightingSetup
{
    [MenuItem("Saloon/Apply lantern lighting to open scene")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Application.isPlaying)throw new InvalidOperationException("Stop Play mode before applying lighting.");
        var world=GameObject.Find("Saloon World");if(world==null)throw new InvalidOperationException("Open the saloon scene first.");
        Undo.RegisterFullObjectHierarchyUndo(world,"Upgrade lanterns and lighting");
        SaloonInterior.Apply(world);SaloonSurfaces.Apply(world.transform);
        // Persist procedural interior meshes and materials so reopening the scene
        // retains the edit-mode preview as well as the runtime migration.
        const string folder="Assets/Generated/Lighting";Directory.CreateDirectory(folder);
        var interior=world.transform.Find("Western interior");
        foreach(var filter in interior.GetComponentsInChildren<MeshFilter>())
        {
            var mesh=filter.sharedMesh;if(mesh==null||AssetDatabase.Contains(mesh))continue;
            string path=folder+"/"+SafeName(mesh.name)+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else EditorUtility.CopySerialized(mesh,saved);
            filter.sharedMesh=saved;
        }
        foreach(var material in world.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&!AssetDatabase.Contains(m)).Distinct().ToArray())
        {
            string path=folder+"/"+SafeName(material.name)+".mat";
            var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(saved==null){AssetDatabase.CreateAsset(material,path);saved=material;}else EditorUtility.CopySerialized(material,saved);
            foreach(var renderer in world.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<materials.Length;i++)if(materials[i]==material){materials[i]=saved;changed=true;}
                if(changed)renderer.sharedMaterials=materials;
            }
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.scene);EditorSceneManager.SaveScene(world.scene);
        Debug.Log("SALOON_LIGHTING_SETUP PASS: saved lantern fixtures, materials and room lighting");
    }
    static string SafeName(string name)=>string.Concat(name.Select(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_'?c:'_'));
}
