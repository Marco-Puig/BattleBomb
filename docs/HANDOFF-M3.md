# Handoff — M3: enemies

> **Status — in progress.** Task 28 complete and play-approved, plus three playtest-directed
> follow-ups: the lunge works airborne (sheds carried momentum, full grounded range), knockback
> inherits attacker momentum through a saturating curve (cap 6, half at 6 u/s — the two consts in
> `HitApplication`), and aerials knock back ~10% harder than their ground counterparts (Michael's
> rule, authored as data). Task 29 complete: `PlayerCondition` in Core, vitals on
> `CharacterDefinition`, the actor gates commands while staggered/down and exposes
> `ApplyEnemyHit` — the seam tasks 31–33 call. **Task 30 was built, then cut by D26** (Block
> removed with the input verb — fc9dbb6 reverted by a2d5d11); defence is the defence stat and
> movement. Task 31 complete: the four archetype brains as one pure machine in `Core/Enemies`
> (grunt/brute close on both axes and swing from reach; ranged/caster hold a standoff band, fire
> only from inside it, and never close depth; brutes never flinch; sticky `TargetSelection` skips
> downed players). Task 32 complete: `ProjectileState`/`ProjectileSimulation` — straight-line
> fixed-step flight aimed at the fire-time target position, spherical hit test (a bolt flies at
> firing height, so jumping over it works), life expiry. Task 33 complete: `EnemyDefinition` →
> `EnemySpec`, `EnemyActor` on the shared motor registered through the generalised
> `TargetRegistry` (`ISimTarget`), `EnemySpawner` with authored entries and `ResetBrood`, the
> driver stepping brains/projectiles and resolving both hit directions; dummies left the scene
> (the prefab remains for tuning); four abstract archetype assets in `Data/Enemies/` on paper
> numbers. Live-verified end to end: enemies spawn, chase, telegraph, hit, down a player, and
> retarget the survivor. **Plus D28**, Michael's playtest direction: turn-taking attack tokens
> (waiting melee circles a hover ring), hit-and-peel cooldowns, depth-drifting archers, seeded
> per-enemy desync, and hops through the shared motor — live-verified alternating grunts and
> drifting archers. Task 34 complete: per-archetype silhouettes (capsule/cylinder/sphere/cube,
> distinct colours), `EnemyVisual` as the enemy body's single tint writer (rest colour, telegraph
> ramp + windup swell, hit flash, depleted grey — one writer because two flicker, the ChargeTell
> lesson) with interpolated follow, `TelegraphTell` drawing the brute's ground impact box heating
> ember→orange, the caster's tell ending in a white flicker, projectile bolts sized by damage with
> grounded shadow blobs (bolt renders chest-high, sim flies at foot height), and `PlayerHealthBars`
> top-centre with the downed state spelled out. Live-verified: bolts + blobs in flight, the marker
> underfoot mid-telegraph, players flashing on enemy hits, bars tracking to DOWN — and Michael
> passed all five readability checks. Task 35 complete: `ReviveChannel` and `AttemptCountdown`
> pure in Core (the channel's whole rulebook — starts on Light beside a downed partner, advances
> only in-control/in-range/same-target, restarts from zero when broken; contextual Light channels
> only from combat-Ready so charges keep their buttons), revive numbers authored on
> `CharacterDefinition` (channel 90, range 1.8, half health, grace 60), the driver passing each
> player its revive context and applying completions (one actor never rewrites another), the
> attempt-over beat (120 steps) resetting players to spawn + `ResetBrood` + clearing bolts via the
> `AttemptReset` event, `DownedPose` laying the body flat on the sim position, HitFlash holding
> downed players grey, and `ReviveHud` (DOWN marker + channel bar + WIPED OUT banner).
> Live-verified through two full wipe cycles: downed pose + marker, reset to spawn at full
> health, encounter respawning and re-engaging, zero errors. Michael play-approved the whole
> flow, then directed **D29**: revive is now Castle Crashers CPR — each Light press is a pump
> (paper 10 to complete), the mash pace prices the restored health (75 steps → 65%, 240 → 25%,
> linear between), and silence drains a pump per 30 quiet steps until the channel drops. All
> authored on `CharacterDefinition`; the break rules and grace stand. The mash feel is on
> Michael's mega-pass checklist. Task 36 complete: `Core/Loot` — `DeterministicRandom` (seeded
> xorshift value-struct, the project's first RNG, decision 11) and `DropRoll` (chance
> 10%+5%×rank capped 60%; quality = [0.5,1.5) spread × progress × difficulty × multipliers ×
> elite bonus, slots at paper 1 until M7; both draws always consumed so the stream never depends
> on outcomes). Driver owns the seed (serialized), raises `EnemyDied` (rank/XP/IsElite=false/
> position) when the 45-step dying beat ends, rolls once per kill, spawns the gold `DropPickup`
> token at the corpse, and resolves free grabs in the fixed step (registry order wins ties,
> downed players grab nothing, 0.9 radius); health bars show a per-player loot count; wipe
> resets clear tokens and counts; dying enemies release their melee token. Live-verified by
> reflection kill: beat → despawn → token at corpse → unclaimed outside radius → grabbed the
> step a player stands on it → cleared by the wipe. Task 37 complete:
> `Core/Movement/BodySeparation` — pairwise planar push-apart (personal radius 0.45, push cap
> 0.03/step, fast bodies at ≥4.5 u/s receive nothing but still push others — knockback and the
> launcher are never cushioned; a perfectly stacked pair splits along X deterministically),
> applied by the driver after all motors step, players then enemies in registry order. Never a
> Rigidbody — the M1 seam made real. Live-verified: a full brawl's closest pair held at 1.04
> (above the 0.9 overlap line) where crowds previously stacked into one point. Task 38
> (close-out) awaits Michael's mega pass.

