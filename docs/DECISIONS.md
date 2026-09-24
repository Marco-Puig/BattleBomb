# BattleBomb — Decision Log

One file, append-only, newest at the bottom. Every decision that would otherwise have to be
re-derived from scratch by a future session goes here, with the reasoning that produced it.

Read this before proposing architecture. If you are about to contradict an entry, say so
explicitly and add a superseding entry rather than quietly changing course.

Status legend: **Locked** (decided, build on it) · **Open** (still being decided) · **Superseded**

---

## D1 — Rebuild from scratch on the `Redo` branch · **Locked**

The 2019 codebase is reference material, not a foundation. Do not modify, migrate, or refactor it.

The complete legacy project is preserved permanently on `main` at `12039a6`. To consult it:
`git show main:Assets/Scripts/Health.cs`, or `git checkout main`. Nothing was deleted — it was
moved out of the working tree so day-to-day work isn't paying to index 1.5 GiB and 4,289 `.meta`
files it will never touch.

**Why:** the legacy structure was unworkable — flat `Assets/Scripts`, no namespaces, no assembly
boundaries, the player controller living inside a purchased art asset's demo folder, and four
parallel incompatible damage implementations. The README already anticipated the rebuild.

---

## D2 — Unity 6 LTS · **Locked**

Target the current Unity 6 LTS line. **Unity 6.5 LTS is installed** on the dev machine as of this
entry. The legacy project sat on 2021.1.23f1, a non-LTS release that left support years ago.

**Why:** we are rewriting the code regardless, so migration cost is near zero right now and rises
permanently from here. A multi-year project should not start on an unsupported editor.

**Consequence:** 2019-era vendor packages (A* Pathfinding Project, Steamworks.NET, Unity Standard
Assets) are not expected to compile on Unity 6. Standard Assets in particular no longer ships.
Each is re-evaluated and re-acquired at current versions when a system actually needs it — not
ported forward wholesale.

---

## D3 — Universal Render Pipeline · **Locked**

URP, not Built-in.

**Why:** 2D lighting, Shader Graph, and the platform reach we need for D5. Built-in receives no
further Unity investment. Materials are re-authored as art is ported, which was already happening.

**Consequence:** mobile-viable settings are a constraint, not an afterthought (see D5). Post-processing
is used as enhancement, never as something gameplay readability depends on.

---

## D4 — Story mode first, multiple modes by design · **Locked**

Ship story mode first, authored as **chapters** so content extends without structural change.
Planned to follow: endless procedural (escalating difficulty), PvP, missions.

**Why:** story is the stated vision and gives the clearest finishable target. But "more modes later"
is a known requirement today, and retrofitting mode-awareness into a codebase that assumed one mode
is one of the most expensive refactors there is.

**Consequence — this is a hard architectural constraint:**
- Game mode is a first-class concept from the first line of code. Core systems (combat, character,
  stats, inventory, loot) must never assume story mode.
- Level/encounter construction sits behind an interface. Story supplies authored chapters; endless
  supplies a generator. Neither is the "real" one.
- Win/loss, run lifecycle, and progression persistence are mode-owned, not global.
- Save data is namespaced per mode so endless-mode progress can't corrupt a story save.

---

## D5 — PC/console launch, mobile kept viable · **Locked**

Primary target is PC and console. Mobile is not the launch platform, but the ability to ship there
is treated as a competitive advantage worth protecting.

**Why:** stated priority is PC/console; mobile capability differentiates BattleBomb against similar
games. Keeping the door open is cheap now and very expensive to retrofit.

**Consequence:**
- The new Input System, action-based, from day one. No `Input.GetKey` / `GetAxis` / `GetButton`
  anywhere in gameplay code — the legacy project's single worst coupling.
- Platform services (Steam achievements, IDs, leaderboards) live behind an interface. Steamworks is
  one implementation. A mobile or console build must compile with Steamworks absent entirely.
- UI is resolution- and aspect-agnostic, with touch-viable hit targets.
- Performance budget is set by the weakest intended target, not by the dev machine.

---

## D6 — Plan the full game before building · **Locked**

Produce a complete game design document first, then build in dependency order against it. The plan
is expected to change; it is a living document, not a contract.

**Why:** explicitly requested. The 2019 build stalled partly from building systems without a target
for them to serve.

---

## D7 — EditMode tests on a Unity-free Core · **Locked**

Pure game logic — damage, stats, inventory, combo state, progression — lives in a Core assembly with
no scene or `MonoBehaviour` dependencies, covered by fast EditMode tests. MonoBehaviours are thin
wiring over Core. PlayMode tests deferred until scenes stabilize.

**Why:** best bug-fixing leverage per unit of effort, and the only practical way for a session to
verify a change without a human entering Play mode.

---

## D8 — Assembly definitions from the start · **Locked**

New code is split into separate compiled assemblies via `.asmdef` files rather than everything
landing in Unity's default `Assembly-CSharp`.

An asmdef is a small file that says "this folder compiles into its own DLL, and it may reference
only these other assemblies." Three concrete benefits:
1. **Compile time** — editing a UI script recompiles UI, not the entire project.
2. **Enforced direction** — Core cannot reference Gameplay even by accident. The compiler blocks it,
   so the layering can't erode silently over months.
3. **Testability** — D7 only works if Core genuinely cannot touch `UnityEngine` gameplay types, and
   an asmdef is what makes that a compile error instead of a code-review note.

Exact assembly list is settled alongside the design document.

---

## D9 — The hook · **Locked**

Source: `docs/hook.md`, written by Michael. That file is the canonical statement; this entry is the
architectural reading of it.

> Castle Crashers-style **co-op** combat, Skyrim / Dungeon Defenders-style **gear progression**, and
> an **endless loot chase**.

**Design pillars, in the order they constrain code:**

1. **Co-op is the frame.** Not a mode, not a stretch goal — the combat is co-op combat. See O3;
   this is the highest-blast-radius open question in the project.
2. **Simple, responsive combat.** Explicitly a non-goal to make combat complicated. Low input
   complexity, high game feel. Responsiveness is a hard requirement, which constrains netcode
   (see O3) more than anything else does.
3. **Characters are mechanically similar, visually distinct.** Each has a small ability set —
   elemental attack, movement ability, special — differing in animation, element, and VFX with only
   small gameplay differences. This is the Castle Crashers model, stated explicitly.

   *Architectural consequence, and it is a gift:* there is **one** ability framework, and characters
   are **data**, not code. A new character is a set of authored assets plus tuning values — no new
   C# class. Any design where each character needs its own script is wrong and should be pushed back
   on. This is the single biggest lever on content velocity in the whole project.

4. **Elements interact with the world, not just enemies.** Fire performs better in cold environments;
   enemies carry elemental resistances (Dungeon Defenders model). Element is therefore a first-class
   input to the damage pipeline, not a damage-type enum bolted on — environment, attacker, and
   defender all contribute.
5. **Gear is the retention engine.** Tiers, qualities, and add-ons/enchants. Lower tiers cap out
   lower; rarer gear rolls better stats *and* affords more min/max customization. The stated goal is
   *"make finding gear exciting without making combat complicated"* — depth lives in the loot and
   build systems, deliberately **not** in the moment-to-moment combat.

   *Architectural consequence:* item generation, affix rolling, tier/quality tables, and stat
   aggregation are pure logic with no scene dependency — exactly the Core described in D7, and the
   highest-value EditMode test surface in the project. Build it there and it stays verifiable.

6. **Movement is left-to-right with limited vertical movement.** Level authoring, camera, and combat
   targeting all follow from this — but the precise meaning is still open, see O4.

---

## D10 — Local co-op first, network-shaped architecture · **Locked**

Ship **local couch co-op**. No netcode, no servers, no lag compensation in the initial build. But
write the code as though it will be networked later, because D4 commits to PvP and D9 makes co-op a
pillar.

**Why:** an online-first build would spend the solo dev's limited hours on prediction and
reconciliation before the game is proven fun. A local-only build written carelessly would need a
rewrite to ever go online. This is the option that buys the door staying open at low cost.

**What "network-shaped" concretely requires — these are not optional:**
- Player intent is expressed as **commands/input snapshots**, never as direct calls that mutate state.
  Two local players and two remote players must look identical to the simulation.
- **Simulation is separated from presentation.** Game state advances independently of animation, VFX,
  and audio; those observe state, they never own it. Nothing gameplay-relevant may live only in an
  Animator state or a particle system.
- **No singletons holding mutable game state**, and no `GameObject.Find` reaching across the scene to
  mutate another entity. Both are unfixable under networking.
- Anything player-count-dependent is driven by a **player registry**, never by a hardcoded "the player".
  The legacy code's `FindGameObjectWithTag("Player")` pattern is exactly what must not reappear.
- Time-dependent logic advances on a **fixed simulation step**, not on `Time.deltaTime` in `Update`.

Treat a violation of any of these as a bug, not a style preference. They are cheap now and
effectively unpayable later.

---

## D11 — Two players, ground plane with depth · **Locked**

**Up to 2 players**, local. Movement is a **ground plane with depth**: left/right traverses the level,
up/down moves nearer or further on the plane. Explicitly **not** the 2019 platformer model — jump,
ground checks, and single-line movement do not carry over.

**Why 2 rather than 4:** far simpler shared camera, encounter balance, and readability, and a
realistic scope for a solo dev. The social hook of the genre survives at 2.

**Why depth rather than a 2D line:** two players on a single line constantly occlude each other and
the enemies they are fighting. Depth is what makes shared-screen combat legible.

**Constraint — do not clone Castle Crashers.** It is the reference for *feel and structure*, not a
template to reproduce. Where a mechanic can be meaningfully differentiated, differentiate it.

---

## D12 — Loot chase lives in both story and endless · **Locked**

Story chapters are replayable at **scaling difficulty tiers** with better loot tables (Diablo model),
*and* a separate **endless procedural mode** carries long-term retention.

**Why:** maximises the return on every authored chapter — the same content serves narrative,
difficulty progression, and farming — while still giving endless mode a distinct role.

**Consequence:** difficulty tier and loot table are **inputs** to encounter and reward generation,
never baked into a chapter's authored data. A chapter is content; the tier applied to it is state.
Item generation must be identically callable from story and endless — which is only true if it lives
in the Unity-free Core (D7).

---

## D13 — Movement and combat space · **Locked**

