# M8 Plan 3 — Feel and Steam (Tasks F4, 106–114) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The guest's own hero answers the guest's hands at once, even over a bad connection (D62). Two friends in two homes then play a real game over Steam: one opens a solo game, the other joins it from the Steam friends list (D59). Michael's app ID replaces Spacewar at the close-out.

**Architecture:** Host-authoritative, unchanged (D58). The host's simulation does not change at all. The guest runs a **second, smaller step** for its own hero only, `CharacterActor.PredictStep`: the motor, the combat machine, and the mana and vitals tick, with nothing that belongs to the host (no hits, grabs, revives, chests or quick-use). Each prediction is logged against the command that produced it. When a snapshot says "the host has played your command *n*", the guest takes the host's state for its hero at *n* and replays *n+1…now* from the log. Any difference is drawn away over a few frames. Steam arrives behind the Platform seam as a separate assembly that nothing references. It compiles only when Steamworks.NET is installed and registers itself at startup. It provides the identity, a peer-to-peer transport over Valve's relay, "Join game" through rich presence, and (on BattleBomb's own app) Steam Cloud saves. Without Steam the game runs exactly as it does today (rule 6).

**Tech Stack:** Unity 6.5 (6000.5.8f1), C# (.NET Standard 2.1), Input System 1.20 (action callbacks), Steamworks.NET 2025.164.1 (Steamworks SDK 1.64, UPM git package, pinned tag), NUnit EditMode + PlayMode suites, the Unity MCP bridge, Multiplayer Play Mode (Michael's feel pass only).

**Source of scope:**
- `docs/HANDOFF-M8.md`: Stages E and F, the close-out, and planning decisions 3, 4 and 19.
- Decisions D58, D59, D61 and D62.
- Michael's lag table (Task 96, 2026-09-26):

  | Lag | Moving | Attacking |
  |---|---|---|
  | None | fine | fine |
  | Normal | noticeable but OK | noticeable but OK |
  | Bad | too late | too late |

  At Bad the guest is unplayable without prediction, so prediction is required, not optional polish.
- The board's "Carried into later tasks → Plan 3" list, Task 96's deferred feel items, and candidate F4. Every item is placed below; see "Where the carried items land".

**Written against:** `main` at `a1abd69` (Task 96 committed; Plan 1 closed), **with Plan 2 (Tasks 97–105) planned but not yet built**. The Builder was on Task 97 when this plan was written.
- Every Plan 1 file was read at `a1abd69`.
- Every Plan 2 shape this plan relies on was read from `docs/superpowers/plans/2026-09-25-m8-plan2-menus-saves-joining.md`. Anchors inside code that Plan 2 adds are quoted from that plan.
- **Before each task, re-read every file it modifies.** Where a quoted line has changed (Plan 2 as built, or an inserted `96x` fix), keep the change, apply this plan's intent on top, and say so in the `DONE`.

**How this plan changes code.** As in Plan 2: **no existing file is ever replaced wholesale.** Every edit to an existing file is anchored: "replace this exact block with this one", or "after this exact line, add". New files are given whole. If an anchor is not found verbatim, stop and re-read; do not guess.

**The wire does not change.** Prediction needs nothing new on the wire:
- the snapshot already carries `AckGuestFrame`, the guest command the host last played (Plan 1 put it there for this plan);
- the guest already predicts with exactly the quantised command it sends (`CommandCodec.Quantized`).

`NetProtocol.Version` stays where Plan 2 leaves it (8).

Consequences worth knowing before building (surface them in Task 114's close-out):

1. **The couch changes in one way: F4.** A button tapped and released between two simulation steps used to vanish. Now it counts as pressed for one step. This is most visible above 60 fps, and in the revive heartbeat's mashing (D31).
2. **The guest's own hero is no longer drawn from the host's picture.** It moves the moment the guest presses. Its health, stagger and statuses come from the newest snapshot rather than the interpolated one, so a hit shows on the guest's health bar about a tenth of a second sooner than before.
3. **When the host disagrees, the host wins, and the difference is eased, never snapped.** The guest's hero slides to where the host says over about a fifth of a second, unless the jump is a teleport (a respawn or an airlock).
4. **Steam in the editor.** Once Steamworks.NET is installed, every play session on a PC with Steam running signs in to Steam as Spacewar (app 480). A solo game then opens to Steam friends by default (D59). With Steam closed the editor behaves exactly as today.
5. **The save's name becomes the Steam ID** when Steam is running. The first time, the machine's existing `local` save is copied under the new name, so nothing looks lost. It is a copy; the old file stays.
6. **No Steam lobby objects.** Friends join through Steam's rich presence ("Join game" in the friends list) and the overlay's "Invite to game". Both carry a connect string naming the host. That covers everything D59 asks for, with no lobby state to keep in step. See the departures below.

**Where this plan departs from HANDOFF-M8.** Send the orchestrator this text for the spec, in Task 114 Step 5.

- **Delta snapshots are not built.** Task 96 measured the paper case at 2.9–3.9 KB per snapshot (86–117 KB/s). The worst case is 56 KB at the game's real four-status ceiling. That is comfortably inside Steam's relay once its send-rate floor is raised, and Task 110 raises it explicitly (256 KB/s floor, 1 MB/s ceiling). The lever is parked for M11 (Endless), where enemy counts grow; enemies are 60 % of a snapshot, and each status costs 24 B.
- **Friends join by rich presence, not through a Steam lobby.**
  - While a game listens with a free seat, the host's rich presence carries a `connect` string: `+battlebomb_join <host Steam ID>`.
  - Steam shows "Join Game" on that friend in the friends list and offers "Invite to Game" in the overlay.
  - Accepting either raises `GameRichPresenceJoinRequested_t` in the friend's running game, or passes the string on the command line.
  - `ILobbyService` is therefore smaller than planning decision 3 describes. It creates the platform's transport and holds a waiting join request, and that is all. "Open" means "the transport is listening"; the platform advertises it on its own.
- **`PlatformRegistry` holds the one live platform instance** as well as the factory. A storefront's API is one per process, so this is the platform connection, not game state (rule 7).
- **Prediction steps aside when the host stops answering.** If the newest acknowledged command is more than 45 steps (0.75 s) old, the guest's hero is drawn from the picture again until answers resume. Without this, the guest could walk around freely while the host holds for a loading guest (the airlock), then be snapped back.
- **The guest's own side state comes from the newest snapshot**, not the interpolated picture. This is a consequence of reconciling against the newest snapshot.
- **Steam Cloud is used only on BattleBomb's own app ID.** On Spacewar (every build until Task 114) saves stay in local files: Spacewar's cloud space belongs to Valve's test app. The code is built in 112 and switched on by the app-ID swap in 114.
- **`SteamAPI.RestartAppIfNecessary` is not called.** Every M8 build runs with `steam_appid.txt` beside it, where the call does nothing. It belongs to the Early Access release checklist (M13).
- **`INetTransport.Listen` returns why it failed** (null when it worked), rather than throwing. `NetSession` no longer knows what a socket is (the board's Plan 3 item).
- **The Steam assembly lives at `Platform/Steam/`**, with its own asmdef inside Platform's folder, not at a top-level `Platform.Steam/`. CLAUDE.md's layout table keeps its top-level list; it gains one line for the new assembly (the orchestrator's edit, in Task 114's `DONE`).

---

## Before you start

- [ ] **Plan 2 is committed through Task 105**, with any `96x` fixes. Task 103's `ReplicaWorld.HideUntilSeen`, Task 104's `NetSession.FriendsTransport`, and Task 104's dev-panel "Friends" and "Go quiet" rows are all anchors here. If the orchestrator starts Plan 3 early, stop at the first task whose anchors are not in the tree yet.
- [ ] **The Unity MCP bridge must be connected.** It is the test gate. If `editor_status` does not answer, stop and ask the orchestrator (Michael opens Unity).
- [ ] `editor_status`. If the editor is in play mode, `editor_stop` (standing permission).
- [ ] Record the baseline:
  - `run_tests` mode `EditMode`. Expected 839 after Plan 2, plus whatever `96x` added.
  - `run_tests` mode `PlayMode` with **`async_tests: true`**. Expected 106 after Plan 2, plus `96x`.

  Write the two numbers in your lane file: every count below is "baseline + N".
  - **PlayMode is always async.** A synchronous PlayMode run that times out wedges the bridge until Michael restarts it. An async run whose request "times out" keeps running: wait about 300 s for the full suite, then read `test_status`.
  - Full EditMode output spills to a file. Read the summary with `head -c 400 "<path>"`, and failures with `grep -B3 -A8 '"Status": "Failed"' "<path>"`.
- [ ] After every PlayMode run, delete Unity's generated `Assets/InitTestScene*.unity` files and their `.meta`s.
- [ ] **Line endings are mixed; keep each file's own.** Check with Python bytes, not Git Bash `grep` (it miscounts CR). New files are LF.

  | Ending | Files this plan touches |
  |---|---|
  | CRLF | `CharacterActor.cs`, `SimulationDriver.cs`, `GameSession.cs`, `FrontendFlow.cs`, `InterpolatedVisual.cs`, `IPlatformServices.cs`, `NullPlatformServices.cs`, `FileSaveStore.cs`, `ArchitectureFitnessTests.cs`, `NullPlatformServicesTests.cs` |
  | LF | `SeatInput.cs`, every `Net` file (Core, Gameplay and Platform), `NetDevOverlay.cs`, every test not listed as CRLF |
- [ ] Bash heredocs break on apostrophes in this harness. Write C# with the Write tool, and anchored edits with the Edit tool or a Python script in the scratchpad.
- [ ] **PlayMode per-step facts come from `Stepped`, never from `Frame` read between yields.** The editor can run several steps in one render frame.
- [ ] **QUIET, per task:**

  | Task | QUIET | Why |
  |---|---|---|
  | 108 | **Yes** | Michael's two-editor feel pass |
  | 113 | **Yes**, for its last step | The collaborator's first internet game; Michael's attention, not the machine's |
  | 114 | **Yes** | The two-home pass |
  | Every other task | No | Proven by the harnesses. The bridge cannot click in the Multiplayer Play Mode clone, and Michael has declined screen control: never reach the clone another way. |

- [ ] **Steam in tests.** Until Task 109 nothing here touches Steam. From 109 on:
  - the PlayMode suite pins itself offline (Task 109 Step 8), so it behaves the same with Steam open or closed;
  - only the `BattleBomb.Tests.Steam` assembly talks to Steam, and each of its tests is **Ignored** (not failed) when the Steam client is not running.

  Run them once with Steam open (Michael's PC normally has it) and say which ran in the `DONE`.

**How to run a single fixture:** `run_tests` with `mode: EditMode` (or `PlayMode` with `async_tests: true`), `filter_type: testName`, `filter: <FixtureName>`.
- The filter matches substrings, not regexes (`"A|B"` matches nothing).
- Valid `filter_type` values are `testName`, `assembly` and `category`. Anything else wedges the bridge.

**Commit convention:** one commit per task, subject `<task>: <what>`, and a body explaining why, ending with:

```
Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

In the team setup the sim holder sends `DONE` and the orchestrator commits (`docs/team/PROTOCOL.md` rule 9). Every "Commit" step below is a `DONE`.

### Where the carried items land

| Carried item | Lands in |
|---|---|
| Candidate F4: a tap shorter than one sample is lost | **Task F4** (first) |
| `catch (SocketException)` in Gameplay, always naming port 7777 | 110 |
| Delta snapshots, if the bandwidth needs them | **Not built.** Decided on 96's numbers (departures above). 110 sets Steam's send rate. |
| Task 107: a Jump in a frame the host skipped | 107 — written down; built only if 108 shows it (see 107) |
| Scale the teleport threshold by the gap between snapshots (93) | 107 |
| Hold the render clock through a host pause (93) | 107 |
| Tune let-go and drain (`StarvedRepeatSteps`, `InputBufferDrainSteps`) | 107 — decided unchanged, re-checked by Michael in 108 |
| 96b: arrow keys cross windows in the two-editor rig | 108's sheet carries the Builder's workaround |
| `PlayerSnapshot.OpenScreen` still on the wire, unread | Stays unread. The guest's own screen already reaches its prediction through the driver (Plan 2's menu events). |

### If Michael's Plan 2 pass (Task 105) finds something

As before, a finding lands as a small inserted task (`105a`, `105b`…), done before whichever Plan 3 task first touches the same file. The files both plans touch:

| File | Plan 3 tasks |
|---|---|
| `Gameplay/Net/NetGuest.cs`, `Gameplay/Net/ReplicaWorld.cs` | 106, 107 |
| `Gameplay/Net/NetSession.cs` | 106, 110, 111 |
| `Gameplay/Simulation/SimulationDriver.cs` | 106 |
| `Gameplay/Session/GameSession.cs` | 109 |
| `UI/Frontend/FrontendFlow.cs`, `UI/Combat/NetBanner.cs` | 111 |
| `UI/Debug/NetDevOverlay.cs` | 106, 111 |
| `Tests/PlayMode/PlaybackTransport.cs` | 106, 110 |

---

## File map

**Core** (`Assets/_BattleBomb/Core/`)
- Create `Net/PredictionLog.cs`: `PredictedBody`, `PredictionEntry`, `PredictionLog` (106).
- Create `Net/ReplicaMotion.cs`: the slide-or-teleport rule (107).
- Modify:
  - `Net/NetProtocol.cs`: `PredictionMaxAheadSteps`, `CorrectionDecayPerStep` (106).
  - `Net/RenderClock.cs`: the stall hold (107).

**Platform** (`Assets/_BattleBomb/Platform/`)
- Create `ILobbyService.cs` and `PlatformRegistry.cs` (109).
- Modify:
  - `IPlatformServices.cs`: `Lobby` (109).
  - `NullPlatformServices.cs`: an offline `ILobbyService` (109).
  - `Net/INetTransport.cs`: `Listen` returns why it failed (110).
  - `Net/LocalSocketTransport.cs`, `Net/LoopbackTransport.cs`, `Net/LagSimulator.cs` (110).
- Create `SaveNames.cs`: one name-mangling rule for every store (112). Modify `FileSaveStore.cs` to use it (112).

**Platform.Steam** (`Assets/_BattleBomb/Platform/Steam/`, a new assembly `BattleBomb.Platform.Steam`, compiled only when Steamworks.NET is installed)
- Create:
  - `BattleBomb.Platform.Steam.asmdef` and `AssemblyInfo.cs` (109);
  - `SteamPlatformServices.cs` and `SteamCallbackPump.cs` (109; the lobby 110, 111; the cloud 112);
  - `SteamP2PTransport.cs` (110; presence 111);
  - `JoinAddress.cs` (111);
  - `SteamCloudSaveStore.cs` (112).

**Gameplay** (`Assets/_BattleBomb/Gameplay/`)
- Modify `Players/SeatInput.cs`: taps between samples (F4).
- Modify `Characters/CharacterActor.cs`: `PredictStep`, `CaptureBody`, `RestoreFromHost`, `CorrectionOffset` (106).
- Modify `Simulation/SimulationDriver.cs`: `LightIsContextual` (106).
- Create `Net/LocalPrediction.cs` (106).
- Modify `Net/NetGuest.cs`, `Net/ReplicaWorld.cs` (106, 107).
- Modify `Net/NetSession.cs`:
  - `PredictOwnPlayer` (106);
  - `Listen`'s answer (110);
  - `FriendsTransport` from the platform, `JoinFriend`, `JoinWaitingFriend`, `FriendJoinWaiting` (111).
- Modify `Session/GameSession.cs`: the platform from the registry, and adopting the local save (109).

**Presentation** — Modify `Characters/InterpolatedVisual.cs`: draw the correction offset (106).

**UI** — Modify:
- `Debug/NetDevOverlay.cs`: the prediction toggle (106); the Steam line and "Join Steam ID" (111).
- `Frontend/FrontendFlow.cs`: a waiting friend's game is joined at the front door (111).
- `Combat/NetBanner.cs`: the waiting-invite line (111).

**Editor** — Create `Editor/Build/CollaboratorBuild.cs` (113).

**Packages / project root**
- Modify `Packages/manifest.json`: Steamworks.NET (109).
- Create `steam_appid.txt` at the repository root: `480` (109; the real ID in 114). **It is outside `Assets/`: list it in the `DONE` so the orchestrator commits it.**

**Tests** (`Assets/_BattleBomb/Tests/`)
- Modify EditMode:
  - `SeatInputTests.cs` (F4);
  - `Net/RenderClockTests.cs` (107);
  - `Net/LocalSocketTransportTests.cs`, `Net/LoopbackTransportTests.cs`, `Net/NetSessionHostTests.cs` (110);
  - `ArchitectureFitnessTests.cs`, `NullPlatformServicesTests.cs` (109).
- Create EditMode:
  - `Net/PredictionLogTests.cs` (106);
  - `Net/ReplicaMotionTests.cs` (107);
  - `PlatformRegistryTests.cs`, `SaveAdoptionTests.cs` (109);
  - `Net/FriendsTransportTests.cs` (111);
  - `SaveNamesTests.cs` (112).
- Create PlayMode:
  - `AnsweringHost.cs` and `LocalPredictionSmokeTests.cs` (106);
  - `OfflinePlatform.cs`, the suite's offline pin (109);
  - `FriendJoinSmokeTests.cs` (111).
- Modify PlayMode: `PlaybackTransport.cs` (106 — prediction off; 110 — `Listen`).
- Create the Steam test assembly (`Tests/Steam/`, EditMode, compiled only with Steamworks.NET):
  - `BattleBomb.Tests.Steam.asmdef` and `SteamSessionTests.cs` (109);
  - `SteamTransportTests.cs` (110);
  - `JoinAddressTests.cs` (111).

**Docs**
- `docs/team/m8-plan3-pass.md`: Michael's sheet for 108, 113 and 114, written with this plan.
- `docs/team/netcode/collaborator-how-to-run.md`: the one-page sheet. It ships inside the build as `HOW-TO-RUN.txt` (113).
- HANDOFF-M8 and ROADMAP text goes to the orchestrator in Task 114's `DONE`.

---

## Input fidelity

### Task F4: A tap between two samples counts

**Why it comes first.**
- **The bug.** `SeatInput.Sample` reads what is held *at the instant a step samples*. A button pressed and released between two samples is never seen. That happens with a quick tap, or with any press that lands in a render frame with no simulation step (above 60 fps).
- **Why fix it before the feel pass.** Michael's feel pass (Task 108) is about whether the guest's presses land. A press lost at the keyboard would be blamed on the network.
- **Who it affects.** It is the same on the couch; the revive heartbeat's mashing (D31) is where it bites hardest. The wire neither adds nor removes the loss: a one-step press survives packet loss through the command redundancy.

**The fix.**
- Each button action's `performed` callback marks the button "tapped since the last sample", along with the device it came from.
- The next sample treats a tapped button as held for that one step, so it is pressed on that step and released on the next.
- A button already held at the last sample is never pressed again. A device change can re-perform a held action (the G4 lesson), and the existing edge rule (`pressed = held & ~before`) keeps that from becoming a second press. A tap of an already-held button is simply ignored, so its release is not delayed.
- Two taps between one pair of samples are still one press. At 60 samples a second that is a mash faster than 30 presses a second per button.

**Files:**
- Modify: `Assets/_BattleBomb/Gameplay/Players/SeatInput.cs` (LF)
- Test: `Assets/_BattleBomb/Tests/EditMode/SeatInputTests.cs` (LF)

- [ ] **Step 1: Write the failing tests**

In `Assets/_BattleBomb/Tests/EditMode/SeatInputTests.cs`, after the method `A_solo_player_answers_to_the_keyboard_and_every_controller` (its closing brace), add:

```csharp

        [Test]
        public void A_tap_between_two_samples_is_pressed_for_one_step()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);

            // Down and up again before the next sample: each goes through its own Input System update, as a quick tap
            // or a frame with no simulation step does (F4).
            Press(_keyboard.jKey);
            Release(_keyboard.jKey);
            PlayerCommand tapped = Next(one, seats);
            Assert.That(tapped.WasPressed(CommandButtons.Light), Is.True, "A tap shorter than a step was lost.");
            Assert.That(tapped.IsHeld(CommandButtons.Light), Is.True, "A tap is held for the one step it is seen.");

            PlayerCommand after = Next(one, seats);
            Assert.That(after.WasReleased(CommandButtons.Light), Is.True, "The tap never let go.");
            Assert.That(after.WasPressed(CommandButtons.Light), Is.False, "One tap pressed twice.");
            Assert.That(after.IsHeld(CommandButtons.Light), Is.False);
        }

        [Test]
        public void A_tap_between_two_samples_names_the_controller_it_came_from()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);

            Press(_padB.buttonSouth);
            Release(_padB.buttonSouth);
            Assert.That(Next(one, seats).WasPressed(CommandButtons.Jump), Is.True, "The tap was lost.");
            Assert.That(one.LastDeviceId, Is.EqualTo(_padB.deviceId),
                "The front door seats a player by the pad they pressed on (D57) — a quick tap included.");
        }
```

- [ ] **Step 2: Run them and see them fail**

`recompile`, then `run_tests` mode `EditMode`, filter `SeatInputTests`.

Expected:
- `A_tap_between_two_samples_is_pressed_for_one_step` fails with "A tap shorter than a step was lost."
- `A_tap_between_two_samples_names_the_controller_it_came_from` fails with "The tap was lost."
- The other fifteen pass.

- [ ] **Step 3: Record taps between samples**

In `Assets/_BattleBomb/Gameplay/Players/SeatInput.cs`:

(a) After the field `private InputDevice _lastPressed;` add:

```csharp

        /// <summary>Buttons pressed since the last sample, whether or not they are still down, and the device of the latest
        /// (F4). A sample reads what is held at one instant, so without this a tap that starts and ends between two
        /// samples — a quick mash, or a frame with no simulation step above 60 fps — is never seen.</summary>
        private CommandButtons _tapped;
        private InputDevice _tappedOn;
```

(b) In `AddButton`, replace

```csharp
            if (action != null)
            {
                _buttons.Add((action, button));
            }
```

with

```csharp
            if (action != null)
            {
                _buttons.Add((action, button));
                action.performed += context => Tap(button, context.control);
            }
```

(c) In `Sample`, replace

```csharp
            CommandButtons before = _primed ? _previouslyHeld : held;
            _primed = true;

            // The device of a fresh press, not of whichever held action came last in the list:
            // someone resting on another pad's shoulder must not take Player 2's join press.
            CommandButtons pressed = held & ~before;
            for (int i = 0; i < _buttons.Count && pressed != CommandButtons.None; i++)
            {
                InputControl control = _buttons[i].action.activeControl;
                if ((pressed & _buttons[i].button) != 0 && control != null)
                {
                    _lastPressed = control.device;
                    _lastDevice = control.device;
                    break;
                }
            }
```

with

```csharp
            CommandButtons before = _primed ? _previouslyHeld : held;
            _primed = true;

            // A press since the last sample counts even if it has already let go (F4): this step sees it held, so it is
            // pressed now and released on the next. A tap of a button already down at the last sample adds nothing — it
            // cannot be a new press, and holding it on would only make its release a step late.
            CommandButtons tapped = _tapped & ~before;
            held |= tapped;
            _tapped = CommandButtons.None;

            // The device of a fresh press, not of whichever held action came last in the list:
            // someone resting on another pad's shoulder must not take Player 2's join press.
            CommandButtons pressed = held & ~before;
            InputDevice pressedOn = null;
            for (int i = 0; i < _buttons.Count && pressed != CommandButtons.None; i++)
            {
                InputControl control = _buttons[i].action.activeControl;
                if ((pressed & _buttons[i].button) != 0 && control != null)
                {
                    pressedOn = control.device;
                    break;
                }
            }

            // A tap already let go has no active control left; the device it came from was kept when it was pressed.
            if (pressedOn == null && tapped != CommandButtons.None)
            {
                pressedOn = _tappedOn;
            }

            if (pressedOn != null)
            {
                _lastPressed = pressedOn;
                _lastDevice = pressedOn;
            }

            _tappedOn = null;
```

(d) After the method `Remember` add:

```csharp

        private void Tap(CommandButtons button, InputControl control)
        {
            _tapped |= button;
            if (control != null)
            {
                _tappedOn = control.device;
            }
        }
