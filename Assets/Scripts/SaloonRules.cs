using System;
using System.Collections.Generic;
using System.Linq;

namespace Saloon
{
    public enum Role { Unknown, Sheriff, Deputy, Outlaw, Renegade, Swindler }
    public enum Phase { Lobby, Discussion, Turns, Finished, Choosing, Regroup, Aftermath }

    [Serializable]
    public class Tuning
    {
        [UnityEngine.Serialization.FormerlySerializedAs("openingSeconds")]
        public float discussionSeconds = 30;
        public float turnSeconds = 30;
        public float choosingSeconds = 15;
        public float regroupSeconds = 3;
        // Legacy round limit remains ignored by the round clock.
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
        public float robberySeconds = 2; // Legacy scene field; drawer checks use DrawerInteractionSeconds.
        public float walkSpeed = 3.5f;
        public float interactionDistance = 2.5f;
    }

    public static class SaloonHealth
    {
        public const int Maximum=100;
        public const int RevolverDamage=20;
        public const int CigaretteDamage=2*RevolverDamage;
        public const int BeerHealing=20;
        public static int BeerHealingAmount(int poured,bool cigaretteBuff)=>Math.Max(0,Math.Min(BeerHealing,poured))*(cigaretteBuff?2:1);
        public const int Segments=Maximum/RevolverDamage;
        public static int Clamp(int health)=>Math.Max(0,Math.Min(Maximum,health));
        public static int AfterDamage(int health,int damage)=>Math.Max(0,Clamp(health)-Math.Max(0,damage));
        public static int AfterShot(int health)=>AfterDamage(health,RevolverDamage);
        public static int AfterBeer(int health,int healing=BeerHealing)=>health<=0?0:Math.Min(Maximum,Clamp(health)+Math.Max(0,Math.Min(Maximum,healing)));
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
        public const int MinimumPlayers=4;
        public const int LobbyCapacity=8;
        public const int BarCapacity=LobbyCapacity;
        public const float DrawerInteractionSeconds=2;
        public const int DrawerWorkSteps=3;
        static readonly string EscapeGuide="1. Choose <b>Bar</b> and lock your plan.\n2. Walk to the cash drawer beside the beer tray. <b>C</b> checks it; <b>H</b> works the drawer for two seconds. Complete three shared steps across three rounds to take the money. Both look the same to witnesses.\n3. In the next discussion, choose <b>Attempt escape</b> or press <b>X</b>, then lock your Bar plan. Everyone hears the warning and has time to change rooms.\n4. Your attempt starts with the next action phase. You still have one action to shoot, heal, reload or pass. Stay at Bar and avoid headshots until Bar actions finish. One headshot stops the attempt; keep the money and try again later.";
        public static readonly Role[] Deck = { Role.Sheriff, Role.Deputy, Role.Outlaw, Role.Outlaw, Role.Renegade };
        public static bool SupportedMatchSize(int count)=>count>=MinimumPlayers&&count<=LobbyCapacity;
        public static bool IsIndependent(Role role)=>role==Role.Renegade||role==Role.Swindler;
        public static Role IndependentFor(float roll)=>roll<.5f?Role.Renegade:Role.Swindler;
        public static Role[] DeckFor(int count,Role independent=Role.Renegade)
        {
            if(!IsIndependent(independent))throw new ArgumentOutOfRangeException(nameof(independent));
            return ClassicDeckFor(count).Select(r=>r==Role.Renegade?independent:r).ToArray();
        }
        // Anonymous briefing slots never reveal which independent was rolled to another role.
        public static Role[] MatchupFor(int count,Role ownRole)
        {
            var roles=DeckFor(count,IsIndependent(ownRole)?ownRole:Role.Renegade).ToList();
            if(!roles.Remove(ownRole))throw new ArgumentOutOfRangeException(nameof(ownRole));
            return new[]{ownRole}.Concat(roles.Select(r=>IsIndependent(r)?Role.Unknown:r)
                .OrderBy(r=>Allies(ownRole,r)?0:1).ThenBy(r=>r)).ToArray();
        }
        static Role[] ClassicDeckFor(int count)
        {
            switch(count)
            {
                case 4:return new[]{Role.Sheriff,Role.Deputy,Role.Outlaw,Role.Renegade};
                case 5:return Deck.ToArray();
                case 6:return new[]{Role.Sheriff,Role.Deputy,Role.Deputy,Role.Outlaw,Role.Outlaw,Role.Renegade};
                case 7:return new[]{Role.Sheriff,Role.Deputy,Role.Deputy,Role.Outlaw,Role.Outlaw,Role.Outlaw,Role.Renegade};
                case 8:return new[]{Role.Sheriff,Role.Deputy,Role.Deputy,Role.Deputy,Role.Outlaw,Role.Outlaw,Role.Outlaw,Role.Renegade};
                default:throw new ArgumentOutOfRangeException(nameof(count),"Matches require four to eight players.");
            }
        }
        public static bool CanRob(Role role)=>role==Role.Outlaw;
        public static bool CanInspectSafe(Role role)=>role!=Role.Unknown&&Enum.IsDefined(typeof(Role),role);
        public static bool KnowsRole(Role viewer,Role subject)=>viewer==Role.Outlaw&&subject==Role.Outlaw;
        public static bool SwindlerWins(Role victim,Role attacker,bool lethal,bool selfInflicted=false,bool reflected=false)
            =>victim==Role.Swindler&&lethal&&!selfInflicted&&!reflected&&(attacker==Role.Sheriff||attacker==Role.Deputy);
        public static bool Allies(Role a,Role b)=>
            ((a==Role.Sheriff||a==Role.Deputy)&&(b==Role.Sheriff||b==Role.Deputy))||
            (a==Role.Outlaw&&b==Role.Outlaw);

