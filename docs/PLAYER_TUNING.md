# Player movement tuning

All movement values live in `src/player/tuning.ts`. Units are internal pixels
and seconds. The controller sets horizontal velocity immediately, including
direction reversal and stopping; there is no acceleration or deceleration.

| Parameter | Default | Effect |
| --- | --- | --- |
| `maxHorizontalSpeed` | 140 | Higher means faster running and longer horizontal jumps. |
| `gravity` | 700 | Higher means faster falling and shorter/lower jumps. |
| `jumpVelocity` | -280 | More negative means higher jumps and longer airtime. |

On level ground, jump height is approximately `jumpVelocity² / (2 × gravity)`
(56 pixels) and airborne travel is approximately
`maxHorizontalSpeed × 2 × abs(jumpVelocity) / gravity` (112 pixels).
Collision steps and the 12×20 body affect platform-edge reach.

Space triggers only a new press while grounded. Holding or releasing it does
not change jump height. Opposing horizontal inputs cancel. There is no coyote
time, jump buffering, double jump or jump cut.
