# Brief: hero template, the knight test skin (`rig/hero-template`)

**What:** the 15 body-part drawings of the template's first skin: the knight from style frame
SF1, cut into the parts of `docs/art/HERO_TEMPLATE.md`, **with the Templar's proportions
unchanged**. He is a stand-in for building and animating the rig at M9, not one of the four
heroes (those wait for the story bible).

**Before you start:** SF1 must be approved. Both prompts attach it.

**The pipeline:**
1. Run **Sheet A** (the 8 base parts), then **Sheet B** (the 7 variants: expressions and hand
   poses), in the same chat as your approved SF1.
2. Save the results into `ArtSource/rig/hero-template/AI/_raw/` as `sheet-a.png` and
   `sheet-b.png`.
3. **The Art lane cuts them.** Each part is cut out, scaled, and placed on its fixed canvas with
   its joint on the pivot (`HERO_TEMPLATE.md` §4), then saved as
   `AI/hero-template-<part>-<label>__ai_v1.png` (for example
   `hero-template-hand-front-grip__ai_v1.png`; single-drawing parts use `default`). You get back
   an assembled preview of the knight built from the cut parts, to approve.
4. Parts that come out wrong (a missing joint cap, a wrong scale) get fixed by editing in the
   same chat: "Keep everything; redraw only the back leg so it is the same length as the front
   leg."

**Resolution note:** at this sheet size the knight is about 800 px tall, so parts are scaled up
about 2× onto the 1600-px canvases. That is fine for a draft. Your redraw, being vector, exports
sharp.

---

## Sheet A: the 8 base parts

**Attach, in this order:** (1) your approved SF1 image, (2)
`ArtSource/rig/hero-template/guides/parts-sheet-a.png` (the grey layout guide).

```
Make a body-parts sheet for a 2D cut-out animation rig of the knight in the first attached
image. Draw his body parts completely separated from one another, laid out exactly like the
second attached image: each grey shape there shows the position and rough size of one part.
Replace each grey shape with the knight's matching part, keeping his proportions exactly as in
the first image and the same scale for every part, as if the knight had been taken apart like a
paper puppet.

Top row: his huge helmeted head (left); his short, round torso without head, arms or legs
(right).
Middle row: his front arm hanging straight down from the shoulder to the wrist; his back arm,
the same; his big round front hand, relaxed and open; his back hand, the same.
Bottom row: his short front leg with its boot, hanging straight down from the hip, the boot
pointing right; his back leg with its boot, the same.

Rules for every part:
- Draw each part whole, including the end that would be hidden under the next part: every arm
  and leg top, and the bottom of the head, finishes in a full round cap, like a ball joint.
- Arms and legs hang straight down; boots point to the right; the head faces three-quarters to
  the right, the same as in the first image.
- The back arm, back hand and back leg are one shade darker than the front ones.
- No part overlaps or touches another; leave clear empty space between all parts.
- No sword: the weapon is drawn separately.

Style: match the first attached image exactly: same outline weight and colour (warm near-black
#1A1917), same flat colours with a single hard-edged shadow tone, light from directly above, no
gradients, no texture, no ground shadow.

Output: a single image, portrait 1024x1536, transparent background, no grey guide shapes left
visible, no text, no labels, no watermark, no border.
```

## Sheet B: the 7 variants

**Attach, in this order:** (1) your approved SF1 image, (2) the approved Sheet A result, (3)
`ArtSource/rig/hero-template/guides/parts-sheet-b.png`.

```
Using the knight from the first image and his body parts from the second image, make a second
body-parts sheet with alternate drawings of his head and hands, laid out exactly like the third
attached image (each grey shape shows the position and rough size of one part). Every part must
be the same scale and the same style as in the second image.

Row 1: his head with a fierce, determined attacking expression (left); his head flinching in pain
(right). His face is hidden by the helmet, so show the feeling through the eye slit, the tilt of
the helmet and small motion marks.
Row 2 (centre): his head knocked out, dizzy and slumped.
Row 3: his front hand as a clenched fist; his front hand gripping a handle (an empty grip, no
weapon); his back hand as a fist; his back hand gripping, the same.

Rules for every part:
- Each head is exactly the same size and shape as the head in the second image; only the
  expression and tilt change.
- The back hands are one shade darker than the front ones.
- No part overlaps or touches another; leave clear empty space between all parts.

Style: match the first and second attached images exactly: same outline weight and colour (warm
near-black #1A1917), same flat colours with a single hard-edged shadow tone, light from directly
above, no gradients, no texture, no ground shadow.

Output: a single image, portrait 1024x1536, transparent background, no grey guide shapes left
visible, no text, no labels, no watermark, no border.
```

---

## The redraw (for later)

The hand version goes in `Hand/`: one Illustrator master, `hero-template__hand_v1.ai`, with one
artboard per drawing at the `HERO_TEMPLATE.md` §4 sizes and a crosshair on each pivot. Export
each artboard as `hero-template-<part>-<label>__hand_v1.png`. Keep the AI parts on a locked,
hidden reference layer (PROVENANCE §3).
