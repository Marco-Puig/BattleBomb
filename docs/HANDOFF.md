# Handoff — M1 groundwork

You are picking up the BattleBomb rebuild. Work through the tasks in order. Each one is small and has
an acceptance check you must actually run. **Do not skip ahead, and do not start a task whose
predecessor is not green.**

---

## Read first

- `CLAUDE.md` — the working brief and the rules.
- `docs/ARCHITECTURE.md` — §1 assemblies, §2 what Core may contain, §4 the input/command path.
- `docs/DECISIONS.md` — D13 (depth plane), D14 (readable shadows), D15 (3D world, 2D characters).

Do not re-litigate anything in `DECISIONS.md`. If a task seems to conflict with it, stop and say so
rather than improvising.

## Non-negotiable rules

1. **Never edit a test to make it pass.** A failing `ArchitectureFitnessTests` means the code broke a
   rule — fix the code.
2. **Nothing new in `BattleBomb.Core` may touch** `MonoBehaviour`, `ScriptableObject`, `GameObject`,
   `Time`, coroutines, `Resources`, `SceneManager`, or `UnityEngine.Random`. Value types like
   `Vector2`/`Mathf` are fine.
3. **No `Input.GetKey` / `GetAxis` / `GetButton` anywhere.** Input already has one route:
   `InputSystemCommandSource` → `PlayerCommand`.
4. **No `FindGameObjectWithTag("Player")`.** Players come from `PlayerRegistry` on `SimulationDriver`.
5. **`[SerializeField] private`**, never public fields. No new singletons.
6. Nothing may be hardcoded to one player.

## Tools

The Unity MCP server drives the editor. The ones you need:

| Call | Use |
|---|---|
| `recompile` | After any script edit. |
| `run_tests` with `mode: EditMode` | The verification gate. |
| `console` with `level: error` | Read editor errors. |
| `capture_game_view` / `capture_scene_view` | See what you built. |
| `create_gameobject`, `add_component`, `create_prefab`, `add_scene_to_build` | Scene work. |

If a call reports the Pipeline server is unreachable, Unity is closed or busy — say so and stop rather
than guessing.

---

## State you are inheriting

Complete and previously verified green (19 EditMode tests):

```
Assets/_BattleBomb/
├── Core/Players/        PlayerCommand, CommandButtons, PlayerId
├── Core/Simulation/     SimulationClock                     ← written, NOT yet verified
├── Gameplay/Players/    IPlayerCommandSource, PlayerRegistry,
│                        InputSystemCommandSource, PlayerActions   ← rewritten, NOT yet verified
├── Gameplay/Simulation/ SimulationDriver                    ← written, NOT yet verified
├── Platform/            IPlatformServices + Null implementation
├── Input/               BattleBombControls.inputactions      ← authored by hand, NOT yet imported
└── Tests/EditMode/      4 test fixtures (+ SimulationClockTests, NOT yet verified)
```

**The last four items have never been compiled.** Unity's editor process exited during a package
version swap before they could be checked. Task 1 is to find out whether they are correct.

Packages were just changed: `com.unity.2d.animation` is now `15.1.0` (13.0.6 does not compile against
Unity 6.5) and `com.unity.cinemachine` `3.1.7` was added.

---

## Task 1 — Get back to green

**Goal:** the project compiles and every EditMode test passes.

1. Ask Michael to open the project in Unity if `recompile` reports no reachable editor. Wait; do not
   try to launch Unity yourself.
2. Call `recompile`. If it reports errors in `Library/PackageCache/...`, that is a package problem —
   report it and stop, do not edit package files.
3. Fix any compile errors in the files listed above. Likely candidates, in order of probability:
   - a missing `using` between `BattleBomb.Gameplay.Players` and `BattleBomb.Gameplay.Simulation`;
   - `FindFirstObjectByType` availability in `InputSystemCommandSource.OnEnable`;
   - the `List<(InputAction, CommandButtons)>` tuple syntax in `InputSystemCommandSource`.
4. Call `run_tests` with `mode: EditMode`.
5. Check `console` with `level: error` and confirm `BattleBombControls.inputactions` imported without
   an error. If it failed to parse, report the exact message — do not rewrite the file blind.

**Done when:** `run_tests` reports 0 failed and at least 27 tests total, and the console has no errors.

**Commit:** `M0: fixed-step simulation clock, driver, and input action asset`

---

## Task 2 — Fill the gaps in the Core test suite

**Goal:** the two Core types with no coverage get some.

Add to `Assets/_BattleBomb/Tests/EditMode/`:

- `PlayerIdTests.cs` — equality, `==` and `!=`, `GetHashCode` consistency for equal ids, `ToString`
  returning `"P1"` for `PlayerId.One` and `"P2"` for `PlayerId.Two`, and that a `PlayerId` used as a
  dictionary key resolves correctly.
