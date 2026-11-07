# Last Call 0.2.4 — bounded state updates for slow LAN clients

Update **both computers** to 0.2.4. Extract the whole Windows ZIP. Older clients use a
different network format and are rejected during connection approval.

The real LAN report showed the host advancing while the joining player's room countdown
stopped, followed by a disconnect. 0.2.3 sent every complete JSON snapshot through a
reliable fragmented queue. Under a constrained connection that can replay obsolete state
instead of delivering the current room phase. The earlier packet-loss-only tests did not
cover that bandwidth constraint.

- State snapshots are compressed UTF-8 and sent as bounded, independently replaceable
  datagrams. Missing old updates cannot block a newer complete state; late and duplicate
  packets cannot roll the game backward. Player action commands remain reliable.
- A measured five-player state shrank from 6,478 bytes to 787 bytes before packet headers.
- Increased the transport packet queue from 128 to 1,024 to tolerate reception bursts.
- The HUD explicitly reports when current host state has stopped arriving.
- Both host and client now log the transport disconnect reason before cleanup, plus build
  version, state age/sequence and byte counters. Long match-abort messages fit the overlay.
- Added loss/reordering/duplicate/bounds tests and an 8,000-byte/s LAN regression, with
  packet loss and a five-second outage during room selection.

Validation: both builds succeeded. Native Windows functional, room/coin/voice-connected,
and bandwidth-limited LAN checks passed; all repeated five-player matches completed.
One Windows CI host hung during intentional post-match shutdown, while two repeated runs
exited cleanly; that shutdown issue remains open. The Mac functional, five-player and
capped-link voice tests passed. The specific laptop/Wi-Fi path still needs its own retest.

[Windows bandwidth-cap results](https://github.com/kiskalloakos/last-call-saloon/actions/runs/42405254189)
· [Full validation and known shutdown limitation](https://github.com/kiskalloakos/last-call-saloon/blob/main/Documentation/VERIFICATION.md).

The precise host-side reason for the earlier laptop disconnect cannot be proven
retrospectively because 0.2.3 did not log remote-client transport reasons. 0.2.4 preserves
that evidence if a problem remains.
