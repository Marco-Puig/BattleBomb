# HANDOFF — M7: chapters, the story-free half

**Status — the machine is complete, 2026-08-21. Awaiting Michael's pass.** Directed by Michael in
the M7 design session (five sections approved). Decisions D48–D52 locked. **EditMode 582/582,
PlayMode 13/13**, up from M6's close (`c99e8f5`, 476 and 5).

Thirteen commits, `fd68210` through `cb4193d`. The story-free half is built and proven against the
graybox fixture; **authored chapters are what remains, and they wait on the story.** Michael's
checklist is below — his pass is the last gate.

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

## Build log

Every task landed with both gates green and one verified commit. Three tasks grew a second
commit (`78b`, `81b`, `82b`) where a review found something worth its own entry in the history.

| Task | Commit | What landed |
|---|---|---|
| 73 | `fd68210` | The chest's focus machine extracted into `ChestNavigation` — M6's close-out debt, paid before M7 added to the screen |
| 74 | `00e53cf` | `Sack` split out of `Inventory` so the couch shares one bag and one wallet (D51); `SharedStash` owns them |
| 75 | `10b149a` | The Core chapter model: chapters, stages, arenas, waves, tiers, `EncounterInputs`, `StageRun`, the progress gate, the picker |
| 76 | `ef59563` | The save (D52): a versioned model in Core, `ISaveStore` behind the Platform seam, migration forward and refusal of the future |
| 77 | `44b95a6` | Authoring assets, the four scene markers, the stage-scene whitelist test, and the graybox `Fixture Chapter` |
| 78 | `85da4e5` | The stage runner: streaming, waves, the clamp as the gate, the airlock, the wipe |
| 78b | `adc63b6` | `LoadedStage` extracted; five bugs a live-driving review found, including two players' worth of teleports |
| 80 | `a7343dc` | The front door — title, character select with couch join, chapter select, and the session that crosses the scene change |
| 81 | `e8b9e28` | Autosave at D52's five moments, the results screen, and the guard that will not overwrite a save it could not read |
| 81b | `6088c67` | The room you wake up in — a wipe or resume now lands *at* the checkpoint, not in the fight past it |
| 82 | `fda4a1b` | The M7 tripwire (D45) and the dev-only tier overlay (D50) |
| 79 | `30babb8` | Tiers applied: `EncounterInputs` reaches the loot roll and the spawner; four scene orphans deleted |
| 82b | `cb4193d` | The second player the suite never had — the two-player regression the milestone most needed |

**Gates at close: EditMode 582/582, PlayMode 13/13.** Up from 476 and 5 at M6's close.

Note the order: **79 was executed last, out of sequence.** It was skipped by mistake during the
long 78/78b review cycle and only noticed when Task 82's implementer went to break the encounter
wiring for a tripwire test and found there was nothing to break. Until it landed, difficulty tiers
changed only enemy health and damage — Hard and Nightmare dropped exactly what Normal dropped, and
stage two's authored level stamp, loot progress and climate reached nothing.

---

## What the reviews found, and why it matters

Every task was reviewed twice — once for spec compliance, once for quality — by agents that drove
the running game rather than only reading the diff. **Every task passed both gates before review.
Most of them still had defects.** The pattern is worth recording, because it is the same one M6's
close-out noted and it has now repeated with more evidence:

- **An inventory event fired twice** on every change (74). Invisible, because every listener was an
  idempotent redraw — and a trap for the first sound effect or analytics counter to subscribe.
- **A stray kill restarted a wave's timer** (75). A wipe racing an in-flight death would silently
  re-lengthen the next spawn.
- **Fourteen gear stats were saved by array position** (76) with only three covered by a test. A
  reorder would have passed the whole suite while transposing every saved item's stats.
- **A checkbox showed the opposite of what the game would do** (77). Ticking "shopkeeper" silently
  forced "checkpoint", and the inspector kept displaying the untruth — to the person who will
  hand-author every real chapter.
- **The gate never opened** (78). `ApplyBounds` was never called on the transition the run makes by
  itself, so a cleared arena was an inescapable box.
