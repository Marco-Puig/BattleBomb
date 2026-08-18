# Handoff — M1: movement and camera

Continues `docs/HANDOFF.md` (tasks 1–6). Same rules, same tools, same reporting format — re-read that
file's "Non-negotiable rules" and "Tools" sections before starting; they are not repeated here.

**M1 is done when:** two players move on the depth plane with readable grounded shadows, jump, and a
single shared camera holds both. Every piece of movement and framing maths is in Core and covered by
EditMode tests.

---

## Decisions taken while planning this milestone

Recorded because they are load-bearing and a future session will need the reasoning. These follow
from `GAME_DESIGN.md`; none contradicts a locked decision in `DECISIONS.md`.

1. **The button set matches the ability slot table (§3), and nothing else.**
   *Resolved by Michael, 2026-08-17 — recorded as D17, which supersedes the vocabulary this document
   originally drafted here.* The verbs are `Light, Heavy, Magic, Equipment, Jump, Block`; Interact
   folds into Light contextually and there is no dodge. Task 7 below is written against D17.

2. **Jump lands in M1, not M2.** §2.4 makes jump a general-purpose mechanic. It is movement, so it
   belongs to the movement motor; bolting a vertical axis onto a finished horizontal motor later is
   exactly the rework this rebuild exists to avoid. It also gives D14's grounded shadow its clearest
   test: a shadow that stays on the floor while the character rises.

3. **The depth band is a project constant, not per-area data.** §2.1 says the band never changes
   between areas, so it is `DepthBand` in Core — `Z ∈ [-3, +3]`, 6 units. An arena varies only in its
   X extent. Encoding it as a constant makes the rule impossible to violate per-scene.
   *Flagged for Michael — 6 units is my starting number, chosen against a ~1-unit character and §2.2's
   ~1 lane-unit melee tolerance. It is one constant to change once the game is playable.*

4. **Core owns character position; there is no physics engine in the loop.** The motor is a pure
   function of (state, command, tuning, bounds, dt). For M1 the only collision is the arena clamp and
   the ground plane. Obstacles and character-vs-character resolution arrive with M3 and will enter as
   an explicit Core-defined seam, never as a `Rigidbody`.

5. **The camera is hand-rolled for M1; Cinemachine stays installed but unused.** Framing two players
   on a fixed side-on axis is a ~40-line pure function — putting it in Core makes it testable, which
   a Cinemachine graph is not. Cinemachine earns its place later for impulse/shake (M2 hit feel) and
   can consume the same computed focus point.

6. **Simulation stepping of characters is ordered, not event-driven.** The driver walks its character
   registry in ascending `PlayerId` order and steps each one. An `Action<int> Stepped` event that
   fires in registration order would make the simulation's outcome depend on scene load order — fine
   today, unfixable once characters interact (D10).

---

## Task 7 — Align the command vocabulary with the design

**Goal:** the six buttons the game actually has (D17).

`CommandButtons` becomes exactly:

```csharp
None      = 0,
Light     = 1 << 0,   // basic melee combo; performs Interact in context
Heavy     = 1 << 1,   // slow, high-commitment; launcher / guard-break class
Magic     = 1 << 2,   // the character's element, offensively
Equipment = 1 << 3,   // the equipped active item (§5)
Jump      = 1 << 4,   // universal (§2.4)
Block     = 1 << 5,   // hold to guard; active defence, not turtling
```

Then update, in this order:

1. `Core/Players/CommandButtons.cs` — the enum above, keeping the existing comment style.
2. `Gameplay/Players/PlayerActions.cs` — constants renamed to match.
3. `Gameplay/Players/InputSystemCommandSource.cs` — the `AddButton` calls.
4. `Input/BattleBombControls.inputactions` — edit the JSON in place, preserving its formatting.
   The old and new sets are both seven actions, so **every action renames in place keeping its
   existing `id`** — nothing is added or deleted and no new ids are needed:

   | Action | Keyboard | Gamepad |
   |---|---|---|
   | Move *(unchanged)* | WASD composite | left stick |
   | Light *(was Attack)* | `<Keyboard>/j` | `<Gamepad>/buttonWest` |
   | Heavy *(unchanged)* | `<Keyboard>/k` | `<Gamepad>/buttonNorth` |
   | Magic *(was Ability1)* | `<Keyboard>/l` | `<Gamepad>/buttonEast` |
   | Equipment *(was Ability2)* | `<Keyboard>/i` | `<Gamepad>/rightShoulder` |
   | Jump *(was Dodge)* | `<Keyboard>/space` | `<Gamepad>/buttonSouth` |
   | Block *(was Interact)* | `<Keyboard>/leftShift` | `<Gamepad>/leftShoulder` |

