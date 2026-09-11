# Visual anchor asset specification

Use the exact palette and rules in `ART_BIBLE.md`. This is the complete limited
anchor inventory; it is not a production asset backlog.

| Export | Native size | Frames, in order |
| --- | --- | --- |
| `player/courier.png` | 216×32, 9 cells of 24×32 | idle, run-a, run-b, jump, fall, wall, startup, slash, hurt |
| `enemies/guard/guard.png` | 192×32, 6 cells of 32×32 | idle, walk-a, walk-b, startup, attack, death |
| `environment/visual-design/structure.png` | 96×16, 6 cells of 16×16 | ground top, ground fill, platform left, platform right, wall, inset background |
| `environment/visual-design/swatch.png` | 16×32 | Single two-tile decorative column |
| `environment/visual-design/cutting-comb.png` | 32×16, 2 cells of 16×16 | retracted, active |
| `ui/health.png` | 40×8, 5 cells of 8×8 | full, empty, normal-state, hit-state, dead-state |

All PNGs use exact opaque palette colors plus binary transparency. Cell sizes
include transparent margins; no padding between cells. No text on sprite sheets.
Do not export these files until the images pass native-grid and palette checks.

Player pivot: (12,29), feet registered to the bottom of the existing 12×20 body.
Guard pivot: (16,29), feet registered to the bottom of the existing 14×24 body.
Use separate visual images at scale 1 if changing the existing physics image
would change body size or origin. Attack art must follow latched swing direction,
not current input. Weapon accents cannot enlarge the existing attack rectangle.

The showcase is a small optional laboratory, not a stage. It should use the
existing player controller, combat/damage model and one existing Guard. It needs
a short walkable run, two heights and a wall, the comb with existing damage
semantics, a recessed background and the health cells. A static retracted-comb
specimen is sufficient to show the second state. Leave all other enemy art and
all existing mechanical test fixtures unchanged.

Do not use runtime gradients, browser-font HUD text, fractional positioning of
the art layer, scaled source pixels or color tints to fill missing artwork.
Final artwork requires visual review in addition to mechanical validation.
