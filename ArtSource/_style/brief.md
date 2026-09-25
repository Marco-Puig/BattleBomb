# Style frames: brief and prompts

**What these are:** six throwaway test images that pin down the look before any real asset is
drafted. They never ship, and they invent nothing. The only character is the Templar Knight
(your own existing art), and the enemies and the stage are generic stand-ins.

**How to run them (ChatGPT images):**
1. Run them **in order, in one chat.** SF1 sets the style, and every later frame attaches an
   approved earlier one.
2. When a result is close but wrong, **edit rather than re-roll**: "Keep everything the same;
   change only …".
3. Save each keeper into `ArtSource/_style/AI/_raw/` as `sf1-….png`, `sf2-….png` and so on, and
   note anything you changed in the prompt below it.
4. Judge each one against the checklist in `docs/art/ART_BIBLE.md` §10.
5. If a "transparent" background comes back white or as a painted checkerboard, ask once more
   ("the background must be truly transparent, with an alpha channel"). If it still fails, keep
   the image anyway; I can strip a flat background.

---

## SF1: the character style (the one that matters most)

**Tests:** line weight, flat colour with one shadow tone, top-down light, big-head proportions.
**Attach:** `Assets/_BattleBomb/Art/Sprites/Characters/Templar Knight/PNG/PNG Sequences/Idle/Idle_000.png`

```
Redraw the knight in the attached image as new game art. Keep his design AND his proportions
exactly as they are: the huge great helm with the gold cross on its face, about two-thirds of his
height, sitting straight on his short, round body; the white tabard with the red cross; the grey
armour; the big round hands and short stubby legs. Only the drawing style changes, to match the
style below.

Pose: standing at rest, body turned three-quarters toward the right, feet apart and planted,
a short sword held point-down in his front hand, relaxed but ready.

Style: clean 2D vector cartoon art for a side-scrolling action game. Bold, smooth, clean outlines
in warm near-black (#1A1917): the outer silhouette line is thick, about 1/36 of the character's
height; inner detail lines are half that. Flat, saturated fill colours, each with exactly one
hard-edged shadow tone about 25% darker and slightly cooler, and at most one small highlight.
Light comes from directly above, so the shading is symmetrical left to right. No gradients, no
texture, no noise, no painted ground shadow. Big-head proportions: the head is about two-thirds
of the total height and sits straight on a short, round body with no visible neck; short arms
with big round hands; short stubby legs with small boots. Friendly, expressive, bouncy.

Output: a single image, portrait 1024x1536, transparent background, the whole subject inside the
frame with a clear margin, nothing cropped, no text, no watermark, no border.
```

## SF2: the same style in action

**Tests:** whether the style holds in an energetic pose (the Rayman bounce).
**Attach:** your approved SF1.

```
Match the style of the attached image exactly: same outline weight and colour, same flat colours
and single shadow tone, same proportions. Draw the same knight in a new pose: mid-swing of an
overhead sword chop, leaning hard into the swing, back foot lifted, tabard flying behind him,
with strong squash-and-stretch energy. Keep light from directly above and no ground shadow.

Output: a single image, portrait 1024x1536, transparent background, the whole subject inside the
frame with a clear margin, nothing cropped, no text, no watermark, no border.
```

## SF3: a gameplay moment (the checkpoint for "not a clone")

**Tests:** characters against a lush world, shadow readability (D14), and whether it reads as its
own game. **Attach:** your approved SF1.
**Judge:** turn the result greyscale. Can you find every shadow at a glance?

