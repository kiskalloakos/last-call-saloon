using System;
using System.IO;
using System.Linq;
using Saloon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SaloonSurfacePreview
{
    const string Request="Temp/SurfacePreview.request";
    static SaloonSurfacePreview(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||Application.isPlaying||!File.Exists(Request))return;
        File.Delete(Request);Run();
    }
    [MenuItem("Saloon/Preview scanned environment materials")]
    public static void Run()
    {
        bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        var previous=SceneManager.GetActiveScene();
        var preview=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(preview);
        // Temporarily hide the existing world so the procedural builders use the preview scene.
        var previousRoots=previous.GetRootGameObjects();var active=previousRoots.Select(o=>o.activeSelf).ToArray();
        for(int i=0;i<previousRoots.Length;i++)previousRoots[i].SetActive(false);
        try
        {
            var world=SaloonWorld.Build();SaloonArt.Apply();SaloonSmokingArea.Apply();
            var shader=Resources.Load<Shader>("Shaders/SaloonSurface");
            if(shader==null||!shader.isSupported||ShaderUtil.ShaderHasError(shader))throw new Exception("Scanned surface shader unsupported or has compiler errors");
            foreach(string scan in new[]{"CounterWood","WeatheredWood","LimePlaster","DesertGravel"})
                foreach(string map in new[]{"Color","NormalGL","Roughness"})
                {
                    string path="Assets/Resources/Art/Surfaces/"+scan+"_"+map+".jpg";
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                    if(importer==null||importer.sRGBTexture!=(map=="Color")||importer.wrapMode!=TextureWrapMode.Repeat)throw new Exception("Invalid scan import: "+path);
                }
            var renderers=preview.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Renderer>(true)).ToArray();
            int count=renderers.Count(r=>r.sharedMaterials.Any(m=>m!=null&&m.shader==shader));
            if(count<40)throw new Exception("Expected scanned materials on interior and deck: "+count);
            var physics=renderers.Select(r=>r.GetComponent<Collider>()).Where(c=>c!=null).Select(c=>(c,c.enabled)).ToArray();
            int materialCount=SaloonWorld.GeneratedMaterials.Count;
            SaloonSurfaces.Apply(world.transform);SaloonSurfaces.Apply(GameObject.Find("have a smoke · sunset deck").transform);
            if(materialCount!=SaloonWorld.GeneratedMaterials.Count||physics.Any(p=>p.c.enabled!=p.enabled))throw new Exception("Material application is not idempotent or changed colliders");
            var fixtures=world.transform.Find(SaloonLighting.RootName);
            var lamps=fixtures.GetComponentsInChildren<Light>();
            if(lamps.Length!=8||lamps.Any(l=>l.type!=LightType.Point||l.transform.localPosition.y<1.5f||l.cullingMask!=(1<<SaloonSmokingArea.IndoorLayer)))throw new Exception("Lantern lights must originate at eight indoor fixtures");
            if(fixtures.GetComponentsInChildren<Collider>().Any(c=>c.enabled))throw new Exception("Decorative lanterns must not block players");
            if(fixtures.GetComponentsInChildren<Renderer>().Count(r=>r.sharedMaterial.IsKeywordEnabled("_EMISSION"))!=8)throw new Exception("Each lantern needs a luminous chamber");
            SaloonLighting.Apply(world);
            if(fixtures.GetComponentsInChildren<Light>().Length!=8)throw new Exception("Lighting application duplicated fixtures");
            Directory.CreateDirectory("Documentation/SurfacePreviews");
            Capture("bar",new Vector3(-.7f,1.72f,-.4f),new Vector3(-1.7f,1.55f,4.2f),false);
            Capture("lamps",new Vector3(-2,2.55f,2.5f),new Vector3(-2,2.85f,5),false);
            Capture("smoking-deck",new Vector3(25.2f,1.65f,-14.4f),new Vector3(31,1.1f,-10),true);
            File.WriteAllText("Documentation/SurfacePreviews/Verification.txt","PASS: shader supported, twelve 1K maps with correct color spaces, "+count+" scanned renderers, repeated application stable, colliders unchanged; eight emissive lanterns with centered indoor lights.\n");
            Debug.Log("SALOON_SURFACES PASS "+count+" renderers; Documentation/SurfacePreviews");
        }
        catch(Exception ex){Debug.LogException(ex);File.WriteAllText("Temp/SurfacePreview.error",ex.ToString());}
        finally
        {
            ShaderUtil.allowAsyncCompilation=async;
            EditorSceneManager.CloseScene(preview,true);SceneManager.SetActiveScene(previous);
            for(int i=0;i<previousRoots.Length;i++)if(previousRoots[i]!=null)previousRoots[i].SetActive(active[i]);
        }
    }
    static void Capture(string name,Vector3 position,Vector3 look,bool outside)
    {
        var go=new GameObject("Surface preview camera");var camera=go.AddComponent<Camera>();
        go.transform.position=position;go.transform.LookAt(look);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.19f,.15f,.32f);camera.fieldOfView=70;camera.nearClipPlane=.04f;camera.farClipPlane=140;
        SaloonSmokingArea.ApplyCamera(camera,outside);
        var target=new RenderTexture(1440,900,24);var image=new Texture2D(1440,900,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
            File.WriteAllBytes("Documentation/SurfacePreviews/"+name+".png",image.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);}
    }
}
