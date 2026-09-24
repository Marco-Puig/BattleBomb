# HANDOFF — M8: online co-op

**Status — designed, 2026-09-24. Not started.** Directed by Michael in the M8 design session (eight
decisions, then three design sections approved). Decisions **D58–D62** locked. Starts after
Groundwork (D57) and the Builder's pre-M8 bug batch land. Evidence behind every call here:
`docs/team/netcode/readiness.md` (the audit of the real code) and `docs/team/netcode/options.md`
(the options and why the rejected ones lost).

**What this milestone deliberately is not:** content, art, or a new rule of play. Everything a
player can do online is something they can already do on the couch; M8 makes it work across two
PCs without changing how either player's game feels. **The couch plays exactly as before.**

---

## What M8 is

The milestone where D10's promise is cashed (D54). Seven milestones built a simulation shaped for
networking — player intent as commands through one pipe, a fixed step, a player registry, no
global "the player" — without ever sending a byte. M8 sends the bytes:

- **One PC runs the game (the host); the friend's PC sends its presses and draws what comes back
  (the guest)** (D58). Our own thin layer, carried over Steam today, over any transport tomorrow.
- **Two players in any shape** — solo, couch, or one per PC — and **friends drop in** at character
  select or at a checkpoint room, into games that are open to them by default (D59).
- **Nothing pauses online** (D60), **each player keeps what they earned** (D61), and **the guest's
  own character reacts instantly** on their own screen (D62).

It also pays one debt the audit found: the shopkeeper's rack is rolled and priced by the UI today (a
rule 2 violation); it moves into the simulation (D61).

---

## The design — what online co-op is like

Approved by Michael as written (design §1, 2026-09-24).

- **Starting.** A solo game is open to your platform friends (Steam's, in Early Access) by
  default: they see "Join game", and you can invite from the overlay. A setting closes it. A couch
  game is full and never open. A friend who joins at character select picks a hero from **their own
  save** and you launch together; two online players may pick the same hero.
- **Dropping in mid-run.** A request that arrives mid-fight shows the friend *"Waiting for the host
  to reach a checkpoint."* When the host's run reaches a checkpoint room, the friend's game loads
  and they appear at that room's respawn point — **only once their PC has finished loading**, never
  standing defenceless while it does.
- **During play.** The host plays exactly as today. The guest's running, jumping, turning and the
  start of every swing or cast react instantly on their PC; whether it hit, damage numbers, and
  everything enemies do arrive about a tenth of a second later. **Nothing pauses** — chests, shops,
  the hero panel and settings open full-screen on your own display while the world runs. **The stage
  exit waits until both PCs have loaded the next stage**, so nobody walks into empty space.
- **Loot.** Drops are free for whoever grabs first, as on the couch. What the guest grabs goes into
  **their own** sack; they sort it at any chest; their menu actions take a round trip to show.
- **Saving.** At every D52 autosave moment each PC writes its own player's save. The guest's keeps
  loot, gold, XP, levels, gear, and credit for any chapter and tier finished together. Only the
  host's save remembers where the run is.
- **Leaving.** The guest leaves or drops → the host carries on solo, and solo's rules (the chest
  pause, the solo camera) come back. The host leaves or drops → the guest sees *"The host left"*
  and returns to the title, keeping everything up to the last autosave — what a crash costs.
  **About 10 seconds of silence is a drop**; shorter hiccups freeze the guest's picture and catch up.

---

## The architecture

### Roles

| | Host | Guest |
|---|---|---|
| Runs the simulation | Yes — `SimulationDriver.RunStep`, unchanged | **No.** Its driver runs in **replica mode** |
| Rolls anything (loot, crits, elites, rack, combines) | Yes — the host owns every seed | Never |
| Its own player's input | Local seat, as today | Local seat, **sent to the host** and predicted locally (D62) |
| The partner | A `RemoteCommandSource` fed from the wire | A replica, drawn from snapshots |
| Enemies, drops, bolts, stage | Simulated | Drawn from snapshots and events |
| Saves | Its own participant only | Its own participant only, when the host says a D52 moment happened |

### Where the code goes

