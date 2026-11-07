# Last Call 0.2.0 — seated heist alpha

Download the Windows ZIP on the Windows laptop and the Mac ZIP on the Mac.
Use **0.2.0 on both devices**, or the matching Unity project; old 0.1.2 builds have different rules.

## Two-laptop LAN test

1. Extract the **entire** ZIP; on Windows run `Last Call.exe` inside the extracted folder.
2. Host LAN on one laptop; join from the other using the host's local IP and port 7777.
3. With two humans, the host chooses **Start Practice** to fill three places with active bots.
4. Use headphones; game traffic works on LAN, but Vivox voice needs internet.

## Changes

- Seated discussion → choose rooms/companions → shared one-action window → regroup.
- Private robbery at the bar, witnessed only by roommates; shared three-step safe and delayed escape.
- Practice opponents shoot, heal, rob and pursue faction victory with perfect role knowledge.
- Only the Sheriff is public; Outlaws no longer receive a fellow-Outlaw name.
- **E prepares beer for free**, one pickup per round before spending your action.
  **B drinks/heals** as your action; **G gives** it to a nearby player who automatically drinks.
- First/third-person drinking, travelling handoff mug and gulp/clink sounds.
- Layered revolver gunshot and cosmetic post-shot spin in first and third person.
- Eliminated players remain at their death seat with names and empty HP dots; seats are not reused.
- Small, muted private hints follow your role, phase and action availability.
- Low-poly furniture/models, corrected seating and mug props.

## Outlaw robbery

Choose **Bar**, then **H** during room actions: work for three seconds, using your action.
Three successful work actions across players/rounds open the safe; the third worker gets the money.
Getting shot interrupts work. On a later action, press **X** at the bar to declare escape.
Choose **Bar again** and survive through the **end of the next room-action phase** with the bag.
Either Outlaw escaping wins for both, although neither knows the other's identity beforehand.
No escape declarations during the final round. Sheriff death alone is not a victory.

## Validation

Editor gameplay, robbery/privacy/escape, beer, seats and firing checks passed; both platform
builds succeeded. Two packaged Mac processes passed LAN joining, privacy and synchronized results.
The Windows executable has not been run on Windows here; this release is ready for that laptop test.

Full rules: [README](../README.md). Art attribution is included inside both downloads.
