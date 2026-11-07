# Last Call — saloon prototype

A separate Unity **6000.5.10f1** project for a five-player, first-person social shooter.
This prototype does not modify the parent project's assets, scripts, scenes, or settings.

## Current source: seated room rounds

Discussion (30s) → room/companion choices (15s) → shared room actions (30s maximum) →
regroup (3s). Players are seated at a common table during discussion, automatically moved
to fixed room positions for actions, and returned together afterward. Walking and standing
are rejected by the host throughout a match; looking, aiming and voice remain available.

During room choice, click Bar / West room / East room and optionally a companion. A pair
must choose each other and the same side room. Side rooms hold two; unmatched/full room
requests default to the bar. Choices lock at the deadline. Solo practice bots accept your
invitation automatically; other bots fill side rooms so you can inspect the layout.

Everyone can spend one action in the shared action window: one bullet, reload, take beer
at a bar station (E), drink (B), give beer to a nearby room companion (G), or pass (Space).
Completed/passed actions can end the phase early; timed drinks/reloads finish before regroup.
HP, ammunition, items, roles and beer stock persist. Existing victory rules still apply;
robbery and replacing the Sheriff-death objective are separate future work.

Open the Saloon scene, press Play → Solo Practice. Use Saloon → Verify rounds in Play mode
for automated host-command/state checks. No new standalone build has been made.

Visual fixes: preserve imported FBX rotations and normalize using a separate parent;
correct upright furniture; remove the stray bench/wall pieces; hide the beer box renderer
and show a mug at the beer station. Fixed seat positions replace the old scattered layout.

## Latest art and flow direction

The local beta art pass replaces placeholder furniture and revolver visuals, adds visible
beer mugs and a safe, removes floating environment instruction labels, and simplifies
the HUD. Rigged first-person arms are imported but still need grip/action animations.
No new player build is made. See [art provenance](Documentation/ASSET-SOURCES.md).

After testing, the owner reports that free movement and rapid solo turns do not feel
like rounds. The recommended replacement is **seated discussion → room agreements →
fixed room stations with a shared one-action window → regroup**. This is a proposal,
not yet implemented: see [the room/round route](Documentation/ROOM-ROUND-PROPOSAL.md).
The sequential-turn design below is historical; the current source uses the seated room cycle above.

## Design status — 2026-11-07

This README records both the working prototype and the next design proposal.
**Discussion and one-action turns now work in local Unity source. Room assignments, robbery, and downing remain proposals. No new standalone build has been made.**

| Status | What exists |
|---|---|
| Released in 0.1.2 | First-person movement/shooting, saloon/porch/back room, seating, beer pickup/sharing/healing, hidden roles, proximity voice, private online rooms, LAN and practice modes, Windows/Mac builds, no hats |
| Local source additions | Public HP dots under names; repeating discussion phases and one-action turns, with host enforcement, timers, passing, and a round cap; these are not in the 0.1.2 downloads |
| Current source match flow | Private roles → 30-second discussion → sequential turns of up to eight seconds → next discussion; six rounds maximum; existing assassination/elimination wins, or a draw at the round cap |
| Proposed next design | Negotiated room assignments and witnesses; a robbery objective; downing/recovery before permanent elimination |

The game should preserve the feeling of standing in a saloon, talking over proximity
voice, watching other people's behaviour, and deciding whether to help them with a beer.
We want the structure of rounds to make those decisions matter while retaining direct
movement, aiming, cover, and physical interactions.

The owner is open to changing the victory objectives and has requested a round system.
The robbery, action budget, timings, and recovery rules below are recommendations to
refine together, rather than settled implementation requirements.

### Problem observed in the current prototype

The Sheriff is a known target with three HP, and every player starts with a six-shot
revolver. An Outlaw can kill the Sheriff in a short burst and immediately end the match.
The opening weapon lock delays that attack but does not change the incentive.
Adding more discussion time alone would leave the same dominant opening.

## Implemented round test — Unity source only

Open `Assets/Scenes/Saloon.unity`, press **Play**, then choose **Solo Practice**.
Four inert targets fill the remaining slots and automatically pass after about 0.75 seconds.
The discussion lasts 30 seconds; when the HUD says **YOUR TURN**, take one action or
press **Space** to pass. A turn times out after eight seconds if you do nothing.
The order is randomized once per match and repeated; **Esc** shows the order.
To shorten a solo test, edit **Saloon Prototype → Tuning → Discussion Seconds** before Play.
The host can also change discussion/turn lengths, round limit, and beer supply between matches.

