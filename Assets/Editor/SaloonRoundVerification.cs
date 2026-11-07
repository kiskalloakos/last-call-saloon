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
    static bool running,cleaningUp;
    static SaloonRoundVerification() { EditorApplication.update+=Poll; }

    [MenuItem("Saloon/Verify rounds in Play mode")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before running the verification.");
        VerifyClock();
        SessionState.SetBool(Pending,true);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Saloon/Verify round clock")]
    public static void VerifyClock()
    {
        VerifySnapshotWire();
        void Check(bool ok,string message){if(!ok)throw new Exception("Round clock: "+message);}
        Check(SaloonStations.Position(false,0,0).y+1.35f>1.6f,"bar eye clears counter");
        var r=new SaloonRounds();var living=new HashSet<ulong>{1,2,3};
        r.Start(living,0,10,10,1,5,2);Check(!r.TrySpend(1,0),"discussion lock");r.Tick(10,living.Contains);Check(r.Phase==Phase.Choosing,"choose phase");
        r.Tick(15,living.Contains);Check(r.CanAct(1,15)&&r.CanAct(2,15),"shared action window");
        Check(r.TrySpend(1,24.9f,3)&&!r.TrySpend(1,24.9f),"one action, late timer");r.Tick(25,living.Contains);Check(r.Phase==Phase.Turns,"timed action completes");
        r.Tick(28,living.Contains);Check(r.Phase==Phase.Aftermath,"aftermath after timeout");r.Tick(30,living.Contains);Check(r.Phase==Phase.Aftermath,"final effects retain three seconds");r.Tick(31,living.Contains);Check(r.Phase==Phase.Finished,"round cap after aftermath");
        r.Start(living,0,0,10,2,1,2);r.Tick(0,living.Contains);r.Tick(1,living.Contains);foreach(var id in living)r.TrySpend(id,1);r.Tick(1,living.Contains);r.Tick(4,living.Contains);Check(r.Phase==Phase.Regroup,"non-final aftermath then regroup");r.Tick(6,living.Contains);Check(r.Phase==Phase.Discussion&&r.Round==2,"next discussion after regroup");
        var rooms=new Dictionary<ulong,int>{{1,1},{2,1},{3,1}};var companions=new Dictionary<ulong,ulong?>{{1,2},{2,1},{3,null}};
        var assigned=SaloonRounds.AssignRooms(living,id=>rooms[id],id=>companions[id]);Check(assigned[1]==1&&assigned[2]==1&&assigned[3]==0,"pair capacity fallback");
        companions[2]=null;assigned=SaloonRounds.AssignRooms(living,id=>rooms[id],id=>companions[id]);Check(assigned[1]==1&&assigned[2]==1,"unaccepted invitation preserves the chosen room when there is space");
        companions[2]=1;assigned=SaloonRounds.AssignRooms(living,id=>rooms[id],id=>companions[id],new[]{5,1,2});Check(assigned.Values.Count(v=>v==1)==1,"corpse reserves one room seat");
        Debug.Log("SALOON_ROUND_CLOCK PASS");
    }

    public static void VerifySnapshotWire()
    {
        void Check(bool ok,string message){if(!ok)throw new Exception("Snapshot wire: "+message);}
        Snapshot Wire(Snapshot snapshot)=>Snapshot.FromWireJson(snapshot.ToWireJson());
        foreach(Phase phase in Enum.GetValues(typeof(Phase)))
        {
            var empty=Wire(new Snapshot{phase=phase,players=Array.Empty<PlayerView>()});
            Check(empty.duel==null&&empty.roomTurn==null,"absent panels stay absent in "+phase);
        }
        var duel=new DuelView{stage="choosing",side="HEADS",opponent="Peer",remaining=3};
        var queue=new RoomTurnView{currentId="0",currentName="Host",order=new[]{"Host","Peer","Third"},yourTurn=true};
        var pair=Wire(new Snapshot{phase=Phase.Turns,duel=duel});
        Check(pair.duel?.stage=="choosing"&&pair.duel.side=="HEADS"&&pair.roomTurn==null,"only real coin panel survives");
        duel.stage="playing";duel.yourTurn=true;
        pair=Wire(new Snapshot{phase=Phase.Turns,duel=duel,actionAvailable=true});
        Check(pair.duel.yourTurn&&pair.actionAvailable&&pair.roomTurn==null,"coin winner retains action authorization");
        var bar=Wire(new Snapshot{phase=Phase.Turns,roomTurn=queue});
        Check(bar.duel==null&&bar.roomTurn.yourTurn&&bar.roomTurn.currentId=="0"&&bar.roomTurn.order.Length==3,"bar queue survives without a phantom coin");
        queue.currentId="";queue.yourTurn=false;queue.spent=true;
        bar=Wire(new Snapshot{phase=Phase.Turns,roomTurn=queue});
        Check(bar.roomTurn.spent&&bar.roomTurn.currentId==""&&bar.duel==null,"completed queue retains its real spent state");
        foreach(var phase in new[]{Phase.Discussion,Phase.Choosing,Phase.Aftermath,Phase.Regroup,Phase.Finished})
        {
            var stale=Wire(new Snapshot{phase=phase,duel=duel,roomTurn=queue});
            Check(stale.duel==null&&stale.roomTurn==null,"phase change clears stale panels in "+phase);
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
            game.HostLan(true);game.StartCoroutine(game.VerifyGameplay());
        }
        if(!game.VerificationFinished)return;
        if(!cleaningUp){cleaningUp=true;game.Leave();return;}
        if(game.InRoom||game.View!=null)return;
        cleaningUp=false;
        SessionState.SetBool(Pending,false);running=false;
        Debug.Log(game.VerificationPassed?"SALOON_EDITOR_ROUNDS PASS":"SALOON_EDITOR_ROUNDS FAIL");
        EditorApplication.ExitPlaymode();
    }
}
