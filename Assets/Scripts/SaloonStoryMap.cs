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
            bool won=terminal&&SaloonRules.WonMatch(role,view.outcome);
            bool active=!terminal&&view.health>0;
            if(role==Role.Outlaw)
            {
                bool bag=view.moneyBag||view.outlawTeamMoneyBag,escape=(view.escapePending||view.outlawTeamEscapePending)&&bag;
                bool worked=Has(StoryProgress.SafeWork)||view.safeProgress>0||bag;
                map.Caption="Steal the money from the safe. Escape with it to win.";
                map.Add("Outlaw",4,145,300,true,false);
                map.Add("Reach the Bar",7,310,300,bar,active&&!bar,0);
                map.Add("Work the safe",2,480,198,worked,active&&bar&&!worked,1);
                map.Add("Take the money",4,650,300,bag,active&&worked&&!bag,2);
                map.Add("Declare escape",5,820,198,escape,active&&bag&&!escape,3);
                map.Add("Finish the escape",5,820,415,won,active&&escape,4);
                map.Add("Defend yourself",3,480,415,hit,false,true,1);
                int next=!bar?1:!worked?2:!bag?3:!escape?4:5;
                map.NextTitle=map.Nodes[next].Title;
                if(escape)
                    map.NextDetail=view.moneyBag?"Choose Bar next round. Keep the money. Stay alive until that round ends. A side room cancels the escape.":
                        "Your partner started the escape. Protect your partner at Bar until the next round ends.";
                else if(bag&&!view.moneyBag)
                    map.NextDetail="Your partner has the money. Protect your partner while they start the escape.";
                else if(view.round>=SaloonPosse.ArrivalRound)
                    map.NextDetail="The posse arrives after this round. The deadline to start an escape has passed.";
                else if(bag)
                    map.NextDetail=$"At Bar, press X on a later turn. Start the escape by round {SaloonPosse.LastEscapeRound}.";
                else if(bar&&worked)
                    map.NextDetail="Finish the third safe job to take the money. Press H by the safe.";
                else if(bar)
                    map.NextDetail="Press F to stand. Walk to the safe. Press H. Stay beside the safe for 3 seconds.";
                else
                    map.NextDetail="Choose Bar to reach the safe.";
            }
            else if(role==Role.Renegade)
            {
                bool outlaws=Has(StoryProgress.OutlawsGone),law=Has(StoryProgress.LawGone);
                map.Caption="Be the last player alive. You have unlimited bullets.";
                map.Add("Renegade",6,145,300,true,false);
                map.Add("Stop the Outlaws",3,350,198,outlaws,active&&!outlaws,0);
                map.Add("Fight the Law",0,585,198,law,active&&outlaws&&!law,1);
                map.Add("Win alone",6,820,300,won,active&&outlaws&&law,2);
                map.Add("Go to the Bar",7,350,415,bar,false,true,0);
                map.Add("Shoot a rival",3,585,415,hit,false,true,4);
                int next=!outlaws?1:!law?2:3;
                map.NextTitle=map.Nodes[next].Title;
                map.NextDetail=!outlaws?"Find an Outlaw. Fight the Outlaws first.":!law?"Defeat the Sheriff and Deputy.":$"Win by round {SaloonPosse.ArrivalRound}.";
            }
            else
            {
                bool sheriff=role==Role.Sheriff,inspected=!string.IsNullOrEmpty(view.safeReport);
                bool arrival=won&&(view.outcome??"").Contains("POSSE");
                map.Caption=sheriff?"Protect the money with your Deputy.":"Help the Sheriff protect the money.";
                map.Add(role.ToString(),sheriff?0:1,145,300,true,false);
                map.Add("Guard the Bar",7,310,300,bar,active&&!bar,0);
                bool winPathAvailable=active&&bar&&(!sheriff||inspected);
                map.Add(sheriff?"Inspect the safe":"Defend your ally",sheriff?2:1,465,415,sheriff?inspected:hit,active&&bar&&sheriff&&!inspected,true,1);
                map.Add("Stop the criminals",3,645,198,won&&!arrival,winPathAvailable,1);
                map.Add("Hold for the posse",6,645,415,arrival,winPathAvailable,1);
                map.Add("Win as the Law",0,820,300,won,false,3,4);
                map.NextTitle=!bar?"Guard the Bar":sheriff&&!inspected?"Inspect the safe":"Win as the Law";
                map.NextDetail=!bar?"Choose Bar to guard the safe.":sheriff&&!inspected?"Press F to stand. Walk to the safe. Press C to check for clues.":
                    $"Defeat the Outlaws and the Renegade.\nOr stop the escape. Keep one Law player alive through round {SaloonPosse.ArrivalRound}.";
            }
            if(active)
            {
                if(view.phase==Phase.Discussion)
                    map.NextControl=(view.players??Array.Empty<PlayerView>()).Any(p=>p.id==view.ownId&&p.ready)
                        ?"Your plan is locked. Press Space to unlock it.":"Right-click to plan. Press Space to lock your choice.";
                else if(view.phase==Phase.Choosing)
                    map.NextControl="Select Lock choice when you are ready.";
                else if(view.phase==Phase.Turns&&!view.actionAvailable&&!string.IsNullOrEmpty(view.actionBlockReason))
                    map.NextControl=view.actionBlockReason;
            }
            if(terminal){map.NextTitle=won?"You won":view.phase==Phase.Aftermath&&string.IsNullOrEmpty(view.outcome)?"Round over":"Match over";map.NextDetail=view.outcome??"";}
            else if(view.health<=0){map.NextTitle="You are out";map.NextDetail="Watch the players who are still alive.";}
            return map;
        }
    }
}
