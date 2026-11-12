# Last Call 0.2.16 — 2026-11-12

Windows x64, universal macOS (Intel and Apple Silicon), and Linux x64 playtest release. All players must use **0.2.16**.

- Added **open mic**: press **T** once to speak hands-free; press T again to return to push-to-talk. The Escape menu has the same switch.
- Kept **hold V** for push-to-talk and **M** for mute in either mode.
- Added a small microphone indicator showing open mic, push-to-talk, mute, connection and permission states.
- Menus pause microphone transmission. Closing the menu resumes the selected mode. Open mic stays selected across room/channel changes and resets when you leave the session.
- Confirmed the private cigarette area still has its own voice channel. Smokers cannot hear indoor conversation or gunshots; indoor players cannot hear smokers. Audio is muted in both directions while changing channels. Regroup restores indoor proximity chat; eliminated players retain their separate channel.
- Added a **Linux x64 download** and a native Linux release-test workflow.

Indoor proximity voice has full volume within 2 metres and fades to silence at 10 metres. Walls do not attenuate indoor voice. Voice needs internet, including during LAN play.

Downloads: extract the full archive. Windows: open `Last Call.exe`; macOS: open `Last Call.app`; Linux: open `LastCall-Linux/Last Call.x86_64`. Keep the accompanying data and libraries together.

The Mac app is ad-hoc signed, not notarized. Physical microphone/speaker listening, household routers and minimum hardware are not certified by these automated checks.

Validation:

- Unity 6000.5.10f1 compiled matching Windows, universal Mac and Linux players.
- Packaged Mac gameplay regression passed.
- Packaged Mac voice regression passed: push-to-talk, open-mic intent, mute/menu overrides, per-phase privacy rules, real Vivox transitions through indoor/outdoor/indoor/eliminated channels, one active channel at a time, transition muting and open-mic reset on leaving. This test keeps the physical microphone muted.
- Four independent packaged Mac players completed three no-bot LAN rounds and entered round four, covering exact roles, movement, paired coin/blackjack, headshots and healing.
- Two live Mac Vivox sessions transmitted synthesized speech (remote peak energy 0.5469); speech and mouth activity returned to zero after audio stopped while the sender stayed connected. Physical microphones remained muted.
- Universal Mac executable contains x86_64 and arm64 slices; deep signature verification passed.

Archive checksums are supplied in `SHA256SUMS-0.2.16.txt`.
