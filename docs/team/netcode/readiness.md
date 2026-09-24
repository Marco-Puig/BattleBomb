# M8 readiness audit — what online co-op starts from

**Netcode lane · 2026-09-24 · read against `main` at `bd5f110`, before Groundwork lands.**
Sources: the code itself (file:line throughout), D10/D11/D42/D51–D54, `ARCHITECTURE.md`, the
Groundwork plan. Nothing here is a decision; the options memo (`options.md`) builds on it.

---

## In plain language

D10 did its job. Player intent already travels as small "commands" through one pipe, the game
advances in fixed 1/60 s steps, players are found through a registry, and there are no global
"the player" shortcuts. A remote player can be plugged in as just another command source — that
part is genuinely cheap.

What D10 did **not** give us, because it was never asked to, is a way to *copy the world* from one
machine to another. The game's state is spread across about a dozen Unity objects, much of it in
private fields, and a few things change it from outside the fixed step: the menus, the stage
loader, and the pause. Those are the real M8 work. None of it is a rewrite; all of it is
engineering we can plan exactly.

One finding shapes the topology choice more than any other: **stages load in the background and
change the game the moment they finish loading** (§3, item 4). Two machines finish loading at
different moments. For the "both machines run the game" approach (lockstep) that is a desync on
every stage; for the "one machine runs it, the other watches and sends input" approach
(host-authoritative) it does not matter at all.

---

## 1. What D10 already bought

| Promise | Where it is real | What it is worth online |
|---|---|---|
| **Intent is a command, never a call** | `PlayerCommand` (`Core/Players/PlayerCommand.cs:9`) is a readonly struct: frame, `Vector2` move, held/pressed/released flags. `IPlayerCommandSource.Sample(frame)` (`Gameplay/Players/IPlayerCommandSource.cs:10`) is the only way the sim hears a player. | A `RemoteCommandSource` on the host is a drop-in. A command packs to under 10 bytes (step number, a quantised stick, the held flags — edges are re-derived). Groundwork adds six menu flags to the same `uint` (plan Task 1), so **menu input rides the same pipe too**. |
| **Fixed step** | `SimulationClock` (`Core/Simulation/SimulationClock.cs`), 60 steps/s, pure; `SimulationDriver.RunStep` (`Gameplay/Simulation/SimulationDriver.cs:634`) runs named phases in a fixed order. | Every command and every snapshot can be stamped with a step number. |
| **Players come from a registry** | `PlayerRegistry` keyed by `PlayerId` (`Gameplay/Players/PlayerRegistry.cs`); `CharacterRegistry.Ordered` sorts by id every read (`Gameplay/Characters/CharacterRegistry.cs:20`). Groundwork makes the seat the id (plan Task 5). | Player 2 can live on another machine without anything iterating "the player". |
| **Deterministic iteration order** | Targets sorted by body name, ordinal (`TargetRegistry.cs:11`); enemies named `"{definition} {serial:000}"` in spawn order (`Gameplay/Combat/EnemySpawner.cs:84`). Grabs, revives, and interactables resolve in registry order. | Order-dependent outcomes (who grabs first) are reproducible on one machine. |
| **Seeded randomness, owned by the sim** | `DeterministicRandom` (`Core/Loot/DeterministicRandom.cs`) is a value-type xorshift. Three streams on the driver: loot, combat, spawn (`SimulationDriver.cs:94-96`). | The host's rolls are the only rolls; no RNG ever needs to cross the wire. |
| **No physics in gameplay** | Contact is custom maths: `HitResolver`, `BodySeparation`, `ReviveChannel.FindTarget`, grab/interact radii (`SimulationDriver.cs:950-1002`). The arena is an `ArenaBounds` clamp, not colliders. | No Rigidbody to synchronise; the guest never needs the stage's colliders. *(Confirmed exhaustively in §4.)* |
| **Presentation observes** | `InterpolatedVisual` lerps `PreviousPosition → Position` by `driver.Alpha` (`Presentation/Characters/InterpolatedVisual.cs`); effects hang off a small event set (§2.3). | If a guest's actors hold *received* state instead of *simulated* state, every visual and HUD keeps working untouched. |
| **State is mostly plain values** | `MotorState`, `CombatState`, `PlayerCondition`, `ReviveChannel`, `ManaPool`, `EnemyState`, `Health`, `ProjectileState` are Core structs. | Snapshotting an actor is copying a handful of structs. |
| **Saves are a pure model behind a store** | `SaveModel`/`SaveMapper`/`SaveCodec` in Core; `ISaveStore` in Platform (D52). | Each participant saving their own file is a mapping question, not a storage one. Steam Cloud is the planned second `ISaveStore`. |

