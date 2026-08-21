# BattleBomb

Unity 6.5 LTS (`6000.5.8f1`) + URP, 3D project. A side-on co-op action-RPG brawler with elemental
combat and a loot chase. Built for PC/console, kept viable for mobile.

This file is the working brief. The three documents in `docs/` are the authority behind it:

| Document | Holds |
|---|---|
| `docs/DECISIONS.md` | Locked decisions (D1…D16) and their reasoning. **Wins any disagreement.** |
| `docs/GAME_DESIGN.md` | Pillars, combat, elements, roster, milestones M0–M9. |
| `docs/ARCHITECTURE.md` | Assemblies, layering rules, Core's constraints, testing, conventions. |

Read the relevant document before proposing anything structural. Do not re-litigate a locked decision
without saying which one and why.

---

## Branches

- **`Redo`** — current work. The Unity 6 rebuild. This is where everything happens.
- **`main`** — the 2019 project, frozen at `12039a6`. **Reference only.** Never copy a file forward
  without rewriting it to the architecture rules; the patterns it demonstrates are largely the ones
  those rules exist to prevent.

---

## Layout

```
Assets/_BattleBomb/
├── Core/            BattleBomb.Core             ← depends on nothing of ours
├── Gameplay/        BattleBomb.Gameplay         → Core, Platform
├── Presentation/    BattleBomb.Presentation     → Core, Gameplay  (read-only)
├── UI/              BattleBomb.UI               → Core, Gameplay  (read-only)
├── Platform/        BattleBomb.Platform         → Core
├── Data/            authored assets, no code
├── Editor/          BattleBomb.Editor           → everything
└── Tests/EditMode/  BattleBomb.Tests.EditMode
```

Dependency direction is one-way and enforced by `ArchitectureFitnessTests`. If Core needs to
reference Gameplay, the design is inverted.

---

## The rules that matter most

1. **Core is pure.** No `MonoBehaviour`, `ScriptableObject`, `GameObject`, `SceneManager`,
   `Resources`, `Time.*`, coroutines, or Unity `Random`. Every Core type must be constructible from a
   plain unit test with no scene loaded. `UnityEngine` value types (`Vector2`, `Mathf`) are fine.
2. **Gameplay owns state; Presentation and UI observe it.** Nothing gameplay-relevant may live only
   in an Animator, a particle system, or a UI field. A hitbox that exists because an animation state
   entered is a bug.
3. **Input is commands, never polling.** Devices become `PlayerCommand` structs, and only
   `InputSystemCommandSource` touches a device. No `Input.GetKey`/`GetAxis`/`GetButton` anywhere.
4. **Players come from `PlayerRegistry`.** `FindGameObjectWithTag("Player")` is forbidden — it cannot
   express two local players, let alone a remote one.
5. **Data is authored as assets, consumed as plain structs.** ScriptableObject is an authoring
   format; Core sees the `.ToRuntime()` struct. A new character is data, never a new C# class.
6. **Steamworks sits behind `IPlatformServices`.** A build with it entirely absent must compile and
   run — `NullPlatformServices` is the default.
7. **`[SerializeField] private`, never public fields.** No singletons holding mutable game state.

Full detail and the reasoning for each: `docs/ARCHITECTURE.md` §§1–9.

---

## Working with the Unity editor

The Unity MCP server is connected, so a session can drive the editor directly rather than asking for
manual steps. Useful calls:

- `recompile` / `recompile_status` — after editing scripts.
- `run_tests` with `mode: EditMode` — the primary verification gate.
- `run_tests` with `mode: PlayMode` and `async_tests: true` — the wiring tripwire (D45). It
  **must** be async: entering play mode domain-reloads and drops a synchronous request.
- `console` — read editor and player logs (`level: error` to filter).
- `capture_game_view` / `capture_scene_view` — see the result of a change. Note that a
  ScreenSpaceOverlay canvas is invisible to these; UI must render in camera space to be checked.

**Verify with `run_tests` before claiming anything works.** EditMode is green or the change is not
done. **PlayMode is the second gate since D45** — it boots the real scene and walks the loop, and
it exists because M4 and M5 each shipped a bug a green EditMode suite could not see.

**Live verification protocol (Michael's rule):** for slow-paced checks — positions, registrations,
settled states — drive the editor yourself with synthetic input and `eval` sampling. For anything
with fast motion (jumps, dashes, combat actions, hit feel), do **not** slow-motion-sample your way
to it: Michael watches the screen in real time, so hand him a short checklist of what to verify and
let him confirm what he saw. Spawning subagents is fine when a task genuinely benefits from one.

---

## Progress

| # | Milestone | State |
|---|---|---|
| **M0** | Foundation — Unity 6 + URP, assemblies, Input System, tests green | **complete** |
| **M1** | Movement and camera — two players on the depth plane, shared camera | **complete** |
| **M2** | Combat core — the D19 kit against training dummies | **complete** |
| **M3** | Enemies — plus the aerial pair, deferred into it | **complete** |
| **M4** | Gear and stats | **complete** |
| **M5** | Abilities and elements | **complete** |
| **M5B** | Element rework — D46 signature casts, D47 billboards | **complete** |
| **M6** | Loot loop — D42–D45, the chest, the economy, elites | **complete** |
| M7 | Chapters — story-free half designed (D48–D52, `docs/HANDOFF-M7.md`); authored chapters wait on story | **next** |
| M8 | **Vertical slice** — the real target | |

Build order and completion criteria: `docs/GAME_DESIGN.md` §10.

**Story and world are being written outside this repo.** M7's machine is built story-free against
a graybox fixture; only authored chapters need the story. Do not draft placeholder lore to fill
the gap; it will only be thrown away.

---

## Before considering a change done

- [ ] Could this logic live in Core? Does it?
- [ ] Does Core still avoid everything in rule 1?
- [ ] Does any new code read input directly, or find players by tag?
- [ ] Does presentation own state the simulation should own?
- [ ] Is anything hardcoded to one player, or to story mode?
- [ ] Is new Core logic covered by an EditMode test?
- [ ] Does new Gameplay *wiring* have a PlayMode check, or a reason it does not need one?
- [ ] Would this still work with a second player, and with Steamworks absent?
- [ ] Is `run_tests` green — both modes?
