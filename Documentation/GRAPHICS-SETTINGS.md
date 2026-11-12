# Graphics settings

Open **Graphics Settings** from the main menu or the Escape menu in a match.
These are local preferences; an online match continues while the menu is open.

- **Low:** no shadows, no anti-aliasing, reduced texture detail and per-vertex local lighting.
- **Medium (default):** hard shadows to 25 metres, full texture detail, no anti-aliasing and two pixel lights.
- **High:** soft shadows to 50 metres, full texture detail, 4x anti-aliasing and four pixel lights.
- **Resolution:** choose from display-reported resolutions and the current window size.
- **Display mode:** windowed or borderless fullscreen.
- **VSync:** off by default. When on, the monitor refresh rate controls frame rate.
- **Frame rate limit:** 60 (default), 90, 120, 144, 165, 240 or unlimited. Used with VSync off.

Select **Apply** to use and save changes. Display changes require **Keep Changes**
within 15 seconds; otherwise all changes from that Apply revert. Escape also reverts
an unconfirmed change. Cancel/Back discards edits that have not been applied.
Defaults stages Medium, VSync off and 60 FPS, keeping the current window size and mode.

Preferences use the `SaloonGraphics.v1` PlayerPrefs key and load on subsequent starts.
In the Unity Editor, display size and mode are not changed; quality and frame limits apply.
Settings affect future player builds; existing distributed builds do not gain this menu.

The minimum performance target is **60 FPS**. A 60 FPS cap does not establish minimum
hardware requirements: those still require measurements on real PCs at a specified
resolution and quality preset.
