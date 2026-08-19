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

## Open

- **O7 / O8 — resolved 2026-08-18** as D23 (shared free-grab drops) and D25 (partner revive).
- **O10 — The Equipment verb after D27 (2026-08-18).** Passive equipment leaves the Equipment
  button without a function: potions bypass it (inventory instant-use) and nothing else activates.
  Candidate resolution: cut the verb — four buttons (Light, Heavy, Magic, Jump), which hands back
  exactly the mobile control-budget headroom that put Heavy on watch in D26. Michael to rule.
- **O9 — Prestige must feel like a fresh start without invalidating the loot grind
  (2026-08-18).** D24 resets the level, but endgame gear carries, so a prestiged player stomps the
  early game and the reset is theatre. Michael and his collaborator floated story-milestone
  "progress chests" that return old gear — and flagged their own concern: chunky recovery means
  the fresh start only lasts until the first chest. Proposal under discussion: **level
  requirements on gear**, so prestige-to-level-1 re-locks the stash and it comes back online
  continuously as the level is re-earned. Resolves with M4's item design.
- **O2 — Git LFS: deliberately deferred (Michael, 2026-08-18).** Working policy: a few hi-res
  hero assets plus lightweight placeholders live in plain git. Rules that keep this safe — no file
  near 100 MB (GitHub hard-rejects; warns at 50 MB), hero binaries are commit-rarely (iterate
  outside the repo, commit keepers — every committed version lives in history forever), and one
  hi-res source per asset (Unity's import settings handle per-platform compression). *Correction
  to the earlier note:* enabling LFS later for **new** files is a one-line `.gitattributes` change
  with no history rewrite; only retroactive migration rewrites. Tripwire to flip it on: any asset
  wanting >50 MB, or binary churn growing the pack (currently 1.51 GiB) by ~500 MB.
