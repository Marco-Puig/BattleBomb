# Art bible

**v0 · 2026-09-24 · Art lane. Draft for Michael's sign-off (ROADMAP §6, needed by M9).**
Makes D16 (bold-outline cartoon), D15/D47 (2D billboard characters in a lit 3D world), and D14
(shadows are the depth cue) concrete enough to check a prompt or a drawing against.

Michael's calls (2026-09-24): **references are Castle Crashers and Rayman Legends · clean vector
outlines · chunky, big-headed heroes.**

---

## 1. The look in one line

> **Castle Crashers-style characters living in a Rayman Legends world.**

The characters are clean-vector cartoons: bold near-black outlines, flat saturated colour, one
hard shadow tone. They move through **lush, painterly, softly lit 3D environments** that are
deeper and richer than the characters, but lower in contrast. The contrast is the point: flat,
crisp characters over soft, rich worlds, so a fighter and their shadow always stand out.

## 2. Not a Castle Crashers clone (D16's flagged debt)

Clean vector outlines on chunky characters is exactly Castle Crashers' character style, so **the
characters alone will read close to it.** That is accepted; the difference has to be carried
elsewhere, deliberately:

1. **The world.** Castle Crashers' backgrounds are flat painted backdrops. Ours are lit 3D stages
   with real depth, atmosphere, and Rayman-style lushness: layered foliage, light shafts, glow.
2. **The elemental climate is visible** (§6). A region's element colours its light and weather,
   something Castle Crashers has no equivalent of.
3. **One cel shadow tone and an engine rim light** on characters, where Castle Crashers is
   nearly flat.
4. **Character design and palette,** which come from the story bible. Heroes wear their
   element's colour family.

**Checkpoint:** judge this at the first gameplay style frame (SF3). If it reads as a lookalike
there, the fix is in the world and the palette, not in abandoning clean lines.

---

## 3. Characters

### Proportions
- **Heroes:** the head is about **a third to two-fifths** of total height (roughly 2.5 heads
  tall). Big hands and big feet, because poses read through them. Thin wrists, ankles and neck,
  which hide the joints on a cut-out rig (D16).
- The Templar Knight rig is the **line-weight** reference, not the proportion reference. Its
  head is about 60% of its height, far chunkier than this.
- **Enemies vary by archetype**, so a crowd reads at a glance (GAME_DESIGN §6): the grunt is small
  and round, the ranged enemy lanky, the caster tall with a hat or staff breaking its silhouette,
  the brute huge and blocky. Regions reskin them (after the story bible); the silhouettes stay.

### Line
- **One ink colour: warm near-black `#1A1917`**, measured from the Templar Knight. Never pure
  black, never coloured lines.
- **Two weights.** The outer silhouette line is about **1/36 of the character's height** (about
  3%, again the Templar's measure). Interior detail lines are **half** that.
- Lines are **clean, smooth vector**: even width along a stroke, round ends, no sketchiness.

### Colour and shading
- **Flat, saturated fills.** Each colour gets **exactly one hard-edged shadow tone**, about 25%
  darker and shifted slightly cooler, plus at most one small highlight. **No gradients, no
  texture.**
- **Light comes from directly above.** Facing left is a mirror flip of facing right (D47), so
  shading drawn from one side would swap sides whenever a character turns. Light from above
  stays right both ways.
- **Never paint a ground shadow under a character.** The engine casts it from an invisible
  capsule (D15), and a painted one would fight it.
- A hero's dominant colour is their **element's colour family** (§5). Their other colours come
  from the story bible.

### How characters are lit in the engine (a recommendation for M9's lighting session)
**Unlit sprites with the cel shading painted in, plus two engine touches:** a light tint toward
the region's climate colour (§6), and an optional rim light. D15 offers normal maps instead, but
AI drafts cannot produce consistent normal maps, and every hand redraw would need them drawn too,
doubling the redraw cost for a look D16 does not want anyway.

### Readability test (every character)
Fill the character solid black and shrink it to **64 px tall**. You should still be able to tell
who it is and what it's doing.

---

## 4. Environments: the Rayman half

- **Three depth zones.**
  - **Backdrop** (far): atmospheric, paler and cooler with distance, lower contrast.
  - **Play band**: the ground plane where fights happen (the depth band, GAME_DESIGN §2.1).
  - **Foreground**: occasional dark framing elements at the screen edges, never over the play band.
