# Last Call 0.2.11 — saloon minigames and gameplay update

Released 2026-11-10. All players should use 0.2.11.

- Coin Flip and Blackjack tables, with private outcomes and table interactions.
- Room reservations, signs and windows, plus a smoking area.
- Posse arrival and updated round completion and pacing.
- Private Sheriff clues and role guidance on the Tab card.
- Compact HUD, updated combat feedback, revolver audio, footsteps and character poses.
- Includes the latest README instructions and gameplay changes from main.

## Validation

- Unity rule, round-clock, snapshot-wire and room-reservation checks passed.
- Windows x64 and universal macOS builds succeeded with Unity 6000.5.10f1.
- Packaged Mac gameplay and five-player networking checks passed, including minigames, combat, robbery, beer, privacy and round completion.
- The published Windows ZIP passed download/checksum verification, gameplay checks and five-player networking on Windows Server 2022.
- The separate-room, paired-room and paired-room-with-voice checks timed out during planning. The full Windows suite failed, so room-flow and voice validation remain incomplete. [Windows test logs](https://github.com/kiskalloakos/last-call-saloon/actions/runs/42899932020).
- Player archives were built from gameplay source in commit `22656df338b68b731f285a1dd6eb4cdfce4a234e`; subsequent documentation records the test results.
- Both ZIP archives passed integrity checks; SHA-256 checksums are included.
- Fixed editor-only HUD API calls that prevented standalone builds and updated the room-label check for the new minigame signs.

## Downloads

- [Windows x64](https://github.com/kiskalloakos/last-call-saloon/releases/download/v0.2.11/LastCall-Windows-0.2.11.zip)
- [macOS — Intel and Apple Silicon](https://github.com/kiskalloakos/last-call-saloon/releases/download/v0.2.11/LastCall-macOS-0.2.11.zip)
- [SHA-256 checksums](https://github.com/kiskalloakos/last-call-saloon/releases/download/v0.2.11/SHA256SUMS-0.2.11.txt)

Extract the complete ZIP. On Windows, run `Last Call.exe` with its data folder beside it. On macOS, open `Last Call.app`. The Mac app is not notarized; macOS may require approval through its system Open controls.

This remains a private-group test build. Router/firewall behavior and internet-dependent voice still need testing on the players' devices.
