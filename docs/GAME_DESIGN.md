# BattleBomb — Game Design Document

**Status:** living document. Systems sections are derived from locked decisions in `DECISIONS.md`
and are safe to build against. Sections marked **⚠ NEEDS INPUT** are placeholders — do not invent
content to fill them.

**Reading order for a new session:** `DECISIONS.md` first (what was decided and why), then this
(what the game is), then `CLAUDE.md` (how to work in the repo).

---

## 1. Vision

> Castle Crashers-style co-op combat, Skyrim / Dungeon Defenders-style gear progression, and an
> endless loot chase.

Canonical statement: `docs/hook.md`.

### Pillars

1. **Combat is simple and responsive.** Low input complexity, high game feel. Any proposal that adds
   mechanical complexity to moment-to-moment combat is measured against this and usually loses.
2. **Depth lives in gear, not in inputs.** The build-crafting, min/maxing, and mastery surface is the
   loot and stat system. This is the explicit design goal: *make finding gear exciting without making
   combat complicated.*
3. **Characters are data, not code.** Mechanically similar, visually distinct. A new character is
   authored assets and tuning values — never a new C# class.
4. **Co-op is the frame.** Two players, shared screen, designed for from the start.
5. **Every authored space earns twice.** Story content is replayed at scaling difficulty, so content
   serves both narrative and endgame.

---

## 2. Combat model

### 2.1 Space

A **side-on ground plane**. Left/right traverses the level; up/down moves nearer and further within a
**fixed-width depth band** that never changes between areas.

Depth is a **real simulation axis** — its own position, velocity, and reach tests. It is not sprite
sort order and not a rendering trick. Folding depth into Y-position is how the 2019 build failed.

### 2.2 Reach rules

| | Depth behaviour |
|---|---|
| **Melee** | Depth-limited. Cannot reach a target several lane units away even when aligned left-to-right. |
| **Projectiles / ranged** | Cross depth freely. This is ranged's mechanical identity — it trades damage or cadence for ignoring melee's constraint. |

Tolerance is **generous** (~1 lane unit). Attacks **soft-lunge** to close small depth gaps, so a
near-miss becomes a hit.

### 2.3 Legibility

**Hard-edged grounded shadows** are the primary spatial cue, on every character, enemy, and
interactable. No HUD element carries depth information. Backgrounds must never compete with shadow
readability — this is the specific failure mode a previous parallax attempt hit.

### 2.4 Jump

A **general-purpose** mechanic, as in the inspiration games: traversal over obstacles, dodging,
combo extensions, boss mechanics. Not reserved for one job.

The standard jump is **fixed-height** — tap or hold, the same arc every time (D18). Jump boosts or
double jumps, if they ever exist, are explicit mechanics layered on top, never variable height on
the base jump.

### 2.5 Feel targets

Skill expression lives in **timing, left-to-right spacing, and ability use** — deliberately not in
fine depth alignment, which would not survive touch controls.

⚠ **NEEDS TUNING TARGETS** — attack frame data, combo window length, lunge distance, hitstop, and
i-frame windows shipped M2 on paper values Michael play-approved as a baseline (2026-08-18). The
settle-and-record pass is deferred, at his direction, until more mechanics and artwork exist; until
then the authored assets in `Assets/_BattleBomb/Data/Combat/` are the reference.

### 2.6 Attack verbs and combos (D19)

- **Light** — fast, hits 1–2 targets, creates space. The rhythm verb.
- **Heavy** — slower, cleaves a crowd, longer lunge. **The only hold in combat:** charge it for a
  bigger single-target hit.
- **The stick aims; it never picks the combo.** Attack direction and lunge follow the held movement
  direction; combos are pure button sequences.
- **Combos are authored content.** M2 ships `L-L-L`, `L-L-H` (launcher), and charged Heavy. A new
  combo is an animation plus data, never new code — the set grows over time.
- **Aerials:** Jump→Light pops enemies up for a hit or two, deliberately short of juggling
  (extended "air tech" is a future skill ceiling). Jump→Heavy slams an AoE where the grounded
  shadow marks the landing.
- **Magic is one press.** No charged casts, no chain-into-magic finishers. Fuelled by slowly
  regenerating mana; regen and capacity are gear stats (§5).

### 2.7 Block (D19)

Hold to block kinetic damage and projectiles — **never magic**, so casters always answer turtles.
A perfect-timed block staggers the attacker and opens a counter window. No stamina meter: blocking
is priced by inaction and magic vulnerability, and the reward lives in the timing.