```

**Why `performed` and not `WasPressedThisFrame`.** The polling call compares against the Input System's *current* update, so it misses a press made two updates before the sample. The callback fires once for every press the Input System processes, however many updates sit between two samples. None of the project's button actions has an interaction (checked in `BattleBombControls.inputactions`), so `performed` is the press itself.

**Why no unsubscribe.** Each seat owns its own copy of the action asset. `Dispose` disables and destroys that copy, so no callback can outlive the seat.

- [ ] **Step 4: Run them and see them pass**

`recompile`, then `run_tests` mode `EditMode`, filter `SeatInputTests`. Expected: 17/17.

Those include the tests that guard held buttons across device changes: `A_button_already_down_when_the_seat_wakes_is_not_a_press`, `A_button_held_through_a_controller_plugging_in_stays_held`, and `A_button_held_while_its_device_changes_seats_is_not_the_new_seats_press`. If any goes red, the tap is being counted before `before` is taken. It must be added to `held` only after `before` is computed from the untouched `held`, exactly as in (c).

- [ ] **Step 5: The gates**

- `run_tests` mode `EditMode`: baseline + 2, all passing.
- `run_tests` mode `PlayMode` (async): baseline, all passing. The front door, the chapter loop and the online suites all sample through `SeatInput`.

- [ ] **Step 6: Commit (`DONE`)**

Paths:
- `Assets/_BattleBomb/Gameplay/Players/SeatInput.cs`
- `Assets/_BattleBomb/Tests/EditMode/SeatInputTests.cs`

Subject: `F4: a tap between two samples counts`. Body: a tap that starts and ends between two simulation steps now counts as held for one step, so quick mashes and presses in frames with no step (above 60 fps) are no longer lost; a button held across a device change is still never pressed twice. First of M8 Plan 3, so Michael's feel pass judges the network, not the keyboard.

---

## Stage E — Feel

### Task 106: The guest predicts its own hero

**Why.** Michael's lag table says it plainly: at Bad lag (200 ms, 2 % loss), moving and attacking are "too late". Today the guest's own hero is drawn from the host's picture. Every press must go to the host, be played, come back in a snapshot, and then wait out the 100 ms interpolation cushion before it shows: roughly a round trip plus a tenth of a second.

D62, Michael's decision 7: predict the guest's own movement and swings.
- Run, jump, facing, and the start of every attack and cast react on the guest's PC at once.
- Hit confirmation, damage numbers and enemy reactions still come from the host, a round trip later.

**How it works.** Each guest step, after the command is sent:

1. **Reconcile, if a new snapshot arrived.**
   - The snapshot names the last guest command the host played (`AckGuestFrame`, *n*) and where the hero stood after it.
   - The guest puts its hero there (`RestoreFromHost`) and replays every logged command after *n*.
   - Wherever that leaves the hero, compared with where it was drawn, becomes a `CorrectionOffset`. The offset is drawn and eased away by a fifth each step.
2. **Predict this step** with the command just sent (`PredictStep`), and log the result.

`PredictStep` is `Step` without anything the host owns:
- no revive channel, loot grab, chest, quick-use, or hit window;
- nothing raised, nothing spent from the bag.

A Light press that the host would spend on something other than a swing starts no swing here. That covers a downed partner in revive range, a drop underfoot, or a chest or shop in reach (`SimulationDriver.LightIsContextual`, judged from what this machine draws). Otherwise the guest would start a swing the host never plays and be pulled out of it a round trip later.

**What the snapshot doesn't carry.** A swing's lunge (`_lungePerStep`, `_lungeStepsLeft`, `_attackRooted`) never travels. The log keeps the guest's own record of it at each step, and a replay starts from the host's state plus that record.

**When prediction steps aside** (the hero is drawn from the picture exactly as before this task):
- the hero is waiting to appear (a drop-in, Task 103);
- no snapshot has yet named a command of this machine's;
- the newest answer is more than `NetProtocol.PredictionMaxAheadSteps` (45) steps old — the host has stopped answering;
- `NetSession.PredictOwnPlayer` is off. That is the development panel's switch for Michael's A/B test, and every recorded-host test.

**Files:**
- Create: `Assets/_BattleBomb/Core/Net/PredictionLog.cs`, `Assets/_BattleBomb/Gameplay/Net/LocalPrediction.cs`
- Modify:
  - `Assets/_BattleBomb/Core/Net/NetProtocol.cs` (LF)
  - `Gameplay/Characters/CharacterActor.cs` (CRLF)
  - `Gameplay/Simulation/SimulationDriver.cs` (CRLF)
  - `Gameplay/Net/NetGuest.cs`, `Gameplay/Net/ReplicaWorld.cs`, `Gameplay/Net/NetSession.cs` (LF)
  - `Presentation/Characters/InterpolatedVisual.cs` (CRLF)
  - `UI/Debug/NetDevOverlay.cs` (LF)
  - `Tests/PlayMode/PlaybackTransport.cs` (LF)
- Test:
  - Create `Assets/_BattleBomb/Tests/EditMode/Net/PredictionLogTests.cs`
  - Create `Tests/PlayMode/AnsweringHost.cs` and `Tests/PlayMode/LocalPredictionSmokeTests.cs`

- [ ] **Step 1: Write the failing log tests**

Create `Assets/_BattleBomb/Tests/EditMode/Net/PredictionLogTests.cs`:

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Core.Stats;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class PredictionLogTests
    {
        private static PlayerCommand Command(int frame) =>
            PlayerCommand.FromState(frame, Vector2.right, CommandButtons.None, CommandButtons.None);

        private static PredictedBody Body(float x) => new PredictedBody(
            MotorState.AtRest(new Vector3(x, 0f, 0f)), CombatState.Ready, ManaPool.FromValues(50f, 50f), true,
            new Vector3(0.1f, 0f, 0f), 3, true);

        private static PredictionLog Filled(int from, int to)
        {
            var log = new PredictionLog();
            for (int frame = from; frame <= to; frame++)
            {
                log.Record(Command(frame), frame % 2 == 0, Body(frame));
            }

            return log;
        }

        [Test]
        public void Each_step_is_kept_with_its_command_and_what_the_prediction_made_of_it()
        {
            PredictionLog log = Filled(10, 14);

            Assert.That(log.Count, Is.EqualTo(5));
            Assert.That(log.OldestFrame, Is.EqualTo(10));
            Assert.That(log.NewestFrame, Is.EqualTo(14));
            Assert.That(log.TryGet(12, out PredictionEntry entry), Is.True);
            Assert.That(entry.Frame, Is.EqualTo(12));
            Assert.That(entry.LightContextual, Is.True);
            Assert.That(entry.Body.Motor.Position.x, Is.EqualTo(12f));
            Assert.That(entry.Body.LungeStepsLeft, Is.EqualTo(3), "The lunge travels with the body — no snapshot carries it.");
            Assert.That(entry.Body.AttackRooted, Is.True);
            Assert.That(log.TryGet(15, out _), Is.False);
        }

        [Test]
        public void What_the_host_has_played_is_forgotten()
        {
            PredictionLog log = Filled(10, 14);

            log.DiscardThrough(12);
            Assert.That(log.OldestFrame, Is.EqualTo(13));
            Assert.That(log.Count, Is.EqualTo(2));

            log.DiscardThrough(20);
            Assert.That(log.Count, Is.EqualTo(0));
            Assert.That(log.OldestFrame, Is.EqualTo(-1));
        }

        [Test]
        public void The_replay_takes_the_unanswered_steps_oldest_first_and_leaves_the_log_empty()
        {
            PredictionLog log = Filled(10, 14);
            log.DiscardThrough(11);

            var replay = new List<PredictionEntry>();
            log.TakeAll(replay);

            Assert.That(replay.ConvertAll(e => e.Frame), Is.EqualTo(new[] { 12, 13, 14 }));
            Assert.That(log.Count, Is.EqualTo(0));
        }

        [Test]
        public void A_full_log_forgets_its_oldest_step()
        {
            PredictionLog log = Filled(0, PredictionLog.Capacity + 9);

            Assert.That(log.Count, Is.EqualTo(PredictionLog.Capacity));
            Assert.That(log.OldestFrame, Is.EqualTo(10));
            Assert.That(log.NewestFrame, Is.EqualTo(PredictionLog.Capacity + 9));
            Assert.That(log.TryGet(PredictionLog.Capacity + 5, out PredictionEntry late), Is.True);
            Assert.That(late.Body.Motor.Position.x, Is.EqualTo(PredictionLog.Capacity + 5f));
        }

        [Test]
        public void A_frame_that_goes_back_starts_a_new_log()
        {
            PredictionLog log = Filled(500, 505);

            log.Record(Command(3), false, Body(3f));

            Assert.That(log.Count, Is.EqualTo(1), "A new scene's clock starts again; the old run's steps mean nothing to it.");
            Assert.That(log.OldestFrame, Is.EqualTo(3));
        }
    }
}
```

- [ ] **Step 2: Run them and see them fail**

`recompile`. Expected: compile errors, because `PredictedBody`, `PredictionEntry` and `PredictionLog` do not exist.

- [ ] **Step 3: The log**

Create `Assets/_BattleBomb/Core/Net/PredictionLog.cs`:

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Players;
using BattleBomb.Core.Stats;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// The guest's own body after one predicted step (D62, HANDOFF-M8 planning decision 19): what a snapshot carries back
    /// for this player that a step changes, plus the swing's lunge, which no snapshot carries — a replay continues a lunge
    /// from this machine's own record of it.
    /// </summary>
    public readonly struct PredictedBody
    {
        public readonly MotorState Motor;
        public readonly CombatState Combat;
        public readonly ManaPool Mana;
        public readonly bool LeapAvailable;
        public readonly Vector3 LungePerStep;
        public readonly int LungeStepsLeft;
        public readonly bool AttackRooted;

        public PredictedBody(
            in MotorState motor, in CombatState combat, in ManaPool mana, bool leapAvailable,
            Vector3 lungePerStep, int lungeStepsLeft, bool attackRooted)
        {
            Motor = motor;
            Combat = combat;
            Mana = mana;
            LeapAvailable = leapAvailable;
            LungePerStep = lungePerStep;
            LungeStepsLeft = lungeStepsLeft;
            AttackRooted = attackRooted;
        }
    }

    /// <summary>One predicted step: the command as sent, whether its Light was the host's to spend, and the body after.</summary>
    public readonly struct PredictionEntry
    {
        public readonly PlayerCommand Command;
        public readonly bool LightContextual;
        public readonly PredictedBody Body;

        public PredictionEntry(in PlayerCommand command, bool lightContextual, in PredictedBody body)
        {
            Command = command;
            LightContextual = lightContextual;
            Body = body;
        }

        public int Frame => Command.Frame;
    }

    /// <summary>
    /// The guest's own steps the host has not answered for yet (HANDOFF-M8 planning decision 19). When a snapshot says the
    /// host has played command <c>n</c>, everything up to <c>n</c> is history, and <c>n+1</c> … now is replayed from the
    /// host's state. Frames only grow; one that does not starts a new log (a new scene's clock). A full log forgets its
    /// oldest step — two seconds unanswered, and the prediction stepped aside long before.
    /// </summary>
    public sealed class PredictionLog
    {
        public const int Capacity = 128;

        private readonly PredictionEntry[] _entries = new PredictionEntry[Capacity];
        private int _start;
        private int _count;

        public int Count => _count;

        public int OldestFrame => _count > 0 ? At(0).Frame : -1;

        public int NewestFrame => _count > 0 ? At(_count - 1).Frame : -1;

        public void Record(in PlayerCommand command, bool lightContextual, in PredictedBody body)
        {
            if (_count > 0 && command.Frame <= NewestFrame)
            {
                Clear();
            }

            if (_count == Capacity)
            {
                _start = (_start + 1) % Capacity;
                _count--;
            }

            _entries[(_start + _count) % Capacity] = new PredictionEntry(command, lightContextual, body);
            _count++;
        }

        public bool TryGet(int frame, out PredictionEntry entry)
        {
            for (int i = _count - 1; i >= 0; i--)
            {
                PredictionEntry candidate = At(i);
                if (candidate.Frame == frame)
                {
                    entry = candidate;
                    return true;
                }

                if (candidate.Frame < frame)
                {
                    break;
                }
            }

            entry = default;
            return false;
        }

        /// <summary>Forgets every step up to and including <paramref name="frame"/> — the host has played them.</summary>
        public void DiscardThrough(int frame)
        {
            while (_count > 0 && At(0).Frame <= frame)
            {
                _start = (_start + 1) % Capacity;
                _count--;
            }
        }

        /// <summary>Hands over every step still in the log, oldest first, and empties it — the replay records them again.</summary>
        public void TakeAll(List<PredictionEntry> into)
        {
            into.Clear();
            for (int i = 0; i < _count; i++)
            {
                into.Add(At(i));
            }

            Clear();
        }

        public void Clear()
        {
            _start = 0;
            _count = 0;
        }

        private PredictionEntry At(int index) => _entries[(_start + index) % Capacity];
    }
}
```

- [ ] **Step 4: Run them and see them pass**

`recompile`, then `run_tests` mode `EditMode`, filter `PredictionLogTests`. Expected: 5/5. Also run filter `ArchitectureFitnessTests`: Core is still pure.

- [ ] **Step 5: The numbers**

In `Assets/_BattleBomb/Core/Net/NetProtocol.cs`, after the line `public const float ReplicaTeleportDistance = 3f;` add:

```csharp

        /// <summary>How far the guest's own hero may be predicted past the newest command the host has answered for (Task 106).
        /// At Michael's "bad" lag an answer is about twenty steps behind, so 45 (0.75 s) is margin, not a limit anyone plays
        /// at. Past it the host has stopped answering — a hold, a stall — and the hero is drawn from the picture until it
        /// answers again, rather than wandering off only to be pulled back.</summary>
        public const int PredictionMaxAheadSteps = 45;

        /// <summary>How much of a correction to the guest's own hero is still drawn one step later (Task 106). 0.8 leaves a
        /// tenth after ten steps: the host's word is eased in over about a sixth of a second instead of snapped to.</summary>
        public const float CorrectionDecayPerStep = 0.8f;
```

- [ ] **Step 6: The actor predicts**

In `Assets/_BattleBomb/Gameplay/Characters/CharacterActor.cs` (CRLF; keep it):

(a) After the line `public Vector3 PreviousPosition => _previous.Position;` add:

```csharp

        /// <summary>
        /// Where the drawn body still is relative to the simulated one (Task 106). When the host's answer moves the guest's
        /// own predicted hero, the difference is drawn away over a few steps rather than snapped. Zero everywhere else — the
        /// host, the couch, and every body the guest draws from the host's picture.
        /// </summary>
        public Vector3 CorrectionOffset { get; private set; }
```

(b) In `ApplyReplica`, replace

```csharp
            _leapAvailable = snapshot.LeapAvailable;
            Statuses.Restore(snapshot.Statuses);
            transform.position = _state.Position;
        }
```

with

```csharp
            _leapAvailable = snapshot.LeapAvailable;
            Statuses.Restore(snapshot.Statuses);

            // A body drawn from the picture carries no lunge of this machine's making (Task 106: the guest's own hero moves
            // between the picture and its prediction, and a stale lunge must not survive the crossing).
            _lungePerStep = Vector3.zero;
            _lungeStepsLeft = 0;
            _attackRooted = false;
            transform.position = _state.Position;
        }

        /// <summary>
        /// The guest's own step (D62, HANDOFF-M8 planning decision 19): what <see cref="Step"/> does to this body, with nothing
        /// that belongs to the host — no revive channel, loot grab, chest, quick-use or hit window, and nothing spent from the
        /// bag. A Light the host will spend on one of those (<paramref name="lightContextual"/>) starts no swing here, and a
        /// revive the host says is running still holds the attack buttons. The host's answer arrives in a snapshot and
        /// <see cref="RestoreFromHost"/> takes it.
        /// </summary>
        internal void PredictStep(int frame, in PlayerCommand command, in ArenaBounds bounds, float dt, bool lightContextual)
        {
            _previous = _state;

            PlayerCommand effective = _condition.InControl ? command : PlayerCommand.Idle(frame);
            if (_revive.IsActive)
            {
                effective = WithoutAttacks(effective);
            }
            else if (lightContextual && _combat.Phase == AttackPhase.Ready)
            {
                effective = WithoutLight(effective);
            }

            bool frozen = _combat.HitstopSteps > 0;
            CombatStepResult combat = PhaseCombatMachine(effective);
            if (frozen)
            {
                return;
            }

            _condition = _condition.Step();
            _mana = _mana.Step(_sheet.ManaRegen, dt);
            PhaseCastAndAttack(effective, combat);
            PhaseMovement(effective, ClampFor(bounds), dt);
            transform.position = _state.Position;
        }

        /// <summary>This player's body as the guest's prediction logs it (Task 106).</summary>
        internal PredictedBody CaptureBody() => new PredictedBody(
            _state, _combat, _mana, _leapAvailable, _lungePerStep, _lungeStepsLeft, _attackRooted);

        /// <summary>
        /// Task 106: the host's word on this player at the command it last played, with this machine's own record of the
        /// swing's lunge at that step — no snapshot carries it. Everything after is replayed by <see cref="PredictStep"/>.
        /// </summary>
        internal void RestoreFromHost(in PlayerSnapshot host, in PredictedBody lunge)
        {
            _state = host.Motor;
            _previous = _state;
            _combat = host.Combat;
            _condition = host.Condition;
            _revive = host.Revive;
            _mana = host.Mana;
            _leapAvailable = host.LeapAvailable;
            Statuses.Restore(host.Statuses);
            _lungePerStep = lunge.LungePerStep;
            _lungeStepsLeft = lunge.LungeStepsLeft;
            _attackRooted = lunge.AttackRooted;
            transform.position = _state.Position;
        }

        /// <summary>
        /// Task 106: the host moved this predicted body by <c>-delta</c>, so the drawing keeps showing it where it was and eases
        /// over. A jump past the teleport distance is a teleport — a respawn, an airlock — and is drawn as one.
        /// </summary>
        internal void AddCorrection(Vector3 delta)
        {
            Vector3 offset = CorrectionOffset + delta;
            float limit = NetProtocol.ReplicaTeleportDistance;
            CorrectionOffset = offset.sqrMagnitude > limit * limit ? Vector3.zero : offset;
        }

        internal void DecayCorrection()
        {
            Vector3 offset = CorrectionOffset * NetProtocol.CorrectionDecayPerStep;
            CorrectionOffset = offset.sqrMagnitude < 1e-6f ? Vector3.zero : offset;
        }

        internal void ClearCorrection() => CorrectionOffset = Vector3.zero;
```

`CharacterActor.cs` already has `using BattleBomb.Core.Net;`, `using BattleBomb.Core.Combat;` and `using BattleBomb.Core.Spatial;`.

**Why each phase is or isn't here:**
- **The combat machine always runs.** During hitstop it only counts the freeze down, exactly as in `Step`.
- **Vitals are the condition and the mana.** `PhaseVitals` also steps the bag's quick-slot cooldown. The guest's bag is the host's mirror (Plan 2), so it is never stepped here.
- **Cast and attack.** A cast spends its mana and a leap its lift, as on the host. A swing's lunge target is picked from the enemies this machine draws: a little behind the host's, and the correction covers the difference.
- **Movement** is untouched `Step` code.

- [ ] **Step 7: The driver says when a Light is not a swing**

In `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs` (CRLF; keep it), replace

```csharp
            int index = HitResolver.LungeTarget(attacker.Position, attacker.Facing, attack, _candidatePositions);
            target = index >= 0 ? _candidatePositions[index] : default;
            return index >= 0;
        }
```

with

```csharp
            int index = HitResolver.LungeTarget(attacker.Position, attacker.Facing, attack, _candidatePositions);
            target = index >= 0 ? _candidatePositions[index] : default;
            return index >= 0;
        }

        /// <summary>
        /// Task 106: whether this player's Light would be spent on something other than a swing by the host — a downed partner
        /// to revive (D25), a drop to grab (D30), a chest or shop to open (D42) — judged from what this machine draws. The
        /// guest's prediction leaves such a press to the host, rather than starting a swing the host never will.
        /// </summary>
        internal bool LightIsContextual(CharacterActor actor)
        {
            IReadOnlyList<CharacterActor> actors = Characters.Ordered;
            int self = -1;
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i] == actor)
                {
                    self = i;
                    break;
                }
            }

            if (self < 0)
            {
                return false;
            }

            return FindReviveTarget(actors, self) >= 0
                || FindGrabTarget(actor) >= 0
                || (!_openScreens.ContainsKey(actor.PlayerId.Value) && FindInteractable(actor) >= 0);
        }
```

On the guest `RunStep` never runs, so the scratch lists `FindReviveTarget` fills are free to use here.

- [ ] **Step 8: The prediction**

Create `Assets/_BattleBomb/Gameplay/Net/LocalPrediction.cs`:

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Core.Spatial;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// The guest's own hero, predicted (D62, HANDOFF-M8 planning decision 19). Every local step it runs
    /// <see cref="CharacterActor.PredictStep"/> with the command this machine has just sent, so the hero answers the guest's
    /// hands at once instead of a round trip later. When a snapshot says which command the host last played, the next step
    /// puts the hero where the host says it was then and replays every command since from the log; whatever moved is drawn
    /// away over a few steps.
    /// <para>
    /// It steps aside — the hero is drawn from the host's picture, as before Task 106 — while the hero waits to appear (a
    /// drop-in), before the host has answered any command of this machine's, while switched off (the development panel, and
    /// every test whose host is a recording), and when the newest answer is more than
    /// <see cref="NetProtocol.PredictionMaxAheadSteps"/> steps old.
    /// </para>
    /// </summary>
    internal sealed class LocalPrediction
    {
        private readonly SimulationDriver _driver;
        private readonly int _playerId;
        private readonly PredictionLog _log = new PredictionLog();
        private readonly List<PredictionEntry> _replay = new List<PredictionEntry>(PredictionLog.Capacity);
        private PlayerSnapshot _answer;
        private ArenaBounds _bounds = ArenaBounds.Default;
        private int _heardFrame = -1;
        private int _ack = -1;
        private bool _unreconciled;

        internal LocalPrediction(SimulationDriver driver, int playerId)
        {
            _driver = driver;
            _playerId = playerId;
        }

        /// <summary>The hero is predicted this step: the picture leaves its body alone.</summary>
        internal bool IsActive { get; private set; }

        /// <summary>How far the last answer moved the hero.</summary>
        internal float LastCorrection { get; private set; }

        internal bool TryGetLogged(int frame, out PredictedBody body)
        {
            bool found = _log.TryGet(frame, out PredictionEntry entry);
            body = entry.Body;
            return found;
        }

        /// <summary>A snapshot arrived. Only the newest answer matters; the next step takes it.</summary>
        internal void Heard(WorldSnapshot snapshot)
        {
            if (snapshot.HostFrame <= _heardFrame || !snapshot.TryGetPlayer(_playerId, out PlayerSnapshot own))
            {
                return;
            }

            _heardFrame = snapshot.HostFrame;
            _ack = snapshot.AckGuestFrame;
            _answer = own;
            _bounds = snapshot.Bounds;
            _unreconciled = true;
        }

        /// <summary>One local step, with the command this machine has just sent.</summary>
        internal void Step(in PlayerCommand command, CharacterActor actor, bool enabled)
        {
            if (actor == null)
            {
                IsActive = false;
                return;
            }

            int frame = command.Frame;
            bool active = enabled && actor.isActiveAndEnabled && _ack >= 0 && _ack < frame
                && frame - _ack <= NetProtocol.PredictionMaxAheadSteps;
            if (!active)
            {
                if (IsActive)
                {
                    actor.ClearCorrection();
                }

                IsActive = false;

                // The commands are still kept, so the prediction can pick up from the next answer. The body is the
                // picture's — all this machine knows while it is not predicting.
                _log.Record(command, false, actor.CaptureBody());
                return;
            }

            actor.DecayCorrection();
            if (_unreconciled || !IsActive)
            {
                Reconcile(actor, frame);
            }

            IsActive = true;
            bool lightContextual = _driver.LightIsContextual(actor);
            actor.PredictStep(frame, command, _bounds, _driver.StepDuration, lightContextual);
            _log.Record(command, lightContextual, actor.CaptureBody());
        }

        /// <summary>
        /// The host's word at the command it last played, then every command since replayed on top. The drawing stays where
        /// it was and eases over (<see cref="CharacterActor.CorrectionOffset"/>).
        /// </summary>
        private void Reconcile(CharacterActor actor, int frame)
        {
            _unreconciled = false;
            Vector3 before = actor.Position;
            PredictedBody atAck = _log.TryGet(_ack, out PredictionEntry acked) ? acked.Body : default;
            actor.RestoreFromHost(_answer, atAck);

            _log.DiscardThrough(_ack);
            _log.TakeAll(_replay);
            for (int i = 0; i < _replay.Count; i++)
            {
                PredictionEntry step = _replay[i];
                if (step.Frame >= frame)
                {
                    break;
                }

                actor.PredictStep(step.Frame, step.Command, _bounds, _driver.StepDuration, step.LightContextual);
                _log.Record(step.Command, step.LightContextual, actor.CaptureBody());
            }

            Vector3 moved = before - actor.Position;
            LastCorrection = moved.magnitude;
            actor.AddCorrection(moved);
        }
    }
}
```

**Why the Light decision is replayed as logged, not asked again.** It depended on the world at that step: a drop underfoot then may have been grabbed since. The host judged the press against its own world at that step, and the log's record is this machine's best guess at the same moment.

- [ ] **Step 9: The guest runs it, and the picture leaves the hero alone**

In `Assets/_BattleBomb/Gameplay/Net/ReplicaWorld.cs`:

(a) After Task 103's line `internal bool IsHidingLocal => _hidden != null;` add:

```csharp

        /// <summary>The player this machine is predicting (Task 106), or -1: the picture leaves that body to the prediction.</summary>
        internal int Predicted { get; set; } = -1;
```

(b) In `ApplyPlayers`, replace