        // The Renegade must be alone before the Sheriff's death can award Outlaws.
        public static string Winner(IEnumerable<Role> living)
        {
            var roles = living.ToArray();
            if (roles.Length == 0) return "DRAW — NO SURVIVORS";
            if (roles.Length == 1 && roles[0] == Role.Renegade) return "RENEGADE WINS — LAST STANDING";
            if (!roles.Contains(Role.Sheriff)) return "OUTLAWS WIN — SHERIFF DEAD";
            if (!roles.Contains(Role.Outlaw) && !roles.Contains(Role.Renegade)) return "LAW WINS";
            return "";
        }

        public static bool WonMatch(Role role,string outcome,bool personalVictory=false)
        {
            switch(role)
            {
                case Role.Sheriff:
                case Role.Deputy: return outcome=="LAW WINS";
                case Role.Outlaw: return outcome=="OUTLAWS WIN — MONEY ESCAPED"||outcome=="OUTLAWS WIN — SHERIFF DEAD";
                case Role.Renegade: return outcome=="RENEGADE WINS — LAST STANDING";
                case Role.Swindler: return personalVictory;
                default: return false;
            }
        }

        public static string Objective(Role role)
        {
            switch (role)
            {
                case Role.Sheriff: return "Stay alive and eliminate the Outlaws and any Renegade. Your death wins for Outlaws unless the Renegade is the sole survivor.";
                case Role.Deputy: return "Keep the Sheriff alive and stop the Outlaws. Sheriff death gives Outlaws the win unless the Renegade is the sole survivor.";
                case Role.Outlaw: return "Kill the Sheriff or escape with the money to win for the Outlaw team. Hold Tab to identify your teammates.";
                case Role.Renegade: return "Discover your rivals. Eliminate everyone else, leaving the Sheriff until last. You cannot rob or escape with money.";
                case Role.Swindler: return "Convince the Sheriff or a Deputy to kill you. Earn a personal win while the others finish the match. Self-inflicted damage and Outlaw kills do not count.";
                default: return "Waiting for the host to deal the roles.";
            }
        }

