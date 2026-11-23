# Last Call 0.3.0 — Bluff, Break & Escape

Replayability roadmap and the implemented cash drawer theft: [Replayability reference](Documentation/REPLAYABILITY-README.md).

0.3.0 brings hidden Deputies, a secret Renegade-or-Swindler slot, spoken Bluff Dice, cash-drawer theft, improved escapes, medical opportunities and breakable glass.

For role instructions, refer to [Your role, in simple words](#your-role-in-simple-words).

Test release **0.3.0** is available for Windows x64 and macOS (Intel and Apple Silicon).
Use **0.3.0** on every computer in the match.

1. Download [Windows x64](https://github.com/kiskalloakos/last-call-saloon/releases/download/v0.3.0/LastCall-Windows-0.3.0.zip) or [Mac (Intel and Apple Silicon)](https://github.com/kiskalloakos/last-call-saloon/releases/download/v0.3.0/LastCall-macOS-0.3.0.zip).
2. Extract the complete archive.
3. On Windows, open `Last Call.exe`. Keep its data folder beside it.
4. On macOS, open `Last Call.app`.

The major update adds player-controlled Bluff Dice throws, three-round drawer theft, an action for the escapee, immediate escape victories, shattering windows and bottles, optional solo healing missions, a playable walkthrough and configurable practice. See the [full 0.3.0 changelog](Documentation/RELEASE-0.3.0.md).

The Mac app is not notarized.
If macOS requests approval, use the system's Open option.

This release adds independent head movement, synchronized speech animation, visible room windows, persistent bodies while spectating, a right-click discussion planner with invitations, cleaner HUDs, and slower blackjack and coin-flip pacing. It also includes crouching, updated weapon movement, and the sunset overlook.

This release includes these features:

- Discussion and planning with a Ready control.
- Matches with four to eight players, with optional bots.
- One Western character model for all players.
- Combat with damage from headshots only.
- Movement inside rooms, including bots that stand and dodge.
- Beer that restores health after non-lethal hits.
- Ragdoll deaths and spectator views.
- Updated Tab role instructions, smoother revolver movement and a rebuilt bar.

Room and companion selections update immediately.
The planner keeps your latest selections while it waits for the host.
A delayed companion selection does not send an earlier room selection again.

After a disconnect, the main menu shows **COPY DISCONNECT DETAILS**.
This control copies the transport reason and build ID.
The reported Windows return to the menu has not been reproduced.
A correction for that problem is not confirmed.
This test release includes planner corrections and more disconnect information.

## Four devices on LAN

1. Connect all four computers to the same local network. Do not use guest Wi-Fi that isolates devices.
2. Start **0.3.0 on every computer**.
3. Select **Local network** on each computer.
4. On the host computer, turn **Fill empty seats with bots** off.
5. Keep port **7777**.
6. Click **HOST LAN**.
7. On each other computer, enter the host computer's local IPv4 address, usually `192.168.x.x`.
8. Set port **7777** on each other computer.
9. Click **JOIN LAN** on each other computer. Do not use `127.0.0.1` to connect to another computer.
10. If Windows Firewall requests access, permit Last Call on the private or home network.
11. If the macOS firewall requests access, permit incoming connections for Last Call.
12. When the lobby shows four people, start the match from the host computer.
13. Select a plan on each computer.
14. Click **Ready** on each computer.

Four people can start without a fifth player or a bot.
Room actions start when all four players are ready.
The development-source roles are Sheriff, Deputy, one Outlaw and one independent (Renegade or Swindler).

Game traffic uses UDP.
Vivox voice also requires an internet connection.
The four-device Wi-Fi and firewall configuration still requires tests on the actual devices.
Automated LAN checks do not verify the router or each device's firewall.

All players use the **Gunman** from [Low Poly Western Starter Pack by tt-3D](https://assetstore.unity.com/packages/3d/characters/low-poly-western-starter-pack-383104).
The model uses the original textures.
Its movements include walking, sitting with bent legs, revolver aim, recoil and drinking.
The mug moves to the mouth during a drink.
The transition between standing and sitting does not compress the character.
Ragdoll deaths remain available.

Since 0.2.14, mouse look moves the head independently.
Move the mouse up/down to nod yes, or left/right to shake no while standing or seated.
The body stays facing forward for head turns within 65 degrees; it follows larger turns and walking.

During voice communication, the mouth and lower face follow fresh voice energy.
Zero energy, microphone mute, drinking or death closes the mouth immediately.
Missing voice-energy updates expire after 0.3 seconds.
This cartoon effect does not give accurate lip synchronization.

The pack supplies the rigged model.
The game supplies the movements.
The content uses Unity's Standard Asset Store EULA. The previous character used the CC0 license.

Discussion, room planning and companion invitations use one panel.
Select your plan.
Click **Lock choice** or press **F**. For a solo smoke, select **Have a Smoke** and click **Go alone**.
Discussion has a 30-second countdown. Everyone locking in starts room actions early.
Unconfirmed plans return to the Bar when time runs out.
A plan change clears that player's Ready status.

Mouse look stays active during discussion. Right-click to open or close the room planner; F readies your plan.
Practice players set their own Ready status.
They use the bar and side rooms, change plans each round and accept invitations unless they carry the money.
Headshots damage allies and enemies equally. The shooter takes no reflected damage, and hit feedback does not reveal allegiance.
The Sheriff and all Deputies are Law allies. All Outlaws are teammates.

The upper-left display always identifies the Sheriff.
It shows “YOU” for the local Sheriff and an eliminated status after the Sheriff's death.
Hold **Tab** to read your private role, including when the planning panel is open.
Deputies are hidden from the Sheriff. Neither Law players nor independents receive other players' hidden roles.
In five-player matches, Outlaws see their teammate's name on the Tab card and an Outlaw label above them. Four-player matches have only one Outlaw.

Hold Tab for a private illustrated story map for your role. Curved trails connect
circular milestones. A checkmark shows completed steps. Gold marks the next
steps. The panel below the map explains what to do next. Use **Back** and **Next**
in the private information panel to read more clues. Progress survives reconnects and resets
for a rematch.

Each role has its own map:

- Sheriff and Deputy: protect the money and help each other.
- Outlaw: use H and X to steal the money and escape.
- Renegade: fight the Outlaws first.

Eliminated players see a living player's first-person view.
Use the **Left/Right arrows** or spectator buttons to change the view.
Hidden roles stay private.
Eliminated players use a separate voice channel.

The first-person revolver has no visible hand.
Its muzzle rises with recoil, rolls back smoothly and gradually returns to rest.
The third-person arm holds the revolver with a straight wrist and a closed hand around the handle.
The fingers keep their original joint rotations to prevent a stretched hand shape.

A headshot moves the target's torso and head away from the hit.
The character briefly flashes warm red, then returns to position.
A lethal shot starts the ragdoll movement.
When a room has two players, they initially face each other, including at the bar.

Select **No bots** or **Bots** when you host a LAN match.
In the lobby, the host can change this setting and select any fill target from **4 to 8 players**.
Bots fill empty places only when the host starts the match.
With **No bots**, any group of four to eight humans can start immediately.
Every human in the lobby joins the match.

| Match size | Roles |
|---|---|
| 4 | 1 Sheriff, 1 Deputy, 1 Outlaw, 1 independent |
| 5 | 1 Sheriff, 1 Deputy, 2 Outlaws, 1 independent |
| 6 | 1 Sheriff, 2 Deputies, 2 Outlaws, 1 independent |
| 7 | 1 Sheriff, 2 Deputies, 3 Outlaws, 1 independent |
| 8 | 1 Sheriff, 3 Deputies, 3 Outlaws, 1 independent |

The lobby holds **up to 8 human players**, including the host.
Both **online rooms and LAN matches support 4–8 players**, including the host.
The Bar has eight seats and a shared tray of ready-filled beers; the discussion table has eight separate places.
Coin Flip, Blackjack and Have a Smoke each hold two players.
The lobby rejects a ninth connection and keeps the existing players connected.
Friends can replace bots after a match ends.
Bot settings cannot change during a match.
Players must use the same build and protocol; earlier five-player builds cannot join these matches.

Development builds can verify three complete rounds with independent processes:

```sh
python3 Tools/lan_match_test.py --app /absolute/path/to/game --players 8 --logs /tmp/lastcall-lan-8
python3 Tools/lan_match_test.py --app /absolute/path/to/game --players 8 --online --logs /tmp/lastcall-online-8
```

Use `--players 4`, `5`, `6`, `7` or `8`. Online verification creates a private Relay room and requires internet.
The new role distributions still need human balance playtests.

## Your role, in simple words

In the development source, each match starts with the existing personal role reveal,
followed by a nine-second **Your matchup** screen using the results-screen portraits.
It shows the actual match's role counts, your allies and enemies, and which identities
you need to discover. These are anonymous role slots with role names above the
portraits: there are no player names, and the order does not match the player roster.
The first discussion gets its full time after both opening screens.

Hold **Tab** to see your role and objective.
Tab also works when Discuss & Plan is open.
The upper-left corner always identifies the Sheriff.
Everyone knows the Sheriff. Deputies are hidden, even from the Sheriff. Outlaws know each other; independents receive no hidden role identities. Each match rolls one independent: 50% Renegade, 50% Swindler, kept secret from other players.

At the start of each round, select a room.
If necessary, select a companion for that room.
Click **Lock choice** (**Go alone** for Have a Smoke) or press **F**.
Room actions start when the countdown ends, or early when every living player has locked a plan.
Wait for your turn.
You have **one action per round**, such as a shot, a drawer check, an inspection or a drink.

### Sheriff — protect the money

You and the **Deputy** are a team.
Everyone knows that you are the Sheriff.
The game does not automatically identify the Deputy to you.
HP means health.

1. Select **Bar** to watch the cash drawer beside the shared beer tray.
2. Press **Space** to stand and use **WASD** to walk to the customer-facing side of the counter. Press **C** beside the drawer on your turn to inspect.
3. Read your private report.

The private report says whether the money is present and whether the drawer changed since your last check.
It can also report tampering while the money is still present. It does not identify a thief. Any role can inspect, even at full health.

To shoot a suspected enemy:

1. Press **Q** to draw your gun.
2. Wait for the gun to rise.
3. Aim at the enemy's head with the mouse.
4. Hold the left mouse button for 0.5 seconds for an accurate shot, or right-click for an instant shot with more spread.

You and the Deputy win if you eliminate every Outlaw and the Renegade.
**H and X do not work for you**.
Only Outlaws can rob, collect money or escape with it.
Headshots damage allies and enemies equally. The shooter takes no reflected damage, and hit feedback does not reveal allegiance.

### Deputy — help the Sheriff

You and the **Sheriff** are a team.
Your role is secret.
The game does not automatically identify you to the Sheriff.

- Protect the money.
- Select **Bar** to stay near the cash drawer.
- For a private conversation, invite someone to a side room.
- On your turn, press **Q** to draw your gun.
- Aim at an enemy's head.
- Hold the left mouse button for 0.5 seconds for an accurate shot, or right-click for an instant shot with more spread.

**H and X do not work for you**.
You cannot rob or escape with the money.
**C beside the cash drawer inspects it**, just like the Sheriff. Walk up to it during room actions; inspection uses your action and records a private report under **Tab**.
You share the Sheriff's victory conditions. Keep the Sheriff alive; Sheriff death awards Outlaws unless the Renegade is the sole survivor.
Headshots damage allies and enemies equally. The shooter takes no reflected damage, and hit feedback does not reveal allegiance.

### Outlaw — steal the money and escape

In a **4-player match**, you are the **only Outlaw**.
Kill the Sheriff or escape with the money to win for your team.
In a **5-player match**, there are **two Outlaws**.
Six-player matches also have two Outlaws; seven- and eight-player matches have three.
If any Outlaw escapes with the money, **all Outlaws win**.
Hold Tab to identify your Outlaw teammates.

1. Select **Bar**.
2. Press **Space** to stand and use **WASD** to walk to the cash drawer beside the beer tray.
3. Press **H** on your turn and stay nearby for two seconds. Each successful check completes one shared step. Three steps across three rounds collect the money.
4. During the next discussion, press **X** or select **Attempt escape**, then lock your Bar plan.
5. Your escape plan is public during discussion: everyone gets at least ten seconds to reconsider rooms. Survive the next room-action phase; one headshot interrupts it.

To defend yourself:

1. Press **Q** to draw your gun.
2. Aim at an enemy's head.
3. Hold the left mouse button for 0.5 seconds for an accurate shot, or right-click for an instant shot with more spread.

Headshots damage allies and enemies equally. The shooter takes no reflected damage, and hit feedback does not reveal allegiance.
Sheriff death gives your team victory, regardless of who killed them, unless the Renegade is the sole survivor. Escaping with the money is your other victory route.

### Renegade — eliminate the Outlaws, then the Law

Other hidden roles are not revealed to you. Listen to claims, observe actions, and hold **Tab** to review your own objective.

You have **no ally**.
Your objective is to be the **last player alive**.
The Sheriff and Deputy together are the Law.

1. Fight the **Outlaws first**.
2. Keep the Law alive while they help fight the Outlaws.
3. After the Outlaws die, eliminate the Deputies and any other survivors.
4. Kill the Sheriff last. An earlier Sheriff death gives Outlaws the win.

To shoot:

1. Select a room with a suspected Outlaw.
2. On your turn, press **Q** to draw your gun.
3. Aim at the enemy's head.
4. Hold the left mouse button for 0.5 seconds for an accurate shot, or right-click for an instant shot with more spread.
5. You have unlimited bullets. You can shoot once per round without reloading.

At the Bar, walk to the shared beer tray and press **E** to take and drink a ready beer. It heals 20%, or 40% with your cigarette buff, and uses one action.
**H and X do not work for you**.
You cannot rob, collect money or escape with it.
You win when everyone else is dead.

### Swindler — make the Law kill you

One independent slot is randomly Renegade or Swindler, with equal chance on each
new match and rematch. Only the recipient learns which independent was dealt;
the anonymous opening shows everyone else an **Independent: Renegade or Swindler** slot.

The Swindler has no allies and earns a personal victory if a Sheriff or Deputy
lands the lethal shot. Nonlethal hits, self-inflicted damage, reflected damage,
Outlaw kills, and simply surviving do not count. The win is saved privately;
the match continues for the other players. The Swindler's results card still
shows Winner alongside any faction winners. Law can win by eliminating the
Outlaws without killing a surviving Swindler. The Swindler has normal ammunition,
cannot rob or escape. Practice bots do
not simulate this role's social bluffing.

### H and X: how to take the money and escape

These controls work for **Outlaws only**, at the **Bar** during room actions.
The Sheriff, Deputy, Renegade and Swindler cannot rob, collect money or escape with it.

Only Outlaws can take the money or escape with it. Other roles can press **C** to inspect the same drawer, including when at full health.

1. Walk to the drawer beside the beer tray and press **H** during room actions.
2. Stay nearby for the two-second check. It uses your action; walking away or taking a headshot interrupts it.
3. Three successful checks in three different rounds give the third worker the money. Progress is shared by all Outlaws, with at most one step per round. An empty-drawer attempt still uses your action.
4. During the next discussion, choose **Attempt escape** or press **X**, then lock a **Bar** plan. This publicly identifies your escape attempt before room assignments, with a red Bar highlight and at least ten seconds for everyone to reconsider their plans.
5. The escape starts with the next action phase. You and every defender keep one normal action to shoot, heal, reload or pass. Once all Bar actions finish without a headshot stopping you, the game goes straight to the Outlaw victory screen.
6. Survive at the Bar until actions end. A headshot interrupts the attempt; you keep the money and can try again in a later discussion.

Both C inspection and H theft have the same visible two-second check. A witness can record who checked the drawer, but cannot see what they did inside. Inspection reports privately show whether the money is present and whether the drawer changed since that player's last check.

In bot matches, an available Law or Renegade bot responds to the public escape warning by guarding Bar. A bot explicitly invited by a human keeps that invitation, so diversion remains possible. Cancelling and redeclaring an escape cannot repeatedly extend discussion.

A side-room plan cancels an escape. If the carrier dies, the money returns to the drawer and your team must complete all three drawer steps again. A completed escape gives the **Outlaw team victory**, including eliminated teammates.

### Useful buttons for every role

| Button | Function |
|---|---|
| Hold **Tab** | Shows your private role and objective. |
| **Hold Control** | Crouch while standing; either Control key works. Release to stand when there is room overhead. Crouching slows movement and lowers your camera and hitboxes. |
| **Space**, then **WASD** | Space makes you stand. WASD moves you during room actions. Space near an empty seat makes you sit. |
| **Q**, then **hold left-click** or **right-click** | Q draws your gun. Hold left-click for 0.5 seconds for an accurate shot; right-click fires instantly with more spread. A miss also uses your action. |
| **R** | Reloads your drawn gun. Uses your action. |
| **E** near the shared beer tray | Takes and drinks a ready beer. Uses one beer and one action; heals 20%, or 40% with your cigarette buff. |
| **F** during Discuss & Plan | Sets or clears Ready. |
| **F** during your action turn | Passes and gives up your action for this round. |
| Hold **V** | Sends microphone audio in push-to-talk mode. |
| **T** | Toggles open mic: talk hands-free until switched off. |
| **M** | Mutes or unmutes the microphone in either mode. |
| **Right-click** during Discuss & Plan | Opens/closes the planner. Mouse look is active when it is closed. |

Everyone starts with **100% health**.
An unbuffed **headshot** removes **20 percentage points** from the target. Five unbuffed headshots eliminate a player at 100%; a cigarette-buffed headshot removes 40 points.
Headshots damage allies and enemies equally. The shooter takes no reflected damage, and hit feedback does not reveal allegiance.
The Sheriff and all Deputies are Law allies. All Outlaws are teammates.

Multiplayer shots use your aim when the shot fires, including camera kick. Left-click charges for 0.5 seconds with no added spread; release early to cancel. Right-click fires immediately with a 1.25-degree spread cone at rest, increasing to 3 degrees while moving. Both modes use one bullet and one action, deal the same headshot damage, and use the same cigarette buff. Q must finish drawing before either shot fires. Holstering, opening a menu, losing focus, or changing rounds cancels an unfinished charge. A held left button fires only once; press it again to start another charge.
Head hitboxes follow the visible head while standing, crouching, looking around and flinching.
The host checks recent rendered poses to compensate for network delay; it still decides hits and damage.
Two valid shots into a living target count separately, even when they arrive together.

Exterior window panes and individual backbar bottles shatter when shot, with falling glass and a breaking sound. A shot uses the normal bullet and action. Broken glass lets later bullets pass through. Damage stays visible to every player through later rounds and reconnects, then resets for a new match.

A nonlethal hit does not discard a trigger already queued during the revolver draw.
Use matching updated builds on every device (network protocol `state-36`).

At **0% health**, your body moves back, falls and rests on the floor.
Your camera briefly follows the body.
Then you watch other players.
Your body stays on the floor while you spectate the surviving players.

## Tutorial and help topics — development source

The first launch of a new player build offers a short playable walkthrough.
Select **Start walkthrough**, or **Skip tutorial** to go to the main menu.
The main menu's **Tutorial** button opens this choice again at any time.

The walkthrough takes place on a separate training map: a compact saloon with a
shooting lane, shared bar with a cash drawer, two connected minigame booths, and a smoking
veranda with the normal game's sunset sky, desert mesas, cacti, and weathered
cedar deck. Indoors, follow the single warm spotlight. It lights a reachable path,
then the object you must use. Inactive indoor stations stay dim. Complete the
action in the prompt. Health and bullets use the same HUD as normal matches.
During the planning exercise, choose Bar. Other choices stay unlocked and show
a correction, so the walkthrough can continue safely.
The next prompt appears automatically. There are no chapter screens or Next
buttons during the route. The target is under five minutes; players can take
longer without a failure timer.

The route teaches movement, crouching, quick and accurate shots, reload, the
one-action-per-round rule, beer, planning, Coin Flip, Blackjack, smoking and its
boost, all five roles, drawer inspection and theft, escape response, and spectating.
It shows **1 ACTION = 1 ROUND** and a clear action list with controls in
parentheses during actual play. The spectator exercise shows a death-camera
countdown, then Previous/Next buttons and explicit Left/Right arrow-key guidance.
The Outlaw exercise uses three two-second drawer checks in separate rounds. Training
rounds advance quickly.

Press **Esc** to pause or **F1** to open **Help topics**. The searchable topic
library contains the detailed rules and individual practice examples. Reading a
help topic preserves the walkthrough's position. **Try this topic** opens a
separate practice example on the normal map. The tutorial runs offline and
restores the normal map and practice settings on exit.

In Unity, select **Saloon → Preview interactive tutorial** to try it.
**Preview tutorial spectator checkpoint** opens the elimination/spectator exercise
directly for a quick retry in the Editor. **Preview tutorial sunset deck** opens
the smoking exercise directly, facing the sun.
**Verify short tutorial in Play mode** checks the continuous physical route;
**Verify interactive tutorial in Play mode** checks the individual topic examples.
Automatic first-launch display applies to player builds; the Editor uses manual
preview. The normal 0.3.0 downloads include the playable walkthrough and help library.

## Start testing in Unity

For a new source checkout:

1. Import the licensed Western Starter Pack from your Unity account.
2. Run **Saloon → Prepare Western Gunman character**.
3. Open the **SaloonPrototype** project.
4. Open `Assets/Scenes/Saloon.unity`.
5. Enter Play mode and select **Solo practice** on the title screen.
6. Choose **3–7 bots**, discussion time (**5–120 seconds**), action time
   (**5–60 seconds**) and shared beer supply (**0–16**), then select **Start practice**.

The public repository excludes the imported pack and derived meshes.
Compiled downloads include them.
Four practice bots are selected by default. Your selection creates a match with
one human and three to seven bots, using the role deck for that match size.
They know the roles and aim accurately.
They act as opponents, with no conversation function.

Solo practice runs locally.
It does not open a LAN port or automatically connect voice.
The LAN address, port and No Bots setting do not prevent solo practice.
Practice players obey the host's action limit, ammunition, health, room and raycast rules.

A match with **No bots** requires four to eight humans.
A match with **Bots** requires at least one human.
For LAN, host a match and connect with the host's local IP address and matching UDP port.
The default port is **7777**.
For online play, create or join with a room code.

All devices must use the same source or build version.
Use **0.3.0** clients for this playtest.
The game rejects mixed versions with a message.
The disconnect reason stays on the menu.
It is not replaced silently.

## Room minigames

The West room alternates **Coin Flip on odd rounds** and **Bluff Dice on even
rounds**, starting with Coin Flip. Its name, invitations and room card show the
announced game during discussion, before anyone locks a vote. The wall graphic
changes with it. Both games use the same two seats and give their winner the
normal combat opening; Blackjack stays in the East room. Tutorial Coin Flip
lessons continue to use Coin Flip.

### Coin Flip

Two players sit opposite each other at a felt table in the **Coin Flip room**.

1. Choose **HEADS** or **TAILS** in the center of the screen.
2. Wait for the coin result.

The first selection fixes both sides.
A gold coin moves above the felt and lands with the host's result facing up.
Both players see the same toss.
If nobody makes a selection, the assigned sides start a toss after eight seconds.

The cursor stays free for the coin controls; mouse look resumes automatically after the toss.
After a four-second result pause, the winner can press **Q**, then hold left-click for an accurate shot or right-click for a quick shot.
The winner can also press **F** to pass.
Only the room participants can see the coin; the opaque room windows hide it from the Bar.

### Bluff Dice

One player is the **speaker**. Click **Throw Dice** or press **Space** to shake
the cup and throw **three secret dice**. The cup waits for this input. They receive a
private sentence to say aloud, such as “At least two of my dice match.” Only
those three dice count. The game assigns a true or false claim with equal
probability; the speaker sees which it is and tries to make the listener guess
wrong. Read the exact sentence aloud, then argue or bluff around it.

The **listener** hears the claim and chooses just **Believe** or **Don't Believe**.
A correct judgment wins the listener the combat opening. A wrong judgment wins
it for the speaker. Reveal the speaker's three dice and explain the truth and
judgment. There are no bids, raises, face selectors or extra hands to eliminate.

Classic allows 15 seconds to throw; missing this deadline forfeits to the
listener without rolling automatically. A throw takes 2.4 seconds, followed by
eight seconds to speak, then six seconds to
judge. The choices unlock after the speaking interval. A listener who does not
answer in time forfeits to the speaker. After the usual three-second result
pause, the winner gets the existing ten-second exclusive opening. Both retain
one normal combat action; passing, opening expiry, damage and the opponent's
response are unchanged. Medical events work with either West room game.

Human listeners do not receive the private sentence, dice values or truth flag
before reveal. Hold **V** to talk, or use **T** for open mic. Speech is social:
the game checks the assigned sentence and does not transcribe or verify what
someone actually says. Bots communicate their assigned sentence through a
caption and make blind judgments; they do not understand microphone speech.
Public table/spectator snapshots contain no secret prompt, truth or roll.

Use **Saloon → Play Bluff Dice at table** to open
`Assets/Scenes/BluffDiceTest.unity`. It starts offline with one bot and a paused
instruction screen. The first hand lets you throw as the speaker; the next
hand lets you judge a captioned bot claim. Practice waits indefinitely for
your throw or judgment and holds its reveal until you choose
**Continue to combat** or **Throw again**.
**N** starts a fresh hand with restored health, ammo and actions. After combat,
practice waits for you to request another hand. Classic keeps its short timing.
The test scene is excluded from the normal release scene list.

The painted cup-and-dice mural remains outside the room, replacing Coin Flip's
mural only on Bluff Dice rounds. The wall remains solid and private.
Use **Saloon → Verify Bluff Dice rotation and combat opening** for the live
suite, or `python3 Tools/verify_bluff_dice.py` for standalone production rules.

### Blackjack

Two players play a short blackjack hand in the **Blackjack room**.
The host randomly assigns the dealer and player roles.
Both sit opposite each other at a felt table.
Cards move from a physical deck onto the table.
The dealer's second card stays hidden until the player stands.

For the player:

1. Choose **HIT** in the centered screen controls to take a card.
2. Click **STAND** to keep your hand.

For the dealer, choose **DRAW CARD** in the screen controls to take a card below 17.
The dealer automatically stands on every 17.
The cursor stays free for card decisions; mouse look resumes automatically when the hand ends.

Aces count as 1 or 11.
Face cards count as 10.
A blackjack with two cards beats an ordinary 21.
A tie automatically draws a high card for each seat from the remaining deck. Aces rank highest; equal ranks redraw only those cards, with no suit advantage or new Blackjack hand.
No hand totals or calculated scores appear on the table or screen; read the cards yourself.
The opening deal takes 2.2 seconds. Hits and reveals settle for one second before the next decision.
You have 45 seconds for each player decision, renewed after each hit.
An idle dealer automatically takes a card after five seconds. A tiebreak shows its high cards in separate rows for two seconds before the winner's result.

The Blackjack result remains visible for three seconds before combat unlocks. Both players can stand and dodge during this pause.
The winner then has ten seconds to draw with **Q**, hold left-click for accuracy or right-click for a quick shot, or press **F** to pass.
Q followed by a quick right-click stores one shot until the draw animation finishes. A deliberate left-click charge starts after the revolver is ready.
The opponent can act after the winner's action or the opening timeout.
Blackjack does not use either player's combat action.

Practice bots play automatically.
The Bar and smoking deck can be visited alone. Unmatched Blackjack and Coin Flip choices return to the Bar.
At the Bar, everyone can use their action immediately; there is no turn queue.
The Bar has a shared tray of ready beers. Walk up and press E to drink one as your round action.

### Private plans and room capacity

Room and companion selections stay private to their owner. Inviting someone shares the requested room only with that person.
Other players receive no destination or companion selection during planning.
The planner shows fixed capacity and hides reservation counts. Incoming invitations are visible only to their intended recipient.
Incoming invitations name the inviting player and destination. Click **Join** to accept and reserve your place. Human invitees choose whether to accept; practice players can accept automatically.

Seats use **first ready, first served** reservations.
Coin Flip, Blackjack and **Have a Smoke** each have two places.

1. Select a destination.
2. Click **Lock choice** (**Go alone** for Have a Smoke) to reserve your own place, or invite someone to go with you.

Clearing Ready or changing your plan releases the place.
Inviting another human reserves your own place. The other person must accept or set Ready too.
A requested practice companion is moved with you when the room has space; unrelated bots yield to human plans.
A full destination rejects more Ready requests until a place becomes available.
The Bar stays available. Reservations reset each round.

Coin Flip and Blackjack are enclosed rooms with no door or doorway.
Solid plaster walls close the full front of each room, with large painted cards
and coin-flip artwork on the Bar side. Each room has a centered ceiling lantern.
Bar players cannot see room occupants, the coin, or cards; room occupants cannot see the Bar.
Table state is sent only to room participants and eligible spectators.
Both sides block bullets and movement.
The walls remain intact under gunfire.

Room assignment puts players inside when room actions start.
Nearby indoor voices through room walls are quieter and low-pass filtered in both directions.
Voices within the same room stay clear. Indoor player sounds such as gunshots
and footsteps also pass through a low-pass filter when heard from another room.
The smoking area stays isolated.
The Bar keeps its existing layout and rules.

### Smoking area

**Have a Smoke** takes you out the back to a private sunset deck with two places.
Choose it during room voting; there is no walking route from the Bar or front entrance.
Select **Have a Smoke** and click **Go alone** to reserve your own place, or invite another player.
Practice bots reserve deck spots when a player selects **Go with** or accepts their invitation.
Players arrive facing a low desert sun, layered sandstone mesas, cacti and an apricot-to-violet evening sky.
Walking outside is silent. Wooden footsteps resume inside the saloon.

- Click the left mouse button to smoke.
- Hold **V** to talk, or press **T** to switch open mic on. **M** mutes either mode.
- Use **WASD** to walk.
- Press **Space** near an empty bench spot to sit facing the sunset; press **Space** again to stand. Both smokers can sit and smoke together.

The cigarette has an animated first-person hand and a third-person arm pose.
It also has a glowing end, ash and exhaled smoke.
Repeated clicks wait for the four-second smoking animation to finish.
The host rejects weapons and saloon actions outside.
Smoking does not use a combat action.

Entering the smoking area grants one **cigarette buff**. Your next fired revolver shot or beer you drink consumes it. A shot, including a miss or body hit, uses the buff; a damaging headshot removes **40 percentage points** instead of 20. A ready beer heals **40 percentage points instead of 20**. Health never exceeds **100%**. Repeated visits do not stack buffs. The buff survives regroup and later rounds until used; death, a new match or returning to the lobby clears it.

Your HUD shows **CIGARETTE BUFF · NEXT SHOT OR BEER x2** while it is ready. Other players and spectators do not receive your buff status. Boosted hits deal 40 points to the target, including allies. Damage never reflects onto the shooter.
The yard stays available for the normal action window.

Indoor and outdoor players receive separate action snapshots and voice channels.
Outdoor players cannot see indoor characters or hear indoor conversation and gunshots.
Indoor players cannot see or hear outdoor players.
The Bar starts in warm sunset light through the side windows. All four Bar
pendants and both wall lamps remain visible and lit, at reduced brightness.
Lamp and ambient light have been raised from the initial dark revision to
preserve visibility, including at night. It gradually darkens over roughly
twenty rounds, then holds that night setting. New matches reset to sunset.
Blackjack, Coin Flip, and the smoking deck keep their existing lighting.

Two exterior windows on each long side of the Bar, plus one in each minigame
room, look out onto the desert. Glass blocks movement and shots. The walls
between the Bar and both minigame rooms remain opaque.
The front entrance overlooks the same desert sunset through low saloon doors,
but its scenery never includes the private back deck or its smokers. The
entrance still blocks walking out; voting is the only way to go out the back.
Regroup restores the shared player list and indoor voice.

### Toilet Trouble — optional safe solo mission

During discussion, one living player **below full health** is chosen uniformly
at random and privately offered the mission. Every role is eligible, including
bots; repeats are possible. If everyone is healthy, no offer appears. **Reject**
keeps normal room planning. **Accept** locks the mission and a return to Bar.
Bathroom is never a selectable room or an invitation destination.

At the start of room actions, the worker appears alone in a physically separate
bathroom outside the Bar map. They cannot be attacked, use weapons, hear other
rooms or be seen by their living occupants. **Left click eight times** to pump
(0.4 seconds per accepted stroke). There is a **12-second timeout**, including
idle time before the first click. E can equip the plunger without pumping.

Success heals **20 percentage points**, capped at 100%, once, and immediately
returns the player to the Bar **with their combat action still available**.
Timeout or abandoning the work returns them without healing, also keeping that
action. The shared action clock continues; short rounds reserve a small window
for returning to combat. Protection ends on return. Beer stock, beer healing,
beer action cost, ammo and cigarette buffs are unchanged by the mission.

Suction and gurgling are private to the bathroom. The worker's completion flush
can finish playing locally after return; other rooms and smokers cannot hear it.
The scene creates a separate **Isolated solo bathroom** root at world (0, 0, 40),
with its own render layer and voice channel. The Bar has no bathroom extension
or walkable entrance to the mission.

Use **Saloon → Verify isolated solo toilet mission** to run the
mission, privacy, damage protection, return/action, timeout and audio checks.

### Room tests

Use **Saloon → Play Have a Smoke outside** to try the yard.
**Saloon → Verify smoking area and private plans in Play mode** checks these functions:

- Visits with one or two players.
- Plan privacy and host weapon restrictions.
- First-person and third-person animations.
- Visibility between areas.
- Actual Vivox channel changes, with the verification microphone muted.

The current network protocol is `state-36`; all players must use the same build.

Generated builds, Unity caches, logs and verification captures are removed during local project cleanup. Unity recreates its cache on opening; the Editor verification commands regenerate captures when needed.
Both devices must use the updated build.

Use **Saloon → Play coin flip at table** to play against a bot.
**Saloon → Verify tabletop coin flip in Play mode** checks these functions:

- Table controls at both seats.
- Coin movement and the final face.
- Spectator access and combat openings.
- Automatic behavior after inactivity and cleanup.

Use **Saloon → Play blackjack at table** in Unity to play against a bot.
**Saloon → Verify tabletop blackjack in Play mode** checks seating, card privacy, Hit/Stand/Deal raycasts, both camera views and cleanup.
**Saloon → Verify round clock** checks card scores.
**Saloon → Verify rounds in Play mode** also checks the room sequence.

## Interface (Unity development version)

The development source uses 0–100% health. Each unbuffed revolver headshot removes 20 percentage points; five unbuffed hits eliminate a player at full health. A cigarette buff doubles your next shot’s damage or your next beer’s healing, then is consumed. A ready beer restores 20 points, or 40 with the buff. Health is capped at 100%.

The lower-left display shows five 20% health segments, the exact percentage.
The lower-right display shows ammunition.
Five tightly spaced health markers and exact percentages appear above visible players' hats during discussion and room actions.
The companion list also shows each player's health percentage.
Room, companion, Ready and turn messages appear at the right edge.

For the Sheriff, hold **Tab** to see private clues beside your role during discussion or room actions.
Scroll while holding Tab to read older clues.
There is no separate Clues button or drawer for any role.
The Renegade can hold **Tab** to review its objective, without revealing other hidden roles.
Hold **Tab** for role instructions.
Press **Esc** to open the lobby or settings.
The interface uses small, flat slate controls without large permanent HUD panels.

## Private discussion clues (Unity development version)

To record a witness clue:

1. At the Bar during room actions, watch someone check the cash drawer.
2. Keep them clearly visible for approximately **one second**.

Your notebook records their name and the round, but not whether they inspected or stole.
Looking away, walls, other rooms or death prevent this record.
The clue does not confirm completed work or money collection.

For a private drawer inspection, available to every role:

1. At the Bar on your turn, press **C**.
2. Read the private inspection report.

The report says whether money is present and whether the drawer changed since your previous inspection.
It never identifies a thief. The report does not update automatically.

Any living player can hold **Tab** to read their own private drawer clues on the role card.
Inspection reports are not automatically shared with teammates.
The notebook holds your six latest clues, with the newest first.
Older retained clues are available by scrolling.
Clues stay between rounds and reset for a new match.

Hold **V**, or enable open mic with **T**, to tell nearby players what you found.
Clues are not shared automatically.
You can stay silent or give false information.
Other players cannot open your notebook.
Reports show information from the time of the observation. They do not update automatically.

## What happens in a round

| Phase | Default length | Function |
|---|---:|---|
| Discussion + planning | 30 seconds, or earlier when everyone locks in | Players talk and select a room and optional companion. Everyone sees selections, invitations and locked plans. Major actions are unavailable. |
| Room actions | Approximately 30 seconds maximum, with extensions for setup and turns | Indoor players have **one** action each. Bar players can act concurrently; side-room duels retain their opening rules. Aftermath starts when all indoor actions and timed actions finish. Smokers do not delay it. |
| Aftermath | 3 seconds | Actions are unavailable. Players stay in their rooms while shots, hit effects and gun spins finish. |
| Regroup | 3 seconds between rounds | Everyone returns to the common table. The next discussion starts. |

During room actions, press **Space** to stand or sit near an empty seat.
Use **WASD** to walk.
Movement stays available while you wait and after you use your action.
You must stay inside your assigned room.
Players stay seated during discussion and regroup.

A new round keeps roles, HP, ammunition, items, drawer state and pending escape plans.
Actions can end early when all living players finish or pass.
Timed actions that have started finish before aftermath.
There is no round limit or automatic timeout victory. Matches continue until a normal victory condition is met.
Elimination results wait until aftermath finishes before the game shows roles and results. A successful escape goes straight to results once all Bar actions finish, or when the action window expires.

A lethal hit makes the body fall as a ragdoll.
The dying player briefly sees the fall, then sees a living player's view.

Each side room has two places. The planner shows both locked seats and players still choosing it.
Selecting a room immediately publishes your intention; it does not reserve a seat.
Click **Lock choice** (**Go alone** for Have a Smoke) to reserve your own place, or invite an available player.
Accepting an invitation reserves **both** players together in one step.
Locked players disappear from invitation choices, and full rooms cannot be selected.
Use **Unlock / change** to release a plan. Changing a shared plan unlocks both partners.

The **Bar and Have a Smoke permit solo visits**. Locked solo choices are respected.
Only Blackjack and Coin Flip need two players; unmatched minigame plans return to the Bar.
Other players and intact pairs keep their selected destinations; nobody is recruited or moved to supply company.
The private smoking area only contains players who chose it or accepted a smoking invitation.
Assignments stay fixed during actions. The HUD and pause-menu player list show the actual room.

The planner opens at the start of discussion. It shows everyone's selected room, partner and lock status,
including players who have not selected anything yet. Right-click closes or reopens it.
A compact plan list stays visible when it is closed. Only the invited player can accept an invitation.
Practice bots offer invitations and obey the same locked-seat rules as humans.
The 30-second timer prevents an idle player from holding up the round; locking in everyone starts early.

The cash drawer is built into the customer-facing side of the main Bar counter. The shared beer tray is farther left along the counter so drinkers and drawer users have separate places to stand.
H advances shared Outlaw drawer work; the third step takes the money. C privately checks it for any role. Both require physical reach within 1.35 metres and a clear path. Rejected distant attempts do not spend an action. Walking away or taking a headshot interrupts the two-second check and keeps the action spent.
When any player is within reach, the drawer gains a pulsing gold highlight and a **C** prompt; Outlaws also see **H**. The prompt explains when an action is unavailable. Leaving reach or blocking access hides the highlight.
The space behind the counter is staff-only; railings close both ends.
It has no coin toss or minigame.
Beer, combat and drawer actions stay available.

### West room: the rotating game awards the opening

On Coin Flip rounds, the West room starts with random opposite Heads/Tails assignments.
On Bluff Dice rounds, one spoken claim and a Believe / Don't Believe judgment award that same opening.
Both players can select a side.
The **first selection fixes both sides** and gives the opponent the opposite side.
The centered screen controls allow eight seconds for a selection; the table stays clear around the coin.
The physical toss takes 2.2 seconds.
The host selects the random result when the toss finishes.

Indoor spectators can watch the table.
Only the room's players receive private action permission.
Major actions and gun drawing are unavailable before the result.
Blackjack uses its card game instead.

The winner gets the first action.
Press **Q** to draw, then hold left-click for an accurate shot or right-click for a quick shot.
Alternatively, start another permitted action or pass.
The opponent can act after the winner uses their action.
Both players keep the limit of one action each.

After the four-second result pause, the winner has ten seconds to act; otherwise the opening passes.
An eliminated winner cannot block the survivor.
The coin determines action order. It does not require a gunfight.
The shared action deadline includes the longest coin setup window and waits for timed actions that have already started.

### Bar: concurrent actions

Nobody waits for another player before shooting, inspecting, drinking, checking the drawer or planning escape.
Each player can start one major action or pass at any time during room actions.
An action uses only that player's allowance; the other players can act immediately.
Players can draw, look, talk, stand and walk independently of who has acted.
The shared action deadline still applies. Timed actions finish before aftermath.

Theft takes three shared actions in three separate rounds. Plan escape with **X** during the following discussion, then stay at Bar through that action phase. The earliest money escape is round four.
The Bar has no queue that delays any of those actions.

## Featured room: medical supplies

Round one has no event. From round two, each discussion has a 50% chance of
no event, a 25% chance of supplies in the West room (Coin Flip or Bluff Dice),
and a 25% chance in Blackjack.
An event room glows teal with a small medical icon; it stays fixed for the round
and still needs two players. Win its
minigame, then choose **Heal 20%** or **Keep opening** within the normal opening
timer. Healing takes your one round action and lets your opponent respond;
keeping the opening costs nothing and declines the supplies. An ignored choice
times out as a normal passed opening.

Supplies restore 20 health points, capped at 100. Full-health winners automatically
keep their opening without a prompt. Supplies use no shared beer and neither consume nor multiply a smoking boost.
Other roles and rooms have no special access, and there is no public reward-use
announcement. This development feature is disabled in the guided tutorial.

## What can I do with my one action?

| Action | Function and cost |
|---|---|
| **Shoot** | Fires one aimed bullet. A miss also uses your action and ammunition. |
| **Reload** | Refills the six-shot cylinder in 3.5 seconds. Reserve ammunition is unlimited. The Renegade can shoot without reloading. |
| **Drink beer** | Walk to the shared tray at the Bar and press E. Takes one ready beer and drinks it for 2.5 seconds, restoring 20 points (40 with your cigarette buff), capped at 100%. Uses one action and one shared beer. |
| **Work the drawer** | H beside the Bar cash drawer. Outlaws only; three shared two-second steps across three rounds collect the money. |
| **Declare escape** | X during discussion while carrying the money. Outlaws only. Lock a Bar plan; the escape begins in the next action phase. |
| **Inspect drawer** | C beside the Bar cash drawer. Any role; uses your action and gives a private report. |
| **Medical supplies** | Win the featured room and choose Heal 20% instead of using your opening action. |
| **Pass** | F gives up your action for this round. |

The shared tray visibly holds the remaining beers. Walk up and press **E** once to take and drink one.
There is no pouring, carrying, offering, fill-quality minigame or follow-up choice.
Full health, an empty tray or being too far away preserves your beer supply and action.
Drinking keeps the existing first-person mug animation and sound. A nonlethal hit does not stop it; elimination cancels healing.

Drawing or holstering with **Q** has no action cost.
It occupies your hands for 0.25 seconds.
A right-click during a permitted draw stores one quick shot until drawing finishes.
This also applies at the bar and after a minigame.
The game checks turn permission again before firing the stored shot.

Looking, speaking and walking have no action cost. Drinking uses your action.
A busy player cannot start another action.
Everyone starts with 100% health.
**Only headshots cause damage: 20 percentage points normally, 40 with a cigarette buff**.
Body hits and misses use one bullet and your action.
They cause no damage or penalty for shooting an ally.

At 0% health, a player is **eliminated**.
An eliminated player cannot move, act, revive or return during the match.
Eliminated players use a separate voice channel.
A new match resets everyone.

Ragdolls land and stay on the floor until the match resets.
At approximately 3.2 seconds after death, the player starts spectating the survivors.
The body remains visible; its name label hides and its settled physics no longer block movement.
The dying player's camera follows the body until that change.
Later room assignments can use the empty seat.
Surviving players keep their usual common-table places.

## Robbery: blend into the Bar

Stand with **Space** and walk to the cash drawer beside the beer tray. During room actions, press **H** as an Outlaw or **C** as any role. Both checks use one action, take two seconds, and look the same to witnesses.

Each successful H check advances shared work by one step, capped at one step per round for the whole Outlaw team. The third step takes the money immediately; there is no separate pickup. If the drawer is empty, the check still spends your action. Leaving reach or taking a headshot interrupts the check and gives no money.

An ordinary C inspection gives you a private report: money present or missing, plus whether the drawer changed since your previous check. The report never names the thief, and no live theft result is shared with the room. A witness who watches long enough learns who checked the drawer, but not whether they inspected or took money.

If the carrier dies, the money returns to the drawer. All three theft steps must be completed again.

## How do we win now?

**Sheriff death ends the match: Outlaws win, unless the Renegade is the sole survivor.** The killer's role does not change that result.
Outlaws can also win by escaping with the money after three shared drawer steps.
The Renegade must eliminate everyone else before killing the Sheriff.

| Faction | Victory condition |
|---|---|
| Sheriff + Deputy (Law) | Keep the Sheriff alive and eliminate all Outlaws and the Renegade. The Law cannot rob or escape. |
| Outlaws | Sheriff dies, unless only the Renegade survives; or any Outlaw escapes with the money. All Outlaws share either team victory, including eliminated teammates. |
| Renegade | Eliminate everyone else and leave the Sheriff until last. Be the only survivor. The Renegade cannot rob or escape with money. |

Escape is planned during the discussion after theft:

1. Select **Attempt escape** or press **X** while carrying the money.
2. Lock your **Bar** plan. Planning is free; a side-room plan cancels the attempt.
3. The escape warning is public during discussion, with at least ten seconds to reconsider rooms. Every defender still has an unused action when the next action phase starts.
4. The carrier keeps one normal action to shoot, heal, reload or pass while escaping. Stay at Bar; once all Bar actions finish without an interrupting headshot, the Outlaw victory screen appears immediately. Players in side rooms do not delay a guaranteed escape.
5. Any headshot interrupts the attempt. The carrier keeps the money and can plan another try during a later discussion.

There is no match round limit or automatic timeout victory. Escape may be attempted in any discussion after collecting the money. The normal response window and headshot interruption still apply. If the carrier dies, all three theft steps reset.

Results appear after the final aftermath animations.

A Renegade who is the sole survivor wins after the final-shot effects.
All roles are shown when the match ends.
Disconnected players keep their place for 20 seconds. The client automatically tries to reconnect to the same LAN or online room, retaining its role, health, inventory, position and round participation. The match continues during this grace period. If a player does not return in time, the match is aborted and the reserved place is released.

Results show a portrait and role for every player. Dead players appear in gray. Each player on the winning team gets a **Winner** label. Each human player can vote for a rematch or withdraw their vote. Unanimous votes start another match in the same room; bots do not vote and reserved disconnected players block the next match. **Return everyone to lobby** lets any player bring the whole group back to the existing lobby to change settings or wait for players. Reconnecting requires the original running client; restarting the app or losing the host is not supported.

Use **Saloon → Verify rematch and reconnect in Play mode** to exercise the four-player UDP flow, including reconnect preservation and reservation expiry.

## Beer animation and audio

Unused beer stays behind when players regroup at the end of a round.
Beer cannot continue into the next round.
Discarded beer does not return to shared stock.

Carried beer, self-drunk beer and given beer appear as mugs.
In third person, the arm reaches for the handle and raises and tilts the mug toward the mouth.
In first person, the mug moves toward the camera.
A given beer moves in an arc to the recipient.
The recipient drinks automatically.

Drinking plays a gulp sound.
A transfer plays a quiet clink.
These original alpha sounds are generated procedurally. They are not recorded samples.

The revolver sound combines a sharp report, body, metal and room effects.
After an accepted shot, the revolver briefly spins in first and third person.
The spin is visual only and gives no extra shot.
Shooting adds a short camera movement.
Damage adds a short shake and red tint.
Both views show a brief muzzle flash and light.

Camera effects do not change the host's aiming ray or move your seated position.
A **non-lethal headshot does not cancel** a normal or given drink.
Health restores when the drink timer finishes.
For example, a shot reduces a player from 40% to 20%.
A ready beer restores health from 20% to 40%, or to 60% with a cigarette buff.

A lethal headshot cancels drinking. Beer cannot revive a player.
Damage interrupts a drawer check.

Mug movements and third-person gun grips use procedural poses.
Local playtest results can guide later improvements.

## What practice players do

Practice players use **complete role knowledge**, including hidden roles, to pursue team victory.
This aggressive test AI does not simulate human deduction.

- They use the bar and side rooms and change plans each round.
- They accept a human's room invitation unless they carry money.
- A carrier stays at the bar.
- Law players shoot criminals and protect the money.
- Only Outlaws can take money and plan an escape; every role can inspect the drawer.
- Outlaws can shoot to defend themselves and treat each other as allies.
- The Renegade targets living Outlaws first, then the Law.
- The Renegade never robs or declares a money escape. Its objective is to be the last survivor.

Practice players aim their shots, draw their guns, and use beer when needed. They reload when empty. The Renegade has unlimited bullets.
They pass when no useful action is available.
They briefly keep a fixed aim point before firing, so movement can avoid a shot.
They receive no extra actions or HP.
Their bullets cannot pass through walls.

Practice players stand and move sideways around opponents during room actions.
They can move while waiting or after using their action.
They avoid furniture and room boundaries, change direction and sometimes stop.
Drinking and drawer checks slow their movement.
They return to their seats for discussion.

Practice players are silent.
They have no proximity conversations, accusations or false claims.
They can act faster and aim more accurately than humans.
Their difficulty is not balanced for normal play.

## Controls and voice

| Input | Function |
|---|---|
| Hold Control | Crouch; release to stand when there is room overhead. |
| Space / WASD | Stand or sit near an empty seat / walk and dodge inside your room during actions. |
| Mouse | Look / aim. |
| Right-click during discussion | Open or close the room planner. |
| Q / hold left-click / right-click / R | Draw or holster / accurate shot after 0.5 seconds / instant shot with more spread / reload. |
| E near the shared tray | Take and drink a ready beer at the Bar. |
| H / X (Outlaw only) | Take money beside the drawer / plan escape during discussion. |
| F / C | Set or clear Ready during discussion / inspect the drawer as any role. |
| F during actions / hold Tab | Pass / read your private role and objective. |
| Left / Right arrows | Change the living player's view in spectator mode. |
| Hold V / T / M | Push-to-talk / toggle open mic / toggle microphone mute. |
| Esc | Open the menu, host settings, voice devices and player list, or leave. |

Proximity voice connects when you join.
Speech detection automatically adapts to your microphone and background noise. If voice disconnects or a channel join fails, it retries automatically; the mic indicator shows when voice is reconnecting.
Hold **V** for push-to-talk, or press **T** once to enable open mic and speak without holding a key.
Press **T** again to return to push-to-talk. The Escape menu also has an open-mic switch.
**M** mutes either mode. The top-right indicator shows mic mode and mute state.
Menus pause microphone transmission; closing them restores your selected mode.
Open mic stays selected across indoor, smoking and spectator channel changes, and resets when you leave the session.
After elimination, you hear the game audio and proximity chat from the viewpoint you're spectating, including the watched player's own voice when they speak. Use the arrow keys to change viewpoints. Your microphone stays in eliminated-player chat during the match.
The cigarette area uses a separate private channel: smokers cannot hear inside, and indoor players cannot hear smokers.
Regroup returns everyone to indoor proximity voice. These rules apply in both microphone modes.
It has full volume within two metres and fades by 10 metres.
During room actions, nearby voices across Bar/Coin Flip/Blackjack boundaries are muffled both ways.
Voices within the same room remain clear.
Waiting-room voice stays on; Deal Roles pauses microphone transmission and voice playback
through both opening role screens, then voice resumes for table discussion.
Vivox voice requires internet on both LAN computers.
Game networking can run on LAN without internet.

Use headphones for two nearby laptops.
Press **Esc** for microphone selection, microphone playback tests and individual player mutes.

## Validation and scope

From Edit mode, run **Saloon → Verify rounds in Play mode**.
This checks host commands, rooms, robbery, privacy, escape, practice AI and beer.
**Verify rules and asset setup** checks role rules, victory rules and imports.
**Verify low-poly art** checks model and material availability.
These checks do not create a new player build.
[VERIFICATION.md](Documentation/VERIFICATION.md) records tests and limits.

Only the Sheriff is publicly identified during a match.
Each player knows their own role.
Outlaws do **not** receive an ally's name.
All roles are shown after the match.
A line at the bottom gives private suggestions for your phase and role.
It is not a public event log.

The host runs the simulation and sends a separate snapshot to each player.
The game has no host migration, reconnect after restarting the app, matchmaking, prediction or production anti-cheat.
Bots deliberately receive private role information on the host.
This alpha is for trusted private groups.

[ASSET-SOURCES.md](Documentation/ASSET-SOURCES.md) records art sources and licenses.
[DESIGN-HISTORY.md](Documentation/DESIGN-HISTORY.md) contains previous design notes.
The parent Project-IDK is separate.
This prototype does not change it.

## Graphics settings — development source

Open **Graphics settings** on the title screen or **Graphics** in the Escape menu.
You can change the quality preset, resolution, window mode, VSync, and frame rate
limit. Defaults are Medium, VSync off and a 60 FPS limit. Settings save
locally; unconfirmed display changes revert after 15 seconds.
See [graphics settings](Documentation/GRAPHICS-SETTINGS.md).

This menu is included starting with 0.2.15. Its menu, quality changes,
Apply/Cancel behavior and preference persistence were exercised in the Editor on
2026-11-12. OS display switching and timed rollback still require a player test.
The minimum performance target is **60 FPS**; minimum hardware is not established.

## Next playtest priorities and beta readiness

These are proposed acceptance gates, not completed test claims. Current scope is
private matches for **4–8 players**.

- [ ] Run at least three complete human playtest sessions covering four through eight
  players, including new players. Record waiting time, confusion, winners, victory
  reasons and desire to replay.
- [ ] Complete real-device LAN/Wi-Fi/firewall tests and online room-code tests
  across separate networks, with Windows and Mac players and voice active.
- [ ] Investigate the reported Windows return to menu. Capture disconnect details
  and logs; verify a correction or document the cause and a reliable recovery path.
  Current automated recovery checks do not close this report.
- [ ] Exercise full matches through every victory condition, elimination,
  spectating, rematch, reconnect expiry and host loss, without a stuck match or
  unexplained loss of state. Run a two-hour session to check stability.
- [ ] Validate 60 FPS on candidate minimum PCs at a stated resolution and preset,
  including hosting, voice and busy scenes. Record frame times and peak memory.
- [ ] Test graphics display changes and automatic rollback in a future player
  candidate. No new build is required for source or Editor iteration.
- [ ] Verify that new players can plan, ready, understand their role, use minigames
  and finish a match without developer instructions. Tune timing, drawer checks,
  escape and sound levels using those observations.
- [ ] Define supported OS/hardware, known issues and a support/bug-reporting path.
  Decide whether practice bots remain explicitly omniscient test opponents or need
  a fairer normal-practice mode.

Already implemented: build-version/protocol rejection, disconnect details,
20-second reconnection in the original running client, private evidence and thief
identity, role instructions, spectator cycling/persistent bodies, and multiple
animation/grip/hit-reaction improvements. These still need human usability and
balance feedback; they are not wholly unimplemented TODOs.

Deferred scope: host migration, reconnect after app restart,
matchmaking, wall-based voice attenuation and production anti-cheat. These are not
all prerequisites for a private-group beta. A public release must state its
limitations clearly and assess abuse protection for its intended audience.

Before public Early Access, also prepare reliable installation/updates, appropriate
signing/notarization for offered platforms, audio/input preferences, basic
accessibility, a support channel, service-capacity checks and a realistic roadmap.
Assess whether the current game offers enough replay value at its intended price.
[Steam's Early Access guidance](https://partner.steamgames.com/doc/store/earlyaccess)
requires a playable current product and clear communication about its unfinished state.

Run **Saloon → Verify matches without a round limit in Play mode** to check play beyond round 20 and late escape attempts.

## HUD and combat feedback

HUD means the status information shown during play.
The round header shows the number of living indoor players with an unused action.
It excludes smokers and eliminated players.
When no actions remain, a separate message identifies any timed action that must finish before aftermath.

There is no crosshair hit marker or shot-result popup.
Shots use a larger muzzle flash made from intersecting planes that gradually disappear.
The barrel has stronger recoil.
The HUD shows no hit, miss, target-HP or damage-result text.
The vitality bar shows health changes.
Permanent control hints are absent from the HUD; a small top-right microphone indicator shows voice mode and mute state.

Reloading uses a separate positional cylinder and cartridge sound.
The gunshot keeps its sharp report.
Persistent status messages explain used turns, minigame openings, reloading and other unavailable actions.

The HUD uses bundled Rye headings and Barlow Semi Condensed body text.
Floating labels replace black HUD panels.
The role and vitality bar appear at the lower left.
Cartridge shapes and ammunition appear at the lower right.
The Sheriff badge and name appear at the upper left.
Menus and planning controls use light parchment, dark text and rust-red selections.

Indoor walking uses five prepared wooden footstep sounds from Kenney Impact Sounds (CC0).
Outdoor walking uses softer, synthesized dirt sounds.
The previous synthesized wood sounds are removed.
Standing uses a separate input debounce, so Space works immediately after a shot.
Coin Flip and Blackjack seats stay locked only until their minigame finishes.
Either player can then stand and dodge, including after using their action.

Other players' footsteps use positional audio.
Your own footsteps stay centered.
Seated, stationary, eliminated, stale and teleporting characters do not trigger footsteps.
The bundled font licenses are in `Assets/Resources/Fonts`.


Revolver shots, reload and drinking audio in the development source now use CC0
recordings from Freesound. Source links, authors and processing notes are in
[asset sources](Documentation/ASSET-SOURCES.md#recorded-sound-replacement--2026-11-11).


The public Bar floor includes the walking route around the center discussion table
and the aisle toward the entrance. Center-table and chair colliders match their
visible footprint. The outside entrance is physically closed; E does not enter
or leave the building. The smoking deck remains a separate room selected in planning.

## Beer artwork and round transitions — development source

Beer now uses eugeneloza’s CC0 OpenDungeons tankard, with a fitted ale and foam
surface, on the shared tray, on tables and in both drinking views.
The source and conversion are recorded in [asset sources](Documentation/ASSET-SOURCES.md).

After the last action, the waiting period retains the same beer tray, coin and
physical blackjack hand. Table controls still close. Returning to the discussion
table updates camera position and facing together, avoiding a slide through the
bar furniture. Use **Saloon → Verify beer artwork and scene continuity** for the
focused Play mode checks and previews in `Documentation/BeerScenePreviews/`.