```csharp
                if (actor == null)
                {
                    continue;
                }

                MotorState motor = player.Motor;
```

with

```csharp
                if (actor == null)
                {
                    continue;
                }

                if (player.PlayerId == Predicted)
                {
                    // This machine's own hero is predicted (D62): its body is the prediction's, answered by the newest
                    // snapshot rather than the picture's. The grab count and the refusal flash still come from here.
                    _driver.ApplyReplicaPlayerSide(player.PlayerId, player.GrabCount, player.RefusedSteps);
                    continue;
                }

                MotorState motor = player.Motor;
```

In `Assets/_BattleBomb/Gameplay/Net/NetGuest.cs`:

(a) After the field `private readonly MenuGate _menu = new MenuGate();` add:

```csharp
        private LocalPrediction _prediction;
```

(b) In `Begin`, after Task 103's line `_world.HideUntilSeen(hidden, local.Value);` add:

```csharp
            _prediction = new LocalPrediction(_driver, local.Value);
```

(c) After Task 103's property `public bool WaitingToAppear => _world != null && _world.IsHidingLocal;` add:

```csharp

        /// <summary>This machine's own hero is being predicted (D62, Task 106) — for the smoke suite and the development panel.</summary>
        public bool IsPredicting => _prediction != null && _prediction.IsActive;

        /// <summary>How far the host's last answer moved this machine's own hero — for the smoke suite.</summary>
        public float LastCorrection => _prediction != null ? _prediction.LastCorrection : 0f;

        /// <summary>The smoke suite's "perfect host": the body this machine predicted for its own hero after the command it sent
        /// at <paramref name="guestFrame"/>, while that step is still unanswered.</summary>
        public bool TryGetPredicted(int guestFrame, out PredictedBody body)
        {
            if (_prediction != null && _prediction.TryGetLogged(guestFrame, out body))
            {
                return true;
            }

            body = default;
            return false;
        }
```

(d) In `OnLocalStep`, replace

```csharp
            _writer.Reset();
            CommandCodec.Write(_writer, Mathf.Max(0, _buffer.NewestFrame), _recent);
            _net.Send(NetChannel.Unreliable, _writer);
```

with

```csharp
            _writer.Reset();
            CommandCodec.Write(_writer, Mathf.Max(0, _buffer.NewestFrame), _recent);
            _net.Send(NetChannel.Unreliable, _writer);

            // The guest's own hero answers the guest's hands now, not a round trip later (D62) — with exactly the command
            // just sent, so what the host plays is what was predicted.
            _prediction.Step(command, _world.PlayerById(_local.Value), _net.PredictOwnPlayer);
            _world.Predicted = _prediction.IsActive ? _local.Value : -1;
```

(e) In `OnMessage`, replace

```csharp
                case NetMessageKind.Snapshot:
                    WorldSnapshot snapshot = _buffer.Rent();
                    SnapshotCodec.Read(reader, snapshot);
                    _buffer.Add(snapshot);
                    break;
```

with

```csharp
                case NetMessageKind.Snapshot:
                    WorldSnapshot snapshot = _buffer.Rent();
                    SnapshotCodec.Read(reader, snapshot);
                    if (_buffer.Add(snapshot))
                    {
                        // Read now, before the buffer can recycle it: the prediction keeps only its own player's answer.
                        _prediction.Heard(snapshot);
                    }

                    break;
```

`NetGuest.cs` already has `using BattleBomb.Core.Net;` (for `PredictedBody`) and, from Task 103, `using BattleBomb.Gameplay.Characters;`.

In `Assets/_BattleBomb/Gameplay/Net/NetSession.cs`, after the line `public string LastRefusal { get; private set; }` add:

```csharp

        /// <summary>Guest: this machine's own hero is predicted (D62, Task 106). On unless switched off — the development
        /// panel's A/B for Michael's feel pass, and every test whose host is a recording that cannot answer.</summary>
        public bool PredictOwnPlayer { get; set; } = true;
```

- [ ] **Step 10: The correction is drawn**

In `Assets/_BattleBomb/Presentation/Characters/InterpolatedVisual.cs` (CRLF; keep it), replace

```csharp
            transform.position = Vector3.Lerp(_actor.PreviousPosition, _actor.Position, _driver.Alpha);
```

with

```csharp
            // Zero except on the guest's own predicted hero, where it eases the host's corrections in (Task 106).
            transform.position = Vector3.Lerp(_actor.PreviousPosition, _actor.Position, _driver.Alpha)
                + _actor.CorrectionOffset;
```

The shadow, the health bar and the camera keep reading `Position`. On a correction they sit a few centimetres from the drawn body for a sixth of a second, which nobody will see; M9's art pipeline can move them onto the drawn body if it matters.

- [ ] **Step 11: Recorded hosts cannot answer, so their guests do not predict**

In `Assets/_BattleBomb/Tests/PlayMode/PlaybackTransport.cs`:

(a) In `Connect`, after the line `_connected = true;` add:

```csharp

            // A recording cannot answer this guest's commands, so there is nothing to predict against (Task 106): the guest's
            // own hero is drawn from the recorded picture, which is what every recorded test compares.
            GameSession session = GameSession.Find();
            if (session != null && session.Net != null)
            {
                session.Net.PredictOwnPlayer = false;
            }
```

(b) Add `using BattleBomb.Gameplay.Session;` to its usings.

This covers every recorded-host test in one place: `GuestReplicaSmokeTests`, `GuestStageSmokeTests` and `ReplicaReplaySmokeTests`, plus Plan 2's `GuestMenuSmokeTests` and the recorded parts of `OnlineJoinSmokeTests`.
- Their snapshots carry either `AckGuestFrame = -1` or the acks of a *different* guest's recording.
- A predicting guest would reconcile against frames it never sent.

- [ ] **Step 12: The development panel's switch**

In `Assets/_BattleBomb/UI/Debug/NetDevOverlay.cs`:

(a) Replace Task 104's line

```csharp
            var area = new Rect(Screen.width - width - 8f, Screen.height - row * 7f - 8f, width, row * 7f);
```

with

```csharp
            var area = new Rect(Screen.width - width - 8f, Screen.height - row * 8f - 8f, width, row * 8f);
```

(b) Just before the line `GUILayout.EndArea();` add:

```csharp
            if (!_folded && net != null && role == NetRole.Guest
                && GUILayout.Button(net.PredictOwnPlayer ? "Prediction: on" : "Prediction: off", _style))
            {
                // Michael's feel pass (Task 108): the same connection, with and without the guest's prediction.
                net.PredictOwnPlayer = !net.PredictOwnPlayer;
            }

```

- [ ] **Step 13: A host that answers, and the prediction tests**

Create `Assets/_BattleBomb/Tests/PlayMode/AnsweringHost.cs`:

```csharp
using System;
using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Stats;
using BattleBomb.Platform.Net;
using UnityEngine;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// A host that answers (Task 106) — the guest-side harness prediction needs, where <see cref="PlaybackTransport"/>'s
    /// recording cannot. It welcomes whoever connects and launches the fixture chapter; then, at 60 steps a second of real
    /// time, it sends a snapshot every second step. Each snapshot acknowledges the newest guest command that arrived at least
    /// <see cref="AnswerDelaySteps"/> steps earlier (a round trip) and says where the guest's hero stands after it, through
    /// <see cref="Answer"/>, which the test supplies. The host's own hero stands still. Nothing else happens in its world.
    /// </summary>
    internal sealed class AnsweringHost : INetTransport
    {
        internal const int Start = 1000;
        internal const int LoadMargin = 120;

        /// <summary>About Michael's "bad" lag: 200 ms there and back.</summary>
        internal const int AnswerDelaySteps = 12;

        internal const int GuestPlayerId = 1;
        internal const float GuestStartX = 2f;

        private static readonly NetPeer Host = new NetPeer(1);

        private readonly Queue<NetEvent> _inbox = new Queue<NetEvent>();
        private readonly List<WireCommand> _commands = new List<WireCommand>();
        private readonly List<(int ArrivedAt, int GuestFrame)> _arrivals = new List<(int ArrivedAt, int GuestFrame)>();
        private readonly NetWriter _writer = new NetWriter();
        private double _start = -1.0;
        private int _next = Start - LoadMargin;
        private int _hostFrame = Start - LoadMargin;
        private bool _connected;
        private bool _launched;

        /// <summary>Where the guest's hero stands after the given guest command, as this host "played" it; null leaves the
        /// guest out of that snapshot.</summary>
        internal Func<int, PlayerSnapshot?> Answer { get; set; }

        /// <summary>The newest guest command this host has acknowledged, or -1.</summary>
        internal int Acknowledged { get; private set; } = -1;

        public void Listen()
        {
        }

        public void Connect(string address)
        {
            _connected = true;
            _inbox.Enqueue(NetEvent.Connected(Host));
            _writer.Reset();
            HandshakeCodec.WriteWelcome(_writer, new WelcomeMessage(GuestPlayerId));
            _inbox.Enqueue(NetEvent.Data(Host, NetChannel.Reliable, _writer.ToArray()));
        }

        public void Send(NetPeer peer, NetChannel channel, byte[] payload, int length)
        {
            if (length == 0 || (NetMessageKind)payload[0] != NetMessageKind.Commands)
            {
                return;
            }

            var reader = new NetReader(payload, length);
            reader.ReadByte();
            CommandCodec.Read(reader, _commands);
            for (int i = 0; i < _commands.Count; i++)
            {
                _arrivals.Add((_hostFrame, _commands[i].Frame));
            }
        }

        public void Update(double nowSeconds)
        {
            if (!_connected)
            {
                return;
            }

            if (_start < 0.0)
            {
                _start = nowSeconds;
            }

            int now = Start - LoadMargin + (int)((nowSeconds - _start) * 60.0);
            while (_next <= now)
            {
                _hostFrame = _next;
                if (!_launched)
                {
                    _launched = true;
                    _writer.Reset();
                    HandshakeCodec.WriteLaunch(_writer, new LaunchMessage("fixture", 0, 0, -1, new[] { 0, 0 }, GuestPlayerId));
                    _inbox.Enqueue(NetEvent.Data(Host, NetChannel.Reliable, _writer.ToArray()));
                }
                else if (_hostFrame >= Start && _hostFrame % NetProtocol.SnapshotEverySteps == 0)
                {
                    SendSnapshot();
                }

                _next++;
            }
        }

        public bool TryReceive(out NetEvent netEvent)
        {
            if (_inbox.Count > 0)
            {
                netEvent = _inbox.Dequeue();
                return true;
            }

            netEvent = default;
            return false;
        }

        public void Disconnect(NetPeer peer) => _connected = false;

        public void Dispose() => _connected = false;

        private void SendSnapshot()
        {
            for (int i = _arrivals.Count - 1; i >= 0; i--)
            {
                if (_arrivals[i].ArrivedAt <= _hostFrame - AnswerDelaySteps)
                {
                    Acknowledged = Math.Max(Acknowledged, _arrivals[i].GuestFrame);
                    _arrivals.RemoveAt(i);
                }
            }

            var world = new WorldSnapshot { HostFrame = _hostFrame, AckGuestFrame = Acknowledged };
            world.Players.Add(Standing(0, -2f));
            PlayerSnapshot? guest = Acknowledged >= 0 && Answer != null ? Answer(Acknowledged) : Standing(GuestPlayerId, GuestStartX);
            if (guest.HasValue)
            {
                world.Players.Add(guest.Value);
            }

            _writer.Reset();
            SnapshotCodec.Write(_writer, world);
            _inbox.Enqueue(NetEvent.Data(Host, NetChannel.Unreliable, _writer.ToArray()));
        }

        internal static PlayerSnapshot Standing(int playerId, float x) => new PlayerSnapshot(
            playerId, MotorState.AtRest(new Vector3(x, 0f, 0f)), CombatState.Ready, Unhurt, ReviveChannel.Inactive,
            ManaPool.FromValues(50f, 50f), true, null, -1, 0, 0);

        /// <summary>The snapshot a host that agrees with every prediction would send: the guest's own logged body.</summary>
        internal static PlayerSnapshot Agreeing(in PredictedBody body) => new PlayerSnapshot(
            GuestPlayerId, body.Motor, body.Combat, Unhurt, ReviveChannel.Inactive, body.Mana, body.LeapAvailable,
            null, -1, 0, 0);

        private static PlayerCondition Unhurt => new PlayerCondition(Health.FromValues(100f, 100f), 0, 0);
    }
}
```

Create `Assets/_BattleBomb/Tests/PlayMode/LocalPredictionSmokeTests.cs`:

```csharp
using System.Collections;
using System.Collections.Generic;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Platform;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// A real guest machine against a host that answers (D62, Task 106). The host "plays" the guest's commands a round trip
    /// late — 12 steps, about Michael's bad lag — and answers with whatever the test says. When it agrees with the guest,
    /// nothing is corrected; when it disagrees once, the host wins and the drawing eases over; and the guest's own hero moves
    /// on the step it is told to, long before the host could have answered.
    /// </summary>
    public sealed class LocalPredictionSmokeTests
    {
        private AnsweringHost _host;
        private NetGuest _guest;
        private SimulationDriver _driver;
        private CharacterActor _own;
        private ScriptedCommandSource _input;
        private CharacterDefinition[] _couch;

        [UnitySetUp]
        public IEnumerator JoinAHostThatAnswers()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "local-prediction";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;

            _couch = new CharacterDefinition[] { Object.Instantiate(session.Roster[0]), null };
            session.Characters[0] = _couch[0];
            session.Characters[1] = _couch[1];

            _host = new AnsweringHost();
            NetSession.FindOrCreate().Join(_host, "answering");
            for (int i = 0; i < 1500 && _guest == null; i++)
            {
                yield return null;
                _guest = Object.FindAnyObjectByType<NetGuest>();
            }

            Assert.That(_guest, Is.Not.Null, "The guest never loaded the host's run.");
            _driver = Object.FindAnyObjectByType<SimulationDriver>();

            for (int i = 0; i < 600 && _own == null; i++)
            {
                yield return null;
                foreach (CharacterActor actor in _driver.Characters.Ordered)
                {
                    if (actor.PlayerId.Value == AnsweringHost.GuestPlayerId)
                    {
                        _own = actor;
                    }
                }
            }

            Assert.That(_own, Is.Not.Null, "The guest's own hero never spawned.");
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            _driver.Players.Unregister(_own.PlayerId);
            _input = _own.gameObject.AddComponent<ScriptedCommandSource>();
            _input.Bind(_own.PlayerId.Value);
            _driver.Players.Register(_input);
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            GameSession session = GameSession.Find();
            if (session != null)
            {
                Object.Destroy(session.gameObject);
            }

            if (_couch != null && _couch[0] != null)
            {
                Object.Destroy(_couch[0]);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator The_guests_hero_moves_on_the_step_it_is_told_to_long_before_the_host_answers()
        {
            _host.Answer = Agreeing;
            yield return Until(() => _guest.IsPredicting, "The guest never started predicting its own hero.");

            int pressedAt = -1;
            int movedAt = -1;
            int ackWhenMoved = -1;
            float startX = _own.Position.x;
            void Watch(int frame)
            {
                if (pressedAt >= 0 && movedAt < 0 && _own.Position.x > startX + 1e-3f)
                {
                    movedAt = frame;
                    ackWhenMoved = _host.Acknowledged;
                }
            }

            _driver.Stepped += Watch;
            try
            {
                _input.Set(Vector2.right, CommandButtons.None);
                pressedAt = _driver.Frame;
                yield return Until(() => movedAt >= 0, "The guest's hero never moved.");
            }
            finally
            {
                _driver.Stepped -= Watch;
                _input.Release();
            }

            Assert.That(movedAt - pressedAt, Is.LessThanOrEqualTo(2),
                "The hero waited for something — prediction moves it on the step the command is sent.");
            Assert.That(ackWhenMoved, Is.LessThan(pressedAt),
                "The host had already answered for the press, so this proves nothing about prediction.");
        }

        [UnityTest]
        public IEnumerator A_host_that_agrees_corrects_nothing()
        {
            _host.Answer = Agreeing;
            yield return Until(() => _guest.IsPredicting, "The guest never started predicting its own hero.");

            float worst = 0f;
            void Watch(int frame) => worst = Mathf.Max(worst, _guest.LastCorrection, _own.CorrectionOffset.magnitude);
            _driver.Stepped += Watch;
            try
            {
                _input.Set(Vector2.right, CommandButtons.None);
                yield return Steps(60);
                _input.Set(Vector2.left, CommandButtons.Jump);
                yield return Steps(30);
                _input.Release();
                yield return Steps(40);
            }
            finally
            {
                _driver.Stepped -= Watch;
            }

            Assert.That(worst, Is.LessThan(0.01f),
                "Replaying the same commands from the same state moved the hero — the replay is not the prediction.");
        }

        [UnityTest]
        public IEnumerator A_host_that_disagrees_wins_and_the_drawing_eases_over()
        {
            // The host says the hero is a unit further right than this machine predicted — until the guest has taken that
            // correction. Every answer built before then is +1 against the same uncorrected log, so however many snapshots
            // one render frame sends, the guest takes exactly one unit.
            bool taken = false;
            int disagreeFrom = int.MaxValue;
            _host.Answer = frame =>
            {
                PlayerSnapshot? agreed = Agreeing(frame);
                taken |= _guest.LastCorrection > 0.5f;
                if (!agreed.HasValue || taken || frame < disagreeFrom)
                {
                    return agreed;
                }

                MotorState m = agreed.Value.Motor;
                return agreed.Value.WithMotor(new MotorState(
                    m.Position + new Vector3(1f, 0f, 0f), m.Velocity, m.Facing, m.IsGrounded, m.StepsSinceGrounded, m.JumpBufferedFor));
            };

            yield return Until(() => _guest.IsPredicting, "The guest never started predicting its own hero.");

            var drawn = new List<Vector3>();
            var offsets = new List<float>();
            float biggest = 0f;
            void Watch(int frame)
            {
                drawn.Add(_own.Position + _own.CorrectionOffset);
                offsets.Add(_own.CorrectionOffset.magnitude);
                biggest = Mathf.Max(biggest, _guest.LastCorrection);
            }

            _driver.Stepped += Watch;
            try
            {
                _input.Set(Vector2.right, CommandButtons.None);
                disagreeFrom = _driver.Frame + 10;
                yield return Until(() => biggest > 0.5f, "The host's disagreement never reached the hero.");
                yield return Steps(40);
            }
            finally
            {
                _driver.Stepped -= Watch;
                _input.Release();
            }

            Assert.That(biggest, Is.EqualTo(1f).Within(0.05f), "The host's word did not move the hero by what it said.");
            int peak = offsets.IndexOf(Mathf.Max(offsets.ToArray()));
            Assert.That(offsets[peak], Is.EqualTo(1f).Within(0.05f), "The correction was snapped, not drawn away.");
            Assert.That(offsets[offsets.Count - 1], Is.LessThan(0.05f), "The correction never finished easing in.");
            for (int i = 1; i < drawn.Count; i++)
            {
                Assert.That((drawn[i] - drawn[i - 1]).magnitude, Is.LessThan(0.5f),
                    $"The drawn hero jumped at step {i} — a correction must ease, never snap.");
            }
        }

        [UnityTest]
        public IEnumerator Switched_off_the_guests_hero_is_drawn_from_the_host_as_before()
        {
            // This host never moves the hero, whatever the guest presses.
            GameSession.Find().Net.PredictOwnPlayer = false;
            _host.Answer = frame => AnsweringHost.Standing(AnsweringHost.GuestPlayerId, AnsweringHost.GuestStartX);
            yield return Until(() => _guest.RenderFrame >= AnsweringHost.Start + 60, "The guest never started drawing.");

            bool predicted = false;
            void Watch(int frame) => predicted |= _guest.IsPredicting;
            _driver.Stepped += Watch;
            try
            {
                _input.Set(Vector2.right, CommandButtons.None);
                yield return Steps(60);
            }
            finally
            {
                _driver.Stepped -= Watch;
                _input.Release();
            }

            Assert.That(predicted, Is.False, "Prediction ran while switched off.");
            Assert.That(_own.Position.x, Is.EqualTo(AnsweringHost.GuestStartX).Within(0.01f),
                "Without prediction the hero is wherever the host's picture puts it — and this host never moved it.");
        }

        private PlayerSnapshot? Agreeing(int guestFrame) =>
            _guest.TryGetPredicted(guestFrame, out PredictedBody body) ? AnsweringHost.Agreeing(body) : (PlayerSnapshot?)null;

        private IEnumerator Steps(int count)
        {
            int target = _driver.Frame + count;
            for (int guard = 0; guard < count * 20 && _driver.Frame < target; guard++)
            {
                yield return null;
            }
        }

        private static IEnumerator Until(System.Func<bool> done, string failure)
        {
            for (int guard = 0; guard < 1800 && !done(); guard++)
            {
                yield return null;
            }

            Assert.That(done(), Is.True, failure);
        }
    }
}
```

**How the "perfect host" works.**
- The host acknowledges guest command *n* twelve steps after it arrived.
- It answers with the body the guest itself logged for *n* (`NetGuest.TryGetPredicted`). The guest has not discarded *n* yet, because it discards only through the previous answer.
- A correct replay reproduces the prediction exactly, so any correction is a bug in replay, not in the test.

**What the four tests prove:**
- "Moves on the step it is told to" shows the hero moved before the host could have played the press.
- "Disagrees once" moves the hero by one unit. The drawing holds back by that unit and eases over, never jumping.
- "Switched off" is the A/B switch, and the pre-106 behaviour.

- [ ] **Step 14: Run the prediction tests red, then green**

`recompile`, then `run_tests` mode `PlayMode` (async), filter `LocalPredictionSmokeTests`.

To see the red that matters, temporarily make `LocalPrediction.Step` return right after its `if (!active)` block, so it never predicts. Expected:
- `...moves_on_the_step...` fails ("The guest never started predicting").
- The other three fail on the same line or pass for the wrong reason.

Restore it. Expected: 4/4.

Then mutate once more. Remove `if (step.Frame >= frame) { break; }`, and separately make the replay use `lightContextual: false`. Neither should turn `A_host_that_agrees_corrects_nothing` red while the test walks and jumps with no drop or chest in reach. Say so in the `DONE`: that guard is belt-and-braces, not load-bearing.

Last, mutate `actor.AddCorrection(moved);` to nothing. `A_host_that_disagrees...` must go red on "The drawn hero jumped". Restore it.

- [ ] **Step 15: The gates**

- `run_tests` mode `EditMode`: baseline + 7 (F4's 2, 106's 5), all passing.
- `run_tests` mode `PlayMode` (async): baseline + 4, all passing.

Every recorded-host suite (`GuestReplicaSmokeTests`, `GuestStageSmokeTests`, `ReplicaReplaySmokeTests`, `GuestMenuSmokeTests`, `OnlineJoinSmokeTests`) must be unchanged. If one fails on the guest's own hero's position, its host is not a `PlaybackTransport`. Switch prediction off in that test's setup, the way Step 11 does, and say so in the `DONE`.

- [ ] **Step 16: A settled check you can make yourself (no QUIET)**

- `editor_play` the Frontend scene with the development panel. Host local is impossible in one editor, so this only checks that the panel draws.
- Take `capture_game_view` of the title and confirm the Net panel still fits: its area grew by one row.
- `editor_stop`. The toggle itself is Michael's to use in Task 108.

- [ ] **Step 17: Commit (`DONE`)**

Paths:
- `Assets/_BattleBomb/Core/Net/PredictionLog.cs` (+ `.meta`)
- `Assets/_BattleBomb/Core/Net/NetProtocol.cs`
- `Assets/_BattleBomb/Gameplay/Characters/CharacterActor.cs`
- `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs`
- `Assets/_BattleBomb/Gameplay/Net/LocalPrediction.cs` (+ `.meta`)
- `Assets/_BattleBomb/Gameplay/Net/NetGuest.cs`, `Assets/_BattleBomb/Gameplay/Net/ReplicaWorld.cs`, `Assets/_BattleBomb/Gameplay/Net/NetSession.cs`
- `Assets/_BattleBomb/Presentation/Characters/InterpolatedVisual.cs`
- `Assets/_BattleBomb/UI/Debug/NetDevOverlay.cs`
- `Assets/_BattleBomb/Tests/EditMode/Net/PredictionLogTests.cs` (+ `.meta`)
- `Assets/_BattleBomb/Tests/PlayMode/AnsweringHost.cs` (+ `.meta`), `Assets/_BattleBomb/Tests/PlayMode/LocalPredictionSmokeTests.cs` (+ `.meta`), `Assets/_BattleBomb/Tests/PlayMode/PlaybackTransport.cs`

Subject: `106: the guest predicts its own hero`. Body: Michael's lag table said the guest's own moves and swings arrive too late at bad lag, so the guest now runs its own hero's motor and combat machine at once with the command it sends, logs each step, and on every snapshot takes the host's state at the last played command and replays the rest; any difference is drawn away over a sixth of a second. Hits, grabs, revives and chests stay the host's, and prediction steps aside whenever the host is not answering.

---

### Task 107: The picture through pauses, gaps and lost snapshots

