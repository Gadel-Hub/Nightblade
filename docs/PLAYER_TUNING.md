# Player movement tuning

Status: **porting**. Source reviewed at `5a8f52038b15e32edd64d6c4a3f35b1e417359bd`:
`src/player/tuning.ts`, `src/player/PlayerController.ts`, `src/main.ts`, and
`src/scenes/DevelopmentScene.ts` (body size, terrain, and input wiring).
Unity **6000.3.24f1** is unavailable; no Unity compilation or runtime parity is claimed.

## Reference conversion

One Unity world unit represents 32 reference pixels. Distances, velocities, and
accelerations divide by 32; seconds stay unchanged. Phaser +Y is down; Unity +Y
is up. All seven tuning fields are serialized on `PlayerMovement` in
`Assets/Prefabs/Player.prefab`; C# defaults match that prefab.

| Setting | Phaser | Unity | Purpose / conversion |
| --- | --- | --- | --- |
| Run speed | 140 px/s | 4.375 units/s | Immediate ±speed, zero on neutral; no acceleration |
| Gravity | +700 px/s² | 21.875 units/s² downward | Positive magnitude; subtract from vertical velocity |
| Jump velocity | −280 px/s | +8.75 units/s | Fixed upward launch from ground |
| Wall-slide cap | +55 px/s | −1.71875 units/s lower limit | Clamp only while falling against held wall; serialized magnitude 1.71875 |
| Wall-jump X | 170 px/s away | 5.3125 units/s away | Sign points away from contacted wall |
| Wall-jump Y | −270 px/s | +8.4375 units/s | Fixed upward launch from eligible wall |
| Outward commitment | 0.12 s | 0.12 s | Temporary forced horizontal velocity; not permanent control loss |
| Player body | 12×20 px | 0.375×0.625 units | Explicit centered BoxCollider2D; unrelated to sprite bounds |

Ideal continuous normal-jump estimates are 0.4 s to apex, 1.75 units (56 px) of
rise, and 3.5 units (112 px) of same-height horizontal travel. These are comparison
guides, not measured Unity results; discrete integration lowers these slightly.

## Physics and input decisions

`PlayerMovement` owns velocity in `FixedUpdate`; Unity handles terrain collision
and separation. The prefab uses a dynamic Rigidbody2D with mass 1, zero linear
and angular damping, frozen Z rotation, continuous collision detection, no
interpolation, and Never Sleep. Its shared PhysicsMaterial2D has zero friction
and bounce, as does lab terrain. The collider stays on the root; the sprite is
on a separate `Visual` child at unit scale.

Rigidbody `gravityScale` is **0**. The controller applies `velocity.y -= gravity *
fixedDeltaTime`, then clamps a slide. This avoids a second gravity contribution
after the cap and keeps tuning independent of global Physics2D gravity. There is
no controller fall-speed cap outside a wall slide. Collisions stop upward travel
at ceilings; no jump-cut or special ceiling impulse is added.

Physics is explicitly **60 Hz** (`TimeManager.asset`). The POC does not override
Arcade's fixed 60 Hz default. That default and gravity-before-position integration
were also inspected in the locally installed Phaser 3.90.0 source, matching its
locked version. Unity's Box2D solver, contact tolerances, CCD, and fixed input
sampling differ from Arcade's scene-update ordering. In particular, the 0.12 s
lock is quantized to physics steps (up to one extra 1/60 s step). These differences
need runtime comparison; no compensating tuning changes have been guessed.

The ordinary Input System asset contains only `Player/Move` (one 1D Axis
composite: A/Left Arrow negative, D/Right Arrow positive) and `Player/Jump`
(Space, Button). Both directions cancel, including mixed A + Right Arrow.
Each player clones the asset and enables/disables its own map. Project input
settings process events before FixedUpdate; `WasPressedThisFrame` is consumed
for that step only. A held jump does not repeat; an ineligible press is discarded.
No coyote time, stored jump request, double jump, or variable jump height exists.

The player is on layer **9 / Player**. Terrain is **8 / Terrain**; the serialized
contact mask is **256**. The existing default Physics2D collision matrix is not
overridden; these layers collide normally. Ground/left/right use solid contacts
from `Rigidbody2D.GetContacts`, filtered to Terrain. Upward normals ≥0.9 indicate
floor; rightward normals ≥0.9 indicate a left wall; leftward normals ≤−0.9 indicate
a right wall. A ceiling normal never counts as ground. Ground also requires
nonpositive vertical velocity to reject lingering contacts after a launch. No
proximity probe, sprite geometry, or vertical-speed-only ground test is used.
The threshold targets the reference's axis-aligned geometry, not slope traversal.

## Wall rules and states

Left contact takes precedence when both walls touch. Sliding requires airborne
contact, input into that wall, nonpositive Y velocity, and no remaining push.
Neutral/away input releases the slide immediately; rising contact does not slide.

A wall jump does not require sliding or input into the wall. It records the wall
side, launches away/up, and replaces horizontal input for 0.12 s. Landing clears
the restriction and timer. Reaching the opposite wall ends the push early and
permits a jump there. Leaving and retouching the same side without landing or
jumping from the other side does not grant another wall jump.

State priority follows the POC: grounded Idle/Run, then WallJump while committed,
then WallSlide, otherwise Jump/Fall by vertical direction. Debug grounded state is
cleared immediately on a ground launch instead of displaying the previous contact
for that step. Damage interruption and respawn APIs remain outside this task.

## Checks and maintainer gate

Run `python3 scripts/validate_movement.py` from the repository (Python 3 with
PyYAML). This compares the actual Git reference against serialized tuning and C#
defaults, and checks prefab components, input wiring, contact mask/layers, material,
scene references, test geometry, and parity statuses. It does **not** compile C#,
load Unity assets, simulate collisions, or establish feel. Mono alone is present
without Unity reference assemblies; no Unity compilation path is available here.

Use [the manual movement checklist](MOVEMENT_VALIDATION.md) in the pinned editor.
The maintainer must accept movement before combat work begins. Only then change
the relevant parity entries to `manually accepted`.

API references used for wiring:
[Rigidbody2D contacts](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody2D.GetContacts.html),
[linear velocity](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody2D-linearVelocity.html),
and [InputAction press sampling](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputAction.html).
