# BattleBomb — Roadmap to Early Access

**Status:** agreed with Michael, 2026-09-24. This is the plan from M7's finished machine to a Steam
Early Access launch. `DECISIONS.md` still wins any disagreement — the decisions this plan rests on
are **D54** (Early Access with online co-op), **D55** (AI drafts first, provenance always), and
**D56** (the story is Castle Crashers-thin). `GAME_DESIGN.md` §10 points here for the build order.

The plan is ordered by dependency, not by calendar. There is no launch date on purpose (D54):
a milestone finishes when it is right.

---

## 1. Where the project stands

**The machine is built; the game is not.** M0–M7 took the rebuild from nothing to a working engine
in about a week: combat, enemies, gear, elements, the loot loop, the economy, saves, stage
streaming, tiers, the front door — 626 EditMode and 15 PlayMode tests. UI Pass 01 has since
rebuilt the chest, the shopkeeper's counter, and the hero panel to the Claude Design mockups.

What a player would actually see is almost all placeholder:

| | Now | Early Access needs |
|---|---|---|
| Heroes | 1 (`Default`, a capsule) | 4, one per element, drawn |
| Enemy types | 4 archetypes, primitive shapes | 2 regional families, drawn |
| Chapters | 1 graybox fixture, 2 stages | 2 authored chapters |
| Bosses | 0 | 2 |
| Item templates | 14 | a real catalogue, boss signature items |
| Art in game | none | all of it |
| Sound | none — not a single clip | all of it |
| Online play | none | full online co-op |
| Steam | an empty interface | lobbies, invites, cloud saves, achievements, store page |

**What that means for the plan:** code has been the fast part. The slow parts are the ones that
need Michael — the story, art direction, playtesting, and decisions — so the plan is built around
keeping those moving while the engineering runs ahead of them.

---

## 2. The target: what Early Access contains

| | Early Access |
|---|---|
| **Platform** | Steam, PC. Steam Deck targeted. Consoles and mobile later (D5). |
| **Players** | Two (D11), local couch **and online** (D54). How the two mix, and the topology, settle in M8. |
| **Heroes** | Four — Fire, Ice, Earth, Air (D46). |
| **Story** | Castle Crashers-thin (D56): a goal, four heroes, regions, bosses. |
| **Chapters** | Two, each with its region, enemy family and boss, on all three tiers (D50). |
| **Endless** | Yes — it needs no story and gives the loot chase unlimited runway (D12). |
| **Art and audio** | AI drafts, disclosed, replaced by hand over time (D55). Store art and the four heroes hand-drawn before the store page goes live. |
| **Language** | English (decision due in M13). |

Everything else lives on the post-Early-Access list (§8), so a good idea always has a home that is
not the current milestone.

---

## 3. The order, and why

```
Groundwork → M8 Online co-op → M9 Look & sound → M10 Vertical slice → M11 Endless
           → M12 Chapter 2 → M13 Early Access readiness → launch
```

**Online comes first, before content.** Three reasons:

1. **Networking a finished game is the classic retrofit disaster.** Every boss, effect, and menu
   built before it would have to be rebuilt. Built first, everything after it is online-aware by
   default. D10 built the engine for exactly this: player input already flows as commands through
   one pipe, so a remote player is "just another command source".
2. **It is pure engineering**, so it is the one big item that runs while the story and the art are
   being unstuck — otherwise engineering idles waiting on content.
3. **It is the largest unknown.** Finding out early whether the simulation holds up over the
   internet is far cheaper than finding out late.

**Rejected orders:** slice-first (the original M8) — proves fun sooner, but the slice is blocked on
the story anyway and everything in it gets retrofitted for online; demo-first — earliest
wishlists, but the demo would advertise an online game it cannot show, and it carries the same
retrofit cost.

**The vertical slice keeps its meaning** — the real target, the thing the 2019 build never reached
— and moves from M8 to M10.

**One sanctioned swap:** if the World session (§5.1) slips, M11 Endless may move ahead of M10. It
needs no story, only an environment kit.

### How every milestone runs

