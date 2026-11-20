using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        Texture2D resultPortraits;

        void ShowMatchResults(Snapshot view)
        {
            worldHud=false;
            if(resultPortraits==null)resultPortraits=Resources.Load<Texture2D>("UI/ResultPortraits");
            var cast=view.matchResults;
            if(cast==null||cast.Length==0)
            {
                cast=view.players.Where(p=>!string.IsNullOrEmpty(p.revealedRole)&&p.revealedRole!=nameof(Role.Unknown))
                    .Select(p=>
                    {
                        Enum.TryParse(p.revealedRole,out Role role);
                        return new MatchResultPlayer{id=p.id,name=p.name,role=role,alive=p.alive,winner=SaloonRules.WonMatch(role,view.outcome)};
                    }).ToArray();
            }
            var roster=cast.OrderByDescending(p=>p.winner).ThenBy(p=>p.role).ToArray();
            var (heading,reason)=ResultHeading(view.outcome);

            Fill(new Rect(0,0,1280,720),new Color(.016f,.021f,.019f,.88f));
            var panel=new Rect(76,54,1128,612);
            Fill(panel,StoryNight);
            Outline(panel,StoryQuiet);
            Fill(new Rect(panel.x,panel.y,panel.width,3),StoryGold);
            ResultText(new Rect(105,81,1070,56),heading,39,StoryCream,true);
            ResultText(new Rect(105,139,1070,32),reason,20,StoryGrey);

            const float cardWidth=198,gap=16;
            float left=(1280-(roster.Length*cardWidth+Mathf.Max(0,roster.Length-1)*gap))*.5f;
            for(int i=0;i<roster.Length;i++)
                ResultCard(new Rect(left+i*(cardWidth+gap),197,cardWidth,324),roster[i],view.ownId);

            ResultText(new Rect(300,546,680,28),$"Rematch votes  {view.rematchVotes} of {view.rematchRequired}. All players must vote.",17,StoryCream);
            if(PlannerAction(338,585,292,44,view.ownRematchVote?"Withdraw vote":"Vote for rematch",true))Game.Act("rematch");
            if(PlannerAction(650,585,292,44,"Return everyone to lobby"))Game.Act("lobby");
        }

        void ResultCard(Rect card,MatchResultPlayer player,string localId)
        {
            var textColor=player.alive?StoryCream:new Color(.77f,.77f,.77f);
            var border=player.alive?(player.winner?StoryGold:StoryQuiet):new Color(.43f,.43f,.43f);
            Fill(card,player.alive?new Color(.11f,.14f,.13f):new Color(.125f,.125f,.125f));
            Outline(card,border);
            if(player.winner)Fill(new Rect(card.x,card.y,card.width,3),border);

            if(resultPortraits!=null)
            {
                int column=Mathf.Clamp((int)player.role-1,0,3);
                var portrait=new Rect(card.x+8,card.y+8,182,182);
                GUI.DrawTextureWithTexCoords(portrait,resultPortraits,new Rect(column*.25f,player.alive?.5f:0,.25f,.5f));
            }
            ResultText(new Rect(card.x+8,card.y+199,182,28),player.role.ToString(),21,textColor,true);
            string name=player.name+(player.id==localId?" (you)":"");
            ResultText(new Rect(card.x+8,card.y+229,182,49),name,18,textColor);
            Fill(new Rect(card.x+12,card.y+282,174,1),border);
            var status=new Rect(card.x+12,card.y+288,player.winner?80:174,26);
            ResultText(status,player.alive?"Alive":"Dead",17,textColor);
            if(player.winner)
            {
                var label=new Rect(card.x+98,card.y+288,88,26);
                Fill(label,player.alive?StoryGold:new Color(.70f,.70f,.70f));
                ResultText(label,"Winner",17,StoryNight);
            }
        }

        void ResultText(Rect area,string text,int size,Color color,bool heading=false)
            =>Text(area.x,area.y,area.width,area.height,text,new GUIStyle(heading?title:body)
            {fontSize=size,richText=false,wordWrap=true,alignment=TextAnchor.MiddleCenter,normal={textColor=color}});

        static (string Heading,string Reason) ResultHeading(string outcome)
        {
            switch(outcome)
            {
                case "LAW WINS": return ("The Law wins","All Outlaws and the Renegade are dead.");
                case "LAW WINS — POSSE ARRIVED": return ("The Law wins","The posse arrived.");
                case "OUTLAWS WIN — MONEY ESCAPED": return ("Outlaws win","An Outlaw escaped with the money.");
                case "RENEGADE WINS — LAST STANDING": return ("Renegade wins","The Renegade is the last player alive.");
                case "DRAW — NO SURVIVORS": return ("Draw","Nobody survived.");
                case "DRAW — POSSE CAPTURED THE SURVIVORS": return ("Draw","The posse caught the survivors.");
                case "MATCH ABORTED — RECONNECT WINDOW EXPIRED": return ("Match ended early","A player could not reconnect in time.");
                default: return ("Match results",outcome??"");
            }
        }
    }
}
