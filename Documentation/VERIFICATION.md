# Multiplayer shot registration — 2026-11-23

- Replaced the fixed eye-height head sphere with a mesh-fitted head volume driven by
  the same humanoid rig as the visible character. A hidden authoritative rig keeps
  working when the host is in a different visibility area.
- Trigger commands carry click-time aim, including camera kick, and the last actually
  rendered snapshot interval. Client rendering and host hit tests interpolate the
  same authoritative head samples, including nonlinear flinches and packet loss.
- The host retains one second of combat geometry and accepts shot ages up to 500 ms;
  queued draw shots have an 800 ms total-age cap. It rewinds query colliders only,
  restores them in `finally`, and retains static wall/glass occlusion. Clients do not
  claim hits, head positions or damage. Invalid times/poses and duplicate actions are
  rejected; a recently acknowledged stance remains valid while its confirmation travels.
- Fire bypasses the unrelated 90 ms action debounce. Legal draw clicks remain queued
  through nonlethal hits and are cleared on death, lobby reset or a new match.
- `SALOON_SHOOTING PASS`: 64 checks using eight real UDP participants. Covers stale
  movement aim, actual camera-kick trigger input, duplicate clicks, seated/crouched/look
  poses, both orders of simultaneous hits, flinching and nonlinear interpolated heads,
  delayed movement, snapshots arriving before render, stance acknowledgement delay,
  body hits, glass, targets invisible to the host, queued draw shots and invalid inputs.
  Log: `/tmp/lastcall-shooting.log`.
- Existing `SALOON_ROOM_ACTIONS PASS` and rendered `SALOON_COMBAT PASS`: normal head
  damage, no body damage, dodging, healing, weapon grip, flinch/flash, recovery and
  lethal ragdoll behavior. Rendered flinch capture inspected.
  Logs: `/tmp/lastcall-shooting-room-actions.log`, `/tmp/lastcall-shooting-visual.log`.
- `python3 Tools/verify_planning.py`: **21,142 checks passed**.
- Eight independent Mac processes passed three rounds and the start of round four
  with the actual `Shoot()` path over an impaired LAN: 80 ms one-way latency,
  25 ms jitter and 2% configured packet loss on seven links. All links dropped packets,
  every player exited 0, and headshot health/confirmation checks passed.
  Logs: `/tmp/lastcall-shooting-lan-impaired`.
- Eight independent Mac processes passed the same three-round shooting/healing flow
  through an actual private Unity Relay room, with every player exiting 0.
  Logs: `/tmp/lastcall-shooting-online`.
- Matching macOS and Windows x64 development builds succeeded under
  `Builds/MultiplayerVerification`. Protocol is now `state-17`; all peers need these
  updated builds. Windows executable verified as PE32+ x86-64.
  Logs: `/tmp/lastcall-shooting-build-mac.log`, `/tmp/lastcall-shooting-build-windows.log`.

The matching test ZIPs in `Builds/MultiplayerVerification` passed complete CRC checks;
macOS executable permissions were verified. `SHA256SUMS-shooting-fix.txt` records both
archive hashes. The Mac build contains Intel and Apple silicon executables.

Windows runtime, physical mixed-device/network play and the user's original live
playtest still need a device check. The automated players ran on this Mac; successful
regressions do not establish that every possible network or aiming issue is eliminated.

# Eight-player multiplayer source update — 2026-11-22

- Online and LAN matches support 4–8 participants, including the host, with the
  agreed decks: 6 = Sheriff / 2 Deputies / 2 Outlaws / Renegade;
  7 = Sheriff / 2 Deputies / 3 Outlaws / Renegade;
  8 = Sheriff / 3 Deputies / 3 Outlaws / Renegade. Four/five-player decks remain defined.
- Host bot-fill controls cover every size from four through eight. Saved scenes
  receive eight stools, eight pouring stations and eight distinct discussion seats
  at startup. Seat interaction IDs do not overlap. Side rooms still hold two.
- Lobby rosters scroll; six-to-eight-player results use two rows. Sheriff and Outlaw
  private information lists every permitted teammate. Protocol `state-16` requires
  matching updated builds on all devices.
- `python3 Tools/verify_planning.py`: **21,142 checks passed**, including exact decks,
  unsupported sizes, eight concurrent Bar actions and paired side-room limits.
- Unity `RunLobby`: **PASS** for real UDP rounds at 4, 5, 6, 7 and 8 humans, private
  roles, distinct seating, movement, actions, ninth-player rejection, bot filling
  and replacement. Final rendered lobby, bot controls and eight-player results were
  inspected. Log: `/tmp/lastcall-eight-lobby-final.log`.
- Unity `RunKnowledge`: **PASS** at every size from four through eight, including
  all Deputy/Outlaw identities and spectator permissions.
  Log: `/tmp/lastcall-eight-knowledge.log`.
- Unity `RunRematch`: **PASS** with eight UDP participants: reconnect at full capacity
  preserves role, health, inventory, evidence and position; results include all eight
  players and all Law winners; unanimous rematch, return to lobby and reconnect
  expiry preserve the expected roster. Log: `/tmp/lastcall-eight-rematch.log`.
- Unity `RunPublicFloor`: **25 checks passed**, including the real collision loop
  around the eight-chair discussion table and the closed entrance. Discussion seats
  retain the original radius to preserve walking clearance.
  Log: `/tmp/lastcall-eight-floor.log`.
- Local macOS development build succeeded at
  `Builds/MultiplayerVerification/macOS/Last Call.app`.
  Log: `/tmp/lastcall-eight-build.log`.
- Eight independent packaged processes passed **three rounds and the start of round
  four** over LAN and, separately, an actual private online Relay room. Every process
  exited 0 with the correct eight-role deck. Covered standing/walking, coin and
  blackjack pairs, shooting, healing and private role replication.
  Logs: `/tmp/lastcall-eight-lan-processes` and `/tmp/lastcall-eight-online-processes`.
- An initial simultaneous online join attempt hit Unity Lobby HTTP 429 throttling.
  Online joining now has three bounded retries with increasing delay and jitter for
  rate-limit errors; other errors still surface immediately. The final eight-process
  online run passed without throttling, so live retry recovery was not exercised in
  that successful run.

Remaining human checks: separate physical devices/networks, Windows runtime,
router/firewall behavior, physical microphones, minimum-hardware performance and
balance of the new role distributions. The automated Relay clients ran on one Mac.

# Release 0.2.2 — LAN room-selection regression, 2026-11-07

- Existing 0.2.1 rendered Mac host/client smoke reproduction exited successfully; the
  reported Windows stall was not reproduced on this Mac.
- Fixed a confirmed rule/UI issue: unaccepted companion requests formerly forced Bar;
  the chosen room now survives when capacity permits. Pure room/clock checks pass.
- New 0.2.2 Mac and Windows builds succeeded; saved cloud-project settings validated in
  command-line Unity. Strict version/payload approval, conditional cursor changes, forced
  chooser visibility, lower snapshot frequency and persistent disconnect reasons included.
- `SALOON_ROOMS PASS` on two rendered real Mac processes using the full 30-second discussion
  with actual Vivox sessions and muted microphones: host East (2), client West (1), both
  host acknowledgements true, chooserFrames=1808 each, voice=True. Logs:
  `/tmp/saloon-022-rooms-voice`. Test shutdown produces a transport-shutdown message after PASS.
- Windows hardware remains untested: the original stall's exact cause is not confirmed.
- Packaged rendered `-saloonTest functional` completed with `SALOON_HEIST PASS`,
  `SALOON_BEER PASS`, `SALOON_RELEASE PASS`, `SALOON_SOCIAL PASS`, `SALOON_POLISH PASS`
  and `SALOON_GAMEPLAY PASS`; exit 0. Log: `/tmp/saloon-022-functional.log`.

# Release 0.2.1 — 2026-11-07

- Mac and Windows builds succeeded: 354,205,448 and 212,966,516 bytes before packaging.
- Two packaged Mac processes passed `SALOON_PRIVACY PASS` and `SALOON_SMOKE PASS`,
  matching results, five participants, no walking and clean exits; `/tmp/saloon-021-lan`.
- Local Mac app copied to `Builds/Playtest-0.2.1/Last Call.app`; bundle version verified 0.2.1.
- Windows file confirmed PE32+ x86-64. Windows hardware runtime/microphones remain a human test.
- ZIP CRC checks and authored whitespace checks passed; checksums accompany the release.
- Earlier Editor checks below cover the source packaged into this release.

# Earlier Editor-only drinking, aftermath and bar-turn fixes — 2026-11-07

No new standalone build, upload or release was made.

- `SALOON_BEER PASS`: first/third-person mug rim now tilts toward the drinker (-Z), rather
  than presenting the base to the mouth. Direction checks and fresh rendered captures pass;
  handoff, delayed healing, interruption and positional audio continue to pass.
- `SALOON_POLISH PASS`: real Sheriff draw/fire commands rejected outside their bar turn;
  a five-player queue contains each occupant once, idle turns auto-pass, and the Sheriff's
  turn permits exactly one shot even with cooldown reset/repeated clicks.
- The final action preserves position and weapon pose in a three-second `Aftermath` phase,
  locks further actions, and retains the spin before regroup. The lethal final-shot fixture
  keeps the dying shooter's recent first-person weapon visible without an elimination/result
  overlay. Roles/results appear only after the three-second delay; the corpse keeps its seat.
- Pure clock checks cover timed-action completion → aftermath → regroup/next round, plus
  final-round aftermath → results. Existing heist/beer fixtures isolate their feature from
  group ordering; the new polish fixture exercises actual group authorization.
- `SALOON_HEIST PASS`, `SALOON_SOCIAL PASS`, `SALOON_RELEASE PASS`, `SALOON_GAMEPLAY PASS`
  and `SALOON_EDITOR_ROUNDS PASS` passed in Unity. Rendered captures include corrected beer,
  `BarOrderPreview.png` and `AftermathPreview.png`. README describes current Editor rules.
- Authored whitespace checks passed. No new physical-device/network session was performed.

# Earlier Editor-only social update — 2026-11-07

No new standalone build, tag, upload or release was created for this update.

- `SALOON_SOCIAL PASS`: real discussion room commands include sender/recipient/room,
  withdraw correctly, and create a room-private coin state for exactly two living players.
  First claim wins against a competing claim; opposite sides are assigned; the winner is
  hidden until reveal. Drawing/firing before the toss and the loser's early fire command
  are rejected without wasting their action. Real opening/response shots apply damage,
  the opener receives no bonus shot, and no-input/dead-opener cases do not stall the room.
- Private Sheriff inspection spends an action, reports missing money without naming the
  carrier, and remains a last-known report after subsequent changes. Other roles receive
  no report or safe-progress value. Inspection creates no global event.
- Revised `SALOON_HEIST PASS`: no observer robbery, carrier, escape or robbery-busy flags,
  including roommates; no forced holster or work/bag geometry. An empty-safe H attempt
  spends its full action/time rather than revealing theft with a free rejected command.
  Real-shot interruption, three shared steps and delayed escape still pass.
- `SALOON_BEER PASS`, `SALOON_RELEASE PASS`, `SALOON_GAMEPLAY PASS` and
  `SALOON_EDITOR_ROUNDS PASS` continue to pass. Actual shot/damage snapshots trigger
  the camera feedback/hit tint and visible first-person muzzle mesh/light in the test.
- UI captures: `InvitationPreview.png`, `CoinChoicePreview.png`, `MuzzleImpactPreview.png`.
  Latest README distinguishes current Editor rules from the published 0.2.0 ZIPs.
- Authored source/documentation whitespace checks passed. New rules/fields have not been
  tested between physical devices; the user's next build remains pending.

# Release 0.2.0 — 2026-11-07

- `SALOON_RELEASE PASS`: no ally/hidden-role payload, stable dead seats and common-table
  identities, no living/corpse overlap, free beer pickup with duplicate protection,
  same-round self-drink healing/action spending, accepted-shot spin/audio, finite unclipped
  gunshot samples, contextual hints. Round clock also reserves corpse seats in side rooms.
- `SALOON_HEIST PASS`, `SALOON_BEER PASS`, `SALOON_GAMEPLAY PASS`,
  `SALOON_EDITOR_ROUNDS PASS` and asset/rule checks passed in the Editor.
- Windows x64 Mono development build succeeded: 212,934,288 bytes before packaging.
- Universal macOS development build succeeded: 354,173,216 bytes before packaging.
- Two real 0.2.0 Mac game processes passed LAN practice on localhost with three active bots:
  both reported `SALOON_PRIVACY PASS` and `SALOON_SMOKE PASS`, five players, no walking,
  identical `LAW WINS — MONEY DEFENDED` result and clean exit. Logs: `/tmp/saloon-020-lan`.
- Windows executable confirmed as PE32+ x86-64; Windows runtime/physical microphones require
  the user's Windows laptop test. No claim of Windows runtime validation on this Mac.
- Both ZIP archives passed complete CRC checks. SHA-256 checksums included with the release.
  Debug-only `DoNotShip` folders excluded. Asset attribution and quick-start instructions included.
- Authored source/documentation whitespace checks passed; imported Unity metadata and original
  third-party license line endings were preserved. Current rules are in README.md; older evidence
  below is historical.

# Earlier source update — robbery, active opponents and beer, 2026-11-07

No standalone build or release was created. Historical checks below describe older
versions; README.md describes the current rules.

- Unity 6000.5.10f1 compiled runtime and Editor scripts.
- `SALOON_HEIST PASS`: bar-only timed work, one-action spending, actual-shot interruption,
  persistent safe progress, third-step collection, no global event, private room-filtered
  work/bag/progress, delayed escape victory, leaving-bar cancellation, revised faction wins,
  and real bot shooting/robbery commands.
- `SALOON_BEER PASS`: giver action/inventory spending, recipient action preserved, travel
  before drinking, delayed healing, actual-shot interruption without revival, valid finite
  procedural sound samples, travelling mug created through snapshot delivery, third-person
  raised mug with positional audio, and first-person recipient mug/audio followed by healing.
- `SALOON_GAMEPLAY PASS`, `SALOON_ROUND_CLOCK PASS`, `SALOON_EDITOR_ROUNDS PASS`: seated
  round flow, shared actions, legal host commands and test shutdown remain functional.
- Rendered handoff, third-person drinking and first-person receiving captures saved as
  `BeerHandoffPreview.png`, `BeerThirdPersonPreview.png`, `BeerFirstPersonPreview.png`.
  Visual inspection corrected mug height/tilt and added the existing character's arm reach.
- Minimal match HUD retained; contextual key instructions remain in Esc/README.
- No new cross-device session or physical-speaker listening test was performed. Audio clips
  and active AudioSources were checked in the Editor; the sounds are procedural alpha cues.
  Detailed finger grips and first-person arms remain unfinished.

## Earlier release — 0.1.2, 2026-11-07

- Separate Last Call Saloon Unity cloud project created and bound to this project.
- Vivox, Relay, and Lobby enabled; anonymous UAS sign-in used for players.
- Two real app processes authenticated and joined the same positional Vivox channel.
- Generated test speech was injected while microphone inputs stayed muted; the receiver decoded remote audio with measured energy 0.0620.
- A private Relay room was created and joined by another app process; both saw two replicated players.
- Two-player LAN practice passed with three inert targets, private roles, movement, and synchronized results.
- Normal first-launch auto-connect produced SALOON_VOICE CONNECTED positional.
- Hat geometry removed from all runtime avatars; hat-free gameplay capture inspected.
- macOS and Windows x64 Mono builds succeeded.
- No Vivox signing key is included in the client or source.

Still requiring the human two-laptop test: physical microphones, perceived distance/panning, and Windows voice runtime behavior.

---

# Verification — 2026-11-07

Unity 6000.5.10f1, macOS development build.

## Five-process networking

- player-0.log: `SALOON_PRIVACY PASS mode=host`
- player-0.log: `SALOON_SMOKE PASS mode=host players=5 role=Renegade outcome=DRAW — TIME EXPIRED movement=True`
- player-1.log: `SALOON_PRIVACY PASS mode=client`
- player-1.log: `SALOON_SMOKE PASS mode=client players=5 role=Outlaw outcome=DRAW — TIME EXPIRED movement=True`
- player-2.log: `SALOON_PRIVACY PASS mode=client`
- player-2.log: `SALOON_SMOKE PASS mode=client players=5 role=Sheriff outcome=DRAW — TIME EXPIRED movement=True`
- player-3.log: `SALOON_PRIVACY PASS mode=client`
- player-3.log: `SALOON_SMOKE PASS mode=client players=5 role=Deputy outcome=DRAW — TIME EXPIRED movement=True`
- player-4.log: `SALOON_PRIVACY PASS mode=client`
- player-4.log: `SALOON_SMOKE PASS mode=client players=5 role=Outlaw outcome=DRAW — TIME EXPIRED movement=True`

## Gameplay integration

- SALOON_CHECK opening blocks weapon drawing
- SALOON_CHECK opening blocks damage and ammunition consumption
- SALOON_CHECK opening transitions to continuous real-time play
- SALOON_CHECK hitscan applies one damage and consumes one round; actual hp=2 ammo=5
- SALOON_CHECK fire-rate limiter rejects an immediate second shot
- SALOON_CHECK other player can damage the host
- SALOON_CHECK bar tap removes one shared beer
- SALOON_CHECK drinking visibly holsters and occupies hands
- SALOON_CHECK healing does not apply before drinking completes
- SALOON_CHECK completed drinking restores one health
- SALOON_CHECK beer transfers to a nearby player through line of sight
- SALOON_CHECK occupied hands prevent firing during drinking
- SALOON_CHECK taking damage interrupts drinking and consumes the started beer
- SALOON_CHECK looking at a stool seats the player
- SALOON_CHECK seated player can stand into free space
- SALOON_CHECK reload has a real vulnerable duration
- SALOON_CHECK occupied hands prevent firing during reload
- SALOON_CHECK reload fills the cylinder after its timer
- SALOON_CHECK solid partition blocks damage
- SALOON_CHECK lethal damage disables the eliminated player's collision
- SALOON_CHECK eliminating one Outlaw does not end a match early
- SALOON_CHECK elimination reaches the victim's private snapshot
- SALOON_GAMEPLAY PASS

## Build and visual inspection

- Scene generation, imported model/texture checks, role deck, and victory precedence passed.
- macOS build succeeded; its Info.plist includes the microphone usage description.
- The built menu and first-person saloon were captured and inspected; Preview.png shows the final build.
- Final gameplay, preview, and five-player network processes exited with code 0.
- Character and weapon presentation remains placeholder quality.

## Not verified

- Relay internet sessions and live Vivox audio: no Unity Gaming Services project exists yet; setup is documented in CLOUD-SETUP.md.
- Remote machines, real network latency, Windows builds, and human social playtesting.

The network smoke test uses five processes on the same Mac, not five remote computers.

## LAN update 0.1.1

- Windows Mono x64 build succeeded (205,433,610 bytes before ZIP compression).
- macOS build succeeded with the same two-player practice feature.
- Two-process practice test passed with three inert targets: movement, role privacy, and synchronized result on both real clients.
- Windows executable was cross-built on macOS; execution on the second Windows laptop is the next manual test.

## Local round-system source — 2026-11-07

No standalone player was built or published for this change. Unity 6000.5.10f1 compiled
both runtime and Editor assemblies, and the final Editor verification returned to Edit
mode with zero Console warnings or errors.

- `SALOON_ROUND_CLOCK PASS`: discussion lock, active-player authorization, single action,
  late timed-action completion, deadline rejection, timeout/pass advancement, skipped
  dead players (including first/final slots and death at expiry), and round cap.
- `SALOON_GAMEPLAY PASS`: real host commands and physics reject discussion/inactive shots,
  enforce one bullet even after cooldown reset, spend misses, block a second major
  action, complete timed reload/healing, budget beer pickup/transfer, allow free seating,
  and preserve health/items/supply/private roles across round transitions.
- `SALOON_EDITOR_ROUNDS PASS`: complete Play-mode test and LAN shutdown without accessing
  an uninitialized Vivox service. Tests do not capture or transmit microphone input.
- The round/turn timer and spent-action HUD were visually inspected in Play mode.
- `git diff --check` passed.

Run from the Editor's **Saloon → Verify rounds in Play mode** menu while in Edit mode.
Remote-device replication of the new round fields has not been tested; the existing
0.1.2 downloads still contain the old continuous-combat rules and should not be mixed
with this source version. Room allocation, robbery, downing, and replacement victory
conditions are design proposals recorded in README.md, not verified gameplay features.

## Low-poly beta visual pass — 2026-11-07

- Installed Blender converted twelve source models with script auto-execution disabled.
  Export report records triangle counts; safe reduced from 12,414 to 3,724 triangles.
- Unity imported models/textures and compiled the runtime and Editor assemblies.
- `SALOON_LOW_POLY_ART PASS` checks all twelve imported model renderers/materials and wood texture.
- `SALOON_ROUND_CLOCK PASS`, `SALOON_GAMEPLAY PASS`, and `SALOON_EDITOR_ROUNDS PASS` passed
  with the saved art overlay, including actual shots, beer transfer/healing and seating.
- Added checks confirm no active floating environment TextMesh labels and that carried
  beer is replicated for other players' mug visuals.
- Scene and compact HUD visually inspected in Unity Play mode; imported weapon orientation
  adjusted after inspection. The final active revolver is TimDev's 816-triangle model.
  Original seat/interaction colliders were retained.
- Scene assets saved without generating a standalone player. This is a local beta art pass;
  first-person arms are staged for grip/action animation, and robbery remains unimplemented.

Final art export completed with the active 816-triangle TimDev revolver, including its
required attribution. Final manual Play preview connected Vivox and confirmed the new
weapon and compact HUD. Stopping the Editor preview produced an SDK-uninitialization
cleanup warning; the automated LAN gameplay checks do not initialize cloud voice.
No new cross-device or standalone-build validation was performed for the visual pass.

## Seated-room cycle and furniture repair — 2026-11-07

- `SALOON_ROUND_CLOCK PASS`: Discussion → Choosing → shared Actions → Regroup → next round/cap;
  late timed actions finish; mutual pair requests, capacity and fallback rules checked.
- `SALOON_GAMEPLAY PASS` / `SALOON_EDITOR_ROUNDS PASS`: real host commands reject movement,
  standing and discussion shots; assign mutual companions to the same room; replicate room
  roster; permit simultaneous actions; reject second actions and late room changes; regroup
  everyone and preserve health/ammunition. Tests run in the existing Unity Editor, no build.
- Rebuilt saved scene with two screened conversation rooms, a shared discussion table,
  inward-facing chairs and closer seating stations. Removed bench and decorative wall slabs.
- Fixed model fitting: retain the FBX import rotation and scale an identity parent instead
  of overwriting/scaling the rotated imported root. Tables/chairs/shelves/cups now stand upright.
