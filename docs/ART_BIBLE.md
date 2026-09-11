# Art bible

Status: visual direction specification; the anchor requires manual approval
before producing remaining sprites or level tiles. Existing laboratory shapes
are programmer art, not examples of this finished style.

## Raster and export rules

- Game image: 320×180 physical source pixels. Environment grid: 16×16.
- Author at native resolution. One source pixel equals one game pixel.
- Runtime asset scale is exactly 1; display enlargement is integer nearest
  neighbor. No rotations other than 90-degree turns of decorative grid marks.
- PNG RGBA exports, alpha only 0 or 255. No color profiles, gradients, blur,
  antialiasing, intermediate alpha, compression artifacts or filtered resizing.
- Transparent pixels do not count as a palette color. Every opaque pixel must
  match one of the sixteen values below exactly. No tint multiplication.
- Player frame canvas: 24×32; Guard: 32×32. Feet align at Y=29. Keep identical
  frame canvases and origins; do not crop each pose independently.
- Preserve the player's 12×20 and Guard's 14×24 collision bodies independently
  of sprite canvas size. Visual pose changes never resize collision/hurtboxes.
- Environment frames are 16×16. Larger props comprise whole tiles, with no
  additional detail density. HUD cells are 8×8; glyphs occupy at most 3×5.

## Master palette

These are the entire opaque color vocabulary, not illustrative suggestions.

| ID | Name | Hex | Role |
| --- | --- | --- | --- |
| 0 | Ink | `#101522` | Character outlines, empty space, UI field |
| 1 | Night | `#22283D` | Far background, internal outline breaks |
| 2 | Slate | `#3A4560` | Background structures, tile shadow |
| 3 | Steel | `#66728A` | Solid structural faces and inactive mechanisms |
| 4 | Bone | `#E8DEC2` | Faces, tool edges, HUD glyphs |
| 5 | Spark | `#FFF3D1` | Tiny hazard-tip and active-strike highlights |
| 6 | Amber | `#E6AD45` | Player body identity; enemy anticipation accent |
| 7 | Ochre | `#9A6639` | Player body shadow |
| 8 | Sea glass | `#68B8B0` | Guard front planes; sparse system accents |
| 9 | Deep teal | `#31766F` | Guard body shadow; circuit surface |
| A | Coral | `#E56B6F` | Active danger, hostile weapon edge |
| B | Wine | `#983C55` | Hazard shadow, enemy damage pose |
| C | Lilac | `#A28ACB` | Level 1 constructed surface edges |
| D | Violet | `#635A91` | Level 1 modular faces |
| E | Lime | `#A4BD69` | Level 3 signals; restrained Level 2 status marks |
| F | Moss | `#536B45` | Level 3 board layers |

Player uses Ink, Amber, Ochre and Bone; Spark is allowed only on the active
weapon edge. Guard normally uses Ink, Deep teal, Sea glass and Bone. During
anticipation replace Sea glass highlights with Amber; do not brighten the
whole silhouette. Coral is reserved for dangerous geometry/hostile strikes,
not harmless color swatches. The player never changes identity palette by level.

Maximum four opaque colors per ordinary 16×16 tile or character frame. An
active weapon may add Spark or Coral as a fifth. HUD uses Ink, Bone, Amber and
Wine. Do not use all sixteen colors in one object.

## Pixel clusters, outlines and light

Use a one-pixel Ink exterior contour on characters and dangerous mechanisms.
Break at most two adjacent contour pixels at the upper-left highlight; never
break the feet or the weapon-facing edge. Internal separations use Ink only
where necessary to distinguish limbs; otherwise use the local shadow color.

Light comes from upper left. Each material has one body color and at most one
shadow color. Highlights are short straight bars or 2×2 corner clusters, not
specular ramps. No dithering on characters. No isolated decorative single-pixel
noise; isolated pixels are allowed for eyes, weapon tips and status indicators.
Diagonals use deliberate 1:1 or 2:1 steps. Avoid pillow shading and thin parallel
highlight bands that imply a higher color-depth image.

The darkest background is Night against Ink recesses. Background objects use
Night/Slate, no bright outline, and no solid-looking continuous top lip.
Walkable geometry has a continuous light top edge plus a dark underside.
Character silhouettes must remain distinguishable over both Slate and Violet.
Leave at least a character-width of quiet background around tricky jumps.

## Character language

Player: an original compact courier in a rigid amber overshirt, beveled cream
faceplate and dark jointed legs. The head is offset toward the facing side;
a short angular back panel makes the rear silhouette distinct. No cloth mask,
headband, trailing scarf, katana silhouette or copied heroic pose. The hand
carries a short flat cutting tool, with a hooked tip rather than a long sword.
The eye opening is a two-pixel dark notch on the forward faceplate edge.

The torso is about 8 pixels wide, head 7×6, ordinary visible standing height
22–24 pixels. Hands and boots are two-pixel clusters. Preserve a one-pixel gap
between separated limbs. Attack frames must move the forward arm and torso,
not merely add a floating slash to an idle pose. Wall contact places the forward
hand above the head, knee toward the wall, rear leg bent away; the head still
faces the wall. Hurt bends the torso backward and pulls feet forward.

