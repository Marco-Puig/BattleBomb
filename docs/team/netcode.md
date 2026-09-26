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

Nothing. **Plan 2 COMMITTED 5bba6ed** (plan, pass sheet, this file; pushed). The orchestrator has
woken the Builder on Plan 2 (starting at 97, without 96) and given it this lane's address.
PREPARE TO COMPACT received 2026-09-25; READY TO COMPACT sent after this update.

**Orchestrator's reply address:** `uds:\\.\pipe\LOCAL\cc-msg-e4fdbd589a82ce7d4a7fdf41546e0095`
(name "Battlebomb"). Always reply to the `from=` on its latest message.

## Current state (2026-09-25)

All written lane work is done and committed: readiness (bdc1f58), options, D58–D62, HANDOFF-M8
(524042f), Plan 1 (1a7fdac + D57 alignment), pre-M8 fixes plan, **Plan 2 (5bba6ed)**, and
`docs/team/m8-plan2-pass.md` (the orchestrator changed D1 to "pick a hero with the **A** and **D**
keys"). Plan 1 is built (86–95, f393d98); Task 96 = Michael's Plan 1 pass + lag table, **not done
yet**.

**On call now:** answer the Builder's questions about Plan 2 as they come (it messages this lane
directly). Nothing else runs until the orchestrator wakes this lane for Plan 3.

## Next steps

1. **Builder questions on Plan 2** — answer from the plan file
   (`docs/superpowers/plans/2026-09-25-m8-plan2-menus-saves-joining.md`) and the quick reference
   below. Re-read the relevant task and the real file before answering; the Builder premise-checks
   every task, so expect "the plan says X, the code says Y" — keep their change, apply the plan's
   intent. Anything touching another lane, scope or a locked decision goes to the orchestrator.
