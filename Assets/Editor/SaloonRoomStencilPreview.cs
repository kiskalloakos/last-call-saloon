using System;
using System.IO;
using System.Linq;
using Saloon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SaloonRoomStencilPreview
{
    const string Request="Temp/RoomStencils.request";
    static SaloonRoomStencilPreview(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/BluffDiceVerify.request"))
        {File.Delete("Temp/BluffDiceVerify.request");SaloonEditorIteration.RunBluffDice();return;}
        if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/ToiletDutyVerify.request"))
        {File.Delete("Temp/ToiletDutyVerify.request");SaloonEditorIteration.RunToiletDuty();return;}
        if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/BarLightingVerify.request"))
        {File.Delete("Temp/BarLightingVerify.request");SaloonEditorIteration.RunBarLighting();return;}
        if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/RoomWallsVerify.request"))
        {File.Delete("Temp/RoomWallsVerify.request");SaloonEditorIteration.RunWindows();return;}
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Request))return;
        File.Delete(Request);
        try{Apply();}catch(Exception ex){Debug.LogException(ex);File.WriteAllText("Temp/RoomStencils.error",ex.ToString());}
    }

    [MenuItem("Saloon/Apply and preview room wall stencils")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before applying wall stencils.");
        var world=GameObject.Find("Saloon World");if(world==null)throw new InvalidOperationException("Open the Saloon scene first.");
        foreach(string name in new[]{"CoinFlip","Blackjack"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Art/RoomStencils/"+name+".png");
            importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;
            importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=true;importer.alphaTestReferenceValue=.35f;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        Undo.RegisterFullObjectHierarchyUndo(world,"Replace room plaques with wall stencils");
        SaloonRoomWindows.Apply();SaloonLighting.Apply(world);
        SaloonRoomSigns.Apply();
        var root=world.transform.Find(SaloonRoomSigns.RootName);
        SaloonRoomSigns.Apply();
        var graphics=root.GetComponentsInChildren<MeshRenderer>();
        if(world.transform.Find("Minigame room signs")!=null||root.childCount!=2||graphics.Length!=2||root.GetComponentsInChildren<Collider>().Length!=0)
            throw new Exception("Expected two wall graphics, no old signs or colliders, and stable repeated application.");
        if(world.GetComponentsInChildren<SaloonRoomWindow>().Length!=0||world.transform.Find("One-way room windows")!=null||world.GetComponentsInChildren<SaloonRoomWall>().Length!=2)
            throw new Exception("The minigame rooms must have solid Bar-facing walls without interior window geometry.");
        var lamps=world.transform.Find(SaloonLighting.RootName);
        var roomLamps=lamps.GetComponentsInChildren<Light>().Where(l=>l.type==LightType.Point&&l.transform.position.z<-1.7f).ToArray();
        if(roomLamps.Length!=2||roomLamps.Any(l=>l.name!="Pendant lantern"||l.transform.position.y<3.1f||Mathf.Abs(Mathf.Abs(l.transform.position.x)-5)>.01f))
            throw new Exception("Each enclosed room needs one ceiling pendant at its center.");
        Physics.SyncTransforms();
        var walls=world.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&(c.name=="Room back"||c.name=="Room side"||c.GetComponent<SaloonRoomWall>()!=null)).ToArray();
        foreach(var renderer in lamps.GetComponentsInChildren<Renderer>())
            if(walls.Any(w=>w.bounds.Intersects(renderer.bounds)))throw new Exception("Lamp intersects a room wall: "+renderer.transform.parent.name+" / "+renderer.name);
        for(int a=0;a<3;a++)for(int b=0;b<3;b++)
            if(SaloonPrivacy.MuffledVoice(Phase.Turns,a,b)!=(a!=b))throw new Exception("Cross-room indoor audio must remain muffled in both directions.");
        foreach(var graphic in graphics.Concat(lamps.GetComponentsInChildren<MeshRenderer>()).Concat(world.transform.Find(SaloonExteriorWindows.RootName).GetComponentsInChildren<MeshRenderer>()))
        {
            var material=graphic.sharedMaterial;
            if(graphic.transform.parent==root&&(material.mainTexture==null||!material.IsKeywordEnabled("_ALPHATEST_ON")||graphic.transform.localScale.y<2.5f))
                throw new Exception("Stencil needs transparent artwork and matte, shadow-free geometry.");
            if(AssetDatabase.Contains(material))continue;
            string path="Assets/Generated/Materials/"+material.name+".mat";
            var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(saved==null){AssetDatabase.CreateAsset(material,path);saved=material;}else EditorUtility.CopySerialized(material,saved);
            graphic.sharedMaterial=saved;
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.scene);EditorSceneManager.SaveScene(world.scene);
        bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        try
        {
            Directory.CreateDirectory("Documentation/RoomStencilPreviews");
            Capture("from-bar",new Vector3(0,1.75f,2.8f),new Vector3(0,1.9f,-1.7f),100);
            Capture("coin-flip",new Vector3(-5,1.75f,1.8f),new Vector3(-5,2.1f,-1.7f),65);
            Capture("blackjack",new Vector3(5,1.75f,1.8f),new Vector3(5,2.1f,-1.7f),65);
            Capture("inside-coin-flip",new Vector3(-5,1.7f,-2.25f),new Vector3(-5,2.4f,-4.6f),90);
            Capture("inside-blackjack",new Vector3(5,1.7f,-2.25f),new Vector3(5,2.4f,-4.6f),90);
        }
        finally{ShaderUtil.allowAsyncCompilation=async;}
        File.WriteAllText("Temp/RoomStencils.pass","PASS: two solid Bar-facing room walls, large murals, exterior windows retained, centered ceiling lamps without wall intersections, muffled indoor audio preserved, repeated application stable, scene saved and five previews rendered.\n");
        Debug.Log("SALOON_ROOM_STENCILS PASS");
    }

    static void Capture(string name,Vector3 position,Vector3 look,float fov)
    {
        var go=new GameObject("Room stencil preview camera");var camera=go.AddComponent<Camera>();
        go.transform.position=position;go.transform.LookAt(look);camera.fieldOfView=fov;camera.nearClipPlane=.04f;
        SaloonSmokingArea.ApplyCamera(camera,false);
        var target=new RenderTexture(1600,900,24);var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
            File.WriteAllBytes("Documentation/RoomStencilPreviews/"+name+".png",texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