### 2.8 Hit feedback (D20)

Every landed hit pops a **floating damage number** by default — pillar 2 made visible in combat,
not just on the stat screen. Settings will offer number text-size options and a full disable once a
settings menu exists; the renderer treats size as a parameter from day one. Numbers are
presentation-only: nothing in the simulation reads them back.

---

## 3. Characters and abilities

Every character shares **one ability framework** and the same slots. They differ in animation,
element, VFX, and small tuning values — the Castle Crashers model, stated explicitly in the hook.

| Slot | Verb | Role |
|---|---|---|
| Basic attack | **Light** | Grounded melee combo chain. The default verb; performs **Interact** when an interactable is in range. |
| Heavy attack | **Heavy** | Slower, cleaves a crowd, longer lunge; hold to charge a bigger single-target hit (§2.6). As the `L-L-H` combo ender it launches. |
| Elemental attack | **Magic** | The character's element expressed offensively (§4). |
| Equipment use | **Equipment** | 1–2 chosen loadout slots from an unlockable catalog. Utility-first, damage derived from the weapon — spice, never the meta (D19). |
| Guard | **Block** | Hold to guard. Active defence by design: timed-block rewards, not turtling. |
| Jump | **Jump** | Shared, universal (§2.4). |

### 3.1 Controls

Six verbs after the movement stick — the complete input surface, sized for one-thumb mobile play
(full mapping and reasoning: D17):

| Verb | Keyboard | Gamepad |
|---|---|---|
| Move | WASD | Left stick |
| Light | J | X (west) |
| Heavy | K | Y (north) |
| Magic | L | B (east) |
| Equipment | I | RB (right shoulder) |
| Jump | Space | A (south) |
| Block | Left Shift (hold) | LB (left shoulder) |

There is **no dodge button** — evasion is jump (§2.4) and free depth movement (§2.1); the playstyle
is deliberately forward. There is **no interact button** — Light is contextual. **Combos are
sequences, holds, and stick+button flavours, never two face buttons pressed at once:** one thumb
operates every button on a touch screen, so chorded inputs are out by construction (D17).

**Implementation rule:** a character is a data asset — visual set, element, per-slot ability variant,
base stat weights. Any design requiring a bespoke script per character is wrong and should be pushed
back on. This is the largest single lever on content velocity in the project.

⚠ **NEEDS INPUT** — the roster: how many characters, who they are, and how each ties into the story.

---

## 4. Elements

Element is a **first-class input to the damage pipeline**, contributed by three parties:

```
base damage
  → attacker element
  → defender elemental resistance      (Dungeon Defenders model)
  → environment elemental climate      (fire performs better in cold environments)
  → gear and stat modifiers
  → final damage
```

Environments carry an elemental climate that modifies effectiveness. This makes character choice
situational without adding a single input to combat — depth via context, not via complexity.

**Two element sources per player (D19):** the weapon's infusion applies its status automatically on
hit (the Dungeon Defenders half), and the character casts their own element on Magic (the Castle
Crashers half). Statuses react — soak + shock chain-stuns, and a deliberately small set of similar
pairs — so a lone player self-combos by pairing weapon element against their own, and a co-op
partner adds a third source (§7). Reactions are the elemental depth Castle Crashers never had.

**Baseline roster** (inherited from the 2019 `CharacterClassData`, ⚠ confirm): Fire, Water, Electric,
Earth.

---

## 5. Gear and progression

The retention engine. Depth here is intentional and unbounded, unlike combat.

**Terminology, binding (D19):** *gear* = armor + weapon, the build this section describes;
*Equipment* = the loadout button (D17, §3). Equipment damage derives from the weapon and can never
out-grow it.

### 5.1 Axes

- **Tier** — caps maximum potential. Low-tier gear cannot reach high-tier ceilings regardless of rolls.
- **Quality / rarity** — better stat rolls *and* more customization surface.
- **Add-ons / enchants** — the min/max layer. Rarer items afford more of it.

### 5.2 Slots and attributes

**Slots** (from the 2019 `ItemType`, ⚠ confirm): Helmet, Chest, Pants, Boots, Gloves, Weapon, Pet,
plus Food as a consumable.

**Attributes** (from the 2019 `Attributes`, ⚠ confirm): Strength, Agility, Intellect, Stamina.

### 5.3 Generation

```
tier + quality  →  stat ranges  →  roll  →  affix slot count  →  affix selection  →  item
```