- Beer supply collider remains functional with its cube renderer hidden and a mug in its place.
- Manual inspection from the discussion player's seated camera confirms inward-facing players,
  upright furniture and a visible mug. Cross-device replication not run for this source update.


## 0.2.3 joining-player snapshot repair — 2026-11-07

Confirmed the failure before the fix using Unity 6000.5.10f1: a Choosing snapshot with null
`duel` and `roomTurn` serialized both as empty nested objects; deserialization returned both
as non-null. The regression failed with “Inactive room panels survive the snapshot wire round
trip”. This is the exact discrepancy hidden by the old host-only direct-object view.

- `SALOON_SNAPSHOT_WIRE PASS`: absent panels in all seven phases; real coin choosing/playing
  authorization; bar order/current actor including host ID zero; completed/spent queue;
  stale panels removed outside Turns. Uses the production snapshot encoder/decoder.
- `SALOON_ROUND_CLOCK PASS` and rules/assets verification passed before building.
- `SALOON_GAMEPLAY PASS` on the actual 0.2.3 macOS player, including heist, beer, release,
  social/coin and polish/ordered-turn suites. Host rendering now uses the wire round trip too.
- Rendered LAN separate-room regression: host East, client West; both opened the discussion
  planner, rendered the chooser, received room/companion acknowledgements and spent a real
  action before aftermath. No phantom coin/turn panels outside Turns or in solo rooms.
- Rendered LAN mutual-pair regression with live Vivox and muted microphones: both reached
  West, saw the coin choice, spent their authorized actions and reached aftermath while voice
  remained connected. Host/client chooser render counts were 5,432 / 5,390.
- Conversation planner was visually inspected without the phantom room-action panels;
  the normal release menu was also checked to show version 0.2.3.
- Five-process network smoke test: all five returned exit code zero with private roles
  during play, synchronized results/role reveals, and seated movement locked.
- Both release ZIPs passed archive CRC checks; Windows includes the EXE, UnityPlayer DLL,
  data directory and Mono runtime.
- Both platform builds succeeded: universal macOS 354,206,308 bytes, Windows x64
  212,968,048 bytes before ZIP compression.

Test logs: `/tmp/saloon-wire-before.log`, `/tmp/saloon-023-build.log`,
`/tmp/saloon-023-functional.log`, `/tmp/saloon-023-split/`, `/tmp/saloon-023-pair-voice/`,
`/tmp/saloon-023-five/`.
The LAN harness sends choices through production commands and confirms host acknowledgement;
it does not claim automated physical button-click coverage. Windows native execution and the
reported kick-out on the remote laptop were not reproduced/tested on this Mac.


## Native Windows 0.2.3 release checks — 2026-11-07

Executed the published Windows ZIP on GitHub-hosted Windows Server 2022, using the
unchanged released EXE and data directory. The ZIP SHA-256 was checked against the release
checksum before execution: `01b3c17188c13d23742440cc564676e48f0589551c2b142cada625b4e9255ac7`.
No new player build or gameplay change was needed for these tests.

- Full built-player functional suite passed (`SALOON_GAMEPLAY PASS`, exit 0).
- Five independent Windows players completed a match, all exit 0, with private roles,
  synchronized results, and locked seated movement.
- Rendered separate-room and mutual-pair checks passed on both host and client: discussion
  planner, host-acknowledged room/companion selection, room assignments, coin ownership and
  authorized actions through aftermath. Voice-connected paired-room checks also passed;
  microphones stayed muted. This verifies voice connection, not physical audio capture.
- Impaired LAN tests passed in separate and paired rooms: 80ms delay in each direction,
  +/-25ms jitter, 2% random packet loss, and a five-second bidirectional packet outage during
  room selection. Both players recovered and spent their actions. Actual relay counters:
  separate 2,328 forwarded / 490 dropped; pair 2,090 forwarded / 520 dropped, including the
  intentional outage. No unexpected disconnect occurred before successful completion.
- The CI machine uses Microsoft's software Direct3D renderer. Initial rendered runs at
  higher quality exceeded the test deadline before completing the round and failed. Those
  are not counted as passes. After explicitly setting 640x360 and the project's `Very Low`
  quality, all rendered checks passed without changing the released game. Disconnect
  messages following intentional process shutdown are expected test cleanup.
- The tests render the HUD but submit choices through production commands; they do not
  simulate physical mouse clicks. These results do not guarantee every laptop/driver or
  real Wi-Fi environment. Supplied laptop logs came from earlier 0.2.1/0.2.2 sessions and
  do not establish a 0.2.3 failure; the hardware retest remains separate.

Evidence and full logs:

