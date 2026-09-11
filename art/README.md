# Native artwork

`source/*.json` is the editable artwork: each string is one native pixel row.
`.` is transparent; `0`–`F` refer to `palette.json`, copied exactly from the
approved art bible. Frame names/order follow `docs/ART_ASSET_SPEC.md`.
Edit these grids directly. No tracing, resizing, filtering or color quantization
is performed. Player feet register at (12,29); Guard at (16,29).

`manifest.json` fixes each export's dimensions and frame size.

- `npm run assets:export` writes the PNGs to `public/assets`, one cell per pixel.
- `npm run assets:validate` checks all registered exports without modifying them.
- `npm run test:assets` checks valid round trips and deliberate corruptions.
- `npm run build` validates existing exports before compiling the game.

The development-only PNG codec is `pngjs`; it is not imported by the game.
The validator checks dimensions, CRC, exact palette membership, binary alpha,
source row/index validity, per-frame color budgets, and exact decoded pixels
against the source. A rescaled/repainted PNG fails even if its dimensions and
palette were restored afterward. Error messages identify the file and pixel.
It cannot judge silhouette or pose quality; those require native-resolution
visual review. Only registered exports are final assets. Add future approved
exports to the manifest with their exact specs, rather than weakening checks.