---

## 2. Where state lives

Everything the fixed step reads or writes. "Class" means a mutable reference type — it has to be
deep-copied to snapshot, where a struct is copied by assignment.

### 2.1 Per player — `CharacterActor` (`Gameplay/Characters/CharacterActor.cs:56-97`)

| Field(s) | Type | Notes |
|---|---|---|
| `_state`, `_previous` | `MotorState` struct | position, velocity, facing, grounded, coyote, jump buffer |
| `_combat` | `CombatState` struct | phase, current attack/cast, charge, hitstop |
| `_condition` | `PlayerCondition` struct | health, down, stagger, grace, hold |
| `_revive` | `ReviveChannel` struct | the D31 mash channel |
| `_mana` | `ManaPool` struct | |
| `_lungePerStep`, `_lungeStepsLeft`, `_attackRooted`, `_strikeMomentum`, `_leapAvailable` | loose fields | lunge and momentum bookkeeping |
| `_spawnPosition`, `_spawnCaptured` | loose | respawn home (D49) |
| `Statuses` | `StatusTrack` **class** | elemental marks (D40) |
| `_sheet`, `_activeKit`, `_activeMagic`, `_activeTuning`, `_damageScale`, `_weaponClass`, `_infusion*`, `_shotSpeed` | derived | **rebuilt from loadout + allocations** by `RefreshStats` (`:841`) — never need sending, only their inputs |
| definition-derived tuning (`_kit`, `_tuning`, revive tuning…) | derived | from the chosen `CharacterDefinition` |

Its bag: `PlayerInventory` (`Gameplay/Items/PlayerInventory.cs`) holds `Inventory` (**class**,
746 lines in Core: loadout, quick slot, cooldown, settings) and `XpLedger` (struct). The sack and
wallet are **not** per player: they sit in the one `SharedStash` (`Gameplay/Items/SharedStash.cs`),
found with `FindAnyObjectByType` (`PlayerInventory.cs:61`). D51 made that right for the couch; it is
wrong online, where each participant brings their own save (§6).

### 2.2 Per enemy — `EnemyActor` (`Gameplay/Characters/EnemyActor.cs:33-44`)

`MotorState` ×2, `EnemyState` (brain: phase, steps, seeded variation), `Health`, target index,
dying-beat counter, death-reported flag, elite flag, carried drop (`ItemInstance`), `StatusTrack`
(**class**), strike momentum. Configured from `EnemyDefinition` + tier multipliers + a serial seed.
**Identity:** the spawner's `_serial` (`EnemySpawner.cs:24`), reset to 0 at every launch, airlock
hand-over and wipe (`ResetBrood`, `:32`). Unique within a stage attempt; not unique across one.

### 2.3 On the driver — `SimulationDriver`

| State | Where | Notes |
|---|---|---|
| RNG streams ×3 | `:94-96` | seeded from **constant** serialized fields 1, 2, 3 (`:40-47`) — see §7.1 |
| Projectiles | `_projectiles`, `List<ProjectileState>` (`:90`) | no ids; list position only |
| Drops on the ground | `_pickups`, `List<DropPickup>` (`:166`) | each a GameObject; **no ids**, addressed by list index |
| Grab counts, refused-grab timers | `Dictionary<int,int>` ×2 (`:167`, `:174`) | HUD only, but sim-owned |
| Open screens + what opened them | `_openScreens`, `_openSources` (`:182-186`) | D42 |
| Menu pause holders | `_menuPauseHolders` (`:415`) | settings menu |
| Attempt countdown | `_attempt` (`:93`) | the wipe beat |
| Encounter row, arena bounds | `_encounter`, `_bounds` (`:111`, `:531`) | set by the stage runner |
| Chests and shopkeepers | `_interactables`, `List<WorldInteractable>` (`:178`) | registration order |

