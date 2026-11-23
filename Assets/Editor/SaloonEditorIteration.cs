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

    [MenuItem("Saloon/Verify breakable windows and bottles")]
    public static void RunBreakables(){Begin("breakables");}

    [MenuItem("Saloon/Verify Bluff Dice rotation and combat opening")]
    public static void RunBluffDice(){Begin("dice");}

    [MenuItem("Saloon/Verify isolated solo toilet mission")]
    public static void RunToiletDuty(){Begin("toilet");}

    [MenuItem("Saloon/Verify Bar sunset lighting and round fade")]
    public static void RunBarLighting(){Begin("bar-lighting");}

    [MenuItem("Saloon/Verify matches without a round limit in Play mode")]
    public static void RunEndless(){Begin("endless");}

    [MenuItem("Saloon/Verify medical room opportunities in Play mode")]
    public static void RunMedical(){Begin("medical");}

    [MenuItem("Saloon/Verify cash drawer, theft and escape in Play mode")]
    public static void RunCashDrawer(){Begin("cash-drawer");}

    [MenuItem("Saloon/Verify role cards and revolver draw in Play mode")]
    public static void Run()
    {Begin("guides");}

    [MenuItem("Saloon/Verify character story maps in Play mode")]
    public static void RunStoryMaps(){Begin("storymaps");}

    [MenuItem("Saloon/Verify physical safe access in Play mode")]
    public static void RunSafeAccess(){Begin("safe-access");}

    [MenuItem("Saloon/Verify safe highlight and key prompts in Play mode")]
    public static void RunSafeHighlight(){Begin("safe-highlight");}

    [MenuItem("Saloon/Verify solo Bar, center-table walking and closed entrance")]
    public static void RunPublicFloor(){Begin("public-floor");}

    [MenuItem("Saloon/Verify ragdoll deaths in Play mode")]
    public static void RunRagdoll()
    {Begin("ragdoll");}

    [MenuItem("Saloon/Verify multiplayer shot registration")]
    public static void RunShooting(){Begin("shooting");}

    [MenuItem("Saloon/Verify LAN lobby and four-to-eight-player matches in Play mode")]
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

    [MenuItem("Saloon/Verify public Bar and safe placement")]
    public static void RunBarLayout(){Begin("bar-layout");}

    [MenuItem("Saloon/Verify beer artwork and scene continuity")]
    public static void RunBeerScene()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before verifying.");
        // A manually stopped older verification may leave its pending flag set.
        SessionState.SetBool("Saloon.RoundVerification.Pending",false);
        SessionState.SetBool("Saloon.TutorialVerification.Pending",false);
        running=cleaning=false;Begin("beer-scene");
    }

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

    [MenuItem("Saloon/Verify solid room walls and muffled audio in Play mode")]
    public static void RunWindows(){Begin("windows");}

    [MenuItem("Saloon/Verify solo smoking access in Play mode")]
    public static void RunSoloSmoking(){Begin("solo-smoking");}

    [MenuItem("Saloon/Verify smoking area and private plans in Play mode")]
    public static void RunSmoking(){Begin("smoking");}

    [MenuItem("Saloon/Verify voice controls and spectator audio in Play mode")]
    public static void RunVoiceControls(){Begin("voice-controls");}

    [MenuItem("Saloon/Verify cigarette damage buff in Play mode")]
    public static void RunCigaretteBuff(){Begin("cigarettebuff");}

    [MenuItem("Saloon/Verify recorded sound effects in Play mode")]
    public static void RunLibraryAudio(){Begin("libraryaudio");}

    [MenuItem("Saloon/Verify rematch and reconnect in Play mode")]
    public static void RunRematch(){Begin("rematch");}

    [MenuItem("Saloon/Verify role privacy and random independents in Play mode")]
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
        running=cleaning=false;started=0;
        SaloonProject.OpenForEditing();SessionState.SetString(Mode,mode);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        if(mode=="dice"||mode=="blackjack"||mode=="coin"||mode=="planning"||mode=="storymaps"||mode=="safe-highlight")
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
        string mode=SessionState.GetString(Mode,"");
        if(!running)
        {
            running=true;started=EditorApplication.timeSinceStartup;game.AutomatedInput=true;game.Port="18112";
            bool lobby=mode=="breakables"||mode=="shooting"||mode=="rematch"||mode=="lobby"||mode=="disconnect"||mode=="recovery";
            if(lobby)game.SetBotsEnabled(false);
            if(mode!="solo")game.HostLan(!lobby);
            game.StartCoroutine(mode=="breakables"?game.VerifyBreakableProps():mode=="dice"?game.VerifyBluffDice():mode=="toilet"?game.VerifyToiletDuty():mode=="bar-lighting"?game.VerifyBarLighting():mode=="endless"?game.VerifyEndlessMatch():mode=="medical"?game.VerifyMedicalSupplies():mode=="cash-drawer"?game.VerifyCashDrawer():mode=="beer-scene"?game.VerifyBarAndBeer():mode=="shooting"?game.VerifyMultiplayerShooting():mode=="voice-controls"?game.VerifyVoiceControls():mode=="solo-smoking"?game.VerifySoloSmokingAccess():mode=="public-floor"?game.VerifyPublicFloor():mode=="safe-highlight"?game.VerifySafeHighlight():mode=="safe-access"?game.VerifySafeAccess():mode=="storymaps"?game.VerifyStoryMaps():mode=="bar-layout"?game.VerifyBarAccessAndSafe():mode=="sunset"?game.VerifySunsetOverlook():mode=="crouch"?game.VerifyCrouchAndWeapon():mode=="model-room-fixes"?game.VerifyModelRoomFixes():mode=="libraryaudio"?game.VerifyLibraryAudio():mode=="cigarettebuff"?game.VerifyCigaretteBuff():mode=="health"?game.VerifyPercentageHealth():mode=="knowledge"?game.VerifyRenegadeKnowledge():mode=="rematch"?game.VerifyRematchAndReconnect():mode=="solo"?game.VerifySoloPractice():mode=="coin"?game.VerifyTabletopCoin():mode=="smoking"?game.VerifySmoking():mode=="windows"?game.VerifyRoomWindows():mode=="blackjack"?game.VerifyTabletopBlackjack():mode=="evidence"?game.VerifyEvidence():mode=="recovery"?game.VerifyLanRecovery():mode=="disconnect"?game.VerifyDisconnectDetails():mode=="planning"?game.VerifyPlanningDraft():mode=="botmovement"?game.VerifyBotMovement():mode=="roomactions"?game.VerifyRoomActions():mode=="combat"?game.VerifyHitReactionsAndGrip():mode=="western"?game.VerifyWesternCharacter():mode=="lobby"?game.VerifyEditorLobby():mode=="ragdoll"?game.VerifyEditorRagdoll():game.VerifyEditorRoleCardsAndDraw());
        }
        bool timeout=EditorApplication.timeSinceStartup-started>180;
        if(!game.VerificationFinished&&!timeout)return;
        if(!cleaning){cleaning=true;game.Leave();return;}
        if(game.InRoom||game.View!=null)return;
        Debug.Log(game.VerificationPassed&&!timeout?"SALOON_EDITOR_ITERATION PASS":"SALOON_EDITOR_ITERATION FAIL");
        bool passed=game.VerificationPassed&&!timeout;
        SessionState.SetBool(Pending,false);running=cleaning=false;EditorApplication.ExitPlaymode();
        if((Application.isBatchMode||Array.IndexOf(Environment.GetCommandLineArgs(),"-saloonVerifyExit")>=0)&&(mode=="breakables"||mode=="dice"||mode=="toilet"||mode=="bar-lighting"||mode=="endless"||mode=="medical"||mode=="cash-drawer"||mode=="shooting"||mode=="roomactions"||mode=="public-floor"||mode=="lobby"||mode=="storymaps"||mode=="sunset"||mode=="crouch"||mode=="ragdoll"||mode=="model-room-fixes"||mode=="speech"||mode=="western"||mode=="combat"||mode=="knowledge"||mode=="rematch"||mode=="solo"||mode=="coin"||mode=="blackjack"||mode=="planning"||mode=="windows"||mode=="smoking"||mode=="solo-smoking"||mode=="voice-controls"))EditorApplication.Exit(passed?0:1);
    }
}