```
Core/Net/               pure, EditMode-tested: the byte writer/reader, codecs for commands,
                        snapshots and messages, the input buffer, the snapshot buffer,
                        the prediction log, the handshake state
Platform/               INetTransport, NetPeer, NetChannel; ILobbyService; the loopback and
                        local-socket transports; the lag simulator; PlatformRegistry
Platform.Steam/         NEW assembly, compiled only when Steamworks.NET is installed:
                        SteamPlatformServices, the Steam P2P transport, Steam lobbies,
                        Steam Cloud ISaveStore
Gameplay/Net/           NetSession (lives on the GameSession object, survives scenes);
                        NetHost and NetGuest (live in the Gameplay scene); RemoteCommandSource;
                        ReplicaWorld; the requests seam for menus
```

Dependency direction is unchanged: Core ← Platform ← Gameplay ← UI/Presentation.
`Platform.Steam` references Platform, Core and the Steamworks.NET assembly; **nothing references
it** — it registers itself at startup (planning decision 3). Rule 6 holds: without the package the
assembly does not compile, `NullPlatformServices` is the default, and the game runs offline and
over the local transports.

### What travels

| Direction | Message | Channel | When |
|---|---|---|---|
| guest → host | `Hello` — protocol version, build id, chosen hero, the guest's save payload (that hero's `CharacterSave`, sack, wallet, auto-flags) | reliable | on connect |
| host → guest | `Welcome` — the guest's player id, chapter/stage/tier, the host's hero, the loaded stages, a baseline snapshot | reliable | when the guest may enter |
| guest → host | `Commands` — ack of the latest snapshot, and the last four commands (redundancy against loss) | unreliable | every step |
| host → guest | `Snapshot` — host frame, the last guest command consumed, every player, enemy, dummy, bolt, the drop ids, the stage state, per-player screens/grab counts/refusals, the attempt countdown | unreliable | every 2nd step (30 Hz) |
| host → guest | `Events` — hits, deaths, drops spawned (with the item), screens opened/closed, attempt reset, checkpoint/stage/chapter completed | reliable, ordered | batched per step |
| host → guest | `LoadStage` / guest → host `StageReady` | reliable | at every launch, preload and hand-over |
| guest → host | `Request` — player id, verb, arguments, sack revision | reliable | a menu action |
| host → guest | `RequestResult`, `InventoryState` (the guest's full sack, wallet, loadout, quick slot, ledger — binary, ~8 KB at a full sack) | reliable | after a request; after any change to the guest's inventory, at most once per snapshot |
| host → guest | `AutosaveNow` — the D52 moment, and chapter/tier credit when it is a completion | reliable | at each autosave moment |
| host → guest | `SessionMoment` — results opened, return to chapter select, launch | reliable | host-driven moments (D60) |
| both | `Bye` | reliable | a clean leave |

---

## Paper numbers

Tuned live; recorded here so the first build has somewhere to start.

| Thing | Paper value | Why |
|---|---|---|
| Simulation step | 60 Hz (unchanged) | Feel is a design value; the network adapts to it, never the reverse |
| Command packets | every step, carrying the last 4 commands | One lost packet costs nothing |
| Stick quantisation | 16 bits per axis, **applied on the guest before it predicts** | The host and the guest's prediction see the same number |
| Snapshot rate | every 2nd step — 30 Hz | Combat reads at 30 Hz with 100 ms of interpolation |
| Snapshot size | ~2.5 KB with 2 players, 20 enemies, 20 bolts, no compression → ~75 KB/s | Fine for Steam's relay; **delta compression is the lever if it is not** |
| Guest interpolation delay | 6 steps (100 ms), adaptive to measured jitter | Two snapshots plus margin |
| Host input buffer | target 2 commands deep, adaptive to 6 | Absorbs jitter without adding much delay |
| Starved input | repeat the last *held* buttons and stick; never repeat a press | A lost packet must not become a double jump |
| "Connection problem" banner | after 1 s of silence | |
| Drop | 10 s of silence | Michael's call, design §1 |
| Fake lag — normal | 100 ms round trip, ±10 ms jitter, no loss | A decent home connection |
| Fake lag — bad | 200 ms round trip, ±30 ms jitter, 2 % loss on the unreliable channel | The one to make feel acceptable |
| Protocol version | 1 | Mismatch refuses the join with a readable reason |

---

## Planning decisions (engineering, settled at design time)

1. **The host's simulation is untouched.** `RunStep`'s phases, order and outcomes do not change for
   online. The host hears the guest through `RemoteCommandSource` (an `IPlayerCommandSource`, D10)
   and speaks through `Stepped`-time hooks. If a change would alter how the host's game plays, it is
   the wrong change.
2. **Core/Net is pure and owns the wire.** `NetWriter`/`NetReader` over a `byte[]`, little-endian,
   with quantisation helpers; one codec per message. **Every Core struct that travels gets a field-
   coverage test**: it reflects over the struct's fields, writes random values, round-trips them,
   and fails if any field was not carried — so a field added in M9 cannot silently stop syncing.
   Where a Core struct cannot be rebuilt from its public surface (`Health` and `ManaPool` have
   private two-value constructors; `AttackTuning`'s radial flag is private), Core gains a named
   factory (`Health.FromValues`, `ManaPool.FromValues`, `AttackTuning.IsRadialAuthored`) — additions,
   never behaviour changes.
3. **Platform owns the seam; Steam registers itself.** `INetTransport` (listen, connect, send on a
   channel, poll received messages, connected/disconnected events) and `ILobbyService` (open/close
   to friends, invite overlay, join-requested event, rich presence, leave) join Platform beside
   `ISaveStore`. `PlatformRegistry` is a stateless factory slot: `NullPlatformServices` by default;
   `Platform.Steam` fills it from `[RuntimeInitializeOnLoadMethod]` when present. `GameSession`
   creates its `IPlatformServices` through the registry instead of `new NullPlatformServices()`.
4. **Three transports behind one interface.** `LoopbackTransport` (in-memory pair, for tests);
   `LocalSocketTransport` (**TCP** on localhost/LAN — for two editors under Multiplayer Play Mode;
   TCP because it gives the reliable channel for free and a dev transport does not need real UDP);
   Steam P2P over the relay (`ISteamNetworkingSockets`, connection-oriented so a host leaving is an
   event). A **`LagSimulator` decorator** wraps any of them with seeded latency, jitter and loss
   (loss on the unreliable channel only), so feel is testable on one PC and tests are repeatable.
5. **`NetSession` lives on the `GameSession` object** (DontDestroyOnLoad), so the connection
   survives Frontend → Gameplay → Frontend. It owns the role (Offline / Host / Guest), the
   transport, the peer, the guest's player id, the handshake, and the timeouts. It pumps the
   transport early in the frame (`DefaultExecutionOrder` ahead of the driver), so received commands
   are in the buffer before the driver's `Update` samples them.
6. **The guest never simulates.** `SimulationDriver` gains a replica mode: its clock still runs and
   still samples the local player's command every step (it is sent, and predicted), but `RunStep`
   is replaced by `ReplicaStep`: apply the interpolated snapshot for the render frame, raise the
   replicated events, raise `Stepped`. **Every Presentation and UI observer keeps working
   unchanged**, because actors hold received state exactly where they held simulated state.
   `StageRunner`, `EnemySpawner` and `SaveService` gain replica guards: they load what they are told,
   spawn what they are shown, and save when they are told.
7. **Actors take state from the wire through internal methods.** `CharacterActor.ApplyReplica`,
   `EnemyActor.ApplyReplica`, a replica spawn/despawn on `EnemySpawner`, `DropPickup` by id. Setting
   `_previous` from the last applied state keeps `InterpolatedVisual`'s per-step lerp intact.
8. **Entity ids are session-monotonic on the host.** Enemies take a network id from a counter that
   **never resets** (the spawner's `_serial` resets per stage attempt and is kept for its D28
   arithmetic only). Drops take one from the driver. Dummies and interactables are identified by
   (stage generation, prop index) — both machines spawn props from the same markers. Bolts travel as
   the whole list each snapshot; the guest draws the list.
9. **`HitEvent` crosses as entity references**, `{kind: player | enemy | dummy, id}`, resolved back to
   local components on the guest before `HitLanded` fires there.
10. **Stage flow is the host's; readiness is both.** `StageRunner` raises `StageLoadRequested(stage,
    generation, offsetX)`; `NetHost` forwards it as `LoadStage`; the guest's runner loads the same
    scene at the same offset and answers `StageReady`. **The airlock waits for both**:
    `TryLeaveStage` requires the host's `_next.IsReady` *and* every participant's `StageReady` for
    that generation. At a launch the host holds its first step until the guest is ready (a
    "waiting for partner" hold — nothing is running yet), with the 10 s drop rule as the backstop.
11. **Menus go through a requests seam.** `ChestScreen` stops calling `PlayerInventory.Request*`
    directly and calls an `IPlayerRequests` the host hands it: locally the existing `PlayerInventory`
    behaviour, unchanged for the couch; on the guest a `RemotePlayerRequests` that sends a `Request`
    and reports *pending*. **The player is explicit** (the host overwrites it with the sender's id —
    a guest can only act as itself) and **every request carries the sack's revision**; the host
    refuses a request aimed at a sack that has changed since (`Sack.Revision`, incremented on every
    mutation). The host applies remote requests in a new first phase of `RunStep`, after
    `SampleCommands`. Screens redraw from `Changed`, never from a return value.
12. **The shopkeeper's rack lives in the simulation** (the audit's rule 2 finding). Opening a shop
    rolls that player's rack into the driver (`_racks[player]`) from the loot stream; the screen
    observes it; buying names a rack slot and the price comes from the simulation's `PriceBook`. The
    couch changes only in that the rack now survives a screen rebuild.
13. **Two stashes online, one on the couch.** `PlayerInventory` stops finding "the" `SharedStash`
    with `FindAnyObjectByType`: the binder assigns each player's stash. Couch: one stash, both
    players (D51 unchanged). Online on the host: the host's stash and the guest's, the guest's
    restored from the `Hello` payload. Auto-sell and auto-equip stay on their stash.
14. **Each machine saves only its own participant.** `SaveService.SaveNow` captures the local
    players' actors and stash only. On the guest, `AutosaveNow` triggers the same capture from the
    replicated `InventoryState`, into the guest's own `LoadedSave` (the heroes not played carry over
    exactly as today), plus the chapter/tier credit on a completion. The resume point is written by
    the host only.
15. **Screens: "local players on this display", never "characters in the world".** A
    `PlayerRegistry.LocalCount` (sources that are not remote) replaces `actors.Count` in the chest
    layout (`ChestScreenHost.cs:191` — whose comment already anticipates online) and in
    `CameraRig`'s solo reframe (`CameraRig.cs:135`). `ChestScreenHost` opens screens only for local
    players. Online, `HoldMenuPause` and the solo chest pause are no-ops (`SimulationDriver.IsOnline`);
    a solo host with an open lobby and no guest is not online.
16. **Session moments are the host's.** Results, *Return to chapter select*, and launching run on the
    host and reach the guest as `SessionMoment`; the guest's results screen shows and follows, and
    cannot end the host's.
17. **Late join is a bind, not a boot.** The host's `SessionBinder` gains `BindLate(slot, definition,
    payload)`: activate the second player object, attach the remote source, restore the guest's
    stash and character, place them at the current checkpoint room's respawn point, set their spawn.
    It runs only when the guest reports ready **and** the run is still at a checkpoint room;
    otherwise it waits for the next one.
18. **On each machine, "which seat" and "which player" separate.** The guest's local seat owns every
    local device (Groundwork's seat 0 rule) but speaks as the guest's player id; the host's player
    object for the guest carries a `RemoteCommandSource` in place of its input source. The Player
    prefab gains a disabled `RemoteCommandSource` that `NetHost` enables — and the input source it
    replaces is disabled first, so `CharacterActor` binds the right one.
19. **Prediction is the guest's own character only** (D62, Stage E): a `PredictStep` on
    `CharacterActor` runs the motor, the combat machine and the mana/vitals tick with the local
    command — **no hit windows, grabs, revives, interactions or quick-use**, which are the host's. The
    `PredictionLog` keeps each step's command and predicted state; when a snapshot acknowledges
    command *n*, the guest takes the host's state for itself and replays *n+1…now*. A visual offset
    decays corrections over a few frames rather than snapping.
20. **Versioning is loud.** `Hello` carries the protocol version and the build id; a mismatch refuses
    the join and says why on both screens. The host never trusts a guest's sizes: every count read
    from the wire is bounded.

---

## The build — stages and tasks

Approved by Michael (design §2): each stage ends with something he can try. Three implementation
plans, written one at a time: **Plan 1 = A + B**, **Plan 2 = C + D**, **Plan 3 = E + F + close-out**.
Task numbers continue from M7 (83); Groundwork's are `G1–G14`.

**Before Task 84:** Groundwork (D57) is merged — seats, the menu flags, `MenuPress`,
`IInputDeviceReport` — and the Builder's pre-M8 batch: destroyed enemies unregistered immediately
(readiness §4.3 item 1), the DEBUG grant out of release, seeds from the session.

### Stage A — The remote controller *(Plan 1)*

- **84 — The wire.** `Core/Net`: `NetWriter`/`NetReader`, quantisation, message framing, the
  command codec. EditMode: round-trips, bounds, the stick's quantisation is stable.
- **85 — The transport seam.** Platform: `INetTransport`, `NetPeer`, `NetChannel`, `LoopbackTransport`,
  `LocalSocketTransport`, `LagSimulator`, `PlatformRegistry`. EditMode: loopback ordering, seeded
  lag is repeatable, loss hits unreliable only, a localhost socket pair connects and exchanges.
- **86 — The input buffer and the remote source.** `Core/Net/InputBuffer` (ordering, redundancy
  de-duplication, starvation repeats held never pressed, adaptive depth); `RemoteCommandSource`.
  EditMode, two players.
- **87 — The session and the handshake.** `NetSession`, `Hello`/`Welcome`, version refusal,
  timeouts, the host swapping the guest slot's source; a dev-only *Host local* / *Join local* pair
  on the title (behind `DEVELOPMENT_BUILD || UNITY_EDITOR`). PlayMode: the Gameplay scene hosted,
  Player 2 driven through loopback by a headless scripted guest.
- **88 — Two editors.** Multiplayer Play Mode added, the two-instance scenario, the fake-lag
  profiles selectable. **Michael tries it:** Player 2 moves from the other window.

### Stage B — The mirror *(Plan 1)*

- **89 — Ids and the snapshot.** Entity ids (planning decision 8); the snapshot model and codec for
  players, enemies, dummies, bolts, drops, stage, driver state; the Core factories of planning
  decision 2; field-coverage tests.
- **90 — The host speaks.** `NetHost`: snapshots every 2nd step, events captured inside the step
  and batched, `HitEvent` as entity references.
- **91 — Replica mode.** The driver's `ReplicaStep`; `SnapshotBuffer` and interpolation;
  `ApplyReplica` on actors; replica spawn/despawn of enemies and drops; replicated events; replica
  guards on `StageRunner`, `EnemySpawner`, `SaveService`.
- **92 — The stage follows.** `LoadStage`/`StageReady`; the both-ready airlock; the launch hold.
- **93 — Proof.** PlayMode `ReplicaReplaySmokeTests`: a host run recorded to memory and replayed into
  a replica scene, positions and health compared frame by frame. The hosted smoke suite extended to
  the full fixture chapter with a remote Player 2.
- **94 — Michael's pass (stage B).** Two editors, both lag profiles: how late does the guest feel?
  The answer sizes Stage E. Plan 1 close-out notes.

### Stage C — Menus and saves *(Plan 2)*

- **95** — The requests seam: explicit player, `Sack.Revision`, the host's request phase,
  `RequestResult`.
- **96** — The rack in the simulation.
- **97** — `InventoryState` to the guest; the guest's chest, shop and hero panel online.
- **98** — Online screen rules: no pause online, local-player layout and camera, host-driven session
  moments.
- **99** — Two stashes; the `Hello` save payload; each machine saves its own participant;
  `AutosaveNow`; chapter credit.

### Stage D — Joining and leaving *(Plan 2)*

- **100** — The lobby at character select: a remote slot, host-driven chapter select, launch both.
- **101** — Drop-in at checkpoint rooms: the waiting state, `Welcome` with a baseline, `BindLate`,
  appear-when-loaded.
- **102** — Leaving and drops: `Bye`, the banner, the 10 s rule, the host's live switch back to solo
  rules, the guest's return to title; open-by-default with its setting.
- **103** — **Michael's pass (stage D):** join late, leave, rejoin, pull the plug on each side.

### Stage E — Feel *(Plan 3)*

- **104** — `PredictionLog` and `CharacterActor.PredictStep`.
- **105** — Reconcile, replay, and correction smoothing.
- **106** — **Michael's feel pass** on the guest side, both lag profiles.

### Stage F — Steam *(Plan 3)*

- **107** — Steamworks.NET (UPM, pinned tag), the `Platform.Steam` assembly with its version define,
  `SteamPlatformServices` (init on app 480, identity → the save's name).
- **108** — The Steam P2P transport.
- **109** — Lobbies, invites, join requests, rich presence.
- **110** — Steam Cloud as the second `ISaveStore`.
- **111** — The collaborator's build and a one-page "how to run it" sheet (Steam running,
  `steam_appid.txt` beside the exe); **the first real internet game**.

### Close-out *(Plan 3)*

- **112** — Michael's Steamworks app ID swapped in (created at close-out — ROADMAP §5.3; its
  paperwork started early enough not to block this); the collaborator plays the fixture chapter start
  to finish from both homes — fight, revive, grab, chest, shop, wipe, results, save — with a
  checklist from each end; the couch replayed exactly as before; both gates green; close-out notes.

---

## Testing

Approved by Michael (design §3).

- **EditMode, every task, two players always.** Every codec round-trips; every travelling Core
  struct has a field-coverage test; the input buffer, snapshot buffer and prediction log are pure
  and pinned; the loopback and lag simulator are seeded and repeatable.
- **PlayMode — the wiring tripwire (D45), online.** A hosted run with a headless scripted guest
  over loopback (Stage A on), and a record-and-replay run into a replica scene (Stage B on). Two
  Gameplay scenes cannot share one process (their wiring finds "the" driver), which is why the guest
  in automated tests is headless or recorded.
- **Multiplayer Play Mode, daily.** Two editor instances over `LocalSocketTransport` with a lag
  profile. Heavy on the machine: ask the orchestrator for `QUIET` first.
- **Michael's passes** after B, D and E; **the collaborator** at F and at close-out.

**Done when** (ROADMAP §4 M8): two players on two PCs over the internet play the fixture chapter
start to finish — fight, revive, grab, chest, shop, wipe, results, save — with a checklist pass from
both ends; the couch plays exactly as before; both gates green.

---

## Standing watches, inherited into M8

From M7 and Groundwork, unchanged: Heavy on watch (D26); the reaction pairs unauthored; Earth and
Air infusions provisional; the playable roster unauthored; `StageRunner` size (M8 adds a load hook
and a readiness gate — keep them small); `EncounterInputs.Default`'s quiet fallback.

New in M8:

- **Bandwidth.** The paper snapshot is ~75 KB/s uncompressed. Measure it at Stage B; delta
  compression against the last acknowledged snapshot is the planned lever, not a default.
- **Replica guards are a whole-system property.** Any new `Stepped` subscriber that mutates state
  must be host-only or it double-acts on the guest. Plan 1 adds an acceptance test that lists
  `Stepped` subscribers and fails on one not marked for its role.
- **A snapshot field forgotten is a silent desync on the guest's screen.** The field-coverage tests
  guard the Core structs; the actor-level `ApplyReplica` needs a PlayMode comparison (Task 93) to
  stay honest.

---

## Explicitly out of scope

More than two players (D11); PC↔mobile crossplay and the mobile transport (D58 keeps the door open);
mid-fight drop-in (D59); voice chat; public matchmaking with strangers; host migration (the host
leaving ends the run — D61); delta compression unless Stage B measures a need; anti-cheat;
leaderboards.