        public static RoleGuide Guide(Role role,int playerCount=5)
        {
            const string check="<b>C beside the Bar cash drawer</b> · Check it for two seconds as your round action. Every role can inspect it, even at full health. Hold <b>Tab</b> to read your private report.";
            const string warning="A drawer check does not prove a role. Compare reports and witness accounts. Shots damage allies too. Hit feedback never confirms allegiance.";
            switch(role)
            {
                case Role.Sheriff:return new RoleGuide(
                    "Stay alive and protect the money. Your Deputies are hidden. Everyone knows you are the Sheriff.",check,
                    "Stay alive and defeat the Outlaws and any Renegade.",warning,
                    "GUARD THE MONEY, FIND YOUR ALLIES",
                    "1. Choose Bar and lock your plan.\n2. Inspect the drawer and compare private reports with other players' claims.\n3. An escape warning during discussion tells you when to guard the Bar. Shoot the escaping player once in the head to stop the attempt.\n4. Work with your hidden Deputies. A surviving Swindler does not block a Law victory.");
                case Role.Deputy:return new RoleGuide(
                    "Help the Sheriff protect the money. Your role is hidden, even from the Sheriff. Earn their trust.",check,
                    "Share the Law's victory. The Sheriff must survive.",warning,
                    "EARN TRUST, STOP THE ESCAPE",
                    "1. Compare clues with the Sheriff.\n2. Check the Bar cash drawer with C. The same check can conceal an Outlaw's theft.\n3. When an escape is announced, choose Bar; one headshot stops the attempt.\n4. Use beer at the Bar when injured.");
                case Role.Outlaw:return new RoleGuide(
                    playerCount==4?"You are the only Outlaw. Kill the Sheriff or take the money and escape.":"Kill the Sheriff or steal and escape. Either wins for your whole team.",
                    "C checks the cash drawer. H completes one two-second theft step. Your team needs three steps in three different rounds. Plan escape with X during the next discussion.",
                    "Sheriff death wins for your team, unless only the Renegade survives. Or carry the money through an uninterrupted escape phase.",
                    "One headshot cancels your escape. Keep the money and try again next discussion. Dying returns the money to the drawer. Your escape plan is public so defenders can choose Bar. You know your Outlaw teammates.",
                    "KILL THE SHERIFF, OR STEAL AND ESCAPE",EscapeGuide);
                case Role.Renegade:return new RoleGuide(
                    "Work alone. Discover hidden rivals and be the last player alive.",check+" You have unlimited bullets.",
                    "Be the sole survivor. Leave the Sheriff until last.",
                    "Keep the Sheriff alive until everyone else is dead. An early Sheriff death wins for Outlaws. You cannot steal or escape. Hidden roles are not revealed to you.",
                    "WIN ALONE",
                    "1. Listen to claims and watch the drawer.\n2. Use C for a private drawer report.\n3. Stop escape attempts with a headshot.\n4. Defeat every other survivor. Use beer to heal.");
                case Role.Swindler:return new RoleGuide(
                    "Work alone. Make the Sheriff or a hidden Deputy believe you deserve a bullet.",check,
                    "Earn a personal win if the Law lands the lethal shot. Others continue their match and your victory is saved.",
                    "Outlaw kills, self-inflicted damage and surviving do not win. You cannot take the money or escape with it.",
                    "MAKE THE LAW KILL YOU",
                    "1. Everyone knows the Sheriff; Deputies are hidden.\n2. Bluff, make claims and draw suspicion.\n3. Convince the Law to land the lethal shot.\n4. Your personal win stays saved until the final results.");
                default:return new RoleGuide("Waiting for your role.","","","","","");
            }
        }

