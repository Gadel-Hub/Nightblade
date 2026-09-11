# Combat tuning

Values live in `src/combat/tuning.ts`; time is seconds, distances are internal pixels.

| Parameter | Default | Effect of increasing |
| --- | --- | --- |
| `startupDuration` | 0.08 | Longer delay before the slash can hit. |
| `activeDuration` | 0.10 | Longer damage window. |
| `recoveryDuration` | 0.18 | Longer wait before another attack. |
| `attackRange` | 24 | Extends the outer hitbox edge farther from the player body. |
| `attackHitboxWidth` | 24 | Extends the hitbox inward from its outer edge. Keep at or below range. |
| `attackHitboxHeight` | 18 | More vertical reach, centered on the body. |
| `attackDamage` | 1 | More damage per target per swing. |
| `targetHealth` / `durableTargetHealth` | 3 / 10 | More hits needed to remove a test dummy. |

J starts a slash only while idle; presses during startup/active/recovery are
ignored. Facing follows horizontal input and is latched at swing start. The
hitbox follows the body while running/jumping, but never flips mid-swing.
Each target can be hit once per swing, tracked by target identity. Dummies
are non-solid. Terrain collision remains separate from combat overlap checks.

C resets the player and targets in the combat laboratory; R resets them and
returns to the movement start. F1 shows phase, active status, hit count and
combat rectangles (cyan player hurtbox, yellow attack, orange targets).
The plain cream rectangle is the temporary slash itself, not a debug overlay.
