# Handoff — M2: combat core

> **Status — complete (2026-08-18).** Tasks 17–25 shipped, plus a playtest-directed movement
> redesign: whiffed swings never root (Lights free, Heavies at 80%), airborne swings hang then
> fall, the lunge never targets a partner, hitstop halved. Task 26 (aerials) slipped to M3 exactly
> as planning decision 10 pre-authorised. Task 27's tuning session was deferred by Michael until
> more mechanics and art exist — combat runs on his play-approved paper values, all living in
> `Assets/_BattleBomb/Data/Combat/DefaultKit.asset`.

Continues `docs/HANDOFF-M1.md` (tasks 7–16). Same rules, same tools, same reporting format — re-read
`docs/HANDOFF.md`'s "Non-negotiable rules" and "Tools" sections before starting; they are not
repeated here. The combat design itself is **D19–D21**; nothing below overrides them.

**M2 is done when:** either player can fight training dummies with the grounded D19 kit — the
`L-L-L` chain, the `L-L-H` launcher, and charged Heavy — with the soft lunge closing depth gaps,
depth-limited hit resolution, knockback and launches on the dummies, partner shoves (D21), hitstop,
and floating damage numbers (D20). Every piece of combat maths lives in Core under EditMode tests,
and damage flows through the full §4 pipeline shape even though every elemental multiplier is
neutral until M5.

---

## Decisions taken while planning this milestone

Recorded because they are load-bearing and a future session will need the reasoning. 1–2 are
Michael's, promoted to the decision log; the rest follow from the locked decisions.

1. **Damage numbers on every hit.** *Resolved by Michael, 2026-08-17 — recorded as D20.* Size is a
   renderer parameter from day one; the settings toggle arrives with the settings menu.

2. **Partner contact is knockback, never damage.** *Resolved by Michael, 2026-08-17 — recorded as
   D21.* Hit resolution distinguishes target kinds from the start.

3. **M2's targets are training dummies, not enemies.** M3 owns AI; hit resolution needs bodies to
   hit now. A dummy is health plus the existing `CharacterMotor` stepped with idle commands, so
   knockback, launches, gravity, ground snap, and arena clamping are all reused rather than
   duplicated — and the launcher and slam have something visible to do.

4. **Block ships in M3, not M2.** Nothing attacks the player in M2. A guard with nothing to guard
   and a perfect-block with nothing to time against can be neither felt nor verified. The verb is
   reserved (D17) and the combat machine leaves the seam.

5. **Magic, mana, infusions, and status reactions ship in M5, per the build order.** The damage
   pipeline still carries element from day one — with every multiplier neutral — so M5 fills values
   into a shape that already exists instead of retrofitting one.

6. **Damage is a flat number per attack in M2.** Weapon-derived damage is M4's job; `AttackTuning`
   carries a base damage that M4 will re-derive from the equipped weapon. The same applies later to
   D19's derived Equipment damage.

7. **Hitstop is simulation steps, not `Time.timeScale`.** A timescale flinch is global,
   presentation-flavoured, and non-deterministic across future clients (D10). Freezing the attacker
   and the victim for N fixed steps is data: deterministic, testable, and per-character.

8. **Combat steps before movement inside a character's step.** The combat machine decides each step
   whether the motor sees the stick or a lunge. One direction of authority; the motor never asks
   the combat state anything.

9. **The stick aims only at attack start.** D19: held direction turns the attack and its lunge,
   nothing else. Facing and the lunge target are chosen on the step an attack begins and are fixed
   for its duration — no steering mid-swing.

10. **Aerials are M2's cut line.** Tasks 25–26 (charged Heavy is authored earlier; Jump→Light pop
    and Jump→Heavy slam) land after the grounded kit is verified. If the milestone drags, they slip
    to early M3 without renegotiating anything — they reuse every system the grounded kit builds.

---

## Task 17 — Core: element and the damage pipeline

**Files:** `Core/Combat/Element.cs`, `Core/Combat/ElementalMultipliers.cs`,
`Core/Combat/DamageCalculator.cs`, plus tests.

```csharp
public enum Element { None = 0, Fire, Water, Electric, Earth }   // §4 baseline roster

public readonly struct ElementalMultipliers      // one shape for resistance AND climate
{
    public ElementalMultipliers(float fire, float water, float electric, float earth);
    public float For(Element element);           // Element.None always returns 1
    public static ElementalMultipliers Neutral { get; }   // all 1
}

public static class DamageCalculator             // §4, exactly, in order
{
    public static float Resolve(float baseDamage, Element element,
                                in ElementalMultipliers resistance,
                                in ElementalMultipliers climate,
                                float gearMultiplier);
    // = max(0, baseDamage) * resistance.For(element) * climate.For(element) * max(0, gearMultiplier)
}
```

