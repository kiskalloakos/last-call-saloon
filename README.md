# Last Call — local development build

New player? Start with [Your role, in simple words](#your-role-in-simple-words).

Test release **0.2.10**, 2026-11-08, packages the current game for Windows x64 and macOS
(Intel + Apple Silicon). All players must use **0.2.10** together.

[Download Windows and Mac](https://github.com/kiskalloakos/last-call-saloon/releases/tag/v0.2.10).
Extract the whole ZIP. Windows: open `Last Call.exe` beside its data folder.
Mac: open `Last Call.app`. The Mac app is not notarized; use the system's Open option
if macOS asks to approve the downloaded app.

This release includes Ready-based discussion/planning, four/five-player matches with
optional bots, the shared Western character, headshots-only combat, free room movement,
bots that stand and dodge, beer healing through non-lethal hits, ragdolls, spectator POVs,
updated Tab role guides, smoother revolver motion and the rebuilt bar.

Room and companion choices now update immediately and keep your latest clicks while
waiting for the host. A delayed companion click no longer resends an older room choice.
If a connection ends, the main menu offers **COPY DISCONNECT DETAILS** with the transport
reason and build ID. The reported Windows return-to-menu has not been reproduced or
confirmed fixed; this is a test release for the repaired planner and improved diagnostics.

## Four devices on LAN

1. Put all four computers on the same home/local network. Avoid guest Wi-Fi that isolates devices.
2. Run **0.2.10 on every computer** and choose **LOCAL / LAN**.
3. On one computer choose **NO BOTS**, keep port **7777**, then click **HOST LAN**.
4. On the other three, enter the host computer's local IPv4 address (often `192.168.x.x`),
   use the same port **7777**, and click **JOIN LAN**. Do not use `127.0.0.1` on another computer.
5. If prompted by Windows Firewall, allow Last Call on your private/home network.
   On macOS, allow incoming connections for Last Call if the firewall asks.
6. When the lobby shows four people, the host starts. No fifth player or bot is required.
7. Each person chooses their plan and clicks **Ready**. All four must be ready to begin room actions.

The four roles are Sheriff, Deputy, one Outlaw and one Renegade. Game traffic uses UDP;
Vivox voice also needs internet. A four-device Wi-Fi/firewall setup still needs testing
on those actual devices; automated LAN checks do not verify the router or each device's firewall.

Everyone now uses the same **Gunman** from [Low Poly Western Starter Pack by tt-3D](https://assetstore.unity.com/packages/3d/characters/low-poly-western-starter-pack-383104),
with the original textures. Character motion includes walking, a bent-leg seated pose,
revolver aiming/recoil and drinking with the mug lifted to the mouth. Motion blends
between standing and sitting; it does not squash the character. Ragdoll deaths still work.
While someone talks through in-game voice, their mouth flaps unevenly and the lower face
moves with voice activity and volume. It closes during silence, mute, drinking or death.
This is a simple cartoon effect, not precise lip sync.
The pack supplies the rigged model; the game supplies these movements. The Asset Store
content uses Unity's Standard Asset Store EULA, rather than the old character's CC0 license.

Discussion, room planning and companion invitations are now one panel. Choose your plan
and click **Ready** (or Space). Every living player must be ready; there is no discussion
countdown or separate room-selection timer. Editing a plan clears that player's Ready.
Hold the right mouse button to look around while planning. Practice players ready themselves.
Practice players spread between the bar and side rooms, rotate plans each round, and
accept your invitations unless carrying the money. Friendly fire costs the shooter 1 HP
as well as the target's normal 1 HP damage: Sheriff/Deputy and Outlaw/Outlaw are allies.
The public Sheriff is always identified at the top left, including “YOU” for the local
Sheriff and an eliminated status after death. Hold Tab to check your own private role,
including while the discussion/planning panel is open, without holding right mouse.
The Tab card gives a plan for your own role: protection and support for Sheriff/Deputy,
the H/X robbery-and-escape walkthrough for Outlaws only, and Outlaws-first combat for the Renegade.

Eliminated players follow living players' first-person views. Use **Left/Right arrows** or
the spectator buttons to change POV. Hidden roles remain private and dead voice stays separate.
The first-person revolver is shown without a hand, with a short muzzle-up recoil,
a smooth backward roll and a gradual return to rest. The third-person revolver follows
an arm pose with a neutral wrist and a fitted closed hand around the handle. The fingers
keep their original joint rotations to avoid the stretched mitten shape.
When someone takes a headshot, their torso and head flinch away from the hit, briefly flash warm
red and settle back into position. A lethal shot still launches the ragdoll.
Two-player room openings face the players
toward each other, including at the bar.

Choose **No bots** or **Bots** when hosting LAN. In the lobby, the host can change this
and choose a **4-player** or **5-player** bot fill target. Bots fill missing seats only
when the host starts. With **No bots**, four humans can start immediately; a fifth
person is optional. Every human in the lobby joins the match.

| Match size | Roles |
|---|---|
| 4 | 1 Sheriff, 1 Deputy, 1 Outlaw, 1 Renegade |
| 5 | 1 Sheriff, 1 Deputy, 2 Outlaws, 1 Renegade |

A lobby holds **up to 8 human players**, including the host. Match starts currently
require **4 or 5 players**; rules for 6–8 will be added later. A ninth connection is
rejected without removing anyone already in the lobby. Friends can replace bots
after a match ends. Bot settings cannot change during a match.

## Your role, in simple words

**Hold Tab to see your role and goal.** This also works while Discuss & Plan is open.
The top-left corner always shows who the Sheriff is. All other roles are secret.

At the start of each round, choose a room and, if you want, someone to meet there.
Click **Ready** or press **Space**. The round starts when every living player is ready.
Then wait for your turn. You get **one action per round**: for example, one shot,
one robbery job, one inspection or one drink.

### Sheriff — protect the money

- You are on the same team as the **Deputy**. Everyone knows you are the Sheriff,
  but you do not automatically know who the Deputy is.
- Choose **Bar** to watch the safe. On your turn, press **C to inspect it**.
  Only you see the report: money inside, signs of robbery, or money missing.
  It does **not** tell you who stole the money. Press C on a later turn to check again.
- To shoot a suspected enemy, press **Q to draw**, wait for the gun to come up,
  aim at their head with the mouse, then **left-click to shoot**.
- You win together with the Deputy if you stop the money escaping until the last round
  and at least one of you is alive, or if you eliminate every Outlaw and the Renegade.
- **H and X do not work for you.** Only Outlaws can rob, collect money or escape with it.
- Shooting the Deputy also takes **1 HP from you**. HP means health.

### Deputy — help the Sheriff

- You and the **Sheriff** are a team. Your role is secret; the Sheriff does not
  automatically know that you are helping.
- Help protect the money. Choose **Bar** to stay near the safe, or invite someone
  to a side room if you want a private conversation.
- On your turn, press **Q**, aim, then **left-click** to shoot an enemy.
- **H and X do not work for you.** Protect the money; you cannot rob or escape with it.
- **C does not work for you.** Only the Sheriff can inspect the safe.
- You share the Sheriff's winning conditions. You can still win if the Sheriff dies.
- Shooting the Sheriff also takes **1 HP from you**.

### Outlaw — steal the money and escape

- In a **4-player match**, you are the **only Outlaw**: escape with the money to win.
- In a **5-player match**, there are **two Outlaws**. If either escapes with the money,
  **both Outlaws win**. The game does not tell you who your teammate is.
- Choose **Bar**. On your turn, press **H to work on the safe or collect its money**.
  Follow the robbery and escape steps below.
- Once you carry the money, use a later turn to press **X to start your escape**.
  Stay at the bar and survive the next round's room actions to finish escaping.
- Use **Q**, aim, then **left-click** if you need to defend yourself.
  Shooting another Outlaw also takes **1 HP from you**.
- Killing the Sheriff does **not** win the game. Your team needs to escape with the money.

### Renegade — eliminate the Outlaws, then the Law

- You have **no teammate**. Your goal is to be the **last player alive**.
- Fight the **Outlaws first**. Keep the Sheriff and Deputy alive while they help
  fight the Outlaws. Once the Outlaws are dead, turn on the Sheriff and Deputy.
- Choose a room with a suspected Outlaw. On your turn, press **Q**, aim, then
  **left-click** to shoot. Use **R** to reload on a later turn if needed.
- At the bar, **E** takes a beer for free; **B** drinks it to recover 1 HP as your action.
- **H and X do not work for you.** You cannot rob, collect money or escape with it.
  You win when everyone else is dead, not by stealing money.

### H and X: how to take the money and escape

These buttons work for **Outlaws only**, at the **Bar** during room actions.
Sheriff, Deputy and Renegade cannot rob, collect the money or escape with it.

1. On your turn, press **H**. Wait **3 seconds** for the safe job to finish.
   Getting shot interrupts it. This uses your action for the round.
2. The safe needs **three completed jobs in total**, across any players and rounds.
   Whoever finishes the third job gets the money. You cannot do all three jobs in one turn.
3. Once you carry the money, wait for a **later turn** and press **X** to start escaping.
   You cannot start an escape in the final round, because you need one more round to finish it.
4. In the next Discuss & Plan, choose **Bar again**, then Ready.
   Going to a side room cancels your escape.
5. Stay alive with the money until that round's room actions finish. **The Outlaw team wins** (just you in a 4-player match).

If the carrier dies, the money goes back to the open safe. In a five-player match,
the other Outlaw can choose Bar and use **H** on their turn to collect it.
They do not have to open the safe three times again.

### Useful buttons for every role

| Button | What it does |
|---|---|
| Hold **Tab** | See your own role and goal. |
| **F**, then **WASD** | Stand, walk and dodge during room actions. F near an empty seat sits down. |
| **Q**, then **left-click** | Draw your gun, aim at a head, then shoot. One shot uses your action, even if you miss. |
| **R** | Reload your drawn gun. Uses your action. |
| **E** at the bar | Take one beer before spending your action. Taking it is free. |
| **B** | Drink your beer to recover **1 HP**. Uses your action. |
| Aim at a nearby player, then **G** | Give them your beer; they drink it automatically. Uses your action. |
| **Space** during Discuss & Plan | Ready or unready. |
| **Space** during your action turn | Pass: do nothing and give up this round's action. |
| Hold **V** | Talk through your microphone. |
| Hold **right mouse** during Discuss & Plan | Look around while the planning panel is open. |

Everyone starts with **3 HP**. Only a **headshot** removes **1 HP** from the target. A friendly headshot
also removes **1 HP from the shooter**: Sheriff and Deputy are allies, and the two Outlaws
are allies. At **0 HP**, your body flies back, falls and rests on the floor. After a short
death-camera view, you watch other players. Your body disappears when spectator mode starts.

## Start testing in Unity

For a fresh source checkout, first import the licensed Western Starter Pack from your
Unity account and run **Saloon → Prepare Western Gunman character**. The imported pack
and derived meshes are excluded from the public repository; compiled downloads include them.

Open this **SaloonPrototype** project, open `Assets/Scenes/Saloon.unity`, then press
**Play → Solo Practice**. Four active practice players fill the other slots. They know
roles and aim accurately: they are opponents, not passive targets or conversation bots.
They still obey the host's one-action budget, ammunition, health, rooms and raycast hits.

A real match needs four or five people with **No bots**, or at least one human with
**Bots**. Host LAN and join using the host's local IP and matching
UDP port (7777 by default), or create/join an online room code. All devices must use the
same source/build version: use matching 0.2.10 clients for this playtest. Mixed versions are rejected with a
clear message. A disconnect reason stays on the menu instead of being silently replaced.

## Interface (Unity development version)

Health sits at the lower left as separate HP segments and a count; ammo sits at the lower right.
Room, partner, Ready and turn prompts stay at the right edge. **Sheriff: hold Tab to see your private clues alongside your role**, during discussion or room actions. Scroll to read older clues while holding Tab. Other roles can open **Clues** at the upper left during discussion.
Hold **Tab** for role help; **Esc** opens lobby/settings. The in-game interface uses compact,
flat slate controls with no large persistent HUD panels.

## Private discussion clues (Unity development version)

- At the Bar during room actions, keep someone working the safe in view for about **one second**. If you can see them clearly, your notebook records their name and round. Looking away, walls, other rooms and death prevent this. It does not say whether they finished or got any money.
- **Sheriff: press C on your turn at the Bar.** Your inspection records the safe's condition, whether new tool marks appeared since your previous check, and the round of the latest completed safe work. It never names the thief. An interrupted attempt leaves no completed-work marks.
- **Sheriff: hold Tab** to read **YOUR CLUES · PRIVATE** on your role card, at any time while alive. Other roles use the discussion Clues drawer. It holds your six latest clues, newest first. Scroll to read older ones; they stay through rounds and reset for a new match.
- Hold **V** to tell the table what you found. Clues are not shared automatically. You can keep quiet or bluff; nobody else can open your notebook. Reports are snapshots of what you knew then, not live updates.

## What happens in a round

| Phase | Default length | What happens |
|---|---:|---|
| Discussion + planning | Until every living player is Ready | Talk, pick a room and optional companion, then Ready. Editing your plan clears Ready. Major actions are locked. |
| Room actions | About 30 seconds, extended for setup/turn budgets | Everyone gets **one** action, following the room's coin/turn order. Different rooms proceed independently. |
| Aftermath | 3 seconds | Actions lock; players stay in their rooms while shots, hits and gun spins finish. |
| Regroup | 3 seconds, between rounds | Everyone returns to the common table; the next discussion begins. |

During room actions, press **F** to stand or sit near an empty seat and **WASD** to walk.
You can move throughout room actions, including while waiting or after spending your action.
You stay inside your assigned room; discussion and regrouping remain seated. A new
round preserves roles, HP, ammunition, items, safe progress and pending escape attempts.
Actions can end early when all living players finish/pass; started timed actions finish
before aftermath. Six rounds maximum, configurable before starting the match. The final
round ends after aftermath, without an unnecessary regroup. Elimination/escape victories
also wait through aftermath before revealing roles/results. A lethal hit throws the body
into a ragdoll fall; the dying player briefly sees that fall before switching to a living POV.

Each side room has two places. A requested pair must nominate **each other and the same
room**. Mutual pairs receive priority; solo requests fill remaining places. Unaccepted
invitations go to their chosen room alone when space remains. Only capacity shortages
fall back to the bar, with an explicit message. Confirmed pairs retain priority and stay
together. The bar can hold everyone.
The assignment is locked during actions; the HUD/pause roster shows the actual room.

Room and companion choices stay visible beside the discussion view. Incoming invitations
identify the player and requested room; clicking one accepts both. Ready starts room actions
as soon as every living player has confirmed. Hold right mouse to look around while planning.

### Two-player rooms: coin for the opening

Any room with exactly two living players (including the bar) starts with random opposite
Heads/Tails assignments. Both can choose a side; the **first claim locks it**, giving the
other player the opposite side. With no input, the assignments auto-flip after three seconds.
The host rolls the coin after a 1.2-second flip; the result and opening player are visible
only to that room. Major actions and drawing are blocked until the result.

The winner gets the first move: Q to draw, then click to shoot, or another legal action/pass.
The opponent can respond once the winner spends their action; both keep a one-action budget.
Ignoring the opening automatically passes it after six seconds (shorter for short action
windows), and an eliminated opener cannot block the survivor. This determines action
**order**, not a mandatory shootout. The shared action deadline includes 4.2 extra seconds
for coin setup, then still waits for already-started timed actions.

### Larger rooms: ordered turns

The bar with **three or more living players** uses a freshly randomized queue each round.
The HUD shows the active player, their timer and the room order. Each player may start one
major action or pass; the next player then gets their turn. Q/drawing and major actions are
blocked outside your turn, including for the Sheriff and practice bots. Everyone can still
look, talk and prepare one beer while waiting. Finished players see “Action spent,” not
a promise of another turn.

Each turn gets the action-window duration divided by the number of occupants, with a
ten-second minimum. Every waiting player gets a fresh window when their turn begins,
even after a late host frame. An idle turn auto-passes; dead players are skipped. The
overall room-action deadline expands to preserve waiting turns. Started timed actions may overlap later turns and finish before
aftermath; the turn order controls who **starts** an action, not a guaranteed safe healing period.

## What can I do with my one action?

- **Shoot:** one aimed bullet. Missing still spends the action and ammunition.
- **Reload:** refill the six-shot cylinder; 3.5 seconds, unlimited reserve ammunition.
- **Prepare beer (free):** E at any bar station; shared stock starts at eight, carry one.
  One pickup per round, before spending your action; taking beer preserves your action.
  Press B to drink/heal in the same round, or G to give it away as your action.
- **Drink:** B consumes your carried beer; 2.5 seconds, restores one HP, capped at three.
- **Give beer:** aim at a nearby, living, same-room player with free hands and press G.
  Your beer travels to them over 0.65 seconds, then they automatically drink for 2.5 seconds.
  This spends **your** action, not theirs; their hands are occupied until the drink ends.
  A full-health recipient still drinks, so do not expect extra/max health.
- **Work the safe / collect the money:** Outlaw only, H at a bar station; see robbery below.
- **Declare escape:** Outlaw only, X at a bar station while carrying the money; see victory below.
- **Inspect safe:** Sheriff only, C at the bar; costs your action and gives a private report.
- **Pass:** Space gives up your action this round.

Drawing/holstering (Q) is free preparation, but occupies your hands for 0.6 seconds.
Looking, speaking and preparing a beer are free. Busy players cannot start another action. Beer handoffs
fail without spending resources if the intended receiver is busy, already carrying beer,
out of range, obstructed or in another room.

Everyone starts at three HP. **Only headshots deal damage: 1 HP.** Body hits and misses
still use one bullet and your action, but cause no damage or friendly-fire penalty. Zero HP
means **eliminated**, not downed: no movement/actions, no mid-match respawn or revival.
Eliminated players use a separate voice channel. A new match resets everyone.
Ragdolls land on the floor and rest briefly, then disappear with their name labels and
physics when the host switches that player to observer mode, about 3.2 seconds after death.
The dying player's camera follows their body during that time. Later room assignments
can use the freed seat; common-table seating keeps each surviving player's usual place.

## Robbery: why stay at the bar?

Press **H** during room actions to work the safe discreetly for **three seconds**.
It costs your action. Three completed work actions open it; progress is shared and persists
between rounds. Work begins in turn order; already-started attempts can overlap. The player completing the third
step collects the money. Only Outlaws can work the safe or collect the money. Sheriff,
Deputy and Renegade are blocked from both H and X without spending their action.

A shot interrupts work and gives no progress for that attempt; the action stays spent.
Working an empty safe still takes your action/time and reports only that nothing was
recovered; pressing H cannot reveal money state for free or identify the carrier.
When the carrier is eliminated, the money becomes collectible at the bar again with H;
the safe remains open. It does not become a loose object you have to walk over to pick up.

Robbery has no reaching/leaning animation, visible money pouch, forced holster, sound cue,
global notification, role reveal or public action log. Roommates receive no raw robbery,
bag, escape or robbery-busy flags; they may collect the private witness clue described above, but cannot read
an automatic visual confession. The worker/carrier still sees their own private status.
Safe progress is no longer live public HUD information.

The **Sheriff can spend an action pressing C at the bar** to inspect the safe. The private
report says sealed/tampered/open with money inside, or money missing; it never names the
thief. The displayed report is explicitly the **last inspection**, not live telemetry:
later theft does not silently update it. Other players do not receive that report or
the progress value. There is no automatic global theft announcement.

## How do we win now?

**Killing the Sheriff alone is not an Outlaw victory.** Outlaws must escape with the money.
The Renegade has a separate goal: eliminate the Outlaws, then the Law, and be the last alive.

| Faction | Victory |
|---|---|
| Sheriff + Deputy (Law) | Eliminate all Outlaws/Renegade, or keep money from escaping through the final round while at least one Law player lives. They cannot rob or escape. |
| Outlaws | Either Outlaw successfully escapes with the money; both share the faction win. |
| Renegade | Eliminate the Outlaws first, then the Sheriff and Deputy. Win as the only survivor. Cannot rob or escape with money. |

Escape is a two-round commitment:

1. Carry the bag at the bar and spend an action pressing **X**. You cannot declare during
   the final round because another response round is required.
2. Regroup, discuss and choose the **bar again**. A successful assignment to a side room
   cancels the attempt. Discussion does not cancel it.
3. Survive the **end of the next room-action phase**, still at the bar with the money.
   That resolves the escape and both Outlaws win. You may spend your action
   defending yourself during that response phase.

A hit alone does not cancel escape; elimination does. Eliminating the carrier makes the
money available at the bar again. At the six-round limit, surviving Law wins if no escape
has resolved. A Renegade who is the sole survivor wins immediately, after the final-shot effects.
Otherwise, if no Law remains and no Outlaw escaped by the round limit, the result is a draw. All roles
are revealed when the match ends. A mid-match disconnect aborts it.

## Beer animation and audio

Both carried/self-drunk and gifted beer are visible as mugs. Drinking raises and tilts
the mug toward the mouth in third person, with the skinned arm reaching for its handle,
and toward the camera in first person. A gift
flies in an arc from giver to recipient, then the recipient drinks automatically. Gulping
plays during the sip; a quiet clink accompanies handoff. These are original procedural
alpha sounds, not recorded samples. The revolver has a layered crack/body/metal/room gunshot and a short flourish after each
accepted shot, visible in first and third person. The spin is cosmetic and grants no extra shot.
Shooting adds a brief camera kick; receiving damage adds a short shake and red tint.
Both viewpoints show a short muzzle burst/light. Camera feedback is cosmetic and does
not alter the host's aiming ray or add movement to your seated position.

A non-lethal headshot does **not** cancel a normal or gifted drink. Healing applies when
the drinking timer completes: 2 HP → shot to 1 HP → beer finishes at 2 HP. A lethal
headshot cancels drinking; beer cannot revive someone. Damage still interrupts safe work.
Mug animations and third-person gun grips are procedural poses that can be refined from local playtest feedback.

## What practice players do

Practice players pursue faction victory using **perfect role knowledge**, including hidden
roles. This is deliberately aggressive test AI, not a simulation of human deduction.

- Spread between the bar and side rooms, and change their plans each round. They accept
  a human's room invitation when not carrying money. A carrier stays at the bar.
- Law shoots criminals and protects the money. Only Outlaws work the safe, collect money
  and declare escape; they can also shoot to defend themselves. Outlaws treat one another as allies.
- The Renegade targets Outlaws while any are alive, then targets the Law. It never robs
  or declares a money escape, and aims to be the last survivor.
- Aim real shots, draw, reload, take/drink beer when appropriate, and pass when no useful
  action is available. They commit to an aim point briefly before firing, so movement can dodge a shot. They
  receive no extra action, HP or wall-penetrating bullets.
- Stand and strafe around opponents during room actions, including while waiting for
  their turn or after using their action. They avoid furniture and room boundaries,
  change direction and sometimes pause. Drinking, handoffs and safe work pause movement.
  They return to their seats for discussion.
- They are silent: no bot proximity conversation, accusations or bluffing yet. They may
  act faster/more accurately than humans and are not balanced opponents.

## Controls and voice

| Input | Effect |
|---|---|
| F / WASD | Stand or sit near an empty seat / walk and dodge within your room during actions |
| Mouse / right mouse | Look / narrower aiming FOV |
| Q / left click / R | Draw or holster / shoot / reload |
| E / B / G | Take beer at bar / drink / give beer to aimed player |
| H / X (Outlaw only) | Work safe or collect money / declare escape |
| Space / C | Ready or unready during discussion / Sheriff inspection at the bar |
| Space during actions / hold Tab | Pass / privately read role and objective |
| Left / Right arrows | Switch living player POV while spectating |
| Hold V / M | Push-to-talk / toggle mic mute |
| Esc | Menu, host tuning, voice devices, roster, leave |

Proximity voice connects on joining. It is full-volume within two metres and fades by
10 metres. Walls do not yet muffle sound: nearby rooms can overhear each other. Both LAN
players need internet for Vivox voice; game networking itself can run on LAN. Use headphones
for two nearby laptops. Microphone selection/echo testing and individual mutes are in Esc.

## Validation and scope

Use **Saloon → Verify rounds in Play mode** from Edit mode for host-command, room, robbery,
privacy, escape, practice AI and beer tests. **Verify rules and asset setup** checks role/win
rules and imports; **Verify low-poly art** checks model/material availability. No new player
build is generated by those checks. Tests and limits are recorded in
[VERIFICATION.md](Documentation/VERIFICATION.md).

Only the Sheriff is publicly identified during a match. Each player knows their own role;
Outlaws do **not** receive an ally name. All roles reveal after the match. A quiet line at
the bottom gives private phase/role-specific suggestions; it is not a public event log.

The host runs the simulation and sends per-player snapshots. There is no host migration,
reconnect-in-progress, matchmaking, prediction, or production anti-cheat. Bots deliberately
have private role access on the host. This is a trusted private alpha.

Art provenance/licenses: [ASSET-SOURCES.md](Documentation/ASSET-SOURCES.md).
Old design notes: [DESIGN-HISTORY.md](Documentation/DESIGN-HISTORY.md).
Parent Project-IDK is separate and is not modified by this prototype.

## Next playtest priorities

1. Run a full five-human session: measure waiting, confusion, who wins and why; tune turn,
   discussion and aftermath lengths from observations.
2. Give eliminated players a useful spectator view and make the first-time controls clearer.
3. Improve deduction: test how inspection reports, invitations and conflicting testimony
   create believable suspicions; avoid mechanically revealing the thief.
4. Balance the safe/escape economy and soften perfect-knowledge practice AI for normal practice.
5. Polish seated poses, hand grips, hit reactions and sound levels after the rules settle.
6. Harden networking with build-version checks, disconnect handling and reconnect support.
