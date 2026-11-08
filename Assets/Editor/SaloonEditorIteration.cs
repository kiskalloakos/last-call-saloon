using System;
using Saloon;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SaloonEditorIteration
{
    const string Pending="Saloon.EditorIteration.Pending";
    const string Mode="Saloon.EditorIteration.Mode";
    static bool running,cleaning;
    static double started;
    static SaloonEditorIteration(){EditorApplication.update+=Poll;}

    [MenuItem("Saloon/Verify role cards and revolver draw in Play mode")]
    public static void Run()
    {Begin("guides");}

    [MenuItem("Saloon/Verify ragdoll deaths in Play mode")]
    public static void RunRagdoll()
    {Begin("ragdoll");}

    [MenuItem("Saloon/Verify LAN lobby and four-player matches in Play mode")]
    public static void RunLobby()
    {SaloonProject.Verify();Begin("lobby");}

    [MenuItem("Saloon/Verify Western character animations in Play mode")]
    public static void RunWestern()
    {SaloonWesternCharacter.Prepare();Begin("western");}

    [MenuItem("Saloon/Verify talking mouths in Play mode")]
    public static void RunSpeech()
    {SaloonWesternCharacter.Prepare();Begin("speech");}

    [MenuItem("Saloon/Verify hit reactions and revolver grip in Play mode")]
    public static void RunCombat()
    {SaloonWesternCharacter.Prepare();Begin("combat");}

    [MenuItem("Saloon/Verify headshots, healing and room movement in Play mode")]
    public static void RunRoomActions(){Begin("roomactions");}

    [MenuItem("Saloon/Verify bot dodging movement in Play mode")]
    public static void RunBotMovement(){Begin("botmovement");}

    [MenuItem("Saloon/Verify rapid room and companion choices in Play mode")]
    public static void RunPlanning(){Begin("planning");}

    [MenuItem("Saloon/Verify disconnect details in Play mode")]
    public static void RunDisconnect(){Begin("disconnect");}

    [MenuItem("Saloon/Verify four-player LAN recovery in Play mode")]
    public static void RunRecovery(){Begin("recovery");}

    static void Begin(string mode)
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before verifying.");
        SaloonProject.OpenForEditing();SessionState.SetString(Mode,mode);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }

    static void Poll()
    {
        if(!SessionState.GetBool(Pending,false)||EditorApplication.isCompiling||!EditorApplication.isPlaying)return;
        var game=UnityEngine.Object.FindAnyObjectByType<SaloonGame>();if(game==null)return;
        if(!running)
        {
            running=true;started=EditorApplication.timeSinceStartup;game.AutomatedInput=true;game.Port="18112";
            string mode=SessionState.GetString(Mode,"");
            bool lobby=mode=="lobby"||mode=="disconnect"||mode=="recovery";
            if(lobby)game.SetBotsEnabled(false);
            game.HostLan(!lobby);game.StartCoroutine(mode=="recovery"?game.VerifyLanRecovery():mode=="disconnect"?game.VerifyDisconnectDetails():mode=="planning"?game.VerifyPlanningDraft():mode=="botmovement"?game.VerifyBotMovement():mode=="roomactions"?game.VerifyRoomActions():mode=="combat"?game.VerifyHitReactionsAndGrip():mode=="speech"?game.VerifyTalkingMouths():mode=="western"?game.VerifyWesternCharacter():mode=="lobby"?game.VerifyEditorLobby():mode=="ragdoll"?game.VerifyEditorRagdoll():game.VerifyEditorRoleCardsAndDraw());
        }
        bool timeout=EditorApplication.timeSinceStartup-started>60;
        if(!game.VerificationFinished&&!timeout)return;
        if(!cleaning){cleaning=true;game.Leave();return;}
        if(game.InRoom||game.View!=null)return;
        Debug.Log(game.VerificationPassed&&!timeout?"SALOON_EDITOR_ITERATION PASS":"SALOON_EDITOR_ITERATION FAIL");
        SessionState.SetBool(Pending,false);running=cleaning=false;EditorApplication.ExitPlaymode();
    }
}