```
A mock gameplay screenshot from a side-on 2.5D co-op action game. The camera looks at the scene
from the side and slightly above.

The world: a lush, painterly, softly lit grassy meadow built as a 3D stage. A broad, flat walkable
ground strip runs across the lower half of the image; that is where the fighting happens. Behind
it: rolling hills, big rounded trees, glowing flowers and distant mountains that grow paler and
bluer with distance. At the far left and right edges, a few dark leafy plants in the foreground
frame the shot without covering the ground strip. The ground strip itself is a light, even,
low-contrast grass-and-dirt surface: no dark patches and no shadows painted on it. The
environment is softer, richer and lower in contrast than the characters.

The action: two chunky cartoon knights in the style of the attached image, one in blue and
silver and one in red and gold, fight four small, round, featureless placeholder enemies
(simple grey-green blob creatures with eyes). The characters stand at different depths on the
ground strip. Every character casts a crisp, hard-edged, dark shadow directly beneath them on
the ground; those shadows are the darkest shapes on the ground, so it is obvious where each one
stands.

Characters: match the style of the attached image exactly (same outline weight and colour, same
flat colours and single shadow tone). Environment: painterly and soft, never outlined.

Output: a single image, landscape 1536x1024, no UI, no text, no watermark, no border.
```

## SF4: the four elemental climates

**Tests:** §6 of the bible, where the element sets a region's light while the play band stays
readable. **Attach:** your approved SF3.

```
Show the scene in the attached image four times in a 2x2 grid. Keep the layout, characters and
camera identical in every panel; change only the lighting, colour grade and small atmospheric
effects.
Top left, FIRE: warm orange-red key light from a low sun, heat haze, floating embers.
Top right, ICE: pale blue-white soft light, frost along the edges of plants, light snowfall.
Bottom left, EARTH: dusty ochre light, drifting dust, small pebbles.
Bottom right, AIR: crisp white light from a high sun, streaming leaves, wind streaks.
In every panel the walkable ground strip stays light and even, so the characters' hard dark
shadows remain clearly readable.

Output: a single image, landscape 1536x1024, no text or labels, no watermark, no border.
```

## SF5: the four signature casts (D46)

**Tests:** each element's shape language and colours (bible §5).
**Attach:** your approved SF1.

```
Four spell effects for a side-scrolling action game, arranged in a 2x2 grid with generous empty
space between them, each seen from the side and travelling to the right:
1. FIRE (top left): a line of flames bursting up along the ground, red-orange (#FF4A1C) flame
   tongues with pale yellow-white cores (#FFF1B0) and deep red shadows (#9E1B0F), embers rising.
2. ICE (top right): a long, fast bolt of ice flying straight ahead, sharp faceted crystal, sky
   blue (#59BFFF) with white-cyan highlights (#E8FBFF) and deep blue shadows (#1F6FA8), a short
   frosty trail.
3. EARTH (bottom left): a short wall of chunky, angular rocks erupting out of the ground, ochre
   stone (#8C6B40) with pale sand highlights (#E3C99A) and dark umber shadows (#4A3320), pale
   dust puffs at its base and no marks on the ground.
4. AIR (bottom right): a curling gust of wind lifting upward, pale mint-white swooshes (#D9F2FF)
   with teal-grey edges (#5FA3A0), spiral lines and a few carried leaves.
Style: match the attached image's clean vector cartoon look. Solid shapes (rocks, ice) get the
bold near-black (#1A1917) outline; flames and wind use bold flat shapes with no outline. Flat
colours with one hard-edged shade, bright cores, simple graphic shapes. No gradients, no
photographic effects. None of the effects is a ring on the ground or a vertical beam of light.

Output: a single image, square 1024x1024, transparent background, each effect fully inside its
quarter with a clear margin, no text, no labels, no watermark, no border.
```

## SF6: the item icon style

**Tests:** the icon look for the sack and shop. Your existing icons use a wobbly marker line; the
new ones follow the clean-vector choice. **Attach:** your approved SF1.

```
Four game item icons in a single row, each centred in its own equal square and drawn at the same
size and the same three-quarter view:
1. a steel knight's helmet, 2. a pair of brown leather boots, 3. a wooden hunting bow with a
taut string, 4. a red health potion in a round glass bottle with a cork.
Style: match the attached image's clean vector cartoon look: bold smooth near-black (#1A1917)
outline (thick outer silhouette, thinner inner lines), flat saturated colours with one hard-edged
shadow tone, one small highlight, light from directly above, no gradients, no texture. Each icon
must read clearly when shrunk to 64 pixels.

Output: a single image, landscape 1536x1024, transparent background, the four icons evenly spaced
with clear margins, no text, no labels, no watermark, no border.
```