Discrete lanes were proposed and **rejected**: *"I don't want lane changing to feel like an old racing
game."* That objection stands — snapping buys certainty by making movement feel mechanical, and D9
asks for responsive, fluid combat. The certainty is bought with a rule instead.

**The model:**

- **Depth traversal is continuous and free**, for players and enemies alike. No snapping, no rails.
- The playable depth band is a **fixed width** across all areas. Players build one spatial intuition
  and it never changes.
- **Melee is depth-limited.** You cannot reach an enemy several *lane units* away in depth even when
  perfectly aligned left-to-right. A "lane unit" is the balance unit for depth distance — a number in
  the design, not a rail in the world.
- **Projectiles cross depth freely.** This is the mechanical identity of ranged: it trades damage or
  cadence for the ability to ignore the constraint melee lives under.
- **Jump is a general-purpose mechanic**, as in all three inspiration games — traversal over
  obstacles, dodging, combo attacks, boss mechanics. It is not reserved for one defensive job.

**Why this works:** parallax failed previously because it fed the player depth information about the
*camera* while they needed depth information about the *combat plane* — two channels disagreeing.
Fixing that is a matter of putting cues on the plane itself, which is independent of whether movement
is continuous. So continuity costs nothing here, and keeps the feel D9 asks for.

**Implementation consequence:** depth is a real simulation axis with its own position, velocity, and
reach tests — not a rendering trick and not sprite sort order. Melee resolution tests horizontal reach
**and** depth delta against a tolerance. Getting this wrong by folding depth into Y-position is the
single most likely way to end up back where the 2019 build was.

---

## D14 — Reach is communicated by shadows and forgiven by lunge · **Locked**

- **Hard-edged ground shadows** are the always-on diegetic depth cue. A shadow sits *on the combat
  plane*, so its position is depth — the one channel that cannot disagree with the simulation. No HUD
  element carries this information.
- **Attacks soft-lunge**, automatically closing a small depth gap so a near-miss becomes a hit.
- **Depth tolerance is generous** — roughly a full lane unit. Depth positioning is coarse and
  low-effort by design.

**Why:** the 2019 failure was a *cue* problem, not a reach problem — parallax fed camera depth where
combat depth was needed. Shadows fix the cue; lunge absorbs the residual error. Together they let
depth stay continuous (D13) without reintroducing "will this connect?" uncertainty.

**Consequence:** skill lives in timing, left-to-right spacing, and ability use — **not** in fine depth
alignment. This is deliberate: it satisfies D9's "simple and responsive" pillar and it is the only
version of this combat that survives touch controls (D5), where precise depth input is not realistic.

**Art requirement, not a nice-to-have:** every character, enemy, and interactable casts a clearly
readable grounded shadow. An asset without one is incomplete, because it is invisible to the game's
primary spatial cue. Backgrounds must never compete with shadow readability.

---

## D15 — 3D URP world, side-on camera, 2D skeletal characters · **Locked**

Unity 6, URP, **3D** project template. Camera is fixed side-on.

- **Environments are 3D.** This gives the real depth axis D13 requires and real cast shadows D14
  requires, natively, instead of reimplementing both on top of a 2D setup.
- **Characters and enemies are 2D sprite art on a shared skeletal rig** — explicitly *not* low-poly
  3D models. Target look is **HD and modern**: not retro, not Mario-esque, not pixel art.

**Art is authored to a template.** One skeleton, one animation set, per-character artwork fitted to
that template and swapped in — the Castle Crashers painter model. Unity's 2D Animation package
provides this via **Sprite Library / Sprite Resolver** (Spine calls it "skins"): define slots — head,
torso, arm — and swap art per character while every existing animation keeps working.

**Why this matters more than it looks:** it is pillar 3 ("characters are data, not code") extended
into the art pipeline. A new character becomes a set of fitted textures — no new animation work, no
new C# class. It also removes the one real cost objection to 2D art, which is that hand-drawn frames
multiply with roster size.

**Shadow technique — required, not optional.** Billboarded sprites cast flat cutout shadows, which
would silently undermine D14's primary spatial cue. Every character carries an **invisible 3D proxy
mesh (capsule)** that casts the shadow while the visible character stays 2D. The result is a real,
correctly-shaped, light-responsive grounded shadow with 2D artwork.

**Follow-on requirements:** sprites need normal maps to respond to 3D lighting, or a deliberate
unlit-plus-baked-shading treatment; sprite sorting in a 3D scene must be driven by the depth axis
(D13), never by manual sort orders.

---

## D16 — Art pipeline and direction · **Locked**

**Toolchain:** Unity's **2D Animation package** (Sprite Library / Sprite Resolver) to start. Migrate
to Spine only if animation tooling becomes the actual bottleneck.

**Migration risk and its mitigation — act on this from the first character.** Rigs do not port between
Unity and Spine; migrating means re-rigging. That cost is acceptable *only* if it never becomes
re-**drawing**. So source art is authored as **layered files with cleanly separated body parts at high
resolution**, independent of either tool's rig format. The Unity rig is then a consumer of the source
art, not the master copy of it. This is a discipline, not a one-time setup, and skipping it early is
what would make the migration option illusory later.

**Direction:** stylised cartoon with **bold outlines** — saturated palette, expressive, highly legible
when the screen is busy with two players and a crowd of enemies. Legibility is a functional
requirement here, not a taste preference (see D14).

**Lighting consequence:** bold-outline cartoon art fights physically-realistic lighting. Characters
use a flat or cel-shaded lighting response; environments carry the full 3D lighting. The grounded
shadow comes from the invisible proxy mesh (D15) and is independent of how the character itself is
lit, so D14 holds regardless of how stylised the character shading goes.

**Differentiation debt — flagged deliberately.** Of the directions considered, this is the closest to
Castle Crashers, while D11 explicitly requires not cloning it. Differentiation therefore has to be
carried by other means: palette and world identity, character design, UI, and above all the elemental
environment system (§4 of the design doc), which Castle Crashers has no equivalent of. Revisit this
if the game starts reading as a lookalike.

---

## D17 — Six-verb control scheme · **Locked**

Directed by Michael (2026-08-17), superseding the draft vocabulary in `docs/HANDOFF-M1.md`
(`Attack, Elemental, Dodge, Special, Jump, Interact`) before it was ever implemented. The reference
is Castle Crashers' control feel — within D11's "feel and structure" allowance.

The complete input surface after the movement stick:

| Verb | Role | Keyboard | Gamepad |
|---|---|---|---|
| **Light** | Basic melee combo chain; performs **Interact** when an interactable is in range | J | X (west) |
| **Heavy** | Slower, high-commitment attack; launcher / guard-break class | K | Y (north) |
| **Magic** | The character's element, offensively (design §4) | L | B (east) |
| **Equipment** | Use the equipped active item — the loot game's combat verb (design §5) | I | RB |
| **Jump** | Universal (design §2.4, D13) | Space | A (south) |
| **Block** | Hold to guard | Left Shift | LB |

**What is deliberately absent:**

- **No dodge button.** The intended playstyle is forward, not passive. Evasion is jump — D13 already
  makes it general-purpose, and §6's bosses are built on "jump-dodgeable and depth-dodgeable"
  patterns — plus free depth movement. A dodge button would duplicate both and cost the seventh
  button a phone screen does not have.
- **No interact button.** Light is contextual: near an interactable, the same press interacts.
- **No special button.** High-impact, cooldown-gated expression lives inside Magic (charged or held
  casts) and Equipment (item actives). Depth stays in gear, not in inputs (pillar 2).

**Block's design intent:** active defence — timed-block rewards and cancel windows are the M2-era
design space. A turtle button would fight the forward playstyle this scheme exists to serve.

**Mobile constraint — binding on all future combat design:** one thumb operates every button on a
touch screen, so combos are **sequences, holds, and stick+button flavours — never two face buttons
pressed simultaneously**. Chorded inputs are unperformable on touch and must not be designed.

**Supersedes / amends:** the design doc's §3 slot table loses its "Movement ability (dodge/dash)"
slot and its "Special" slot per the above, and gains Heavy, Equipment, and Block; D9.3's ability-set
enumeration ("elemental attack, movement ability, special") is refined accordingly. D13/D14 are
untouched — their defensive verbs were always jump and depth, never a button.

---

## D18 — The standard jump is fixed-height · **Locked**

Directed by Michael after playing M1 (2026-08-17). Tap or hold, the arc is identical — the
early-release jump cut M1 first shipped with is removed. If jump boosts or double jumps ever
arrive, they are explicit mechanics layered on top; the base jump never varies. The jump input
buffer stays: it is about responsiveness, not height.

**Why:** a constant jump is a constant promise. Boss patterns (§6 builds them on "jump-dodgeable"),
platforming gaps, and combo timing can all be authored against one arc, and the player's spatial
intuition never has to account for how long they held a button.

**Consequence:** `MovementTuning` carries no jump-cut multiplier and `CharacterMotor` ignores Jump
release entirely. `Tapped_and_held_jumps_trace_the_identical_arc` pins the rule so variable height
cannot creep back in.

---

## D19 — Combat model: rhythm melee, two element sources, spice-not-meta equipment · **Locked**