Implemented major actions: **one bullet**, reload, take a beer, drink a beer, give a beer,
or pass. A missed bullet spends the action. Invalid attempts (empty cylinder, full HP,
blocked beer handoff, wrong turn) spend no action or resources. Drawing/holstering,
walking, looking, seating, standing, and proximity voice remain available without spending
an action. Drawing still occupies hands for its existing short animation.
Reloads/drinks begun before the turn deadline finish before the next player's turn begins.
HP, roles, ammunition, carried beers, and the shared supply persist between rounds.
Eliminated players are skipped. The six-round cap is currently a **draw**.

**The old victory rules still apply in this test: killing the Sheriff can still end the
match.** Replacing those rules requires deciding the robbery objective below.
No recovery phase, downed state, room allocation, safe, money bag, or robbery action is
implemented yet. All existing 0.1.2 downloads still use continuous combat; use the Editor
for the new rules, and do not mix those builds with the new Editor in a network test.

Run **Saloon → Verify round clock** for deterministic timing/death/action-budget checks.
Run **Saloon → Verify rounds in Play mode** from Edit mode for actual host-command,
shooting, beer, reload, snapshot, and round-transition checks; it returns to Edit mode.

## Further round design — proposals beyond the source test

### Match, round, and turn

- **Match:** one complete game, with one assignment of hidden roles and one eventual winning faction.
- **Round:** a repeating discussion → action turns → recovery/checkpoint cycle within that match.
- **Turn:** one player's short opportunity to spend their action during the action phase.

Roles stay the same throughout a match. **HP, ammunition, carried items, safe progress,
and injuries carry between rounds.** There is no automatic respawn, full heal, beer
refill, or new role deal at a round boundary. A new match resets everyone.

### Starting values for the first round-based test

These values are hypotheses, not final balance decisions:

| Setting | First value to try |
|---|---|
| Human players | 5 |
| Match limit | 6 rounds |
| Discussion phase | 30 seconds per round |
| Action window | Up to 8 seconds per player; ends early after an action or a pass |
| Major actions | 1 per eligible player per round |
| Recovery/checkpoint | About 5 seconds |
| Turn order | Random seating order chosen once per match, then repeated in the same circular order |
| Health | Keep 3 HP initially |
| Beer | Keep the scarce shared supply; do not replenish it each round |

A full five-player cycle would take roughly 75 seconds at these maximum windows,
with a six-round match lasting roughly 7.5 minutes plus setup. Actual matches should
be shorter when people act quickly, pass, become incapacitated, or win early.

### How one round plays

| Phase | What players do | What the game permits |
|---|---|---|
| **1. Discussion and positioning** | Talk at the bar, split off for private conversations, watch the safe, negotiate help, and choose positions | Everyone can move, look, sit/stand, and talk; damaging and objective-changing actions are locked |
| **2. Action turns** | Each eligible player gets one action window in the fixed order | Only the active player can spend a major action; everyone can continue moving, taking cover, and talking |
| **3. Recovery and checkpoint** | Assess injuries, accuse or defend someone, and plan the next round | Resolve recovery deadlines and match limits; no free healing or extra attacks |
| **4. Continue or end** | Begin the next discussion, or see the result | Check victory after completed actions and at the final checkpoint; reveal all roles when the match ends |

The active player is identified clearly, with a small timer and an action indicator.
An inactive player still controls their character; they are not frozen in place while
someone aims at them. Proximity chat stays active throughout the round.

### One action should be a meaningful commitment

Recommended initial action list:

| Major action | Consequence |
|---|---|
| **Shoot** | Fire one aimed bullet; a miss still spends the action and ammunition |
| **Reload** | Refill the cylinder using the existing reload duration; no shot in that turn |
| **Take a beer** | Remove one beer from the shared supply and carry it |
| **Drink** | Consume a carried beer to restore one HP |
| **Give a beer** | Transfer your carried beer to a nearby player; receiving it does not spend their action |
| **Help a downed player** | Spend a beer and a vulnerable interaction to restore them to one HP |
| **Work the safe** | Complete an interaction that adds one step of safe-opening progress |
| **Handle the money** | Pick up or pass the money bag |
| **Attempt escape** | Announce an escape attempt with the bag and begin the response cycle described below |
| **Pass** | End your turn immediately without spending resources |

Walking, looking, speaking, sitting, and standing do not consume this action.
Drawing the gun is preparation; firing spends the action. Out-of-turn shooting,
healing, reloading, taking supplies, and moving the money are rejected by the host.
Actions keep their physical interaction distances and durations.

**One shot means one bullet, not a timed burst or an entire six-round cylinder.**
The physical cylinder may contain several bullets, but the action budget limits how
many can be used in a round. Hits continue to come from actual aiming and collision;
there is no random hit roll. Passive movement and cover provide counterplay without
introducing free counter-shots that would recreate unrestricted combat.

