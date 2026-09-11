# Enemy tests

`src/enemies/tuning.ts` contains enemy health, speeds (pixels/second), ranges
(pixels), and attack/death durations (seconds). Higher range detects/reaches
farther; higher speed closes distance faster; longer startup gives more warning,
longer active keeps danger present, and longer recovery leaves more punish time.
Damage uses the existing player hit reaction and knockback values unchanged.

Press 1–7 for the tests below. Number keys reset the selected test; death respawns
there and resets its enemies. R returns to movement, C to the dummy laboratory.
F1 shows enemy health, state, hurtboxes and active attack rectangles.

| Key | Test |
| --- | --- |
| 1 | Guard |
| 2 | Ranged Attacker |
| 3 | Pursuer |
| 4 | Heavy |
| 5 | Guard + Ranged Attacker |
| 6 | Pursuer + Ranged Attacker |
| 7 | Guard + Heavy |

Guard patrols a fixed short segment, commits its facing at startup, slashes
once, then recovers. Gold means startup; a red rectangle is the active attack.
There is no contact damage. One attack can damage the player at most once,
including when the player is invulnerable. Each player swing hits each enemy
once using the established target-identity set. Lethal hits immediately remove
enemy attacks; a brief gray dead state precedes body/visual/collider cleanup.

Ranged remains stationary, locks facing during its gold windup, fires one
horizontal projectile, then recovers. No tracking aim or prediction is used.
`projectileSpeed` controls travel, `projectileLifetime` caps flight time, and
`maxProjectiles` caps live shots per shooter. Shots are consumed on terrain or
player contact (even protected contact), expiry, leaving their bay, shooter
death, or encounter reset. F1 outlines projectile collision rectangles.

Pursuer has one health and fast direct horizontal pursuit. It stops to deliver
a short-windup slash. Geometry blocks it normally; it cannot navigate platforms.
Heavy slowly approaches within a bounded area, commits to a long startup and
a wide frontal strike, then stays vulnerable through a long recovery. Its
two-damage, 52-pixel strike provides area denial without modifying player
knockback. A small light mark on each placeholder shows facing.

| Archetype | HP | Speed | Detection | Startup / active / recovery | Damage |
| --- | --- | --- | --- | --- | --- |
| Guard | 3 | 30 | 48 | .35 / .12 / .55 | 1 |
| Ranged | 2 | 0 | 260 | .55 / projectile / 1.1 | 1 |
| Pursuer | 1 | 92 | 240 | .12 / .10 / .24 | 1 |
| Heavy | 6 | 20 | 140 | .80 / .22 / .95 | 2 |

Detection measures center distance horizontally with a separate vertical band.
Melee range extends from the front body edge, centered vertically. Guard patrol
and Heavy roam distances limit displacement from their starting X position.
Nonlethal damage does not stagger enemies or cancel their committed attacks.
Combinations are integration fixtures; encounter difficulty is not production tuning.
