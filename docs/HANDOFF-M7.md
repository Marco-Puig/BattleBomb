# HANDOFF — M7: chapters, the story-free half

**Status — designed, 2026-08-20. Not started.** Directed by Michael in the M7 design session
(five sections approved). Decisions D48–D52 locked. Builds on M6's close (`c99e8f5`, EditMode
476/476, PlayMode 5/5).

**What this milestone deliberately is not:** the story. Michael and his collaborator are still
pinpointing the goal that pulls the player through the game — the princesses-and-crystals
question — and until they have, no chapter can be authored. Everything below is the *machine*
a chapter runs in: the data format, the streaming, the saves, the tiers, the front door. It is
proven against a graybox fixture and waits for content. **Draft no lore to fill it.**

---

## What M7 is

The milestone where the arena becomes a game you can put down and pick back up. Six milestones
built a simulation that boots into one test scene with two players already standing in it, and
forgets everything when play stops. M7 gives it a front door, a shape to move through, and a
memory:

- **Chapters made of stages** (D48), hand-built geometry streamed in behind checkpoint rooms,
  so the breather room you designed in M6 is also the loading screen nobody sees.
- **A save** (D51, D52) — one per machine, Castle Crashers style, that remembers the sack, the
  roster, the wallet, and how far you got.
- **Difficulty tiers** (D50), hidden behind names, that make every chapter replayable from the
  start of the game rather than after the credits.
- **The front door** — title, character select, chapter select, results — so two humans can
  pick who is Fire and where they are going.

It also pays the two debts M6 left: the chest screen's implicit state machine, and the sack
living inside each player rather than belonging to the couch (D51 amends D43 here).

The three constants M6 left stranded at scene level — `_lootProgress`, the level stamp, the
climate rows — move into stage data, where D23, D36 and D41 always said they belonged.

---

## The design

### Chapters and stages (D48)

A **chapter** is an id, a name, and an ordered list of stages. Nothing else; order is the only
sequencing logic there is. When the story arrives, writing Chapter 1 is creating one asset.

A **stage** is 5–10 minutes of constant forward progression — never a defence objective — made
of two natures that are authored separately and meet only at runtime:

