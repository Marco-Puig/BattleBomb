# Brief: effect kits (`effect/<kit>`)

**What:** the drawings every effect is built from. Unity animates them (`docs/art/EFFECTS.md`
§1), so each prompt asks for **separate still pieces on one sheet**, never animation frames.
What each effect does, how long it lasts, and its rules: `docs/art/EFFECTS.md`.

**Before you start:** SF5 (the four casts) must be approved. The element kits attach it.

**Pipeline:** save each sheet into `ArtSource/effects/AI/_raw/` (`fire.png`, `ice.png` and so
on). The Art lane cuts each piece onto its canvas (EFFECTS.md §6) as
`effects/<kit>/AI/<kit>-<piece>__ai_v1.png`, and sends back a contact sheet for approval.

**The shared rules, already written into each prompt:** facing and moving right; outlines only
on solid things (rock, ice, the heart); flat colour, one shade and a bright core; nothing dark on
the ground; no rings with vertical beams.

---

## Fire (`effect/fire`, 7 pieces)

**Attach:** your approved SF5.

```
A sheet of separate fire effect pieces for a side-scrolling action game, matching the fire in
the attached image exactly. A 4x2 grid of equal cells, one piece per cell, each piece centred
with clear empty space around it, no piece touching another:
Top row: (1) a tall licking flame tongue, (2) a second flame tongue leaning right, (3) a short
wide flame tongue, (4) a round burst of flame exploding outward.
Bottom row: (5) a single glowing ember, (6) a small flame that could sit on a character's
shoulder, (7) a spark: a small burst of flame shards and embers, (8) leave empty.
Colours: red-orange (#FF4A1C) flames, pale yellow-white (#FFF1B0) hot cores, deep red (#9E1B0F)
shadow tone. Style: bold flat graphic shapes with no outline, one hard-edged shade, bright
cores, no gradients, no smoke, nothing on the ground.

Output: a single image, landscape 1536x1024, transparent background, no text, no labels, no
watermark, no border.
```

## Ice (`effect/ice`, 8 pieces)

**Attach:** your approved SF5.

```
A sheet of separate ice effect pieces for a side-scrolling action game, matching the ice in the
attached image exactly. A 4x2 grid of equal cells, one piece per cell, each piece centred with
clear empty space around it, no piece touching another:
Top row: (1) a long, sharp ice bolt pointing right, (2) a large faceted ice shard, (3) a medium
shard, (4) a small shard.
Bottom row: (5) a soft puff of frost mist, (6) a single snowflake, (7) a crust of frost and small
icicles, wide and low, as if coating the ground around a character's feet, (8) a spark: a small
burst of ice splinters.
Colours: sky blue (#59BFFF), white-cyan highlights (#E8FBFF), deep blue shadow tone (#1F6FA8).
Style: solid ice pieces (bolt, shards, crust) have a bold near-black (#1A1917) outline; mist,
snowflake and spark have no outline. Flat colours, one hard-edged shade, crisp highlights, no
gradients.

Output: a single image, landscape 1536x1024, transparent background, no text, no labels, no
watermark, no border.
```

## Earth (`effect/earth`, 8 pieces)

**Attach:** your approved SF5.

```
A sheet of separate earth effect pieces for a side-scrolling action game, matching the rocks in
the attached image exactly. A 4x2 grid of equal cells, one piece per cell, each piece centred
with clear empty space around it, no piece touching another:
Top row: (1) a large chunky angular rock, (2) a medium rock, (3) a small rock, (4) a tall rock
pillar with a pointed top, as if erupting from the ground.
Bottom row: (5) a shorter, wider rock pillar, (6) a pale, soft puff of dust, (7) a single small
pebble, (8) a spark: a burst of rock chips.
Colours: ochre stone (#8C6B40), pale sand highlights (#E3C99A), dark umber shadow tone
(#4A3320); the dust is pale sand, never dark. Style: rocks, pillars and pebble have a bold
near-black (#1A1917) outline; dust and spark chips have none. Flat colours, one hard-edged shade,
no gradients, no cracks or marks on any ground.

Output: a single image, landscape 1536x1024, transparent background, no text, no labels, no
watermark, no border.
```

