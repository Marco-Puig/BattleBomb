# Brief: held weapons (`weapon/<definition id>`)

**What:** the weapon sprites the rig holds in `hand-front`, separate from the sack icons: the
Hunting Knife (id 7), and the Hunting Bow (id 8) with its arrow. Canvas, pivot and orientation:
`docs/art/HERO_TEMPLATE.md` §9. The template's test knight fights with the Hunting Knife, the
only sword-class item.

**Before you start:** SF1 and icon Batch B must be approved. The held weapon should look like
its icon.

**Pipeline:** save the sheet into `ArtSource/weapons/AI/_raw/sheet.png`. The Art lane cuts each
piece onto its canvas with the grip on the pivot, as `weapons/hunting-knife/AI/hunting-knife__ai_v1.png`,
`weapons/hunting-bow/AI/hunting-bow__ai_v1.png` and `weapons/hunting-bow/AI/hunting-bow-arrow__ai_v1.png`.

---

## The weapon sheet

**Attach, in this order:** (1) your approved SF1, (2) your approved icon Batch B.

```
Three separate weapon sprites for a 2D side-scrolling game, seen flat from the side, matching
the style of the first attached image and the designs of the second attached image exactly.
Arrange them in a row of three equal cells, each centred with clear empty space, not touching:
(1) the hunting knife standing straight up, point at the top, handle at the bottom, sharp edge
facing right;
(2) the hunting bow standing straight up, its string on the left side and the curve of the bow
bulging to the right, no arrow;
(3) a single arrow lying horizontally, pointing right, with a steel tip and pale feather
fletching.
All three are new and plain: no glow, no gems.
Style: clean vector cartoon: bold smooth near-black (#1A1917) outline (thick outer silhouette,
thinner inner lines), flat colours with one hard-edged shadow tone, one small highlight, light
from directly above, no gradients, no texture.

Output: a single image, landscape 1536x1024, transparent background, no hand holding them, no
text, no labels, no watermark, no border.
```
