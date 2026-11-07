# Last Call 0.2.3 — joining-player room panels

Replace the game on **both computers** with 0.2.3. Extract the entire Windows ZIP before
running `Last Call.exe`; the other files in that folder are required.

Fixed a confirmed host/client serialization difference: Unity's JSON serializer turns null
inline coin-toss and room-turn objects into empty objects. The old client treated those as
real panels, showing “Room actions complete”, “Action spent” and “hear their response” even
during discussion and room planning. The host bypassed JSON and did not show the bug.

- Snapshots carry explicit presence flags and restore absent panels to null on receipt.
- The host now passes its own view through the same JSON path as joining players.
- Coin and turn cards only render during room actions; coin pointer handling is also phase-gated.
- The network protocol/version check rejects older clients rather than mixing snapshot formats.
- Regression coverage includes every phase, live coin/queue payloads, and rendered two-process
  discussion/planner → room assignment → coin/action → aftermath with live voice connected.

Validation: snapshot/clock checks and the full built-game gameplay suite passed. Both
rendered LAN players selected separate rooms and spent actions; a second run confirmed
mutual invitations, coin choices, both players' actions, and connected Vivox voice with muted
microphones. Five-player smoke results are recorded in VERIFICATION.md.

Windows x64 and universal macOS (Intel + Apple Silicon) builds were produced from the same
source. Windows was cross-built on macOS; native Windows execution and the reported kick-out
on the other computer still require a device retest. No separate native crash was reproduced.