**Events presentation and UI listen to** — the whole surface, so it is small:
`HitLanded` (carries `Component` references for attacker and target — must become ids on the wire),
`EnemyDied`, `Stepped`, `MenuStepped`, `AttemptReset`, `ScreenChanged`; on the runner
`CheckpointReached`, `StageCompleted`, `ChapterCompleted`; on bags and the stash `Changed`.
Subscribers: `DamageNumbers`, `HitFlash`, `EnemyVisual`, `CastTell`, `ChestScreenHost`,
`SettingsMenu`, `ResultsScreen`, `SaveService`, `StageRunner`, `EnemySpawner`.

### 2.4 The stage — `StageRunner` (`Gameplay/World/StageRunner.cs`)

`StageRun` (**class**, Core: phase, arena index, banked checkpoint, alive count, wave timer), the
current and preloading `LoadedStage`, `_roomBehind`, `_preloadRequested`, the clamp cache, tier.
Stage *geometry* is a scene loaded additively; stage *markers* (arenas, rooms, spawns, exit) are
read from it.

### 2.5 The session — `GameSession`, `SessionBinder`, `SaveService`

`GameSession` (DontDestroyOnLoad, found not static): chapter/stage/tier/resume, per-slot
`CharacterDefinition`s, the store, the loaded save, `StoryProgress`. `SessionBinder` applies it in
`Awake`/`Start`. `SaveService` autosaves at D52's five moments, capturing **every** actor into **one**
save (`SaveService.cs:83-97`).

---

## 3. Side doors — state that changes outside the fixed step

These are the places a second machine would miss, or see at a different moment. Each must either
move inside the step (as a command) or become host-only.

1. **Menu requests.** The chest, shop, and hero panel call `PlayerInventory.Request*` directly
   from UI (`PlayerInventory.cs:145-427`) and read synchronous results. Combine and the shop rack
   draw from the driver's loot stream (`SimulationDriver.cs:263-302`). *Full inventory in §5.*
2. **Screens and pause.** `OpenScreen`/`CloseScreen`/`HoldMenuPause` are public
   (`SimulationDriver.cs:422-468`); `PausedForScreen` stops the clock solo at a chest and for
   everyone under the settings menu (`:412`, `:606-617`).
3. **Session binding.** `SessionBinder.Awake` deactivates empty slots and sets definitions;
   `Start` restores the sack, wallet and ledgers (`SessionBinder.cs:39-130`).
4. **Stage loads land whenever they land.** `LoadSceneAsync(...).completed` (`StageRunner.cs:218`)
   runs `OnStageReady`, which **places every player, full-heals them, sets the encounter and the
   clamp** (`:258-288`) — at a real-time moment, between steps. The airlock is worse: the clamp only
   reaches into the next stage once *this machine's* load finished (`:560`), and leaving the stage
   waits on `_next.IsReady` (`:465`). Load time differs per PC, so **the step at which a player may
   cross the exit is machine-dependent.**
5. **Debug and test entry points** — `RollDebugItem`, `SpawnDebugDrop`, `DebugDownPlayer(s)`,
   `GrantCoins` (the DEBUG grant row). Host-only online; harmless.

---

## 4. Sources of non-determinism

Swept exhaustively over Core, Gameplay, Presentation and UI. **Core is clean**: no `Time`, no Unity
`Random`, no physics, no static state. Divergence lives at the seams. Read this section with the
topology in mind: under host-authority almost all of it is harmless (the host is the only machine
whose answer counts); under lockstep every row is a bill.

