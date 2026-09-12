# Movement validation handoff

**Pending:** open the project with Unity **6000.3.24f1**, resolve packages, and
confirm no import/compile errors or missing references. Open `MovementLab` and
enter Play mode. `BootstrapLab` remains the unchanged static rendering scene.
No runtime checks below were performed in the implementation environment.

A/D or arrows move; Space jumps. F1 toggles the development overlay. Its collider
wireframe is visible with Gizmos enabled. F1 is read in fixed updates and tooling
starts hidden. Disable `Movement Diagnostics (F1)` to remove it; it is inactive
in non-development builds. The camera uses plain tracking only to keep the
course visible. Restart Play mode to reset the test; no respawn system is present.

## Course guide

Positions below are world X. Hierarchy objects use the listed `TEMP_` names.
Terrain has explicit colliders and separate tiled placeholder visuals, mostly
on the one-unit grid; half/quarter-unit dimensions serve the ceiling and shaft tests.

| Area | Geometry / use |
| --- | --- |
| X 0–5 | Flat start; Player spawns at (2, 0.3125). Test response, direction changes, jump height, and landing. |
| X 5–8 | ShortPlatform (0.5 high), RaisedPlatform (1 high): top/side/corner transitions. |
| X 9–12 | LowCeiling underside at Y 1.5: walk under, then jump into it. |
| X 14–17 | Three-unit normal-jump gap. Lower recovery floor and half-unit steps permit returning after a miss. |
| X 22–28 | TallWall starts at Y 1 so it can be entered underneath; OppositeWall creates a 1.5-unit shaft. Alternate wall jumps and exit over the shorter right wall to WallJumpExit. |
| X 34–39 | NarrowShaftLeft/Right leave 0.75 units between them. Player width is 0.375, so opposite-wall contact can end the push before 0.12 s. Enter underneath the left wall and exit right. |
| X 42–45 | One-unit-wide EdgeLandingPlatform and RaisedEdgeLanding: land near corners and run off edges. |

## Manual checks

- [ ] A/D and arrows reach ±4.375 units/s immediately in open space. Release stops
  horizontal movement. Opposite inputs cancel, including A + Right Arrow.
- [ ] Space on ground produces the fixed jump. Compare trajectory to the Phaser
  reference; ideal continuous apex is +1.75 units after 0.4 s, with discrete-step
  differences expected. Press duration does not change height.
- [ ] Hold Space through landing: no automatic next jump. Press and release in
  free air: no second jump. Press before landing: no stored jump on landing.
- [ ] Flat landings reliably become grounded; wall sides and ceiling contacts do
  not. Run off platform edges: no delayed/coyote jump. Jump immediately after a
  confirmed landing; check no stale contact permits repeated airborne jumps.
- [ ] Hit the ceiling: upward travel stops and falling resumes without sticking,
  bouncing, false grounding, or a new jump opportunity.
- [ ] Observe independent left/right contact flags on each wall, including bottom
  and top corners. Centered free-air apex is neither grounded nor wall contact.
- [ ] Falling while pressing into a wall caps downward velocity at −1.71875.
  Release/press away: uncapped falling resumes. Rising against a wall is not sliding.
- [ ] Jump from a left wall: +5.3125 X and upward launch +8.4375 before that step's
  gravity. Repeat from a right wall with −5.3125 X and the same Y impulse.
- [ ] Neutral wall contact still permits a wall jump; sliding is not required.
- [ ] Rapidly reverse input after a wall jump: the push continues away for nominal
  0.12 s, then control returns. F1 shows remaining commitment. Compare this feel
  with the POC, allowing for fixed-step quantization.
- [ ] In the narrow shaft, reach the other wall during the push. Control releases
  early and a new press jumps from that wall; no lock traps the player.
- [ ] Jump away, return to the same wall without landing, and press again: rejected.
  Merely touching the opposite wall does not clear the remembered side; jumping
  from it does. Alternating wall jumps work repeatedly.
- [ ] Land after wall jumps: both the remembered wall and outward timer clear.
  Ordinary running/jumping still work. Repeat both shafts several times and
  compare symmetry, sliding, and landing with the reference.
- [ ] Miss the normal gap: recovery floor and steps let the player return without
  restarting. Complete the gap and both wall-jump exits.
- [ ] Repeat at different rendering frame rates (e.g. 30, 60, 120) while physics
  remains 60 Hz. Check missed/duplicated presses, edge contacts, slide cap, and lock.
- [ ] F1 hides/shows position, velocities, ground/walls, state, and lock history.
  Disabling diagnostics does not affect movement; normal play has no debug overlay.
- [ ] Replace the prefab Visual's sprite with a differently sized sprite. The
  collider remains 0.375×0.625 and all movement behavior remains unchanged.

Record editor version, platform, frame rate, observed differences, and acceptance
in the review. Keep movement `porting` until the maintainer accepts it. Combat is
blocked by this human gate; Web builds and deployment are not part of this check.
