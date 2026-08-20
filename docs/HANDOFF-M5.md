# HANDOFF-M5 — Abilities and elements

**Status — tasks 50–58 built and committed, 401/401 green. Task 59 is Michael's mega pass.**
Design locked with Michael 2026-08-19 (D38–D41, plus O11 opened). Read first: `DECISIONS.md`
D38–D41 and O11, then `GAME_DESIGN.md` §2.6 and §4.

## Build log

| # | Commit | What landed |
|---|---|---|
| 50 | `6be7fc7` | The driver step reads as a phase list. Zero behaviour change, 325/325 both sides. |
| 51 | (with 50's branch) | Elements became authored data; the `Element` enum is gone. Fire is the first asset. 334/334. |
| 52 | — | Statuses, priced from the applying hit; the same-element rule made automatic by threading `ElementalDefence` through every damage path. 349/349. |
| 53 | — | The reaction framework, built and tested with synthetic elements, its table deliberately empty. 361/361. |
| 54 | — | The four sleeping affixes woke: magic damage/range, per-element resistance, weapon infusion on every hit. 373/373. |
| 55 | — | The Magic kit: one button, three casts, mana gating, the stick read at the press. 389/389. |
| 56 | `5fffcf0` | Magic in the world — casts resolve, the leap lifts, the Fire caster burns players. 392/392. |
| 57 | — | Magic made visible: element-coloured burn tint, quiet tick numbers, the mana bar's cost mark, cast tells. 392/392. |
| 58 | — | The Ember Stone, the mana potion, a cold climate on the test scene. 401/401. |

**Deviations from the design, both deliberate and recorded:**

- **Magic damage is flat, not a percentage.** D39's parenthetical said "+% cast damage"; it ships
  as a flat addition, matching how weapon damage already works and keeping a low-base character's
  casts exactly as gear-scalable as a high-base one's. One number range to flip if Michael
  disagrees.
- **The debug panel grants items.** Testing magic needs a specific weapon, stone, and potions, and
  drops are random by design. The panel's Grant row rolls them mid-ladder (score 1.7, Shiny
  territory) so a feel judgement is never about an absurd item. Dies with the panel in M6.

---

## What M5 is

The milestone where the game's differentiator arrives: elemental statuses that react. Magic
finally spends the mana pool M4 left regenerating, the four sleeping affixes (Magic Damage,
Magic Range, Elemental Resistance, Weapon Infusion) come alive, statuses flow both ways between
players and enemies, and the environment gets its vote through climate. **Fire is the only
authored element** — the roster itself is contested (O11), so the element framework is data-driven
end to end and the reaction system ships built, tested, and empty of content.

---

## Paper numbers

All authored data, tuned live like everything else.

| Thing | Paper value |
|---|---|
| Splash mana cost | 20 (of base 100 pool) |
| Aura mana cost | 45 |
| Elemental double jump mana cost | 15 |
| Mana regen | ~1/s base (M4's existing value) + gear |
| Burn from a cast | 50% of the applying hit's resolved damage, over 3 s |
| Burn from weapon infusion | 25% of the hit's damage over 3 s, scaled by affix magnitude |
| Burn tick cadence | every 30 steps (0.5 s) — 6 ticks over a full duration |
| Same-element rule | elemental damage ×0.5 both ways; status never applies; kinetic untouched |
| Climate example | cold region: Fire ×1.25, applied to all elemental damage, any dealer |
| Ember Stone active | fire burst = 75% of wearer's weapon damage; cooldown 15 s (900 steps) |
| Mana potion | restores 35% of max mana (mirrors the health potion fraction) |
| Stick-down read | stick Y < −0.5 at the instant of the Magic press |

---

## Planning decisions (engineering, settled at design time)

1. **ElementId replaces the enum.** Core carries element as an int-backed id; `ElementDefinition`
   (ScriptableObject) → runtime struct via an `ElementCatalog`, like the item catalog. Id 0 =
   None, Fire = 1. The old `Element` enum is deleted; `AffixRoll.Element`, enemy resistance
   tables, and `DamageCalculator` migrate. Existing authored assets re-point to Fire.
2. **Statuses are keyed by element, one per element per target.** Reapplication refreshes the
   duration; the stronger per-tick damage wins. Fixed-capacity storage, no per-step allocation.
3. **Status strength is priced at application** — per-tick damage computed from the applying
   hit's resolved damage, the same pattern as arrows priced at impact (M4).
4. **DoT bypasses Defence, respects Elemental Resistance and climate.** Defence is for hits
   (D40). Tick damage raises hit events flagged as DoT so presentation can style them.
5. **DoT never staggers.** Burn removes health only — no hit reaction, no interrupt. Rhythm
   protection.
6. **Friendly casts follow D21.** A partner caught in a splash or aura takes knockback only —
   no damage, no status, no damage number.
7. **Reaction vocabulary v1:** `ReactionSpec { BurstDamageFraction, StunSteps, ConsumesStatus }`;
   the table is an authored list of (status element, incoming element) → spec. Ships empty;
   EditMode tests use synthetic element ids.
8. **Casts are combat-machine phases** — a cast family parallel to attacks (windup/active/
   recovery), authored per character as a `MagicKit` (per-cast damage, range/radius, mana cost,
   phase steps). Mana is checked and spent at cast start; an unaffordable press does nothing.
9. **Double jump is once per airborne**, reset on landing, applied through the motor; the burst
   resolves at the liftoff point.
10. **GearContribution grows MagicDamage and MagicRange.** Per-element resistance cannot be a
    flat field — `StatSheet` aggregates it per element id in its own small structure.
11. **Ember Stone is the first equipment active.** The quick-use path M4 stubbed (`Nothing`)
    grows an active-fire result; the item definition authors the element, the derived-damage
    fraction, and the cooldown. D37's guards are the point: derived damage, real cooldown.
12. **Mana potion:** consumable definitions gain a restore target (health or mana); alchemy
    naming applies unchanged ("Vial of Mana" … "Elixir of Mana").
13. **Climate lives on the driver for M5** — a serialized per-scene list of (element id,
    multiplier). Chapters take ownership in M7.
14. **Statuses clear on death and on a player going down.** A revive starts clean.

---

## Tasks

Each task lands with `run_tests` green and one commit, as always.

### 50 — Driver tidy-up *(the M4 close-out warning, paid before Magic adds a phase)*
Name and separate the driver's step phases into a legible order (commands → simulation →
combat resolution → deaths and loot → events out). **Zero behavior change** — the full suite
green before and after is the whole definition of done.

