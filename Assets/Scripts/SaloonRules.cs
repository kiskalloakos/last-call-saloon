using System;
using System.Collections.Generic;
using System.Linq;

namespace Saloon
{
    public enum Role { Unknown, Sheriff, Deputy, Outlaw, Renegade }
    public enum Phase { Lobby, Discussion, Turns, Finished, Choosing, Regroup }

    [Serializable]
    public class Tuning
    {
        [UnityEngine.Serialization.FormerlySerializedAs("openingSeconds")]
        public float discussionSeconds = 30;
        public float turnSeconds = 30;
        public float choosingSeconds = 15;
        public float regroupSeconds = 3;
        public int roundLimit = 6;
        public int health = 3;
        public int cylinder = 6;
        public int sharedBeers = 8;
        public float shotInterval = .65f;
        public float reloadSeconds = 3.5f;
        public float drinkSeconds = 2.5f;
        public float handoffSeconds = .65f;
        public float drawSeconds = .6f;
        public float robberySeconds = 3;
        public float walkSpeed = 3.5f;
        public float interactionDistance = 2.5f;
    }

    public static class SaloonRules
    {
        public static readonly Role[] Deck = { Role.Sheriff, Role.Deputy, Role.Outlaw, Role.Outlaw, Role.Renegade };

        // Elimination victory only. Criminals must actually escape with the money.
        public static string Winner(IEnumerable<Role> living)
        {
            var roles = living.ToArray();
            if (roles.Length == 0) return "DRAW — NO SURVIVORS";
            if (!roles.Contains(Role.Outlaw) && !roles.Contains(Role.Renegade)) return "LAW WINS";
            return "";
        }

        public static string Objective(Role role)
        {
            switch (role)
            {
                case Role.Sheriff: return "Keep the money from escaping through Round 6, or eliminate all criminals. Sheriff death does not end the match.";
                case Role.Deputy: return "Defend the money with the Sheriff. You can still win after the Sheriff dies.";
                case Role.Outlaw: return "Escape with the money; either Outlaw can win for both. Your fellow Outlaw is hidden, too.";
                case Role.Renegade: return "Personally escape with the money. The Outlaws are also your rivals.";
                default: return "Waiting for the host to deal the roles.";
            }
        }

        public static string Hint(Snapshot view,PlayerView self)
        {
            if(self==null||!self.alive||view.phase==Phase.Lobby||view.phase==Phase.Finished)return "";
            if(view.phase==Phase.Discussion)
                return view.ownRole=="Outlaw"?"Ask who’s staying at the bar. An empty room is an opportunity.":view.ownRole=="Renegade"?"Listen for a quiet route to the money. Your escape must be your own.":"Ask who saw the safe. A story is only as good as its witness.";
            if(view.phase==Phase.Choosing)return view.moneyBag?"Choose the bar. A side room cancels your escape.":"Choose the bar for the safe, or invite someone for a private word.";
            if(view.phase==Phase.Regroup)return "Remember who you saw. Decide what you want to tell the table.";
            if(view.busyRemaining>0)return "Keep an eye on the room. A hit interrupts what you’re doing.";
            if(!view.actionAvailable)return "Your action is spent. Listen, watch, and plan your next round.";
            if(view.escapePending)return "Hold the bar until this action phase ends. Protect the money.";
            if(view.moneyBag)return "X · Commit to escape, then survive the next action phase at the bar.";
            if(view.carriedBeer>0&&view.health<self.maxHealth)return "B · Drink to recover one health. G · Offer it to someone nearby.";
            if(self.room==0)return view.safeProgress==3?"H · Collect the money before someone else does.":view.ownRole=="Outlaw"||view.ownRole=="Renegade"?"H · Work the safe. Three completed attempts release the money.":"Watch the safe. E prepares a beer; B drinks it as your action.";
            return "One action. Draw with Q, then aim carefully—or hear them out.";
        }
    }

    [Serializable]
    public class Command
    {
        public string kind;
        public string name;
        public float x, z, yaw, pitch;
    }

    [Serializable]
    public class PlayerView
    {
        public string id, name, revealedRole;
        public float x, y, z, yaw, pitch;
        public bool alive, drawn, seated, sheriff, busy;
        public int shots,health,maxHealth,carriedBeer;
        public bool drinking, robbing, moneyBag, escaping;
        public int room, chosenRoom, poseVersion;
        public string companion;
        public int giftSequence;
        public string giftTarget;
        public float drinkRemaining,drinkDuration,giftRemaining;
    }

    [Serializable]
    public class Snapshot
    {
        public PlayerView[] players;
        public Phase phase;
        public float remaining;
        public string outcome, ownRole, ally, notice, voiceRoom;
        public string activePlayerId, activePlayerName;
        public string[] turnOrder;
        public int round, roundLimit;
        public bool actionAvailable;
        public int health, ammo, beers, carriedBeer;
        public bool sandbox, moneyBag, escapePending;
        public int safeProgress=-1;
        public string objectiveNotice;
        public float busyRemaining;
    }
}
