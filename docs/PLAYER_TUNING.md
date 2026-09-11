# Player movement tuning

All movement values live in `src/player/tuning.ts`. Units are internal pixels
and seconds. The controller sets horizontal velocity immediately, including
direction reversal and stopping; there is no acceleration or deceleration.

| Parameter | Default | Effect |
| --- | --- | --- |
| `maxHorizontalSpeed` | 140 | Higher means faster running and longer horizontal jumps. |
| `gravity` | 700 | Higher means faster falling and shorter/lower jumps. |
| `jumpVelocity` | -280 | More negative means higher jumps and longer airtime. |
| `wallSlideMaxDownwardSpeed` | 55 | Higher means faster descent while pressing into a wall. |
| `wallJumpHorizontalVelocity` | 170 | Higher means a stronger outward push and more clearance. Positive magnitude, mirrored by wall side. |
| `wallJumpVerticalVelocity` | -270 | More negative means a higher wall jump. |
| `wallJumpPushDuration` | 0.12 | Seconds of forced outward velocity; higher means more separation but longer before horizontal input resumes. Contact with the opposite wall ends it early. |

On level ground, jump height is approximately `jumpVelocity² / (2 × gravity)`
(56 pixels) and airborne travel is approximately
`maxHorizontalSpeed × 2 × abs(jumpVelocity) / gravity` (112 pixels).
Collision steps and the 12×20 body affect platform-edge reach.

Space triggers only a new press while grounded. Holding or releasing it does
not change jump height. Opposing horizontal inputs cancel. There is no coyote
time, jump buffering, double jump or jump cut.

Hold toward a wall while falling to slide; press Space while in contact to
wall jump (also works while rising). The outward push overrides horizontal
input briefly so holding toward the old wall cannot immediately cancel it.
This is propulsion, not an input forgiveness window. Push distance before
steering resumes is approximately horizontal wall-jump speed × push duration.

A second jump from the same wall side is disallowed until landing or jumping
from the opposite side. Merely leaving and returning to a wall does not reset
this restriction. This intentionally prevents repeated single-wall ascent;
alternating walls permits shaft ascent. The restriction tracks side, not wall
identity, so different walls on the same side also require a landing/opposite
jump. R clears all movement state.

The development scene includes a 32-pixel short gap and a 104-pixel long gap
on its lower lane, a single wall beside the drop, and narrow/wider shafts.
The upper shaft has a partial ceiling and an open left exit. R returns to the
start after dropping into the lower tests. Use F1 to inspect contacts and state.
