namespace Saloon
{
    public enum TutorialTask
    {
        Move, Plan, Action, RoleCard, Heal, QuickShot, AccurateShot, Reload,
        WatchRooms, RoomPrivacy, Coin, BlackjackPlayer, BlackjackDealer, Smoke, SmokeBeer,
        Sheriff, Deputy, SafeWork, Escape, Renegade, Spectate, Finish
    }

    public sealed class TutorialLesson
    {
        public readonly TutorialTask Task;
        public readonly string Group,Title,Objective,Details;
        public TutorialLesson(TutorialTask task,string group,string title,string objective,string details)
        {Task=task;Group=group;Title=title;Objective=objective;Details=details;}
    }

    // Game names and control names are technical terms. Keep instructions short,
    // use one instruction per sentence, and use the same name for each mechanic.
    public static class SaloonTutorialLessons
    {
        public const string ActionRule="1 ACTION = 1 ROUND";
        public const string ActionMeaning="Each player gets ONE action per round. After that action, wait for the next round. Unused actions do not carry forward.";
        public static readonly string[] Actions={
            "Shoot (left / right click): one action, even if the shot misses.",
            "Reload (R): one action.",
            "Drink beer (E): one action and one shared beer.",
            "Medical supplies (featured room winner): heal 20% instead of using your opening action.",
            "Inspect the cash drawer (C): one two-second action, available to every role.",
            "Work drawer (H): one two-second action. Three shared steps in separate rounds collect the money. Outlaw only.",
            "Plan escape (X during discussion): lock your Bar plan; the next action phase is your escape attempt.",
            "Pass (F): give up your action for this round."
        };
        public static readonly string[] FreeActions={
            "Look, aim, stand, sit, move, and crouch.",
            "Draw or holster the revolver with Q.",
            "Read your role with Tab. Talk to other players.",
            "Choose a room, send an invitation, and lock a plan during discussion.",
            "Choose Heads or Tails. Use Hit, Stand, or Draw Card in Blackjack."
        };
        public static readonly TutorialLesson[] All={
            new TutorialLesson(TutorialTask.Move,"Basics","Stand, move, and crouch",
                "Press Space to stand. Move with WASD. Hold Ctrl to crouch.",
                "You start at the Bar. Move the mouse to look around. The center of the screen is your aim point.\n\n"+
                "Press Space to stand. Use W, A, S, and D to move. Hold Ctrl to crouch. Release Ctrl to stand upright. Crouching makes movement slower.\n\n"+
                "Movement does not use your round action. After you use an action, you can still move and dodge.\n\n"+
                "During discussion, players stay at the discussion table. During room actions, you must stay in your assigned area.\n\n"+
                "This tutorial has no time limit. Tutorial opponents do not attack. Esc opens these instructions and the chapter list."),
            new TutorialLesson(TutorialTask.Plan,"Basics","Choose your room",
                "Right-click to open the planner. Select Bar. Press F to lock your plan.",
                "Each round starts with discussion. Talk about the last round. Choose where to go next. The host sets the discussion time.\n\n"+
                "Right-click to open the planner. Select Bar for this exercise. Press F, or select Lock choice. Everyone locking a plan starts room actions early.\n\n"+
                "Bar accepts up to eight players. Coin Flip and Blackjack each need two players. Have a Smoke accepts one or two players.\n\n"+
                "To request a partner, select a room and invite a player. The other player must accept. An invitation does not force them to join.\n\n"+
                "Locked plans reserve places. Changing a plan removes your Ready status. Unconfirmed or unmatched minigame plans return to Bar when discussion ends.\n\n"+
                "Everyone can see your room plan. Your role and private clues stay private. After room actions, aftermath and regroup lead to the next discussion."),
            new TutorialLesson(TutorialTask.Action,"Basics","One action per round",
                "Press F during room actions to pass. This uses your action.",
                ActionMeaning+"\n\nThe action list below applies to normal matches. Rejected requests do not use an action.\n\n"+
                "At the Bar, players can act at the same time. There is no queue. In a minigame room, the winner gets the opening.\n\n"+
                "The opening is the first chance to act. The other player can respond after that action or pass.\n\n"+
                "A timed action must finish. A beer does not heal you when you press E. Reloading does not fill the cylinder immediately.\n\n"+
                "Use F to pass in this exercise. During discussion, F locks your plan instead.\n\n"+
                "Have a Smoke removes your indoor action opportunity for that round. Smoking does not give an extra shot or action."),
            new TutorialLesson(TutorialTask.RoleCard,"Basics","Read your private information",
                "Hold Tab to read your role and private information.",
                "A role is your team and objective. Roles include Sheriff, Deputy, Outlaw, Renegade, and Swindler.\n\n"+
                "Hold Tab to open your private role card. Read your objective, known roles, story map, and private clues there. Release Tab to return.\n\n"+
                "Everyone knows the Sheriff. Deputies are hidden even from the Sheriff. Outlaws know their teammates. Independent players discover hidden roles through play.\n\n"+
                "A four-player match has one Sheriff, one Deputy, one Outlaw, and one independent. Each match secretly rolls Renegade or Swindler with equal chance; the Swindler wins personally if killed by the Law. Larger matches add Deputies and Outlaws.\n\n"+
                "Names above players do not reveal every role. You can tell the truth or lie during discussion. Your private information does not automatically inform teammates."),
            new TutorialLesson(TutorialTask.Heal,"Combat","Find beer and heal",
                "Stand with Space. Find the shared beer tray at the Bar. Press E near the tray.",
                $"Health starts at {SaloonHealth.Maximum}%. At 0%, you are eliminated. Beer restores {SaloonHealth.BeerHealing}% health, up to 100%.\n\n"+
                "You start this exercise at 60% health. The gold marker points to the shared beer tray on the counter.\n\n"+
                "Select Bar during discussion in a normal match. During room actions, stand with Space. Walk to the beer tray. Press E near it.\n\n"+
                "Drinking uses your action and removes one beer from the shared supply. Wait for the drinking animation to finish. Healing takes about 2.5 seconds.\n\n"+
                "You cannot heal from a minigame room or the smoking deck. Beer cannot revive an eliminated player. Plan your visit before your health becomes critical."),
            new TutorialLesson(TutorialTask.QuickShot,"Combat","Fire a quick shot",
                "Press Q to draw. Aim at the training opponent. Right-click once.",
                "Q draws or holsters your revolver. Drawing does not use your action. A short draw animation must finish before firing.\n\n"+
                "Right-click fires a quick shot immediately. A quick shot has more spread. Moving makes that spread larger.\n\n"+
                "Only a headshot causes damage. Body hits and misses cause no damage. They still use one bullet and your action.\n\n"+
                $"A normal headshot removes {SaloonHealth.RevolverDamage}% health. Both shot types cause the same damage.\n\n"+
                "You can only shoot during room actions, when your action is available. You can only damage players in your room.\n\n"+
                "A wall or window blocks the shot. You cannot shoot into another room from the Bar. Fire one quick shot for this exercise."),
            new TutorialLesson(TutorialTask.AccurateShot,"Combat","Aim for a headshot",
                "Press Q to draw. Aim at the opponent's head. Hold left-click for 0.5 seconds.",
                "Hold left-click for 0.5 seconds to fire an accurate shot. The shot fires when the hold completes.\n\n"+
                "Release left-click early to cancel. A canceled hold does not use ammunition or your action. A fired shot does.\n\n"+
                "Keep the center of the screen on the head. This exercise requires a headshot. A missed attempt gives you another training action.\n\n"+
                "Standing still helps you aim. An opponent can move or crouch while you aim. A quick shot fires sooner, with more spread.\n\n"+
                "Headshots damage allies and enemies equally. Shooting does not reveal their allegiance.\n\n"+
                "A smoking boost can increase target damage. The shooter takes no reflected damage."),
            new TutorialLesson(TutorialTask.Reload,"Combat","Reload the revolver",
                "Press Q to draw. Press R. Wait for the reload to finish.",
                "Most roles have a six-bullet cylinder. Ammunition does not refill at the next round.\n\n"+
                "You start this exercise with two bullets. Press Q to draw the revolver. Press R to reload it.\n\n"+
                "Reloading uses your action. The cylinder fills when the reload finishes, after about 3.5 seconds. You cannot shoot during that reload.\n\n"+
                "You can still be attacked while reloading. Reload before you run out when the situation permits.\n\n"+
                "A full cylinder does not need a reload. The Renegade has unlimited bullets and does not need to reload."),
            new TutorialLesson(TutorialTask.WatchRooms,"Rooms","Check room privacy from the Bar",
                "Stand with Space. Move toward the painted room wall.",
                "Coin Flip and Blackjack are enclosed. Solid walls block sight in both directions. You cannot watch players, cards, or the coin from the Bar.\n\n"+
                "Nearby voices across indoor room walls sound quieter and muffled in either direction. Voices within the same room stay clear.\n\n"+
                "Room assignments are public during planning, but what players do inside stays out of sight. Ask what happened when everyone returns to the table.\n\n"+
                "The cash drawer and beer tray remain at the Bar. Moving toward the marked wall completes this privacy exercise."),
            new TutorialLesson(TutorialTask.RoomPrivacy,"Rooms","Check privacy from inside a room",
                "Press Space to stand. Move toward the front wall inside Coin Flip.",
                "You are now inside Coin Flip. Neither you nor players outside can see through the wall.\n\n"+
                "The marker shows the wall. Stand after the minigame has finished. You cannot walk or shoot through it. Stay in this room until regroup.\n\n"+
                "Nearby voices from other indoor rooms are quieter and muffled. Your voice sounds muffled to those listeners too. Your room partner hears you clearly.\n\n"+
                "The smoking deck remains completely isolated from indoor sound. The next exercises show how minigames decide who acts first."),
            new TutorialLesson(TutorialTask.Coin,"Rooms","Play Coin Flip",
                "Select Heads or Tails. Wait for the result. Press F when your action becomes available.",
                "Two players enter Coin Flip together. The first accepted Heads or Tails choice assigns both sides.\n\n"+
                "Select Heads or Tails with the mouse. The coin toss decides the winner. Wait for the toss and result pause.\n\n"+
                "The winner gets the opening. They have ten seconds to use an action or pass in a normal match.\n\n"+
                "The opponent can respond after that action or pass. If the opening expires, it becomes a pass. Each player still gets only one action.\n\n"+
                "Choosing a side does not use your action. Losing the toss causes no damage by itself. It only changes the opening.\n\n"+
                "After round one, medical supplies may appear in a room marked by a glowing medical icon. Its winner can heal 20% as their action or keep the normal opening. Supplies use no beer or smoking boost. The opponent can respond after healing.\n\n"+
                "Weapons and standing stay locked until the minigame result. This tutorial opponent passes its opening. Press F when your action becomes available."),
            new TutorialLesson(TutorialTask.BlackjackPlayer,"Rooms","Play Blackjack: player",
                "Use Hit or Stand. Wait for the hand result. Press F when your action becomes available.",
                "Blackjack has one player and one dealer. Try to finish closer to 21 than the dealer without going over 21.\n\n"+
                "Cards 2 through 10 use their number. J, Q, and K count as 10. An ace counts as 11 or 1.\n\n"+
                "Use Hit to take another card. Use Stand to keep your total. A total over 21 is a bust and loses.\n\n"+
                "The dealer's second card stays hidden during your choices. You cannot use that hidden card to decide.\n\n"+
                "The dealer draws below 17 and stops at 17 or more. A tied hand draws an automatic high card for each seat. Aces rank highest; equal ranks redraw only those cards.\n\n"+
                "The hand winner gets the opening after the result pause. The opponent can respond after the winner acts or passes.\n\n"+
                "When Blackjack has this round's medical supplies, its winner may heal 20% instead of using their opening action. The choice expires with the normal opening timer. Full-health winners keep their opening.\n\n"+
                "There are no bets, splits, insurance, or double-down choices. Cards do not use your action. Press F after the game when your action is available."),
            new TutorialLesson(TutorialTask.BlackjackDealer,"Rooms","Play Blackjack: dealer",
                "Wait for the player's choice. Use Draw Card when available. Pass after the result.",
                "You are the dealer for this exercise. The other player chooses Hit or Stand first.\n\n"+
                "Wait until Draw Card appears. Select Draw Card while your total is below 17. The game stops your draws at 17 or more.\n\n"+
                "A total over 21 is a bust. The other player wins if you bust. A tie uses automatic high cards, with aces highest; it never restarts the hand.\n\n"+
                "Normal matches also advance a delayed dealer draw automatically. You do not get to keep drawing above 17.\n\n"+
                "Wait for the result pause. Use your opening action or wait for the opponent's opening. Press F to pass in this exercise.\n\n"+
                "In both minigames, seated players must finish the game before standing. After the result, Space and WASD let you stand and dodge."),
            new TutorialLesson(TutorialTask.Smoke,"Rooms","Have a Smoke",
                "Left-click to take a drag on the smoking deck.",
                "Have a Smoke takes you out the back. Select it during room voting; there is no walking route from the Bar. You can go alone or with one accepted partner.\n\n"+
                "The deck has separate voice and visibility during room actions. Indoor players cannot hear the smokers. Smokers cannot hear indoor players.\n\n"+
                "You cannot shoot, heal, inspect the drawer, or take money outside. You also cannot watch indoor events.\n\n"+
                "Use left-click to take a drag. Use WASD to walk on the deck. Weapons stay unavailable. Everyone returns inside at regroup.\n\n"+
                "A visit gives one boost. The boost comes from entering the deck, not from each drag. More drags do not add boosts.\n\n"+
                "Use this room for a private conversation or a later boost. Avoid it when you need to check the money or heal now.\n\n"+
                "An Outlaw carrying money must consider the escape plan. Choosing a side room or the deck cancels a pending escape."),
            new TutorialLesson(TutorialTask.SmokeBeer,"Rooms","Use the smoking boost",
                "Find the beer tray. Press E near it. Wait for the 40% heal.",
                "The smoking boost affects your next fired shot OR your next healing beer. It does not affect both.\n\n"+
                "A boosted headshot causes 40% damage instead of 20%. Firing consumes the boost even if the shot misses.\n\n"+
                "A boosted beer restores up to 40% health instead of 20%. Drinking consumes the boost. Health still stops at 100%.\n\n"+
                "The boost stays across rounds until used. It does not stack. Elimination or a new match removes it.\n\n"+
                "For this exercise, you start back at the Bar with a boost and 40% health. Drink a beer to reach 80%.\n\n"+
                "Smoking itself does not heal you. You must return to the Bar and use a later round action to drink."),
            new TutorialLesson(TutorialTask.Sheriff,"Roles","Try the Sheriff",
                "Stand. Find the cash drawer beside the beer tray. Press C nearby.",
                "The Sheriff and Deputies form the Law team. Everyone can identify the Sheriff. Deputies must earn the Sheriff's trust.\n\n"+
                "Stay alive and protect the money. Stop the Outlaws from escaping with it. Eliminate all Outlaws and any Renegade to win early.\n\n"+
                "Use C by the cash drawer to inspect it. The two-second check uses your action. Every role can do this, even at full health.\n\n"+
                "Your private report says whether money is present and whether the drawer changed since your last inspection. It never names a thief. Hold Tab to read it.\n\n"+
                "Compare reports with earlier checks. Ask who visited the counter and what they saw. Share useful evidence with your Deputies.\n\n"+
                "Shooting a Deputy damages your ally without revealing their role. Sheriff death gives Outlaws the win unless the Renegade is the sole survivor."),
            new TutorialLesson(TutorialTask.Deputy,"Roles","Try the Deputy",
                "As the Deputy, press C beside the Bar cash drawer.",
                "A Deputy shares the Law objective. Protect the money and help the Sheriff stop the escape.\n\n"+
                "Everyone knows the Sheriff. Nobody automatically knows your Deputy role. Earn the Sheriff's trust.\n\n"+
                "Use C by the cash drawer for a private two-second inspection. It uses your action. Hold Tab to read your report.\n\n"+
                "Inspection does not automatically send the report to the Sheriff. Tell the Sheriff what you learned.\n\n"+
                "You cannot take the money or escape with it. The Sheriff and all other Deputies are your allies.\n\n"+
                "A headshot can damage a Law ally. Do not treat an unproven accusation as confirmed evidence.\n\n"+
                "Keep the Sheriff alive. Sheriff death gives Outlaws the win unless the Renegade is the sole survivor."),
            new TutorialLesson(TutorialTask.SafeWork,"Roles","Try the Outlaw: steal",
                "Press H beside the cash drawer. Stay nearby until the two-second check finishes.",
                "The Outlaw team wins when the Sheriff dies, unless the Renegade is the sole survivor, or when an Outlaw escapes with the money.\n\n"+
                "Outlaws know their Outlaw teammates. A four-player match has only one Outlaw. The Renegade is your enemy.\n\n"+
                "Choose Bar. Stand with Space. Walk to the cash drawer beside the beer tray. Press H during room actions.\n\n"+
                "The H check uses one action and takes two seconds. C inspection has the same visible duration, so witnesses cannot tell which choice you made.\n\n"+
                "Complete three shared drawer steps across three rounds. The third takes the money. Only one step can succeed per round, even with several Outlaws. Moving away or taking a headshot interrupts the check, but your action stays spent.\n\n"+
                "A witness may learn that you checked the drawer, but not what you did inside. This exercise advances after a successful theft; a normal match follows its round timer."),
            new TutorialLesson(TutorialTask.Escape,"Roles","Try the Outlaw: escape",
                "During discussion, press X to plan your escape. Lock your Bar plan. Survive the next action phase.",
                "You start this exercise with the money from a previous round.\n\n"+
                "During discussion, use X or select Attempt escape, then lock your Bar plan. Planning is free; the escape starts at the beginning of the next room-action phase. You still have one normal action to shoot, heal, reload or pass.\n\n"+
                "Everyone gets a fresh response window. A public discussion warning names the escaping player and gives everyone ten seconds to reconsider rooms. One headshot interrupts the attempt; you keep the money and can try again during a later discussion.\n\n"+
                "Choosing a side room cancels the plan. Elimination returns the money to the drawer.\n\n"+
                "If an Outlaw escapes, the Outlaw team wins, including eliminated teammates.\n\n"+
                "This exercise starts the escape in the next action phase. Stay at Bar. Once all Bar actions finish without a headshot interrupting you, the victory screen appears."),
            new TutorialLesson(TutorialTask.Renegade,"Roles","Try the Renegade",
                "Hold Tab to review your objective. Shoot the marked Outlaw in the head.",
                "The Renegade works alone. Hold Tab to review your objective. Other hidden roles are not revealed. You have unlimited bullets.\n\n"+
                "Eliminate the Outlaws first, then the Deputies and any other survivors. Leave the Sheriff until last. Be the only player alive to win.\n\n"+
                "If the Sheriff dies while anyone else survives with you, the Outlaws win. Your final Sheriff kill wins only when it leaves you alone.\n\n"+
                "You cannot take the money or escape with it. An Outlaw escape is an Outlaw win.\n\n"+
                "Unlimited bullets do not give unlimited actions. One fired shot still uses your only action for that round.\n\n"+
                "This training Outlaw has 20% health. Read the role card first. Use either shot type to land the headshot."),
            new TutorialLesson(TutorialTask.Spectate,"Finish","Elimination and voice",
                "Wait for spectator view. Press Left or Right to change the player you watch.",
                "At 0% health, you are eliminated. Your body falls and stays in the room. Beer cannot revive you.\n\n"+
                "After the short death view, you watch a living player's first-person view. Use Left or Right to change the player.\n\n"+
                "You hear game sounds and nearby voices from the watched player's position. Your microphone goes to eliminated-player chat.\n\n"+
                "Living players cannot hear your eliminated-player chat. Watching a player does not give their hidden role to you.\n\n"+
                "In a normal match, hold V to talk. T enables or disables open mic. M mutes your microphone.\n\n"+
                "Esc opens voice devices and local player mute controls. Match menus pause your microphone. Lobby and results voice remain active.\n\n"+
                "Online voice needs internet, including in a LAN match. This offline tutorial does not connect voice or use your microphone."),
            new TutorialLesson(TutorialTask.Finish,"Finish","Ready for a match",
                "Review any chapter, or select Finish tutorial.",
                "Choose Solo practice to train with bots. Set the bot count, discussion time, action time, and shared beer supply before starting.\n\n"+
                "Practice bots know private roles. Use them to learn controls. Human players can lie, negotiate, and behave differently.\n\n"+
                "For friends on one network, use Local network. The host shares their local address and port. Other players select Join LAN.\n\n"+
                "For internet play, use Online room. The host creates a private room and shares its code. Use the same build on every device.\n\n"+
                "Matches need four to eight players. The host can fill empty places with bots. Bots join when the host starts.\n\n"+
                "The results show all roles and the winning team. Players can request a rematch. The host can return to the lobby.\n\n"+
                "Remember: one action per player, per round. Guard your health. Keep track of the cash drawer and the money.\n\n"+
                "Open Tutorial on the main menu at any time. Use the chapter list to repeat any exercise. Tutorial changes do not change your practice settings.")
        };
    }
}
