# Last Call 0.2.17 — 2026-11-14

Windows x64, universal macOS (Intel and Apple Silicon), and Linux x64 playtest release. All players must use **0.2.17**.

- Both Outlaws now share safe-work, money collection, and escape declaration progress on their character maps, including when they are in different areas.
- The money still has one carrier. Their partner gets support guidance, and cannot declare an escape without carrying the money.
- Cancelled escapes and lost money update both maps. Single-Outlaw matches continue to use personal progress.
- A 5.5-second cinematic introduction privately reveals your role on a black screen, then fades into the saloon. Movement and actions are blocked during the reveal; the opening discussion gets the additional time.
- Includes the current character story maps, saloon surface and lighting work, bartender, safe access, room planning, and gameplay fixes developed since 0.2.16.

Extract the complete archive. Open `Last Call.exe` on Windows, `Last Call.app` on macOS, or `LastCall-Linux/Last Call.x86_64` on Linux. Keep the data folders and libraries together.

Checksums are supplied in `SHA256SUMS-0.2.17.txt`.

Validation:

- Unity 6000.5.10f1 built all three platforms.
- Production planning, action, healing, and map checks passed 21,008 assertions, including shared money, shared escape, cancellation, loss, and single-Outlaw progress.
- Unity story-map regression passed 32 checks, including both Outlaws' private snapshots, team-only visibility, and compressed network serialization.
- The published Mac download passed SHA256 verification and matches the installed app file for file; deep signatures and normal 0.2.17 startup passed.
- The final packaged Mac gameplay suite passed, including heist, beer, blackjack, social, practice, combat feedback, and round completion.
- Five independent Mac players completed three no-bot LAN rounds and entered round four, including movement, paired coin/blackjack, headshots, and healing.

The Mac app is ad-hoc signed. Physical microphone/speaker listening and household network behavior require manual testing.

Native published-package validation:

- [Linux voice, gameplay, five-player, and LAN checks passed](https://github.com/kiskalloakos/last-call-saloon/actions/runs/43587759354).
- [Windows voice controls, gameplay, and five-player checks passed](https://github.com/kiskalloakos/last-call-saloon/actions/runs/43587756507). That run's three legacy rendered-room cases failed because the fixture toggles the already-open discussion planner closed before checking that it is open. Current room behavior is also covered by the separate multi-round LAN suite.
- [Windows four/five-player LAN, impaired connections, and interruption recovery passed](https://github.com/kiskalloakos/last-call-saloon/actions/runs/43588065431).
