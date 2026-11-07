# Low-poly saloon art direction

Research and proposed workflow, 2026-11-07. No art replacements or builds made yet.

## Target

Simple silhouettes, coarse painted textures, weathered wood, muted cloth, aged brass,
warm lantern light and cooler light at doors/windows. Retain clear faces, visible hands,
and readable interactions: these are important evidence in the social game.
Valheim's own FAQ describes low-resolution textures and sparse polygon detail combined
with modern materials, lighting and post effects:
https://www.valheimgame.com/faq/

Proposed starting texture density: 64 pixels per metre, checked at the actual first-person
camera distance. Use a small shared palette; try a 128–256 px prop atlas, increasing only
where close-up readability demands it. These are project experiments, not Valheim specs.
Keep movement/camera/animation responsive; retro artwork does not require low frame rates,
input delay, camera jitter or vertex wobble. Preserve smooth frame pacing on both test laptops.

## Free asset shortlist

These are researched candidates, not newly imported assets. Verify archive contents and
included license when importing; keep provenance with the files.

| Asset | Proposed use | License on creator page | Source |
|---|---|---|---|
| Kenney Furniture Kit | Reuse existing tables, chairs, stools and shelving; unify materials | CC0 | https://kenney.nl/assets/furniture-kit |
| Donnee Low-Poly Tavern Asset Pack | Candidate textured bar, door, window, tableware; page lists FBX/PNG and Blender sources | CC0 | https://donnee.itch.io/3d-tavern-pack |
| Poly Haven Wood Planks | Starting material/reference for simplified wood texture, not full-detail treatment everywhere | CC0 | https://polyhaven.com/a/wood_planks |
| Quaternius Ultimate Modular Men Pack | Optional animated character base if existing Kenney silhouettes prove unsuitable; requires style/clothing/rig evaluation | CC0 | https://quaternius.com/packs/ultimatemodularcharacters.html |

Poly Haven license: https://polyhaven.com/license
Donnee ships 2048 px textures; a low-poly mesh alone does not establish the desired coarse
texture style. Test reduced/repainted textures on one object before adopting the pack.
The existing Poly Haven barrel is more realistic than the Kenney furniture; simplify its
material or replace it once the target look is established.
Do not mix full packs indiscriminately. Existing character animation should stay working
until a replacement is proven. No hats, consistent with the established direction.

## Blender work, in order

1. Beer mug: first joint exercise. About 150–400 triangles as an initial budget, 8–12 sides,
   chunky handle, separate foam surface, dark wood/tin body. Suggested size 0.12 m diameter
   by 0.16 m tall, with handle extending outside it. Check proportions against hands in Unity.
   Separate liquid/foam if needed for later animation; keep a useful grip reference/pivot.
2. Till/safe: clear closed/open state with drawer or door as a separate object, pivot at the
   correct movement axis. Roughly 400–900 triangles initially. No robbery logic added by this art task.
3. Modular room pieces: wall, doorway, window, floor and beam sections on a 1 m grid. Build
   these around the room prototype's sightlines and collision, once its layout is agreed.
4. First-person hands and revolver: prioritize recognizable draw, recoil and reload poses.
   Separate trigger, cylinder and hammer if animated. Retain aim alignment while changing visuals.
5. Character clothing/hair variations on the chosen existing rig. Keep allegiance visually
   secret except for the public Sheriff marker; do not model five role-coded outfits.

Triangle counts are suggested starting budgets, not strict limits. Silhouette, material
consistency and animation readability matter more than minimizing every triangle.
Use metres, apply object transforms before export, keep names clear, and export a simple
FBX test to confirm scale, orientation, pivots, materials and collision in Unity.

## First complete art test

Finish one bar corner: existing stool/table, one mug, wood material and a lantern.
Check standing and seated views, player faces/HP dots, readable mug interactions and
shot feedback. Compare a screenshot and motion in Editor before expanding to the whole map.
Measure frame pacing on both laptops when a matching build is later authorized.
Leave round rules unchanged during the visual pass so feedback can identify what changed.

## Implemented beta pass

The source now uses Donnee furniture/doors/windows/tableware, a Pixor ale mug, a reduced
Grab3D safe, a TimDev revolver, and coarse Poly Haven wood textures. Provenance and
remaining integration limits are recorded in ASSET-SOURCES.md. The safe is decorative
with collision until robbery rules are agreed. Rigged WRAD arms are imported; weapon
and mug grip/draw/reload/drink animations remain to be authored. No scratch-modeling of
these base objects is required. Existing animated characters and seat colliders remain.

All floating environment labels are removed, including LAST CALL, SALOON, BACK ROOM,
and BEER / E TO TAKE. Player names and HP dots remain. Interaction hints appear in the
HUD when relevant; the permanent keyboard instruction strip is moved into the pause menu.
The compact HUD uses round/time/action status, health dots, ammunition and voice status.
No new standalone build is made for this pass.

The next game-flow proposal is ROOM-ROUND-PROPOSAL.md: seated discussion, public room
agreements, fixed room stations, shared one-action phase and regrouping.
