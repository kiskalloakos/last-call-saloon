using System;
using System.Collections.Generic;
using System.Linq;

namespace Saloon
{
    public enum Role { Unknown, Sheriff, Deputy, Outlaw, Renegade }
    public enum Phase { Lobby, Discussion, Turns, Finished, Choosing, Regroup, Aftermath }

    [Serializable]
    public class Tuning
    {
        [UnityEngine.Serialization.FormerlySerializedAs("openingSeconds")]
        public float discussionSeconds = 30;
        public float turnSeconds = 30;
        public float choosingSeconds = 15;
        public float regroupSeconds = 3;
        // Legacy serialized setting; positive values are ignored by the round clock.
        [UnityEngine.HideInInspector] public int roundLimit = 0;
        // Health is always percentage points; old scene overrides must not restore 3 HP.
        public int health => SaloonHealth.Maximum;
        public int cylinder = 6;
        public int sharedBeers = 8;
        public float shotInterval = .65f;
        public float reloadSeconds = 3.5f;
        public float drinkSeconds = 2.5f;
        public float handoffSeconds = .65f;
        public float drawSeconds = .25f;
        public float robberySeconds = 3;
        public float walkSpeed = 3.5f;
        public float interactionDistance = 2.5f;
    }

    public static class SaloonHealth
    {
        public const int Maximum=100;
        public const int RevolverDamage=20;
        public const int CigaretteDamage=2*RevolverDamage;
        public const int BeerHealing=20;
        public const int Segments=Maximum/RevolverDamage;
        public static int Clamp(int health)=>Math.Max(0,Math.Min(Maximum,health));
        public static int AfterDamage(int health,int damage)=>Math.Max(0,Clamp(health)-Math.Max(0,damage));
        public static int AfterShot(int health)=>AfterDamage(health,RevolverDamage);
        public static int AfterBeer(int health,int healing=BeerHealing)=>health<=0?0:Math.Min(Maximum,Clamp(health)+Math.Max(0,Math.Min(BeerHealing,healing)));
        public static int ShotsToKill(int health)=>(Clamp(health)+RevolverDamage-1)/RevolverDamage;
        public static string Percentage(int health)=>Clamp(health)+"%";
    }

    public readonly struct RoleGuide
    {
        public readonly string Goal,Actions,Win,Warning,PlanTitle,Plan;
        public RoleGuide(string goal,string actions,string win,string warning,string planTitle,string plan)
        {Goal=goal;Actions=actions;Win=win;Warning=warning;PlanTitle=planTitle;Plan=plan;}
    }

    public static class SaloonRules
    {
        public const int LobbyCapacity=8;
        const string EscapeGuide="1. Choose <b>Bar</b>, then Ready.\n2. On your turn, press <b>H</b>. Wait 3 seconds. A hit stops the job.\n3. Three jobs total open the safe. The last worker gets the money.\n4. Press <b>X</b> on a later turn.\n5. Choose <b>Bar again</b> next round. Stay alive with the money until actions end.\nThe posse arrives after round 10. Declare by round 9 to escape in time.";
        public static readonly Role[] Deck = { Role.Sheriff, Role.Deputy, Role.Outlaw, Role.Outlaw, Role.Renegade };
        public static bool SupportedMatchSize(int count)=>count==4||count==5;
        public static Role[] DeckFor(int count)=>count==4?new[]{Role.Sheriff,Role.Deputy,Role.Outlaw,Role.Renegade}:count==5?Deck.ToArray():throw new ArgumentOutOfRangeException(nameof(count),"Role rules are currently defined for four or five players.");
        public static bool CanRob(Role role)=>role==Role.Outlaw;
        public static bool Allies(Role a,Role b)=>
            ((a==Role.Sheriff||a==Role.Deputy)&&(b==Role.Sheriff||b==Role.Deputy))||
            (a==Role.Outlaw&&b==Role.Outlaw);

        // Elimination victory only. Criminals must actually escape with the money.
        public static string Winner(IEnumerable<Role> living)
        {
            var roles = living.ToArray();
            if (roles.Length == 0) return "DRAW — NO SURVIVORS";
            if (roles.Length == 1 && roles[0] == Role.Renegade) return "RENEGADE WINS — LAST STANDING";
            if (!roles.Contains(Role.Outlaw) && !roles.Contains(Role.Renegade)) return "LAW WINS";
            return "";
        }