**Why.** Three things Plan 1's build found in the guest's picture, deferred to here (Task 93's notes; the board's Plan 3 list). One of them, prediction's corrections, is already Task 106's; this task does the rest.

1. **A host pause spends the guest's cushion.** The guest draws six steps (100 ms) behind the newest snapshot, which is what absorbs jitter. When snapshots stop, the render clock crawls forward to the newest and waits there. Then, when snapshots resume, it is right at the edge with no cushion and rebuilds the six steps at a quarter step per step. That leaves about 0.4 s of stutter after every stop. A stop happens during the airlock hold while a guest loads, after a lag spike, and in the editor's pause.
   - **Fix:** once the clock has reached the newest snapshot, it waits there until the newest is a full delay ahead again. Only then does it move on. The cost is one more tenth of a second of stillness on top of a stop that was already still; the gain is no stutters after it.
2. **A lost snapshot can turn a slide into a teleport.** The guest draws a body sliding between two snapshots unless it moved more than 3 units between them. That rule was written for snapshots 2 steps apart. With snapshots lost, two neighbours can be 10 or 20 steps apart, and a fast body — a knocked-back enemy — legitimately covers 3 units in that time. It is then drawn as a snap.
   - **Fix:** the limit is a speed, not a distance: 3 units per 2 steps, scaled by the real gap.
3. **Let-go and drain stay as they are.**
   - `StarvedRepeatSteps` (15): a silent guest's last input is repeated for a quarter of a second, then let go.
   - `InputBufferDrainSteps` (30): the host's buffer merges one step after half a second above its target.

   Michael's lag table says nothing against either, and Task 96's pass found no problem with them. Retuning without evidence would be guessing. Task 108's sheet has a check for each, including "a Heavy charged through a stall fires on let-go, by design".

**Written down, not built: the Jump in a skipped frame** (the board's Task 107 item).
- **What happens.** When the host's buffer merges two guest commands into one step, a press in the first is played one step later than the guest's log has it. The next snapshot corrects the difference, which is Task 106's easing.
- **The fix, if needed.** Have snapshots carry the host's held baseline, so the guest's replay could merge the same way.
- **Why not now.** A one-step difference is inside what the easing hides, so it is not built unless Michael's feel pass (108) shows it. If it does, it comes back as `108a`.

**Files:**
- Create: `Assets/_BattleBomb/Core/Net/ReplicaMotion.cs`
- Modify: `Assets/_BattleBomb/Core/Net/RenderClock.cs`, `Gameplay/Net/ReplicaWorld.cs` (both LF)
- Test:
  - Modify `Assets/_BattleBomb/Tests/EditMode/Net/RenderClockTests.cs`
  - Create `Tests/EditMode/Net/ReplicaMotionTests.cs`

- [ ] **Step 1: Write the failing tests**

In `Assets/_BattleBomb/Tests/EditMode/Net/RenderClockTests.cs`, after the method `A_long_stall_snaps_rather_than_crawling_to_catch_up` (its closing brace), add:

```csharp

        [Test]
        public void After_a_stop_it_waits_for_a_full_cushion_before_moving_on()
        {
            var clock = new RenderClock(delaySteps: 6);
            clock.Advance(100);

            // The host stops (a hold, a spike): the picture spends its cushion and waits at the newest snapshot.
            for (int i = 0; i < 20; i++)
            {
                clock.Advance(100);
            }

            Assert.That(clock.Frame, Is.EqualTo(100f));

            // Snapshots resume, one step per step. The picture stays put until the newest is a full delay ahead...
            for (int newest = 101; newest <= 105; newest++)
            {
                Assert.That(clock.Advance(newest), Is.EqualTo(100f), $"It crept on at newest {newest} with no cushion.");
            }

            // ...then moves on with the whole cushion behind it.
            for (int newest = 106; newest <= 130; newest++)
            {
                clock.Advance(newest);
            }

            Assert.That(130 - clock.Frame, Is.EqualTo(6f).Within(0.5f), "The cushion was not rebuilt.");
        }
```

Create `Assets/_BattleBomb/Tests/EditMode/Net/ReplicaMotionTests.cs`:

```csharp
using BattleBomb.Core.Net;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class ReplicaMotionTests
    {
        [Test]
        public void Between_neighbouring_snapshots_three_units_is_the_line()
        {
            Assert.That(ReplicaMotion.IsTeleport(Vector3.zero, new Vector3(2.9f, 0f, 0f), NetProtocol.SnapshotEverySteps), Is.False);
            Assert.That(ReplicaMotion.IsTeleport(Vector3.zero, new Vector3(3.1f, 0f, 0f), NetProtocol.SnapshotEverySteps), Is.True);
        }

        [Test]
        public void Across_lost_snapshots_the_line_moves_with_the_gap()
        {
            // Four snapshots lost: ten steps apart. Ten units in ten steps is a fast slide, not a teleport; sixteen is one.
            Assert.That(ReplicaMotion.IsTeleport(Vector3.zero, new Vector3(10f, 0f, 0f), 10), Is.False,
                "A fast body across lost snapshots was drawn as a teleport.");
            Assert.That(ReplicaMotion.IsTeleport(Vector3.zero, new Vector3(16f, 0f, 0f), 10), Is.True);
        }

        [Test]
        public void A_gap_shorter_than_the_snapshot_rate_never_tightens_the_line()
        {
            Assert.That(ReplicaMotion.IsTeleport(Vector3.zero, new Vector3(2.9f, 0f, 0f), 1), Is.False);
            Assert.That(ReplicaMotion.IsTeleport(Vector3.zero, new Vector3(2.9f, 0f, 0f), 0), Is.False);
        }
    }
}
```

- [ ] **Step 2: Run them and see them fail**

`recompile`. Expected: `ReplicaMotion` does not compile. Temporarily comment out `ReplicaMotionTests.cs` and run `RenderClockTests`. Expected: `After_a_stop_it_waits_for_a_full_cushion_before_moving_on` fails with "It crept on at newest 101 with no cushion"; the other five pass. Uncomment.

- [ ] **Step 3: The clock waits out a stop**

In `Assets/_BattleBomb/Core/Net/RenderClock.cs`:

(a) After the field `private readonly int _delay;` add:

```csharp

        /// <summary>The picture reached the newest snapshot — the host stopped for longer than the cushion (Task 107).</summary>
        private bool _stalled;
```

(b) Replace

```csharp
            float target = newestHostFrame - _delay;
            if (!IsRunning || Mathf.Abs(target - Frame) > SnapAfterSteps)
            {
                Frame = Mathf.Max(0f, target);
                return Frame;
            }

            // One step forward first, then the correction measured from where that lands — measured
            // before the step, an in-step clock would read a one-step gap every step and creep ahead.
            Frame += 1f;
            Frame += Mathf.Clamp((target - Frame) * CorrectionRate, -MaxCorrection, MaxCorrection);
            if (Frame > newestHostFrame)
            {
                Frame = newestHostFrame;
            }

            return Frame;
        }

        public void Reset() => Frame = -1f;
```

with

```csharp
            float target = newestHostFrame - _delay;
            if (!IsRunning || Mathf.Abs(target - Frame) > SnapAfterSteps)
            {
                Frame = Mathf.Max(0f, target);
                _stalled = false;
                return Frame;
            }

            if (_stalled)
            {
                // The host stopped for longer than the cushion (a hold, a spike) and the picture is waiting at the newest
                // snapshot. It waits on until the newest is a full delay ahead again, rather than creeping forward with no
                // cushion at all: a tenth of a second more stillness after a stop, instead of stutters for half a second.
                if (target < Frame)
                {
                    return Frame;
                }

                _stalled = false;
            }

            // One step forward first, then the correction measured from where that lands — measured
            // before the step, an in-step clock would read a one-step gap every step and creep ahead.
            Frame += 1f;
            Frame += Mathf.Clamp((target - Frame) * CorrectionRate, -MaxCorrection, MaxCorrection);
            if (Frame >= newestHostFrame)
            {
                Frame = newestHostFrame;
                _stalled = _delay > 0;
            }

            return Frame;
        }

        public void Reset()
        {
            Frame = -1f;
            _stalled = false;
        }
```

- [ ] **Step 4: One rule for a slide or a teleport**

Create `Assets/_BattleBomb/Core/Net/ReplicaMotion.cs`:

```csharp
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// How the guest tells a slide from a teleport between two snapshots (Task 107). The line is a speed, not a distance:
    /// <see cref="NetProtocol.ReplicaTeleportDistance"/> in the time between two neighbouring snapshots — 90 units a second,
    /// far past anything that runs, falls or is knocked back. So when snapshots are lost and the two around the picture are
    /// further apart, the line moves out with them, and a fast body is still drawn sliding.
    /// </summary>
    public static class ReplicaMotion
    {
        public static bool IsTeleport(Vector3 from, Vector3 to, int gapSteps)
        {
            float scale = Mathf.Max(1f, gapSteps / (float)NetProtocol.SnapshotEverySteps);
            float limit = NetProtocol.ReplicaTeleportDistance * scale;
            return (to - from).sqrMagnitude > limit * limit;
        }
    }
}
```

In `Assets/_BattleBomb/Gameplay/Net/ReplicaWorld.cs`:

(a) Replace

```csharp
        private const float TeleportDistanceSq = NetProtocol.ReplicaTeleportDistance * NetProtocol.ReplicaTeleportDistance;

```

with nothing (delete the line and the blank line after it).

(b) In `Apply`, replace

```csharp
            WorldSnapshot near = t < 0.5f ? from : to;
            ApplyPlayers(from, to, t, near);
            ApplyEnemies(from, to, t, near);
            ApplyDummies(from, to, t, near);
```

with

```csharp
            WorldSnapshot near = t < 0.5f ? from : to;
            _gapSteps = to.HostFrame - from.HostFrame;
            ApplyPlayers(from, to, t, near);
            ApplyEnemies(from, to, t, near);
            ApplyDummies(from, to, t, near);
```

(c) After the field `private readonly HashSet<int> _unspawnable = new HashSet<int>();` add:

```csharp

        /// <summary>Steps between the two snapshots around the picture this step — more than two when some were lost.</summary>
        private int _gapSteps = NetProtocol.SnapshotEverySteps;
```

(d) Replace the method `Blend`:

```csharp
        private static MotorState Blend(in MotorState a, in MotorState b, float t)
        {
            MotorState near = t < 0.5f ? a : b;
            if ((b.Position - a.Position).sqrMagnitude > TeleportDistanceSq)
            {
                return near;
            }
```

with

```csharp
        private MotorState Blend(in MotorState a, in MotorState b, float t)
        {
            MotorState near = t < 0.5f ? a : b;
            if (ReplicaMotion.IsTeleport(a.Position, b.Position, _gapSteps))
            {
                return near;
            }
```

`Blend` stops being `static` because it reads the gap. Its three callers are instance methods already.

- [ ] **Step 5: Run them and see them pass**

`recompile`, then `run_tests` mode `EditMode`, filters `RenderClockTests` (6/6) and `ReplicaMotionTests` (3/3).

The existing `It_never_draws_past_the_newest_snapshot` runs with a delay of 0. With no cushion there is nothing to wait for, which is why `_stalled` needs `_delay > 0`.

- [ ] **Step 6: The gates**

- `run_tests` mode `EditMode`: baseline + 11, all passing.
- `run_tests` mode `PlayMode` (async): baseline + 4, all passing. In particular `GuestReplicaSmokeTests` (its teleport case) and `ReplicaReplaySmokeTests`: the render clock drives both, and the replay's error tolerances must still hold.

- [ ] **Step 7: Commit (`DONE`)**

Paths:
- `Assets/_BattleBomb/Core/Net/RenderClock.cs`
- `Assets/_BattleBomb/Core/Net/ReplicaMotion.cs` (+ `.meta`)
- `Assets/_BattleBomb/Gameplay/Net/ReplicaWorld.cs`
- `Assets/_BattleBomb/Tests/EditMode/Net/RenderClockTests.cs`
- `Assets/_BattleBomb/Tests/EditMode/Net/ReplicaMotionTests.cs` (+ `.meta`)

Subject: `107: the guest's picture through pauses and lost snapshots`. Body: after the host stops for longer than the jitter cushion, the guest's picture now waits until a full cushion has arrived before moving on, instead of creeping forward with none and stuttering for half a second; and the slide-or-teleport rule is now a speed, so a fast body across lost snapshots still slides. Let-go and drain are unchanged on the evidence (Michael re-checks them in 108); the skipped-frame Jump is written down and built only if 108 shows it.

---

### Task 108: Michael's feel pass (Stage E) — **needs QUIET**

**Why.** Prediction is judged by hands and eyes, and only Michael can drive the second window. This is a checklist task: `docs/team/m8-plan3-pass.md`, Part E, written with this plan. It repeats Plan 1's lag table with prediction on, and runs the same moves with prediction off in the same sitting, so the difference is judged side by side.

**Files:** none (unless a finding becomes a `108x` fix).

- [ ] **Step 1: Ready the build**

- EditMode and PlayMode are green at the tip.
- The editor is out of play mode.
- Multiplayer Play Mode's Player 2 is available: Plan 1's Task 90 set it up.

Tell the orchestrator you are ready. It calls `QUIET ON`.

- [ ] **Step 2: Tell Michael the sheet is ready**

Via the orchestrator: `docs/team/m8-plan3-pass.md`, Part E. About 20 minutes.

**96b.** In the two-editor rig, the arrow keys in one window can move the other window's hero. The sheet says to drive with **WASD** in both windows, and carries the Builder's 96b finding. If 96b was fixed, delete that line from the sheet before handing it over and say so in the `DONE`.

- [ ] **Step 3: Record the verdicts**

The orchestrator relays Michael's answers. Write them into your lane file's close-out draft: the new lag table (moving and attacking × None, Normal, Bad × prediction on and off), and each check's result.

- Any "too late" with prediction on, at Normal lag, is a failed check. Report it before Stage F starts.
- At Bad lag, "noticeable but OK" is the target and "too late" is a finding.
- A finding is raised as a `108a`, `108b`… task before 109. The orchestrator decides whether Stage F waits for it.

- [ ] **Step 4: `QUIET OFF`, then `DONE`**

Paths: your lane file only (the verdicts). Subject: `108: Michael's feel pass (Stage E)`.

---

## Stage F — Steam

**Before Stage F:** Task 108 has passed, or the orchestrator has said which of its findings Stage F may run ahead of.

Every M8 build runs on **Spacewar, app 480**, Valve's shared test app (Michael's decision 8a). It needs:
- `steam_appid.txt` containing `480`, beside the executable (and, in the editor, at the project root);
- the Steam client running and signed in.

Michael's own app ID replaces 480 at the close-out (114).

**Research, checked 2026-09-26 against the package's source at the pinned tag:**
- Steamworks.NET **2025.164.1** (Steamworks SDK 1.64) installs as a UPM git package: `https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.164.1`. Its assembly is named `com.rlabrecque.steamworks.net`.
- The package ships **no MonoBehaviour wrapper**. The game calls `SteamAPI.InitEx`, `SteamAPI.RunCallbacks` every frame, and `SteamAPI.Shutdown` itself.
- Its code compiles only on Windows, macOS, Linux and Android targets. Every file is wrapped in `#if !DISABLESTEAMWORKS`. The editor on a Windows target is fine.

### Task 109: Steamworks.NET and the platform registry

**Why.** HANDOFF planning decision 3: Steam registers itself behind the Platform seam, and nothing references it.
- **The new assembly.** `BattleBomb.Platform.Steam` compiles only when the package is installed: a `versionDefines` entry sets `STEAMWORKS_NET`, and the assembly's `defineConstraints` require it. Rule 6's promise ("a build with Steamworks entirely absent compiles and runs") is kept by construction.
- **The registry.** `PlatformRegistry`, in Platform, is where the game asks for its platform.
  - The Steam assembly registers a factory before the first scene loads.
  - The first caller brings Steam up. If Steam will not start (the client is closed), everyone gets `NullPlatformServices` and the game runs exactly as today.
- **What this task delivers.** Identity: the save's name becomes the Steam ID, and the machine's old `local` save is copied under it the first time, so Michael's progress does not appear lost. Networking and friends are Tasks 110 and 111.
- **Tests stay offline.** The PlayMode suite pins the offline platform before any test, so it behaves the same with Steam open or closed.

**Files:**
- Modify:
  - `Packages/manifest.json`
  - `Assets/_BattleBomb/Platform/IPlatformServices.cs` (CRLF), `Platform/NullPlatformServices.cs` (CRLF)
  - `Gameplay/Session/GameSession.cs` (CRLF)
  - `Tests/EditMode/ArchitectureFitnessTests.cs` (CRLF), `Tests/EditMode/NullPlatformServicesTests.cs` (CRLF)
- Create:
  - `steam_appid.txt` (repository root)
  - `Assets/_BattleBomb/Platform/ILobbyService.cs`, `Platform/PlatformRegistry.cs`
  - `Platform/Steam/BattleBomb.Platform.Steam.asmdef`, `Platform/Steam/AssemblyInfo.cs`, `Platform/Steam/SteamPlatformServices.cs`, `Platform/Steam/SteamCallbackPump.cs`
- Test (create):
  - `Tests/EditMode/PlatformRegistryTests.cs`, `Tests/EditMode/SaveAdoptionTests.cs`
  - `Tests/PlayMode/OfflinePlatform.cs`
  - `Tests/Steam/BattleBomb.Tests.Steam.asmdef`, `Tests/Steam/SteamSessionTests.cs`

- [ ] **Step 1: Write the failing tests (registry, adoption, fitness)**

Create `Assets/_BattleBomb/Tests/EditMode/PlatformRegistryTests.cs`:

```csharp
using BattleBomb.Platform;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    public sealed class PlatformRegistryTests
    {
        [SetUp]
        public void ForgetBefore() => PlatformRegistry.ResetForTests();

        [TearDown]
        public void ForgetAfter() => PlatformRegistry.ResetForTests();

        [Test]
        public void With_nothing_registered_the_game_runs_offline()
        {
            Assert.That(PlatformRegistry.Current, Is.InstanceOf<NullPlatformServices>());
            Assert.That(PlatformRegistry.Current.Lobby.CanHost, Is.False);
        }

        [Test]
        public void A_platform_that_will_not_start_leaves_the_game_offline()
        {
            var steamClosed = new FakePlatform(starts: false);
            PlatformRegistry.Register(() => steamClosed);

            Assert.That(PlatformRegistry.Current, Is.InstanceOf<NullPlatformServices>(),
                "Rule 6: with the platform unavailable the game still runs, on the null platform.");
            Assert.That(steamClosed.ShutDown, Is.True, "A platform that failed to start was left half up.");
        }

        [Test]
        public void A_platform_that_starts_is_the_one_everybody_gets_and_it_starts_once()
        {
            int made = 0;
            var steam = new FakePlatform(starts: true);
            PlatformRegistry.Register(() =>
            {
                made++;
                return steam;
            });

            Assert.That(PlatformRegistry.Current, Is.SameAs(steam));
            Assert.That(PlatformRegistry.Current, Is.SameAs(steam));
            Assert.That(made, Is.EqualTo(1), "The platform's API is one per process: it must come up once.");
        }

        [Test]
        public void A_platform_that_shuts_down_leaves_the_slot_for_the_next_caller()
        {
            var steam = new FakePlatform(starts: true);
            PlatformRegistry.Register(() => steam);
            Assert.That(PlatformRegistry.Current, Is.SameAs(steam));

            PlatformRegistry.Release(steam);
            var again = new FakePlatform(starts: true);
            PlatformRegistry.Register(() => again);

            Assert.That(PlatformRegistry.Current, Is.SameAs(again));
        }

        internal sealed class FakePlatform : IPlatformServices
        {
            private readonly bool _starts;
            private readonly NullPlatformServices _offline = new NullPlatformServices();

            private readonly ILobbyService _lobby;

            internal FakePlatform(bool starts, ISaveStore saves = null, string userId = null, ILobbyService lobby = null)
            {
                _starts = starts;
                Saves = saves ?? new MemorySaveStore();
                UserId = userId;
                _lobby = lobby;
            }

            internal bool ShutDown { get; private set; }

            internal string UserId { get; }

            public bool IsAvailable => _starts;

            public IAchievements Achievements => _offline.Achievements;

            public IPlayerIdentity Identity => UserId != null ? new SignedIn(UserId) : _offline.Identity;

            public ILeaderboards Leaderboards => _offline.Leaderboards;

            public ISaveStore Saves { get; }

            public ILobbyService Lobby => _lobby ?? _offline.Lobby;

            public bool Initialise() => _starts;

            public void Shutdown() => ShutDown = true;

            private sealed class SignedIn : IPlayerIdentity
            {
                internal SignedIn(string id) => UserId = id;

                public bool IsSignedIn => true;

                public string UserId { get; }

                public string DisplayName => "Test";
            }
        }
    }
}
```

Create `Assets/_BattleBomb/Tests/EditMode/SaveAdoptionTests.cs`:

```csharp
using BattleBomb.Gameplay.Session;
using BattleBomb.Platform;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>The first time a platform identity names the save (Task 109), the machine's old save comes with it.</summary>
    public sealed class SaveAdoptionTests
    {
        private GameObject _go;

        [SetUp]
        public void Make()
        {
            PlatformRegistry.ResetForTests();
            _go = new GameObject("SaveAdoptionTests");
        }

        [TearDown]
        public void Clean()
        {
            Object.DestroyImmediate(_go);
            PlatformRegistry.ResetForTests();
        }

        [Test]
        public void Signing_in_the_first_time_copies_the_local_save_under_the_new_name()
        {
            var store = new MemorySaveStore();
            store.Write("local", "{\"old\":true}");
            PlatformRegistry.Register(() => new PlatformRegistryTests.FakePlatform(true, store, "76561190000000001"));

            GameSession session = _go.AddComponent<GameSession>();
            session.LoadFromStore();

            Assert.That(session.SaveName, Is.EqualTo("76561190000000001"));
            Assert.That(store.TryRead("76561190000000001", out string adopted), Is.True, "The old save was left behind.");
            Assert.That(adopted, Is.EqualTo("{\"old\":true}"));
            Assert.That(store.Exists("local"), Is.True, "A copy, never a move: the old file stays as it was.");
        }

        [Test]
        public void A_save_already_under_the_platform_name_is_never_overwritten()
        {
            var store = new MemorySaveStore();
            store.Write("local", "{\"old\":true}");
            store.Write("76561190000000001", "{\"mine\":true}");
            PlatformRegistry.Register(() => new PlatformRegistryTests.FakePlatform(true, store, "76561190000000001"));

            GameSession session = _go.AddComponent<GameSession>();
            session.LoadFromStore();

            store.TryRead("76561190000000001", out string kept);
            Assert.That(kept, Is.EqualTo("{\"mine\":true}"));
        }
    }
}
```

Both stored blobs are placeholders, not real saves. `LoadFromStore` refuses to decode them (`LoadOutcome` is a refusal, not `Ok`), which is fine: these tests check which file is read and written, not what is in it.

In `Assets/_BattleBomb/Tests/EditMode/ArchitectureFitnessTests.cs` (CRLF; keep it), after the method `Core_references_no_other_BattleBomb_assembly` (its closing brace) add:

```csharp

        [Test]
        public void Only_the_Steam_assembly_touches_Steamworks_and_nothing_references_it()
        {
            // Rule 6 and D58: Steam lives behind the Platform seam in one assembly that registers itself; every other
            // assembly must compile and run with the package entirely absent.
            string[] offenders = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("BattleBomb", StringComparison.Ordinal))
                .Where(a => a.GetName().Name != "BattleBomb.Platform.Steam" && a.GetName().Name != "BattleBomb.Tests.Steam")
                .Where(a => a.GetReferencedAssemblies().Any(r =>
                    r.Name == "com.rlabrecque.steamworks.net" || r.Name == "BattleBomb.Platform.Steam"))
                .Select(a => a.GetName().Name)
                .ToArray();

            Assert.That(offenders, Is.Empty, "Only BattleBomb.Platform.Steam may know Steam exists: " + string.Join(", ", offenders));
        }
```

`ArchitectureFitnessTests.cs` already has `using System;` and `using System.Linq;`.

In `Assets/_BattleBomb/Tests/EditMode/NullPlatformServicesTests.cs` (CRLF; keep it), replace

```csharp
            Assert.That(services.Identity.IsSignedIn, Is.False);
            Assert.DoesNotThrow(services.Shutdown);
        }
```

with the same four lines followed by:

```csharp

        [Test]
        public void Offline_nobody_can_host_or_join_through_a_platform()
        {
            IPlatformServices services = new NullPlatformServices();

            Assert.That(services.Lobby.CanHost, Is.False);
            Assert.That(services.Lobby.CreateTransport(), Is.Null);
            Assert.That(services.Lobby.HasJoinRequest, Is.False);
            Assert.That(services.Lobby.TryTakeJoinRequest(out string address), Is.False);
            Assert.That(address, Is.Null);
        }
```

- [ ] **Step 2: Run them and see them fail**

`recompile`. Expected: compile errors, because `PlatformRegistry`, `ILobbyService` and `IPlatformServices.Lobby` do not exist.

- [ ] **Step 3: The seam grows a lobby and a registry**