## Air (`effect/air`, 7 pieces)

**Attach:** your approved SF5.

```
A sheet of separate wind effect pieces for a side-scrolling action game, matching the wind in
the attached image exactly. A 4x2 grid of equal cells, one piece per cell, each piece centred
with clear empty space around it, no piece touching another:
Top row: (1) a long curling swoosh of wind moving right, (2) a swoosh curling upward, (3) a short
tight swoosh, (4) a spiral of wind like a small whirlwind.
Bottom row: (5) a single leaf tumbling, (6) a long thin speed line, (7) a spark: a tight burst
of short swooshes, (8) leave empty.
Colours: pale mint-white (#D9F2FF) with teal-grey edges and shade (#5FA3A0) so it reads against a
bright sky; the leaf is fresh green. Style: bold flat graphic shapes with no outline, one
hard-edged shade, no gradients.

Output: a single image, landscape 1536x1024, transparent background, no text, no labels, no
watermark, no border.
```

## Melee (`effect/melee`, 10 pieces)

**Attach:** your approved SF2 (the knight mid-swing).

```
A sheet of separate melee combat effect pieces for a side-scrolling action game, in the flat
graphic style of the attached image. A 5x2 grid of equal cells, one piece per cell, each piece
centred with clear empty space around it, no piece touching another. Every piece faces right.
Top row: (1) a crescent sword-swipe arc, level; (2) a crescent arc slashing down diagonally;
(3) a crescent arc slashing up diagonally; (4) a much wider, thicker crescent for a heavy
blow; (5) a crescent sweeping upward in a rising curve.
Bottom row: (6) a small hit star, white with a pale yellow core; (7) a bigger hit star with
short speed lines; (8) the biggest, sharpest hit star, bold and spiky, for a critical hit;
(9) a small pale dust puff; (10) a flatter, wider pale dust puff.
Colours: the arcs are white with a faint pale-blue tint and a bright leading edge; stars are
white and pale yellow; dust is pale sand. Style: bold flat shapes with no outline, one hard
shade, no gradients, nothing dark.

Output: a single image, landscape 1536x1024, transparent background, no text, no labels, no
watermark, no border.
```

## Loot glow (`effect/loot`, 4 pieces, greyscale)

**Attach:** nothing. These are plain light shapes, tinted by the game.

```
Four separate light-glow textures for a game, in pure greyscale (white on transparent) because
the game tints them with colour. Arrange them in a row of four equal cells, each centred, not
touching:
(1) a soft glowing ring seen at a low angle, so it is a flat ellipse, bright at its rim and
fading softly in and out; (2) a tall vertical beam of light, brightest in its centre line and
fading softly to its sides and top; (3) a four-pointed sparkle star; (4) a smaller six-pointed
sparkle.
Style: clean and simple, soft light falloff only, no outline, no colour, no texture.

Output: a single image, landscape 1536x1024, transparent background, no text, no labels, no
watermark, no border.
```

## Feedback (`effect/feedback`, 5 pieces)

**Attach:** your approved SF1.

```
A sheet of separate game feedback effect pieces, matching the style of the attached image. A row
of five equal cells, one piece per cell, each centred with clear empty space, not touching:
(1) a bright golden starburst of rays, as if a hero just levelled up; (2) a column of small
rising golden sparkles; (3) a plump cartoon heart, red with a bold near-black (#1A1917)
outline, one hard-edged shade and a white highlight; (4) a bright pulse burst of short radiating
lines, pale pink-white, as if the heart just beat; (5) a small sputtering puff of grey-lilac
smoke, as if a spell fizzled.
Style: flat colours, one hard-edged shade; only the heart is outlined; no gradients except the
soft glow of the burst.

Output: a single image, landscape 1536x1024, transparent background, no text, no labels, no
watermark, no border.
```