Settled with Michael in the M2 design session (2026-08-17). The mix is deliberate: Castle Crashers
supplies the hands, Dungeon Defenders supplies the build, and the depth band plus the elemental
world supply what neither game had (D11's differentiation).

**Attacks.**
- **Light**: fast, hits 1–2 targets, creates space. The rhythm verb.
- **Heavy**: slower, cleaves a crowd, longer lunge; hold to charge a bigger single-target hit —
  **the only hold in the combat kit**. Rejected: charged casts and additional holds; more holds add
  complication, not depth.
- **Combos are pure button sequences**: `L-L-L`, `L-L-H` (launcher), and charged Heavy ship with
  M2. The combo system must make a new combo an animation plus authored data — never new code — so
  the set grows as content.
- **The stick aims; it never picks the combo.** The player is always moving, so held direction
  turns the attack and its lunge — nothing else. Rejected: the depth-shove and any
  stick-modifies-the-combo input.
- **Aerials**: Jump→Light pops enemies up for a hit or two — deliberately short of juggling, with
  extended air time ("tech") reserved as a future skill-expression layer. Jump→Heavy slams an AoE
  where the grounded shadow marks the landing (D14 as an aiming reticle).

**Elements — two sources per player.**
- The **weapon's infusion** applies its status automatically on hit (Dungeon Defenders).
- The **character's own element** casts on Magic with **one press** (Castle Crashers), fuelled by
  slowly regenerating mana; regen and capacity are gear stats, so caster-leaning builds are found
  in loot, not picked from a menu.
- Statuses react (soak + shock → chain stun, and a deliberately small set of similar pairs). A solo
  player self-combos by pairing weapon element against their own; co-op adds the partner as a third
  source. This is the loot hook working: *"this sword is good for me specifically."*

**Block.** Blocks kinetic damage and projectiles — **never magic**, so casters always answer
turtles. A perfect-timed block staggers the attacker and opens a counter. No stamina meter:
blocking is priced by inaction and magic vulnerability, and the reward lives in the timing.

**Equipment stays spice, never meta** — the CC-bow / one-best-tower failure, named by Michael.
1–2 chosen loadout slots from a broad unlockable catalog, with three structural guards:
1. Equipment damage is **derived** — a percentage of weapon damage — so it scales with the build
   and can never out-grow it.
2. Real cooldowns: equipment is a moment, not a rotation.
3. The catalog is utility-first: healing, deployment, control, mobility. The build focus stays on
   armor and weapon.

**Terminology, binding on all docs and code:** *gear* = armor + weapon, the build (§5);
*Equipment* = the loadout button (D17).

---

## D20 — Damage numbers on every hit · **Locked**

Directed by Michael in the M2 design session (2026-08-17). Every landed hit pops a floating damage
number by default. Settings must eventually offer number text-size options and a full disable —
that menu is later UI work, but the number renderer treats size as a parameter from day one so the
option stays cheap.

**Why:** pillar 2 — depth lives in gear. Numbers are how a better weapon is *felt* in combat rather
than read on a stat screen (the Dungeon Defenders answer). Styled small and quick so the screen
stays clean; the settings disable keeps a pure Castle Crashers read available to whoever wants it.

**Consequence:** the simulation reports every hit's final resolved damage outward, and numbers are
presentation-only — nothing in Core or Gameplay ever reads them back.

---

## D21 — Partner contact is knockback, never damage · **Locked**

Directed by Michael in the M2 design session (2026-08-17): *"interrupt their flow in the moment but
not too detrimental."* A player's attacks can shove the other player — real knockback that
interrupts — but never remove health.

**Why:** full pass-through makes the partner a ghost and the couch quiet; full friendly fire
punishes fighting side by side, which D19's crowd-cleaving Heavy actively encourages. Knockback
keeps the chaos and the mutual awareness at zero cost in health.

**Consequence:** hit resolution distinguishes target kinds from M2 onward — enemies take damage and
knockback, partners take knockback only, and no damage number appears over a partner (a shove
resolves no damage for D20 to show). Whether a shove also interrupts an in-progress attack is a
feel knob, tuned live and recorded with §2.5's targets.

---

## D22 — Enemies: archetypes × regions × ranks, plus elites · **Locked**

Directed by Michael in the M3 design session (2026-08-18). Enemy mechanics are **standardized
archetypes**; regions and ranks style and scale them — Castle Crashers' region styling mixed with
Dungeon Defenders' structured difficulty, copying neither roster.

- **~Four archetypes** carry all enemy behaviour: the depth-limited **melee grunt** (§2.2), the
  **ranged** attacker who crosses depth freely, the **caster** whose elemental attacks Block never
  stops (D19 — the anti-turtle), and the slow, telegraphed **brute** dodged with jump and depth.
- **A region authors 2–3 skins** of those archetypes — its element, its attacks, its look. A new
  region's roster is data, never code: pillar 3 applied to enemies.
- **Rank** is the authored composition ladder (grunts early, brutes late) with stat scale;
  **difficulty** (§8's replay tiers) is a separate multiplier applied on top. Two dials — hard
  mode may both swap in meaner compositions and scale them.
- **Elites** are a rare per-spawn modifier: visibly armored, tougher, worth better loot (D23) —
  and they **drop exactly what they wear**, rolled at the elite's boosted quality. Standard
  enemy-armor art covers common elites; when one wears a real player-itemizable piece the
  difference is visible on sight. "More art but bigger payoff" — accepted explicitly.

**Consequence:** enemy authoring is archetype definition (behaviour tuning) + region skin
(element, attacks, visuals) + rank tables — ScriptableObject in, struct out, like everything else.

---

## D23 — Loot flow: shared drops, rank-driven chance, progress-driven quality · **Locked** *(resolves O7)*

Directed by Michael (2026-08-18); Dungeon Defenders' loot progression is the explicit model.

- **Drops are shared and free-grab** (O7 resolved): one roll per drop, it lands in the world, and
  whoever grabs it keeps it. The shared discovery moment — and the couch chaos — beat instanced
  fairness.
- **Drop chance scales with enemy rank. Quality scales with story progress × difficulty × active
  multipliers**, and elite kills add roughly 5–10% on top (D22).
- **Bosses always drop an authored signature item** at a quality floor well above the level's norm.

**Consequence:** the §5.3 generator takes (rank, progress, difficulty, multipliers, elite bonus)
and rolls once per drop; boss tables carry fixed item identities with rolled stats.

---

## D24 — Progression never caps: prestige cycles · **Locked**

Directed by Michael (2026-08-18) as **the main differentiator from Castle Crashers and Dungeon
Defenders** — both cap levels; BattleBomb never does.

- Levels 1–99, then **prestige**: back to level 1 carrying **one permanent stat point** and a
  **visible prestige badge**. Repeatable forever, and **each successive cycle costs more XP**.
- The per-cycle reward is deliberately small: *"only the most dedicated players should be
  noticeably stronger."* The badge, not the stat total, is the main flex — and power growth stays
  arithmetic, so enemies and endless mode remain balanceable forever.
- **Rejected:** one infinite ladder with doubling XP per level — after a handful of doublings a
  level takes orders of magnitude longer, and a bar that never visibly moves feels capped, which
  defeats the differentiator.

**Consequence:** XP, level, and prestige count are simulation state alongside gear. Pillar 2 is
amended in spirit, not letter: gear stays the *big* power lever; leveling is the *endless* one.

---

## D25 — Co-op failure: partner revive · **Locked** *(resolves O8)*

Directed by Michael (2026-08-18). A downed player is revived by their partner — Interact is
contextual Light (D17), so a revive is walking over and pressing Light. Both players down ends the
attempt at the last checkpoint; solo, going down is the attempt ending.

**Consequence:** M3 ships death handling with revive from the start, never as a retrofit. How
difficulty scales with player count remains an M3 tuning question, not a design one.

---

## D26 — Block is cut: five verbs, defence is stats and movement · **Locked** *(amends D17, D19, D22)*

Directed by Michael with his collaborator (2026-08-18). D17's own mobile constraint — one thumb
operates every on-screen button — is the binding budget, and six verbs plus the stick was already
the ceiling. Block is the verb that goes; Heavy was discussed for the same cut and **stays for
now**, explicitly on watch.

- **No block button, no guard state.** Defence is the **defence stat** (M4's gear system) and
  movement itself — depth, spacing, and jump were always the designed defensive verbs. This is the
  same reasoning that rejected a dodge button in D17, now applied consistently.
- The input vocabulary is **five verbs**: Light, Heavy, Magic, Equipment, Jump.
- **Supersedes:** D17's Block row and "Block's design intent" paragraph; D19's Block paragraph
  entirely (the perfect-timed block, its counter window, and the guard-break framing of Heavy);
  D22's caster rationale — with no Block there is no turtling to punish, so the caster archetype
  survives as slow, hard-hitting elemental artillery whose statuses arrive with M5, not as the
  anti-turtle.
- **Consequence:** M3 task 30 was built and reverted the same day (fc9dbb6 → a2d5d11) — the
  combat machine has no Guarding phase and the input asset carries no Block action. If Block ever
  returns, it is a new decision argued against the same control budget.

---

## D27 — Equipment: two passive slots, no deployables, no keys · **Locked** *(amends D19)*

Directed by Michael with his collaborator (2026-08-18).

- **No deployable equipment** — no turrets, no placed constructions. The Dungeon Defenders tower
  line is out entirely, which also deletes the one-best-tower meta D19 guarded against.
- **No progression-gating equipment** — nothing like Castle Crashers' shovel or horn. Story
  progress never depends on carrying an item.
- **Two slots, effects passive.** Equipment modifies the player while worn; it is never an
  activated ability.
- **Bows are weapons**, a deliberate weapon class — not equipment. First recorded ranged weapon
  class; M4's weapon design inherits this.
- **Potions (health/mana) are instant-use from the inventory**, not equipment and not a loadout
  slot.

**Supersedes in D19:** the "utility-first catalog: healing, deployment, control, mobility" line
(deployment is out, healing lives in potions) and the "equipment is a moment, not a rotation"
framing — passive equipment has no moment. D19's structural guard survives in spirit: equipment
stays spice; the build focus stays armor and weapon.

**Consequence — opens O10:** with every equipment effect passive, the **Equipment button** (D17)
has nothing left to activate.

---

## D28 — Enemies fight like a crowd, not a queue · **Locked** *(amends D22)*

Directed by Michael after the first live fight (2026-08-19): the task-33 brains beelined at one
target and archers stood still — *"very Dungeon Defenders."* The Castle Crashers reference means
the space **between** attacks moves. Four behaviours, all data on `EnemyDefinition`:

- **Turn-taking:** at most N melee attackers per player at once (driver-enforced token, paper
  N=1). Melee without the token circles its target at a **hover ring**, strafing depth. The
  **brute never waits** (`TakesTurns` off) — relentlessness is his identity, and the contrast
  sells the others.
- **Hit-and-peel:** melee cooldown backs out to the hover ring instead of standing on the player.
- **Living archers:** ranged and caster drift in depth inside their band and scatter diagonally
  when dived — never statues. The drift deliberately never *seeks* the target's depth: projectiles
  cross depth for them, which is §2.2's ranged identity, test-pinned.
- **Per-enemy seeds:** each spawn gets a deterministic seed; strafe flips and hop pulses run on
  per-enemy beats so no two enemies move in lockstep. Hops go through the shared motor
  (`JumpRequested` → a Jump press in the synthesized command); jump speed is authored per enemy.

**Deferred deliberately:** reacting to the *player's* attacks (dodge-rolls out of your swing)
needs the perception struct to carry player combat state — a future, explicit extension, not a
retrofit.

---

## D29 — Revive is a skill: mash to pump, pace decides the health · **Locked** *(amends D25)*
**Michael, 2026-08-19, after play-approving the task-35 channel:** "reviving should have a little
skill involved and the amount of hp restored reflects the skill. Castle Crashers did it
perfectly."

- **Mash, not hold.** Each Light press beside the downed partner is one *pump*; the revive
  completes after an authored pump count (paper 10). Faster mashing completes sooner — the fill
  is per-press, exactly the Castle Crashers CPR feel.
- **Pace decides the health.** The restored fraction scales with how quickly the pumps landed:
  completing at the authored *fast* pace (paper 75 steps) restores the max fraction (paper 65%),
  dawdling to the *slow* pace (paper 240 steps) restores the min (paper 25%), linear between.
  The old flat 50% is gone.
- **Stopping drains the bar.** Going quiet mid-revive decays pumps (paper: one lost per 30
  silent steps); draining to zero drops the channel. No one gets locked into a channel by a
  single tap, and the bar visibly bleeding out is the urgency cue.
- **Everything else D25/task-35 built stands:** contextual Light, broken by stagger or leaving
  range, restart from zero, revive grace, attacks stripped while channelling. All numbers are
  authored on `CharacterDefinition` — tuning, not code.

---

## D30 — Loot is inspected and taken, never hoovered · **Locked** *(amends D23)*
**Michael, 2026-08-19, after the M3 mega pass:** "I prefer the loot to show stats when you walk
over it — not everything is an upgrade, not everything is worth your time. Click Light to grab
it. There could be a setting to autoequip new items when picked up, but by default just place
new stuff in your inventory."

- **Walking over a drop shows what it is.** Stats when M4's items exist; the placeholder shows
  its quality roll. Judging worth is the player's job — the game never assumes a drop matters.
- **Light takes it, deliberately.** Contextual like the revive; no touch-hoover. First press
  wins a contested drop, so D23's couch race survives — it just requires intent.
- **Inventory by default, auto-equip as a setting** (lands with M4/M6). Two playstyles honored:
  rush-grab-and-sort-later, and inspect-on-the-spot.

---

## D31 — The heartbeat revive: precision buys health, mashing buys time · **Locked** *(amends D29)*
**Michael, same session:** "the mashing isn't quite the same as Castle Crashers' heartbeat
quicktime revive, where you can spend time getting a precise revive with a lot of hp or a quick
button mash with very little hp."

- **A heartbeat pulses over the downed partner** (paper: every 45 steps). A press **on the
  beat** is worth a big progress chunk (paper 2.0) at full accuracy; a **rushed** press (within
  15 steps of the last) is worth the floor (paper 0.5) at zero accuracy; between the two,
  accuracy is how close to the beat's centre the press landed.
- **Health restored = average accuracy** across the channel (25% → 65%). A pure mash completes
  fastest and pays the floor; riding the beat takes seconds longer and pays the ceiling. D29's
  pace-based pricing is gone.
- **Silence still drains progress** (paper: one chunk per 60 quiet steps — longer than a beat,
  so the rhythm itself is never punished) and an empty bar drops the channel. The break rules,
  the contextual-Light gate, and the revive grace stand.

---

## D32 — The stat language: four base stats, direct gear stats · **Locked** *(amends D19, refines D24)*

Directed by Michael in the M4 design session (2026-08-19). The split honors both halves of the
hook: leveling feels Castle Crashers, loot reads Dungeon Defenders.

- **Base stats**, leveled with allocated points: **Strength** (+% weapon damage), **HP** (pure
  health pool — resistance is gear's job), **Mana** (capacity only), **Speed**. Paper: +1% damage,
  +5 HP, +5 mana, +0.5% move speed per point — all authored data.
- **Speed has a hard velocity cap.** Points past the cap never raise velocity — they shrink
  **speed penalties**, armor weight and combat slows alike (paper: 2% per over-cap point, resist
  capped at 75% so slows always matter a little).
- **One point per level, player-allocated** — the Castle Crashers allocate screen. **Prestige
  resets allocations along with the level** and banks +1 permanent point (D24 refined: a cycle's
  ~98 points are re-earned every climb — that reset is half of what makes the fresh start real;
  D36 is the other half).
- **Gear rolls direct concrete stats**, never attributes — D35's catalog.
- **Amends D19:** mana *capacity* is a base stat; *regen* stays a gear stat.

---

## D33 — One quality ladder: nine ranks carry the whole arc · **Locked**

Directed by Michael (2026-08-19). The hook's tier and quality axes merge into one player-facing
ladder:

**Nothing < Battlescarred < Torn < Rusty < Shiny < Pristine < Legendary < Mythical < Godly.**
*(Reordered — see the amendment at the end of this decision. The line above is the original.)*

- Each rank does **three jobs at once**: a stat budget multiplier, an affix count (a rolled range
  capping at **3–4 on Godly** — even a Godly can come up short), and upgrade capacity (D35).
  Paper table lives in `HANDOFF-M4.md`.
- **Progress and difficulty push drops up the ladder** — D23's continuous quality score maps onto
  a rank through authored thresholds, so M3's tested `DropRoll` feeds the ladder unchanged.
  Finding your first Legendary *is* progression.
- **Materials are name flavor, never a hidden axis** — "Shiny Leather Chestplate," "Rusty Steel
  Helmet." Weapons start plain ("Pristine Hunting Bow," "Legendary Hunting Knife"); Destiny-style
  signature names are planned content, boss drops especially.
- **Amended (Michael, 2026-08-22, adopting the UI Pass 01 ramp).** The ladder is reordered and
  **Godly is deferred to a later release**. The nine that ship:

  **Nothing < Battlescarred < Rusty < Torn < Clean < Shiny < Pristine < Legendary < Mythical.**

  Three changes: **Rusty and Torn swap**, **Clean is new** at rank 4, and **Godly becomes rank 9,
  released later** — "an extremely rare 10th tier… not all items will get the Godly tier," and
  which items may roll it is still open. Godly keeps a name and a colour in the enum but sits one
  rung past `QualityTable.RankCount`, so no drop rolls it and no combine promotes into it;
  releasing it means growing that count and authoring one row, never renumbering saved items.
  **Nothing may name a rank as "the maximum" — derive it from `RankCount`.**

  Consequences, all accepted at the time:
  - **The ladder's affix ceiling now caps on Mythical**, which inherited rank 8 from Godly. Its
    row is unchanged, so 3–4 affixes still caps the launch game — the bullet above reads "Godly"
    only because Godly used to sit there.
  - **Sell value is `2^rank`, so the middle of the economy moved**: Rusty dropped a rung and
    halved, Shiny gained one and doubled. The *ceiling* did not move. The paper tables in
    `HANDOFF-M4.md` and `HANDOFF-M6.md` are stale for the middle ranks.
  - **Saves migrate** — `SaveCodec.CurrentVersion` 1 → 2 remaps every stored rank. Without it a
    saved Shiny reads back as a Clean.
  - Colours live in `Gameplay/Loot/QualityColors.cs` as hex, diffable against the design doc.
    Godly is `#73e6ff`, the cyan Pristine used to wear.

---

## D34 — Slots: a lean five, plus the worn loadout · **Locked**

Directed by Michael (2026-08-19), trimming the 2019 list (Helmet, Chest, Pants, Boots, Gloves,
Weapon, Pet).

- **Gear slots: Helmet, Chest, Boots, Weapon, Pet.** Every drop is chunky, and a region armor set
  is three art pieces (D22's elites visibly wear what they drop).
- **Weapon classes: Sword and Bow** through the vertical slice. A bow turns Light into a shot
  through M3's projectile simulation — weaker per hit, crossing depth freely (§2.2's ranged
  identity). Class is a data field; more classes are content, never code.
- **Pets come in three classes**, all data: **attackers** (fight their own way, minimal stats),
  **stat pets** (pure character boost), and **uniques** (one bespoke designed-in effect — e.g.
  +0.1% to prestige stats). M4 ships stat pets end to end; attacker behaviour (an ally brain) and
  unique effects are deferred to their own design moment.
- Alongside gear: two worn equipment pieces (D27) and one quick-use slot (D37).

---

## D35 — Gear stats and the min/max layer: Diablo rolls, DD investment · **Locked**

Directed by Michael (2026-08-19): *"I like the Diablo model, however upgrade capacity increases
with quality as well."*

- **Core stats** every item of a slot always has: weapons roll Damage + Swing Speed (bows: Draw
  Speed and Shot Speed); armor rolls **Defence** (D26's stat, % damage reduction) + **Weight**
  (the speed tax — tankier rolls weigh more).
- **Affixes roll at the drop and are immutable** — present-or-absent per item, count from D33's
  range. Live in M4: Crit Chance, Crit Damage, Life Steal, +Max HP, +Max Mana, Mana Regen,
  Reduced Weight, Knockback Power. Generated but neutral until M5: Magic Damage, Magic Range,
  Elemental Resistance, Weapon Infusion element.
- **Upgrade capacity scales with quality** — the Dungeon Defenders half. M4 stamps it on the
  instance; the flow that spends it (player-chosen stat raises, a currency sink) lands with M6.
- **Defence spectrum:** additive across pieces. At top-quality rolls a max-tank build caps at
  ~70% reduction and a max-lightweight build lands around ~30%, both ends scaling down with
  lesser quality. Defence and weight travel together on armor rolls, so the spectrum *is* the
  build choice. Fine-tuning explicitly expected.
- **Equipment drops from the same generator** — one loot pipeline. The catalog (each piece's
  passive, optional active) is authored content; which piece drops and how it rolled is the
  generator's job.

---

## D36 — O9 resolved: level requirements on gear · **Locked**

Directed by Michael (2026-08-19), confirming the proposal on file.

- Every item is stamped a **required level** from its drop context — gear from level-40 content
  asks for level 40. Equip validation enforces it.
- **Prestige to level 1 re-locks the stash**, and it comes back online continuously as levels are
  re-earned. The fresh start is real, the loot grind is never invalidated, and there is no chunky
  progress-chest recovery — the concern that killed the chest proposal.

---

## D37 — O10 resolved: the Equipment button is the quick-use slot · **Locked** *(amends D27)*

Directed by Michael (2026-08-19).

- The button survives, repurposed: it fires **one quick-use slot**, which holds **a consumable or
  a worn equipment piece with an active**. Health potions will be the most common tenant —
  mid-fight healing never opens a menu.
- **Amends D27:** *some* equipment carries an active effect; worn effects stay passive by
  default. D19's structural guards return for actives — damage **derived** from weapon damage,
  **real cooldowns** — so an active can never outgrow the build.
- The five-verb surface stands: Light, Heavy, Magic, Quick-use, Jump. The input action keeps its
  binding (I / RB); only its meaning changed.

---

## D38 — Elements are authored data; the roster itself is open · **Locked** *(amends §4's baseline roster)*

Directed by Michael in the M5 design session (2026-08-19). The element roster is genuinely
contested — Michael wants Fire/Earth/Electricity/Poison; his collaborator wants the Avatar
quartet Fire/Water/Earth/Air — so the roster becomes **O11** and the framework stops caring.

- **An element is an authored definition, never an enum entry**: id, display name, status
  behaviour, colour/VFX identity — ScriptableObject in, struct out, like characters and enemies
  (pillar 3). Core knows element ids exist, never what they are named. Adding an element is
  creating one asset.
- **Fire ships first** — it is in both candidate sets. Its status is **Burn**: damage over time,
  priced from the hit that applied it (paper: ~50% of the applying hit's damage again over 3
  seconds; reapplication refreshes, the stronger source wins). Deriving status strength from the
  applying hit means gear scaling flows into statuses with no second tuning axis.
- The 2019-inherited baseline roster in §4 (Fire/Water/Electric/Earth) is superseded; Core's
  `Element` enum is replaced by ids plus authored definitions.

---

## D39 — The Magic kit: three casts on one button · **Locked** *(amends D19)*

Directed by Michael (2026-08-19). Magic stays one **button** — the amendment is that stick
flavours and aerials, always legal combo grammar (D17), now apply to it:

| Input | Cast | Space it owns | Paper mana |
|---|---|---|---|
| Press | **Splash** — a narrow ground line erupts ahead, in facing direction | The line in front; depth-limited like melee | 20 |
| Stick down + press | **Aura** — radial AoE around the character | The circle around you — the depth answer, the crowd moment | 45 |
| Press airborne | **Elemental double jump** — a burst boosts you up, chip damage at the liftoff point | Vertical mobility; once per airborne | 15 |

- All three cast the character's element and apply its status at full strength. Base pool 100,
  regen ~1/s: magic is a rhythm within the fight, never a rotation. An empty pool means the
  press does nothing; the fizzle cue is presentation's job.
- **The splash is deliberately depth-limited** — free depth crossing is the bow's identity
  (§2.2) and magic must not eat it. Magic's depth answer is the aura's radius.
- **Magic damage is character base × gear.** Strength never touches it (D32). The caster build
  lives in loot: Magic Damage (+% cast damage), Magic Range (splash length + aura radius), Mana
  Regen. Casts are combat-machine phases like swings — no casting mid-swing, no swinging
  mid-cast.
- **Stick up + Magic is deliberately unassigned** — expansion room, not a gap.
- **Amends D19:** "Magic is one press", and — for Magic only — "the stick aims; it never picks
  the combo" (the stick-down flavour explicitly picks the cast; melee is untouched). **D18
  stands**: the double jump is exactly the explicit layered mechanic it reserved space for; the
  base jump arc never varies. Charged casts stay rejected; Heavy remains the only hold.

---

## D40 — Statuses are two-way; same element cancels; two mitigation stats · **Locked**

Directed by Michael (2026-08-19): "full two-way."

- **Enemies status players.** The caster archetype's artillery applies its element's status —
  the priority target D22 promised. Every combatant carries the same status machinery.
- **The same-element rule, symmetric and automatic:** matching attacker and defender elements
  halve **elemental** damage both ways, and the status never applies (a Fire being cannot
  burn). Kinetic weapon damage is untouched — an infused blade against its own element behaves
  like a plain sword, never worse: no affix may ever roll as a penalty. Chosen over full
  cancellation so a matchup is punished, never nulled.
- **Mitigation splits into two jobs:** **Defence** (armor's core stat) reduces *hits*, as it
  has since M4. **Elemental Resistance** (the per-element affix) reduces that element's damage
  *and* shortens its statuses on you — the counter-purchase to a region's element, the Dungeon
  Defenders loop working.

---

## D41 — Reactions ship framework-first; climate is symmetric · **Locked** *(amends D19's chain-stun)*

Directed by Michael (2026-08-19): "just do Fire for now."

- **The reaction system is built and pinned in M5** — target carries status A, is hit by
  element B, the authored pair table names the effect (burst damage, stun, consume rules) —
  tested with synthetic elements. **The authored table ships empty** until O11 settles the
  roster.
- D19's *soak + shock → chain-stun* survives as the anchor **pattern** — a setup status plus a
  trigger status producing a chain-stun — but which elements own it joins O11, since Water and
  Electric are exactly the contested names.
- **Climate multiplies all elemental damage regardless of who deals it** — a cold region's
  Fire ×1.25 helps your Fire and the enemy caster's alike. The symmetry is what makes it
  strategy rather than a buff (§4's "depth via context"): region intel drives gear and roster
  choice both ways. M5 sets climate at scene level; chapters author it properly in M7.

---

## D42 — The chest: inventory access is a place, not a button · **Locked** *(extends D30)*

Directed by Michael in the M6 design session (2026-08-20). Castle Crashers plans builds between
levels; Dungeon Defenders organizes in the tavern. BattleBomb's mixture: **checkpoint rooms** as
the calm spaces, and inside them **the chest** (working name — an ender-chest-like object) as the
single point of inventory access.

- **Walking up to a chest opens your inventory; away from one, the sack is sealed.** Every
  checkpoint room contains a chest, and extra chests may be placed mid-level where a stretch
  needs one. Field pickups queue in the sack until the next chest — which makes D30's auto-equip
  setting the only field-side equip path, deliberately.
- **The screen is two tabs.** **Item Sack**: a thumbnail grid (8 wide full-screen, 4 wide in the
  couch split), background colour = quality, category filter (All / Weapons / Armor / Pets /
  Equipment / Consumables), with the selected item's stats, vs-worn deltas, and every action —
  equip, per-stat upgrade, combine, sell, lock — on the right. **Hero**: stat allocation on the
  left, the worn loadout on the right. Settings live in the global settings menu, never on the
  chest.
- **Per mode:** solo, the screen takes the full display and pauses the world. Couch co-op, it
  takes the opener's half while the other half stays live gameplay — the camera frames only the
  players still playing; a player at a chest stands idle beside it. Online (whenever D10's
  deferral ends) returns to full screen on each player's own display.
- **Shopkeepers are a separate, rarer thing** (D43) — the chest is an inventory button made
  physical, never a shop.

**Consequence:** checkpoint rooms enter the vocabulary now — M6 builds one in the test scene
with a chest, a shopkeeper, and an M2 training dummy; M7's chapters place them through real
levels.

---

## D43 — The economy: selling is the only faucet · **Locked** *(resolves D35's currency sink)*

Directed by Michael (2026-08-20): "Once you have gear that is godly, getting a couple coins here
and there from killing an enemy means nothing."

- **One currency, per player**, unnamed until the story names it. **No coin drops from enemies —
  rejected** because any flat drop is rounding error in an uncapped game. Money enters play only
  by selling items, and **sell prices scale with quality rank × required level**, so the faucet
  inherits the loot ladder's endless scaling automatically.
- **Shopkeepers** (DD-style; the story spreads them around in M7, mostly in checkpoint rooms —
  M6 ships one in the test scene) buy anything at sell price, and sell health and mana potions
  plus a small rack of 3–4 generator-rolled gear pieces at current progress quality, rerolled
  per visit.
- **The sack caps at 200 slots**, worn gear excluded; identical consumables stack to 5, one slot
  per stack. **Auto-sell at cap** is a per-player setting: an overflowing pickup instantly sells
  the lowest-quality *unlocked* bagged item. **Locks** protect items absolutely — never
  auto-sold, and manual sell or combine demands the unlock first. Cap hit with nothing sellable:
  the pickup refuses — a red X on the walk-over card and a 200/200 flash, punishing on purpose
  so it is learned once.

**Amended 2026-08-23** — the shopkeeper's counter, from the inventory design session:

- **The counter has two sides, Buy and Sell**, switched by a button rather than a tab. Buy shows
  the rack as one priced row per roll — the piece, its rank, what it would change about the
  player against what they wear, and whether they can afford it. Sell is the ordinary sack. They
  are modes and not one screen because four rack rows and a five-row grid do not both fit the
  half, and the tab row is already carrying sack-versus-hero in local co-op.
- **"Clear the junk"**: one press sells every unlocked, non-consumable piece below a rank
  threshold the player sets on the panel. This is the manual counterpart to auto-sell — auto-sell
  is reactive, one item at a time, at the cap; this is the player deliberately emptying the
  bottom of the bag. Locks are absolute here as everywhere, worn gear is not in the sack to
  begin with, and **consumables are spared**, on the same reasoning that makes auto-sell spend
  gear before it spends a potion stack.
- **The threshold stops at Clean.** The sweep has no confirmation step, so its reach is capped
  where "junk" stops being a fair description; one press must never be able to sell a Legendary.
- **The shopkeeper's screen has no hero half** (Michael, 2026-08-23). A chest is about the player,
  so it frames them and puts the doll and the loadout in the free half. A shopkeeper is about the
  shopkeeper: solo and online the camera zooms onto *them*, the free half is left to the world,
  and the sack-versus-hero tab row is not built. In local co-op nothing zooms — the display still
  belongs to both players — and the panel is simply that player's half.

---

## D44 — Investment: deepen or gamble · **Locked** *(completes D35's second half)*

Directed by Michael (2026-08-20). Both flows live on the Item Sack panel, at any chest.

- **Upgrading is guaranteed progress:** a capacity point raises **any stat the item already
  has** — core stat or rolled affix, never adding one (the roll stays immutable) — by ~8% of its
  rolled value, and **the money cost doubles per point already spent on that item**. Capacity
  gates the total, money gates the pace; maxing an item costs on the order of twice its own sell
  price.
- **Combining is the gamble:** two of the **same item at the same quality rank** are both
  consumed for one fresh reroll of it — **2% chance (tunable) it returns one rank higher**.
  Spent points die with the inputs; the result rolls fresh capacity; its required level is the
  higher of the two inputs; locked items refuse.
- **The three fates of a drop** — worn (and deepened), sold (guaranteed progress), or combined
  (the gamble) — are the loot loop. Selling junk funds deepening the keeper; duplicates tempt
  you off the guaranteed path.

**Amended 2026-08-23** (Michael):

- **Worn gear is deepened where it is worn.** The loadout in the hero panel is a place the cursor
  can go — solo by pushing right off the sack grid, in local co-op through the hero tab — and a
  worn slot offers Upgrade, Take off and Lock. Taking a piece off, deepening it and putting it
  back on was never the intent; it was simply the only route the screen offered.
- **"Combine all"**, on its own button, grinds a whole pile in one press: it keeps pairing
  duplicates of the anchor's exact definition and rank until fewer than two remain. Because an
  unpromoted reroll returns at the same rank it rejoins the pile, so a stack of four is three
  combines ending in one piece, not two ending in two. **A promotion leaves the pile and is never
  re-gambled** — the rank-up is the payoff, and cascading it would make the outcome unreadable.
- **The bulk verb is not Heavy**, which was the obvious choice and is the one button it cannot be:
  Heavy is how a combine in progress is abandoned, and a modal screen with no way out was M6's one
  real defect. It sits on Magic, named in the footer while the pick is open.

---

## D45 — Play-mode smoke tests arrive · **Locked** *(amends D7)*

Directed by Michael (2026-08-20), on evidence: M4 and M5's only bugs were Gameplay wiring bugs
invisible to a green EditMode suite — M5's leap was dead in-game while 401 tests passed.

- D7's deferral ("until scenes stabilise") has expired on its own terms: scenes are stable, and
  M6 is the most wiring-heavy milestone yet.
- **M6 ships a small PlayMode smoke suite**: boot the gameplay scene, drive synthetic commands
  through the whole loop — kill, drop, grab, chest, equip, sell — and assert the state. A
  tripwire for "completely broken", never a feel-check; Michael's mega passes keep that job.
  EditMode remains the primary gate and the home of all logic coverage.

---

## D46 — The roster lands; each element owns its signature cast · **Locked** *(resolves O11, amends D39)*

Directed by Michael with his collaborator (2026-08-20). The roster is **Fire, Ice, Earth, Air**.
The compromise that settled it also reshapes the Magic kit: the press casts were "a similar line
of splash damage" differing only in status, and now each element's press cast is its own move.

| Element | Signature cast (the press) | Status (infusions and casts alike, D40) |
|---|---|---|
| **Fire** | The current line — mid-range ground splash ahead | **Burn** — damage over time (unchanged) |
| **Ice** | A long-range projectile, low damage | **Chill** (working name) — slowed for a few seconds on contact |
| **Earth** | A short-range rock wall that erupts and quickly falls — decent damage, **stuns** on hit | No mark; the infusion grants the wielder **crit chance** (provisional, see amendment) |
| **Air** | A mid-range gust that **launches** enemies skyward to start a combo | No mark; the infusion grants the wielder **knockback** (provisional, see amendment) |

- **The signature cast belongs to the element, not the character** — every Fire character shares
  the line (the Castle Crashers model, and it keeps "a new element is one authored asset", D38:
  the asset simply grows its signature cast). Characters still differ in animation, VFX, and
  their aura/leap tuning.
- **The aura and the elemental double jump keep their D39 shapes** — radial crowd answer,
  mana-priced mobility — and carry the element's status, so an Ice aura is a radial chill.
- **Statuses grow kinds**: damage over time (Burn) and now **slow** (Chill). Earth's stun and
  Air's launch are properties of the *hit*, never lingering marks — the stun rides D41's
  existing stun machinery, the launch rides the melee launcher's. Earth and Air infusions
  therefore mark nothing *for now*: they stay honest elemental damage (same-element rule,
  resistances, climate all apply), and giving them a mark later is authoring, not code.
- **Depth rules hold (§2.2, D39's reasoning):** Fire, Earth, and Air press casts are
  depth-limited like melee. **Ice's bolt flies straight ahead in the caster's lane** — long
  range is its trade; free depth crossing remains the bow's identity, uneaten.
- **D41's chain-stun anchor loses its owners** — soak + shock were Water and Electric, both now
  gone from the roster, and Earth stuns directly. The reaction table still ships empty; picking
  this roster's pairs is its own short session with Michael and his collaborator.
- **Amended (Michael, 2026-08-20, during the M5B pass):** until their statuses are designed,
  Earth and Air express their infusions as **wielder passives** — an Earth-infused weapon adds
  crit chance (the Castle Crashers skull), an Air-infused weapon adds knockback, both scaled by
  the infusion's rolled magnitude like every affix. Explicitly provisional: "we will think more
  thoroughly of these effects later." Fire and Ice infusions mark the target; Earth and Air
  empower the wielder — the same asymmetry Castle Crashers' own weapons carry.

---

## D47 — Characters are 2D billboards in a 3D world · **Locked**

Directed by Michael with his collaborator (2026-08-20). Characters, enemies, weapons, and pets
render as **2D billboarded sprites — Castle Crashers-style art — inside the 3D URP environment.**

- **The simulation does not know.** Rule 2 (Gameplay owns state, Presentation observes) means
  hitboxes, reach, depth, and timing are Core numbers, so the entire visual layer swaps without
  touching a line of gameplay code. This decision is cheap *because* of that rule.
- Depth stays a real simulation axis (§2.1), and the **hard-edged grounded shadow stays the
  primary depth cue** (§2.3) — under flat sprites it becomes more load-bearing, not less.
- Facing flips are sprite flips; elite quality tinting (D42-era, M6 task 69) tints the sprite.
- **Deferred to the art pass:** the sprite pipeline (frame sheets vs skeletal 2D), resolution
  targets, and how depth-band scaling reads. Current placeholder visuals stay as they are until
  real art exists — converting placeholder capsules into placeholder sprites buys nothing.

---

## D48 — Chapters are stages; stages stream through checkpoint airlocks · **Locked** *(implements D4's interface)*

Directed by Michael in the M7 design session (2026-08-20). The structural fork he called before
anything else: **the goal is something you move toward, never something you defend.** Chapters
are constant forward progression; no defence-objective encounter type exists or is planned.

- **A chapter is an ordered list of stages.** A stage is 5–10 minutes of play, the unit of
  resume and selection; the whole game must not be completable in a day (Castle Crashers is
  the explicit counter-example). The ender-chest rooms of D42 are the stage checkpoints —
  always between stages, mid-stage where a stretch needs one.
- **A stage has two natures, authored apart.** *The space* is a small additive Unity scene,
  hand-built in the scene view — Michael wants to design story levels by hand — holding
  geometry and dumb marker components only. *The game* — arenas, spawn waves, climate, level
  stamp, loot progress, checkpoint and shopkeeper placement — is a data asset consumed as a
  `StageSpec`. An architecture test fails any stage scene carrying a non-whitelisted component.
  This is rule 2 made mechanical for levels, and it is what keeps D4 intact: endless mode's
  generator produces the same `StageSpec` and points its geometry field at prefab chunks
  instead of a scene. One field differs; neither mode is "the real one."
- **The Gameplay scene is the machine** and never reboots; stage scenes load additively into
  it. **The checkpoint room is the airlock** — Destiny 2's load zones on our floor plan: the
  next stage streams in behind the door while the player sells and re-equips, and the finished
  stage unloads once they cross out. No loading screen is ever shown.
- **Rejected:** a scene per stage carrying its own encounters (a generator cannot emit scenes,
  so story would become structurally special); stages as pure data with prefab geometry (right
  for endless, wrong for the hand-crafted story Michael asked for). The split takes the half of
  each that fits.
- **The LittleBigPlanet-style map** Michael wants for stage selection is a view over a Core
  selection model; M7 renders that model as a list, the map arrives with art.

**Consequence:** D23's loot progress, D36's level stamp, and D41's climate — the three constants
M6 left at scene level — become per-stage data. `SimulationDriver._lootProgress` and the
hardcoded level stamp are deleted, not defaulted.

---

## D49 — A wipe keeps the loot and respawns at the chest · **Locked** *(refines D25)*

Directed by Michael (2026-08-20): *"If you wipe you should be able to keep the loot you
collected, you will respawn at your most recent checkpoint or shopkeeper and have a chance to
try to upgrade and equip stuff to go again."*

- Loot, XP, and money earned since the checkpoint are kept; enemies since it reset. The cost
  of a wipe is the refight. Genre precedent (Castle Crashers, Dungeon Defenders) is unanimous,
  and in a game where selling is the only faucet (D43), confiscating grabbed drops would be
  theft against the loop's core pleasure.
- The respawn point is the last checkpoint or shopkeeper room — where the chest is — so a wipe
  funnels the player into the economy. The loop teaches itself.
- **Quitting mid-stage resumes from the same point by the same rule.** One boundary to learn.
- **Rejected:** losing gains since the checkpoint (roguelike tension, but against D23's shared
  grab moment); keeping loot but restarting the stage (a second rule for no new information).

---

## D50 — Difficulty tiers are data rows, unlocked per chapter, hidden behind names · **Locked**

Directed by Michael (2026-08-20). Dungeon Defenders' shape, with the numbers kept from the
player.

- **A tier is a row:** enemy stat multiplier, enemy level bump, loot-progress multiplier. Three
  rows at launch; a fourth is a row, and endless mode may one day generate rows. Applied by one
  pure function, `tier × stage → EncounterInputs`, which feeds the spawner and D23's quality
  roll.
- **Per-chapter unlock, deliberately light:** chapter N opens when N−1 is beaten on any tier;
  tier T+1 of a chapter opens when it is beaten on tier T. A player may rush the newest chapter
  on tier 1 or grind an early chapter to tier 3. Both overreach paths are open on purpose.
- **The player sees names only.** No multiplier is displayed anywhere in the shipped UI —
  *"they should figure out pretty quick if they bit off more than they can chew if they wipe
  quickly."* A dev-only overlay shows the rows and effective numbers, compiled out of release.
- **Rejected:** a single difficulty with NG+ after the credits (back-loads all replay value,
  fighting the not-done-in-a-day goal); unbounded tiers (endless mode already owns scaling
  forever, and every tier past four is unbalanced by construction).

---

## D51 — One save per machine, Castle Crashers style; the couch shares the sack · **Locked** *(amends D43, D42)*

Directed by Michael (2026-08-20), reversing a profiles-per-person model he had first leaned
toward: *"No profiles, make it like CC … profile should not matter locally because all progress
is shared. For online play the profile is tied to their account anyways."*

- **No local profiles.** One save on the machine, named from `IPlayerIdentity` when a platform
  supplies one and `local` when it does not. Title → character select; Player 2 joins at
  character select and picks a character. Online play is each participant bringing their own
  account's save; reconciling two saves' progress is the networking milestone's problem.
- **The sack and wallet belong to the save, not the player** — D43's "one currency, per
  player" and "per-player setting" are amended to per save. Both couch players' chest screens
  (D42's split halves) are views over the same 200 slots. Shared across the roster too: farm
  with Fire, gear up Ice. Per character: XP, level, prestige, the worn loadout, quick-use.
- D23's "whoever grabs it keeps it" becomes a moment locally and keeps its meaning online.
- **Rejected:** a sack per character (reverses the shared-roster rule and makes trying an
  element cost the inventory); a sack per player slot (the same human on the other controller
  loses their gear).
- **Progress gating stands:** nothing launches above the save's unlocks. Couch play is at the
  machine's save, so the earlier "furthest-progressed account" exception is now simply the rule.

**Consequence:** `PlayerInventory` splits — sack, wallet, and settings move to a session-owned
stash; the worn loadout and quick-use stay per player. Settings persist in the save — the
PlayerPrefs path M6 planned was never built, and now never will be.

---

## D52 — Saves: a pure model in Core, a store behind the Platform seam · **Locked** *(extends D4, D5)*

Settled in the M7 design session (2026-08-20), the engineering shape of D51.

- **Core owns the model** (plain serializable data), the mapping to and from live simulation
  state, the versioned text codec, and the migration table. `JsonUtility` is permitted in Core:
  it is not on §2's forbidden list and needs no scene, so round-trip tests are plain EditMode.
- **Core never touches a disk.** `ISaveStore` (named text blobs: read, write, list, delete)
  joins the Platform layer beside `IPlayerIdentity`; `FileSaveStore` is the default in
  `NullPlatformServices`; Steam Cloud is a later second implementation, zero Core changes.
- **Story progress is its own namespace** inside the save (D4); endless mode adds its own
  later; the roster, sack, and wallet sit outside both because the loot chase lives in every
  mode (D12).
- **Versioned and conservative:** every save carries a schema version; older migrates forward
  on load; **newer refuses to load** rather than corrupt. Items save by identity id plus rolled
  values, never by name.
- **Autosave** on checkpoint-room entry, stage completion, chest close, and clean quit — so a
  crash costs exactly what a wipe costs (D49), and the player never learns a second rule.

---

## D53 — A downed partner is left where they fell, and the run stops banking · **Locked** *(amends D49, refines D25/D31)*

Directed by Michael after playing M7's machine (2026-08-21): *"if your teammate dies and you
continue on, they should not be dragged but you should be punished, you cannot collect any more
checkpoints and if you wipe you get sent to the last checkpoint where both teammates were alive."*

**The problem it replaces.** The arena clamp applied to every body, so a downed partner was yanked
forward one arena at a time — instantly, about an arena's width, with no walk. The *rule* was
defensible (a body outside the play area is unreachable and forces a full wipe) but it read as a
glitch, and it quietly made abandoning your partner free.

- **A downed player is exempt from the horizontal clamp.** The body stays exactly where it fell.
  Reviving is still D31's mash — walk to them and press Light — so there is a real window while the
  arena is still open, and it closes when the survivor crosses into the next arena.
- **While anyone is down, checkpoints stop banking.** Walking into a checkpoint room still *works*
  — the phase advances, the chest opens, the airlock streams the next stage — but the room does
  **not** become the wipe point. Entering and banking were one action; they are two now, because
  refusing the whole thing would make a stage impossible to leave after a partner died in it.
- **A wipe therefore returns to the last room where everyone was alive**, which needs no separate
  bookkeeping: if a room only banks when the couch is whole, the banked room is by definition the
  last whole one.
- **Reaching a checkpoint room does not revive anyone** (Michael's explicit choice over the gentler
  reading). Press on alone and your partner is out of the run until you wipe — which is what makes
  pressing on a decision rather than a shortcut.

**Why the harsh reading:** the gentler one — stand them up at the room but do not bank it — keeps
the couch whole and costs the survivor only a rollback. Michael chose the version where abandoning
a partner actually abandons them. The pressure is to go back and revive, not to run ahead.

- **A body on the floor is not moved — by the arena, or by anyone.** Exempting the downed from the
  clamp is only half of it: the surviving partner's own swing still shoved the corpse (D21's
  knockback, 9 m/s on a Heavy), which walks it forward one hit at a time and rebuilds the very
  problem this decision closes. `ApplyImpulse` refuses a downed body outright. The swing still
  lands and still announces itself — it simply moves nothing. The one exception is the sub-unit
  crowding nudge, deliberately kept so a reviver cannot stand inside the corpse.

**Consequence:** solo play is unaffected (one player down is a wipe, D25). `StageRun` learns whether
the couch is whole when a room is reached; the clamp learns to skip the downed. `_roomBehind` can
go back to counting only living players, since a body no longer needs protecting from the clamp.

**Known edge, accepted:** the rule does not reach across a stage boundary. Press on through an
airlock with a partner down and the old stage's rooms unload with it, so a wipe restarts the new
stage and the partner returns at its spawn. Blocking the airlock would strand the survivor in a
stage they cannot finish, which is worse.

---

## D54 — Early Access on Steam, with online co-op at launch · **Locked** *(amends D10; reorders GAME_DESIGN §10)*

Settled with Michael (2026-09-24), planning the road from M7's machine to a release. The full plan
is `docs/ROADMAP.md`.

- **The first release is Steam Early Access**, on PC. It launches with four heroes (one per
  element, D46), two authored chapters with their bosses on all three tiers (D50), and **Endless
  mode**. Consoles and mobile stay later (D5).
- **Online co-op ships in Early Access** — Michael's explicit choice over Steam's Remote Play
  Together (local co-op streamed, zero netcode) and over couch-only. This amends D10's "no netcode
  in the initial build": the network-shaped architecture D10 insisted on is now cashed in rather
  than protected. Topology, how couch and online players mix (D11 still caps it at two), and join points belong
  to M8's design session.
- **Online is built before content.** Retrofitting networking into a finished game rebuilds every
  boss, effect, and menu; built first, everything after it is online-aware by default. It is also
  pure engineering, so it runs while the story is unstuck, and it is the largest remaining unknown.
- **Endless is in Early Access because it needs no story** (D56) and gives the loot chase
  unlimited runway (D12) while chapters keep arriving.
- **No launch date.** The order is by dependency; a milestone finishes when it is right (Michael:
  quality first).
- **Rejected:** a full 1.0 (a long stretch with no players and no income); PC and console at
  launch (dev kits, certification, usually a publisher); a demo-first order (it would advertise
  an online game it cannot show, and carries the retrofit cost).

**Consequence:** the build order after M7 is M8 Online co-op, M9 Look & sound, M10 Vertical slice,
M11 Endless, M12 Chapter 2, M13 Early Access readiness. The vertical slice keeps its meaning — the
real target — and moves from M8 to M10. `GAME_DESIGN.md` §7, §8, §10 and §11 are updated.

---

## D55 — Art and audio: AI drafts first, provenance always · **Locked** *(extends D16)*

Directed by Michael (2026-09-24): *"I want AI-assisted drafts for all the art we need to work on
first. I want the files organized in a way that lets me know it was done by ai or me, this way I
can draw my own versions of the ai drafts."*

- **Every art asset is drafted with AI first** and redrawn by Michael or his collaborator over
  time. Music follows the same pipeline; sound effects come from licensed libraries.
- **Provenance is recorded three ways:** the folder (`AI/` or `Hand/`), the filename (`__ai_v1`),
  and one manifest listing every asset's AI draft, its hand version, and its status — AI draft,
  being redrawn, hand final. The manifest is also the redraw backlog. Exact paths are M9's to set.
- **The game references stable slots, never files.** A slot ("Fire hero — torso") points at
  whichever version is current, so a hand-drawn replacement is one re-pointed entry; nothing is
  rewired, and the draft stays alongside for reference.
- **A test enforces it:** shipping art missing from the manifest fails the suite, alongside the two
  import traps that each cost a recovery session in August — a `.cs` file inside `Art/`, and
  duplicate GUIDs from importing with `.meta` files.
- **Steam requires AI-generated content that ships to be disclosed.** The manifest is what makes
  the disclosure accurate. The store art and the four heroes are hand-drawn before the store page
  goes live, because players judge AI art hardest where it is most visible.
- **Division of labour:** Claude writes each asset's brief and prompt from the art bible and the
  manifest; Michael generates the drafts in the tool of his choice. Claude cannot generate images
  in this setup.
- **D16's discipline binds the drafts too:** layered, cleanly separated body parts at high
  resolution, fitted to the one shared rig (D15) — or a draft cannot be rigged.

**Amended 2026-09-24 (the Art lane's provenance spec, accepted by the orchestrator):** a slot is
the unit **swapped whole** — one icon, one effect, one track, and **one whole character skin**,
not one body part as the "Fire hero — torso" example above had it. A half-redrawn character, hand
head on AI arms, would look wrong on screen, so a character's parts change over together; the
parts live inside the skin and are addressed by the rig's categories. It is still one re-pointed
entry. Michael's image tool is **ChatGPT images**. Detail: `docs/art/PROVENANCE.md`.

---

## D56 — The story is Castle Crashers-thin · **Locked** *(shapes GAME_DESIGN §9)*

Michael (2026-09-24), with the story stalled: keep it Castle Crashers-thin.

- **The story is a goal, four heroes, regions, and bosses** — almost no dialogue, and no dialogue
  or cutscene system to build.
- **It is settled in one World session.** Michael and his collaborator make every call; Claude asks
  the questions and records the answers as a one-page story bible. The standing rule holds: Claude
  invents no lore.
- **The bible holds:** the goal (moved toward, never defended — D48); setting and tone; the four
  heroes (name, look, a one-line personality, why they wield their element); per chapter the
  region, its climate element, its enemy family (2–3 elemental skins of the four archetypes, D22),
  its boss (the concept, and how it is dodged by jump and by depth — §6) and the boss's signature
  drop (D23); the shopkeeper; one line on why Endless exists.
- **Gate:** ideally early in M8, because the art drafts wait on it; hard gate at M10's start. If it
  slips, M11 Endless may move ahead of M10 — it needs no story.

**Why thin:** it turns an open-ended writing project into a short list of decisions, and the
genre's reference point proves the loop does not need more. Writing it outside the repo with no
structure is the pattern that stalled.

---

## D57 — The menu layer and the seats · **Locked** *(amends D17; retires PlayerInput)* — built by Groundwork

Directed by Michael in two sittings — the map itself, then its open details at Groundwork's
kickoff (2026-09-24): *"A should be select/confirm, B should be back,
we can use x/y for options like selling, upgrading, locking … we should not make them
redundant. Controls shouldnt get too advanced that we can't implement touch taps and swipes as
mobile replacements."* And: *"esc to be the natural back button, only pause if you are not in a
menu. Arrow keys and enter/space also are helpful."*

| Role | Gamepad | Keyboard |
|---|---|---|
| Navigate | Left stick + D-pad | WASD + arrows |
| Confirm | A | Enter / Space |
| Back | B | Esc — backs out one level; closes the screen from its top level |
| Sell (Combine-all mid-pick) | X | J |
| Lock / release | Y | K |
| Switch tab or mode | LB / RB | Q / E |
| Pause | Start — and inside a menu, leave it outright | Esc, outside menus only |

- **Menus have their own action map and their own buttons.** A physical button carries a fight
  meaning and a menu meaning at once — A is Jump and Confirm — and a screen never reads a combat
  verb. Rebinding a verb can never move "confirm". D17's five-verb budget is untouched.
- **No two buttons do the same job.** So X does nothing at the shop rack: A already buys there.
- **X and Y are only ever shortcuts.** Every verb they fire is also reachable by choosing the item
  and then the verb, which is what keeps touch possible (D5): a phone taps, it needs no X.
- **X sells instantly; the lock is the only safety** (Michael's choice over hold-to-sell and a
  second press above Clean).
- **Escape is Back inside a menu and Pause outside one.** Start still leaves any screen outright,
  so M6's promise — getting out is never a puzzle — survives Escape becoming one level at a time.
  The press that closes a screen never also opens the settings.
- **Solo at a chest, the shoulders cross between the sack and the hero panel** — the "other half"
  in every layout.
- **Prompts show the device the player last pressed**: Xbox letters on every controller
  (Michael's choice), key caps on a keyboard, badges per UI Pass 01's hint row.
- **Seats replace PlayerInput.** Player 2 owns the device they joined with; Player 1 owns
  everything else — so a solo player switches between keyboard and any controller just by using
  it. At character select Player 1 is held to the device that started the game, so a press on
  another is a join. The seat is authored and is the player's id, which fixes the solo "P2" label.
- **A button held across a scene change is not a press.** A now leaves the results screen and
  confirms the title; without this a held A would skip through the front door.

**Rejected:** double-binding confirm and back (A and X both confirming) — Michael: *"we should not
make them redundant."* Hold-to-sell — his call, instant with locks.

---

## D58 — Online runs on the host's machine, over our own layer · **Locked** *(amends D10; cashes in D54)*

Settled in the M8 design session with Michael (2026-09-24).

- **Host-authoritative.** One player's PC — the host — runs the one real simulation, exactly as it
  runs today. The guest's device becomes `PlayerCommand`s sent to the host, where a
  `RemoteCommandSource` feeds them in like any local pad (D10's promise, cashed). The host sends the
  guest world snapshots and simulation events; the guest's machine draws them and never rolls,
  resolves or decides anything. The host owns every RNG seed.
- **Our own thin netcode layer**, not a library: a message codec, snapshots, a replica mode for the
  guest, an input buffer, and own-character prediction (D62). The simulation already owns its clock,
  state, order and ids; a library would make us translate it into someone else's.
- **The transport sits behind a Platform seam.** Steam (via Steamworks.NET, a UPM package pinned to
  a tag, in its own assembly) is the PC Early Access transport: friends-only lobbies, overlay invites,
  P2P over Valve's relay, Steam Cloud as D52's second `ISaveStore`. An in-memory loopback and a plain
  UDP transport exist for tests and two-editor development. **Nothing above the seam may assume
  Steam** — Michael: *"Whatever is the best free option for mobile."* A free cross-platform
  transport (Epic Online Services is the candidate) slots in when mobile arrives (D5). Rule 6
  stands: a build without Steamworks compiles and runs.

**Why host-authoritative:** it needs no bit-identical maths across two PCs (the audit counted 588
`float`s across 62 Core files and `Mathf.Pow` in the XP curve and prices), it is untouched by the ten
live sources of divergence the audit found (stage loads landing at machine-dependent moments above
all), and it keeps the host's game exactly as it plays today. Cheating is irrelevant to two friends
co-operating.

**Rejected:** lockstep (input delay on both players, plus determinism forever); rollback (all of
lockstep's cost plus whole-world save/restore); Netcode for GameObjects (per-object sync on its own
tick, no full prediction/reconciliation); FishNet (the strongest library — kept as the fallback if
our layer stalls); Photon Fusion/Quantum (paid per concurrent player past 100, and a rewrite into
their model); Facepunch.Steamworks (last release April 2024).

---

## D59 — Two players, any shape; friends drop in at character select and checkpoint rooms · **Locked** *(extends D11, D51)*

Settled with Michael (2026-09-24).

- **Two in total** (D11 unchanged): solo, a couch pair on one PC, or one player on each of two PCs.
  A couch pair cannot also take an online guest.
- **A guest joins at character select, or at a checkpoint room mid-run.** A join request that
  arrives mid-fight waits until the host's run is at a checkpoint room; the guest appears at that
  room's respawn point. Mid-fight drop-in is rejected. (Michael chose the checkpoint rooms over the
  recommended character-select-only.)
- **A solo game is open to the host's platform friends by default** — they see "Join game", and the
  host can invite from the overlay at any time. A setting turns it off. A couch game is full and never
  open.
- **The host's save decides what can be launched**; a guest may help in a chapter above their own
  unlocks.

**Consequence:** a solo run can become a two-player online run mid-chapter, so every solo rule — the
chest pause, the camera, the chest layout — switches live when a guest arrives or leaves. M8 must
send a whole running world to a newcomer and load their save into a live run.

---

## D60 — Online, nothing pauses · **Locked** *(extends D42)*

Settled with Michael (2026-09-24).

- **No screen stops the world online** — chest, shopkeeper, hero panel, settings. Each opens
  full-screen on its own player's display (D42's online rule, extended); the player at a screen stands
  idle, as a couch player at a chest does now.
- **Each display lays out for its own local players**, not for how many characters exist — online
  that is always one, so full-screen chest and solo camera.
- **The host drives the whole-session moments:** leaving results, returning to chapter select,
  launching. The guest's screens follow.
- Solo and couch keep today's rules. A solo game switches to this rule the moment a guest drops in.

**Rejected:** settings pausing both players ("Paused by Player 2") — either player could stop the
other's game at will.

---

## D61 — Online saves: the host holds the run, each player keeps what they earned · **Locked** *(amends D51; extends D52)*

Settled with Michael (2026-09-24). D51 left "reconciling two saves' progress" to the networking
milestone; this is that reconciliation.

- **The host's machine holds the guest's gear during a match.** At join the guest's character and
  stash travel to the host; the host's simulation owns them for the run. Online there are therefore
  **two stashes in the simulation**, one per save; the couch keeps one shared stash (D51 unchanged
  for the couch). Auto-sell and auto-equip travel with their stash — they are saved state. The guest's
  menu actions are requests that name the player and carry a sack revision; the host refuses one
  aimed at a sack that has since changed.
- **The shopkeeper's rack lives in the simulation**, rolled by the host; buying names a rack slot.
  (Today the menu rolls and prices it — a rule 2 violation the audit found.)
- **The guest keeps everything they earned** — loot, gold, XP, levels, allocations, worn gear — and
  **credit for chapters and tiers finished with the host**, written on the guest's own machine at
  D52's autosave moments from the state the host sends. The resume point stays the host's.
- **Leaving:** the host quits or drops → the guest keeps everything up to the last autosave moment,
  exactly what a crash costs (D52), and returns to the title. The guest leaves → the host carries on
  solo, with D25's solo rules from that step.

**Rejected:** the guest holding their own gear with the host keeping a mirror (instant menus, but two
machines deciding at every grab whether the sack has room); loot-and-XP-only (a helper made to replay
a chapter alone to unlock it).

---

## D62 — The guest's own character is predicted · **Locked**

Settled with Michael (2026-09-24).

- **The guest's movement and swings react instantly on their own machine:** running, jumping,
  facing, and the start of every attack and cast are predicted locally with Core's own
  `CharacterMotor` and `CombatMachine`, then corrected to the host's answer when it arrives.
- **Everything the host decides arrives a round trip later** — whether a swing hit, damage numbers,
  knockback from enemies, and everything about enemies.
- **Built in two steps:** interpolation only first, to measure real connections; prediction on top.

**Rejected:** movement-only prediction (attacks feel late); no prediction (the guest's own character
visibly behind their presses).

---

## Open

- **O7 / O8 — resolved 2026-08-18** as D23 (shared free-grab drops) and D25 (partner revive).
- **O9 / O10 — resolved 2026-08-19** as D36 (level requirements on gear) and D37 (the quick-use
  slot).
- **O11 — resolved 2026-08-20** as D46: Fire, Ice, Earth, Air, each with its own signature
  cast. The reaction pairs for this roster remain open (D46's last bullet).
- **O2 — Git LFS: deliberately deferred (Michael, 2026-08-18).** Working policy: a few hi-res
  hero assets plus lightweight placeholders live in plain git. Rules that keep this safe — no file
  near 100 MB (GitHub hard-rejects; warns at 50 MB), hero binaries are commit-rarely (iterate
  outside the repo, commit keepers — every committed version lives in history forever), and one
  hi-res source per asset (Unity's import settings handle per-platform compression). *Correction
  to the earlier note:* enabling LFS later for **new** files is a one-line `.gitattributes` change
  with no history rewrite; only retroactive migration rewrites. Tripwire to flip it on: any asset
  wanting >50 MB, or binary churn growing the pack (currently 1.51 GiB) by ~500 MB.
