# CLAUDE.md

Guidance for Claude Code when working in this repository.

> **STATUS — read first.** This document describes the **legacy 2019 project**, which lives on the
> `main` branch. You are most likely on `Redo`, where BattleBomb is being rebuilt from scratch on
> Unity 6 + URP. Everything below is historical reference for understanding the original vision —
> **not** a description of code you should extend or a foundation to build on.
>
> Start with **`docs/DECISIONS.md`** for what has actually been decided. Do not modify, migrate, or
> refactor legacy code. This file is rewritten once the new project is scaffolded.

## Project

**BattleBomb!** — a 2D/2.5D hack-n-slash built in **Unity 2021.1.23f1** (built-in render pipeline).
Company `Nitrous Interactive`, bundle id `com.NitrousInteractive.BattleBomb`, version `0.1.6`, Standalone target.

Originally written Summer 2019 and revisited intermittently. It is an in-progress hobby project, not a shipped
product: expect commented-out experiments, duplicate systems, and placeholder logic. Prefer minimal, surgical
changes over refactors unless asked.

## Build & Run

There is no CLI build script, no test suite, and no CI. Everything happens through the Unity Editor.

- Open the project folder in Unity Hub with editor **2021.1.23f1** (see `ProjectSettings/ProjectVersion.txt`).
  A mismatched editor version will silently upgrade serialized assets.
- Entry scene is `Assets/Scenes/Menu/Menu.unity`.
- Build scene order (`ProjectSettings/EditorBuildSettings.asset`):
  `Menu` → `LevelSelect` → `selection` (character select) → `TrainingGrounds` → `Moon Dungeon/Chunk 1` → `Chunk 2`.
- `com.unity.test-framework` is installed but there are no tests and no test assembly. Don't claim tests pass.
- Verification means opening the relevant scene in the editor and entering Play mode. You cannot verify gameplay
  changes from the command line — say so rather than asserting a change works.

## Repository Hygiene (read before any git operation)

**There is no `.gitignore`.** Only a `.collabignore` (Unity Collaborate legacy) exists, and git does not read it.
Consequences:

- `Logs/` and `UserSettings/` are **committed** — Unity-generated noise is tracked.
- `Library/`, `Temp/`, and `obj/` do not currently exist on disk. The moment the project is opened in Unity they
  will appear as thousands of untracked files. Do not `git add -A` / `git add .` in this repo.
- If asked to add a `.gitignore`, use the official Unity template and `git rm --cached` the already-tracked
  `Logs/` and `UserSettings/` — but confirm first, since that rewrites what's tracked.

## Code Layout

First-party gameplay code (no namespaces, no assembly definitions — all in `Assembly-CSharp`):

| Path | Contents |
|---|---|
| `Assets/Scripts/*.cs` | Flat pile of ~38 gameplay/UI scripts (health, mana, combo, camera, scene loading, menus) |
| `Assets/EnemyFramework/` | `Boss`, `BossHealth`, `BossWeapon`, `Boss_Enrage`, `Boss_Run` |
| `Assets/Better Inventory/` | Current ScriptableObject-driven inventory (`InventoryObject`, `ItemObject`, `PlayerStats`) |
| `Assets/InventorySystem/` | Older slot/bag inventory — superseded, still compiled |
| `Assets/NPCs/NPC_System.cs` | NPC interaction |
| `Assets/HighscoreTable/`, `Assets/ScoreTable/` | Two separate score-table implementations |

Third-party packages vendored under `Assets/` — **do not edit or reformat these**:
`Assets/AI` (A* Pathfinding Project, has its own asmdefs), `Assets/Plugins/Steamworks.NET` +
`Assets/Scripts/Steamworks.NET` + `Assets/Editor/Steamworks.NET`, `Assets/Standard Assets`, `Assets/Samples`
(Cinemachine demos), `Assets/DailyRewards` (`NiobiumStudios`), `Assets/Loading Screen Studio` (`Michsky.LSS`),
`Assets/ScoreTable/Stuff/CodeMonkey`, `Assets/Scripts/Controls/{Joystick Pack,RTS_Camera}`,
`Assets/Scripts/Pause_system`, `Assets/Art/**`.

Only `Assets/AI` uses assembly definitions; everything else lands in the default `Assembly-CSharp`, so any
first-party script can reference any other without setup.

## Critical Gotchas

**The player controller lives inside a purchased art asset.**
`Player_Main` and its base `PlayerControllerMain` are in `Assets/Art/Low_Swordman/Demo/Scripts/` — a vendor demo
folder. First-party code depends on them directly (`Assets/Scripts/Health.cs:19`,
`Assets/Scripts/WeaponsMenu.cs:11`, `Assets/Scripts/DashMove.cs:5`). Reimporting or deleting the Low_Swordman
asset breaks compilation project-wide. Player movement, attack, and combo dispatch all live in
`Assets/Art/Low_Swordman/Demo/Scripts/Player_Main.cs`, not under `Assets/Scripts`.