### 51 — Elements become data (Core)
`ElementDefinition` + `ElementCatalog`, `ElementId` through Core, the enum deleted, every
consumer migrated, the Fire asset authored, existing data re-pointed.
**Done when:** an EditMode test can define a synthetic element with zero code changes; suite green.

### 52 — Statuses (Core)
Status storage on combatants, application rules (two-way, same-element, refresh/strongest-wins),
Burn ticking through the pipeline (resistance + climate, never Defence), clear on death/down.
**Done when:** EditMode covers apply, refresh, same-element block, DoT math, expiry, down-clear.

### 53 — Reactions framework (Core)
Pair-table lookup at application time, consume rules, the v1 effect vocabulary (burst, stun).
**Done when:** synthetic-element tests prove a pair triggers, consumes, and an empty table is inert.

### 54 — The four affixes live (Core)
`AffixEffects` gets real mappings; `GearContribution`/`StatSheet` grow; infusion-on-hit enters
hit resolution; resistance shortens status durations.
**Done when:** a rolled item measurably changes cast damage, cast size, incoming elemental
damage, and status durations in tests.

### 55 — The Magic kit (Core combat machine)
Cast phases, three casts, mana gating and spend, the stick-down read, airborne press = double
jump. `CommandButtons.Magic` finally consumed.
**Done when:** machine tests cover all three casts, unaffordable presses, once-per-airborne.

### 56 — Gameplay wiring
CharacterActor executes casts (splash depth-limited, aura radial, jump impulse + burst); driver
processes Magic; `CharacterDefinition` authors the MagicKit (test character = Fire); infusion
wired into player hits; climate field on the driver; the caster enemy authors Fire and statuses
players.
**Done when:** eval-driven live checks show cast damage, Burn ticking on an enemy and on a
player, and mana draining/regenerating.

### 57 — Presentation and UI
Status tint/particle placeholder on burning things, DoT damage numbers styled distinct, the mana
bar reflecting costs (insufficient-mana cue), item-card affix lines carrying real values, cast
placeholder VFX.
**Done when:** visible in game-view captures; presentation owns no simulation state.

### 58 — Content
Ember Stone, the mana potion line, verification that Fire-infused weapons drop and apply Burn,
a climate value set on the test scene.
**Done when:** the stone fires from the quick slot, the potion restores mana, an infused drop burns.

### 59 — Michael's mega pass
Fast-motion checklist (his, per the live verification protocol): splash feel and aim, the
stick-down gesture's reliability mid-brawl, the double jump arc and its mana price, Burn
readability on enemies and on yourself, infusion feel, the Ember Stone moment, whether the mana
economy paces like a rhythm rather than a rotation.

### 60 — Fixes and close-out
Mega-pass fixes, docs updated, close-out notes recorded here, final green run, milestone commit.

---

## Standing watches

- **The verb budget** (M3 close-out): contextual Light already carries revive → grab → swing;
  the stick-down Magic flavour adds gesture complexity. Watch for misreads in the mega pass.
- **Determinism:** casts and status ticks run through the seeded combat stream; nothing rolls
  from fresh seeds (the xorshift small-seed trap, M4).
- **O11 is not ours to resolve.** If element questions come up mid-build, the answer is "the
  framework doesn't care" — anything that *does* care is a design error to fix.