Create `Assets/_BattleBomb/Platform/ILobbyService.cs`:

```csharp
using BattleBomb.Platform.Net;

namespace BattleBomb.Platform
{
    /// <summary>
    /// How platform friends find a game and join it (D59, HANDOFF-M8 planning decision 3) — platform-neutral, so nothing
    /// above the seam assumes Steam (D58). A game is joinable while its transport listens with a free seat, and the platform
    /// shows friends that by itself. A friend's "join" — an accepted invite, a click on "Join game" — waits here until the
    /// front door takes it.
    /// </summary>
    public interface ILobbyService
    {
        /// <summary>False when there is no platform network: nothing opens to friends, nothing is joined through it.</summary>
        bool CanHost { get; }

        /// <summary>A fresh transport over the platform's own network, or null when <see cref="CanHost"/> is false.</summary>
        INetTransport CreateTransport();

        /// <summary>A friend's game waits to be joined.</summary>
        bool HasJoinRequest { get; }

        /// <summary>Takes the waiting join, once: the address to hand <see cref="INetTransport.Connect"/>.</summary>
        bool TryTakeJoinRequest(out string address);
    }
}
```

Create `Assets/_BattleBomb/Platform/PlatformRegistry.cs`:

```csharp
using System;
using UnityEngine;

namespace BattleBomb.Platform
{
    /// <summary>
    /// Where the game gets its platform (HANDOFF-M8 planning decision 3). A platform assembly that is present — Steam's,
    /// when Steamworks.NET is installed — registers a factory before the first scene loads; nothing references it. The first
    /// caller brings it up, and if it cannot come up (Steam is not running) everyone gets <see cref="NullPlatformServices"/>
    /// and the game runs offline (rule 6). One live instance, because a storefront's API is one per process: it holds the
    /// connection to the platform, never game state.
    /// </summary>
    public static class PlatformRegistry
    {
        private static Func<IPlatformServices> _factory;
        private static IPlatformServices _current;

        /// <summary>Called by a platform assembly before the first scene. The last registration wins.</summary>
        public static void Register(Func<IPlatformServices> factory) => _factory = factory;

        public static IPlatformServices Current => _current ?? (_current = Start(_factory));

        /// <summary>Tests and tools: this platform, already started, instead of whatever is registered.</summary>
        public static void Use(IPlatformServices platform) => _current = platform;

        /// <summary>A platform that has shut itself down (the game quitting, the editor leaving play) leaves the slot, so the
        /// next caller starts again rather than talking to a dead API.</summary>
        public static void Release(IPlatformServices platform)
        {
            if (ReferenceEquals(_current, platform))
            {
                _current = null;
            }
        }

        /// <summary>Tests only: forget the live platform and the registration.</summary>
        public static void ResetForTests()
        {
            _current = null;
            _factory = null;
        }

        private static IPlatformServices Start(Func<IPlatformServices> factory)
        {
            IPlatformServices platform = null;
            try
            {
                platform = factory?.Invoke();
                if (platform != null && platform.Initialise())
                {
                    return platform;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Platform: {e.Message} — running offline.");
            }

            platform?.Shutdown();
            return new NullPlatformServices();
        }
    }
}
```

In `Assets/_BattleBomb/Platform/IPlatformServices.cs` (CRLF; keep it), replace

```csharp
        /// <summary>Where saves go (D52). Local files when no platform is present.</summary>
        ISaveStore Saves { get; }
```

with

```csharp
        /// <summary>Where saves go (D52). Local files when no platform is present.</summary>
        ISaveStore Saves { get; }

        /// <summary>Friends finding and joining games (D59). Offline, nobody can.</summary>
        ILobbyService Lobby { get; }
```

In `Assets/_BattleBomb/Platform/NullPlatformServices.cs` (CRLF; keep it):

(a) Replace

```csharp
    public sealed class NullPlatformServices : IPlatformServices, IAchievements, IPlayerIdentity, ILeaderboards
```

with

```csharp
    public sealed class NullPlatformServices : IPlatformServices, IAchievements, IPlayerIdentity, ILeaderboards, ILobbyService
```

(b) Replace

```csharp
        public ILeaderboards Leaderboards => this;
```

with

```csharp
        public ILeaderboards Leaderboards => this;

        public ILobbyService Lobby => this;
```

(c) Replace

```csharp
        public void FetchTop(string boardId, int count, Action<LeaderboardEntry[]> onComplete) =>
            onComplete?.Invoke(Array.Empty<LeaderboardEntry>());
```

with

```csharp
        public void FetchTop(string boardId, int count, Action<LeaderboardEntry[]> onComplete) =>
            onComplete?.Invoke(Array.Empty<LeaderboardEntry>());

        // ILobbyService — no platform network, so nothing opens to friends and nothing is joined through one (rule 6).

        public bool CanHost => false;

        public Net.INetTransport CreateTransport() => null;

        public bool HasJoinRequest => false;

        public bool TryTakeJoinRequest(out string address)
        {
            address = null;
            return false;
        }
```

- [ ] **Step 4: The session asks the registry, and adopts the old save**

In `Assets/_BattleBomb/Gameplay/Session/GameSession.cs` (CRLF; keep it):

(a) Replace

```csharp
            if (Store == null)
            {
                IPlatformServices platform = new NullPlatformServices();
                Store = platform.Saves;
                SaveName = platform.Identity.IsSignedIn ? platform.Identity.UserId : "local";
            }
```

with

```csharp
            if (Store == null)
            {
                IPlatformServices platform = PlatformRegistry.Current;
                Store = platform.Saves;
                SaveName = platform.Identity.IsSignedIn ? platform.Identity.UserId : LocalSave;
                AdoptLocalSave();
            }
```

(b) After the method `LoadFromStore` (its closing brace) add:

```csharp

        /// <summary>The one save a machine keeps when no platform names it (D51).</summary>
        public const string LocalSave = "local";

        /// <summary>
        /// The first time a platform identity names the save (Task 109: the Steam ID), the machine's existing save is copied
        /// under the new name, so signing in never looks like starting over. A copy, never a move — the old file stays — and
        /// never over a save the new name already has.
        /// </summary>
        private void AdoptLocalSave()
        {
            if (SaveName == LocalSave || Store.Exists(SaveName) || !Store.TryRead(LocalSave, out string text))
            {
                return;
            }

            Store.Write(SaveName, text);
        }
```

- [ ] **Step 5: Run the EditMode tests green**

`recompile`, then `run_tests` mode `EditMode`, filters `PlatformRegistryTests` (4/4), `SaveAdoptionTests` (2/2), `NullPlatformServicesTests` (all) and `ArchitectureFitnessTests` (all; the new one passes because no Steam assembly exists yet).

- [ ] **Step 6: Install Steamworks.NET**

In `Packages/manifest.json`, replace

```json
    "com.unity.2d.animation": "15.1.0",
```

with

```json
    "com.rlabrecque.steamworks.net": "https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.164.1",
    "com.unity.2d.animation": "15.1.0",
```

Then `package_resolve`, and `package_status` until it settles.
- **Git must be on the machine's `PATH`** (UPM clones git packages). If resolution fails with a git error, stop and tell the orchestrator; Michael installs Git for Windows.
- **Read back** `Packages/packages-lock.json`: it must name `com.rlabrecque.steamworks.net` at `2025.164.1`, with a commit hash.

Create `steam_appid.txt` at the **repository root** (beside `Assets/`, not in it). The content is exactly the three characters `480`, with no newline:

```
480
```

- In the editor, Steam reads it from the working directory, which is the project root.
- In a build, Task 113's script puts it beside the executable.
- It lives outside `Assets/`: list it in the `DONE`.

- [ ] **Step 7: The Steam assembly**

Create `Assets/_BattleBomb/Platform/Steam/BattleBomb.Platform.Steam.asmdef`:

```json
{
    "name": "BattleBomb.Platform.Steam",
    "rootNamespace": "BattleBomb.Platform.Steam",
    "references": [
        "BattleBomb.Core",
        "BattleBomb.Platform",
        "com.rlabrecque.steamworks.net"
    ],
    "includePlatforms": [
        "Editor",
        "LinuxStandalone64",
        "macOSStandalone",
        "WindowsStandalone64"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [
        "STEAMWORKS_NET"
    ],
    "versionDefines": [
        {
            "name": "com.rlabrecque.steamworks.net",
            "expression": "2025.164.1",
            "define": "STEAMWORKS_NET"
        }
    ],
    "noEngineReferences": false
}
```

**How this keeps rule 6.** `versionDefines` sets `STEAMWORKS_NET` for this assembly only when the package (at 2025.164.1 or later) is installed. `defineConstraints` then compiles the assembly only when that symbol is set. Remove the package and the assembly quietly stops existing: nothing references it, so nothing breaks.

Create `Assets/_BattleBomb/Platform/Steam/AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BattleBomb.Tests.Steam")]
```

Create `Assets/_BattleBomb/Platform/Steam/SteamCallbackPump.cs`:

```csharp
using Steamworks;
using UnityEngine;

namespace BattleBomb.Platform.Steam
{
    /// <summary>
    /// Steam's callbacks, run once a frame ahead of the network session (order -400; <c>NetSession</c> is -300), so a
    /// connection's news is in before the transport is pumped. It also shuts Steam down when the game — or the editor's play
    /// session — ends.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    internal sealed class SteamCallbackPump : MonoBehaviour
    {
        private SteamPlatformServices _services;

        internal static SteamCallbackPump Create(SteamPlatformServices services)
        {
            var go = new GameObject("Steam");
            if (Application.isPlaying)
            {
                // Edit mode has no scenes to survive — and refuses the call (the Steam tests run there).
                DontDestroyOnLoad(go);
            }

            SteamCallbackPump pump = go.AddComponent<SteamCallbackPump>();
            pump._services = services;
            return pump;
        }

        private void Update() => SteamAPI.RunCallbacks();

        private void OnApplicationQuit() => _services?.Shutdown();
    }
}
```

Create `Assets/_BattleBomb/Platform/Steam/SteamPlatformServices.cs`:

```csharp
using System.Globalization;
using Steamworks;
using UnityEngine;

namespace BattleBomb.Platform.Steam
{
    /// <summary>
    /// Steam, behind the platform seam (D58, rule 6): who the player is, and — from Tasks 110 to 112 — the peer-to-peer
    /// network, friends joining, and the cloud. Registered before the first scene and brought up by the first caller. With
    /// the Steam client closed it declines to start, and the game runs on <see cref="NullPlatformServices"/>.
    /// </summary>
    public sealed class SteamPlatformServices : IPlatformServices, IPlayerIdentity
    {
        /// <summary>Spacewar, Valve's shared test app (Michael's decision 8a). Every M8 build runs on it until the close-out
        /// swaps BattleBomb's own ID into steam_appid.txt (Task 114).</summary>
        public const uint SpacewarAppId = 480;

        private readonly NullPlatformServices _offline = new NullPlatformServices();
        private SteamCallbackPump _pump;
        private bool _running;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => PlatformRegistry.Register(() => new SteamPlatformServices());

        public bool IsAvailable => _running;

        /// <summary>Steam achievements and leaderboards are Early Access work (M13); until then they are the offline ones.</summary>
        public IAchievements Achievements => _offline.Achievements;

        public IPlayerIdentity Identity => _running ? this : _offline.Identity;

        public ILeaderboards Leaderboards => _offline.Leaderboards;

        public ISaveStore Saves => _offline.Saves;

        public ILobbyService Lobby => _offline.Lobby;

        public bool Initialise()
        {
            if (_running)
            {
                return true;
            }

            if (!Packsize.Test())
            {
                Debug.LogWarning("Steam: the Steamworks.NET library does not match this platform — running offline.");
                return false;
            }

            ESteamAPIInitResult result = SteamAPI.InitEx(out string error);
            if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Debug.Log($"Steam: not available ({result}: {error}) — running offline.");
                return false;
            }

            _running = true;
            _pump = SteamCallbackPump.Create(this);
            Debug.Log($"Steam: signed in as {DisplayName} on app {SteamUtils.GetAppID().m_AppId}.");
            return true;
        }

        public void Shutdown()
        {
            if (!_running)
            {
                return;
            }

            _running = false;
            if (_pump != null)
            {
                // Edit mode (the Steam tests) cannot Destroy; play mode must not DestroyImmediate.
                if (Application.isPlaying)
                {
                    Object.Destroy(_pump.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(_pump.gameObject);
                }

                _pump = null;
            }

            SteamAPI.Shutdown();
            PlatformRegistry.Release(this);
        }

        // IPlayerIdentity — the Steam ID is the save's name (Task 109); plain digits, so every store can hold it.

        public bool IsSignedIn => _running && SteamUser.BLoggedOn();

        public string UserId => _running ? SteamUser.GetSteamID().m_SteamID.ToString(CultureInfo.InvariantCulture) : string.Empty;

        public string DisplayName => _running ? SteamFriends.GetPersonaName() : "Player";
    }
}
```

`Object` in `Shutdown` is `UnityEngine.Object`: the file has `using UnityEngine;` and no `using System;`.

- [ ] **Step 8: The PlayMode suite never touches Steam**

Create `Assets/_BattleBomb/Tests/PlayMode/OfflinePlatform.cs`:

```csharp
using BattleBomb.Platform;
using NUnit.Framework;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// Runs once before every PlayMode test in this assembly: the platform is the offline one, whatever is installed and
    /// whether or not the Steam client is open on this PC. The suite tests the game; Steam has its own tests
    /// (BattleBomb.Tests.Steam), and a suite that behaved differently with Steam open would be two suites.
    /// </summary>
    [SetUpFixture]
    public sealed class OfflinePlatform
    {
        [OneTimeSetUp]
        public void Offline() => PlatformRegistry.Use(new NullPlatformServices());
    }
}
```

**Why a namespace-level set-up fixture.** NUnit runs `[SetUpFixture]` before every fixture in its namespace (`BattleBomb.Tests.PlayMode`), after the play-mode domain reload has registered Steam's factory. `Use` then fills the slot before any test's first `PlatformRegistry.Current`.

- [ ] **Step 9: The Steam test assembly**

Create `Assets/_BattleBomb/Tests/Steam/BattleBomb.Tests.Steam.asmdef`:

```json
{
    "name": "BattleBomb.Tests.Steam",
    "rootNamespace": "BattleBomb.Tests.Steam",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "BattleBomb.Core",
        "BattleBomb.Platform",
        "BattleBomb.Platform.Steam",
        "com.rlabrecque.steamworks.net"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS",
        "STEAMWORKS_NET"
    ],
    "versionDefines": [
        {
            "name": "com.rlabrecque.steamworks.net",
            "expression": "2025.164.1",
            "define": "STEAMWORKS_NET"
        }
    ],
    "noEngineReferences": false
}
```

Create `Assets/_BattleBomb/Tests/Steam/SteamSessionTests.cs`:

```csharp
using System.IO;
using BattleBomb.Platform;
using BattleBomb.Platform.Steam;
using NUnit.Framework;

namespace BattleBomb.Tests.Steam
{
    /// <summary>
    /// Steam itself, when the client is running on this PC (on the app steam_appid.txt at the project root names — Spacewar
    /// until the close-out). With Steam closed every test here is Ignored, not failed: the rest of the game must not depend
    /// on it (rule 6).
    /// </summary>
    public sealed class SteamSessionTests
    {
        private SteamPlatformServices _steam;
        private bool _started;

        [OneTimeSetUp]
        public void StartSteam()
        {
            _steam = new SteamPlatformServices();
            _started = _steam.Initialise();
        }

        [OneTimeTearDown]
        public void StopSteam() => _steam.Shutdown();

        [Test]
        public void Signed_in_the_player_is_their_Steam_ID_on_the_app_steam_appid_names()
        {
            if (!_started)
            {
                Assert.Ignore("The Steam client is not running on this PC.");
            }

            Assert.That(_steam.IsAvailable, Is.True);
            Assert.That(_steam.Identity.IsSignedIn, Is.True);
            Assert.That(ulong.TryParse(_steam.Identity.UserId, out ulong id) && id != 0UL, Is.True,
                "The save's name must be the Steam ID's digits.");
            Assert.That(_steam.Identity.DisplayName, Is.Not.Empty);

            uint named = uint.Parse(File.ReadAllText("steam_appid.txt").Trim());
            Assert.That(Steamworks.SteamUtils.GetAppID().m_AppId, Is.EqualTo(named),
                "Steam is running the game as a different app from the one steam_appid.txt names.");
        }
    }
}
```

This test runs in edit mode, where `Shutdown` must destroy the pump with `DestroyImmediate`. That is why Step 7's `Shutdown` checks `Application.isPlaying`.

In edit mode the pump's `Update` does not run. `SteamAPI.RunCallbacks` is not needed for anything this test reads.

- [ ] **Step 10: Run everything**

`recompile`, then `recompile_status` until done.

Expected:
- no compile errors;
- in the console (`console`, level error): nothing new;
- the assembly list includes `BattleBomb.Platform.Steam` and `BattleBomb.Tests.Steam`.