5. Any test or the debug overlay that names the old verbs.

**Done when:** `run_tests` green on the vocabulary tests, console clean, and in Play mode the overlay
shows Jump flagging on space and Block on left shift.

**Commit:** `M1: six-verb control scheme (D17)`

---

## Task 8 — Core spatial: the depth band and arena bounds

**Files:** `Core/Spatial/DepthBand.cs`, `Core/Spatial/ArenaBounds.cs`, plus tests.

```csharp
public static class DepthBand          // §2.1 — identical in every area, by construction
{
    public const float HalfWidth = 3f;
    public const float Min = -HalfWidth;
    public const float Max =  HalfWidth;
    public const float Width = HalfWidth * 2f;
    public static float Clamp(float z);
}

public readonly struct ArenaBounds
{
    public ArenaBounds(float minX, float maxX, float groundY = 0f);  // ArgumentException if maxX < minX
    public float MinX { get; }  public float MaxX { get; }  public float GroundY { get; }
    public float MinZ => DepthBand.Min;   public float MaxZ => DepthBand.Max;
    public float Width => MaxX - MinX;
    public Vector3 Center { get; }                        // (mid X, GroundY, 0)
    public Vector3 ClampHorizontal(Vector3 position);     // clamps X and Z; leaves Y untouched
    public bool Contains(Vector3 position);               // X/Z only — Y is the jump axis, not a bound
    public static ArenaBounds Default { get; }            // (-10, 10, 0)
}
```

**Tests:** X clamped at each edge and left alone inside; Z clamped to the depth band regardless of the
arena's X extent; Y passed through untouched even when far above ground; `Center`; `Contains` ignoring
Y; the invalid-constructor throw.

**Commit:** `M1: depth band and arena bounds in Core`

---

## Task 9 — Core motor: horizontal movement and facing

**Files:** `Core/Movement/Facing.cs`, `MovementTuning.cs`, `MotorState.cs`, `CharacterMotor.cs`, tests.

```csharp
public enum Facing { Left = -1, Right = 1 }

public readonly struct MovementTuning
{
    float MaxSpeed;          // 6      units/sec, lateral
    float Acceleration;      // 60     units/sec² toward the desired velocity
    float Deceleration;      // 80     units/sec² toward zero when there is no input
    float DepthSpeedScale;   // 0.85   depth movement is slightly slower — perspective feel
    // vertical fields are added in Task 10; include them now only if you do both tasks together
    public static MovementTuning Default { get; }
}

public readonly struct MotorState
{
    Vector3 Position;  Vector3 Velocity;  Facing Facing;  bool IsGrounded;
    int StepsSinceGrounded;   // 0 while grounded
    int JumpBufferedFor;      // steps left on a remembered jump press, 0 = none
    public static MotorState AtRest(Vector3 position, Facing facing = Facing.Right);
}

public static class CharacterMotor
{
    public static MotorState Step(in MotorState state, in PlayerCommand command,
                                  in MovementTuning tuning, in ArenaBounds bounds, float dt);
}
```

Horizontal rules, exactly:

