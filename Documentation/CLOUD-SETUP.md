# Configured Unity services

Configured on 2026-11-07 for **Last Call Saloon** in the existing Unity organization.

- Project ID: `df9e82b7-ec21-4418-bf3e-8e50cdecf800`
- Vivox Voice and Text Chat: enabled
- Relay: enabled
- Lobby: enabled
- Authentication: anonymous player sign-in
- Optional Safe Text, recording, and transcription: not enabled

The builds contain public Vivox connection identifiers and use Unity Authentication
for access tokens. **No Vivox signing key is included in code or builds.**
The editor setup binds this separate project to its cloud project before building.

## Playing

No Unity account is needed by players. Join a LAN or online room as usual; proximity
voice connects automatically once the player and room are ready. Allow microphone
access, then hold V to speak. M toggles your microphone mute.

An internet connection is required for Vivox, including when gameplay is on LAN.
For internet gameplay, choose Online room, create a room, and give its room code to friends.
The host must keep the game running.

## Microphone controls

Open Esc for microphone selection, an eight-second local echo test, and per-player mute.
The macOS build includes the microphone usage description and requests OS permission.
Windows microphone access may also be governed by its privacy settings.

The echo test transmits only to your unique echo channel; other players cannot hear it.
Push-to-talk uses microphone muting with transmission restricted to the current voice channel.
The microphone is muted before joining or switching channels.

## Spatial and life-state behavior

- Conversational distance: 2 metres
- Audible distance: 10 metres
- Linear distance attenuation and directional panning
- Position/orientation updates: 10 times per second
- Eliminated players: separate non-positional channel
- Rematches: living players return to the positional channel
- Wall occlusion: not implemented in this prototype

Channel isolation is enforced by trusted game clients. A production release would need
server-issued channel permissions rather than relying on clients to choose correctly.

## Configuration for forks

To use your own backend, create your own Unity project and activate Vivox, Relay, and Lobby.
Update the project binding in `Assets/Editor/SaloonProject.cs` and the public Vivox routing
identifiers in `Assets/Scripts/SaloonConnection.cs`. Keep any signing key out of the client;
use UAS authentication or a server-side token provider.

## Official references

- [Sessions with Netcode for GameObjects](https://docs.unity.com/en-us/mps-sdk/tutorials/build-your-first-session)
- [Vivox positional channels](https://docs.unity.com/en-us/vivox-unity/developer-guide/channels/positional-channels)
- [Vivox positional channel configuration](https://docs.unity.com/en-us/vivox-unity/developer-guide/channels/positional-channel-configuration)
- [Microphone mute and transmission modes](https://docs.unity.com/en-us/vivox-unity/developer-guide/muting/mute-user-microphone)
