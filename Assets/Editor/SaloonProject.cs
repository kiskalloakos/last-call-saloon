using System;
using System.IO;
using System.Linq;
using Saloon;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaloonProject
{
    const string ScenePath="Assets/Scenes/Saloon.unity";

    [MenuItem("Saloon/Set up saloon scene")]
    public static void Setup()
    {
        Directory.CreateDirectory("Assets/Scenes");Directory.CreateDirectory("Assets/Generated/Materials");
        AssetDatabase.Refresh();
        LinkCloudProject();
        foreach(string path in new[]{"Assets/Resources/Art/Character/characterMedium.fbx","Assets/Resources/Art/Animations/idle.fbx","Assets/Resources/Art/Animations/run.fbx"})
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            bool character=path.Contains("characterMedium");
            if(importer.animationType!=ModelImporterAnimationType.Legacy||importer.materialImportMode!=ModelImporterMaterialImportMode.None||(character&&!importer.isReadable))
            {
                importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;
                if(character)importer.isReadable=true;
                importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
            }
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SaloonWorld.Build();SaloonArt.Apply();
        foreach(var mat in SaloonWorld.GeneratedMaterials)
        {
            string path="Assets/Generated/Materials/"+mat.name.Replace('/','-')+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing!=null)
            {
                EditorUtility.CopySerialized(mat,existing);
                foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>())
                {
                    var materials=renderer.sharedMaterials;
                    bool changed=false;
                    for(int i=0;i<materials.Length;i++)if(materials[i]==mat){materials[i]=existing;changed=true;}
                    if(changed)renderer.sharedMaterials=materials;
                }
                continue;
            }
            AssetDatabase.CreateAsset(mat,path);
        }
        new GameObject("Saloon Prototype").AddComponent<SaloonGame>();
        var camera=new GameObject("First person camera").AddComponent<Camera>();camera.tag="MainCamera";
        camera.transform.position=new Vector3(0,1.75f,-5.6f);camera.transform.rotation=Quaternion.identity;
        camera.fieldOfView=78;camera.nearClipPlane=.035f;camera.farClipPlane=140;camera.gameObject.AddComponent<AudioListener>();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        PlayerSettings.companyName="Saloon Prototype";PlayerSettings.productName="Last Call - Saloon Prototype";
        PlayerSettings.bundleVersion="0.2.0";PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"com.saloonprototype.lastcall");
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.defaultIsNativeResolution=false;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
        PlayerSettings.macOS.buildNumber="1";
        var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var microphone=settings.FindProperty("microphoneUsageDescription");
        if(microphone!=null){microphone.stringValue="Proximity voice chat and the microphone echo test in this private multiplayer prototype.";settings.ApplyModifiedPropertiesWithoutUndo();}
        QualitySettings.SetQualityLevel(2);QualitySettings.pixelLightCount=6;
        AssetDatabase.SaveAssets();
        Debug.Log("SALOON_SETUP PASS "+Path.GetFullPath(ScenePath));
    }

    [MenuItem("Saloon/Verify rules and asset setup")]
    public static void Verify()
    {
        Assert(SaloonRules.Winner(new[]{Role.Sheriff,Role.Deputy})=="LAW WINS","Law victory");
        Assert(SaloonRules.Winner(new[]{Role.Sheriff,Role.Renegade})=="","Renegade prevents premature law victory");
        Assert(SaloonRules.Winner(new[]{Role.Renegade})=="","Renegade still needs the money");
        Assert(SaloonRules.Winner(new[]{Role.Outlaw,Role.Renegade})=="","Sheriff death does not resolve the heist");
        Assert(SaloonRules.Winner(new[]{Role.Deputy,Role.Renegade})=="","Deputy can continue defending");
        Assert(SaloonRules.Winner(SaloonRules.Deck)=="","Full deck does not finish immediately");
        Assert(SaloonRules.Deck.Count(r=>r==Role.Outlaw)==2&&SaloonRules.Deck.Length==5,"Role deck");
        foreach(string model in new[]{"Art/tableRound","Art/chair","Art/stoolBar","Art/bookcaseOpen","Art/WineBarrel","Art/Character/characterMedium"})
            Assert(Resources.Load<GameObject>(model)!=null,"Imported model "+model);
        Assert(Resources.Load<Texture2D>("Art/BarrelDiffuse")!=null,"Poly Haven barrel texture");
        var publicPlayer=new PlayerView{id="1",name="Stranger",sheriff=false};
        string json=JsonUtility.ToJson(publicPlayer);
        Assert(!json.Contains("Outlaw")&&!json.Contains("Deputy")&&!json.Contains("Renegade"),"Public player payload excludes hidden allegiance");
        Assert(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null,"Playable scene saved");
        Assert(CloudProjectSettings.projectId=="df9e82b7-ec21-4418-bf3e-8e50cdecf800","Saloon cloud project linked");
        Debug.Log("SALOON_VERIFY PASS: victory precedence, deck, imported models, texture, public payload, saved scene");
    }

    [MenuItem("Saloon/Build macOS playtest")]
    public static void BuildMac()
    {
        Setup();Verify();Directory.CreateDirectory("Builds/macOS");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=new[]{ScenePath},locationPathName="Builds/macOS/Last Call.app",target=BuildTarget.StandaloneOSX,
            options=BuildOptions.Development
        });
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Saloon build failed: "+report.summary.result);
        Debug.Log("SALOON_BUILD PASS "+report.summary.totalSize+" bytes");
    }

    [MenuItem("Saloon/Build Windows playtest")]
    public static void BuildWindows()
    {
        Setup();Verify();Directory.CreateDirectory("Builds/Windows");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=new[]{ScenePath},locationPathName="Builds/Windows/Last Call.exe",target=BuildTarget.StandaloneWindows64,
            options=BuildOptions.Development
        });
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Install Windows Build Support in Unity Hub; build result: "+report.summary.result);
        Debug.Log("SALOON_WINDOWS_BUILD PASS "+report.summary.totalSize+" bytes");
    }

    public static void OpenForEditing(){EditorSceneManager.OpenScene(ScenePath);}
    [MenuItem("Saloon/Build 0.2.0 Windows and Mac release")]
    public static void BuildPlaytestRelease()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before building.");
        Verify();EditorSceneManager.SaveOpenScenes();PlayerSettings.bundleVersion="0.2.0";AssetDatabase.SaveAssets();
        foreach(var target in new[]{BuildTarget.StandaloneOSX,BuildTarget.StandaloneWindows64})
        {
            bool mac=target==BuildTarget.StandaloneOSX;
            string path=mac?"Builds/0.2.0/macOS/Last Call.app":"Builds/0.2.0/Windows/Last Call.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=path,target=target,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Release build failed: "+target+" "+report.summary.result);
            Debug.Log("SALOON_RELEASE_BUILD PASS "+target+" "+report.summary.totalSize+" bytes");
        }
    }
    public static void SetupAndVerify(){Setup();Verify();}
    static void LinkCloudProject()
    {
        const string project="df9e82b7-ec21-4418-bf3e-8e50cdecf800";
        if(CloudProjectSettings.projectId==project)return;
        var connectType=typeof(CloudProjectSettings).Assembly.GetType("UnityEditor.Connect.UnityConnect",true);
        var instance=connectType.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static).GetValue(null);
        connectType.GetMethod("BindProject",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance)
            .Invoke(instance,new object[]{project,"Last Call Saloon","kiskalloakos1"});
        Debug.Log("SALOON_CLOUD_LINK bound="+CloudProjectSettings.projectBound+" project="+CloudProjectSettings.projectId);
    }
    public static void InspectCharacter()
    {
        var go=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Art/Character/characterMedium"));
        var skeleton=go.GetComponentsInChildren<Transform>().First(t=>t.name=="Root");
        var clip=Resources.LoadAll<AnimationClip>("Art/Animations/idle").First(c=>!c.name.StartsWith("__preview__"));
        Debug.Log("CHARACTER BEFORE root="+skeleton.localPosition+" scale="+skeleton.localScale+" rotation="+skeleton.localEulerAngles);
        foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())Debug.Log("CHARACTER RENDER before="+r.bounds+" local="+r.localBounds);
        clip.SampleAnimation(skeleton.gameObject,.5f);
        Debug.Log("CHARACTER AFTER root="+skeleton.localPosition+" scale="+skeleton.localScale+" rotation="+skeleton.localEulerAngles);
        foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())Debug.Log("CHARACTER RENDER after="+r.bounds+" local="+r.localBounds);
        foreach(var b in AnimationUtility.GetCurveBindings(clip).Take(20))Debug.Log("CHARACTER BIND "+b.path+" "+b.propertyName);
        UnityEngine.Object.DestroyImmediate(go);
    }
    static void Assert(bool condition,string message){if(!condition)throw new Exception("SALOON_VERIFY FAIL: "+message);}
}
