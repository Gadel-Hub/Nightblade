# Player combat tuning

Status: **porting** pending maintainer acceptance in Unity **6000.3.24f1**.
Reference reviewed at `5a8f52038b15e32edd64d6c4a3f35b1e417359bd`:
`src/combat/tuning.ts`, `src/combat/PlayerCombat.ts`,
`src/combat/PlayerDamage.ts`, and `src/scenes/DevelopmentScene.ts`.

## Reference conversion

One Unity world unit represents 32 reference pixels. Distances and velocities
divide by 32. Phaser +Y points down and Unity +Y points up; seconds and integer
damage values are unchanged.

| Setting | Phaser | Unity | Purpose / conversion |
| --- | --- | --- | --- |
| Startup | 0.08 s | 0.08 s | Attack begins with no active hitbox |
| Active | 0.10 s | 0.10 s | Hitbox enabled; each target can be hit once |
| Recovery | 0.18 s | 0.18 s | Hitbox disabled; new attacks rejected |
| Attack hitbox | 24×18 px | 0.75×0.5625 units | Explicit child BoxCollider2D, independent of art |
| Attack center offset | 18 px from body center | ±0.5625 units | Body half-width 6 px plus hitbox half-width 12 px; the box extends 24 px from the body edge |
| Attack damage | 1 | 1 | Subtracted once per target per swing |
| Player hurtbox | 12×20 px | 0.375×0.625 units | Existing explicit player body collider; independent of visual bounds |
| Player maximum health | 3 | 3 | Restored fully on lab respawn |
| Received knockback X | 150 px/s away | 4.6875 units/s away | Divide by 32; source side selects sign |
| Received knockback Y | −160 px/s | +5 units/s | Divide by 32 and reverse Y |
| Hit reaction | 0.18 s | 0.18 s | Movement and combat control disabled; knockback owns velocity |
| Invulnerability | 0.45 s | 0.45 s | Further damage ignored; continues after control returns |
| Respawn delay | 0.8 s | 0.8 s | Dead player hidden and Rigidbody2D simulation disabled |
| Damage source | 1 | 1 | One attempt per trigger entry in CombatLab |
| Normal target health | 3 | 3 | Stationary development target |
| Durable target health | 10 | 10 | Stationary repeated-swing target |

## Implementation decisions

`PlayerCombat` uses the explicit `Idle`, `Startup`, `Active`, and `Recovery`
phases. A J press is sampled with the Input System's fixed-update processing and
consumed by the next rendered update. Starting a swing captures facing and clears
a `HashSet<CombatTarget>`. During the active phase an `OverlapBox` queries only
layer 10 (`Damageable`); successful targets enter the set, so physics callback
frequency cannot produce repeated damage. The disabled attack collider is explicit
gameplay geometry and never follows sprite dimensions.

`PlayerHealth` owns only maximum/current health. `PlayerDamage` rejects nonpositive,
protected, or non-normal-state hits; interrupts movement and attacks; applies the
reference knockback; and restores control after 0.18 seconds. On a same-X hit it
pushes opposite current facing, matching the reference. Lethal damage zeroes
velocity, disables Rigidbody2D simulation, and hides only the separate `Visual`
child. `CombatLabRespawn` restores the player and both test targets after 0.8
seconds at its explicit spawn transform. Checkpoints and production reset flow
remain outside this port.

`CombatLab` contains the player prefab, open floor and walls, normal and durable
stationary targets, a trigger damage source, a spawn point, and development-only
F1 diagnostics. Placeholders reuse the isolated temporary sprites; all combat
colliders remain separately authored.

## Validation and maintainer gate

Unity **6000.3.24f1** imported and opened `CombatLab` without compile or scene
serialization errors. A temporary PlayMode validation assembly exercised phase
timing, hitbox activation, both hitbox directions, per-swing hit tracking, attacks
while movement control is active and while airborne, damage rejection during
invulnerability, knockback, hit recovery, lethal damage, respawn, and target reset.
The test passed. It also confirmed that the Attack action is enabled and resolves
the J binding. The temporary assembly was kept outside the repository.

Unity batch mode did not drive `WasPressedThisFrame` from synthetic keyboard
events, so physical J and F1 input were not claimed from automation. In Play mode,
manually verify J startup/active/recovery, left/right reach, moving and airborne
attacks, one hit per target per swing, damage-source re-entry, knockback direction,
the 0.18-second control return, the remaining invulnerability window, death and
respawn timing, and the F1 overlay/wireframes. Compare attack timing, reach,
knockback, reaction, invulnerability, and respawn feel with the Phaser reference
before marking combat `manually accepted`.