        public static string Objective(Role role)
        {
            switch (role)
            {
                case Role.Sheriff: return "Survive and stop the escape until the posse arrives after round 10, or eliminate all criminals. Sheriff death does not end the match.";
                case Role.Deputy: return "Defend the money with the Sheriff. You can still win after the Sheriff dies.";
                case Role.Outlaw: return "Escape with the money; either Outlaw can win for both. Your fellow Outlaw is hidden, too.";
                case Role.Renegade: return "You know everyone’s role. Eliminate the Outlaws first, then the Sheriff and Deputy. Be the last player alive before the posse arrives after round 10. You cannot rob or escape with money.";
                default: return "Waiting for the host to deal the roles.";
            }
        }

        public static RoleGuide Guide(Role role,int playerCount=5)
        {
            switch(role)
            {
                case Role.Sheriff:return new RoleGuide(
                    "Protect the money with the Deputy. Everyone knows you are the Sheriff.",
                    "<b>C at the Bar</b> · Inspect the safe. Your private report shows its condition, not the thief's name. Check again on a later turn.",
                    "Eliminate every Outlaw and the Renegade, or survive and stop the escape until the posse arrives after round 10. You cannot rob or escape with money.",
                    "The Deputy is your ally. Shooting them also costs you 20% health.",
                    "HOW TO PROTECT THE MONEY",
                    "1. Choose <b>Bar</b>, then Ready.\n2. On your turn, press <b>C</b> to inspect. It tells you whether the safe was touched, if new marks appeared, or money is missing. Hold <b>Tab</b> to read your private clues at any time.\n3. Hold <b>V</b> to talk. Ask what others did and compare their stories.\n4. Use your turn to shoot a suspected thief or help an ally with beer.\n5. Keep the money from escaping. The Deputy is helping you, but their role is hidden.");
                case Role.Deputy:return new RoleGuide(
                    "Help the Sheriff protect the money. The Renegade knows your role; the Sheriff does not.",
                    "Protect the Sheriff and stop the thieves. <b>C is Sheriff-only.</b> Use your turn to defend the money or help the Sheriff.",
                    "You share the Sheriff's win. Survive and keep the money safe until the posse arrives after round 10, or eliminate all criminals. You can still win after the Sheriff dies.",
                    "The Sheriff is your ally. Shooting them also costs you 20% health.",
                    "HOW TO HELP THE SHERIFF",
                    "1. Choose <b>Bar</b> to help guard the safe, then Ready.\n2. Hold <b>V</b> to talk and find out who may be stealing.\n3. Wait for your turn. Press <b>Q</b>, aim, then <b>left-click</b> to shoot an enemy.\n4. Pour with <b>E</b>, then <b>E</b> to stop. Aim at a nearby Sheriff and press <b>G</b> to offer your poured healing.\n5. Ask the Sheriff about their <b>C</b> inspection. You cannot inspect it yourself.\nProtect the Sheriff; do not shoot them.");
                case Role.Outlaw:return new RoleGuide(
                    playerCount==4?"You are the only Outlaw. Steal the money and escape to win.":"Steal the money. If either Outlaw escapes with it, both Outlaws win.",
                    "Choose <b>Bar</b>. Use <b>H</b> to work on the safe or collect the money, then <b>X</b> on a later turn to start escaping.",
                    (playerCount==4?"You must":"Either Outlaw must")+" finish an escape with the money before the posse arrives after round 10. Declare by round 9. Killing the Sheriff alone does not win the match.",
                    playerCount==4?"You have no teammate in a four-player match. The Law and Renegade are your rivals.":"Your fellow Outlaw is hidden. Shooting another Outlaw also costs you 20% health.",
                    "HOW TO TAKE THE MONEY & ESCAPE",EscapeGuide);
                case Role.Renegade:return new RoleGuide(
                    "You know everyone’s role. Eliminate the Outlaws first, then the Sheriff and Deputy.",
                    "Use your turn to shoot an enemy or heal yourself. <b>H and X are Outlaw-only.</b> You do not steal money.",
                    "Be the only player left alive before the posse arrives after round 10. An Outlaw escaping with the money does not count as your win.",
                    "Keep the Law alive while you fight the Outlaws. After the Outlaws are gone, turn on the Law.",
                    "OUTLAWS FIRST, THEN THE LAW",
                    "1. Hold <b>Tab</b> to see everyone’s role.\n2. Choose a room with an Outlaw, then Ready.\n3. On your turn, press <b>Q</b>, aim, then <b>left-click</b> to shoot.\n4. Use <b>B</b> to heal yourself with beer when needed.\n5. Once all Outlaws are dead, eliminate the Sheriff and Deputy.\nYou win when nobody else is alive.");
                default:return new RoleGuide("Waiting for the host to deal your role.","","","","","");
            }
        }

