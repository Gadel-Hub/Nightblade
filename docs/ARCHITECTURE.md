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
floor and an outlined area reserved for combat tests. The shaft is geometry
for future wall-jump testing; wall jumping is not implemented yet.

A/D or left/right arrows set basic horizontal velocity. Space jumps when
grounded. `src/player/PlayerController.ts` owns player movement and reset;
the scene translates keyboard input and owns collision geometry. Movement
constants live in `src/player/tuning.ts`, including the gravity used by the
Phaser configuration. R resets position and velocity. See `PLAYER_TUNING.md`
for the fixed-height jump baseline. There is no combat system.

F1 toggles fixed-camera debug text and Arcade Physics body outlines and
velocity indicators. The text reports position, velocity, blocked floor/wall
contacts and a derived placeholder movement state. Combat hitboxes and
hurtboxes do not exist yet.

All visuals are programmer art. There are no production levels, backend,
persistence, final assets or menus.
