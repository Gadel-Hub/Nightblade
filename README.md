# Nightblade

The canonical Unity foundation lives on `main`. Open this repository root with
**Unity 6000.3.24f1 (6.3 LTS)**. `BootstrapLab` verifies rendering,
`MovementLab` exercises accepted player movement, and `CombatLab` exercises the
combat foundation. Install that exact editor with **Web Build Support** through
Unity Hub.

Player movement is **manually accepted**. Combat remains **porting** pending its
manual feel check. Unity has imported and compiled the project; generated caches
remain ignored.

## Layout

```text
Assets/
  Art/Placeholder/
  Input/
  Prefabs/Player.prefab
  Scenes/{BootstrapLab,MovementLab,CombatLab}.unity
  Scripts/{Player,Combat,Enemies,Levels,Debug}/
Packages/
ProjectSettings/
```

Empty development folders have `.gitkeep` files and stable folder `.meta` files.
`PlayerMovement` owns movement; optional diagnostics and a plain lab camera live
under `Scripts/Debug`. No gameplay framework is involved.

## Technical baseline

- Built-in 2D rendering; Unity Pixel Perfect Camera package **5.1.1**.
- Reference **320×180**, **32 PPU**, orthographic size **2.8125**: a **10×5.625**
  world-unit viewport. Render-texture upscaling and both-axis cropping are enabled;
  stretch fill, HDR, MSAA, and texture mipmaps are disabled. Default window/Web
  canvas size is **960×540** (3× reference).
- Eight floor sprites each use a **32×32** texture at unit transform scale, spaced
  **1 world unit** apart. Their edges lie on integer world coordinates.
- Plain `TEMP_` player/enemy rectangles, a camera-child static HUD block, and a
  32×32 one-pixel stripe swatch verify placement, scale, and pixel presentation.
  The HUD is only a rendering marker, with no health or UI behavior.
- Input System **1.20.0** is pinned; Active Input Handling is Input System only.
  `Player/Move` uses A/D or arrows, `Player/Jump` uses Space, and `Player/Attack`
  uses J. Input and movement run in fixed updates at 60 Hz.
- All three labs are enabled build scenes; `BootstrapLab` remains first. Web uses
  IL2CPP with threading disabled and Unity's default Web template. No deployment
  pipeline or build output is included.

Package choices follow Unity's documentation for
[Pixel Perfect](https://docs.unity3d.com/Packages/com.unity.2d.pixel-perfect@5.1/manual/index.html),
[Input System](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.inputsystem.html),
and the editor's [2D Sprite core package](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.2d.sprite.html).

## Asset integration

All temporary PNGs live under `Assets/Art/Placeholder`. Their import metadata uses
Sprite (Single), Point filtering, no mipmaps, uncompressed textures, preserved
alpha, 32 PPU, and explicit center pivots. These are per-asset defaults, not a
global importer that overrides future supplied specifications. Keep each asset's
`.meta` file when replacing it.

The static bootstrap roots and player prefab have separate `Visual` children. Replace the child's
SpriteRenderer sprite and adjust its visual offset/pivot as required by the
supplied asset. The player prefab's root has an explicit 0.375×0.625 BoxCollider2D
and dynamic Rigidbody2D; changing the visual does not resize that collider. Lab
terrain also has separately authored collision geometry. `BootstrapLab` stays
static. Combat damage areas also use explicit colliders; no gameplay shape is
derived from a sprite.
Other native sprite sizes are welcome; do not resize artwork to fit a 32×32 box.
Temporary dimensions and colors impose no production design requirements.

## Current validation

Unity **6000.3.24f1** imports and compiles the project, and all three lab scenes
open. Use [player tuning](docs/PLAYER_TUNING.md), the
[movement regression checklist](docs/MOVEMENT_VALIDATION.md), and
[combat tuning](docs/COMBAT_TUNING.md) when changing those systems. Web Build
Support is installed; an actual Web build and deployment remain later work.

The Phaser reference remains untouched on `proof-of-concept` (also tagged
`phaser-prototype`). Its assets and Pages workflow were not copied or replaced.
See [the parity checklist](docs/PORT_PARITY.md) for audited behavior and tuning.
