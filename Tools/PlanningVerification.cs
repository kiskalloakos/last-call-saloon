// Test-only substitutes for Unity's clock and presentation APIs. Business logic is
// compiled directly from Assets/Scripts by verify_planning.py.
using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine
{
    public static class Time { public static float time; }
    public static class Mathf { public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public const float Rad2Deg=57.29578f;public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x); }
    public struct Vector2 { public static Vector2 zero=>default; }
    public struct Vector3 {public float x,y,z;public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3{x=a.x-b.x,y=a.y-b.y,z=a.z-b.z};}
    public sealed class TestBody {public TestBody transform=>this;public Vector3 position;}
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
    public static class SaloonArt {public static UnityEngine.Vector3 SafePosition;}
    public class DuelView {public string stage,firstName;public bool canHit,canDeal,waitingForOpening;}
    public class RoomTurnView {public bool spent,yourTurn;public string currentId,currentName;public string[] order;public float remaining;}
    public static class SaloonStations { public static string Name(int room)=>new[]{"Bar","Coin Flip","Blackjack","Have a Smoke"}[room]; }
    public static class SaloonBathroom { public const int Room=4; }
    public partial class SaloonGame
    {
        class Participant
        {
            public ulong id;public string name,notice="";public int hp=100,room,chosenRoom,planVersion;
            public bool toiletOffer,toiletDuty,bot,bag,planSelected,personalVictory,escapePlanned,nearSafe=true,gatherStation=true;public ulong? companion;public Role role;
            public float busyUntil;public string busyAction="";public int escapeRound,bagRound,inspectedProgress=-1,inspectedRevision=-1;public string safeReport="";public float drawerStarted,bodyYaw,yaw,pitch;public UnityEngine.Vector2 move;public UnityEngine.TestBody body=new UnityEngine.TestBody();public StoryProgress storyProgress;public List<string> evidence=new List<string>();
            public bool cigaretteBuff,drawn,fireAfterDraw,nearTray=true;public object pendingShot;public int beer,beerHealing,drinkHealing;public float beerChoiceUntil;
            public bool Alive=>hp>0;
        }
        class TestConnection { public ulong LocalId=1;public Command sent;public void Send(Command command){sent=command;} }
        bool TutorialWalkthrough=false;
        enum WalkthroughBeat { Plan }
        WalkthroughBeat WalkthroughStep=WalkthroughBeat.Plan;
        readonly Dictionary<ulong,Participant> players=new Dictionary<ulong,Participant>();
        readonly SaloonRounds rounds=new SaloonRounds();
        readonly TestConnection Connection=new TestConnection();
        readonly Tuning tuning=new Tuning();
        int beers=8,safeProgress,drawerRevision,lastSafeWorkRound;
        readonly List<string> events=new List<string>();
        void AddEvent(string value)=>events.Add(value);
        bool AtSafe(Participant p)=>p.nearSafe;
        void NoteEvidence(Participant p,string clue)=>p.evidence.Add(clue);
        bool NearBeerTray(Participant player)=>player.nearTray; // Physical distance is covered by the Unity verification.
        void CancelShotCharge(Participant player){}
        class Duel {public bool revealed;public float combatAt;public ulong first;}
        class RoomTurn {public ulong[] order;public int index;public float seconds,until;}
        readonly Dictionary<int,Duel> duels=new Dictionary<int,Duel>();
        readonly Dictionary<int,RoomTurn> roomTurns=new Dictionary<int,RoomTurn>();
        string pendingOutcome="",outcome="";
        void Finish(string value){pendingOutcome="";outcome=value;phase=Phase.Finished;}
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
            Check(State("Plan escape")==StoryState.Next&&SaloonStoryMap.Build(view).NextDetail.StartsWith("Your partner has the money."),"noncarrier gets team support guidance");
            view.outlawTeamEscapePending=true;
            Check(State("Plan escape")==StoryState.Complete&&SaloonStoryMap.Build(view).NextTitle=="Finish the escape","partner's escape declaration advances both maps");
            view.outlawTeamEscapePending=false;
            Check(State("Plan escape")==StoryState.Next,"cancelled partner escape reopens the declaration milestone");
            view.outlawTeamMoneyBag=false;
            Check(State("Take the money")==StoryState.Next,"losing the team's money restores recovery");
            view.moneyBag=view.escapePending=true;
            Check(State("Plan escape")==StoryState.Complete,"single Outlaw personal progress still works");
        }

        public static void Main()
        {
            TestDrawerTheft();TestIndependentRoles();TestRenegadeTargets();TestMatchSizes();TestClock();TestAssignments();TestReservations();TestPractice();TestPracticeEscapeResponse();TestClient();TestConcurrentBar();TestEscapeNoticeWindow();TestCigaretteHealing();TestNoMatchDeadline();TestSharedOutlawMap();
            SaloonRoomReservationVerification.Run();
            foreach(Role role in Enum.GetValues(typeof(Role)))
                Check(SaloonRules.CanInspectSafe(role)==(role!=Role.Unknown),"every dealt role can inspect the drawer");
            foreach(Role viewer in Enum.GetValues(typeof(Role)))foreach(Role subject in Enum.GetValues(typeof(Role)))
                Check(SaloonRules.KnowsRole(viewer,subject)==(viewer==Role.Outlaw&&subject==Role.Outlaw),"only Outlaws know hidden teammate roles");
            Console.WriteLine("PASS: "+checks+" checks (planning, concurrent Bar actions, escape timing, side-room openings and cigarette healing).");
        }
        static void TestDrawerTheft()
        {
            foreach(var role in new[]{Role.Sheriff,Role.Deputy,Role.Outlaw,Role.Renegade,Role.Swindler})
            {
                var g=Fixture();var p=g.players[1];p.role=role;foreach(var id in g.players.Keys)g.rounds.SetReady(id,true);g.rounds.Tick(0,id=>true);g.phase=Phase.Turns;
                g.BeginDrawerCheck(p,false);Check(p.busyAction=="inspect"&&p.busyUntil==2&&g.rounds.HasSpent(p.id),"every healthy role gets the same two-second inspection");
                UnityEngine.Time.time=2;g.CompleteInspection(p);Check(p.safeReport.Contains("money present")&&p.inspectedProgress==0,"inspection returns private drawer state");
            }
            for(int count=4;count<=8;count++)
            {
                var g=Fixture(count);var a=g.players[1];var b=g.players[2];a.role=b.role=Role.Outlaw;
                void Actions()
                {
                    foreach(var id in g.players.Keys)g.rounds.SetReady(id,true);
                    g.rounds.Tick(UnityEngine.Time.time,id=>true);g.phase=g.rounds.Phase;
                    a.busyUntil=b.busyUntil=0;a.busyAction=b.busyAction="";
                }
                void Next()
                {
                    float now=UnityEngine.Time.time;g.rounds.BeginAftermath(now);g.rounds.Tick(now+3,id=>true);g.rounds.Tick(now+6,id=>true);
                    UnityEngine.Time.time=now+6;g.phase=g.rounds.Phase;Actions();
                }
                Actions();
                for(int step=1;step<=SaloonRules.DrawerWorkSteps;step++)
                {
                    var worker=step%2==1?a:b;var other=worker==a?b:a;
                    g.BeginDrawerCheck(worker,true);g.BeginDrawerCheck(other,true);
                    Check(worker.busyUntil==other.busyUntil,"competing theft steps have the shared inspection timing");
                    UnityEngine.Time.time+=2;g.CompleteRobbery(worker);g.CompleteRobbery(other);
                    Check(g.safeProgress==step&&g.drawerRevision==step,"only one shared theft step completes per round");
                    Check(g.players.Values.Count(p=>p.bag)==(step==3?1:0),"money stays in the drawer until step three");
                    g.CompleteRobbery(worker);Check(g.safeProgress==step,"duplicate completion cannot advance another step");
                    if(step<3)
                    {
                        g.DeclareEscape(worker);Check(!worker.escapePlanned,"partial drawer progress grants no escape");
                        g.CompleteInspection(g.players[3]);Check(g.players[3].safeReport.Contains("money present")&&g.players[3].safeReport.Contains("tampered"),"private inspection detects partial tampering without claiming money missing");
                        Next();
                    }
                }
                Check(a.bag&&!b.bag&&a.bagRound==3,"the third round worker receives the single money bag");
                g.ReturnDrawerMoney(a);a.bag=false;Check(g.safeProgress==0&&g.drawerRevision==4,"carrier death resets all theft steps and changes the inspection revision");
                g.CompleteRobbery(b);Check(!b.bag&&g.safeProgress==0,"returned money cannot be recollected in the same round");
                Next();g.CompleteRobbery(b);Check(!b.bag&&g.safeProgress==1,"returned money requires starting the three steps again");
            }
            {
                var g=Fixture();var p=g.players[1];p.role=Role.Outlaw;
                foreach(var id in g.players.Keys)g.rounds.SetReady(id,true);g.rounds.Tick(0,id=>true);g.phase=Phase.Turns;
                p.nearSafe=false;g.BeginDrawerCheck(p,true);Check(!g.rounds.HasSpent(1),"distant attempts spend no action");
                p.nearSafe=true;g.BeginDrawerCheck(p,false);UnityEngine.Time.time=2;g.CompleteInspection(p);
                g.safeProgress=0;g.drawerRevision=2;g.CompleteInspection(p);Check(p.safeReport.Contains("changed since"),"taking and returning cash between inspections is still detectable");
                p.role=Role.Deputy;g.CompleteRobbery(p);Check(!p.bag,"host rechecks role at theft completion");
                p.role=Role.Outlaw;p.nearSafe=false;g.CompleteRobbery(p);Check(!p.bag,"leaving reach prevents collection");
            }
        }

        static void TestIndependentRoles()
        {
            Check(SaloonRules.IndependentFor(0)==Role.Renegade&&SaloonRules.IndependentFor(.4999f)==Role.Renegade&&
                SaloonRules.IndependentFor(.5f)==Role.Swindler&&SaloonRules.IndependentFor(1)==Role.Swindler,"50/50 boundary chooses one independent");
            for(int size=4;size<=8;size++)
            foreach(var independent in new[]{Role.Renegade,Role.Swindler})
            {
                var deck=SaloonRules.DeckFor(size,independent);
                Check(deck.Length==size&&deck.Count(SaloonRules.IsIndependent)==1&&deck.Contains(independent),"exactly one selected independent per deck");
                foreach(var role in deck.Distinct())
                {
                    var slots=SaloonRules.MatchupFor(size,role);
                    Check(slots.Length==size&&slots[0]==role,"briefing starts with own role for every role/count");
                    Check(SaloonRules.IsIndependent(role)?slots.All(r=>r==role||!SaloonRules.IsIndependent(r)):
                        slots.Count(r=>r==Role.Unknown)==1&&slots.All(r=>!SaloonRules.IsIndependent(r)),"briefing never reveals another player's independent roll");
                    Check(!string.IsNullOrEmpty(SaloonRules.Guide(role,size).Goal),"all dealt roles have guidance");
                }
            }
            foreach(Role attacker in Enum.GetValues(typeof(Role)))
            {
                bool law=attacker==Role.Sheriff||attacker==Role.Deputy;
                Check(SaloonRules.SwindlerWins(Role.Swindler,attacker,true)==law,"only a lethal Law shot wins");
                Check(!SaloonRules.SwindlerWins(Role.Swindler,attacker,false)&&!SaloonRules.SwindlerWins(Role.Swindler,attacker,true,true)&&
                    !SaloonRules.SwindlerWins(Role.Swindler,attacker,true,false,true),"nonlethal self-inflicted and reflected damage do not win");
                var game=Fixture();var killer=game.players[1];var target=game.players[2];
                killer.role=attacker;target.role=Role.Swindler;target.hp=20;
                game.RecordSwindlerVictory(killer,target);Check(!target.personalVictory,"production award method rejects a living target");
                target.hp=0;game.RecordSwindlerVictory(killer,target);Check(target.personalVictory==law,"production award records the eligible killer");
                Check(game.players.Values.Where(p=>p!=target).All(p=>!p.personalVictory),"award is personal");
            }
            Check(SaloonRules.Winner(new[]{Role.Sheriff,Role.Swindler})=="LAW WINS","Law need not kill the Swindler to win");
            Check(SaloonRules.Winner(new[]{Role.Outlaw,Role.Swindler})=="OUTLAWS WIN — SHERIFF DEAD","Sheriff death wins for Outlaws even with Swindler alive");
            Check(SaloonRules.Winner(new[]{Role.Swindler})=="OUTLAWS WIN — SHERIFF DEAD","surviving Swindler cannot override Sheriff death");
            Check(SaloonRules.Winner(new[]{Role.Deputy,Role.Renegade})=="OUTLAWS WIN — SHERIFF DEAD","Renegade must leave Sheriff until last even after all Outlaws die");
            Check(SaloonRules.Winner(new[]{Role.Renegade})=="RENEGADE WINS — LAST STANDING","sole surviving Renegade takes priority over Sheriff death");
            Check(SaloonRules.WonMatch(Role.Outlaw,"OUTLAWS WIN — SHERIFF DEAD")&&!SaloonRules.WonMatch(Role.Deputy,"OUTLAWS WIN — SHERIFF DEAD"),"Sheriff death awards the Outlaw team, never surviving Deputies");
            Check(SaloonRules.WonMatch(Role.Swindler,"OUTLAWS WIN — MONEY ESCAPED",true)&&
                !SaloonRules.WonMatch(Role.Swindler,"LAW WINS",false),"personal victory survives faction results without awarding an unearned win");
            var view=new Snapshot{ownRole="Swindler",personalVictory=true,health=0,phase=Phase.Turns};
            Check(SaloonStoryMap.Build(view).NextTitle=="Personal victory earned","dead Swindler guidance preserves personal victory");
        }
        static void TestRenegadeTargets()
        {
            var g=Fixture();var lone=g.players[1];lone.role=Role.Renegade;
            var sheriff=g.players[2];sheriff.role=Role.Sheriff;
            var deputy=g.players[3];deputy.role=Role.Deputy;
            var outlaw=g.players[4];outlaw.role=Role.Outlaw;
            var swindler=g.players[5];swindler.role=Role.Swindler;
            Check(g.Enemies(lone,outlaw)&&!g.Enemies(lone,sheriff)&&!g.Enemies(lone,deputy),"Renegade bot targets Outlaws first and protects Sheriff");
            outlaw.hp=0;
            Check(g.Enemies(lone,deputy)&&g.Enemies(lone,swindler)&&!g.Enemies(lone,sheriff),"Renegade bot leaves Sheriff alive while other survivors remain");
            deputy.hp=swindler.hp=0;
            Check(g.Enemies(lone,sheriff),"Renegade bot targets Sheriff only for the final kill");
        }
        static void TestMatchSizes()
        {
            var counts=new[]{new[]{1,1,1,1},new[]{1,1,2,1},new[]{1,2,2,1},new[]{1,2,3,1},new[]{1,3,3,1}};
            for(int size=4;size<=8;size++)
            {
                var deck=SaloonRules.DeckFor(size);
                Check(SaloonRules.SupportedMatchSize(size)&&deck.Length==size,"supported deck size "+size);
                foreach(var role in new[]{Role.Sheriff,Role.Deputy,Role.Outlaw,Role.Renegade})
                    Check(deck.Count(r=>r==role)==counts[size-4][(int)role-1],"agreed role count for "+size+" / "+role);
                deck[0]=Role.Unknown;
                Check(!SaloonRules.DeckFor(size).Contains(Role.Unknown),"dealing returns an independent deck");
                var game=Fixture(size);
                foreach(var player in game.players.Values)Check(game.TryReady(player,true),"all players can lock the Bar");
                game.rounds.Tick(0,id=>true);game.phase=game.rounds.Phase;
                var assignments=game.rounds.AssignRooms(game.players.Keys,id=>0,game.RoomCapacities(),true);
                Check(assignments.Count==size&&assignments.All(p=>p.Value==0),"everyone fits at the Bar");
                foreach(var player in game.players.Values)Check(game.SpendAction(player),"each player has their own Bar action");
                game.rounds.Tick(0,id=>true);
                Check(game.rounds.Phase==Phase.Aftermath,"all actions complete at "+size+" players");
                var mixed=game.rounds.AssignRooms(game.players.Keys,id=>id<=2?1:id<=4?2:3,game.RoomCapacities(),true);
                Check(mixed.Count(p=>p.Value==1)==2&&mixed.Count(p=>p.Value==2)==2&&mixed.Count(p=>p.Value==3)<=2,"side rooms stay limited to pairs");
            }
            foreach(int size in new[]{-1,0,1,2,3,9,10})
            {
                Check(!SaloonRules.SupportedMatchSize(size),"unsupported match size "+size);
                bool threw=false;try{SaloonRules.DeckFor(size);}catch(ArgumentOutOfRangeException){threw=true;}
                Check(threw,"unsupported decks are rejected");
            }
        }

        static void TestCigaretteHealing()
        {
            foreach(bool boosted in new[]{false,true})
            {
                var g=Fixture();var player=g.players[1];player.hp=40;player.cigaretteBuff=boosted;
                foreach(var p in g.players.Values)g.rounds.SetReady(p.id,true);g.rounds.Tick(0,id=>true);g.phase=Phase.Turns;
                int supply=g.beers;g.DrinkBeer(player);
                Check(player.drinkHealing==(boosted?40:20)&&!player.cigaretteBuff&&player.busyAction=="drink", "ready beer commits fixed healing and the drinker's buff");
                Check(g.beers==supply-1&&g.rounds.HasSpent(player.id),"ready beer spends one supply and one action");
                g.DrinkBeer(player);Check(g.beers==supply-1,"duplicate drink cannot spend another beer");
            }
            for(int hp=0;hp<=100;hp++)
            {
                int healed=SaloonHealth.AfterBeer(hp,40);
                Check(healed==(hp==0?0:Math.Min(100,hp+40)),"boosted beer caps health and cannot revive a dead player");
            }
            var game=Fixture();foreach(var p in game.players.Values)game.rounds.SetReady(p.id,true);game.rounds.Tick(0,id=>true);game.phase=Phase.Turns;
            var drinker=game.players[1];drinker.hp=100;drinker.cigaretteBuff=true;int stock=game.beers;
            game.DrinkBeer(drinker);Check(drinker.cigaretteBuff&&game.beers==stock&&!game.rounds.HasSpent(drinker.id),"full-health rejection preserves beer, buff and action");
            drinker.hp=40;drinker.nearTray=false;game.DrinkBeer(drinker);
            Check(drinker.cigaretteBuff&&game.beers==stock&&!game.rounds.HasSpent(drinker.id),"being away from the tray preserves beer, buff and action");
            drinker.nearTray=true;game.beers=0;game.DrinkBeer(drinker);
            Check(drinker.cigaretteBuff&&!game.rounds.HasSpent(drinker.id),"an empty tray preserves the buff and action");
        }
        static void TestConcurrentBar()
        {
            SaloonGame Escape(int count=5)
            {
                var g=Fixture(count);var carrier=g.players[1];carrier.role=Role.Outlaw;carrier.bag=true;
                g.DeclareEscape(carrier);Check(carrier.escapePlanned&&!g.rounds.HasSpent(1),"escape planning preserves the action");
                foreach(var p in g.players.Values)g.rounds.SetReady(p.id,true);
                UnityEngine.Time.time=10;g.rounds.Tick(10,id=>true);g.phase=g.rounds.Phase;g.BeginPlannedEscapes();return g;
            }
            for(int count=4;count<=8;count++)
            {
                var g=Escape(count);var carrier=g.players[1];
                Check(carrier.escapeRound==1&&g.players.Values.All(p=>g.ActionBlockReason(p)==""),"carrier and all defenders have fresh concurrent actions");
                g.ResolveEscapes();Check(g.phase==Phase.Turns,"unused defender actions keep the response window open");
                foreach(var p in g.players.Values.Where(p=>p!=carrier))Check(g.SpendAction(p),"defenders can act in any order");
                g.ResolveEscapes();g.rounds.Tick(10,id=>true);
                Check(g.phase==Phase.Turns&&g.rounds.Phase==Phase.Turns&&g.ActionBlockReason(carrier)=="","all misses still leave the carrier its action");
                Check(g.SpendAction(carrier)&&!g.SpendAction(carrier),"carrier gets exactly one action during escape");
                g.ResolveEscapes();Check(g.phase==Phase.Finished&&g.outcome=="OUTLAWS WIN — MONEY ESCAPED"&&g.pendingOutcome=="","completed Bar actions go straight to the escape victory screen");
            }
            {
                var g=Escape();var carrier=g.players[1];Check(g.SpendAction(carrier),"carrier can act before the defenders");
                foreach(var p in g.players.Values.Where(p=>p.id>2))g.SpendAction(p);
                g.ResolveEscapes();Check(g.phase==Phase.Turns,"the last unused defender action cannot be skipped");
                g.SpendAction(g.players[2]);g.ResolveEscapes();Check(g.phase==Phase.Finished,"the last defender action resolves the escape immediately");
            }
            {
                var g=Escape();var carrier=g.players[1];carrier.hp=60;g.DrinkBeer(carrier);
                Check(carrier.busyAction=="drink"&&g.rounds.HasSpent(1)&&carrier.escapeRound==1,"escaping carrier can heal with its normal action");
                foreach(var p in g.players.Values.Where(p=>p!=carrier))g.SpendAction(p);
                g.ResolveEscapes();Check(g.phase==Phase.Turns,"victory waits for the carrier's timed heal");
                g.CancelEscapeOnHit(carrier);g.rounds.Tick(10,id=>true);
                Check(g.rounds.Phase==Phase.Turns&&carrier.busyUntil>10,"interrupting escape does not shorten an existing drink");
                g.ResolveEscapes();Check(g.outcome==""&&carrier.bag,"interrupted escape keeps the money and never wins");
                UnityEngine.Time.time=carrier.busyUntil;g.rounds.Tick(UnityEngine.Time.time,id=>true);g.phase=g.rounds.Phase;g.ResolveEscapes();
                Check(g.phase==Phase.Aftermath&&g.outcome=="","cancelled attempt ends normally after its timed action");
            }
            foreach(int room in new[]{1,2,3,4})
            {
                var g=Escape();g.players[5].room=room;
                foreach(var p in g.players.Values.Where(p=>p.room==0))g.SpendAction(p);
                g.ResolveEscapes();Check(g.phase==Phase.Finished&&!g.rounds.HasSpent(5),"unreachable side-room players cannot delay a guaranteed escape");
            }
            {
                var g=Escape();foreach(var p in g.players.Values)g.SpendAction(p);
                var defender=g.players[2];defender.busyUntil=12;defender.busyAction="reload";
                g.ResolveEscapes();Check(g.phase==Phase.Turns,"an in-progress Bar action finishes before results");
                UnityEngine.Time.time=12;g.ResolveEscapes();Check(g.phase==Phase.Finished,"timed action completion releases escape victory");
            }
            {
                var g=Escape();g.rounds.Tick(g.rounds.Deadline,id=>true);g.phase=g.rounds.Phase;g.ResolveRoundEnding();
                Check(g.phase==Phase.Finished&&g.outcome=="OUTLAWS WIN — MONEY ESCAPED","fallback deadline resolves an unchallenged escape without an extra round");
            }
            foreach(string invalid in new[]{"hit","dead","no money","wrong room","wrong role","stale","existing outcome"})
            {
                var g=Escape();var carrier=g.players[1];foreach(var p in g.players.Values)g.SpendAction(p);
                if(invalid=="hit")g.CancelEscapeOnHit(carrier);
                if(invalid=="dead")carrier.hp=0;
                if(invalid=="no money")carrier.bag=false;
                if(invalid=="wrong room")carrier.room=1;
                if(invalid=="wrong role")carrier.role=Role.Deputy;
                if(invalid=="stale")carrier.escapeRound++;
                if(invalid=="existing outcome")g.pendingOutcome="LAW WINS";
                g.ResolveEscapes();Check(g.outcome=="","invalid or superseded escape cannot win: "+invalid);
            }
            {
                var g=Escape();var carrier=g.players[1];g.CancelEscapeOnHit(carrier);
                Check(g.ActionBlockReason(carrier)=="","a headshot cancels escape without spending the carrier action");
            }
            var game=Fixture();var robber=game.players[1];robber.role=Role.Outlaw;robber.bag=true;robber.bagRound=1;game.DeclareEscape(robber);
            Check(!robber.escapePlanned,"money cannot escape in its collection round");
            robber.bagRound=0;game.phase=Phase.Turns;game.DeclareEscape(robber);Check(!robber.escapePlanned,"late action-phase declarations cannot bypass fresh defenders");
            game.phase=Phase.Discussion;game.DeclareEscape(robber);game.SetRoomPlan(robber,3,null);Check(!robber.escapePlanned,"leaving Bar cancels the planned attempt");
        }
        static void TestEscapeNoticeWindow()
        {
            var g=Fixture();var carrier=g.players[2];carrier.role=Role.Outlaw;carrier.bag=true;
            UnityEngine.Time.time=29;g.DeclareEscape(carrier);
            Check(g.rounds.Deadline==39&&g.rounds.DiscussionHeldUntil==39,"last-second declaration leaves ten seconds to change rooms");
            foreach(var p in g.players.Values)g.rounds.SetReady(p.id,true);
            g.rounds.Tick(29,id=>true);g.rounds.Tick(38.99f,id=>true);
            Check(g.rounds.Phase==Phase.Discussion,"all-ready cannot skip the escape warning window");
            UnityEngine.Time.time=35;g.SetRoomPlan(carrier,3,null);g.DeclareEscape(carrier);
            Check(g.rounds.Deadline==39&&g.rounds.DiscussionHeldUntil==39,"cancel and redeclare cannot repeatedly extend discussion");
            g.rounds.SetReady(carrier.id,true);g.rounds.Tick(39,id=>true);
            Check(g.rounds.Phase==Phase.Turns&&g.rounds.Deadline==69,"warning ends with a fresh full action phase");
            g.rounds.BeginAftermath(39);g.rounds.Tick(42,id=>true);g.rounds.Tick(45,id=>true);
            Check(g.rounds.Phase==Phase.Discussion&&g.rounds.DiscussionHeldUntil==0,"new discussion clears the warning hold");
            g.rounds.HoldDiscussionUntil(55);g.rounds.Start(g.players.Keys,0,30,30,0);
            Check(g.rounds.DiscussionHeldUntil==0,"new match clears the warning hold");
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
        static void TestNoMatchDeadline()
        {
            foreach(int targetRound in new[]{20,21,50})
            {
                var g=Fixture();
                while(g.rounds.Round<targetRound)
                {
                    float now=UnityEngine.Time.time;
                    g.rounds.BeginAftermath(now);g.phase=Phase.Aftermath;g.ResolveRoundEnding();
                    Check(g.pendingOutcome=="","reaching a late round never awards an automatic victory");
                    g.rounds.Tick(now+3,id=>true);g.rounds.Tick(now+6,id=>true);
                    UnityEngine.Time.time=now+6;g.phase=g.rounds.Phase;
                }
                var carrier=g.players[1];carrier.role=Role.Outlaw;carrier.bag=true;carrier.bagRound=targetRound-1;
                g.DeclareEscape(carrier);Check(carrier.escapePlanned,"escape can be planned in round "+targetRound);
                foreach(var p in g.players.Values)g.rounds.SetReady(p.id,true);
                g.rounds.Tick(g.rounds.DiscussionHeldUntil,id=>true);g.phase=g.rounds.Phase;g.BeginPlannedEscapes();
                Check(carrier.escapeRound==targetRound,"late escape starts with normal authorization");
                g.rounds.Tick(g.rounds.Deadline,id=>true);g.phase=g.rounds.Phase;g.ResolveRoundEnding();
                Check(g.phase==Phase.Finished&&g.outcome=="OUTLAWS WIN — MONEY ESCAPED","late escape still wins after its response window");
            }
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
            g=Fixture(sandbox:true);
            for(int round=1;round<=5;round++)
            {
                float now=UnityEngine.Time.time;
                g.PlanPracticeBots();
                var human=g.players[1];
                Check(g.CanSelectPlan(human,3,null),"practice bots leave the deck available for a solo visit in round "+round);
                g.SetRoomPlan(human,3,null);
                Check(g.TryReady(human,true),"a solo smoke plan locks after practice bots have planned");
                g.PlanPracticeBots();
                g.rounds.Tick(now,id=>true);
                var rooms=g.rounds.AssignRooms(g.players.Keys,id=>g.players[id].chosenRoom,preventSolo:true);
                Check(g.rounds.Phase==Phase.Turns&&rooms[1]==3&&rooms.Values.Count(room=>room==3)==1,"the solo smoker is the only deck occupant");
                g.rounds.BeginAftermath(now);
                g.rounds.Tick(now+3,id=>true);g.rounds.Tick(now+6,id=>true);
                g.phase=g.rounds.Phase;UnityEngine.Time.time=now+6;
                foreach(var p in g.players.Values){p.chosenRoom=0;p.companion=null;}
            }
            foreach(int room in new[]{0,1,2,3})
            {
                g=Fixture(sandbox:true);g.SetRoomPlan(g.players[1],room,null);g.TryReady(g.players[1],true);g.PlanPracticeBots();
                Check(g.players[1].chosenRoom==room&&g.players[1].companion==null&&g.rounds.IsReady(1),"practice bots preserve a human's solo locked choice without creating a partnership");
                Check(room==0||g.players.Values.Count(p=>p.chosenRoom==room&&g.rounds.IsReady(p.id))==1,"a bot is never recruited to fill a locked solo side-room choice, including smoking");
            }
        }
        static void TestPracticeEscapeResponse()
        {
            for(int count=4;count<=8;count++)foreach(var defenderRole in new[]{Role.Sheriff,Role.Deputy,Role.Renegade})
            {
                var g=Fixture(count,sandbox:true);
                foreach(var p in g.players.Values)p.role=Role.Outlaw;
                var carrier=g.players[2];carrier.bag=true;carrier.bagRound=0;
                var guard=g.players[3];guard.role=defenderRole;
                // Even a previously locked bot pair can react to the public warning.
                g.SetRoomPlan(guard,1,4);g.SetRoomPlan(g.players[4],1,3);g.TryReady(guard,true);
                g.PlanPracticeBots();
                Check(carrier.escapePlanned&&g.rounds.IsReady(carrier.id)&&carrier.chosenRoom==0,"carrier announces escape before other bot plans in "+count+" seats");
                Check(guard.chosenRoom==0&&guard.companion==null&&g.rounds.IsReady(guard.id),defenderRole+" bot responds to public escape instead of leaving Bar");
                var stable=g.players.Values.Select(p=>(p.id,p.chosenRoom,p.companion)).ToArray();g.PlanPracticeBots();
                Check(stable.All(p=>g.players[p.id].chosenRoom==p.chosenRoom&&g.players[p.id].companion==p.companion),"warning response remains stable across bot ticks");
            }
            var quiet=Fixture(sandbox:true);quiet.players[2].bag=true;quiet.players[2].bagRound=1;quiet.players[2].role=Role.Outlaw;
            quiet.PlanPracticeBots();Check(!quiet.players[2].escapePlanned,"bot cannot announce an escape in the theft round");
            var invited=Fixture(sandbox:true);invited.players[2].bag=true;invited.players[2].bagRound=0;invited.players[2].role=Role.Outlaw;invited.players[3].role=Role.Deputy;
            invited.SetRoomPlan(invited.players[1],2,3);invited.PlanPracticeBots();
            Check(invited.players[3].chosenRoom==2&&invited.players[3].companion==1&&invited.rounds.IsReady(3),"explicit human invitation can distract a defending bot");
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
