using System;
using Saloon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SaloonDicePracticeEditor
{
    public const string ScenePath="Assets/Scenes/BluffDiceTest.unity";
    public static void Play()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before opening the dice test environment.");
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)==null)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var root=new GameObject("Bluff Dice test environment");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            root.AddComponent<SaloonBluffDicePractice>();EditorSceneManager.SaveScene(scene,ScenePath);EditorSceneManager.CloseScene(scene,true);
        }
        EditorSceneManager.OpenScene(ScenePath);EditorApplication.EnterPlaymode();
        var gameView=Type.GetType("UnityEditor.GameView,UnityEditor");if(gameView!=null)EditorWindow.GetWindow(gameView).Focus();
    }
}
