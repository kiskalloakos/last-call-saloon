# Last Call — local development build

For role instructions, refer to [Your role, in simple words](#your-role-in-simple-words).

Test release **0.2.14**, dated 2026-11-11, is available for Windows x64 and macOS (Intel and Apple Silicon).
All players must use **0.2.14**.

1. Download [Windows x64](https://github.com/kiskalloakos/last-call-saloon/releases/download/v0.2.14/LastCall-Windows-0.2.14.zip) or [Mac (Intel and Apple Silicon)](https://github.com/kiskalloakos/last-call-saloon/releases/download/v0.2.14/LastCall-macOS-0.2.14.zip).
2. Extract all files from the ZIP file.
3. On Windows, open `Last Call.exe`. Keep its data folder beside it.
4. On macOS, open `Last Call.app`.

Mac four-player no-bot tests and native Windows gameplay, room, voice-connection and LAN recovery checks passed. See [release notes and validation](Documentation/RELEASE-0.2.14.md).

The Mac app is not notarized.
If macOS requests approval, use the system's Open option.

This release adds independent head movement, synchronized speech animation, visible room windows, persistent bodies while spectating, a right-click discussion planner with invitations, cleaner HUDs, and slower blackjack and coin-flip pacing. It also includes crouching, updated weapon movement, and the sunset overlook.

This release includes these features:

- Discussion and planning with a Ready control.
- Matches with four or five players, with optional bots.
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
2. Start **0.2.14 on every computer**.
3. Select **LOCAL / LAN** on each computer.
4. On the host computer, select **NO BOTS**.
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
The roles are Sheriff, Deputy, one Outlaw and one Renegade.

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

In 0.2.14, mouse look moves the head independently.
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
Click **Ready** or press **Space**.
All living players must be ready.
There is no discussion countdown or separate timer for room selection.
A plan change clears that player's Ready status.

Mouse look stays active during discussion. Right-click to open or close the room planner; Space readies your plan.
Practice players set their own Ready status.
They use the bar and side rooms, change plans each round and accept invitations unless they carry the money.
A headshot against an ally removes 20% health from the shooter and 20% health from the target.
The Sheriff and Deputy are allies. The Outlaws are allies.

The upper-left display always identifies the Sheriff.
It shows “YOU” for the local Sheriff and an eliminated status after the Sheriff's death.
Hold **Tab** to read your private role, including when the planning panel is open.
You do not have to hold the right mouse button to use Tab.

The Tab card gives instructions for your role:

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
In the lobby, the host can change this setting and select a **4-player** or **5-player** fill target.
Bots fill empty places only when the host starts the match.
With **No bots**, four humans can start immediately. A fifth human is optional.
Every human in the lobby joins the match.

| Match size | Roles |
|---|---|
| 4 | 1 Sheriff, 1 Deputy, 1 Outlaw, 1 Renegade |
| 5 | 1 Sheriff, 1 Deputy, 2 Outlaws, 1 Renegade |

The lobby holds **up to 8 human players**, including the host.
A match currently requires **4 or 5 players**.
Rules for 6–8 players are planned.
The lobby rejects a ninth connection and keeps the existing players connected.
Friends can replace bots after a match ends.
Bot settings cannot change during a match.

## Your role, in simple words

Hold **Tab** to see your role and objective.
Tab also works when Discuss & Plan is open.
The upper-left corner always identifies the Sheriff.
The Renegade knows everyone’s role. All other roles remain secret from the other players.

At the start of each round, select a room.
If necessary, select a companion for that room.
Click **Ready** or press **Space**.
The round starts when every living player is ready.
Wait for your turn.
You have **one action per round**, such as a shot, safe work, an inspection or a drink.

### Sheriff — protect the money

You and the **Deputy** are a team.
Everyone knows that you are the Sheriff.
The game does not automatically identify the Deputy to you.
HP means health.

1. Select **Bar** to watch the safe.
2. On your turn, press **C** to inspect the safe.
3. Read your private report.

The report shows money inside, signs of robbery or money missing.
It does not identify the thief.
A later inspection requires C on a later turn.

To shoot a suspected enemy:

1. Press **Q** to draw your gun.
2. Wait for the gun to rise.
3. Aim at the enemy's head with the mouse.
4. Click the left mouse button to shoot.

You and the Deputy win if you eliminate every Outlaw and the Renegade.
You also win if you prevent escape until the last round and at least one of you stays alive.
**H and X do not work for you**.
Only Outlaws can rob, collect money or escape with it.
A headshot against the Deputy also removes **20% health from you**.

### Deputy — help the Sheriff

You and the **Sheriff** are a team.
Your role is secret.
The game does not automatically identify you to the Sheriff.

- Protect the money.
- Select **Bar** to stay near the safe.
- For a private conversation, invite someone to a side room.
- On your turn, press **Q** to draw your gun.
- Aim at an enemy's head.
- Click the left mouse button to shoot.

**H and X do not work for you**.
You cannot rob or escape with the money.
**C does not work for you**. Only the Sheriff can inspect the safe.
You share the Sheriff's victory conditions, including if the Sheriff dies.
A headshot against the Sheriff also removes **20% health from you**.

### Outlaw — steal the money and escape

In a **4-player match**, you are the **only Outlaw**.
You must escape with the money to win.
In a **5-player match**, there are **two Outlaws**.
If either Outlaw escapes with the money, **both Outlaws win**.
The game does not identify your ally to you.

1. Select **Bar**.
2. On your turn, press **H** to work on the safe or collect its money.
3. Complete the robbery and escape procedure below.

Once you carry the money, X starts your escape on a later turn.
You must stay at the bar and survive the next round's room actions to complete the escape.

To defend yourself:

1. Press **Q** to draw your gun.
2. Aim at an enemy's head.
3. Click the left mouse button to shoot.

A headshot against another Outlaw also removes **20% health from you**.
The Sheriff's death alone does not give you victory.
Your team must escape with the money.

### Renegade — eliminate the Outlaws, then the Law

You know everyone’s role from the start. Hold **Tab** to see the complete role roster, including players in other rooms. Roles also appear above visible players for you.

You have **no ally**.
Your objective is to be the **last player alive**.
The Sheriff and Deputy together are the Law.

1. Fight the **Outlaws first**.
2. Keep the Sheriff and Deputy alive while they help fight the Outlaws.
3. After the Outlaws die, fight the Sheriff and Deputy.

To shoot:

1. Select a room with a suspected Outlaw.
2. On your turn, press **Q** to draw your gun.
3. Aim at the enemy's head.
4. Click the left mouse button to shoot.
5. If necessary, press **R** to reload on a later turn.

At the bar, hold **E** to pour as your action; release it to stop.
**B** drinks the poured beer, restoring its shown healing (5–20%).
**H and X do not work for you**.
You cannot rob, collect money or escape with it.
You win when everyone else is dead.

### H and X: how to take the money and escape

These controls work for **Outlaws only**, at the **Bar** during room actions.
The Sheriff, Deputy and Renegade cannot rob, collect money or escape with it.

1. On your turn, press **H**.
2. Wait **3 seconds** for the safe work to finish.
3. Repeat the safe work on later turns until three work actions are complete.
4. Once you carry the money, wait for a **later turn**.
5. Press **X** to start the escape.
6. In the next Discuss & Plan, select **Bar again**.
7. Click **Ready**.
8. Stay alive with the money until that round's room actions finish.

A shot interrupts safe work.
The work uses your action for the round.
The safe requires **three completed work actions in total**, across players and rounds.
The player who completes the third action gets the money.
One turn cannot complete all three actions.

A side-room assignment cancels your escape.
A completed escape gives the **Outlaw team victory**.
This means one Outlaw in a four-player match or both Outlaws in a five-player match.

If the carrier dies, the money returns to the open safe.
In a five-player match, the other Outlaw can collect it with **H** on their turn at the Bar.
The safe does not require another three work actions.

### Useful buttons for every role

| Button | Function |
|---|---|
| Hold **Tab** | Shows your private role and objective. |
| **Hold Control** | Crouch while standing; either Control key works. Release to stand when there is room overhead. Crouching slows movement and lowers your camera and hitboxes. |
| **F**, then **WASD** | F makes you stand. WASD moves you during room actions. F near an empty seat makes you sit. |
| **Q**, then **left-click** | Q draws your gun. A left-click shoots at your aim point. A miss also uses your action. |
| **R** | Reloads your drawn gun. Uses your action. |
| **E** at the bar | Hold to pour as your action; release to stop. |
| **B** | Drinks your poured beer for its shown **5–20% healing**, included in the pour action. |
| Aim at a nearby player, then **G** | Gives them your poured beer, preserving its healing. Included in the pour action. |
| **Space** during Discuss & Plan | Sets or clears Ready. |
| **Space** during your action turn | Passes and gives up your action for this round. |
| Hold **V** | Sends your microphone audio. |
| **Right-click** during Discuss & Plan | Opens/closes the planner. Mouse look is active when it is closed. |

Everyone starts with **100% health**.
An unbuffed **headshot** removes **20 percentage points** from the target. Five unbuffed headshots eliminate a player at 100%; a cigarette-buffed headshot removes 40 points.
A headshot against an ally also removes **20% health from the shooter**.
The Sheriff and Deputy are allies. The two Outlaws are allies.

At **0% health**, your body moves back, falls and rests on the floor.
Your camera briefly follows the body.
Then you watch other players.
Your body stays on the floor while you spectate the surviving players.

## Start testing in Unity

For a new source checkout:

1. Import the licensed Western Starter Pack from your Unity account.
2. Run **Saloon → Prepare Western Gunman character**.
3. Open the **SaloonPrototype** project.
4. Open `Assets/Scenes/Saloon.unity`.
5. Select **Play → Solo Practice**.

The public repository excludes the imported pack and derived meshes.
Compiled downloads include them.
Four active practice players fill the other places.
They know the roles and aim accurately.
They act as opponents, with no conversation function.

Solo practice runs locally.
It does not open a LAN port or automatically connect voice.
The LAN address, port and No Bots setting do not prevent solo practice.
Practice players obey the host's action limit, ammunition, health, room and raycast rules.

A match with **No bots** requires four or five humans.
A match with **Bots** requires at least one human.
For LAN, host a match and connect with the host's local IP address and matching UDP port.
The default port is **7777**.
For online play, create or join with a room code.

All devices must use the same source or build version.
Use **0.2.14** clients for this playtest.
The game rejects mixed versions with a message.
The disconnect reason stays on the menu.
It is not replaced silently.

## Room minigames

### Coin Flip

Two players sit opposite each other at a felt table in the **Coin Flip room**.

1. Choose **HEADS** or **TAILS** in the center of the screen.
2. Wait for the coin result.

The first selection fixes both sides.
A gold coin moves above the felt and lands with the host's result facing up.
Both players see the same toss.
If nobody makes a selection, the assigned sides start a toss after eight seconds.

Hold the right mouse button to look around.
After a four-second result pause, the winner can press **Q**, then click to shoot.
The winner can also press **Space** to pass.
Indoor spectators can see the coin through the glass but cannot use its controls.

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
Hold the right mouse button to look around.

Aces count as 1 or 11.
Face cards count as 10.
A blackjack with two cards beats an ordinary 21.
A tie starts another hand with the same roles.
No hand totals or calculated scores appear on the table or screen; read the cards yourself.
The opening deal takes 3.2 seconds. Each card takes time to arrive before the next decision.
You have 45 seconds for each player decision, renewed after each hit.
An idle dealer automatically takes a card after eight seconds. A push stays visible for five seconds before a new hand.

The result remains visible for four seconds before combat unlocks. Both players can stand and dodge during this pause.
The winner then has ten seconds to draw with **Q**, click to shoot, or press **Space** to pass.
A quick Q and click stores the shot until the draw animation finishes.
The opponent can act after the winner's action or the opening timeout.
Blackjack does not use either player's combat action.

Practice bots play automatically.
A player alone in a room does not need a minigame.
The Bar keeps its normal turn sequence.
The Bar now has a beer-pouring minigame; pouring uses the round action.

### Private plans and room capacity

Room and companion selections stay private to their owner. Inviting someone shares the requested room only with that person.
Other players receive no destination or companion selection during planning.
The planner shows fixed capacity and hides reservation counts. Incoming invitations are visible only to their intended recipient.
Incoming invitations name the inviting player and destination. Click **Join** to accept and reserve your place. Human invitees choose whether to accept; practice players can accept automatically.

Seats use **first ready, first served** reservations.
Coin Flip, Blackjack and **Have a Smoke** each have two places.

1. Select a destination.
2. Choose someone to go with, or **Go Alone**. This reserves your own place and marks you Ready.

Clearing Ready or changing your plan releases the place.
Inviting another human reserves your own place. The other person must accept or set Ready too.
A requested practice companion is moved with you when the room has space; unrelated bots yield to human plans.
A full destination rejects more Ready requests until a place becomes available.
The Bar stays available. Reservations reset each round.

Coin Flip and Blackjack are enclosed rooms with no door or doorway.
Their front glass covers the full width and permits sight in one direction only.
Players in the main saloon can see inside, including the coin and visible cards.
Players inside see an opaque pane when they look out.
Both sides block bullets and movement.
The glass cannot break from gunfire.

Room assignment puts players inside when room actions start.
Occupants can hear indoor voices and gunshots through the glass.
The smoking area stays isolated.
The Bar keeps its existing layout and rules.

### Smoking area

**Have a Smoke** is a separate sunset deck with two places.
One player can visit alone.
Players arrive side by side facing a low desert sun, layered sandstone mesas,
cacti and an apricot-to-violet evening sky. Walking outside is silent for both
players; wooden footsteps resume inside the saloon.

- Click the left mouse button to smoke.
- Hold **V** to talk.
- Use **WASD** to walk.

The cigarette has an animated first-person hand and a third-person arm pose.
It also has a glowing end, ash and exhaled smoke.
Repeated clicks wait for the four-second smoking animation to finish.
The host rejects weapons and saloon actions outside.
Smoking does not use a combat action.

Entering the smoking area grants one **cigarette damage buff**. It stays until your next fired revolver shot, including a miss or body hit. A damaging headshot with the buff removes **40 percentage points** instead of 20. Repeated visits do not stack buffs. The buff survives regroup and later rounds until used; death, a new match or returning to the lobby clears it.

Your HUD shows **CIGARETTE BUFF · NEXT SHOT 40%** while it is ready. Other players and spectators do not receive your buff status. Friendly-fire hits deal 40 points to the target when buffed; the shooter’s normal 20-point penalty stays the same.
The yard stays available for the normal action window.

Indoor and outdoor players receive separate action snapshots and voice channels.
Outdoor players cannot see indoor characters or hear indoor conversation and gunshots.
Indoor players cannot see or hear outdoor players.
Regroup restores the shared player list and indoor voice.

### Room tests

Use **Saloon → Play Have a Smoke outside** to try the yard.
**Saloon → Verify smoking area and private plans in Play mode** checks these functions:

- Visits with one or two players.
- Plan privacy and host weapon restrictions.
- First-person and third-person animations.
- Visibility between areas.
- Actual Vivox channel changes, with the verification microphone muted.

The current network protocol is `state-14`; all players must use the same build.
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

The development source uses 0–100% health. Each unbuffed revolver headshot removes 20 percentage points; five unbuffed hits eliminate a player at full health. A cigarette buff raises the next shot’s damage to 40 points. Beer restores the poured amount, rounded up to 5, 10, 15 or 20 points and capped at 100% health.

The lower-left display shows five 20% health segments, the exact percentage.
The lower-right display shows ammunition.
Five tightly spaced health markers and exact percentages appear above visible players' hats during discussion and room actions.
The companion list also shows each player's health percentage.
Room, companion, Ready and turn messages appear at the right edge.

For the Sheriff, hold **Tab** to see private clues beside your role during discussion or room actions.
Scroll while holding Tab to read older clues.
There is no separate Clues button or drawer for any role.
The Renegade can hold **Tab** to see everyone’s role.
Hold **Tab** for role instructions.
Press **Esc** to open the lobby or settings.
The interface uses small, flat slate controls without large permanent HUD panels.

## Private discussion clues (Unity development version)

To record a witness clue:

1. At the Bar during room actions, watch someone who works on the safe.
2. Keep them clearly visible for approximately **one second**.

Your notebook records their name and the round.
Looking away, walls, other rooms or death prevent this record.
The clue does not confirm completed work or money collection.

For a Sheriff inspection:

1. At the Bar on your turn, press **C**.
2. Read the private inspection report.

The report records the safe's condition and new tool marks since your previous inspection.
It also records the latest completed safe-work round.
It never identifies the thief.
An interrupted attempt leaves no completed-work marks.

The Sheriff can hold **Tab** while alive to read **YOUR CLUES · PRIVATE** on the role card.
Other roles use the discussion Clues drawer.
The notebook holds your six latest clues, with the newest first.
Older retained clues are available by scrolling.
Clues stay between rounds and reset for a new match.

Hold **V** to tell other players what you found.
Clues are not shared automatically.
You can stay silent or give false information.
Other players cannot open your notebook.
Reports show information from the time of the observation. They do not update automatically.

## What happens in a round

| Phase | Default length | Function |
|---|---:|---|
| Discussion + planning | Until every living player is Ready | Players talk and select a room and optional companion. Plan changes clear Ready. Major actions are unavailable. |
| Room actions | Approximately 30 seconds maximum, with extensions for setup and turns | Indoor players have **one** action each in room turn order. Aftermath starts when all indoor actions and timed actions finish. Smokers do not delay it. |
| Aftermath | 3 seconds | Actions are unavailable. Players stay in their rooms while shots, hit effects and gun spins finish. |
| Regroup | 3 seconds between rounds | Everyone returns to the common table. The next discussion starts. |

During room actions, press **F** to stand or sit near an empty seat.
Use **WASD** to walk.
Movement stays available while you wait and after you use your action.
You must stay inside your assigned room.
Players stay seated during discussion and regroup.

A new round keeps roles, HP, ammunition, items, safe progress and pending escape attempts.
Actions can end early when all living players finish or pass.
Timed actions that have started finish before aftermath.
The maximum is six rounds, configurable before the match starts.
The final round ends after aftermath, without regroup.
Elimination and escape results also wait until aftermath finishes before the game shows roles and results.

A lethal hit makes the body fall as a ragdoll.
The dying player briefly sees the fall, then sees a living player's view.

Each side room has two places.
A requested pair must select **each other and the same room**.
Mutual pairs have priority.
Players without a pair fill the remaining places.
An unaccepted invitation can send the requester alone to the selected room if space remains.

Only a capacity shortage sends a player back to the bar, with an explicit message.
Confirmed pairs keep priority and stay together.
The bar can hold everyone.
Assignments stay fixed during actions.
The HUD and pause-menu player list show the actual room.

Room and companion selections stay visible beside the discussion view.
Incoming invitations identify the player and requested room, even when the planner is closed.
Right-click to open the compact destination menu. Every invitation names the sender and destination.
Choose Join to accept it and ready yourself in one click.
To make your own plan, choose a destination, then Go with a practice player or Go on your own.
These controls submit the destination and readiness together. Inviting a human sends a request for their reply.
Only the invited player receives the request. Practice bots offer invitations of their own and yield seats
when a human chooses a destination or practice partner. Existing human reservations keep priority.
Room actions start when every living player confirms Ready.
Mouse look stays active during discussion. Right-click to open or close the room planner; Space readies your plan.

The Bar always uses a normal turn queue, including with one or two players.
It has no coin toss or minigame.
Beer, combat and safe actions stay available.

### Coin Flip room: coin for the opening

Only the Coin Flip room starts with random opposite Heads/Tails assignments.
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
Press **Q** to draw, then click to shoot.
Alternatively, start another permitted action or pass.
The opponent can act after the winner uses their action.
Both players keep the limit of one action each.

After the four-second result pause, the winner has ten seconds to act; otherwise the opening passes.
An eliminated winner cannot block the survivor.
The coin determines action order. It does not require a gunfight.
The shared action deadline includes the longest coin setup window and waits for timed actions that have already started.

### Larger rooms: ordered turns

The bar uses a new random turn queue each round, including with one or two living players.
The host maintains the turn queue and timers. The top-right turn/action panel has been removed in 0.2.14.
Each player can start one major action or pass.
Then the next player's turn starts.
Gun drawing and major actions are unavailable outside your turn, including for the Sheriff and practice bots.

All players can look, talk and walk while they wait. Pouring starts only on your turn.
An action spends that player’s turn; it does not grant another turn.

The action-window duration divided by the number of occupants gives the turn duration, with a ten-second minimum.
Each waiting player receives a full turn window when their turn starts, even after a late host frame.
An idle turn passes automatically.
The queue skips dead players.
The overall room-action deadline increases to preserve waiting turns.

Timed actions can continue during later turns, but finish before aftermath.
Turn order controls when an action starts.
It does not guarantee protection while health restores.

## What can I do with my one action?

| Action | Function and cost |
|---|---|
| **Shoot** | Fires one aimed bullet. A miss also uses your action and ammunition. |
| **Reload** | Refills the six-shot cylinder in 3.5 seconds. Reserve ammunition is unlimited. |
| **Pour beer** | Hold your tap handle or E to pour; release to stop. Uses your action and one beer from the shared stock of eight. One pour per round; full-health players cannot pour. |
| **Drink poured beer** | B drinks in 2.5 seconds, restoring the shown 5–20 percentage points, capped at 100%. Included in the pour action. |
| **Give poured beer** | Aim + G sends the same healing amount to a nearby living player with free hands. Included in the pour action. |
| **Work the safe / collect the money** | H at a bar station. Outlaws only. Refer to the robbery section. |
| **Declare escape** | X at a bar station while carrying the money. Outlaws only. Refer to the victory section. |
| **Inspect safe** | C at the bar. Sheriff only. Uses your action and gives a private report. |
| **Pass** | Space gives up your action for this round. |

After pouring, choose B to drink, aim + G to give it away, or Space to leave it.
You have six seconds to choose; unclaimed beer stays at the bar without healing.
Fill near the line for 20% healing. Partial pours round up in 5% steps: 1 → 5, 11 or 12 → 15, and 17 → 20.
Overflow wastes beer. The tap closes automatically after 4.5 seconds.
Full-health players can watch, move and use another action. Injured players may skip pouring and shoot instead.
Hold the actual tap handle to pour and release to stop. Scroll while holding it to open or close the valve and change the real flow rate. Click the filled mug to drink.
Mouse look stays active normally at the Bar; no right mouse hold or beer pop-up is used.
To give beer, aim at the intended recipient before pressing G.

The beer travels for 0.65 seconds.
The recipient then drinks automatically for 2.5 seconds.
The pour already used **your** action; the handoff costs no additional action. The recipient keeps their action, but their hands stay occupied until the drink finishes.

A recipient at full health still drinks.
Maximum health does not increase.

Drawing or holstering with **Q** has no action cost.
It occupies your hands for 0.25 seconds.
A click during a permitted draw stores one shot until drawing finishes.
This also applies at the bar and after a minigame.
The game checks turn permission again before firing the stored shot.

Looking, speaking and walking have no action cost. Pouring uses your action.
A busy player cannot start another action.
A beer transfer fails without resource use if the recipient is busy, carries beer or is out of range.
An obstruction also prevents transfer.
It also fails if the recipient is in another room.

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

## Robbery: why stay at the bar?

Press **H** during room actions to work on the safe for **three seconds**.
The work uses your action.
Three completed work actions open the safe.
Progress is shared and stays between rounds.
Work starts in turn order, but attempts that have already started can overlap.
The player who completes the third step collects the money.

Only Outlaws can work on the safe or collect the money.
H and X are unavailable to the Sheriff, Deputy and Renegade.
Rejected H and X commands do not use their action.

A shot interrupts safe work and gives no progress for that attempt.
The action stays used.
Work on an empty safe still uses your action and the work time.
The result reports only that no money was recovered.
H cannot show the money state without an action cost or identify the carrier.

If the carrier dies, the money becomes available at the bar through H.
The safe stays open.
The money does not become a loose object on the floor.

Robbery has no reaching or leaning animation, visible money bag, forced holster movement or sound cue.
It also has no global notification, role reveal or public action log.
Roommates receive no raw robbery, bag, escape or safe-work activity flags.
They can collect the private witness clue described above.
There is no automatic visual indication that identifies a thief.
The worker or carrier sees their own private status.

Safe progress is not public live HUD information.
The **Sheriff can press C at the bar** to inspect the safe as an action.
The private report shows a sealed, tampered or open safe with money inside, or money missing.
It never identifies the thief.

The display identifies the report as the **last inspection**.
Later theft does not update it automatically.
Other players do not receive the report or progress value.
There is no automatic global theft announcement.

## How do we win now?

**The Sheriff's death alone does not give the Outlaws victory**.
Outlaws must escape with the money.
The Renegade must eliminate the Outlaws, then the Law, and be the last survivor.

| Faction | Victory condition |
|---|---|
| Sheriff + Deputy (Law) | Eliminate all Outlaws and the Renegade. Alternatively, survive and prevent escape until the posse arrives after round 10. The Law cannot rob or escape. |
| Outlaws | Either Outlaw completes an escape with the money. Both Outlaws share the team victory. |
| Renegade | Eliminate the Outlaws first, then the Sheriff and Deputy. Be the only survivor. The Renegade cannot rob or escape with money. |

Escape requires two rounds:

1. Carry the money at the bar.
2. On your turn, press **X** to declare escape. This uses your action.
3. Declare by **round 9** to leave time for completion.
4. After regroup, select the **bar again** during discussion.
5. Stay alive at the bar with the money until the **next room-action phase ends**.

A successful side-room assignment cancels the escape attempt.
Discussion does not cancel it.
During the response phase, you can use your action to defend yourself.
Completion gives both Outlaws victory.

A non-lethal hit does not cancel escape.
Elimination cancels escape and makes the money available at the bar again.
After round 7, distant shots, approaching hoofbeats and a public countdown announce the posse.
The posse is the group that arrives to stop the criminals.
Rounds 8, 9 and 10 are full response rounds.
The posse surrounds the saloon after round 10's actions.

An escape completed in round 10 wins before the posse arrives.
Otherwise, surviving Law wins.
If no Law survives and no faction meets its objective, the posse captures the remaining criminals.
That result is a draw.
Results appear after the final aftermath animations.

A Renegade who is the sole survivor wins after the final-shot effects.
All roles are shown when the match ends.
Disconnected players keep their place for 20 seconds. The client automatically tries to reconnect to the same LAN or online room, retaining its role, health, inventory, position and round participation. The match continues during this grace period. If a player does not return in time, the match is aborted and the reserved place is released.

Results show the outcome and every player’s role and survival status. Each human player can vote for a rematch or withdraw their vote. Unanimous votes start another match in the same room; bots do not vote and reserved disconnected players block the next match. **Return everyone to lobby** lets any player bring the whole group back to the existing lobby to change settings or wait for players. Reconnecting requires the original running client; restarting the app or losing the host is not supported.

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
A perfectly poured drink restores health to 40%; a 5% pour restores it to 25%.

A lethal headshot cancels drinking. Beer cannot revive a player.
Damage still interrupts safe work.

Mug movements and third-person gun grips use procedural poses.
Local playtest results can guide later improvements.

## What practice players do

Practice players use **complete role knowledge**, including hidden roles, to pursue team victory.
This aggressive test AI does not simulate human deduction.

- They use the bar and side rooms and change plans each round.
- They accept a human's room invitation unless they carry money.
- A carrier stays at the bar.
- Law players shoot criminals and protect the money.
- Only Outlaws work on the safe, collect money and declare escape.
- Outlaws can shoot to defend themselves and treat each other as allies.
- The Renegade targets living Outlaws first, then the Law.
- The Renegade never robs or declares a money escape. Its objective is to be the last survivor.

Practice players aim actual shots, draw, reload and take or drink beer when useful.
They pass when no useful action is available.
They briefly keep a fixed aim point before firing, so movement can avoid a shot.
They receive no extra actions or HP.
Their bullets cannot pass through walls.

Practice players stand and move sideways around opponents during room actions.
They can move while waiting or after using their action.
They avoid furniture and room boundaries, change direction and sometimes stop.
Drinking, beer transfers and safe work stop their movement.
They return to their seats for discussion.

Practice players are silent.
They have no proximity conversations, accusations or false claims.
They can act faster and aim more accurately than humans.
Their difficulty is not balanced for normal play.

## Controls and voice

| Input | Function |
|---|---|
| Hold Control | Crouch; release to stand when there is room overhead. |
| F / WASD | Stand or sit near an empty seat / walk and dodge inside your room during actions. |
| Mouse | Look / aim. |
| Right-click during discussion | Open or close the room planner. |
| Q / left click / R | Draw or holster / shoot / reload. |
| E / B / G | Pour or stop at the bar / drink poured beer / offer it to the player at your aim point. |
| H / X (Outlaw only) | Work on the safe or collect money / declare escape. |
| Space / C | Set or clear Ready during discussion / inspect the safe as Sheriff at the bar. |
| Space during actions / hold Tab | Pass / read your private role and objective. |
| Left / Right arrows | Change the living player's view in spectator mode. |
| Hold V / M | Send microphone audio / toggle microphone mute. |
| Esc | Open the menu, host settings, voice devices and player list, or leave. |

Proximity voice connects when you join.
It has full volume within two metres and fades by 10 metres.
Walls do not yet reduce voice volume.
Nearby indoor rooms can hear each other.
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

## Next playtest priorities

1. Run a complete session with five humans.
2. Measure waiting time, confusion, winners and reasons for victory.
3. Adjust turn, discussion and aftermath durations from the observations.
4. Improve spectator views and initial control instructions.
5. Test how inspection reports, invitations and conflicting statements help players identify suspects.
6. Keep the thief's identity private unless players infer it from evidence.
7. Balance safe work and escape requirements.
8. Reduce practice AI role knowledge for normal practice.
9. Improve seated poses, hand grips, hit reactions and sound levels after the rules stabilize.
10. Improve networking with build-version checks, disconnect handling and reconnection support (20-second reconnection is implemented; host migration remains future work).

Run **Saloon → Verify posse arrival in Play mode** to check posse timing and victory priority.

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
Permanent control hints and voice-status text are absent from the HUD.

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
Standing uses a separate input debounce, so F works immediately after a shot.
Coin Flip and Blackjack seats stay locked only until their minigame finishes.
Either player can then stand and dodge, including after using their action.

Other players' footsteps use positional audio.
Your own footsteps stay centered.
Seated, stationary, eliminated, stale and teleporting characters do not trigger footsteps.
The bundled font licenses are in `Assets/Resources/Fonts`.


Revolver shots, reload, drinking and approaching-posse audio in the development source now use CC0
recordings from Freesound. Source links, authors and processing notes are in
[asset sources](Documentation/ASSET-SOURCES.md#recorded-sound-replacement--2026-11-11).
