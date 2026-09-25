# Brief: fixture environment kit (`environment/fixture`), fixture-only

**What:** the smallest environment that lets M9 prove the environment pipeline and the art
bible's shadow-readability rules (bible §4, GAME_DESIGN §2.3) on the fixture chapter
(`Scenes/Stages/FixtureStage1/2`). **It is a plain, generic meadow with no region identity. It
will be replaced when the story bible's regions exist, and it does not ship.**

**The space it dresses** (from the game): the walkable depth band is **6 u deep** (z −3 to +3).
Fixture arenas are up to **16 u wide**, and a hero is **2 u tall**. The camera looks from the
side, slightly above.

## The six pieces

| Piece | What it is | How M9 uses it |
|---|---|---|
| `ground` | A **seamless, tileable** top-down texture for the walkable floor: light, even, low-contrast grass and dirt | Tiled across the depth band's floor; **the surface the grounded shadows land on** |
| `backdrop-far` | Sky, distant mountains and clouds: a wide painted strip, **seamless left to right** | A flat card far behind the band, at a real 3D distance |
| `backdrop-mid` | Rolling hills and rounded trees on a transparent sky, seamless left to right | A card between the far layer and the band |
| `backdrop-near` | A low row of bushes and flowers on a transparent background, seamless left to right | A card just behind the band's back edge. **It never extends onto the walkable floor.** |
| `frame-a`, `frame-b` | Two dark leafy plant clumps on transparent backgrounds | Foreground cards at the screen edges. **They never cover the band.** |

The backdrop layers sit at **real 3D depths**, so any parallax comes from the camera itself,
never from a scripted scrolling trick. Faked parallax is how the 2019 build lost its depth cue
(D14).

## Acceptance: measured, not eyeballed

The Art lane measures `ground` before it goes anywhere: **in greyscale its brightness sits
between 55% and 85%, and its variation stays within about ±10%** (bible §4 rule 1). Being AI
art, it may be levelled to fit, which is still AI (PROVENANCE §3). ChatGPT's "seamless" images
often show a faint seam; the Art lane can blend one away.

**Pipeline:** save each result into `ArtSource/environments/fixture/AI/_raw/`. The Art lane
checks and cleans each one, then saves it as `AI/fixture-<piece>__ai_v1.png`.

**Before you start:** SF3 must be approved, and SF4 helps. Attach SF3 to every prompt.

---

## The environment style block (it moves into the bible once SF3 is approved)

```
Environment style: lush, painterly 2D background art for a side-scrolling game: soft rounded
shapes, layered foliage, gentle painted texture, saturated but soft colour, warm sunlight, a hint
of glow. No outlines. Lower contrast and softer than the game's bold-outlined characters, so the
characters and their shadows stand out against it.
```

## 1. Ground (square 1024×1024)

```
A seamless, tileable ground texture seen from directly above, for the walkable floor of a
side-scrolling game: a soft mix of short light-green grass and light sandy dirt, evenly lit,
LOW contrast, with no dark patches, holes, stones, shadows, flowers or strong details, so a
character's dark shadow on it is always the darkest thing in view. The left edge must continue
seamlessly into the right edge, and the top into the bottom.
Environment style: lush, painterly, soft, gentle painted texture, no outlines, soft saturated
colour. Match the ground colour of the attached image, but lighter and calmer.

Output: a single image, square 1024x1024, no text, no watermark, no border.
```

## 2. Backdrop far (landscape 1536×1024)

```
A wide painted background strip for a side-scrolling game: a bright soft sky with a few big
rounded clouds, and a range of distant, pale blue-violet mountains along the lower third,
softened by atmospheric haze. The left edge must continue seamlessly into the right edge so the
strip can repeat. Nothing in the foreground, and no buildings, castles, bridges or waterfalls:
just sky, clouds and mountains.
Environment style: lush, painterly 2D background art, soft rounded shapes, gentle painted
texture, saturated but soft colour, warm sunlight, no outlines. Match the sky and distance of the
attached image.

Output: a single image, landscape 1536x1024, no text, no watermark, no border.
```

## 3. Backdrop mid (landscape 1536×1024)

```
A painted background layer for a side-scrolling game: gently rolling green hills with big,
rounded, lush trees and a few glowing flowers, filling the lower half of the image; everything
above the hills is fully transparent. No buildings, castles, bridges or waterfalls. Slightly hazy
and softer than a foreground would be. The
left edge must continue seamlessly into the right edge so the layer can repeat.
Environment style: lush, painterly 2D background art, soft rounded shapes, layered foliage,
gentle painted texture, saturated but soft colour, warm sunlight, no outlines. Match the hills
and trees of the attached image.

Output: a single image, landscape 1536x1024, transparent background above the hills, no text, no
watermark, no border.
```

## 4. Backdrop near (landscape 1536×1024)

```
A painted layer for a side-scrolling game: a low, continuous row of lush bushes, tall grass
and small flowers along the bottom third of the image, seen from the side; everything above is
fully transparent, and nothing sits below the row's base. The left edge must continue seamlessly
into the right edge so the row can repeat.
Environment style: lush, painterly 2D background art, soft rounded shapes, layered foliage,
gentle painted texture, saturated but soft colour, warm sunlight, no outlines, medium-light
values (nothing near black).

Output: a single image, landscape 1536x1024, transparent background, no text, no watermark, no
border.
```

## 5. Foreground frames (square 1024×1024)

```
Two separate foreground plant clumps for the edges of a side-scrolling game screen, side by side
with clear space between them, on a transparent background: (1) a tall clump of broad dark-green
leaves and ferns leaning right, (2) a lower, wider clump of dark leaves and a few stems leaning
left. They are close to the camera, so they are darker, larger and slightly less detailed than
the background.
Environment style: lush, painterly, soft rounded shapes, no outlines.

Output: a single image, square 1024x1024, transparent background, no text, no watermark, no
border.
```
