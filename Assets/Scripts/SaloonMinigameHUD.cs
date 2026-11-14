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
                    DecisionControls("CALL THE COIN",y=>
                    {
                        if(TableAction(472,y,160,"HEADS"))Game.ChooseCoin(0);
                        if(TableAction(648,y,160,"TAILS"))Game.ChooseCoin(1);
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
                DecisionControls("YOUR MOVE",y=>
                {
                    bool enabled=GUI.enabled;GUI.enabled=enabled&&duel.canHit;
                    if(TableAction(472,y,160,"HIT"))Game.ChooseBlackjack("hit");
                    if(TableAction(648,y,160,"STAND"))Game.ChooseBlackjack("stand");
                    GUI.enabled=enabled;
                });
            else if(duel.canDeal)
                DecisionControls("YOUR DEAL",y=>{if(TableAction(520,y,240,"DRAW CARD"))Game.ChooseBlackjack("deal");});
        }

        void DecisionControls(string label,System.Action<float> actions)
        {
            bool prior=worldHud;worldHud=false;
            // One consistent bottom position keeps choices clear of the cards, coin and opponent.
            Text(440,560,400,26,label,tableActionCaption);
            actions(594);worldHud=prior;
        }

        bool TableAction(float x,float y,float width,string label)
        {
            var rect=new Rect(x,y,width,48);
            bool enabled=GUI.enabled;GUI.enabled=true;
            bool clicked=GUI.Button(rect,label,enabled?tableActionButton:tableActionDisabled);GUI.enabled=enabled;
            if(Event.current.type==EventType.Repaint)
            {
                bool hover=enabled&&rect.Contains(Event.current.mousePosition);
                var brass=new Color(.84f,.65f,.38f,enabled?(hover?1:.48f):.18f);
                Outline(rect,brass);
                Fill(new Rect(rect.x+12,rect.yMax-3,rect.width-24,1),new Color(.95f,.78f,.48f,hover?.65f:.12f));
            }
            return clicked&&enabled;
        }
    }
}