        public static string Hint(Snapshot view,PlayerView self)
        {
            if(self==null||!self.alive||view.phase==Phase.Lobby||view.phase==Phase.Finished)return "";
            if(view.phase==Phase.Discussion)
                return self.ready?"Seat reserved. Unready or change your plan to release it.":"Choose your destination privately, then Ready. Only the person you invite sees that invitation.";
            if(view.phase==Phase.Choosing)return view.moneyBag?"Choose the bar. A side room cancels your escape.":view.ownRole=="Renegade"?"Find the Outlaws first. Choose someone to meet in a room.":"Choose a room or invite someone for a private word.";
            if(view.phase==Phase.Regroup)return "Remember who you saw. Decide what you want to tell the table.";
            if(view.phase==Phase.Aftermath)return "Let the dust settle.";
            if(self.room==3)return "Left-click · Take a drag    V · Talk    WASD · Stroll. No weapons outside.";
            if(view.duel?.stage=="blackjack")return view.duel.canHit?"Choose Hit or Stand.":view.duel.canDeal?"Your deal.":"Watch the cards.";
            if(view.duel?.stage=="result")return "Get ready. F to stand, WASD to dodge. Weapons unlock after the result pause.";
            if(view.duel?.stage=="redealing")return "Blackjack push. A tied hand is redealt with the same dealer.";
            if(view.duel?.stage=="choosing")return "Choose Heads or Tails. First claim locks the sides; the coin decides the opening.";
            if(view.duel?.stage=="flipping")return "Let the coin land. Weapons are locked until the opening is decided.";
            if(view.duel?.waitingForOpening==true)return view.duel.firstName+" has the opening. You may respond after their action or pass.";
            if(view.pouring)return "E · Stop pouring near the fill line. Healing rounds up in steps of 5%, up to 20%.";
            if(view.carriedBeer>0&&view.beerChoiceRemaining>0)return "B · Drink for +"+view.beerHealing+"% health. Aim + G · Offer. Space · Leave beer.";
            if(view.roomTurn?.spent==true)return "Your action is spent. Watch the remaining turns.";
            if(view.roomTurn!=null&&!view.roomTurn.yourTurn)return string.IsNullOrEmpty(view.roomTurn.currentId)?"The room has finished its actions.":view.roomTurn.currentName+" has the next move. Your turn follows the room order.";
            if(view.busyRemaining>0)return "Beer heals when you finish drinking. A headshot interrupts safe work.";
            if(!view.actionAvailable)return "Your action is spent. Listen, watch, and plan your next round.";
            if(view.escapePending)return "Hold the bar until this action phase ends. Protect the money.";
            if(view.moneyBag)return "X · Commit to escape, then survive the next action phase at the bar.";
            if(view.carriedBeer>0&&view.health<self.maxHealth)return "B · Drink your poured beer. G · Offer it to someone nearby.";
            if(view.ownRole=="Renegade")return "Eliminate the Outlaws first, then the Law. Q to draw, aim and click to shoot. Be the last alive.";
            if(self.room==0)return view.ownRole=="Sheriff"?"C · Inspect the safe privately, using your action.":view.ownRole=="Outlaw"?"H · Work the safe quietly. Three completed attempts release the money.":"E · Pour beer as your action; E again to stop. B drinks it; G offers it.";
            return "One action. Draw with Q, then aim carefully—or hear them out.";
        }
    }

