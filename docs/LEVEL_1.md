# Level 1 playtest

Launch `/#level1`, or press **L** in the development laboratory. A/D or arrows
move, Space jumps/wall-jumps, J slashes, F1 toggles diagnostics. R restarts the
whole level, including checkpoint progress. V returns to the laboratory;
there V opens the art showcase. No later level launches from the exit.

## Route

`src/levels/visualDesign.ts` contains hand-authored collision rectangles,
hazards, spawns and encounters in internal pixels. World size is 6144×360.

| Region (approximate X) | Purpose |
| --- | --- |
| 0–640 | Safe opening, low blocks and distinct layered plate surfaces |
| 640–1200 | Two separated Guards with generous approach/retreat space |
| 1200–1800 | Comb hazards in isolation, then low raised plates |
| 1800–2800 | Required alternating-wall ascent, descending ledges, Guard landing area |
| 2800–3200 | Quiet midpoint checkpoint, away from detection ranges |
| 3200–4500 | Guard spacing, elevated Ranged attackers, cover and Pursuer pressure |
| 4500–5950 | Learned comb/shaft rhythm followed by mixed encounters |
| 5950–6144 | Quiet exit frame and temporary completion overlay |

Eight Guards, three Ranged attackers and two Pursuers are placed explicitly.
No Heavy or boss. Guards retain their short patrols. Raised firing positions
activate when the player approaches their height; floor plates give cover.
Nine static comb strips (32 or 48 pixels wide) deal one damage event per entry,
using the accepted contact latch and invulnerability behavior.

The intended careful clear is roughly 2–3 minutes, aiming at three. This is a
layout estimate, not a measured human clear. Running past fights can be much
faster: enemies are not mandatory kill gates. The first full playtest must
judge whether encounters and wall traversal provide enough activity without
empty travel, and whether the checkpoint is late enough/early enough.

## Lifecycle

`Checkpoint.ts` stores only the initial and activated respawn positions in
memory. The frame at X=3008 changes from Steel to Amber on contact. Activation
does not heal; death restores full health at that position after the accepted
death delay. No localStorage or save files are involved.

Every respawn clears attack/damage/wall-push state and hazard contact latches,
destroys all current enemies and their projectiles/colliders, then recreates
the relevant encounters. After checkpoint activation, enemies behind it stay
absent on respawn. R makes a new run with all thirteen enemies and no checkpoint.
The safe floor permits recovery from missed ledges; there are no hidden death
planes. Comb contact and enemy attacks are the damage sources.

The camera follows without smoothing and clamps to world bounds. Rendering
rounds pixels; art remains scale 1 and separate from physics. At the exit,
completion latches once, interrupts combat, destroys enemies/projectiles and
pauses physics. R resumes a fresh scene; V returns to the lab.

## Provisional visuals and contracts

No PNGs were added or edited. Existing Courier, Guard, structure, swatch, comb
and health sheets are reused. Checkpoint and exit use simple integer-grid,
palette-colored rectangles. Other enemy visuals remain existing placeholders.
`src/levels/artFrames.ts` names pose/tile/HUD frames; pose selection never owns
combat timings. Collision dimensions remain independent from sprite margins.
Shared player, combat and enemy tuning/decision code is unchanged.

## Verification

Run `npm run assets:validate`, `npm run test:assets`, and `npm run build`.
For browser checks, install Chromium once with `npx playwright install chromium`,
start `npm run dev`, then run `npm run test:level`. If Vite uses another address,
set `LEVEL_TEST_URL`, for example `LEVEL_TEST_URL=http://127.0.0.1:5175 npm run test:level`.
The browser tests inject access to the game into the served entry module only;
there is no test global in the shipped application.

Checks exercise real Phaser physics/controller steps for blocks, comb crossings,
shafts and descending ledges. Lifecycle checks cover health, hit latching,
checkpoint activation, repeated death, enemy/body counts, projectile cleanup,
melee single-hit behavior, F1, bounded camera, completion and real-keyboard
restart/scene transitions. Individual traversal fixtures isolate enemies;
they do not establish human encounter difficulty or a three-minute clear time.
Native 320×180 opening/shaft/checkpoint/encounter/exit captures were inspected.
The accepted laboratory movement/combat/damage/enemy and art checks were also run.

Human gate: play from beginning to end. Review shaft exit readability, ranged
platform approaches, the final Guard/Pursuer spacing, checkpoint placement and
clear length before any further level work. Art polish is not an acceptance gate.