Then:
- `run_tests` mode `EditMode`: baseline + 20, all passing, of which 1 is Ignored if Steam is closed.
  - Name `SteamSessionTests` in the `DONE`: passed (with the Steam ID's last four digits and the persona name) or Ignored.
  - The fitness test must still pass, now that the Steam assemblies exist.
- `run_tests` mode `PlayMode` (async): baseline + 4, all passing.

- [ ] **Step 11: A settled live check (no QUIET)**

With Steam running:
1. `editor_play` the Frontend scene.
2. Read `console`. Expected: `Steam: signed in as <name> on app 480.`
3. `editor_stop`, then read `console` again: no errors on the way out (the pump's `OnApplicationQuit` shut Steam down).
4. Play once more: it signs in again. That proves `Release` emptied the slot.

With Steam closed, the same play logs `Steam: not available (k_ESteamAPIInitResult_NoSteamClient…) — running offline.` and the game plays exactly as before. If Michael's Steam is open, one of the two is enough; say which in the `DONE`.

Last, check the save: with Steam running, the `saves` folder (`Application.persistentDataPath/saves`) now holds `<SteamID>.save` beside `local.save`, with the same content.

- [ ] **Step 12: Commit (`DONE`)**

Paths:
- `Packages/manifest.json`, `Packages/packages-lock.json`
- `steam_appid.txt` — **outside `Assets/`: the orchestrator allows it**
- `Assets/_BattleBomb/Platform/ILobbyService.cs`, `Assets/_BattleBomb/Platform/PlatformRegistry.cs` (+ `.meta`s)
- `Assets/_BattleBomb/Platform/IPlatformServices.cs`, `Assets/_BattleBomb/Platform/NullPlatformServices.cs`
- `Assets/_BattleBomb/Platform/Steam/` (the folder `.meta`, the asmdef, `AssemblyInfo.cs`, `SteamPlatformServices.cs`, `SteamCallbackPump.cs`, and their `.meta`s)
- `Assets/_BattleBomb/Gameplay/Session/GameSession.cs`
- `Assets/_BattleBomb/Tests/EditMode/PlatformRegistryTests.cs`, `Assets/_BattleBomb/Tests/EditMode/SaveAdoptionTests.cs` (+ `.meta`s)
- `Assets/_BattleBomb/Tests/EditMode/ArchitectureFitnessTests.cs`, `Assets/_BattleBomb/Tests/EditMode/NullPlatformServicesTests.cs`
- `Assets/_BattleBomb/Tests/PlayMode/OfflinePlatform.cs` (+ `.meta`)
- `Assets/_BattleBomb/Tests/Steam/` (the folder `.meta`, the asmdef, `SteamSessionTests.cs`, and their `.meta`s)

Subject: `109: Steamworks.NET behind the platform seam`. Body: Steam arrives as its own assembly that compiles only when Steamworks.NET is installed and registers itself, so a build without it is unchanged (rule 6); the game now asks a platform registry for its platform, falling back to offline when Steam is not running. The Steam ID names the save, and the machine's old save is copied under it the first time.

---

### Task 110: The Steam peer-to-peer transport

**Why.** The development transports (loopback, the local socket) reach only this PC. Friends in two homes need Steam's network.
- `ISteamNetworkingSockets` over Valve's relay: neither player opens a port, and neither learns the other's address.
- It is connection-oriented, so a host leaving is an event rather than a silence (HANDOFF planning decision 4).
- The address is a Steam ID. As with the local socket, there is one peer at a time (D11).

**Two smaller fixes ride along:**
- **`Listen` says why it failed.** Today `NetSession.Host` catches `SocketException` — Gameplay knowing what a socket is — and always blames port 7777 (the board's Plan 3 item). Steam has its own ways to fail. So `INetTransport.Listen` now returns null when listening, or a readable reason; nothing above the seam catches a transport's exceptions.
- **Steam's send rate is set, not guessed.** Task 96 measured the paper case at 86–117 KB/s of snapshots. Steam's bandwidth estimator starts well below that and climbs, so an unset connection would drop snapshots for its first seconds. The transport sets a floor of 256 KB/s and a ceiling of 1 MB/s at creation. That is why delta snapshots are not needed in M8 (see the departures).

**Files:**
- Modify:
  - `Assets/_BattleBomb/Platform/Net/INetTransport.cs`, `Platform/Net/LocalSocketTransport.cs`, `Platform/Net/LoopbackTransport.cs`, `Platform/Net/LagSimulator.cs` (all LF)
  - `Gameplay/Net/NetSession.cs` (LF)
  - `Platform/Steam/SteamPlatformServices.cs`
  - `Tests/PlayMode/PlaybackTransport.cs`, `Tests/PlayMode/AnsweringHost.cs`
  - `Tests/EditMode/Net/LocalSocketTransportTests.cs`, `Tests/EditMode/Net/LoopbackTransportTests.cs`
- Create: `Assets/_BattleBomb/Platform/Steam/SteamP2PTransport.cs`
- Test (create): `Tests/Steam/SteamTransportTests.cs`

- [ ] **Step 1: Write the failing tests for `Listen`**

In `Assets/_BattleBomb/Tests/EditMode/Net/LocalSocketTransportTests.cs`, replace

```csharp
        public void Listening_on_a_busy_port_throws_once_and_leaves_nothing_behind()
        {
            var squatter = new TcpListener(IPAddress.Loopback, 0);
            squatter.Start();
            var host = new LocalSocketTransport(((IPEndPoint)squatter.LocalEndpoint).Port);
            try
            {
                Assert.Throws<SocketException>(() => host.Listen());
```

with

```csharp
        public void Listening_on_a_busy_port_says_which_and_leaves_nothing_behind()
        {
            var squatter = new TcpListener(IPAddress.Loopback, 0);
            squatter.Start();
            int port = ((IPEndPoint)squatter.LocalEndpoint).Port;
            var host = new LocalSocketTransport(port);
            try
            {
                Assert.That(host.Listen(), Does.Contain("busy").And.Contain(port.ToString()),
                    "A busy port must come back as a reason naming it — the port in use is not always 7777.");
```

In `Assets/_BattleBomb/Tests/EditMode/Net/LoopbackTransportTests.cs`, after the method `Connecting_announces_each_side_to_the_other` (its closing brace) add:

```csharp

        [Test]
        public void Listening_on_the_loopback_always_works()
        {
            LoopbackTransport.CreatePair(out LoopbackTransport host, out LoopbackTransport _);
            Assert.That(host.Listen(), Is.Null);
        }
```

- [ ] **Step 2: Run them and see them fail**

`recompile`. Expected: compile errors ("cannot apply `Does.Contain` to void" and "cannot implicitly convert void"), because `Listen` returns nothing.

- [ ] **Step 3: `Listen` returns why**

In `Assets/_BattleBomb/Platform/Net/INetTransport.cs`, replace

```csharp
        void Listen();
```

with

```csharp
        /// <summary>Starts listening for one peer. Null when listening; otherwise one readable sentence saying why not — "Port
        /// 7777 is busy.", "Steam would not open a listening socket." — for the development panel and the front door. Nothing
        /// above the seam catches a transport's exceptions (D58).</summary>
        string Listen();
```

In `Assets/_BattleBomb/Platform/Net/LocalSocketTransport.cs`, replace

```csharp
        public void Listen()
        {
            // Kept only once started: a busy port throws here, and a listener that never started must
            // not be left behind for every Update to call Pending on.
            var listener = new TcpListener(IPAddress.Loopback, _port);
            try
            {
                listener.Start(1);
            }
            catch
            {
                listener.Stop();
                throw;
            }

            _listener = listener;
        }
```

with

```csharp
        public string Listen()
        {
            // Kept only once started: a listener that never started must not be left behind for every Update to call
            // Pending on.
            var listener = new TcpListener(IPAddress.Loopback, _port);
            try
            {
                listener.Start(1);
            }
            catch (SocketException)
            {
                // Another copy is already hosting on this port.
                listener.Stop();
                return $"Port {_port} is busy.";
            }

            _listener = listener;
            return null;
        }
```

In `Assets/_BattleBomb/Platform/Net/LoopbackTransport.cs`, replace

```csharp
        public void Listen() => _listening = true;
```

with

```csharp
        public string Listen()
        {
            _listening = true;
            return null;
        }
```

In `Assets/_BattleBomb/Platform/Net/LagSimulator.cs`, replace

```csharp
        public void Listen() => _inner.Listen();
```

with

```csharp
        public string Listen() => _inner.Listen();
```

In `Assets/_BattleBomb/Tests/PlayMode/PlaybackTransport.cs` and `Assets/_BattleBomb/Tests/PlayMode/AnsweringHost.cs`, replace

```csharp
        public void Listen()
        {
        }
```

with

```csharp
        public string Listen() => null;
```

**Then grep for every other implementer:** `: INetTransport` across `Assets/`, including any Plan 2 test harness. Give each the same change.

In `Assets/_BattleBomb/Gameplay/Net/NetSession.cs`:

(a) Replace

```csharp
            try
            {
                transport.Listen();
            }
            catch (SocketException)
            {
                // Another copy is already hosting here. The role is committed only once something is
                // listening, so this machine stays offline and the panel says why.
                transport.Dispose();
                Status = $"Port {NetProtocol.DevPort} is busy";
                return;
            }
```

with

```csharp
            string refused = transport.Listen();
            if (refused != null)
            {
                // Nothing is listening — another copy hosts on the port, or the platform said no. The role is committed
                // only once something listens, so this machine stays offline and the panel says why.
                transport.Dispose();
                Status = refused;
                return;
            }
```

(b) Delete the line `using System.Net.Sockets;` — but first grep `NetSession.cs` for `Socket`. After (a), nothing else should use it. If Plan 2 added a use, keep the line and say so in the `DONE`.

- [ ] **Step 4: Run them and see them pass**

`recompile`, then `run_tests` mode `EditMode`, filters `LocalSocketTransportTests`, `LoopbackTransportTests`, `LagSimulatorTests` and `NetSessionHostTests`, all green.

`NetSessionHostTests.Hosting_on_a_busy_port_stays_offline_and_says_why` checks `Status` contains "busy", which the reason still does.

- [ ] **Step 5: The Steam transport**

Create `Assets/_BattleBomb/Platform/Steam/SteamP2PTransport.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using BattleBomb.Core.Net;
using BattleBomb.Platform.Net;
using Steamworks;

namespace BattleBomb.Platform.Steam
{
    /// <summary>
    /// Steam's peer-to-peer network (HANDOFF-M8 planning decision 4, Task 110): ISteamNetworkingSockets over Valve's relay, so
    /// neither player opens a port and neither learns the other's address. Connection-oriented, so a peer leaving is an event
    /// rather than a silence. The address is a Steam ID, as digits. One peer at a time, like the local socket (D11): a second
    /// caller is turned away at once.
    /// </summary>
    public sealed class SteamP2PTransport : INetTransport
    {
        /// <summary>The only virtual port BattleBomb uses.</summary>
        private const int VirtualPort = 0;

        private const int ReceiveBatch = 32;

        /// <summary>Task 96 measured the paper case at 86–117 KB/s of snapshots. Steam's estimator starts lower and climbs, so
        /// the floor is set above that, and the ceiling well above the worst case.</summary>
        internal const int SendRateMinBytes = 256 * 1024;
        internal const int SendRateMaxBytes = 1024 * 1024;

        /// <summary>Longer than the game's own ten-second drop (design §1), so the game — not Steam — says why a friend went.</summary>
        internal const int TimeoutConnectedMs = 15000;

        private readonly Queue<NetEvent> _inbox = new Queue<NetEvent>();
        private readonly IntPtr[] _received = new IntPtr[ReceiveBatch];
        private Callback<SteamNetConnectionStatusChangedCallback_t> _statusChanged;
        private HSteamListenSocket _listen = HSteamListenSocket.Invalid;
        private HSteamNetConnection _connection = HSteamNetConnection.Invalid;
        private NetPeer _peer;
        private bool _connected;

        public SteamP2PTransport()
        {
        }

        /// <summary>Tests: one end of Steam's in-process socket pair, already connected (see <see cref="TryCreatePair"/>).</summary>
        private SteamP2PTransport(HSteamNetConnection connection, NetPeer peer)
        {
            Watch();
            _connection = connection;
            _peer = peer;
            _connected = true;
            _inbox.Enqueue(NetEvent.Connected(peer));
        }

        /// <summary>
        /// Tests: two transports joined by Steam's in-process socket pair — no network and no second account — so sending and
        /// receiving are proven against the real library on one PC. The friend-to-friend path is the two-home pass (113).
        /// </summary>
        internal static bool TryCreatePair(out SteamP2PTransport host, out SteamP2PTransport guest)
        {
            var one = new SteamNetworkingIdentity();
            one.SetLocalHost();
            var two = new SteamNetworkingIdentity();
            two.SetLocalHost();
            if (!SteamNetworkingSockets.CreateSocketPair(
                out HSteamNetConnection a, out HSteamNetConnection b, false, ref one, ref two))
            {
                host = null;
                guest = null;
                return false;
            }

            host = new SteamP2PTransport(a, new NetPeer(2));
            guest = new SteamP2PTransport(b, new NetPeer(1));
            return true;
        }

        /// <summary>Who is on the other end, or <see cref="NetPeer.None"/>.</summary>
        public NetPeer Peer => _connected ? _peer : NetPeer.None;

        public string Listen()
        {
            Watch();
            SteamNetworkingConfigValue_t[] options = Options();
            _listen = SteamNetworkingSockets.CreateListenSocketP2P(VirtualPort, options.Length, options);
            return _listen == HSteamListenSocket.Invalid ? "Steam would not open a listening socket." : null;
        }

        public void Connect(string address)
        {
            if (!TryParseSteamId(address, out ulong steamId))
            {
                throw new ArgumentException($"'{address}' is not a Steam ID.", nameof(address));
            }

            Watch();
            var identity = new SteamNetworkingIdentity();
            identity.SetSteamID64(steamId);
            SteamNetworkingConfigValue_t[] options = Options();
            _peer = new NetPeer(steamId);
            _connection = SteamNetworkingSockets.ConnectP2P(ref identity, VirtualPort, options.Length, options);
            if (_connection == HSteamNetConnection.Invalid)
            {
                _inbox.Enqueue(NetEvent.Disconnected(_peer));
                _peer = NetPeer.None;
            }
        }

        public void Send(NetPeer peer, NetChannel channel, byte[] payload, int length)
        {
            if (!_connected || peer != _peer || length <= 0)
            {
                return;
            }

            int flags = channel == NetChannel.Reliable
                ? Constants.k_nSteamNetworkingSend_ReliableNoNagle
                : Constants.k_nSteamNetworkingSend_UnreliableNoNagle;
            GCHandle pinned = GCHandle.Alloc(payload, GCHandleType.Pinned);
            try
            {
                EResult result = SteamNetworkingSockets.SendMessageToConnection(
                    _connection, pinned.AddrOfPinnedObject(), (uint)length, flags, out long _);

                // A full send buffer drops an unreliable message, which is what unreliable means. Anything else — no
                // connection, or a reliable message that cannot be queued — is a connection that is gone.
                if (result != EResult.k_EResultOK
                    && !(result == EResult.k_EResultLimitExceeded && channel == NetChannel.Unreliable))
                {
                    Lost();
                }
            }
            finally
            {
                pinned.Free();
            }
        }

        public void Update(double nowSeconds)
        {
            while (_connected)
            {
                int count = SteamNetworkingSockets.ReceiveMessagesOnConnection(_connection, _received, ReceiveBatch);
                if (count < 0)
                {
                    Lost();
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    Take(_received[i]);
                }

                if (count < ReceiveBatch)
                {
                    return;
                }
            }
        }

        public bool TryReceive(out NetEvent netEvent)
        {
            if (_inbox.Count > 0)
            {
                netEvent = _inbox.Dequeue();
                return true;
            }

            netEvent = default;
            return false;
        }

        public void Disconnect(NetPeer peer)
        {
            if (!_connected || peer != _peer)
            {
                return;
            }

            // Linger, so a goodbye already queued (the session's Bye) still reaches them.
            SteamNetworkingSockets.CloseConnection(
                _connection, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "left", true);
            _connection = HSteamNetConnection.Invalid;
            Lost();
        }

        public void Dispose()
        {
            if (_connection != HSteamNetConnection.Invalid)
            {
                SteamNetworkingSockets.CloseConnection(
                    _connection, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "left", true);
                _connection = HSteamNetConnection.Invalid;
            }

            if (_listen != HSteamListenSocket.Invalid)
            {
                SteamNetworkingSockets.CloseListenSocket(_listen);
                _listen = HSteamListenSocket.Invalid;
            }

            _connected = false;
            _peer = NetPeer.None;
            _statusChanged?.Dispose();
            _statusChanged = null;
        }

        internal static bool TryParseSteamId(string address, out ulong steamId) =>
            ulong.TryParse(address, NumberStyles.None, CultureInfo.InvariantCulture, out steamId) && steamId != 0UL;

        private void Watch()
        {
            if (_statusChanged == null)
            {
                _statusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnStatusChanged);
            }
        }

        /// <summary>
        /// Every Steam connection's news reaches every transport, so each keeps to its own: a knock on its listen socket, and
        /// changes to its one connection.
        /// </summary>
        private void OnStatusChanged(SteamNetConnectionStatusChangedCallback_t change)
        {
            HSteamNetConnection connection = change.m_hConn;
            SteamNetConnectionInfo_t info = change.m_info;
            switch (info.m_eState)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                    if (_listen == HSteamListenSocket.Invalid || info.m_hListenSocket != _listen)
                    {
                        return;
                    }

                    if (_connection != HSteamNetConnection.Invalid)
                    {
                        // D11: one guest. A second caller is turned away at the door.
                        SteamNetworkingSockets.CloseConnection(
                            connection, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "The game is full.", false);
                        return;
                    }

                    if (SteamNetworkingSockets.AcceptConnection(connection) == EResult.k_EResultOK)
                    {
                        _connection = connection;
                        _peer = new NetPeer(info.m_identityRemote.GetSteamID64());
                    }

                    return;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    if (connection == _connection && !_connected)
                    {
                        _connected = true;
                        _inbox.Enqueue(NetEvent.Connected(_peer));
                    }

                    return;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    if (connection == _connection)
                    {
                        Lost();
                    }

                    return;
            }
        }

        private void Take(IntPtr pointer)
        {
            SteamNetworkingMessage_t message = SteamNetworkingMessage_t.FromIntPtr(pointer);
            try
            {
                if (message.m_cbSize <= 0 || message.m_cbSize > NetProtocol.MaxMessageBytes)
                {
                    return;
                }

                var payload = new byte[message.m_cbSize];
                Marshal.Copy(message.m_pData, payload, 0, message.m_cbSize);

                // Received messages keep only the reliable bit of the flags they were sent with.
                NetChannel channel = (message.m_nFlags & Constants.k_nSteamNetworkingSend_Reliable) != 0
                    ? NetChannel.Reliable
                    : NetChannel.Unreliable;
                _inbox.Enqueue(NetEvent.Data(_peer, channel, payload));
            }
            finally
            {
                SteamNetworkingMessage_t.Release(pointer);
            }
        }

        /// <summary>The connection is gone: close what is left of it and tell the session once. A host keeps listening.</summary>
        private void Lost()
        {
            NetPeer lost = _peer;
            bool wasConnected = _connected || _connection != HSteamNetConnection.Invalid;
            if (_connection != HSteamNetConnection.Invalid)
            {
                SteamNetworkingSockets.CloseConnection(_connection, 0, null, false);
            }

            _connection = HSteamNetConnection.Invalid;
            _connected = false;
            _peer = NetPeer.None;
            if (wasConnected && !lost.IsNone)
            {
                _inbox.Enqueue(NetEvent.Disconnected(lost));
            }
        }

        private static SteamNetworkingConfigValue_t[] Options() => new[]
        {
            Int32(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin, SendRateMinBytes),
            Int32(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax, SendRateMaxBytes),
            Int32(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutConnected, TimeoutConnectedMs),
        };

        private static SteamNetworkingConfigValue_t Int32(ESteamNetworkingConfigValue key, int value) =>
            new SteamNetworkingConfigValue_t
            {
                m_eValue = key,
                m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                m_val = new SteamNetworkingConfigValue_t.OptionValue { m_int32 = value },
            };
    }
}
```

**Checked against the package source at 2025.164.1:**
- The signatures of `CreateListenSocketP2P`, `ConnectP2P`, `AcceptConnection`, `CloseConnection`, `CloseListenSocket`, `SendMessageToConnection`, `ReceiveMessagesOnConnection` (it throws unless the array's length equals the count asked for, which `ReceiveBatch` guarantees) and `CreateSocketPair`.
- The static `SteamNetworkingMessage_t.Release(IntPtr)` and `FromIntPtr`.
- `SteamNetworkingIdentity.SetSteamID64`, `SetLocalHost` and `GetSteamID64`.
- `Constants.k_nSteamNetworkingSend_*`, and `HSteamListenSocket.Invalid` / `HSteamNetConnection.Invalid`.
- The `ESteamNetworkingConfigValue` keys and the `ESteamNetworkingConnectionState` values.

If a name has moved, fix the call and say so in the `DONE`.

In `Assets/_BattleBomb/Platform/Steam/SteamPlatformServices.cs`:

(a) Replace

```csharp
    public sealed class SteamPlatformServices : IPlatformServices, IPlayerIdentity
```

with

```csharp
    public sealed class SteamPlatformServices : IPlatformServices, IPlayerIdentity, ILobbyService
```

(b) Replace

```csharp
        public ILobbyService Lobby => _offline.Lobby;
```

with

```csharp
        public ILobbyService Lobby => _running ? this : _offline.Lobby;
```

(c) After the property `public string DisplayName => _running ? SteamFriends.GetPersonaName() : "Player";` add:

```csharp

        // ILobbyService — Steam's own network (Task 110). Friends' join requests are Task 111's.

        public bool CanHost => _running;

        public Net.INetTransport CreateTransport() => _running ? new SteamP2PTransport() : null;

        public bool HasJoinRequest => false;

        public bool TryTakeJoinRequest(out string address)
        {
            address = null;
            return false;
        }
```

(d) In `Initialise`, after the line `_running = true;` add:

```csharp
            // The relay's routes are measured in the background from now, so the first friend to join does not wait for it.
            SteamNetworkingUtils.InitRelayNetworkAccess();
```

- [ ] **Step 6: The Steam transport, tested against the real library**

Create `Assets/_BattleBomb/Tests/Steam/SteamTransportTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using BattleBomb.Core.Net;
using BattleBomb.Platform.Net;
using BattleBomb.Platform.Steam;
using NUnit.Framework;
using Steamworks;

namespace BattleBomb.Tests.Steam
{
    /// <summary>
    /// The Steam transport over Steam's in-process socket pair: the real library, the real message path, one PC. Needs the
    /// Steam client running; Ignored otherwise. Two homes over the relay are Michael's pass (Task 113).
    /// </summary>
    public sealed class SteamTransportTests
    {
        private SteamPlatformServices _steam;
        private bool _started;

        [OneTimeSetUp]
        public void StartSteam()
        {
            _steam = new SteamPlatformServices();
            _started = _steam.Initialise();
        }

        [OneTimeTearDown]
        public void StopSteam() => _steam.Shutdown();

        [Test]
        public void An_address_that_is_not_a_Steam_ID_is_refused_before_Steam_is_asked()
        {
            Assert.That(SteamP2PTransport.TryParseSteamId("76561197960287930", out ulong id), Is.True);
            Assert.That(id, Is.EqualTo(76561197960287930UL));
            Assert.That(SteamP2PTransport.TryParseSteamId("127.0.0.1:7777", out _), Is.False);
            Assert.That(SteamP2PTransport.TryParseSteamId("0", out _), Is.False);
            Assert.That(SteamP2PTransport.TryParseSteamId(null, out _), Is.False);
            Assert.Throws<ArgumentException>(() => new SteamP2PTransport().Connect("not a steam id"));
        }

        [Test]
        public void Reliable_and_unreliable_messages_cross_on_their_own_channels()
        {
            SteamP2PTransport host = Pair(out SteamP2PTransport guest);
            try
            {
                guest.Send(guest.Peer, NetChannel.Reliable, new byte[] { 1, 2, 3 }, 3);
                guest.Send(guest.Peer, NetChannel.Unreliable, new byte[] { 9, 8, 7, 6 }, 2);

                List<NetEvent> events = Pump(host, got => Data(got).Count >= 2);
                List<NetEvent> data = Data(events);
                Assert.That(data.Count, Is.EqualTo(2), "Not everything arrived.");
                Assert.That(data[0].Channel, Is.EqualTo(NetChannel.Reliable));
                Assert.That(data[0].Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
                Assert.That(data[1].Channel, Is.EqualTo(NetChannel.Unreliable));
                Assert.That(data[1].Payload, Is.EqualTo(new byte[] { 9, 8 }), "The length sent is the length that arrives.");
            }
            finally
            {
                host.Dispose();
                guest.Dispose();
            }
        }

        [Test]
        public void A_message_at_the_frame_limit_arrives_whole()
        {
            SteamP2PTransport host = Pair(out SteamP2PTransport guest);
            try
            {
                var big = new byte[NetProtocol.MaxMessageBytes];
                for (int i = 0; i < big.Length; i++)
                {
                    big[i] = (byte)(i * 31);
                }

                guest.Send(guest.Peer, NetChannel.Reliable, big, big.Length);
                List<NetEvent> data = Data(Pump(host, got => Data(got).Count >= 1));
                Assert.That(data.Count, Is.EqualTo(1));
                Assert.That(data[0].Payload, Is.EqualTo(big));
            }
            finally
            {
                host.Dispose();
                guest.Dispose();
            }
        }

        [Test]
        public void Leaving_is_heard_on_the_other_side()
        {
            SteamP2PTransport host = Pair(out SteamP2PTransport guest);
            try
            {
                guest.Disconnect(guest.Peer);
                List<NetEvent> events = Pump(host, got => got.Exists(e => e.Kind == NetEventKind.Disconnected));
                Assert.That(events.Exists(e => e.Kind == NetEventKind.Disconnected), Is.True,
                    "A peer that left was not an event — the session would wait out ten seconds of silence instead.");
                Assert.That(host.Peer.IsNone, Is.True);
            }
            finally
            {
                host.Dispose();
                guest.Dispose();
            }
        }

        private SteamP2PTransport Pair(out SteamP2PTransport guest)
        {
            if (!_started)
            {
                Assert.Ignore("The Steam client is not running on this PC.");
            }

            Assert.That(SteamP2PTransport.TryCreatePair(out SteamP2PTransport host, out guest), Is.True,
                "Steam would not make a socket pair.");
            return host;
        }

        private static List<NetEvent> Pump(SteamP2PTransport transport, Func<List<NetEvent>, bool> done)
        {
            var events = new List<NetEvent>();
            for (int i = 0; i < 400 && !done(events); i++)
            {
                SteamAPI.RunCallbacks();
                transport.Update(0.0);
                while (transport.TryReceive(out NetEvent e))
                {
                    events.Add(e);
                }

                if (!done(events))
                {
                    Thread.Sleep(5);
                }
            }

            return events;
        }

        private static List<NetEvent> Data(List<NetEvent> events) => events.FindAll(e => e.Kind == NetEventKind.Data);
    }
}
```

If `Reliable_and_unreliable…` fails only on the channel (both arrive, but with the wrong flag), the in-process pair is not preserving the reliable bit. Pass `true` for `bUseNetworkLoopback` in `TryCreatePair`: real loopback packets carry their flags. Say so in the `DONE`.

- [ ] **Step 7: Run everything**

`recompile`. Then:
- `run_tests` mode `EditMode`: baseline + 25, all passing, of which up to 4 are Ignored if Steam is closed (`SteamSessionTests` 1, `SteamTransportTests` 3).
  - `An_address…` runs either way. It never touches Steam: the parse fails before `Watch`.
  - Name in the `DONE` which Steam tests ran and which were Ignored.
- `run_tests` mode `PlayMode` (async): baseline + 4.

- [ ] **Step 8: Commit (`DONE`)**

Paths:
- `Assets/_BattleBomb/Platform/Net/INetTransport.cs`, `Assets/_BattleBomb/Platform/Net/LocalSocketTransport.cs`, `Assets/_BattleBomb/Platform/Net/LoopbackTransport.cs`, `Assets/_BattleBomb/Platform/Net/LagSimulator.cs`
- `Assets/_BattleBomb/Gameplay/Net/NetSession.cs`
- `Assets/_BattleBomb/Platform/Steam/SteamP2PTransport.cs` (+ `.meta`), `Assets/_BattleBomb/Platform/Steam/SteamPlatformServices.cs`
- `Assets/_BattleBomb/Tests/PlayMode/PlaybackTransport.cs`, `Assets/_BattleBomb/Tests/PlayMode/AnsweringHost.cs` (and any other implementer found)
- `Assets/_BattleBomb/Tests/EditMode/Net/LocalSocketTransportTests.cs`, `Assets/_BattleBomb/Tests/EditMode/Net/LoopbackTransportTests.cs`
- `Assets/_BattleBomb/Tests/Steam/SteamTransportTests.cs` (+ `.meta`)

Subject: `110: the Steam peer-to-peer transport`. Body: friends in two homes need Steam's relay, so ISteamNetworkingSockets joins the transports behind the seam, with its send rate set above Task 96's measured snapshot rate so no delta compression is needed. Listening now reports why it failed through the transport, so the session no longer catches socket exceptions or blames port 7777 for everything.

---

### Task 111: Friends join through Steam

**Why.** D59: solo games are open to platform friends by default. Friends see "Join game", the host can invite from the overlay, and a setting closes it. Plan 2 built the setting and a transport slot, `NetSession.FriendsTransport`, that Plan 3 fills. This task makes Steam's friends list find the game and makes the guest's click reach it.

**How Steam carries it, without a lobby object:**
- **The host is joinable while it listens with a free seat.** The Steam transport sets the rich-presence key `connect` to `+battlebomb_join <host Steam ID>`, and clears it the moment a guest takes the seat or the game stops listening.
- **What friends see.** While the key is set, Steam's friends list shows "Join Game" on the host, and the overlay's "Invite to Game" sends the same string.
- **Accepting.** Inside a running game it arrives as `GameRichPresenceJoinRequested_t`. From outside, Steam starts the game with the string on its command line (on BattleBomb's own app; on Spacewar it would start Spacewar, which is why the how-to-run sheet says to start the game first).
- **Where the join waits.** Either way it waits in `ILobbyService` until the front door takes it. A join accepted in the middle of the joiner's own game waits there, and the banner says so; nobody's game is thrown away by a click.

**The fallback for the first real test.** The development panel shows this machine's Steam name and ID, and has a "Join" box that takes a friend's Steam ID. If the friends list misbehaves on Spacewar, the collaborator types Michael's ID instead. It is development-only, like the rest of the panel.

**The development panel's "Friends" switch now means:** off → the platform's network (Steam, when running); on → the local socket, for two editors. Plan 2's "off → nothing" becomes "off → Steam". With Steam closed it is still nothing, so every Plan 2 test and check is unchanged.

**Files:**
- Modify:
  - `Assets/_BattleBomb/Platform/PlatformRegistry.cs`
  - `Platform/Steam/SteamPlatformServices.cs`, `Platform/Steam/SteamP2PTransport.cs`
  - `Gameplay/Net/NetSession.cs` (LF)
  - `UI/Frontend/FrontendFlow.cs` (CRLF), `UI/Combat/NetBanner.cs` (LF), `UI/Debug/NetDevOverlay.cs` (LF)
- Create: `Assets/_BattleBomb/Platform/Steam/JoinAddress.cs`
- Test (create):
  - `Tests/EditMode/Net/FriendsTransportTests.cs`
  - `Tests/PlayMode/FriendJoinSmokeTests.cs`
  - `Tests/Steam/JoinAddressTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `Assets/_BattleBomb/Tests/EditMode/Net/FriendsTransportTests.cs`:

```csharp
using System;
using BattleBomb.Gameplay.Net;
using BattleBomb.Platform;
using BattleBomb.Platform.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>Where "open to friends" gets its transport (D59, Task 111): the platform's network unless the development
    /// panel puts the local socket in its place.</summary>
    public sealed class FriendsTransportTests
    {
        [SetUp]
        public void Offline()
        {
            PlatformRegistry.ResetForTests();
            NetSession.FriendsTransport = null;
        }

        [TearDown]
        public void Forget()
        {
            NetSession.FriendsTransport = null;
            PlatformRegistry.ResetForTests();
        }

        [Test]
        public void With_no_platform_network_nothing_opens_to_friends()
        {
            PlatformRegistry.Use(new NullPlatformServices());
            Assert.That(NetSession.FriendsTransport, Is.Null, "Rule 6: without Steam nothing opens, and nothing is made to try.");
            Assert.That(NetSession.FriendJoinWaiting, Is.False);
        }

        [Test]
        public void The_platforms_network_is_the_default_and_the_panel_swaps_the_local_socket_in_and_out()
        {
            var lobby = new FakeLobby { Make = NewLoopback };
            PlatformRegistry.Use(new PlatformRegistryTests.FakePlatform(true, lobby: lobby));

            Assert.That(NetSession.FriendsTransport, Is.Not.Null);
            Assert.That(NetSession.FriendsTransport(), Is.InstanceOf<LoopbackTransport>(), "Not the platform's transport.");

            NetSession.UseLocalFriends(true, 0);
            INetTransport local = NetSession.FriendsTransport();
            Assert.That(local, Is.InstanceOf<LocalSocketTransport>(), "The panel's local socket did not take the slot.");
            local.Dispose();

            NetSession.UseLocalFriends(false, 0);
            Assert.That(NetSession.FriendsTransport(), Is.InstanceOf<LoopbackTransport>(),
                "Switching the panel off must hand friends back to the platform, not to nothing.");
        }

        [Test]
        public void A_friends_game_waiting_is_seen_but_never_started_by_drawing_code()
        {
            var lobby = new FakeLobby { Waiting = "76561190000000002" };
            PlatformRegistry.Register(() => new PlatformRegistryTests.FakePlatform(true, lobby: lobby));

            Assert.That(NetSession.FriendJoinWaiting, Is.False,
                "The banner asked before anything had started the platform — drawing code must never be what starts it.");

            Assert.That(PlatformRegistry.Current, Is.Not.Null);
            Assert.That(NetSession.FriendJoinWaiting, Is.True);
        }

        private static INetTransport NewLoopback()
        {
            LoopbackTransport.CreatePair(out LoopbackTransport one, out LoopbackTransport _);
            return one;
        }

        internal sealed class FakeLobby : ILobbyService
        {
            internal string Waiting;
            internal Func<INetTransport> Make;

            public bool CanHost => Make != null;

            public INetTransport CreateTransport() => Make?.Invoke();

            public bool HasJoinRequest => Waiting != null;

            public bool TryTakeJoinRequest(out string address)
            {
                address = Waiting;
                Waiting = null;
                return address != null;
            }
        }
    }
}
```

Create `Assets/_BattleBomb/Tests/PlayMode/FriendJoinSmokeTests.cs`:

```csharp
using System;
using System.Collections;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Session;
using BattleBomb.Platform;
using BattleBomb.Platform.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// A friend's game that waits to be joined (D59, Task 111) — an accepted invite, a click on "Join game" — is joined at the
    /// front door, from this machine's own save; and never taken from under a guest already playing with this machine.
    /// </summary>
    public sealed class FriendJoinSmokeTests
    {
        private WaitingLobby _lobby;

        [UnitySetUp]
        public IEnumerator AtTheFrontDoor()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                UnityEngine.Object.Destroy(stale.gameObject);
                yield return null;
            }

            _lobby = new WaitingLobby();
            PlatformRegistry.Use(new LobbyPlatform(_lobby));

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "friend-join";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            GameSession session = GameSession.Find();
            if (session != null)
            {
                UnityEngine.Object.Destroy(session.gameObject);
            }

            PlatformRegistry.Use(new NullPlatformServices());
            yield return null;
        }

        [UnityTest]
        public IEnumerator A_friends_game_waiting_at_the_front_door_is_joined_there()
        {
            LoopbackTransport.CreatePair(out LoopbackTransport hostSide, out LoopbackTransport guestSide);
            hostSide.Listen();
            _lobby.Make = () => guestSide;
            _lobby.Waiting = "loopback";

            for (int i = 0; i < 120 && _lobby.Waiting != null; i++)
            {
                yield return null;
            }

            NetSession net = GameSession.Find().Net;
            Assert.That(_lobby.Waiting, Is.Null, "The front door never took the waiting join.");
            Assert.That(net, Is.Not.Null);
            Assert.That(net.Role, Is.EqualTo(NetRole.Guest), "Taking a friend's join must join their game.");
            hostSide.Dispose();
        }

        [UnityTest]
        public IEnumerator A_waiting_join_is_never_taken_from_under_our_own_guest()
        {
            LoopbackTransport.CreatePair(out LoopbackTransport hostSide, out LoopbackTransport guestSide);
            NetSession net = NetSession.FindOrCreate();
            net.Host(hostSide);
            HeadlessGuest guest = HeadlessGuest.Join(guestSide);
            for (int i = 0; i < 300 && !net.IsConnected; i++)
            {
                yield return null;
            }

            Assert.That(net.IsConnected, Is.True, "The headless guest never joined.");

            _lobby.Make = () =>
            {
                LoopbackTransport.CreatePair(out LoopbackTransport other, out LoopbackTransport _);
                return other;
            };
            _lobby.Waiting = "76561190000000002";
            for (int i = 0; i < 60; i++)
            {
                yield return null;
            }

            Assert.That(net.Role, Is.EqualTo(NetRole.Host), "Our own guest was dropped for someone else's game.");
            Assert.That(_lobby.HasJoinRequest, Is.True, "The join was spent instead of left waiting.");
            UnityEngine.Object.Destroy(guest.gameObject);
        }

        private sealed class WaitingLobby : ILobbyService
        {
            internal string Waiting;
            internal Func<INetTransport> Make;

            public bool CanHost => Make != null;

            public INetTransport CreateTransport() => Make?.Invoke();

            public bool HasJoinRequest => Waiting != null;

            public bool TryTakeJoinRequest(out string address)
            {
                address = Waiting;
                Waiting = null;
                return address != null;
            }
        }

        private sealed class LobbyPlatform : IPlatformServices
        {
            private readonly NullPlatformServices _offline = new NullPlatformServices();

            internal LobbyPlatform(ILobbyService lobby) => Lobby = lobby;

            public bool IsAvailable => true;

            public IAchievements Achievements => _offline.Achievements;

            public IPlayerIdentity Identity => _offline.Identity;

            public ILeaderboards Leaderboards => _offline.Leaderboards;

            public ISaveStore Saves => _offline.Saves;

            public ILobbyService Lobby { get; }

            public bool Initialise() => true;

            public void Shutdown()
            {
            }
        }
    }
}
```

`HeadlessGuest.Join(INetTransport)` is as Plan 2's Task 104 left it. If it is not a `MonoBehaviour` with a `gameObject`, use whatever teardown `OnlineJoinSmokeTests` uses for it.

Create `Assets/_BattleBomb/Tests/Steam/JoinAddressTests.cs`:

```csharp
using BattleBomb.Platform.Steam;
using NUnit.Framework;

namespace BattleBomb.Tests.Steam
{
    /// <summary>The connect string a Steam friend's "Join Game" carries (Task 111). Pure strings: no Steam needed.</summary>
    public sealed class JoinAddressTests
    {
        [Test]
        public void What_the_host_advertises_is_what_the_guest_reads()
        {
            string connect = JoinAddress.Format(76561197960287930UL);
            Assert.That(connect, Is.EqualTo("+battlebomb_join 76561197960287930"));
            Assert.That(JoinAddress.TryParse(connect, out string address), Is.True);
            Assert.That(address, Is.EqualTo("76561197960287930"));
        }

        [Test]
        public void A_join_accepted_while_the_game_was_closed_is_found_on_the_command_line()
        {
            string[] args = { "BattleBomb.exe", "-screen-fullscreen", "0", "+battlebomb_join", "76561197960287930", "-foo" };
            Assert.That(JoinAddress.TryParse(args, out string address), Is.True);
            Assert.That(address, Is.EqualTo("76561197960287930"));
        }

        [Test]
        public void Anything_else_is_not_a_join()
        {
            Assert.That(JoinAddress.TryParse("+connect_lobby 109775241000000000", out _), Is.False);
            Assert.That(JoinAddress.TryParse("+battlebomb_join", out _), Is.False);
            Assert.That(JoinAddress.TryParse("+battlebomb_join not-an-id", out _), Is.False);
            Assert.That(JoinAddress.TryParse(string.Empty, out _), Is.False);
            Assert.That(JoinAddress.TryParse((string)null, out _), Is.False);
            Assert.That(JoinAddress.TryParse(new[] { "BattleBomb.exe" }, out _), Is.False);
        }
    }
}
```

- [ ] **Step 2: Run them and see them fail**

`recompile`. Expected: compile errors, because `NetSession.FriendJoinWaiting`, `JoinAddress` and `PlatformRegistry.Started` do not exist.

- [ ] **Step 3: Drawing code can look without starting**

In `Assets/_BattleBomb/Platform/PlatformRegistry.cs`, after the line `public static IPlatformServices Current => _current ?? (_current = Start(_factory));` add:

```csharp

        /// <summary>The platform if something has already brought it up, or null. For drawing code (a banner, the development
        /// panel), which must never be what starts it: a banner drawn before a test pins the platform offline would otherwise
        /// start Steam under the test.</summary>
        public static IPlatformServices Started => _current;
```

- [ ] **Step 4: The connect string**

Create `Assets/_BattleBomb/Platform/Steam/JoinAddress.cs`:

```csharp
using System;
using System.Globalization;

namespace BattleBomb.Platform.Steam
{
    /// <summary>
    /// The rich-presence connect string that makes a game joinable by Steam friends (D59, Task 111): <c>+battlebomb_join
    /// 76561198…</c>. While it is set Steam shows friends "Join Game", and the overlay's "Invite to Game" sends it. Accepted in
    /// a running game it arrives as a callback; accepted from outside, Steam starts the game with it on the command line. The
    /// address inside is the host's Steam ID — what <see cref="SteamP2PTransport.Connect"/> takes.
    /// </summary>
    internal static class JoinAddress
    {
        internal const string Command = "+battlebomb_join";

        internal static string Format(ulong hostSteamId) =>
            Command + " " + hostSteamId.ToString(CultureInfo.InvariantCulture);

        internal static bool TryParse(string connect, out string address)
        {
            address = null;
            return !string.IsNullOrWhiteSpace(connect)
                && TryParse(connect.Split((char[])null, StringSplitOptions.RemoveEmptyEntries), out address);
        }

        /// <summary>A command line: the join follows <see cref="Command"/> as the next argument.</summary>
        internal static bool TryParse(string[] args, out string address)
        {
            address = null;
            if (args == null)
            {
                return false;
            }

            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] == Command && SteamP2PTransport.TryParseSteamId(args[i + 1], out ulong id))
                {
                    address = id.ToString(CultureInfo.InvariantCulture);
                    return true;
                }
            }

            return false;
        }
    }
}
```

- [ ] **Step 5: The transport says when the game is joinable**

In `Assets/_BattleBomb/Platform/Steam/SteamP2PTransport.cs`:

(a) Replace

```csharp
        private NetPeer _peer;
        private bool _connected;

        public SteamP2PTransport()
        {
        }
