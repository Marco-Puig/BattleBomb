# HANDOFF — M6: the loot loop

**Status — complete, 2026-08-21.** Directed by Michael in the M6 design session (visual companion
mockups for the chest screen; five sections approved). Decisions D42–D45 locked. **EditMode
476/476, PlayMode 5/5.**

Michael's pass found one real defect and asked for three changes; all four landed before close.
His verdict on the second pass: *"It all seems to be working correctly."*

The empty reaction table (D41/D46) and the unauthored Earth and Air statuses are **content gaps
Michael and his collaborator own**, not unfinished systems — the same standing they had at M5B's
close.

---

## Build log

| Task | Commit | What landed |
|---|---|---|
| 61 | `7d71555` | M5's debts: item structs regrouped (`ItemIdentity`/`RestorePayload`/`ActivePayload`/`ItemInvestment`, the last carrying D43's lock), `CharacterActor.Step` given named phases, construction moved to `OnEnable` |
| 62 | `0c54bff` | `Wallet` and `PriceBook` — sell, shop premium, and the doubling upgrade curve, with the paper table pinned as law |
| 63 | `5b6fa1e` | Sack rules: the 200-slot cap, stacks as one slot, five-of-a-kind ceiling, absolute locks, opt-in auto-sell, and the refusal with no overflow valve |
| 64 | `076db4e` | Deepen or gamble: the +8% upgrade and the 2% combine, with 20,000 simulated gambles pinning the jackpot rate |
| 65 | `99f47d5` | Elites: the rare spawn roll, the two-way toughening, and the guaranteed drop |
| 66 | `ccc6220` | Chests and shopkeepers as world objects, the per-mode pause, `PlayerInventory` as a request surface with a changed event, elites spawning, the checkpoint corner |
| 66b | `784da02` | Two bugs live verification caught (below) |
| 67 | `7a64521` | The chest screen: Item Sack and Hero tabs, driven by the command stream |
| 68 | `c253a83` | The shop rack, the global settings menu, `Pause` as a system button, and the death of the debug panel |
| 69 | `9688c76` | Drop under-glow and bounce, elite armor tinting, the refused-grab red X |
| 70 | `a8c8ad8` | D45's PlayMode smoke suite, verified against a deliberate break |
| 71a | `0f9e585` | The chest screen you could not leave — the open-press leak, Escape, and the ✕ |
| 71b | `ce412ca` | The item dropdown, upgrading in the right panel, sell values halved |
| 72 | *(this)* | Close-out |

## What Michael's pass found

He approved eight of the ten checks outright. The rest:

