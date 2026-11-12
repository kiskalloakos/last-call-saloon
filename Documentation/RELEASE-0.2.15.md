# Last Call 0.2.15 — 2026-11-12

Windows x64 and universal macOS playtest release. All players must use 0.2.15.

- Added Graphics Settings to the main menu and Escape menu.
- Low, Medium and High quality presets; resolution and windowed/borderless mode.
- VSync and frame-rate limits, with Medium / VSync off / 60 FPS defaults.
- Local preference saving, Apply/Cancel/Defaults and a 15-second confirmation for display changes.

The 60 FPS setting is a cap, not a minimum-hardware certification. Human multiplayer,
real-device networking and hardware performance checks remain on the README checklist.
The Mac app is ad-hoc signed, not notarized. Voice requires internet, including on LAN.

Validation:

- macOS: packaged gameplay regressions passed.
- macOS: four independent no-bot players completed three LAN rounds and entered
  round four, covering the role deck, movement, paired coin/blackjack, headshots
  and healing.
- Universal Mac executable contains Intel x86_64 and Apple Silicon arm64 slices;
  deep ad-hoc signature verification passed.
- GitHub Mac download checksum matched the release checksum. The downloaded app
  was installed and launched showing 0.2.15 with Graphics Settings available.

- Windows Server 2022: [gameplay, five-player connections, rendered separate/paired
  rooms and paired voice connection passed](https://github.com/kiskalloakos/last-call-saloon/actions/runs/43239310730).
- Windows Server 2022: [four/five-player LAN, packet loss/delay and interruption
  recovery suite passed](https://github.com/kiskalloakos/last-call-saloon/actions/runs/43239318018).

These automated checks do not validate household routers, real microphone audio or minimum hardware.

Source revision: `131fe1a`. ZIP checksums are included in `SHA256SUMS-0.2.15.txt`.
OS display switching and timed rollback have not yet been exercised in a player.

