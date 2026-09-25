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

**Pipeline:** save each result into `ArtSource/_raw/` (see GPT_PROMPTS.md). The Art lane files it,
and `prepare.py` here makes it seamless, measures the ground, and saves it as
`AI/fixture-<piece>__ai_v2.png`.

**v2 (Michael, 2026-09-24):** the first round came out as semi-realistic anime landscapes with
pale hazy colour, "not what was originally in my head". He wants the **storybook** world: painted
and glowing, but with simplified storybook shapes and rich, bright colour. Round 2 rewrites the
style block and attaches the new storybook style frame **SF3b** instead of SF3.

**Before you start:** SF3b must be approved. Attach it to every prompt.

---

## The environment style block (also in the art bible, §9)

```
Environment style: a whimsical storybook world painted for a 2D side-scrolling game. Simplified,
exaggerated, rounded shapes (bulbous trees, curving hills, chunky rocks) with clean painted edges,
rich saturated colour, bold colour contrast between layers, and a soft magical glow. Hand-painted
but clearly stylised, never realistic: no photographic detail, no fine textures, no realistic
leaves or grass blades, no hazy washed-out distance. Distant layers are simpler and cooler (vivid
purples, teals and blues), near layers warmer and more saturated (greens and golds). Softer than
the game's bold-outlined characters and with no black outlines, so the characters and their
shadows stand out.
```

## 1. Ground (square 1024×1024)

```
A seamless, tileable ground texture seen from directly above, for the walkable floor of a
side-scrolling game: a soft, simple mix of light, bright green grass and light golden sandy path,
painted in a few large, clean patches of flat colour with gentle painted edges, evenly lit, LOW
contrast. No dark patches, holes, stones, shadows, flowers, grass blades or fine texture, so a
character's dark shadow on it is always the darkest thing in view. The left edge must continue
seamlessly into the right edge, and the top into the bottom.
Match the ground colours of the attached image, but lighter and calmer.
Environment style: a whimsical storybook world painted for a 2D side-scrolling game. Simplified,
exaggerated, rounded shapes (bulbous trees, curving hills, chunky rocks) with clean painted edges,
rich saturated colour, bold colour contrast between layers, and a soft magical glow. Hand-painted
but clearly stylised, never realistic: no photographic detail, no fine textures, no realistic
leaves or grass blades, no hazy washed-out distance. Distant layers are simpler and cooler (vivid
purples, teals and blues), near layers warmer and more saturated (greens and golds). Softer than
the game's bold-outlined characters and with no black outlines, so the characters and their
shadows stand out.

Output: a single image, square 1024x1024, no text, no watermark, no border.
```

## 2. Backdrop far (landscape 1536×1024)

```
A wide painted background strip for a side-scrolling game: a bright, clear sky in rich blue with
a few big, puffy, simply shaped clouds, and a range of distant mountains with rounded, exaggerated
peaks along the lower third, painted in vivid purples and teals, not grey and not hazy. The left
edge must continue seamlessly into the right edge so the strip can repeat. Nothing in the
foreground, and no buildings, castles, bridges or waterfalls: just sky, clouds and mountains.
Match the sky and distance of the attached image.
Environment style: a whimsical storybook world painted for a 2D side-scrolling game. Simplified,
exaggerated, rounded shapes (bulbous trees, curving hills, chunky rocks) with clean painted edges,
rich saturated colour, bold colour contrast between layers, and a soft magical glow. Hand-painted
but clearly stylised, never realistic: no photographic detail, no fine textures, no realistic
leaves or grass blades, no hazy washed-out distance. Distant layers are simpler and cooler (vivid
purples, teals and blues), near layers warmer and more saturated (greens and golds). Softer than
the game's bold-outlined characters and with no black outlines, so the characters and their
shadows stand out.

Output: a single image, landscape 1536x1024, no text, no watermark, no border.
```

## 3. Backdrop mid (landscape 1536×1024)

```
A painted background layer for a side-scrolling game: gently rolling hills with big, round,
lollipop-like storybook trees and a few glowing flowers, filling the lower half of the image, in
rich teal-greens; everything above the hills is fully transparent. No buildings, castles, bridges
or waterfalls. The left edge must continue seamlessly into the right edge so the layer can repeat.
Match the hills and trees of the attached image.
Environment style: a whimsical storybook world painted for a 2D side-scrolling game. Simplified,
exaggerated, rounded shapes (bulbous trees, curving hills, chunky rocks) with clean painted edges,
rich saturated colour, bold colour contrast between layers, and a soft magical glow. Hand-painted
but clearly stylised, never realistic: no photographic detail, no fine textures, no realistic
leaves or grass blades, no hazy washed-out distance. Distant layers are simpler and cooler (vivid
purples, teals and blues), near layers warmer and more saturated (greens and golds). Softer than
the game's bold-outlined characters and with no black outlines, so the characters and their
shadows stand out.

Output: a single image, landscape 1536x1024, transparent background above the hills, no text, no
watermark, no border.
```

## 4. Backdrop near (landscape 1536×1024)

```
A painted layer for a side-scrolling game: a low, continuous row of round, chunky storybook
bushes and a few simple flowers along the bottom third of the image, seen from the side, in bright
warm greens and golds; everything above is fully transparent, and nothing sits below the row's
base. Medium-light values, nothing near black. The left edge must continue seamlessly into the
right edge so the row can repeat.
Environment style: a whimsical storybook world painted for a 2D side-scrolling game. Simplified,
exaggerated, rounded shapes (bulbous trees, curving hills, chunky rocks) with clean painted edges,
rich saturated colour, bold colour contrast between layers, and a soft magical glow. Hand-painted
but clearly stylised, never realistic: no photographic detail, no fine textures, no realistic
leaves or grass blades, no hazy washed-out distance. Distant layers are simpler and cooler (vivid
purples, teals and blues), near layers warmer and more saturated (greens and golds). Softer than
the game's bold-outlined characters and with no black outlines, so the characters and their
shadows stand out.

Output: a single image, landscape 1536x1024, transparent background, no text, no watermark, no
border.
```

## 5. Foreground frames (square 1024×1024)

```
Two separate foreground plant clumps for the edges of a side-scrolling game screen, side by side
with clear space between them, on a transparent background: (1) a tall clump of big, simple,
rounded leaves leaning right, (2) a lower, wider clump of rounded leaves and a few curly stems
leaning left. They are close to the camera, so they are darker (deep teal-green), larger and
simpler than the background, drawn as a few bold shapes rather than detailed foliage.
Environment style: a whimsical storybook world painted for a 2D side-scrolling game. Simplified,
exaggerated, rounded shapes (bulbous trees, curving hills, chunky rocks) with clean painted edges,
rich saturated colour, bold colour contrast between layers, and a soft magical glow. Hand-painted
but clearly stylised, never realistic: no photographic detail, no fine textures, no realistic
leaves or grass blades, no hazy washed-out distance. Distant layers are simpler and cooler (vivid
purples, teals and blues), near layers warmer and more saturated (greens and golds). Softer than
the game's bold-outlined characters and with no black outlines, so the characters and their
shadows stand out.

Output: a single image, square 1024x1024, transparent background, no text, no watermark, no
border.
```