- **A player standing still was flung 15 units** across an arena the instant their partner walked
  into a room (78b), and **a downed body slid out of a doorway on its own**.
- **Enemies spawned into the calm room you had just respawned in** (81b) — a direct violation of
  D49, which exists in Michael's own words.
- **A refused save would have been overwritten** by an empty one at the next checkpoint (81),
  losing a player's game permanently and silently.

None of these were caught by a test. All were caught by looking. That is now the third milestone in
a row where that has been true, and it is why D45's suites keep growing rather than being trimmed.

---

## Michael's pass — the checklist

Play from the **Frontend** scene. Keyboard is Player 1; a gamepad joins as Player 2 at character
select by pressing Light.

**Two controllers if you can.** Solo works, but several of the interesting checks are about what
happens to your partner.

1. **The front door.** Title → Start → pick a character → chapter select shows *Fixture Chapter*
   and `[ Normal ]`, with Hard and Nightmare dimmed. Launch.
2. **The first fight.** Grunts arrive from both edges. You cannot leave the arena until they are
   dead; the moment the last one drops, you can walk right.
3. **The checkpoint room.** A chest and a training dummy. Open the chest, close it. Walk on.
4. **The airlock — this is the milestone's headline.** Clear arena 2, walk into the end room
   (chest, shopkeeper, dummy), then keep walking right past it. **No loading screen, no hitch** —
   stage 2's floor is simply there. The floor changes colour at the doorway; that is deliberate,
   marking a new area. Say if you saw a stutter, a pop, or geometry appearing late.
5. **Leave your partner behind on purpose.** Have Player 2 stand at the far left of an arena while
   Player 1 walks into the chest room. Player 2 should **stay exactly where they are**. Two
   versions of this used to fling them across the whole arena instantly; both are fixed and
   pinned by tests, but you are the one who will notice if it still looks wrong.
6. **Die on purpose past a checkpoint.** You should stand back up **in the chest room**, with
   everything you picked up, and **nothing spawning** — that is the "chance to upgrade and equip
   and go again" you asked for. Walking back out starts the fight fresh.
7. **The downed-partner drag.** If your partner goes down and you push on alone, their body gets
   pulled forward one arena at a time — instantly, about an arena's width, no walk. **The rule is
   deliberate**: a body outside the play area is unreachable and forces a full wipe. But the
   *motion* reads as a glitch. **Your call** whether to smooth it (interpolate it, or reposition it
   deliberately so it can carry a visual tell) or leave it.
8. **Tiers.** Hard stays dimmed until you finish the fixture chapter on Normal. Finish it — you get
   a results screen — then launch Hard. Enemies are tougher **and drops should skew better**; open
   settings → *tier overlay* to see the numbers the player never sees.
9. **Save and resume.** Settings → *Return to chapter select*, then Continue from the title. You
   should land back where you were with your sack and wallet intact.
10. **Anything that looks wrong.** Geometry popping, a player stuck on a gate, a chest that will
    not open, a screen you cannot leave.

### Three decisions that are yours, not bugs

- **The roster is not authored.** Character select offers one option, "Default". D46 locked the
  roster as Fire/Ice/Earth/Air and the four *element* assets exist, but no character assets do. The
  question: are the four characters identical tuning with a different element each — in which case
  creating them is near-trivial — or do they differ in movement, health, or kit? The first is
  wiring; the second is design, and nobody should invent it for you.
- **Casters and Brutes no longer spawn anywhere.** The old test scene's hand-authored spawn list
  fielded all four enemy archetypes; the fixture chapter fields only Grunt and Ranged. Correct for
  a wiring rig, but you lose feel-testing on two archetypes — including the Brute, whose whole
  identity is being uninterruptible. Add them to a fixture wave, or wait for real chapters.
- **Two ground materials at the stage boundary.** The seam lands cleanly at the doorway and reads
  as "next area". Whether stages should look *continuous* or *distinct* is an art call.

---

## Close-out notes — what felt wrong to build

