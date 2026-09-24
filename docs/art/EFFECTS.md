# Effects: what each shows, for how long, and what it is built from

**v0 · 2026-09-24 · Art lane.** Covers ROADMAP M9's effect list: hits per element, the four
signature casts, aura, leap, statuses, level-up, and the revive heartbeat, plus the drop glow
and the melee basics. Timings and sizes come from the game data: **60 steps a second**, and
**a hero is 2 world units (u) tall**. Prompts: `ArtSource/effects/brief.md`.

---

## 1. The approach: a sprite kit that Unity animates

ChatGPT draws single, consistent images well. It cannot draw frame-by-frame animation that
stays consistent from frame to frame. So **every effect is built from a kit of single drawings**
(flame tongues, ice shards, rocks, swooshes, sparks), and **Unity's particle systems move,
scale, spin and fade them** (M9's job). A hand redraw replaces the drawings, and the motion
stays as it is.

Frame-by-frame flipbooks are allowed where a kit cannot do the job. v0 needs none.

---

## 2. Rules for every effect

1. **Outline only solid things:** rocks, ice, the heart. Flames, wind, sparks, dust and light are
   flat shapes with no outline (bible §5, SF5).
2. **Flat colour, one shade, a bright core.** Only light (glows, beams, flashes) may fade softly;
   nothing else uses a gradient.
3. **Element effects use their element's palette and shapes** (bible §5).
4. **Nothing an effect leaves on the ground is darker than the floor, or outlives the effect.**
   No scorch marks, craters, or dark cracks (bible §4: nothing dark in the play band).
5. **No ground effect is a steady ring with a vertical beam.** That shape belongs to loot
   (bible §5). The aura is a ring too, but it is huge, lasts half a second, and is made of
   element pieces. The loot ring is small, steady, and plain light.
6. **No effect covers a grounded shadow for longer than a flash.**
7. **Draw everything facing right**; the game mirrors it for the left.
8. Every effect fires from a simulation event both machines see. That is M9's side (online).

---

## 3. Per element: the Magic kit (D39, D46) and statuses (D40)

| | Fire | Ice | Earth | Air |
|---|---|---|---|---|
| **Signature cast** (press) | Flames erupt along the ground in a line **3.2 u ahead** (1.6 hero heights). Builds over 10 steps (0.17 s), burns for 4, then dies down in about 0.4 s. Hits up to 3. | A bolt flies straight ahead **in the caster's lane at 14 u/s** (7 hero heights a second), leaving a frost trail. It shatters into shards on its one target. | A short wall of rocks **erupts 1.6 u ahead**, builds over 14 steps (0.23 s), stands for 4, then crumbles in about 0.3 s into pale dust. Hits up to 3. | A curling gust sweeps **2.4 u ahead** and lifts enemies skyward (launch speed 11). Hits up to 4. |
| **Aura** (stick down + press) | A ring of flames bursts outward along the ground to **2.6 u radius** (1.3 hero heights), then fades. Seen from the side, the circle is a flat ellipse on the floor. It expands in about 0.25 s and fades in 0.3 s. | A frost nova: shards and frost puffs spread outward to the same radius. | A shockwave: rock chunks pop up in a widening ring, then fall. | A whirlwind: swooshes spin outward around the caster. |
| **Leap** (press in the air) | A burst of flame under the feet at liftoff, then a short ember trail that follows the rise. | A burst of shards at liftoff, then a frost trail. | A burst of rock chips at liftoff, then a dust trail. | A burst of wind at liftoff, then a swirl trail. |
| **Status on the target** (players too, D40) | **Burn, 3 s**: small flames on the body and rising embers, with a flare on each damage tick (every 0.5 s). | **Chill, 3 s** (movement ×0.55): a frost crust at the feet, flakes drifting, and a cool tint on the sprite (engine side). | **Stun, 0.75 s** (a property of the hit, not a lasting mark): small pebbles orbit above the head. | **Launch** (a property of the hit): a swirl under the target as it rises. |
| **Hit spark** (casts and infused weapons) | A burst of flame tongues and embers, about 0.15 s | Shards bursting outward | Rock chips and a dust puff | A tight swoosh burst |

---

## 4. Melee and movement (no element)

| Effect | What it shows | Timing, size |
|---|---|---|
| **Slash arcs**: Light 1–3 | A crescent swipe, pale white with a faint tint, facing right. Three shapes: level, down-diagonal, up-diagonal. | Visible during the 4-step damage window (0.07 s) plus a 0.1 s fade; reach 1.6 u |
| Heavy, charged Heavy | A wider, thicker crescent | 6-step window; reach 2.0 u |
| The L-L-H launcher | A rising crescent that sweeps upward | Launches (speed 9) |
| **Kinetic hit sparks** | Light: a small white-yellow star. Heavy: bigger, with speed lines. **Crit: the biggest, sharpest star.** Not a skull, which is Castle Crashers' mark. | About 0.12 s |
| **Movement dust** | Pale puffs at jump-off, landing, and hard turns. Never dark. | About 0.3 s |

---

## 5. Loot, progression, and co-op

| Effect | What it shows | Notes |
|---|---|---|
| **Drop glow** (GAME_DESIGN §5.4) | A soft ring of light on the ground under the item, plus a **vertical beam** at the top of the ladder. Faint at low ranks, a real light source at the top. | **Greyscale drawings, tinted at runtime by `QualityColors`**, so one texture serves every rank. The pop and bounce are motion, not art. |
| **Level-up** | A bright starburst around the hero, then rising sparkles | About 1 s. Not a ring-and-beam. |
| **Revive heartbeat** (D31) | A heart above the downed partner, beating every 45 steps (0.75 s). A press **on the beat** gives a big, bright pulse; a **rushed** press a small, dim one. Silence dims the heart. | The heart has an outline (it is a solid object); the pulses are light. |
| **Mana fizzle** (D39: an empty pool) | A sputter of grey-lilac smoke at the casting hand | About 0.3 s |

---

## 6. The kits: slots, pieces, sizes

One slot per kit, because a kit is redrawn together (PROVENANCE §5). Files:
`ArtSource/effects/<kit>/AI/<kit>-<piece>__ai_v1.png`.

| Slot | Pieces |
|---|---|
| `effect/fire` | flame-tongue-a · flame-tongue-b · flame-tongue-c · flame-burst · ember · burn-flame · spark |
| `effect/ice` | bolt · shard-a · shard-b · shard-c · frost-puff · snowflake · frost-crust · spark |
| `effect/earth` | rock-a · rock-b · rock-c · pillar-a · pillar-b · dust-puff · pebble · spark |
| `effect/air` | swoosh-a · swoosh-b · swoosh-c · spiral · leaf · speed-line · spark |
| `effect/melee` | arc-level · arc-down · arc-up · arc-heavy · arc-launcher · hit-light · hit-heavy · hit-crit · dust-a · dust-b |
| `effect/loot` | glow-ring · glow-beam · sparkle-a · sparkle-b *(greyscale)* |
| `effect/feedback` | levelup-burst · sparkle-rise · heart · heart-pulse · fizzle-puff |

**Canvas sizes** (transparent; the piece fills about 85%):
- **256 × 256:** small particles (ember, snowflake, leaf, pebble, sparkles, sparks, hits).
- **1024 × 512:** long horizontal pieces (the ice bolt, the arcs, the speed line).
- **512 × 1024:** the tall glow beam.
- **512 × 512:** everything else.

Effects are shown at gameplay size, never in the hero panel, so these canvases are generous.