### 4.1 Clean, confirmed

- **No randomness but ours.** No `UnityEngine.Random`, `System.Random`, `Guid`, `DateTime`,
  `Stopwatch`, `GetInstanceID`. `GetHashCode` on `ElementId`/`PlayerId` returns the value and only
  keys dictionaries.
- **No physics at all.** No `Physics*`, `Rigidbody`, `CharacterController`, triggers, raycasts. The
  only colliders are on `CreatePrimitive` visuals and are destroyed on creation.
- **One pump.** `SimulationDriver.Update` is the only thing that advances the simulation; no
  `FixedUpdate`, coroutines, `Invoke`, async or threads anywhere.
- **No static simulation state.** The statics are colour, font and sprite caches and UI scratch
  lists.
- **Presentation's `Time` use is visual only** (spins, flashes, damage-number fades, camera
  smoothing).

### 4.2 The RNG streams

| Stream | Seed | Drawn by |
|---|---|---|
| loot | constant **1** (`Gameplay.unity:298`) | each non-elite death (drop roll + generator); **the shop rack when a shop screen is built** (`ChestScreen.cs:161`); **combine and combine-all from the chest UI** |
| combat | constant **2** | every player melee hit, every cast target, every arrow/bolt impact (crits — always drawn) |
| spawn | constant **3** | one elite draw per spawned enemy, plus gear draws for an elite |

Enemy "variation" is not a stream: it is the spawner's serial, used as plain arithmetic
(`EnemyBrain.cs:211,222`). The streams are never reseeded per chapter or attempt; each one's position
is its whole history since the Gameplay scene booted — including shop visits and combines. The
number of draws per item depends on the catalog's contents and order.

### 4.3 What diverges, most serious first

1. **Destroyed things keep acting for the rest of the frame.** Unity's `Destroy` takes effect at
   the end of the frame, and the driver can run up to five steps in one frame. So after
   `ResetBrood` (a wipe, a launch, an airlock hand-over — `EnemySpawner.cs:38`) **living enemies go on
   stepping, attacking and being hit** for the remaining steps of that frame, and a death then lands
   on the rewound run. A despawned corpse (`SimulationDriver.cs:941`) keeps pushing bodies —
   `SeparateBodies` does not skip the depleted (`:850-856`). Old props linger in the registries the
   same way (`LoadedStage.cs:154`). Whenever a frame runs more than one step — any machine under
   60 fps — **this is a single-machine, frame-rate-dependent bug today**, independent of M8. Fix:
   unregister (or deactivate) immediately rather than waiting for `OnDisable`.
2. **Stage loads mutate the simulation when they land** (§3 item 4). Also: until the stage is
   ready, `OnStepped` returns early while the driver keeps stepping players in
   `ArenaBounds.Default`; the runner's every-third-step check uses the global frame
   (`StageRunner.cs:322`), so which steps it lands on shifts with load time.
3. **The chest UI draws from the loot stream** — combine, combine-all, and the shop rack, which
   is only rolled if the screen is actually built. A machine that does not draw that screen skips
   the draws and every later drop diverges. The UI's cursor also uses wall-clock key repeat
   (`ChestScreen.cs:259-267`), so *which item* a held direction lands on depends on frame timing.
4. **Pause stops one machine.** `HoldMenuPause` and `PausedForScreen` freeze the local clock; while
   paused, commands are sampled and menus run once per rendered frame (`SimulationDriver.cs:608-616`).
5. **Player id vs slot.** Today the id is `PlayerInput.playerIndex`, assigned in join order
   (`InputSystemCommandSource.cs:39`), while definitions bind by slot (`SessionBinder.cs:67-72`), and
   every per-player index (enemy targets, attack tokens, revive target, spawn offsets) hangs off the
   id. **Groundwork Task 5 fixes this** — the seat becomes the id.
