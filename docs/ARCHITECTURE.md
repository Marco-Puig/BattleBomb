# BattleBomb — Codebase Architecture

How the code is organised and why. Derived from `DECISIONS.md`; if the two ever disagree,
`DECISIONS.md` wins and this file is stale.

**This document is enforcement, not suggestion.** Several rules here exist because violating them is
cheap today and unpayable later — specifically D10 (network-shaped) and D7 (testable Core). Where a
rule is machine-checkable, there is a test for it (§8).

---

## 1. Assemblies

New code compiles into separate assemblies via `.asmdef` files rather than Unity's default
`Assembly-CSharp`. Benefits: editing UI recompiles UI only; layering is enforced by the compiler
instead of by discipline; and Core stays genuinely testable.

```
Assets/_BattleBomb/
├── Core/            BattleBomb.Core             ← no project dependencies at all
├── Gameplay/        BattleBomb.Gameplay         → Core, Platform
├── Presentation/    BattleBomb.Presentation     → Core, Gameplay  (read-only)
├── UI/              BattleBomb.UI               → Core, Gameplay  (read-only)
├── Platform/        BattleBomb.Platform         → Core
├── Data/            (assets only, no code)
├── Editor/          BattleBomb.Editor           → everything
└── Tests/
    ├── EditMode/    BattleBomb.Tests.EditMode   → Core (+ others as needed)
    └── PlayMode/    BattleBomb.Tests.PlayMode   (deferred, D7)
```

**Dependency direction is one-way and never negotiated:**

```
        ┌─────────────┐
        │    Core     │   pure logic — depends on nothing of ours
        └──────┬──────┘
         ┌─────┴─────┐
         ▼           ▼
   ┌──────────┐  ┌──────────┐
   │ Platform │  │          │
   └────┬─────┘  │          │
        └───────▶│ Gameplay │   simulation — owns all game state
                 └────┬─────┘
              ┌───────┴───────┐
              ▼               ▼
       ┌──────────────┐  ┌────────┐
       │ Presentation │  │   UI   │   observe state, never own it
       └──────────────┘  └────────┘
```

If you find yourself wanting Core to reference Gameplay, the design is inverted — the logic you are
reaching for belongs in Core, or the thing calling it belongs in Gameplay.

---

## 2. What "Core" actually means

Core **may** reference `UnityEngine` for value types — `Vector2`, `Vector3`, `Mathf`. Reimplementing
vector math to achieve ideological purity is a waste of effort and a source of bugs.

Core **may not** contain or call:

