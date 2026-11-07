# Last Call — current alpha

Five players in a low-poly saloon: one public Sheriff, one hidden Deputy, two Outlaws,
and one Renegade. Release **0.2.0**, 2026-11-07. Use matching 0.2.0 downloads on both laptops;
old 0.1.2 clients have incompatible rules.

[Download Windows and Mac](https://github.com/kiskalloakos/last-call-saloon/releases/tag/v0.2.0).
Extract the entire Windows ZIP and run `Last Call.exe` inside its folder.

## Start testing in Unity

Open this **SaloonPrototype** project, open `Assets/Scenes/Saloon.unity`, then press
**Play → Solo Practice**. Four active practice players fill the other slots. They know
roles and aim accurately: they are opponents, not passive targets or conversation bots.
They still obey the host's one-action budget, ammunition, health, rooms and raycast hits.

A real match needs five people. Host LAN and join using the host's local IP and matching
UDP port (7777 by default), or create/join an online room code. All devices must use the
same source/build version: do not mix this Editor with old 0.1.2 downloads.

## What happens in a round

| Phase | Default length | What happens |
|---|---:|---|
| Discussion | 30 seconds | Everyone sits at the common table, looks around and talks. Major actions are locked. |
| Choose rooms | 15 seconds | Click Bar, West room or East room, and optionally request a companion. |
| Room actions | Up to 30 seconds | Players are automatically seated in their assigned rooms. Everyone can spend **one** action during the same window. |
| Regroup | 3 seconds | Everyone returns to the common table; the next discussion begins. |

Walking and standing are disabled during the match. You can look, aim and speak. A new
round preserves roles, HP, ammunition, items, safe progress and pending escape attempts.
Actions can end early when all living players finish/pass; started timed actions finish
before regroup. Six rounds maximum, configurable before starting the match.

Each side room has two places. A requested pair must nominate **each other and the same
room**. Mutual pairs receive priority; solo requests fill remaining places. Unaccepted
invitations and full-room requests fall back to the bar. The bar can hold everyone.
The assignment is locked during actions; the HUD/pause roster shows the actual room.

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
- **Work the safe / collect the money:** H at a bar station; see robbery below.
- **Declare escape:** X at a bar station while carrying the money; see victory below.
- **Pass:** Space gives up your action this round.

Drawing/holstering (Q) is free preparation, but occupies your hands for 0.6 seconds.
Looking, speaking and preparing a beer are free. Busy players cannot start another action. Beer handoffs
fail without spending resources if the intended receiver is busy, already carrying beer,
out of range, obstructed or in another room.

Everyone starts at three HP. Shots deal one damage, with no headshot multiplier. Zero HP
means **eliminated**, not downed: no movement/actions, no mid-match respawn or revival.
Eliminated players use a separate voice channel. A new match resets everyone. Bodies, names and empty HP dots remain at their death seat;
living players never fill that seat. Common-table seating does not compress after deaths.
A corpse in a side room reserves its seat until the next match.

## Robbery: why stay at the bar?

Press **H** during room actions to work the safe for **three seconds**, weapon holstered.
It costs your action. Three completed work actions open it; progress is shared and persists
between rounds. Several people can work simultaneously. The player completing the third
step collects the money. Anyone, including Law players, can work or collect it: touching
money does not automatically reveal a role.

A shot interrupts work and gives no progress for that attempt; the action stays spent.
If someone already carries the money, another work attempt is rejected without cost.
When the carrier is eliminated, the money becomes collectible at the bar again with H;
the safe remains open. It does not become a loose object you have to walk over to pick up.

People in the room can **look at the worker** and see leaning/reaching and the money pouch.
There is no global notification, role reveal, robbery sound cue, or public action log.
Robbery animation flags, bag visibility and safe progress are filtered out of other rooms'
snapshots. Roommates still need to look in the right direction; testimony is not certified
by the game. Work/bag visuals disappear from others' view on regroup, so discussion relies
on what people saw and remember. The carrier can always see their own objective status.

## How do we win now?

**Killing the Sheriff does not end the match.** Money replaces that assassination objective.

| Faction | Victory |
|---|---|
| Sheriff + Deputy (Law) | Successfully secure the money through an escape, eliminate all Outlaws/Renegade, or keep money from escaping through the final round while at least one Law player lives. |
| Outlaws | Either Outlaw successfully escapes with the money; both share the faction win. |
| Renegade | Personally escape with the money. Being the sole survivor alone is insufficient. |

Escape is a two-round commitment:

1. Carry the bag at the bar and spend an action pressing **X**. You cannot declare during
   the final round because another response round is required.
2. Regroup, discuss and choose the **bar again**. A successful assignment to a side room
   cancels the attempt. Discussion does not cancel it.
3. Survive the **end of the next room-action phase**, still at the bar with the money.
   That resolves the escape and the carrier's faction wins. You may spend your action
   defending yourself during that response phase.

A hit alone does not cancel escape; elimination does. Eliminating the carrier makes the
money available at the bar again. At the six-round limit, surviving Law wins if no escape
has resolved. If no Law remains and no criminal escaped, the result is a draw. All roles
are revealed when the match ends. A mid-match disconnect aborts it.

## Beer animation and audio

Both carried/self-drunk and gifted beer are visible as mugs. Drinking raises and tilts
the mug toward the mouth in third person, with the skinned arm reaching for its handle,
and toward the camera in first person. A gift
flies in an arc from giver to recipient, then the recipient drinks automatically. Gulping
plays during the sip; a quiet clink accompanies handoff. These are original procedural
alpha sounds, not recorded samples. The revolver has a layered crack/body/metal/room gunshot and a short flourish after each
accepted shot, visible in first and third person. The spin is cosmetic and grants no extra shot.

A shot interrupts a normal or gifted drink: consumed beer is lost and no healing arrives.
Healing applies only when the drinking timer completes. Drinking sound stops on interruption.
The mug/arm animations are procedural; detailed finger grips and first-person arms remain
future work.

## What practice players do

Practice players pursue faction victory using **perfect role knowledge**, including hidden
roles. This is deliberately aggressive test AI, not a simulation of human deduction.

- Default to the bar to contest the objective; may accept a human's room invitation when
  not carrying money. A carrier stays at the bar instead of accepting isolation.
- Law shoots criminals. Criminals work/collect money and declare escape, or shoot a rival
  carrier, an isolated enemy, or a nearly eliminated target. Outlaws treat one another as
  allies; the Renegade treats everyone as a rival.
- Aim real shots, draw, reload, take/drink beer when appropriate, and pass when no useful
  action is available. They receive no extra action, HP or wall-penetrating bullets.
- They are silent: no bot proximity conversation, accusations or bluffing yet. They may
  act faster/more accurately than humans and are not balanced opponents.

## Controls and voice

| Input | Effect |
|---|---|
| Mouse / right mouse | Look / narrower aiming FOV |
| Q / left click / R | Draw or holster / shoot / reload |
| E / B / G | Take beer at bar / drink / give beer to aimed player |
| H / X | Work safe or collect money / declare escape |
| Space / hold Tab | Pass / privately read role and objective |
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