| The space | The game |
|---|---|
| A small **additive Unity scene**, hand-built in the scene view: floors, walls, dressing, and dumb **marker components** (player spawns, arena bounds, checkpoint-room volumes, the shopkeeper's spot, the exit). | A **stage asset** → `StageSpec`: the ordered arenas, each an ordered list of spawn waves (archetype, region, rank, count, trigger); the climate; the enemy level stamp; the loot-progress value; which arenas are followed by a checkpoint room and whether it has a shopkeeper. |

The rule that makes this legal under D4 and rule 2: **nothing gameplay-relevant lives only in
the scene.** Stage scenes hold geometry and markers; an architecture test loads every one of
them and fails on any component outside the whitelist. The machine consumes a `StageSpec`; a
story chapter's spec points at a hand-built scene, and endless mode's generator will one day
point the same field at assembled prefab chunks. One field differs.

**The Gameplay scene is the machine** — sim rig, both players, camera, UI — and it never
reboots. Stage scenes load additively into it and unload behind the player.

**The checkpoint room is the airlock** (Destiny 2's load zones, on your own floor plan). The
ender-chest rooms M6 built are the stage checkpoints; the one at a stage's end is where the
next stage streams in behind the door while you sell and re-equip, and the stage you finished
unloads once you cross out. Checkpoints may also sit mid-stage where a stretch needs one.

Flow through a stage — arena cleared, gate opens, checkpoint reached, stage complete — is a
pure Core state machine (`StageRun`) under EditMode tests. The scene only shows what it decided.

**Stage selection is a Core model too**: which stages exist, which are unlocked, which is
selected. M7 renders it as a list. The LittleBigPlanet-style map Michael wants — vast, but you
always know where you are — is a second *view* over the same model, built in the art phase,
with an optional map-position field added to stage data then. Nothing in the schema changes.

### Wipes (D49)

Both players down (solo: you down) ends the attempt, per D25. **You keep everything you
grabbed and earned.** You respawn at the last checkpoint or shopkeeper room you reached — which
is where the chest is, so a wipe funnels you straight into the economy: sell, deepen, re-equip,
go again. Enemies since that checkpoint reset. Quitting mid-stage resumes from the same point
by the same rule, so the player learns one boundary, not two.

### Tiers and the gate (D50)

A **tier** is a data row — enemy stat multiplier, enemy level bump, loot-progress multiplier —
three at launch, shown to the player **by name only**. No multiplier is ever displayed; a
player learns what the second tier means by wiping quickly. A dev-only overlay (settings →
debug, compiled out of release) shows the rows and the effective numbers for the running stage.

Applying a tier is one pure function, `tier × stage → EncounterInputs`, producing the numbers
the spawner and D23's quality roll consume. It replaces the driver's `_lootProgress` field and
the hardcoded level stamp outright.

Unlocking is light on purpose, so players *can* overreach:

- Chapter N unlocks when chapter N−1 is beaten on any tier.
- Tier T+1 of a chapter unlocks when that chapter is beaten on tier T.

The gate — *given this progress, is (chapter, tier) launchable?* — is a pure Core function
with EditMode tests on every edge.

### The save (D51, D52)

**No local profiles.** One save per machine, tied to the platform account when
`IPlayerIdentity` supplies one and to a local default when it does not. All local progress is
shared, exactly like Castle Crashers; online play (when D10's deferral ends) is each participant
bringing their own account's save, and reconciling two saves' progress belongs to the
networking milestone.

What the one save holds:

| Scope | State |
|---|---|
| Per save, shared by the couch | **The sack** (200 slots, locks, stacks), **the wallet**, settings (auto-equip, auto-sell), story progress |
| Per character in the roster | XP, level, prestige, the worn loadout, the quick-use slot |
| Story progress (namespaced per D4) | Per chapter: highest tier beaten; the resume point — current stage and last checkpoint reached |

The sack and wallet are shared across the roster — farm with Fire, gear up Ice — and shared by
the two couch players: both chest screens are views over the same 200 slots, and D23's
"whoever grabs it keeps it" stays a moment locally while keeping its meaning online. Endless
mode will add its own progress namespace later without touching the roster, because the loot
chase lives in both modes (D12).

**When it saves:** on every checkpoint-room entry, every stage completion, every chest close,
and clean quit. A crash costs at most what a wipe costs.

**Plumbing:** Core owns the save model as plain data, the mapping to and from simulation
state, the versioned text codec (`JsonUtility` — not on Core's forbidden list; it needs no
scene), and the migration table. Core never touches a disk: the Platform layer gets
`ISaveStore` (named text blobs in, out, list, delete) with a local-file default; Steam Cloud is
a second implementation behind the same interface later. Every save carries a schema version;
older saves migrate forward on load, **newer saves refuse to load** rather than corrupt. Items
save by identity id plus rolled values, never by name.

### The front door

A new **Frontend scene** holds every screen that is not play. Launching a stage loads the
Gameplay scene (the machine) plus the stage's geometry scene additively; returning to the menu
unloads both. The machine never runs without a stage; the menus never carry simulation.

**Title → Character select → Chapter select → Play → Results.**

1. **Title** — continue or start. Continue jumps to chapter select with the resume point
   highlighted.
2. **Character select** — each present player picks an element from D46's roster. **Player 2
   joins here** by pressing a button on a second device, Castle Crashers style.
3. **Chapter select** — the list view (the map's future home), tier picker by name, locked
   entries visibly locked and unlaunchable.
4. **Play** — stages flow through airlocks without returning to the menu. The pause/settings
   menu gains *Return to chapter select*, which saves first.
5. **Results** — on chapter completion: what you grabbed, XP gained, what unlocked; then back to
   chapter select.

The rules carry over unchanged: every menu runs on `PlayerCommand` structs through the screen
plumbing M6 built for the chest; every interactive element is clickable the way the chest's ✕
is (D5's thumbs); players are `PlayerRegistry` slots, never "player 1 is the keyboard."

---

## Paper numbers

All authored data, tuned live. Names are working names — tiers are not lore, but Michael may
still rename them.

| Tier | Enemy HP | Enemy damage | Level bump | Loot progress |
|---|---|---|---|---|
| 1 — Normal | ×1.0 | ×1.0 | +0 | ×1.0 |
| 2 — Hard | ×1.6 | ×1.3 | +10 | ×1.5 |
| 3 — Nightmare | ×2.5 | ×1.7 | +25 | ×2.25 |

| Thing | Paper value |
|---|---|
| Stage length | 5–10 minutes of play; the whole game must not be completable in a day |
| Fixture chapter | 2 stages; stage 1: two arenas, a mid-stage checkpoint, an end checkpoint with shopkeeper; stage 2: three arenas, a different climate, a higher loot progress and level stamp |
| Save schema version | 1 |
| Sack cap | 200, now per save (D51) — one number if the couch finds it tight |

---

## Planning decisions (engineering, settled at design time)

1. **Stage geometry scenes are additive and dumb.** A `StageGeometry` whitelist test in the
   EditMode suite opens every scene under `Scenes/Stages/` and asserts each component is a
   renderer, collider, light, particle system, transform, or one of the marker components (`PlayerSpawnMarker`,
   `ArenaMarker`, `CheckpointRoomMarker`, `ShopkeeperMarker`, `StageExitMarker`). Markers are
   Gameplay-assembly MonoBehaviours holding data only — no `Update`, no logic.
2. **`StageSpec` is a Core struct; `StageDefinition` is the ScriptableObject that authors it**,
   `ToRuntime()` like every other definition. The geometry scene is referenced by name (a
   string) so Core never sees `SceneManager`; the Gameplay stage runner resolves it.
3. **`StageRun` is Core's flow state machine** — arena index, wave index, gate state,
   checkpoints reached, complete — advanced by the driver each step from kill events. The
   Gameplay `StageRunner` spawns waves when `StageRun` says so, opens gates when it says so,
   and never decides anything itself.
4. **The airlock is the stage runner's only scene work**: on entering the end checkpoint room,
   `LoadSceneAsync(additive)` the next stage; on crossing its exit, move both players to the new
   scene's spawn markers, then unload the previous scene. Camera continuity comes from the
   players never teleporting further than the door.
5. **Wipe and resume share one code path**: `StageRun.ResetToCheckpoint()` respawns players at
   the checkpoint's spawn markers and rewinds arena state; loot, XP, and wallet are untouched
   because they were never part of `StageRun`.
6. **The save model lives in `Core/Saves`** as `[Serializable]` classes with
   `[SerializeField] private` fields (rule 7 holds; `JsonUtility` needs fields, not readonly
   structs). `SaveCodec` wraps the envelope (version, payload) and owns the migration table.
   `SaveMapper` converts between the model and live simulation state in both directions.
7. **`ISaveStore` joins the Platform layer** next to `IPlayerIdentity`; `FileSaveStore` writes
   under `Application.persistentDataPath` and is the default in `NullPlatformServices`. The
   save's name is derived from `IPlayerIdentity` when present, `local` otherwise.
8. **The sack becomes session state.** `PlayerInventory` splits: the sack, the wallet, and the
   settings move to a session-owned `SharedStash` the driver constructs once; the worn loadout
   and quick-use stay on the player. Both chest screens observe the one stash and re-validate
   their selection every step.
9. **The chest gets `ChestNavigation`**, a Unity-free model of the six focus states with typed
   transitions (`Enter`, `Back`, `Move`), tested in EditMode. `ChestScreen` shrinks to rendering
   and command forwarding. This lands *before* planning decision 8 touches the chest.
10. **Settings live in the save.** M6 planned PlayerPrefs persistence for its two toggles and
    never built it — they are in-memory flags on `PlayerInventory` today. Under D51 they are per
    save, so they move to the stash and persist with it; no PlayerPrefs path is ever written.
11. **Tier application is `EncounterInputs.From(tier, stage)`** in Core, consumed by the spawner
    and by `GenerationContext`. `SimulationDriver._lootProgress` and the hardcoded
    `StoryProgressLevel` are deleted in the same task, never left as fallbacks.
12. **The Frontend scene is the build's first scene.** It holds a `FrontendFlow` MonoBehaviour
    hosting a Core `FrontendState` machine (title → characters → chapters → launching). Screens
    are UGUI, driven by the command pipeline, with the chest's pointer support pattern. Results
    is a screen the Gameplay scene shows before unloading back to Frontend.
13. **The dev overlay is the settings menu's debug section** (the DEBUG grant row's
    neighbour), guarded by `#if DEVELOPMENT_BUILD || UNITY_EDITOR`.
14. **A second PlayMode fixture, `ChapterLoopSmokeTests`**, paced by simulation frames like
    M6's, walks the whole milestone: boot Frontend → pick a character → launch the fixture
    chapter → clear arena → checkpoint → airlock streams stage 2 → wipe → respawn with loot →
    quit → save exists → relaunch resumes at the checkpoint.

---

## Tasks

Each task lands with `run_tests` green in both modes and one commit, as always.

### 73 — The chest's state object *(M6 close-out note 3, paid before new weight)*
`ChestNavigation` in UI (Unity-free, EditMode-tested); `ChestScreen` reduced to rendering and
forwarding. Behaviour identical; the M6 smoke suite is the regression net.

### 74 — The shared stash (D51)
`SharedStash` owns the sack, wallet, and settings; `PlayerInventory` keeps the worn loadout and
quick-use. Both chest screens observe the stash and re-validate selection per step. Tests cover
two players acting on one sack in the same step.

### 75 — Chapters, stages, tiers, and the stage run (Core)
`ChapterSpec`, `StageSpec`, `ArenaSpec`, `WaveSpec`, `TierSpec`, `EncounterInputs.From`,
`StageRun`, `StageSelection`, and the progress gate. Tests pin flow, tier math, and every gate
edge.

### 76 — The save (Core + Platform)
`Core/Saves`: model, `SaveMapper`, `SaveCodec` with version envelope and migration table.
`ISaveStore` + `FileSaveStore` in Platform. Tests: round-trip of a populated save, v0→v1
migration, newer-version refusal, item identity survives a rename.

### 77 — Authoring and the fixture
`ChapterDefinition`, `StageDefinition`, `TierDefinition` ScriptableObjects; the marker
components; the `Fixture Chapter` asset with two graybox stage scenes under `Scenes/Stages/`;
the stage-geometry whitelist test.

### 78 — The stage runner (Gameplay)
Waves spawned from `StageRun`, gates, checkpoint rooms placed from markers (the M6 chest and
shopkeeper become marker-placed), the airlock streaming, wipe → reset to checkpoint.

### 79 — Tiers applied, the orphans deleted
`EncounterInputs` wired into the spawner and `GenerationContext`; climate read from the stage;
`_lootProgress`, the hardcoded level stamp, and the scene-level climate rows removed.

### 80 — The Frontend scene
Title, character select with join, chapter select with the tier picker and the gate, the
`FrontendState` machine, scene handoff to the machine, *Return to chapter select* in pause.

### 81 — Results and autosave
The results screen; saves on checkpoint entry, stage completion, chest close, and quit; continue
from title resumes at the saved checkpoint.

### 82 — The chapter smoke suite and the dev overlay
`ChapterLoopSmokeTests`, verified against a deliberate break; the tier overlay in settings →
debug.

### 83 — Michael's pass, fixes, and close-out
The checklist is written when 82 lands; it includes the airlock's feel (no hitch, no visible
load), the wipe → chest funnel, and joining at character select.

---

## Explicitly out of scope

Real chapters, bosses, and narrative; the map's art; mid-stage join (Player 2 joins at
character select or not at all); online save reconciliation; the endless generator (only the
`StageSpec` seam it will feed); profile name entry; tier-dependent XP.

---

## Standing watches, inherited from M6

- **Heavy stays on watch** (D26).
- **The reaction pairs** for Fire/Ice/Earth/Air remain Michael's and his collaborator's; the
  table is still runtime-unexercised, and the smoke suites are where the first authored pair
  gets verified.
- **Earth and Air infusions are provisional wielder passives** (D46 as amended).
- **The DEBUG grant row dies with real content** — still true; it survives M7 because the
  fixture is not content.
- **Offered, not actioned:** leap lift add-with-cap; magic damage as a percentage; pointer
  support on dropdown rows.
