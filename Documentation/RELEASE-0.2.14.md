# Last Call 0.2.14 — 2026-11-11

Matching Windows x64 and universal macOS builds for four or five players. For four people, choose LOCAL / LAN and NO BOTS, then start once all four people are in the lobby. Default port: UDP 7777.

Changes include independent head motion and nodding, tighter voice/mouth synchronization, room window rendering in packaged games, persistent bodies while spectating, right-click planning, clearer invitations and practice bot reservations, smaller Sheriff text, simplified health and six-slot ammo displays, slower blackjack with no table totals, cleaner coin-flip controls, crouching, weapon positioning, and the sunset overlook.

The Mac app is ad-hoc signed and is not notarized. Vivox voice needs an internet connection even when gameplay uses LAN.

Packaged validation:

- Mac: four independent players, no bots, using the LAN interface; three completed rounds and round four started. Room assignment, coin flip, blackjack, movement, shooting, healing, role privacy and the exact four-player role deck passed.
- Mac: gameplay regressions passed.
- Mac: Metal rendering checks passed for independent head motion, mouth timing and visibility through both room windows from the bar.
- Universal Intel/Apple Silicon Mac app: deep signature verification passed after ad-hoc signing. Windows executable: x64.
- Windows Server 2022: [gameplay, five-player connections, rendered room planning, paired coin play and Vivox connection checks all passed](https://github.com/kiskalloakos/last-call-saloon/actions/runs/43062776306).
- Windows Server 2022: [four-player and five-player no-bot matches, four-player packet loss/delay and a 13-second interruption all passed](https://github.com/kiskalloakos/last-call-saloon/actions/runs/43062772236). These tests completed three full rounds including paired coin flip/blackjack, shots and beer healing.

Source revision: `d7e983f`. ZIP checksums are included in `SHA256SUMS-0.2.14.txt`.

Actual router and client firewall access must be allowed on each player's device; these automated checks do not test your friends' computers or router.
