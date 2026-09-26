# Lane: Netcode

**Mission:** get **M8 — online co-op (D54)** ready to build the moment Groundwork lands: know what
the engine already gives online play and what it lacks, lay out the real options, run M8's design
session with Michael, then write the spec and the implementation plan. You design and plan; the
Builder builds (unless the orchestrator hands you the sim).

**Owns:** `docs/team/netcode/` (research, options, drafts, spikes) · `docs/HANDOFF-M8.md` (new) ·
M8 plan files under `docs/superpowers/plans/` (new) · this file. **No writes under `Assets/`.**

**Read first (in order):** `CLAUDE.md` · `docs/team/PROTOCOL.md` · `docs/team/BOARD.md` · this file
· `docs/ROADMAP.md` §3 and §4 "M8" · `docs/DECISIONS.md` D10, D11, D42, D51, D52, D53, D54 ·
`docs/ARCHITECTURE.md` · `docs/HANDOFF-M7.md` close-out notes · the Groundwork plan
(`docs/superpowers/plans/2026-09-24-groundwork-input.md`) — it replaces the input layer M8 builds on.

---

## The work, in order

1. **Readiness audit** → `docs/team/netcode/readiness.md`. What D10 already bought (commands, the
   fixed step, the player registry, no singletons) and what each topology would still need. Read the
   real code: `SimulationDriver`, `PlayerRegistry`, `IPlayerCommandSource`, `PlayerCommand`,
   `CharacterActor`, `DeterministicRandom` and its streams, `PlayerInventory`'s request surface, the
   solo pause, `SaveMapper`. Name every place state lives, every source of non-determinism
   (floats, Unity physics, `Time`, iteration order), and every UI action that mutates state.
2. **Options memo** → `docs/team/netcode/options.md`. Topology (host-authoritative vs lockstep or
   rollback), transport and lobbies (Steam networking and friend invites; which Steamworks wrapper;
   whether Unity's Netcode packages fit a custom simulation or state sync is written over the
   command model), and test tooling (Multiplayer Play Mode, Steam Playtest, a dev app id). Use
   Context7 for current library docs — do not work from memory. End with a recommendation and why.
3. **The design session** → with Michael, in this session. Agenda: the questions in `ROADMAP.md`
   §4 "M8". One decision at a time, your recommendation first, plain language (he is not
   technical). Record every answer in this file. Send the orchestrator the draft `D` entries.
4. **Spec and plan** → `docs/HANDOFF-M8.md`, then an implementation plan with
   `superpowers:writing-plans`, in the style of the Groundwork plan: exact files, full code, tests
   (two players always — CLAUDE.md), gates, one `DONE` per task.

**Spikes** that need Unity (a latency test, a Steam lobby smoke test) need the sim: `LOCK REQUEST`
with the reason and a time estimate. Draft spike code under `docs/team/netcode/spike/` first.

---

## Waiting on

**COMMITTED for the Plan 2 DONE** (sent 2026-09-25: plan file, m8-plan2-pass.md, this file).
Orchestrator ACKed ONLINE (2026-09-25). **Reply address:
`uds:\\.\pipe\LOCAL\cc-msg-e4fdbd589a82ce7d4a7fdf41546e0095`** (name "Battlebomb").

### Plan 2 brief from the orchestrator (2026-09-25, verbatim points)
1. BOARD "Carried into later tasks → Plan 2": unregister-by-id race (unregister only when TryGet
   returns this source; RemoteCommandSource AND InputSystemCommandSource); lost refusal reason;
   UseSeat assumes P2 authored active; guest's front door stays live; abandoned body holds gates
   (D60/D61); Task 103 drop-in baseline (DropSpawned for every drop on the ground before first
   snapshot); ScreenChanged missing on replica; HoldMenuPause online (menu-open count separate from
   world pause — `The_guests_own_menu_never_reaches_the_host` guards it); results screen holding a
   remote P2's last buttons; load generation for stale BeginLoad completions; mid-match rejoin
   deadlocks the airlock.