Continues `docs/HANDOFF-M2.md` (tasks 17–27). Same rules, same tools, same reporting format —
re-read `docs/HANDOFF.md`'s "Non-negotiable rules" and "Tools" sections before starting; they are
not repeated here. The design authority is **D22** (archetypes × regions × ranks, elites), **D23**
(loot flow), **D25** (partner revive), and **D26** (no Block — defence is stats and movement) —
nothing below overrides them. Check `editor_status` before any recompile or synchronous test run (Michael plays between
sessions), and follow the live-verification protocol in `CLAUDE.md`: slow-paced checks by `eval`,
fast motion by Michael's checklist.

**M3 is done when:** two players can fight a spawned, mixed enemy group and every archetype applies
its designed pressure — **grunts** crowd through the same depth-limited melee rules the players
live under, **ranged** pokes across depth and punishes standing still, a **caster** lobs slow,
hard-hitting elemental artillery behind the longest telegraphs, and a **brute** forces jump- and
depth-dodges. Players take damage and can be staggered, downed, revived by their partner (D25),
and reset when the attempt fails. Enemies telegraph, flinch, die, and roll drops through D23's
seam. The aerial pop and slam ride in from M2. Every piece of new logic lives in Core under
EditMode tests, and `run_tests` is green.

---

## Decisions taken while planning this milestone

Recorded because they are load-bearing and a future session will need the reasoning.

1. **Enemies are commands through the same motor.** An enemy brain emits movement intent and attack
   triggers; the motor beneath is the player's `CharacterMotor`, exactly as `TrainingDummy` already
   proved. Knockback, launch, gravity, ground snap, and arena clamping are inherited, not
   duplicated — and a brain that only emits intent is network-shaped (D10) by construction.

2. **The enemy attack cycle is its own small machine, but it reuses `AttackTuning`.** Enemies do
   not run the player's combo machine — they have no combos. Their cycle is
   Approach → Telegraph → Active → Recovery → Cooldown, and the telegraph **is** the attack's
   `StartupSteps`, authored long. One tuning vocabulary for every attack in the game.

3. **Enemies resolve hits with the same `HitResolver`.** D22 says the grunt is "depth-limited like
   the player (§2.2)" — so it literally uses the player's reach geometry. No parallel resolver.