- **Lush and painterly**: rounded, layered shapes, soft painted textures, light shafts, glowing
  details, saturated colour. The environment is **softer and lower-contrast than the
  characters**, never crisper.

### Shadow readability: rules, not taste (D14, GAME_DESIGN §2.3)
The grounded shadow is the game's only depth cue, so nothing in the play band may compete with it.

1. **The play-band floor is light and even.** In a greyscale screenshot it sits between **55% and
   85% brightness**, and its texture detail stays within about **±10%** of that.
2. **In the play band, only outlines and grounded shadows are darker than 45%.** There are no
   dark patches, holes, decals, or painted shadows.
3. **Scenery shadows stay out of the play band, or stay soft and faint there.** A character's
   hard-edged shadow must be the only hard-edged shadow a player stands on.
4. **The check:** take a greyscale screenshot of any stage with two players and a crowd. Every
   shadow should be findable at a glance.

---

## 5. Colour

### Ink and loot (fixed)
- Ink: `#1A1917`.
- The quality ladder's colours are fixed in `Gameplay/Loot/QualityColors.cs`: Nothing `#8c8c8c`,
  Battlescarred `#8a8a8f`, Rusty `#a75c28`, Torn `#f2ede2`, Clean `#5fbf4f`, Shiny `#2b62d9`,
  Pristine `#ffd83a`, Legendary `#ff7a1a`, Mythical `#a855f7`, Godly `#73e6ff` (reserved).
- The UI palette belongs to the Claude Design project "BATTLEBOMB — UI PASS 01".

### Elements: the clash, and the rule that settles it
**Finding.** Each element's current colour (`_color` in `Data/Elements/*.asset`) sits next to a
quality colour. Fire `#FF7326` is almost exactly Legendary `#ff7a1a`. Ice `#59BFFF` sits between
Shiny and Godly, Earth `#8C6B40` near Rusty, and Air `#D9F2FF` near Torn. A ten-colour loot
ladder uses most of the colour wheel, so **hue alone can never keep elements and loot apart.**

**Rule: separate them by form and place, not only by hue.**
- **Loot owns one shape:** the drop's glow is a **ring on the ground plus, at the top of the
  ladder, a vertical beam of light** (GAME_DESIGN §5.4). No elemental effect ever uses a ground
  ring with a vertical beam.
- **Elements own their shapes** (below): flame tongues, ice shards, rocks, swirls. A loot glow is
  never shaped like one.
- **One colour change (proposal): Fire moves from `#FF7326` to red-orange `#FF4A1C`**, because
  Fire and Legendary are the only near-identical pair. Flames still run red to yellow; the change
  only shifts which end dominates. It is a data change to `Fire.asset` for the Builder at M9.

| Element | Base | Light (core, highlight) | Dark (shadow tone) | Shape language | Status look |
|---|---|---|---|---|---|
| **Fire** | `#FF4A1C` *(proposed; now `#FF7326`)* | `#FFF1B0` | `#9E1B0F` | Licking flame tongues, rising embers, heat shimmer | **Burn:** small flames on the target, embers rising |
| **Ice** | `#59BFFF` | `#E8FBFF` | `#1F6FA8` | Angular shards, faceted crystal, frost rings | **Chill:** frost crust on the feet, a cool tint, slow drifting flakes |
| **Earth** | `#8C6B40` | `#E3C99A` | `#4A3320` | Chunky angular rocks, dust puffs, ground cracks | **Stun** (a property of the hit): small rocks orbiting the head |
| **Air** | `#D9F2FF` | `#FFFFFF` | `#5FA3A0` | Curling swooshes, spirals, speed lines, carried leaves | **Launch** (a property of the hit): an upward swirl under the target |

Air is pale on purpose (wind is nearly invisible). It relies on its **teal-grey dark tone and its
swoosh shapes** to read against bright skies.

---

## 6. Elemental climate: the world's colour (D41)

A region's climate element sets its **key light and weather**. Each is story-independent;
which region gets which element comes from the story bible.

