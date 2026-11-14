using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public static readonly Vector3 BartenderPosition=new Vector3(-5.8f,.08f,4.65f);
        public static readonly Vector3 BartenderApproach=new Vector3(-5.8f,.08f,2.35f);
        int bartenderBarShots;
        bool bartenderArrived;
        float bartenderArrivedAt=-100;
        int BartenderMood=>SaloonBartenderMood.FromShots(bartenderBarShots);
        public bool BartenderMenuOpen { get; private set; }
        public bool CanQuestionBartender=>View?.bartenderArrived==true&&System.Enum.TryParse(View.ownRole,out Role role)&&SaloonRules.CanAskBartender(role);
        public bool BartenderMenuVisible=>BartenderMenuOpen&&!MenuOpen&&!ShowRole&&AtBeerBar&&CanQuestionBartender&&NearBartender;
        public bool NearBartender=>Self?.alive==true&&Self.room==0&&AtBartender(new Vector3(Self.x,Self.y,Self.z));

        bool AtBartender(Vector3 position)
        {
            var offset=position-BartenderApproach;
            if(Mathf.Abs(offset.y)>1||Vector3.ProjectOnPlane(offset,Vector3.up).sqrMagnitude>1.35f*1.35f||position.z>3.1f)return false;
            var origin=position+Vector3.up*1.6f;
            var direction=new Vector3(BartenderPosition.x,1.65f,3.2f)-origin;
            return !Physics.RaycastAll(origin,direction.normalized,direction.magnitude,~(1<<SaloonRagdoll.Layer),QueryTriggerInteraction.Ignore)
                .Any(hit=>hit.collider.GetComponentInParent<SaloonBody>()==null);
        }

        public void ToggleBartender()
        {
            if(BartenderMenuOpen){CloseBartender();return;}
            if(!AtBeerBar||!CanQuestionBartender||!NearBartender)return;
            BartenderMenuOpen=true;SyncPointer();
        }
        public void CloseBartender(){BartenderMenuOpen=false;SyncPointer();}
        public void AskBartender(string target)
        {
            Connection.Send(new Command{kind="bartender",name=target});
            // Keep the private answer panel open; the host alone spends the question.
        }

        void QuestionBartender(Participant person,Command command)
        {
            if(!bartenderArrived){person.notice="The bartender has not arrived. Questions open in round "+SaloonRules.BartenderArrivalRound+".";return;}
            if(!SaloonRules.CanAskBartender(person.role)){person.notice="The Renegade already knows every role and cannot question the bartender.";return;}
            if(person.bartenderUsed){person.notice="You already asked your one question this match. Hold Tab to read the answer.";return;}
            if(phase!=Phase.Turns||!person.Alive||person.room!=0||!AtBartender(person.body.transform.position))
            {person.notice="Walk up to the bartender at the Bar during room actions. F stands; WASD moves.";return;}
            if(!ulong.TryParse(command.name,out var targetId)||!players.TryGetValue(targetId,out var target)||target==person||!target.Alive)
            {person.notice="Choose another living player to ask about.";return;}
            if(!SpendAction(person))return;
            person.bartenderUsed=true;
            person.bartenderQuestion="Is "+target.name+" an Outlaw?";
            person.bartenderAnswer=SaloonBartenderMood.Answer(target.role,BartenderMood,UnityEngine.Random.value);
            person.notice="Bartender: "+person.bartenderAnswer;
        }

        void NoteBartenderShot(Participant person)
        {
            if(bartenderArrived&&phase==Phase.Turns&&person.room==0&&bartenderBarShots<int.MaxValue)bartenderBarShots++;
        }

        void TickBartenderArrival()
        {
            if(bartenderArrived||!MatchActive||rounds.Round<SaloonRules.BartenderArrivalRound)return;
            bartenderArrived=true;bartenderArrivedAt=Time.time;
            AddEvent("Bartender arrived");
        }
    }
}