6. **Unstable sort on possibly-equal names.** `TargetRegistry` sorts by name with `List.Sort`
   (not stable), and names repeat: dummy clones, the serial restarting after `ResetBrood` while
   item 1's lingering enemies still hold the old names, elites renamed after registering. Hit order,
   crit draws, attack tokens and separation all follow this order. Deterministic on one machine for
   the same history; fragile across two.
7. **The clock is wall-time.** `Time.deltaTime` with a five-step cap (`SimulationDriver.cs:619`,
   `SimulationClock.cs:69-72`): how many steps a frame runs, and when time is dropped, is
   per-machine.
8. **Floats.** 588 `float`s across 62 of Core's 101 files; every position and velocity a `Vector3`;
   Euler integration; outcome thresholds compared as floats (crit, drop, elite, quality rank).
   No `Sin`/`Cos`/`Atan2`/`Exp` in simulation code. `Sqrt` and `normalized` (correctly rounded,
   low risk) in targeting, separation, hits, projectiles and stick normalisation. **`Mathf.Pow`**
   (not correctly rounded; depends on the platform maths library) in `XpCurve` and `PriceBook`. The
   same x64 build is very likely bit-identical to itself; Mono vs IL2CPP, ARM (Steam Deck is x64, but
   D5's later targets are not), and `Pow` are where it breaks — and the raw analog stick value is a
   float *inside the command*.
9. **Each machine starts from its own save and selection** — expected, but it means lockstep would
   first have to agree on a starting world.
10. **Saves are written synchronously inside the step** (`SaveService.cs:97`); a slow disk is a
    hitch, and a long enough hitch drops time (item 7).

**For host-authority, only items 1, 3 and 4 need work** — item 1 because it is a bug anyway, 3 and 4
because they are side doors (§3). Items 2 and 5–10 cost nothing: the host's answer is the answer.

---

## 5. UI actions that mutate state

Swept exhaustively (every file under `UI/` and `Presentation/`). Line numbers are today's; the
Groundwork plan rewires the chest, settings, results and front door onto the new menu buttons
(Light/Heavy → Confirm/Back) but does not change *what* they mutate.

**Read-only, verified:** `HeroPanel`, `JunkBar`, `RackRow`, `ActionPopover`, `ComparePanel`,
`ItemCell`, `ChestNavigation` (cursor state only), all of `UI/Combat/*`, `CommandDebugOverlay`, and
all of Presentation (`CameraRig` only reads).

### 5.1 The chest, shop and hero panel — already one request layer

`ChestScreen` calls `PlayerInventory.Request*` for every item verb: allocate, sell-junk, equip,
quick-consumable, sell, lock, unequip, lock-worn, buy, combine-all, combine, upgrade, upgrade-worn
(`UI/Chest/ChestScreen.cs:337-655`). This is the best seam in the codebase for online — each becomes
a `{player, verb, arguments}` message applied inside the host's step. Four things stand in the way:

1. **The player is implied, never passed.** It is whichever `PlayerInventory` the screen was bound
   to (`ChestScreenHost.cs:166-179`). A message needs an explicit id.
2. **Arguments are sack indices** into a sack the partner can reshuffle. The UI already cancels a
   pending combine because of it (`ChestScreen.cs:316-326`); over a round trip it becomes routine.
   Requests need stable item ids, or a sack revision number the host checks.
3. **The shop is decided by the UI.** The rack is rolled in `ChestScreen.Bind` via
   `ChestScreenHost.RollStock` → `SimulationDriver.RollShopStock` (`ChestScreenHost.cs:45`,
   `SimulationDriver.cs:287`), stored in the UI (`ChestScreen._stock`), priced in the UI (`:508`), and
   `RequestBuy` trusts the item and price it is handed (`PlayerInventory.cs:406-427`). That is a rule
   2 violation today and a hard blocker online: the rack must live in the simulation and "buy" must
   become "buy rack slot N".
4. **Results are read synchronously** (`RequestCombine` hands back a `CombineResult` the screen
   shows). Online the answer arrives a round trip later; the screen must redraw from the event, not
   the return value.

`PlayerInventory.Inventory` also hands the UI Core's mutable `Inventory` — nothing misuses it today,
but a read-only view would close the bypass. `RequestQuickEquipment` and `TryPrestige` are never
called from UI.

### 5.2 Everything else that changes state from a screen

| What | Where | Why it matters online |
|---|---|---|
| **Closing a screen** — also triggers an autosave | `ChestScreenHost.RequestClose` → `SimulationDriver.CloseScreen` (`:458`); `SaveService.cs:208-214` | A guest's close must reach the host; which machine writes which save is M8's call. |
| **Settings pauses everyone** | `SettingsMenu.cs:89/100` → `HoldMenuPause` | The world cannot stop online. |
| **Auto-sell / auto-equip** | `SettingsMenu.cs:174-178` → `PlayerInventory.SetAutoSell/SetAutoEquip` | Not UI settings: **shared, saved state** the pickup path reads (`Inventory.cs:128,509`; `SaveModel.cs:17-18`). Host-owned requests. |
| **The DEBUG grant** | `SettingsMenu.cs:163` → loot, coins, XP for every player | **Ships in release builds** (not behind `#if`, `:124-128`). Must be compiled out or host-only before anyone plays online. |
| **Return to chapters** | `SettingsMenu.cs:138` → `SaveNow` + `LoadScene("Frontend")` | One player tears down the machine for both. Host-driven online. |
| **Results pause and leave** | `ResultsScreen.cs:162`, `:129-149` — **any** player's press leaves | Host-driven online. |
| **The front door** | `FrontendFlow.cs:74-265` — session, save read, `FrontendState` join/pick/ready, chapter/tier, `LaunchNow` → `LoadScene("Gameplay")` | Couch-only; slot 0 is in charge; devices and slots are fixed scene wiring. Needs a host-owned lobby. |

### 5.3 Three smaller traps

- **"How many players" means two things.** The chest's pause rule counts characters (correct
  online: two characters, never pauses), but the **same count** picks the couch half-screen
  (`ChestScreenHost.cs:191`) and turns off the solo camera reframe (`CameraRig.cs:135`). Online each
  display has one local player and should get the full-screen layout — these need "local players on
  this display", not "characters in the world".
