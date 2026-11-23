# Last Call — replayability reference

Updated 2026-11-23. Target experience: suspicion and betrayal, with funny chaos.

**Items 1 and 2: implemented; live engineering checks completed. Human social-balance testing is still outstanding.**

**Current step:** Toilet Trouble is now an optional, isolated 12-second solo mission, randomly offered only to injured players. Unclogging heals 20% and returns the worker to Bar with their combat action available. Beer is unchanged. Bar sunset lighting fades toward night over rounds 1–20, with every lamp retained at lower brightness. Blackjack, Coin Flip, and the smoking deck retain their lighting. The West room now alternates Coin Flip and spoken Bluff Dice contests, announced before voting. Darts is not planned. Owner feedback: middle-paced Blackjack is the current revision; theft/escape feel okay and treatment/smoking feel worth the risk. Phantom stays deferred.

## Current focus: the money and the theft

The money sits in a normal counter drawer so approaching it has an innocent
explanation. To prevent round-two money escapes,
three shared Outlaw steps across three rounds collect it.

### Current victory rules

- Sheriff death ends the match for the Outlaw team, regardless of the killer.
- If that death leaves only the Renegade alive, the Renegade wins instead; they
  must leave the Sheriff until last.
- Outlaws can also win by escaping with the money. Three shared drawer steps
  across three rounds collect it; the earliest money escape is round four.
- The random Renegade/Swindler slot remains. There is no posse, round cap, or automatic timeout victory; rounds continue until a normal victory condition is met.

### Implemented location

The standalone floor safe is replaced by a normal cash drawer integrated into
the main Bar counter. The beer tray is farther left along the same counter,
4.15 metres from the drawer horizontally, giving drinkers and drawer users
separate places to stand. The drawer interaction is on the customer-facing side.

The purpose is to give approaching the money an ordinary explanation. Beer
already brings some people to the counter, but beer alone is insufficient cover:
the current game refuses drinking at full health. A legitimate drawer inspection
must therefore be available to every role, including healthy players.

### Implemented interaction

- Anyone can inspect the drawer to learn privately whether the money is present
  and whether its state changed since their last inspection.
- An Outlaw can instead advance theft through the same visible reach/check
  animation. The choice and result stay private.
- Inspection and theft each use the player's one round action, with comparable
  visible duration. Watching the animation does not distinguish the two.
- Theft takes three shared two-second steps across three rounds. At most one
  step succeeds per round, even when multiple Outlaws work simultaneously.
  The third worker immediately receives the money; no separate pickup is needed.
- Inspections can detect partial tampering while the money is still present.
- Carrier death returns the money and resets all three steps.
- Witness clues describe a counter interaction, not a confirmed robbery. There
  is no automatic thief name or public carrier label during theft.

This still permits deduction: if money disappeared between two inspections,
players can compare who visited, what they claimed, and who witnessed them.
Suspicion comes from context rather than a role-exclusive destination.

### Implemented escape

After stealing, the carrier can commit to escape during the next discussion.
The attempt is publicly announced during discussion, before room assignments
lock. The Bar card pulses red and the carrier is identified in the planner.
Everyone gets at least ten seconds after the first announcement to reconsider
rooms, even if all players are ready or the discussion timer was nearly over.
Cancelling and declaring again cannot repeatedly extend that window. The attempt
preserves the carrier's normal action to shoot, heal, reload or pass. Everyone at
the Bar gets an action. If no headshot stops the attempt, completed Bar actions
lead straight to the Outlaw victory screen, including when other rooms are still
playing. Any timed Bar action already underway finishes first; the action deadline
remains the fallback for idle players.

In bot matches, carriers announce before the other bots pair off. One available
Law or Renegade bot responds to that public warning by choosing Bar, including
leaving a previously locked bot-only pair. Explicit human invitations are
respected, so diverting that defender remains possible. Bots do not use a hidden
carrier flag to decide where to guard; they react to the public declaration.

The response window must start while defenders still have their actions; a
last-second declaration cannot bypass it. A landed damaging headshot cancels
that escape attempt without requiring the Law to kill a full-health carrier.
The carrier keeps the money and may try again later, avoiding another theft
grind. Normal health damage still applies.

The code also lets any damaging headshot interrupt escape, so the Law does not
need to eliminate a full-health carrier in one action phase. The focused cash
drawer Play mode verification passed 51 checks on 2026-11-23, including
separate beer and drawer interaction positions. The Bar/beer Play mode suite
also passed 45 checks, including walking access to the relocated tray; the
planning/rules harness passed 21,177 checks. Human sessions still need to check
the social balance.

### What counts as fixed

- A player can approach and use the drawer for a useful innocent reason at any
  health level, without an animation or label certifying their role.
