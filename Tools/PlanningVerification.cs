// Test-only substitutes for Unity's clock and presentation APIs. Business logic is
// compiled directly from Assets/Scripts by verify_planning.py.
using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine
{
    public static class Time { public static float time; }
    public static class Mathf { public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b); }
    public static class Debug { public static void Log(string value){} }
    public class HideInInspector:Attribute {}
    public static class JsonUtility
    {
        public static string ToJson(object value)=>throw new NotSupportedException();
        public static T FromJson<T>(string value)=>throw new NotSupportedException();
    }
}
namespace UnityEngine.Serialization { public class FormerlySerializedAsAttribute:Attribute {public FormerlySerializedAsAttribute(string name){}} }
namespace Saloon
{
    public class DuelView {public string stage,firstName;public bool canHit,canDeal,waitingForOpening;}
    public class RoomTurnView {public bool spent,yourTurn;public string currentId,currentName;public string[] order;public float remaining;}
    public static class SaloonStations { public static string Name(int room)=>new[]{"Bar","Coin Flip","Blackjack","Have a Smoke"}[room]; }
    public partial class SaloonGame
    {
        class Participant
        {
            public ulong id;public string name,notice="";public int hp=100,room,chosenRoom,planVersion;
            public bool bot,bag,planSelected,gatherStation=true;public ulong? companion;public Role role;
            public float busyUntil;public string busyAction="";public int escapeRound;
            public bool cigaretteBuff,drawn;public int beer,beerHealing,drinkHealing;public float beerChoiceUntil;
            public bool Alive=>hp>0;
        }
        class TestConnection { public Command sent;public void Send(Command command){sent=command;} }
        readonly Dictionary<ulong,Participant> players=new Dictionary<ulong,Participant>();
        readonly SaloonRounds rounds=new SaloonRounds();
        readonly TestConnection Connection=new TestConnection();
        readonly Tuning tuning=new Tuning();
        void BeginPour(Participant player){} // Pour geometry is covered by the Unity verification.
        class Duel {public bool revealed;public float combatAt;public ulong first;}
        class RoomTurn {public ulong[] order;public int index;public float seconds,until;}
        readonly Dictionary<int,Duel> duels=new Dictionary<int,Duel>();
        readonly Dictionary<int,RoomTurn> roomTurns=new Dictionary<int,RoomTurn>();
        string pendingOutcome="";
        void QueueFinish(string value){pendingOutcome=value;}
        static readonly int[] PracticeRoomCycle={0,1,1,2,3};
        Phase phase;bool Sandbox,Hosting=true;public bool RoomPlannerOpen=true;Snapshot View;
        PlayerView Self=>View?.players.First(p=>p.id=="1");
        bool ObserverReady(Participant player)=>false;
        static int checks;
        static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
        static SaloonGame Fixture(int count=5,bool sandbox=false)
        {
            UnityEngine.Time.time=0;
            var game=new SaloonGame{Sandbox=sandbox};
            for(ulong id=1;id<=(ulong)count;id++)game.players[id]=new Participant{id=id,name="Player "+id,bot=sandbox&&id>1};
            game.rounds.Start(game.players.Keys,0,30,30,0);game.phase=Phase.Discussion;game.Publish();return game;
        }
        void Publish()
        {
            View=new Snapshot{phase=phase,round=rounds.Round,ownId="1",players=players.Values.Select(p=>new PlayerView{id=p.id.ToString(),alive=p.Alive,planSelected=p.planSelected,chosenRoom=p.chosenRoom,companion=p.companion?.ToString()??"",ready=rounds.IsReady(p.id),planVersion=p.planVersion}).ToArray()};
            SyncPlanDraft(View,Self);
        }
        void Invite(ulong from,ulong to,int room)
        {var p=players[from];Check(CanSelectPlan(p,room,to),"fixture invitation is available");SetRoomPlan(p,room,to);p.planVersion++;}
        void Join(ulong recipient,RoomInvitation invitation)
        {AcceptRoomInvitation(players[recipient],new Command{kind="joinInvite",name=invitation.fromId,x=invitation.room,invitationVersion=invitation.planVersion,planVersion=players[recipient].planVersion+1},true);}
        static void TestSharedOutlawMap()
        {
            var view=new Snapshot{ownRole="Outlaw",phase=Phase.Turns,health=100,round=3,
                storyProgress=StoryProgress.Bar|StoryProgress.SafeWork,safeProgress=3,outlawTeamMoneyBag=true};
            StoryState State(string title)=>SaloonStoryMap.Build(view).Nodes.Single(n=>n.Title==title).State;
            Check(State("Take the money")==StoryState.Complete&&!view.moneyBag,"partner's money completes the map without granting possession");
            Check(State("Declare escape")==StoryState.Next&&SaloonStoryMap.Build(view).NextDetail.Contains("fellow Outlaw"),"noncarrier gets team support guidance");
            view.outlawTeamEscapePending=true;
            Check(State("Declare escape")==StoryState.Complete&&SaloonStoryMap.Build(view).NextTitle=="Ride out alive","partner's escape declaration advances both maps");
            view.outlawTeamEscapePending=false;
            Check(State("Declare escape")==StoryState.Next,"cancelled partner escape reopens the declaration milestone");
            view.outlawTeamMoneyBag=false;
            Check(State("Take the money")==StoryState.Next,"losing the team's money restores recovery");
            view.moneyBag=view.escapePending=true;
            Check(State("Declare escape")==StoryState.Complete,"single Outlaw personal progress still works");
        }