For the first test, let attacks occur in Round 1 after its discussion phase. If the
opening is still too abrupt, compare a non-violent first round as a separate variation.

### Rooms and witnesses — latest proposal, not implemented

The owner's proposal: negotiate who shares each room during discussion. An Outlaw
can keep the Sheriff talking elsewhere while their partner stays near the bar's till.
A player who also stays at the bar can witness the robbery and accuse them next round.
This provides concrete information to trade: access, whereabouts, overheard words,
visible actions, and conflicting accounts. Being near the till alone is suspicious,
not proof of a hidden role.

Suggested flow to discuss:

1. **Gather and discuss:** everyone can meet, accuse, negotiate partners, and nominate a room/companion.
2. **Commit room choices:** publish the final room roster before separating. A proposed pair requires both players to accept; players may instead choose the bar. No majority should be able to force an unwilling player into a private pair. Conflicting/unaccepted choices fall back to the bar, with a clear cutoff.
3. **Separate and act:** the bar and two side rooms break line of sight. For this experiment, stay in the chosen room during the action phase; otherwise everyone can rush back to watch the till. Walking, aiming and cover still work inside each room. This restriction is a proposal, not part of today's implementation.
4. **Regroup:** everyone returns for the next discussion and compares testimony. Room rosters are public; what happened inside a room is not globally announced.

Keep bar attendance unrestricted initially: choosing the bar must be a viable way to
watch the money or get beer, rather than an automatic criminal tell. A player may
choose to stay alone, but cannot demand that everyone else leave. This means the
Outlaws must persuade or distract potential witnesses instead of receiving guaranteed privacy.

**Witnesses:** only someone with line of sight to the hands/till sees the robbery
interaction. Do not send a global “X robbed the till” log or role reveal. A player
seeing the interaction can report it, lie about it, or be contradicted; the game does
not certify testimony. Make the interaction last several visible seconds and spend
one action per work step so being caught has consequences.
Anyone should be able to inspect or handle the till/money under the eventual objective
rules, including Law players, so “I was securing it” remains a possible explanation.

**Hearing:** normal proximity chat within a room; quieter/muffled voices through adjacent
walls and louder shouts that can carry farther. Players can overhear fragments without
seeing who handled the till. The current voice system has distance attenuation only;
wall muffling and distinct shout controls are not implemented. Room distances would
need to stay within audible range until that is changed.

A room choice supplies an opportunity/alibi, not an extra major action. Distracting the
Sheriff is ordinary conversation and persuasion. With the current sequential turns,
the Sheriff could use their turn to shoot the distracting player; social distraction
does not stun or mechanically disable them. Test whether visible turn ownership leaks
too much about who could have robbed. If it does, compare a later simultaneous action
phase where everyone still gets exactly one action, without changing this first test.

Open decisions: mutual nominations versus a group allocation vote, whether rooms lock
for the whole action phase, whether the Sheriff can inspect a different room by spending
an action, till versus safe placement, and what the stolen money actually lets a faction
win. These need playtesting before we implement room gates or replace victory rules.

### Robbery objective carried forward from the earlier proposal

Use one safe and one visible, transferable money bag in the existing saloon.
As a starting rule, the safe needs three completed work actions to open, and that
progress persists across rounds. Anyone can interact with the safe or carry the bag;
interacting with it should not function as an automatic role detector.

Someone carrying the bag can claim to be securing it for the Sheriff. Someone giving
beer can be helping an ally or buying trust. These claims can be checked against
visible actions and witnesses, without automatically proving allegiance.

Recommended proposed victory conditions:

| Faction | Proposed victory |
|---|---|
| Sheriff and Deputy | Secure the bag through a completed escape, keep it from escaping with a criminal through Round 6, or eliminate both Outlaws and the Renegade |
| Outlaws | Either Outlaw completes an escape with the bag; both Outlaws share that victory |
| Renegade | Personally complete an escape with the bag |

Allowing a Law player to secure the bag through the same escape interaction is a new
proposal here: it keeps that interaction available to everyone and makes the identity
of the trusted carrier consequential. We should decide whether to keep this early Law
victory or require Law to defend until the deadline before implementation.

**The Sheriff's death no longer ends the match by itself.** The Deputy can continue
protecting or recovering the money. A faction can still win after one of its members
has died; defending the money until the deadline remains a team objective.

An escape must give the other players a response:

1. On their turn, the carrier spends their action declaring an escape at the exit.
2. They must keep the bag and remain at the exit, conscious, through one full cycle of opponents' turns.
3. If they are still eligible at the start of their next turn, the escape succeeds and the match ends according to the carrier's faction.

