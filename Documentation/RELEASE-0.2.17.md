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
- The final packaged Mac gameplay suite passed, including heist, beer, blackjack, social, practice, combat feedback, and round completion.
- Five independent Mac players completed three no-bot LAN rounds and entered round four, including movement, paired coin/blackjack, headshots, and healing.

The Mac app is ad-hoc signed. Physical microphone/speaker listening and household network behavior require manual testing.
