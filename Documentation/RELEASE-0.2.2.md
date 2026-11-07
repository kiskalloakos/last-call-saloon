# Last Call 0.2.2 — LAN room-selection repair

Replace the game on **both laptops**; mixed versions are now rejected. The main menu shows
the version. Extract the full Windows ZIP before running `Last Call.exe`.

- Cursor lock/visibility is applied only when it changes, instead of on every snapshot.
- Entering room selection closes the pause menu and shows the chooser automatically.
- Pending companion invitations no longer discard a room choice: you go alone when space
  remains. Confirmed pairs retain priority and stay together; full rooms explicitly say why
  you were assigned to Bar.
- Snapshots reduced from 20 to 10 per second to reduce reliable fragmented traffic.
- Disconnect reasons survive returning to the menu; phase transitions are logged.
- New rendered LAN regression uses the real 30-second discussion with live positional voice
  and muted microphones, checks that both room pickers render, sends choices through the
  network, confirms host acknowledgement and verifies each player's assigned room.

The rendered Mac test passed for both host and joining client: room pickers rendered 1,808
times each, voice remained connected, host reached East and client reached West with unmatched
invitations. This reproduces the timing/path, but the original Windows stall has **not** been
reproduced here and Windows still needs the user's retest. No claim of a confirmed Windows
native crash fix is made.

Full rules: [README](../README.md). Existing coin flips, ordered bar turns, hidden robbery,
healing and three-second aftermath remain included.
