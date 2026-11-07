# Proposed route: seated discussion and assigned rooms

2026-11-07. The seated Discussion → Choosing → shared Actions → Regroup cycle is now implemented in Unity source. See README.md for current timings and controls. The robbery/victory and injury sections below remain future proposals.

## What the current test missed

The original idea was a social round with room assignments, distractions, robbery and
witnesses. Adding a clock to an always-mobile shooter did not create that structure.
Solo practice makes the problem worse: inert targets pass in 0.75 seconds and cannot
negotiate, witness, bluff, or accuse. It verifies commands, not the social experience.

## Recommended next experiment

Remove manual walking during a match. Keep looking, aiming, talking, item interactions
and animations smooth. Players occupy clear seats/standing stations; transitions move
them between stations. Walking can remain available in the lobby.

1. **Gather — 40 seconds:** everyone sits around a common saloon table/bar. All living
   players can hear discussion. Weapons and resource actions are locked. Compare last
   round's claims; negotiate who will talk to whom. A visible round/phase countdown and
   distinct transition sound make the boundary unmistakable.
2. **Choose rooms — 15 seconds:** each player publicly nominates a room and optional
   companion. Paired conversations require mutual acceptance. The bar is a valid default
   and can hold multiple people; nobody can force another player to leave it empty.
   Unaccepted nominations/timeouts stay at the bar. Show the final roster before moving.
3. **Separate — 30 seconds:** transition everyone to fixed stations in the bar or two side
   rooms. No walking between rooms until regrouping. Each player has **one action during
   this shared window**, rather than sequential, globally announced turns. Everyone can
   look and talk throughout. Pass or let the window expire. End early only when everyone
   has finished/passed, allowing timed interactions to finish first.
4. **Regroup:** transition to the shared table. Briefly show the new round number; begin
   discussion. Role, health, ammunition and carried items persist. Nobody automatically
   heals or receives another beer at a boundary.

This replaces the current sequential turn clock; it is not a second overlapping turn
system. First test with five people. Solo mode should later demonstrate a scripted
witness/distraction example instead of pretending inert targets test social deduction.

## Make robbery and testimony physical

Place the till/safe at the bar. A player stationed there can spend their action on a
clearly animated, several-second work interaction. Other bar stations can see it;
side-room stations cannot. Do not publish a global action log or announce who spent
which action. Everyone may inspect/handle the money under the final objective rules,
so touching it does not certify a role. A witness can accurately report an interaction
while still misinterpreting its purpose, lying about it, or suppressing the evidence.

The distracting Outlaw persuades the Sheriff to accept a side-room conversation. They
do not stun or disable the Sheriff. Their partner chooses the bar; a third player who
also chooses the bar can witness what happens and report it next discussion. The
Outlaws therefore need cooperation or persuasion, rather than guaranteed secrecy.

Normal voices are clear in the same room; quieter, muffled voices from adjacent rooms
may be overheard. Consider a shout button for deliberate cross-room communication.
Current Vivox only attenuates by distance: wall muffling/shouting would be new work.
During shared discussion, hear everyone seated at the central table clearly.

Fixed stations need camera/cover placement tests. A bar witness must actually see the
safe interaction, and a side-room camera must not see through a door into it. Chairs,
hands and mugs should communicate what people are doing without floating world labels.

## Victory and injury rules to test alongside it

Recommendation: remove automatic Outlaw victory on Sheriff death. Law defends the money
through six rounds; Outlaws win if either escapes with it; Renegade must escape personally.
The safe could require three work actions across rounds. Carrying the bag and declaring
escape each cost an action. An escape declared this round resolves after the next full
shared action window if the carrier is still eligible, giving everyone a response.
Disable new declarations after Round 5 so the six-round deadline is unambiguous.

At zero HP, down players for at least the next response window; keep their voice available
and allow a nearby player to spend a beer/action to help. This keeps an eyewitness in the
conversation instead of immediately removing their testimony. Permanent elimination,
Law securing money early, objective visibility, and precise recovery timing remain open.

One bullet per player still limits bursts in the shared phase, but coordinated attackers
can focus fire. Evaluate a short draw telegraph and a non-violent first round if the
opening remains too abrupt. Do not silently restore unrestricted shooting.

## Implementation order after agreement

1. Gather/Choose/Separate/Regroup state machine, fixed stations and public room roster.
2. Shared one-action phase with host enforcement, clear lockouts and transition feedback.
3. Till progress, witnesses, visible item handling, replacement victory and response window.
4. Voice attenuation by room and a real five-person playtest.

Do not implement additional room gates or new victory logic as part of the art pass.