- **"Chest opens correctly, but no clear way to close it"** — and it was a real defect, not a
  missing label. The Light press that opened the chest was leaking into the screen on the same
  step and diving the cursor into the action row, so Heavy was faithfully backing him out of a
  level he never chose. Input is now swallowed until every menu button has been released once.
  Escape/Start leaves from anywhere, and a ✕ in the corner is a real clickable button with an
  EventSystem behind it, so a pointer or a tap has a route out (D5's mobile viability).
- **The dropdown.** Selecting an item raises a menu under its own cell instead of a strip along
  the bottom, listing only rows the item can use.
- **Upgrading moved to the right panel**, where each stat shows its before and after and the
  cursor stays put so points can be spent in a row.
- **Sell values cut by half.** Applied to income alone: halving the sinks alongside it would
  have shrunk every number and changed nothing, because the grind is the ratio between them.
  `PriceBook.SellReturn` is the dial.

## Bugs live verification caught (that the tests could not)

Both found by driving the editor rather than by a red test, which is the whole argument D45 was
locked on:

1. **An elite was rolling a Vial of Health to wear.** D22 promises an elite drops what it wears
   and advertises it visibly; a potion makes that a lie. The carried roll is forced into an armor
   slot now.
2. **Drop quality was pinned to the bottom third of the ladder.** Story progress was a hardcoded
   `1`, so nothing above Rusty could ever fall and the loop was literally unjudgeable. It became
   a scene dial like the climate (M7's chapters take it over), and elite drops now spread Torn
   through Legendary.

## Close-out notes — what felt wrong to build

Written for whoever picks up M7, in the M3/M4/M5 tradition of recording the friction rather than
just the result.

1. **The smoke suite earned its keep on its first day, twice.** It caught the chest-close bug the
   moment it was written — and more importantly it caught the *cause* rather than the symptom.
   The obvious fix was "add a close button"; the test failing specifically on `heavy` is what
   exposed the press leak underneath. Every link the suite does *not* cover is a link where that
   would not have happened, which is the argument for widening it as M7 adds flow.

2. **Everything the tests could not see, a human or a screenshot found.** Three defects this
   milestone — the elite wearing a potion, the whole ladder pinned below Rusty, the screen with
   no exit — and not one was findable by unit tests, because none of them was a logic error.
   Two came from driving the editor and looking; one came from Michael playing. Budget for
   looking, not just for asserting.

3. **`ChestScreen` is at the size where it wants splitting.** It is two files and about 700 lines
   holding navigation, a focus state machine, actions, and drawing. The focus enum has grown to
   six states and the transitions between them are implicit in two switch statements. M7's shop
   and any real settings work will add more. Give it a proper state object before that, not
   after — this is the same warning task 50 gave the driver and task 61 gave the actor, and both
   times paying late cost a bug.

4. **Placeholder UI is now load-bearing.** The chest screen is the first UI a player would call a
   feature rather than a debug overlay, and it is built in code with legacy `Text` and no art.
   That was right for M6 and it will be wrong soon: the art pass (D47) has to replace it, and
   nothing about the current layout is authored where an artist could reach it.

5. **The economy has one dial and no data behind it.** `SellReturn` at 0.5 came from a single
   play session's feel. There is no telemetry, no simulated career, and no answer to "how long
   should a Godly piece take". Before M8 that question wants a spreadsheet, not another pass.

6. **The item dropdown has no pointer support, but the ✕ does.** Adding the EventSystem for the
   close button opened a door: half the screen is now clickable-shaped without being clickable.
   Either finish it (rows become buttons) or accept it, but the current state will read as broken
   to anyone who tries to click a menu row.

---

## Deviations from the plan, deliberate and recorded

- **The chest screen is navigated by `PlayerCommand`, not an EventSystem** — planning decision 1
  said UGUI + `MultiplayerEventSystem`. Driving the menu from the command stream is smaller,
  keeps rule 3 exact (devices become commands in exactly one place), needs no extra machinery for
  two players on two screen halves, and lets the smoke suite press buttons the same way a
  controller does. Cost: no mouse support, which a gamepad-first couch game does not need yet.
- **Legacy `UnityEngine.UI.Text`, not TextMeshPro.** TMP's essential resources are not imported,
  and importing a pile of font assets for UI that gets redrawn at the art pass buys nothing.
- **The canvas renders in camera space, not overlay.** An overlay canvas is composited outside
  the camera and is invisible to every screenshot the editor can take — which would have made
  this screen unverifiable by anything except a human at the monitor.
- **`Pause` is a new command button.** D17's five-verb budget is about what one thumb does
  mid-fight; a menu button is not a combat verb. The vocabulary acceptance test now separates the
  two lists rather than being loosened, so the five stay guarded exactly as before.
- **The debug grant survived the debug panel's death**, as a labelled DEBUG row in the settings
  menu. Combining needs two identical items at the same rank and random drops almost never
  oblige — without it, D44's gamble is untestable by hand, which is exactly how M5's reaction
  table ended up shipping unexercised.

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

### 71 — Michael's mega pass *(the open gate)*

Controls the pass needs: **Escape / Start** opens settings. At a chest or shopkeeper the screen
is driven with the **stick** to move, **Light** to select, **Heavy** to go back — and Heavy with
nothing left to back out of closes the screen. Open settings first and fire the **DEBUG** row: it
hands both players two of every starter item, 5,000 coin, and enough XP to have points to spend.

1. **The chest opens where it should, and only there.** Walk to the checkpoint corner (far left)
   and press Light at the chest. Press Light out in the open — nothing should happen.
2. **Solo pauses, couch does not.** With one player the world stops while the screen is open.
   With two, the partner keeps fighting and the browsing player stands still taking no orders.
3. **The comparison reads at a glance.** Select a weapon: does the vs-worn arrow tell you
   whether it is an upgrade without reading numbers?
4. **Deepen something.** Spend a point on a stat; watch the price double for the next one.
5. **Gamble something.** Combine two identical items — the reroll comes back fresh. The 2%
   promotion is rare on purpose; do not expect to see it by hand.
6. **The cap punishes.** Fill a sack (the DEBUG row a few times), then walk over a drop: a red X
   over your head and the count flashed, and the drop stays on the ground.
7. **Auto-sell rescues it.** Turn auto-sell on in settings, grab again — the worst unlocked piece
   is sold to make room. Lock your keeper first and confirm it is spared.
8. **Hunt an elite.** They are about one spawn in twelve, wear a plate tinted the colour of what
   they will drop, and take roughly two and a half times the punishment. Kill one: it drops
   exactly the piece it was wearing.
9. **Read the floor.** Do the drop glows scale the way you wanted — barely there at the bottom,
   a light source at the top?
10. **The shopkeeper round trip.** Sell junk, buy something off the rack, leave and come back to
    see the rack rerolled.

Fast-motion items (the grab animation's snap, the drop bounce) are yours to judge in real time,
per your standing rule — nothing here was slow-motion sampled.

### 72 — Fixes and close-out
Mega-pass fixes, close-out notes in this file, progress tables, memory.

---

## Standing watches, inherited into M7

- **Heavy stays on watch** (D26) — unchanged.
- **O11 resolved as D46** (Fire/Ice/Earth/Air, signature casts) and built as M5B before this
  milestone (`HANDOFF-M5B.md`). The reaction *pairs* for the roster remain open design.
- **The reaction table is still empty and runtime-unexercised** — when the pairs are authored,
  treat the first one as unverified code (M5 close-out note 4). The smoke suite is now the
  natural home for that verification.
- **Earth and Air infusions are provisional wielder passives** (crit, knockback) until their
  statuses are designed (D46 as amended).
- **Three constants are M7's to take over from the scene**: `_lootProgress` (D23's story-progress
  multiplier, currently a driver field defaulting to 2), `StoryProgressLevel` (D36's level stamp,
  still a hardcoded 1 so everything is equippable), and the climate rows M5 left at scene level.
  Chapters own all three.
- **The DEBUG grant row in settings dies with real content** — it exists because combining needs
  two identical items and drops never oblige.
- **Offered, not actioned (Michael's call):** leap lift add-with-cap instead of replace; magic
  damage percentage instead of flat; finishing pointer support so dropdown rows are clickable
  like the ✕ already is.