The rhythm that carried M4–M7: a **design session** where the milestone has open decisions →
decisions recorded in `DECISIONS.md` and the build written up as `HANDOFF-Mx.md` → an
implementation plan → the build → **both test gates green** (EditMode, then PlayMode with two
players) → **Michael's checklist pass** for anything fast-moving → close-out notes.

---

## 4. Milestones

### Groundwork — before M8 — built (2026-09-25, 56e4be5..527df2b); Michael's pass pending

Small, mostly designed, and blocking nothing but itself.

- **Reconnect the Unity bridge.** It dropped last session. It is the test gate — nothing is done
  without it.
- **Input switching and button icons.** Each player's device is detected and published so the UI
  can observe it (the same shape as the camera fix); prompts show that player's buttons as badges
  in the design's hint-row style (24px, round for face buttons, rounded-square for Start, coloured
  per button). About fifteen hardcoded prompt strings across `ChestScreen.Visuals`, `HeroPanel`,
  `SettingsMenu`, `FrontendFlow`, and `ResultsScreen` move onto it.
- **The menu button map** — designed 2026-08-23 from Michael's answers; the proposed items were
  confirmed at kickoff and recorded as **D57**, which is the authority (it amends D17):

  | Role | Gamepad | Keyboard |
  |---|---|---|
  | Navigate | Left stick + D-pad | WASD + arrows |
  | Confirm | A | Enter / Space |
  | Back | B | Esc — backs out one level; closes the screen from the top level |
  | Item verb 1 | X — Sell the whole stack; Combine-all mid-pick (nothing at the rack: A buys) | J |
  | Item verb 2 | Y — Lock | K |
  | Switch tab / mode | LB / RB | Q / E |
  | Pause | Start | Esc, outside menus only |

  Two rules come with it: **no two buttons do the same job** (Michael: "we should not make them
  redundant"), and **X and Y are only ever shortcuts** — every verb they fire stays reachable by
  choosing the item and then the verb, which is what keeps touch controls possible (D5, D17).
  This inverts today's menus, where X confirms and Y backs out.
- **Two known bugs:** a solo player can be labelled "P2" (HANDOFF-M7's watch list — fix before any
  name is on screen); in split co-op the filter chips overlap the top grid row by ~10px.

**Done when:** both gates green; Michael's pass on switching keyboard ↔ pad mid-menu, icons
following each player separately, and two players on different devices at once.

### M8 — Online co-op

**Designed 2026-09-24** in the Netcode lane's session with Michael — **D58–D62**:

- **D58** — the host's machine runs the one real game; the guest sends button presses and draws
  what the host sends back. Our own thin layer over a transport seam: Steam (Steamworks.NET) for
  PC, a free cross-platform transport later for mobile.
- **D59** — two players in any shape (solo, couch, or one per PC). Friends join at character
  select **or at a checkpoint room mid-run**; a solo game is open to friends by default.
- **D60** — nothing pauses online; the host drives the whole-session moments.
- **D61** — the host holds the guest's gear during a match; the guest keeps loot, XP, and chapter
  credit. The shopkeeper's rack moves into the simulation.
- **D62** — the guest's own movement and swings are predicted, the rest arrives a round trip later.

Evidence: `docs/team/netcode/readiness.md`, `docs/team/netcode/options.md`. Spec and plan:
`docs/HANDOFF-M8.md` and its implementation plan (Netcode lane, in progress).

**Delivers:** a real `IPlatformServices` for Steam, with `NullPlatformServices` still the default
(rule 6); the host / invite / join flow in the front end, including drop-in at checkpoint rooms
(D59); remote players as command sources; state sync and the guest's prediction; the online rules
for screens and pause; per-participant saves and the rack in the simulation (D61); Steam Cloud as
the second `ISaveStore` (D52); PlayMode tests with two networked players.

**Needs from Michael:** the design session (done); **the collaborator as the remote tester** —
there is no second PC; and **the Steamworks account and app ID at M8's close-out** (his choice —
development runs on Valve's test app 480 until then). Valve's onboarding paperwork can take days,
so start it early enough that it does not hold up the two-PC pass.

**Done when:** two players on two PCs over the internet play the fixture chapter start to finish —
fight, revive, grab, chest, shop, wipe, results, save — with a checklist pass from both ends; the
couch plays exactly as before; both gates green.

### M9 — Look & sound

Makes art and sound drop-in, then drops the first of them in.

**Starts with** the art bible (§5.2) and a short engineering session on the rig, animation, and
lighting approach.

**Delivers:**
- **The art pipeline** (D55): the `AI/` and `Hand/` folders, the manifest, stable slots, and the
  test that enforces them — plus the two August traps (a `.cs` file inside `Art/`; duplicate GUIDs
  from importing with `.meta` files).
- **Characters as 2D billboards on one shared skeletal rig** (D15, D16, D47): per-hero art swapped
  in as skins, the invisible shadow-casting proxy (D15), the character lighting treatment, sorting
  driven by the depth axis.
- **Animation that observes the simulation** (rule 2). The state list — idle, run, jump, fall, the
  three Lights, Heavy, charge, the three casts, hurt, downed, revived, grab — comes from the combat
  machine's own phases, so the art knows exactly what to draw.
- **The four enemy archetypes** on the same approach; **weapons as sprites** (the twelve sword
  PNGs finally get homes).
- **Effects:** hits per element, the four signature casts, aura, leap, statuses, level-up, the
  revive heartbeat.
- **The audio system:** mixer buses (music, effects, UI), sounds triggered by simulation events,
  music per region / menu / boss, volume settings — and the exact sound list, generated from the
  game's events.
- **The HUD rebuilt** from an HTML design (Michael's rule), replacing the IMGUI placeholders —
  health, mana, revive, the loot card. Front-end screens designed alongside: title, character
  select, chapter select, results, pause, settings. The text-rendering decision (§6) lands first.
- **The 3D environment kit pipeline** — backgrounds must never compete with shadows (§2.3). Blender
  is connected to the session for blocking out pieces.
- **Everything online-aware:** every effect and sound fires from an event both machines see.
- **The combat feel pass** deferred since M2 happens here, with real art on screen; §2.5's tuning
  targets get recorded.

**Needs from Michael:** the art bible sign-off; AI drafts of the hero template, one hero, and the
four archetypes; the HUD design review; the feel pass.

**Done when:** the fixture chapter plays with drafted art and sound for one hero and all four
archetypes, couch and online; §2.5's targets written down.

### M10 — Vertical slice

**Hard gate: the World session is done.**

**Delivers:**
- **Chapter 1**, authored: Michael designs the stage layouts (he hand-designs story levels, M7);
  the stage data, waves and wiring are built against them.
- **Region 1's enemy family** (D22) and its climate — which also returns the Caster and the Brute to
  play; neither spawns anywhere since M7.
- **All four heroes** authored and on character select.
- **Bosses — the framework and the first one**, starting with a boss design session: patterns that
  mix jump-dodgeable and depth-dodgeable (§6), the signature drop above the level's norm (D23).
- **The reaction pairs** authored and seen working at runtime for the first time (D41, D46).
- **An item content pass:** boss signature items, the weapon roster, equipment actives beyond the
  Ember Stone.
- **A tutorial** folded into the first stage: the five verbs, contextual Light, the chest loop. No
  walls of text.
- **Balance:** the economy spreadsheet M6 asked for, the XP curve, the tier numbers, difficulty by
  player count (§7).
- **A demo build** of Chapter 1 for Steam Next Fest.

**Needs from Michael:** the World session; stage designs; the boss session; AI drafts for the
remaining heroes, region 1, its environment kit, and the boss.

**Done when:** someone who has never seen the game plays Chapter 1 solo, on a couch, and online,
from the title to the boss kill, without help; a Steam Playtest wave with outside players; Michael's
pass. This build is the demo candidate.

### M11 — Endless

**Starts with a design session:** how a run starts and ends, checkpoint rooms every so often and
what a wipe costs, how stages are assembled from the chapters' authored pieces and fed through the
existing stage seam (D4, D48), the difficulty curve, loot keeping pace forever (D12), its own save
namespace (D52), and whether Steam leaderboards are in.

**Delivers:** the generator in Core, with tests; Endless in the front end; its save namespace;
couch and online.

**Done when:** a run climbs past chapter difficulty with drops that keep up, couch and online, and
two runs never look the same.

### M12 — Chapter 2

**Delivers:** region 2 with a different climate so element choice matters (§4); its enemy family;
its boss, built on M10's framework as data and patterns; its stages; its items; both chapters on
all three tiers; Endless picking up the second kit; the pets decision acted on (§6).

**Needs from Michael:** Chapter 2 in the story bible, its stage designs, its drafts.

**Done when:** both chapters on every tier, the unlock chain holds, Michael's pass.

### M13 — Early Access readiness

- **Settings, complete:** volumes; resolution, window mode, vsync, frame cap; control rebinding;
  accessibility — quality that reads without colour (colour is the chase's main signal; the frame
  shapes help), screen-shake toggle, damage-number size and off switch (D20), text size.
- **Steam Deck:** legible at 1280×800 and running well — aiming for Verified.
- **Performance:** a minimum-spec target set by the weakest intended machine (D5), then a profiling
  pass.
- **Steam features:** achievements, rich presence, cloud saves verified.
- **Build pipeline:** versioned builds, a Steam beta branch for testers, crash logs players can
  send, save migration tested across versions.
- **Store and launch:** capsule art, screenshots, trailer, Steam's Early Access questions (this
  roadmap answers them), the content survey and AI disclosure from the manifest, the price, a place
  for players to report bugs.
- **Final QA:** full playthroughs couch and online; a last Steam Playtest wave.

**Done when:** Steam's review passes the build and the store page — and it launches.

---

## 5. The parallel tracks

These run alongside the milestones, mostly on Michael's side.

### 5.1 World — the thin story (D56)

**One World session.** Michael and the collaborator make every call; Claude asks the questions and
organises the answers into a **one-page story bible**. Claude invents no lore.

The bible holds: **the goal** players move toward (never something defended — D48); **setting and
tone**; **the four heroes** — name, look, a one-line personality, why each wields their element;
**per chapter** — the region, its climate element, its enemy family (2–3 elemental skins of the
four archetypes), its boss (the concept, and how it is dodged by jumping and by depth) and the
boss's signature drop; **the shopkeeper**; one line on why Endless exists.

Three design calls sit beside it: do the four heroes play identically apart from their element;
which reaction pairs exist; are pets beyond stat pets in Early Access.

**When:** ideally early in M8, because the art drafts wait on it. Hard gate at M10.

### 5.2 Art and audio (D55)

- **The art bible first:** style frames inside D16 — bold outlines, saturated colour, cel-shaded
  characters in lit 3D environments — and a palette per element and per region. Every prompt and
  every drawing is checked against it, so the drafts stay consistent.
- **Provenance:** folder, filename, and manifest, as D55 sets out; the game points at slots, so a
  hand-drawn replacement is one re-pointed entry.
- **Who does what:** Claude writes each asset's brief and prompt; Michael generates the drafts in
  the AI tool of his choice (Claude cannot generate images in this setup) and redraws on his own
  schedule.
- **Draft order**, by what depends on what:
  1. the hero template — one skeleton cut into body parts every hero fits
  2. the four hero skins
  3. Chapter 1's enemy family
  4. Chapter 1's environment kit
  5. effects
  6. HUD and menus — HTML design first
  7. the first boss
  8. store art
- **Audio:** sound effects from licensed libraries; music drafted with AI per region, menu and
  boss; the same manifest, the same provenance.
- **Redraw by visibility:** the store art and the four heroes are hand-drawn before the store page
  goes live — players judge AI art hardest where it is most visible. Early Access can otherwise
  ship partly AI, disclosed.

### 5.3 Business

- **Steamworks account and app** ($100, refunded once the game earns $1,000) — set up **at M8's
  close-out** (Michael's choice): M8 is developed on Valve's test app 480, and Steam Playtest's free
  closed tests wait for the real app ID. Start the paperwork a few days ahead — it can take that long.
- **The "Coming Soon" store page** goes up once the art is presentable — around mid-M10. Wishlists
  only build while it is live, and Steam requires it up at least two weeks before launch anyway.
- **Steam Next Fest** with M10's demo. It runs in February, June, and October with registration
  deadlines ahead of each; the edition is chosen once M10's end is in sight.
- **Early Access launch:** Steam asks why Early Access, for how long, and what is planned. This
  roadmap is the answer.

---

## 6. Decisions and when they are needed

| Decision | Owner | Needed by |
|---|---|---|
| Online at Early Access | Michael | **done** — D54 |
| The menu map's proposed items (X, Y, LB/RB) | Michael | **done** — D57 |
| Text rendering: stay on the current legacy text, or move to TextMeshPro (sharper text; the Archivo weights resolve with it) | Claude recommends, Michael agrees | Before M9's HUD rebuild |
| Topology, how couch and online players mix, join points, online screens and saves | M8 design session | **done** — D58–D62 |
| The World session | Michael + collaborator | Early M8 ideally; M10 at the latest |
| The art bible | Michael | M9 |
| Hero tuning; reaction pairs; boss design | Michael | M10 |
| Pets beyond stat pets in Early Access | Michael | M12 |
| How Endless works | M11 design session | M11 |
| English only at launch (recommended); the price | Michael | M13 |

---

## 7. Risks

1. **Online is the biggest and riskiest item.** M8 opens with a design session and proves sync on
   the graybox fixture before any polish; Steam Playtest brings in a real remote player. Steam's
   Remote Play Together works for free during development as a testing aid.
2. **The story stays stalled.** The thin story makes it one session, not a script; M8, M9 and
   Endless need none of it; the hard gate at M10 makes a stall visible instead of silent.
3. **Art is made twice.** Stable slots and one shared rig keep a redraw cheap; redraw in order of
   visibility, not all at once.
4. **Michael's time to play and judge is the real bottleneck, not code.** Each milestone ends in
   one checklist pass, as M3–M7 did; Claude verifies the slow, settled states itself. The Unity
   bridge is the test gate, so reconnecting it comes first.
5. **Scope creep.** Early Access scope is written down (§2); new ideas go to §8.

---

## 8. After Early Access

In no fixed order: hand-drawn replacements for the rest of the AI drafts · Chapter 3 onward · the
LittleBigPlanet-style chapter map (a view over `StageSelection`, waiting on art) · the Godly rank
(reserved, D33 as amended) · attacker and unique pets (D34) · more heroes · PvP (D4) · missions ·
consoles and mobile (D5) · localisation.

---

## 9. Small debts, and where each gets paid

| Debt | Source | Paid in |
|---|---|---|
| Solo player labelled "P2" | HANDOFF-M7 watch | **Paid** — Groundwork (G5, 3e6b3bb) |
| Split co-op filter chips overlap the grid | UI Pass 01 | **Paid** — Groundwork (G13, 7c3014f) |
| Archivo 500/600/700 weights missing | UI Pass 01 | With the text-rendering decision |
| Solo camera `_screenFill` (0.72) puts loadout cells over the character | UI Pass 01 | M9, once real sprite heights exist |
| The twelve sword PNGs have no weapon | Art import | M9 |
| Casters and Brutes spawn nowhere | HANDOFF-M7 | M10, region 1's family |
| The DEBUG grant row | HANDOFF-M6 | M10, with real content |
| Earth and Air infusions are provisional | D46 | M10's balance pass |
| Heavy on watch | D26 | Re-judged at M9's feel pass |
| `StageRunner` at 739 lines | HANDOFF-M7 | Watch — split when M11 adds the generator's path |
| `EncounterInputs.Default` falls back quietly | HANDOFF-M7 | M11, when a second stage source exists |
| Offered, not actioned: leap lift add-with-cap; magic damage as a percentage; pointer support on dropdown rows | HANDOFF-M5–M7 | Feel pass (M9); pointer support in M13 |

---

## 10. What to do first

1. ~~**Reopen Unity with the MCP bridge** so the test gate is back.~~ Done.
2. ~~**Groundwork** — confirm the three proposed menu buttons, then it gets built.~~ Built
   2026-09-25 (D57); Michael's pass pending.
3. **Book the World session** with the collaborator — it gates the art.
4. **Pick an AI image tool** for the drafts, and start thinking about the art bible.
5. **At M8's close-out:** create the Steamworks account (start the paperwork a few days ahead).