**Tests:** neutral everything passes base damage through unchanged; resistance scales only the
matching element; climate likewise; the gear multiplier scales everything; all stages compound;
`Element.None` damage ignores resistance and climate entirely; the result is never negative.

**Commit:** `M2: element and the damage pipeline in Core`

---

## Task 18 — Core: attack data and the combo kit

**Files:** `Core/Combat/AttackTuning.cs`, `Core/Combat/CombatKit.cs`, plus tests.

```csharp
public readonly struct AttackTuning
{
    int   StartupSteps;      // steps before the hit window
    int   ActiveSteps;       // hit-window length
    int   RecoverySteps;     // steps after, before Ready / the next chain link
    float Damage;            // flat for M2 (planning decision 6)
    float ReachX;            // hit box extent in front of the attacker
    float DepthTolerance;    // §2.2 — ~1 lane unit; melee's identity
    float LungeDistance;     // max distance the soft lunge may close
    int   MaxTargets;        // 1–2 for Light; larger for Heavy's cleave
    float KnockbackSpeed;    // horizontal impulse away from the attacker
    float LaunchSpeed;       // vertical impulse; nonzero only on the launcher and slam
    int   HitstopSteps;      // applied to attacker and victims on contact
}
```

`CombatKit` is the combo table (D19: a new combo is data, never code): an ordered array of chain
positions where each position maps **Light → next chain link** and **Heavy → ender, if any**, plus
the standalone Heavy, the charged Heavy (`ChargeSteps` to reach it), and `ComboWindowSteps` — how
long after an attack's recovery a follow-up press still chains instead of restarting. The exact
shape may be refined during implementation; the tests pin the behaviour, not the layout:

**Tests:** the default kit exposes `L-L-L` (three links), `L-L-H` (the position after two Lights
maps Heavy to a launcher with `LaunchSpeed > 0`), and a charged Heavy; every authored attack has
positive step counts and non-negative distances.

**Commit:** `M2: attack data and the combo kit in Core`

---

## Task 19 — Core: the combat machine

**Files:** `Core/Combat/AttackPhase.cs`, `Core/Combat/CombatState.cs`,
`Core/Combat/CombatMachine.cs`, plus tests.

```csharp
public enum AttackPhase { Ready, Startup, Active, Recovery, Charging }

public readonly struct CombatState
{
    AttackPhase Phase;
    int  StepsInPhase;
    int  ComboIndex;         // position in the chain; 0 when Ready
    CommandButtons Buffered; // Light or Heavy remembered mid-attack, None otherwise
    int  BufferedFor;        // steps left on that memory, 0 = none
    int  ChargeSteps;        // grows while Heavy is held in Charging
    int  HitstopSteps;       // while > 0, the whole character is frozen (motor included)
    public static CombatState Ready { get; }
}

public static class CombatMachine
{
    public static CombatStepResult Step(in CombatState state, in PlayerCommand command,
                                        in CombatKit kit);
}

public readonly struct CombatStepResult
{
    CombatState  State;
    bool         AttackStarted;    // this step opened a startup — aim + pick the lunge target now
    bool         HitWindowOpened;  // first Active step — hit resolution runs exactly once here
    AttackTuning Attack;           // valid when either flag is set or an attack is in flight
    bool         MovementLocked;   // motor must ignore the stick this step
}
```

Behaviour to build and pin:

- Light from Ready starts link 1. Light pressed during link N's Active/Recovery **buffers** and
  chains into link N+1 when recovery ends — the same buffered-input feel the jump already has.
- Heavy after two Lights (buffered the same way) fires the launcher; Heavy from Ready starts the
  standalone Heavy; **holding** Heavy from Ready enters Charging, and release fires — the charged
  variant if `ChargeSteps` reached the kit's threshold, the ordinary Heavy otherwise.
- The chain resets to Ready when recovery ends with nothing buffered and `ComboWindowSteps` passes.
- `HitstopSteps > 0` decrements and suppresses everything else — phases do not advance during
  hitstop, and the buffer's countdown also pauses so hitstop never eats a buffered input.
- Jump and Block inputs are ignored by the machine in M2 (Block is M3; aerials arrive in Task 26).

