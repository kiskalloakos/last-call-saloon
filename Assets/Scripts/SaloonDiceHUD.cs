using UnityEngine;

namespace Saloon
{
    public partial class SaloonHUD
    {
        GUIStyle DiceLabel(int size=16)=>new GUIStyle(body){fontSize=size,wordWrap=true,normal={textColor=new Color(.94f,.93f,.86f)}};
        readonly Color diceGold=new Color(.78f,.59f,.32f);
        Texture2D diceDot;
        void DicePanel(Rect rect)
        {
            Fill(new Rect(rect.x+4,rect.y+5,rect.width,rect.height),new Color(0,0,0,.24f));
            Fill(rect,new Color(.028f,.049f,.040f,.97f));Outline(rect,new Color(.62f,.48f,.27f,.75f));
            Fill(new Rect(rect.x+1,rect.y+1,rect.width-2,2),new Color(.78f,.59f,.32f,.35f));
        }
        void DiceCircle(Rect rect,Color color)
        {
            if(diceDot==null)
            {
                diceDot=new Texture2D(32,32,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave};
                for(int y=0;y<32;y++)for(int x=0;x<32;x++)
                {float distance=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(16,16));diceDot.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(16-distance)));}
                diceDot.Apply();uiTextures.Add(diceDot);
            }
            Graphic(rect,diceDot,color);
        }
        void DiceFace(Rect rect,int face)
        {
            Fill(new Rect(rect.x+3,rect.y+4,rect.width,rect.height),new Color(0,0,0,.35f));
            Fill(rect,new Color(.70f,.59f,.40f));
            Fill(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-5),new Color(.96f,.90f,.76f));
            var positions=new[]{new Vector2(-1,-1),new Vector2(1,1),new Vector2(-1,1),new Vector2(1,-1),Vector2.zero,new Vector2(-1,0),new Vector2(1,0)};
            float dot=rect.width*.13f;
            for(int i=0;i<7;i++)
            {
                bool show=i<2&&face>=2||i>=2&&i<4&&face>=4||i==4&&face%2==1||i>=5&&face==6;
                if(show)DiceCircle(new Rect(rect.center.x+positions[i].x*rect.width*.26f-dot/2,rect.center.y+positions[i].y*rect.height*.26f-dot/2,dot,dot),new Color(.13f,.10f,.065f));
            }
        }
        void DiceHand(float x,float y,int[] dice)
        {for(int i=0;i<(dice?.Length??0);i++)DiceFace(new Rect(x+i*62,y,48,48),dice[i]);}
        void DiceHeader(string status)
        {
            DicePanel(new Rect(220,24,840,100));
            Text(242,34,390,40,"BLUFF DICE",new GUIStyle(DiceLabel(28)){font=headingFont});
            Text(244,78,570,27,status,DiceLabel(17));
            var d=Game.View?.duel;
            int step=d?.stage=="waiting"||d?.stage=="rolling"?0:d?.stage=="speaking"?1:d?.stage=="judging"?2:3;
            string[] steps={"THROW","CLAIM","CALL","REVEAL"};
            for(int i=0;i<4;i++)
            {
                float x=650+i*98;
                Text(x,40,96,24,steps[i],new GUIStyle(DiceLabel(13)){alignment=TextAnchor.MiddleCenter,normal={textColor=i==step?diceGold:new Color(.49f,.52f,.45f)}});
                Fill(new Rect(x+8,70,80,2),i<=step?diceGold:new Color(.19f,.24f,.20f));
            }
            if(Game.DicePractice&&TableAction(878,83,156,"HOW TO PLAY",height:28))Game.ShowDicePracticeHelp();
            if(d!=null&&(!Game.DicePractice||d.stage!="judging"&&d.stage!="waiting"))
            {
                float duration=d.stage=="waiting"?SaloonGame.BluffThrowSeconds:d.stage=="rolling"?SaloonGame.DiceRollSeconds:d.stage=="speaking"?SaloonGame.BluffSpeakSeconds:d.stage=="judging"?SaloonGame.BluffJudgeSeconds:SaloonGame.DiceResultSeconds;
                Fill(new Rect(221,120,838*Mathf.Clamp01(d.remaining/duration),3),diceGold);
            }
        }
        void DicePracticeControls()
        {
            if(!Game.DicePractice||Game.MenuOpen||Game.View.duel?.game=="bluff"&&Game.View.duel.stage!="playing")return;
            bool prior=worldHud;worldHud=false;
            if(Game.View.phase==Phase.Aftermath||Game.View.phase==Phase.Finished)
            {
                DiceHeader("Practice · Hand finished");DicePanel(new Rect(320,410,640,200));
                Text(350,432,580,40,"Ready for another bluff?",DiceLabel(25));
                if(TableAction(350,510,580,"NEXT HAND · N"))Game.NewDicePracticeHand();
            }
            else
            {
                DicePanel(new Rect(24,78,310,64));Text(38,87,286,22,"Practice · Hand "+Game.DicePracticeHands,DiceLabel());
                Text(38,114,286,20,"N · Start another hand",DiceLabel(14));
            }
            worldHud=prior;
        }
        void DiceHelp()
        {
            DiceHeader("Practice · Nothing moves until you start");DicePanel(new Rect(220,160,840,512));
            Text(250,184,780,40,"One spoken claim. One judgment.",DiceLabel(26));
            Text(250,246,780,62,"The SPEAKER clicks THROW DICE or presses Space to roll 3 secret dice. Then say the given claim aloud and make the listener guess wrong.",DiceLabel(20));
            Text(250,340,780,54,"The LISTENER hears the claim, then chooses BELIEVE or DON'T BELIEVE. A correct judgment wins the opening. A wrong judgment gives it to the speaker.",DiceLabel(20));
            Text(250,430,780,52,"Only the speaker's 3 dice count. Reveal them to see what was true. No bids, raises or adding your own dice.",DiceLabel(19));
            Text(250,510,780,46,"Practice: bot captions and blind guesses. You alternate listener and speaker. No judgment timer. Review the reveal before combat.",DiceLabel(18));
            if(TableAction(350,584,580,"START PRACTICE"))Game.CloseDicePracticeHelp();
            Text(250,643,780,23,"N starts a new hand. In multiplayer: hold V to talk, or T for open mic.",DiceLabel(15));
        }
        void DiceControls(DuelView d)
        {
            bool prior=worldHud;worldHud=false;
            if(Game.DicePracticeHelp){DiceHelp();worldHud=prior;return;}
            string status=d.stage=="waiting"?(d.bluffIsSpeaker?"Your cup. Your throw.":"Waiting for "+d.bluffSpeakerName+" to throw."):d.stage=="rolling"?"Shake. Throw. Keep your secret.":d.stage=="speaking"?(d.bluffIsSpeaker?"Your dice. Your poker face.":"Watch the cup. Listen to the claim.")+"  ·  "+Mathf.CeilToInt(d.remaining)+"s":d.bluffIsSpeaker?"The claim is out. Await their call.":Game.DicePractice?"Trust your read. Take your time.":"Make your call  ·  "+Mathf.CeilToInt(d.remaining)+"s";
            DiceHeader(status);
            if(d.stage=="waiting")
            {
                DicePanel(new Rect(380,552,520,124));
                if(d.bluffIsSpeaker)
                {
                    Text(404,565,472,27,Game.DicePractice?"Throw when you're ready.":"Throw within "+Mathf.CeilToInt(Mathf.Min(d.remaining,SaloonGame.BluffThrowSeconds))+"s to keep your opening.",new GUIStyle(DiceLabel(18)){alignment=TextAnchor.MiddleCenter});
                    if(TableAction(404,608,472,"THROW DICE  ·  SPACE"))Game.ThrowBluffDice();
                }
                else
                {
                    Text(404,577,472,30,"THE SPEAKER HAS THE CUP",new GUIStyle(DiceLabel(20)){alignment=TextAnchor.MiddleCenter});
                    Text(404,623,472,27,"Their turn to throw. Your turn to read them.",new GUIStyle(DiceLabel(17)){alignment=TextAnchor.MiddleCenter});
                }
                worldHud=prior;return;
            }
            if(d.stage=="rolling")
            {
                DicePanel(new Rect(420,596,440,80));
                Text(440,607,400,28,"THREE DICE. ONE SECRET.",new GUIStyle(DiceLabel(20)){alignment=TextAnchor.MiddleCenter});
                Text(440,640,400,23,d.bluffIsSpeaker?"Your dice are being thrown onto the baize.":"Their roll stays hidden beneath the cup.",new GUIStyle(DiceLabel(15)){alignment=TextAnchor.MiddleCenter});
                worldHud=prior;return;
            }
            DicePanel(new Rect(220,500,840,188));
            if(d.bluffIsSpeaker)
            {
                Text(244,514,560,23,"SPEAKER  /  SAY THIS ALOUD",new GUIStyle(DiceLabel(14)){normal={textColor=diceGold}});
                Text(244,544,548,60,"“"+d.bluffClaim+"”",DiceLabel(25));
                Text(828,516,210,23,"YOUR SECRET ROLL",new GUIStyle(DiceLabel(13)){alignment=TextAnchor.MiddleCenter,normal={textColor=diceGold}});
                DiceHand(844,552,d.yourDice);
                Text(244,612,550,27,d.bluffClaimTrue?"TRUE CLAIM  ·  Make them doubt it.":"FALSE CLAIM  ·  Sell the bluff.",DiceLabel(17));
                Text(244,652,788,23,Game.DicePractice?"Your partner makes a blind call after the speaking time.":"Hold V to speak  ·  T for open mic  ·  Make them guess wrong",DiceLabel(15));
            }
            else
            {
                Text(244,514,788,23,"LISTENER  /  "+(d.bluffBotCaption?"YOUR PARTNER SAYS":d.bluffSpeakerName.ToUpperInvariant()+" HAS THE DICE"),new GUIStyle(DiceLabel(14)){normal={textColor=diceGold}});
                Text(244,543,788,52,d.bluffBotCaption?"“"+d.bluffClaim+"”":"Listen to their spoken claim. Can you trust them?",DiceLabel(24));
                bool enabled=GUI.enabled;GUI.enabled=enabled&&d.bluffCanJudge;
                if(TableAction(244,603,382,"BELIEVE  /  TRUE"))Game.ChooseBluff(true);
                if(TableAction(650,603,386,"CALL BLUFF  /  FALSE"))Game.ChooseBluff(false);
                GUI.enabled=enabled;
                Text(244,657,788,22,d.bluffCanJudge?"A correct call wins the opening. A wrong call hands it to the speaker.":"Hear them out. Make your call after the speaking time.",DiceLabel(15));
            }
            worldHud=prior;
        }
        void DiceResult(DuelView d)
        {
            bool prior=worldHud;worldHud=false;
            if(Game.DicePracticeHelp){DiceHelp();worldHud=prior;return;}
            DiceHeader(!d.bluffTruthKnown?"Hand forfeited":Game.DicePracticeReview?"The cup is up. The truth is on the table.":"The cup is up  ·  Combat in "+Mathf.CeilToInt(Mathf.Min(d.remaining,SaloonGame.DiceResultSeconds))+"s");
            DicePanel(new Rect(220,476,840,212));
            Text(244,488,550,35,d.firstId==Game.LocalId?"You win the opening":d.firstName+" wins the opening",new GUIStyle(DiceLabel(25)){normal={textColor=diceGold}});
            Text(244,533,560,48,d.bluffTruthKnown?"“"+d.bluffClaim+"”":"No dice were thrown.",DiceLabel(21));
            string judgment=d.bluffJudged?(d.bluffListenerId==Game.LocalId?"You called ":"They called ")+(d.bluffBelieved?"TRUE.":"BLUFF."):"";
            Text(244,585,560,40,judgment+" "+d.result,DiceLabel(16));
            if(d.bluffTruthKnown)Text(820,491,220,24,d.bluffClaimTrue?"THE CLAIM WAS TRUE":"THE CLAIM WAS FALSE",new GUIStyle(DiceLabel(14)){alignment=TextAnchor.MiddleCenter,normal={textColor=diceGold}});
            DiceHand(844,533,d.bluffIsSpeaker?d.yourDice:d.opponentDice);
            if(Game.DicePracticeReview)
            {
                if(TableAction(244,636,382,"CONTINUE TO COMBAT",height:38))Game.ContinueDicePractice();
                if(TableAction(650,636,386,"THROW AGAIN",height:38))Game.NewDicePracticeHand();
            }
            else Text(244,643,788,30,"Space to stand and dodge. The winner acts first; the opponent responds afterwards.",DiceLabel(16));
            worldHud=prior;
        }
    }
}
