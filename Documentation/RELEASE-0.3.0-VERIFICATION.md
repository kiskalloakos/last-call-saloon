# Last Call 0.3.0 release verification

The Windows x64 and universal macOS players were built from commit
`2064994a3e0fbb2231b0e6061da4c63671a431e8` with Unity 6000.5.10f1.
The version is 0.3.0, build 300, protocol `state-36`.

## Release checks

- Planning, concurrent Bar actions and escape timing: **21,334 checks passed**.
- Bluff Dice rules: **52,595 checks passed**.
- Native macOS release features: **437 checks passed** (drawer/escape 57,
  medical 123, toilet mission 121, toilet audio 21, Bluff Dice 115), plus Blackjack.
- Native macOS breakables: **91 checks passed**, including actual UDP shots,
  broken-prop snapshots, reconnects, persistence and new-match reset.
- Native macOS voice controls passed.
- Five independent Mac players passed movement, three rounds of paired games,
  headshots and beer healing, and reached round four beyond the former cap.
- Four independent Mac players completed the same match with 80 ms one-way
  latency, 25 ms jitter and 2% configured packet loss on all three remote links.
- Native Windows Server 2022 release features, breakables, voice controls and
  five-player startup passed against the final public ZIP:
  [Windows release checks](https://github.com/kiskalloakos/last-call-saloon/actions/runs/45262936567).
- Final Windows four-player and five-player complete matches and the
  four-player delayed/lossy match passed against the same public ZIP.
  [Windows match/network results](https://github.com/kiskalloakos/last-call-saloon/actions/runs/45262942865).
- The additional 13-second outage scenario recovered the connection and all four
  players passed rounds one and two, including paired headshots. Three players
  healed in round three; the fourth remained standing at `(-2.60, 0.08, 0.67)`
  without reaching the tray before the round ended. The overall outage scenario
  therefore **failed** its healing assertion. This appears to be another crowded
  navigation limitation in the automated fixture; the full outage suite is not
  claimed as passing. It remains an explicit playtest limitation.

## Published archives

Both ZIPs contain the platform instructions, complete changelog and asset credits.
Archive CRC checks pass; Windows is PE x64. The Mac app contains both x86_64 and
arm64, has version 0.3.0/build 300, and passes strict ad-hoc signature verification.
The app is not Apple-notarized.

The final Mac ZIP was downloaded from the public GitHub release: SHA256, archive
CRC, executable permissions and strict code signature match the local package.
Windows CI verifies the public download against the uploaded checksum file before
executing the game.

| Archive | SHA256 |
| --- | --- |
| LastCall-Windows-0.3.0.zip | `1b87438bae4e615fe60751d5f8e12735585a44e36e6ea58282a4a0126c4bd784` |
| LastCall-macOS-0.3.0.zip | `263e5d52a618330254fbfdb3a43969534a915bd35738a16239376c2c81127d47` |

## Test scope

The opt-in `release` suite exercises current release features, breakables, voice
controls and five-player startup. The `lan` suite executes complete four- and
five-player matches, delayed/lossy links and a 13-second connection interruption.
The retained legacy `standard` suite contains an obsolete all-world text-label/
room-stencil assertion and was not used as the release gate.

Two automated fixtures were corrected during release verification: precise bottle
shots now charge instead of using random quick-fire spread, and beer-seeking test
players use a stool gap and the public aisle instead of a straight diagonal.
Neither change alters normal gameplay. Physical mixed-device voice, firewalls and
social balance still benefit from a real group playtest.