Leaving the exit, losing the bag, or being downed cancels the attempt. The fixed circular
turn order ensures every other eligible player gets a chance to act before success;
crossing a discussion or round boundary does not skip anyone's response.
With the initial six-round limit, new escape attempts must begin by Round 5; Round 6
provides the final response cycle and resolves those attempts before the deadline.
Show that cutoff clearly and disable late declarations rather than accepting an
attempt that has no remaining opportunity to succeed.

### Injuries, beer, and staying involved

Proposed: reaching zero HP makes a player **downed** rather than immediately eliminated.
They drop the bag, cannot move or take major actions, and can still speak to living
players to ask for help. Their HP dots can show an empty, subtly pulsing state.

Helping costs another player's action, a beer, and several seconds with their weapon
holstered. It restores one HP, making beer and choosing whom to trust materially useful.

Give a downed player until the **end of the next round** to receive help, so someone
shot on the last turn still gets a full round of rescue opportunities. Otherwise they
are permanently eliminated and enter the existing dead-player voice channel.
Do not add an execution action to the first experiment; test the recovery window first.

### Example: a round with decisions rather than a burst of shots

During discussion, the Sheriff asks someone to watch the back exit. An Outlaw claims
to be the Deputy and offers to guard the money. On their turn, one player takes a beer,
another works the safe, and the Sheriff chooses between investigating that behaviour
and spending their action on a shot. If a player shoots, their attack is spent for the
round, giving others time to respond. On a later round, helping an injured player means
giving up a chance to shoot or advance the robbery. Once someone announces an escape,
the group has a complete response cycle to decide whether to protect or stop them.

### What we should settle and test together

- Does one action per player create worthwhile choices, or too much waiting?
- Are 30 seconds of discussion and eight-second turns appropriate for this small map?
- Should the first round allow attacks?
- Should Law be able to win early by securing the bag through the exit?
- Is three work actions the right safe requirement, and should Law be able to undo progress?
- Does the beer-based recovery window create cooperation without making injuries meaningless?
- Does knowing the other Outlaw make coordination too easy? Keep current role knowledge initially, then vary it separately.
- Do players argue, bargain, cover one another, and change their minds based on actions?

If sequential turns feel slow, a later comparison could give everyone one action in a
shared action window. That is a separate experiment: simultaneous attacks can recreate
the opening burst, so the sequential version is the recommended first test.

The next implementation would need host-controlled round/turn state, action budgets,
clear turn feedback, safe/bag/escape interactions, recovery states, and revised victory
rules. **This documentation update does not implement those systems or create a build.**

## Two laptops on the same Wi-Fi