4. ~~A perfect block staggers any attacker.~~ **Superseded by D26** — Block is cut. The brute is
   simply uninterruptible: jump and depth are the answer, exactly his archetype's job.

5. ~~Block guards the front only.~~ **Superseded by D26.**

6. **Players get a short post-hit grace.** A few steps of invulnerability after taking a hit, so a
   crowd of grunts cannot stunlock a player to death from one mistake. Enemies get no grace — the
   player's crowd control (cleaves, launcher, slam) is supposed to dominate a group.

7. **Revive is a short interruptible channel.** D25 says "walking over and pressing Light" — the
   press starts a channel of a second or two during which the reviver cannot act, and taking a hit
   interrupts it. That keeps enemy pressure meaningful during a revive without inventing new verbs.

8. **Elites defer to M6, but the seam ships now.** An elite's entire payoff is loot (D22 — it
   drops what it wears), which does not exist until M4–M6. The death event carries an `IsElite`
   flag from day one so nothing is retrofitted; no elite spawns until the loot loop can pay it.

9. **Rank is a number on the enemy definition, nothing more.** It scales stats and feeds D23's drop
   chance. The composition ladder — which ranks appear where in a level — is encounter authoring
   and lands with chapters (M7). Same for difficulty multipliers: the *slot* exists in the drop
   maths, but the difficulty system itself is M7's.

10. **Drops are placeholder pickups proving D23's flow.** One roll per kill in Core, a physical
    pickup in the world, first grabber keeps it — chance scaling with rank, the whole shape — but
    the pickup is an abstract token until M4's item generation exists. XP is likewise a payload on
    the death event with no ladder consuming it; D24's leveling arrives with M4's stat system.

11. **Deterministic randomness enters Core here.** The drop roll is the project's first random
    number. Core cannot touch Unity `Random` (rule 1), so this milestone introduces a small seeded
    RNG (xorshift-class) injected where needed — which is exactly what D10's determinism wants
    anyway. Gameplay owns the seed.

12. **Attempt-over is a sandbox reset.** Both players down (or solo down) → short beat, then
    players and encounter respawn. Real run lifecycle — checkpoints, retries, win/loss — is
    mode-owned (D4) and arrives with M7. Do not build a checkpoint system now.

13. **Placeholder enemies stay abstract.** They are named Grunt, Ranged, Caster, Brute — never
    skeletons, goblins, or anything lore-flavoured. Region families resolve with the story (§9),
    and placeholder lore only gets thrown away.

---

## Task 28 — Aerials: the pop and the slam

M2's task 26, slipped here exactly as its planning decision 10 pre-authorised. D19's aerial pair,
first because it completes the player's kit before anything starts hitting back.

- `CombatMachine.Step` gains an `isGrounded` parameter (kept pure — the caller passes it). Airborne
  Light selects the kit's **AerialLight**; airborne Heavy selects **AerialHeavy**, the slam.
