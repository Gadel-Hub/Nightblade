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
| `playerHealth` | 3 | More damage needed to kill the player. |
| `hazardDamage` | 1 | More health lost per contact entry. |
| `receivedKnockbackHorizontal` | 150 | Faster push away from the source (pixels/second). |
| `receivedKnockbackVertical` | -160 | More negative gives a higher knockback arc. |
| `hitReactionDuration` | 0.18 | Longer interruption of movement and attack input. |
| `postHitInvulnerability` | 0.45 | Longer protection from new hits, measured from damage reception. |
| `respawnDelay` | 0.8 | Longer wait while dead before laboratory respawn. |

J starts a slash only while idle; presses during startup/active/recovery are
ignored. Facing follows horizontal input and is latched at swing start. The
hitbox follows the body while running/jumping, but never flips mid-swing.
Each target can be hit once per swing, tracked by target identity. Dummies
are non-solid. Terrain collision remains separate from combat overlap checks.

C resets the player and targets in the combat laboratory; R resets them and
returns to the movement start. F1 shows phase, active status, hit count and
combat rectangles (cyan player hurtbox, yellow attack, orange targets).
The plain cream rectangle is the temporary slash itself, not a debug overlay.

The red pad deals one hit on entry; sustained contact does not repeat damage,
even after invulnerability expires. Exit and re-enter to create another event.
Entries during invulnerability are consumed, not queued. Protection prevents
rapid re-entry hits; contact latching independently prevents per-frame damage.

Damage interrupts a swing, applies source-based knockback, and blocks controls
for the hit reaction. Input presses during the interruption are discarded.
Keep invulnerability at least as long as hit reaction (the hit state itself
also rejects damage). Horizontal control resumes immediately afterward.
An interrupted wall push is cleared, but the same-wall jump restriction stays.

At zero health the body is disabled and player hidden; attacks and further
damage are rejected. After the delay, player and dummies reset at the combat
spawn with full health. R and C also clear all attack/damage/contact state.
F1 includes health, damage state and remaining invulnerability; a plain health
and state readout remains visible for manual testing when F1 is off.
