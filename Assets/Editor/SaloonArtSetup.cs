using System;
using System.IO;
using Saloon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaloonArtImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.Contains("/Art/LowPoly/"))return;
        var texture=(TextureImporter)assetImporter;texture.maxTextureSize=256;
        texture.filterMode=FilterMode.Point;texture.mipmapEnabled=true;
        texture.textureCompression=TextureImporterCompression.Uncompressed;
    }
    void OnPreprocessModel()
    {
        if(!assetPath.Contains("/Art/LowPoly/"))return;
        var model=(ModelImporter)assetImporter;model.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        model.animationType=ModelImporterAnimationType.Generic;model.importAnimation=false;
        model.materialLocation=ModelImporterMaterialLocation.InPrefab;
    }
}

public static class SaloonArtSetup
{
    [MenuItem("Saloon/Apply low-poly art to open scene")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before applying scene artwork.");
        var world=GameObject.Find("Saloon World");
        if(world==null)throw new InvalidOperationException("Open the Saloon scene first.");
        Undo.RegisterFullObjectHierarchyUndo(world,"Apply low-poly saloon art");
        SaloonArt.Apply(true);SaloonRoomWindows.Apply();SaloonRoomSigns.Apply();
        Directory.CreateDirectory("Assets/Generated/Materials");
        foreach(var mat in SaloonWorld.GeneratedMaterials)
        {
            if(AssetDatabase.Contains(mat))continue;
            string path="Assets/Generated/Materials/"+mat.name.Replace('/','-')+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing==null){AssetDatabase.CreateAsset(mat,path);continue;}
            EditorUtility.CopySerialized(mat,existing);
            foreach(var renderer in world.GetComponentsInChildren<Renderer>())
            {
                var materials=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<materials.Length;i++)if(materials[i]==mat){materials[i]=existing;changed=true;}
                if(changed)renderer.sharedMaterials=materials;
            }
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());Verify();
    }

    [MenuItem("Saloon/Verify low-poly art")]
    public static void Verify()
    {
        foreach(string name in new[]{"Bar","Chair","Table","Shelf","Door","Window","Cup","Plate","Mug","Safe","Revolver","Arms"})
        {
            var prefab=Resources.Load<GameObject>("Art/LowPoly/"+name);
            if(prefab==null||prefab.GetComponentsInChildren<Renderer>().Length==0)throw new Exception("Missing asset: "+name);
            foreach(var renderer in prefab.GetComponentsInChildren<Renderer>())
                foreach(var mat in renderer.sharedMaterials)
                    if(mat==null||mat.shader==null||mat.shader.name.Contains("InternalError"))throw new Exception("Invalid material: "+name);
        }
        if(Resources.Load<Texture2D>("Art/LowPoly/Textures/WoodPlanks")==null)throw new Exception("Missing wood texture");
        Debug.Log("SALOON_LOW_POLY_ART PASS: twelve models, materials, and wood texture");
    }
}