```
desiredX = command.Move.x * tuning.MaxSpeed
desiredZ = command.Move.y * tuning.MaxSpeed * tuning.DepthSpeedScale     // Move.y is the depth axis
rate     = command.Move.sqrMagnitude > 0 ? tuning.Acceleration : tuning.Deceleration
vx = MoveTowards(state.Velocity.x, desiredX, rate * dt)
vz = MoveTowards(state.Velocity.z, desiredZ, rate * dt)
position = state.Position + new Vector3(vx, 0, vz) * dt
clamped  = bounds.ClampHorizontal(position)
if (clamped.x != position.x) vx = 0        // no stored speed pushing into a wall
if (clamped.z != position.z) vz = 0
facing = command.Move.x >  0.01f ? Facing.Right
       : command.Move.x < -0.01f ? Facing.Left
       : state.Facing                       // pure depth input never changes which way you face
```

`Mathf.MoveTowards` is fine in Core — it is value maths, not engine state.

**Tests:** accelerates toward max and does not exceed it; decelerates to exactly zero and stays there;
depth speed is `DepthSpeedScale` × lateral speed for equal input; facing flips on lateral input and
survives pure-depth input; velocity is zeroed on the axis that hit a wall but not the other; position
never leaves the arena; and a determinism test — 120 identical steps from an identical start produce
an identical `MotorState` twice over.

**Commit:** `M1: horizontal character motor in Core`

---

## Task 10 — Core motor: gravity, jump, coyote time, input buffer

Extends `MovementTuning` and the same `CharacterMotor.Step`.

```csharp
float Gravity;             // 45    units/sec², positive magnitude
float JumpSpeed;           // 12    upward units/sec at takeoff  → ~1.6u high, ~0.53s airborne
float MaxFallSpeed;        // 30    terminal velocity, positive
float JumpCutMultiplier;   // 0.45  upward velocity kept when Jump is released early
int   CoyoteSteps;         // 6     ≈0.1s at 60Hz — jump still works just after walking off a ledge
int   JumpBufferSteps;     // 6     ≈0.1s — a press just before landing fires on landing
```

Vertical rules, in this order within the step:

```
buffered = max(0, state.JumpBufferedFor - 1)
if (command.WasPressed(Jump))  buffered = tuning.JumpBufferSteps
stepsSinceGrounded = state.IsGrounded ? 0 : state.StepsSinceGrounded + 1
vy = state.Velocity.y

if (buffered > 0 && stepsSinceGrounded <= tuning.CoyoteSteps) {
    vy = tuning.JumpSpeed;  buffered = 0;
    grounded = false;  stepsSinceGrounded = tuning.CoyoteSteps + 1;   // consumes the coyote window
} else {
    if (!state.IsGrounded) vy = Max(vy - tuning.Gravity * dt, -tuning.MaxFallSpeed);
    else                   vy = 0;
    if (command.WasReleased(Jump) && vy > 0) vy *= tuning.JumpCutMultiplier;
}

y = position.y + vy * dt
if (y <= bounds.GroundY) { y = bounds.GroundY; vy = 0; grounded = true; stepsSinceGrounded = 0; }
else                     { grounded = false; }
```

**Tests:** a grounded jump rises; a second press mid-air does nothing; a press within `CoyoteSteps` of
leaving the ground still jumps and one step later does not; a press `JumpBufferSteps` before landing
fires on the landing step; gravity accumulates and clamps at `MaxFallSpeed`; landing snaps exactly to
`GroundY` with zero vertical velocity; releasing Jump while rising cuts upward velocity by the
multiplier and releasing while falling does not; horizontal control still works mid-air.

**Commit:** `M1: gravity, jump, coyote time and jump buffering`

---

## Task 11 — Gameplay data: character definition and arena volume

**Goal:** establish §5's authoring pattern — ScriptableObject in, plain struct out — on its first
real case.

`Gameplay/Data/CharacterDefinition.cs`:

```csharp
[CreateAssetMenu(menuName = "BattleBomb/Character Definition", fileName = "CharacterDefinition")]
public sealed class CharacterDefinition : ScriptableObject
{
    [SerializeField] private string _displayName = "Unnamed";
    [Header("Movement")]  // one serialized field per MovementTuning field, defaults matching Default
    public string DisplayName => _displayName;
    public MovementTuning ToRuntime();
}
```