```

with

```csharp
        private NetPeer _peer;
        private bool _connected;

        /// <summary>Told true while this transport listens with its one seat free, false otherwise — how friends see "Join
        /// Game" (Task 111). Null for a transport that only joins.</summary>
        private readonly Action<bool> _joinable;

        public SteamP2PTransport(Action<bool> joinable = null)
        {
            _joinable = joinable;
        }
```

(b) In `Listen`, replace

```csharp
            return _listen == HSteamListenSocket.Invalid ? "Steam would not open a listening socket." : null;
```

with

```csharp
            if (_listen == HSteamListenSocket.Invalid)
            {
                return "Steam would not open a listening socket.";
            }

            Advertise();
            return null;
```

(c) In `OnStatusChanged`, replace

```csharp
                    if (connection == _connection && !_connected)
                    {
                        _connected = true;
                        _inbox.Enqueue(NetEvent.Connected(_peer));
                    }
```

with

```csharp
                    if (connection == _connection && !_connected)
                    {
                        _connected = true;
                        _inbox.Enqueue(NetEvent.Connected(_peer));
                        Advertise();
                    }
```

(d) In `Lost`, replace

```csharp
            if (wasConnected && !lost.IsNone)
            {
                _inbox.Enqueue(NetEvent.Disconnected(lost));
            }
        }
```

with

```csharp
            if (wasConnected && !lost.IsNone)
            {
                _inbox.Enqueue(NetEvent.Disconnected(lost));
            }

            // A host that lost its guest is still listening, with the seat free again (D59: a friend can drop in).
            Advertise();
        }

        private void Advertise() =>
            _joinable?.Invoke(_listen != HSteamListenSocket.Invalid && _connection == HSteamNetConnection.Invalid);
```

(e) In `Dispose`, replace

```csharp
            _connected = false;
            _peer = NetPeer.None;
            _statusChanged?.Dispose();
            _statusChanged = null;
        }
```

with

```csharp
            _connected = false;
            _peer = NetPeer.None;
            _statusChanged?.Dispose();
            _statusChanged = null;
            Advertise();
        }
```

`Advertise` is false the moment a guest's connection is accepted (`_connection` is set while it is still connecting). A second friend clicking "Join Game" in that instant is turned away by the one-seat rule anyway.

- [ ] **Step 6: Steam hears friends**

In `Assets/_BattleBomb/Platform/Steam/SteamPlatformServices.cs`:

(a) Replace

```csharp
        private SteamCallbackPump _pump;
        private bool _running;
```

with

```csharp
        private SteamCallbackPump _pump;
        private bool _running;
        private Callback<GameRichPresenceJoinRequested_t> _joinRequested;

        /// <summary>A friend's game this player asked to join (D59) — "Join Game", an accepted invite, or the command line —
        /// waiting for the front door.</summary>
        private string _pendingJoin;
```

(b) In `Initialise`, after the line `SteamNetworkingUtils.InitRelayNetworkAccess();` add:

```csharp
            _joinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnJoinRequested);
            if (JoinAddress.TryParse(System.Environment.GetCommandLineArgs(), out string launchedToJoin))
            {
                // Steam started the game to join a friend (an invite accepted while it was closed).
                _pendingJoin = launchedToJoin;
            }
```

(c) In `Shutdown`, replace

```csharp
            SteamAPI.Shutdown();
            PlatformRegistry.Release(this);
```

with

```csharp
            _joinRequested?.Dispose();
            _joinRequested = null;
            _pendingJoin = null;
            SteamFriends.ClearRichPresence();
            SteamAPI.Shutdown();
            PlatformRegistry.Release(this);
```

(d) Replace Task 110's

```csharp
        public Net.INetTransport CreateTransport() => _running ? new SteamP2PTransport() : null;

        public bool HasJoinRequest => false;

        public bool TryTakeJoinRequest(out string address)
        {
            address = null;
            return false;
        }
```

with

```csharp
        public Net.INetTransport CreateTransport() => _running ? new SteamP2PTransport(SetJoinable) : null;

        public bool HasJoinRequest => _pendingJoin != null;

        public bool TryTakeJoinRequest(out string address)
        {
            address = _pendingJoin;
            _pendingJoin = null;
            return address != null;
        }

        /// <summary>Friends see "Join Game" while this is set (D59). An empty value deletes the key.</summary>
        private void SetJoinable(bool joinable)
        {
            if (_running)
            {
                SteamFriends.SetRichPresence("connect", joinable ? JoinAddress.Format(SteamUser.GetSteamID().m_SteamID) : string.Empty);
            }
        }

        private void OnJoinRequested(GameRichPresenceJoinRequested_t request)
        {
            if (JoinAddress.TryParse(request.m_rgchConnect, out string address) && address != UserId)
            {
                _pendingJoin = address;
            }
        }
```

The comment on the lobby block (Task 110's `// ILobbyService — Steam's own network (Task 110). Friends' join requests are Task 111's.`) becomes `// ILobbyService — Steam's own network (Task 110) and friends' joins (Task 111).`.

- [ ] **Step 7: The session takes friends' joins**

In `Assets/_BattleBomb/Gameplay/Net/NetSession.cs`:

(a) Add `using BattleBomb.Platform;` to the usings.

(b) Replace Task 104's

```csharp
        /// <summary>Where "open to friends" gets its transport (D59): Steam's lobby from Plan 3, the local socket from
        /// the development panel, and nothing in a build with neither — so nothing opens (rule 6). A factory slot,
        /// like the planned PlatformRegistry, holding no game state.</summary>
        public static Func<INetTransport> FriendsTransport { get; set; }

        /// <summary>Development: solo games open on this machine's local socket (another editor can drop in), or not.</summary>
```

with

```csharp
        /// <summary>Where "open to friends" gets its transport (D59): the platform's own network — Steam's (Task 110) — unless
        /// the development panel has put the local socket in its place; nothing in a build with neither, so nothing opens
        /// (rule 6). Set to null, it goes back to the platform's. A factory slot, holding no game state.</summary>
        public static Func<INetTransport> FriendsTransport
        {
            get => _friendsTransport ?? PlatformFriends();
            set => _friendsTransport = value;
        }

        private static Func<INetTransport> _friendsTransport;

        private static Func<INetTransport> PlatformFriends()
        {
            ILobbyService lobby = PlatformRegistry.Current.Lobby;
            return lobby.CanHost ? lobby.CreateTransport : (Func<INetTransport>)null;
        }

        /// <summary>Development: solo games open on this machine's local socket (another editor can drop in), or — switched
        /// off — on the platform's network again.</summary>
```

(c) After the method `OpenToFriends` — whose last lines are

```csharp
            else if (!open && Role == NetRole.Host && Peer.IsNone)
            {
                Close();
            }
        }
```

— add:

```csharp

        /// <summary>Joins a platform friend's game (D59) over the platform's own network, from this machine's own save.</summary>
        public void JoinFriend(string address)
        {
            INetTransport transport = PlatformRegistry.Current.Lobby.CreateTransport();
            if (transport == null)
            {
                Status = "There is no online service to join friends through.";
                return;
            }

            Join(transport, address);
        }

        /// <summary>
        /// The front door's hand-off (D59, Task 111): a friend's game that waits to be joined — an accepted invite, a click on
        /// "Join Game" — is joined now, unless this machine is already a guest or has a guest of its own, in which case it keeps
        /// waiting. True when it joined.
        /// </summary>
        public static bool JoinWaitingFriend()
        {
            GameSession session = GameSession.Find();
            NetSession net = session != null ? session.Net : null;
            if (net != null && (net.Role == NetRole.Guest || !net.Peer.IsNone))
            {
                return false;
            }

            if (!PlatformRegistry.Current.Lobby.TryTakeJoinRequest(out string address))
            {
                return false;
            }

            FindOrCreate().JoinFriend(address);
            return true;
        }

        /// <summary>A friend's game waits to be joined — for the banner. Never starts the platform (drawing code asks it).</summary>
        public static bool FriendJoinWaiting
        {
            get
            {
                IPlatformServices platform = PlatformRegistry.Started;
                return platform != null && platform.Lobby.HasJoinRequest;
            }
        }

        /// <summary>Development: who this machine is on the platform — "Name (76561…)" — for the panel, so a friend can be
        /// told the ID to join by; null offline, or before anything has started the platform.</summary>
        public static string PlatformPlayer
        {
            get
            {
                IPlatformServices platform = PlatformRegistry.Started;
                return platform != null && platform.IsAvailable && platform.Identity.IsSignedIn
                    ? $"{platform.Identity.DisplayName} ({platform.Identity.UserId})"
                    : null;
            }
        }
```

- [ ] **Step 8: The front door takes it; the banner says it waits**

In `Assets/_BattleBomb/UI/Frontend/FrontendFlow.cs` (CRLF; keep it), in `Update`, replace Task 102's

```csharp
            NetSession net = _session.Net;
            if (net != null && net.Role == NetRole.Guest)
            {
                UpdateAsGuest(net);
                return;
            }
```

with

```csharp
            NetSession net = _session.Net;

            // A platform friend's game waits to be joined (D59, Task 111) — an accepted invite, a click on "Join Game". Taken
            // here at the front door, and never from under a guest already playing with this machine.
            if (NetSession.JoinWaitingFriend())
            {
                net = _session.Net;
            }

            if (net != null && net.Role == NetRole.Guest)
            {
                UpdateAsGuest(net);
                return;
            }
```

In `Assets/_BattleBomb/UI/Combat/NetBanner.cs`, in `Line`, replace

```csharp
                if (_guest != null && _guest.WaitingToAppear)
                {
                    return "Waiting for the host to reach a checkpoint room.";
                }

                return null;
```

with

```csharp
                if (_guest != null && _guest.WaitingToAppear)
                {
                    return "Waiting for the host to reach a checkpoint room.";
                }

                if (NetSession.FriendJoinWaiting)
                {
                    // Taken at the front door (Task 111); while it waits, the player is told how to get there.
                    return "A friend's game is waiting — leave this one to join it.";
                }

                return null;
```

- [ ] **Step 9: The development panel's Steam line and "Join"**

In `Assets/_BattleBomb/UI/Debug/NetDevOverlay.cs`:

(a) After the field `private int _lag;` add:

```csharp
        private string _friendId = string.Empty;
        private GUIStyle _field;
```

(b) Replace

```csharp
            if (_style == null || _style.fontSize != fontSize)
            {
                _style = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
            }
```

with

```csharp
            if (_style == null || _style.fontSize != fontSize)
            {
                _style = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
                _field = new GUIStyle(GUI.skin.textField) { fontSize = fontSize };
            }
```

(c) Replace Task 106's

```csharp
            var area = new Rect(Screen.width - width - 8f, Screen.height - row * 8f - 8f, width, row * 8f);
```

with

```csharp
            var area = new Rect(Screen.width - width - 8f, Screen.height - row * 10f - 8f, width, row * 10f);
```

(d) Replace Task 104's

```csharp
            if (!_folded && GUILayout.Button(_friends ? "Friends: local socket" : "Friends: off", _style))
```

with

```csharp
            string platformPlayer = NetSession.PlatformPlayer;
            if (!_folded && GUILayout.Button(
                _friends ? "Friends: local socket" : platformPlayer != null ? "Friends: platform" : "Friends: off", _style))
```

(e) Just before the line `GUILayout.EndArea();` add:

```csharp
            if (!_folded && platformPlayer != null)
            {
                // The first two-home test's fallback (Task 113): read this line out to the friend, who types the number below
                // if Steam's "Join Game" does not appear.
                GUILayout.Label(platformPlayer, _style);
                if (role == NetRole.Offline)
                {
                    GUILayout.BeginHorizontal();
                    _friendId = GUILayout.TextField(_friendId, _field);
                    if (GUILayout.Button("Join", _style) && _friendId.Trim().Length > 0)
                    {
                        NetSession.FindOrCreate().JoinFriend(_friendId.Trim());
                    }

                    GUILayout.EndHorizontal();
                }
            }

```

- [ ] **Step 10: Run them and see them pass**

`recompile`. Then:
- `run_tests` mode `EditMode`:
  - `FriendsTransportTests` 3/3;
  - `JoinAddressTests` 3/3 (they run without Steam: pure strings);
  - the whole suite at baseline + 31, of which up to 4 are Ignored without Steam.
- `run_tests` mode `PlayMode` (async):
  - `FriendJoinSmokeTests` 2/2;
  - the whole suite at baseline + 6.
  - Plan 2's `OnlineJoinSmokeTests` and `OnlineMenuSmokeTests` must be unchanged. They set `FriendsTransport` themselves and reset it to null, which now means "the platform's", and the suite pins the platform offline, so null is still nothing.

- [ ] **Step 11: A settled live check (no QUIET)**

With Steam running:
1. `editor_play` the Frontend scene.
2. Read `console`: `Steam: signed in as …`.
3. Take `capture_game_view`: the Net panel shows "Friends: platform" and a line with your Steam name and ID. The Join box is not typed into; that is Task 113.
4. Launch a solo game (Space, Space, Space at the title, character select and chapter select; Plan 2 made solo games open to friends).
5. Check presence with the bridge's `eval`: `Steamworks.SteamFriends.GetFriendRichPresence(Steamworks.SteamUser.GetSteamID(), "connect")`. It must be `+battlebomb_join <your ID>`.
6. `editor_stop`.

If `eval` cannot reach the Steamworks namespace, skip step 5 and say so: Task 113's pass shows the same thing from the friend's side.

- [ ] **Step 12: Commit (`DONE`)**

Paths:
- `Assets/_BattleBomb/Platform/PlatformRegistry.cs`
- `Assets/_BattleBomb/Platform/Steam/JoinAddress.cs` (+ `.meta`), `Assets/_BattleBomb/Platform/Steam/SteamP2PTransport.cs`, `Assets/_BattleBomb/Platform/Steam/SteamPlatformServices.cs`
- `Assets/_BattleBomb/Gameplay/Net/NetSession.cs`
- `Assets/_BattleBomb/UI/Frontend/FrontendFlow.cs`, `Assets/_BattleBomb/UI/Combat/NetBanner.cs`, `Assets/_BattleBomb/UI/Debug/NetDevOverlay.cs`
- `Assets/_BattleBomb/Tests/EditMode/Net/FriendsTransportTests.cs` (+ `.meta`)
- `Assets/_BattleBomb/Tests/PlayMode/FriendJoinSmokeTests.cs` (+ `.meta`)
- `Assets/_BattleBomb/Tests/Steam/JoinAddressTests.cs` (+ `.meta`)

Subject: `111: friends join through Steam`. Body: D59's "open to friends" now reaches Steam — while a game listens with a free seat, its rich presence carries a connect string, so friends see "Join Game" and the overlay can invite; a friend's click waits for the front door, and is never taken from under a guest already playing. No Steam lobby object is kept in step; the development panel shows the Steam ID and can join by it, the first real test's fallback.

---

### Task 112: Steam Cloud as the second save store

