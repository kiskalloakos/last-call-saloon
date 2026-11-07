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
