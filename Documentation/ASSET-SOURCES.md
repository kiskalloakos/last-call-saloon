# Placeholder asset provenance

Downloaded/imported on 2026-11-07. The original placeholder artwork is CC0; the beta revolver below is CC BY 4.0.
Original license files are retained under `Assets/Resources/Art`.

| Asset | Creator | Source and license | Used files |
|---|---|---|---|
| Furniture Pack 1.0 | Kenney | [Creator's furniture page](https://kenney.nl/assets/furniture-kit); [Kenney's original OpenGameArt upload](https://opengameart.org/content/furniture-kit); CC0 in downloaded archive | table, tableRound, chair, chairRounded, stoolBar, kitchenBar, bookcaseOpen, bookcaseClosed, bench, lampRoundTable |
| Animated Characters Protagonists 1.1 | Kenney | [Kenney](https://kenney.nl/); CC0 in existing project's included license | characterMedium.fbx, criminalMaleA skin, idle/run animations copied into this independent project |
| Wine Barrel 01 | James Ray Cock / Poly Haven | [Model](https://polyhaven.com/a/wine_barrel_01); [Poly Haven asset license](https://polyhaven.com/license), CC0 | 1K FBX and 1K diffuse JPG |

Furniture archive: https://opengameart.org/sites/default/files/kenney_furniturePack.zip

Barrel FBX: https://dl.polyhaven.org/file/ph-assets/Models/fbx/1k/wine_barrel_01/wine_barrel_01_1k.fbx

Barrel diffuse: https://dl.polyhaven.org/file/ph-assets/Models/jpg/1k/wine_barrel_01/wine_barrel_01_diff_1k.jpg

The wooden building shell, bottles, brass tap, placeholder revolver, materials, and
generated gunshot sound were assembled for this prototype; they are not downloaded third-party content.
The supplied Kenney/Poly Haven websites are sources, not game branding.

## Low-poly beta art pass — 2026-11-07

| Asset | Creator / source | License and integration |
|---|---|---|
| Tavern bar, table, chair, shelf, door, window, cup, plate | [Donnee](https://donnee.itch.io/3d-tavern-pack) | Creator page explicitly CC0; archive Readme retained. Runtime meshes use simplified materials/256 px textures. |
| Ale mug | [Frakassets / Pixor](https://pixor.art/library/food/mug-ale/) | CC0 on creator page; original GLB retained, exported FBX. Visible on tables and when carried/drunk. |
| Office Safe | [Grab3D](https://grab3d.com/models/props/office-safe/) | CC0; original license and origin JSON retained. Procedural geometry, AI-assisted generator code disclosed by creator. Decimated to 3,724 triangles. Visual/collision only; no robbery action yet. |
| Colt lowpoly revolver | [TimDev / OpenGameArt](https://opengameart.org/content/colt-lowpoly-revolver) | CC BY 4.0; FBX/texture adaptation. Attribution included in the pause menu and Resources/AssetCredits.txt. Replaces first/third-person placeholder gun. |
| WRAD ARMS | [wriks](https://wriks.itch.io/wrad-arms) | CC0 LICENSE and README retained. Rig imported for animation work; not yet fitted to the live weapon/drink poses. |
| Wood Planks | [Amal Kumar / Poly Haven](https://polyhaven.com/a/wood_planks) | [CC0](https://polyhaven.com/license); reduced to 256 px and used on floor/timber/furniture. |

Original model subset and SHA256 manifest: `ArtSource/LowPoly/`.
Runtime derivatives: `Assets/Resources/Art/LowPoly/`.
Reproducible Blender conversion: `Tools/export_lowpoly_art.py`; run with `--disable-autoexec`.
Original downloaded scripts are never run. Meshes, texture references and materials were
inspected during conversion. Existing animated Kenney characters are retained; the optional
Quaternius character replacement was not needed for this pass.

The CC0 Old Revolver by Lucian Pavel was evaluated and retained as an unused source candidate; its close-up appearance did not fit the beta. The simpler TimDev model is the active weapon.
