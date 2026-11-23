using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        bool NearBeerTray(Participant p)=>SaloonBeerTaps.WithinReach(p.body.transform.position,tuning.interactionDistance);

        void DrinkBeer(Participant p)
        {
            if(!p.Alive||phase!=Phase.Turns||p.room!=0)
            {p.notice="Drink a ready beer at the Bar during room actions.";return;}
            if(!NearBeerTray(p)){p.notice="Walk up to the shared beer tray, then press E.";return;}
            if(p.hp>=tuning.health){p.notice="You are already at full health.";return;}
            if(beers<=0){p.notice="The shared beer tray is empty.";return;}
            if(!SpendAction(p,tuning.drinkSeconds))return;
            beers--;p.drinkHealing=PrepareBeerHealing(p,SaloonHealth.BeerHealing);
            p.beer=0;p.beerHealing=0;p.beerChoiceUntil=0;
            p.drawn=false;p.fireAfterDraw=false;p.pendingShot=null;CancelShotCharge(p);
            p.busyUntil=Time.time+tuning.drinkSeconds;p.busyAction="drink";
            p.notice="Drinking · +"+p.drinkHealing+"% on finishing.";
        }

        int PrepareBeerHealing(Participant drinker,int amount)
        {
            bool boosted=drinker.cigaretteBuff&&drinker.Alive&&drinker.hp<tuning.health&&amount>0;
            int healing=SaloonHealth.BeerHealingAmount(amount,boosted);
            if(boosted)drinker.cigaretteBuff=false;
            return healing;
        }
    }
}
