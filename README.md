# Nightblade

The canonical Unity foundation lives on `main`. Open this repository root with
**Unity 6000.3.24f1 (6.3 LTS)**, then open `Assets/Scenes/BootstrapLab.unity`.
Install that exact editor with **Web Build Support** through Unity Hub.

This is a static technical bootstrap: no gameplay has been ported. The project
files were authored without running Unity; Editor import, compilation, rendering,
and Web target validation remain pending. On first import, Unity will populate
remaining default settings and resolve packages. Review and commit normal settings
and `Packages/packages-lock.json`; leave generated caches ignored.

## Layout

```text
Assets/
  Art/Placeholder/
  Prefabs/
  Scenes/BootstrapLab.unity
  Scripts/{Player,Combat,Enemies,Levels,Debug}/
  Tests/
Packages/
ProjectSettings/
```

Empty development folders have `.gitkeep` files and stable folder `.meta` files.
There are no custom C# scripts or frameworks in this bootstrap.

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
  No action maps, bindings, `PlayerInput`, or gameplay controls are implemented.
- `BootstrapLab` is the sole enabled build scene. Web uses IL2CPP with threading
  disabled and Unity's default Web template. No deployment pipeline or build
  output is included. Web target availability requires the matching editor module.

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

Player and enemy roots have separate `Visual` children. Replace the child's
SpriteRenderer sprite and adjust its visual offset/pivot as required by the
supplied asset. Future gameplay colliders and hitboxes belong to explicitly
configured gameplay objects, independently of these visuals. No collider,
rigidbody, damage area, or automatic sprite-derived physics shape exists here.
Other native sprite sizes are welcome; do not resize artwork to fit a 32×32 box.
Temporary dimensions and colors impose no production design requirements.

## Verification handoff

Static validation passed on 2026-09-12: YAML/JSON parsing, unique asset GUIDs and
scene references, PNG dimensions/checksums and import settings, viewport/grid
arithmetic, build-scene registration, and Git exclusions. The Pixel Perfect
script GUID/serialized fields and pinned registry dependencies were checked
against Unity's package data. All parity source paths exist at the audited ref;
other branch/remote/tag refs remain unchanged. These checks do not establish that
Unity imports or renders the scene.

Pending in **6000.3.24f1**:

1. Import the root project, let packages resolve, and confirm no Console compile
   or import errors and no missing scripts/materials/sprites.
2. Open `BootstrapLab`, enter Play mode, and check its static camera, eight floor
   cells, player/enemy rectangles, HUD marker, and stripe swatch.
3. Check Game view at 320×180, 960×540, and 1000×700: native/integer pixels, no
   blurred stripes, and letterboxing at the nonmatching aspect. Do not use a Game
   view preview scale below 1× to judge crispness. Viewports smaller than the
   reference are outside this baseline.
4. Confirm a floor tile measures exactly 1×1 world units at transform scale 1.
5. In Build Profiles, confirm Web is available with Web Build Support installed;
   switch the local active target to Web and check package compilation. A local
   smoke build may go to ignored `Builds/Web/`; publishing is a later task.
6. Review Unity's serialized changes and package lock before committing them.

The Phaser reference remains untouched on `proof-of-concept` (also tagged
`phaser-prototype`). Its assets and Pages workflow were not copied or replaced.
See [the parity checklist](docs/PORT_PARITY.md) for audited behavior and tuning.
