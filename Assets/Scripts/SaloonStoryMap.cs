using System;
using System.Collections.Generic;
using System.Linq;

namespace Saloon
{
    [Flags]
    public enum StoryProgress { None=0, Bar=1, SafeWork=2, EnemyHit=4, OutlawsGone=8, LawGone=16 }
    public enum StoryState { Later, Next, Complete }

    public sealed class StoryMilestone
    {
        public string Title;
        public int Art;
        public float X,Y;
        public StoryState State;
        public bool Optional;
        public int[] From;
    }

    // Uses only recipient-authorized snapshot data. Rendering never needs the host's roster.
    public sealed class SaloonStoryMap
    {
        public readonly List<StoryMilestone> Nodes=new List<StoryMilestone>();
        public string Caption,NextTitle,NextDetail,NextControl;
        public int Completed=>Nodes.Count(n=>n.State==StoryState.Complete);

        void Add(string title,int art,float x,float y,bool done,bool next,params int[] from)
            =>Add(title,art,x,y,done,next,false,from);

        void Add(string title,int art,float x,float y,bool done,bool next,bool optional,params int[] from)
            =>Nodes.Add(new StoryMilestone{Title=title,Art=art,X=x,Y=y,Optional=optional,State=done?StoryState.Complete:next?StoryState.Next:StoryState.Later,From=from});

        public static SaloonStoryMap Build(Snapshot view)
        {
            var map=new SaloonStoryMap();
            if(view==null||!Enum.TryParse(view.ownRole,out Role role)||role==Role.Unknown)
            {map.NextTitle="Waiting for your role";map.NextDetail="You get a role when the match starts.";return map;}
            bool Has(StoryProgress flag)=>(view.storyProgress&flag)!=0;
            bool bar=Has(StoryProgress.Bar),hit=Has(StoryProgress.EnemyHit);
            bool terminal=view.phase==Phase.Finished||view.phase==Phase.Aftermath;
            bool won=role==Role.Swindler?view.personalVictory:terminal&&SaloonRules.WonMatch(role,view.outcome);
            bool active=!terminal&&view.health>0;
            if(role==Role.Outlaw)
            {
                bool bag=view.moneyBag||view.outlawTeamMoneyBag,escape=(view.escapePending||view.outlawTeamEscapePending)&&bag;
                bool planned=(view.escapePlanned||escape)&&bag;
                map.Caption="Kill the Sheriff, or complete three drawer steps and escape.";
                map.Add("Outlaw",4,145,300,true,false);
                map.Add("Reach the Bar",7,310,300,bar,active&&!bar,0);
                map.Add("Take the money",4,480,198,bag,active&&bar&&!bag,1);
                map.Add("Plan escape",5,650,300,planned,active&&bag&&!planned,2);
                map.Add("Finish the escape",5,820,198,won&&view.outcome=="OUTLAWS WIN — MONEY ESCAPED",active&&planned,3);
                map.Add("Kill the Sheriff",3,480,415,won&&view.outcome=="OUTLAWS WIN — SHERIFF DEAD",active,true,0);
                int next=!bar?1:!bag?2:!planned?3:4;
                map.NextTitle=map.Nodes[next].Title;
                map.NextDetail=escape?"The escape is underway. Stay at Bar until actions end; any headshot cancels the attempt.":
                    bag&&!view.moneyBag?"Your partner has the money. Protect your partner when they attempt escape.":
                    planned?"Lock your Bar plan. The attempt will start with the action phase.":
                    bag?"During discussion, choose Attempt escape or press X, then lock your Bar plan.":
                    bar?"H beside the drawer · "+Math.Max(0,view.safeProgress)+" / "+SaloonRules.DrawerWorkSteps+" steps. One shared step per round.":"Choose Bar to reach the cash drawer, or hunt the Sheriff.";
            }
            else if(role==Role.Renegade)
            {
                map.Caption="Be last alive. Leave the Sheriff until last. You have unlimited bullets.";
                map.Add("Renegade",6,145,300,true,false);
                map.Add("Read the room",7,350,198,bar,active&&!bar,0);
                map.Add("Land a headshot",3,585,198,hit,active&&bar&&!hit,1);
                map.Add("Win alone",6,820,300,won,active&&hit,2);
                int next=!bar?1:!hit?2:3;
                map.NextTitle=map.Nodes[next].Title;
                map.NextDetail=!bar?"Choose Bar. Listen to claims and watch the cash drawer.":!hit?"Identify a rival through their actions. Nobody reveals hidden roles to you.":"Eliminate everyone else before the Sheriff. Be the last survivor.";
            }
            else if(role==Role.Swindler)
            {
                map.Caption="Win by convincing the Law to kill you.";
                map.Add("Swindler",6,145,300,true,false);
                map.Add("Draw suspicion",7,350,198,bar,active&&!bar,0);
                map.Add("Be killed by Law",3,585,300,won,active,1);
                map.Add("Personal victory",6,820,300,won,false,2);
                map.NextTitle=won?"Personal victory earned":"Make the Law kill you";
                map.NextDetail=won?"Your win is saved. Watch the others finish the match.":"Bluff and act suspiciously. The Sheriff or a Deputy must land the lethal shot. Self-inflicted damage and Outlaw kills do not count.";
            }
            else
            {
                bool sheriff=role==Role.Sheriff,inspected=!string.IsNullOrEmpty(view.safeReport);
                bool inspector=SaloonRules.CanInspectSafe(role);
                map.Caption=sheriff?"Stay alive and protect the money with your Law allies.":"Keep the Sheriff alive and protect the money.";
                map.Add(role.ToString(),sheriff?0:1,145,300,true,false);
                map.Add("Guard the Bar",7,310,300,bar,active&&!bar,0);
                bool winPathAvailable=active&&bar&&(!inspector||inspected);
                map.Add(inspector?"Check the drawer":"Defend your ally",inspector?2:1,465,415,inspector?inspected:hit,active&&bar&&inspector&&!inspected,true,1);
                map.Add("Stop the criminals",3,645,198,won,winPathAvailable,1);
                map.Add("Win as the Law",0,820,300,won,false,3);
                map.NextTitle=!bar?"Guard the Bar":inspector&&!inspected?"Check the drawer":"Win as the Law";
                map.NextDetail=!bar?"Choose Bar to watch the cash drawer.":inspector&&!inspected?"Press Space to stand. Walk to the cash drawer. Press C to check it privately.":
                    "Defeat the Outlaws and any Renegade. Keep the Sheriff alive and stop the money from escaping.";
            }
            if(active)
            {
                if(view.phase==Phase.Discussion)
                    map.NextControl=(view.players??Array.Empty<PlayerView>()).Any(p=>p.id==view.ownId&&p.ready)
                        ?"Your plan is locked. Press F to unlock it.":"Right-click to plan. Press F to lock your choice.";
                else if(view.phase==Phase.Choosing)
                    map.NextControl="Select Lock choice when you are ready.";
                else if(view.phase==Phase.Turns&&!view.actionAvailable&&!string.IsNullOrEmpty(view.actionBlockReason))
                    map.NextControl=view.actionBlockReason;
            }
            if(terminal){map.NextTitle=won?"You won":view.phase==Phase.Aftermath&&string.IsNullOrEmpty(view.outcome)?"Round over":"Match over";map.NextDetail=view.outcome??"";}
            else if(view.health<=0){map.NextTitle=won?"Personal victory earned":"You are out";map.NextDetail=won?"The Law killed you. Your personal win is saved; watch the rest of the match.":"Watch the players who are still alive.";}
            return map;
        }
    }
}