- Three theft actions across three separate rounds collect the money; teammates share progress and cannot rush several steps in the same round.
- A player can explain both Outlaw victory routes: Sheriff death (except a sole-surviving Renegade), or complete three drawer steps and escape in a later round; earliest money escape is round four.
- Before room assignments, the public escape warning gives everyone time to choose Bar; one available defending bot guards instead of automatically leaving.
- The Law has a fresh, clearly communicated chance to interrupt escape without
  needing to eliminate a full-health target in one action phase.
- The focused check covers competing Outlaw attempts, one carrier, the fresh
  response window, headshot interruption, elimination, and private snapshots;
  eight-client UDP reconnect/rematch now verifies carried money, planned and active
  escape, private reports, personal victory, action spending, and clean resets.
- Human playtests at small and larger match sizes check whether guarding, bluffing,
  stealing, and responding offer meaningful choices; automated bots cannot validate
  the social experience.
- Playtest the Outlaw balance follow-up alongside the focused Blackjack pacing changes in item 4 below.

## Nine-point roadmap

The two approved role changes from the original ten-point discussion are grouped
into item 1, preserving every idea in nine reference items. Unless marked otherwise,
these mechanics remain proposals.

1. **Role knowledge and random independents — approved; implemented in source.** Deputies are hidden from the Sheriff, the Renegade receives no complete role roster, and Outlaws retain teammate knowledge; one independent slot secretly rolls 50/50 Renegade or Swindler, with the Swindler earning a personal win from a lethal Law shot while the match continues. Live role privacy, Swindler results, and reconnect/rematch checks pass; human balance testing remains.
2. **Deduction and theft — implemented in source; balance follow-up remains.** Shared counter interactions, private inspections, and contextual witness accounts keep theft ambiguous; three shared steps in separate rounds prevent round-two money escapes, and Sheriff death is an alternative Outlaw victory unless the Renegade is alone; reflected friendly-fire damage, allegiance-colored hit confirmations, and allegiance-dependent hit milestones are removed. Live theft and reconnect/rematch checks pass; human balance testing remains.
3. **Reasons to leave the Bar — first version implemented; human testing next.** Coin Flip and Blackjack now block sight both ways; voices and player sounds across indoor room boundaries are muffled, while smoking remains isolated. Waiting-room voice stays on, pauses during the opening role reveal, and returns for table discussion. Round one stays quiet; later rounds have a 50% chance of a medical event, marked only by a teal animated room card and medical icon in Coin Flip or Blackjack; the winner chooses a 20% heal as their action or keeps their normal opening. The reward expires with that opening, uses no beer, and does not consume or multiply the smoking boost.
4. **Pacing — middle-speed revision implemented after owner feedback.** The first faster version felt rushed; dealing, dealer actions, and results now use a middle pace. Automatic high-card draws resolve ties without replaying the hand. Keep discussion time, player decision time, combat openings, and damage unchanged; playtest before changing combat balance. There is no shorter-match-deadline proposal now that the posse is gone.
5. **Fake Deputy — deferred until ordinary bluffing is tested.** Hidden Deputies already let any Outlaw bluff this identity. Test that first; a dedicated dynamite role can wait.
6. **Phantom — explicitly deferred for later.** Retain this idea in the backlog; do not implement it now. Give eliminated players one simple, limited haunt such as a bottle-shattering fake gunshot, shutter slam, or brief lamp outage, with no lethal damage, money manipulation, or unrestricted spectator scouting while haunting.
7. **More round events — next candidate; Phantom is deferred.** Medical supplies are the first event: the featured room offers its winner a 20% heal instead of the normal opening action. After testing that tradeoff, add a small selection of visually distinct round events for variety without permanent rules.
8. **Additional maps — explicitly deferred.** Revisit once Classic consistently creates interesting matches. Consider Last Express, with three compact carriages and a station escape deadline, followed by Frontier Hotel, with balcony witnesses and alternative approaches to the money room; each should change decisions and visibility.
9. **Character variety — lower priority.** Distinct appearances would improve recognition, witness descriptions, and attachment, but cosmetics alone add little strategic replayability; keep appearance independent of secret role and revisit after the core loop is satisfying.


## Human playtest needed before claiming balance is finished

Owner feedback on 2026-11-23: theft and escape feel okay; leaving for treatment
and smoking feels worth the risk. This is useful initial feedback, not a claim
that the full multi-session checklist below has been completed.

The remaining gap requires real players: automated clients verify correctness,
but cannot tell whether bluffing and deduction are enjoyable. No human session
has been claimed as completed.

- Run at least two four-player and two seven/eight-player matches, rotating who
  guards and who steals; record whether Renegade or Swindler was dealt.