2. Shop rack into the simulation (RequestBuy trusts UI's item and price).
3. Reassess **candidate F4** (tap lost between samples) and **G8's catch-up-loop pause item** each
   on its own (F1: Destroy immediate on 6000.5.8f1 — no longer one family). Place in Plan 2 or 3, or
   say why neither.
4. TargetRegistry.Ordered live-list watch + EnemyDied handler that throws: say whether Plan 2
   touches them.
5. Leave Plan 3 items alone (SocketException seam, delta snapshots, Task 107 skipped frame).
Constraints: Michael declined screen control — clone-side checks go into a harness (HeadlessGuest,
PlaybackTransport) or one checklist; put Plan 2's checks in a section list at the end and start
`docs/team/m8-plan2-pass.md` (allowed: "plus the pass file if you start it"). Mark every task that
needs QUIET. PlayMode runs async only. Same-hero couch save = Michael's open call — if Plan 2's
saves depend on it, send QUESTION with a recommendation; don't pick. Michael hasn't run Plan 1's
pass: write Plan 2 so a Plan 1 finding lands as a small inserted fix, not a rewrite.
**Builder's lesson (builder.md close-out):** whole-file replacement blocks silently revert later
work — use anchored edits; tell, don't infer (every event carries its step); PlayMode per-step facts
from `Stepped`, never `Frame` between yields; mixed CRLF/LF (CRLF: StageRunner, SimulationDriver,
TrainingDummy, LoadedStage, GameSession, SessionBinder, SaveService, FrontendFlow; LF: Net files +
tests). When done: DONE with plan path (+ pass file).

## DONE SENT (2026-09-25) — M8 Plan 2 written; waiting for COMMITTED

Paths (don't touch until COMMITTED): `docs/superpowers/plans/2026-09-25-m8-plan2-menus-saves-joining.md`
(10,134 lines, Tasks 97–105), `docs/team/m8-plan2-pass.md` (Michael's one sitting), this file.
Last-minute additions after the design notes below: NetDevOverlay "Go quiet 4 s / 12 s" buttons
(switch the NetSession component off; for Michael's D4 drop check) and `NetSession.JoinLocal` moving
the joining editor to save name `local-guest` (two editors share one save folder). Anchor check:
every "replace this block" anchor in the plan exists in the code at f393d98 or in an earlier task's
new code (one composite, 103 (e), verified by hand). Next after COMMITTED: be the Builder's reference
for Plan 2; Plan 3 (106–114) after Michael's Plan 1 lag table (Task 96) + 96's bandwidth re-measure;
Plan 3's first task = F4 (latch press edges between samples).

## (history) writing M8 Plan 2 (Tasks 97–105)

Plan 1 is **built** (86–95 committed, f393d98; 96 = Michael's pass, not done yet — Michael: "I
haven't done any of the playtests yet, but you can continue"). Writing Plan 2 against the code **as
built**, not as Plan 1 described it. Inputs: `docs/team/builder.md` (Plan 1 build log + close-out
draft), `docs/team/m8-plan1-pass.md`, BOARD "Carried into later tasks" → Plan 2 list, candidate F4
+ catch-up-loop pause item (assess). Output:
`docs/superpowers/plans/2026-09-25-m8-plan2-menus-saves-joining.md` (drafted in parts under the
scratchpad `plan2/`, then assembled).

**Progress (scratchpad `plan2/`):** part0 (header, carried-items table, file map), part1 (97),
part2 (98), part3 (99) WRITTEN. Next: part4 (100), part5 (101), part6 (102), part7 (103), part8
(104), part9 (105 + pass sheet + self-review), then assemble → plan file, write
`docs/team/m8-plan2-pass.md`, DONE. Design change while writing: **no "Replica Stash" on the
guest** (YAGNI — the guest never simulates its partner's sack); only the host gets a second
"Guest Stash" in 101. Test fixtures created so far: OnlineMenuSmokeTests (host side, 8 tests after
99), GuestMenuSmokeTests (guest side, 5). Counts after 99: EditMode 817, PlayMode 80.
part4 (100) WRITTEN. **Renumbered while writing:** NetMessageKind Request=14, RequestResult=15,
Participant=16, **Moment=17** (AutosaveNow + SessionMoment merged: `MomentKind` ChapterCompleted=1,
CheckpointReached=2, StageCompleted=3, ScreenClosed=4 in `Core/Net/SessionCodec.cs`), LobbyPick=18,
LobbyState=19. Versions 97→3, 99→4, 100→5, 101→6, 102→7, 103→8. After 100: EditMode 820, PlayMode 87.
100 made `SimulationDriver.IsReplica` public and added public `IsOnline {get; internal set;}`;
guest chapter end = `StageRunner.ReplicaChapterCompleted()` raising ChapterCompleted (results + guest
SaveService credit). `CameraRig.FramingOneScreen` public; PlayMode asmdef now refs Presentation.
Debug grant moved into `PlayerRequestRunner.DebugGrant` (#if); acceptance test scans runner too.
SettingsMenu guest row = `LeaveTheGame()` (Leave + load Frontend; 101 adds the mirror save).
MomentKind trimmed to ChapterCompleted=1, CheckpointReached=2, StageCompleted=3 (guest saves on its
own chest close locally). part5 (101) WRITTEN: LobbyPick {roster, ready, Brought SaveGame} in
`Core/Net/LobbyCodec.cs`; `SaveMapper.ParticipantFrom(save, elementId)`; NetSession host-side
`GuestPick/GuestReady/GuestBrought/GuestLobbyChanged`, guest `SendLobbyPick(roster, ready)` +
101-only auto-send on Welcome (`StandInPick`, 102 removes); `PlayerInventory.UseStash`,
`MirroredFromHost`; binder `GuestDefinition`, `RestoreGuest`, "Guest Stash"; SaveService local-only
+ replica gate `GuestCopyArrived()`; NetHost `SendMoment` for chapter/checkpoint/stage; NetGuest
`_saves.SaveNow()` on checkpoint/stage moments. HeadlessGuest `Pick/Bring/AutoPick/SendPick`.
After 101: EditMode 825, PlayMode 93.
part6 (102) WRITTEN: `FrontendState.SetRemote/IsRemote` (+ Launch gated on everyone ready),
Core `GuestLobby`, `LobbyState` (+`Same`) in LobbyCodec (`WriteLobby/ReadLobby`), NetSession
`IsFull/SetFull`, `PublishLobby`, guest `HostLobby/LobbyPick/LobbyReady`, refusal grace
(`NetProtocol.RefusalGraceSeconds=2`, `_refusedAt`), RestoreCouch restores chapter/stage/tier/resume;
FrontendFlow `SyncRemote` + `UpdateAsGuest` + `RepaintLobby` + public `Lobby`; HeadlessGuest
`HostLobby`, `Refusal`; new fixture OnlineJoinSmokeTests; four host setups wait `IsReady(1)`.
After 102: EditMode 834, PlayMode 96.
part7 (103) WRITTEN: `LoadStageMessage.Placed` (5-arg ctor; 4-arg = !isLaunch), `LaunchMessage.DropIn`
(optional ctor arg), `StageRunner.TryDescribeForDropIn(slot, out current, out next?, out respawn)` +
stale-load fix (`stage != _current && stage != _next`) + ReplicaLoad launch honours Placed;
`SessionBinder.BindLate(net, respawn)` + open-host branch (NetHost.Begin(..., remote null, binder));
NetHost `_bound/_launchSent/_binder`, `Admit()` (Stepped + MenuStepped), `Bind()` baseline,
`SendLaunch(chapterId, stage, tier, resume, dropIn)`, RemoteStageReady = `!_bound || ready`,
HoldForPeer = `_bound`; NetSession `JoinedMidRun`; ReplicaWorld `HideUntilSeen`/`IsHidingLocal`;
NetGuest `WaitingToAppear`; new `UI/Combat/NetBanner.cs` (IMGUI, self-installing, `Line`).
After 103: EditMode 836, PlayMode 100. NEXT: part8 (104).

### Plan 2 design (settled 2026-09-25 from the code at f393d98 — re-derive nothing below)
- **Protocol v3.** New NetMessageKind: Request=14, RequestResult=15, Participant=16, LobbyPick=17,
  LobbyState=18, AutosaveNow=19, SessionMoment=20. New ReplicatedEventKind: ScreenOpened=4,
  ScreenClosed=5, RackChanged=6. Snapshot `PlayerSnapshot.OpenScreen` stays on the wire but the
  guest stops reading it (screens follow the reliable events; minimal churn).
- **97 requests seam.** Core: `Sack.Revision` (+ internal `Touch()` at every Inventory mutation +
  RestoreSack), `PlayerRequestKind` (Equip, Unequip, Sell, SellJunk, Lock, LockWorn, Upgrade,
  UpgradeWorn, Combine, CombineAll, QuickConsumable, Allocate, Buy, CloseScreen, SetAutoEquip,
  SetAutoSell, DebugGrant), `PlayerRequest` {Kind, PlayerId, Sequence, Revision, A,B,C,D} with named
  factories, `RequestOutcome` {Ok, Refusal, A,B,C}, `RequestCodec`. Revision checked only for kinds
  naming a sack index (Equip, Sell, Lock, Upgrade, Combine, CombineAll). Gameplay: `IPlayerRequests`
  {Pending; Send(request, Action<RequestOutcome>)}, `LocalPlayerRequests` (runs now → couch
  unchanged), static `PlayerRequestRunner.Run` (screen preconditions; Buy needs shop). Driver:
  `RequestsFor(id)`, `RequestClose(id)`, `QueueRemoteRequest`, RunStep phase
  `ApplyRemoteRequests` after SampleCommands, `RemoteRequestAnswered` event. ChestScreen: every
  `_bag.RequestX` → Send + continuation (same code as today inside the callback). Keep
  PlayerInventory.Request* public (LootLoopSmokeTests calls them). NetHost overwrites PlayerId with
  guest id; NetGuest `RemotePlayerRequests` (sequence, pending, callbacks).
- **98 rack.** Driver rolls `_racks[player]` in OpenScreen(Shopkeeper) via RollShopStock (becomes
  private); `RackFor(id)`; `RackChanged` event; Buy = rack slot, price from buyer's PriceBook;
  ChestScreenHost.RollStock/Buy removed; RequestBuy(item, price) stays but only the runner calls it.
- **99 guest's screens.** Host sends ScreenOpened/Closed + RackChanged events; guest driver
  `ApplyReplicaScreen` (sets _openScreens, _openSources = nearest interactable of that kind, raises
  ScreenChanged). `Participant` message = {playerId, revision, full, deflated SaveCodec JSON of a
  one-character SaveGame}; host→guest for the guest (full) and host's P1 (loadout+ledger only);
  guest applies via `PlayerInventory.ApplyMirror` (one Changed). Host flushes participant on:
  request answered (before RequestResult), guest screen opened, before AutosaveNow, bind; else rate
  limit ParticipantMinSteps=15. `PlayerRegistry.IsLocal/LocalCount` via marker `IRemotePlayerSource`;
  ChestScreenHost opens only local players; split = LocalCount>1. MayOpenScreen retired. ChestScreen
  cancels a pending combine only if Sack.Revision moved (XP no longer cancels).
- **100 screen rules.** `SimulationDriver.IsOnline` (NetHost sets while a guest is BOUND);
  `PausedForScreen = (!IsOnline && menuHolders>0) || (Characters<=1 && screens>0)`; online a held
  menu idles all LOCAL players' bodies (StepPlayers); MenuPauseHeld unchanged (MenuGate +
  SettingsMenu:99 keep working). Catch-up loop re-checks pause (G8 item) and drops the banked
  remainder. CameraRig frames the sole local player. SettingsMenu: local bags only; guest's toggles
  via requests; guest's "Return" row = "Leave the game" (SaveMirror + Leave). Results: SessionMoment
  ResultsOpened → guest's ResultsScreen opens, can't leave; host counts only local presses (fixes
  remote-held-buttons item).
- **101 saves.** Online each player object gets its own stash: host P2 → new "Guest Stash"; guest P1
  → "Replica Stash"; local players keep the scene stash (`PlayerInventory.UseStash`, rebuilds if
  already built). Host restores guest participant into P2. SaveService captures LOCAL players only;
  guest saves only via AutosaveNow {moment, chapterId, tier} (+ own clean exits), and only once a
  full participant has arrived (`MirrorValid`); ChapterCompleted on guest records credit + unlock
  lines; resume point host-only. Same-hero couch save question does NOT block Plan 2 (online = one
  local player per machine) — say so in DONE, no QUESTION.
- **102 lobby.** Welcome → guest's FrontendFlow in guest mode (Core `GuestLobby`: pick, ready, back
  → leave). Guest → `LobbyPick` {roster pick, ready, participant blob from its own LoadedSave for
  that hero}. Host FrontendFlow polls NetSession each Update → `FrontendState.SetRemote(1, joined,
  pick, ready)`; local slot-1 input ignored while remote; `FrontendState.Launch` requires everyone
  ready. Host → `LobbyState` {screen, host pick, host ready, inMatch}. Host keeps guest pick on
  NetSession (never in session.Characters). Refuse when couch P2 joined ("The game is full"). Keep
  refusal reason (host lets guest close on Refuse; Lost() keeps Status when never welcomed).
  HeadlessGuest auto-sends LobbyPick(0, ready, empty) on Welcome; host test setups add
  `UntilFrames(() => flow.State.IsReady(1))` before Confirm(0)×2 (OnlineHostSmokeTests,
  OnlineLaunchHoldSmokeTests, ReplicaReplaySmokeTests.Record).
- **103 drop-in.** NetHost present whenever hosting (listening), bound later. Guest ready mid-match →
  host queues; at AtCheckpoint sends Launch(drop-in) + LoadStage current (launch, resume = checkpoint
  arena) + next if loaded; guest StageReady → `SessionBinder.BindLate` (activate P2, remote source,
  guest stash + participant, place at room respawn + PartnerOffset, SetSpawnPoint) only while still
  AtCheckpoint, else next room; baseline: DropSpawned for every drop on the ground, ScreenOpened for
  open screens, participants — all before the first snapshot that includes P2. `RemoteStageReady`
  only gates on a BOUND guest (fixes rejoin deadlock). StageRunner load completion ignores a stage
  that is neither _current nor _next (stale-load fix). Guest's P2 stays inactive until its first
  snapshot (drop-in only). TryBindLate from Stepped AND MenuStepped. Bind/unbind never inside RunStep.
- **104 leaving.** Guest leaves/drops → host deactivates P2 (solo rules return; gates free),
  IsOnline=false, keeps listening. Host leaves/drops → guest to TITLE with "The host left" banner.
  `NetBanner` (UI) "Connection problem…" after 1 s; 10 s drop exists. `PlayerRegistry.Unregister(source)`
  removes only if it is the registered one (both sources); `UseSeat` rebuilds if already enabled.
  Open-by-default: setting row "Open to friends" (save field `_closedToFriends`, default open);
  a solo launch hosts through `NetSession.FriendsTransport` factory (Plan 3 = Steam; dev panel sets
  local socket). Couch never opens.
- **105** Michael's pass → `docs/team/m8-plan2-pass.md` (one sitting; QUIET).
- **F4 → Plan 3** (first task, before 106; input feel, not netcode; jump the queue if Michael's pass
  reports lost presses). **G8 catch-up-loop → Plan 2 Task 100.** TargetRegistry.Ordered / throwing
  EnemyDied → Plan 2 doesn't touch (no new mid-walk despawns or EnemyDied handlers; bind/unbind
  outside RunStep).

## Current job — DONE: pre-M8 fixes plan written (2026-09-24)

Written: F1 `Despawn.Now` (deactivate then destroy) in ResolveDeaths (collect-then-despawn),
ResetBrood, LoadedStage.Unload; PlayMode tripwire forces five steps a frame with
`Time.captureDeltaTime` and checks from a Stepped handler. F2 `SettingsRows` table (release: 3 rows;
dev: + grant, overlay, last), menu routed through it, source-scan acceptance test for the release
flavour. F3 `RunSeeds.From(entropy)` in Core/Loot; `GameSession.Seeds`/`DrawRunSeeds` (Guid entropy)
called in `FrontendFlow.LaunchNow`; `SessionBinder.Awake` → `SimulationDriver.UseSeeds`; no session →
constants 1/2/3. Next lane job: Plan 2 once the Builder is into Plan 1.

### The brief as received

Write `docs/superpowers/plans/2026-09-24-pre-m8-fixes.md`, Groundwork style, tasks **F1–F3**, one
DONE per task (i.e. the plan's tasks each commit separately when executed; the plan itself is one
DONE from me):
- **F1 deferred Destroy** — enemies removed by ResetBrood/despawn keep stepping for the rest of a
  multi-step frame; corpses feed SeparateBodies; LoadedStage props linger. Unregister/deactivate
  immediately. Failing test must fail on today's code with >1 step in a frame.
- **F2 DEBUG grant row** behind the same `#if` as the tier-overlay row; check Toggle's row indices
  and cursor wrap in both build flavours.
- **F3 seeds** — with a GameSession, each launch draws fresh loot/combat/spawn seeds held on the
  session (one set per run); with no session keep today's constants (tests deterministic); shape it
  so M8's host passes its own (D58).
Against post-Groundwork shapes; re-read every file. Then Plan 2 once the Builder is into Plan 1.

## Current state

Steps 1–2 done (readiness committed `bdc1f58`; options DONE sent). Step 3: the design session with
Michael, in this session, one decision at a time, recommendation first. Agenda order: topology →
netcode layer + Steam wrapper (Claude recommends, Michael agrees) → how couch and online mix →
join point → screens online → saves online (who holds guest gear / what guest keeps / leaving) →
feel → test tooling + Steamworks account timing. Record each answer below as it lands.
**All four steps of the lane's brief are done: audit, options, design session (D58–D62),
spec (HANDOFF-M8, approved) and Plan 1.** Remaining lane work waits on execution: Plan 2 once
Plan 1 is underway; Plan 3 after Michael's Stage B lag table. Until then: answer the Builder's
questions about Plan 1, and review its commits against the plan if the orchestrator asks.

(History: design presentation §1 (player experience) approved; §2 (build order) approved; §3
(testing) approved.) `docs/HANDOFF-M8.md` written and
self-reviewed (20 planning decisions; tasks 86–114 across stages A–F + close-out — renumbered from 84–112 because 84/85 were taken by M7-era commits). Also corrected
`options.md` (bandwidth ~2.5 KB/snapshot ~75 KB/s; local transport is TCP). **Michael approved the
written spec (2026-09-24); DONE sent** (HANDOFF-M8 + options.md + this file — do not touch
HANDOFF-M8/options.md until COMMITTED — HANDOFF-M8, options, lane file COMMITTED; renumbering DONE sent separately). Now: `superpowers:writing-plans` for Plan 1 (tasks 86–96; Plan 2 = 97–105, Plan 3 = 106–114)
→ `docs/superpowers/plans/2026-09-24-m8-plan1-wire-and-mirror.md`
(needs deep reads: SimulationDriver/CharacterActor/EnemyActor/StageRunner/LoadedStage/SessionBinder,
the Groundwork plan's final shapes, Tests/PlayMode smoke suites + ScriptedCommandSource). Then send DONE for readiness.md and go
straight to the options memo (orchestrator: no need to wait for COMMITTED).

Research already done (Context7 + release pages, 2026-09-24): Steamworks.NET 2025.164.1 (Aug 2025,
SDK 1.64, UPM git URL `https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.164.1`,
no MonoBehaviour wrapper included); Facepunch.Steamworks 2.5.2 (Apr 2024 — two years stale, zip
install, nicer async API: lobbies, InviteFriend, OnGameLobbyJoinRequested, relay SocketManager);
NGO 2.11 has no full prediction/reconciliation ("client anticipation" only), own tick (default 30),
CustomMessagingManager for raw messages, Steam only via community transports; Multiplayer Play Mode
2.0.x on Unity 6000.4+, up to 4 editor instances (needs a non-Steam transport); ISteamNetworkingMessages
is connectionless P2P to a SteamID over Valve's relay. `steam_appid.txt` = 480 (Spacewar).

### Plan 1 — WRITTEN (2026-09-24)

`docs/superpowers/plans/2026-09-24-m8-plan1-wire-and-mirror.md` — Tasks 86–96, ~8,000 lines, full
code, self-reviewed (placeholder scan clean; type-consistency list at its end). DONE sent to the
orchestrator. Executed by the sim holder after Groundwork + the Builder's pre-M8 batch. Plan 2
(97–105) is written when Plan 1 is underway; Plan 3 (106–114) after Michael's Stage B lag table.

### Plan 1 working notes (kept for Plan 2/3 context)

Progress: part0 (header, file map), part1 (Tasks 86–88), part2 (Tasks 89–90), part3 (91–92)
written in `<scratchpad>/plan1/`; part4 (93–94: SnapshotBuffer/RenderClock/ReplicaWorld, drops
removed only if born before the snapshot, NetSession holds messages until NetGuest.Start; stage
hooks StageLoadRequested/StageHandedOver/RemoteStageReady + HoldForPeer), part5 (95–96 +
self-review) to go. If the
scratchpad is lost after a reset, re-derive from these notes. Being written in parts under the
scratchpad, then concatenated to
`docs/superpowers/plans/2026-09-24-m8-plan1-wire-and-mirror.md`. Settled shapes:
- Core/Net: `NetWriter`/`NetReader` (LE, `NetFormatException`), `NetQuantize` (16-bit axis),
  `NetProtocol` consts, `NetMessageKind` byte enum (Hello, Welcome, Refuse, Commands, Launch,
  Snapshot, Events, LoadStage, StageReady, HandOver, KeepAlive, SessionEnd, Bye), `WireCommand`,
  `CommandCodec`, `InputBuffer` + `RemoteCommandStream` (edges re-derived; starved = repeat held,
  never press; catch-up merges two), `HandshakeCodec`, snapshot structs + `StateCodec` +
  `SnapshotCodec`, `ItemWire` (ItemSave JSON via SaveMapper — drops are rare), `SnapshotBuffer`,
  `RenderClock`. Core additions: `Health.FromValues`, `ManaPool.FromValues`,
  `AttackTuning.IsRadialAuthored`, `StatusTrack.Restore`.
- Platform/Net: `INetTransport` (Listen, Connect(address), Send(peer, channel, bytes, len),
  Update(now), TryReceive, Disconnect), `LoopbackTransport.CreatePair`, `LagSimulator` (send-side
  delay, seeded, loss on unreliable only), `LagProfile` None/Normal/Bad, `LocalSocketTransport`
  (TCP loopback, length-prefixed frames). No PlatformRegistry until Plan 3 (YAGNI).
- Gameplay/Net: `NetSession` on the GameSession object (role, handshake, keepalive, timeouts,
  `MessageReceived`); `NetHost`/`NetGuest` added at runtime by `SessionBinder` (no scene edits);
  `RemoteCommandSource`; `CharacterActor.BindSource`; Groundwork's `InputSystemCommandSource` gains
  `UseSeat(seat, speakAs)` so the guest's slot 1 owns all devices but speaks as P2.
- Dev join: `UI/Debug/NetDevOverlay` (IMGUI, dev builds only) — Host local / Join local / lag
  profile / status; no FrontendFlow logic change except one line `_session.Roster = _roster`.
  Plan 1 dev simplification: the guest plays the host's hero if slot 1 is empty (Plan 2's lobby
  replaces it); a guest connected at launch takes slot 1 over any local P2 join.
- Guest guards in Plan 1: driver replica (sample + send + apply, no RunStep), StageRunner (no own
  launch/waves; follows LoadStage/HandOver), SaveService (no writes), SessionBinder.Start (no
  restore), `SimulationDriver.MayOpenScreen` (remote players cannot open screens until Plan 2).
- Tests can use only public API (no InternalsVisibleTo in the repo). PlayMode tests use a
  `HeadlessGuest` test helper over `LoopbackTransport`; replay proof records host→guest messages.

## Next steps

1. Finish reading the sim code; write `docs/team/netcode/readiness.md`.
2. Options memo (Context7 for Steamworks.NET / Facepunch / Netcode for GameObjects / NGO transports).
3. Design session with Michael.

## Answers and decisions

- 2026-09-24 (orchestrator, COMMITTED 8959474 + 5758b03): drafts recorded verbatim as **D58 (topology
  + own layer + transport seam), D59 (two players, join at character select + checkpoint rooms, open
  by default), D60 (nothing pauses online), D61 (online saves), D62 (guest prediction).** D57 =
  Groundwork's menu layer. ROADMAP §4 M8 / §5.3 / §10 and GAME_DESIGN §7 updated. **Use D58–D62 in
  HANDOFF-M8 and the plan.**
- 2026-09-24 (Michael, design presentation §1 "What online co-op will be like"): **approved**,
  including my three calls — a late joiner appears only once their PC has loaded; the stage exit
  waits until both PCs have loaded the next stage; ~10 s of silence is a drop, shorter hiccups
  freeze-and-catch-up.
- 2026-09-24 (Michael, design §2 "build order"): **approved** — A remote controller → B mirror →
  C menus and saves (shop rack into the sim) → D joining and leaving → E feel (prediction) → F Steam
  (app 480, collaborator's first internet game) → close-out (Steamworks app, full chapter from both
  homes). **Three plans, written one at a time:** Plan 1 = A+B (now, in full), Plan 2 = C+D,
  Plan 3 = E+F+close-out, each written when the previous is underway.
- 2026-09-24 (Michael, design §3 "testing"): **approved** — EditMode per piece (two players); the
  PlayMode tripwire gains a hosted run with a scripted remote guest over loopback, and a
  record-and-replay test into the replica; both gates green per stage; Multiplayer Play Mode daily
  with fake lag (normal ≈100 ms RTT; bad ≈200 ms + loss) — ask the orchestrator for QUIET when
  running two editors; Michael's checklist after B, D, E; collaborator at F and close-out with a
  zipped build + a one-page "how to run it" sheet (Steam running, `steam_appid.txt` = 480 beside
  the exe).
- 2026-09-24 (Michael): **approved the written spec `docs/HANDOFF-M8.md`** — "Approved, write Plan 1".
- 2026-09-24 (Michael, M8 design session, decision 1): **Topology — host-authoritative.** The host's
  PC runs the one real simulation; the guest sends commands and draws what the host sends; the
  guest's own character is predicted locally. Chosen over lockstep and rollback (options.md §1).
- 2026-09-24 (Michael, decision 2): *"Whatever is the best free option for mobile."* Recorded as:
  **our own thin netcode layer (free), with the transport behind a seam.** Steam (Steamworks.NET)
  is the PC Early Access transport; a cross-platform transport — Epic Online Services is free,
  cross-platform, with authenticated P2P (checked in Epic's docs 2026-09-24) — slots in when mobile
  arrives, and could enable PC↔mobile crossplay later (not an M8 question). **Design constraint from
  this answer: nothing above the transport seam may assume Steam** (ids, lobbies, invites all behind
  Platform interfaces). Told Michael the interpretation; he can object.
- 2026-09-24 (Michael, decision 3): **Two in total** — solo, a couch pair, or one player per PC.
  A couch pair cannot also take an online guest. D11 unchanged.
- 2026-09-24 (Michael, decision 4): **Join at character select AND at checkpoint rooms** (chosen over
  my recommendation of character select only; mid-fight drop-in rejected). M8 scope therefore
  includes: a full world snapshot sent to a newcomer, the guest's save loaded into a live run
  (activating the second player object mid-run — today `SessionBinder.Awake` deactivates an empty
  slot for good), the stage scene(s) streamed to the guest. Rule I told Michael: a join request that
  arrives mid-fight waits until the host's run is at a checkpoint room (`StagePhase.AtCheckpoint`);
  the guest appears at that room's respawn point.
- 2026-09-24 (Michael, decision 4b): **Solo games are open to friends by default** — friends see
  "Join game", the host can invite from the overlay any time, a setting turns it off. Couch games are
  full and never open. (Write it platform-neutral: "platform friends", Steam's today.) Consequence:
  a solo game becomes a two-player online game mid-run, so the solo rules (chest pause, camera,
  layout) must switch live when a guest arrives or leaves.
- 2026-09-24 (Michael, decision 5): **Nothing pauses online** — chest, shop, hero panel, settings
  all full-screen on the player's own display with the world live; the player stands idle. Solo and
  couch keep today's rules; a solo game switches to the online rule the moment a friend drops in.
  (Implied, from options §7: each display lays out for its own local players; the host drives the
  whole-session moments — results, return to chapters, launch.)
- 2026-09-24 (Michael, decision 6a): **The host's PC holds the guest's sack and gear during the
  match.** The guest's character + stash travel to the host at join; two stashes in the simulation
  online (one per save); guest menu actions are requests with a round trip (~0.1 s) before redraw.
- 2026-09-24 (Michael, decision 6b): **The guest keeps loot, gold, XP, levels, gear AND chapter/tier
  credit** for chapters finished with the host, written on the guest's own PC at D52's autosave
  moments. The resume point stays the host's. The host's save gates what can be launched; a guest
  may help above their own unlocks. Leaving: host quits/drops → guest keeps everything up to the
  last autosave (same as a crash); guest leaves → host carries on solo (D25's solo rules from then).
- 2026-09-24 (Michael, decision 7): **Predict the guest's movement and swings** — run, jump, facing,
  and the start of every attack/cast react instantly on the guest's PC; hit confirmation, damage
  numbers, enemy reactions arrive a round trip later. Built in two steps: interpolation-only first
  (measure real connections), then prediction on top.
- 2026-09-24 (Michael, decision 8a): **Steamworks account and app ID at the M8 close-out** (chosen
  over my recommendation of "when the Steam task starts"). All M8 development on app 480
  (Spacewar); Steam Playtest is not available until the close-out. Risk to surface in the spec:
  Valve's onboarding (tax/bank/identity) may take days, so the close-out should start that paperwork
  early enough not to block the two-PC pass. ROADMAP §5.3 says "set up during M8" — orchestrator to
  note the timing.
- 2026-09-24 (Michael, decision 8b): **The collaborator is the remote tester** (from their own home,
  over the real internet). No second PC at home. On app 480 the collaborator needs a build zip with
  `steam_appid.txt`, and Steam running — the plan must include a way to hand builds over.
- 2026-09-24 (orchestrator, COMMITTED bdc1f58): audit findings scheduled — deferred-Destroy bug,
  DEBUG grant, constant seeds → Builder batch after Groundwork, before M8. **Shop rack → MINE: the
  M8 spec and plan must say "the rack lives in the simulation; buy = buy rack slot N".** Seeds: M8
  only needs to say the host owns the seed. Same-hero couch save → Michael's queue, not M8.
- 2026-09-24 (Michael): online co-op ships in Early Access (D54), chosen over Remote Play Together
  and couch-only. D11 still caps the game at two players; how couch and online mix is M8's call.

## Log

- 2026-09-25 — M8 Plan 2 written (97–105) against f393d98 and DONE sent with the pass sheet. Every
  board "Plan 2" item placed; F4 → Plan 3 first task; G8 catch-up → Task 100; TargetRegistry watch
  untouched; same-hero couch save does not block Plan 2 (no QUESTION). Only Task 105 needs QUIET.
- 2026-09-24 — Plan 1 re-read against Groundwork's final shapes; two corrections sent as DONE:
  (1) D57 makes every button carry a menu meaning, so the host now masks remote commands to the five
  verbs (`NetProtocol.RemoteVerbs`) — else the guest's Start opens/pauses the host's settings and
  the guest's A leaves the host's results; EditMode + PlayMode tests added. (2) `SeatAssignment`
  can leave seat 0 held to one device (character select) or a couch P2 holding a pad, so
  `SessionBinder` resets seats with `Follow(Title, …)` on both machines at match start. Also noted:
  the guest's own settings "Return" row only leaves the picture until Plan 2.
- 2026-09-24 — Pre-M8 fixes plan F1–F3 COMMITTED; standing by as the Builder's reference.
- 2026-09-24 — Plan 1 written (Tasks 86–96) and DONE sent. Design choices worth remembering:
  dev host/join is a self-installing IMGUI panel (no scene edits, UI stays off Platform); NetHost/
  NetGuest are added at runtime by SessionBinder.Awake (order -100 is load-bearing); the guest
  plays the host's hero until Plan 2's lobby; `MayOpenScreen` keeps remote players out of screens
  until Plan 2; the replay proof records host→guest messages + truth and compares the replica.
- 2026-09-24 — HANDOFF-M8 approved by Michael; tasks renumbered 86–114 (COMMITTED 524042f).
- 2026-09-24 — Design session done; D58–D62 recorded by the orchestrator (8959474, 5758b03).
- 2026-09-24 — readiness.md complete; DONE sent. Flagged out-of-lane: deferred-Destroy multi-step
  bug (enemies act after a wipe below 60 fps), DEBUG grant in release, shop rack rolled in the UI,
  constant RNG seeds (+ same-hero couch save collision).
- 2026-09-24 — Delivery notice: my resent ONLINE was *held for the orchestrator user's approval and
  expired* (the orchestrator runs in a different permission mode). The orchestrator nonetheless
  answered "ONLINE received". **Messages from this lane may be held; mirror every DONE/QUESTION at
  the top of this file under Waiting on** so it is seen either way.
- 2026-09-24 — ONLINE resent to the orchestrator's from= address. **Session names keep changing:
  always reply to the `from=` address on the orchestrator's latest message** (PROTOCOL §6 updated).
- 2026-09-24 — Lane seeded by the orchestrator.