`Gameplay/World/ArenaVolume.cs` — MonoBehaviour with `_minX`, `_maxX`, `_groundY`, a
`public ArenaBounds ToRuntime()`, and an `OnDrawGizmos` that draws the arena box using `DepthBand` for
its Z extent so the fixed band is visible while building a level.

Author one asset: `Assets/_BattleBomb/Data/Characters/Default.asset`.

**Tests:** an EditMode test using `ScriptableObject.CreateInstance<CharacterDefinition>()` asserting
`ToRuntime()` carries the authored values through. Do **not** load the asset from disk in the test —
construct the instance.

**Commit:** `M1: character definition and arena volume authoring`

---

## Task 12 — Gameplay: the character actor

**Files:** `Gameplay/Characters/CharacterActor.cs`, `Gameplay/Characters/CharacterRegistry.cs`, and
changes to `Gameplay/Simulation/SimulationDriver.cs`.

`CharacterActor`:

- `[SerializeField] CharacterDefinition _definition;` and `[SerializeField] int _playerIndex;`
- `Awake`: `PlayerId` comes from a sibling `IPlayerCommandSource` if one exists, else `_playerIndex`.
  Seed `MotorState.AtRest(transform.position)`.
- `OnEnable` / `OnDisable`: register and unregister with the driver's `Characters`, same
  find-the-driver pattern `InputSystemCommandSource` already uses.
- **`internal void Step(int frame, in PlayerCommand command, in ArenaBounds bounds, float dt)`** —
  keeps `_previous`, runs `CharacterMotor.Step`, writes `transform.position = _state.Position`.
  `internal` is deliberate: Presentation and UI reference Gameplay, so `internal` is the compiler-level
  guarantee they cannot drive the simulation (§3).
- Public, read-only: `PlayerId`, `Position`, `PreviousPosition`, `Facing`, `IsGrounded`.

`CharacterRegistry` — same shape as `PlayerRegistry`, holding actors in **ascending `PlayerId` order**;
expose an ordered `IReadOnlyList<CharacterActor>`.

`SimulationDriver` gains:

- `[SerializeField] private ArenaVolume _arena;` → `public ArenaBounds Bounds` (fall back to
  `ArenaBounds.Default` with one `Debug.LogWarning` if unset, not one per step).
- `public CharacterRegistry Characters { get; }`
- Inside the existing step loop, **after** `SampleAll` and **before** `Stepped` is raised: walk
  `Characters` in order and call `Step(frame, command, Bounds, StepDuration)`, passing
  `PlayerCommand.Idle(frame)` for any actor with no command source.

**Done when:** `run_tests` green, and in Play mode a player capsule visibly moves with WASD, stops at
the arena edges, cannot leave the depth band, and jumps. Attach a `capture_game_view`.

**Commit:** `M1: character actor stepped deterministically by the simulation driver`

---

## Task 13 — Presentation: interpolated visuals

**File:** `Presentation/Characters/InterpolatedVisual.cs` — the first code in that assembly.

```csharp
// LateUpdate
transform.position = Vector3.Lerp(_actor.PreviousPosition, _actor.Position, _driver.Alpha);
transform.rotation = Quaternion.Euler(0f, _actor.Facing == Facing.Right ? 0f : 180f, 0f);
```

It reads and never writes gameplay state — that is the whole point of the assembly split. If you find
yourself needing to call something on `CharacterActor` that does not exist because it is `internal`,
that is the design working; report it rather than widening the API.

Restructure the player prefab to:

```
Player            PlayerInput, InputSystemCommandSource, CharacterActor
└── Visual        InterpolatedVisual
    └── ShadowProxy   capsule mesh, casts shadows
```

**Done when:** motion is smooth at high frame rates, the capsule turns to face travel direction, and
`ArchitectureFitnessTests` still passes.

**Commit:** `M1: interpolated presentation for character motion`

---

## Task 14 — Core: camera framing maths

**Files:** `Core/Cameras/CameraTuning.cs`, `CameraFrame.cs`, `CameraFraming.cs`, tests.