| Forbidden in Core | Why |
|---|---|
| `MonoBehaviour`, `ScriptableObject`, `GameObject`, `Component` | Requires a scene to exist to test anything. |
| `SceneManager`, `Resources`, `Addressables` | Ties logic to asset loading and scene state. |
| `Time.*`, coroutines, `Update`-driven state | Simulation advances on an explicit passed-in step (D10). |
| `Random.*` (Unity's) | Non-deterministic and untestable. Core uses a seeded RNG it owns. |
| `Debug.Log` in hot paths | — |

The test: **every Core type must be constructible and exercisable from a plain unit test with no
scene loaded.** If it isn't, it is in the wrong assembly.

Core holds: damage resolution, elemental interaction, stat aggregation, item generation and affix
rolling, progression, inventory rules, combat state machines, and mode rules.

---

## 3. Simulation and presentation are separate (D10)

**Gameplay owns state. Presentation and UI read it.**

- Animation, VFX, audio, and camera **observe** simulation state. They never own gameplay-relevant
  data. If a hitbox only becomes active because an Animator entered a state, that is a bug — the
  simulation must own it, and the animation must follow.
- Presentation and UI reference Gameplay, so the compiler cannot stop them mutating it. Prevent it by
  API design: expose read-only views and events outward, keep mutating methods internal to Gameplay.
- Nothing gameplay-relevant may exist *only* in an Animator, a particle system, or a UI field.

This is the rule most likely to be violated by accident, and the one that most reliably makes a
codebase impossible to network, test, or debug later.

---

## 4. Input is commands, never polling (D5, D10)

```
Input System action  →  PlayerCommand (per player, per fixed step)  →  Simulation
```

- `PlayerCommand` is a plain struct: movement vector, button states/flags, frame number.
- **No gameplay code reads input directly.** No `Input.GetKey`, no `Input.GetAxis`, no
  `Input.GetButton` anywhere — the legacy project's single worst coupling, and the reason it could
  never have been networked or ported to touch.
- Two local players are two command sources. A remote player later is a third kind of source, and the
  simulation cannot tell the difference. That equivalence is the entire point.
- Players are resolved through a **player registry**. `GameObject.FindGameObjectWithTag("Player")` is
  forbidden — it is unfixable under co-op, let alone networking.

Simulation advances on a **fixed step**. Presentation interpolates between steps for smoothness.

---

## 5. Data is authored as assets, consumed as plain structs

ScriptableObjects are an **authoring format**, not a runtime type.

```
CharacterDefinition (ScriptableObject, Gameplay)
        │  .ToRuntime()
        ▼
CharacterData (plain struct, Core)  ──▶  consumed by Core logic
```

**Why:** Core cannot reference `ScriptableObject` (§2), and tests must construct data without loading
assets. This split is what makes item generation and damage math testable at all.

It also delivers pillar 3 of the design: characters, items, abilities, and enemies are **data**. A new
character is an authored asset plus tuning values, never a new C# class. Any proposal requiring a
bespoke script per character or per item should be pushed back on.

---

## 6. Modes are first-class (D4)

```csharp
interface IGameMode
{
    void         BuildEncounters(/* ... */);   // story: authored chapter. endless: generator.
    RunResult    EvaluateOutcome(/* ... */);   // win/loss is mode-owned
    ISaveScope   SaveScope { get; }            // namespaced, so modes cannot corrupt each other
}
```

No core system may assume story mode. Difficulty tier and loot table are **inputs** to encounter and
reward generation, never baked into authored chapter data — a chapter is content, the tier applied to
it is state (D12).

---

## 7. Platform services are abstracted (D5)

`IPlatformServices` covers achievements, player identity, and leaderboards. Implementations:
`SteamPlatformServices` and `NullPlatformServices`.

**A build with Steamworks entirely absent must compile and run.** The 2019 project called Steam
inline from UI scripts, which would have blocked a console or mobile build outright.

---

## 8. Testing (D7)

- **EditMode tests cover Core.** Damage resolution, elemental interaction, stat aggregation, item
  generation, affix rolling, progression. These are pure functions of their inputs — the highest-value
  test surface in the project, and the only way a session can verify a change without a human entering
  Play mode.
- **Architecture fitness tests** make the rules above machine-checkable rather than aspirational.
  At minimum, an EditMode test that reflects over `BattleBomb.Core` and fails if any type derives from
  `MonoBehaviour` or `ScriptableObject`. Add more as rules are violated in practice.
- **Seeded RNG** — loot tests assert exact outcomes for a given seed. Loot bugs are subtle, compounding,
  and destroy trust in the chase.
- PlayMode tests deferred until scenes stabilise.

---

## 9. Conventions

- **Root namespace `BattleBomb`**, mirroring folders: `BattleBomb.Core.Combat`,
  `BattleBomb.Gameplay.Movement`, `BattleBomb.UI.Inventory`. Set as the project's root namespace so
  new files get it automatically.
- `[SerializeField] private` for inspector wiring — **not** `public` fields. The 2019 project exposed
  everything publicly, so any script could mutate any other's state.
- **No singletons holding mutable game state.** Stateless service locators are acceptable; a
  `GameManager.Instance` that owns player health is not.
- Interfaces at assembly boundaries so the far side can be substituted and tested.
- 4-space indent, `.editorconfig` is authoritative.
- Legacy code on `main` is **reference only**. Never copy a 2019 file forward without rewriting it to
  these rules — the patterns it demonstrates are largely the ones this architecture exists to prevent.

---

## 10. M0 — bringing the project into existence

✅ **Complete.** Runbook for replacing the legacy tree on `Redo` with a fresh Unity 6 project. Kept
for the record; every step below is done. Unity 6.5 (`6000.5.8f1`), assemblies per §1, 19 EditMode
tests green.

**Nothing here is destructive in the permanent sense.** The complete 2019 project stays on `main` at
`12039a6`. Steps 3–4 only remove it from the `Redo` working tree.

1. **Install Unity 6 LTS** via Unity Hub. ✅ *Unity 6.5 LTS installed.*

   *Unity Hub also lists two cloud projects, `BattleBomb!` (Jun 2021) and `BattleBomb!(1)`
   (May 2023). Neither has a local folder — a full search of `C:` and `H:` found only this repo, so
   no work exists outside git. They are Unity Cloud dashboard records; "in production" is a
   dashboard designation. **Reuse neither.** This repo's legacy `ProjectSettings.asset` is linked to
   the first (`cloudProjectId: 1191d042-2c36-4535-9e7b-558220dff25f`, org `unity_eorj80ajvmyubq`).*
2. **Create a scratch project** — Unity Hub → New → **Universal 3D** (URP, 3D — *not* the 2D
   template; see D15) → create it somewhere temporary such as `Documents/BattleBombTemp`.
3. **Clear the Redo tree** — delete `Assets/`, `Packages/`, `ProjectSettings/`, `Logs/`,
   `UserSettings/`, and `.collabignore` (a Unity Collaborate remnant; git is the VCS now).
   Keep `docs/`, `README.md`, `LICENSE`, `steam_appid.txt`, `.gitignore`, `.gitattributes`,
   `.editorconfig`, `CLAUDE.md`.
   **Requires explicit go-ahead** — recoverable from `main`, but not something to do unprompted.
4. **Move the scratch project in** — copy its `Assets/`, `Packages/`, `ProjectSettings/` into the repo
   root, then delete the scratch folder.
5. **Open the repo in Unity 6** and confirm it compiles and enters Play mode clean.
6. **Add packages** — Input System (D5/§4), 2D Animation (D16), Test Framework, Cinemachine.
   Set the project's **root namespace to `BattleBomb`** in Editor Settings.
   Verify **Force Text** serialization and **Visible Meta Files** carried over.
   **Do not add `com.unity.collab-proxy`** — the legacy project carried it; git replaces it.

   *Cloud linkage:* the fresh project starts unlinked, which is correct — nothing in the
   architecture needs Unity Cloud services, and Steam sits behind `IPlatformServices` (§7). If you
   later want Cloud Build or Analytics, **relink to the existing `BattleBomb!` project** rather than
   creating a third orphaned dashboard record.
7. **Scaffold** — assemblies and folders per §1, the architecture fitness test per §8, a
   `PlayerCommand` path per §4, and `IPlatformServices` with a null implementation per §7.
8. **Rewrite `CLAUDE.md`** — it currently documents the legacy project and is only kept alive by a
   status banner. At this point it should describe the new project instead.

**Done when:** the project opens clean in Unity 6, the assemblies compile in the direction §1
requires, and the EditMode test run is green.

---

## 11. Quick review checklist

Before considering a change done:

- [ ] Does new logic live in Core if it could?
- [ ] Does Core still avoid everything in §2's forbidden table?
- [ ] Does any new code read input directly, or find players by tag?
- [ ] Does presentation own any state the simulation should own?
- [ ] Is anything hardcoded to one player, or to story mode?
- [ ] Is new Core logic covered by an EditMode test?
- [ ] Would this still work with a second player, and with Steamworks absent?
