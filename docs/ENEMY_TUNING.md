# Enemy tests

`src/enemies/tuning.ts` contains enemy health, speeds (pixels/second), ranges
(pixels), and attack/death durations (seconds). Higher range detects/reaches
farther; higher speed closes distance faster; longer startup gives more warning,
longer active keeps danger present, and longer recovery leaves more punish time.
Damage uses the existing player hit reaction and knockback values unchanged.

Press 1 for the Guard bay. Number keys reset the selected test; death respawns
there and resets its enemies. R returns to movement, C to the dummy laboratory.
F1 shows enemy health, state, hurtboxes and active attack rectangles.

Guard patrols a fixed short segment, commits its facing at startup, slashes
once, then recovers. Gold means startup; a red rectangle is the active attack.
There is no contact damage. One attack can damage the player at most once,
including when the player is invulnerable. Each player swing hits each enemy
once using the established target-identity set. Lethal hits immediately remove
enemy attacks; a brief gray dead state precedes body/visual/collider cleanup.