- Ask each player to explain their own objective and who they know after the intro.
- Test an innocent drawer inspection and an Outlaw theft with witnesses present;
  players should need reports, timing, and claims to distinguish them.
- Ask a Law player to shoot a suspected ally: shooter health, hit feedback, and
  the headshot milestone must not certify that target's allegiance.
- Attempt an escape against attentive defenders, then one after persuading them
  to spend their actions elsewhere. Record whether each side felt it had a choice.
- Record rounds to first theft, escape attempts, match length, winner, and why
  players trusted or suspected someone. Check whether the same guarding strategy
  dominates every match before changing timing or rewards.

After these sessions, tune only the problems actually observed. Continue with
the next priority only after the focused pacing and Outlaw follow-up playtests; human balance validation remains unfinished.


## Item 3: first medical opportunity playtest

- Confirm round one has no event. On later rounds, confirm there may be no event; when one appears, everyone sees the same teal room highlight and medical icon before locking a plan.
  Invite a partner there; an unmatched solo minigame plan still returns to Bar.
- Enter injured, win, and select Heal 20%: health rises by 20 points, up to 100;
  your action is spent, your opponent can respond, and you cannot also shoot.
- Win again and Keep opening: you retain the usual action but cannot later claim
  those supplies. At full health, healing is disabled. Doing nothing times out
  normally. The loser and the other minigame room receive no medical choice.
- Compare a match where the Sheriff stays at Bar with one where the Sheriff
  leaves for treatment. Discuss who guards, who accompanies them, and whether
  the Outlaws gain a believable opportunity. Record whether healing ever feels
  worthwhile, whether it becomes automatic, and whether ten seconds is enough
  to understand and make the choice.

This is development source, not a new downloadable build. Start from the current
Unity project. Supplies heal immediately, produce no public reward announcement,
and remain available to every role under the same winner-only rules.


## Item 4: Blackjack pacing playtest

- Initial dealing takes 2.2 seconds, between the old 3.2-second deal and the
  overly fast 1.2-second version, with 0.4-second card spacing and 0.5-second
  slides/flips. Hits and hole-card reveals settle for one second. An idle dealer
  advances after five seconds (between the previous eight and two);
  bots also make their card decisions sooner.
- A tied total or two natural blackjacks automatically draw one high card per
  seat from the remaining deck. Aces rank above kings; suits do not matter.
  Equal ranks redraw only the high-card pair. The original hands stay visible,
  with the deciding cards in separate rows. The two-second tiebreak presentation
  leads directly to the winner's result, with no new Blackjack hand or input.
- Blackjack results last three seconds (between the previous four and two). Both weapons remain
  locked until that pause ends. The winner still gets the full ten-second
  opening, including the existing medical choice, before the opponent responds.
- Discussion time, the 45-second player decision allowance, and damage stay
  unchanged. Sheriff death already ends matches; leave required headshots alone
  until playtests establish whether combat needs adjustment.
- Test both seats with human partners. Confirm cards and the opening winner are
  readable at the middle pace, ties are understandable, and the three-second
  result pause gives enough preparation time. Record minigame duration, idle
  time, accidental/duplicate inputs, missed openings, and medical choices.
- After this playtest, consider #7 more round events, then #5
  only if ordinary Deputy bluffing needs help. Revisit #8 maps and #9 appearances
  once Classic consistently produces interesting matches. Keep #6 Phantom in the
  backlog for later.


## Back deck and front entrance

- Have a Smoke is presented as going out the back during room voting. It has no
  walking entrance from inside the saloon, and solo or paired visits retain
  their private player snapshots and voice channel.
- The front entrance frames the same desert sunset through low saloon doors.
  Its view includes scenery only, never the back deck or its smokers. The front
  barrier still prevents leaving through the entrance during room actions.
- The back bench has two usable spots. Walk near an empty spot and press Space
  to sit facing the sunset; press Space again to stand. Sitting and taking a
  drag are free and retain the existing cigarette buff rules.


## Exterior windows

- Two exterior windows on each long Bar wall and one in each minigame room
  show the desert without exposing the private back smoking deck.
- These are exterior views only. The Bar-facing minigame walls and murals
  remain opaque; their private conversations and games retain the same rules.
- Glass stops movement and shots. Saved solid walls are cut through all outer
  layers automatically when the layout is applied, without changing room bounds.


## Bar sunset lighting

- The Bar uses low golden light entering through the side windows. All four
  Bar pendants and both wall lamps remain visible and on at lower brightness;
  the daylight fill is also dimmed. Lamp and ambient levels have been raised
  after the initial dark revision, especially at night, so faces and furniture
  remain readable. No lamp fixtures are hidden or disabled.
