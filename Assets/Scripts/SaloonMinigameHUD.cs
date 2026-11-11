using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        // Decisions live on screen; the felt is reserved for the physical cards/coin.
        void MinigameControls()
        {
            var duel=Game.View.duel;
            if(duel==null||Game.View.phase!=Phase.Turns||Game.MenuOpen||Game.ShowRole||Game.Self?.alive!=true)return;
            if(duel.stage=="result")
            {
                string winner=duel.firstId==Game.LocalId?"You win":duel.firstName+" wins";
                string result=duel.game=="coin"?duel.result+" · "+winner:winner;
                Text(420,279,440,42,result,new GUIStyle(title){fontSize=25,alignment=TextAnchor.MiddleCenter});
                Text(420,326,440,25,"Get ready · F to stand / WASD to dodge",new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
                Fill(new Rect(570,365,140,2),new Color(1,.90f,.68f,.25f));
                Fill(new Rect(570,365,140*Mathf.Clamp01(duel.remaining/4),2),new Color(1,.90f,.68f));
                return;
            }
            if(duel.stage=="playing")return;
            if(Cursor.lockState==CursorLockMode.Locked)return;
            if(duel.game=="coin")
            {
                if(duel.stage=="choosing"&&duel.canChooseCoin)
                    DecisionPanel("CALL THE COIN",294,y=>
                    {
                        if(Button(488,y,144,40,"HEADS"))Game.ChooseCoin(0);
                        if(Button(648,y,144,40,"TAILS"))Game.ChooseCoin(1);
                    });
                // During the toss the coin itself is the entire presentation.
                return;
            }
            if(duel.game!="blackjack")return;
            if(duel.stage=="redealing")
            {
                Text(440,485,400,32,"Push · another hand",new GUIStyle(body){alignment=TextAnchor.MiddleCenter});
                return;
            }
            if(duel.stage=="dealing")return;
            bool hidden=System.Array.Exists(duel.dealerHand??System.Array.Empty<int>(),card=>card<0);
            if(!duel.isDealer&&hidden)
                DecisionPanel("YOUR HAND",510,y=>
                {
                    bool enabled=GUI.enabled;GUI.enabled=enabled&&duel.canHit;
                    if(Button(488,y,144,40,"HIT"))Game.ChooseBlackjack("hit");
                    if(Button(648,y,144,40,"STAND"))Game.ChooseBlackjack("stand");
                    GUI.enabled=enabled;
                });
            else if(duel.canDeal)
                DecisionPanel("YOUR DEAL",510,y=>{if(Button(488,y,304,40,"DRAW CARD"))Game.ChooseBlackjack("deal");});
        }

        void DecisionPanel(string label,float top,System.Action<float> actions)
        {
            bool prior=worldHud;worldHud=false;
            // A compact, centered panel; no counters, totals, seat labels or rule text.
            Fill(new Rect(468,top,344,112),paper);
            Fill(new Rect(488,top+19,30,2),gold);
            Text(532,top+9,260,25,label,new GUIStyle(subtitle){alignment=TextAnchor.MiddleRight});
            actions(top+51);worldHud=prior;
        }
    }
}