Entirely **pure logic with no scene dependency**. It lives in Core, is covered by EditMode tests, and
is called identically by story and endless modes. This is the highest-value test surface in the
project — loot bugs are subtle, compounding, and destroy trust in the chase.

---

## 6. Enemies and bosses

Enemy design vocabulary, built from the combat model rather than invented separately:

- **Depth-limited melee attackers** — pressure you into managing depth.
- **Ranged attackers** — cross depth, punish standing still, create the reason to keep moving.
- **Elementally resistant variants** — reward roster and gear choice (§4).
- **Bosses** — use jump-dodgeable and depth-dodgeable patterns, mixing both defensive verbs.

⚠ **NEEDS INPUT** — specific enemy families, boss roster, and how they tie to the world.

---

## 7. Co-op

**Two players, local, shared screen.** Written network-shaped so online is a later project rather
than a rewrite (see D10 for the non-negotiable rules that depend on).

Design consequences:
- Shared camera framing constrains level width and enemy placement. Encounters must stay readable
  when players separate.
- Difficulty scales with player count.
- **Partner contact is knockback, never damage (D21).** A player's swing can shove the other player
  and interrupt their flow, but never removes health — and shows no damage number, because none
  resolves.
- ⚠ **OPEN (O7)** — loot distribution: shared drops vs per-player instanced rolls. Changes the item
  generation API, so settle before §5.3 is built.
- ⚠ **OPEN (O8)** — failure state: revives, whether one death ends an attempt.

---

## 8. Game modes

Mode is a first-class concept from the first line of code. No core system assumes story mode.

| Mode | Status | Description |
|---|---|---|
| **Story** | First to ship | Authored **chapters**, extendable without structural change. |
| **Story — scaling difficulty** | With story | Chapters replayed at higher tiers with better loot tables. |
| **Endless** | Planned | Procedural, escalating difficulty. Long-term retention. |
| **PvP** | Planned | Requires the networking groundwork D10 protects. |
| **Missions** | Considered | — |

Difficulty tier and loot table are **inputs** to encounter and reward generation, never baked into a
chapter's authored data. A chapter is content; the tier applied to it is state.

---

## 9. World and story

⚠ **NEEDS INPUT — do not invent. Being written by Michael and a collaborator, outside this repo.**

**This does not block engineering.** Milestones M0–M6 are entirely systemic and require no narrative
input. Story is first needed at **M7 (chapters)**. Do not stall on it, and do not draft placeholder
lore to fill the gap — it will only have to be thrown away.

The 2019 build implies a planet-hopping premise: scenes named Moon Dungeon, Mercury, and Training
Grounds, plus starfield and planetary skyboxes and a moon cutscene. Treat that as a hint about
original intent, not as canon.

Needed by M7: setting and tone, the chapter spine, who the characters are and why they fight, and how
elements are justified in the fiction. §3 (roster) and §6 (enemy and boss families) resolve alongside it.

---

## 10. Build order

Dependency-ordered. Each milestone is verifiable before the next begins.

| # | Milestone | Done when |
|---|---|---|
| **M0** | Foundation | Unity 6 + URP project, assembly layout, new Input System, test harness green. |
| **M1** | Movement and camera | Two players move on the depth plane with readable shadows; shared camera holds both. |
| **M2** | Combat core | Melee combo, lunge, hit resolution, damage pipeline. Damage math covered by EditMode tests. |
| **M3** | Enemies | Basic AI, telegraphed attacks, elemental resistance, death and rewards. |
| **M4** | Gear and stats | Item generation, equipment, stat aggregation — all Core, all tested. |
| **M5** | Abilities and elements | Ability framework, per-character variants, environment climate modifiers. |
| **M6** | Loot loop | Drops, inventory, comparison UI. The chase is legible and satisfying. |
| **M7** | Chapters | Chapter flow, save/progression, difficulty tiers. |
| **M8** | **Vertical slice** | One chapter, two characters, one boss, full loop, co-op, polished. |
| **M9+** | Scale | More chapters and characters, endless mode, then PvP. |

M8 is the real target. It is the thing the 2019 build never reached, and it proves every system talks
to every other before content scales.

---

## 11. Explicitly out of scope for now

Recorded so they are not re-litigated: online multiplayer (D10 — deferred, not foreclosed), mobile
builds (D5 — kept viable, not shipped), PvP, missions, monetisation, daily rewards, and the 2019
highscore and inventory systems.
