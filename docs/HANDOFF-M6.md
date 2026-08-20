# HANDOFF — M6: the loot loop

**Status — designed, 2026-08-20.** Directed by Michael in the M6 design session (visual
companion mockups for the chest screen; five sections approved). Decisions D42–D45 locked;
build not yet started.

---

## What M6 is

The milestone where the chase becomes a game. Everything the generator has been rolling since M4
finally has somewhere to go: a **chest** to organize at (D42), an **economy** to sell into
(D43), an **investment layer** to deepen or gamble with (D44), and **elites** (D22, deferred
since M3) to hunt for it all. The debug panel — the last UI that mutates state directly — dies,
replaced by the project's first real screens. And after two milestones whose only bugs were
wiring bugs, the first **PlayMode smoke suite** arrives (D45).

The 2019 build died with an unfinished inventory. This is that hurdle, taken with the data model
already built and tested underneath it.

---

## Paper numbers

All authored data, tuned live. Michael's sign-off on the design was explicit that grind-vs-
progression numbers will move.

| Thing | Paper value |
|---|---|
| Sack cap | 200 slots; worn gear excluded; consumable stacks (max 5) take one slot |
| Sell price | ~3 × 2^(rank index, Battlescarred = 1 … Godly = 8) × (1 + 0.04 × required level) — Rusty Lv5 ≈ 29, Shiny Lv12 ≈ 70, Godly Lv60 ≈ 2,600 |
| Shop buy premium | ~3× sell price; potions always stocked; gear rack 3–4 pieces, rerolled per visit |
| Upgrade step | +8% of the stat's rolled value per capacity point |
| Upgrade cost | maxing an item ≈ 2× its own sell price; steps double: base = 2×sell / (2^capacity − 1) → Shiny Lv12 (2 cap): ~47, 93 · Godly Lv60 (4 cap): ~347, 693, 1,387, 2,773 |
| Combine promotion | 2% chance the reroll returns one rank higher |
| Combine result level | max(required level of the two inputs) |
| Elite spawn chance | ~1 in 12, any archetype |
| Elite stats | ~×2.5 HP, ~×1.3 damage |
| Elite loot bonus | +5–10% quality (D22/D23's number) |
| Auto-sell payout | full sell price, lowest-quality unlocked item first |

---

## Planning decisions (engineering, settled at design time)

1. **UGUI + the Input System's MultiplayerEventSystem** for all new UI. Chosen because the couch
   split needs two controllers driving two separate screen halves simultaneously, and that
   pairing is Unity's purpose-built answer. Each player's half gets its own event system with a
   player-restricted root.
2. **The UI can look, never touch.** Every chest-screen action is a request method on the
   Gameplay side (`PlayerInventory`), which mutates and raises a **changed event** the UI
   redraws from. The manual `RefreshStats` choreography dies with the debug panel.
3. **Input at the chest is a map switch, not new polling.** `InputSystemCommandSource` flips the
   player's action map to UI while their screen is open; gameplay commands stop, UI navigation
   flows through the event system, and rule 3 (commands, never polling) is untouched because
   simulation mutations still arrive only as requests.
4. **Only loot-loop surfaces become real UI**: the chest screen, the shopkeeper screen, the
   field drop card (rebuilt in UGUI — its red-X cap state and grab feedback touch it anyway),
   and the minimal global settings menu. Health bars, damage numbers, and the revive HUD stay
   IMGUI placeholders until the art pass — rebuilding them now is scope M6 does not need.
5. **The wallet is Core simulation state**, per player, alongside XP and the ledger. Prices come
   from a pure `PriceBook` (sell, shop premium, upgrade curve) so every number above is one
   authored asset.
6. **Chest and shopkeeper open via contextual Light** (D17's pattern), priority-ordered below
   revive and drop-grab. Solo full-screen pauses via an explicit driver pause flag — never
   `Time.timeScale` (the M5 frozen-clock work is the cautionary tale).
7. **The elite roll is Core spawn logic** with the deterministic RNG; `EnemySpec` gains elite
   multipliers, the drop is rolled at spawn and carried by the enemy, and presentation tints
   the elite's armor with the carried drop's quality colour. `QualityColors` becomes the single
   quality→colour source for drops, elites, and UI alike.
8. **Locks live on the item instance** (a flag), spent capacity alongside it — both persist with
   the item. This lands inside the task-61 constructor regrouping, never on top of the
   16-parameter pile.
9. **Settings are per-player toggles** (auto-equip D30, auto-sell D43) in a minimal pause →
   settings menu; persistence via PlayerPrefs behind the Platform seam.
10. **The PlayMode suite is one fixture, deliberately small** (D45): boot the gameplay scene,
    drive synthetic commands through kill → drop → grab → chest → equip → sell, assert state.
    New `BattleBomb.Tests.PlayMode` assembly. EditMode remains the primary gate.

---

## Tasks

Each task lands with `run_tests` green and one commit, as always.

### 61 — Debts *(M5 close-out notes 3, 5, 6, paid before new weight)*
Regroup `ItemInstance`/`ItemSpec` constructor parameters into cohesive structs (adding lock +
spent-capacity in the process); give `CharacterActor.Step` the driver's named-phase treatment;
move the actor's `Awake` construction to `OnEnable`.

### 62 — The wallet and the price book (Core)
Per-player `Wallet`; `PriceBook` (sell price, shop premium, upgrade cost curve) as pure logic
fed by one authored asset. Tests pin the scaling table above.

### 63 — Sack rules (Core)
Cap, stacks-as-slots, locks, pickup refusal, and auto-sell selection (lowest-quality unlocked).
Tests cover every edge: full sack, all-locked sack, stack overflow past 5.

### 64 — Deepen and gamble (Core)
The upgrade rule (any present stat, +8% steps, doubling cost, capacity gate) and the combine
rule (same definition + rank, both consumed, fresh reroll, 2% promotion, max input level,
investment dies, locked refuses). Tests roll thousands of combines to pin the promotion rate.

### 65 — Elites (Core)
Spawn roll, stat multipliers on `EnemySpec`, drop rolled at spawn with the quality bonus wired
into `GenerationContext`. Tests pin determinism and the bonus math.

### 66 — Gameplay wiring
Chest and shopkeeper world objects with contextual-Light interaction; the checkpoint corner in
the test scene (chest + shopkeeper + M2 dummy); `PlayerInventory` request methods + changed
event; UI action-map switching; solo pause flag / couch half-screen state; elite spawning in the
existing encounters.

### 67 — The chest screen (UI): Item Sack tab
UGUI + MultiplayerEventSystem scaffolding, the 8-wide/4-wide grid, quality backgrounds,
category filter bar, selected-item panel with vs-worn deltas, per-stat upgrade buttons, equip /
combine / sell / lock actions.

### 68 — The chest screen (UI): Hero tab, shopkeeper, settings
Hero tab (allocation + worn loadout, unequip); the shopkeeper screen (sell from sack, buy from
rack); the minimal global settings menu with the two per-player toggles. The debug panel dies
here.

### 69 — Presentation: the chase made visible
Drop under-glow scaling with quality, spawn bounce, grab animation + sound stub, the rebuilt
UGUI drop card with red-X cap state and 200/200 flash, elite armor tinting, money tick and
sell/equip flourish on the chest screen.

### 70 — The PlayMode smoke suite (D45)
The one-fixture loop test. Treat it as the milestone's second gate: it must fail when task 66's
wiring is deliberately broken, or it is not a tripwire.

### 71 — Michael's mega pass
Checklist covers: the full loop solo, the couch split with both players browsing/testing
simultaneously, cap punishment, auto-sell, locks, combining (watch the 2% land at least once via
a debug-forced promotion), elite hunt, shopkeeper round trip.

### 72 — Fixes and close-out
Mega-pass fixes, close-out notes in this file, progress tables, memory.

---

## Standing watches

- **Heavy stays on watch** (D26) — unchanged.
- **O11 (element roster)** — still open with Michael's collaborator; nothing in M6 blocks on it.
- **The reaction table is still empty and runtime-unexercised** — when O11 lands, treat the
  first authored pair as unverified code (M5 close-out note 4). The new smoke suite is the
  natural home for that verification.
- **Offered, not actioned (Michael's call):** leap lift add-with-cap instead of replace; magic
  damage percentage instead of flat.
