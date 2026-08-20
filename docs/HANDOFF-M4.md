# Handoff — M4: gear and stats

> **Status — built through task 48, awaiting Michael's mega pass (task 49), 2026-08-20.**
> Designed with Michael 2026-08-19 (D32–D37); tasks 39–48 built and committed one green run
> each (438e100 → 0c22f3d, 323/323). Everything in "M4 is done when" is implemented and
> machine-verified: the StatSheet and its caps, the nine-rank ladder, the deterministic
> generator (replay-pinned, elite/boss forcings bound), the XP ledger with the prestige reset,
> the inventory with the D36 level lock and force-return, real drops with item cards, gear
> live in combat (damage scale, defence, crits on their own seeded stream, life steal, weight
> slow, swing-scaled kits), the bow (arrows aim across depth, priced at impact), the quick-use
> slot, and the ladder HUD. Live-verified by eval: a Godly knife took P1's damage 10→25.5, a
> 20-raw enemy hit landed 14.17 through 29% defence, an arrow crossed depth and took exactly
> 6.3 HP, and a level-40 knife force-returned to the bag at the prestige moment. Feel checks
> (swing speed, bow cadence, the potion press, tank-build movement) are Michael's checklist —
> synthetic input can't reach actions while the editor is unfocused, the known M1 trap.
> Deviations from plan, both deliberate: the inventory types live in `Core/Items` (a class
> named Inventory inside a namespace segment named Inventory fights C# resolution), and the
> debug panel lives in `Gameplay/Items` (it mutates player state, which UI never may — the
> observe-only rule outranks the planned file path).

Continues `docs/HANDOFF-M3.md` (tasks 28–38). Same rules, same tools, same reporting format —
re-read `docs/HANDOFF.md`'s "Non-negotiable rules" and "Tools" sections before starting; they are
not repeated here. The design authority is **D32** (the stat language), **D33** (the quality
ladder), **D34** (slots, weapons, pets), **D35** (gear stats and the min/max layer), **D36**
(level requirements on gear), **D37** (the quick-use slot), plus **D24** (prestige), **D26**
(defence is a stat), and **D30** (loot is inspected and taken) — nothing below overrides them.
Check `editor_status` before any recompile or synchronous test run (Michael plays between
sessions), and follow the live-verification protocol in `CLAUDE.md`: slow-paced checks by `eval`,
fast motion by Michael's checklist.

**M4 is done when:** two players fight the M3 encounter and the loot game is real around it —
kills grant XP that levels players up and banks allocatable points (Strength / HP / Mana / Speed);
drops generate as real items with a quality rank, rolled core stats, rolled affixes, and a
required level; walking over a drop shows the item card (D30) and Light grabs it into the
inventory; equipping through the debug panel changes what combat actually deals and takes — weapon
damage × Strength, swing speed, crit, life steal, defence, weight slow; a bow turns Light into
shots; the quick-use slot drinks a potion mid-fight; prestige resets the climb and re-locks
level-gated gear (D36). Every piece of new logic lives in Core under EditMode tests, and
`run_tests` is green.

---

## Decisions taken while planning this milestone

Recorded because they are load-bearing and a future session will need the reasoning.

1. **Two seeded streams, one discipline each.** The loot stream keeps M3's rules: `DropRoll`
   stays fixed-draw per kill, and `ItemGenerator` may draw variably per item — still fully
   deterministic, because the whole sequence replays identically from the seed. Combat rolls
   (crit) get their **own seeded stream**, so loot replay never shifts because a fight went
   differently, and vice versa.

2. **`StatSheet` is rebuilt only when the loadout or allocations change**, cached on the actor.
   Nothing recomputes aggregation per step.

3. **XP attribution, paper rule:** every *living* player earns the kill's full XP; downed players
   earn nothing. Authored, tunable — co-op should never punish the reviver.

4. **The quality table is one authored asset** (budget multiplier, affix range, upgrade capacity
   per rank — the D33 paper table). Thresholds mapping D23's continuous quality score onto ranks
   live beside it. M3's tested `DropRoll` is consumed, never modified.

5. **Swing-speed scaling rounds the authored attack step counts, floor 1.** That is the only
   sanctioned way a stat touches timing — the fixed-step sim stays deterministic.

6. **`ItemInstance` is a plain serializable struct** — rolled values stored, never re-derived
   from a seed. It is save-shaped for M7 without building saves now.

7. **Auto-equip semantics, paper:** when the setting is on, a grabbed item auto-equips only if
   its quality rank strictly beats the equipped piece's (or the slot is empty). Real comparison
   logic is M6's; this is deliberately crude.

8. **Prestige force-returns invalidated gear.** Equipped items whose required level exceeds the
   reset level unequip into the inventory at the prestige moment — the D36 re-lock has no
   grandfather clause.

9. **Pets ship walking, not running.** The three classes (D34) exist as data from the start, but
   only **stat pets** aggregate into `StatSheet` in M4. Attacker pets need an ally brain and
   unique pets need bespoke hooks — each is its own future design moment, not a retrofit.

10. **Equipment actives: machinery now, content later.** The quick-use slot, its cooldown gate,
    and the active-effect data shape land in M4, proven by potions. The first authored active
    arrives once M5's ability framework exists to express it.

11. **The inventory definition of done is written down (the 2019 antidote).** Inventory is done
    when its operations pass their tests and a played session can grab, inspect, equip, and
    quaff. Anything UI-shaped beyond the debug panel is M6, by name. It never gets redesigned
    mid-milestone.

---

## Task 39 — Core: the stat vocabulary and StatSheet

**Files:** `Core/Stats/BaseStats.cs`, `Core/Stats/StatTuning.cs` (per-point values, authored),
`Core/Stats/GearContribution.cs`, `Core/Stats/StatSheet.cs`, plus tests.

- **`BaseStats`:** the four allocations (Strength, HP, Mana, Speed) as point counts.
- **`StatSheet.Build(...)`** takes base allocations + per-point tuning + the equipped items'
  contributions and produces the block combat reads: max health, max mana, mana regen, weapon
  damage, swing-speed multiplier, crit chance, crit damage, defence, move speed, slow resistance,
  life steal, knockback power.
- **Aggregation order (D32/D35):** flat bonuses sum first, percentages after, same-stat affixes
  stack additively. Defence is additive and **capped (paper 70%)**. Speed applies to velocity up
  to the hard cap; over-cap points become slow resistance (paper 2%/point, capped 75%).

**Tests:** empty gear equals base; each stat aggregates independently; stacking is additive; the
defence cap holds; the speed cap and over-cap conversion are exact; determinism.

**Commit:** `M4: the stat vocabulary and StatSheet`

---

## Task 40 — Core: the item model and quality ladder

**Files:** `Core/Items/QualityRank.cs`, `Core/Items/ItemSlot.cs`, `Core/Items/WeaponClass.cs`,
`Core/Items/PetClass.cs`, `Core/Items/AffixId.cs`, `Core/Items/AffixRoll.cs`,
`Core/Items/ItemInstance.cs`, `Core/Items/QualityTable.cs`, `Core/Items/ItemNaming.cs`, tests.

- **The nine ranks (D33):** Nothing, Battlescarred, Torn, Rusty, Shiny, Pristine, Legendary,
  Mythical, Godly.
- **`QualityTable`** carries per rank: stat budget multiplier, affix count range, upgrade
  capacity. Paper values:

  | Rank | Budget | Affixes | Capacity |
  |---|---|---|---|
  | Nothing | ×0.5 | 0 | 0 |
  | Battlescarred | ×0.7 | 0 | 1 |
  | Torn | ×0.85 | 0–1 | 1 |
  | Rusty | ×1.0 | 1 | 2 |
  | Shiny | ×1.2 | 1–2 | 3 |
  | Pristine | ×1.45 | 2 | 4 |
  | Legendary | ×1.75 | 2–3 | 5 |
  | Mythical | ×2.1 | 3 | 6 |
  | Godly | ×2.5 | 3–4 | 8 |

- **`ItemInstance`:** definition id, slot, weapon/pet class where relevant, quality rank, rolled
  core stats, affix list (id + magnitude), required level, upgrade capacity, upgrades spent
  (always 0 in M4), and the M5-reserved element fields — plain struct, decision 6.
- **`ItemNaming`:** quality prefix + definition name → "Shiny Leather Chestplate".

**Tests:** table shape (monotone budget and capacity, affix cap of 4); name composition; the
affix ids split cleanly into the M4-live and M5-reserved groups.

**Commit:** `M4: the item model and quality ladder`

---

## Task 41 — Core: the item generator

**Files:** `Core/Items/GenerationContext.cs`, `Core/Items/ItemGenerator.cs`, tests.

- **`GenerationContext`:** the quality score (straight from `DropRoll`), the progress level (for
  the D36 stamp), the definition catalog, and the special-case forcings — forced slot (D22's
  elites drop what they wear), forced definition + quality floor (D23's bosses).
- **`ItemGenerator.Roll(ref rng, in context) → ItemInstance`:** score → rank via authored
  thresholds; kind/slot from authored weights (consumables common, armor next, weapons, equipment,
  pets rarest) unless forced; definition picked among slot matches; core stats rolled within the
  rank's budget; affix count from the rank's range, picks without duplicates, magnitudes rolled;
  required level stamped from progress.
- Pure, injected rng, no scene — §5.3's promise kept.

**Tests:** replay determinism (same seed and context, same item); threshold mapping at the edges;
affix counts honour the ranges and the Godly 3–4 cap; no duplicate affixes; forced slot, forced
definition, and quality floor all bind; required level stamps from progress; a distribution sweep
lands inside sane bands.

**Commit:** `M4: the item generator`

---

## Task 42 — Core: XP, levels, and prestige

**Files:** `Core/Progression/XpCurve.cs`, `Core/Progression/XpLedger.cs`, tests.

- **`XpLedger`:** level (1–99), XP into level, unspent points, allocations (`BaseStats`),
  prestige count, permanent points.
- **The curve:** XP to reach the next level grows polynomially (paper: `40 × level^1.5`,
  authored), multiplied by the cycle cost (paper ×1.25 per prestige — D24's rising price).
- **Level-up:** +1 unspent point per level, overflow XP carries.
- **Prestige (D24/D32/D36):** available at 99 — level and allocations reset, permanent points +1,
  prestige count +1; unspent pool becomes the permanent points, re-allocatable from scratch.

**Tests:** grants and multi-level overflow; point arithmetic; 99 is a wall until prestige; the
reset semantics exactly; the cycle cost rises; permanent points survive every reset.

**Commit:** `M4: XP, levels, and prestige`

---

## Task 43 — Core: the inventory

**Files:** `Core/Inventory/Inventory.cs`, `Core/Inventory/Loadout.cs`,
`Core/Inventory/QuickSlot.cs`, tests.

- **Per player:** an unbounded item list plus the loadout — five gear slots (Helmet, Chest,
  Boots, Weapon, Pet), two equipment slots, one quick-use slot.
- **Operations, the whole M4 surface:** add (consumables stack), equip/unequip (validating slot
  match and **required level vs current level** — the D36 lock lives here), assign quick slot
  (a consumable or a worn equipment piece), use quick slot (potion heal out, cooldown gate),
  the auto-equip flag (decision 7 semantics).
- **Prestige interaction:** re-validating the loadout force-returns over-level gear to the
  inventory (decision 8).

**Tests:** every operation; the level lock blocks and unblocks with level changes; the prestige
force-return; stacking; quick-slot cooldown; auto-equip only upgrades by rank.

**Commit:** `M4: the inventory`

---

## Task 44 — Gameplay: item authoring and real drops

**Files:** `Gameplay/Data/ItemDefinition.cs` (ScriptableObject → `.ToRuntime()` struct),
`Gameplay/Data/QualityLadder.cs` (the authored table asset), starter catalog under `Data/Items/`,
`Gameplay/Items/PlayerInventory.cs` (thin wrapper owning a Core `Inventory` per player), driver
and `DropPickup` upgrades, `UI/Combat/LootHud.cs` upgrade.

- **Starter catalog, placeholder-plain:** Leather and Steel armor pieces (Helmet, Chestplate,
  Boots), Hunting Knife (sword class), Hunting Bow, Health Potion, one stat pet, one or two
  equipment pieces (passive-only for now). Names per D33's flavor rule.
- **The drop chain becomes real:** the driver feeds `DropRoll`'s quality score into
  `ItemGenerator` and the pickup carries an `ItemInstance` instead of a bare float.
- **The item card (D30):** `LootHud` shows name, quality, core stats, affixes, required level
  while a living player stands in grab range. Light grabs into that player's inventory.

**Done when:** live check — kill, read the card, grab it, and the inventory holds it (M3's HUD
grab count still proves the grab; the debug panel that lists contents arrives with task 45).

**Commit:** `M4: items drop, are inspected, and are taken`

---

## Task 45 — Gameplay: gear changes the fight

**Files:** `CharacterActor` + driver wiring, `HitResolver`/`HitApplication` touches,
`UI/Inventory/InventoryDebugPanel.cs` (IMGUI — equip/unequip, allocate points, toggles).

- **The sheet goes live:** attack damage = authored move multiplier × weapon damage × Strength;
  crit resolves in `HitResolver` from the combat stream (decision 1); defence reduces incoming
  damage before `Health`; max HP/mana from the sheet; swing speed scales attack step counts
  (decision 5); weight sums into a slow the motor applies, shrunk by over-cap Speed; life steal
  heals the attacker from resolved damage through the driver.
- **The debug panel is placeholder by construction** — the sixth IMGUI component; M6 replaces it.

**Tests:** damage/defence/crit/life-steal resolution against authored cases; swing-speed rounding
floor; the slow pipeline.

**Done when:** Michael's checklist — a better weapon *feels* bigger (numbers and pace), a tank
set visibly slows and shrugs, swing speed reads at real speed.

**Commit:** `M4: gear changes the fight`

---

## Task 46 — Gameplay: the bow

**Files:** `CombatMachine`/`CharacterActor` ranged path, driver projectile spawn for players,
tests.

- Equipping a bow turns **Light into a shot** through M3's `ProjectileSimulation`: damage from
  the sheet (weaker per hit than melee by authored ratio), draw speed as cadence, shot speed as
  flight; crosses depth freely — §2.2's identity, now in the player's hands. Heavy and aerials
  stay melee (paper — revisit with M5).

**Tests:** bow Light spawns a projectile and never a melee hit; cadence honours draw speed;
damage carries Strength and crit.

**Done when:** Michael's checklist — the bow trade (safety for damage) reads in a real fight.

**Commit:** `M4: the bow`

---

## Task 47 — Gameplay: the quick-use slot

**Files:** quick-use input wiring (the action already exists as `Equipment` — meaning per D37),
potion effect, cooldown gate, HUD hint of the slotted item.

- The button fires `Inventory.UseQuickSlot`: a Health Potion heals (paper 35% of max), stacks
  decrement, the cooldown gates re-use. The cooldown machinery is the future actives' seam
  (decision 10).

**Tests:** heal clamps at max; empty slot is a no-op; the cooldown gate; stack exhaustion clears
the slot.

**Done when:** Michael quaffs mid-fight without a menu.

**Commit:** `M4: the quick-use slot`

---

## Task 48 — Gameplay/UI: the endless ladder

**Files:** driver XP application, `PlayerHealthBars` level/XP display, allocation + prestige in
the debug panel.

- `EnemyDied.XpReward` finally lands: every living player's ledger earns it (decision 3).
- Level and a small XP bar join the health bar; the debug panel allocates points and, at 99,
  prestiges — reset, badge count, the D36 re-lock visibly returning gear to the inventory.

**Done when:** Michael levels up, allocates, prestiges (debug-accelerated), and watches his gear
lock and return as he re-climbs.

**Commit:** `M4: the endless ladder`

---

## Task 49 — Close out M4

1. Full `run_tests` — report the summary line.
2. **Michael's session** — the loot loop end to end: fight, level, drop, inspect, grab, equip,
   feel the difference, potion under pressure, bow versus sword, prestige and re-climb. Iterate
   paper numbers live; anything settled gets recorded, the rest stays paper per §2.5's deferral.
3. Update the progress table in `CLAUDE.md`: M4 → complete, M5 → next.
4. Update this file's status header, and record anything that felt wrong to build.

**Commit:** `M4: gear and stats complete`

---

## Escalate rather than solve

- Comparison or management UI beyond the debug panel — the loot-card polish, sorting, stash,
  tooltips are **M6, by name** (decision 11).
- The upgrade-spend flow or its currency (M6 — M4 only stamps capacity).
- Magic casting, mana consumption, statuses, infusion effects, or making any M5-reserved stat
  multiply anything (M5).
- Attacker or unique pet behaviour (decision 9) — their own design moment.
- Boss encounters or signature drops actually spawning (M7) — the generator's forcing seam is
  the whole M4 obligation.
- Saves and persistence (M7) — `ItemInstance` being save-shaped is enough.
- Any urge toward a per-item, per-affix, or per-equipment C# class — items are data end to end,
  pillar 3.
- Any new assembly, `.asmdef` reference, or package.
- Balance crusades — every number here is paper; Michael tunes live at task 49.
