# Repository foundation

The app uses TypeScript, Vite and Phaser 3. Run `npm install`, then
`npm run dev`. `npm run build` type-checks and produces `dist/`;
`npm run preview` serves that build. Desktop keyboard input is required.

`src/main.ts` creates the game at 320×180, enables pixel-art rendering,
disables antialiasing and rounds camera pixels. Resize handling chooses a
whole-number display scale with CSS nearest-neighbor rendering. Viewports
smaller than the internal canvas retain 1× scale and may scroll.

`src/scenes/DevelopmentScene.ts` is the only scene and starts directly.
It generates one white texture for a rectangular placeholder player and
uses static Arcade Physics rectangles for solid terrain. No asset loading
or boot scene is needed. The camera follows the player within a 1920×540
world. This scene remains the mechanics test area as development proceeds.

The sandbox has a long floor, platforms at several heights, a low corridor,
boundary walls, ceilings, a narrow wall-test shaft, a drop to a lower safety
floor and an outlined area reserved for combat tests. A lower gap lane,
single wall and two open shafts exercise wall jumps and edge collisions.

A/D or left/right arrows set basic horizontal velocity. Space jumps when
grounded. `src/player/PlayerController.ts` owns player movement and reset;
the scene translates keyboard input and owns collision geometry. Movement
constants live in `src/player/tuning.ts`, including the gravity used by the
Phaser configuration. R resets position and velocity. See `PLAYER_TUNING.md`
for the fixed-height jump baseline and wall-slide/jump rules. The controller
tracks the last wall-jump side and remaining outward push duration; movement
labels are derived from those and physics contacts.

`src/combat/PlayerCombat.ts` owns the slash lifecycle and separate combat
rectangles. The scene owns two stationary non-solid test targets and applies
overlap hits. `src/combat/tuning.ts` holds combat values. J attacks; C resets
into the combat laboratory. No attack code modifies movement tuning.

F1 toggles fixed-camera debug text and Arcade Physics body outlines and
velocity indicators. The text reports position, velocity, blocked floor/wall
contacts and the current movement state. Combat hitboxes and
hurtboxes are separate from Arcade terrain bodies and are drawn by the scene
in F1 mode. See `COMBAT_TUNING.md` for timing and test controls.

All visuals are programmer art. There are no production levels, backend,
persistence, final assets or menus.
