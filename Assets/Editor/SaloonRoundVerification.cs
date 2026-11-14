using System;
using System.Collections.Generic;
using System.Linq;
using Saloon;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SaloonRoundVerification
{
    const string Pending="Saloon.RoundVerification.Pending";
    const string PossePending="Saloon.PosseVerification.Pending";
    const string BeerPending="Saloon.BarBeerVerification.Pending";

    [MenuItem("Saloon/Verify bar and beer in Play mode")]
    public static void VerifyBarBeer()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before running the verification.");
        SessionState.SetBool(PossePending,false);SessionState.SetBool(BeerPending,true);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    static bool running,cleaningUp;

    [MenuItem("Saloon/Verify posse arrival in Play mode")]
    public static void VerifyPosse()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before running the verification.");
        VerifyClock();SessionState.SetBool(BeerPending,false);SessionState.SetBool(PossePending,true);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    static SaloonRoundVerification() { EditorApplication.update+=Poll; }

    [MenuItem("Saloon/Verify rounds in Play mode")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before running the verification.");
        VerifyClock();
        SessionState.SetBool(BeerPending,false);SessionState.SetBool(PossePending,false);
        SessionState.SetBool(Pending,true);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Saloon/Verify round clock")]
    public static void VerifyClock()
    {
        VerifySnapshotWire();SaloonSnapshotVerification.Run();SaloonBlackjackVerification.Run();
        void Check(bool ok,string message){if(!ok)throw new Exception("Round clock: "+message);}
        Check(SaloonStations.Position(false,0,0).y+1.35f>1.6f,"bar eye clears counter");
        var r=new SaloonRounds();var living=new HashSet<ulong>{1,2,3};
        r.Start(living,0,10,10,1,5,2);Check(!r.TrySpend(1,0),"discussion lock");
        r.Tick(9,living.Contains);Check(r.Phase==Phase.Discussion,"discussion waits until its deadline");
        r.Tick(10,living.Contains);Check(r.Phase==Phase.Turns,"discussion timer starts actions without every Ready");
        r.Start(living,2,10,10,1,5,2);
        r.SetReady(1,true);r.SetReady(2,true);r.Tick(3,living.Contains);Check(r.Phase==Phase.Discussion,"one unready living player blocks early start");
        r.SetReady(1,false);r.SetReady(3,true);r.Tick(11,living.Contains);Check(r.Phase==Phase.Discussion,"unready can revoke consent");
        r.SetReady(1,true);r.Tick(12,living.Contains);Check(r.Phase==Phase.Turns&&r.CanAct(1,12)&&r.CanAct(2,12),"all ready starts room actions directly");
        Check(r.TrySpend(1,21.9f,3)&&!r.TrySpend(1,21.9f),"one action, late timer");r.Tick(22,living.Contains);Check(r.Phase==Phase.Turns,"timed action completes");
        r.Tick(25,living.Contains);Check(r.Phase==Phase.Aftermath,"aftermath after timeout");r.Tick(27,living.Contains);Check(r.Phase==Phase.Aftermath,"effects retain three seconds");r.Tick(28,living.Contains);Check(r.Phase==Phase.Regroup,"legacy round cap cannot end aftermath");
        r.Tick(30,living.Contains);Check(r.Phase==Phase.Discussion&&r.Round==2,"legacy one-round cap still starts round two");
        for(int next=3;next<=100;next++)
        {
            float now=next*20;foreach(var id in living)r.SetReady(id,true);r.Tick(now,living.Contains);
            Check(r.Phase==Phase.Turns,"endless round starts actions");
            foreach(var id in living)Check(r.TrySpend(id,now),"endless round resets spent actions");
            r.Tick(now,living.Contains);r.Tick(now+3,living.Contains);
            Check(r.Phase==Phase.Regroup,"endless aftermath regroups");r.Tick(now+5,living.Contains);
            Check(r.Phase==Phase.Discussion&&r.Round==next&&living.All(id=>!r.IsReady(id)),"endless discussion clears readiness");
        }
        r.Start(living,0,0,10,2,1,2);foreach(var id in living)r.SetReady(id,true);r.Tick(1,living.Contains);
        foreach(var id in living)r.TrySpend(id,1);r.Tick(1,living.Contains);r.Tick(4,living.Contains);Check(r.Phase==Phase.Regroup,"aftermath then regroup");
        r.Tick(6,living.Contains);Check(r.Phase==Phase.Discussion&&r.Round==2&&living.All(id=>!r.IsReady(id)),"next discussion clears readiness");
        living.Remove(3);r.SetReady(1,true);r.SetReady(2,true);r.Tick(7,living.Contains);Check(r.Phase==Phase.Turns,"eliminated players never block ready");living.Add(3);
        r.Start(living,0,30,30,1);foreach(var id in living)r.SetReady(id,true);
        r.Tick(0,living.Contains,id=>id!=3);r.TrySpend(1,1);r.Tick(1,living.Contains,id=>id!=3);
        Check(r.Phase==Phase.Turns,"a genuinely unused action keeps the round open");
        r.TrySpend(2,1,2);r.Tick(1,living.Contains,id=>id!=3);
        Check(r.Phase==Phase.Turns,"last timed action finishes before early completion");
        r.Tick(3,living.Contains,id=>id!=3);
        Check(r.Phase==Phase.Aftermath&&!r.HasSpent(3),"smoking never holds up completed indoor actions or spends a fake action");
        r.Start(living,0,30,30,1);foreach(var id in living)r.SetReady(id,true);r.Tick(0,living.Contains);
        r.TrySpend(1,1);r.TrySpend(2,1);r.Tick(30,living.Contains);
        Check(r.Phase==Phase.Aftermath,"deadline still ends a round with an idle player");
        for(int round=1;round<=17;round++)
            Check(SaloonPosse.Stage(round,Phase.Discussion)==0&&SaloonPosse.Stage(round,Phase.Turns)==0&&SaloonPosse.Stage(round,Phase.Aftermath)==0&&!SaloonPosse.Arrives(round),"no early posse pressure in round "+round);
        Check(SaloonPosse.Stage(17,Phase.Regroup)==1,"posse warning starts as round 18 approaches");
        Check(SaloonPosse.RoundsRemaining(18,Phase.Discussion)==3&&SaloonPosse.RoundsRemaining(19,Phase.Turns)==2&&SaloonPosse.RoundsRemaining(20,Phase.Turns)==1,"rounds 18–20 are three full warning rounds");
        Check(SaloonPosse.Stage(20,Phase.Aftermath)==4&&SaloonPosse.Arrives(20)&&!SaloonPosse.CanEscape(20)&&SaloonPosse.CanEscape(19),"arrival waits for round 20 actions and round 19 is the last escape declaration");
        Check(SaloonPosse.ArrivalOutcome(new[]{Role.Sheriff,Role.Outlaw}).StartsWith("LAW WINS")&&SaloonPosse.ArrivalOutcome(new[]{Role.Deputy,Role.Renegade}).StartsWith("LAW WINS"),"either surviving Law role can win at arrival");
        Check(SaloonPosse.ArrivalOutcome(new[]{Role.Outlaw,Role.Renegade}).StartsWith("DRAW")&&SaloonPosse.ArrivalOutcome(new[]{Role.Outlaw}).StartsWith("DRAW"),"uncleared criminals are captured");
        Check(SaloonPosse.ArrivalOutcome(new[]{Role.Renegade}).StartsWith("RENEGADE WINS")&&SaloonPosse.ArrivalOutcome(Array.Empty<Role>())=="DRAW — NO SURVIVORS","elimination outcomes retain priority");
        SaloonRoomReservationVerification.Run();
        Debug.Log("SALOON_ROUND_CLOCK PASS");
    }

    public static void VerifySnapshotWire()
    {
        void Check(bool ok,string message){if(!ok)throw new Exception("Snapshot wire: "+message);}
        Snapshot Wire(Snapshot snapshot)=>Snapshot.FromWireJson(snapshot.ToWireJson());
        foreach(Phase phase in Enum.GetValues(typeof(Phase)))
        {
            var empty=Wire(new Snapshot{phase=phase,players=Array.Empty<PlayerView>()});
            Check(empty.duel==null&&empty.roomTurn==null&&empty.blackjackTable==null&&empty.coinTable==null,"absent panels stay absent in "+phase);
        }
        var duel=new DuelView{stage="choosing",side="HEADS",opponent="Peer",remaining=3};
        var queue=new RoomTurnView{currentId="0",currentName="Host",order=new[]{"Host","Peer","Third"},yourTurn=true};
        var pair=Wire(new Snapshot{phase=Phase.Turns,duel=duel});
        Check(pair.duel?.stage=="choosing"&&pair.duel.side=="HEADS"&&pair.roomTurn==null,"only real coin panel survives");
        duel.stage="playing";duel.yourTurn=true;
        pair=Wire(new Snapshot{phase=Phase.Turns,duel=duel,actionAvailable=true});
        Check(pair.duel.yourTurn&&pair.actionAvailable&&pair.roomTurn==null,"coin winner retains action authorization");
        var publicTable=new DuelView{game="blackjack",stage="blackjack",dealerHand=new[]{12,-1},playerHand=new[]{2,3},dealerTotal=-1};
        var watching=Wire(new Snapshot{phase=Phase.Turns,blackjackTable=publicTable});
        Check(watching.duel==null&&watching.roomTurn==null&&watching.blackjackTable.dealerHand[1]==-1&&!watching.blackjackTable.canHit&&!watching.blackjackTable.canDeal,"public table preserves visible cards without private controls or hole card");
        var publicCoin=new DuelView{game="coin",stage="flipping",coinAId="0",coinBId="1",headsId="1",coinRound=2,coinElapsed=.5f,coinDuration=2.2f,result="",firstId=""};
        watching=Wire(new Snapshot{phase=Phase.Turns,coinTable=publicCoin});
        Check(watching.duel==null&&watching.coinTable.coinAId=="0"&&watching.coinTable.headsId=="1"&&watching.coinTable.coinRound==2&&watching.coinTable.coinElapsed==.5f&&watching.coinTable.result==""&&!watching.coinTable.canChooseCoin&&!watching.coinTable.yourTurn,"public coin preserves synchronized toss without controls or an early result");
        var bar=Wire(new Snapshot{phase=Phase.Turns,roomTurn=queue});
        Check(bar.duel==null&&bar.roomTurn.yourTurn&&bar.roomTurn.currentId=="0"&&bar.roomTurn.order.Length==3,"bar queue survives without a phantom coin");
        queue.currentId="";queue.yourTurn=false;queue.spent=true;
        bar=Wire(new Snapshot{phase=Phase.Turns,roomTurn=queue});
        Check(bar.roomTurn.spent&&bar.roomTurn.currentId==""&&bar.duel==null,"completed queue retains its real spent state");
        foreach(var phase in new[]{Phase.Discussion,Phase.Choosing,Phase.Aftermath,Phase.Regroup,Phase.Finished})
        {
            var stale=Wire(new Snapshot{phase=phase,duel=duel,roomTurn=queue,blackjackTable=publicTable,coinTable=publicCoin});
            Check(stale.duel==null&&stale.roomTurn==null&&stale.blackjackTable==null&&stale.coinTable==null,"phase change clears stale panels in "+phase);
        }
        Debug.Log("SALOON_SNAPSHOT_WIRE PASS");
    }

    static void Poll()
    {
        if(!SessionState.GetBool(Pending,false)||EditorApplication.isCompiling)return;
        if(!EditorApplication.isPlaying)return;
        var game=UnityEngine.Object.FindAnyObjectByType<SaloonGame>();if(game==null)return;
        if(!running)
        {
            running=true;game.AutomatedInput=true;game.Port="17987";
            game.tuning.discussionSeconds=30;game.tuning.turnSeconds=8;game.tuning.roundLimit=6;
            game.HostLan(true);game.StartCoroutine(SessionState.GetBool(BeerPending,false)?game.VerifyBarAndBeer():SessionState.GetBool(PossePending,false)?game.VerifyPosseArrival():game.VerifyGameplay());
        }
        if(!game.VerificationFinished)return;
        if(!cleaningUp){cleaningUp=true;game.Leave();return;}
        if(game.InRoom||game.View!=null)return;
        cleaningUp=false;
        SessionState.SetBool(Pending,false);SessionState.SetBool(PossePending,false);SessionState.SetBool(BeerPending,false);running=false;
        Debug.Log(game.VerificationPassed?"SALOON_EDITOR_ROUNDS PASS":"SALOON_EDITOR_ROUNDS FAIL");
        EditorApplication.ExitPlaymode();
    }
}
