using System;
using System.Collections.Generic;
using System.Linq;

namespace Saloon
{
    public enum Role { Unknown, Sheriff, Deputy, Outlaw, Renegade }
    public enum Phase { Lobby, Introductions, Live, Finished }

    [Serializable]
    public class Tuning
    {
        public float openingSeconds = 30;
        public float matchSeconds = 480;
        public int health = 3;
        public int cylinder = 6;
        public int sharedBeers = 8;
        public float shotInterval = .65f;
        public float reloadSeconds = 3.5f;
        public float drinkSeconds = 2.5f;
        public float drawSeconds = .6f;
        public float walkSpeed = 3.5f;
        public float interactionDistance = 2.5f;
    }

    public static class SaloonRules
    {
        public static readonly Role[] Deck = { Role.Sheriff, Role.Deputy, Role.Outlaw, Role.Outlaw, Role.Renegade };

        // Called after EACH death, with Renegade precedence for the last shot.
        public static string Winner(IEnumerable<Role> living)
        {
            var roles = living.ToArray();
            if (roles.Length == 1 && roles[0] == Role.Renegade) return "RENEGADE WINS";
            if (!roles.Contains(Role.Sheriff)) return "OUTLAWS WIN";
            if (!roles.Contains(Role.Outlaw) && !roles.Contains(Role.Renegade)) return "LAW WINS";
            return "";
        }

        public static string Objective(Role role)
        {
            switch (role)
            {
                case Role.Sheriff: return "Eliminate both Outlaws and the Renegade. Your Deputy is hidden.";
                case Role.Deputy: return "Protect the Sheriff. Eliminate both Outlaws and the Renegade.";
                case Role.Outlaw: return "Kill the Sheriff. Your fellow Outlaw is identified below.";
                case Role.Renegade: return "Be the last survivor. Kill the Sheriff LAST.";
                default: return "Waiting for the host to deal the roles.";
            }
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
        public int shots;
    }

    [Serializable]
    public class Snapshot
    {
        public PlayerView[] players;
        public Phase phase;
        public float remaining;
        public string outcome, ownRole, ally, notice, voiceRoom;
        public int health, ammo, beers, carriedBeer;
        public bool sandbox;
        public float busyRemaining;
    }
}