Download the matching Windows or Mac ZIP from [Releases](https://github.com/kiskalloakos/last-call-saloon/releases/latest).
Extract the whole archive; Unity does not need to be installed to run the builds.

1. On the host, choose **Local / LAN → Host LAN** with port `7777`.
2. On the other laptop, choose **Join LAN**, entering the host's local IPv4 address and port `7777`.
3. Once both players appear in the lobby, the host presses **Esc → Start practice / fill empty seats**.
4. Three inert targets fill the empty seats, letting two humans test movement, shooting, seating, and beer.
5. For the actual hidden-role game, gather five humans and use **Deal roles & start match** instead.

Both devices must be on the same local network; guest-network client isolation can prevent direct connections.
If the operating system asks, allow the game to receive connections on the private/home network.
Cloud services, port forwarding, and internet hosting are not required for LAN gameplay.
Proximity voice connects automatically after joining; allow microphone access and hold **V** to speak.
Both laptops need internet access for Vivox even when the game connection uses LAN.

## Play

- Open `Builds/macOS/Last Call.app` for the built macOS version.
- On Windows, extract the Windows ZIP completely and run `Last Call.exe` beside its included data folder and DLLs.
- Or add **this SaloonPrototype folder** to Unity Hub, open `Assets/Scenes/Saloon.unity`, and press Play.
- **Solo Practice** adds four inert targets and deals roles, so you can explore and check interactions; these targets do not simulate social play.
- **Host LAN** starts a five-player lobby. Friends on the same network choose **Join LAN** and enter the host's local IP address and matching UDP port (default `7777`).
- For network tests, all players must use matching source/build versions. The new round source is not compatible with the old 0.1.2 downloads.
- Once five people have joined, the host presses **Esc → Deal roles & start match**.
- For friends on different networks, choose **Online room** and create/join a private room code.

The shipped builds are linked to the saloon's Unity project, with Vivox, Relay, Lobby, and anonymous authentication configured.
You do not need a Unity account or the Unity Editor to play.

## Proximity voice

- **Hold V** to transmit; releasing it stops your microphone transmission.
- **M** toggles microphone mute.
- Voice is full-volume within two metres and fades to silence at ten metres.
- **Esc → Microphone** cycles input devices; the echo test lets you hear yourself for eight seconds without broadcasting to the saloon.
- The voice list shows who is speaking and lets you mute individual players locally.
- Eliminated players use a separate voice channel; rematches return them to proximity voice.
- Use headphones when testing two laptops next to each other to avoid feedback.

## Controls

| Input | Action |
|---|---|
| WASD / mouse | Move / look |
| Q | Draw / holster revolver |
| Left click | Fire |
| Right mouse | Narrow aiming field of view |
| R | Reload |
| E | Sit / stand, or take a beer when looking at the brass tap |
| B | Drink carried beer when injured |
| G | Give carried beer to the player you are looking at |
| Space | Pass your action turn |
| Hold Tab | Privately read your role and objective |
| Hold V | Push-to-talk (voice connects automatically) |
| M | Toggle microphone mute |
| Esc | Lobby, tuning, voice controls, leave room |

Look at an actual chair or stool to sit; look down at its seat if necessary.
The supply of beer is the brass tap at the right end of the bar.

## Current source rules

Everyone has three health points; each shot deals one damage with no headshot multiplier.
The revolver has six rounds and unlimited reserve ammunition; reload takes 3.5 seconds.
Drawing takes 0.6 seconds. Each round starts with discussion, then each living player
gets one major action on their turn. A player can fire at most one bullet per round;
the existing 0.65-second shot cooldown remains as an additional input guard.

Eight shared beers are available. A player can carry one. Drinking takes 2.5 seconds,
holsters the weapon, and restores one health point. Taking damage interrupts drinking
or reloading; a started beer is consumed even if interrupted. Busy actions slow movement.
You cannot fire while drawing, reloading, or drinking.

| Role | Knowledge | Victory |
|---|---|---|
| Sheriff | Publicly identified | Both Outlaws and the Renegade eliminated |
| Deputy | Knows the public Sheriff | Wins with the Sheriff, including after the Deputy dies |
| Two Outlaws | Know each other's names | Sheriff eliminated, except a sole-surviving Renegade wins first |
| Renegade | Knows only the public Sheriff | Be the only survivor; Sheriff must die last |

Roles are revealed to everyone only when the match ends. Reaching the round cap is a draw.
A mid-match disconnect aborts the match rather than silently changing the role balance.
The host can tune discussion duration, turn duration, round limit, and beer supply between matches.
Other parameters are in the scene's **Saloon Prototype → Tuning** component.

## Build and verification

- **Saloon → Set up saloon scene** regenerates this prototype's map; it overwrites its scene layout.
- **Saloon → Verify rules and asset setup** checks victory precedence and imported assets.
- **Saloon → Build macOS playtest** regenerates, verifies, and builds the macOS app.
- **Saloon → Build Windows playtest** needs the matching Windows Build Support module installed through Unity Hub.
- `python3 Tools/smoke_test.py` launches five real app processes and checks their replicated match outcomes and private role views.
- `python3 Tools/smoke_test.py --practice` verifies two real app processes plus three inert practice targets.
- `python3 Tools/cloud_test.py relay` checks a real private Relay room with two app processes.
- `python3 Tools/cloud_test.py voice --wave /path/to/test.wav` checks Vivox voice using a generated mono 16-bit PCM WAV at 48 kHz; microphone inputs stay muted during this test.
- Do not rebuild the app while smoke-test processes are using it.

## Scope and next playtest

One saloon with a porch and back room, first-person controls, host-validated movement,
hitscan damage, seating, beer sharing/healing, role assignment, match results, and rematches.
Character poses, revolver, recoil, and generated gunshot audio are placeholders.
The gun is an original simple mesh assembly; the imported furniture, character, and barrel sources are recorded in `Documentation/ASSET-SOURCES.md`.

This version uses host-authoritative snapshots without client prediction or lag compensation.
It targets trusted private playtests; there is no host migration, dedicated server,
matchmaking, progression, production anti-cheat, or reconnect-in-progress support.
Vivox attenuation follows distance, not wall occlusion; use separated corners/porch for private conversations.
Dead players switch to a separate voice channel. Trusted clients enforce that switch;
production channel access would need server-issued authorization.

The first social playtest should record why each player trusted or suspected someone,
how long it took to fire the first shot, and whether beer sharing and covering reloads mattered.
Change one constraint between matches rather than adding new systems immediately.