        public static string Hint(Snapshot view,PlayerView self)
        {
            if(self==null||!self.alive||view.phase==Phase.Lobby||view.phase==Phase.Finished)return "";
            if(view.toiletDuty)return view.phase==Phase.Discussion?"Solo toilet mission accepted. Heal safely, then return to Bar with your combat action.":"Safe solo mission: left click eight times to unclog for +20% health. Combat action kept.";
            if(view.phase==Phase.Discussion&&view.moneyBag)return view.escapePlanned?"Escape selected. Lock your Bar plan to attempt it next action phase.":"Choose Attempt escape, or press X, then lock your Bar plan.";
            if(view.phase==Phase.Discussion)
                return self.ready?"Plan locked. Unlock or change your plan to release it.":"Choose a room and lock your plan before the timer ends. Everyone can see your choice.";
            if(view.phase==Phase.Choosing)return view.moneyBag?"Choose the bar. A side room cancels your escape.":view.ownRole=="Renegade"?"Find the Outlaws first. Choose someone to meet in a room.":"Choose a room or invite someone for a private word.";
            if(view.phase==Phase.Regroup)return "Remember who you saw. Decide what you want to tell the table.";
            if(view.phase==Phase.Aftermath)return "Let the dust settle.";
            if(self.room==3)return self.seated?"Left-click · Take a drag    V · Talk    Space · Stand from bench.":"Left-click · Take a drag    V · Talk    WASD · Stroll    Space near bench · Sit.";
            if(view.duel?.stage=="blackjack")return view.duel.canHit?"Choose Hit or Stand.":view.duel.canDeal?"Your deal.":"Watch the cards.";
            if(view.duel?.stage=="result")return "Get ready. Space to stand, WASD to dodge. Weapons unlock after the result pause.";
            if(view.duel?.stage=="tiebreak")return "Tied hand: automatic high card decides the opening. Aces high; equal ranks redraw.";
            if(view.duel?.stage=="choosing")return "Choose Heads or Tails. First claim locks the sides; the coin decides the opening.";
            if(view.duel?.stage=="flipping")return "Let the coin land. Weapons are locked until the opening is decided.";
            if(view.duel?.waitingForOpening==true)return view.duel.firstName+" has the opening. You may respond after their action or pass.";
            if(view.roomTurn?.spent==true)return "Your action is spent. Watch the remaining turns.";
            if(view.roomTurn!=null&&!view.roomTurn.yourTurn)return string.IsNullOrEmpty(view.roomTurn.currentId)?"The room has finished its actions.":view.roomTurn.currentName+" has the next move. Your turn follows the room order.";
            if(view.busyRemaining>0)return "Beer heals when you finish drinking. A headshot interrupts a drawer check.";
            if(view.escapePending)return "Escape underway. Stay at Bar; one headshot stops the attempt.";
            if(self.room==0&&view.moneyBag&&!view.actionAvailable)return "Money secured. Plan escape during the next discussion.";
            if(!view.actionAvailable)return "Your action is spent. Listen, watch, and plan your next round.";
            if(view.moneyBag)return "Keep the money. Choose Attempt escape during the next discussion.";
            if(view.ownRole==nameof(Role.Swindler))return "Make the Sheriff or a hidden Deputy shoot you. Only a lethal shot from the Law earns your personal win.";
            if(view.ownRole=="Renegade")return "Eliminate the Outlaws first, then the Law. Q to draw; hold left-click for 0.5 s for accuracy, or right-click for an instant shot. Be the last alive.";
            if(self.room==0)return "C near the cash drawer · Check it privately. Outlaws use H for one theft step. E at the beer tray · Heal.";
            return "Q · Draw. Hold left-click 0.5 s · Accurate shot. Right-click · Quick shot, more spread.";
        }
    }

    [Serializable]
    public class Command
    {
        public int toiletSequence;
        public int bluffRevision;
        public string kind;
        public string name;
        public float x, z, yaw, pitch;
        public bool crouch;
        public bool shotAim;
        public bool accurateShot;
        public int shotSequence;
        public float shotTime;
        public bool shotInterpolated;
        public float shotFromTime,shotToTime;
        public int shotRound;
        public int poseVersion,planVersion,invitationVersion,planRound;
    }