```csharp
public readonly struct CameraTuning
{
    Vector3 OffsetDirection;      // normalised in the constructor; default (0, 0.45, -1)
    float BaseDistance;           // 14
    float MinDistance;            // 10
    float MaxDistance;            // 22
    float ComfortWidth;           //  8   separation below which no extra pull-back happens
    float DistancePerUnitSpread;  //  0.8
    float FocusHeight;            //  1.4 above ground
    float EdgeInset;              //  4   how close the focus may get to an arena X edge
    public static CameraTuning Default { get; }
}

public readonly struct CameraFrame
{
    Vector3 Focus;  float Distance;
    public Vector3 PositionFor(in CameraTuning tuning);   // Focus + OffsetDirection * Distance
}

public static class CameraFraming
{
    public static CameraFrame Compute(IReadOnlyList<Vector3> targets,
                                      in CameraTuning tuning, in ArenaBounds bounds);
}
```

Rules:

```
no targets → focus = (bounds.Center.x, bounds.GroundY + FocusHeight, 0), distance = BaseDistance
otherwise:
  minX, maxX  = extents of targets on X only
  spread      = maxX - minX
  distance    = Clamp(BaseDistance + Max(0, spread - ComfortWidth) * DistancePerUnitSpread,
                      MinDistance, MaxDistance)
  focusX      = (minX + maxX) * 0.5
  lo, hi      = bounds.MinX + EdgeInset, bounds.MaxX - EdgeInset
  focusX      = lo > hi ? bounds.Center.x : Clamp(focusX, lo, hi)
  focus       = (focusX, bounds.GroundY + FocusHeight, 0)
```

Focus Z is always 0: the depth band is fixed (§2.1), so the camera never tracks depth — that stability
is a feature, not an omission.

**Tests:** empty and null target lists; one target centres on it; two targets take the midpoint, not
a count-weighted average (verify with three targets clustered on one side); distance grows only past
`ComfortWidth`; distance clamps at both ends; focus clamps near an edge; an arena narrower than
`2 × EdgeInset` centres instead of inverting.

**Commit:** `M1: camera framing maths in Core`

---

## Task 15 — Presentation: the camera rig and readable shadows

**File:** `Presentation/Cameras/CameraRig.cs`.

- Serialized tuning fields → `CameraTuning`; `[SerializeField] SimulationDriver _driver;`,
  `[SerializeField] Camera _camera;`, `[SerializeField] float _damping = 0.15f;`
- `LateUpdate`: collect character positions from `_driver.Characters`, `CameraFraming.Compute`,
  `Vector3.SmoothDamp` toward `frame.PositionFor(tuning)`, then look at `frame.Focus`.
- **Snap without damping on the first frame** so the camera does not fly in at scene start.

Then tune the scene for D14/§2.3 — hard-edged grounded shadows are the primary depth cue:

- Directional light: shadows **Hard**, strength 1, angled so shadows fall clearly and are not hidden
  under the character.
- In the URP asset: shadow distance short enough (~30–40) that cascades give crisp shadows in the
  play area rather than soft blobs.

**Done when:** with two players far apart the camera pulls back and holds both; walking to an arena
edge stops the camera at the inset; and a jumping character's shadow **stays on the ground and
separates from their feet** — attach `capture_game_view` frames showing both a grounded and a mid-air
character.

**Commit:** `M1: shared camera rig with grounded shadow lighting`

---

## Task 16 — Close out M1

1. Full `run_tests` — report the summary line.
2. Play mode with two players (keyboard + gamepad if one is available, otherwise report that only the
   keyboard path was exercised). Console must be clean.
3. Update the progress table in `CLAUDE.md`: M1 → complete, M2 → next.
4. Report anything that felt wrong to build. Tuning numbers in this document are starting points
   chosen on paper, not from play — if movement feels bad, **say so with specifics**; do not silently
   retune, and do not treat the numbers as sacred either.

**Commit:** `M1: movement and camera complete`

---

## Escalate rather than solve

- Any need for a new assembly, or a new `.asmdef` reference.
- Any urge to put a `MonoBehaviour` in Core, or to make a Presentation component write gameplay state.
- Anything touching combat verbs, damage, characters as content, story, or art direction — M2 onward,
  and several of those need Michael's input before they can be designed at all.