| Climate | Key light | Atmosphere |
|---|---|---|
| Fire | warm orange-red, low sun | heat haze, floating embers, dark warm backdrop |
| Ice | pale blue-white, soft | frost on edges, light snow, pale cool backdrop |
| Earth | ochre, dusty | drifting dust, pebbles, warm hazy backdrop |
| Air | crisp white, high sun | streaming leaves, wind streaks, bright open sky |

Whatever the climate, **§4's play-band rules hold.** Weather never darkens or clutters the
ground players stand on.

---

## 7. Resolution

- **Masters are vector** (Illustrator), so they have no resolution. AI drafts come out at
  ChatGPT's largest size (1536 px on the long edge).
- **The largest a character is ever shown** is the hero panel: the camera fills **72% of the
  screen's height** with the character (`_screenFill` 0.72). At 4K that is about **1,550 px
  tall**, so hand exports should put the character at **about 1,600 px tall**. AI drafts reach
  roughly 1,400 px, which is fine for drafts.
- Gameplay sizes are smaller. M9 measures them on screen and sets the per-platform import size
  (D47 left resolution targets to the art pass).

---

## 8. Animation feel (for M9)

A Rayman bounce on Castle Crashers timing: snappy anticipation, **squash and stretch** through
bone scaling, overlapping follow-through on loose parts (a tabard, hair, a scarf). The state list
comes from the combat machine's phases (ROADMAP M9), never from the art.

---

## 9. Writing prompts for ChatGPT images

- **Every prompt is four parts:** the subject, the pose or layout, the **style block** (verbatim,
  below), and the output line.
- **Never name another game or artist in a prompt.** Naming Castle Crashers pulls the output
  toward copying it, the exact risk D16 flags, and ChatGPT may refuse or dilute named-style
  requests. Describe the qualities instead.
- **Consistency:** once a style frame is approved, **attach it to every later prompt** with
  "Match the style of the attached image exactly: same outline weight and colour, same flat
  colours and single shadow tone."
- **Iterate by editing, not re-rolling:** "Keep everything the same; change only …".
- **Save** the chosen output into the asset's `AI/_raw/`, and keep the exact prompt in its
  `brief.md` (PROVENANCE §2).
- **Sizes:** characters are portrait 1024×1536; a single icon or an effect sheet is square
  1024×1024; scenes and rows of icons are landscape 1536×1024.
- **Transparent backgrounds sometimes fail**: the background comes back white, or as a
  checkerboard painted into the image. Ask once more ("the background must be truly transparent,
  with an alpha channel"). If it still fails, keep the image; the Art lane can strip a flat
  background.

### The character style block (paste verbatim)
```
Style: clean 2D vector cartoon art for a side-scrolling action game. Bold, smooth, clean outlines
in warm near-black (#1A1917): the outer silhouette line is thick, about 1/36 of the character's
height; inner detail lines are half that. Flat, saturated fill colours, each with exactly one
hard-edged shadow tone about 25% darker and slightly cooler, and at most one small highlight.
Light comes from directly above, so the shading is symmetrical left to right. No gradients, no
texture, no noise, no painted ground shadow. Chunky proportions: the head is about a third to
two-fifths of the total height, with big hands and big feet and slim wrists and ankles. Friendly,
expressive, bouncy.
```

### The output line (paste verbatim, choosing the size)
```
Output: a single image, [portrait 1024x1536 | square 1024x1024], transparent background, the whole
subject inside the frame with a clear margin, nothing cropped, no text, no watermark, no border.
```

The first prompts to run are the **style frames**: `ArtSource/_style/brief.md`.

---

## 10. The checklist: every prompt, every draft, every drawing

- [ ] Ink `#1A1917`, smooth; silhouette line about 1/36 of the height, interior lines half.
- [ ] Flat colours, one hard shadow tone, light from above, no gradients or texture.
- [ ] Heroes: head a third to two-fifths of the height; big hands and feet.
- [ ] Readable as a solid black silhouette at 64 px.
- [ ] No painted ground shadow.
- [ ] Element colours and shapes from §5; nothing uses loot's ring-and-beam.
- [ ] Environments: the play band is light and even, with nothing dark enough to pass for a shadow.
- [ ] The prompt names no game or artist, and nothing in it invents lore (D56).
- [ ] Anything that is not a full scene is on a transparent background.