    [Serializable]
    public class PlayerView
    {
        public string id, name, revealedRole,voiceId;
        public float x, y, z, yaw, pitch,bodyYaw;
        public float headX,headY,headZ,headRadius;
        public bool disconnected,rematchVote;
        public bool alive, drawn, seated, crouched, sheriff, busy,ready,observer,bot,planSelected;
        public float deathAge,deathX,deathY,deathZ;
        public int shots,health,maxHealth,carriedBeer,hits,puffs,reloads;
        public bool reloading;public float reloadRemaining,reloadDuration;
        public float smokeAge;
        public float hitAge,hitX,hitY,hitZ;public bool hitReflected;
        public bool shotResolved;
        public bool chargingShot;
        public float shotChargeAge;
        public int shotChargeSequence;
        public float shotAge,shotEndX,shotEndY,shotEndZ,shotNormalX,shotNormalY,shotNormalZ;
        public int shotSurface;
        public bool drinking, robbing, moneyBag, escaping;
        public bool checkingDrawer;public float drawerElapsed;
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
    public class MatchResultPlayer
    {
        public string id,name;
        public Role role;
        public bool alive,winner;
    }

    [Serializable]
    public class Snapshot
    {
        public float serverTime;
        public KnownRole[] knownRoles;
        public RoomInvitation[] invitations;
        public string ownId,reconnectNotice;
        public int rematchVotes,rematchRequired;
        public bool ownRematchVote;
        public PlayerView[] players;
        public MatchResultPlayer[] matchResults;
        public int[] brokenProps;
        public Phase phase;
        public float remaining;
        public string outcome, ownRole, ally, notice, voiceRoom;
        public bool personalVictory;
        public string activePlayerId, activePlayerName;
        public string[] turnOrder;
        public int round, roundLimit;
        public string coinRoomGame;
        public int medicalRoom;
        public bool medicalChoice,medicalCanHeal;
        public bool toiletOffer,toiletDuty,toiletWorking,toiletCompleted,toiletInUse;
        public int toiletPumps,toiletSequence;public float toiletPumpAge,toiletRemaining;
        public int actionsRemaining,actionsInProgress,hitSequence,confirmedHealth,confirmedMaxHealth;
        public bool confirmedKill,confirmedFriendly;
        public float confirmedHitAge;
        public int emptyClicks;public float emptyClickAge;
        public string actionBlockReason;
        public bool actionAvailable,plannedRoomFull;
        public int[] roomCapacities;
        public DuelView duel,blackjackTable,coinTable,diceTable;
        public RoomTurnView roomTurn;
        public bool hasDuel,hasRoomTurn,hasBlackjackTable,hasCoinTable,hasDiceTable;
        public string safeReport;
        public StoryProgress storyProgress;
        public string[] evidence;
        public int health, ammo, beers, carriedBeer,beerHealing;
        public bool UnlimitedAmmo=>ownRole==nameof(Role.Renegade);
        public bool pouring;public float pourElapsed,beerChoiceRemaining;
        public bool sandbox, moneyBag, escapePending,escapePlanned;
        // Shared Outlaw map state; possession and action permissions remain personal.
        public bool outlawTeamMoneyBag, outlawTeamEscapePending;
        // Recipient-only state; never included in public PlayerView records.
        public bool cigaretteBuff;
        public bool botsEnabled;
        public int botMatchSize=5,lobbyCapacity=8;
        public int safeProgress=-1;
        public string objectiveNotice,escapeWarning;
        public float busyRemaining;

        public string ToWireJson()
        {
            // JsonUtility serializes null inline classes as empty objects. Presence must
            // travel separately, or clients invent coin/turn panels that the host lacks.
            hasDiceTable=phase==Phase.Turns&&diceTable!=null;
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
            if(!snapshot.hasDiceTable||snapshot.phase!=Phase.Turns)snapshot.diceTable=null;
            if(!snapshot.hasCoinTable||snapshot.phase!=Phase.Turns)snapshot.coinTable=null;
            if(!snapshot.hasBlackjackTable||snapshot.phase!=Phase.Turns)snapshot.blackjackTable=null;
            if(!snapshot.hasDuel||snapshot.phase!=Phase.Turns)snapshot.duel=null;
            if(!snapshot.hasRoomTurn||snapshot.phase!=Phase.Turns)snapshot.roomTurn=null;
            return snapshot;
        }
    }
}