**Tests:** the full `L-L-L` timeline step by step; `L-L-H` produces the launcher; mashing Light
mid-startup does not skip links; a press after `ComboWindowSteps` restarts at link 1; charge below
threshold fires ordinary Heavy, at threshold fires charged; hitstop freezes phase and buffer
countdowns exactly `HitstopSteps` steps; determinism — identical command sequences from an
identical state twice over produce identical states.

**Commit:** `M2: the combat machine in Core`

---

## Task 20 — Core: hit resolution and the soft lunge

**Files:** `Core/Combat/HitResolver.cs`, plus tests.

Pure geometry, no registries or objects — callers hand in positions:

```csharp
public static class HitResolver
{
    // Fills `hits` with candidate indices, nearest-first on |Δx|, capped at attack.MaxTargets.
    public static int Resolve(Vector3 attacker, Facing facing, in AttackTuning attack,
                              IReadOnlyList<Vector3> candidates, IList<int> hits);

    // The soft lunge (§2.2): nearest candidate in front within ReachX + LungeDistance,
    // or -1. Chosen once, on the step the attack starts (planning decision 9).
    public static int LungeTarget(Vector3 attacker, Facing facing, in AttackTuning attack,
                                  IReadOnlyList<Vector3> candidates);
}
```

A candidate is hit when it is **in front** (Δx has the facing's sign, with a small behind-tolerance
so a target flush against you still counts), within `ReachX` on X, within `DepthTolerance` on |Δz|
— melee is depth-limited, §2.2's identity rule — and within a generous vertical tolerance on |Δy|
so a just-launched target is not magically unhittable.

**Tests:** aligned-but-deep candidates are missed (the rule that makes melee melee); nearest-first
ordering; `MaxTargets` caps a crowd; facing excludes targets behind; the behind-tolerance catches a
flush target; lunge picks the nearest in-range target and returns −1 when none qualifies; a
launched (elevated) target within tolerance is still hit.

**Commit:** `M2: hit resolution and the soft lunge in Core`

---

## Task 21 — Core: health and hit application

**Files:** `Core/Combat/Health.cs`, `Core/Combat/HitApplication.cs` (name flexible), plus tests.

```csharp
public readonly struct Health
{
    public Health(float max);                 // starts full; ArgumentException if max <= 0
    float Current;  float Max;
    public bool IsDepleted { get; }
    public Health Damaged(float amount);      // clamps at 0; negative amounts clamp to 0 damage
}
```

Hit application is the small pure function that turns a landed hit into consequences, so Gameplay
wires rather than decides: given the attack, the attacker's position/element/gear multiplier, and
the target kind (**enemy or partner**, D21) plus its resistances and the climate → resolved damage
(0 for partners), a knockback velocity pointing away from the attacker (X and Z, so a shove also
works in depth), `LaunchSpeed` into vertical velocity, and `HitstopSteps` for both parties.
Injecting the impulse is a `MotorState` with replaced velocity — the motor itself is untouched.

**Tests:** damage reaches `Health` through the full Task 17 pipeline; partners take zero damage but
full knockback (D21); knockback points away from the attacker on both axes; the launcher's
`LaunchSpeed` becomes upward velocity and the motor's own gravity brings the target back down;
depleted health clamps at zero and reports `IsDepleted`.

**Commit:** `M2: health and hit application in Core`

---

## Task 22 — Gameplay: combat authoring assets

**Files:** `Gameplay/Data/AttackDefinition.cs`, `Gameplay/Data/CombatKitDefinition.cs`; extend
`CharacterDefinition` with a `[SerializeField] CombatKitDefinition _combatKit` and surface it in
`ToRuntime` alongside the movement tuning (shape as fits — a second method is fine).

Same pattern as Task 11: ScriptableObject in, plain struct out, `ToRuntime()` at the seam. Author
the Default character's kit under `Assets/_BattleBomb/Data/Combat/`: three Light links, the
launcher, the standalone and charged Heavy. Starting numbers on paper (60 Hz steps): Light ~5/4/8
startup/active/recovery, damage 5, reach 1.6, depth tolerance 1, lunge 1.2, 2 targets max;
launcher `LaunchSpeed` ~9; Heavy ~12/6/16, damage 12, reach 2, lunge 2, 4 targets; charged ~×2
damage, single target; hitstop 3 (Light) / 5 (Heavy). **These are paper values** — Task 27's live
session with Michael replaces them; do not defend them.

**Tests:** `CreateInstance` round-trip tests as in Task 11 — never load assets from disk in a test.

**Commit:** `M2: combat authoring assets`

---

## Task 23 — Gameplay: combat wired into the driver, and training dummies

