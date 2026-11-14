using UnityEngine;

namespace Saloon
{
    public static class SaloonBeerPour
    {
        public const float FullSeconds=3,MaxSeconds=4.5f,ChoiceSeconds=6;
        public static float Fill(float seconds)=>Mathf.Clamp(seconds/FullSeconds,0,1.5f);
        // A broad sweet spot keeps this a relaxed activity. Overflow wastes beer.
        public static int Healing(float seconds)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds))return 0;
            float quality=Mathf.Clamp01(1-Mathf.Abs(1-Fill(seconds)));
            return RoundHealing(quality*SaloonHealth.BeerHealing);
        }
        public static int RoundHealing(float raw)=>float.IsNaN(raw)||raw<=0?0:Mathf.Clamp(Mathf.CeilToInt(Mathf.Min(raw,20)/5)*5,0,20);
    }

    public partial class SaloonGame
    {
        bool TryBeerCommand(Participant p,string kind)
        {
            if(!p.Alive||phase!=Phase.Turns||p.room!=0)return false;
            if(p.busyAction=="pour")
            {
                if(kind=="interact"||kind=="stopPour")StopPour(p);
                else if(kind!="stance")p.notice="E · Stop pouring. Fill to the line; overflow wastes beer.";
                return kind!="stance";
            }
            if(kind=="stopPour")return true;
            if(p.beer>0&&p.beerChoiceUntil>0)
            {
                if(Time.time>=p.beerChoiceUntil){LeaveBeer(p);return true;}
                if(kind=="drink"){DrinkBeer(p);return true;}
                if(kind=="give"){Give(p);return true;}
                if(kind=="leaveBeer"||kind=="pass"){LeaveBeer(p);return true;}
            }
            return false;
        }

        void BeginPour(Participant p)
        {
            if(!p.Alive||phase!=Phase.Turns||p.room!=0){p.notice="Pour beer at the bar during room actions.";return;}
            if(p.hp>=tuning.health){p.notice="Full health · Watch the bar or use your action for something else.";return;}
            Vector3 tap=SaloonBeerTaps.StationPosition(p.beerStation);
            if(Vector3.ProjectOnPlane(tap-p.body.transform.position,Vector3.up).magnitude>tuning.interactionDistance)
            {p.notice="Move back to your beer tap to pour.";return;}
            if(p.beer>0||p.beerPickupRound==rounds.Round){p.notice="One pour per round. Drink, offer or leave your mug.";return;}
            if(beers<=0){p.notice="The shared beer supply is empty.";return;}
            float reserve=SaloonBeerPour.MaxSeconds+SaloonBeerPour.ChoiceSeconds+tuning.handoffSeconds+tuning.drinkSeconds;
            if(!SpendAction(p,reserve))return;
            p.beerPickupRound=rounds.Round;beers--;p.beerHealing=0;p.beerChoiceUntil=0;
            p.pourStarted=Time.time;p.pourTick=Time.time;p.pourFlow=1;p.botPourSeconds=UnityEngine.Random.Range(1.65f,3.55f);
            p.drawn=false;p.fireAfterDraw=false;p.move=Vector2.zero;
            p.busyAction="pour";p.busyUntil=Time.time+SaloonBeerPour.MaxSeconds;
            p.notice="Pouring · E to stop at the line. Up to +"+SaloonHealth.BeerHealingAmount(SaloonHealth.BeerHealing,p.cigaretteBuff)+"% health.";
        }

        void StopPour(Participant p)
        {
            if(!p.Alive||p.busyAction!="pour")return;
            p.beerHealing=SaloonBeerPour.Healing(Time.time-p.pourStarted);p.beer=1;
            p.busyAction="";p.busyUntil=0;p.beerChoiceUntil=Time.time+SaloonBeerPour.ChoiceSeconds;
            rounds.SetCompletion(p.id,p.beerChoiceUntil);
            p.notice="Poured +"+p.beerHealing+"% · B drink for +"+SaloonHealth.BeerHealingAmount(p.beerHealing,p.cigaretteBuff)+"% · G offer · Space leave it.";
        }

        void TickBeer(Participant p)
        {
            if(p.busyAction=="pour")
            {
                float dt=Mathf.Max(0,Time.time-p.pourTick);p.pourTick=Time.time;
                p.pourStarted+=dt*(1-p.pourFlow);
                if(Time.time>=p.busyUntil)StopPour(p);
            }
            if(p.beerChoiceUntil>0&&Time.time>=p.beerChoiceUntil)LeaveBeer(p);
        }

        // A completed pour has already paid for its drink or handoff.
        bool CommitBeerUse(Participant p,float duration)
        {
            if(p.beerChoiceUntil>0)
            {
                if(!p.Alive||phase!=Phase.Turns||p.room!=0||Time.time>=p.beerChoiceUntil||p.busyUntil>Time.time)return false;
                p.beerChoiceUntil=0;rounds.SetCompletion(p.id,Time.time+duration);return true;
            }
            return SpendAction(p,duration);
        }

        void DrinkBeer(Participant p)
        {
            if(p.beer<=0)
            {if(p.room==0)BeginPour(p);else p.notice="Go to the bar to pour a beer.";return;}
            if(p.hp>=tuning.health){p.notice="You are already at full health. Offer or leave your beer.";return;}
            if(!CommitBeerUse(p,tuning.drinkSeconds))return;
            p.drinkHealing=PrepareBeerHealing(p,p.beerHealing);p.beerHealing=0;p.beer--;p.drawn=false;
            p.busyUntil=Time.time+tuning.drinkSeconds;p.busyAction="drink";
            p.notice="Drinking · +"+p.drinkHealing+"% on finishing. Hits do not interrupt the sip.";
        }

        int PrepareBeerHealing(Participant drinker,int poured)
        {
            bool boosted=drinker.cigaretteBuff&&drinker.Alive&&drinker.hp<tuning.health&&poured>0;
            int healing=SaloonHealth.BeerHealingAmount(poured,boosted);
            if(boosted)drinker.cigaretteBuff=false;
            return healing;
        }

        void LeaveBeer(Participant p)
        {
            p.beer=0;p.beerHealing=0;p.beerChoiceUntil=0;rounds.SetCompletion(p.id,Time.time);
            p.notice="Beer left at the bar. Your pour used this round's action.";
        }
    }
}