1. **The plan was wrong in more places than it was right about the hard parts.** Thirteen distinct
   defects in the plan's own code were found and corrected during execution — a constructor that
   contradicted its own test, a re-join loop that silently dropped Player 2, buttons specified with
   clicks disabled, code placed in an assembly that could not compile it, and the gate that never
   opened. Writing a plan with complete code made execution fast and made *review* the real gate;
   it did not make the plan right. A future milestone should expect the same ratio.

2. **The milestone's safety net was single-player until the last commit.** The PlayMode suite
   existed to catch regressions, and the two most severe bugs the milestone shipped were both
   two-player interaction bugs that it structurally could not see. This is a co-op game; the suites
   should have had a partner in them from the start. `82b` fixed it, and the fix cost ~30 lines,
   which is the uncomfortable part.

3. **`StageRunner` is 739 lines and did not shrink when it was split.** Extracting `LoadedStage`
   bought a clean ownership story and killed two whole classes of bug, but the runner still holds
   streaming, wave spawning, region tests, clamp computation, prop placement and lifecycle. The
   deliberate decision was to keep wave spawning, the position predicates and `ApplyBounds`
   together because they tell one story — "the clamp is the gate". Watch it.

4. **The chest close is the one autosave moment never verified by playing.** Once the airlock clamp
   moves forward the chest is behind an unreachable wall, so it was verified by construction: a
   one-line handler into the same `SaveNow` as the other four, all of which were exercised.

5. **`EncounterInputs.Default` is a deliberate footgun.** A driver with no runner falls back to
   level stamp 1 and loot progress 2 — the M6 values — so the bare Gameplay scene still works for
   the older smoke suite. A future path that forgets to set the encounter therefore produces a
   *plausible* game rather than an obviously broken one. It now warns once, mirroring `Bounds`.

6. **Three small things accepted rather than churned:** `ResultsScreen.Repaint` re-runs a
   `FindAnyObjectByType` every step while its reference is null; `StageRun.StandUpAtCheckpoint`'s
   doc says "exactly" about a state that is not bit-for-bit identical; and `GameSession.Store` has
   a public setter nothing currently misuses.

---

## Standing watches, inherited into M8

- **Heavy stays on watch** (D26) — unchanged since M3.
- **The reaction pairs** for Fire/Ice/Earth/Air remain Michael's and his collaborator's. The table
  is still empty and runtime-unexercised; the smoke suites are where the first authored pair gets
  verified.
- **Earth and Air infusions are provisional wielder passives** (D46 as amended).
- **The DEBUG grant row dies with real content** — it survives M7 because the fixture is not
  content.
- **The playable roster is unauthored** — see Michael's decisions above.
- **A solo player can be labelled "P2".** `PlayerId` comes from `PlayerInput.playerIndex`, which is
  allocated across the session, and the Frontend scene's two `PlayerInput` objects claim indices
  first. Two consecutive sessions gave the single active player index 1 then 0, so the health bar
  and HUD read "P2" then "P1". The smoke suites cannot see it — they replace the device source by
  design (rule 3). Worth fixing before anything ships with a name on screen.
- **The LittleBigPlanet-style chapter map** is a view over `StageSelection`, waiting on the art
  phase. Nothing in the schema changes when it arrives.
- **Offered, not actioned** (carried from M6): leap lift add-with-cap; magic damage as a
  percentage; pointer support on the chest's dropdown rows.

---

## Explicitly out of scope

Real chapters, bosses, and narrative; the map's art; mid-stage join (Player 2 joins at
character select or not at all); online save reconciliation; the endless generator (only the
`StageSpec` seam it will feed); profile name entry; tier-dependent XP.

---

## Standing watches, inherited from M6 *(superseded — see "inherited into M8" above)*

- **Heavy stays on watch** (D26).
- **The reaction pairs** for Fire/Ice/Earth/Air remain Michael's and his collaborator's; the
  table is still runtime-unexercised, and the smoke suites are where the first authored pair
  gets verified.
- **Earth and Air infusions are provisional wielder passives** (D46 as amended).
- **The DEBUG grant row dies with real content** — still true; it survives M7 because the
  fixture is not content.
- **Offered, not actioned:** leap lift add-with-cap; magic damage as a percentage; pointer
  support on dropdown rows.
