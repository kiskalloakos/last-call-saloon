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
        public string Title,Detail;
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
        public string Caption,NextTitle,NextDetail;
        public int Completed=>Nodes.Count(n=>n.State==StoryState.Complete);

        void Add(string title,string detail,int art,float x,float y,bool done,bool next,params int[] from)
            =>Nodes.Add(new StoryMilestone{Title=title,Detail=detail,Art=art,X=x,Y=y,Optional=detail.StartsWith("Optional"),State=done?StoryState.Complete:next?StoryState.Next:StoryState.Later,From=from});

        public static SaloonStoryMap Build(Snapshot view)
        {
            var map=new SaloonStoryMap();
            if(view==null||!Enum.TryParse(view.ownRole,out Role role)||role==Role.Unknown)
            {map.NextTitle="Waiting for your role";map.NextDetail="The host will deal the roles when the match begins.";return map;}
            bool Has(StoryProgress flag)=>(view.storyProgress&flag)!=0;
            bool bar=Has(StoryProgress.Bar),hit=Has(StoryProgress.EnemyHit);
            bool terminal=view.phase==Phase.Finished||view.phase==Phase.Aftermath;
            bool won=terminal&&(role==Role.Outlaw?(view.outcome??"").StartsWith("OUTLAWS WIN"):
                role==Role.Renegade?(view.outcome??"").StartsWith("RENEGADE WINS"):(view.outcome??"").StartsWith("LAW WINS"));
            bool active=!terminal&&view.health>0;
            if(role==Role.Outlaw)
            {
                bool bag=view.moneyBag||view.outlawTeamMoneyBag,escape=(view.escapePending||view.outlawTeamEscapePending)&&bag;
                bool worked=Has(StoryProgress.SafeWork)||view.safeProgress>0||bag;
                map.Caption="THE HEIST  /  Follow the money. The lower trail is optional support.";
                map.Add("The Outlaw","Use H to steal at the Bar, then X on a later turn to start your escape.",4,145,300,true,false);
                map.Add("Reach the Bar","Right-click to plan. Choose Bar, then press Space to lock your choice.",7,310,300,bar,active&&!bar,0);
                string safeJobs=view.safeProgress>=0?Math.Min(3,view.safeProgress)+" / 3 jobs complete. ":"";
                map.Add("Work the safe",safeJobs+"Shared by all Outlaws. H beside the safe; stay for 3 seconds.",2,480,198,worked,active&&bar&&!worked,1);
                map.Add("Take the money",safeJobs+"Either Outlaw can finish the third H job and carry the money.",4,650,300,bag,active&&worked&&!bag,2);
                map.Add("Declare escape",$"With the bag, press X at Bar on a later turn. Declare by round {SaloonPosse.LastEscapeRound}.",5,820,198,escape,active&&bag&&!escape,3);
                map.Add("Ride out alive","Choose Bar again next round. Keep the bag and live until actions end.",5,820,415,won,active&&escape,4);
                map.Add("Cover your trail","Optional · Q to draw. Aim at a rival's head and left-click on your turn.",3,480,415,hit,false,1);
                int next=!bar?1:!worked?2:!bag?3:!escape?4:5;
                map.NextTitle=map.Nodes[next].Title;map.NextDetail=map.Nodes[next].Detail;
                if(escape)map.NextDetail="Escape declared. Choose Bar next round; keep the bag and survive until that round's actions end. A side room cancels the escape.";
                else if(view.round>=SaloonPosse.ArrivalRound)map.NextDetail="The posse arrives after this round. It is too late to declare a new escape.";
                else if(bag&&!view.actionAvailable)map.NextDetail="Money secured. Keep it safe; X uses a later turn at the Bar to declare escape.";
                if(bag&&!view.moneyBag)
                    map.NextDetail=escape?"Your fellow Outlaw declared escape. Protect them while they keep the money at the Bar through next round's actions.":
                        "Your fellow Outlaw carries the money. Protect them while they use X on a later turn at the Bar to declare escape.";
            }
            else if(role==Role.Renegade)
            {
                bool outlaws=Has(StoryProgress.OutlawsGone),law=Has(StoryProgress.LawGone);
                map.Caption="THE LONE TRAIL  /  Outlaws first. Then turn on the Law.";
                map.Add("Know your rivals","Read everyone's role in the intel on the right. Target the Outlaws first.",6,145,300,true,false);
                map.Add("Eliminate Outlaws","Meet an Outlaw. Q to draw; aim at the head and left-click on your turn.",3,350,198,outlaws,active&&!outlaws,0);
                map.Add("Turn on the Law","Once Outlaws are dead, use Q + headshots to kill the Sheriff and Deputy.",0,585,198,law,active&&outlaws&&!law,1);
                map.Add("Last one standing",$"Kill every rival and stay alive before round {SaloonPosse.ArrivalRound} ends. You cannot rob.",6,820,300,won,active&&outlaws&&law,2);
                map.Add("Find your footing","Optional · Choose Bar. E pours / stops beer; B drinks it to restore health.",7,350,415,bar,false,0);
                map.Add("Make it count","Optional · On your turn, Q draws. Aim at a rival's head, then left-click.",3,585,415,hit,false,4);
                int next=!outlaws?1:!law?2:3;
                map.NextTitle=map.Nodes[next].Title;map.NextDetail=map.Nodes[next].Detail;
            }
            else
            {
                bool sheriff=role==Role.Sheriff,inspected=!string.IsNullOrEmpty(view.safeReport);
                bool arrival=won&&(view.outcome??"").Contains("POSSE");
                map.Caption="DEFEND THE SALOON  /  Two routes to victory. Support actions are optional.";
                map.Add(sheriff?"Wear the badge":"Stand with the Law",sheriff?"Find your Deputy in the intel on the right. Work together to guard the money.":"Find the public Sheriff marker. Help the Sheriff guard the safe at Bar.",sheriff?0:1,145,300,true,false);
                map.Add("Guard the Bar","Right-click to plan. Choose Bar, then press Space to lock your choice.",7,310,300,bar,active&&!bar,0);
                map.Add(sheriff?"Inspect the safe":"Defend your ally",sheriff?"Optional · F to stand; WASD to the safe. Press C there on your turn to inspect.":"Optional · Q to draw; aim at a rival's head and left-click. Never shoot the Sheriff.",sheriff?2:3,495,198,sheriff?inspected:hit,active&&bar&&!(sheriff?inspected:hit),1);
                map.Add("Stop the criminals","Q to draw, then aim + left-click. Kill every Outlaw and the Renegade.",3,715,198,won&&!arrival,active&&bar,2);
                map.Add("Hold for the posse",$"Guard Bar and stop the thieves. Keep a Law player alive through round {SaloonPosse.ArrivalRound}.",6,585,415,arrival,active&&bar,1);
                map.Add("The Law prevails","Win by killing all criminals OR holding out until the posse arrives.",0,820,415,won,false,3,4);
                map.NextTitle=!bar?"Guard the Bar":sheriff&&!inspected?"Inspect the safe":"Stop criminals or hold the line";
                map.NextDetail=!bar?map.Nodes[1].Detail:sheriff&&!inspected?map.Nodes[2].Detail:
                    $"Stop the money escaping. Eliminate the criminals, or keep a Law player alive until the posse arrives after round {SaloonPosse.ArrivalRound}.";
            }
            if(active)
            {
                if((view.phase==Phase.Discussion||view.phase==Phase.Choosing)&&!map.NextDetail.Contains("Right-click"))
                    map.NextDetail+=" Right-click opens room planning; Space locks your choice.";
                else if(view.phase==Phase.Turns&&!view.actionAvailable)
                    map.NextDetail+=" Wait for your next available action.";
            }
            if(terminal){map.NextTitle=won?"Your side won":"The trail has ended";map.NextDetail=view.outcome??"Match complete.";}
            else if(view.health<=0){map.NextTitle="You have been eliminated";map.NextDetail="Your completed milestones remain recorded. Watch the surviving players.";}
            return map;
        }
    }
}
