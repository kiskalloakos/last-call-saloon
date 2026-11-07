# Enable online rooms and proximity voice

This project contains real Unity Multiplayer Services and Vivox calls, but **no cloud
project has been created or linked**. LAN does not need one; Relay rooms and all voice do.
No live Relay or Vivox session can be verified until this setup is complete.

1. Open this separate project in Unity 6000.5.10f1 and sign in to your Unity account.
2. In **Edit → Project Settings → Services**, create or link a Unity cloud project for this prototype.
3. In that project's Unity Gaming Services dashboard, enable **Authentication** with anonymous sign-in,
   **Multiplayer / Lobby and Relay**, and **Vivox**. Complete any service activation or account prompts shown by Unity.
4. Return to Unity and make a new build so it contains the linked project ID.

The code uses UGS anonymous authentication and SDK integration; credentials are not embedded in source.
Every app process uses a separate authentication profile to permit editor/build testing on one machine.

## Internet playtest

1. The host chooses **Online room → Create room**, then shares the displayed room code.
2. Four friends choose **Online room → Join code** with that code.
3. Everyone opens **Esc → Connect voice**, accepts microphone access, and uses **V** to talk.
4. Test voice up close, across the room, and from the porch before starting a match.
5. Verify that an eliminated player can speak only to other eliminated players; a rematch returns them to the living channel.

The host's game process must remain running. A host exit ends the playtest.
This prototype does not automatically create paid resources or enable billing.

## LAN with voice

The players can connect using IP addresses while still using Vivox for voice, after linking
the same cloud project and rebuilding. A random voice-room identifier is shared by the host
through the game connection, so these players enter the same voice channel.
LAN without a linked cloud project has gameplay but no built-in voice.

## Microphone and mute

- The lobby has an eight-second **Microphone echo test**; use headphones.
- Each participant can be muted locally from the voice list.
- **M** toggles your own microphone mute; speaking additionally requires holding **V**.
- The macOS build includes `NSMicrophoneUsageDescription` and requests microphone permission.
- Distance attenuation is configured for full conversation around two metres and silence beyond ten metres.
- Walls do not block the sound in this first integration.

## Relevant official documentation

- [Sessions with Netcode for GameObjects](https://docs.unity.com/en-us/mps-sdk/tutorials/build-your-first-session)
- [Vivox positional channels](https://docs.unity.com/en-us/vivox-unity/developer-guide/channels/positional-channels)
- [Vivox positional channel configuration](https://docs.unity.com/en-us/vivox-unity/developer-guide/channels/positional-channel-configuration)

Package versions are pinned in `Packages/manifest.json`; the resolved dependencies are in `Packages/packages-lock.json`.
