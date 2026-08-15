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

## Open

- **O7 — Co-op loot distribution.** Shared drops create friction between two players; instanced drops
  remove it but weaken the shared-discovery moment that makes co-op looting fun. Affects the item
  generation API directly (per-player rolls vs one roll), so it should be settled before loot is built.
- **O8 — Co-op failure state.** Whether a downed player can be revived, whether one death ends the
  attempt, and how difficulty scales for two players rather than one.
- **O2 — Git LFS.** Not enabled. `git lfs` is not installed locally and the existing pack is
  ~1.5 GiB. Revisit before large binary art starts landing on `Redo`; enabling later means a history
  rewrite, so decide before the art volume grows.