**Input is in "Both" mode and gameplay uses the legacy API.**
`activeInputHandler: 2` in `ProjectSettings/ProjectSettings.asset`. All gameplay reads legacy
`Input.GetAxis` / `GetKey` / `GetButton`. `com.unity.inputsystem` is installed and
`Assets/NewInputTest/PlayerInputActions.inputactions` exists, but it is an unfinished spike — nothing consumes it.
Match the legacy API when editing gameplay unless explicitly migrating.

Custom axes are defined in `ProjectSettings/InputManager.asset`, not in code. Used by gameplay:
`Console`, `Inventory`, `Interact`, `SpellUse`, `DashL`, `DashR`, plus stock `Fire1`/`Fire2`/`Horizontal`.
Adding a new binding means editing `InputManager.asset` (via Edit → Project Settings → Input Manager).
There is also a literal axis named `PLACEHOLDER`.

**Four unrelated damage implementations, two different method names.**
- `Assets/Scripts/Health.cs` — player; method is `TakenDamage(int)`
- `Assets/Scripts/Enemy.cs` — enemies; method is `TakenDamage(int)`
- `Assets/Scripts/PlayerHealth.cs` — older player health; method is `TakeDamage(int)` (no "n")
- `Assets/EnemyFramework/Scripts/BossHealth.cs` — bosses

`Player_Main.Attack()` calls `enemy.GetComponent<Enemy>().TakenDamage(...)` and will `NullReferenceException` on
any hit collider in the `enemyLayers` mask that lacks an `Enemy` component. The `DamageAnimation()` sprite-flash
coroutine is copy-pasted verbatim across `Health`, `Enemy`, and `PlayerHealth`.

`Health.TakenDamage` gates death on `currentHealth == 0`, not `<= 0` — hence its comment "player can only take
damage in even numbers". Odd damage values overshoot zero and the player never dies. Preserve or fix
deliberately; don't change it as a drive-by.

**`LoadCharacter.cs` is both the spawner and the camera follow.**
It reads `PlayerPrefs.GetInt("selectedCharacter")` (written by `CharacterSelection.StartGame`), instantiates the
prefab, then lerps its own `transform` toward the clone every frame. It must be attached to the camera, and it
throws if `selectedCharacter` indexes past `characterPrefabs`. Character prefabs live in
`Assets/Scripts/Characters/Player {Red,Green,Blue}.prefab`.

**Scene loading is inconsistent.** Most code uses `SceneManager.LoadScene`, but `Assets/Scripts/LoadLevel.cs`
still uses the removed-in-later-versions `Application.LoadLevel`. Scenes are loaded by string name in some places
and `buildIndex` in others.

**Steam is wired to the test AppID.** `steam_appid.txt` contains `480` (Spacewar). `SteamManager`
(`Assets/Scripts/Steamworks.NET/SteamManager.cs`) initializes at runtime; `DisplaySteamID.cs` and
`DisplayNameOnLoad.cs` read from it. Without Steam running these paths log errors — that is expected locally.

**Superseded-but-live code.** `CharacterSelectionOld.cs`, `InventorySystem/OldInventory.cs`, and the
`HighscoreTable` vs `ScoreTable` pair all still compile. Check which one a scene actually references before
editing; grep alone will mislead you.

**Cinemachine version drift.** `Packages/manifest.json` pins `2.7.4` but `Assets/Samples/Cinemachine/2.6.3/`
holds demo scenes from the older version. The samples are not used by the game.

## Conventions

- Unity 2019-era C#: `MonoBehaviour` subclasses, `public` fields for inspector wiring (no `[SerializeField]
  private`), coroutines rather than async, no namespaces, no interfaces outside `Better Inventory`.
- Indentation is **mixed** — tabs in `Health.cs`/`PlayerHealth.cs`/`LoadCharacter.cs`/`CharacterSelection.cs`,
  4 spaces in `Enemy.cs`/`DashMove.cs`/`ComboSystem.cs`. Match the file you are editing; do not normalize a whole
  file as a side effect of a small change.
- Tags and layers are fixed lists in `ProjectSettings/TagManager.asset`. Gameplay layers: `Obstacle` (8),
  `Enemy` (9), `Player` (10). Custom tags include `Ground`, `Enemy`, `Music`, `Pet`, `PetTarget`, `Model`.
  Adding a tag or layer means editing that asset.
- Every asset needs its `.meta` sibling committed. When adding a `.cs` file outside the editor, the `.meta` is
  generated on next Unity open — mention this rather than hand-authoring a GUID.
- Scenes and prefabs are YAML with GUID references. Never hand-edit them to rename a class; rename in the editor
  or the references break silently.