- `CommandButtonsTests.cs` — that combining two flags and testing each with
  `PlayerCommand.IsHeld` behaves, and that `CommandButtons.None` is held by nothing.

Follow the naming style already in that folder: descriptive sentence names, one behaviour per test.

**Done when:** `run_tests` green, total count risen by the tests you added.

**Commit:** `M0: cover PlayerId and CommandButtons`

---

## Task 3 — The gameplay scene

**Goal:** a scene that is not the URP template, laid out side-on per D13/D15.

Create `Assets/_BattleBomb/Scenes/Gameplay.unity` containing:

- A ground plane, roughly 20 × 20 world units, at the origin.
- A directional light angled so objects cast a clearly visible shadow onto the ground — D14 makes the
  grounded shadow a functional requirement, not decoration. Shadows must be **on**.
- A camera positioned side-on: looking down the +Z axis at the play area, raised and tilted down
  roughly 15–20°, far enough back to frame about 16 units of width. **Perspective, not orthographic.**
- An empty GameObject named `Simulation` carrying the `SimulationDriver` component.

Add the scene to Build Settings with `add_scene_to_build`. Leave `Assets/Scenes/SampleScene.unity`
alone.

**Done when:** `capture_scene_view` shows the ground, lit, with a visible shadow from any test object
you drop in, and console is clean. Attach the screenshot to your report.

**Commit:** `M1: gameplay scene with side-on camera and lit ground`

---

## Task 4 — The player prefab

**Goal:** a player that produces commands. No movement yet — that is the next milestone's work.

Create `Assets/_BattleBomb/Prefabs/Player.prefab`:

- Root empty GameObject named `Player`.
- Child capsule named `ShadowProxy` — this is the invisible 3D mesh that casts the character's shadow
  (D15). For now leave its renderer **visible** so you can see something; set its material to
  something plain. Cast Shadows on.
- On the root: `PlayerInput` (Input System) with
  - Actions = `Assets/_BattleBomb/Input/BattleBombControls.inputactions`
  - Default Map = `Gameplay`
  - Behavior = `Invoke Unity Events` (we poll; we do not want SendMessage overhead)
- On the root: `InputSystemCommandSource`. Leave its `_driver` field empty — it finds the driver
  itself.

**Done when:** the prefab exists, and dropping one into the scene produces no console errors on
entering Play mode.

**Commit:** `M1: player prefab with input-driven command source`

---

## Task 5 — Prove the command path end to end

**Goal:** see a real button press arrive as a `PlayerCommand`.

Write `Assets/_BattleBomb/UI/Debug/CommandDebugOverlay.cs` in the `BattleBomb.UI` assembly:

- A `MonoBehaviour` with `[SerializeField] private SimulationDriver _driver;`
- In `OnGUI`, draw one line per entry in `_driver.Commands`: player id, `Move` rounded to 2 decimals,
  and the `Held` flags.
- It **reads only**. It must not call anything that mutates gameplay state, and it must not own any
  state of its own beyond display formatting. This is rule 2 of `CLAUDE.md` in miniature.

Note: `BattleBomb.UI.asmdef` currently has no code, so this is the first thing to compile into it —
if the assembly does not appear, check its `.asmdef` references.

Place two `Player` instances in the scene, add the overlay to a GameObject, wire `_driver`.

**Done when:**
- `run_tests` still green (`ArchitectureFitnessTests` must still pass — if the UI assembly now
  references something it should not, that test will tell you).
- In Play mode, with a keyboard, pressing W/A/S/D changes the `Move` values on screen and J/K/Space
  set the Attack/Heavy/Dodge flags. Verify with `capture_game_view` and attach it.
- Console clean.

**Commit:** `M1: debug overlay proving the input-to-command path`

---

## Task 6 — Housekeeping

Only after 1–5 are green:

- `Assets/InputSystem_Actions.inputactions` is the URP template's asset and is now redundant. Confirm
  nothing references it (search the project), then delete it and its `.meta`. **If anything does
  reference it, leave it and say so.**
- Report whether `Assets/_BattleBomb/Presentation/` and `Editor/` still have no code — that is
  expected, not a problem.

**Commit:** `M1: remove unused template input asset`

---

## Reporting back

For each task, report: what you changed, the `run_tests` summary line, and any screenshot you took.
If you got stuck, say exactly where and what the error was — do not paper over it, and do not mark a
task done that is not.

Things to escalate rather than solve yourself:

- Any change that would need a new assembly, or a new reference in an existing `.asmdef`.
- Any urge to put a `MonoBehaviour` in Core, or to have Core reference Gameplay.
- Anything about characters, story, art, or combat tuning — those are open design questions and not
  yours to invent.