    [Serializable]
    public class Command
    {
        public string kind;
        public string name;
        public float x, z, yaw, pitch;
        public bool crouch;
        public int poseVersion,planVersion,invitationVersion;
    }

    [Serializable]
    public class PlayerView
    {
        public string id, name, revealedRole,voiceId;
        public float x, y, z, yaw, pitch,bodyYaw;
        public bool disconnected,rematchVote;
        public bool alive, drawn, seated, crouched, sheriff, busy,ready,observer,bot;
        public float deathAge,deathX,deathY,deathZ;
        public int shots,health,maxHealth,carriedBeer,hits,puffs,reloads;
        public bool reloading;public float reloadRemaining,reloadDuration;
        public float smokeAge;
        public float hitAge,hitX,hitY,hitZ;
        public bool drinking, robbing, moneyBag, escaping;
        public bool pouring;public float pourElapsed;public int beerHealing,beerStation;public float pourFlow=1;
        public int room, chosenRoom, poseVersion,planVersion;
        public string companion;
        public int giftSequence;
        public string giftTarget;
        public float drinkRemaining,drinkDuration,giftRemaining;
    }

    [Serializable]
    public class KnownRole
    {
        public string id,name,role;
    }

    [Serializable]
    public class RoomInvitation
    {
        public string fromId,fromName;
        public int room,planVersion;
    }

    [Serializable]
    public class Snapshot
    {
        public KnownRole[] knownRoles;
        public RoomInvitation[] invitations;
        public string ownId,reconnectNotice;
        public int rematchVotes,rematchRequired;
        public bool ownRematchVote;
        public PlayerView[] players;
        public Phase phase;
        public float remaining;
        public string outcome, ownRole, ally, notice, voiceRoom;
        public string activePlayerId, activePlayerName;
        public string[] turnOrder;
        public int round, roundLimit;
        public int actionsRemaining,actionsInProgress,hitSequence,confirmedHealth,confirmedMaxHealth;
        public bool confirmedKill,confirmedFriendly;
        public float confirmedHitAge;
        public string actionBlockReason;
        public int posseStage,posseRoundsRemaining;
        public bool actionAvailable,plannedRoomFull;
        public int[] roomCapacities;
        public DuelView duel,blackjackTable,coinTable;
        public RoomTurnView roomTurn;
        public bool hasDuel,hasRoomTurn,hasBlackjackTable,hasCoinTable;
        public string safeReport;
        public string[] evidence;
        public int health, ammo, beers, carriedBeer,beerHealing;
        public bool pouring;public float pourElapsed,beerChoiceRemaining;
        public bool sandbox, moneyBag, escapePending;
        // Recipient-only state; never included in public PlayerView records.
        public bool cigaretteBuff;
        public bool botsEnabled;
        public int botMatchSize=5,lobbyCapacity=8;
        public int safeProgress=-1;
        public string objectiveNotice;
        public float busyRemaining;

        public string ToWireJson()
        {
            // JsonUtility serializes null inline classes as empty objects. Presence must
            // travel separately, or clients invent coin/turn panels that the host lacks.
            hasCoinTable=phase==Phase.Turns&&coinTable!=null;
            hasBlackjackTable=phase==Phase.Turns&&blackjackTable!=null;
            hasDuel=phase==Phase.Turns&&duel!=null;
            hasRoomTurn=phase==Phase.Turns&&roomTurn!=null;
            return UnityEngine.JsonUtility.ToJson(this);
        }

        public static Snapshot FromWireJson(string json)
        {
            var snapshot=UnityEngine.JsonUtility.FromJson<Snapshot>(json);
            if(snapshot==null)return null;
            if(!snapshot.hasCoinTable||snapshot.phase!=Phase.Turns)snapshot.coinTable=null;
            if(!snapshot.hasBlackjackTable||snapshot.phase!=Phase.Turns)snapshot.blackjackTable=null;
            if(!snapshot.hasDuel||snapshot.phase!=Phase.Turns)snapshot.duel=null;
            if(!snapshot.hasRoomTurn||snapshot.phase!=Phase.Turns)snapshot.roomTurn=null;
            return snapshot;
        }
    }
}