2. **Plan 3 (Tasks 106–114)** when the orchestrator says Michael's Plan 1 lag table (Task 96) is in.
   Write it against the code **as built after Plan 2** (read builder.md's Plan 2 build log first),
   anchored edits only, in parts under the scratchpad, like Plan 2. It opens with:
   - **F4 first** (before 106): a tap shorter than one sample is lost — `SeatInput.Sample` reads
     `IsPressed()` at the step. Fix: latch press edges from the action callbacks between samples.
     (It jumps the queue as a `96x` fix if Michael's pass reports lost presses.)
   - **The board's Plan 3 list:** `catch (SocketException)` sits in Gameplay (`NetSession.Host`) and
     always names port 7777 — `Listen` should report failure through `INetTransport` when the Steam
     transport arrives; **decide whether delta snapshots are needed** from 96's bandwidth re-measure
     (~3.5 KB/snapshot, about 105 KB/s, measured at 91 before DropIds left the snapshot); **Task
     107:** a Jump landing in a skipped frame is registered by the host at N+1 while the guest's
     prediction log has it at the skipped frame — if the feel pass shows it, snapshots carry the
     host's held baseline.
   - **Task 96's feel items (builder.md close-out, "Deferred"):** scale the teleport threshold by
     the gap between the snapshots around the render frame; hold the render clock at newest − delay
     while the newest isn't advancing (a host pause); tune let-go and drain (`StarvedRepeatSteps`,
     `InputBufferDrainSteps`; a Heavy charged through a stall fires on let-go, by design).
   - **Then HANDOFF-M8 Stages E+F and the close-out:** 106 PredictionLog +
     `CharacterActor.PredictStep`; 107 reconcile/replay/smoothing; 108 Michael's feel pass; 109
     Steamworks.NET + `Platform.Steam` (app 480; research notes further down); 110 Steam P2P
     transport; 111 lobbies/invites/rich presence — **fills `NetSession.FriendsTransport`**, Plan 2's
     open-by-default slot; 112 Steam Cloud save store; 113 the collaborator's build + how-to-run
     sheet; 114 close-out (Michael's app ID, the two-home pass).
   - **Left by Plan 2 for Plan 3 to know:** a guest waiting to appear can't open its settings;
     `PlayerSnapshot.OpenScreen` is still on the wire, unread by the guest (prediction may want it);
     the banner and lobby text are placeholder (M9 restyles).

## Plan 2 quick reference (final names — the plan file is the authority)

- **Wire:** `NetMessageKind` Request 14, RequestResult 15, Participant 16, Moment 17, LobbyPick 18,
  LobbyState 19. `ReplicatedEventKind` ScreenOpened 4, ScreenClosed 5, RackChanged 6 (menu state:
  the guest applies them on arrival, `IsMenuState`). `MomentKind` ChapterCompleted 1,
  CheckpointReached 2, StageCompleted 3. Protocol 97→3, 99→4, 100→5, 101→6, 102→7, 103→8.
- **97 requests:** Core `PlayerRequest` / `PlayerRequestKind` (1–17) / `RequestOutcome` /
  `RequestRefusal` (Refused, StaleSack, NoScreen, Busy); `Sack.Revision` (Touch at every Inventory
  mutation; checked only for the index verbs Equip, Sell, Lock, Upgrade, Combine, CombineAll).
  Gameplay `IPlayerRequests`, `LocalPlayerRequests` (runs inside Send, so the couch is unchanged),
  `PlayerRequestRunner.Run` (public; no screen needed: CloseScreen, SetAuto*, DebugGrant; shop only:
  Buy, SellJunk; Allocate at a chest only). Driver `RequestsFor`, `RequestClose`, `InventoryOf`,
  `QueueRemoteRequest`, phase `ApplyRemoteRequests` after SampleCommands, `RemoteRequestAnswered`.
  NetHost stamps the guest's own id; NetGuest `RemotePlayerRequests` (one pending; close bypasses).
- **98 rack:** driver `RackSize` 4, `RackFor`, `RackChanged`, `BuyFromRack`, `RollRack` in
  OpenScreen(Shopkeeper) (never on a replica), `ClearRack` in CloseScreen; `RequestBuy` internal.
- **99:** `IRemotePlayerSource` marker; `PlayerRegistry.IsLocal` / `LocalCount`; ChestScreenHost opens
  local players only, split = LocalCount > 1; `Participant` = deflated SaveCodec JSON of a
  one-character SaveGame (`SaveMapper.Participant`, `NetDeflate`, `ParticipantCodec`);
  `PlayerInventory.ApplyMirror`; NetHost `_watched` participants (flushed when forced — an answer, the
  guest's screen opening, a moment, a bind — else after `ParticipantMinSteps` 15; per step:
  participants → events → answers → snapshot); `MayOpenScreen` retired; a combine pick is cancelled
  only when `Sack.Revision` moved (couch change, accepted by the orchestrator, listed in 105's
  close-out). Risk: `System.IO.Compression` in Core — if it won't compile the Builder asks before
  sending the JSON uncompressed.
- **100:** driver `IsReplica` public, `IsOnline {get; internal set}`; `PausedForScreen` ignores the
  menu count online; a held menu idles this display's players in StepPlayers; the catch-up loop
  breaks and `_clock.Reset()`s on a pause (G8); `CameraRig.FramingOneScreen` / `SoleLocalPlayer`;
  SettingsMenu local bags via requests, the guest's row = `LeaveTheGame`; the debug grant moved to
  the runner (the release acceptance test scans the runner too); `StageRunner.ReplicaChapterCompleted`;
  ResultsScreen: the guest can't leave, the host hears only local hands; the PlayMode asmdef
  references Presentation.
- **101:** `LobbyPick` {roster, ready, Brought}; `SaveMapper.ParticipantFrom`; NetSession
  `GuestPick` / `GuestReady` / `GuestBrought`, `SendLobbyPick`; the host's "Guest Stash"
  (`UseStash`); binder `GuestDefinition`, `RestoreGuest`; SaveService saves local players only, with
  the replica gate `GuestCopyArrived` (`MirroredFromHost`); host `SendMoment` flushes the guest's copy
  first.
- **102:** `FrontendState.SetRemote` / `IsRemote`, Launch needs everyone ready; Core `GuestLobby`;
  `LobbyState` (+`Same`); NetSession `IsFull` / `SetFull`, `PublishLobby`, guest `HostLobby`,
  `LobbyPick` / `LobbyReady`, a 2 s refusal grace; RestoreCouch restores chapter/stage/tier/resume;
  FrontendFlow `SyncRemote`, `UpdateAsGuest`, `Lobby`; four host test set-ups wait for `IsReady(1)`.
- **103:** `LoadStageMessage.Placed`; `LaunchMessage.DropIn`; `StageRunner.TryDescribeForDropIn` +
  the stale-load fix; `SessionBinder.BindLate` + the open-host branch; NetHost `_bound` /
  `_launchSent`, `Admit` (Stepped + MenuStepped), `Bind` with the baseline; RemoteStageReady =
  `!_bound || ready`; HoldForPeer = `_bound`; the guest hides its own body until a snapshot has it;
  `UI/Combat/NetBanner`.
- **104:** `PlayerRegistry.Unregister(source)`; `UseSeat` rebuilds when awake; `SessionBinder.Unbind`
  (+`_guestStash`); NetHost.OnPeerLeft unbinds and resets; guest → title with `TakeEnding()` ("The
  host left." / "The connection to the host was lost."); silence → `Lost("went quiet")`;
  `SaveGame.ClosedToFriends`, `GameSession.ClosedToFriends`, `SettingsRow.OpenToFriends` (index 3 in
  both flavours); `NetSession.FriendsTransport`, `UseLocalFriends`, `OpenToFriends`, `LocalGuestSave`;
  dev panel "Friends: local socket" and "Go quiet 4 s / 12 s".
- **Counts:** EditMode 791 → 806, 807, 817, 820, 825, 834, 836, 839; PlayMode 65 → 69, 71, 80, 87,
  93, 96, 100, 106 (after 97 … 104). New fixtures: OnlineMenuSmokeTests, GuestMenuSmokeTests,
  OnlineJoinSmokeTests. Only Task 105 needs QUIET.

## Research notes (kept for Plan 3)

Context7 + release pages, 2026-09-24: Steamworks.NET 2025.164.1 (Aug 2025, SDK 1.64, UPM git URL
`https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.164.1`,
no MonoBehaviour wrapper included); Facepunch.Steamworks 2.5.2 (Apr 2024 — two years stale, zip
install, nicer async API: lobbies, InviteFriend, OnGameLobbyJoinRequested, relay SocketManager);
NGO 2.11 has no full prediction/reconciliation ("client anticipation" only); Multiplayer Play Mode
2.0.x, up to 4 editor instances (needs a non-Steam transport); ISteamNetworkingMessages is
connectionless P2P to a SteamID over Valve's relay. `steam_appid.txt` = 480 (Spacewar). Re-check
versions with Context7 before writing Plan 3.

## Answers and decisions

- 2026-09-25 (orchestrator, COMMITTED 5bba6ed): Plan 2, the pass sheet and this file, pushed. One edit
  to the pass sheet (D1: "the **A** and **D** keys"). Placements accepted as written: F4 = Plan 3's
  first task (or 96x), G8's pause re-check in 100, TargetRegistry untouched, same-hero save outside
  Plan 2. The couch combine-cancel change is accepted (listed in 105's close-out). The orchestrator
  folds "Where this plan departs" into HANDOFF-M8 as tasks land. Builder woken on Plan 2 from 97
  without 96, with this lane's address. PREPARE TO COMPACT; wake for Plan 3 when the lag table is in.
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

- 2026-09-25 — Plan 2 COMMITTED 5bba6ed. PREPARE TO COMPACT: lane file brought current (on call for
  the Builder's Plan 2 questions; Plan 3 opens with F4, then the board's Plan 3 list, 96's feel items
  and the bandwidth re-measure). READY TO COMPACT sent.
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
