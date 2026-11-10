using System;
using Saloon;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SaloonBlackjackPreview
{
    const string Pending="Saloon.BlackjackPreview.Pending";
    static SaloonBlackjackPreview(){EditorApplication.update+=Poll;}

    [MenuItem("Saloon/Play blackjack at table")]
    public static void Play()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before starting a new blackjack preview.");
        SaloonProject.OpenForEditing();SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    [MenuItem("Saloon/Play have a smoke outside")]
    public static void Smoke()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
        SaloonProject.OpenForEditing();SessionState.SetInt("Saloon.Preview.Room",3);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    [MenuItem("Saloon/Play coin flip at table")]
    public static void Coin()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
        SaloonProject.OpenForEditing();SessionState.SetInt("Saloon.Preview.Room",1);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    static void Poll()
    {
        if(!SessionState.GetBool(Pending,false)||EditorApplication.isCompiling||!EditorApplication.isPlaying)return;
        var game=UnityEngine.Object.FindAnyObjectByType<SaloonGame>();if(game==null)return;
        SessionState.SetBool(Pending,false);
        int room=SessionState.GetInt("Saloon.Preview.Room",2);
        if(room==3)game.StartSmokingPractice();else if(room==1)game.StartCoinTablePractice();else game.StartBlackjackTablePractice();
        SessionState.SetInt("Saloon.Preview.Room",2);
        var gameView=Type.GetType("UnityEditor.GameView,UnityEditor");
        if(gameView!=null)EditorWindow.GetWindow(gameView).Focus();
    }
}