**Why.** D52 put saves behind `ISaveStore` so that Steam Cloud could arrive "with zero Core changes", and HANDOFF's Task 112 is that store. With it a player's progress follows them between PCs; Steam Deck players will expect it.

**When it is used.**
- **Only on BattleBomb's own app ID.** Spacewar's cloud space belongs to Valve's shared test app. Filling it with our saves, and sharing it with every developer testing on 480, would be wrong. So until Task 114 swaps the ID, saves stay in local files exactly as today. The store is built and unit-checked now, and switched on by the swap.
- **Not when the player has switched Steam Cloud off**, for their account or for this game.

**Files:**
- Create:
  - `Assets/_BattleBomb/Platform/SaveNames.cs`
  - `Platform/Steam/SteamCloudSaveStore.cs`
  - `Tests/EditMode/SaveNamesTests.cs`
- Modify: `Assets/_BattleBomb/Platform/FileSaveStore.cs` (CRLF), `Platform/Steam/SteamPlatformServices.cs`

- [ ] **Step 1: Write the failing tests**

Create `Assets/_BattleBomb/Tests/EditMode/SaveNamesTests.cs`:

```csharp
using BattleBomb.Platform;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>One naming rule for every save store (Task 112), so a save keeps its name between local files and the cloud.</summary>
    public sealed class SaveNamesTests
    {
        [Test]
        public void A_Steam_ID_is_its_own_file_name()
        {
            Assert.That(SaveNames.FileName("76561198000000000"), Is.EqualTo("76561198000000000.save"));
        }

        [Test]
        public void A_name_can_never_reach_out_of_its_folder()
        {
            Assert.That(SaveNames.FileName("../evil"), Is.EqualTo("___evil.save"));
            Assert.That(SaveNames.FileName("a/b\\c"), Is.EqualTo("a_b_c.save"));
        }

        [Test]
        public void No_name_is_the_local_save()
        {
            Assert.That(SaveNames.FileName(null), Is.EqualTo("local.save"));
            Assert.That(SaveNames.FileName(string.Empty), Is.EqualTo("local.save"));
        }
    }
}
```

- [ ] **Step 2: Run them and see them fail**

`recompile`. Expected: compile errors, because `SaveNames` does not exist.

- [ ] **Step 3: One rule for names**

Create `Assets/_BattleBomb/Platform/SaveNames.cs`:

```csharp
using System.Text;

namespace BattleBomb.Platform
{
    /// <summary>
    /// How a save's name becomes a file name, for every store (Task 112) — so a save keeps its name between local files and
    /// Steam Cloud. A null or empty name is "local" (the <see cref="ISaveStore"/> contract); every character that is not a
    /// letter, a digit, '-' or '_' becomes '_', so a name can never reach out of its folder. Two names that differ only in
    /// punctuation therefore share a file. Unreachable today (D51: one save per machine; Steam IDs are plain digits), but the
    /// next identity source that is not purely alphanumeric needs to know it.
    /// </summary>
    public static class SaveNames
    {
        public const string Extension = ".save";

        public static string FileName(string name) => Sanitise(name) + Extension;

        private static string Sanitise(string name)
        {
            string raw = string.IsNullOrEmpty(name) ? "local" : name;
            var builder = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                builder.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            }

            return builder.ToString();
        }
    }
}
```

In `Assets/_BattleBomb/Platform/FileSaveStore.cs` (CRLF; keep it):

(a) Replace

```csharp
        private const string Extension = ".save";
        private readonly string _directory;
```

with

```csharp
        private readonly string _directory;
```

(b) Replace

```csharp
            foreach (string file in Directory.GetFiles(_directory, "*" + Extension))
```

with

```csharp
            foreach (string file in Directory.GetFiles(_directory, "*" + SaveNames.Extension))
```

(c) Replace

```csharp
        private string PathFor(string name) => Path.Combine(_directory, Sanitise(name) + Extension);
```

with

```csharp
        private string PathFor(string name) => Path.Combine(_directory, SaveNames.FileName(name));
```

(d) Delete the private method `Sanitise` and its summary comment, from the line `/// <summary>A null or empty name is "local" (the <see cref="ISaveStore"/> contract).` down to that method's closing brace. The rule, and that comment's warning, now live in `SaveNames`.

**No behaviour changes.** The old and new rules give the same file for every name: null and empty both mean "local", and punctuation becomes `_`. The existing `FileSaveStore` tests confirm it.

- [ ] **Step 4: Run them and see them pass**

`recompile`, then `run_tests` mode `EditMode`, filters `SaveNamesTests` (3/3) and every `FileSaveStore` / `SaveService` fixture: unchanged.

- [ ] **Step 5: The cloud store**

Create `Assets/_BattleBomb/Platform/Steam/SteamCloudSaveStore.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using System.Text;
using Steamworks;
using UnityEngine;

namespace BattleBomb.Platform.Steam
{
    /// <summary>
    /// Saves in Steam Cloud (D52, Task 112): the same named text blobs as <see cref="FileSaveStore"/>, under the same file
    /// names (<see cref="SaveNames"/>), written through ISteamRemoteStorage so a player's progress follows them between PCs.
    /// A save that will not read is loud, never "no save" — the next autosave would write an empty one over it. A write the
    /// cloud refuses (the app's quota, the cloud briefly unavailable) is reported, and the next autosave tries again.
    /// </summary>
    public sealed class SteamCloudSaveStore : ISaveStore
    {
        public bool Exists(string name) => SteamRemoteStorage.FileExists(SaveNames.FileName(name));

        public bool TryRead(string name, out string text)
        {
            string file = SaveNames.FileName(name);
            if (!SteamRemoteStorage.FileExists(file))
            {
                text = null;
                return false;
            }

            int size = SteamRemoteStorage.GetFileSize(file);
            var bytes = new byte[size];
            int read = size > 0 ? SteamRemoteStorage.FileRead(file, bytes, size) : 0;
            if (read != size)
            {
                throw new IOException($"Steam Cloud returned {read} of {size} bytes of {file}.");
            }

            text = Encoding.UTF8.GetString(bytes, 0, read);
            return true;
        }

        public void Write(string name, string text)
        {
            string file = SaveNames.FileName(name);
            byte[] bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            if (!SteamRemoteStorage.FileWrite(file, bytes, bytes.Length))
            {
                Debug.LogError($"Steam Cloud refused to write {file} ({bytes.Length} bytes); the next autosave tries again.");
            }
        }

        public void Delete(string name) => SteamRemoteStorage.FileDelete(SaveNames.FileName(name));

        public IReadOnlyList<string> Names()
        {
            var names = new List<string>();
            int count = SteamRemoteStorage.GetFileCount();
            for (int i = 0; i < count; i++)
            {
                string file = SteamRemoteStorage.GetFileNameAndSize(i, out int _);
                if (file != null && file.EndsWith(SaveNames.Extension, System.StringComparison.Ordinal))
                {
                    names.Add(file.Substring(0, file.Length - SaveNames.Extension.Length));
                }
            }

            return names;
        }
    }
}
```

**Checked against the package source at 2025.164.1:**
- `FileWrite(string, byte[], int)`, `FileRead(string, byte[], int)`, `FileExists`, `FileDelete`, `GetFileSize`, `GetFileCount` and `GetFileNameAndSize(int, out int)`;
- `IsCloudEnabledForAccount` and `IsCloudEnabledForApp`.

In `Assets/_BattleBomb/Platform/Steam/SteamPlatformServices.cs`:

(a) Replace

```csharp
        public ISaveStore Saves => _offline.Saves;
```

with

```csharp
        /// <summary>Steam Cloud on BattleBomb's own app (Task 112); local files on Spacewar — whose cloud space is Valve's test
        /// app's, not ours — and whenever the player has switched Steam Cloud off.</summary>
        public ISaveStore Saves => _saves ?? (_saves = CloudSaves() ?? _offline.Saves);
```

(b) After the field `private string _pendingJoin;` add:

```csharp
        private ISaveStore _saves;
```

(c) In `Shutdown`, after the line `_pendingJoin = null;` add `_saves = null;`.

(d) After the method `OnJoinRequested` add:

```csharp

        /// <summary>
        /// The cloud store, when this is BattleBomb's own app and the player has the cloud on — or null. The first time, this
        /// player's save from the local files (under their Steam ID, or the machine's old "local" save) is copied up, so the
        /// swap from Spacewar to BattleBomb's own app (Task 114) never looks like starting over. A copy: the local file stays.
        /// </summary>
        private ISaveStore CloudSaves()
        {
            if (!_running || SteamUtils.GetAppID().m_AppId == SpacewarAppId
                || !SteamRemoteStorage.IsCloudEnabledForAccount() || !SteamRemoteStorage.IsCloudEnabledForApp())
            {
                return null;
            }

            var cloud = new SteamCloudSaveStore();
            ISaveStore local = _offline.Saves;
            string id = UserId;
            if (!cloud.Exists(id) && (local.TryRead(id, out string text) || local.TryRead(null, out text)))
            {
                cloud.Write(id, text);
            }

            return cloud;
        }
```

- [ ] **Step 6: The gates**

- `run_tests` mode `EditMode`: baseline + 34, all passing, of which up to 4 are Ignored without Steam.
- `run_tests` mode `PlayMode` (async): baseline + 6.

**No test writes to Steam Cloud, on purpose.**
- On Spacewar the store is never chosen.
- A test that wrote into Spacewar's shared cloud would be the very thing this task avoids.
- The cloud's first real write is Michael's check in Task 114, on BattleBomb's own app.

Say so in the `DONE`.

- [ ] **Step 7: Commit (`DONE`)**

Paths:
- `Assets/_BattleBomb/Platform/SaveNames.cs` (+ `.meta`), `Assets/_BattleBomb/Platform/FileSaveStore.cs`
- `Assets/_BattleBomb/Platform/Steam/SteamCloudSaveStore.cs` (+ `.meta`), `Assets/_BattleBomb/Platform/Steam/SteamPlatformServices.cs`
- `Assets/_BattleBomb/Tests/EditMode/SaveNamesTests.cs` (+ `.meta`)

Subject: `112: Steam Cloud as the second save store`. Body: D52's save seam gets its second store, so a player's progress follows them between PCs; it switches on only on BattleBomb's own app ID (Task 114) — Spacewar's cloud is Valve's shared test space — and copies the player's local save up the first time. Local and cloud stores now share one file-naming rule.

---

### Task 113: The collaborator's build — the first real internet game — **needs QUIET for Steps 3–4**

**Why.** Michael's decision 8b: the collaborator is the remote tester, from their own home, over the real internet, and there is no second PC. They need:
- a Windows build they can unzip and run, with `steam_appid.txt` beside the executable;
- a development build, so the Net panel's Steam ID line and "Join" fallback are there;
- a one-page sheet on how to run it.

That sheet is `docs/team/netcode/collaborator-how-to-run.md`, written with this plan. It ships inside the zip as `HOW-TO-RUN.txt`.

This is the first time two homes play. Everything before it ran on one PC.

**Files:**
- Create: `Assets/_BattleBomb/Editor/CollaboratorBuild.cs`

- [ ] **Step 1: The build script**

Create `Assets/_BattleBomb/Editor/CollaboratorBuild.cs`:

```csharp
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BattleBomb.Editor
{
    /// <summary>
    /// The build the collaborator plays from their own home (HANDOFF-M8 Task 113): Windows, a development build (the Net
    /// panel's Steam ID and "Join" are the first test's fallback), steam_appid.txt beside the executable, the one-page
    /// how-to-run sheet inside, zipped for sending. It lands in Documents\BattleBomb Builds — outside the repository, so
    /// nothing built is ever committed.
    /// </summary>
    public static class CollaboratorBuild
    {
        private const string Sheet = "docs/team/netcode/collaborator-how-to-run.md";

        [MenuItem("BattleBomb/Build/Collaborator build (Windows)")]
        public static void Build()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string builds = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BattleBomb Builds");
            string folder = Path.Combine(builds, $"BattleBomb-{DateTime.Now:yyyy-MM-dd-HHmm}");

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = Path.Combine(folder, "BattleBomb.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"The collaborator build failed: {report.summary.result}, {report.summary.totalErrors} errors.");
            }

            File.Copy(Path.Combine(root, "steam_appid.txt"), Path.Combine(folder, "steam_appid.txt"), true);
            File.Copy(Path.Combine(root, Sheet), Path.Combine(folder, "HOW-TO-RUN.txt"), true);

            // Unity's side folders (Burst's debug information, the IL2CPP backup) are not for shipping.
            foreach (string extra in Directory.GetDirectories(folder, "*DoNotShip*")
                .Concat(Directory.GetDirectories(folder, "*DontShip*")))
            {
                Directory.Delete(extra, true);
            }

            string zip = folder + ".zip";
            ZipFile.CreateFromDirectory(folder, zip);
            Debug.Log($"Collaborator build: {zip}");
        }
    }
}
```

`recompile`. If `ZipFile` does not resolve (the Editor assembly's API level lacks `System.IO.Compression.ZipFile`):
- keep everything but the zip lines;
- log the folder;
- the sheet already tells Michael to right-click the folder and choose **Send to → Compressed (zipped) folder**.

Say which in the `DONE`.

- [ ] **Step 2: Build it**

Run the menu item through the bridge (`menu` with `BattleBomb/Build/Collaborator build (Windows)`), or ask the orchestrator for `QUIET ON` first if Michael is using the machine: a build takes the editor for minutes. Then:
- read `console` for `Collaborator build: …`;
- check the folder holds `BattleBomb.exe`, `steam_appid.txt` (containing `480`), `HOW-TO-RUN.txt`, and `BattleBomb_Data\Plugins\x86_64\steam_api64.dll`. Without the DLL, Steam never starts in the build.

Launch the built `BattleBomb.exe` once yourself with Steam running (the bridge cannot; ask the orchestrator to have Michael double-click it). It should reach the title, and Steam should show Michael as "playing Spacewar". That is the proof the native library and the app ID made it into the build.

- [ ] **Step 3: The first internet game (QUIET)**

The orchestrator calls `QUIET ON` and arranges the sitting with Michael and the collaborator.
- Michael sends the zip (any file-sharing he likes), and both follow `HOW-TO-RUN.txt`.
- Michael's checks are `docs/team/m8-plan3-pass.md`, Part F: join by "Join Game", join by typed Steam ID, a stage with a checkpoint room, a chest each, a hit each way, leave and rejoin, one pulled cable.
- Collect both sides' answers through the orchestrator.

- [ ] **Step 4: Record, `QUIET OFF`, and `DONE`**

Write the verdicts into your lane file's close-out draft. For each check record which home saw it, and anything that differed between the two homes.

A finding becomes a `113a`… task. The likely ones:
- "Join Game" never appears: rich presence on Spacewar.
- The overlay does not open: the game was launched outside Steam.
- The relay takes long to connect.

The typed-ID fallback exists so the sitting can go on while a finding is fixed.

Paths:
- `Assets/_BattleBomb/Editor/CollaboratorBuild.cs` (+ `.meta`)
- your lane file

Subject: `113: the collaborator's build and the first two-home game`. Body: the collaborator plays from their own home (Michael's decision 8b), so the build script makes a zipped Windows development build with steam_appid.txt and the one-page how-to-run sheet inside, outside the repository; and the first game over the real internet has been played.

---

## Close-out

### Task 114: BattleBomb's own app, the whole chapter from two homes, and M8's close-out — **needs QUIET**

**Why.**
- **The app ID.** Michael's decision 8a: the Steamworks account and app ID arrive at the close-out, and ROADMAP §5.3 warns that Valve's paperwork (tax, bank, identity) can take days. Michael's queue item 6 starts that early enough.
- **The pass.** HANDOFF's close-out: the collaborator and Michael play the fixture chapter start to finish from both homes — fight, revive, grab, chest, shop, wipe, results, save — with a checklist from each end.
- **The couch.** It is replayed exactly as before.
- **The gates.** Both are green, and the close-out notes are written.

**Files:**
- Modify: `steam_appid.txt` (the real ID)
- Your lane file (the close-out draft)
- Nothing else unless a finding needs it

- [ ] **Step 1: The app ID (Michael, through the orchestrator)**

Michael provides the App ID number from his Steamworks partner account (partner.steamgames.com → the app). Before the swap, he sets these on the partner site:
- **Steam Cloud:** App Admin → Steam Cloud. A byte quota of 20 MB per user and 50 files is ample (a save is tens of KB). Publish the change.
- **The collaborator's access:** add them to the partner group as a developer, or issue them a key from the app's own key tool (Users & Permissions / Request Steam Product Keys), so the app is in their library. Without this, their Steam will not run an unreleased app's ID.

If the paperwork is not done, stop here and tell the orchestrator. Steps 3–5 can run on Spacewar and be repeated on the real ID later, as the orchestrator decides.

- [ ] **Step 2: Swap the ID**

Replace the content of `steam_appid.txt` with the number. Nothing in the code names 480 except `SteamPlatformServices.SpacewarAppId`, which is only ever compared against: it switches the cloud off on Spacewar.

`recompile`, then `run_tests` mode `EditMode`. With Steam running, `SteamSessionTests` checks the new app ID. The Steam client must know the app (Michael's library), or `InitEx` fails and the test is Ignored; say which.

`editor_play` the Frontend scene with Steam running. The console says `Steam: signed in as … on app <the new ID>.` Then `editor_stop`.

The cloud check, a settled state and no QUIET:
1. Play to the first checkpoint room and let the autosave run.
2. Stop, and look at Steam's cloud listing for the app (store.steampowered.com/account/remotestorage, Michael's account). It shows `<SteamID>.save`.
3. The local `saves` folder still has the Spacewar-era file. The adoption copied it up; it did not move it.

Build the collaborator's zip again (Task 113's menu item): it now carries the new `steam_appid.txt`.

- [ ] **Step 3: The two-home pass (QUIET)**

The orchestrator calls `QUIET ON` and arranges the sitting. The checklist is `docs/team/m8-plan3-pass.md`, Part G1: the fixture chapter start to finish, with each check ticked from each home.

The one thing only the real app can show: after the sitting, the collaborator's progress is in *their* Steam Cloud. On a second PC, or after deleting their local `saves` folder, it comes back.

- [ ] **Step 4: The couch, replayed**

Michael, on his own PC, with a pad and the keyboard: Part G2 of the same sheet.
- A two-player couch run of the fixture chapter behaves as before M8: the chest pauses nothing on the couch, and a solo chest pauses the world.
- With Steam running, nobody is offered "Join Game" while the couch is full. D59: couch games are full and never open; the transport is never created, so presence is never set.

- [ ] **Step 5: The gates and the close-out notes**

`run_tests` mode `EditMode` and mode `PlayMode` (async) at the tip. Record the counts (see the table below), and which Steam tests ran.

Write the close-out draft in your lane file, in Plan 1's shape (`builder.md`'s "M8 Plan 1 close-out draft"). HANDOFF-M8 is the orchestrator's to write from it.
- **Build log:** F4, 106–114, one row each, with commits.
- **Where the plan was wrong:** every premise check that found something, and every extra spec.
- **What felt wrong to build.**
- **Deferred:** at least
  - delta snapshots (M11);
  - the skipped-frame Jump, if 108 did not need it;
  - Steam achievements and leaderboards (M13);
  - `RestartAppIfNecessary` (M13);
  - the shadow, health bar and camera reading `Position` rather than the drawn body (M9).
- **Michael's verdicts:** the Stage E lag table (on/off), and Parts F, G1 and G2.
- **"Where this plan departs from HANDOFF-M8"**, from this plan's header, for the orchestrator to fold into the spec's planning decisions.
- **The ROADMAP line:** *M8 complete — `<F4's commit>`..`<114's commit>`; Michael and the collaborator played the fixture chapter from two homes on `<date>`.*

Also send the orchestrator, for Michael's queue: **"Two couch players on the same hero share one character save"** (queue item 2) is still open. M8 did not touch it (Plan 2's note).

- [ ] **Step 6: `QUIET OFF`, then `DONE`**

Paths:
- `steam_appid.txt`
- your lane file

Subject: `114: M8 close-out — BattleBomb's own app, two homes, the couch`. Body: Michael's app ID replaces Spacewar, so saves move to Steam Cloud; Michael and the collaborator played the fixture chapter start to finish from two homes, and the couch played exactly as before M8. The close-out notes and the spec's departures go to the orchestrator for HANDOFF-M8.

---

## Self-review

**Spec coverage** (HANDOFF-M8 Stages E, F, the close-out; D58–D62; the board's Plan 3 list):

| Spec item | Task |
|---|---|
| Stage E — `PredictionLog` and `CharacterActor.PredictStep` (planning decision 19) | 106 |
| Stage E — reconcile, replay, correction smoothing | 106 (the replay and the eased offset); 107 (the picture around it) |
| Stage E — Michael's feel pass, both lag profiles | 108 |
| Stage F — Steamworks.NET (UPM, pinned tag), `Platform.Steam` with its version define, `SteamPlatformServices` on 480, identity → the save's name | 109 |
| Stage F — the Steam P2P transport | 110 |
| Stage F — lobbies, invites, join requests, rich presence | 111 (rich presence and invites; no lobby object — see the departures) |
| Stage F — Steam Cloud as the second `ISaveStore` | 112 |
| Stage F — the collaborator's build and how-to-run sheet; the first real internet game | 113 |
| Close-out — Michael's app ID, two homes, the couch replayed, gates, notes | 114 |
| Planning decision 3 — `PlatformRegistry`, `ILobbyService`, Steam registers itself | 109, 111 |
| Rule 6 — a build without Steamworks compiles and runs | 109 (define constraint + fitness test), every task's offline tests |
| The board: F4 | F4 |
| The board: `SocketException` in Gameplay | 110 |
| The board: delta snapshots | Decided not needed (header) |
| The board: skipped-frame Jump | 107 (written down), 108 (checked) |
| Task 96: teleport threshold by gap; render clock through a pause; let-go and drain | 107 |
| 96b: arrow keys cross windows | 108's sheet |

**Placeholder scan.** Every code step carries its code. Two steps name conditional follow-ups, with the exact fallback given:
- 110 Step 6: the pair's channel bit;
- 113 Step 1: `ZipFile`.

"Plan 2 as built" anchors are quoted from Plan 2's own text. Their premise is checked per task, as the Builder already does.

**Names that cross tasks:**

| Name | Defined in | Used in |
|---|---|---|
| `PredictedBody`, `PredictionEntry`, `PredictionLog` | 106 | 106 |
| `CharacterActor.PredictStep`, `CaptureBody`, `RestoreFromHost`, `CorrectionOffset`, `AddCorrection`, `DecayCorrection`, `ClearCorrection` | 106 | 106 |
| `SimulationDriver.LightIsContextual` | 106 | 106 |
| `LocalPrediction`; `NetGuest.IsPredicting`, `LastCorrection`, `TryGetPredicted`; `ReplicaWorld.Predicted` | 106 | 106 |
| `NetSession.PredictOwnPlayer` | 106 | 106 (`PlaybackTransport`, the panel) |
| `NetProtocol.PredictionMaxAheadSteps`, `CorrectionDecayPerStep` | 106 | 106 |
| `ReplicaMotion.IsTeleport` | 107 | 107 |
| `INetTransport.Listen` returning `string` | 110 | 110 (every implementer, including 106's `AnsweringHost`) |
| `PlatformRegistry.Register`, `Current`, `Use`, `Release`, `ResetForTests` | 109 | 109, 111 |
| `PlatformRegistry.Started` | 111 | 111 |
| `ILobbyService.CanHost`, `CreateTransport`, `HasJoinRequest`, `TryTakeJoinRequest`; `IPlatformServices.Lobby` | 109 | 110, 111 |
| `SteamPlatformServices.SpacewarAppId` | 109 | 112 |
| `SteamP2PTransport(Action<bool>)` | 111 (the parameterless form in 110) | 111 |
| `SteamP2PTransport.TryParseSteamId`, `TryCreatePair` | 110 | 110, 111 |
| `JoinAddress.Format`, `TryParse` | 111 | 111 |
| `NetSession.FriendsTransport` (now a property with a backing field), `JoinFriend`, `JoinWaitingFriend`, `FriendJoinWaiting`, `PlatformPlayer` | 111 | 111 |
| `GameSession.LocalSave` | 109 | 109 |
| `SaveNames.FileName`, `Extension` | 112 | 112 |
| `SteamCloudSaveStore` | 112 | 112 |
| `PlatformRegistryTests.FakePlatform` (with its `lobby` parameter) | 109 | 109, 111 |

## Test counts

| After | EditMode (baseline +) | PlayMode (baseline +) | Of which Ignored without Steam |
|---|---|---|---|
| F4 | 2 | 0 | — |
| 106 | 7 | 4 | — |
| 107 | 11 | 4 | — |
| 108 | 11 | 4 | — |
| 109 | 20 | 4 | 1 |
| 110 | 25 | 4 | 4 |
| 111 | 31 | 6 | 4 |
| 112 | 34 | 6 | 4 |
| 113, 114 | 34 | 6 | 4 |

The baseline is whatever Plan 2 (and any `96x` / `105x` fix) left. Expected 839 / 106 if nothing was inserted.
