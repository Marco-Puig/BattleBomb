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

**Art direction (D47):** characters, enemies, weapons, and pets are **2D billboarded sprites —
Castle Crashers-style art — in the 3D environment.** The simulation never knows: hitboxes and
timing are Core numbers, so the visual layer is swappable by construction. Sprite pipeline and
resolution targets are decided at the art pass, not before.

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
- **Magic is one button, three casts (D39, amended by D46):** press = the element's **signature
  cast** (each element's own move — §4); stick down + press = a radial **aura** around the
  character (the depth answer, the crowd moment); press airborne = the **elemental double jump**
  (a mana-priced boost up with a small burst at the liftoff point). No charged casts, no chords;
  stick up + Magic is deliberately unassigned expansion room. Fuelled by slowly regenerating
  mana; capacity is a base stat, regen is a gear stat (§5, D32). Every cast applies the
  character's element status (§4).

### 2.7 Defence (D26)

There is **no block button**. Defence is the **defence stat** (§5, from M4) and movement itself —
depth, spacing, and jump (§2.4). The mobile control budget was at its limit, and the designed
defensive verbs were always movement: the same reasoning that rejected a dodge button in D17,
applied consistently.

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
| Elemental attack | **Magic** | The character's element expressed offensively — the three-cast kit: the element's signature cast, aura, elemental double jump (§2.6, §4, D39/D46). |
| Quick-use | **Quick-use** | Fires the one quick-use slot: a consumable (health potions most commonly) or a worn equipment piece with an active (D37). Equipment itself is two worn slots, passive by default, some pieces carrying an active with derived damage and a real cooldown (D27 as amended). Spice, never the meta (D19). |
| Jump | **Jump** | Shared, universal (§2.4). |

### 3.1 Controls

Five verbs after the movement stick — the complete input surface, sized for one-thumb mobile play
(full mapping and reasoning: D17, amended by D26):

| Verb | Keyboard | Gamepad |
|---|---|---|
| Move | WASD | Left stick |
| Light | J | X (west) |
| Heavy | K | Y (north) |
| Magic | L | B (east) |
| Quick-use | I | RB (right shoulder) |
| Jump | Space | A (south) |

There is **no dodge button and no block button** — evasion and defence are jump (§2.4), free depth
movement (§2.1), and the defence stat (§2.7, D26); the playstyle is deliberately forward. There is
**no interact button** — Light is contextual. **Combos are
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

**An element is authored data, never code (D38)** — a definition holding its name, its status
behaviour, its signature cast, and its visual identity. **The roster is Fire, Ice, Earth, Air
(D46, resolving O11), and each element's press cast is its own move:**

| Element | Signature cast | Status |
|---|---|---|
| **Fire** | Mid-range ground line ahead | **Burn** — damage over time |
| **Ice** | Long-range, low-damage bolt in the caster's lane | **Chill** — slowed for a few seconds |
| **Earth** | Short-range rock wall — decent damage, stuns on hit | No mark — infusion grants the wielder crit chance (provisional) |
| **Air** | Mid-range gust that launches enemies for a combo | No mark — infusion grants the wielder knockback (provisional) |

Statuses are priced from the hit that applied them, so gear scaling flows into them
automatically. Earth's stun and Air's launch belong to the hit, not to a mark; their infusions
are **wielder passives** (D46 as amended) — Earth's crit chance is the Castle Crashers skull,
Air's is knockback, both scaled by the infusion's roll. Ice's bolt is long but lane-bound —
free depth crossing stays the bow's identity (§2.2).

**Two element sources per player (D19):** the weapon's infusion applies its status automatically on
hit (the Dungeon Defenders half), and the character casts their own element on Magic (the Castle
Crashers half — the three-cast kit, D39). Statuses **react** — a setup status plus a trigger
status chain-stuns, and a deliberately small set of similar pairs — so a lone player self-combos
by pairing weapon element against their own, and a co-op partner adds a third source (§7). The
reaction framework shipped with M5; the authored pair table stays empty until the pairs for this
roster are designed (D41, D46). Reactions are the elemental depth Castle Crashers never had.

**Statuses are two-way (D40):** enemy casters status players. **Same element cancels:** matching
elements halve elemental damage both ways and the status never applies; kinetic weapon damage is
untouched, so an infused blade is never worse than a plain one. Mitigation splits into two jobs —
**Defence** reduces hits, **Elemental Resistance** (per-element, from gear) reduces that
element's damage and shortens its statuses.

**Environments carry an elemental climate** that multiplies all elemental damage regardless of
who deals it (D41) — symmetric, so region intel drives gear and character choice both ways. This
makes character choice situational without adding a single input to combat — depth via context,
not via complexity.

---

## 5. Gear and progression

The retention engine. Depth here is intentional and unbounded, unlike combat.

**Terminology, binding (D19):** *gear* = armor + weapon, the build this section describes;
*Equipment* = the two-slot worn loadout, passive by default with some actives (D27, D37). Bows are
**weapons**, a deliberate ranged weapon class, never equipment; potions are consumables, fired
mid-fight from the quick-use slot (D37).

### 5.1 The quality ladder (D33)

One player-facing ladder carries the whole arc — the hook's tier and quality axes, merged:

**Nothing < Battlescarred < Torn < Rusty < Shiny < Pristine < Legendary < Mythical < Godly**

Each rank does three jobs at once: a **stat budget** multiplier, an **affix count** (a rolled
range capping at 3–4 on Godly — even a Godly can come up short), and **upgrade capacity** (§5.3).
Progress and difficulty push drops up the ladder; within a rank, the rolls vary. Materials are
name flavor ("Shiny Leather Chestplate", "Rusty Steel Helmet"), never a hidden axis; weapons start
plain and grow Destiny-style signature names as content, boss drops especially.

### 5.2 Slots and stats (D32, D34, D35)

**Gear slots:** Helmet, Chest, Boots, Weapon, Pet — plus the two worn equipment pieces and one
quick-use slot.

**Base stats** — the character, leveled with allocated points (D32): **Strength** (+% weapon
damage), **HP** (pure pool — resistance is gear's job), **Mana** (capacity — regen lives on gear),
**Speed** (hard velocity cap; over-cap points shrink slows, armor weight and combat effects alike).

**Gear stats are direct and concrete.** Core stats every item of a slot has: weapons roll Damage +
Swing Speed (bows: Draw and Shot Speed); armor rolls **Defence** (% reduction — additive, a
max-tank build caps ~70% and a max-lightweight ~30% at top rolls, both scaling down the ladder)
+ **Weight** (the speed tax; tankier rolls weigh more). The affix pool: Crit Chance, Crit Damage,
Life Steal, +Max HP, +Max Mana, Mana Regen, Reduced Weight, Knockback Power, plus the magic group
(live with M5): **Magic Damage** (+% cast damage), **Magic Range** (splash length and aura
radius), **Elemental Resistance** (per-element — reduces that element's damage and shortens its
statuses, D40), and **Weapon Infusion** (the weapon applies its element's status on every hit).

**Weapon classes:** Sword (the whole D19 kit) and Bow (Light becomes a shot — weaker per hit,
crossing depth freely per §2.2). **Pets** come in three classes (D34): attackers, stat pets, and
uniques with one bespoke effect each.

### 5.3 Generation and the min/max layer (D35, D36)

```
drop context → quality rank → kind → definition → core stat rolls → affix rolls → required level → item
```

Entirely **pure logic with no scene dependency**. It lives in Core, is covered by EditMode tests, and
is called identically by story and endless modes. This is the highest-value test surface in the
project — loot bugs are subtle, compounding, and destroy trust in the chase.

The min/max layer is both halves of the inheritance: **affixes roll at the drop and are immutable**
(the Diablo half — hunting the right combination), while **upgrade capacity** scales with quality
and is spent by the player after the drop, raising stats of their choice (the Dungeon Defenders
half — D44's deepen-or-gamble flow, §5.6). Every item is stamped a **required level** from its
drop context, enforced at equip (D36).

### 5.4 Drops (D23, D30)

Drops are **shared and free-grab**: one roll per drop, it lands in the world, whoever grabs it
keeps it. **Chance** scales with enemy rank; **quality** scales with story progress × difficulty ×
active multipliers, elites adding ~5–10% on top. **Bosses always drop an authored signature item**
at a quality floor well above the level's norm. A drop is **inspected and taken, never hoovered**
(D30): walking over it shows the item card, Light grabs it deliberately, and it lands in the
inventory by default — auto-equip is a setting.

**Drop feel (M6):** a drop glows from beneath in its quality colour — subtle low on the ladder,
a genuine light source at the top — pops from the corpse with a small bounce, and is taken with
a quick grab animation and sound, never a swing. A full sack refuses the grab: a red X on the
card and a 200/200 flash (D43).

### 5.5 Character progression (D24, D32, D36)

Leveling **never caps** — the main differentiator from Castle Crashers and Dungeon Defenders.
Levels run 1–99 granting **one allocatable stat point each** (D32), then **prestige**: back to
level 1, allocations reset, banking one permanent stat point and a visible prestige badge, each
cycle costing more XP than the last. Level-required gear re-locks at that moment and returns as
the climb re-earns it (D36) — the fresh start is real twice over. The per-cycle reward is
deliberately small — only the most dedicated players are noticeably stronger — so gear remains the
dominant power source (pillar 2) while the ladder itself never ends.

### 5.6 The chest, the economy, and investment (D42–D45)

The loot loop lives in **checkpoint rooms** — calm spaces holding a **chest** (the only point of
inventory access: walk up and it opens, walk away and the sack seals), usually a **shopkeeper**,
and a training dummy to test builds on. Solo, the chest screen fills the display and pauses the
world; couch co-op, it takes the opener's half while the partner plays on. Two tabs: **Item
Sack** (quality-coloured thumbnail grid with a category filter; the selected item's stats,
vs-worn deltas, and actions alongside) and **Hero** (stat allocation and the worn loadout).

**Money enters only by selling** (D43): one per-player currency, prices scaling with quality ×
required level so income scales forever. It leaves through shopkeeper stock and **upgrading**
(D44): capacity points deepen any stat an item already has, the cost doubling per point.
Duplicates offer the gamble instead — **combining** two identical same-rank items into one fresh
reroll with a 2% shot at the next rank, spent points dying with the inputs. Worn, sold, or
combined: the three fates of every drop are the chase.

The sack: 200 slots (worn gear excluded, consumables stacking to 5 per slot), item locks, and an
auto-sell-at-cap setting that sells the worst unlocked piece. Elites (D22) finally spawn in M6 —
each rolls its drop at spawn and visibly wears it, armor tinted the drop's quality colour, so
the fight advertises its own reward.

---

## 6. Enemies and bosses

Enemy mechanics are **standardized archetypes**; regions and ranks style and scale them (D22):

| Archetype | The pressure it applies |
|---|---|
| **Melee grunt** | Depth-limited like the player (§2.2) — crowds you into managing depth. |
| **Ranged** | Crosses depth freely — punishes standing still. |
| **Caster** | Slow, hard-hitting elemental artillery behind the longest telegraphs — the priority target; its hits apply its element's status to players (§4, D40). |
| **Brute** | Slow, telegraphed, jump- and depth-dodgeable — tests both defensive verbs. |

- **A region authors 2–3 skins** of these archetypes — its element, its attacks, its look. A new
  region's roster is data, never code; elemental resistances reward roster and gear choice (§4).
- **Rank** composes encounters (grunts early, brutes late) and scales stats; **difficulty** (§8)
  multiplies on top. Two dials, deliberately separate.
- **Elites (rare):** visibly armored, tougher, 5–10% better loot — and they **drop what they
  wear** (D22). Standard enemy armor marks common elites; a player-itemizable piece is visibly
  distinct on sight, so the fight advertises its own reward.
- **Bosses** mix jump-dodgeable and depth-dodgeable patterns and always drop their signature item
  (§5.4).

⚠ **NEEDS INPUT** — the specific families and bosses per region resolve with the world and story (§9).

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
- **Loot is shared and free-grab (D23)** — the discovery moment belongs to the whole couch,
  grabbing included.
- **A downed player is revived by their partner** — contextual Light (D17), so a revive is walking
  over and pressing the button you already know. Both down = back to the last checkpoint (D25).

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