- **Jump→Light** pops the struck target up briefly — a small `LaunchSpeed`, deliberately short of
  juggling (extended air tech is a future skill layer, not M3's).
- **Jump→Heavy** slams: the attacker descends fast (actor motion policy, like the M2 air-stall),
  and the hit resolves **on the landing step** as a radial hit centred on the landing point — the
  grounded shadow is the aiming reticle (D14). `HitResolver` gains a radial variant (planar radius
  plus the usual vertical tolerance).
- `CombatKit` gains the two aerial attacks; `CombatKitDefinition` and `DefaultKit.asset` gain their
  authored entries. Paper values: pop ~4/3/8 steps, damage 4, launch ~5; slam damage ~14, radius
  ~2.2, strong knockback, hitstop 5.

**Tests:** airborne Light selects the aerial attack and grounded combos are unchanged; the slam
resolves at the landing position against multiple targets; the radial resolver respects the
vertical tolerance and `MaxTargets`.

**Done when:** tests green, and Michael's checklist confirms the pop's air moment and the slam
landing where the shadow said it would — fast motion, his eyes, not frame sampling.

**Commit:** `M3: aerial pop and slam`

---

## Task 29 — Core: the player can be hurt

**Files:** `Core/Combat/PlayerCondition.cs` (name flexible), plus tests. Wired in
`Gameplay/Characters/CharacterActor.cs`.

Players gain `Health` (the Task 21 struct, reused) and a condition layer around it:

```csharp
public readonly struct PlayerCondition
{
    Health Health;
    int    StaggerSteps;    // > 0: control lost — the actor feeds the motor Idle,
                            // the combat machine resets to Ready, any revive channel breaks
    int    GraceSteps;      // > 0: post-hit invulnerability (planning decision 6)
    bool   IsDown;          // health depleted — no actions until revived (Task 35)
}
```

Taking a hit while not in grace: damage through the full §4 pipeline (player resistance is
`Neutral` until gear exists, M4), knockback through the motor as impulse (mechanism already exists
from D21's shove), stagger steps from the attack, grace steps started. While `IsDown`, nothing
lands and nothing is targetable (Task 31's selection skips downed players).

**Tests:** damage reaches player health through the pipeline; a hit during grace does nothing; a
hit during stagger extends nothing it shouldn't (no stunlock: grace outlasts stagger by design);
depletion sets `IsDown`; stagger counts down and control returns; determinism.

**Commit:** `M3: player health, stagger, and the downed state in Core`

---

## Task 30 — ~~Core: Block and the perfect window~~ · cut by D26

Built (fc9dbb6) and reverted (a2d5d11) the same day: Michael and his collaborator cut Block for
the mobile control budget. The input vocabulary is five verbs; defence is the defence stat (M4)
and movement. Nothing in this milestone references a guard.

---

## Task 31 — Core: the enemy brain

**Files:** `Core/Enemies/EnemyArchetype.cs`, `Core/Enemies/EnemyTuning.cs`,
`Core/Enemies/EnemyState.cs`, `Core/Enemies/EnemyBrain.cs`, `Core/Enemies/TargetSelection.cs`,
plus tests. A new Core folder, not a new assembly.

```csharp
public enum EnemyArchetype { Grunt, Ranged, Caster, Brute }   // D22 — the four pressures

public readonly struct EnemyTuning
{
    EnemyArchetype Archetype;
    float  MoveSpeed;
    float  PreferredRangeX;    // grunt/brute: attack reach; ranged/caster: standoff distance
    AttackTuning Attack;       // StartupSteps IS the telegraph — authored long (decision 2)
    Element Element;           // the region skin's element rides here (None until M5 matters)
    int    CooldownSteps;      // beat between attacks
    bool   Interruptible;      // brutes: false (decision 4)
    int    StaggerSteps;       // flinch length when interrupted
    float  ProjectileSpeed;    // ranged/caster only
}

public enum EnemyPhase { Approach, Telegraph, Active, Recovery, Cooldown, Staggered }

public static class EnemyBrain
{
    public static EnemyStepResult Step(in EnemyState state, in EnemyPerception view,
                                       in EnemyTuning tuning);
    // A player hit interrupts via a separate pure transition:
    public static EnemyState Interrupted(in EnemyState state, in EnemyTuning tuning);
    // → Staggered for StaggerSteps if Interruptible, unchanged otherwise.
}

public readonly struct EnemyPerception   // everything the brain is allowed to know
{
    Vector3 SelfPosition; Facing SelfFacing;
    bool    HasTarget; Vector3 TargetPosition;
}

public readonly struct EnemyStepResult
{
    EnemyState State;
    Vector2    MoveIntent;        // becomes the stick of a synthesized command (decision 1)
    bool       AttackStarted;     // telegraph opened this step
    bool       HitWindowOpened;   // same contract as CombatStepResult — resolve hits once, here
    bool       ProjectileFired;   // ranged/caster: the driver spawns a projectile this step
}
```

Approach behaviour per archetype — this is the whole difference between them:

- **Grunt:** close on X to `PreferredRangeX`, close depth to within the attack's `DepthTolerance`
  (it lives under §2.2 like the player), attack when in reach and off cooldown.
- **Ranged:** hold a standoff band — retreat when the target closes, advance when it flees. Fires
  across any depth (§2.2's ranged identity): no depth-closing at all.
- **Caster:** ranged's movement with a longer telegraph and cooldown — its hit is the hardest
  single poke, so the telegraph must always be visibly answerable by depth or jump.
- **Brute:** grunt's approach, slower, with the long telegraph and no flinch.

`TargetSelection.Choose(self, playerPositions, playerDowned, currentTarget, switchMargin)` is
sticky: keep the current target unless it is downed or gone, or another player is closer by more
than the margin — hysteresis so a co-op pair straddling an enemy doesn't cause flip-flopping.
Downed players are never selected. Players come to the driver from `PlayerRegistry` — a brain
hardcoded to one player is the D10 bug.

During hitstop the enemy state freezes exactly as the player machine does.

**Tests:** each archetype's approach behaviour (grunt closes depth, ranged holds standoff and
never closes depth, brute is slow); telegraph → active → recovery → cooldown timeline; the grunt
does not open a hit window out of reach; `Interrupted` staggers a grunt mid-telegraph and does not
stagger a brute; target stickiness and the downed-player exclusion; determinism — identical
perception sequences produce identical states.

**Commit:** `M3: the enemy brain in Core`

---

## Task 32 — Core: projectiles

**Files:** `Core/Combat/ProjectileState.cs`, `Core/Combat/ProjectileSimulation.cs`, plus tests.

```csharp
public readonly struct ProjectileState
{
    Vector3 Position; Vector3 Velocity;    // aimed at the target's position when fired; no homing
    float   Damage;  Element Element;
    int     LifeSteps;                     // despawn cap
}

public static class ProjectileSimulation
{
    public static ProjectileState Step(in ProjectileState p);   // straight line, fixed step
    public static int HitTest(in ProjectileState p, IReadOnlyList<Vector3> targets, float radius);
}
```

Projectiles cross depth freely — that is the entire mechanical identity of ranged (§2.2), so the
velocity simply points at where the target was at fire time, depth included. A hit lands through
the player's condition (task 29 — grace and the downed state swallow it). Life expiry despawns.

**Tests:** straight-line flight; the hit test respects the radius; depth crossing (a projectile
fired from deep hits a shallow target); expiry; determinism.

**Commit:** `M3: projectiles in Core`

---

## Task 33 — Gameplay: enemy authoring, actors, and the spawner

**Files:** `Gameplay/Data/EnemyDefinition.cs`, `Gameplay/Characters/EnemyActor.cs`,
`Gameplay/Combat/EnemySpawner.cs`, `Gameplay/Simulation/SimulationDriver.cs` (extend).

- **`EnemyDefinition`** (ScriptableObject → `.ToRuntime()` struct, the Task 22 pattern): archetype
  and every `EnemyTuning` number, max health, **rank** (an int — decision 9), **elemental
  resistances** (four floats → `ElementalMultipliers`), element, XP reward. This is D22's
  consequence made real: a new enemy — and later a whole region skin — is an asset, never a class.
  Resistances ship as authored, tested data now; they become player-visible when elemental damage
  exists (M5). Author four assets under `Data/Enemies/`: Grunt, Ranged, Caster, Brute — abstract
  names only (decision 13). Paper values (60 Hz):

  | | HP | speed | telegraph | active | recovery | cooldown | damage | reach | notes |
  |---|---|---|---|---|---|---|---|---|---|
  | Grunt | 30 | 3.0 | 18 | 4 | 20 | 45 | 6 | 1.4 | stagger 20 when flinched |
  | Ranged | 20 | 2.5 | 24 | 1 | 12 | 90 | 5 | — | standoff 6–9, projectile speed 8/s |
  | Caster | 25 | 2.2 | 36 | 1 | 16 | 150 | 10 | — | Magic, projectile speed 6/s |
  | Brute | 90 | 1.8 | 45 | 6 | 30 | 60 | 18 | 2.2 | uninterruptible, heavy knockback |

  Paper values, same status as M2's: Michael's play replaces them, do not defend them.

- **`EnemyActor`**: brain + motor + `Health`, registered in `TargetRegistry` so player attacks
  find it — this is the dummy pattern promoted, and `TrainingDummy` stays as a prefab for tuning
  but leaves the scene. The brain's `MoveIntent` becomes a synthesized `PlayerCommand` into the
  motor; player hits route through `HitApplication` as today, plus `EnemyBrain.Interrupted`.
- **`EnemySpawner`**: an authored list of (definition, position, spawn-delay steps). It spawns,
  tracks its brood, and can `Reset()` for Task 35's attempt-over. Nothing procedural — endless
  mode's generator is a different milestone (D12).
- **Driver:** fixed step order, for determinism: players (registry order) → enemies (registry
  order) → projectiles → hit resolution → deaths. Enemy hit windows resolve against players via
  the same `HitResolver` (decision 3), then the player's condition (Task 29 — grace and the
  downed state swallow hits). The driver builds each brain's `EnemyPerception` — a brain never
  touches the scene; if a brain seems to need more knowledge, extend the perception struct
  deliberately.

**Done when:** `run_tests` green; in Play mode (slow checks by `eval`) spawned enemies register,
approach, and a grunt's landed hit reduces a player's health.

**Commit:** `M3: enemies authored, spawned, and stepped by the driver`

---

## Task 34 — Presentation and UI: reading the fight

**Files:** `Presentation/Enemies/EnemyVisual.cs` and material/prefab work,
`Presentation/Enemies/TelegraphTell.cs`, projectile visuals, `UI/Combat/PlayerHealthBars.cs`.

- **Archetype silhouettes:** four visibly distinct placeholder bodies (shape and colour — abstract,
  decision 13), each with the D15 shadow-proxy capsule. An enemy without a grounded shadow is
  invisible to the game's primary spatial cue (D14) — that rule applies to placeholders too.
- **The telegraph tell is the point of this milestone's presentation.** During Telegraph the enemy
  must read unambiguously: colour ramp / windup pose scale on the body, and for the brute a ground
  marker at the impact zone. The caster's tell gets the most contrast — its hit is the biggest,
  so the telegraph is the player's whole defence (D26: no guard exists).
- **Projectiles cast grounded shadows.** A projectile's depth is combat information (D14): a
  simple blob under each one, moving with it.
- **Hurt feedback:** players reuse `HitFlash`; enemy→player hits pop damage numbers (D20 — every
  hit, both directions).
- **`PlayerHealthBars`:** minimal per-player health readout, observe-only, styled like
  `DamageNumbers` (serialized size — the settings menu binds later).

**Done when:** Michael's checklist — telegraph readability per archetype at real speed, projectile
shadows, damage numbers both ways. His eyes, not frame sampling.

**Commit:** `M3: enemy presentation and player health UI`

---

## Task 35 — Downed, revive, and the attempt reset

**Files:** `Core/Combat/ReviveChannel.cs` (plus tests), `CharacterActor` wiring, spawner/driver
reset, presentation for the downed pose and channel progress.

- **Downed** (Task 29's `IsDown`): the actor ignores commands, the visual drops flat/tinted,
  enemies retarget (Task 31 already excludes downed players).
- **Revive — D25 exactly:** Light is contextual (D17), and a downed partner is the game's first
  interactable. Within revive range of a downed partner, Light starts a **channel** (paper: 90
  steps) instead of an attack; the channel is Core state with tests — progress, broken by the
  reviver being staggered or leaving range, completing into the partner at 50% health with a short
  grace (Task 29's mechanism reused). The reviver cannot attack mid-channel.
- **Attempt over:** both down in co-op, or down solo → a short beat, then the sandbox resets —
  players respawned full, spawner `Reset()`. A placeholder by design (decision 12); no checkpoint
  system.
- **UI:** a progress ring/bar over the channel, and a "partner down" hint pointing the living
  player at the downed one.

**Tests:** the channel's full timeline; interruption by stagger and by range; the revived state
(half health, grace, standing); contextual Light prefers the revive over an attack inside range;
both-down detection; determinism.

**Commit:** `M3: downed players, partner revive, and the attempt reset`

---

## Task 36 — Death and drops: the D23 seam

**Files:** `Core/Loot/DeterministicRandom.cs`, `Core/Loot/DropRoll.cs` (plus tests),
`Gameplay/Loot/DropPickup.cs`, driver/spawner death flow.

- **Enemy death:** health depleted → brief dying beat (corpse fade is presentation) → despawn and
  a single `EnemyDied` event raised inside the fixed step: enemy spec (rank, XP payload — nothing
  consumes XP until M4, decision 10), `IsElite` (always false in M3 — decision 8), and position.
- **`DeterministicRandom`:** the project's first RNG — small, seeded, xorshift-class, in Core,
  injected (decision 11). Unity `Random` stays forbidden.
- **`DropRoll`:** D23's shape as a pure function — inputs (rank, story progress, difficulty,
  active multipliers, elite bonus), one roll per kill: first *whether* (chance scales with rank),
  then *quality* (scales with progress × difficulty × multipliers, elite bonus on top). Quality is
  computed and carried even though M3 has no items to apply it to — the function signature is the
  seam M4/M6 fill. Paper chance: `10% + 5% × rank`, capped sensibly.
- **`DropPickup`:** the roll succeeding spawns a placeholder token in the world at the corpse —
  free-grab, first player within grab radius keeps it, resolved inside the fixed step so
  simultaneous grabs are deterministic (D23: the couch chaos is the feature). A per-player grab
  count on the HUD proves the flow end to end.

**Tests:** the RNG is deterministic from a seed; chance scales with rank and clamps; quality
responds to each input independently and compounds; elite bonus applies; one roll per death;
grab assignment picks exactly one player.

**Commit:** `M3: death, the drop roll, and free-grab pickups`

---

## Task 37 — Soft character separation

**Files:** `Core/Movement/BodySeparation.cs`, plus tests; applied by the driver.

Deferred from M1, due now that crowds exist: overlapping bodies (enemy-enemy, enemy-player,
player-player) push gently apart so a group never stacks into one point. An explicit Core-defined
seam, exactly as M1 recorded — **never a Rigidbody or the physics engine**.

- Pairwise, X and Z only, never Y; push speed capped low so it reads as crowding, not force.
- Knockback and launches dominate: a body moving fast is not separated against its motion — the
  shove and the launcher must never feel cushioned.
- Applied after all motors step, before hit resolution, in deterministic (registry) order.

**Tests:** two overlapped bodies separate; separated bodies stay put; the push never exceeds its
cap; Y untouched; a fast-moving body is left alone; determinism.

**Commit:** `M3: soft character separation`

---

## Task 38 — Close out M3

1. Full `run_tests` — report the summary line.
2. **Michael's session** — the first real fight. His checklist: each archetype's pressure felt
   (crowded by grunts, punished for standing still, forced to prioritise the caster, forced to
   dodge the brute), telegraph readability, being downed and revived under pressure, the attempt
   reset, drop grabs racing the partner, the aerial pair in a real crowd. Iterate the Task 33
   asset numbers live; anything settled gets recorded, the rest stays paper per §2.5's deferral.
3. Update the progress table in `CLAUDE.md`: M3 → complete, M4 → next.
4. Update this file's status header, and record anything that felt wrong to build.

**Commit:** `M3: enemies complete`

---

## Escalate rather than solve

- Any need for a new assembly, `.asmdef` reference, or third-party package — including any
  pathfinding urge. The plane is open ground; straight-line steering is enough for M3, and
  obstacle avoidance is a future milestone's problem if it ever exists.
- Any urge to give a brain scene knowledge beyond `EnemyPerception`, or Presentation a write path.
- Anything touching Magic, mana, statuses and reactions (M5), item generation or inventory (M4/M6),
  elites actually spawning (M6), encounter composition ladders or difficulty tiers (M7).
- Any temptation to name or theme placeholder enemies — region families arrive with the story (§9).
- Real animation or art direction — telegraphs and silhouettes here are explicitly placeholder.
- If enemy authoring starts wanting a per-archetype C# subclass, stop — that is the pillar-3
  violation D22 exists to prevent; the archetype switch lives in one brain, driven by data.