- [Windows gameplay and five-player pass results](https://github.com/kiskalloakos/last-call-saloon/actions/runs/42402360230)
  (this initial run failed overall because of the rendered-test deadline described above).
- [All Windows rendered room and voice checks passed](https://github.com/kiskalloakos/last-call-saloon/actions/runs/42403329360).
- [All Windows impaired-LAN checks passed](https://github.com/kiskalloakos/last-call-saloon/actions/runs/42403332812).

Re-run from GitHub Actions → **Test published Windows game**, selecting release `v0.2.3`
and suite `standard` or `stress`; `rooms` runs only the rendered cases. Logs are retained
as workflow artifacts for 14 days. Test tooling lives in `Tools/windows_release_test.py`,
`Tools/lan_room_test.py` and `Tools/udp_test_link.py`.


## 0.2.4 slow-LAN snapshot stream — 2026-11-07

The real Mac-host/Windows-client retest exposed a gap in the 0.2.3 checks: the host reached
Turns while the joining player's room countdown stopped, followed by a remote close and
match abort. The old snapshots were full UTF-16 JSON at 10 Hz, delivered through a reliable
fragmented FIFO. Previous loss/outage tests had not limited available bandwidth.

0.2.4 replaces that stream with UTF-8/deflate snapshots in datagrams of at most 1,012 payload
bytes, with explicit version/part metadata. The receiver keeps at most two incomplete
states, validates bounded sizes and ignores duplicate/stale updates; a missing state cannot
block a subsequent complete state. Commands remain reliable. Transport packet queues are
1,024 packets rather than 128. Existing disconnect timeouts are unchanged. Startup and
both sides of disconnect now log build/platform, transport reason, state age/sequence,
byte counters and timestamps. The HUD shows a stale-state warning and fits abort text.

Confirmed evidence:

- `SALOON_SNAPSHOT_STREAM PASS` in Unity: loss, reverse packet order, duplicates, stale
  updates, sequence wrap, UTF-8, missing-panel presence flags, metadata bounds, and a
  portable raw-deflate fixture. `SALOON_SNAPSHOT_WIRE PASS`, clock and asset/rule checks passed.
- Full Mac built-player functional suite passed. Real five-player state size was 6,478
  legacy bytes versus 787 compressed bytes before chunk headers (about 88% smaller).
- Mac before/after bandwidth test: 8,000 bytes/s host-to-client, 80ms delay each way,
  +/-25ms jitter, 2% random loss and a five-second outage during Choosing. The old client
  remained behind while the host completed actions; the final 0.2.4 pair reached all phases,
  selected companions/rooms, claimed the coin and spent both actions with voice connected.
  Maximum observed phase lag in that final run was below the 250ms polling interval.
- Native Windows old-build baseline failed both capped-link cases, unable to finish joining
  before the harness deadline. This also reproduces a slow-join failure under limited
  bandwidth. [Old Windows baseline](https://github.com/kiskalloakos/last-call-saloon/actions/runs/42405044472).
- Native Windows 0.2.4 capped-link cases both passed, using exactly the candidate ZIP checked
  against its release SHA-256. Both clients joined, recovered from the five-second outage,
  completed room/companion/coin selection and spent their actions. Room/action transition
  lag was at most about 0.3s; initial discussion visibility lag was about 1.05s. The final
  receive log shows state age returning from 4.38s during the outage to 0.21s after recovery.
  [Windows capped-link pass](https://github.com/kiskalloakos/last-call-saloon/actions/runs/42405254189).

The cap and outage affect game UDP traffic; Vivox voice follows its own service connection.
Windows rendered checks use a Windows Server 2022 software GPU at 640x360 / Very Low.
The tests render controls and send production room/action commands; they do not simulate
physical mouse clicks or prove operation on the user's specific laptop/Wi-Fi. The host's
0.2.3 log did not preserve its remote-client transport reason, so the precise reason for
that real disconnect cannot be established retrospectively. No personal laptop logs are
included in this repository.


Final 0.2.4 validation and remaining limitation:

- The exact final Mac binary passed the full functional suite and a five-process match;
  all five processes exited 0. Final Mac capped-link pair/voice test passed on both sides.
- The exact Windows release binary passed the full functional suite, separate rooms,
  paired room/coin actions, and voice-connected paired room. The Windows standard run
  is red overall because its five-player host did not exit after a completed match;
  all five logs contain `SALOON_SMOKE PASS` before shutdown.
  [Windows gameplay/room/voice results](https://github.com/kiskalloakos/last-call-saloon/actions/runs/42405251569).
- Repeated five-player Windows matches: all three matches reached successful results on
  every client. Runs 2 and 3 exited 0 on all five processes; run 1 again hung during the
  host's intentional post-match shutdown. This shutdown issue is not claimed fixed and
  must not be described as an entirely green Windows suite. It was not an unexpected
  mid-match disconnect. [Repeat logs](https://github.com/kiskalloakos/last-call-saloon/actions/runs/42405884748).
- Both Windows capped-link cases passed with clean exits on both host and client:
  [successful constrained-LAN suite](https://github.com/kiskalloakos/last-call-saloon/actions/runs/42405254189).
- Both platform ZIPs passed CRC checks and their uploaded sizes/SHA-256 matched local
  files. Windows ZIP SHA-256:
  `99dc5ad8788b367fe2cff27e321563b8c52a0ad308b52134c36f755b526d2884`.
- The first two Windows candidate attempts failed at download because a read-only Actions
  token cannot access a draft release. No game test ran in those attempts. The candidate
  was then explicitly published as a prerelease for validation, with 0.2.3 remaining latest.


## Local 0.2.5 presentation, spectator and Ready iteration — 2026-11-08

This iteration is local only; no GitHub release, push or cloud CI was performed.
Both players were built to `Builds/Local/` with version 0.2.5. Windows was cross-built
on this Mac and was not executed natively in this iteration.

- Ready-driven round clock passes: elapsed time cannot end discussion, all living players
  must confirm, unready revokes confirmation, eliminated players do not block, each new
  discussion clears Ready, and all-ready enters actions directly. Timed actions/aftermath
  and the round cap retain their existing checks.
- Runtime presentation checks pass: room edits clear Ready; both players at a two-player
  bar face each other; pre-assignment look packets are rejected using pose versions;
  first-person view has a skinned hand; the revolver rolls backward while the hand remains
  planted; eliminated players enter and cycle through living POVs without revealing roles.
- The existing full built-player gameplay/heist/beer/social/polish suite passed after its
  fixtures were changed to confirm Ready through the production readiness path.
- A rendered two-process Mac LAN pair passed discussion/planning, mutual invitations,
  Ready, room assignment, coin choice and action acknowledgements on both host and client.
- A second rendered LAN run passed both sides with an 8,000-byte/s cap, packet loss,
  jitter and a five-second blackout during discussion/planning. Both processes exited 0;
  the client stayed connected through recovery and the largest observed phase lag was 0.26s.
- Rendered captures are in `Builds/Local/Preview/`: discussion/Ready, coin-facing, first-
  and third-person grips, recoil/roll frames, and two spectator POVs. Visual inspection
  corrected wrist/palm alignment and clipped spectator button labels.
- Local controls: Space or the Ready button confirms discussion; right mouse lets you look
  around the planning panel; Left/Right arrows or spectator buttons cycle living POVs.
- These are procedural grip/arm poses, not authored character animations. Voice isolation,
  hidden-role privacy and the one-action rule remain in place.

## Local 0.2.6 practice rooms, fair turns and friendly fire — 2026-11-08

Local only, with Mac and Windows builds in `Builds/Local/`. Windows was cross-built on
the Mac; native Windows execution was not performed for this iteration.

- Bots now choose stable plans across all three rooms, rotate those plans each round,
  accept human invitations and return to their own plan if an invitation is withdrawn.
  A money carrier stays at the bar. Actual Ready-driven room assignment is checked.
- Bar turns have a ten-second minimum. A delayed host frame past the shared deadline
  can time out only the current actor; waiting actors receive fresh full windows.
  The regression puts the human last after four bots and confirms their final action works.
- Shooting a faction ally removes one HP from the shooter in addition to the target's
  normal one HP damage: Sheriff/Deputy in both directions, and Outlaw/Outlaw. Enemy hits
  are exempt. Built-player checks cover both factions, Renegade/enemy shots, denied repeat
  shots, ammunition/action consumption, and a lethal shot that eliminates both allies.
- The complete built-player gameplay, heist, beer, release, social, polish and new practice
  regression suite passed and exited 0. Both platform builds and rule/clock checks passed.
- The rebuilt rendered host/joined-client pair passed planning, Ready, mutual room
  assignment, coin choice and both action acknowledgements; both exited 0.

## Local 0.2.7 public Sheriff visibility — 2026-11-08

- Added a persistent top-left public Sheriff panel, with `(YOU)` for the local Sheriff
  and an explicit eliminated status. Other roles remain private.
- A built-player regression passed 100 production shuffles/deals, checking the full deck,
  exactly one Sheriff, and the compressed private snapshot for all five recipients on
  every deal. Both local-Sheriff and remote-Sheriff panels were captured and inspected.
- Rule/clock checks and both local platform builds passed. Windows was cross-built,
  its ZIP verified, and was not executed natively. No GitHub push or release was performed.

## Local 0.2.8 planning role shortcut and revolver motion — 2026-11-08

- Tab is read independently of mouse lock and clears on release, menu opening or death.
  A native keyboard test pressed Tab with discussion/planning open and the pointer free,
  captured the visible private role card, passed and exited 0. No right mouse input was used.
- Removed the first-person arm. Recoil now has a short eased attack/recovery; the backward
  revolver roll lasts 0.95 seconds with zero endpoint velocity/acceleration, subtle movement
  during the flourish, and a full return to rest. Draw/reload tilt blends gradually.
- Built-player presentation checks passed: recoil recovery, backward/full-circle roll,
  angular step bounds sampled at 60 Hz, no first-person arm, and spectator POV switching.
  Rest, recoil and roll frames were rendered and visually inspected.
- The full gameplay/heist/beer/release/social/polish/practice suite passed and exited 0.
  Both local platform builds passed; Windows was cross-built and its ZIP integrity checked,
  without native Windows execution. The normal local Mac build was opened at version 0.2.8.
- This iteration remains unpublished; no GitHub push or release was performed.

## Unity Editor iteration: simple role cards and straight revolver draw — 2026-11-08

- Source/Editor changes only. No player build, version change, GitHub push or release.
- Holding Tab shows a larger private card for the player's role: plain-language goal,
  exact action keys, shared H/X robbery and escape steps, win conditions and friendly-fire rule.
  All four cards were rendered and visually inspected in the Unity Game view without clipping.
- Removed the generic busy roll that tilted the revolver sideways while drawing. Draw and
  holster now use an eased vertical motion with a fixed horizontal position and no yaw/roll.
  Shot recoil and the backward flip curves are unchanged.
- The Editor-only check passed three draw/holster cycles, sampling horizontal position,
  rotation, monotonic vertical movement, settled visibility and accepted draw commands.
- Re-run using `Saloon > Verify role cards and revolver draw in Play mode`.
  Results: `SALOON_EDITOR_PRESENTATION PASS` and `SALOON_EDITOR_ITERATION PASS`.
  Preview PNGs are stored in the OS temporary directory under `SaloonPrototype-EditorPreview`.
- Verification stopped Play mode and left `Assets/Scenes/Saloon.unity` open in Unity for testing.

## Unity Editor iteration: ragdoll deaths and observer cleanup — 2026-11-08

- Lethal shots now pass their impact direction into an eleven-body, jointed cosmetic
  ragdoll. The body is thrown from its seat, collides with the room/floor, then holds its
  resting pose. The dying player's camera briefly follows it from outside the body.
- The host publishes death direction, age and observer state in snapshots. About 3.2
  seconds after death, the corpse, nameplate and cosmetic collisions disappear as the
  dead player changes to a living player's POV. Later assignments can use the freed seat.
- Ragdoll collisions are excluded from weapon/interaction raycasts and cannot move living
  players. New matches remove the added physics components and restore living models.
- Editor checks passed real lethal-shot launch, floor landing/rest, shared observer-state
  snapshot serialization, corpse/collider cleanup, local death camera, spectator handoff,
  freed-seat reuse, a side-room wall/landing case and new-match restoration.
- The full Editor gameplay, robbery, beer, release, social, polish and practice regression
  suite passed. Historical documentation screenshots generated by that suite were restored;
  ragdoll previews remain in the OS temporary `SaloonPrototype-EditorPreview` directory.
- Re-run via `Saloon > Verify ragdoll deaths in Play mode`. No new builds or version change;
  no GitHub push or publication. Unity remains open for manual testing.

## Unity role-guide correction — 2026-11-08

The Tab card's right column is now role-specific. Sheriff receives an inspection and
protection plan; Deputy receives a support/protection plan with shooting, beer and asking
the Sheriff for their report. Only Outlaw and Renegade receive the H/X robbery walkthrough.
The Deputy action text no longer leads with H/X, and the common footer no longer lists
Sheriff-only inspection as an action for every role. Gameplay permissions are unchanged.
The Editor check passed all four cards and confirmed Law plans contain no H/X robbery
walkthrough. Sheriff and Deputy captures were inspected for readability and clipping.
Unity was left open; no build was created.

## Role rules corrected to the user's design — 2026-11-08

This replaces the earlier generalized money objectives, including the earlier Law/Renegade
money-escape paths. Only Outlaws may rob, collect money or escape with it. Sheriff and
Deputy protect the money. Renegade fights the Outlaws first, then the Law, and wins as
the sole survivor; Renegade has no money objective.

- Host checks now reject non-Outlaws at robbery start, completion/collection, escape
  declaration and escape resolution. Denied H/X commands leave the action available.
  Injected bag/escape state cannot give a non-Outlaw an escape win or money HUD prompt.
- Outlaw tests still complete safe work, collect money and win after the full escape
  response round. Empty-safe probing remains an Outlaw action with private feedback.
- Renegade bot tests verify attacks on Outlaws while they live, followed by attacks on
  the Law once all Outlaws are dead. A real final shot queues and completes the Renegade's
  last-survivor victory without robbery or money escape.
- The complete Editor clock, wire, gameplay, heist, beer, release, social, polish and
  practice suites passed. Role guides, contextual hints, menu rules and README were updated.
- Editor/source only; no new builds, version change, GitHub push or publication.


## Four/five-player matches and optional LAN bots — 2026-11-08

- LAN creation and the host's lobby offer **No bots / With bots**. With bots, the host
  chooses a four- or five-player fill target. Bots are created only on Start; turning
  bots off removes existing bots without silently replacing them on Start.
- Four players receive exactly Sheriff, Deputy, Outlaw and Renegade. Five players receive
  one extra Outlaw. The four-player Outlaw Tab guide explains that there is no teammate.
- Network admission allows eight humans including the host; a ninth is rejected with
  a full-lobby reason. Rules for six through eight players remain undefined, so those
  lobby sizes cannot start. A joining human can replace a bot after a match ends.
- `Saloon > Verify LAN lobby and four-player matches in Play mode` passed with real
  Unity Transport/Netcode UDP clients in the Editor process: four- and five-human role
  deals, private role snapshots, room/Ready commands, distinct bar seats, every player's
  queued turn and reliable pass commands. All clients remained connected through each round.
- The same test passed eight-human roster replication, rejection of a ninth connection
  without dropping the original eight, rejection of undefined match sizes, bot fill to
  four/five, disabling bots, replacing a bot with a human and shrinking the bot target.
- Results: `SALOON_LOBBY PASS` and `SALOON_EDITOR_ITERATION PASS` in
  `/tmp/saloon-editor-roles-corrected.log`. Lobby captures in the temporary
  `SaloonPrototype-EditorPreview` directory were visually checked.
- The full Editor snapshot/clock, gameplay, heist, beer, release, social, polish and
  practice regression suite also passed (`SALOON_EDITOR_ROUNDS PASS`). Historical
  documentation screenshots generated by that suite were restored.
- These are same-machine Editor network tests, not a native Windows or cross-device LAN
  retest. No builds were made, no version was changed and nothing was published.


## Western Gunman character and joint motion — 2026-11-08

- Acquired and imported **Low Poly Western Starter Pack 1.0** by tt-3D through the Unity
  Asset Store after explicit approval of Unity's Terms of Service and Asset Store EULA.
  Original pack files remain in `Assets/tt-3d/Low-PolyWesternStarterPack`.
- Every player now uses the same Gunman, original textures and shared materials. The
  runtime prefab is `Assets/Resources/Art/Western/Gunman.prefab`, normalized uniformly
  to 1.78 metres including the hat. Character colours no longer vary by player.
- Added game-driven joint motion on the imported Humanoid skeleton: breathing idle,
  direction-aware walking, eased sitting/standing with bent knees, aim/recoil on shots,
  and drinking with the mug's rim at the mouth. These movements are implemented by the
  game, not animation clips supplied by the pack. Weapon and mug grips use this rig's
  hand, forearm and finger bones. Existing first-person revolver motion is preserved.
- The Editor character verification passed identical model/material checks, real host
  movement with leg motion, bent-knee seating without scale compression, a real draw
  and shot, recoil/flip previews, a real drink with mouth contact and health recovery.
  All seven captured poses were inspected in `SaloonPrototype-EditorPreview`.
- The new skeleton also passed the complete ragdoll/death-camera/observer regression:
  eleven connected bodies, shot launch, floor landing and rest, corpse removal,
  spectator POV handoff, side-room landing and restoration for a new match.
- The full Editor snapshot/clock, gameplay, heist, beer, release, social, polish and
  practice suites passed with the new character (`SALOON_EDITOR_ROUNDS PASS`).
  Historical documentation screenshots generated by that suite were restored.
- Re-run via `Saloon > Verify Western character animations in Play mode` and
  `Saloon > Verify ragdoll deaths in Play mode`. Results: `SALOON_WESTERN PASS`,
  `SALOON_RAGDOLL PASS` and `SALOON_EDITOR_ITERATION PASS` in
  `/tmp/saloon-editor-roles-corrected.log`.
- Editor/source only. No player builds, version bump, GitHub push or publication.


## Wonky speech-driven mouths — 2026-11-08

- Added `SaloonTalk`, a lower-face blend shape affecting 54 vertices on a derived copy
  of the Gunman body mesh. The original pack model is unchanged. A small dark mouth
  opening follows the head and changes height/width with an uneven chatter rhythm.
- Vivox speech detection gates animation; AudioEnergy adjusts its strength, with a
  fallback for platforms reporting speech without energy. Silence/mute closes the
  mouth promptly. Drinking and elimination immediately close it before other animation
  or ragdoll takeover. No microphone recording or audio-sample capture was added.
- Each client announces its signed-in Vivox player ID through a reliable game command;
  the host includes that ID in player snapshots. Cached activity is matched by that ID,
  so players sharing a display name do not animate one another's mouth. Voice activity
  and audio still come directly from Vivox rather than adding game-network audio traffic.
- `Saloon > Verify talking mouths in Play mode` passed simulated voice activity/energy,
  changing jaw weights, duplicate-name identity isolation, silence/mute, drinking
  priority, resuming speech, elimination and disconnected cleanup. Quiet, speaking and
  stopped face captures were visually inspected. No live microphone session was used.
- The real UDP LAN Editor suite also passed reliable identity announcement and correct
  voice-to-avatar mapping on all clients in four- and five-player rounds. Existing
  eight-place capacity, ninth-join rejection, bot choices and connection checks passed.
- Results: `SALOON_SPEECH PASS`, `SALOON_LOBBY PASS` and `SALOON_EDITOR_ITERATION PASS`
  in `/tmp/saloon-editor-roles-corrected.log`. Preview images remain in the temporary
  `SaloonPrototype-EditorPreview` directory.
- Editor/source changes only; no builds, version changes or publication. Unity remains
  open on the Saloon scene with Play mode stopped.


## Visible hit reactions and corrected revolver grip — 2026-11-08

- Non-lethal damage now drives a directional pelvis/torso/head flinch with a short
  warm-red material flash, followed by a 0.7-second recovery. Authoritative player
  positions, seats, collision bodies and aim inputs are unchanged. Lethal hits continue
  directly into the existing ragdoll.
- Host snapshots carry a hit counter, direction and age. Each fresh hit triggers once;
  repeated snapshots do not restart it, old events expire, and new matches clear it.
  The material flash uses renderer property blocks and preserves shared materials.
- Replaced the revolver's forced wrist orientation and per-finger world rotations with
  an arm pose that keeps original wrist/finger joint rotations and bone lengths. The
  gun attaches at the palm, with the barrel following the player's aim. First-person
  draw, recoil and backward-roll curves remain unchanged.
- Added a compact `SaloonGunHold` shape on the derived character mesh. The source's broad
  mitten polygons fit around the handle instead of folding into long spikes. It blends
  during draw/holster, releases for drinking and death, and coexists with `SaloonTalk`.
- The final combat Editor test passed three aim pitches, seated/standing grips, native
  wrist/finger rotations, unchanged arm lengths, palm attachment and barrel alignment.
  Front/profile rendered close-ups of the final hand were inspected. A preview-only
  light was used for hand inspection and removed at the end of the test.
- A real legal shot passed damage, visible directional flinch/flash, compressed hit-data
  round-trip, duplicate suppression, pose recovery, fixed collision positions, a second
  directional hit and lethal ragdoll takeover. Result: `SALOON_COMBAT PASS` and
  `SALOON_EDITOR_ITERATION PASS` in `/tmp/saloon-editor-combat.log`.
- The real UDP LAN Editor suite passed hit-counter/direction delivery to every client in
  four- and five-player matches, plus all existing turn, capacity and bot-setting checks.
  The full snapshot/clock, gameplay, heist, beer, release, social, polish and practice
  regression suite also passed (`SALOON_LOBBY PASS`, `SALOON_EDITOR_ROUNDS PASS`).
  Historical documentation images written by that suite were restored; final combat
  previews remain in the OS temporary `SaloonPrototype-EditorPreview` directory.
- Editor/source changes only; no new builds, version change, GitHub push or publication.


## 2026-11-08 — headshots, persistent beer healing and room movement

- Only the dedicated head sphere accepts bullet damage (1 HP). Body hits still spend
  ammo/action, cause no HP loss or hit reaction, and do not apply friendly-fire penalties.
  Standing CharacterControllers cannot swallow head rays; furniture still blocks shots.
- F stands/sits for free during room actions, including before/after a player's turn.
  WASD uses host CharacterController collision and stays inside the assigned room.
  Discussion remains seated. Standing checks nearby clear positions; side-room chairs
  need more clearance than bar stools. Bar stations now align with the five actual stools.
- Non-lethal damage preserves normal/gifted drinking and the original completion time.
  Lethal damage clears the timer, disables head collision and cannot revive the victim.
- Bots commit to a head position for 0.3 seconds before firing, allowing a sidestep to
  dodge. Their existing legal actions, target priorities and resources are unchanged.
- Replaced the stretched bar visual with a paneled oak/walnut counter, brass rail/taps,
  bottle shelves, backbar panels, wall trim, lamps and room pictures. Geometry is combined
  by material. Existing licensed local textures/props and collision geometry are reused.
- `SALOON_ROOM_ACTIONS PASS`: actual command/raycast tests covered free standing/sitting,
  movement after passing, both rooms, room bounds, discussion locks, body/head shots,
  friendly body hits, seated/standing headshots, human/bot dodges, visible head alignment,
  normal/gifted healing through damage, and lethal non-revival. Rendered bar preview
  inspected at OS temporary `SaloonPrototype-EditorPreview/room-actions-new-bar.png`.
- `SALOON_LOBBY PASS`: real UDP clients in four- and five-player Editor matches stood and
  moved while waiting, replicated standing poses, received every turn and stayed connected.
  Capacity, private roles, bots and replacement checks also passed. This uses clients in
  one Editor process; it is not a run on the Windows computer.
- Full clock/snapshot, gameplay, heist, beer, release, social, polish and practice suites
  passed (`SALOON_EDITOR_ROUNDS PASS`). Final combat/grip/flinch/wire/recovery/ragdoll
  checks passed too (`SALOON_COMBAT PASS`, `SALOON_EDITOR_ITERATION PASS`).
- Checks ran in the open Unity Editor, logged in `/tmp/saloon-editor-combat.log`.
  Historical documentation images generated by regression checks were restored.
  README explains uninterrupted beer healing; README and in-game controls explain
  headshots and F/WASD movement.
  No new player builds, version bump, GitHub push or release.


## 2026-11-08 — bots stand and dodge during room actions

- Bots now stand automatically at the start of room actions, strafe around nearby
  opponents and change direction/pause at varied intervals. Movement runs independently
  of turn order and spent actions, through the same host input and CharacterController
  collision path as human movement. A short capsule sweep avoids furniture/players;
  predicted movement and host clamping keep bots in their assigned room.
- Bots pause for drinking, handoffs and safe work; they can continue moving while
  drawing/reloading/shooting. Existing committed aim points and action authorization
  remain intact. They stop at aftermath and return to seated discussion.
- Added the Editor menu `Saloon/Verify bot dodging movement in Play mode`. Its production
  movement loop passed automatic standing in all three rooms, actual displacement after
  leaving chairs, replicated walking animation, room bounds, waiting/spent-turn movement,
  unchanged ammo/actions from roaming, dodging a real pre-aimed headshot, and a moving
  bot drawing/landing exactly one legal headshot. Drinking pauses, aftermath stops and
  seated discussion also passed: `SALOON_BOT_MOVEMENT PASS` and
  `SALOON_EDITOR_ITERATION PASS` in `/tmp/saloon-editor-combat.log`.
- README describes bot movement. Editor/source only; no new player builds or release.


## 2026-11-08 — 0.2.9 downloadable release validation

- Created universal Intel/Apple Silicon macOS and Windows x64 Development players from
  current source, version 0.2.9 (`Saloon/Build current downloadable release`).
- The first packaged-player test exposed stripping of `Unlit/Color`, used by talking mouths.
  Character creation failed on the host. The release preparation now retains this shader
  (and validates the runtime sprite shader) in GraphicsSettings. Both players were rebuilt.
- Added `Tools/lan_match_test.py` and opt-in `lan-match-host/client` player automation.
  These run one host plus three/four independent processes, without bots, for three rounds:
  Ready/room planning, all-player bar turns and free movement, two paired rooms with coin
  choices and actual headshots, bar beer healing, and complete final results. Private role
  decks are checked across all process logs. Delayed links can be injected per client.
- Final packaged Mac four-process test passed all clients through all three rounds and
  kept every connection until results. Exact four-role deck passed. Logs:
  `Builds/0.2.9/TestResults/mac-four-final/`. Tests use separate processes on one host;
  they cannot certify four physical devices' router/Wi-Fi/firewall configuration.
- Extended the native Windows workflow with a LAN suite for four/five players and impaired
  four-player links. Platform results are recorded below after that workflow finishes.
- Public source excludes the Asset Store pack and derived Gunman files; compiled players
  contain the licensed character. ZIPs include setup instructions, role README and credits.

- Native candidate Windows testing passed functional gameplay, five-player smoke, a full
  five-player three-round match and impaired four-player three-round play. The normal
  four-player test exposed E/B test commands sent inside the 90 ms input debounce;
  its driver now waits 250 ms and retries only while the drink remains unspent.
- Rendered candidate tests also exposed a real state-stream bug: NGO's FastBufferReader
  includes its consumed eight-byte named-message hash in Length. The old chunk-size
  guard rejected full 1000-byte chunks (and therefore every multi-packet update).
  The handler now bounds unread bytes (`Length - Position`). Added a regression through
  the actual connection handler with a consumed hash prefix and several full-size chunks;
  this passed in release preparation. Both platforms were rebuilt with the correction.
- Legacy rendered room fixtures now allow the legitimate coin caused by a bot joining
  the human's chosen side room. Their test logs use absolute paths on macOS.

- Final packet-fixed Mac players passed four-process three-round LAN again, plus rendered
  separate-room and paired-room host/client flows. Previously stalled paired clients now
  receive aftermath and report success. New ZIPs passed CRC and contents checks.
- Final native Windows LAN suite succeeded on Windows Server 2022:
  https://github.com/kiskalloakos/last-call-saloon/actions/runs/42485842797
  It downloaded/verified the final Windows ZIP and passed four-player and five-player
  three-round matches, exact decks and impaired four-player links. Every process completed.
- Final native Windows standard suite succeeded:
  https://github.com/kiskalloakos/last-call-saloon/actions/runs/42485846945
  Functional gameplay, five-player smoke, rendered separate/paired rooms and paired
  Vivox voice all passed. Both workflows ran source d9be3a6 and the final published ZIP.
  Full artifacts are retained under `Builds/0.2.9/TestResults/windows-final-*` locally.
- Final archive hashes:
  Windows `053777f32351273431125b058fdfa5c2038ce74fd81dad6942ee2012b5cfaa82`;
  macOS `253503bcc7fabc142dee2f2e090e63014bfd0a67fdf1b0edf4e49341c3189868`.
  GitHub's asset digests match these values. The candidate tag was advanced without
  forcing, from this task's initial candidate to the tested packet-fix commit d9be3a6.
  Version 0.2.9 is promoted to the latest release after validation. Four physical devices
  were not available; the included LAN setup guide covers same-network/IP/firewall needs.


## 2026-11-08 — reported Windows failure after 0.2.9

- User reports a Windows crash and inability to choose rooms/companions. No log from
  this failed run has been supplied yet; earlier attached Player logs are no longer
  present locally. Native crash versus return-to-menu is still unconfirmed. Do not
  interpret the previous CI passes as proof that this user's failure is resolved.
- Previous rendered automation sent choices through Game.ChooseRoom and set AutomatedInput,
  so it did not prove normal pointer/button handling. Tested a normal joined Editor client
  against a separate packaged host, with automatic voice active and real mouse clicks.
- Reproduced a concrete planner race with 500 ms delay each way: click West, then a
  companion before the server reply. The companion callback read Self.chosenRoom from
  the older Bar snapshot and sent Bar again. The visible room reverted to Bar.
- Added a per-discussion local plan draft used by room, companion, Go alone and Ready
  controls. It displays immediately, retains the newest choice across old snapshots,
  shows a saving notice until the server matches, and resets for a new round/phase/leave.
  Automated Ready loops now avoid toggling an already pending local Ready request.
- `SALOON_PLANNING PASS`: rapid room/companion choices, stale acknowledgements,
  Ready/unready, West/East/West ordering, Go alone, editing clearing Ready, confirmation
  and new-round reset. Regression menu: Verify rapid room and companion choices in Play mode.
- Repeated the same real mouse clicks over the same delayed connection with normal input:
  West and the companion remained selected after host acknowledgement and the saving
  notice cleared. Logs include `SALOON_PLAN send ... room=1 companion=10002 host=False`
  in `/tmp/saloon-input-investigation.log`. Temporary local delay relays were stopped.
- Future build logs include Application.buildGUID to distinguish binaries sharing a
  version label. No new build, version bump or GitHub update was made for this repair.
- Windows crash investigation remains open pending the failed run's Player.log (and
  Player-prev.log if the game was reopened). This local planner fix is not a verified
  fix for a native Windows crash.
- The existing full round/snapshot, gameplay, heist, beer, release, social, polish and
  practice regression suite passed after the draft change (`SALOON_EDITOR_ROUNDS PASS`).
  Historical documentation captures generated by that suite were restored.


## 2026-11-08 — Windows report clarified as returning to main menu

- User confirms the Windows process remains open and returns to its main menu; both
  room and companion choices were unresponsive. This narrows the remaining report to a
  disconnect, not a confirmed native crash. The failed run's transport message/log is
  still needed to establish who closed the connection and why.
- Traced the menu path: the transport's local disconnect callback raises LostConnection;
  the game clears the session and retains the reason. Round/planning code has no ordinary
  kick path for a joined participant (the explicit capacity rejection runs only on join).
- Added a clearer timeout/remote-close/rejection summary and a copy-details button after
  disconnection. The first unexpected reason survives shutdown cleanup; reports include
  build GUID, platform, UTC, transport event, raw reason, last state/age, main-thread gap
  and focus. Actual frame stalls over two seconds are logged separately. Timeouts and
  match-abort rules were not changed to conceal the failure.
- `SALOON_DISCONNECT_TEST PASS` used a real UDP host/client, an explicit host disconnect,
  captured reason/context and cleanup preservation. `SALOON_EDITOR_ITERATION PASS` in
  `/tmp/saloon-input-investigation.log`. Initial fixture checked Connected too early
  inside the callback; final fixture waits until Netcode completes its shutdown.
- Local source only. No new downloads were published. The actual reported unexpected
  disconnection remains unresolved pending the Windows menu message / Player.log.


## 2026-11-08 — four-player recovery and 0.2.10 test candidate

- User no longer has the failed session's disconnect message/log. Continued without it;
  the cause of that specific unexpected return to menu is unconfirmed.
- `SALOON_RECOVERY PASS` and `SALOON_EDITOR_ITERATION PASS` in
  `/tmp/saloon-recovery-verification.log`: four actual UDP connections through separate
  relays, one joined peer loses every packet for 13 seconds during discussion, its reliable
  room/companion edits arrive after recovery, fresh snapshots resume, all four Ready and
  complete their bar turns without disconnecting. This runs within one Editor process;
  it does not represent four physical devices or a native Windows execution.
- Added an opt-in 13-second blackout case to the independent-process packaged LAN harness
  and Windows LAN suite. Only that test mode tolerates the deliberately stale snapshots
  and holds Ready until recovery. Production timeouts and disconnect behavior are unchanged.
- Preparing uniquely versioned 0.2.10 test downloads, retaining 0.2.9 as the stable release.
  The previous notes saying no new builds describe the earlier local repair stage.

- Built both targets successfully (`SALOON_CURRENT_RELEASE PASS`) from source commit
  `34ac11ed9637e5ca139d226adc837903320187d8`. Mac executable contains x86_64 + arm64.
  Published 0.2.10 as a prerelease with fixed ZIPs (no replacing binaries under one version).
  Windows SHA256: `02fca0fc62d80a9d43e82049541eaa8da4045cedf4e1ff29dc85cbd317ebf9ea`;
  Mac SHA256: `817a657a49c71a17cc8c20b0d08a1c5d25c39d2fd872db904f7360431367b8b0`.
- Packaged Mac checks in `Builds/0.2.10/TestResults`: `mac-four-direct` passed all four
  processes and all three rounds; `mac-four-blackout-retry` passed all four, queued
  transport recovery, exact role deck, movement, coin/headshots, beer healing and results
  after an actual 13-second packet outage. `mac-pair-voice` passed rendered room planning,
  mutual invitations, coin choice and room actions with Vivox connected and a 5-second
  outage; phase lag after recovery was at most 0.26 seconds. `mac-functional.log` passed
  `SALOON_GAMEPLAY PASS` and the included gameplay regressions.
- Preserve the first failed run: `mac-four-blackout` stayed in Lobby because one of three
  joined processes never completed its initial transport handshake (`MaxConnectionAttempts`);
  the other two joined normally, the relays reported no errors. No blackout was injected
  because Discussion never began. Direct joining and the same impaired test on fresh ports
  then passed. This intermittent join failure has not been diagnosed; do not report every
  test run as passing or claim the user's original connection issue is fully resolved.

- Actual published Windows ZIP passed both Windows Server 2022 native jobs:
  [LAN run 42494738932](https://github.com/kiskalloakos/last-call-saloon/actions/runs/42494738932)
  and [standard run 42494741800](https://github.com/kiskalloakos/last-call-saloon/actions/runs/42494741800).
  Inspected downloaded artifacts: every results.json item passed, exit 0, no timeout;
  both release.txt files match the Windows SHA256 above; no runtime exceptions in saved
  game logs. LAN: four players, five players, four with delay/loss, four with delay/loss
  plus a confirmed 13-second blackout. Standard: functional gameplay, five-player smoke,
  separate rooms, mutual-pair rooms and mutual-pair rooms with real Vivox sessions.
- Kept the 0.2.10 release marked prerelease. The exact reported Windows disconnect and
  the single intermittent impaired initial join remain unconfirmed/unresolved. These tests
  use multiple processes on one machine per platform, not four separate physical devices.


## 2026-11-09 — private discussion evidence (Unity development source)

- Added a host-authoritative private notebook, six latest entries with round labels,
  shown during discussion. Entries persist across rounds and reset with the heist for
  a new match. Every snapshot carries only its recipient's notebook; spectator POV
  never exposes the followed player's clues. No global clue messages or role reveals.
- A living bar player must look toward a living safe worker within 12 units, unobstructed,
  continuously for 0.75 seconds. Looking away or losing sight resets the observation.
  The clue names the worker but explicitly does not claim completion or money ownership.
- Sheriff C retains its role/room/action restrictions. Reports include new marks since
  the previous check and the round of the latest completed work, with no thief identity.
  Interrupted work does not add completed-work marks; reports remain last-known checks.
- `SALOON_EVIDENCE PASS` (twice) and `SALOON_EDITOR_ITERATION PASS` in
  `/tmp/saloon-evidence.log`: looking away, brief glances, walls, death, sustained sight,
  deduplication, recipient privacy, interrupted work, inspection deltas/action cost,
  stale reports, compressed wire roundtrip, history bound and new-match reset.
- Visually inspected the populated notebook alongside discussion planning in Unity:
  both clues readable, scrolling available, no overlap with room/companion/Ready buttons.
  README and the Sheriff role guide describe the mechanic. No new downloadable builds.

- A report-height edit made during the first broad run triggered hot reload and invalidated
  that run's avatar/network fixture state. Stopped and restarted Unity; the clean run in
  `/tmp/saloon-evidence-regressions.log` has no runtime exceptions and passed snapshot wire/
  stream, round clock, heist, beer, release, social, polish, practice, gameplay and
  `SALOON_EDITOR_ROUNDS PASS`. Restored the suite's generated historical PNG captures.
- Existing downloadable 0.2.10 binaries are unchanged; test this feature in Unity source.


## 2026-11-09 — compact corner interface

- Replaced the default Unity beveled controls with flat slate controls and restrained
  cool accents. Gameplay no longer has large panel backgrounds behind phase/Sheriff,
  a full-width footer, or a permanently expanded notebook. Main menus, role help,
  pause and results use the same typography and control styles.
- Room/partner/Ready controls form a compact right-edge list. Coin and turn prompts
  stay in that corner. A collapsed Clues button opens a small private scrollable list
  during discussion; it closes for room actions. Existing clue rules stay unchanged.
- Health uses separate filled/depleted segments plus an exact HP count at bottom left,
  with a distinct low-health color. Ammo is bottom right. Transient notices disappear
  after four seconds; connection warnings, elimination and active timers remain visible.
- Reduced world nameplate/HP-dot scale. Field, hover, focus, selected and disabled styles,
  sliders and scrollbars share the flat theme. Runtime textures/skin are cleaned up.
- Manually checked in Unity: solo launch, compact discussion controls, Ready advancing
  to room actions/coin, clean center view and HP/ammo corner placement.

- Final compile and private clue verification passed in a clean Editor session:
  `/tmp/saloon-minimal-ui-verified.log`, `SALOON_EVIDENCE PASS` and
  `SALOON_EDITOR_ITERATION PASS`, no runtime exceptions in that run.
- Checked the final Clues drawer opening/closing with real mouse input during discussion.
  Unity remains open in a local discussion preview. No release binaries were rebuilt.


## 2026-11-09 — Sheriff clues on Tab

- The Sheriff's role card always shows their own six latest private clues in its right
  column, newest first, with an empty-state inspection hint. The Sheriff no longer has
  a separate discussion clue drawer. Other roles retain their existing witness drawer.
- Tab releases the cursor for scrolling in discussion and room actions, including while
  holding right mouse. Gameplay clicks/actions/movement are suppressed while reading;
  planning controls are hidden behind the role overlay. Releasing Tab restores normal input.
- Updated inspection notice, role instructions, bottom key hint and README. No clue
  generation, role privacy or inspection action-cost rules changed.
- `SALOON_EVIDENCE PASS` and `SALOON_EDITOR_ITERATION PASS` in `/tmp/saloon-tab-clues.log`,
  including a new check that a displayed Tab card in room actions unlocks the pointer.
  Source compiles with no runtime exceptions in that run. No player builds created.


## 2026-11-11 — percentage health (Unity development source)

- Health now uses integer percentage points from 0 to 100. A damaging revolver
  headshot removes 20 points; a player at full health dies on the fifth hit.
  Beer restores 20 points, capped at 100. Friendly-fire damage and its shooter
  penalty each remove 20 points. Body shots retain their existing no-damage rule.
- Removed the serialized scene's old 3-HP setting and made maximum health fixed at
  100, so existing scene data cannot restore the old unit system. Protocol state-13
  prevents older builds from joining and interpreting percentage health as HP units.
- Bottom-left HUD shows a large exact percentage, five 20-point segments and the
  number of hits to elimination. Visible players show five markers, percentage and
  shots remaining above their hats. One-hit health is red; the HUD also marks
  two-hit health amber. Spectator HUD and companion choices use percentage health.
- `SALOON_HEALTH PASS` and `SALOON_DISCUSSION_HEALTH PASS`: actual legal host
  headshots produce 100 → 80 → 60 → 40 → 20 → 0, spend one bullet/action, and
  replicate the exact public and confirmed health through the compressed wire.
  Repeated triggers are rejected. Coverage includes partial health, double friendly
  elimination, full-health drink rejection, 20/80/95-point healing, no revival,
  spectator health, and labels at 100/80/60/40/20/1 percent with correct marker counts.
- `SALOON_EDITOR_ROUNDS PASS`: snapshot wire/stream and round clock, heist, beer,
  release, blackjack, social, polish, practice, table dodge, combat feedback,
  completion and full gameplay checks. Historical PNGs generated by this suite
  were restored; they are not current health screenshots.
- `SALOON_ROOM_ACTIONS PASS`: standing/seated headshots, body misses, dodging,
  normal and gifted drinks surviving non-lethal hits, and lethal hits cancelling
  drinks without revival. An existing fixture sent two stance commands in the
  same frame despite the 150 ms stance debounce; it now waits 200 ms between them.
- `SALOON_REMATCH PASS`: four real UDP connections in one Editor process,
  reconnect retaining 40% health and other resources, unanimous rematch and lobby
  return resetting health to 100%, and reconnect expiry. This is not a test on
  four physical devices or native Windows hardware.
- Initial overhead rendering check failed at 48 changed pixels. Enlarged health
  lettering and distance scaling; final check passed at 111 pixels, and final HUD
  and overhead captures were inspected. Initial failures remain in the saved log.
- Logs, marker summary and final screenshots: `Builds/HealthPercentage-2026-11-11/`.
  Re-run the focused check with **Saloon → Verify percentage health in Play mode**.
  No standalone player builds or downloads were produced for this source change.


## 2026-11-11 — compact health labels and beer recheck

- Replaced the wider overhead row with five tightly packed dots and an exact
  percentage. Reduced health text size and distance scaling; brought the name
  closer to the health row with a bottom-anchored name block so role lines stay
  above health. Low-health red and depth-tested visibility remain available.
- Removed shot-count wording from overhead labels, the local HUD and companion
  choices. This supersedes the shot-count display described in the previous entry.
- `SALOON_HEALTH PASS`, `SALOON_DISCUSSION_HEALTH PASS` and
  `SALOON_EDITOR_ITERATION PASS`: labels at 100/80/60/40/20/1 percent contain the
  correct markers and no shot-count text. Inspected fresh rendered overhead and
  HUD captures. The visibility pixel probe now renders at 1280×900 to verify
  compact text at the same resolution as its saved reference image.
- Expanded beer checks: no immediate healing before completion; normal drinking
  from 20/80/95 percent; gifted drinking from 20/80/95/100 percent; capped healing
  and repeated server ticks do not apply healing twice. Both paths restore 20
  percentage points, capped at 100. No healing logic change was necessary.
- `SALOON_ROOM_ACTIONS PASS` and `SALOON_EDITOR_ITERATION PASS`: normal and gifted
  drinks finish after non-lethal hits; lethal hits cancel drinking without revival.
- Final captures and log: `Builds/HealthPercentage-2026-11-11/CompactHUD.png`,
  `CompactWorldHealth.png` and `CompactHealth-Editor.log`. Unity source only.


## 2026-11-11 — centered player labels and Renegade HUD

- Explicitly centered TextMesh alignment for name, revealed role and health.
  Name and health now share distance scaling. Vertical placement uses actual font
  bounds to keep a small consistent gap instead of the previous fixed offset.
- Renegade no longer receives the witness Clues drawer. Deputy and Outlaw retain
  their drawer; Sheriff clues remain on Tab. Observation collection and role
  knowledge permissions are unchanged.
- `SALOON_HEALTH PASS`, `SALOON_DISCUSSION_HEALTH PASS` and
  `SALOON_EDITOR_ITERATION PASS`: verified all four roles' drawer visibility,
  centered multi-line revealed roles, shared scale, matching projected horizontal
  centers, visible health at all tested percentages, and no shot-count wording.
  Fresh Renegade HUD and world-label renders inspected. Beer checks still pass.
- Captures/log: `Builds/HealthPercentage-2026-11-11/RenegadeHUD.png`,
  `CenteredWorldHealth.png` and `CenteredLabels-Editor.log`. Unity source only.


## 2026-11-11 — remove the standalone Clues drawer for every role

- Deleted the Clues button, drawer rendering and its open/closed state for all
  roles. Removed obsolete drawer-visibility checks. Sheriff clues and scrolling
  remain inside the Sheriff-only Tab card; other role cards remain unchanged.
- Source checks confirm no EvidenceCard, ShowsWitnessClues or cluesOpen references
  remain, and the Sheriff-only Tab clues branch is intact. No gameplay rules changed.


## 2026-11-11 — private cigarette damage buff

- Actual entry into the smoking area grants one non-stacking buff; choosing the
  area or taking a drag is not the trigger. Unused buffs persist across regroup
  and rounds. Death, returning to lobby and starting a new match clear them.
- The next successfully fired revolver shot consumes the buff before hit testing,
  including misses and body hits. A valid headshot deals 40 percentage points;
  subsequent unbuffed hits deal 20. A buffed friendly target loses 40, with the
  existing 20-point shooter penalty. Rejected shots do not consume the buff.
- Private owner HUD reads CIGARETTE BUFF · NEXT SHOT 40%. Only the recipient's
  Snapshot carries their status; PlayerView has no buff field, and a spectator
  never inherits the followed player's buff. Protocol state-14 rejects old builds.
- `SALOON_CIGARETTE PASS` / `SALOON_EDITOR_ITERATION PASS`: real room assignment,
  entry without dragging, repeated drags/no stacking, persistence, compressed
  snapshot privacy, 40→20 damage, one bullet/action/hit event, misses, body hits,
  holstered/empty/cooldown/spent-action rejection, friendly damage, lethal clamp,
  death, spectator privacy, lobby/new-match reset, buffered draw and HUD cleanup.
  Rendered private HUD capture inspected.
- `SALOON_EDITOR_ROUNDS PASS`: full existing gameplay, round clock, snapshot wire,
  stream, heist, beer, release, blackjack, social, polish, practice, table dodge,
  combat feedback and completion regressions. Historical generated PNGs restored.
- `SALOON_REMATCH PASS` / `SALOON_EDITOR_ITERATION PASS`: four real UDP peers in one
  Editor process; reconnect delivers the owner's retained buff, rematch and lobby
  reset succeed. This does not certify four physical devices or Windows execution.
- Log and HUD capture: `Builds/CigaretteBuff-2026-11-11/`. Re-run the focused check
  via Saloon → Verify cigarette damage buff in Play mode. Unity source only;
  no standalone downloads rebuilt.


## 2026-11-11 — recorded reload, drinking and posse audio

- Replaced synthesized reload, drinking, posse hoofbeats and posse warning shots
  with CC0 recordings from Freesound: Dredile, Q.K., Max_Headroom and Resaural.
  License links, source URLs, author credits and checksums accompany the files.
  Original HQ preview MP3s are under ArtSource/Audio/Recorded; runtime mono PCM
  WAVs are under Assets/Resources/Audio/Recorded. These folders are included in
  source control; a generic Library folder would be excluded by the Unity ignore rule.
- Preparation is reproducible with Tools/prepare_library_audio.py and ffmpeg.
  Drink performance is fitted to 2.5 seconds with pitch-preserving tempo adjustment;
  horse rumble is filtered and the gallop loop has an 80 ms boundary crossfade.
  All four imported peaks remain below 0.75, with no synthesis in the replacements.
- Drinking no longer autoplays when avatars spawn. Drink/reload playback follows
  the action duration and received progress. Hidden avatars silence reload audio too.
  Posse ambience fades in at fixed original horse pitch, grows clearer/louder as
  stages advance, and does not restart on every stage. Results and disabling the
  component stop playback without destroying shared imported clips.
- `SALOON_LIBRARY_AUDIO PASS` / `SALOON_EDITOR_ITERATION PASS`, including after the
  final resource-path import: bundled clip identities, 44.1 kHz mono data, finite
  bounded samples, license file, no startup drinking, normal-pitch local drinking,
  health/action completion, reload synchronization and ammo completion, lethal
  interruption, recorded gallop fade-in, stage continuity, recorded distant shot,
  results cleanup and retained shared assets.
- `SALOON_EDITOR_ROUNDS PASS`: existing snapshot/stream/clock, heist, beer handoff,
  release, blackjack, social, polish, practice, table dodge, combat feedback,
  completion and gameplay checks all pass. Historical generated captures restored.
- Audio cannot be auditioned through this model's input; these checks establish
  recorded provenance, signal levels and in-game playback behavior, not a subjective
  listening judgment. User listening feedback can guide any further mix adjustment.
- Logs: Builds/RecordedAudio-2026-11-11/Editor.log. Re-run with Saloon → Verify
  recorded sound effects in Play mode. Unity source only; no downloads rebuilt.


## 2026-11-11 — user-selected revolver shot

- Revolver firing now uses Heathers Gunshot Effect2.wav by okieactor (Freesound
  #415912), CC0 1.0. Removed the former generated gunshot. The selected recording's
  layered blast and roughly 1.918-second reverb tail are retained, with mono
  conversion, level adjustment and a short tail fade. No additional sound layers
  or pitch processing were added to the file. First significant transient: 8 ms;
  measured peak: 0.656. Sources and both file hashes are in the audio manifests.
- `SALOON_LIBRARY_AUDIO PASS` / `SALOON_EDITOR_ITERATION PASS`: selected clip loads
  through SaloonGunAudio.Shot, has the expected full tail and valid bounded samples;
  existing recorded playback checks pass. Updated the legacy release assertion,
  which required the generated shot's >0.9 peak, to check audible recorded audio
  with normalized headroom. Its initial failure remains in the saved log.
- `SALOON_RELEASE PASS`, `SALOON_GAMEPLAY PASS` and `SALOON_EDITOR_ROUNDS PASS`:
  actual accepted firing plays the replacement; full existing gameplay regressions
  pass after the assertion update. Historical generated screenshots restored.
- Log: Builds/RecordedAudio-2026-11-11/SelectedRevolver-Editor.log. Unity source only.


## 2026-11-11 — Bar beer-pouring minigame

- E starts pouring on the player's Bar turn and spends one action plus one shared beer.
  E again or STOP closes the tap. Full-health players cannot pour; they can watch,
  talk, walk or use another action. Skipping beer permits a normal shot.
- A relaxed three-second fill targets 20 points. Partial pours round upward to
  5/10/15/20 points (1 → 5, 11/12 → 15, 17 → 20). Overflow wastes beer; the tap
  closes at 4.5 seconds. The host measures time and computes the result.
- After pouring, B drinks, aim + G gives the same healing to someone else, and
  Space leaves the beer. This continuation is included in the pour action.
  Unclaimed beer is left after six seconds. No second shot/pour is granted.
- Actual healing occurs once at drink completion, capped at 100. Non-lethal hits
  preserve pouring and self/gifted drinking. Elimination never revives a player.
  The round waits for the last pour, choice and sip; regroup clears all beer state.
- Added five public animated tap stations, a fill meter, displayed healing and
  drink/leave controls. Hold right mouse to look, walk or aim while using Bar UI.
  Bots use the host pour/stop/drink commands. Wire protocol is now state-15.
- Unity 6000.5.10f1 compiled runtime and Editor scripts. Final checks passed:
  SALOON_BAR_BEER (53 checks), SALOON_HEALTH, SALOON_DISCUSSION_HEALTH,
  SALOON_ROOM_ACTIONS, SALOON_ROUND_CLOCK, SALOON_SNAPSHOT_WIRE/STREAM, and the full
  SALOON_GAMEPLAY / SALOON_EDITOR_ROUNDS suite (including heist, beer, release,
  blackjack/social, polish, practice and round completion). Updated existing
  fixture mugs to specify their poured healing. The earlier audio assertion
  failure was resolved by the concurrent recorded-audio work before the final run.
- Inspected the rendered fill UI and five-station Bar capture. Screenshots and
  selected final-run markers: Builds/BeerPour-2026-11-11/. These are host-command
  and compressed-wire checks in one Editor process, not a multi-machine playtest.
  Unity source only; downloadable binaries were not rebuilt.


## 2026-11-11 — Pour directly on the countertop; ordinary mouse look

- Removed the right-side beer HUD panel and the Bar's pointer mode. Mouse movement
  now looks around normally; pouring does not turn or seize the camera.
- Pouring takes place entirely at the physical tap and mug: animated handle,
  stream, rising beer/foam, fill line, overflow and healing result on the counter.
  The mug stays on the counter until drinking or gifting starts.
- Aim at your tap or POUR control and click to start; click again to stop. Click
  the finished mug to drink, or use DRINK/LEAVE controls on the bar edge. E/B/G/Space
  remain available. A drawn gun keeps priority for shooting. Other players can
  watch live pours but cannot operate another player's station.
- Unity 6000.5.10f1 runtime/Editor compilation passed. SALOON_BAR_BEER PASS
  (67 checks) includes real ray hits from stool height, foreign-station/full-health
  rejection, weapon priority, ordinary cursor lock and camera input, live public
  geometry, readable lettering, clickable mug and leaving beer. Existing healing,
  gifting and round checks still pass. SALOON_ROOM_ACTIONS and its Editor runner
  also passed after the input change.
- Inspected first-person and public Bar renders. Captures and final markers:
  Builds/BeerPour-2026-11-11/Countertop/. Unity source only; no release build.


## 2026-11-11 — Discussion planner toggle and held tap valve

- Discussion starts in normal mouse look. P opens/closes the room planner and
  releases/restores the cursor. Right mouse is not needed. Ready closes the
  planner; Space still toggles Ready. Added a persistent P/Space instruction.
- Removed the physical POUR button. Hold the tap handle to pour; release the
  mouse to stop. Scroll while holding the handle to change the valve opening
  and actual host-measured flow (0.35–1.5x). Handle angle, stream width and
  fill animation follow the flow. E can also be held/released for pouring.
  Camera look stays available. Menu/role view or releasing the mouse stops
  the held tap. Drink/gift/leave and 5% healing increments remain available.
- Unity runtime/Editor compilation and SALOON_BAR_BEER / SALOON_EDITOR_ROUNDS
  passed. Added discussion-mode toggling and real flow/clamping checks; retained
  physical ray, healing, gifting, damage and action-budget coverage. Inspected
  the rendered countertop without the pour button. Captures and final markers:
  Builds/BeerPour-2026-11-11/HoldTapAndDiscussion/. Source only.


## 2026-11-11 — Independent head gestures, speech cutoff and player window visibility

- The host now stores body facing separately from camera/aim yaw and includes it in
  snapshots. Mouse up/down nods the neck/head; left/right shakes the head. Standing
  and seated torsos remain still within a 65-degree horizontal head range. Larger
  turns and walking bring the body around. Revolver aim still follows the camera;
  the mug's mouth target follows the posed head.
- Removed the silent-audio speech fallback and minimum mouth strength. The mouth
  closes in the frame that zero energy is supplied, without an animation tail.
  Vivox sends energy updates at 10 Hz; samples expire after 0.3 seconds without an
  SDK energy event. Mute, lost audio, channel changes, drinking and death suppress
  speech. Speaker state is captured during SDK events so participant disposal
  between Update and avatar LateUpdate cannot dereference a disposed participant.
- Replaced the runtime Standard/Fade material with a Resources-backed glass shader
  with explicit blending, back-face culling and depth-write state. No runtime
  transparency keyword variant is needed. Layout version 3 upgrades saved windows.
  Inside privacy faces and permanent shot/movement barriers remain in place.
- Removed top-right duel and room-turn cards, including Your Move / Q Draw /
  Action Spent text and unused coin-overlay texture. Physical table controls,
  discussion room planner and turn authorization remain available.
- Unity 6000.5.10f1 compiled runtime/Editor scripts and produced local 0.2.13
  universal macOS and Windows x64 builds. Both ZIP archives passed CRC checks.
  Outputs: Builds/ModelRoomFixes-2026-11-11/.
- SALOON_MODEL_ROOM / SALOON_EDITOR_ITERATION passed in the Editor. The same model,
  speech and window checks passed in the final macOS player with Metal rendering:
  standing/seated gestures, unchanged torso/hips, body-follow limits, wraparound,
  independent snapshot yaw, silent/stale/invalid speech, identity mapping, drinking
  and death. Bar-view pixel probes saw both interiors (Blackjack: 487 marker pixels;
  Coin Flip: 48); inside privacy probes saw zero outside marker pixels.
- SALOON_PRESENTATION and SALOON_GAMEPLAY passed in the final Mac build. Inspected
  native player captures showing occupied rooms through the glass and no top-right
  action card. Final two-process Vivox test passed with synthetic speech and muted
  microphones: receiver peak energy 0.4976, speech drove mouth activity, and both
  voice level and rendered mouth returned to zero after injection stopped while
  the sender remained connected. No runtime exceptions in the final test logs.
- Evidence, screenshots, archive hashes and test markers are under
  Builds/ModelRoomFixes-2026-11-11/Verification/, Presentation/ and Voice/.
  Windows runtime/graphics and physical microphone/speaker listening are untested
  for this change. No GitHub release, upload or published download was changed.


## 2026-11-11 — Bodies persist while eliminated players spectate

- Removed corpse hiding from the observer transition and spectator viewpoint changes.
  Eliminated players enter the existing spectator camera after the death delay;
  their fallen model remains visible until lobby/match reset. An avatar first seen
  after its player is already observing also creates a ragdoll.
- Settled ragdolls retain their pose with kinematic bodies and disabled colliders,
  keeping movement, interactions and seat reuse available. Regrouping does not
  reposition dead players. Starting a new match restores the living model and
  removes the old ragdoll components.
- Unity 6000.5.10f1 compiled the source and passed SALOON_RAGDOLL and
  SALOON_EDITOR_ITERATION in Play mode. Checks exercised real lethal shots,
  landing, persistent corpses after observer transition, duplicate snapshots,
  already-observing snapshots, local spectator entry and target switching,
  regrouping, seat reuse, side-room landing and new-match cleanup.
- Log: /tmp/saloon-persistent-bodies-editor.log. Retained-corpse render:
  /private/var/folders/bx/rq_p_44d63d1x6wwfzqyw85m0000gn/T/SaloonWindows-death-03-body-retained.png.
  Source only; no player build or publication.


## 2026-11-11 — Control crouching and first-person weapon clipping

- Hold either Control key while standing to crouch. Release to stand when the
  full-height capsule fits overhead. The host owns posture, moves the capsule
  center to keep feet grounded, reduces walking speed to 45%, and replicates
  crouch through snapshots. Eye height, shot/interaction origins, head/body
  hitboxes, character joints and held world props follow the lowered posture.
  Seated discussion and room regroup reset crouch; stale input releases it.
- The first-person revolver and its muzzle flash render on a dedicated layer
  through a depth-clearing camera after the world camera. Third-person weapons
  retain world occlusion. Shot rays still collide with walls and window barriers.
- Unity 6000.5.10f1 compiled runtime and Editor scripts and passed SALOON_CROUCH
  and SALOON_EDITOR_ITERATION using Metal in an isolated temporary project copy.
  Checks covered capsule/feet, camera, rig/head alignment, snapshot encoding,
  crouch speed, standing-head ray misses and crouched head hits, stale input and
  pose rejection, low ceilings, seated players and regroup reset. Render probes
  confirmed 5,881 gun pixels against an opaque surface closer than the muzzle
  and 5,721 pixels against the room-window glass shader. Inspected both captures.
- Run again with Saloon → Verify crouching and weapon clipping in Play mode.
  Evidence: Builds/CrouchAndWeapon-2026-11-11/. Source changes only; no player
  build or publication. Windows runtime rendering was not exercised.


## 2026-11-11 — Sunset smoking overlook and silent outdoor walking

- Replaced the placeholder yard with a cedar overlook, open rails, a bench and
  a low-poly desert of layered sandstone mesas, saguaro cacti and scattered rocks.
  A Resources-backed sky shader draws a low golden sun, apricot horizon, violet
  evening sky and thin pink clouds. Outdoor environment lighting has its own
  layer mask. The saloon's lighting and indoor visibility stay separate.
- Both smokers arrive side by side facing the sunset. Kept the existing room
  footprint, movement bounds, smoking props and privacy/voice routing. Decorative
  distant scenery has no gameplay colliders. Area setup remains idempotent.
- Outdoor footsteps now stop immediately and do not accumulate distance or play
  for local or remote players. Recorded wooden footsteps resume indoors. Removed
  allocation of the unused outdoor dirt samples.
- Unity 6000.5.10f1 compiled scripts and the sunset shader and passed SALOON_SUNSET
  and SALOON_EDITOR_ITERATION with Metal in the isolated verification project.
  Checks covered arrival/facing, layer masks, lighting isolation, scenery
  colliders, idempotent setup, indoor steps, silent outside movement for both
  perspectives, resumed indoor steps, smoking props, actual deck walking and
  returning indoors. Render probes found 113,971 warm sunset pixels and 1,951
  sun pixels; inspected the final first-person capture.
- Evidence: Builds/SunsetOverlook-2026-11-11/Verification.log and Overlook.png.
  Run with Saloon → Verify sunset overlook and silent outdoor walking. Source
  only; no new player build or publication. Windows runtime was not exercised.


## 2026-11-11 — Clean minigame controls and slower blackjack

- Removed blackjack table totals, names, status text and physical action plaques.
  Cards and deck remain on the felt. Hit/Stand and Draw Card use a small screen
  panel below the hands; no calculated totals are displayed in that panel.
- Removed coin-table seat labels, status lettering and physical Heads/Tails
  buttons. Heads/Tails choices appear in the middle of the screen, then disappear
  during the toss. Only the physical coin and its face engravings remain on the
  felt. Both minigames suppress the duplicate room timer and action-block text.
- Blackjack deals over 3.2 seconds, with 0.65-second card spacing and slower
  slides/flips. A card/action has a 1.5-second settling/input interval. Player
  decisions allow 45 seconds, renewed after each hit; idle dealer draws wait eight
  seconds. Bots also wait between decisions. Pushes remain visible for five seconds.
- Both games show a four-second result/reaction phase. The host rejects weapon
  and action commands during this phase, while allowing players to stand and dodge.
  The winner then has a full ten-second opening; the shared round deadline preserves
  the response window. Existing draw/queued-fire behavior resumes after the pause.
- Compiled and exercised in the user's open Unity 6000.5.10f1 Editor, with no
  standalone builds. Final SALOON_BLACKJACK, SALOON_TABLETOP, SALOON_COIN_TABLE and
  SALOON_EDITOR_ITERATION checks passed. Covered card/hidden-card state, action
  ownership, paced input, renewed decision time, host-side reaction lock, legal
  opening shot and response, push/redeal, timeout/pass, standing during result,
  clean table geometry, spectator read-only state and phase cleanup.
- Inspected Game-view captures for both minigames: blackjack controls below both
  hands and centered coin choices. Captures are SaloonBlackjack-screen.png and
  SaloonCoin-screen.png in Unity's system temporary directory; run logs are in
  Logs/Editor.log. Source only; no build or publication.


## 2026-11-11 — Incoming invitations and one-click discussion plans

- Restored incoming invitations as a recipient-only Snapshot array carrying the
  sender ID/name, requested room and sender plan version. Other players' public
  room/partner choices remain redacted. Withdrawn/changed requests update with
  snapshots; eliminated players and action-phase snapshots have no active requests.
- A discussion notice shows incoming invitations while the planner is closed.
  The planner now places rooms across the top and people down the side: each
  square chooses a room and a partner in one click, with an On your own row.
  Highlighted Join invite buttons accept both fields atomically. A plan summary
  and separate Ready button preserve explicit first-Ready seat reservation.
- Acceptance is checked on the host against the sender's latest room, recipient
  and plan version. A stale click cannot silently apply an invitation that changed.
  Its acknowledgement restores the authoritative plan through existing draft
  versioning. Invitations do not reserve another player's seat or ready them.
- Capitalized Have a Smoke in destination names, outdoor sign and Editor menu;
  retained internal scene-object names used for layout and verification.
- Unity 6000.5.10f1 compilation and final SALOON_PLANNING / SALOON_RESERVATIONS /
  SALOON_EDITOR_ITERATION checks passed in the open Editor. Covered compressed
  invitation wire data, recipient-only disclosure, raw plan redaction, one-click
  acceptance, no automatic Ready, stale acceptance rejection/recovery, withdrawal,
  multiple requests, elimination, action-phase cleanup and destination naming.
- Inspected the rendered planner with two incoming invitations, selected plan,
  Have a Smoke column and Ready control. Capture: SaloonPlanner-invitations.png
  in Unity's system temporary directory. Log: Logs/Editor.log. Source only;
  no standalone build or publication.


## 2026-11-11 — Replace planning grid; fix practice invitations and blocked rooms

- Replaced the room/person matrix with a compact dark in-game destination menu.
  Invitation cards explicitly name the sender and destination, with a Join action.
  Four destination cards lead to a short company list and Go on your own control.
  Right-click and Close dismiss the menu; Escape behavior was not changed.
- Join and practice Go with / Go on your own submit plan and readiness atomically.
  Existing selection-version acknowledgements reconcile rejections and reopen the
  menu with an explanation if a real human-filled room rejects a choice.
- Fixed practice planning as a two-pass host allocation. Human-requested bot pairs
  get space before autonomous bot plans; only after every bot destination is
  applied are their Ready states checked. Bots yield seats for human choices;
  they do not displace already-ready humans. Client full-room checks exclude
  reclaimable bot reservations.
- Bots now proactively send one stable, destination-specific invitation per
  uncommitted human, leaving an open seat for that recipient. Accepted human
  invitations override autonomous bot choices. Money-carrying bots keep the Bar.
- Final Unity 6000.5.10f1 Play mode checks passed: SALOON_PRACTICE_PLANNER,
  SALOON_PLANNING and SALOON_EDITOR_ITERATION. Exercised actual bot ticks and
  frontend command paths: unsolicited invitation, reserved recipient space,
  stable offer, single-command Join/Ready, real room entry with the inviter,
  two bots already occupying Coin Flip followed by a human requesting another
  practice partner, yielding, readiness and actual Coin Flip assignment.
  Also verified human-filled-room rejection/reopened UI and human-seat priority.
- Retained existing plan-draft, reservation, privacy, stale invitation and
  withdrawal checks. Inspected the rendered replacement menu with an actual
  bot-generated Coin Flip invitation. Capture: SaloonPlanner-invitations.png
  in Unity's system temporary directory. Source only; no new standalone build.


## Published 0.2.14 — 2026-11-11

Built matching universal macOS and Windows x64 players from d7e983f. Mac gameplay regressions, four independent players with no bots on the LAN interface, and Metal head/mouth/window rendering checks passed. The opt-in LAN runner now actively plays the slower blackjack hand and checks its finish condition before sending a new round's plan.

The published Windows ZIP passed native Windows Server 2022 validation: functional gameplay, five-player smoke, separate rooms, paired room and paired-room Vivox connection (run 43062776306); four players, five players, four with packet loss/delay and four with a 13-second interruption (run 43062772236). Every case passed. This does not verify the actual household router or each friend's Windows firewall.

The GitHub Mac ZIP was downloaded, its SHA256 checked against the published checksum, extracted to Downloads/GAMES/Last Call.app, deep signature verified and launched showing 0.2.14 with NO BOTS selected. Old local 0.2.12 and ModelRoomFixes 0.2.13 output folders were removed after validation (1,980,790,080 bytes). Screenshot-only iteration evidence and project source were retained.


## 2026-11-12 — Graphics settings, Editor-only verification

- Recovered Unity 6000.5.10f1 startup by stopping the stale licensing helper left
  by the sandboxed launch and opening LASTCALL through Hub. Logs/Editor.log records
  a successful licensing handshake and license update. No license was removed,
  returned or replaced.
- Unity compiled and entered Play mode with Metal rendering. Manually exercised
  Graphics Settings from the main menu and from the Escape menu in a five-seat
  solo practice match.
- Verified the initial Medium / VSync off / 60 FPS selections. Applied Low and a
  90 FPS limit, closed/reopened the menu, stopped/restarted Play mode and confirmed
  those selections persisted. The rendered scene changed when quality applied.
- Staged High and VSync on; verified the frame-limit controls disable and display
  monitor refresh rate. Cancel discarded those edits and reopening showed Low/90.
  Then applied High/VSync on and confirmed those selections from the practice
  match's graphics menu. Restored Medium/VSync off/60 FPS using Defaults + Apply.
- Escape closed graphics back to the match menu. Stopped Play mode and left Unity
  open in Edit mode. No player build or publication was performed.
- Limits: no hardware performance claim, actual FPS-cap measurement, OS resolution
  or window-mode switching, or timed display rollback was validated. The Editor
  deliberately skips OS display changes and their confirmation branch; these
  require a future standalone-player test.


## Published 0.2.15 — 2026-11-12

Built Windows x64 and universal macOS players from the graphics-settings release
source (`131fe1a`). Packaged Mac gameplay regressions and four independent no-bot
LAN players passed. Logs: /tmp/lastcall-release-0215.log,
/tmp/lastcall-0215-functional.log and /tmp/lastcall-0215-lan/.
The GitHub Mac archive was downloaded, SHA256 verified, extracted, deep signature
verified and installed at Downloads/GAMES/Last Call.app. Launch visually confirmed
0.2.15 and the Graphics Settings entry point. Removed the prior 0.2.14 build folder
and replaced app after successful launch, freeing 1,156,862,317 bytes; source and
screenshot/test evidence folders were retained.

Published Windows ZIP passed both native Windows Server 2022 workflows: functional,
five-player, separate rooms, paired room and paired voice connection (43239310730);
four-player, five-player, impaired four-player and a 13-second interruption
(43239318018). Initial draft-release attempts failed to download because the runner
could not see the draft; final published-release reruns passed.


## 2026-11-12 — Open microphone and private proximity voice, 0.2.16

Added T to toggle session-only open mic, preserving hold-V push-to-talk and M mute.
The Escape voice menu has the same switch. A small HUD indicator shows the mode,
mute, connection and permission state. Menus pause transmission. Channel changes
retain the selection; leaving the session resets to push-to-talk.

Preserved the private smoking channel and living/dead separation. New packaged
voice-controls regression passed on Mac: input/mute/menu combinations, privacy in
every phase, live Vivox indoor/outdoor/indoor/dead transitions with exactly one
active channel, immediate two-way transition muting, outdoor snapshot isolation
and open-mic reset. Physical capture stays disabled during this regression.
Log: /tmp/lastcall-0216-voice-controls.log.

Unity compiled universal Mac, Windows x64 and Linux x64 players. Packaged Mac
gameplay passed (/tmp/lastcall-0216-functional.log). Four independent no-bot
players completed three rounds and entered round four, covering roles, movement,
paired coin/blackjack, headshots and healing (/tmp/lastcall-0216-lan/). Two live
Vivox processes transmitted synthetic speech with muted physical microphones;
receiver peak energy was 0.5469, with zero speech/mouth activity after stopping
(/tmp/lastcall-0216-audio-valid/). The initial audio fixture was empty because
macOS speech synthesis was sandboxed; regenerating it with system access fixed
the fixture. No game code changed for that rerun.

Mac universal slices and deep signature verification passed. ZIP CRC checks
passed; SHA256 checksums accompany all three archives. Added native Linux release
CI and voice-controls to the Windows standard suite. Physical device listening
and household network testing remain human checks.

Published 0.2.16 Windows validation passed on Windows Server 2022: standard voice,
gameplay, five-player and room suites (43244373151), and four/five-player LAN with
loss/delay and interruption recovery (43244381329).

Downloaded the final GitHub Mac archive, checked SHA256 and deep signatures, and
compared every installed bundle file to the download. Native launch showed
0.2.16. Installed path: Downloads/GAMES/Last Call.app. Removed all discovered old
0.2.15 app/build/archive copies, including temporary downloads, freeing
1,648,355,499 bytes. Old screenshot and test evidence folders contain no game
builds and were retained. A crash dump from the initial malformed synthetic
audio fixture was removed from the first Mac package; the corrected archive and
checksum replaced it on GitHub before final download verification.

Linux initially crashed in Unity's headless NullGfxDevice path before Start.
The same binary passed voice-controls and gameplay under Mesa OpenGL/Xvfb
(43245042341). That run then failed to locate the five-player test logs because
the smoke runner passed relative paths. Resolving the log folder before launch
fixes that harness issue; no game binaries were changed for these CI repairs.

Final native Linux validation passed on Ubuntu 22.04 using Mesa OpenGL/Xvfb:
voice-controls, gameplay, five independent clients and four-player LAN
(run 43245476294). Release 0.2.16 is promoted as the latest GitHub download for
Windows x64, universal macOS and Linux x64. Release notes contain all test links.


## 2026-11-13 — Private character story maps

- Replaced the text-only Tab role card with separate Sheriff, Deputy, Outlaw and
  Renegade maps. AI-generated circular Western illustrations sit on curved trails
  with branching victory paths and optional support objectives. Completed nodes
  desaturate to grey with a checkmark; next nodes receive a gold ring and NEXT
  badge, supported by a plain-language action panel. Private clues and known
  roles remain visible beside the map.
- Host participant flags record Bar arrival, completed safe work and rival
  headshots. Reconnects retain that participant state; ResetHeist clears it for
  a new match. Money and escape steps reflect current authoritative ownership
  and pending escape state, so cancellation restores the required next step.
  Renegade elimination milestones remember only deaths visible under the
  existing activity privacy rules, including smoking-area separation.
- Unity 6000.5.10f1 compiled and passed SALOON_STORY_MAP (25 checks) and
  SALOON_EDITOR_ITERATION in the open Editor. Covered all four initial maps,
  actual inspection and safe completion, money collection, escape declaration
  and cancellation, compressed network serialization, participant progress
  retention, recipient isolation, unseen deaths, observed-death persistence,
  alternative Law victories and match reset. Reviewed all four rendered layouts
  after fixing scaled trail/checkmark rendering. Source verification also passed
  12,747 existing planning/gameplay checks via Tools/verify_planning.py.
- Retained previews: Documentation/StoryMapPreviews/. Generated art and exact
  prompt provenance: ArtSource/UI/StoryMilestones.txt. Runtime atlas:
  Assets/Resources/UI/StoryMilestones.png. Run again with Saloon → Verify
  character story maps in Play mode. Source only; no standalone build or
  publication. Windows/Linux rendering and real reconnect transport were not
  newly exercised for this UI change.

Story-map instruction follow-up: added visible action, location, key and timing
instructions beneath every milestone for all four roles. Adjusted the map rows
and NEXT panel to keep the instructions separate. Reviewed rendered previews;
SALOON_STORY_MAP (25 checks) and SALOON_EDITOR_ITERATION passed again.


## 2026-11-13 — Physical safe access

- Outlaw H safe work/money collection and Sheriff C inspections now require
  the host participant to be within 1.35 metres of the physical safe, at the
  same floor level, with no wall/counter blocking access. Client command
  coordinates cannot authorize access. Rejected distant attempts preserve the
  action. Safe work rechecks access continuously and at completion; leaving
  cancels it without safe progress or an action refund.
- Nearby role-specific H/C hints identify the safe. Updated role guides, Tab
  milestone instructions and README explain F to stand and WASD to approach.
  Practice Outlaws find a collision-aware route through the Bar furniture,
  then use the same proximity checks. Failed route searches are throttled.
- Unity 6000.5.10f1 compiled and passed SALOON_SAFE_ACCESS (21 checks), the
  existing SALOON_HEIST suite, SALOON_STORY_MAP (25 checks), SALOON_EVIDENCE and
  SALOON_EDITOR_ITERATION. Exercised real H/C command rejection/acceptance,
  spoofed command coordinates, height/room/obstruction rejection, walk-away
  cancellation, open-safe collection, nearby prompts, actual bot movement,
  interruption by shots, escape timing, private reports and witness clues.
  Updated existing fixtures to use the physical safe; the escape-cancellation
  fixture selects a valid paired side room under the current no-solo rules.
- Source checks also passed 12,747 planning/gameplay checks. Reviewed updated
  Outlaw and Sheriff map instructions. Final markers are retained in
  Documentation/StoryMapPreviews/Verification.txt. No standalone build or
  publication; Windows/Linux execution and physical-device multiplayer were
  not newly exercised for this change.


## 2026-11-14 — Safe proximity highlight and role key prompts

- Added a local pulsing gold outline and rim tint to the safe when the living
  local Outlaw or Sheriff is within the same reach/obstruction checks used by
  physical safe actions. A callout anchored to the safe shows a large H with
  Work safe / take money, or C with Inspect safe. The callout reports current
  work, busy or spent-action state. When the safe is outside the camera view,
  the key remains visible with a turn-to-face-the-safe instruction.
- The highlight hides outside reach, behind access obstructions, during menus
  and Tab, outside room actions, after death, and for roles without a safe
  action. An Outlaw carrying the money is not offered another collection.
  Highlight meshes have no colliders, cast no shadows and use a dedicated
  depth-tested shader. Original safe material identities/colors are preserved;
  visibility is local and no role state is replicated for other players.
- Unity 6000.5.10f1 / Metal compiled and passed SALOON_SAFE_HIGHLIGHT (33 checks)
  and SALOON_EDITOR_ITERATION. Reviewed both first-person H/C captures. Checks
  covered proximity, obstruction, each role, death, discussion/finished phases,
  pause/graphics/Tab, bag ownership, work/spent states, material preservation and
  component cleanup. Existing obsolete-API compiler warnings remain unrelated.
- Previews and final markers: Documentation/SafeHighlightPreviews/. Rerun with
  Saloon → Verify safe highlight and key prompts in Play mode. Source only;
  no standalone build or publication; Windows/Linux rendering was not newly
  exercised for this presentation change.


## 2026-11-14 — Twenty-round posse schedule

- Extended posse arrival from the end of round 10 to the end of round 20.
  Rounds 1–17 have no active posse pressure. The first warning appears while
  regrouping into round 18; rounds 18, 19 and 20 retain the three complete
  warning/response rounds. The posse arrives after round 20's actions.
- Outlaws can declare escape through round 19 and complete it in round 20
  before posse resolution. A new declaration in round 20 is rejected without
  consuming the action. Earlier elimination/escape victories remain available.
- Role guides, objectives, story-map instructions and round counter, pause
  menu and README now use the new schedule. Player-facing source strings read
  the shared posse timing constants so the displayed deadlines stay aligned.
- Unity 6000.5.10f1 passed SALOON_ROUND_CLOCK, SALOON_POSSE and
  SALOON_EDITOR_ROUNDS. The host scenario exercises four complete 20-round
  schedules: Law arrival victory, a last-chance escape, capture without Law,
  and preserving an elimination result. Per-round serialized snapshots verify
  warning stages and countdowns. Updated an older assertion to respect the
  Sheriff's existing permission to know the Deputy during aftermath.
- Tools/verify_planning.py passed 12,768 checks, including new explicit
  no-early-pressure, rounds-18–20 countdown and actual round-19/20 declaration
  checks. Final markers: Documentation/PosseTimingVerification.txt. Source
  only; no standalone build or publication.


## 2026-11-14 — Private one-use bartender question

- Added a physical, idle bartender behind the left end of the Bar counter, with
  an I interaction for the Sheriff. The private target picker asks whether one
  living player is an Outlaw. The answer contains exactly Yes or No; it does not
  change exact-role labels or known-role lists. The question spends the current
  round action and can be asked only once per match. Closing the picker is free.
- Host authorization checks Sheriff role, action phase, life, Bar membership,
  physical approach, floor height, obstructions, valid living target and normal
  action availability. Client coordinates cannot authorize access. Rejections do
  not consume the question. Participant state retains the question and answer
  over reconnects and action windows; ResetHeist clears both for a new match.
- Question/answer fields are recipient-only. No public event is emitted. The
  answer remains readable in the Sheriff’s Tab private-intel panel independently
  of the bounded safe-evidence list. The menu releases the pointer and suppresses
  combat/movement input; leaving reach, death or a phase change closes it.
- Unity 6000.5.10f1 / Metal compiled and passed SALOON_BARTENDER (35 checks) and
  SALOON_EDITOR_ITERATION. Covered authorization failures, spoofed coordinates,
  blocked access, spent actions, duplicate/changed targets, both non-Outlaw roles,
  compressed snapshot serialization, every other recipient, unchanged role
  disclosure, reconnect retention, later action windows, cancellation and reset.
  Tools/verify_planning.py passed 20,996 existing source checks.
- Reviewed rendered bartender, target picker, Yes answer and Tab private intel.
  Previews and markers: Documentation/BartenderPreviews/. Rerun with Saloon →
  Verify private bartender question in Play mode. Source only; no standalone
  build or publication. Real multiplayer transport and Windows/Linux execution
  were not newly exercised for this feature.


## 2026-11-14 — Solo Bar priority, public walking and closed outside entrance

- Bar is now a valid solo destination. Only unmatched side-room occupants
  return to Bar; intact pairs and explicit Bar choices are preserved. Fallback
  never assigns anyone to an unselected room, including private smoking.
  Practice bots no longer fill a human's locked solo room choice. Explicit
  invitations/partner choices remain available. Planner copy and README match.
- Expanded Bar movement to include the central aisle, a complete route around
  the discussion table and the inside entrance. The nearest public rectangle
  keeps movement out of both enclosed game-room interiors without teleporting
  players across the hall. Center table and discussion-chair colliders now
  match the visible replacement art footprint.
- Added closed physical entrance leaves using the existing Door art. Their
  colliders seal the opening. There is no E entrance/exit action, no door
  command, and no new room transition; the smoking deck remains a planning
  destination. This follows the user's explicit correction against E travel.
- Tools/verify_planning.py passed 20,996 checks, including exhaustive room
  combinations/capacities, solo Bar preservation, no unchosen room assignments,
  intact pairs, unmatched fallback and no automatic bot recruitment for smoking.
- Unity 6000.5.10f1 passed SALOON_PUBLIC_FLOOR (19 checks) and
  SALOON_EDITOR_ITERATION. Exercised real host assignments, private smoking
  snapshot separation, preserved solo carrier/action state, actual input and
  collision movement around all four sides of the table and down the entrance
  aisle, closed-door ray collision and no room/action change from E there.
  Reviewed center-table and closed-entrance captures. Final evidence is in
  Documentation/PublicFloorPreviews/. Source only; no standalone build or
  publication. Windows/Linux execution was not newly exercised.

Bartender eligibility follow-up: the Sheriff, Deputy and each Outlaw now have
separate allowances of exactly one private question per entire match. The
Renegade is excluded because they already know all roles. Shared eligibility
rules govern host commands, client menus, prompts and private snapshot fields.
Deputy and Outlaw Tab panels now also retain their own answer. Action cost and
Yes/No-only answers are unchanged.

Unity / Metal passed SALOON_BARTENDER (59 checks) and SALOON_EDITOR_ITERATION.
New checks exercise all four eligible participants asking in the same match,
independent allowances, each role’s serialized private answer, duplicate
requests in a later action window, reconnect retention for every eligible
participant, and Renegade/Unknown rejection. Reviewed Deputy and Outlaw private
intel captures in Documentation/BartenderPreviews/. Existing source checks
passed 20,996 assertions. Source only; no standalone build or publication.


## 2026-11-14 — Distinct bartender character and gunfire mood

- Replaced the player Gunman clone with an original procedural saloon keeper:
  older, bald with silver side hair and a handlebar moustache, stout burgundy
  waistcoat, ivory rolled sleeves, bow tie and apron. Hands polish a tankard;
  the animation slows as mood falls. The character has no player rig, weapons,
  gameplay colliders or imported character dependencies. Materials clean up
  with the NPC.
- Host-owned mood starts at 100. Every accepted Bar shot, including misses and
  bot shots, adds one shared shot and removes 10 mood, floored at zero. Empty
  triggers, holstered/rejected shots and side-room shots do not affect it.
  Round changes and reconnects do not regenerate mood; ResetHeist restores it
  for a new match. Gunfire is still permitted.
- Mood is the percentage probability of a useful, truthful Yes/No answer. At
  100 it is guaranteed; at 50 it is 50%; at zero the reply is always Maybe.
  Maybe conveys no role information and consumes both the round action and
  the participant's one question. The host draws once and stores the result,
  preserving privacy, serialization and no-reroll behavior. Renegade remains
  excluded. The role guides, prompts and Tab instructions describe Maybe.
- Added a shared mood meter to the Bar HUD and question panel, with numeric
  mood, status, helpful-answer probability and Bar shot count. A depth-tested
  sign above the character reflects mood too. The separated smoking area's
  snapshots receive -1 for mood and shot count to avoid indoor activity leaks.
- Unity 6000.5.10f1 / Metal passed SALOON_BARTENDER (1,393 checks) and
  SALOON_EDITOR_ITERATION. Covered every discrete mood/probability sample,
  truthful vs useless answers for both role outcomes, saturation, actual
  missed Bar shots, rejected repeat/empty/holstered shots, side rooms, bot
  shots, per-recipient compressed snapshots, outdoor isolation, round and
  reconnect retention, ten-shot exhaustion, Maybe spending, no reroll and
  new-match reset. The probability fixture samples interval centers to avoid
  floating-point boundary ambiguity. Existing source checks passed 20,996
  assertions. Reviewed calm character/meter, question panel and fed-up Maybe
  captures in Documentation/BartenderPreviews/.
- Source only; no standalone build or publication. Windows/Linux rendering and
  real multi-process transport were not newly exercised for this feature.

## Solo smoking and lantern correction — 2026-11-14

- Corrected room assignment: only Coin Flip (heads or tails) and Blackjack reject
  lone occupants; Have a Smoke and Bar accept solo visits. Capacity and lock-order
  checks still apply. Updated the planner's description.
- `python3 Tools/verify_planning.py`: PASS, 21,002 checks, including locked solo
  smoking after early start and discussion expiry, without recruiting a bot.
- Updated the assignment invariants in the Play-mode planning/practice checks and
  added real server transition, deck placement, entry-buff and privacy checks to
  `VerifyPublicFloor` for both early start and timer expiry.
- Environment preview validation now also checks eight luminous lantern chambers,
  eight point lights at their fixtures, indoor light masks, no decorative collision,
  and repeat application without duplicate lights.
- Play-mode `VerifySoloSmokingAccess`: PASS, 12 checks using actual plan commands,
  server transitions, outdoor placement/privacy, cigarette entry buff and smoking;
  both early-lock and timer-expiry paths pass. Both solo minigames still fall back.
- Saved the migrated lantern fixtures, generated interior meshes/materials and
  ambient lighting into `Assets/Scenes/Saloon.unity` with `SaloonLightingSetup.Apply`.
  Visual captures are in `Documentation/SurfacePreviews/`.


## 2026-11-14 — Bartender arrival after five complete rounds

- Per the confirmed timing, the bartender is absent for rounds 1–5 and arrives
  when round 6 discussion begins. One shared arrival-round constant controls
  host scheduling and player instructions. Before arrival, the server rejects
  all bartender requests without spending a question or round action. The
  client hides the question interaction and mood meter; the NPC and its sign
  are created lazily after the arrival snapshot, then hidden on a new match.
- The host emits exactly one public “Bartender arrived” event and supplies a
  six-second notification window in each recipient's snapshot. The banner is
  drawn after gameplay overlays so the automatically opened room planner cannot
  cover it. Reconnects preserve arrival and do not restart the timer. A new lobby
  or match clears arrival even if a previous round counter remains in memory.
- Shots before arrival do not lower the absent bartender's mood. He arrives at
  100; the existing mood, Maybe and one-question-per-player rules apply afterward.
  The Renegade remains excluded. Tab explains the waiting period before arrival.
- Unity 6000.5.10f1 / Metal passed SALOON_BARTENDER (1,461 checks) and
  SALOON_EDITOR_ITERATION. The added cases exercise all five early rounds, all
  five participants, command rejection without spending, client menu denial,
  lazy NPC absence, redacted early mood, a real pre-arrival shot, the round-five
  regroup boundary, round-six discussion arrival, per-recipient compressed
  notification state, event/timer deduplication, reconnect and lobby/match reset.
  Existing source checks passed 21,002 assertions.
- Reviewed the empty counter during round five. The final notification layering
  fix compiled, but its rendered overlay recheck was not completed while the
  user's active Play session continued. The rerunnable fixture captures both
  BeforeArrival.png and Arrival.png in the LastCall-Bartender temporary folder.
  Source only; no standalone build or publication.


## 2026-11-14 — Free licensed bartender model

Replaced the procedural placeholder with CDmir / TinyWorlds' Bartender from
https://opengameart.org/content/bartender, listed by the author under CC0 1.0.
Original blend, source page, license provenance and repeatable Blender exporter
are retained in ArtSource/Bartender/. Runtime FBX retains the rig and uses the
packed Body, Head and Hair diffuse maps. The body used a separate legacy UV
layer; its face loops were copied into the primary UV channel for correct Unity
texture mapping. Character height is normalized to 1.88 metres.

Unity / Metal passed SALOON_BARTENDER (1,461 checks) and
SALOON_EDITOR_ITERATION with the imported mesh. Reviewed the final textured
character at the counter; preview is Documentation/BartenderPreviews/Bartender.png.
Arrival timing, privacy, mood and one-question rules are retained. The character
uses subtle root breathing; no polishing or retargeted Humanoid animation is
claimed for this imported rig. Source only; no new standalone build.


## 2026-11-14 — 0.2.17 shared Outlaw progress and cinematic opening

Both Outlaws now receive the same safe-work milestone and live completed-job count.
Their private snapshots also carry team money and escape state, so collection and
declaration advance both character maps without granting the partner a second bag
or escape action. Cancellation and loss reopen the relevant steps. Team state is
excluded from Law and Renegade snapshots. Added a 5.5-second private role introduction
with a full-viewport black screen, animated titles, and fade into the saloon.

Production checks passed 21,008 assertions (`python3 Tools/verify_planning.py`).
Unity story-map checks passed 32 assertions, including private team state and wire
serialization (`/tmp/lastcall-0217-storymaps.log`). Updated stale release/multiplayer
fixtures to use the current ally-role visibility rules, and corrected an old combat
fixture that assumed a Bar shot spent two players' actions. No game rules were
changed for those fixture corrections.

Unity built universal Mac, Windows x64, and Linux x64 players. Final Mac packaged
functional suite passed (`/tmp/lastcall-0217-functional-verified.log`). Five separate
Mac players completed three no-bot LAN rounds and entered round four
(`/tmp/lastcall-0217-lan-final/`). Build log:
`/tmp/lastcall-release-0217-verified.log`.

Published v0.2.17 with Mac, Windows, Linux, and SHA256 assets. Downloaded the final
GitHub Mac ZIP and verified SHA256
`706446e1eac0ec8cbacfca0e56dd8620d10e40b6ff6765e068ebac1a9a88c0ea`.
Its bundle matched the installed `/Applications/Last Call.app` file for file;
deep signature verification and native startup reporting 0.2.17 passed. Deleted
the old 0.2.16 build directory, replaced app backup, and temporary old download,
removing 1,592,031,983 bytes. Retained historical screenshot/test evidence and
removed the redundant temporary 0.2.17 archive/extraction after verification.

Native Linux published-package checks passed (run 43587759354). Windows standard
run 43587756507 passed voice controls, functional gameplay, and five-player
connections. Its rendered-room fixture is stale: SaloonGame.ApplySnapshot opens
RoomPlannerOpen on Discussion, while SaloonAutomation's RoomTest immediately
calls ToggleRoomPlanner and asserts the planner is open. All three legacy room
cases therefore failed their planner assertion. The first-round cinematic also
correctly hides the planner until the introduction finishes. This is a test-harness
limitation; no production gameplay was changed to satisfy the obsolete fixture.

Windows published-package four/five-player LAN, impaired connections, and
interruption recovery all passed in run 43588065431. Source and packages were
unchanged for this validation. Release notes link to the native Linux success,
Windows standard partial result, and Windows LAN success without claiming the
obsolete rendered-room fixture passed.

## 2026-11-23 — Configurable solo practice (development source)

Solo practice now opens a setup page before starting. It offers 3–7 bots
(4–8 total players), 5–120 seconds of discussion, 5–60 seconds of room actions,
and 0–16 shared beers. The bot selection uses the existing match-size setting;
the selected timers and supply feed the normal local match startup.

`Saloon → Verify solo practice startup in Play mode`: PASS, 49 checks in
`Logs/Editor.log`, ending with `SALOON_SOLO PASS` and
`SALOON_EDITOR_ITERATION PASS`. Checks cover all five bot counts, exact role
decks, minimum/maximum timers and beer supply, active-match count locking,
local commands and minigames, teardown, and a real UDP client joining a LAN
host after practice.

Visually inspected the setup page in the running Unity Game view: all settings,
the total-player summary, Back and Start practice fit without overlap. Native
mouse automation returned `noWindowsAvailable`, so mouse clicks through the
new page were not verified. The visual preview temporarily opened directly
on setup; that preview override was removed. No player build was produced.

## 2026-11-23 — Interactive first-build tutorial (development source)

Added a 23-chapter local tutorial, with first-launch welcome, Skip tutorial,
per-build acknowledgement, and a permanent main-menu Tutorial button. Build
identity includes both application version and build GUID. Editor and explicit
automation runs do not automatically open onboarding; Editor preview is available
through `Saloon → Preview interactive tutorial`.

Chapters exercise real movement, planning, action spending, role cards, beer,
quick and accurate shots, reload, window approaches, Coin Flip, both Blackjack
seats, smoking, boosted beer, Sheriff/Deputy inspection, three Outlaw safe jobs,
escape declaration and response, Renegade role knowledge and combat, a round-six
bartender question, and spectator cycling. Each chapter resets its own scenario.
Tutorial bots cannot roam, shoot, or change plans autonomously. The normal
minigame opponent can play cards and pass its opening. Instructions pause the
local action timers. Exiting restores the original tuning object and bot settings.

The welcome and action chapter show **1 ACTION = 1 ROUND** in large bold text.
The action chapter lists eight action categories and separately lists free
controls. Indoor sound is correctly described as two-way proximity sound;
minigame windows have one-way visibility. The smoking deck has separate voice
and activity visibility. The tutorial does not request or connect a microphone.

Validation:

- `Saloon → Verify interactive tutorial in Play mode`: PASS, 72 checks,
  `SALOON_TUTORIAL PASS checks=72` and `SALOON_EDITOR_TUTORIAL PASS` in
  `Logs/Editor.log`. Includes skip/replay, build identity, every exercise,
  completion, early exit, and settings restoration.
- Existing solo-practice suite: PASS, 49 checks, including all 3–7 bot counts
  and a real UDP peer joining LAN after solo teardown.
- Native keyboard preview: Enter opened the welcome's first lesson; Enter
  started it; Space and Ctrl+W completed the actual movement exercise. The
  completion state and Next lesson control were visually confirmed.
- Inspected rendered welcome, action list, healing instructions, and final
  chapter. Images are under the OS temporary `lastcall-tutorial` directory.
- A sentence-length check found no tutorial lesson sentence longer than 20
  words. Short instructions and stable technical terms follow the requested
  [ASD-STE100 writing direction](https://www.asd-ste100.org/assets/files/ASD-STE100_ISSUE9.pdf).
  A complete approved-dictionary conformance audit was not performed.
- `git diff --check`: PASS.

The first-launch decision and persistence were tested in the Editor with build
identities. Fresh-install auto-display in a packaged player and full mouse play
through all chapters still require a player playtest. No new player build or
release was produced.

## 2026-11-23 — Continuous training saloon and optional help library

The default first-launch tutorial is now a continuous playable route. A compact
welcome offers Start walkthrough, Help topics, and Skip tutorial. The route has
short prompts and automatic progress, with no chapter counter or Next buttons.
The old detailed exercises remain optional topic practice, with searchable help.

`SaloonTrainingMap` builds a separate saloon shell, shooting lane, bar and safe,
two open-access minigame booths with real one-way glass, and a connected smoking
veranda. It disables the original world and smoking deck while active. Exit and
topic-practice transitions restore the original objects, lighting, fog, and
practice settings. Shared game models and minigame furniture retain their normal
interaction anchors; the training shell and connected walking paths are new.

The walkthrough runs real host commands and timers for shots, reload, beer,
planning, both minigames, smoking, role inspection, safe work, escape response,
the bartender, and spectator cycling. Its Outlaw example starts with two work
steps already completed by training allies. Normal match rules are unchanged.
The action checkpoint prominently shows **1 ACTION = 1 ROUND** and all eight
action categories. Esc pauses the route; F1 opens help without resetting it.

Validation in Unity Editor:

- `Verify short tutorial in Play mode`: PASS, 33 checks. The test navigated the
  physical paths through production movement and colliders, performed the real
  interactions, and reached the exit in **53.7 seconds**. It did not teleport
  between teaching stations or bypass timed actions. Table seating and the
  elimination/respawn demonstration use the same tutorial transitions as a player.
- Automatic continuation, one persistent training map, inactive original world,
  non-attacking opponents, offline startup, pause/help preservation, map/settings
  restoration, and transition to an individual topic example all passed.
- Retained topic-practice suite: PASS, 72 checks.
- Solo-practice regression suite: PASS, 49 checks, including real UDP LAN after
  solo teardown.
- Native keyboard preview: Enter started the new route; F1 opened the help
  browser; typing a search filtered topics; Esc and Enter returned to the same
  training position. Inspected rendered welcome, new map, prompts, minigames,
  and the optional help layout.
- `git diff --check`: PASS.

The 53.7-second run is an automated mechanical baseline. It excludes reading,
decision time, and first-time aiming. The design target is under five minutes;
a fresh-player timing/usability test is still needed to confirm that target.
Rendered evidence is in the OS temporary `lastcall-walkthrough` directory.
No new packaged player or release was produced.

## 2026-11-23 — Anonymous opening matchup

Retained the original 5.5-second personal role reveal and added a nine-second
results-style matchup briefing before discussion. The briefing uses the existing
role portraits, labels each anonymous slot above its portrait, and marks the
local role, allies, enemies, and known/public/undiscovered identities under the
current role rules. The host's existing intro allowance now covers both screens.

The lineup is constructed from the match-size role deck with the local role first,
then allies and enemies in role order. It never maps portraits to actual player
names, IDs, roster order, or revealed-role fields. Lobby/end state clears it;
snapshot refreshes and later rounds do not replay it.

Validation:

- Unity compiled the change and rendered the original reveal followed by the
  five-player Outlaw briefing: one ally, three enemies, two hidden identities,
  role names above portraits, and no player names.
- Temporary checks compiling the production intro state logic covered all 20
  role/player-count combinations (four roles, four through eight players), exact
  deck composition, anonymous ordering despite changed names/IDs/revealed roles,
  rematch reset, lobby/end cleanup, and no replay on refresh/later rounds.
- The extended temporary planning harness passed 21,052 assertions; its extra
  walkthrough stubs accommodate a concurrent tutorial change and are not game
  changes. The regular planning suite passed 20,831 assertions before that change.
- `git diff --check` passed for the changed intro and README.

The compact six-through-eight-player visual layout was not manually reviewed;
window-control failures interrupted that check. No standalone build or release
was produced.


## 2026-11-23 — Focused tutorial lighting and wrong-plan recovery

The native preview had competing bright stations, floating signs, a destination
marker, a direction banner, and the normal match HUD. The training map now has
one warm spotlight. It lights a reachable waypoint, then the actual target,
beer tray, table, safe, or bartender. Cool ambient light keeps obstacles visible.
The camera background is dark during training and restores on exit.

The route shows one current instruction with a short explanation. Turn guidance
appears when the light is outside the view. Interaction keys appear when the
real game reach checks allow the action. Match counters, idle character labels,
station signs, progress bars, repeated markers, and generic success banners are
removed from the walkthrough. Actual minigame choices and the large bold
**1 ACTION = 1 ROUND** reminder with its eight-action list remain.

Fixed the reported planning dead end: trying to lock Coin Flip, Blackjack, or
Have a Smoke during the Bar exercise now keeps discussion open and explains how
to correct the choice. Bar has a visible outline in the planner. The host checks
all ready paths, including Go alone. A plan that has already advanced into an
invalid room safely returns to the same map and planning checkpoint.

Validation in Unity Editor:

- Continuous tutorial: PASS, **43 checks**, **56.1 seconds** automated route.
  Includes all three wrong destinations, Go alone, direct ready commands,
  recovery from an already-transferred wrong plan, and completing the route
  after correction.
- One active training spotlight, waypoint following, actual beer/card station
  focus, normal lighting/background restoration, offline startup, help
  preservation, and separate topic-practice takeover passed.
- Normal room/companion planning and reservation regression suites: PASS,
  including a real LAN host, paired locking, full-room rejection, unlock, and
  delayed-snapshot handling.
- Rendered movement, offscreen turn guidance, target, beer, cards, and the action
  list were visually inspected. Copies are in `Logs/Verification/tutorial-focus`.
- `git diff --check`: PASS.

The automated time excludes human reading and aiming. Fresh-player usability
and under-five-minute timing still need a player playtest. No packaged player
build or release was produced.


## 2026-11-23 — Action keys and spectator checkpoint controls

Added the actual controls in parentheses beside all eight actions in the large
round-action reminder and the help library: shooting (left/right click), reload
(R), beer (E), inspect (C), safe work (H), escape (X), bartender (I), and pass (F).
The reminder also names movement (WASD), aiming (mouse), and drawing (Q).
The missed-shot action cost remains explicit.

The spectator exercise previously used an ambiguous Left/Right instruction and
hid the normal clickable controls when the training HUD was simplified. It now
shows a countdown during the normal 3.2-second death-camera delay, then explicitly
names the Left/Right arrow keys and offers visible Previous/Next buttons. The
pointer stays available for the buttons. Training bots are restored and placed
in front of the bar, looking toward the lit beer tray, so there are always
multiple living views. Switching targets continues the route and restores the
learner for the exit. A host snapshot refresh also handles an overdue local
observer update. Normal-match spectator rules and delay are unchanged.

Validation:

- Continuous tutorial: PASS, **49 checks**, **57.7 seconds** automated route.
- The spectator camera automatically reached a living player's eye position
  after the delay; both target directions worked; the controls rendered with
  an unlocked pointer; switching continued through respawn and the exit.
- The updated action reminder, death countdown, and settled spectator view with
  its buttons were visually inspected. Evidence: `Logs/Verification/tutorial-focus`.
- Added **Saloon → Preview tutorial spectator checkpoint** for direct Editor
  retries without repeating earlier exercises. Opened that preview and verified
  a native Right-arrow key press restored the learner and continued to the exit.
- `git diff --check`: PASS. No packaged player build or release was produced.


## 2026-11-23 — Normal game vitals and tutorial sunset deck

The continuous tutorial now calls the normal `Vitals()` renderer, using the same
health percentage, five health segments, cartridge icons, empty cartridges,
Renegade unlimited-ammo display, smoking boost indicator, and spectator health
behavior. The planner, role card, and bartender panel keep normal visibility
rules. Removed the redundant health percentage from the tutorial prompt.

The connected smoking deck now uses `SaloonSunset.Build`, the same desert sky,
low sun, mesas, cacti, terrain, and outdoor sun as the real smoking area. Cedar
planks, open rails, and a bench use the normal weathered outdoor surface style.
The saloon's east wall and roof stop before the deck so the sun is visible on
arrival. Entering faces the player toward the sunset; the indoor action lamp is
off outside, and outdoor scenery is hidden when the player returns indoors.
Generated tutorial scenery materials and meshes are disposed with the map.

Validation in Unity Editor:

- Continuous route: PASS, **53 checks**, **54.9 seconds** automated run.
- The shared sunset vista has only nonblocking scenery and uses the outdoor
  visibility layer. Its directional light affects only outdoor scenery.
- The arrival camera faces the sun, and a physical ray confirms a clear low-sun
  view from the entrance. Returning indoors restores the single action light.
- Inspected the rendered normal health/ammo HUD during shooting, reload,
  healing, minigames, smoking, and spectating. Inspected the sunset render.
  Evidence is in `Logs/Verification/tutorial-focus`.
- Added **Saloon → Preview tutorial sunset deck** for direct Editor review.
- `git diff --check`: PASS. No packaged player build or release was produced.

## Beer artwork and waiting-scene continuity — 2026-11-23

- Unity 6000.5.10f1 compiled the runtime and Editor changes.
- **Verify beer artwork and scene continuity** passed in Editor Play mode,
  including beer stock, healing, restrictions, replacement mug import, unchanged
  camera/position during aftermath, disabled actions and a single camera cut at regroup.
- Tabletop blackjack and coin-flip checks passed: their physical cards/coin
  persist during aftermath, controls close, and props clear at regroup.
- Production planning/rules verification passed over 21,000 checks.
- Screenshots and focused checks: `Documentation/BeerScenePreviews/`.
- No downloadable player builds were rebuilt; remote human transition testing
  remains outside this local Editor check.

## 2026-11-23 — Hidden allies and random independent role

Development source now hides Deputies from the Sheriff and removes the Renegade's
complete role roster. Only Outlaws retain named hidden teammate knowledge; the
public Sheriff badge remains available. Removed the Renegade story-map flags that
could certify hidden factions from observed deaths. Updated role guides, tutorial
copy, bartender eligibility, and private information panels accordingly.

Each new match/rematch chooses exactly one independent with a 50/50 host roll:
Renegade or Swindler. The anonymous opening hides that selection from other roles
behind an Independent slot, while showing the recipient their own correct role.
The Swindler earns a private personal victory only from a lethal Sheriff/Deputy
shot; nonlethal, Outlaw, Renegade, self-inflicted and reflected damage do not earn
it. Others continue their heist. Final results preserve the personal winner beside
faction winners; rematches/lobby reset it. Law can defeat all threats without
killing a surviving Swindler. A sole surviving Swindler does not win.

Networking identity is now state-20, preventing old clients from joining with
obsolete role/knowledge rules. No packaged player or release was produced.

Validation:

- Production rules/planning harness: 21,009 checks passed, including both decks
  for all 4–8-player sizes, private teammate permissions, masked independent intro
  slots, roll boundaries, lethal-killer eligibility, personal victory recording,
  results eligibility and Swindler story guidance.
- Full runtime and Editor C# assemblies compiled with Unity's compiler and
  references; logs in /private/tmp/lastcall-role-rework-compile/.
- The live Unity run exercised role privacy across both independents and 4–8
  players and reached real Law-caused lethal shots, private snapshot state, result
  preservation and rematch reset. The full run did not pass: repeated synthetic
  avatar deaths/restorations hit ragdoll errors, and a later run lost its host
  fixture. The revised rule fixture detaches local avatar rendering and resets
  Editor-run state after an interrupted run; its full live rerun remains pending.
- Further live attempts were interrupted by editor/window changes. No claim of
  complete rendered, LAN, reconnect or human balance verification is made here.

Practice Swindler bots draw a gun and pass; they do not simulate social bluffing.
The existing Renegade portrait is reused for the Swindler with distinct role text.


## 2026-11-23 — Separate beer and cash drawer positions; live theft checks

- Moved the shared beer tray 2.8 metres left along the counter, from x=-1.8 to
  x=-4.6; the drawer remains at x=-0.45. Updated training routes, beer fixtures,
  and preview framing to use the shared tray position.
- Unity 6000.5.10f1 / Metal passed SALOON_DRAWER: 40 checks covering all five
  roles inspecting at full health, report privacy, competing thefts producing
  one carrier, compressed snapshots, next-phase escape, fresh defender actions,
  headshot interruption, successful escape, elimination returns, and separate
  beer/drawer interaction positions.
- SALOON_BAR_BEER and SALOON_EDITOR_ITERATION passed: 45 assertions covering
  physical walking access between stools, drinking/healing, stock races,
  rejected interactions, visible mugs, and round/camera continuity. Updated an
  old route to stop in front of the fitted drawer instead of inside its collider.
  Refreshed previews are in Documentation/BeerScenePreviews/.
- Tools/verify_planning.py passed 21,043 checks. These are source/editor results;
  human social-balance sessions and dedicated reconnect/rematch checks remain.


## 2026-11-23 — Close role/theft engineering gaps

- Removed reflected ally damage. Headshots still damage allies normally, so a
  mistaken accusation can hurt the team, but shooter health cannot certify a role.
  Ally/enemy hit confirmations now match; the headshot story milestone advances
  for either, closing two further allegiance leaks. Updated handbook/tutorial copy.
- Fixed repeated reconnect: NGO disconnect callbacks supply the current network
  ID, whereas LocalId retains the original seat identity. Comparing the callback
  with Manager.LocalClientId restores recovery after the second disconnect.
- Full live role/privacy verification passed 1,299 assertions across both
  independent roles, 4–8 players, all phases, observer privacy, Swindler killer
  eligibility, private victory, result preservation, rematch reset, and identical
  normal/boosted ally/enemy feedback. Synthetic server fixtures detach avatar
  rendering; the health suite separately exercises rendered labels and spectating.
- Eight real localhost UDP connections passed 34 reconnect/rematch assertions:
  repeated recovery preserves personal victory, money, private reports, planned
  and active escape, spent actions, and single-carrier ownership. Results, votes,
  rematch/lobby resets, reservation expiry, and aborted results also pass.
- Discussion evidence passed 18 assertions: sight/occlusion, death, brief glances,
  ambiguous theft/inspection witnesses, timed drawer reports, report privacy,
  stale reports, wire serialization, notebook limits, and reset. Updated obsolete
  three-step safe fixtures for the two-second shared drawer.
- Health passed 53 assertions plus 12 rendered discussion-health checks, including
  allied damage without reflection, lethal target-only damage, five-hit death,
  partial health, healing, wire confirmation, hidden-role labels and spectating.
  Corrected an obsolete fixture that expected the Renegade to see an Outlaw label.
- Cash drawer regression passed all 40 checks again. Production planning/rules
  harness passed 21,043 checks. Each completed live suite reported
  SALOON_EDITOR_ITERATION PASS; scoped git diff whitespace checks passed.
- Items 1 and 2 are implemented and engineering-verified in development source.
  No new downloadable build was produced. Human social-balance testing is still
  outstanding; its concrete session checklist is in REPLAYABILITY-README.md.


## 2026-11-23 — Enclosed room sight and sound; quiet opening

- Coin Flip and Blackjack privacy panes are opaque on both sides. Saved scenes
  migrate using window layout version 4; the tutorial uses the same two-way
  opacity. Living Bar players no longer receive minigame table snapshots.
- Indoor cross-room voice uses Vivox participant taps with native duplicate
  playback silenced, positional Unity playback, 28% volume, and an 850 Hz
  low-pass filter. Same-room voices stay clear; local player mutes apply to taps.
  Avatar sounds across indoor rooms use the same low-pass cutoff. Smoking still
  uses its isolated channel and visibility rules, and spectator playback follows
  the watched player's room.
- Waiting-room voice remains enabled. Both opening role screens pause input and
  output (including routed sources); table discussion resumes voice automatically.
  Microphone echo tests cannot bypass the role reveal pause.
- Unity / Metal passed 68 room-window assertions, including actual rendered
  occlusion in both directions, hidden Bar table state, collision/bullets, and
  cross-room gunshot filtering. Live Vivox passed 195 voice-control assertions:
  symmetric room rules, audio filter configuration, role-intro silence/resume,
  recovery, outdoor isolation, spectator routing, and cleanup. The live test
  never requests or captures the physical microphone. Corrected its spectator
  fixture to avoid testing exactly on the floating-point death-delay boundary.
- Tabletop coin flip regression passed after updating Bar visibility assertions;
  the production planning/rules harness passed 21,043 checks. Scoped diff checks
  passed. Protocol identity is state-22 to reject clients with old privacy rules.
- Remote-human listening and subjective intelligibility tuning remain untested;
  SDK connectivity and routing are tested, not a two-human recorded conversation.
  No downloadable build was produced. Continue roadmap item 3 with room incentives.


## 2026-11-23 — Featured medical room opportunity (roadmap item 3)

- The host randomly announces Coin Flip or Blackjack for medical supplies during
  each discussion. Snapshots do not reroll it. Normal two-player reservations,
  unmatched fallback, minigame rules, result pauses, and opening deadlines apply.
- The featured room winner chooses Heal 20% or Keep opening. Healing immediately
  restores 20 percentage points (capped at 100), spends the one round action, and
  releases the opponent's response. Keeping the opening is free, declines supplies,
  and retains the existing deadline; inactivity passes normally. Full-health,
  losing, dead, wrong-room, expired and duplicate claims cannot grant healing.
- Supplies use no beer, do not consume or multiply the smoking boost, and produce
  no public reward-use announcement. Choice flags belong only to the eligible
  winner's private snapshot. The guided tutorial has no featured opportunities;
  Help topics explain the normal-match rules. Injured practice bots can use supplies.
- Planning announces the opportunity before room selection and labels its room
  tile. The winner gets a clickable heal/opening choice with the time remaining.
  Inspected both rendered screens. Previews: temporary lastcall-medical-supplies/.
- Unity / Metal: SALOON_MEDICAL passed 84 assertions across both rooms and all
  five roles, including production commands, choice authorization, action/response
  rules, health caps, boost/beer independence, timeouts, reset, and wire state.
- Eight-client UDP reconnect/rematch verification passed 36 assertions, including
  equal featured-room announcements and persistence across reconnection. Both
  tabletop Coin Flip and Blackjack regressions passed. Planning/rules passed
  21,043 checks; scoped whitespace checks passed. Protocol identity is state-23.
- This is development source; no downloadable build was produced. Human balance
  testing remains: does healing tempt the guard away, is foregoing a shot worth
  it, and is the normal ten-second choice window understandable? The session
  checklist is in REPLAYABILITY-README.md under Item 3.

## 2026-11-23 — solid minigame walls and large paintings

Replaced the two Bar-facing window assemblies and their frames with full-height plaster walls and matching walnut dado. Enlarged the painted card and coin graphics to cover the wall faces. Moved both private-room lights to centered ceiling pendants and moved the pendant that straddled a room boundary into the Bar.

Unity compiled both assemblies. `SALOON_ROOM_STENCILS PASS` checked saved-scene migration, two large paintings, no remaining minigame windows, two centered room pendants, no lamp/wall intersections, stable repeated application, and muffled audio rules in both directions. The Play mode room-boundary suite passed (`SALOON_WINDOWS PASS`, `SALOON_EDITOR_ITERATION PASS`): rendered visibility blocked from both sides and Bar seats, movement and bullet collisions, real shots stopped by walls, hidden table state, and a filtered cross-room gunshot. Five rendered previews were inspected under `Documentation/RoomStencilPreviews/`. Scene and source only; no release build.


## 2026-11-23 — Visual, occasional room events

- Round one has no medical event. From round two, each discussion has a 50%
  chance of no event, 25% Coin Flip, and 25% Blackjack. The host rolls once per
  round and shares the same result with all clients; reconnection preserves it.
- Replaced the long event banner with a breathing teal room card, mint medical
  case icon, and short Medical supplies label. Planner selection accents are
  blue. Removed the solo/table explanation and invitation instruction paragraphs,
  and kept the single Choose your room title and Players & invitations column.
  Reduced-motion settings use a static highlight. No-event rounds highlight
  neither table nor Bar.
- Unity / Metal passed 93 medical assertions, including quiet round one, random
  selection boundaries, no-event reward rejection, both rooms, all five roles,
  and the existing winner/action/health/privacy rules. Eight real localhost UDP
  clients passed 36 reconnect/rematch assertions. Both live suites completed with
  SALOON_EDITOR_ITERATION PASS. Production planning/rules passed 21,043 checks.
- Inspected rendered event, quiet first-round, and quiet later-round planners.
  Previews are in MedicalEventPreviews/. Protocol identity is state-24.
  Source changes only; no downloadable build was produced.


## 2026-11-23 — Escape warning before room commitments

- Reproduced a bot defender staying in a locked minigame pair while an Outlaw
  announced escape. Previously the public warning appeared only after room
  assignments, allowing a round-one theft and unopposed round-two escape.
- Escape declarations are now public during discussion, before room assignment.
  The planner marks Bar with a pulsing red card and identifies the carrier's
  intent. The first announcement guarantees ten seconds to reconsider rooms,
  even when everyone has locked or the timer was nearly over. Repeated
  cancel/redeclare commands cannot repeatedly extend discussion. Holds reset
  for each discussion and match. No additional theft steps were introduced.
- Bot carriers announce before bot pair selection. An available Law or Renegade
  bot responds to that public declaration by guarding Bar, releasing a bot-only
  pair if necessary; explicit human invitations remain respected. A round-two
  escape remains possible when nobody successfully responds.
- Production planning/rules passed 21,096 checks, including new 4–8 seat bot
  response scenarios for Sheriff, Deputy and Renegade, explicit distractions,
  theft-round rejection, stable planning, deadline holds, spam prevention, and
  reset. Existing fixture-only unused-field compiler warnings remain.
- Unity / Metal passed 44 drawer/escape assertions, including real wire-format
  public planning warnings, the unskippable planning window, and an actual bot
  shot interrupting a living carrier's escape through normal combat. Eight real
  localhost UDP clients passed 38 reconnect/rematch assertions, including every
  client receiving the warning before room lock. Both live suites completed
  with SALOON_EDITOR_ITERATION PASS. Inspected the rendered planner in
  EscapePreviews/Planning.png. Scoped whitespace checks passed.
- Protocol identity is state-25 to reject clients using the old warning timing.
  Source only; no downloadable build. Human balance testing remains necessary.
  Next roadmap work is item 4, after testing this Outlaw follow-up.


## 2026-11-23 — Sheriff death victory and three-round theft

- Sheriff death now ends actions and awards the Outlaw team, regardless of the
  killer. The sole-surviving Renegade takes priority; killing the Sheriff while
  any other player remains instead awards Outlaws. Eliminated Outlaw teammates
  share their faction's victory. Death handling resolves the result outside the
  shot-specific path too, retaining the usual final aftermath animation.
- Theft requires three shared two-second drawer actions in three different
  rounds. At most one step completes per round across the team, including
  simultaneous attempts and duplicate completions. Only the third worker gets
  the bag. Earliest money collection is round three and escape is round four.
  Interrupted actions grant no progress. Carrier death returns the money and
  resets all three steps. Private inspections can detect partial tampering
  while money is still present; public checks retain the same ambiguous pose.
- Updated role cards, story maps, drawer prompts, training and README guidance.
  Renegade bots leave Sheriff until last. Bots stop seeking the drawer once
  that round's shared step is complete; an otherwise idle bot passes instead
  of repeatedly attempting the rejected action. The focused test fixture keeps
  its inspection bot idle while separately testing another bot's pass.
- Production planning/rules passed 21,177 checks. Unity / Metal passed 51
  drawer/escape assertions and 1,314 role/privacy/victory assertions, including
  actual lethal shots by Outlaw, Renegade, Deputy and Swindler, remaining-action
  lockout, dead Outlaw winners and last-survivor priority. Eight real localhost
  UDP clients passed 41 reconnect/rematch assertions, including partial theft,
  per-round cap persistence and full reset. All three live suites completed
  with SALOON_EDITOR_ITERATION PASS.
- The chapter-based tutorial passed 74 live checks, including three real theft
  actions, automatic training round advancement, delayed escape and cleanup.
  Its full run exposed and corrected a missing initial spectator viewpoint in
  the existing library lesson; cycling views now completes that lesson.
- The continuous walkthrough passed 52 checks in 72.9 seconds, including three
  theft actions, the full ten-second escape warning, spectator controls and map
  restoration. Restricted its retry helper to the action phase so an old spent
  action cannot skip escape discussion, and corrected its test driver to send
  an explicit ready/lock value instead of an unlock command. Both tutorial
  suites completed with SALOON_EDITOR_TUTORIAL PASS. Scoped whitespace checks
  passed.
- Protocol identity is state-26. The existing posse and random independent slot
  remain unchanged by this revision. Source changes only; no downloadable
  build was produced. Human balance testing remains outstanding.


## 2026-11-23 — Posse removed

- Removed the arrival system, round-20 automatic ending, escape round cutoff,
  public countdown, approach audio component and runtime-only hoofbeat/distant
  shot recordings. Removed its snapshot fields and obsolete outcome handling.
  Reload, drinking and revolver recordings remain bundled; the preparation
  script and manifest now contain only active recordings. Archived original
  source audio and historical verification records retain their provenance.
- Updated HUD, pause menu, role guidance, tutorial topics, story maps, README
  and replayability reference. Law wins by eliminating the Outlaws and any
  Renegade while the Sheriff survives. Sheriff death, sole-surviving Renegade,
  Swindler personal wins and three-step theft retain their existing rules.
- Production planning/rules passed 21,254 checks, including no automatic result
  in late rounds and authorized escapes in rounds 20, 21 and 50. Unity / Metal
  passed 82 new checks: normal play through round 25 into round 26, no arrival
  component or bundled arrival audio, no countdown fields over the real wire,
  and a round-26 escape with fresh defender actions and normal victory.
- Eight actual localhost UDP clients passed 41 reconnect/rematch assertions.
  Both live suites completed with SALOON_EDITOR_ITERATION PASS. Runtime and
  Editor assemblies compiled; deleted script GUIDs have no serialized asset
  references. Scoped whitespace checks passed. Protocol identity is state-27.
- Source changes only; no downloadable build was produced.


## 2026-11-23 — Blackjack dead time and high-card ties

- Initial dealing is 1.2 seconds instead of 3.2, with 0.22-second card spacing
  and 0.3-second slides/flips. Card input settles for 0.45 seconds instead of
  1.5. Idle dealers advance after two seconds instead of eight; bots also act
  sooner. Blackjack results pause for two seconds instead of four. Coin Flip
  keeps its existing result timing.
- Tied totals, including two natural blackjacks, automatically draw high cards
  from the remaining deck. Aces are highest; suits do not decide ties. Equal
  ranks redraw only those cards. Two distinct rows show the deciding cards
  while preserving the original hands. A 1.2-second presentation leads to one
  winner and the normal opening, without another Blackjack hand or input.
- Discussion time, the 45-second player decision window, the ten-second combat
  opening, medical choices, and damage are unchanged. Weapons stay locked
  through card settling, the tiebreak, and the result pause.
- Standalone production Blackjack checks passed 10,000 seeded hands. A further
  stand-only sweep resolved 467 tied hands, including 29 equal-rank redraws;
  a fixed equal-fours regression confirms only the high cards are redrawn.
  Production planning/rules passed 21,254 checks. The live Blackjack flow and
  tabletop suite passed 51 assertions with real commands, snapshot serialization, hidden
  cards, duplicate-input rejection, full opening/response, tied naturals, and
  physical tiebreak cards. Joining during a tiebreak presents the full existing
  hand together instead of staggering historical cards. Rendered cards were inspected.
- Updated the replayability plan: remove the shorter match deadline; playtest
  pacing before combat balance, then prioritize Phantom, round events, ordinary
  Deputy bluffing before a dedicated role, and finally maps/appearances.
- Protocol identity is state-28 for the added high-card snapshot fields and
  tiebreak stage. Development source only; no downloadable build or human
  balance session was produced.


## 2026-11-23 — Front sunset and private back bench

- The room-voting smoking card now says Out the back. Its existing solo/paired
  plan remains the only route to the private deck. The front entrance now has
  low saloon doors framing the same desert sunset; full-height collision and
  existing Bar bounds still prevent walking out through it.
- Front scenery uses its own visibility layer and restricted light. Indoor
  cameras see the front vista without the back deck, while smokers see only
  their own deck/vista. Private player snapshots and separate voice channels
  keep smoker avatars, cigarette animations, and conversation out of the Bar.
- Added two interaction spots to the existing bench, including migration of
  saved decks without duplicates. Space near an empty spot sits facing the
  sunset; Space again finds a safe standing position and restores movement.
  Seated smokers can take a drag without spending an action. Weapons, health,
  and cigarette boost rules retain their existing behavior.
- Unity / Metal passed 35 sunset/back-deck checks, including real walking to
  both bench spots, occupancy rejection, seated snapshot posture, smoking while
  seated, safe standing, private snapshots/voice-channel identity, full-height
  entrance collision, separate camera/light masks, and stable repeated setup.
  Inspected rendered front-entrance and seated-pair previews in SmokingPreviews/.
- Additional live regressions passed: medical room choices (93 checks), public
  floor/entrance movement (34 checks), and the continuous tutorial (52 checks).
  The tutorial hides the front vista while its own map runs and restores it on
  exit. An initial tutorial run was interrupted by a pending script reload;
  the clean rerun passed. Runtime and Editor assemblies compiled, and scoped
  whitespace checks passed. Source changes only; no downloadable build.


## 2026-11-23 — Middle Blackjack pace and exterior windows

- Owner feedback: the first faster Blackjack version felt rushed. Opening deal
  now takes 2.2 seconds, with 0.4-second spacing and 0.5-second slides/flips.
  Hits/reveals settle for one second; idle dealers advance after five seconds.
  Bots also pause longer between decisions. Results last three seconds, and
  automatic high-card ties show their deciding pair for two seconds. Discussion,
  the 45-second player choice allowance, ten-second opening, damage, and the
  no-redeal tie rules are unchanged.
- Added two exterior windows to each long Bar wall and one to the outer side of
  each minigame room. Saved-world migration cuts every outer wall layer and
  creates timber frames, recessed sills and transparent glass. Glass remains
  solid to movement and shots; shot trails end at it. Room bounds and the opaque
  Bar-facing room walls/murals are preserved. Reapplying creates no duplicates.
- Exterior views use the existing public desert scenery. Cameras still exclude
  the private back deck, and living indoor snapshots omit smokers; positional
  voice/privacy rules are unchanged. Editor scene-saving support persists the
  new glass material along with the existing room materials.
- Unity / Metal: 101 room/window assertions passed, including rendered views
  through all six openings, a rendered smoking-layer visibility probe through
  each window, character collision, bullet rays/trails, opaque room boundaries,
  hidden tables, muffled sounds and real shots absorbed by the interior walls.
  Blackjack passed 51 flow/tabletop assertions, including the revised timings,
  tied naturals, physical high cards, duplicate-input rejection and opening
  protection. Sunset/back-deck regression passed 35 assertions, including both
  bench spots, seated smoking, safe standing, private snapshots and separate
  voice-channel identity. All three suites ended with SALOON_EDITOR_ITERATION
  PASS. Production planning/rules passed 21,254 checks; scoped whitespace checks
  passed. Six rendered exterior views were inspected in ExteriorWindowPreviews/.
- Recorded positive initial owner feedback on theft/escape and the value of
  treatment/smoking. Phantom is deferred in the replayability reference; round
  events are a candidate next step, with no new event implemented in this change.
- Protocol identity is state-29 for the changed physical layout. Development
  source only; no downloadable build or completed formal balance session claimed.


## 2026-11-23 — Bar sunset lighting and gradual dusk

- Bar views replace the bright shadowless daylight fill and three ceiling lamps
  with two low, golden window light sources, one dim pendant and faint wall
  lamps. Window frames and room geometry still cast normal shadows. Dusk shifts
  the Bar's ambient bounce toward cool night and fades the public exterior sky
  and its sun glow along with the incoming light.
- The existing shared snapshot round drives the presentation: sunset in round
  one, intermediate dusk around round ten, and night by round twenty. Transitions
  blend smoothly, late joiners use their current round immediately, and a new
  match resets sunset. Later rounds keep night; no match limit or rules changed.
- The change is scoped to the Bar camera and its weapon overlay. Original
  ambient, lights, masks and public sky are restored after each render. Blackjack
  and Coin Flip keep their existing lighting; smoking and tutorial views retain
  theirs. No extra network fields or protocol change were required.
- Unity / Metal passed SALOON_BAR_LIGHTING (15 checks): rendered comparisons for
  rounds 1, 10, 20 and 50, a gradual fade, new-match reset, actual serialized round
  input, removal of ceiling floods, source count, and restoration of ambient and
  light state. Mean wide-view luminance was 0.391 before, 0.196 at round one,
  0.163 at round ten and 0.131 at round twenty. Rounds twenty and fifty matched.
  Both minigame room renders matched their original baselines pixel for pixel
  at early and late rounds; the smoking view also matched its baseline.
- Room/window regression passed 101 checks, including six exterior views,
  smoking visibility boundaries, solid glass, bullet rays/trails, opaque room
  walls, real shots and muffled sound. Both live suites ended with
  SALOON_EDITOR_ITERATION PASS. Production planning/rules passed 21,254 checks;
  scoped whitespace checks passed. Inspected before/sunset/dusk/night renders
  saved under BarLightingPreviews/. An old Play session was stopped after a
  script reload; validation ran in clean sessions.
- Added the requested post-lighting reminders to REPLAYABILITY-README.md:
  Toilet Trouble, Darts, Liar's Dice, and the original nine-point roadmap.
  These activity ideas remain proposals. Source only; no downloadable build.


## 2026-11-23 — Random Toilet Trouble and dimmed Bar lamps

- One uniform host selection from all living players each discussion; no role,
  health, human/bot or previous-assignment weighting. Private Accept/Deny offer;
  accepting releases a partner and reserves a solo Bar plan and round action.
  Denying preserves normal planning. Pending offers cannot be readied by a
  partner, stolen by another player, replayed or answered after discussion.
- Added a physical toilet and plunger in the public Bar's right-hand corner
  beside the counter. Space to stand, normal collision movement to approach,
  and E starts a two-second placeholder interaction. Completing it grants
  twenty health points once, capped at 100%, using one action and no beer.
  Cigarette buffs neither multiply this heal nor get consumed by it. Hits,
  distance and phase changes interrupt work; new matches clear assignment.
  Detailed unclog mechanics are deferred. Injured practice bots perform the
  job; full-health bots decline without affecting the random assignment.
- Corrected the earlier Bar lighting revision: all four Bar pendants and both
  sconces remain visible and enabled at lower intensity, and the daylight fill
  stays enabled but dim. A faint amber chamber glow remains visible at night.
  The existing sunset-to-night progression over rounds 1–20 remains. Camera
  rendering restores the original lamp, glass emission, ambient and sky state.
- Unity / Metal: SALOON_TOILET passed 53 checks, including uniform selection
  across 1,000 seeded draws, eliminated-player exclusion, compressed private
  snapshots, all five roles, planning consent, stale/duplicate commands, action
  spending, healing limits, interruptions, reset, actual walking from a Bar
  seat, timed completion, and an assigned bot navigating to complete the job.
  Existing medical opportunities passed 93 regression checks; production
  planning/rules passed 21,254 checks. Scoped whitespace checks passed.
  SALOON_BAR_LIGHTING passed 15 checks with every fixture on. Mean wide-view
  luminance: old 0.392, sunset 0.202, round ten 0.168, round twenty 0.137.
  Blackjack, Coin Flip and smoking renders still match their original lighting
  pixel for pixel. Existing medical opportunities passed 93 regression checks.
  All live suites ended with SALOON_EDITOR_ITERATION PASS. Production planning
  and rules passed 21,254 checks; scoped whitespace checks passed.
- Inspected offer/corner renders in ToiletDutyPreviews/ and refreshed the
  before/sunset/dusk/night images in BarLightingPreviews/. Updated the original
  REPLAYABILITY-README.md: Toilet Trouble is implemented at this initial scope;
  Darts and Liar's Dice remain proposals. Protocol state-30 includes the new
  private duty fields. Development source only; no release build was produced.


## 2026-11-23 — Enclosed bathroom, click-to-pump unclogging and brighter Bar

- Extended the right-hand Bar wall between its exterior windows into an enclosed
  bathroom with a signed doorway, floor, ceiling, solid walls, basin and lamp.
  The toilet and plunger now sit inside it. The extension remains part of the
  Bar assignment and voice area; it does not connect to either minigame room,
  the desert or the private smoking deck. Original solid wall layers and
  decorative chair rails are cut at the doorway. Mesh clipping preserves
  material submeshes and tangents so the surrounding wood renders correctly.
- Replaced the two-second automatic completion with eight distinct left-click
  strokes, at least 0.4 seconds apart. Left click can equip and pump directly;
  E equips without progressing. Holding the button, waiting, duplicate input,
  spam, stale rounds and another player's commands cannot complete the job.
  Each accepted stroke moves the plunger, lowers the visible waste and clears
  the water. The last stroke awards the existing 20-point heal once, capped
  at 100%, for one action. Damage, distance and action-time expiry cancel work
  without healing or a refund. Random discussion offers remain unchanged.
- Replicated Bar-visible clog/stroke state and private command sequence recovery
  for reconnecting workers. Toilet bots use the existing collision-aware Bar
  route finder to navigate the chairs and doorway, then pump normally. Protocol
  state-31 covers the extended map and new pump commands/snapshot fields.
- Raised Bar lamp intensity, daylight fill and ambient bounce while retaining
  every fixture and the sunset-to-night progression. Night remains readable.
  Rendered mean luminance is approximately 0.259 at sunset, 0.224 at round ten
  and 0.188 at round twenty, versus 0.202 / 0.168 / 0.137 in the prior revision.
  Blackjack, Coin Flip and the smoking deck retain their original lighting.
- Unity / Metal: Toilet Trouble passed 83 checks, including actual walking
  through the doorway, enclosing walls, clear visual trim, input validation,
  all roles, no automatic completion, visible plunger/clog changes, healing,
  interruptions, expiry, private snapshots and bot navigation/completion.
  Bar lighting passed 15 checks, including original-lighting comparisons for
  both minigame rooms and smoking. Room/window regression passed 101 checks;
  the current cash-drawer/theft/escape suite passed 51 checks. These live suites
  ended with SALOON_EDITOR_ITERATION PASS. Production planning/rules passed
  21,254 checks and scoped whitespace checks passed.
- Also attempted the older physical-safe suite. Its obstruction test still
  positions its barrier using the former floor-safe height, leaving a gap
  beneath it at the current raised cash drawer; it fails before navigation.
  That legacy fixture was left unchanged. The current drawer suite above passed.
- Inspected and saved bathroom entry, clogged bowl, pumping and completion
  images in ToiletDutyPreviews/ and refreshed BarLightingPreviews/. Updated
  REPLAYABILITY-README.md and README.md. Source changes only; no release build.


## 2026-11-23 — Toilet Foley and injured-only offers

- Toilet duty now samples uniformly from living players below maximum health.
  Full-HP and eliminated players are excluded; if no one is injured, no offer
  appears and existing room plans/readiness stay intact. A selected player who
  reaches full health cannot accept an old offer. Roles, human/bot status and
  previous assignments do not bias selection among eligible players.
- Added three CC0 recorded Foley clips: suction pop on an accepted plunger
  stroke, water gurgle while pumping and one completion flush. Playback follows
  accepted snapshot progress, never raw clicks. Duplicate, stale and initial
  late-join snapshots do not replay effects. Interruptions stop pumping audio;
  regroup stops remaining tails. Sources are positioned at the toilet, fade
  with distance and use the existing indoor low-pass/volume convention through
  room walls. Private smoking snapshots contain no toilet activity or audio.
- Source downloads, hashes and creator links live in ArtSource/Audio/Toilet/;
  runtime mono PCM clips, credits and filters are in Resources/Audio/Toilet/.
  Tools/prepare_toilet_audio.py reproduces the trims, normalization and fades.
  Recorded clip durations: plunger 0.368 s, gurgle 0.650 s, flush 4.512 s.
  PCM peaks are 0.708 / 0.218 / 0.428, without clipping. Protocol is state-32.
- Unity / Metal passed 87 toilet-flow checks plus 18 audio checks (105 total),
  including all-healthy/no-offer planning, 1,000 mixed-health selections, a lone
  99-HP candidate, full-health acceptance rejection, every injured role, actual
  walking/pumping/healing and bot completion. Audio checks cover actual source
  playback, resource samples, silence on spawn, late joins, repeated snapshots,
  one completion flush, interruptions, indoor muffling and smoking isolation.
  The live suite ended with SALOON_EDITOR_ITERATION PASS. Production planning
  and rules passed 21,254 checks; source audio hashes, peaks, Unity metadata and
  scoped whitespace checks passed. Validation used a fresh Play session after
  stopping an old session affected by a script reload. Source only; no release
  build was produced.


## 2026-11-23 — Optional isolated toilet mission with retained combat action

- Replaced the Bar-attached bathroom with a standalone closed room at world
  (0, 0, 40), under the runtime scene root `Isolated solo bathroom`. Room ID 4
  uses render layer 17, separate camera visibility and a private voice-channel
  identity. The former Bar extension and doorway no longer exist. The bathroom
  cannot be selected, reserved or used as an invitation destination.
- Each discussion uniformly selects one living injured player, without role,
  human/bot or previous-assignment bias. Healthy players receive no offer.
  Reject preserves normal planning; accept releases the prior companion,
  reserves a Bar return and starts the mission at the room-action phase.
- Arrival starts a 12-second timer, including idle time. Eight distinct strokes
  at least 0.4 seconds apart heal 20 percentage points, capped at full health,
  once. Success immediately returns the worker to Bar with the same round's
  combat action unspent. Idle/partial-work timeout or abandoning active work
  returns without healing or spending that action. The shared clock continues;
  shortened action phases reserve up to three seconds for combat after return.
- Damage and combat/action commands are blocked while inside. Protection ends
  immediately on return. Return resets movement/combat history to avoid stale
  poses, preserves beer/ammo/cigarette buffs and restores Bar visibility. Beer
  healing, shared stock and action cost were left unchanged. Bots complete the
  same isolated mission and return without spending their action.
- Plunger/gurgle Foley is isolated to the mission; only its worker hears the
  completion flush follow the immediate return to Bar. Private progress and
  reconnect command sequencing prevent outsider, duplicate and stale effects.
- Unity / Metal passed 121 mission checks plus 21 audio checks (142 total),
  ending with `SALOON_EDITOR_ITERATION PASS`. Checks include 1,000 seeded
  selections, all five roles, reject/full-health/stale acceptance, hidden room
  plans, snapshots and camera isolation, damage immunity, normal damage after
  return, idle/partial/short-phase expiry, no round extension, bot completion,
  capped healing and a real boosted headshot fired after returning with the
  retained action. Voice validation covers channel identity and snapshots;
  no live cloud voice call was performed.
- Room/window regression passed 101 checks, with discussion-health verification
  and `SALOON_EDITOR_ITERATION PASS`. The current cash-drawer/theft/escape
  suite passed 51 checks and Bar lighting passed 15 checks, both ending with
  `SALOON_EDITOR_ITERATION PASS`. Production planning/rules passed 21,254
  checks. Inspected fresh mission and returned-to-Bar captures are saved in
  `ToiletMissionPreviews/`. README and REPLAYABILITY-README document the final
  behavior. Network protocol is state-33. Development source only; no release
  build was produced.


## 2026-11-23 — Liar's Dice and announced West room rotation

- Classic alternates Coin Flip on odd rounds and Liar's Dice on even rounds,
  starting with Coin Flip. The authoritative game travels in every discussion
  snapshot, before room voting. Room cards, the discussion banner, invitation
  descriptions, selected plans and the wall presentation name that round's
  game. The same two seats and Coin Flip furniture are reused. Tutorial coin
  lessons continue to use Coin Flip.
- One short contest uses three hidden dice per player, no wild ones and bids
  ordered by quantity then face. A call reveals both cups: meeting the claim
  awards the bidder the opening, otherwise the caller wins. Six seconds per
  decision and a fixed 24-second contest follow a 0.8-second opening roll.
  Idle opening bids one of that player's own faces; later decision timeout
  or the contest deadline automatically challenges the current bid. Invalid,
  duplicate, stale-round/revision and out-of-turn commands cannot change it.
- Each player receives only their own dice until reveal. Public/spectator views
  contain no hidden values; other rooms receive no table state. Physical cups
  and pipped dice follow that same authorization. Bidding uses mouse controls
  and locks standing/weapons. The three-second result pause restores standing
  and dodging before the existing ten-second exclusive winner's opening.
  Both players retain their normal one-action allowance. Passing, idle opening
  expiry, damage and the response use the existing combat rules. Medical events
  support both games. Beer, toilet duty and lighting were left unchanged.
- Bots bid and challenge using only their own hand. Added Editor play and
  verification menus, standalone production-rules checks and protocol state-34
  for the announced game, private dice snapshots and revision-checked commands.
- Standalone rules passed 61,500 checks across 500 seeded rolls, all increasing
  bids, every quantity/face challenge, exact-face counting, copied private
  hands, bounds, completed contests and forfeits. Production planning/rules
  passed 21,254 checks. Live Unity / Metal passed 52 dice-flow checks plus 34
  existing Blackjack flow checks, ending with `SALOON_EDITOR_ITERATION PASS`.
  Dice checks include six rounds of announcement/assignment agreement, actual
  compressed recipient snapshots, private and spectator rendering, invalid and
  stale commands, both timeouts, real paced bot play, unmatched solo fallback,
  winner-only action order, a real opening shot with unchanged damage and the
  medical choice. Existing Coin Flip passed 29 checks and medical events passed
  93 checks; both suites ended with `SALOON_EDITOR_ITERATION PASS`.
- Initial verification fixtures were corrected to compare floating-point timer
  differences with tolerance, clear the stance-input debounce before synthetic
  immediate re-use and wait through the real spectator delay. Final suites
  passed without runtime exceptions. Inspected planning, bidding and reveal
  captures are saved in `LiarsDicePreviews/`. Scoped whitespace and Unity script
  metadata checks passed. README and REPLAYABILITY-README document the behavior.
  Development source only; no release build or separate-machine LAN playtest
  was produced in this task.


## 2026-11-23 — Exterior dice mural and dedicated playable test scene

- Replaced the Dice round's plain text sign with a large transparent cup-and-dice
  stencil. Its paint color, matte material, alpha cutoff, shadow settings, wall
  position and 3.1 × 2.81 metre size match the existing Coin Flip mural. The
  correct graphic swaps in discussion and returns to Coin Flip on coin rounds.
  The artwork adds no collider, doorway or view into the enclosed room. The
  deterministic Swift stencil generator now reproduces all three game graphics.
- Added `Assets/Scenes/LiarsDiceTest.unity`, created and opened by the existing
  Saloon → Play Liar's Dice at table menu. The scene builds the normal saloon
  environment and starts offline with exactly one human and one bot at the
  Dice table. It skips voting, role intro, toilet offers and medical events.
  Only this environment forces Dice on every hand; Classic keeps its rotation.
- N immediately starts another hand, restoring health, ammunition and combat
  actions, including a dead player. New hands advance the round token so old
  inputs cannot apply. After the normal combat phase finishes, the environment
  deals again after two seconds. Normal decision/result/opening timing and
  damage remain active. Leaving clears the practice flag. The new scene is
  excluded from the normal release scene list.
- Unity / Metal passed 62 dice checks plus the 34 existing Blackjack flow checks,
  ending with `SALOON_EDITOR_ITERATION PASS`. Added checks cover mural resource,
  placement, size, paint, collider absence, round swapping, offline two-person
  startup, skipped distractions, consecutive Dice-only hands, state restoration
  and old-command rejection. Production planning/rules passed 21,254 checks.
  The original core Dice rules were unchanged. Scoped whitespace and Unity
  asset metadata checks passed.
- Inspected and saved the Bar-facing mural and practice table renders in
  `LiarsDicePreviews/exterior-mural.png` and `LiarsDicePreviews/practice.png`.
  Opened the dedicated scene in the Unity Editor, observed the playable table
  with its bidding controls, then pressed N and observed a fresh second hand.
  The test environment was left running for the owner. Updated README and the
  original REPLAYABILITY-README. Source changes only; no release build.


## 2026-11-23 — Spoken Bluff Dice replaces Liar's Dice

- Replaced the bid model and number/face grid with a random speaker's three
  secret dice, a private assigned spoken sentence and exactly two listener
  choices: Believe / Don't Believe. A correct judgment awards the listener
  the normal combat opening; an incorrect one awards the speaker. The reveal
  shows the speaker's actual three dice and explains truth and judgment.
- The speaker sees the sentence and whether it is true, with the goal of
  misleading the listener. Human listeners receive neither sentence, truth
  nor roll until reveal. The public table and spectators also receive no
  secret prompt or dice. A bot speaker uses a caption; bot listeners make
  blind guesses and do not consume hidden truth or understand microphone audio.
  Human speech uses existing room voice; it is not transcribed or enforced.
- Classic keeps Coin Flip on odd rounds and announces Bluff Dice on even
  rounds before voting. The cup-and-dice mural remains. A 0.8-second roll
  precedes eight seconds of speaking and six seconds for judgment. Early,
  wrong-actor, malformed, stale-round, stale-revision, old raise/call and duplicate
  inputs are rejected. An idle listener forfeits to the speaker. The existing
  three-second result pause, ten-second opening, action budget, pass/expiry,
  response, damage and medical opportunities remain unchanged.
- Renamed the test scene to BluffDiceTest.unity and updated its boot script,
  menu, artwork resource and generation source. Its instructions wait for Start.
  One local bot and one human alternate listener/speaker roles; the listener's
  judgment is untimed, reveal is held for review, Continue restores the normal
  combat pause/opening, and finished combat waits for the next-hand choice.
  N deals again with restored health, ammo and actions and a new round token.
  The scene remains excluded from release builds. Leaving clears practice state.
- Standalone production rules passed 52,595 checks: all 216 three-dice rolls
  against all 12 predicates, 5,000 seeded games, both judgments, every outcome,
  copied roll privacy, forfeits, true/false and speaker distribution, duplicate
  resolution and statement coverage. Live Unity / Metal passed 88 Bluff Dice
  checks plus 34 existing Blackjack flow checks, ending with
  SALOON_EDITOR_ITERATION PASS. The suite includes compressed private snapshots,
  both human roles and all four truth/judgment outcomes, physical hidden/revealed
  cups, authorization, idle forfeits, real bot judgments, actual opening gunfire,
  medical healing, announcement/rotation, spectator isolation and practice
  help/review/restart timing. Coin Flip passed 29 checks and medical events
  passed 93 checks, both ending with SALOON_EDITOR_ITERATION PASS. Production
  planning/rules passed 21,254 checks. No live multi-human voice call was tested.
- Inspected the speaker, human listener, bot-caption listener and review screens;
  saved current captures in BluffDicePreviews/. The UI uses opaque, readable
  panels, plain claims and only the two judgment buttons during listener play.
  Scoped whitespace, Unity metadata and scene build-exclusion checks passed.
  README and REPLAYABILITY-README document the replacement and its test flow.
  Protocol is state-35. Development source only; no release build.
- Opened the renamed test scene in Play mode, observed the paused instructions,
  clicked Start Practice and observed the bot-caption listener screen with just
  Believe / Don't Believe. Reset it to the paused instruction screen for the
  owner. The local scene log contains no new runtime exceptions.

### Bluff Dice throws and tabletop presentation — 2026-11-23

- Added a 2.4-second host-timed cup shake and animated throw. Three beveled,
  six-sided ivory dice tumble and bounce onto the baize, settling on the host's
  actual roll. Leather shakers have brass bindings; timed procedural clatter
  accompanies shaking and landings. Listener and spectator snapshots still
  contain no secret faces, and their cup stays closed until the judgment.
- Added a brass-edged rolling tray and oxblood playing surface. Lower claim and
  result panels leave the table visible; the header tracks Throw / Claim /
  Call / Reveal. The seated result camera stays on the physical cup lift and
  returns to normal combat framing afterwards. Existing bluff rules remain.
- Production rules passed 52,595 checks. Unity / Metal passed 104 Bluff Dice
  checks and the existing Blackjack flow regression, with
  `SALOON_EDITOR_ITERATION PASS`. Added checks for airborne dice, settling,
  all six authoritative face orientations, hidden throws, cup movement,
  reveal framing and table-surface alignment. Inspected final throw, settled
  and revealed renders. Previews and check output are in
  `BluffDicePreviews/ThrownDice/`. Development source only; no release build.

### Player-controlled Bluff Dice throws — 2026-11-23

- Added a waiting stage: the speaker clicks Throw Dice or presses Space to
  start the existing cup-and-dice throw. No human throw starts automatically.
  Claim and dice snapshots stay empty until the speaker throws; speaking and
  judgment timers begin relative to that input. The host accepts one throw
  from the current speaker and rejects duplicates, stale hands and outsiders.
- Practice starts with the human as speaker and waits indefinitely for their
  throw. Roles still alternate, and bot speakers throw on their own turns.
  Classic gives the speaker 15 seconds; an idle speaker forfeits to the
  listener without rolling or revealing an invented result.
- Production rules passed 52,595 checks; Unity passed 115 Bluff Dice checks
  and the existing Blackjack flow regression, ending with
  `SALOON_EDITOR_ITERATION PASS`. New checks cover waiting beyond the former
  automatic roll, the client throw control, timer start, unauthorized and
  repeated inputs, idle forfeits, bot throws and untimed practice.
  Captures and output are in `BluffDicePreviews/ManualThrow/`.


## 2026-11-23 — Escape actions and immediate victory

- Starting an escape preserves the carrier's normal action: shooting, healing,
  reloading or passing. Headshots still interrupt the escape and preserve the
  money for a later attempt. Interrupting escape no longer shortens a timed
  action already underway.
- Once every living Bar player has used an action and timed Bar actions finish,
  an uninterrupted escape goes straight to the existing Outlaw results screen.
  Side-room games do not delay a guaranteed escape. Unused Bar actions retain
  their response window, including the carrier's action after all defenders miss;
  the normal action deadline remains the fallback.
- Snapshots no longer count the escape itself as a busy action. Updated help,
  tutorial text and escape expectations in the existing verification fixtures.
- Production planning/rules passed 21,334 checks, including four-to-eight-player
  escape actions, timed actions, interruption, deadline fallback, side rooms and
  preservation of a queued elimination result. Unity 6000.5.10f1 / Metal compiled
  runtime and Editor assemblies and passed 57 drawer/escape checks with
  `SALOON_EDITOR_ITERATION PASS`. Live checks include actual defender misses,
  the carrier's final shot, healing before victory, interrupted healing, bot
  headshots and compressed result snapshots with the complete winner roster.
- Inspected the rendered victory screen. Evidence is saved under
  `EscapePreviews/EscapeVictoryAfterFinalAction.png` and
  `EscapePreviews/EscapeActionsVerification.log`. Source and Editor verification;
  no standalone build was made.


## 2026-11-23 — Shootable windows and bottles

- Added individual breakable targets for all six exterior window panes and all
  63 backbar bottles. Each valid bullet hit removes the intact target and its
  bullet collider, emits tumbling triangular glass fragments, and plays a
  positional glass crash. Bottle body, neck, label and stopper break together.
  Window frames and movement boundaries remain in place. Later bullets pass
  through the shattered pane.
- Bottles are excluded from static scenery batching. Saved scenes upgrade the
  interior and windows without duplicates; obsolete invisible bottle/bookcase
  colliders are disabled. The public Bar's invisible movement barrier now uses
  the existing movement-only layer, allowing shots above the physical counter
  to reach visible bottles.
- Destruction is host-authoritative and travels as stable IDs in every full
  snapshot, including reconnect snapshots. It persists between rounds and resets
  on a new match or leaving. New clients reconstruct the state without replaying
  old shatter effects; repeated snapshots do not duplicate effects. Protocol is
  state-36 so all participants use matching scenery and state.
- Unity 6000.5.10f1 / Metal compiled runtime and Editor assemblies. The new
  `Saloon → Verify breakable windows and bottles` suite passed 90 checks using
  four real UDP participants. Checks cover real remote gunfire, normal ammo/action
  spending, rejected shots, target isolation, open-pane raycasts, shards/audio,
  compressed snapshots, client reconstruction, reconnect, round persistence and
  rematch restoration. `SALOON_EDITOR_ITERATION PASS` completed the run.
- Production planning/rules passed 21,334 checks. Inspected the rendered window
  and bottle effects; before/after previews and check output are saved under
  `BreakablePreviews/`. Scoped whitespace checks passed. Development source only;
  no standalone build was created.


## 2026-11-23 — Local project and Mac cleanup

- Read-only Unity dependency audit protected 307 dependencies of the two retained
  scenes, runtime Resources loads and required Editor sources. Removed 112 unused
  assets with their metadata, including the recovery and imported demo scenes,
  unused character variants, legacy models, duplicate textures and stale generated
  meshes/materials. Saloon.unity and BluffDiceTest.unity retain their exact hashes.
- Removed obsolete/evaluated source downloads and duplicate archives; kept current
  exporter inputs, source citations and license records. The low-poly exporter and
  asset verification now omit retired models; checksum inventories match retained
  files.
- Removed all local Builds output, Unity Library/Temp caches, logs, generated IDE
  files, obsolete verification captures, LAST CALL temporary compiler/test output,
  Mac player logs and generated Editor analytics data. Unrelated projects, apps,
  user preferences and Git history were preserved. No installed LAST CALL app or
  release archive was found in the searched app, Desktop, Documents or Downloads
  locations. Unity regenerates its cache when the project is next opened.
- Post-removal verification: Unity compiled; all 307 protected dependencies exist;
  both scenes have no missing scripts; nine retained low-poly models validated;
  four-player UDP breakable scenery regression passed 90 checks and
  SALOON_EDITOR_ITERATION PASS. Production planning passed 21,334 checks and
  Bluff Dice rules passed 52,595 checks. Python tools parse and no orphan asset
  metadata remains. Generated caches were removed after these checks.
- Automatic approval review rejected scene-wide rebuilding/pruning. Continued with
  verified unused-file removal and preserved both retained scene files unchanged.
  Exact removed paths, sizes and verification summary are in CleanupManifest.json.
  Historical capture references above describe past verification; generated images
  and logs are intentionally no longer stored in this checkout.