- **Pointer clicks carry no player.** uGUI's event system reads mouse/touch directly and always
  acts as slot 0 (`ChestScreen.Visuals.cs:212`, `FrontendFlow.cs:305-306`, `ResultsScreen.cs:203`).
  Fine per machine; must never be forwarded as "the host's click".
- **Paused, the frame stops.** Commands are still sampled while solo-paused but at a frame that
  does not advance (`SimulationDriver.cs:614`), so a request scheme stamped with the step number
  needs its own counter for menu time.

The couch-save edge case this sweep also confirmed (two couch players on the same hero share one
character save) is recorded in §7.2.

---

## 6. What each topology would still need

**Host-authoritative** — the host's machine runs the one real simulation exactly as today; the
guest sends commands and receives state.

| Work | Size | Why |
|---|---|---|
| Transport seam + Steam implementation + a loopback one for tests | M | Platform layer; Steam absent must still build (rule 6). |
| `RemoteCommandSource` on the host, with a small jitter buffer | S | The D10 dividend. |
| Entity ids for enemies, drops, projectiles | S | Serial exists for enemies; drops and bolts have none. |
| Snapshot structs + writer/reader for players, enemies, drops, bolts, stage phase | M | §2 is the field list. Derived stats never travel. |
| **Replica mode** on the guest: actors hold received state, the driver does not step | M | Keeps every Presentation/UI observer working unchanged (§1). |
| Event replication (`HitLanded` → ids, deaths, drops, screens, checkpoint/stage/chapter) | S | Small surface (§2.3). |
| Guest-side prediction of the guest's own movement (and attack start) | M–L | Latency feel. Motor and combat machine are pure Core, so it is possible without new rules. |
| Menu requests as network messages with an explicit player and stable item ids; UI redraws from the event, not the return value | M | §5.1. |
| The shop rack moves into the simulation; "buy" names a rack slot | S | §5.1 item 3 — a rule 2 fix the couch also benefits from. |
| "Local players on this display" for layout and camera | S | §5.3. |
| The DEBUG grant compiled out of release, host-only in dev | S | §5.2. |
| Stage flow: host loads and decides, guest loads to follow; a "both loaded" gate at the airlock | S | Side door 4 becomes harmless. |
| Per-participant stash and save capture | M | §6 of D51 said this was M8's problem. |
| Online pause rules (never stop the clock) | S | D42 already chose full-screen-and-live online. |
| Lobby / invite / join in the front door | M | `FrontendState` is couch-only. |

