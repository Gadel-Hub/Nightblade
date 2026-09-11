# Native visual anchor review

Run `npm run dev`, then press **V** to switch between the mechanics laboratory
and art showcase. `/#art` starts directly in the showcase. **R** resets the
player and Guard; A/D/arrows, Space, J and F1 retain their existing meanings.

The showcase has two platform heights, a tall wall, an open Guard encounter,
an active cutting comb and a retracted static comb specimen farther right.
Health cells and the small normal/hit/dead icon report existing gameplay state.
There are no new mechanics or timed hazard behaviors. Enemy and player damage,
knockback, attack phases, detection ranges and movement values are unchanged.

The original invisible physics images still own the 12×20 player and 14×24
Guard bodies. Separate scale-1 images use the approved foot pivots and rounded
render positions. The other laboratories retain their programmer visuals.
V sleeps/wakes scenes without rebuilding their fixtures; keyboard state clears
on wake to avoid carrying a held movement key across the switch.

Player pose selection: idle 0; run 1–2 at 8 fps; jump 3; fall 4; wall 5;
startup/recovery 6; active 7; hurt 8. Guard: idle/recovery 0; patrol 1–2 at
4 fps; startup 3; active 4; death 5. These are held or alternating art frames,
not clocks for damage. Swing facing remains latched, and wall poses face the wall.

Active tools use two subrectangles of the existing active pose (shaft and tip).
The native one-pixel-wide shaft columns repeat to the accepted hitbox's outer edge;
the hooked tip is placed once, unscaled. This makes actual melee reach readable
without new exports, enlarging hitboxes or creating an effects system. F1 shows
the full damage rectangles, which intentionally remain taller than the tool.

Review at 1× and integer enlargement: the amber courier's back panel, raised
wall hand, stride extremes, coiled startup, extended cutting tool and backward
hurt pose; the teal Guard's wider chest and raised anticipation; coral teeth
against violet floor planes; recessed dark checker frames; empty health cells.
Nonlethal enemy hits do not cause a stagger; its folded death pose is the anchor
for damage/death readability. No remaining enemy or production-level art is authorized.

Source/edit/export/validation instructions are in `art/README.md`. All six PNGs
match the accepted dimensions in `ART_ASSET_SPEC.md`; no runtime generation of anchor art
or image-model output is included. The anchor still requires human visual approval.
