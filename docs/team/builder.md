# Lane: Builder

**Mission:** build the engineering milestones in Unity, one plan at a time, with both test gates
green before anything is called done. You hold **the sim** by default (PROTOCOL rule 4) — you are
the only lane that writes under `Assets/` unless the orchestrator lends it out.

**Owns:** `Assets/`, `Packages/`, `ProjectSettings/` (while holding the sim) · this file.

**Read first (in order):** `CLAUDE.md` · `docs/team/PROTOCOL.md` · `docs/team/BOARD.md` · this file
· `docs/ROADMAP.md` §4 "Groundwork" · the plan below.

**The plan:** `docs/superpowers/plans/2026-09-24-groundwork-input.md` — 14 tasks, full code and
tests in each.

---

## How to run the plan

- **Use `superpowers:subagent-driven-development`** (the plan's recommended mode): a fresh
  implementer subagent per task, then a spec review and a quality review. Implementers may run on
  Sonnet to save tokens; reviewers should not.
- **Nobody commits.** Every plan step that says "Commit" becomes a `DONE` message to the
  orchestrator (PROTOCOL rule 9) — then wait for `COMMITTED` before starting the next task.
  Tell every subagent: no git writes, and never discard changes it did not make.
- **The Unity MCP bridge is the gate.** Only you call it. After any on-disk scene edit expect the
  "modified externally" modal (see memory: project status → the recovery notes).
- **Ask for `QUIET` before** any play-mode step that needs Unity in the foreground (synthetic
  keyboard or gamepad input — Task 11 Step 8) and before Michael's pass (Task 14 Step 7).
- **Michael's pass** (Task 14 Step 7) is his fast-motion checklist: send it to the orchestrator,
  who schedules it with quiet on.
- **Shared docs in Task 14** (`DECISIONS.md` D57, `GAME_DESIGN.md` §3.1, `CLAUDE.md`,
  `ARCHITECTURE.md`, `ROADMAP.md`) belong to the orchestrator: send the plan's text for those steps
  in your `DONE`, and the orchestrator applies it.
- After Groundwork: the next plan is M8, which the Netcode lane is writing. The orchestrator will
  point you at it.

---

## Waiting on

Nothing. (11:50 ANSWER from the orchestrator: **F1 = option A** — collect-then-settle, keep
`Destroy`, no Despawn helper, ResetBrood/Unload unchanged; tripwires stay as regression guards
with reworded docs; new red-first test (3 same-step deaths → one step, registry order). DONE lists
the tripwire file + one line per skipped plan step (3, 5). F2 in parallel OK, one DONE per task.
Netcode heads-up: this changes its read of candidate F4 and the catch-up-loop pause item.)

## Current state

**M8 Plan 1 — started (17:00, 2026-09-25).** F5.2 committed (`cc7a9fe`); the pre-M8 batch is done.
Plan: `docs/superpowers/plans/2026-09-24-m8-plan1-wire-and-mirror.md`, Tasks 86–96, one `DONE`
per task. Prerequisite hashes (its "Before you start"): Groundwork `56e4be5..527df2b`, G14
`5f3799a`; F1 `15778dd`, F2 `724d44e`, F3 `5abd671`, F5 `2610ef0` + `cc7a9fe`. **Stale premise
in the plan's header:** "destroyed enemies unregistered immediately" is true via Destroy's
immediate OnDisable (Unity 6.5), not via a Despawn helper — F1 never built one; apply the plan's
intent on top. **Two-editor runs (Task 90 on):** `QUIET REQUEST` first; `DONE` + a note after.
Michael's Groundwork pass may call QUIET ON any time → stop at the next clean point. Netcode is
being reopened as my reference; until then questions go to the orchestrator.
Baseline EditMode 694, PlayMode 35 (F5.2 gates; only docs since). Plan split into
`<scratchpad>/tasks/m8_preamble.md` + `task86.md`…`task96.md` (89 is 1,647 lines); briefing
`m8_context.md` (rules + "plan predates Groundwork and F1–F5: keep their changes, apply intent,
report"). **Task 86 (the wire) phase 1 dispatched (17:15)** — CommandButtons/PlayerCommand shapes
verified; red = compile errors (no BattleBomb.Core.Net).
**Task 86 code in (17:30):** tests + 8 Core/Net files byte-identical to the plan (script). New
fixtures 14/14; EditMode 708/708 (fitness green). Review running, then DONE (paths incl. the folder
metas Core/Net.meta, Tests/EditMode/Net.meta and each file's .meta).
**Task 86 DONE sent (17:45).** Review "Yes with fixes" → applied: the >4-commands test now sends five
whole commands (mutation-proved: guard removed → "Expected NetFormatException but was null");
WireCommand.Equals exact (`Move.Equals`); count-at-bound + buffer-reuse asserts. NOT applied, flagged
for **Task 92**: NetWriter has no MaxMessageBytes cap — an Events batch (≤512 events, ≤8 KB item
JSON each) could exceed 256 KB; LocalSocketTransport drops it as Lost() silently while Loopback
delivers it — split/limit batches there. EditMode 708/708 after.
**Task 86 COMMITTED `5e586a4`** (A–C accepted). Orchestrator on the oversized batch: (1) in 87, the
loopback enforces the socket's frame limit (Disconnect both sides, deliver nothing) so no test passes
on loopback and fails on the socket; (2) at 92, bound/split the Events batch + a test that sends a
batch past the limit and shows it arrives whole. **Leave NetWriter uncapped.**
**Task 87 started (18:00):** addition spec `<scratchpad>/tasks/task87_extra.md` (loopback limit +
3 tests: loopback past-limit drops / at-limit arrives / socket past-limit drops). Phase 1 dispatched.
Expect EditMode 708 + plan's 11 + 3.
**87 progress (18:25):** tests = plan + extra (script-verified); 8 Platform/Net files verbatim, compile.
With the plan's loopback: loopback past-limit red as expected ("Data 262145B"). Socket past-limit
first timed out — a **race**: the connect wait only checked the host's Connected; the guest's connect
completes a pump later, so Send(guest.Peer) sent nothing → added `&& !guest.Peer.IsNone` to that wait
AND to the plan's same wait in Two_sockets… (deviation). Socket tests 2/2 after. Phase 2b (section A)
dispatched. Trap: run_tests testName filter is a substring, not a regex ("A|B" matches nothing).
**87 code complete (18:35):** section A applied; Net fixtures 27/27; EditMode 721/721; socket tests
stable 4 runs. Review running, then DONE.
**87 review → 4 fixes applied (18:55):** (1) `SendTimeoutMs = 1000` on both sockets (a paused peer
can't freeze a blocking Write); (2) `Listen` keeps the listener only once `Start` succeeds — red-first
test `Listening_on_a_busy_port_throws_once_and_leaves_nothing_behind` (red: "Not listening…");
(3) a second peer is accepted-and-closed at once, not left in the backlog as a phantom join;
(4) loopback `Disconnect` ignores a peer it isn't talking to, as the socket does — red-first
`Disconnecting_a_peer_that_is_not_there_does_nothing_as_on_the_socket`. Net 29/29; EditMode
723/723; socket tests stable 3 runs. **For Task 89 (finding 2, second half):** `NetSession.Host`
must `Listen()` before committing `Role = Host`, or catch `SocketException` → `Close()` → "Port
7777 is busy". **Task 87 DONE sent (19:00)** → waiting for COMMITTED.
**Task 87 COMMITTED `4f42e0c`** (A–C accepted; skipping PlayMode OK). Orchestrator for 89: do
**both halves** — `NetSession.Host` calls `Listen()` before committing `Role = Host`, and a
`SocketException` closes cleanly + shows "Port 7777 is busy" on the dev panel.
**Task 88 started (19:10):** premises checked — PlayerCommand/WireCommand shapes,
`NetProtocol.InputBufferTarget/Max` (2/6), `PlayerRegistry.Register/Unregister(PlayerId)`,
`CharacterActor.OnEnable` line 950 `_source = GetComponent<…>()` all as the plan quotes.
CharacterActor.cs is CRLF. RemoteCommandSource looks up only SimulationDriver (the local source
also falls back to CommandSampler — not needed on the host; verbatim). Phase 1 (tests) dispatched.
Expect EditMode 723 + 10.
**88 code in (18:00):** 5 new files byte-identical to the plan; CharacterActor (a)–(c) only, CRLF kept.
Red = compile errors as planned. EditMode 733/733, PlayMode 35/35 (OnEnable changed → ran it).
Review "Yes with fixes" → applied: 3 tests (overlapping redundancy dedupe; held-through-starvation
not re-pressed; release re-primes + re-presses), **mutation-proved** (round 1: last-only dedupe /
starvation clears held / Release keeps primed → exactly the 3 new tests fail, all plan tests pass;
round 2: Release keeps held → the release test fails); `[System.NonSerialized]` on
`RemoteCommandSource._bound` + `CharacterActor._sourceBound` (hot reload kept the bool but reset
`_id` → remote would register as Player 1); blank lines round BindSource. EditMode 736/736.
**Flagged, not built:** (1) buffer depth never drains back to target after a stall (+~83 ms for
the session) → 96's lag pass or a decision; (2) a silently-dropped guest's body repeats its last
stick for up to 10 s (DropAfterSeconds) → cap starved steps, or NetHost releases on HasProblem
(89/Plan 2); (3) **89:** the driver's PausedForScreen branch samples every render frame → drains
the remote stream while paused; (4) Plan 2: Unregister-by-id can remove a newer source (both
source types) → unregister only if TryGet returns this; (5) **92:** flood path acks dropped frames
(LastTakenFrame moves on drop; Add returns true for the dropped one); (6) **94:** HoldForPeer
floods the buffer → Release when the hold clears; (7) merge starts at 8 buffered, not "past 6".
**89 note:** SessionBinder.Awake already finds `_driver` (F3) before the guard — the plan's (c)
repeat lookup is redundant; skip + report. Binder is DefaultExecutionOrder(-100), so UseSeat runs
before the sources' OnEnable as the plan assumes.
**Task 88 DONE sent (18:20)** → waiting for COMMITTED.
**Task 88 COMMITTED `5fd0920`.** Orchestrator's calls, all built **in 89**, red-first in Core: (1) slow
drain (merge one step after N above target; N = `NetProtocol.InputBufferDrainSteps`); (7) merge
threshold = the documented 6 (boundary test 6/7); (2) let go after `StarvedRepeatSteps` = 15;
(3) repeated hostFrame consumes nothing (+ drop the pause backlog on resume). 4 → Plan 2, 5 → 92,
6 → 94 (on the board). Skip 10(c)'s repeat lookup, report it.
**Task 89 started (18:40):** extra spec `<scratchpad>/tasks/task89_extra.md` — X1 busy port (Listen
before Role; SocketException → Dispose, Offline, "Port 7777 is busy"; test on a squatted port),
X2 skip 10(c) lookup, X3 stream (constants + 5 red-first tests + full RemoteCommandStream).
Design choices: a repeat returns held with **no edges** (same presses again = double press);
resume trims to target instead of `Release` (Release re-accepts old redundant copies + re-presses
held buttons); let-go keeps last-heard held as the edge base (no invented press on return).
Stages: S1 X3a+X3b (see 5 red) → S2 X3c (green) → S3 Step 1 + X1a (compile red) → S4 Steps
3, 5–12 with the plan's Host (NetSessionHost red at runtime) → S5 X1b → S6 Steps 14–15 → PlayMode.
Files CRLF: GameSession, SessionBinder, SaveService, SimulationDriver, StageRunner, FrontendFlow;
LF: InputSystemCommandSource, NetProtocol, stream + tests.
**89 progress (18:35):**
- **S1:** the 5 X3b tests were red as predicted.
- **S2:** the stream matches X3c (script); Net 47/47.
- **S3:** compile red, as expected.
- **S4:** every new file is byte-identical to the plan; NetSession = plan + Step 12. All snippets present, X2 applied, line endings intact. Handshake tests 4/4. NetSessionHost was red: SocketException out of Host.
- **S5:** X1b applied → green.
- **EditMode 746/746.** No new compile warnings.
- **S6** (HeadlessGuest + OnlineHostSmokeTests) dispatched; then PlayMode, filtered and full.

**89 gates (18:40):** S6 is byte-identical to the plan. OnlineHostSmokeTests 6/6; full PlayMode 41/41.

**89 review: "Yes with fixes" (19:00).**

Applied:
- **I1.** SaveService's guard is now `_driver.IsReplica`, not the live role. A guest who pressed Leave mid-match stayed in the replica with the role Offline, and the settings menu's return-to-chapters or quitting could then write the replica's empty stash over their real save.
- **I2, host side.** SessionBinder picks the stand-in locally (`seat = hosting && i == guest ? 0 : i`) instead of writing `Characters[1]`. NetHost.SendLaunch sends the host's pick for the guest's seat. Test `The_stand_in_hero_never_enters…` was red first.
- **I2, guest side.** NetSession holds `_couch` (a Clone taken in FollowLaunch). `RestoreCouch()` runs from ReturnToFrontend and NetGuest.OnDestroy. No test yet: the guest harness arrives at Task 95.
- **M3.** Start skips restoring saved gear into `_remoteSeat`; a copy of the host hero's gear could otherwise have reached the shared sack.
- **M7.** New test `A_guest_who_leaves_is_let_go_at_once…`, mutation-proved: with NetHost's Release removed, the new test fails and the plan's leave test still passes.

Flagged, not fixed:
- M1: the refusal reason gets overwritten.
- M2: the guest's own settings menu drives Player 2 on the host.
- M4: SocketException is caught in Gameplay.
- M5: UseSeat relies on Player 2 being authored active.
- M6: NetDevOverlay calls Find in OnGUI.
- M8: LocalLagNames is a mutable static array.

EditMode 746/746; OnlineHost 8/8; **full PlayMode 43/43**. **Task 89 DONE sent (19:05)** → waiting
for COMMITTED. Next: Task 90 (two editors) — QUIET REQUEST before any two-editor run.
**Task 89 COMMITTED `fd33bea`** (A–G accepted). Orchestrator's calls:
- **M2 → build in 95, red-first:** while a screen is open on the guest, NetGuest sends neutral; after it closes, a held button counts only once it has been released (D57's held-across rule), so the A that closes a menu isn't a jump.
- **M6 + M8 → the next task touching those files, 95 at the latest:** NetDevOverlay caches the session (a debug panel must never destroy one); LocalLagNames goes read-only.
- **Tests carried to 95:** a guest who leaves mid-match writes no save; the guest's couch is restored after a match.
- **Deferred:** M1 → Plan 2 lobby; M5 → Plan 2 seats; M4 → Plan 3.
**Task 90 started (19:15):** steps 1–3 (package via `Client.Add`, runInBackground, both suites) need no QUIET; QUIET REQUEST before step 4 (Art idle → granted at once).
**90 progress (19:15):**
- **Step 1:** com.unity.multiplayer.playmode 2.0.2 is in the manifest and lock. The Package Manager also re-sorted `com.unity.pipeline` (fine per orchestrator).
- **Step 2:** runInBackground was already on (1 on disk, true in the editor), so nothing was written.
- **Step 3:** EditMode 746/746; PlayMode 43/43.
- **QUIET ON granted.**
- **MPPM 2.0:** the code lives in UnityEditor.MultiplayerModule. The Window/Multiplayer/Multiplayer Play Mode menu exists. Activate from code with `MultiplayerPlaymode.PlayerTwo.Activate(out err, new List<string>())` via reflection; `PlayerState` goes NotLaunched → Launching → Launched. Deactivate with `.Deactivate(out err)`. Clone data lives in Library/VP.
- **Step 4 BLOCKED (19:15):** Join local exists only in the clone's window. The bridge reaches the main editor only, and Michael declined screen control of Unity (do not re-ask). I sent BLOCKED with (a) Michael clicks Join once, or (b) fold into his step 6 (recommended). Holding QUIET with Player 2 launched.
**CONTINUE (b) (19:15):** I deactivated Player 2 (NotLaunched); the main editor never entered play mode. QUIET OFF sent. Activation created `ProjectSettings/VirtualProjectsConfig.json` (the player-tag list); it goes in the commit. **Task 90 DONE sent (19:20)** with step 6's checklist for Michael.
**Standing rule, from the orchestrator:** Michael's "no" to screen control holds for this session; never reach the clone another way. For **93–95**, split the clone-side checks into (1) what a harness can prove (HeadlessGuest; 95's guest harness) and (2) what needs Michael's eyes or a click. Group 2 goes in that task's checklist; the orchestrator batches them (one pass at 95, the lag table at 96). If a task would block mid-build on a click, say so in its DONE rather than waiting in QUIET.

**Pre-M8 fixes — started (11:00, 2026-09-25).** G14 committed (`5f3799a`), every shared-doc change
applied; **Groundwork built**. Plan: `docs/superpowers/plans/2026-09-24-pre-m8-fixes.md`, F1–F3,
then F5 (grid scrolling; do not clamp). One `DONE` per task. **QUIET ON may come any time** for
Michael's pass → reach a clean point (compiles, out of play mode, no half-written script under
Assets/, finished work sent as `DONE`) and leave Unity alone until QUIET OFF — keep F-tasks small
enough to stop between. Checklist item 12 (partner's sale slides an item under the cursor) is
Michael's call; the orchestrator's recommendation (cursor follows its item + X ignores presses
briefly after a partner-caused change) would ride F5 — **do not build until he says so**.
Baseline: EditMode 680, PlayMode 28 (G14 gates; only docs changed since). Plan split into
`<scratchpad>/tasks/f_preamble.md`, `taskF1.md`, `taskF2.md`, `taskF3.md`; briefing
`f_context.md` (Groundwork's rules + "keep each file's line endings"). F1's quoted code verified
against the tree (ResolveDeaths, ResetBrood, Unload — `_brood`/`_props` are `List<GameObject>`).
**F1 phase 1 dispatched (11:10):** the tripwire `SameStepRemovalSmokeTests` (3 tests; expect all 3
red with a "still a target" violation — if one passes, stop and report: premise wrong).
**F1 STOPPED (11:40): premise wrong on Unity 6000.5.8f1.** Tripwire (verbatim, byte-checked) is
3/3 GREEN on today's code; harness reaches 5 steps/frame (80/80 frames). `Object.Destroy(go)` runs
OnDisable at once here: live, a destroyed dummy left TargetRegistry in the same eval
(isActiveAndEnabled false, still non-null until frame end); the reset step's own Stepped already
showed 0 of the old brood. **Real remaining bug (plan Step 4):** ResolveDeaths walks
`Targets.Ordered` (the registry's own List) and Destroy removes each corpse mid-walk → the next
enemy is skipped that step. Live: Grunt 001/002/003 killed in one step (ApplyStatusDamage via
reflection) → deaths resolved 001+003 in one step, 002 the next. QUESTION sent (A/B/C).
Tripwire file uncommitted under Assets/ (compiles, passes). Implementer agent paused at phase 1.
**Trick:** `System.AppDomain.CurrentDomain.SetData/GetData` carries handlers/lists between evals.
**F1 revised brief:** `<scratchpad>/tasks/taskF1_revised.md` (phase 1b = reworded class doc + new
test `Every_corpse_whose_beat_ends_in_a_step_is_settled_in_that_step_in_order` + `Living()`;
phase 2 = `_dying` + ResolveDeaths collect-then-settle with `Destroy` kept). F1 implementer on
phase 1b. **F2:** phase 1a done — `SettingsMenuReleaseAcceptanceTests` byte-identical to the plan;
recompile deferred until F1's phase 1b lands (never compile a half-written file). F2 notes:
Repaint's `bool autoEquip/autoSell` originals (l.367–368) must go; release proof = compile player
scripts in release (`PlayerBuildInterface.CompilePlayerScripts`, no DEVELOPMENT_BUILD) instead of a
full build.
**Reds confirmed (12:00):** F2 acceptance "SettingsMenu.cs:191 mentions GrantTestLoot( outside…";
F1 fixture 3 guards green + new test red "settled across several steps … < 75, 75, 76 >".
F1 phase 2 and F2 phase 1b+2 dispatched in parallel (disjoint files); recompile only after both
report.
**Code in (12:20):** F1 = `_dying` + collect-then-settle, `Destroy` kept (diff checked). F2 =
SettingsRows.cs + SettingsRowsTests.cs byte-identical to the plan; SettingsMenu diff checked (bools
once, Groundwork's prompt row untouched). Recompiled clean. F1 fixture 4/4; F2 tests 6/6 (the
"Settings" name filter also caught 2 unrelated tests). EditMode 686/686 (680 + 6). PlayMode full
running (expect 28 + 4 = 32). Then: F2 live check (Step 6) + release compile proof (Step 7 via
CompilePlayerScripts), reviews, two DONEs.
**F2 DONE sent (12:35):** PlayMode 32/32; live walk equip → sell → return → grant → overlay → wrap;
release compile (StandaloneWindows64, options None) — Cecil read of the release UI.dll shows no
GrantTestLoot/DebugGrantQuality/_overlay on SettingsMenu; review Yes. (Trap: `eval` has a 5 s
main-thread limit — long work still finishes; check its output afterwards. Cecil is loaded twice —
reach Mono.Cecil by reflection.) **F1:** review pending, then its DONE (with builder.md).
**F2 COMMITTED `724d44e`** (A–C accepted). **F3 waits for F1's commit** — it edits
SimulationDriver.cs too. F3 prep: quoted code verified (seeds l.40/43/47, OnEnable's three
`new DeterministicRandom((uint)_…Seed)` l.586–588 after `_clock = …`; GameSession `LoadOutcome`
l.54, no `using System;` yet; SessionBinder Awake guard l.42–45, `[DefaultExecutionOrder(-100)]`,
`_driver` serialized; FrontendFlow `LaunchNow` l.272, LoadScene l.286).
**F1 done (12:50):** review Ready: Yes; applied its low finding (the new test's corpse match skips
already-settled corpses — Destroy leaves them non-null to frame end). Fixture 4/4 after.
`DONE` sent with builder.md. **Next: F3 on `COMMITTED`** (brief: `taskF3.md`; its quotes verified).
**F1 COMMITTED `15778dd` (13:00)** — A–C accepted; review findings → board watch items.
**F3 started (13:00):** phase 1 = RunSeedsTests + RunSeedsSmokeTests (red = compile errors:
RunSeeds / SimulationDriver.Seeds / GameSession.Seeds missing). Expect EditMode 686 + 3 = 689,
PlayMode 32 + 2 = 34.
**F3 progress (13:10):** both test files byte-identical to the plan; red = compile errors
("RunSeeds does not exist") — **the editor is NOT at a clean point until phase 2 lands** (if QUIET
ON arrives now: finish phase 2 or remove nothing — say so to the orchestrator). Front-door path
checked by the implementer: solo Confirm×2 reaches chapters; LaunchNow still fires with devices
disabled (CommandSampler returns Idle); RejoinTheCouch lands on chapters. Phase 2 dispatched.
**F3 code in (13:40):** RunSeeds.cs byte-identical; diff matches Steps 4–5 (DeterministicRandom is
already in Core.Loot; SimulationDriver already had `using BattleBomb.Core.Loot;`). Recompiled
clean. RunSeedsTests 3/3, RunSeedsSmokeTests 2/2, EditMode 689/689. Full PlayMode running (expect
34); review running. Editor left on an untitled empty scene by a test run — reopen Gameplay after.
**F3 done (13:55):** PlayMode 34/34. Review Ready: Yes; applied its low finding as comments only
(Seeds/_runSeeds are lost on a mid-play domain reload → the run rolls the constants, editor only;
fixed GameSession's stale "Alone among the fields" wording on Characters). Recompiled clean.
`DONE` sent. **Next: F5** (grid scrolling for >40 stacks; do not clamp; say if bigger than one
task) — needs a plan: no plan file exists yet, ask the orchestrator who writes it.
**F3 COMMITTED `5abd671`.** **F5: I write the plan** (orchestrator, 14:00) —
`docs/superpowers/plans/2026-09-25-f5-chest-grid-scrolling.md` (my lane for this one file; send as
QUESTION with the path, not DONE; build only after approval). Written (14:30): two tasks — F5.1
model (`ChestLayout.Rows`, `ChestNavigation.TopRow/KeepCursorInView/IsCursorDrawn`, Move→Step
wrapper; 5 EditMode tests → 694), F5.2 drawing (draw from TopRow, GridAnchor on the drawn cell,
scroll bar, X/Y refuse an undrawn cursor; 1 PlayMode case → 35; live check couch/solo/shop).
Not done: wheel/touch (M13 pointer). Item 12 placed in OnBagChanged, ~1/3 of F5. After F5: M8
Plan 1 (Tasks 86–96), Netcode reopening as reference.
**F5 plan APPROVED + committed `ded6e1e` (14:40).** Scroll bar yes; no guard on A (if the live
check finds the menu opening off its item, guard A too and say so). Build F5.1 then F5.2, one
DONE each. Task files `<scratchpad>/tasks/taskF5_1.md`, `taskF5_2.md`. **F5.1 started.**
F5.1 phase 1 verbatim; red = compile errors (no `rows`, TopRow, KeepCursorInView, IsCursorDrawn) —
**editor not at a clean point until phase 2 lands**. Phase 2 dispatched (14:50).
**F5.1 code in (15:00):** diff = plan verbatim. Recompiled clean (clean point again).
ChestNavigationTests 34/34; EditMode 694/694. Review running; then DONE, then F5.2.
**F5.1 DONE sent (15:10)** — review Ready: Yes. **F5.2 phase 1 dispatched** (the 45-stack PlayMode
case + helpers in LootLoopSmokeTests; expect red "Expected < "Cell 4x0" > But was <empty>").
**F5.1 COMMITTED `2610ef0`.** Orchestrator: F5.2's live check must include one move that skips
`Move` — a combine's reroll past 40 (RecollectOnto → SelectCell) or a tab switch back to the sack —
to confirm Refresh's KeepCursorInView catches it.
**F5.2 red confirmed (15:25):** test + helpers verbatim; the new case fails "…0 elements… Missing:
< "Cell 4x0" >". Phase 2 dispatched.
**F5.2 code in + live check done (16:15):** diff = plan verbatim; LootLoopSmokeTests 15/15;
EditMode 694/694. Live (probe `<scratchpad>/f5_probe.cs`): couch 45 → TopRow 1 at row 5, ring
Cell 4x0, thumb 71/356 of 427, menu y −435 = under Cell 4x0; couch 200 → TopRow 20, ring Cell
4x7, thumb flush at the bottom, menu under Cell 4x7; **skip-Move path**: RB→Hero→RB→Sack (SwitchTab
resets cursor 0 while TopRow 20) → the repaint pulls TopRow to 0, ring Cell 0x0; solo 200 → TopRow
1 at row 5; **shop Sell mode** 200 → TopRow 1, bar clear of the junk bar; **X guard**: cursor 199
before a repaint (TopRow 1) → X sold nothing; after the repaint (TopRow 20) → X sold exactly it.
A needs no guard (Confirm repaints → scrolls to the cursor before anchoring). Captures taken.
Trap: CloseScreen + OpenScreen in one eval leaves the old screen alive to frame end — find the new
one in a later eval. Full PlayMode next (expect 35), then review, DONE.
**Gates + review (16:45):** PlayMode 35/35. Review "Yes with fixes": the menu check compared only y,
and the old bug (cell 39 = Cell 4x7) is on the same row → added an x assertion (deviation — the
plan's test was verbatim). Mutation-proved: old GridAnchor → "menu opened in another column,
expected 69.1 but was 594.2"; fix restored (production diff byte-identical to the reviewed one).
LootLoopSmokeTests 15/15 after. **F5.2 DONE sent (16:55).** Note for item 12 (from the review): "identical values stack" holds
only for potions — gear never stacks; Equals still finds the right stack via the affix array ref.
Board candidate from F1's review: `TargetRegistry.Ordered` hands out its live list — any future
mid-walk despawn (self-destructing enemy, a reaction that despawns what it kills) brings the skip
back in StepEnemies/StepStatuses.

**Groundwork Task 14 — started (10:30, 2026-09-25).** G13b committed (`527df2b`), changes 1–5
accepted; close ✕ on the couch Hero tab → M9 backlog; ultrawide → Later. Task 14 is text for the
orchestrator (D57 corrections, GAME_DESIGN §3.1, CLAUDE.md rule 3 + ARCHITECTURE, ROADMAP §4/§9),
final gates (EditMode 680, PlayMode 28), and Michael's checklist — the plan's 10 items plus: X
sells the whole stack; partner's sale can slide an item under the cursor; no menu left open after
combine-all; held stick through the opening X; results ¾ s dwell; Start only on the title; join
line says only A with P1 on the keyboard; couch Hero-tab strip shows the cursor; bottom-row item's
menu shows Sell/Lock; A on a worn piece opens its menu on the couch Hero tab; pad reconnect keeps
the seat.

**G14 `DONE` sent (10:55)** — final gates EditMode 680/680, PlayMode 28/28, no InitTestScene,
git clean but this file. Waiting for `COMMITTED`; Michael's pass is the orchestrator's to schedule
(quiet on). Next after Groundwork: pre-M8 fixes F1–F3 (`docs/superpowers/plans/2026-09-24-pre-m8-fixes.md`),
then F5 (grid scrolling — do not clamp; say if bigger than one task), then M8 Plan 1.
**G14 handover text (drafted 10:45 — sent in the DONE):**
- *D57 corrections* (checked against the code): (1) "Start still leaves any screen outright" →
  any in-game screen (chest, shop, settings); the results screen is left only with A (any player)
  or the pointer's button, once every hand is off and it has been up ¾ s; Start and Esc do nothing
  there (`ResultsScreen.Tick`). (2) "X sells instantly" → the whole stack under the cursor.
  (3) Prompts: before a player's first press they guess the most recently used device; the
  character-select join line names only the devices a newcomer can join on. "X does nothing at the
  shop rack" verified (Buy focuses `Stock`; `RunOption` acts only on Grid).
- *GAME_DESIGN §3.1*: the plan's paragraph, after l.151 ("…out by construction (D17).").
- *CLAUDE.md* l.56–57 rule 3: "only `InputSystemCommandSource`, with the `SeatInput` it owns,
  touches a device". *ARCHITECTURE*: no occurrence (grep) → no change; optional §4 bullet offered.
- *ROADMAP*: §4 heading "— built (2026-09-25, 56e4be5..<G14>); Michael's pass pending" (→
  complete with his verdict); §9 P2 row paid in G5 `3e6b3bb`, overlap row paid in G13 `7c3014f`;
  optional: §6 menu-map row → done (D57); §10 items 1–2 done; §4 table's "Buy at the rack" is
  superseded by D57.

**Groundwork G13b — started (09:15, 2026-09-25).** G13 committed (`7c3014f`), deviations A–B
accepted. Orchestrator's task (no plan text — I design it): (1) the SACK/HERO strip is built
outside the sack panel so Tabs focus shows on the Hero tab (couch); (2) the item popover clears the
compare panel — rows 2 and 3 must show Sell and Lock in solo and couch (SetAsLastSibling or flip
above the cell, whichever the code favours). Same gates; `DONE` as G13b. Finding 3 (>40 stacks) =
**F5** on the backlog after Groundwork — grid scrolling; do NOT clamp the cursor as a stopgap; say if
F5 is bigger than a normal task. Then Task 14.
**G13b progress (09:45):** code in, one file (`ChestScreen.Visuals.cs`): `gridArea.SetAsLastSibling()`
after compare/junk; split hero panel inside a "Body" rect inset 42 from the top; SACK/HERO tabs on
a root "TabStrip" (same insets as the sack pad), built after the hero, before the prompt row;
`SetTab(tab, on, cursor)` — Bone when `Focus == Tabs` (the chips' idiom). Live (scratchpad
`popover_row2.cs`/`popover_row3.cs`): couch + solo, rows 2 and 3, Sell/Lock drawn over by nothing;
moving CompareArea last reproduced the bug in the same check. Couch Hero tab: strip visible, HERO
Bone, hero title 12 units under the strip. Captures taken. EditMode 680/680. PlayMode running;
combined spec+quality review running.
**Review (10:05) "Yes with fixes":** finding 1 (regression) — split hero doll is 42 shorter, so a
bottom-row worn piece's 5-row upgrade list slid under the opaque Stats panel → `doll.SetAsLastSibling()`
at the end of the HeroPanel ctor. **Found while verifying (pre-existing, fixed, flag as deviation):**
split on the Hero tab the worn popover NEVER showed — `RefreshWornPopover` is reached only via
RefreshSack, which Refresh skips there; A on worn gear opened an invisible list and Upgrade spent
coin on an invisible row. Fix: `if (!sack) RefreshWornPopover();` in Refresh's hero branch. Live:
menu [Upgrade][Take off][Lock] shows; Mythical Steel Boots (4 affixes) upgrade list 5 rows, none
drawn over; Stats-last mutation reproduces the overlap. EditMode 680/680; PlayMode + re-review
running. Reviewer also noted (pre-existing, board): split Hero tab hides the close ✕ with the
sack half — no visible pointer/touch way out there.
**Re-review 1 (10:15):** my new call showed a stale worn menu on Stats/Tabs (`OnWorn` stays true) →
`RefreshWornPopover` now hides unless Focus is Menu or Upgrade (solo unchanged: its only other
state there is Loadout). Live walk: Loadout hidden → A Menu SHOWN → B hidden → Stats hidden → Tabs
hidden → Upgrade SHOWN, 5 rows clear. **Re-review 2: Approved.** EditMode 680/680, PlayMode
28/28 on the final code. `DONE` sent — waiting for `COMMITTED` before Task 14.
**Task 14 prep (checked 09:10):** D57 "X does nothing at the shop rack" holds — Buy mode focuses
`ChestFocus.Stock` and `RunOption` returns unless Focus == Grid. Still to check: "Start leaves any
screen outright" vs the results screen (only Confirm leaves it; Start/Esc do nothing there) and the
front door (Start = title only — already amended); results bullet should gain the ¾ s dwell.

**Groundwork Task 13 — started (08:40, 2026-09-25).** G12 committed (`ad189cb`), deviations A–G
accepted. Brief: `task13.md` + `task13_extra.md` (solo filter-row guard + test). Phase 1 = the solo
test (red); phase 2 = plan Steps 1–2 + the guard. Then: recompile, EditMode 680 (async/assembly),
Step 3 measurement in play mode (Gameplay alone, two players, P1 placed at the chest via
`CharacterActor.PlaceAt` by reflection before opening), capture, PlayMode 28 for safety, reviews, DONE.
**Progress (08:50):** test red first ("Expected: Filters But was: Tabs"); code in (plan Steps 1–2 +
guard, verbatim; files CRLF on checkout, LF in git). EditMode 680/680. Step 3 live (Gameplay alone,
2 players, ScreenSpaceCamera canvas): chip bottom → first cell top = **32.0 canvas units** (clear
True; was −10); cell top flush with GridArea top. Couch cells now 79 units (were 87 — grid is
height-bound, fixed 5 rows); solo/shop keep 128 (HasTabs = split && !shop). Capture taken. PlayMode
running async; spec + quality reviews running.
**Done (09:05):** PlayMode 28/28. Spec ✅. Quality "Ready: Yes"; applied its finding 1 (rack
`offsetMax` = `GridTop - FilterTop`, same 62 at a shop; the dead `20f` branch would have gone stale).
EditMode 680/680 after. Pre-existing findings 2–4 (popover under compare panel; couch Tabs focus
invisible on the Hero tab; >40 stacks walk into undrawn cells) → orchestrator for the board.
`DONE` sent — waiting for `COMMITTED` before Task 14.

**Task 12 gates done (08:25, 2026-09-25):** bridge back. **Dirty-scene cause:** the only undo
record was my own font assignment ("Modified 3 properties in Frontend"); re-saving produced a
byte-identical file (sha1 cccd6cdf…), so the dirty flag carried no change — most likely the
property-edit undo record is flushed after `SaveScene` inside the same eval and re-marks the scene.
Practice from now: save in a **separate** eval call, and check `isDirty` before any sync run.
Fonts test 1/1; EditMode 679/679 (assembly filter, async); PlayMode 28/28 (async). No
InitTestScene leftovers. Step 8 captures (source: screen): title row "WASD Move · Enter Choose";
character select "P1 (Enter: ready) / P2 press A to join" + "WASD Pick · Enter Ready · Esc Back".
Live font proof: in a fresh play domain UiBuild.Display/Ui/Mono = PassionOne-Bold / Archivo /
SpaceMono, badge keys in PassionOne-Bold. Re-review: approved. **`DONE` sent — waiting for
`COMMITTED` before Task 13.**

**Task 12 (00:05, 2026-09-25):** phase 2 code verified line by line against task12.md +
task12_extra.md (only deviation: the plan's duplicate `actors` declaration in
`ResultsScreen.Repaint` dropped — the existing one at l.255 is the same `_driver.Characters.Ordered`).
Recompiled clean. **Fonts assigned in `Frontend.unity`** via eval + SerializedObject + SaveScene —
on disk exactly 3 lines added (the three GUIDs, fileID 12800000). Step 6 greps: grep 1 none; grep 2
only the `ChestScreen.cs:193` comment (accepted). **Bridge wedged:** the first `run_tests` (fonts
test) hit a "Scene(s) Have Been Modified — Frontend" save prompt (Frontend went dirty again after
the save — cause unknown, investigate with `scene.isDirty` once back). I pressed Cancel (did not
save unseen changes). **Bridge bug:** a sync `run_tests` that times out calls
`InvalidatePreviousRun` but never completes its awaited task, so `/api/exec`'s one-slot gate
(`BasePipelineServer.m_ExecGate`) is never released — every later command times out, even
`console`. Unity itself is responsive; `/api/status` answers 401 fast; `/api/progress` =
`{"active":true}`. Fix = Window → Pipeline → Stop Server + Start Server (new server object, new gate)
or any domain reload. **Trap: never start a sync `run_tests` with a dirty scene** — check
`EditorSceneManager` dirty state first. New scratchpad tools: `unity_dialog_text.ps1` (reads a
dialog's body + buttons), `unity_dialog_click.ps1 -Title … -Button …`.
Still to do: the three new tests green, EditMode 679, PlayMode 28, Step 8 capture, reviews, DONE.
**Reviews (00:20):** spec ✅ compliant. Quality "Yes with fixes": (1) Important — the join line said
"A or Enter" even with P1 on the keyboard (Enter is P1's → readies P1); **fixed**: Enter named only
when `FamilyOf(0) != Keyboard`. (2) Minor — `FamilyOf` null-guards `_input` (**fixed**). (3) Minor —
the fonts test doesn't prove `UseFonts` runs: prove it live in Step 8 instead (play mode reloads the
domain, so only FrontendFlow can have set `UiBuild.Display`; read a badge "Key" Text's font = Passion
One). (4) Minor — loot card names only the first player's button when both stand at a drop: not
fixed, report as a known limit (M9 HUD rebuilds these cards). No recompile yet (bridge down).
Orchestrator (00:10): cancelling the save prompt was right; find what dirtied Frontend and put it in
the DONE; run EditMode async or filtered by assembly, not one long sync call.

**Groundwork Task 12 — in progress (01:45).** G11 committed. Additions spec:
`<scratchpad>/tasks/task12_extra.md` (A Start title-only + test; B results dwell + test change;
C Frontend fonts + acceptance test — then I assign PassionOne-Bold / Archivo-Variable /
SpaceMono-Regular in `Frontend.unity` via eval; D IMGUI shadow strips colour tags). Phase 1 = the
three tests (red), phase 2 = plan Steps 1–5 + additions. Then: scene fonts, Step 6 grep (a comment
hit is fine — say so), suites (PlayMode 27 + 1 = 28; EditMode 678 + 1 = 679), Step 8 front-door capture.

**Groundwork Task 11 — committed.** 4 paths. EditMode 678/678,
PlayMode 27/27. Row built last in Build() (was hidden under the split hero board). **Next: Task 12**
— the biggest UI task: settings/results/front-door rows, loot/revive inline, plus the four
additions: results dwell (~0.75 s unscaled, test), Start on the title only (test), Frontend calls
`UseFonts`, IMGUI shadow pass strips colour tags. Read task12.md fresh.

**Task 11 progress (01:25):** code in (plan Steps 1–6 + Grid/Loadout-only-when-they-act), EditMode
678/678, PlayMode 27/27. Captures done in play mode (Gameplay alone): keyboard row "WASD Move ·
Enter Actions · J Sell · K Lock · Q/E Hero · Esc Leave the chest"; pad row (P1's SeatInput
`_lastDevice` set to a 2nd virtual pad by reflection — the 1st goes to P2's stand-in) brass Move ·
green A · blue X · yellow Y · square LB/RB Hero · red B; item-menu row "Move · A Choose · B Back · ≡
Leave the chest" (≡ renders via Archivo). Capture recipe traps: the driver CLOSES a screen whose
player is not at an interactable — place P1 at the chest with `CharacterActor.PlaceAt` (internal →
reflection) before `OpenScreen`; `capture_game_view save_path` is relative to **Assets/** (it made
`Assets/Screenshots/` — deleted via AssetDatabase); use inline captures only. Next: reviews, DONE.

**Groundwork Task 11 — started (01:20).** G10 committed. Addition:
`<scratchpad>/tasks/task11_extra.md` (Grid/Loadout prompts only when they act). QUIET for the
virtual-gamepad step was pre-approved by the orchestrator (01:18) — plan: use reflection for the
pad capture instead, so no quiet needed; say so in the DONE. Task 12 also gets: Frontend calls
`UseFonts` (orchestrator, 01:18 — list as a deviation there).

**Groundwork Task 10 — committed.** EditMode 678/678, PlayMode 27/27. Task 11 notes: Its Step 8 capture: keyboard family via
eval-opened chest + `capture_game_view` (settled — no quiet needed). For the pad capture, prefer
setting the screen's `_family` by reflection + `Refresh()` (visual check only) over QueueStateEvent,
which needs Unity foregrounded (→ QUIET REQUEST); the real "icons follow the hands" path is
Michael's checklist item 2. Note: a ScreenSpaceOverlay canvas is invisible to capture_game_view —
check the chest canvas's render mode (use `source: screen` in play mode).

**Groundwork Task 9 — committed.** EditMode 678/678. **Next: Task 10** (the badge row: `UiBuild` pad colours + RoundedSquare sprite,
`PromptRow`) — read task10.md fresh; apply the ≡-in-Archivo decision in `PromptRow.Draw` and a
dedupe-order comment (see "For Task 10" below). Nothing uses the row until Task 11, so gates are
recompile + EditMode (+ PlayMode for safety since UiBuild is shared).

**Groundwork Task 8 — started (23:30).** G7 committed. Addition: the Start twin test
(`<scratchpad>/tasks/task08_extra.md`). Expect PlayMode 23 + 3 = 26. The ChapterLoop results test
only checks open/pause (never presses to leave), so the results change will not break it.
**Task 13 gains (orchestrator, 23:28):** the solo filter-row bug — `&& !layout.HeroBeside` guard on
the Up-from-Filters→Tabs branch + a solo EditMode test; list under Deviations in G13's DONE.
**Task 14's checklist gains:** X sells a whole stack; a partner's sale can slide an item under your
cursor; no menu left open after combine-all.

**Groundwork Task 7 — committed.** 5 paths. EditMode 672/672,
PlayMode 23/23. **Next: Task 8** (settings + results on the new map; Escape never double-fires) —
read task08.md fresh; plan's PlayMode count says 21 but our base is 23 → expect 25 (+2), +1 if the
"start" twin is added. Watch: results screen all-hands-off swallow vs a scripted partner holding a
button (G2 note).

**Task 7 progress (23:20):** all code in (plan Steps 1, 3, 5, 6 + the stick fix). Stick test pushes
**up**, not right — with the partner the chest is the couch layout (hero = tab), so right from a
one-item sack cannot move; with right it passed without the fix. Proven: plan code → fail;
`_lastMove` alone → fail; both lines → pass. Nav 28/28, loot 9/9, EditMode 672/672. Full PlayMode
+ spec review running; then quality review, `DONE`.

**Groundwork Task 7 — started (22:55).** G6 committed (`4b602b7`). Addition spec:
`<scratchpad>/tasks/task07_extra.md` — the held-stick fix is TWO lines in the swallow branch
(`_lastMove = command.Move; _repeatAt = float.MaxValue;` — the reviewer's one-liner alone would
still move at once because `_repeatAt` starts at 0) + a PlayMode test (hold right through the
opening X; Y must lock the knife in cell 0). Expected PlayMode after Task 7: 18 + 1 route + 2 plan
tests + 1 = 22. EditMode: 668 + 4 nav tests = 672.

**Groundwork Task 6 — committed.** 9 paths. EditMode 668/668,
PlayMode 18/18, spec ✅, quality Yes. Raised to the orchestrator: Start-confirms vs D57 (open
question), stale front-door help text until Task 12, pointer-start homing side effect, same-frame
slot presses, Escape at the title now does nothing. **Next: Task 7** (chest on the new map) — read
task07.md fresh; carry the G2 note (held stick jumps the cursor after the opening press:
`_lastMove = command.Move;` in the swallow branch). PlayMode count after Task 7 per plan: 18 + 3 =
21 (plan said 19 from 16 — our base is 18).

**Groundwork Task 6 — started (22:30).** G5 committed. Additions spec:
`<scratchpad>/tasks/task06_extra.md` (A: FrontendState.Back(0)→Title releases slot 1 + test;
B: `UiBuild.PointerOnly` — Navigation.Mode.None — on the 3 runtime buttons, verified against
`Selectable.OnPointerDown` which only selects when mode != None; PlayMode asmdef also gets
`UnityEngine.UI`; C: 2 more SeatJoinSmokeTests — solo re-home on another pad, buttons pointer-only).
Expect PlayMode 15 → 18 (plan's 1 + 2). EditMode 667 → 668.

**Groundwork Task 5 — committed.** 20 paths (incl. 2 metas +
`ProjectSettings/EditorBuildSettings.asset`). EditMode 667/667, PlayMode 15/15, live eval ids 0/1,
spec ✅, quality Yes after the cross-scene reconnect fix (seed `_vanished` from
`InputSystem.disconnectedDevices`). **Next: Task 6** — read task06.md fresh; handle the UI-module
double-confirm note below *before* its PlayMode test; FrontendState.Back(0)→Title must release
slot 1; seating must use `LastDeviceOf` (= last press).

**Task 5 progress (21:58):** code + tests in (plan Steps 1–8 + extra A/B/C; the reconnect test
reaches the fixture's internal `runtime` by reflection — `runtime` is internal in this package).
Step 10 migration DONE via eval (prefab: PlayerInput removed, `_controls` + seat 0; Gameplay:
Player 2 `_seat` override 1; Frontend: Keyboard→seat 0, Gamepad→seat 1; no dialogs; Frontend
reopened). Verified on disk: 0 PlayerInput refs. Prefab re-save also serialized PlayerInventory's
five M6 fields at their code defaults (harmless). **Project-wide actions cleared**
(`EditorBuildSettings.asset` `m_configObjects: {}`). EditMode 666/666. PlayMode run in progress;
then Step 13 live check, reviews, `DONE`.

**Groundwork Task 5 — started (21:00).** Committed: G1 `56e4be5`, G2 `c73ef97`, G3 `66e8492`,
G4 `aaeaa1f`. EditMode before Task 5: 661; PlayMode 15.
Plan: phase 1 (implementer) = plan Steps 1–3 + the Reclaim tests from
`<scratchpad>/tasks/task05_extra.md` → red (Reclaim missing). Phase 2 (implementer) = plan Steps
4–8 + extra.md A/B/C. Then me: Step 9 recompile (acceptance tests red until migration), Step 10
eval migration of prefab + Gameplay + Frontend (check for a modal after each eval; reopen
Frontend at the end), Step 11 grep on disk, **clear the project-wide actions**
(`InputSystem.actions = null` in edit mode; verify `ProjectSettings/EditorBuildSettings.asset`
loses `com.unity.input.settings.actions`), Step 12 both suites, Step 13 live eval check.
**Project-wide actions evidence (for the G5 DONE):** our code never reads `InputSystem.actions`;
both runtime UI modules (`ChestScreenHost.EnsureEventSystem`, `FrontendFlow` ~l.308) are added with
`AddComponent<InputSystemUIInputModule>()` and no actions → `OnEnable` → `HasNoActions` →
`AssignDefaultActions()` = the package's `DefaultInputActions`, not the project-wide asset
(`InputSystemUIInputModule.cs:1647`); the editor Reset hook also loads the package default
(`InputSystemEditorInitializer.cs:91-100`). Only PlayerInput used the project-wide copy.

**Task 4 plan for the evidence requirement (decided, not yet built):** in `SeatInput`, make
`LastDeviceId` mean *the device of this seat's last button press edge* (`held & ~before`, from
our own held/previouslyHeld — not `WasPressedThisFrame`, since sim steps ≠ frames), persistent,
cleared in `Own` when that device leaves the seat; stick moves and the most-recently-updated guess
feed only `Family`. Keeps Task 5's `IInputDeviceReport`/`LastDeviceOf` names unchanged (update the
doc to "last pressed"). Add SeatInputTests: no press → `LastDeviceId` NoDevice while `Family`
still guesses; a button held on another device does not steal the last-press device.
**Remember: new files need their `.meta` paths in the `DONE`.**

Done so far: baseline EditMode 626/626, PlayMode 15/15. **G1** — menu vocabulary; also fixed
Pause/Heavy duplicate binding ids (since c253a83) and made the unique-id test read the raw file
(`InputActionAsset.FromJson(File.ReadAllText(..))`) because the importer silently re-rolls
duplicates. Gates at G1: EditMode 627/627, PlayMode 15/15.

**For Task 5:** `BattleBombControls` is the project-wide actions asset
(`ProjectSettings/EditorBuildSettings.asset`); Unity enables every map of `InputSystem.actions` on
all devices at startup. See the orchestrator's instruction under Answers (2026-09-24 20:08).

**For Tasks 4–6 (from G3's quality review — plan defects in the wiring):**
- **Follow must get evidence, not a guess.** `SeatInput.LastDeviceId` falls back to
  `MostRecentlyUpdated(owned)` when nothing was pressed (right for prompts), but Task 6 feeds it to
  `Seats.Follow` via `PlayerRegistry.LastDeviceOf`. Pointer start or a noisy HID pad → P1 homed on a
  guess; and after a title round trip P2 gets seated on seat 1's guess. Fix in Task 4/5: track a
  separate *last pressed* device (set on a press edge, cleared when the device leaves the seat) and
  have `LastDeviceOf` / the front door use that; keep the guess for `Family`.
  **HARD REQUIREMENT (G3 re-review):** it must be *persistent* — the last press, kept while that
  device stays in the seat — never "pressed this frame". G3's Follow unhomes P1 whenever their
  reported device is `NoDevice`, so a per-frame value would unhome P1 on every idle frame at
  character select and turn the next press on any device into P1's instead of a join. Until this
  lands, lost-home recovery re-homes P1 on a guess (most recently updated device), not a press.
- **Minor (Task 4):** `Sample` remembers the *last held action in list order*, so someone holding
  LB/Esc on another seat-1 device while P2 presses A seats P2 on the wrong device. Use the action
  pressed this sample.
- **Task 6:** `FrontendState.Back(0)` from Characters → Title keeps slot 1 joined/ready
  (`FrontendState.cs:~108-127`), while `Follow(Title)` forgets the seat → P2 re-seated on a guess.
  Make Back-to-Title clear slot 1 (+ a `FrontendStateTests` case). Consider one EditMode test driving
  `FrontendState` + `SeatAssignment` together in Task 6's order; extend the PlayMode test: after P2
  leaves, ready P1 → Chapters, B on pad 2 → Characters, A on pad 2 must ready P1, not join P2.
- **Reconnect (scope question sent to the orchestrator):** a reconnected pad keeps its
  `InputDevice` object but gets a **new `deviceId`** (`InputManager.cs:2762`, then
  `InputDeviceChange.Reconnected`). P2's pad waking from sleep mid-run would become P1's; P2
  stranded. PlayerInput re-paired regained devices, so this would be a regression.

**For Task 5 (from G4's quality review):**
- `IInputDeviceReport.LastDeviceId`'s doc in the plan says "the device that last did anything" —
  wrong since G4: it is the device of the last button *press* (see `SeatInput.cs` doc). Fix the doc.
- Reclaim: capture each device's id at `InputDeviceChange.Disconnected` (unchanged until re-added);
  on `Reconnected` call `Seats.Reclaim(old, device.deviceId)`. **Test it with
  `runtime.ReportNewInputDevice<Gamepad>()` / `runtime.ReportInputDeviceRemoved(pad)` +
  `InputSystem.Update()`** — `AddDevice<Gamepad>()` devices never go on the disconnected list, so
  they cannot test a reconnect (`InputTestRuntime.cs:278-298`, `InputManager.cs:2853-2877`).
  Two identical pads with no serials can come back as each other's objects. The stand-in's
  `FirstGamepad` (lowest id) swaps P1/P2 pads after a reconnect (Gameplay-scene-alone only).
- `PlayerActions.cs`' class doc still says PlayerInput clones the asset per player — update.

**Known limits reported, not built (G4 review):** taps shorter than one Input System update (or
inside frames with no sim step at >60 fps) are lost — pre-existing; fix would record presses
between samples via `performed`/`onAfterUpdate`. The pre-input prompt guess picks a PS pad
(streams reports) over an idle keyboard. Runtime binding overrides are not copied to seat copies
(matters at M13 rebinding). Steam Input may expose a PS pad twice (raw + virtual) — check before EA.

**For Task 6 (from G5's quality review) — MUST handle before its PlayMode test:** the runtime-added
`InputSystemUIInputModule`s (`FrontendFlow.cs:305-312,326-328`, `ChestScreenHost.cs:128-148`) get
`DefaultInputActions`, whose Submit/Navigate answer on *every* device (`*/{Submit}` = pad A, Enter),
and uGUI buttons default to Automatic navigation, so a mouse click *selects* a button — after any
click on Primary/Back, A or Enter also submits it, unseen by the seats. With Task 6 putting the
seat's Confirm on A/Enter, one press confirms twice (and P2's A fires `Confirm(0)`). Fix: pointer
buttons get `navigation = new Navigation { mode = Navigation.Mode.None }` (Frontend `MakeButton`,
the chest close button), or null the module's move/submit/cancel after adding it.
**Known limits (G5 review, report in DONE/D57 notes):** P1's own pad sleeping at character select
→ P1 unhomed → next press on any device is P1's (never a lockout; a would-be join in that window is
P1's). Two identical serial-less pads asleep at once can wake swapped; a pad that returns as a
brand-new device (`Added`, not `Reconnected`) loses its seat until the title. Gameplay-alone
stand-in follows the lowest-id pad (dev only).

**For Task 10 (from G9's review) — decided:** the "≡" Start label is NOT in Passion One
(`UiBuild.Display`, the badge font) nor Space Mono; only `Archivo-Variable.ttf` (`UiBuild.Ui`)
has U+2261. Windows substitutes it from Arial, so the plan's Task 11 "empty box → MENU" check
would pass here and still fail on console/Deck. In `PromptRow.Draw`, draw a badge label in
`UiBuild.Ui` when it is "≡" (Display otherwise) — deterministic, keeps the real icon. Also: the
dedupe keeps the *first* badge, so "Esc Back" wins only because screens list Back before Pause —
add a line saying so (or prefer Back explicitly).

**For Task 11 (from G10's review):** Step 8's expected keyboard row shows a bone "WASD" key cap
for Move (not a brass disc — that is pad only). Its "≡ → MENU" fallback is moot (≡ is drawn in
Archivo) and would not compile anyway (`System` is now `SystemButton`); a Windows capture cannot
prove ≡ renders. Layout fits (longest chest rows ~560–630 units vs ~790).
**For Task 12 (from G11's review):** the plan's Step 6 grep 2 (`'Light to\|Esc / Start\|Heavy: back'`)
will match an old *comment* at `ChestScreen.cs:~193` ("X, which is Light to the fight…") — accept
that hit (it is a comment, not a prompt) or exclude comments; don't "fix" the comment.
**For Task 12 (from G10's review):** (1) `LootHud.DrawLine` and `ReviveHud` draw each string twice
(black shadow, then colour); a `<color>` tag overrides the shadow colour, so the inline X/J would
draw twice in colour with no dark edge — strip the colour tags for the shadow pass. (2) Front-door
fonts: `UiBuild.UseFonts` is only called by `ChestScreenHost.OnEnable` (Gameplay scene), so on
first boot the front door uses the built-in font, and Archivo/Passion One only after a trip into
Gameplay — have something in `Frontend.unity` call `UseFonts`. (PromptRow now falls back to the
built-in font when the fonts are null, so letters are never blank.)

**For Task 8 (from G7's review):** `_busyBefore` must also cover Start closing the chest (Pause
alone — same race as Escape); add a "start" twin to `Escape_out_of_the_chest_does_not_open_the_settings`.
**For Task 11 (from G7's review):** show X Sell / Y Lock only when they act (a visible item on the
Grid; a non-empty worn slot on the Loadout) — as planned the row would advertise dead buttons.
The chest footer says Light/Magic/Heavy until Task 11 (on a pad, following it sells).
**For Michael's Task 14 checklist (G7's review):** item 5 — "and no menu stays open" after X
combines the pile; X sells a *whole stack* (menu Sell row shows the unit price); couch shared sack:
a partner's sale can slide another item under your cursor just before X.
**Pre-existing bug reported to the orchestrator (G7's review):** solo, Up from Filters goes to an
invisible Tabs strip (`ChestNavigation.cs:~260-267`); sideways there flips `Tab` to Hero unseen and
grid movement then goes via `MoveInHero`. Fix = `&& !layout.HeroBeside` guard + solo test.

**For Task 7 (from G2's quality review):** a stick held through the chest-opening press moves the
cursor — during the swallow `StepCursor` is skipped, so `_lastMove` stays zero and a still-held
stick reads as a fresh push on release (pre-existing since M6). Suggested one-liner inside the
swallow branch before its `return`: `_lastMove = command.Move;`. Decide in Task 7; add to Michael's
checklist ("walk into the chest holding right, tap X").

**For Task 8 (from G2's quality review):** the results screen's swallow needs *every* player's hands
off in the same step. In PlayMode a `ScriptedCommandSource` partner left holding a button (`Set`
holds until told otherwise) would stop the results screen from ever closing — watch for it in the
Task 8 smoke runs.

Prep done: whole plan read; split into one file per task at
`<scratchpad>/tasks/taskNN.md` + `preamble.md` (scratchpad is session-specific — re-split after a
reset if it is gone). Task 1's assumptions verified against the code (7 tests today, `Pause = 1 << 5`,
only the Gameplay map, highest binding id `…151` < the script's `0x200`, `Unity.InputSystem.
TestFramework.dll` already compiled). `.cs` files are LF/no BOM; the `.inputactions` is CRLF.

**How each task runs (my approach):**
- Implementer subagent writes the files — **no Unity MCP calls, no git writes, never touch paths it
  did not change**. Two phases: (1) write the test, report → I `recompile` and confirm the red;
  (2) continue the same agent (SendMessage) to write the implementation → I run the fixture + suite.
- **I run every Unity gate myself** (recompile, run_tests, eval, scene edits, captures) — the bridge
  wedges on one bad `filter_type` and jams on the external-edit modal, so it stays in one hand.
- Then spec review, then quality review (not Sonnet), then `DONE` to the orchestrator → wait for
  `COMMITTED` before the next task.

## Next steps

1. **Task 88** after 87's `COMMITTED` — brief `<scratchpad>/tasks/task88.md`. Expect EditMode 723 +
   its new tests.
2. **Task 89** — `NetSession`; apply the busy-port note above (Listen before `Role = Host`).
3. **Task 90** — `QUIET REQUEST` before any two-editor run (Multiplayer Play Mode package).
4. **Task 92** — bound/split the Events batch against `NetProtocol.MaxMessageBytes` + a test that
   sends a batch past the limit and shows it arrives whole. NetWriter stays uncapped.

## Answers and decisions

- 2026-09-25 00:50 (Orchestrator): **COMMITTED G8.** **Task 12 adds a results dwell:** in
  `ResultsScreen`, a minimum time on screen of ~0.75 s of *unscaled* real time (the world is
  paused), starting when the panel opens; A before the dwell does nothing, after it leaves. Pin it
  with a test; list under Deviations — the "NOT SAVED" warning must not be skippable by accident.
  Per-player arming, owner-only settings close, and the catch-up loop = board's later list (the
  catch-up item sits with F1/F4 for Netcode). Don't fix now.
- 2026-09-24 22:50 (Orchestrator): **COMMITTED G6.** **Start = title only** ("Press Start"); elsewhere
  in the front door it does nothing. D57 amended. **Do it in Task 12** (back in FrontendFlow for the
  prompts): condition becomes `slot == 0 && press.Pause && _state.Screen == FrontendScreen.Title`;
  pin with a test (SeatJoinSmokeTests or FrontendState): Start at chapter select does not launch.
  Pointer-start homing side effect + same-frame cross-slot presses = accepted limits (board's later
  list) — don't fix. Escape-at-title and stale help text: the orchestrator tells Michael.
- 2026-09-24 20:25 (Orchestrator): **Reconnect approved for Task 5.** Add
  `SeatAssignment.Reclaim(previousId, returnedId)` in Core + EditMode tests, fed from
  `InputSystem.onDeviceChange` in the **Gameplay layer next to `SeatInput`** (rule 3: only the
  source and its SeatInput touch devices). **Cover the front door too**: a pad that sleeps during
  character select keeps its seat. List it under "Deviations" in the Task 5 `DONE`.
  **D57 amended by the orchestrator (08f7c40)**: P1 is held to "the device they came into
  character select on", plus a line that a seat's controller that sleeps and wakes keeps its seat.
- 2026-09-24 20:08 (Orchestrator): **COMMITTED G1** (`56e4be5`, pushed). Its session restarted —
  **reply address now `uds:\\.\pipe\LOCAL\cc-msg-e4fdbd589a82ce7d4a7fdf41546e0095`** (name
  "Battlebomb"); the old pipe is dead. Only the Builder is running; the sim is mine uninterrupted.
  **Task 5:** decide the project-wide actions asset with evidence — default is to clear the
  project-wide assignment once PlayerInput is gone, unless something (e.g. the UI input module)
  needs it. Check whether `InputSystemUIInputModule` falls back to it before changing anything, and
  report either way in the Task 5 `DONE`.
- 2026-09-24 (Orchestrator, 5758b03): **D57 is already in `DECISIONS.md`** (recorded early from the
  plan's text, because M8 took D58–D62). Task 14 Step 1 is now: re-read D57 against what was
  actually built and send the orchestrator any correction. Re-split `task14.md` from the plan
  before Task 14 — the scratchpad copy predates this change.
- 2026-09-24 (Orchestrator, 853ad08): **message the orchestrator at the `from=` address of its
  last message, not by name** — Michael renames sessions. Last known:
  `uds:\\.\pipe\LOCAL\cc-msg-a5467c87452d77bccc60c704d3ef83a9` (session name then: "Battlebomb").
- 2026-09-24 (Orchestrator): until the bridge answers, read-only prep only — no Unity calls, no
  writes under `Assets/`. Once `editor_status` answers, run Step 0 + baseline, send `STATUS`, and
  start Task 1 without waiting. The orchestrator replies to whatever address my messages come from.
- 2026-09-24 (Michael, Groundwork kickoff): X = Sell (Combine-all mid-pick), Y = Lock, LB/RB =
  switch tab or mode — as proposed. X sells **instantly**; locks are the only safety. **Xbox
  letters only** on every controller.

## Log

- 2026-09-25 10:55 — **G14 sent** (D57 corrections ×3, GAME_DESIGN/CLAUDE/ROADMAP text, Michael's
  15-item checklist): final gates EditMode 680/680, PlayMode 28/28 at eb016a5. `DONE` sent —
  Groundwork built (56e4be5..527df2b); waiting for `COMMITTED` and Michael's pass. Next: F1–F3, F5.
- 2026-09-25 10:20 — **G13b complete** (tab strip on the screen + focus cursor; sack and hero
  popovers draw over the panels they overhang; couch Hero-tab worn menu now shows at all):
  EditMode 680/680, PlayMode 28/28, review approved. `DONE` sent. G13 committed as `7c3014f`.
- 2026-09-25 09:05 — **G13 complete** (chips clear the grid in couch co-op: 32 units, was −10; solo
  filter-row guard): EditMode 680/680, PlayMode 28/28, spec ✅, quality Yes. `DONE` sent. G12
  committed as `ad189cb`.
- 2026-09-25 08:30 — **G12 complete** (every other prompt; Start title-only; results dwell;
  front-door fonts; shadow strips tags; join line names only the free device): EditMode 679/679,
  PlayMode 28/28, spec ✅, quality approved after fixes. `DONE` sent. Next: Task 13 on `COMMITTED`.
- 2026-09-25 06:50 — Bridge still wedged after a 6 h watch (no server restart). Re-armed (6 h; also
  exits if Unity closes). Task 12 reviews done + fixed; Task 13 brief ready.
- 2026-09-25 01:40 — **G11 complete** (chest/hero badge row; prompts only for buttons that act;
  row over the split hero board): EditMode 678/678, PlayMode 27/27. `DONE` sent. G10 committed.
- 2026-09-25 01:15 — **G10 complete** (PromptRow + pad colours + rounded badge; ≡ in Archivo;
  null-font fallback): EditMode 678/678, PlayMode 27/27. `DONE` sent. G9 committed.
- 2026-09-25 01:00 — **G9 complete** (PromptGlyphs + tests; Assert.Multiple rewrite; Move explicit):
  EditMode 678/678. `DONE` sent. G8 committed.
- 2026-09-25 00:45 — **G8 complete** (settings/results on the new map; three double-fire races
  closed): EditMode 672/672, PlayMode 27/27. `DONE` sent. G7 committed.
- 2026-09-24 23:25 — **G7 complete** (chest on the new map + held-stick fix + combine-menu fix):
  EditMode 672/672, PlayMode 23/23. `DONE` sent. G6 committed as `4b602b7`.
- 2026-09-24 22:45 — **G6 complete** (front door hands out seats; title forgets P2; pointer-only
  buttons): EditMode 668/668, PlayMode 18/18. `DONE` sent. G5 committed.
- 2026-09-24 22:20 — **G5 complete** (seats replace PlayerInput + Reclaim + project-wide actions
  cleared): EditMode 667/667, PlayMode 15/15. `DONE` sent. G4 committed as `aaeaa1f`.
- 2026-09-24 20:50 — **G4 complete** (SeatInput): 13/13, EditMode 661/661; seats by real presses;
  held-through-plug-in fixed. `DONE` sent. G3 committed as `66e8492`.
- 2026-09-24 20:28 — **G3 complete** (SeatAssignment): 14/14, EditMode 648/648; home-device fix.
- 2026-09-24 20:06 — **G2 complete** (MenuPress): 7/7, EditMode 634/634, both reviews passed.
  `DONE` sent. G1 committed as `56e4be5`.
- 2026-09-24 20:00 — **G1 complete**: EditMode 627/627, PlayMode 15/15, both reviews passed. Found
  and fixed Pause/Heavy duplicate binding ids; hardened the unique-id test. `DONE` written here —
  the orchestrator is unreachable, so paused before Task 2.
- 2026-09-24 19:33 — Editor open; Step 0 passed (`editor_status` ready, stopped, no console
  errors). Plan re-split (only task14 had changed). EditMode baseline 626/626. Traps: `run_tests`
  `mode` takes `editor` / `playmode` (the tool's schema); an async PlayMode `run_tests` **times out
  on the request** (the play-mode reload drops the reply) but the run starts — poll `test_status`.
- 2026-09-24 18:56 — Still no editor after a 3h watch (Unity Hub is open, the editor is not; the
  bridge still reports 0 tools). Re-armed the watch (6h).
- 2026-09-24 — ANSWER from the orchestrator: prep read-only until the editor is up. Plan split
  into per-task files; Task 1's assumptions checked.
- 2026-09-24 — ONLINE (session "BattleBomb Builder lane"). Step 0 failed: editor not open. BLOCKED
  sent. The orchestrator now lists as **"BattleBomb Orchestrator"** — "BattleBomb Planning" no
  longer resolves.
- 2026-09-24 — Lane seeded by the orchestrator.
