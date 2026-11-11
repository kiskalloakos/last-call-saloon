# Last Call 0.2.12

Released 2026-11-11. All players must use this version.

- Discussion uses ordinary mouse look; P opens/closes the room planner. Ready returns to looking around.
- Beer pours into the actual countertop mug. Hold the tap handle, scroll to adjust flow, release to stop. Drink it or gift its exact healing amount.
- Healing rounds up in 5-point steps, to a maximum of 20 per beer. Non-lethal hits do not interrupt drinking.
- Includes percentage health, cigarette damage buff, recorded audio and rematch/reconnect improvements from current development source.

## Downloads

- [Windows x64](https://github.com/kiskalloakos/last-call-saloon/releases/download/v0.2.12/LastCall-Windows-0.2.12.zip)
- [macOS: Intel and Apple Silicon](https://github.com/kiskalloakos/last-call-saloon/releases/download/v0.2.12/LastCall-macOS-0.2.12.zip)
- [Checksums](https://github.com/kiskalloakos/last-call-saloon/releases/download/v0.2.12/SHA256SUMS-0.2.12.txt)

Extract the complete archive. Windows requires the data folder beside Last Call.exe. The Mac app is not notarized.

## Validation

- Unity 6000.5.10f1 built Windows x64 and universal macOS (Intel/Apple Silicon) successfully.
- Final packaged Mac gameplay and five-player networking/privacy checks passed.
- The published Windows ZIP passed checksum verification, native gameplay and five-player networking on Windows Server 2022.
- The separate-room, paired-room and paired-room-with-voice Windows checks timed out in discussion/planning. Their validation remains incomplete; the full Windows suite therefore failed. This timeout also occurred in 0.2.11. [Windows results and logs](https://github.com/kiskalloakos/last-call-saloon/actions/runs/43029960758).
- Both ZIPs passed integrity checks. GitHub's asset digests match the final local files.
- Game binaries match source commit `56780fbd83ed14430d45a520c112edc5a7d9c538` (tag `v0.2.12`). The automated privacy fixture now respects the Renegade's intentionally revealed role knowledge.
- The previous local 0.2.11 build folder and its archives were moved to macOS Trash. Current build outputs, logs and checksums are in `Builds/0.2.12/`.
