# Last Call — saloon prototype

A separate Unity **6000.5.10f1** project for a five-player, first-person social shooter.
This prototype does not modify the parent project's assets, scripts, scenes, or settings.

## Two laptops on the same Wi-Fi

Download the matching Windows or Mac ZIP from [Releases](https://github.com/kiskalloakos/last-call-saloon/releases/latest).
Extract the whole archive; Unity does not need to be installed to run the builds.

1. On the host, choose **Local / LAN → Host LAN** with port `7777`.
2. On the other laptop, choose **Join LAN**, entering the host's local IPv4 address and port `7777`.
3. Once both players appear in the lobby, the host presses **Esc → Start practice / fill empty seats**.
4. Three inert targets fill the empty seats, letting two humans test movement, shooting, seating, and beer.
5. For the actual hidden-role game, gather five humans and use **Deal roles & start match** instead.

Both devices must be on the same local network; guest-network client isolation can prevent direct connections.
If the operating system asks, allow the game to receive connections on the private/home network.
Cloud services, port forwarding, and internet hosting are not required for LAN gameplay.
Built-in voice still requires Vivox; for two laptops next to each other, talk in person for this test.

## Play

- Open `Builds/macOS/Last Call.app` for the built macOS version.
- On Windows, extract the Windows ZIP completely and run `Last Call.exe` beside its included data folder and DLLs.
- Or add **this SaloonPrototype folder** to Unity Hub, open `Assets/Scenes/Saloon.unity`, and press Play.
- **Solo Practice** adds four inert targets and deals roles, so you can explore and check interactions; these targets do not simulate social play.
- **Host LAN** starts a five-player lobby. Friends on the same network choose **Join LAN** and enter the host's local IP address and matching UDP port (default `7777`).
- On one computer, use the editor plus separate builds and `127.0.0.1`, with distinct player names.
- Once five people have joined, the host presses **Esc → Deal roles & start match**.
- For friends on different networks, use the prepared online-room integration after following `Documentation/CLOUD-SETUP.md`.

LAN gameplay works without a Unity cloud project. **Voice requires Vivox configuration, including on LAN**; the voice integration cannot connect until the project is linked and its services are enabled.

## Controls

| Input | Action |
|---|---|
| WASD / mouse | Move / look |
| Q | Draw / holster revolver |
| Left click | Fire |
| Right mouse | Narrow aiming field of view |
| R | Reload |
| E | Sit / stand, or take a beer when looking at the brass tap |
| B | Drink carried beer when injured |
| G | Give carried beer to the player you are looking at |
| Hold Tab | Privately read your role and objective |
| Hold V | Push-to-talk after connecting voice |
| M | Toggle microphone mute |
| Esc | Lobby, tuning, voice controls, leave room |

Look at an actual chair or stool to sit; look down at its seat if necessary.
The supply of beer is the brass tap at the right end of the bar.

## First-test rules

Everyone has three health points; each shot deals one damage with no headshot multiplier.
The revolver has six rounds and unlimited reserve ammunition; reload takes 3.5 seconds.
Drawing takes 0.6 seconds and shots are limited to one every 0.65 seconds.
The first 30 seconds lock weapons, then the match runs continuously for eight minutes.

Eight shared beers are available. A player can carry one. Drinking takes 2.5 seconds,
holsters the weapon, and restores one health point. Taking damage interrupts drinking
or reloading; a started beer is consumed even if interrupted. Busy actions slow movement.
You cannot fire while drawing, reloading, or drinking.

| Role | Knowledge | Victory |
|---|---|---|
| Sheriff | Publicly identified | Both Outlaws and the Renegade eliminated |
| Deputy | Knows the public Sheriff | Wins with the Sheriff, including after the Deputy dies |
| Two Outlaws | Know each other's names | Sheriff eliminated, except a sole-surviving Renegade wins first |
| Renegade | Knows only the public Sheriff | Be the only survivor; Sheriff must die last |

Roles are revealed to everyone only when the match ends. Timeout is a draw.
A mid-match disconnect aborts the match rather than silently changing the role balance.
The host can tune opening duration, match limit, and beer supply between matches.
Other parameters are in the scene's **Saloon Prototype → Tuning** component.

## Build and verification

- **Saloon → Set up saloon scene** regenerates this prototype's map; it overwrites its scene layout.
- **Saloon → Verify rules and asset setup** checks victory precedence and imported assets.
- **Saloon → Build macOS playtest** regenerates, verifies, and builds the macOS app.
- **Saloon → Build Windows playtest** needs the matching Windows Build Support module installed through Unity Hub.
- `python3 Tools/smoke_test.py` launches five real app processes and checks their replicated match outcomes and private role views.
- `python3 Tools/smoke_test.py --practice` verifies two real app processes plus three inert practice targets.
- Do not rebuild the app while smoke-test processes are using it.

## Scope and next playtest

One saloon with a porch and back room, first-person controls, host-validated movement,
hitscan damage, seating, beer sharing/healing, role assignment, match results, and rematches.
Character poses, revolver, recoil, and generated gunshot audio are placeholders.
The gun is an original simple mesh assembly; the imported furniture, character, and barrel sources are recorded in `Documentation/ASSET-SOURCES.md`.

This version uses host-authoritative snapshots without client prediction or lag compensation.
It targets trusted private playtests; there is no host migration, dedicated server,
matchmaking, progression, production anti-cheat, or reconnect-in-progress support.
Vivox attenuation follows distance, not wall occlusion; use separated corners/porch for private conversations.
Dead players switch to a separate voice channel. Trusted clients enforce that switch;
production channel access would need server-issued authorization.

The first social playtest should record why each player trusted or suspected someone,
how long it took to fire the first shot, and whether beer sharing and covering reloads mattered.
Change one constraint between matches rather than adding new systems immediately.
