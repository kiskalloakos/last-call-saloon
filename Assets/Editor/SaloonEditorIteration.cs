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

    [MenuItem("Saloon/Verify discussion evidence in Play mode")]
    public static void RunEvidence(){Begin("evidence");}

    [MenuItem("Saloon/Verify tabletop blackjack in Play mode")]
    public static void RunBlackjack(){Begin("blackjack");}

    [MenuItem("Saloon/Verify tabletop coin flip in Play mode")]
    public static void RunCoin(){Begin("coin");}

    [MenuItem("Saloon/Verify solo practice startup in Play mode")]
    public static void RunSolo(){Begin("solo");}

    [MenuItem("Saloon/Verify one-way room windows in Play mode")]
    public static void RunWindows(){Begin("windows");}

    [MenuItem("Saloon/Verify smoking area and private plans in Play mode")]
    public static void RunSmoking(){Begin("smoking");}

    [MenuItem("Saloon/Verify cigarette damage buff in Play mode")]
    public static void RunCigaretteBuff(){Begin("cigarettebuff");}

    [MenuItem("Saloon/Verify recorded sound effects in Play mode")]
    public static void RunLibraryAudio(){Begin("libraryaudio");}

    [MenuItem("Saloon/Verify rematch and reconnect in Play mode")]
    public static void RunRematch(){Begin("rematch");}

    [MenuItem("Saloon/Verify Renegade role knowledge in Play mode")]
    public static void RunKnowledge(){Begin("knowledge");}

    [MenuItem("Saloon/Verify head movement, speech and build windows in Play mode")]
    public static void RunModelRoomFixes(){Begin("model-room-fixes");}

    [MenuItem("Saloon/Verify crouching and weapon clipping in Play mode")]
    public static void RunCrouch(){Begin("crouch");}

    [MenuItem("Saloon/Verify sunset overlook and silent outdoor walking")]
    public static void RunSunset(){Begin("sunset");}

    static void Begin(string mode)
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before verifying.");
        SaloonProject.OpenForEditing();SessionState.SetString(Mode,mode);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        if(mode=="blackjack"||mode=="coin"||mode=="planning")
        {
            var gameView=Type.GetType("UnityEditor.GameView,UnityEditor");
            if(gameView!=null)EditorWindow.GetWindow(gameView).Focus();
        }
    }

    [MenuItem("Saloon/Verify percentage health in Play mode")]
    public static void RunHealth(){Begin("health");}

    static void Poll()
    {
        if(!SessionState.GetBool(Pending,false)||EditorApplication.isCompiling||!EditorApplication.isPlaying)return;
        var game=UnityEngine.Object.FindAnyObjectByType<SaloonGame>();if(game==null)return;
        if(!running)
        {
            running=true;started=EditorApplication.timeSinceStartup;game.AutomatedInput=true;game.Port="18112";
            string mode=SessionState.GetString(Mode,"");
            bool lobby=mode=="rematch"||mode=="lobby"||mode=="disconnect"||mode=="recovery";
            if(lobby)game.SetBotsEnabled(false);
            if(mode!="solo")game.HostLan(!lobby);
            game.StartCoroutine(mode=="sunset"?game.VerifySunsetOverlook():mode=="crouch"?game.VerifyCrouchAndWeapon():mode=="model-room-fixes"?game.VerifyModelRoomFixes():mode=="libraryaudio"?game.VerifyLibraryAudio():mode=="cigarettebuff"?game.VerifyCigaretteBuff():mode=="health"?game.VerifyPercentageHealth():mode=="knowledge"?game.VerifyRenegadeKnowledge():mode=="rematch"?game.VerifyRematchAndReconnect():mode=="solo"?game.VerifySoloPractice():mode=="coin"?game.VerifyTabletopCoin():mode=="smoking"?game.VerifySmoking():mode=="windows"?game.VerifyRoomWindows():mode=="blackjack"?game.VerifyTabletopBlackjack():mode=="evidence"?game.VerifyEvidence():mode=="recovery"?game.VerifyLanRecovery():mode=="disconnect"?game.VerifyDisconnectDetails():mode=="planning"?game.VerifyPlanningDraft():mode=="botmovement"?game.VerifyBotMovement():mode=="roomactions"?game.VerifyRoomActions():mode=="combat"?game.VerifyHitReactionsAndGrip():mode=="speech"?game.VerifyTalkingMouths():mode=="western"?game.VerifyWesternCharacter():mode=="lobby"?game.VerifyEditorLobby():mode=="ragdoll"?game.VerifyEditorRagdoll():game.VerifyEditorRoleCardsAndDraw());
        }
        bool timeout=EditorApplication.timeSinceStartup-started>100;
        if(!game.VerificationFinished&&!timeout)return;
        if(!cleaning){cleaning=true;game.Leave();return;}
        if(game.InRoom||game.View!=null)return;
        Debug.Log(game.VerificationPassed&&!timeout?"SALOON_EDITOR_ITERATION PASS":"SALOON_EDITOR_ITERATION FAIL");
        bool passed=game.VerificationPassed&&!timeout;
        SessionState.SetBool(Pending,false);running=cleaning=false;EditorApplication.ExitPlaymode();
        if(Application.isBatchMode&&(SessionState.GetString(Mode,"")=="sunset"||SessionState.GetString(Mode,"")=="crouch"||SessionState.GetString(Mode,"")=="ragdoll"||SessionState.GetString(Mode,"")=="model-room-fixes"||SessionState.GetString(Mode,"")=="speech"||SessionState.GetString(Mode,"")=="western"||SessionState.GetString(Mode,"")=="combat"||SessionState.GetString(Mode,"")=="knowledge"||SessionState.GetString(Mode,"")=="rematch"||SessionState.GetString(Mode,"")=="solo"||SessionState.GetString(Mode,"")=="coin"||SessionState.GetString(Mode,"")=="blackjack"||SessionState.GetString(Mode,"")=="planning"||SessionState.GetString(Mode,"")=="windows"||SessionState.GetString(Mode,"")=="smoking"))EditorApplication.Exit(passed?0:1);
    }
}
