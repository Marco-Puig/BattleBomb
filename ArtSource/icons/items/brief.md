# Brief: item icons (`item-icon/<definition id>`)

**What:** icons for the 10 items that have none yet. Each item's icon is joined to it by its
definition id (PROVENANCE §6).

| Id | Item | Folder | Batch |
|---|---|---|---|
| 2 | Leather Chestplate | `leather-chestplate/` | A |
| 3 | Leather Boots | `leather-boots/` | A |
| 4 | Steel Helmet | `steel-helmet/` | A |
| 5 | Steel Chestplate | `steel-chestplate/` | A |
| 6 | Steel Boots | `steel-boots/` | A |
| 7 | Hunting Knife | `hunting-knife/` | B |
| 8 | Hunting Bow | `hunting-bow/` | B |
| 10 | Terrier (pet: +max health) | `terrier/` | C |
| 11 | Lucky Charm (equipment: +crit chance) | `lucky-charm/` | C |
| 13 | Ember Stone (equipment: a Fire burst active) | `ember-stone/` | C |

**Not in this brief:** your three hand-made icons (Leather Helmet 1, Health 9, Mana 12). They
stay as they are. They use the wobbly marker line and the new icons use clean vector, so the set
will mix two styles until you decide whether to restyle those three yourself. Batch A draws a
leather helmet **only so the leather set matches**; its draft is reference, not a replacement.

**Before you start:** SF6 (the icon style frame) must be approved. Every batch attaches it.
Batch B also needs the approved held-weapon sheet (`ArtSource/weapons/brief.md`), so each weapon
icon matches the weapon in hand.

## Rules for every icon

- **One icon serves every quality rank.** The sack's cell frame carries the rank colour, and a
  "Rusty" and a "Shiny" Steel Helmet share this picture (D33: materials are name flavour). Draw
  each item **new and plain**: not battered, not gleaming, no rank colours.
- **Materials read as sets:** leather is warm brown with stitching and buckles; steel is cool grey
  with rivets and rolled edges. The helmet, chest and boots of one material share their motifs.
- **Same camera for all:** three-quarter view. Weapons sit on a diagonal, lower-left to upper-right.
- **The subject fills about 80% of its square**, centred.
- **It must read at 64 px.** Clear silhouette, few details.
- The art bible §10 checklist applies.

**Pipeline:** save each batch into `ArtSource/icons/items/AI/_raw/` (`batch-a.png` and so on).
The Art lane cuts each icon onto a 512×512 transparent square, saves it as
`<folder>/AI/<folder>__ai_v1.png` (for example `steel-helmet/AI/steel-helmet__ai_v1.png`), and
sends back a contact sheet at 64 px and 256 px for approval.

---

## Batch A: armour (6 icons, 5 kept)

**Attach:** your approved SF6.

```
Six game item icons for armour, in a 3x2 grid, each centred in its own equal square, all drawn
at the same size and the same three-quarter view, matching the style of the attached image
exactly.
Top row, a LEATHER set, warm brown leather with visible stitching and small brass buckles:
(1) a leather cap-style helmet, (2) a leather chestplate (a sleeveless padded vest with shoulder
straps), (3) a pair of leather boots.
Bottom row, a STEEL set, cool grey polished steel with rivets and rolled edges:
(4) a rounded steel helmet with a nose guard, (5) a steel chestplate (a breastplate with a
rounded chest), (6) a pair of steel boots (armoured sabatons).
All six items are new and plain: not damaged, not glowing, no coloured gems or trim.
Style: clean vector cartoon: bold smooth near-black (#1A1917) outline (thick outer silhouette,
thinner inner lines), flat colours with one hard-edged shadow tone, one small highlight, light
from directly above, no gradients, no texture. Each icon must read clearly at 64 pixels.

Output: a single image, landscape 1536x1024, transparent background, the six icons evenly spaced
with clear margins, no text, no labels, no watermark, no border.
```

## Batch B: weapons (2 icons)

**Attach:** (1) your approved SF6, (2) your approved held-weapon sheet.

```
Two game weapon icons side by side, each centred in its own equal square, both on a diagonal
from lower left to upper right, matching the style of the first attached image exactly and the
weapon designs of the second attached image exactly:
(1) a hunting knife: a short, broad steel blade with a single sharp edge, a wooden handle wrapped
in brown leather, and a small brass guard;
(2) a hunting bow: a simple curved wooden bow wrapped with leather at the grip, a taut
string, and no arrow.
Both are new and plain: no glow, no gems.
Style: clean vector cartoon: bold smooth near-black (#1A1917) outline (thick outer silhouette,
thinner inner lines), flat colours with one hard-edged shadow tone, one small highlight, light
from directly above, no gradients, no texture. Each icon must read clearly at 64 pixels.

Output: a single image, landscape 1536x1024, transparent background, both icons with clear
margins, no text, no labels, no watermark, no border.
```

## Batch C: pet and trinkets (3 icons)

**Attach:** your approved SF6.

```
Three game item icons in a row, each centred in its own equal square, same size, three-quarter
view, matching the style of the attached image exactly:
(1) a small, scruffy, cheerful terrier dog sitting upright, tongue out, a red collar: a
companion pet;
(2) a lucky charm: a four-leaf clover set in a small round brass pendant on a short cord;
(3) an ember stone: a fist-sized dark volcanic stone cracked open, with glowing red-orange
(#FF4A1C) fire inside the cracks and a pale yellow (#FFF1B0) hot core, a few sparks rising.
Style: clean vector cartoon: bold smooth near-black (#1A1917) outline (thick outer silhouette,
thinner inner lines), flat colours with one hard-edged shadow tone, one small highlight, light
from directly above, no gradients, no texture. Each icon must read clearly at 64 pixels.

Output: a single image, landscape 1536x1024, transparent background, the three icons evenly
spaced with clear margins, no text, no labels, no watermark, no border.
```

*Design choices in this batch are the Art lane's reading of existing item names, not new lore:
a four-leaf clover for "Lucky Charm", a cracked volcanic stone for "Ember Stone". Change them
freely.*