**Files:** `Gameplay/Characters/CharacterActor.cs` (extend), `Gameplay/Characters/TrainingDummy.cs`,
`Gameplay/Characters/TargetRegistry.cs`, `Gameplay/Simulation/SimulationDriver.cs` (extend).

- `CharacterActor.Step` becomes: combat machine first (planning decision 8); if `MovementLocked`,
  the motor receives a synthesized command — no stick — plus lunge velocity toward the target
  chosen at `AttackStarted`; hitstop freezes both machines.
- `TrainingDummy`: `Health` plus a `MotorState` stepped with `PlayerCommand.Idle` — it stands
  still, falls, gets shoved, and gets launched entirely through existing motor code (planning
  decision 3). Registers in a `TargetRegistry` on the driver (same shape as `CharacterRegistry`;
  M3's enemies will reuse it). Dummies respawn their health after a few seconds so a tuning session
  never needs a scene reload.
- On `HitWindowOpened`, the driver resolves hits against dummies **and the other player** (partner
  kind, D21), applies Task 21's consequences inside the fixed step, and raises a single
  `HitLanded(attacker, target, damage, position)` C# event for Presentation/UI to observe.
- Scene: two or three dummies spread across X and depth — at least one placed deep specifically so
  the depth-miss is demonstrable.

**Done when:** `run_tests` green; in Play mode (slow-paced checks only — positions and registry
counts via `eval`) dummies register, and a scripted Light press reduces a dummy's health.

**Commit:** `M2: combat stepped by the driver against training dummies`

---

## Task 24 — Presentation and UI: swings, hitstop, damage numbers

**Files:** `Presentation/Characters/AttackTelegraph.cs` (placeholder swing readability — a brief
arc/box flash in front of the attacker during Active; real animation is later art),
`Presentation/Characters/HitFlash.cs` (dummy flashes on `HitLanded`), and
`UI/Combat/DamageNumbers.cs` (D20: world-anchored floating text at the hit position, drifting up
and fading, **font size exposed as a serialized parameter** — the settings menu will bind it
later).

All three observe — `HitLanded` and public state only. No gameplay writes, per the assembly rules.
Partners shoved under D21 get the flash but **no number** (no damage resolved).

**Done when:** visually verified — this is fast motion, so per the live-verification protocol this
lands on **Michael's checklist**, not on frame sampling.

**Commit:** `M2: hit feedback — telegraphs, flashes, damage numbers`

---

## Task 25 — Charged Heavy, end to end

Machine support exists from Task 19 and data from Task 22; this task makes it *legible*: a charging
tell on the character (scale/tint pulse in Presentation — placeholder, same caveat as Task 24), the
release firing the charged single-target hit, and its bigger number (D20 doing its job).

**Commit:** `M2: charged Heavy`

---

## Task 26 — Aerials: the pop and the slam

D19's aerial pair, last because it reuses everything: **Jump→Light** pops the struck target up
briefly (a hit or two of air time, deliberately short of juggling); **Jump→Heavy** slams an AoE
centred where the grounded shadow marks the landing (D14 as the aiming reticle) — resolve it as a
radial hit around the landing point on the step the attacker lands. The combat machine learns
airborne context from the motor's `IsGrounded` — passed in as a parameter, keeping the machine
pure.

**Tests:** airborne Light selects the aerial attack; the slam resolves at the landing position
against multiple targets; grounded combos are unchanged by the additions.

**Commit:** `M2: aerial pop and slam`

---

## Task 27 — Close out M2

1. Full `run_tests` — report the summary line.
2. A tuning session with Michael: combat is fast motion, so hand him the checklist — chain rhythm
   of `L-L-L`, the launcher pop, charge-and-release weight, lunge feel, hitstop, knockback
   strength, number readability at his screen size, and a partner shove (D21). Iterate numbers in
   the Task 22 assets live with him; **record the settled values in `GAME_DESIGN.md` §2.5**,
   replacing the ⚠ NEEDS TUNING TARGETS marker.
3. Update the progress table in `CLAUDE.md`: M2 → complete, M3 → next.
4. Report anything that felt wrong to build.

**Commit:** `M2: combat core complete`

---

## Escalate rather than solve

- Any need for a new assembly or `.asmdef` reference.
- Any urge to give the combat machine scene knowledge, or Presentation a write path.
- Anything touching Magic, mana, statuses and reactions, Block, enemy AI, weapon-derived damage, or
  real animation — those belong to M3–M5, and several need Michael's input first.
- Any real animation or art-direction call — telegraphs and tells in M2 are explicitly placeholder.
- If the combo table's data shape fights the "a new combo is data" rule (D19), stop and report
  rather than special-casing an attack in code.
