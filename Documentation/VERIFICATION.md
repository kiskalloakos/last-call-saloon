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
