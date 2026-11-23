using System;
using Saloon;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SaloonTutorialEditor
{
    const string Pending="Saloon.TutorialVerification.Pending";
    const string Preview="Saloon.TutorialVerification.Preview";
    const string Short="Saloon.TutorialVerification.Short";
    const string Checkpoint="Saloon.TutorialVerification.Checkpoint";
    static bool running,cleaning;
    static double started;
    static SaloonTutorialEditor(){EditorApplication.update+=Poll;}

    [MenuItem("Saloon/Verify interactive tutorial in Play mode")]
    public static void Verify(){Begin(false);}
    [MenuItem("Saloon/Verify short tutorial in Play mode")]
    public static void VerifyShort(){Begin(false,true);}
    [MenuItem("Saloon/Preview interactive tutorial")]
    public static void PreviewTutorial(){Begin(true);}
    [MenuItem("Saloon/Preview tutorial spectator checkpoint")]
    public static void PreviewSpectator(){Begin(true,false,SaloonGame.WalkthroughBeat.Spectate);}
    [MenuItem("Saloon/Preview tutorial sunset deck")]
    public static void PreviewSunset(){Begin(true,false,SaloonGame.WalkthroughBeat.Smoke);}
    static void Begin(bool preview,bool shortRun=false,SaloonGame.WalkthroughBeat? checkpoint=null)
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before starting this check.");
        SaloonProject.OpenForEditing();SessionState.SetBool(Pending,true);SessionState.SetBool(Preview,preview);
        SessionState.SetBool(Short,shortRun);
        SessionState.SetInt(Checkpoint,checkpoint.HasValue?(int)checkpoint.Value:-1);
        EditorApplication.EnterPlaymode();
        var type=Type.GetType("UnityEditor.GameView,UnityEditor");if(type!=null)EditorWindow.GetWindow(type).Focus();
    }
    static void Poll()
    {
        if(!SessionState.GetBool(Pending,false)||EditorApplication.isCompiling||!EditorApplication.isPlaying)return;
        var game=UnityEngine.Object.FindAnyObjectByType<SaloonGame>();if(game==null)return;
        if(SessionState.GetBool(Preview,false))
        {
            int checkpoint=SessionState.GetInt(Checkpoint,-1);
            if(checkpoint>=0)game.PreviewTutorialCheckpoint((SaloonGame.WalkthroughBeat)checkpoint);else game.OpenTutorial();
            SessionState.SetBool(Pending,false);return;
        }
        if(!running)
        {running=true;started=EditorApplication.timeSinceStartup;game.AutomatedInput=true;game.StartCoroutine(SessionState.GetBool(Short,false)?game.VerifyWalkthrough():game.VerifyTutorial());}
        if(!game.VerificationFinished&&EditorApplication.timeSinceStartup-started<320)return;
        if(!cleaning){cleaning=true;game.Leave();return;}
        if(game.InRoom||game.View!=null)return;
        Debug.Log(game.VerificationPassed?"SALOON_EDITOR_TUTORIAL PASS":"SALOON_EDITOR_TUTORIAL FAIL");
        SessionState.SetBool(Pending,false);running=cleaning=false;EditorApplication.ExitPlaymode();
    }
}
