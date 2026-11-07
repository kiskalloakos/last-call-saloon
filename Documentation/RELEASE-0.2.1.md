# Last Call 0.2.1 — ordered turns and heist feedback

Use **0.2.1 on both laptops**. Extract the entire Windows ZIP, then run `Last Call.exe`.
Host LAN on one laptop and join using its local IP and port 7777 from the other.
With two humans, choose **Start Practice** to fill three seats with active bots.
Proximity voice requires internet; use headphones on nearby laptops.

- Discussion invitations identify who wants to talk and in which room; P opens planning.
- Two-person rooms use a Heads/Tails coin flip to decide the opening action.
- Groups of three or more use a random turn queue with an active-player timer; everyone,
  including Sheriff and practice bots, obeys the same one-action rule.
- H works the safe discreetly; C lets the Sheriff inspect its state privately using an action.
- Corrected first/third-person drinking, gunshot spin, muzzle bursts and hit/camera feedback.
- Three-second aftermath preserves final-shot/death animations before regroup or results.
- Dead players retain their seat and identity. Only the Sheriff is publicly identified.

Editor gameplay checks passed before packaging. Both builds succeeded; two packaged Mac
processes passed LAN joining, privacy and synchronized match completion with three bots.
Windows x64 is built but physical Windows gameplay/microphones still need the laptop test.
Both archives passed CRC checks; SHA-256 checksums and asset attribution are included.

Full rules and next priorities are in [README.md](../README.md).