Representative enemy: Guard only. A squat teal articulated sentry, broad
rectangular chest, small recessed faceplate, split feet and a hinged forearm
cutter. The silhouette is wider and more level across the shoulders than the
player. Startup raises the cutter over the forward shoulder and compresses the
knees; active extends it horizontally with the torso leaning forward. Death
folds the torso into the legs, with Wine replacing the front highlight. Do not
add a gameplay stagger to display a damage pose.

Other enemies remain placeholders. Later: the Ranged silhouette should have a
fixed upright emitter, Pursuer a low forward wedge, Heavy a broad low base.
These are silhouette constraints only, not authorization to produce their art.

## Economical animation

| Action | Anchor frames | Convention |
| --- | --- | --- |
| Player idle | 1 | Held; no mandatory breathing loop |
| Player run | 2 | Opposing stride extremes, 8 frames/second while moving |
| Player jump / fall | 1 each | Tucked knees ascending; extended feet descending |
| Player wall contact | 1 | Held while touching/sliding; orient to contacted wall |
| Player slash | 2 | Coiled startup; extended active; reuse coiled pose for recovery |
| Player hurt | 1 | Held for existing hit reaction |
| Guard idle / patrol | 1 / 2 | Idle held, patrol alternates at 4 frames/second |
| Guard startup / active | 1 each | Held for existing state durations |
| Guard death | 1 | Folded pose for existing death duration, then disappears |

Animation is selected from gameplay state; it never decides when a hit occurs.
Existing phase durations remain authoritative. No interpolation, squash/stretch,
tweened rotation, generated in-between frames, subpixel offsets or afterimages.
Horizontal flipping is allowed for all anchor poses and weapon art. No unique
left-facing frames are required. Lighting flips with the pose as an intentional
hardware-era simplification. Pose registration must stay fixed through flips.

## Environment and hazard anchor

Level 1's sample group is a physical assembly of colored blocks, inset frame
panels and stacked plates. Ground uses Slate/Violet faces, Lilac top lips and
Ink seams. Repeat large planes; vary structural joints rather than adding noise.
Wall tiles use vertical inset bars and visible 16-pixel courses. Platform ends
have capped sides and an Ink underside. A background frame contains a sparse
4-pixel checker motif in Night/Slate; it is visibly recessed and non-solid.
One decorative swatch column uses bounded Sea glass/Lilac/Amber inserts, no
menus, application chrome, cursor pointer or readable tool names.

Hazard: a raised toothed cutting comb emerging from a slotted block bed, like
a physical raster cutting tool. A 16×16 active tile has three stepped teeth,
Ink gaps, Wine body, Coral cutting faces and at most three Spark tip pixels.
The inactive art retracts every tooth below a flat Steel rim; it contains no
Coral or Spark. Active danger must read by silhouette as well as color. The
anchor may show the inactive state as a static specimen; do not add timed
hazard behavior to accepted mechanics solely to animate the specimen.

## HUD

Use square inset health cells, not glossy hearts, rounded cards or gradients.
Three existing health units are three 8×8 cells: Ink border, Amber filled core,
Bone upper-left corner. Empty cell uses a Wine inner notch. A small state icon
may represent normal, hit and dead; it must report existing state only.
If numerals are needed use a hand-pixelled 3×5 alphabet, 1-pixel spacing. No
browser fonts in the finished HUD. Keep a four-pixel screen margin. Debug text
and programmer laboratory labels are exempt artwork and remain behind F1
where practical; they are never examples of final environmental lettering.

## Three distinct levels

| Level | Dominant material / accents | Geometry and readable motifs |
| --- | --- | --- |
| Visual design | Slate, Violet, Lilac; sparse Sea glass/Amber | Layered plates, inset checker recesses, grouped swatches, frame strips, visibly assembled square blocks |
| Software systems | Night, Slate, Steel; Sea glass/Lime nodes | Stacked modules, branching walkways, bounded object frames, small connected node clusters, broken links represented by gaps |
| Embedded systems | Deep teal, Moss, Slate; Lime/Bone traces | Thick board strata, vias, buses, pin rows, chip-like pillars, physically exposed signal paths |

Level 1 favors offset layers and horizontal strips; Level 2 favors branching
connections and stacked rectangles; Level 3 favors dense routed lines and
vertical component silhouettes. Increase electrical density near the eventual
boss, not detail resolution. All retain the same outline, lighting, player,
hazard, HUD and animation rules. Tiny schematic glyphs are 2–4 pixels tall,
unreadable as sentences and occupy no more than one quarter of a background
panel. No source-code walls, documentation, brands, logos, real chip labels,
software interfaces or recognizable existing game imagery.

## Deliverable boundaries and review

Runtime exports belong in `public/assets/player`, `enemies/guard`,
`environment/visual-design`, and `ui`. Editable native sources belong in
`art/source` if distinct from exports. Names describe purpose and pose; no
random suffixes, unused alternates or contact sheets loaded as game textures.
Frame order and pivot/body offsets must be documented beside the files.
Any provenance/concept material is separate from approved runtime art.

Approve only after viewing the anchor at 1× and integer enlargement, over dark
and midtone backgrounds, moving and attacking, touching both walls, taking a
hit, and beside the Guard's anticipation/attack poses. Inspect PNG dimensions,
binary alpha and exact palette membership, then inspect silhouettes by eye.
Passing a palette check alone does not establish art quality. Reject smoothed
or inconsistently sized pixels, unclear facing, noisy shading or a borrowed
silhouette. If suitable raster output cannot meet these checks, retain clean
programmer art and report the anchor as incomplete; do not relabel it finished.
