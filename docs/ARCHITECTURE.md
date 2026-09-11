# Repository foundation

The app uses TypeScript, Vite and Phaser 3. Run `npm install`, then
`npm run dev`. `npm run build` type-checks and produces `dist/`;
`npm run preview` serves that build. Desktop keyboard input is required.

`src/main.ts` creates the game at 320×180, enables pixel-art rendering,
disables antialiasing and rounds camera pixels. Resize handling chooses a
whole-number display scale with CSS nearest-neighbor rendering. Viewports
smaller than the internal canvas retain 1× scale and may scroll.

`src/scenes/ArtScene.ts` starts by default and at `/#art`.
`src/scenes/DevelopmentScene.ts` starts at `/#dev` or via V from the showcase.
It generates one white texture for a rectangular placeholder player and
uses static Arcade Physics rectangles for solid terrain. No asset loading
or boot scene is needed. The camera follows the player within a 1920×540
original laboratory; isolated enemy bays extend the world to its right. This
scene remains the mechanics test area as development proceeds.

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

`src/combat/PlayerDamage.ts` owns health, hit/death states, received knockback
and invulnerability/death timers. The scene gates controls during hit/death,
interrupts attacks on accepted damage, and respawns at the combat start.
The red test pad emits one damage event per overlap entry. Damage overlap is
processed before target hits. The player controller's interruption method
clears a pending wall push without changing accepted movement tuning.

F1 toggles fixed-camera debug text and Arcade Physics body outlines and
velocity indicators. The text reports position, velocity, blocked floor/wall
contacts and the current movement state. Combat hitboxes and
hurtboxes are separate from Arcade terrain bodies and are drawn by the scene
in F1 mode. See `COMBAT_TUNING.md` for timing and test controls.

The mechanics laboratory retains programmer art. `ArtScene.ts` is an optional
playable native-art showcase (V, or `/#art`) using the same player controllers
and Guard. Its scale-1 visual layers are separate from unchanged physics bodies.
It preloads six PNG anchors from `public/assets`; `art/source` holds editable
palette grids. `scripts/pixel-assets.mjs` exports/validates them during development,
never at runtime. See `ART_SHOWCASE.md`. There is no backend or persistence.

`src/enemies/EnemyBody.ts` shares body/hurtbox bookkeeping, attack rectangle
geometry and death cleanup. `Guard.ts`, `RangedAttacker.ts`, `Pursuer.ts` and
`Heavy.ts` own their own
explicit decisions. `Projectile.ts` owns one horizontal shot's physics body,
terrain collider, lifetime and cleanup. Shots are owned by the ranged attacker
and cleared when it dies or resets.
`EnemyLab.ts` owns isolated test geometry, explicit encounter placement,
damage integration and reset. Enemies collide with terrain but not each other
or the player; only active attack rectangles/projectiles damage the player. No navigation
or generalized decision system exists. See `ENEMY_TUNING.md`.


`Level1Scene.ts` is the first production stage (`/#level1`, or L in the lab).
It reuses the accepted controllers and explicit enemy classes, with its own
scene orchestration and provisional art layer. `src/levels/visualDesign.ts`
contains its hand-authored layout. `Checkpoint.ts` tracks a run-local respawn
position, and `artFrames.ts` names runtime frame roles. Death rebuilds relevant
encounters and clears projectiles. A latched exit pauses physics and presents
restart/return controls. The level is stopped on return, while the laboratory
can resume its sleeping scene. See `LEVEL_1.md` for the route and browser checks.