- The shared round number drives a smooth transition from warm sunset in round
  one to cool, dim night around round twenty. Later rounds retain night; this
  is visual progression, not a match limit. A new match resets sunset, and late
  joiners/reconnecting clients immediately use their current round's profile.
- Blackjack, Coin Flip, the private smoking deck, and tutorial lighting retain
  their existing appearance. The camera-scoped lighting restores its original
  ambient, lamps, and exterior sky after rendering, including spectator changes.

## Toilet Trouble — implemented safe solo mission

- Each discussion, select one living, injured player uniformly at random. Role,
  human/bot status and prior assignments do not bias the roll; repeats are
  allowed. Healthy and eliminated players are excluded. No injured players
  means no offer. Only the selected player receives Accept/Reject.
- Rejecting preserves normal planning. Accepting reserves a solo mission plus
  a return to Bar, releases any prior partner and locks the decision. The
  bathroom is not a room-picker card and cannot be selected or invited to.
- Discussion stays shared. At the action phase, the accepted worker is placed
  in a standalone bathroom outside the Bar's geometry: scene root
  **Isolated solo bathroom**, world center **(0, 0, 40)**, room ID 4, render layer
  17. The Bar extension/doorway is removed. Living room snapshots, camera
  visibility, sounds and voice isolate the mission from all other activities.
- The worker is protected from damage and cannot use weapons, drink beer,
  inspect/rob the drawer or spend a combat action inside. Left click eight
  times at the toilet; each accepted stroke takes at least 0.4 seconds. E can
  equip the plunger but never completes the job automatically. Waste sinks,
  water clears and each stroke plays the recorded suction/gurgle Foley.
- **Success: +20 health points, capped at 100%, then immediate return to Bar
  with the combat action available in this same round.** The mission consumes
  no beer, ammo or cigarette buff. The worker hears a private completion flush
  after returning. Damage protection ends as soon as the player returns.
- A **12-second timeout** starts on arrival, including time before pumping.
  Timeout or leaving the working position after starting returns the player
  without healing and without spending their action. The shared round timer
  keeps running; short phases reserve three seconds for combat after return.
  No action carries into the next round and no extra match time is added.
- Bots use the same assignment, pumping, healing and return rules. New matches
  reset offers, mission state and timers. Beer remains unchanged: a public,
  exposed heal using limited shared stock and the normal round action.

## Bluff Dice — implemented West room rotation

- Replaces Liar's Dice's escalating bids with one spoken sentence and a binary
  truth judgment. Classic alternates Coin Flip on odd rounds and Bluff Dice on
  even rounds. Discussion snapshots, room cards, plans and invitations announce
  the game before voting. The existing cup-and-dice mural follows the rotation.
- A random speaker rolls three secret dice and gets a private assigned claim.
  Claims are balanced true/false, and the speaker knows which. The listener
  needs to hear the sentence: human listeners receive neither the claim nor
  its truth or dice values before reveal. The speaker's goal is a wrong guess.
- The only listener choices are Believe and Don't Believe. A correct judgment
  gives the listener the normal opening; a wrong one gives it to the speaker.
  Reveal all three speaker dice and explain the result. No bids or face grid.
- The speaker clicks Throw Dice or presses Space; dice do not roll on their
  own. Classic allows 15 seconds to throw, then a 2.4-second animated roll,
  eight seconds to speak, six seconds to judge, then
  the normal three-second result pause and ten-second exclusive opening. Idle
  listeners and speakers who never throw forfeit. Throw commands are accepted
  only once from the current speaker; stale and outsider commands are rejected.
  Action cost, passing, damage, response and medical opportunities are unchanged.
- Voice remains normal room voice (V push-to-talk / T open mic). No speech
  recognition or mic enforcement: players must say the assigned sentence.
  Bots use an explicit text caption and a blind judgment, without hidden-roll
  access. Spectators and public table views receive no secret prompt or roll.
- Saloon → Play Bluff Dice at table opens `BluffDiceTest.unity`, offline with one
  human and one bot. It starts on instructions with the human throwing first,
  alternates speaker/listener roles, removes throw and judgment timeouts, holds the reveal for review
  and waits for another hand after combat. N deals again with fresh health,
  ammo and actions. The scene is excluded from release builds.
- Protocol state-35 carries the new private prompt, listener permission and
  revision-checked judgment. The previous bid command and bidding model are
  removed. Beer, toilet duty, Blackjack and Classic room capacity stay unchanged.

## Remaining activity ideas

- **Darts is not planned**, following the owner's decision.
- **Phantom remains deferred.** A dedicated Fake Deputy, maps and appearances
  remain lower priorities in the nine-point roadmap above.
- Arm Wrestling and Emergency Beer Delivery are unapproved ideas from the
  latest discussion. Test the new rotation before choosing another activity.