        public static void Main()
        {
            TestClock();TestAssignments();TestReservations();TestPractice();TestClient();TestConcurrentBar();TestCigaretteHealing();TestPosseTiming();TestSharedOutlawMap();
            SaloonRoomReservationVerification.Run();
            foreach(Role viewer in Enum.GetValues(typeof(Role)))foreach(Role subject in Enum.GetValues(typeof(Role)))
                Check(SaloonRules.KnowsRole(viewer,subject)==(viewer==Role.Renegade||(viewer==Role.Sheriff&&subject==Role.Deputy)||(viewer==Role.Outlaw&&subject==Role.Outlaw)),"private roles retain their permissions");
            Console.WriteLine("PASS: "+checks+" checks (planning, concurrent Bar actions, escape timing, side-room openings and cigarette healing).");
        }
        static void TestCigaretteHealing()
        {
            foreach(int poured in new[]{5,10,15,20})
            {
                var g=Fixture();var player=g.players[1];player.hp=40;player.cigaretteBuff=true;player.beer=1;player.beerHealing=poured;player.beerChoiceUntil=6;
                foreach(var p in g.players.Values)g.rounds.SetReady(p.id,true);g.rounds.Tick(0,id=>true);g.phase=Phase.Turns;
                // A completed pour has already reserved this action.
                g.rounds.TrySpend(player.id,0,6);
                Check(SaloonHealth.BeerHealingAmount(poured,true)==poured*2&&player.cigaretteBuff,"healing preview doubles the pour without consuming the buff");
                g.DrinkBeer(player);
                Check(player.drinkHealing==poured*2&&!player.cigaretteBuff&&player.beer==0&&player.busyAction=="drink","drinking commits the doubled poured amount and consumes one buff");
                Check(SaloonHealth.AfterBeer(player.hp,player.drinkHealing)==Math.Min(100,40+poured*2),"boosted healing is not truncated to the normal 20-point limit");
                Check(g.PrepareBeerHealing(player,poured)==poured,"the next beer returns to normal healing");
            }
            for(int hp=0;hp<=100;hp++)foreach(int poured in new[]{5,10,15,20})
            {
                int healed=SaloonHealth.AfterBeer(hp,SaloonHealth.BeerHealingAmount(poured,true));
                Check(healed==(hp==0?0:Math.Min(100,hp+poured*2)),"boosted healing caps at 100 and never revives a dead player");
            }
            var game=Fixture();foreach(var p in game.players.Values)game.rounds.SetReady(p.id,true);game.rounds.Tick(0,id=>true);game.phase=Phase.Turns;
            var drinker=game.players[1];drinker.hp=100;drinker.cigaretteBuff=true;drinker.beer=1;drinker.beerHealing=20;
            game.DrinkBeer(drinker);Check(drinker.cigaretteBuff&&drinker.beer==1&&!game.rounds.HasSpent(drinker.id),"full-health rejection preserves the beer, buff and action");
            drinker.hp=40;game.rounds.TrySpend(drinker.id,0);game.DrinkBeer(drinker);
            Check(drinker.cigaretteBuff&&drinker.beer==1,"a rejected spent action does not consume the cigarette buff");
            drinker.beerChoiceUntil=6;game.LeaveBeer(drinker);Check(drinker.cigaretteBuff&&drinker.beer==0,"leaving beer preserves the unused buff");
            Check(game.PrepareBeerHealing(drinker,0)==0&&drinker.cigaretteBuff,"zero-healing beer does not waste the buff");
            var giver=game.players[2];giver.cigaretteBuff=true;
            Check(game.PrepareBeerHealing(drinker,15)==30&&!drinker.cigaretteBuff&&giver.cigaretteBuff,"gifted beer uses only the drinker's buff");
            drinker.cigaretteBuff=true;drinker.hp=100;
            Check(game.PrepareBeerHealing(drinker,20)==20&&drinker.cigaretteBuff,"a full-health gift recipient does not waste a buff");
        }
        static void TestConcurrentBar()
        {
            var g=Fixture();foreach(var p in g.players.Values)g.rounds.SetReady(p.id,true);
            g.rounds.Tick(0,id=>true);g.phase=Phase.Turns;
            Check(g.players.Values.All(p=>g.DuelAllows(p)&&g.ActionBlockReason(p)==""),"every Bar player can act immediately");
            // Even stale legacy queue state must not reintroduce Bar waits or timeouts.
            g.roomTurns[0]=new RoomTurn{order=g.players.Keys.Reverse().ToArray(),until=-1,seconds=10};
            g.AdvanceRoomTurn(0);
            Check(g.players.Values.All(p=>g.DuelAllows(p)&&g.PrivateRoomTurn(p)==null&&!g.rounds.HasSpent(p.id)),"stale Bar queues cannot block, time out or appear for any player");
            Check(g.rounds.Deadline==30,"Bar queue does not extend the shared deadline");
            var robber=g.players[1];robber.role=Role.Outlaw;robber.bag=true;
            g.DeclareEscape(robber);
            Check(robber.escapeRound==1&&g.rounds.HasSpent(1),"a money carrier can declare escape before every other Bar player acts");
            Check(g.players.Values.Where(p=>p!=robber).All(p=>g.ActionBlockReason(p)==""),"escape declaration leaves everyone else's action immediately available");
            g.ResolveEscapes();Check(g.pendingOutcome=="","escape cannot complete in its declaration round");
            foreach(var p in g.players.Values.Where(p=>p!=robber).Reverse())Check(g.SpendAction(p),"Bar players can act in any order");
            Check(!g.SpendAction(robber),"queue removal preserves one action per round");
            g.rounds.Tick(0,id=>true);g.rounds.Tick(3,id=>true);g.rounds.Tick(6,id=>true);
            foreach(var p in g.players.Values)g.rounds.SetReady(p.id,true);
            g.rounds.Tick(6,id=>true);g.phase=g.rounds.Phase;UnityEngine.Time.time=6;
            Check(g.rounds.Round==2&&g.phase==Phase.Turns,"escape response round starts normally");
            foreach(var p in g.players.Values)g.rounds.TrySpend(p.id,6);
            g.rounds.Tick(6,id=>true);g.phase=g.rounds.Phase;
            g.ResolveEscapes();Check(g.pendingOutcome=="OUTLAWS WIN — MONEY ESCAPED","surviving the response round still completes escape");

            g=Fixture();foreach(var p in g.players.Values)g.rounds.SetReady(p.id,true);g.rounds.Tick(0,id=>true);g.phase=Phase.Turns;
            robber=g.players[1];robber.role=Role.Outlaw;robber.bag=true;g.SpendAction(robber);g.DeclareEscape(robber);
            Check(robber.escapeRound==0&&robber.notice.Contains("next round"),"money collected with a spent action explains the next-round escape timing");
            var deputy=g.players[2];deputy.role=Role.Deputy;deputy.bag=true;g.DeclareEscape(deputy);
            Check(deputy.escapeRound==0&&!g.rounds.HasSpent(2),"only Outlaws can declare escape");
            deputy.busyUntil=1;Check(!g.SpendAction(deputy)&&!g.rounds.HasSpent(2),"concurrent actions still respect the player's own busy animation");deputy.busyUntil=0;
            var outlaw=g.players[3];outlaw.role=Role.Outlaw;outlaw.bag=true;outlaw.room=1;g.DeclareEscape(outlaw);
            Check(outlaw.escapeRound==0&&!g.rounds.HasSpent(3),"escape still requires carrying money at the Bar");

            var first=g.players[4];var second=g.players[5];first.room=second.room=2;
            var duel=new Duel{first=first.id,combatAt=10};g.duels[2]=duel;
            Check(!g.DuelAllows(first)&&!g.DuelAllows(second),"side-room minigames still lock combat before the result");
            duel.revealed=true;Check(!g.DuelAllows(first),"side-room reaction pause remains enforced");
            UnityEngine.Time.time=10;Check(g.DuelAllows(first)&&!g.DuelAllows(second),"side-room winner retains the exclusive opening");
            g.rounds.TrySpend(first.id,10);Check(g.DuelAllows(second),"side-room opponent can respond after the opening");
        }
        static void TestClock()
        {
            var r=new SaloonRounds();var ids=new ulong[]{1,2,3,4,5};r.Start(ids,10,30,30,0);
            Check(r.Deadline==40,"discussion has a 30-second timer");r.Tick(39,id=>true);Check(r.Phase==Phase.Discussion,"idle discussion lasts until deadline");
            r.Tick(40,id=>true);Check(r.Phase==Phase.Turns&&r.Deadline==70,"timer starts actions with a fresh full window");
            foreach(var id in ids)r.TrySpend(id,40);r.Tick(40,id=>true);r.Tick(43,id=>true);r.Tick(46,id=>true);
            Check(r.Phase==Phase.Discussion&&r.Deadline==76&&ids.All(id=>!r.IsReady(id)),"new discussion resets timer and locks");
            foreach(var id in ids)r.SetReady(id,true);r.Tick(47,id=>true);Check(r.Phase==Phase.Turns,"everyone locked starts early");
            r.Start(ids,0,30,30,0);foreach(var id in ids.Take(4))r.SetReady(id,true);r.Tick(1,id=>id!=5);
            Check(r.Phase==Phase.Turns,"eliminated players never block an early start");
        }
        static void TestPosseTiming()
        {
            for(int round=1;round<=17;round++)
                Check(SaloonPosse.Stage(round,Phase.Turns)==0&&!SaloonPosse.Arrives(round),"no posse pressure or arrival through round 17");
            Check(SaloonPosse.RoundsRemaining(18,Phase.Turns)==3&&SaloonPosse.RoundsRemaining(19,Phase.Turns)==2&&SaloonPosse.RoundsRemaining(20,Phase.Turns)==1,"final three complete rounds count down 3, 2, 1");
            Check(SaloonPosse.Stage(20,Phase.Aftermath)==4&&SaloonPosse.Arrives(20),"posse arrives only after the twentieth action round");
            SaloonGame AtRound(int target)
            {
                var g=Fixture();float now=0;
                for(int round=1;round<=target;round++)
                {
                    UnityEngine.Time.time=now;foreach(var id in g.players.Keys)g.rounds.SetReady(id,true);
                    g.rounds.Tick(now,id=>true);g.phase=g.rounds.Phase;
                    if(round==target)break;
                    foreach(var id in g.players.Keys)g.rounds.TrySpend(id,now);
                    g.rounds.Tick(now,id=>true);g.rounds.Tick(now+3,id=>true);g.rounds.Tick(now+6,id=>true);now+=6;
                }
                return g;
            }
            var finalChance=AtRound(19);var robber=finalChance.players[1];robber.role=Role.Outlaw;robber.bag=true;
            finalChance.DeclareEscape(robber);
            Check(robber.escapeRound==19&&finalChance.rounds.HasSpent(1),"real round-19 escape declaration is accepted and spends an action");
            var tooLate=AtRound(20);robber=tooLate.players[1];robber.role=Role.Outlaw;robber.bag=true;
            tooLate.DeclareEscape(robber);
            Check(robber.escapeRound==0&&!tooLate.rounds.HasSpent(1)&&robber.notice.Contains("too late"),"real round-20 escape declaration is rejected without spending an action");
        }
        static void TestAssignments()
        {
            for(int count=1;count<=5;count++)for(int mask=0;mask<(1<<(count*2));mask++)for(int sideCapacity=0;sideCapacity<=2;sideCapacity++)
            {
                var ids=Enumerable.Range(1,count).Select(i=>(ulong)i).ToArray();var r=new SaloonRounds();r.Start(ids,0,30,30,0);
                foreach(var id in ids)r.SetReady(id,true);
                var assigned=r.AssignRooms(ids,id=>(mask>>((int)(id-1)*2))&3,new[]{5,sideCapacity,sideCapacity,sideCapacity},true);
                Check(assigned.Count==count&&ids.All(assigned.ContainsKey),"assignment keeps everyone exactly once");
                Check(assigned.Where(p=>p.Value==1||p.Value==2).GroupBy(p=>p.Value).All(g=>g.Count()>=2),"only minigames require two living players");
                Check(assigned.Where(p=>p.Value>0).GroupBy(p=>p.Value).All(g=>g.Count()<=sideCapacity),"assignment respects reduced room capacities");
                Check(assigned.All(p=>p.Value==0||p.Value==((mask>>((int)(p.Key-1)*2))&3)),"fallback never sends a player into a room they did not choose, including private smoking");
                Check(ids.Where(id=>((mask>>((int)(id-1)*2))&3)==0).All(id=>assigned[id]==0),"a Bar choice is never reassigned to provide company elsewhere");
            }
            foreach(bool early in new[]{false,true})
            {
                var solo=Fixture(sandbox:true);solo.SetRoomPlan(solo.players[1],3,null);
                Check(solo.TryReady(solo.players[1],true),"solo smoking plan locks without an invitation");
                solo.PlanPracticeBots();
                if(early)foreach(var p in solo.players.Values)solo.rounds.SetReady(p.id,true);
                solo.rounds.Tick(early?1:30,id=>true);
                Check(solo.rounds.Phase==Phase.Turns,"solo smoking starts via early lock or discussion timeout");
                var rooms=solo.rounds.AssignRooms(solo.players.Keys,id=>solo.rounds.IsReady(id)?solo.players[id].chosenRoom:0,solo.RoomCapacities(),true);
                Check(rooms[1]==3&&solo.players[1].companion==null,"locked solo smoker reaches the deck without a recruited companion");
            }
            var g=Fixture();foreach(var p in g.players.Values)g.rounds.SetReady(p.id,true);
            g.players[1].chosenRoom=g.players[2].chosenRoom=2;g.players[3].chosenRoom=g.players[4].chosenRoom=3;
            var result=g.rounds.AssignRooms(g.players.Keys,id=>g.players[id].chosenRoom,null,true);
            Check(result[1]==2&&result[2]==2&&result[3]==3&&result[4]==3&&result[5]==0,"a solo Bar player leaves both confirmed side-room pairs in place");
            g.players[1].chosenRoom=0;g.players[2].chosenRoom=1;g.players[3].chosenRoom=2;g.players[4].chosenRoom=g.players[5].chosenRoom=3;
            result=g.rounds.AssignRooms(g.players.Keys,id=>g.players[id].chosenRoom,null,true);
            Check(result[1]==0&&result[2]==0&&result[3]==0&&result[4]==3&&result[5]==3,"unmatched side rooms return to Bar while an intentional smoking pair remains private");
        }
        static void TestReservations()
        {
            var g=Fixture();g.Invite(1,2,2);Check(!g.TryReady(g.players[1],true),"sending an invitation cannot lock someone without consent");
            Check(g.players[1].planSelected&&!g.players[3].planSelected,"explicit room choices are distinguishable from players who have not selected anything");
            Check(!g.AcceptPlanCommand(g.players[3],new Command{planVersion=100,planRound=2}),"a command for another round cannot edit the current plan");
            g.Invite(3,2,2);var stale=g.InvitationsFor(g.players[2]).Single(i=>i.fromId=="3");
            var offer=g.InvitationsFor(g.players[2]).Single(i=>i.fromId=="1");g.Join(2,offer);
            Check(g.rounds.IsReady(1)&&g.rounds.IsReady(2),"acceptance locks both seats atomically");
            Check(g.InvitationsFor(g.players[2]).Length==0&&g.InvitationsFor(g.players[1]).Length==0,"locked players have no actionable invitations");
            g.Join(2,stale);Check(g.players[2].companion==1&&g.rounds.IsReady(1)&&!g.rounds.IsReady(3),"stale competing acceptance cannot steal a locked partner");
            Check(!g.CanSelectPlan(g.players[3],1,2),"locked partner is unavailable even in another room");
            Check(!g.TryReady(g.players[3],true)&&!g.rounds.IsReady(3),"third player cannot reserve a full room");
            Check(!g.CanSelectPlan(g.players[4],2,null),"full room is unavailable");
            g.TryReady(g.players[1],false);
            Check(!g.rounds.IsReady(1)&&!g.rounds.IsReady(2)&&g.players[1].companion==null&&g.players[2].companion==null,"unlock releases both partners cleanly");
            g.Invite(4,5,2);var changed=g.InvitationsFor(g.players[5]).Single();g.SetRoomPlan(g.players[4],3,null);g.players[4].planVersion++;
            g.Join(5,changed);Check(!g.rounds.IsReady(5)&&g.players[5].chosenRoom==0,"changed invitation never moves or locks its recipient");
            g.SetRoomPlan(g.players[1],1,null);g.TryReady(g.players[1],true);
            Check(!g.CanSelectPlan(g.players[2],1,1),"locked unpaired choice cannot be targeted by an invitation");
            UnityEngine.Time.time=30;Check(!g.AcceptPlanCommand(g.players[2],new Command{planVersion=100}),"late commands cannot change expired discussion plans");
        }
        static void TestPractice()
        {
            var g=Fixture(sandbox:true);g.PlanPracticeBots();
            var offer=g.InvitationsFor(g.players[1]).Single();
            Check(!g.rounds.IsReady(ulong.Parse(offer.fromId)),"bot invitation leaves its sender available until consent");
            var locked=g.players.Values.Where(p=>g.rounds.IsReady(p.id)).Select(p=>(p.id,p.chosenRoom,p.companion)).ToArray();
            for(int i=0;i<5;i++)g.PlanPracticeBots();
            Check(locked.All(p=>g.rounds.IsReady(p.id)&&g.players[p.id].chosenRoom==p.chosenRoom&&g.players[p.id].companion==p.companion),"bot replanning preserves locked choices");
            g.Join(1,offer);Check(g.rounds.IsReady(1)&&g.rounds.IsReady(ulong.Parse(offer.fromId)),"human accepting a bot locks both players");
            g.PlanPracticeBots();Check(g.players[1].companion==ulong.Parse(offer.fromId),"bot tick preserves accepted human pair");
            g=Fixture(sandbox:true);g.SetRoomPlan(g.players[1],3,2);g.PlanPracticeBots();
            Check(g.rounds.IsReady(1)&&g.rounds.IsReady(2)&&g.players[2].chosenRoom==3,"Go with an available bot locks the requested pair");
            foreach(int room in new[]{0,1,2,3})
            {
                g=Fixture(sandbox:true);g.SetRoomPlan(g.players[1],room,null);g.TryReady(g.players[1],true);g.PlanPracticeBots();
                Check(g.players[1].chosenRoom==room&&g.players[1].companion==null&&g.rounds.IsReady(1),"practice bots preserve a human's solo locked choice without creating a partnership");
                Check(room==0||g.players.Values.Count(p=>p.chosenRoom==room&&g.rounds.IsReady(p.id))==1,"a bot is never recruited to fill a locked solo side-room choice, including smoking");
            }
        }
        static void TestClient()
        {
            var g=Fixture();var stale=g.View;
            g.EditRoom(2,"","room");Check(g.PlannedRoom==2&&g.PlanPending,"local selection appears immediately");
            g.SyncPlanDraft(stale,stale.players[0]);Check(g.PlannedRoom==2,"stale snapshot cannot undo a newer selection");
            g.EditReady();Check(g.Connection.sent.kind=="planReady"&&g.Connection.sent.x==2,"Ready sends the latest complete plan, rather than only a toggle");
            Check(g.PlanReady&&g.RoomPlannerOpen&&g.Connection.sent.planRound==1,"lock keeps the public planner visible and identifies the round");
            var host=g.players[1];host.planVersion=g.Connection.sent.planVersion;host.notice="full";g.Publish();
            Check(!g.PlanReady&&!g.PlanPending&&g.PlannedRoom==0,"server rejection clears optimistic lock and pending state");
            g.EditRoom(3,"2","room");g.EditReady();Check(g.Connection.sent.x==3&&g.Connection.sent.name=="2","rapid edits and Ready preserve room and partner together");
        }
    }
}
