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


## Shared Western character — 2026-11-08

All players use the Gunman from Low Poly Western Starter Pack 1.0 by tt-3D:
https://assetstore.unity.com/packages/3d/characters/low-poly-western-starter-pack-383104
Imported through the user's Unity Asset Store account under the Standard Asset Store EULA.
Original pack files and derived Gunman meshes/prefabs remain local and are excluded from
the public source repository. They are included only within compiled game distributions.
To work from a fresh source checkout, acquire/import the pack through your own Unity
account, then run **Saloon → Prepare Western Gunman character**. The preparation code
creates the local runtime prefab and mouth/grip shapes. The character is not CC0.


## Recorded sound replacement — 2026-11-11

All four recordings are CC0 1.0 Universal, verified on their Freesound source pages.
Original public HQ preview downloads are preserved in `ArtSource/Audio/Recorded/`;
runtime PCM WAV copies, source URLs and hashes are under `Assets/Resources/Audio/Recorded/`.

| Use | Recording / author | Source |
|---|---|---|
| Reload | Clean Revolver Reload — Dredile | [Clean Revolver Reload](https://freesound.org/people/Dredile/sounds/177863/) |
| Drinking | Water_Drink.wav — Q.K. | [Water_Drink.wav](https://freesound.org/people/Q.K./sounds/56270/) |
| Hoofbeats | Horse Galloping.wav — Max_Headroom | [Horse Galloping.wav](https://freesound.org/people/Max_Headroom/sounds/175356/) |
| DistantShot | Distant Gunshot — Resaural | [Distant Gunshot](https://freesound.org/people/Resaural/sounds/541164/) |

License: https://creativecommons.org/publicdomain/zero/1.0/

Changes: mono conversion, loudness adjustment and short edge fades. Drinking uses
pitch-preserving tempo adjustment to retain the full performance in the 2.5-second
action. Horse audio has rumble filtering and an 80 ms loop crossfade. Runtime
approach uses a gradual volume increase and distance filtering, without speeding
up the horse recording or restarting it at every warning stage.
Reproduce with `python3 Tools/prepare_library_audio.py` (ffmpeg required).


### Selected revolver shot — 2026-11-11

[Heathers Gunshot Effect2.wav by okieactor](https://freesound.org/people/okieactor/sounds/415912/)
replaces the generated revolver shot. CC0 1.0 Universal, confirmed on the source
page. Original public HQ preview: ArtSource/Audio/Recorded/RevolverShot.mp3.
Runtime: Assets/Resources/Audio/Recorded/RevolverShot.wav. Converted to mono
44.1 kHz PCM and level-adjusted with a short tail fade. The original layered
blast and reverb are retained; no pitch shift or additional layers were added.
Hashes and download URL are included in both audio source manifests.