**Deterministic lockstep (or rollback)** — both machines run the simulation from both players'
commands.

| Work | Size | Why |
|---|---|---|
| Everything in the host list above **except** snapshots, replicas and prediction | — | Transport, lobby, saves, pause and menus are needed either way. |
| Every side door (§3) turned into a stepped command — including stage loads, which need a both-machines barrier and must not mutate state on arrival | L | Side door 4 alone touches the airlock, the resume, and every launch. |
| Bit-identical floats on two PCs, forever | L, **open-ended** | Every `Vector3` in the game is `float`; one divergent `Mathf` result anywhere is a desync found only by a checksum mismatch minutes later. *(§4 sizes it.)* |
| Desync detection: per-step world hash, exchanged and compared | M | Without it, bugs are invisible. |
| Input delay on **both** players' every action (lockstep), or save/restore of the whole world and re-simulating several steps per frame (rollback) | M / L | Rollback needs the same world snapshot host-authority needs, *plus* re-simulation cost, *plus* rollback-safe effects (a hit spark must not fire twice). |

The memo compares these properly; the audit's point is only that lockstep keeps every cost of
host-authority except the snapshot, and adds an open-ended one.

---

## 7. Other findings

1. **Every run rolls the same loot.** The three seeds are serialized constants 1, 2, 3
   (`SimulationDriver.cs:39-47`) re-read in `OnEnable` (`:581-583`). Each Gameplay boot therefore
   replays the same drop, crit and elite sequence. Not an online problem (the host owns the rolls) —
   a design one, flagged for the orchestrator: seed from the session at launch.
2. **Two couch players on the same hero share one character save.** The roster saves by element
   (`SessionBinder.cs:132`, `SaveMapper`), and `FrontendState` allows equal picks (`:29-32`). Online
   it cannot happen (two saves); on the couch it is a latent overwrite. Flagged, not M8's.
3. **Steam is entirely absent**, as rule 6 requires: `IPlatformServices` has only
   `NullPlatformServices`; no Steamworks package. `steam_appid.txt` at the repo root holds **480** —
   Valve's public test app (Spacewar) — so lobby and P2P work can begin before Michael's own app ID
   exists. `com.unity.multiplayer.center` is installed (the hub only — no netcode package).
4. **`HitEvent` carries `Component` references** (`Gameplay/Simulation/HitEvent.cs:12-13`) — fine
   locally, must become entity ids to cross a wire.
5. **The shopkeeper's rack rolls when the screen opens**, from the loot stream — a host-side roll
   whose result the guest's screen must be sent.
6. **Groundwork changes the ground under M8, helpfully:** seats replace `PlayerInput` and the seat
   becomes the player id; `IInputDeviceReport` is explicitly "not implemented by a remote peer"
   (plan Task 5 Step 4). A remote seat is a third kind of source the registry already expects.

---

## 8. Questions this hands the design session

Carried to `options.md` and then to Michael, one at a time:

1. Topology (§6).
2. Whose inventory is authoritative online — the host holds the guest's, or each player holds their own?
3. What a guest's save keeps: loot and XP surely; story completions and resume point?
4. Join points: character select only, or mid-run.
5. Online screens: chest and settings with the world live (D42 already says full-screen, live).
6. Feel: how much of the guest's own character is predicted.
