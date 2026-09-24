# Groundwork — Input Switching, Button Icons, and the Menu Map — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Any device can be either player and a solo player can swap between keyboard and controller at will; every menu reads A-confirm / B-back / X-Y shortcuts / LB-RB tabs; every prompt shows the button of the device that player last pressed; and the two known Groundwork bugs (solo "P2" label, split co-op filter overlap) are fixed.

**Architecture:** Menus get their own **Menu action map** and six new `CommandButtons` flags, so a physical button carries a fight meaning and a menu meaning at once (A is Jump *and* Confirm) and no screen ever reads a combat verb again. `PlayerInput` is removed: each seat owns its own copy of the controls, restricted to the devices a pure Core rule (`SeatAssignment`) gives it — *Player 2 owns the device they joined with; Player 1 owns everything else*. Prompts come from a pure Core glyph table (kept in step with the input asset by an acceptance test) and render as the design's badge row.

**Tech Stack:** Unity 6.5 (6000.5.8f1), Input System 1.20.0 (`InputActionAsset.devices`, `InputTestFixture` from the already-compiled `Unity.InputSystem.TestFramework`), legacy UGUI `Text`/`Image`, NUnit EditMode + PlayMode suites, Unity MCP bridge for recompiles, tests, scene edits, and captures.

**Source of scope:** `docs/ROADMAP.md` §4 "Groundwork". Decisions Michael made at kickoff (2026-09-24), binding on this plan:

| Question | Answer |
|---|---|
| X / Y / LB-RB | As proposed: **X = Sell** (Combine-all while a combine pick is open), **Y = Lock/unlock**, **LB/RB = switch tab or mode** |
| Guarding X-sell | **Instant; locks protect.** No hold, no second press. |
| Icon sets | **Xbox only** — every controller shows A B X Y. Keyboard shows key caps. |

Three consequences worth knowing before building (surface them in the close-out, Task 14):

1. **X does nothing at the shop rack.** The roadmap listed "Buy at the rack" for X, but A already buys a rack row in one press there, and Michael's rule is that no two buttons do the same job. A wins.
2. **Solo at a chest, LB/RB jump the cursor between the sack and the hero panel.** Solo has no tabs to switch, and the hero panel went unfound for a whole pass (2026-08-23); the shoulders are the "other half" in every layout.
3. **A button held across a scene change is no longer a press.** With A now leaving the results screen *and* confirming the title, a held A would otherwise skip straight through the front door. A new seat's first sample treats whatever is already down as held.

---

## Before you start

- [ ] **The Unity MCP bridge must be connected.** It is the test gate. If `editor_status` does not answer, stop and ask Michael to open the project in Unity.
- [ ] `editor_status` — if the editor is in play mode, `editor_stop` (standing permission).
- [ ] Record the baseline: `run_tests` mode `EditMode` (expect **626 passed**), and `run_tests` mode `PlayMode` with `async_tests: true`, poll `test_status` (expect **15 passed**). Full EditMode output spills to a file: read the summary with `head -c 400 "<path>"` and failures with `grep -B3 -A8 '"Status": "Failed"' "<path>"`.
- [ ] After every PlayMode run, delete Unity's generated `Assets/InitTestScene*.unity` files and their `.meta`s.
- [ ] Bash heredocs break on apostrophes in this harness. Write C# with the Write tool, and data edits with python scripts written to the scratchpad.

**How to run a single EditMode fixture** (used throughout): `run_tests` with `mode: EditMode`, `filter_type: testName`, `filter: <FixtureName>`. Valid `filter_type` values are `testName`, `assembly`, `category` — anything else wedges the bridge.

**Commit convention:** one commit per task, on `main`, subject `G<n>: <what>`, body explaining why, ending with:

```
Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

Write the message to a scratchpad file with python and commit with `git commit -F <file>` (PowerShell here-strings leak `@` into the subject when run through Bash).

---

## File map

**Core** (`Assets/_BattleBomb/Core/Players/`)
- Modify `CommandButtons.cs` — six menu flags.
- Create `MenuPress.cs` — one step's command reduced to menu intents; the Escape rule lives here once.
- Create `SeatAssignment.cs` — which device belongs to which seat; pure.
- Create `InputFamily.cs` — Keyboard or Gamepad.
- Create `PromptGlyphs.cs` — `PromptKey`, `GlyphShape`, `GlyphTone`, `PromptGlyph`, and the table.

**Input asset** — Modify `Assets/_BattleBomb/Input/BattleBombControls.inputactions`: Menu map; D-pad and arrows on Move.

**Gameplay** (`Assets/_BattleBomb/Gameplay/`)
- Modify `Players/PlayerActions.cs` — the Menu map's names.
- Create `Players/IInputDeviceReport.cs` — what a device-backed source can say about its device.
- Create `Players/SeatInput.cs` — one seat's copy of the controls; the device-facing code.
- Rewrite `Players/InputSystemCommandSource.cs` — thin MonoBehaviour over `SeatInput`; seat = player id.
- Modify `Players/PlayerRegistry.cs` — `FamilyOf`, `LastDeviceOf`.
- Modify `Session/GameSession.cs` — `Seats`.
- Modify `Characters/CharacterActor.cs`, `Characters/CharacterRegistry.cs` — two stale comments about `playerIndex`.

**UI** (`Assets/_BattleBomb/UI/`)
- Modify `Chest/ChestNavigation.cs` — `CycleTab`.
- Modify `Chest/ChestScreen.cs` — the new map, X/Y shortcuts, the prompt row.
- Modify `Chest/ChestScreen.Visuals.cs` — prompts replace the hint text; dead text-strip code removed; filter/grid spacing.
- Modify `Chest/ChestScreenHost.cs` — `FamilyFor`.
- Modify `Chest/HeroPanel.cs` — its own static hint line removed (the screen's row covers it).
- Modify `Chest/UiBuild.cs` — pad colours, `RoundedSquare`.
- Create `Chest/PromptRow.cs` — `Prompt` and the badge row.
- Modify `Chest/SettingsMenu.cs`, `Frontend/ResultsScreen.cs`, `Frontend/FrontendFlow.cs`, `Combat/LootHud.cs`, `Combat/ReviveHud.cs`.

**Scenes / prefab** — `Prefabs/Player.prefab`, `Scenes/Gameplay.unity`, `Scenes/Frontend.unity`: `PlayerInput` removed, `_controls` and `_seat` set.

**Tests**
- Modify `Tests/EditMode/BattleBomb.Tests.EditMode.asmdef`, `Tests/PlayMode/BattleBomb.Tests.PlayMode.asmdef` — reference `Unity.InputSystem.TestFramework`.
- Modify `Tests/EditMode/Acceptance/CommandVocabularyAcceptanceTests.cs`, `PlayerPrefabAcceptanceTests.cs`, `PlayersInSceneAcceptanceTests.cs`.
- Create `Tests/EditMode/MenuPressTests.cs`, `SeatAssignmentTests.cs`, `SeatInputTests.cs`, `PromptGlyphsTests.cs`, `Acceptance/PromptGlyphsAcceptanceTests.cs`, `Acceptance/FrontendSeatsAcceptanceTests.cs`.
- Modify `Tests/EditMode/ChestNavigationTests.cs`.
- Modify `Tests/PlayMode/LootLoopSmokeTests.cs`; create `Tests/PlayMode/SeatJoinSmokeTests.cs`.

**Docs** — `docs/DECISIONS.md` (D57), `docs/GAME_DESIGN.md` §3.1, `CLAUDE.md` rule 3, `docs/ARCHITECTURE.md` (wherever rule 3 is stated), `docs/ROADMAP.md`.

---

### Task 1: The menu vocabulary

The Core flags, the action-name constants, and the input asset must agree — the existing acceptance test already pins that for the fight's verbs. Extend the test first, watch it fail, then make all three agree.

**Files:**
- Modify: `Assets/_BattleBomb/Tests/EditMode/Acceptance/CommandVocabularyAcceptanceTests.cs` (whole file)
- Modify: `Assets/_BattleBomb/Core/Players/CommandButtons.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Players/PlayerActions.cs`
- Modify: `Assets/_BattleBomb/Input/BattleBombControls.inputactions` (via script)

- [ ] **Step 1: Rewrite the acceptance test**

Replace the whole of `CommandVocabularyAcceptanceTests.cs` with:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace BattleBomb.Tests.EditMode.Acceptance
{
    /// <summary>
    /// The game's buttons, in one vocabulary, in three places that must agree: the Core enum, the
    /// action-name constants, and the authored input asset. The fight's set is D17's scheme as
    /// amended by D26; the menu's set is D57's.
    /// </summary>
    public sealed class CommandVocabularyAcceptanceTests
    {
        /// <summary>
        /// The five combat verbs, and the whole of D17's budget: this is what one thumb operates
        /// mid-fight, and it is the list pillar 1 protects. Adding to it is a design decision.
        /// </summary>
        private static readonly string[] Buttons =
        {
            "Light",      // basic melee combo; performs Interact in context
            "Heavy",      // slow, high-commitment; launcher class
            "Magic",      // the character's element, offensively
            "Equipment",  // the quick-use slot (D37)
            "Jump",       // universal (§2.4)
        };

        /// <summary>
        /// System buttons, deliberately outside the five. They open menus, never act in the
        /// fight, and are never on the mobile thumb surface.
        /// </summary>
        private static readonly string[] SystemButtons =
        {
            "Pause",      // opens the settings menu (M6)
        };

        /// <summary>
        /// The menu layer (D57): what A, B, X, Y and the shoulders mean on a screen. Their own
        /// map, so rebinding a combat verb can never move "confirm". Never on the mobile thumb
        /// surface either — touch taps the thing it wants.
        /// </summary>
        private static readonly string[] MenuButtons =
        {
            "Confirm",
            "Back",
            "Option",
            "Lock",
            "TabPrevious",
            "TabNext",
        };

        private static string[] GameplayButtons => Buttons.Concat(SystemButtons).ToArray();

        private static string[] AllButtons => GameplayButtons.Concat(MenuButtons).ToArray();

        /// <summary>Every non-composite Gameplay binding the asset must carry, as (path, action).</summary>
        private static readonly (string Path, string Action)[] RequiredBindings =
        {
            ("<Keyboard>/j", "Light"),
            ("<Gamepad>/buttonWest", "Light"),
            ("<Keyboard>/k", "Heavy"),
            ("<Gamepad>/buttonNorth", "Heavy"),
            ("<Keyboard>/l", "Magic"),
            ("<Gamepad>/buttonEast", "Magic"),
            ("<Keyboard>/i", "Equipment"),
            ("<Gamepad>/rightShoulder", "Equipment"),
            ("<Keyboard>/space", "Jump"),
            ("<Gamepad>/buttonSouth", "Jump"),
            ("<Keyboard>/escape", "Pause"),
            ("<Gamepad>/start", "Pause"),
            ("<Gamepad>/leftStick", "Move"),
            ("<Gamepad>/dpad", "Move"),
        };

        /// <summary>Every Menu binding the asset must carry (D57's table).</summary>
        private static readonly (string Path, string Action)[] RequiredMenuBindings =
        {
            ("<Gamepad>/buttonSouth", "Confirm"),
            ("<Keyboard>/enter", "Confirm"),
            ("<Keyboard>/space", "Confirm"),
            ("<Gamepad>/buttonEast", "Back"),
            ("<Keyboard>/escape", "Back"),
            ("<Gamepad>/buttonWest", "Option"),
            ("<Keyboard>/j", "Option"),
            ("<Gamepad>/buttonNorth", "Lock"),
            ("<Keyboard>/k", "Lock"),
            ("<Gamepad>/leftShoulder", "TabPrevious"),
            ("<Keyboard>/q", "TabPrevious"),
            ("<Gamepad>/rightShoulder", "TabNext"),
            ("<Keyboard>/e", "TabNext"),
        };

        [Test]
        public void The_button_enum_holds_exactly_the_designed_buttons()
        {
            string[] actual = Enum.GetNames(typeof(CommandButtons)).OrderBy(n => n).ToArray();
            string[] expected = AllButtons.Concat(new[] { "None" }).OrderBy(n => n).ToArray();

            Assert.That(actual, Is.EqualTo(expected),
                "CommandButtons must match the ability slot table (§3), the system buttons, and " +
                "D57's menu buttons. Extra combat verbs are input complexity the design does not " +
                "have (pillar 1); missing ones cannot be pressed at all.");
        }

        [Test]
        public void Every_button_is_a_distinct_single_flag()
        {
            uint[] values = Enum.GetValues(typeof(CommandButtons))
                .Cast<CommandButtons>()
                .Where(v => v != CommandButtons.None)
                .Select(v => (uint)v)
                .ToArray();

            Assert.That(values.Distinct().Count(), Is.EqualTo(values.Length), "Two buttons share a value.");
            foreach (uint value in values)
            {
                Assert.That(value != 0 && (value & (value - 1)) == 0, Is.True,
                    $"Value {value} is not a single bit, so flag combinations will overlap.");
            }
        }

        [Test]
        public void The_action_name_constants_match_the_vocabulary()
        {
            Dictionary<string, string> constants = typeof(PlayerActions)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue());

            string[] expected = AllButtons.Concat(new[] { "Move", "Map", "MenuMap" }).OrderBy(n => n).ToArray();
            Assert.That(constants.Keys.OrderBy(n => n).ToArray(), Is.EqualTo(expected),
                "PlayerActions must name exactly the actions and maps the asset has.");

            foreach (string button in AllButtons.Concat(new[] { "Move" }))
            {
                Assert.That(constants[button], Is.EqualTo(button),
                    "An action constant whose value differs from its name is a trap for the next reader.");
            }
        }

        [Test]
        public void The_gameplay_map_defines_exactly_the_fight_and_the_system_buttons()
        {
            string[] actual = Map(PlayerActions.Map).actions.Select(a => a.name).OrderBy(n => n).ToArray();
            string[] expected = GameplayButtons.Concat(new[] { "Move" }).OrderBy(n => n).ToArray();

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void The_menu_map_defines_exactly_the_menu_buttons()
        {
            string[] actual = Map(PlayerActions.MenuMap).actions.Select(a => a.name).OrderBy(n => n).ToArray();

            Assert.That(actual, Is.EqualTo(MenuButtons.OrderBy(n => n).ToArray()));
        }

        [Test]
        public void The_input_asset_binds_every_action_on_keyboard_and_gamepad()
        {
            AssertBound(Map(PlayerActions.Map), RequiredBindings);
            AssertBound(Map(PlayerActions.MenuMap), RequiredMenuBindings);
        }

        [Test]
        public void Movement_keeps_its_WASD_composite_and_gains_the_arrows()
        {
            string[] parts = Map(PlayerActions.Map).bindings
                .Where(b => b.isPartOfComposite)
                .Select(b => b.path)
                .ToArray();

            foreach (string key in new[]
            {
                "<Keyboard>/w", "<Keyboard>/a", "<Keyboard>/s", "<Keyboard>/d",
                "<Keyboard>/upArrow", "<Keyboard>/leftArrow", "<Keyboard>/downArrow", "<Keyboard>/rightArrow",
            })
            {
                Assert.That(parts, Contains.Item(key));
            }
        }

        [Test]
        public void Every_action_and_binding_has_a_unique_id()
        {
            InputActionAsset asset = Asset();
            List<Guid> ids = asset.actionMaps.Select(m => m.id)
                .Concat(asset.actionMaps.SelectMany(m => m.actions).Select(a => a.id))
                .Concat(asset.actionMaps.SelectMany(m => m.bindings).Select(b => b.id))
                .ToList();

            Assert.That(ids, Has.No.Member(Guid.Empty), "A map, action or binding has no id.");

            Guid[] duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            Assert.That(duplicates, Is.Empty,
                "Duplicated ids: " + string.Join(", ", duplicates) +
                ". Copy-pasted ids silently alias bindings to each other.");
        }

        private static void AssertBound(InputActionMap map, (string Path, string Action)[] required)
        {
            List<InputBinding> bindings = map.bindings.Where(b => !b.isPartOfComposite).ToList();
            foreach ((string path, string action) in required)
            {
                bool bound = bindings.Any(b => b.path == path && b.action == action);
                Assert.That(bound, Is.True, $"Nothing in '{map.name}' binds {path} to {action}.");
            }
        }

        private static InputActionMap Map(string name)
        {
            InputActionMap map = Asset().FindActionMap(name);
            Assert.That(map, Is.Not.Null, $"No '{name}' action map in the asset.");
            return map;
        }

        private static InputActionAsset Asset()
        {
            InputActionAsset asset =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(AcceptanceFixture.ControlsAssetPath);
            Assert.That(asset, Is.Not.Null,
                $"{AcceptanceFixture.ControlsAssetPath} is missing or failed to import.");
            return asset;
        }
    }
}
```

- [ ] **Step 2: Recompile and watch it fail**

`recompile`, then `recompile_status`. Expected: **compile error** — `PlayerActions` has no `MenuMap`. That is the failure this step wants; continue.

- [ ] **Step 3: Add the menu flags to the Core enum**

In `Assets/_BattleBomb/Core/Players/CommandButtons.cs`, after the `Pause = 1 << 5,` line (inside the enum), add:

```csharp

        /// <summary>
        /// The menu layer (D57): what a screen's buttons mean, kept apart from the five verbs.
        /// They ride their own action map, so rebinding a combat verb can never move "confirm" —
        /// and one physical button can carry both, which is how A is Jump in a fight and Confirm
        /// on a screen. Never on the mobile thumb surface: touch taps the thing it wants (D5).
        /// </summary>
        Confirm     = 1 << 6,
        Back        = 1 << 7,

        /// <summary>The one-press verb for the item under the cursor: sell it, or mid-combine grind
        /// the whole pile. A shortcut only — every verb it fires is also in the item's own menu.</summary>
        Option      = 1 << 8,

        /// <summary>Lock or release the item under the cursor. A shortcut, like Option.</summary>
        Lock        = 1 << 9,

        TabPrevious = 1 << 10,
        TabNext     = 1 << 11,
```

- [ ] **Step 4: Add the constants**

In `Assets/_BattleBomb/Gameplay/Players/PlayerActions.cs`, after the `Pause` constant, add:

```csharp

        /// <summary>The menu layer's own map (D57): screens read these, the fight never does.</summary>
        public const string MenuMap = "Menu";

        public const string Confirm = "Confirm";
        public const string Back = "Back";
        public const string Option = "Option";
        public const string Lock = "Lock";
        public const string TabPrevious = "TabPrevious";
        public const string TabNext = "TabNext";
```

- [ ] **Step 5: Author the Menu map, the D-pad, and the arrows**

The asset is CRLF, four-space-indented JSON with hand-written ids (`b471b0bb-0000-4000-8000-…`). Write this to the scratchpad as `add_menu_map.py` and run it from the repo root with `python add_menu_map.py`:

```python
import json
import pathlib

path = pathlib.Path("Assets/_BattleBomb/Input/BattleBombControls.inputactions")
doc = json.loads(path.read_text(encoding="utf-8-sig"))
assert not any(m["name"] == "Menu" for m in doc["maps"]), "the Menu map already exists"

taken = {m["id"] for m in doc["maps"]}
taken |= {a["id"] for m in doc["maps"] for a in m["actions"]}
taken |= {b["id"] for m in doc["maps"] for b in m["bindings"]}
counter = [0x200]


def new_id():
    while True:
        value = f"b471b0bb-0000-4000-8000-{counter[0]:012x}"
        counter[0] += 1
        if value not in taken:
            taken.add(value)
            return value


def binding(action, path, group, name="", composite=False, part=False):
    return {
        "name": name, "id": new_id(), "path": path, "interactions": "", "processors": "",
        "groups": group, "action": action, "isComposite": composite, "isPartOfComposite": part,
    }


def action(name):
    return {
        "name": name, "type": "Button", "id": new_id(), "expectedControlType": "Button",
        "processors": "", "interactions": "", "initialStateCheck": False,
    }


gameplay = next(m for m in doc["maps"] if m["name"] == "Gameplay")
gameplay["bindings"] += [
    binding("Move", "<Gamepad>/dpad", "Gamepad"),
    binding("Move", "2DVector", "", name="Arrows", composite=True),
    binding("Move", "<Keyboard>/upArrow", "Keyboard", name="up", part=True),
    binding("Move", "<Keyboard>/downArrow", "Keyboard", name="down", part=True),
    binding("Move", "<Keyboard>/leftArrow", "Keyboard", name="left", part=True),
    binding("Move", "<Keyboard>/rightArrow", "Keyboard", name="right", part=True),
]

menu = [
    ("Confirm", "<Gamepad>/buttonSouth", "Gamepad"),
    ("Confirm", "<Keyboard>/enter", "Keyboard"),
    ("Confirm", "<Keyboard>/numpadEnter", "Keyboard"),
    ("Confirm", "<Keyboard>/space", "Keyboard"),
    ("Back", "<Gamepad>/buttonEast", "Gamepad"),
    ("Back", "<Keyboard>/escape", "Keyboard"),
    ("Option", "<Gamepad>/buttonWest", "Gamepad"),
    ("Option", "<Keyboard>/j", "Keyboard"),
    ("Lock", "<Gamepad>/buttonNorth", "Gamepad"),
    ("Lock", "<Keyboard>/k", "Keyboard"),
    ("TabPrevious", "<Gamepad>/leftShoulder", "Gamepad"),
    ("TabPrevious", "<Keyboard>/q", "Keyboard"),
    ("TabNext", "<Gamepad>/rightShoulder", "Gamepad"),
    ("TabNext", "<Keyboard>/e", "Keyboard"),
]

doc["maps"].append({
    "name": "Menu",
    "id": new_id(),
    "actions": [action(n) for n in ["Confirm", "Back", "Option", "Lock", "TabPrevious", "TabNext"]],
    "bindings": [binding(a, p, g) for a, p, g in menu],
})

text = json.dumps(doc, indent=4, ensure_ascii=False).replace("\n", "\r\n") + "\r\n"
path.write_bytes(text.encode("utf-8"))
print("maps:", [m["name"] for m in doc["maps"]])
```

Expected output: `maps: ['Gameplay', 'Menu']`.

- [ ] **Step 6: Recompile, import, and run the fixture**

`recompile` (it also refreshes assets). Then run `CommandVocabularyAcceptanceTests`. Expected: **8 passed**.

- [ ] **Step 7: Run the whole EditMode suite**

Expected: **627 passed** — the old fixture had 7 tests and the new one has 8. No failures elsewhere; nothing reads the new flags yet. Report the real number if it differs, and why.

- [ ] **Step 8: Commit**

```bash
git add Assets/_BattleBomb/Core/Players/CommandButtons.cs Assets/_BattleBomb/Gameplay/Players/PlayerActions.cs Assets/_BattleBomb/Input/BattleBombControls.inputactions Assets/_BattleBomb/Tests/EditMode/Acceptance/CommandVocabularyAcceptanceTests.cs
git commit -F <scratchpad>/g1.txt
```

Subject: `G1: the menu vocabulary — its own map, six buttons, the D-pad and arrows`.

---

### Task 2: One rule for reading a menu press

Escape is bound to both Back (menu map) and Pause (gameplay map). A screen must treat that as Back, while the pad's Start — Pause alone — still leaves outright. Four screens need that rule; it lives once, in Core.

**Files:**
- Create: `Assets/_BattleBomb/Core/Players/MenuPress.cs`
- Test: `Assets/_BattleBomb/Tests/EditMode/MenuPressTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// How a screen reads one step's buttons (D57). The rule worth pinning is Escape's: it is
    /// bound to Back and to Pause at once, and a menu must hear only the Back.
    /// </summary>
    public sealed class MenuPressTests
    {
        private static PlayerCommand Pressed(CommandButtons buttons) =>
            PlayerCommand.FromState(1, Vector2.zero, buttons, CommandButtons.None);

        [Test]
        public void Escape_is_a_back_not_a_pause()
        {
            MenuPress press = MenuPress.From(Pressed(CommandButtons.Back | CommandButtons.Pause));

            Assert.That(press.Back, Is.True);
            Assert.That(press.Pause, Is.False,
                "Escape backs out one level. Read as Pause too, it would close the whole screen.");
        }

        [Test]
        public void Start_alone_is_a_pause()
        {
            MenuPress press = MenuPress.From(Pressed(CommandButtons.Pause));

            Assert.That(press.Pause, Is.True);
            Assert.That(press.Back, Is.False);
        }

        [Test]
        public void Each_menu_button_reads_as_its_own_intent()
        {
            Assert.That(MenuPress.From(Pressed(CommandButtons.Confirm)).Confirm, Is.True);
            Assert.That(MenuPress.From(Pressed(CommandButtons.Option)).Option, Is.True);
            Assert.That(MenuPress.From(Pressed(CommandButtons.Lock)).Lock, Is.True);
            Assert.That(MenuPress.From(Pressed(CommandButtons.TabNext)).Tab, Is.EqualTo(1));
            Assert.That(MenuPress.From(Pressed(CommandButtons.TabPrevious)).Tab, Is.EqualTo(-1));
        }

        [Test]
        public void Both_shoulders_at_once_go_nowhere()
        {
            MenuPress press = MenuPress.From(Pressed(CommandButtons.TabNext | CommandButtons.TabPrevious));

            Assert.That(press.Tab, Is.Zero);
        }

        [Test]
        public void The_fight_verbs_mean_nothing_to_a_menu()
        {
            MenuPress press = MenuPress.From(Pressed(
                CommandButtons.Light | CommandButtons.Heavy | CommandButtons.Magic
                | CommandButtons.Jump | CommandButtons.Equipment));

            Assert.That(press.Any, Is.False,
                "A menu reads its own buttons. The fight's verbs share physical buttons with them, " +
                "but a screen that read Light would sell on the press that opened it.");
        }

        [Test]
        public void A_held_button_is_not_a_new_press()
        {
            PlayerCommand held = PlayerCommand.FromState(
                2, Vector2.zero, CommandButtons.Confirm, CommandButtons.Confirm);

            Assert.That(MenuPress.From(held).Confirm, Is.False);
        }

        [Test]
        public void Anything_held_keeps_a_fresh_screen_waiting()
        {
            PlayerCommand holdingLight = PlayerCommand.FromState(
                1, Vector2.zero, CommandButtons.Light, CommandButtons.Light);

            Assert.That(MenuPress.AnyHeld(holdingLight), Is.True);
            Assert.That(MenuPress.AnyHeld(PlayerCommand.Idle(1)), Is.False);
        }
    }
}
```

- [ ] **Step 2: Run it to see it fail**

`recompile`. Expected: compile error, `MenuPress` does not exist.

- [ ] **Step 3: Write `MenuPress`**

```csharp
namespace BattleBomb.Core.Players
{
    /// <summary>
    /// What a screen should do with one step's command (D57). Every menu reads its buttons
    /// through this, so the one rule that is easy to get wrong lives in one place: Escape is
    /// bound to both Back and Pause, and a step that carries Back is a Back. Start — Pause on
    /// its own — is the pad's way to leave a screen outright.
    /// </summary>
    public readonly struct MenuPress
    {
        public readonly bool Confirm;
        public readonly bool Back;
        public readonly bool Pause;
        public readonly bool Option;
        public readonly bool Lock;

        /// <summary>-1 for the left shoulder, +1 for the right, 0 for neither or both.</summary>
        public readonly int Tab;

        private MenuPress(bool confirm, bool back, bool pause, bool option, bool locking, int tab)
        {
            Confirm = confirm;
            Back = back;
            Pause = pause;
            Option = option;
            Lock = locking;
            Tab = tab;
        }

        public bool Any => Confirm || Back || Pause || Option || Lock || Tab != 0;

        public static MenuPress From(in PlayerCommand command)
        {
            bool back = command.WasPressed(CommandButtons.Back);
            int tab = (command.WasPressed(CommandButtons.TabNext) ? 1 : 0)
                - (command.WasPressed(CommandButtons.TabPrevious) ? 1 : 0);

            return new MenuPress(
                command.WasPressed(CommandButtons.Confirm),
                back,
                command.WasPressed(CommandButtons.Pause) && !back,
                command.WasPressed(CommandButtons.Option),
                command.WasPressed(CommandButtons.Lock),
                tab);
        }

        /// <summary>
        /// Any button at all held. A screen that has just opened ignores input until this is false
        /// once: every menu button shares a physical button with a combat verb, so the press that
        /// opened the screen — X, which is Light and Option — would otherwise act inside it.
        /// </summary>
        public static bool AnyHeld(in PlayerCommand command) => command.Held != CommandButtons.None;
    }
}
```

- [ ] **Step 4: Run the fixture**

`recompile`, run `MenuPressTests`. Expected: **7 passed**.

- [ ] **Step 5: Commit** — subject `G2: one rule for reading a menu press`.

---

### Task 3: Which device belongs to which seat

The whole of "input switching" is one pure rule. Write it in Core, test it to death, and the device code in Task 4 only has to obey it.

**Files:**
- Create: `Assets/_BattleBomb/Core/Players/SeatAssignment.cs`
- Test: `Assets/_BattleBomb/Tests/EditMode/SeatAssignmentTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Players;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// Who owns which device (D57). One rule — Player 2 owns the device they joined with, Player 1
    /// owns everything else — plus character select, where the empty seat listens for a join.
    /// </summary>
    public sealed class SeatAssignmentTests
    {
        private const int Keyboard = 1;
        private const int PadA = 5;
        private const int PadB = 9;

        [Test]
        public void A_fresh_assignment_gives_player_one_every_device()
        {
            var seats = new SeatAssignment();

            Assert.That(seats.Owns(0, Keyboard), Is.True);
            Assert.That(seats.Owns(0, PadA), Is.True);
            Assert.That(seats.Owns(0, PadB), Is.True);
            Assert.That(seats.Owns(1, PadA), Is.False, "Nobody sits in seat two yet.");
        }

        [Test]
        public void At_character_select_player_one_is_held_to_the_device_that_started()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, secondJoined: false, PadA, SeatAssignment.NoDevice);

            Assert.That(seats.FirstSeatHome, Is.EqualTo(PadA));
            Assert.That(seats.Owns(0, PadA), Is.True);
            Assert.That(seats.Owns(0, PadB), Is.False,
                "A press on another pad is a join, so it cannot also be Player 1's.");
            Assert.That(seats.Owns(1, PadB), Is.True);
            Assert.That(seats.Owns(1, Keyboard), Is.True);
            Assert.That(seats.Owns(1, PadA), Is.False);
        }

        [Test]
        public void Joining_on_another_device_seats_player_two_on_it()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, PadA, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, true, PadA, PadB);

            Assert.That(seats.SecondSeatDevice, Is.EqualTo(PadB));
            Assert.That(seats.Owns(1, PadB), Is.True);
            Assert.That(seats.Owns(1, Keyboard), Is.False, "Player 2 owns only what they joined with.");
            Assert.That(seats.Owns(0, PadB), Is.False);
            Assert.That(seats.Owns(0, Keyboard), Is.True, "Player 1 gets everything else back.");
        }

        [Test]
        public void Player_two_leaving_frees_their_device()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, PadA, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, true, PadA, PadB);
            seats.Follow(FrontendScreen.Characters, false, PadA, PadB);

            Assert.That(seats.SecondSeatDevice, Is.EqualTo(SeatAssignment.NoDevice));
            Assert.That(seats.Owns(1, PadB), Is.True, "Still at character select, so it can rejoin.");
        }

        [Test]
        public void Leaving_character_select_alone_hands_player_one_every_device()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, PadA, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Chapters, false, PadA, SeatAssignment.NoDevice);

            Assert.That(seats.Joining, Is.False);
            Assert.That(seats.Owns(0, PadB), Is.True);
            Assert.That(seats.Owns(0, Keyboard), Is.True);
            Assert.That(seats.Owns(1, PadB), Is.False);
        }

        [Test]
        public void A_couch_keeps_its_seats_past_character_select()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, Keyboard, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, true, Keyboard, PadA);
            seats.Follow(FrontendScreen.Chapters, true, Keyboard, PadA);

            Assert.That(seats.Owns(1, PadA), Is.True);
            Assert.That(seats.Owns(0, PadB), Is.True, "A third pad belongs to Player 1.");
            Assert.That(seats.Owns(0, PadA), Is.False);
        }

        [Test]
        public void The_title_forgets_both_seats()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, PadA, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, true, PadA, PadB);
            seats.Follow(FrontendScreen.Title, false, PadA, PadB);

            Assert.That(seats.FirstSeatHome, Is.EqualTo(SeatAssignment.NoDevice));
            Assert.That(seats.SecondSeatDevice, Is.EqualTo(SeatAssignment.NoDevice));
            Assert.That(seats.Owns(0, PadB), Is.True, "On the title anyone can start.");
        }

        [Test]
        public void A_join_on_player_ones_own_device_seats_nobody()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, PadA, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, true, PadA, PadA);

            Assert.That(seats.SecondSeatDevice, Is.EqualTo(SeatAssignment.NoDevice));
        }

        [Test]
        public void Player_one_is_homed_on_their_first_press_when_they_started_by_pointer()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, SeatAssignment.NoDevice, SeatAssignment.NoDevice);
            Assert.That(seats.Owns(1, PadB), Is.False, "No home yet, so nothing can be a join.");

            seats.Follow(FrontendScreen.Characters, false, Keyboard, SeatAssignment.NoDevice);
            Assert.That(seats.FirstSeatHome, Is.EqualTo(Keyboard));
            Assert.That(seats.Owns(1, PadB), Is.True);
        }

        [Test]
        public void No_device_is_owned_by_nobody()
        {
            var seats = new SeatAssignment();

            Assert.That(seats.Owns(0, SeatAssignment.NoDevice), Is.False);
            Assert.That(seats.Owns(1, SeatAssignment.NoDevice), Is.False);
            Assert.That(seats.Owns(2, PadA), Is.False, "There are two seats.");
        }

        [Test]
        public void With_no_front_door_player_two_stands_in_on_the_first_controller()
        {
            var seats = new SeatAssignment();
            seats.StandIn(PadA);

            Assert.That(seats.Owns(1, PadA), Is.True);
            Assert.That(seats.Owns(0, PadA), Is.False);
            Assert.That(seats.Owns(0, Keyboard), Is.True);

            seats.StandIn(SeatAssignment.NoDevice);
            Assert.That(seats.Owns(0, PadA), Is.True, "No controller: Player 1 has everything.");
        }
    }
}
```

- [ ] **Step 2: Run it to see it fail** — `recompile`; expected compile error, `SeatAssignment` missing.

- [ ] **Step 3: Write `SeatAssignment`**

```csharp
using BattleBomb.Core.Chapters;

namespace BattleBomb.Core.Players
{
    /// <summary>
    /// Which input devices belong to which couch seat (D57). One rule: <b>Player 2 owns the
    /// device they joined with; Player 1 owns everything else</b> — so a solo player can pick up
    /// any controller, or the keyboard, at any moment, and that is all "switching" is.
    ///
    /// The exception is character select before Player 2 has joined. There Player 1 is held to
    /// the device that started the game, and every other device belongs to the empty seat, so a
    /// press on one is a join rather than Player 1 readying up.
    ///
    /// Devices are the Input System's <c>deviceId</c>s, carried as plain ints so Core never sees
    /// a device (rules 1 and 3).
    /// </summary>
    public sealed class SeatAssignment
    {
        public const int NoDevice = -1;

        /// <summary>The device that started the game from the title. Only consulted while
        /// Player 2 could still join.</summary>
        public int FirstSeatHome { get; private set; } = NoDevice;

        public int SecondSeatDevice { get; private set; } = NoDevice;

        /// <summary>Character select is up, so an empty second seat listens for a join.</summary>
        public bool Joining { get; private set; }

        public bool Owns(int seat, int deviceId)
        {
            if (deviceId == NoDevice)
            {
                return false;
            }

            if (seat == 1)
            {
                if (SecondSeatDevice != NoDevice)
                {
                    return deviceId == SecondSeatDevice;
                }

                return Joining && FirstSeatHome != NoDevice && deviceId != FirstSeatHome;
            }

            if (seat == 0)
            {
                if (Joining && SecondSeatDevice == NoDevice && FirstSeatHome != NoDevice)
                {
                    return deviceId == FirstSeatHome;
                }

                return deviceId != SecondSeatDevice;
            }

            return false;
        }

        /// <summary>
        /// Brings the seats in line with the front door. Called every frame it runs, with the
        /// device each seat last pressed, so it is declarative: whatever the front door's state
        /// says, the seats agree with by the next frame. The title forgets everything — anyone
        /// can start; the first frame past it homes Player 1 on the device that did.
        /// </summary>
        public void Follow(
            FrontendScreen screen, bool secondJoined, int firstSeatLastDevice, int secondSeatLastDevice)
        {
            if (screen == FrontendScreen.Title)
            {
                FirstSeatHome = NoDevice;
                SecondSeatDevice = NoDevice;
                Joining = false;
                return;
            }

            if (FirstSeatHome == NoDevice)
            {
                FirstSeatHome = firstSeatLastDevice;
            }

            Joining = screen == FrontendScreen.Characters;

            if (!secondJoined)
            {
                SecondSeatDevice = NoDevice;
                return;
            }

            if (SecondSeatDevice == NoDevice
                && secondSeatLastDevice != NoDevice
                && secondSeatLastDevice != FirstSeatHome)
            {
                SecondSeatDevice = secondSeatLastDevice;
            }
        }

        /// <summary>
        /// No front door ran — the Gameplay scene was opened on its own. Player 2 stands in on the
        /// first controller and Player 1 has the rest, which is how that scene has always behaved.
        /// </summary>
        public void StandIn(int firstGamepad)
        {
            FirstSeatHome = NoDevice;
            Joining = false;
            SecondSeatDevice = firstGamepad;
        }
    }
}
```

- [ ] **Step 4: Run the fixture** — `recompile`, run `SeatAssignmentTests`. Expected: **11 passed**.

- [ ] **Step 5: Commit** — subject `G3: which device belongs to which seat`.

---

### Task 4: One seat's controls

The device-facing code, split out of the MonoBehaviour so it can be tested against virtual controllers with no scene. `InputTestFixture` swaps the machine's real devices out for each test, so nothing plugged in can press anything. Its assembly, `Unity.InputSystem.TestFramework`, is already compiled in this project (its only define constraint is the test framework's presence) — no `testables` entry is needed.

**Files:**
- Create: `Assets/_BattleBomb/Core/Players/InputFamily.cs`
- Create: `Assets/_BattleBomb/Gameplay/Players/SeatInput.cs`
- Modify: `Assets/_BattleBomb/Tests/EditMode/BattleBomb.Tests.EditMode.asmdef`
- Test: `Assets/_BattleBomb/Tests/EditMode/SeatInputTests.cs`

- [ ] **Step 1: Reference the Input System's test framework**

In `BattleBomb.Tests.EditMode.asmdef`, add `"Unity.InputSystem.TestFramework"` to `references`, after `"Unity.InputSystem"`:

```json
        "Unity.InputSystem",
        "Unity.InputSystem.TestFramework"
```

- [ ] **Step 2: Write the failing test**

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Players;
using BattleBomb.Tests.EditMode.Acceptance;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// A seat against virtual controllers (D57). The fixture replaces the machine's real devices
    /// for the length of each test, so only what a test adds exists and only what it presses is down.
    /// </summary>
    public sealed class SeatInputTests : InputTestFixture
    {
        private readonly List<SeatInput> _made = new List<SeatInput>();
        private InputActionAsset _controls;
        private Keyboard _keyboard;
        private Gamepad _padA;
        private Gamepad _padB;
        private int _frame;

        public override void Setup()
        {
            base.Setup();
            _controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AcceptanceFixture.ControlsAssetPath);
            Assert.That(_controls, Is.Not.Null, "The controls asset did not load.");
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _padA = InputSystem.AddDevice<Gamepad>();
            _padB = InputSystem.AddDevice<Gamepad>();
            _frame = 0;
        }

        public override void TearDown()
        {
            for (int i = 0; i < _made.Count; i++)
            {
                _made[i].Dispose();
            }

            _made.Clear();
            base.TearDown();
        }

        /// <summary>A seat that has already taken its first, priming sample.</summary>
        private SeatInput Seat(int seat, SeatAssignment seats)
        {
            var input = new SeatInput(_controls, seat);
            _made.Add(input);
            Next(input, seats);
            return input;
        }

        private PlayerCommand Next(SeatInput seat, SeatAssignment seats)
        {
            seat.Own(seats);
            return seat.Sample(++_frame);
        }

        [Test]
        public void A_solo_player_answers_to_the_keyboard_and_every_controller()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);

            Press(_padB.buttonSouth);
            Assert.That(Next(one, seats).WasPressed(CommandButtons.Jump), Is.True, "The second pad.");
            Release(_padB.buttonSouth);
            Next(one, seats);

            Press(_keyboard.jKey);
            Assert.That(Next(one, seats).WasPressed(CommandButtons.Light), Is.True, "The keyboard.");
            Release(_keyboard.jKey);
            Next(one, seats);

            Press(_padA.buttonWest);
            Assert.That(Next(one, seats).WasPressed(CommandButtons.Light), Is.True, "The first pad.");
        }

        [Test]
        public void The_prompts_follow_whatever_was_pressed_last()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);

            Press(_padA.buttonNorth);
            Next(one, seats);
            Assert.That(one.Family, Is.EqualTo(InputFamily.Gamepad));
            Assert.That(one.LastDeviceId, Is.EqualTo(_padA.deviceId));
            Release(_padA.buttonNorth);
            Next(one, seats);

            Press(_keyboard.kKey);
            Next(one, seats);
            Assert.That(one.Family, Is.EqualTo(InputFamily.Keyboard));
            Assert.That(one.LastDeviceId, Is.EqualTo(_keyboard.deviceId));
        }

        [Test]
        public void One_press_carries_the_fight_and_the_menu_together()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);

            Press(_padA.buttonSouth);
            PlayerCommand a = Next(one, seats);
            Assert.That(a.WasPressed(CommandButtons.Jump) && a.WasPressed(CommandButtons.Confirm), Is.True,
                "A is Jump in a fight and Confirm on a screen.");
            Release(_padA.buttonSouth);
            Next(one, seats);

            Press(_keyboard.escapeKey);
            PlayerCommand escape = Next(one, seats);
            Assert.That(escape.WasPressed(CommandButtons.Pause) && escape.WasPressed(CommandButtons.Back), Is.True,
                "Escape pauses outside a menu and backs out inside one.");
            Release(_keyboard.escapeKey);
            Next(one, seats);

            Press(_padA.rightShoulder);
            PlayerCommand rb = Next(one, seats);
            Assert.That(rb.WasPressed(CommandButtons.Equipment) && rb.WasPressed(CommandButtons.TabNext), Is.True);
        }

        [Test]
        public void Two_controllers_drive_two_seats_and_never_each_other()
        {
            var seats = new SeatAssignment();
            seats.StandIn(_padB.deviceId);
            SeatInput one = Seat(0, seats);
            SeatInput two = Seat(1, seats);

            Press(_padB.buttonSouth);
            Assert.That(Next(two, seats).WasPressed(CommandButtons.Jump), Is.True);
            Assert.That(Next(one, seats).IsHeld(CommandButtons.Jump), Is.False, "Player 2's pad moved Player 1.");
            Release(_padB.buttonSouth);
            Next(one, seats);
            Next(two, seats);

            Press(_padA.buttonSouth);
            Assert.That(Next(one, seats).WasPressed(CommandButtons.Jump), Is.True);
            Assert.That(Next(two, seats).IsHeld(CommandButtons.Jump), Is.False, "Player 1's pad moved Player 2.");
        }

        [Test]
        public void An_empty_seat_hears_nothing()
        {
            var seats = new SeatAssignment();
            SeatInput two = Seat(1, seats);

            Press(_padA.buttonSouth);
            Press(_keyboard.spaceKey);
            Assert.That(Next(two, seats).Held, Is.EqualTo(CommandButtons.None));
        }

        [Test]
        public void A_controller_plugged_in_later_is_player_ones_on_the_next_sample()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);

            Gamepad late = InputSystem.AddDevice<Gamepad>();
            Next(one, seats);
            Press(late.buttonEast);

            Assert.That(Next(one, seats).WasPressed(CommandButtons.Magic), Is.True);
        }

        [Test]
        public void A_button_already_down_when_the_seat_wakes_is_not_a_press()
        {
            Press(_padA.buttonSouth);
            var input = new SeatInput(_controls, 0);
            _made.Add(input);

            PlayerCommand first = Next(input, new SeatAssignment());

            Assert.That(first.IsHeld(CommandButtons.Confirm), Is.True);
            Assert.That(first.WasPressed(CommandButtons.Confirm), Is.False,
                "A held across a scene change would confirm the next screen the instant it loaded.");
        }
    }
}
```

- [ ] **Step 3: Run it to see it fail** — `recompile`; expected compile errors, `SeatInput` and `InputFamily` missing.

- [ ] **Step 4: Write `InputFamily`**

```csharp
namespace BattleBomb.Core.Players
{
    /// <summary>
    /// Which set of button pictures a player's prompts show (D57). Xbox letters on every
    /// controller, by Michael's choice (2026-09-24); key caps on a keyboard.
    /// </summary>
    public enum InputFamily
    {
        Keyboard = 0,
        Gamepad = 1,
    }
}
```

- [ ] **Step 5: Write `SeatInput`**

```csharp
using System;
using System.Collections.Generic;
using BattleBomb.Core.Players;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using Object = UnityEngine.Object;

namespace BattleBomb.Gameplay.Players
{
    /// <summary>
    /// One couch seat's controls: its own copy of the action asset, bound to exactly the devices
    /// the seat owns (<see cref="SeatAssignment"/>), sampled into a <see cref="PlayerCommand"/>.
    /// With the <see cref="InputSystemCommandSource"/> that owns it, this is the only code that
    /// touches a device (rule 3). It is a plain class so it can be tested against virtual
    /// controllers without a scene.
    /// </summary>
    /// <remarks>
    /// Replaces PlayerInput (D57). PlayerInput paired devices by control scheme, which pinned
    /// Player 1 to the keyboard and Player 2 to the first gamepad: two controllers on one couch
    /// could not both play, and a solo player on a pad was Player 2 on the title screen. Owning
    /// the copy directly lets one seat hold a keyboard and three pads at once.
    /// </remarks>
    public sealed class SeatInput : IDisposable
    {
        private readonly InputActionAsset _actions;
        private readonly int _seat;
        private readonly List<(InputAction action, CommandButtons button)> _buttons =
            new List<(InputAction, CommandButtons)>();
        private readonly List<InputDevice> _owned = new List<InputDevice>();
        private readonly InputAction _move;

        private CommandButtons _previouslyHeld;
        private bool _primed;
        private InputDevice _lastDevice;

        public SeatInput(InputActionAsset controls, int seat)
        {
            _actions = Object.Instantiate(controls);
            _actions.name = $"{controls.name} (seat {seat})";
            _seat = seat;

            _move = Find(PlayerActions.Map, PlayerActions.Move);
            AddButton(PlayerActions.Map, PlayerActions.Light, CommandButtons.Light);
            AddButton(PlayerActions.Map, PlayerActions.Heavy, CommandButtons.Heavy);
            AddButton(PlayerActions.Map, PlayerActions.Magic, CommandButtons.Magic);
            AddButton(PlayerActions.Map, PlayerActions.Equipment, CommandButtons.Equipment);
            AddButton(PlayerActions.Map, PlayerActions.Jump, CommandButtons.Jump);
            AddButton(PlayerActions.Map, PlayerActions.Pause, CommandButtons.Pause);
            AddButton(PlayerActions.MenuMap, PlayerActions.Confirm, CommandButtons.Confirm);
            AddButton(PlayerActions.MenuMap, PlayerActions.Back, CommandButtons.Back);
            AddButton(PlayerActions.MenuMap, PlayerActions.Option, CommandButtons.Option);
            AddButton(PlayerActions.MenuMap, PlayerActions.Lock, CommandButtons.Lock);
            AddButton(PlayerActions.MenuMap, PlayerActions.TabPrevious, CommandButtons.TabPrevious);
            AddButton(PlayerActions.MenuMap, PlayerActions.TabNext, CommandButtons.TabNext);

            // Owns nothing until the first Own call says otherwise.
            _actions.devices = Array.Empty<InputDevice>();
            _actions.Enable();
        }

        /// <summary>The Input System id of the device that last did anything, or
        /// <see cref="SeatAssignment.NoDevice"/>.</summary>
        public int LastDeviceId => _lastDevice != null ? _lastDevice.deviceId : SeatAssignment.NoDevice;

        public InputFamily Family => _lastDevice is Gamepad ? InputFamily.Gamepad : InputFamily.Keyboard;

        /// <summary>
        /// Rebinds to exactly the devices the seats give this one. Cheap when nothing changed, so
        /// it runs before every sample: a controller plugged in mid-game belongs to somebody on the
        /// next step, and a seat handed over at character select takes effect the same frame.
        /// </summary>
        public void Own(SeatAssignment seats)
        {
            _owned.Clear();
            ReadOnlyArray<InputDevice> devices = InputSystem.devices;
            for (int i = 0; i < devices.Count; i++)
            {
                InputDevice device = devices[i];
                if ((device is Keyboard || device is Gamepad) && seats.Owns(_seat, device.deviceId))
                {
                    _owned.Add(device);
                }
            }

            if (!Matches(_actions.devices, _owned))
            {
                _actions.devices = _owned.ToArray();
            }

            if (_lastDevice != null && !_owned.Contains(_lastDevice))
            {
                _lastDevice = null;
            }

            if (_lastDevice == null)
            {
                _lastDevice = MostRecentlyUpdated(_owned);
            }
        }

        public PlayerCommand Sample(int frame)
        {
            Vector2 move = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
            if (move.sqrMagnitude > 0.25f)
            {
                Remember(_move);
            }

            CommandButtons held = CommandButtons.None;
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i].action.IsPressed())
                {
                    held |= _buttons[i].button;
                    Remember(_buttons[i].action);
                }
            }

            // The first sample treats whatever is already down as held rather than pressed: a
            // button carried across a scene change is not a new decision.
            CommandButtons before = _primed ? _previouslyHeld : held;
            _primed = true;

            PlayerCommand command = PlayerCommand.FromState(frame, move, held, before);
            _previouslyHeld = held;
            return command;
        }

        public void Dispose()
        {
            _actions.Disable();
            if (Application.isPlaying)
            {
                Object.Destroy(_actions);
            }
            else
            {
                Object.DestroyImmediate(_actions);
            }
        }

        private void Remember(InputAction action)
        {
            InputControl control = action.activeControl;
            if (control != null)
            {
                _lastDevice = control.device;
            }
        }

        /// <summary>Before anything is pressed, the device touched most recently is the best guess
        /// — a Steam Deck player should see A B X Y on the title, not key caps.</summary>
        private static InputDevice MostRecentlyUpdated(List<InputDevice> devices)
        {
            InputDevice best = null;
            for (int i = 0; i < devices.Count; i++)
            {
                if (best == null || devices[i].lastUpdateTime > best.lastUpdateTime)
                {
                    best = devices[i];
                }
            }

            return best;
        }

        private static bool Matches(ReadOnlyArray<InputDevice>? current, List<InputDevice> wanted)
        {
            if (!current.HasValue || current.Value.Count != wanted.Count)
            {
                return false;
            }

            ReadOnlyArray<InputDevice> devices = current.Value;
            for (int i = 0; i < wanted.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < devices.Count; j++)
                {
                    if (devices[j] == wanted[i])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private void AddButton(string map, string actionName, CommandButtons button)
        {
            InputAction action = Find(map, actionName);
            if (action != null)
            {
                _buttons.Add((action, button));
            }
        }

        private InputAction Find(string map, string actionName)
        {
            InputAction action = _actions.FindAction($"{map}/{actionName}", throwIfNotFound: false);
            if (action == null)
            {
                Debug.LogWarning($"Seat {_seat}: action '{map}/{actionName}' not found — it will read as inactive.");
            }

            return action;
        }
    }
}
```

- [ ] **Step 6: Run the fixture**

`recompile`, run `SeatInputTests`. Expected: **7 passed**.

If `A_controller_plugged_in_later…` fails while the others pass, the Input System is not re-resolving bindings when `devices` changes on an enabled asset. Fix inside `Own` by wrapping the assignment — `_actions.Disable(); _actions.devices = _owned.ToArray(); _actions.Enable();` — and rerun. Do not change the test.

- [ ] **Step 7: Run the whole EditMode suite** — expected all green (the new source is not wired to anything yet).

- [ ] **Step 8: Commit** — subject `G4: one seat's controls, against virtual controllers`.

---

### Task 5: Seats replace PlayerInput

Wire `SeatInput` into the MonoBehaviour, carry the seats on the session, drop `PlayerInput` from the prefab and both scenes, and make the seat the player's id — which is the "P2" fix: the id was `PlayerInput.playerIndex`, allocated across every PlayerInput alive, and the front door's claimed indices first.

Between this task and Task 6 the front door has no seating logic, so Player 2 cannot join there. That is expected; do not ship between the two.

**Files:**
- Create: `Assets/_BattleBomb/Gameplay/Players/IInputDeviceReport.cs`
- Rewrite: `Assets/_BattleBomb/Gameplay/Players/InputSystemCommandSource.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Players/PlayerRegistry.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Session/GameSession.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Characters/CharacterActor.cs:100-103`, `Assets/_BattleBomb/Gameplay/Characters/CharacterRegistry.cs:17`
- Modify: `Assets/_BattleBomb/Tests/EditMode/Acceptance/PlayerPrefabAcceptanceTests.cs`, `PlayersInSceneAcceptanceTests.cs`
- Create: `Assets/_BattleBomb/Tests/EditMode/Acceptance/FrontendSeatsAcceptanceTests.cs`
- Scene/prefab: `Prefabs/Player.prefab`, `Scenes/Gameplay.unity`, `Scenes/Frontend.unity`

- [ ] **Step 1: Rewrite the prefab acceptance tests**

In `PlayerPrefabAcceptanceTests.cs`, replace the two tests `Player_reads_input_through_the_project_action_asset` and `Player_uses_polled_notifications_not_messages` with:

```csharp
        [Test]
        public void Player_reads_input_through_the_project_action_asset()
        {
            var source = _prefab.GetComponent<InputSystemCommandSource>();
            Assert.That(source, Is.Not.Null, "The player has no InputSystemCommandSource.");

            var controls = new SerializedObject(source).FindProperty("_controls").objectReferenceValue
                as InputActionAsset;
            Assert.That(controls, Is.Not.Null, "The command source has no controls asset assigned.");
            Assert.That(controls.name, Is.EqualTo("BattleBombControls"),
                "The player must use the project's own action asset, not the URP template's.");
        }

        [Test]
        public void Player_has_no_PlayerInput_pairing_devices_behind_the_seats()
        {
            Assert.That(_prefab.GetComponent<PlayerInput>(), Is.Null,
                "PlayerInput pairs devices by control scheme, which pinned Player 1 to the keyboard " +
                "and Player 2 to one pad. Seats own devices now (D57); a PlayerInput here fights them.");
        }
```

- [ ] **Step 2: Replace the scene's device-pairing test**

In `PlayersInSceneAcceptanceTests.cs`, add `using BattleBomb.Gameplay.Session;` to the usings, and replace the test `The_two_players_are_paired_to_different_devices` with:

```csharp
        [Test]
        public void The_players_sit_in_seats_zero_and_one_in_the_binders_order()
        {
            SessionBinder binder = AcceptanceFixture.FindAll<SessionBinder>(_scene).Single();
            SerializedProperty players = new SerializedObject(binder).FindProperty("_players");
            Assert.That(players.arraySize, Is.EqualTo(2));

            for (int i = 0; i < players.arraySize; i++)
            {
                var actor = (Component)players.GetArrayElementAtIndex(i).objectReferenceValue;
                var source = actor.GetComponent<InputSystemCommandSource>();
                Assert.That(source.PlayerId.Value, Is.EqualTo(i),
                    $"The binder's slot {i} must be seat {i}. The id used to come from " +
                    "PlayerInput.playerIndex, which Unity allocates across every PlayerInput alive — " +
                    "a solo player could come out labelled P2 (HANDOFF-M7).");
            }
        }
```

- [ ] **Step 3: Add the front door's seat test**

```csharp
using System.Linq;
using BattleBomb.Gameplay.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BattleBomb.Tests.EditMode.Acceptance
{
    /// <summary>The front door's two command sources: one per seat, both reading the project's
    /// controls, neither carrying a PlayerInput (D57).</summary>
    public sealed class FrontendSeatsAcceptanceTests
    {
        private const string FrontendScenePath = "Assets/_BattleBomb/Scenes/Frontend.unity";

        private Scene _scene;
        private bool _openedByTest;

        [OneTimeSetUp]
        public void OpenScene() =>
            _scene = AcceptanceFixture.OpenForInspection(FrontendScenePath, out _openedByTest);

        [OneTimeTearDown]
        public void CloseScene() => AcceptanceFixture.CloseAfterInspection(_scene, _openedByTest);

        [Test]
        public void The_front_door_has_one_source_per_seat()
        {
            InputSystemCommandSource[] sources = AcceptanceFixture.FindAll<InputSystemCommandSource>(_scene).ToArray();

            Assert.That(sources.Select(s => s.PlayerId.Value).OrderBy(v => v).ToArray(), Is.EqualTo(new[] { 0, 1 }));
            foreach (InputSystemCommandSource source in sources)
            {
                Assert.That(source.GetComponent<PlayerInput>(), Is.Null, $"'{source.name}' still has a PlayerInput.");
                Assert.That(new SerializedObject(source).FindProperty("_controls").objectReferenceValue,
                    Is.Not.Null, $"'{source.name}' has no controls asset.");
            }
        }
    }
}
```

- [ ] **Step 4: Write `IInputDeviceReport`**

```csharp
using BattleBomb.Core.Players;

namespace BattleBomb.Gameplay.Players
{
    /// <summary>
    /// What a device-backed command source can say about the device behind it — for button
    /// prompts, and for the front door handing out seats (D57). A source that is not a device (a
    /// test script, a replay, later a remote peer) simply does not implement it.
    /// </summary>
    public interface IInputDeviceReport
    {
        InputFamily Family { get; }

        /// <summary>The Input System id of the device that last did anything, or
        /// <see cref="SeatAssignment.NoDevice"/>.</summary>
        int LastDeviceId { get; }
    }
}
```

- [ ] **Step 5: Rewrite `InputSystemCommandSource`**

Replace the whole file:

```csharp
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleBomb.Gameplay.Players
{
    /// <summary>
    /// One couch seat's input, turned into <see cref="PlayerCommand"/>s. With the
    /// <see cref="SeatInput"/> it owns, this is the only code in the project that touches an input
    /// device — everything downstream reads commands (§4, rule 3).
    /// </summary>
    /// <remarks>
    /// The seat is authored, and it is the player's id. It used to be <c>PlayerInput.playerIndex</c>,
    /// which Unity allocates across every PlayerInput alive — including the front door's, which
    /// claimed indices first — so a solo player could come out labelled "P2" (HANDOFF-M7). Which
    /// devices the seat owns comes from the session's <see cref="SeatAssignment"/> (D57).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class InputSystemCommandSource : MonoBehaviour, IPlayerCommandSource, IInputDeviceReport
    {
        [Tooltip("Driver this player registers with. Leave empty to find the driver in the scene, " +
                 "or the command sampler when there is no simulation (the front door).")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("The project's controls. Each seat reads its own copy.")]
        [SerializeField] private InputActionAsset _controls;

        [Tooltip("0 for Player 1, 1 for Player 2. This is the player's id, so it is what every " +
                 "P1/P2 label reads.")]
        [SerializeField] private int _seat;

        private readonly SeatAssignment _standIn = new SeatAssignment();
        private IPlayerRegistryHost _host;
        private SeatInput _input;
        private GameSession _session;
        private bool _lookedForSession;

        public PlayerId PlayerId => new PlayerId(_seat);

        public InputFamily Family => _input != null ? _input.Family : InputFamily.Keyboard;

        public int LastDeviceId => _input != null ? _input.LastDeviceId : SeatAssignment.NoDevice;

        public PlayerCommand Sample(int frame)
        {
            if (_input == null)
            {
                return PlayerCommand.Idle(frame);
            }

            _input.Own(Seats());
            return _input.Sample(frame);
        }

        private void OnEnable()
        {
            if (_controls == null)
            {
                Debug.LogError($"{name}: no controls asset — this player will contribute nothing.", this);
                return;
            }

            // Built here rather than in Awake: a mid-play recompile re-runs OnEnable but not Awake.
            _input = new SeatInput(_controls, _seat);
            _lookedForSession = false;

            _host = _driver != null ? _driver : FindHost();
            if (_host == null)
            {
                Debug.LogError($"{name}: nothing to register with — commands will never be sampled.", this);
                return;
            }

            _host.Players.Register(this);
        }

        private void OnDisable()
        {
            // Unregistered through the host it registered with, not the one in the scene now:
            // a scene change can swap the host out from under a source that outlives it.
            _host?.Players.Unregister(PlayerId);
            _host = null;
            _input?.Dispose();
            _input = null;
        }

        /// <summary>
        /// The session's seats, looked for on the first sample rather than in OnEnable: the front
        /// door creates the session in its own OnEnable, which may run after this one. With no
        /// session at all — the Gameplay scene opened on its own — Player 2 stands in on the first
        /// controller, which is how that scene has always behaved.
        /// </summary>
        private SeatAssignment Seats()
        {
            if (!_lookedForSession)
            {
                _session = GameSession.Find();
                _lookedForSession = true;
            }

            if (_session != null)
            {
                return _session.Seats;
            }

            _standIn.StandIn(FirstGamepad());
            return _standIn;
        }

        private static int FirstGamepad()
        {
            int first = SeatAssignment.NoDevice;
            foreach (Gamepad pad in Gamepad.all)
            {
                if (first == SeatAssignment.NoDevice || pad.deviceId < first)
                {
                    first = pad.deviceId;
                }
            }

            return first;
        }

        private static IPlayerRegistryHost FindHost()
        {
            SimulationDriver driver = FindAnyObjectByType<SimulationDriver>();
            if (driver != null)
            {
                return driver;
            }

            return FindAnyObjectByType<CommandSampler>();
        }
    }
}
```

- [ ] **Step 6: Give the registry the device report**

In `PlayerRegistry.cs`, add inside the class (after `IsRegistered`):

```csharp
        /// <summary>Which button pictures this player's prompts show. Keyboard for a source that
        /// is not a device — a test script, a replay, later a remote peer.</summary>
        public InputFamily FamilyOf(PlayerId playerId) =>
            _sources.TryGetValue(playerId.Value, out IPlayerCommandSource source)
                && source is IInputDeviceReport report
                ? report.Family
                : InputFamily.Keyboard;

        /// <summary>The device this player last pressed, for the front door's seating (D57).</summary>
        public int LastDeviceOf(PlayerId playerId) =>
            _sources.TryGetValue(playerId.Value, out IPlayerCommandSource source)
                && source is IInputDeviceReport report
                ? report.LastDeviceId
                : SeatAssignment.NoDevice;
```

- [ ] **Step 7: Carry the seats on the session**

In `GameSession.cs`, add `using BattleBomb.Core.Players;` and, after the `Characters` property:

```csharp
        /// <summary>Which devices each couch seat owns (D57). Here because the front door hands
        /// the seats out and the machine plays on them. Like <see cref="Characters"/>, it does not
        /// survive a mid-play domain reload.</summary>
        public SeatAssignment Seats { get; } = new SeatAssignment();
```

- [ ] **Step 8: Fix the two stale comments**

In `CharacterActor.cs` around line 100, the doc comment on `PlayerId` says the id is read live because PlayerInput assigns its index after sibling OnEnable. Replace that sentence with: `Read from the command source, whose seat is the player's id (D57); the serialized index is only the fallback for an actor with no source.` In `CharacterRegistry.cs` line 17, replace "because a PlayerInput assigns its player index after registration" with "because a source can register before or after its partner, and the order players are listed in must not depend on which". Keep the sorting code exactly as it is.

- [ ] **Step 9: Recompile**

`recompile`, `recompile_status`, `console` with `level: error`. Expected: clean. The acceptance tests from Steps 1–3 now fail (the assets still carry PlayerInput and no `_controls`) — that is correct until Step 10.

- [ ] **Step 10: Migrate the prefab and both scenes**

Editor must not be in play mode. Run each snippet with the MCP `eval` tool (fully qualified names — eval bodies take no `using` directives). Read each return value; it is the evidence.

Prefab:

```csharp
var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/_BattleBomb/Input/BattleBombControls.inputactions");
var path = "Assets/_BattleBomb/Prefabs/Player.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
var input = root.GetComponent<UnityEngine.InputSystem.PlayerInput>();
if (input != null) UnityEngine.Object.DestroyImmediate(input, true);
var source = root.GetComponent<BattleBomb.Gameplay.Players.InputSystemCommandSource>();
var so = new UnityEditor.SerializedObject(source);
so.FindProperty("_controls").objectReferenceValue = asset;
so.FindProperty("_seat").intValue = 0;
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
UnityEditor.PrefabUtility.UnloadPrefabContents(root);
return "prefab: PlayerInput removed=" + (input != null) + ", controls=" + (asset != null);
```

Gameplay scene — seats follow the `SessionBinder`'s slot order, which is the front door's slot order:

```csharp
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_BattleBomb/Scenes/Gameplay.unity");
var binder = UnityEngine.Object.FindAnyObjectByType<BattleBomb.Gameplay.Session.SessionBinder>(UnityEngine.FindObjectsInactive.Include);
var players = new UnityEditor.SerializedObject(binder).FindProperty("_players");
var report = "";
var roots = new System.Collections.Generic.List<UnityEngine.GameObject>();
for (int i = 0; i < players.arraySize; i++)
{
    var actor = (UnityEngine.Component)players.GetArrayElementAtIndex(i).objectReferenceValue;
    var src = actor.GetComponent<BattleBomb.Gameplay.Players.InputSystemCommandSource>();
    var s = new UnityEditor.SerializedObject(src);
    s.FindProperty("_seat").intValue = i;
    s.ApplyModifiedPropertiesWithoutUndo();
    roots.Add(actor.gameObject);
    report += actor.name + "->seat " + i + "; ";
}
UnityEditor.PrefabUtility.RemoveUnusedOverrides(roots.ToArray(), UnityEditor.InteractionMode.AutomatedAction);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return report;
```

If `RemoveUnusedOverrides` does not compile in eval, delete that one line and rerun — orphaned overrides for the removed component are harmless.

Frontend scene — its two sources are plain objects; the old default scheme says which seat each was (Keyboard was Player 1):

```csharp
var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/_BattleBomb/Input/BattleBombControls.inputactions");
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_BattleBomb/Scenes/Frontend.unity");
var report = "";
foreach (var src in UnityEngine.Object.FindObjectsByType<BattleBomb.Gameplay.Players.InputSystemCommandSource>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    var input = src.GetComponent<UnityEngine.InputSystem.PlayerInput>();
    int seat = input != null && input.defaultControlScheme == "Gamepad" ? 1 : 0;
    if (input != null) UnityEngine.Object.DestroyImmediate(input);
    var so = new UnityEditor.SerializedObject(src);
    so.FindProperty("_controls").objectReferenceValue = asset;
    so.FindProperty("_seat").intValue = seat;
    so.ApplyModifiedPropertiesWithoutUndo();
    report += src.name + "->seat " + seat + "; ";
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return report;
```

Expected: two different seats reported (one `->seat 0`, one `->seat 1`). If both report the same seat, set the second by hand in the same way and say so in the task report.

Finish by reopening `Frontend.unity` (Michael's rule: Play starts at the title).

- [ ] **Step 11: Verify on disk, not by return value**

`grep -c 'PlayerInput' Assets/_BattleBomb/Prefabs/Player.prefab Assets/_BattleBomb/Scenes/Frontend.unity` — expected `0` for both. `grep -n '_seat' Assets/_BattleBomb/Scenes/Gameplay.unity` — expected a `_seat` override of `1` on the second player instance.

- [ ] **Step 12: Run both suites**

EditMode: expected all green, including the three acceptance fixtures touched here. PlayMode (`async_tests: true`, poll `test_status`): expected **15 passed** — the smoke suites replace the device sources, so they must not notice. Delete `Assets/InitTestScene*`.

- [ ] **Step 13: Live check (slow, settled state — drive it yourself)**

Enter play mode from `Gameplay.unity`. With `eval`, read `FindObjectsByType<InputSystemCommandSource>` and report each `name`, `PlayerId.Value`, and `Family`. Expected: seats 0 and 1, no errors in `console`. `editor_stop`.

- [ ] **Step 14: Commit** — subject `G5: seats replace PlayerInput; the seat is the player's id`. Body names the P2 fix.

---

### Task 6: The front door hands out the seats

Title → character select is where seats are decided. The front door reports its state to `SeatAssignment.Follow` every frame, and reads menus through `MenuPress`. Proven with virtual controllers against the real Frontend scene.

**Files:**
- Modify: `Assets/_BattleBomb/UI/Frontend/FrontendFlow.cs` (`Update`)
- Modify: `Assets/_BattleBomb/Tests/PlayMode/BattleBomb.Tests.PlayMode.asmdef`
- Test: `Assets/_BattleBomb/Tests/PlayMode/SeatJoinSmokeTests.cs`

- [ ] **Step 1: Reference the test framework from PlayMode**

Add `"Unity.InputSystem.TestFramework"` after `"Unity.InputSystem"` in `BattleBomb.Tests.PlayMode.asmdef`'s `references`.

- [ ] **Step 2: Write the failing PlayMode test**

```csharp
using System.Collections;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.UI.Frontend;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// The front door seating a couch from real (virtual) devices (D57). The smoke suites swap the
    /// device sources out by design, so this is the one place the device path runs end to end.
    /// </summary>
    public sealed class SeatJoinSmokeTests : InputTestFixture
    {
        [UnitySetUp]
        public IEnumerator ClearSessions()
        {
            // A session left by another test would send the front door straight to chapter select.
            foreach (GameSession session in Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None))
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator DropTheSession()
        {
            // The next suite loads Gameplay, and a session with nobody seated deactivates every player.
            foreach (GameSession session in Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None))
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Player_two_joins_on_a_second_controller_and_leaves_with_it()
        {
            InputSystem.AddDevice<Keyboard>();
            var first = InputSystem.AddDevice<Gamepad>();
            var second = InputSystem.AddDevice<Gamepad>();

            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;

            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            Assert.That(flow, Is.Not.Null, "The Frontend scene has no FrontendFlow.");
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Title));
            GameSession session = GameSession.Find();
            Assert.That(session, Is.Not.Null, "The front door made no session.");

            yield return Tap(first.buttonSouth);
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Characters),
                "A on a controller did not start the game from the title.");
            Assert.That(session.Seats.FirstSeatHome, Is.EqualTo(first.deviceId));

            yield return Tap(second.buttonSouth);
            Assert.That(flow.State.IsJoined(1), Is.True, "A on the second controller did not join Player 2.");
            Assert.That(flow.State.IsReady(0), Is.False, "Player 2's join press readied Player 1.");
            Assert.That(session.Seats.SecondSeatDevice, Is.EqualTo(second.deviceId));

            yield return Tap(second.buttonEast);
            Assert.That(flow.State.IsJoined(1), Is.False, "B on Player 2's controller did not leave.");
            Assert.That(session.Seats.SecondSeatDevice, Is.EqualTo(SeatAssignment.NoDevice));
        }

        private IEnumerator Tap(ButtonControl button)
        {
            Press(button);
            yield return null;
            yield return null;
            Release(button);
            yield return null;
            yield return null;
        }
    }
}
```

- [ ] **Step 3: Run it to see it fail**

`recompile`; run PlayMode with `filter_type: testName`, `filter: SeatJoinSmokeTests`, `async_tests: true`. Expected: FAIL at `FirstSeatHome` (nothing calls `Follow` yet) — or earlier if the title does not respond to A, which is also this task's to fix.

- [ ] **Step 4: Rewrite `FrontendFlow.Update`**

Replace the method with:

```csharp
        private void Update()
        {
            if (_input == null || _state == null || _launched)
            {
                return;
            }

            for (int slot = 0; slot < FrontendState.Slots; slot++)
            {
                PlayerCommand command = _input.CommandFor(slot);
                MenuPress press = MenuPress.From(command);
                Steer(slot, command);

                // Start confirms for Player 1 as it always has; Escape backs out (D57).
                if (press.Confirm || (slot == 0 && press.Pause))
                {
                    Confirm(slot);
                }
                else if (press.Back)
                {
                    _state.Back(slot);
                }
            }

            _session.Seats.Follow(
                _state.Screen,
                _state.IsJoined(1),
                _input.Players.LastDeviceOf(new PlayerId(0)),
                _input.Players.LastDeviceOf(new PlayerId(1)));

            if (_state.Screen == FrontendScreen.Launching)
            {
                LaunchNow();
                return;
            }

            Repaint();
        }
```

`FrontendFlow` already imports `BattleBomb.Core.Players`, `BattleBomb.Core.Chapters`, and `BattleBomb.Gameplay.Players`.

- [ ] **Step 5: Run the test** — expected PASS. Then the full PlayMode suite: expected **16 passed**. Delete `Assets/InitTestScene*`.

- [ ] **Step 6: Commit** — subject `G6: the front door hands out the seats`.

---

### Task 7: The chest screen on the new map

A confirms, B backs out one level and closes at the top, Start leaves from anywhere, X sells (or combines the pile mid-pick), Y locks, LB/RB switch halves. Magic, Light, and Heavy stop meaning anything to the screen.

**Files:**
- Modify: `Assets/_BattleBomb/UI/Chest/ChestNavigation.cs` (add `CycleTab`)
- Test: `Assets/_BattleBomb/Tests/EditMode/ChestNavigationTests.cs`
- Modify: `Assets/_BattleBomb/UI/Chest/ChestScreen.cs` (class doc, `Tick`, `RunMenuAction`, `RunWornAction`, three new helpers, two new shortcut methods)
- Modify: `Assets/_BattleBomb/Tests/PlayMode/LootLoopSmokeTests.cs`

- [ ] **Step 1: Write the failing navigation tests**

Append to `ChestNavigationTests`:

```csharp
        [Test]
        public void The_shoulders_flip_the_counter_at_a_shop()
        {
            var nav = new ChestNavigation();
            nav.SetMode(ShopMode.Buy, Shop());

            Assert.That(nav.CycleTab(1, Shop()), Is.EqualTo(ChestOutcome.ModeSwitched));
            Assert.That(nav.Mode, Is.EqualTo(ShopMode.Sell));
            Assert.That(nav.CycleTab(-1, Shop()), Is.EqualTo(ChestOutcome.ModeSwitched));
            Assert.That(nav.Mode, Is.EqualTo(ShopMode.Buy));
        }

        [Test]
        public void The_shoulders_switch_the_tab_in_couch_co_op_and_land_in_its_body()
        {
            var nav = new ChestNavigation();

            Assert.That(nav.CycleTab(1, Layout()), Is.EqualTo(ChestOutcome.TabSwitched));
            Assert.That(nav.Tab, Is.EqualTo(ChestTab.Hero));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Loadout),
                "A shoulder press is a jump to the other half, not a trip to the tab strip.");

            nav.CycleTab(-1, Layout());
            Assert.That(nav.Tab, Is.EqualTo(ChestTab.ItemSack));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid));
        }

        [Test]
        public void Solo_the_shoulders_carry_the_cursor_between_the_sack_and_the_gear()
        {
            var nav = new ChestNavigation();

            Assert.That(nav.CycleTab(1, Solo()), Is.EqualTo(ChestOutcome.None));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Loadout));
            Assert.That(nav.Tab, Is.EqualTo(ChestTab.ItemSack), "Solo has no tabs to change.");

            nav.CycleTab(1, Solo());
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid));
        }

        [Test]
        public void The_shoulders_wait_while_a_pick_or_a_menu_is_open()
        {
            var nav = new ChestNavigation();
            nav.BeginCombine(2);
            Assert.That(nav.CycleTab(1, Solo()), Is.EqualTo(ChestOutcome.None));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Grid), "A combine pick is modal.");

            nav.CancelCombine();
            nav.Confirm(Solo());
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Menu));
            Assert.That(nav.CycleTab(1, Solo()), Is.EqualTo(ChestOutcome.None));
            Assert.That(nav.Focus, Is.EqualTo(ChestFocus.Menu), "An open item menu is answered first.");
        }
```

- [ ] **Step 2: Run to see it fail** — `recompile`; expected compile error, no `CycleTab`.

- [ ] **Step 3: Add `CycleTab` to `ChestNavigation`**

After `SetMode`, add:

```csharp
        /// <summary>
        /// LB / RB (D57): the other half of whatever this screen has two of. At a shopkeeper
        /// that is the counter's side; in couch co-op, the sack-or-hero tab, landing in its body;
        /// solo at a chest, where both halves are already on screen, the cursor crosses to the
        /// other one. A combine pick and an open item menu are answered before anything moves.
        /// </summary>
        public ChestOutcome CycleTab(int direction, in ChestLayout layout)
        {
            if (direction == 0 || PendingCombine >= 0
                || Focus == ChestFocus.Menu || Focus == ChestFocus.Upgrade)
            {
                return ChestOutcome.None;
            }

            if (layout.IsShop)
            {
                return SwitchMode(layout);
            }

            if (layout.HeroBeside)
            {
                Focus = Focus == ChestFocus.Loadout || Focus == ChestFocus.Stats
                    ? ChestFocus.Grid
                    : ChestFocus.Loadout;
                return ChestOutcome.None;
            }

            SwitchTab();
            Focus = Tab == ChestTab.Hero ? ChestFocus.Loadout : ChestFocus.Grid;
            return ChestOutcome.TabSwitched;
        }
```

- [ ] **Step 4: Run the navigation fixture** — expected all `ChestNavigationTests` pass (existing + 4 new).

- [ ] **Step 5: Rewrite the screen's input**

In `ChestScreen.cs`:

(a) Class doc paragraph beginning "Navigated entirely by" becomes:

```csharp
    /// Navigated entirely by <see cref="PlayerCommand"/> through <see cref="MenuPress"/> (D57):
    /// the stick moves, A confirms, B backs out a level and finally closes, Start leaves from
    /// anywhere, X and Y are one-press shortcuts, and the shoulders switch halves. That keeps the
    /// menu inside rule 3's one-input-path promise and makes two players on two screen halves
    /// need no extra machinery.
```

(b) Replace `Tick` with:

```csharp
        /// <summary>One navigation step, from this player's live command (D42).</summary>
        internal void Tick(in PlayerCommand command, float deltaTime)
        {
            if (_bag == null)
            {
                return;
            }

            // The press that opened this screen must not also act inside it. It matters more now
            // than in M6: the opening press is X, which is Light to the fight and Option — sell —
            // to this screen.
            if (_swallowUntilRelease)
            {
                if (MenuPress.AnyHeld(command))
                {
                    return;
                }

                _swallowUntilRelease = false;
            }

            StepCursor(command, deltaTime);
            MenuPress press = MenuPress.From(command);

            if (press.Pause)
            {
                // Start leaves from anywhere. Escape now backs out a level at a time, so the pad's
                // system button is what keeps M6's promise that getting out is never a puzzle.
                Host?.RequestClose(_playerId);
                return;
            }

            if (press.Tab != 0)
            {
                CollectVisible();
                _nav.CycleTab(press.Tab, Layout());
                Refresh();
                return;
            }

            if (press.Option)
            {
                RunOption();
                Refresh();
                return;
            }

            if (press.Lock)
            {
                RunLock();
                Refresh();
                return;
            }

            if (press.Confirm)
            {
                Confirm();
            }
            else if (press.Back)
            {
                Cancel();
            }

            if (Time.unscaledTime > _flashUntil && _flash.Length > 0)
            {
                _flash = string.Empty;
                Refresh();
            }
        }
```

(c) In `Cancel`, change the comment `// Nothing left to back out of: Heavy closes the chest.` to `// Nothing left to back out of: B closes the chest.`

(d) Add the helpers and the two shortcuts after `RunMenuAction`:

```csharp
        private void SellAt(int bagIndex)
        {
            int coins = _bag.RequestSell(bagIndex);
            Flash(coins > 0 ? $"Sold for {coins}." : "Locked — release it first.");
        }

        private void ToggleLockAt(int bagIndex)
        {
            ItemInstance item = _bag.Inventory.Items[bagIndex].Item;
            _bag.RequestLock(bagIndex, !item.Locked);
            Flash(item.Locked ? "Released." : "Locked.");
        }

        private void ToggleWornLock()
        {
            ItemInstance worn = WornItem;
            if (worn.IsEmpty)
            {
                return;
            }

            HeroPanel.Slot slot = WornSlot;
            _bag.RequestLockWorn(slot.Which, slot.EquipmentIndex, !worn.Locked);
            Flash(worn.Locked ? "Released." : "Locked.");
        }

        /// <summary>
        /// X (D57): the one-press verb for what the cursor is on. Mid-combine it grinds the whole
        /// pile; on a sack item it sells — instantly, by Michael's choice (2026-09-24), with the
        /// lock as the only safety. At the rack A already buys in one press, so X there would be a
        /// second button for the same job, which the map rules out.
        /// </summary>
        private void RunOption()
        {
            if (_nav.PendingCombine >= 0)
            {
                RunCombineAll();
                return;
            }

            if (_nav.Focus != ChestFocus.Grid)
            {
                return;
            }

            CollectVisible();
            if (_visible.Count == 0)
            {
                return;
            }

            SellAt(_visible[_nav.Cursor]);
            CollectVisible();
            _nav.ClampCursor(_visible.Count);
        }

        /// <summary>Y (D57): lock or release the item under the cursor, in the sack or worn.</summary>
        private void RunLock()
        {
            if (_nav.PendingCombine >= 0)
            {
                return;
            }

            if (_nav.Focus == ChestFocus.Loadout)
            {
                ToggleWornLock();
                return;
            }

            if (_nav.Focus != ChestFocus.Grid)
            {
                return;
            }

            CollectVisible();
            if (_visible.Count > 0)
            {
                ToggleLockAt(_visible[_nav.Cursor]);
            }
        }
```

(e) In `RunMenuAction`, the `Sell` and `Lock` cases become:

```csharp
                case ItemAction.Sell:
                    SellAt(bagIndex);
                    break;

                case ItemAction.Lock:
                    ToggleLockAt(bagIndex);
                    break;
```

(f) In `RunWornAction`, the `default:` case becomes:

```csharp
                default:
                    ToggleWornLock();
                    break;
```

(g) Add `using BattleBomb.Core.Players;` if it is not already imported (it is — `PlayerCommand` is used).

- [ ] **Step 6: Update the loot smoke suite to the new map**

In `LootLoopSmokeTests.cs`:

(a) Add, beside `KnifeDefinitionId`:

```csharp
        /// <summary>What a real X or J press carries: the fight's Light and the menu's Option at
        /// once (D57). Opening the chest with it proves the opening press cannot sell anything.</summary>
        private const CommandButtons OpenPress = CommandButtons.Light | CommandButtons.Option;
```

(b) Every `yield return Press(CommandButtons.Light);` that opens the chest (the ones directly after `WalkTo(chest.Position, …)`) becomes `yield return Press(OpenPress);`. Grab presses beside drops stay `CommandButtons.Light`.

(c) `Every_way_out_of_the_chest_screen_works` takes `[Values("start", "back", "escape", "close button")] string route`, and its switch becomes:

```csharp
            switch (route)
            {
                case "start":
                    yield return Press(CommandButtons.Pause);
                    break;

                case "back":
                    yield return Press(CommandButtons.Back);
                    break;

                case "escape":
                    yield return Press(CommandButtons.Back | CommandButtons.Pause);
                    break;

                default:
```

(keep the existing `default:` body — the close-button route — unchanged).

(d) Add two tests:

```csharp
        [UnityTest]
        public IEnumerator The_press_that_opens_the_chest_sells_nothing()
        {
            yield return TakeAKnife();
            int slots = _bag.Inventory.SlotsUsed;
            int coins = _bag.Wallet.Balance;

            WorldInteractable chest = FindChest();
            yield return WalkTo(chest.Position, "the chest");
            yield return Press(OpenPress);
            yield return Until(() => _driver.TryGetOpenScreen(_player.PlayerId.Value, out _),
                "the chest screen never opened");
            yield return SimulationFrames(10);

            Assert.That(_bag.Inventory.SlotsUsed, Is.EqualTo(slots),
                "Opening the chest sold something: X is Light to the fight and Option to the screen.");
            Assert.That(_bag.Wallet.Balance, Is.EqualTo(coins));
        }

        [UnityTest]
        public IEnumerator X_sells_and_Y_locks_the_item_under_the_cursor()
        {
            yield return TakeAKnife();

            WorldInteractable chest = FindChest();
            yield return WalkTo(chest.Position, "the chest");
            yield return Press(OpenPress);
            yield return Until(() => _driver.TryGetOpenScreen(_player.PlayerId.Value, out _),
                "the chest screen never opened");

            int slots = _bag.Inventory.SlotsUsed;
            yield return Press(CommandButtons.Lock);
            Assert.That(_bag.Inventory.Items[0].Item.Locked, Is.True, "Y did not lock the item.");

            yield return Press(CommandButtons.Option);
            Assert.That(_bag.Inventory.SlotsUsed, Is.EqualTo(slots), "X sold a locked item.");

            yield return Press(CommandButtons.Lock);
            Assert.That(_bag.Inventory.Items[0].Item.Locked, Is.False, "Y did not release it.");

            int coins = _bag.Wallet.Balance;
            yield return Press(CommandButtons.Option);
            Assert.That(_bag.Inventory.SlotsUsed, Is.EqualTo(slots - 1), "X did not sell the item.");
            Assert.That(_bag.Wallet.Balance, Is.GreaterThan(coins), "The sale paid nothing.");
        }

        /// <summary>Drops the starter knife beside the player and grabs it, so the sack holds exactly
        /// one known item in cell 0.</summary>
        private IEnumerator TakeAKnife()
        {
            int before = _bag.Inventory.SlotsUsed;
            Vector3 where = _player.Position + new Vector3(1.5f, 0f, 0f);
            _driver.SpawnDebugDrop(where, _driver.RollDebugItem(KnifeDefinitionId, 2.2f));
            yield return null;
            yield return WalkTo(where, "the knife");
            yield return Press(CommandButtons.Light);
            yield return Until(() => _bag.Inventory.SlotsUsed > before, "the knife was never taken");
        }
```

These tests assume the sack starts empty in a fresh Gameplay scene, as the existing loop test does. If `_bag.Inventory.SlotsUsed` is not 1 after `TakeAKnife`, the cell-0 assumption is wrong: find the knife's bag index instead and assert on that, rather than weakening the checks.

- [ ] **Step 7: Run both suites**

EditMode: all green. PlayMode: expected **16 + 3 = 19** (the way-out test gains one route; two new tests) — report the exact count. Delete `Assets/InitTestScene*`.

- [ ] **Step 8: Commit** — subject `G7: the chest on the new map — A, B, Start, X sells, Y locks, shoulders switch halves`.

---

### Task 8: Settings and results on the new map; Escape never double-fires

Outside a menu Escape pauses; inside one it backs out. The chest closes on Escape from its top level — and the settings menu must not open on that same press.

**Files:**
- Modify: `Assets/_BattleBomb/UI/Chest/SettingsMenu.cs` (`Tick`, fields, `Close`)
- Modify: `Assets/_BattleBomb/UI/Frontend/ResultsScreen.cs` (`Tick`, fields, `Open`)
- Modify: `Assets/_BattleBomb/Tests/PlayMode/LootLoopSmokeTests.cs`

- [ ] **Step 1: Write the failing smoke tests**

Add to `LootLoopSmokeTests` (`using BattleBomb.UI.Chest;` if not present):

```csharp
        [UnityTest]
        public IEnumerator Escape_out_of_the_chest_does_not_open_the_settings()
        {
            WorldInteractable chest = FindChest();
            yield return WalkTo(chest.Position, "the chest");
            yield return Press(OpenPress);
            yield return Until(() => _driver.TryGetOpenScreen(_player.PlayerId.Value, out _),
                "the chest screen never opened");

            yield return Press(CommandButtons.Back | CommandButtons.Pause);
            yield return Until(() => !_driver.TryGetOpenScreen(_player.PlayerId.Value, out _),
                "Escape did not close the chest from its top level");
            yield return SimulationFrames(5);

            SettingsMenu settings = Object.FindAnyObjectByType<SettingsMenu>();
            Assert.That(settings, Is.Not.Null, "The gameplay scene has no SettingsMenu.");
            Assert.That(settings.IsOpen, Is.False,
                "The Escape that closed the chest also opened the settings.");
        }

        [UnityTest]
        public IEnumerator Escape_outside_a_menu_opens_and_closes_the_settings()
        {
            SettingsMenu settings = Object.FindAnyObjectByType<SettingsMenu>();
            Assert.That(settings, Is.Not.Null, "The gameplay scene has no SettingsMenu.");

            yield return Press(CommandButtons.Back | CommandButtons.Pause);
            Assert.That(settings.IsOpen, Is.True, "Escape in the world did not pause.");

            yield return Press(CommandButtons.Back | CommandButtons.Pause);
            Assert.That(settings.IsOpen, Is.False, "Escape did not back out of the settings.");
        }
```

- [ ] **Step 2: Run them** — PlayMode, `filter: LootLoopSmokeTests`, async. `Escape_outside_a_menu…` already passes: Escape carries Pause, which the old code opens and closes on. `Escape_out_of_the_chest…` passes or fails depending on which of the chest host and the settings menu ticks first that step — it is the guard for that race, and after Step 3 it must pass regardless of order.

- [ ] **Step 3: Rewrite `SettingsMenu.Tick`**

Add fields beside `_lastMove`:

```csharp
        private readonly HashSet<int> _busy = new HashSet<int>();
        private readonly HashSet<int> _busyBefore = new HashSet<int>();
```

Replace `Tick` with:

```csharp
        private void Tick()
        {
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;

            if (!_open)
            {
                // A player at a chest is busy, and so is one who was at it a step ago: Escape
                // backs out of the chest's top level, and the press that closed it must not
                // also open this (D57).
                _busy.Clear();
                for (int i = 0; i < actors.Count; i++)
                {
                    int id = actors[i].PlayerId.Value;
                    if (_driver.TryGetOpenScreen(id, out _))
                    {
                        _busy.Add(id);
                    }
                }

                for (int i = 0; i < actors.Count; i++)
                {
                    int id = actors[i].PlayerId.Value;
                    if (_busy.Contains(id) || _busyBefore.Contains(id))
                    {
                        continue;
                    }

                    // The raw flag, not MenuPress: outside a menu Escape carries Back too, and
                    // Back means nothing here. Escape is the keyboard's pause.
                    if (_driver.CommandFor(id).WasPressed(CommandButtons.Pause))
                    {
                        Open(id);
                        break;
                    }
                }

                _busyBefore.Clear();
                _busyBefore.UnionWith(_busy);
                return;
            }

            PlayerCommand command = _driver.CommandFor(_owner);
            MenuPress press = MenuPress.From(command);
            if (press.Back || press.Pause)
            {
                Close();
                return;
            }

            StepCursor(command);
            if (press.Confirm)
            {
                Toggle();
            }

            Repaint();
        }
```

In `Close()`, add `_busyBefore.Clear();` as its first line, so a stale entry from before the menu opened cannot block the next pause.

- [ ] **Step 4: Make the results screen read A, and wait for hands off**

In `ResultsScreen.cs`, add a field `private bool _swallowUntilRelease;`. In the method that opens the panel (the block that calls `Build(); _open = true; _panel.SetActive(true); HoldPause(true);`), add `_swallowUntilRelease = true;` beside `_open = true;`. Replace the input loop in `Tick` with:

```csharp
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;

            // A fight can end mid-mash, and A is Jump as well as Confirm: nobody leaves the
            // results until every hand has come off the buttons once.
            if (_swallowUntilRelease)
            {
                for (int i = 0; i < actors.Count; i++)
                {
                    if (MenuPress.AnyHeld(_driver.CommandFor(actors[i].PlayerId.Value)))
                    {
                        return;
                    }
                }

                _swallowUntilRelease = false;
            }

            for (int i = 0; i < actors.Count; i++)
            {
                if (MenuPress.From(_driver.CommandFor(actors[i].PlayerId.Value)).Confirm)
                {
                    Leave();
                    return;
                }
            }
```

- [ ] **Step 5: Run both suites** — EditMode green; PlayMode expected **21** passed. Delete `Assets/InitTestScene*`.

- [ ] **Step 6: Commit** — subject `G8: settings and results on the new map; Escape never double-fires`.

---

### Task 9: The button pictures

A pure table from (device family, prompt) to (label, shape, colour), pinned to the input asset so a binding change cannot leave the old button on screen.

**Files:**
- Create: `Assets/_BattleBomb/Core/Players/PromptGlyphs.cs`
- Test: `Assets/_BattleBomb/Tests/EditMode/PromptGlyphsTests.cs`
- Test: `Assets/_BattleBomb/Tests/EditMode/Acceptance/PromptGlyphsAcceptanceTests.cs`

- [ ] **Step 1: Write the failing tests**

`PromptGlyphsTests.cs`:

```csharp
using System;
using BattleBomb.Core.Players;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>The picture for every prompt (D57; UI Pass 01's hint row).</summary>
    public sealed class PromptGlyphsTests
    {
        [Test]
        public void Every_prompt_has_a_picture_on_both_families()
        {
            foreach (PromptKey key in Enum.GetValues(typeof(PromptKey)))
            {
                Assert.That(PromptGlyphs.For(InputFamily.Keyboard, key).Label, Is.Not.Empty,
                    $"No key cap for {key}.");
                Assert.That(PromptGlyphs.For(InputFamily.Gamepad, key).Label, Is.Not.Null,
                    $"No pad picture for {key}.");
            }
        }

        [Test]
        public void Face_buttons_are_round_and_coloured_and_system_buttons_are_square()
        {
            Assert.That(PromptGlyphs.For(InputFamily.Gamepad, PromptKey.Confirm).Shape, Is.EqualTo(GlyphShape.Round));
            Assert.That(PromptGlyphs.For(InputFamily.Gamepad, PromptKey.Confirm).Tone, Is.EqualTo(GlyphTone.Green));
            Assert.That(PromptGlyphs.For(InputFamily.Gamepad, PromptKey.Back).Tone, Is.EqualTo(GlyphTone.Red));
            Assert.That(PromptGlyphs.For(InputFamily.Gamepad, PromptKey.Option).Tone, Is.EqualTo(GlyphTone.Blue));
            Assert.That(PromptGlyphs.For(InputFamily.Gamepad, PromptKey.Lock).Tone, Is.EqualTo(GlyphTone.Yellow));
            Assert.That(PromptGlyphs.For(InputFamily.Gamepad, PromptKey.Pause).Shape, Is.EqualTo(GlyphShape.Square));
            Assert.That(PromptGlyphs.For(InputFamily.Gamepad, PromptKey.Tabs).Shape, Is.EqualTo(GlyphShape.Square));
        }

        [Test]
        public void Every_keyboard_key_is_a_key_cap()
        {
            foreach (PromptKey key in Enum.GetValues(typeof(PromptKey)))
            {
                PromptGlyph glyph = PromptGlyphs.For(InputFamily.Keyboard, key);
                Assert.That(glyph.Shape, Is.EqualTo(GlyphShape.Square), key.ToString());
                Assert.That(glyph.Tone, Is.EqualTo(GlyphTone.Key), key.ToString());
            }
        }

        [Test]
        public void A_button_the_fight_and_the_menu_share_shows_the_same_picture()
        {
            Assert.That(Label(PromptKey.Confirm), Is.EqualTo(Label(PromptKey.Jump)), "A");
            Assert.That(Label(PromptKey.Back), Is.EqualTo(Label(PromptKey.Magic)), "B");
            Assert.That(Label(PromptKey.Option), Is.EqualTo(Label(PromptKey.Light)), "X");
            Assert.That(Label(PromptKey.Lock), Is.EqualTo(Label(PromptKey.Heavy)), "Y");
        }

        private static string Label(PromptKey key) => PromptGlyphs.For(InputFamily.Gamepad, key).Label;
    }
}
```

`Acceptance/PromptGlyphsAcceptanceTests.cs`:

```csharp
using System.Linq;
using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace BattleBomb.Tests.EditMode.Acceptance
{
    /// <summary>
    /// The prompt table and the input asset must agree: a prompt that names a button the asset
    /// does not bind to that action is a lie on screen. When rebinding arrives (M13) this is the
    /// test that says the table has to become the binding's own display name.
    /// </summary>
    public sealed class PromptGlyphsAcceptanceTests
    {
        private static readonly (PromptKey Key, InputFamily Family, string Map, string Action, string Path, string Label)[] Expected =
        {
            (PromptKey.Confirm, InputFamily.Gamepad, "Menu", "Confirm", "<Gamepad>/buttonSouth", "A"),
            (PromptKey.Back, InputFamily.Gamepad, "Menu", "Back", "<Gamepad>/buttonEast", "B"),
            (PromptKey.Option, InputFamily.Gamepad, "Menu", "Option", "<Gamepad>/buttonWest", "X"),
            (PromptKey.Lock, InputFamily.Gamepad, "Menu", "Lock", "<Gamepad>/buttonNorth", "Y"),
            (PromptKey.Tabs, InputFamily.Gamepad, "Menu", "TabPrevious", "<Gamepad>/leftShoulder", "LB/RB"),
            (PromptKey.Tabs, InputFamily.Gamepad, "Menu", "TabNext", "<Gamepad>/rightShoulder", "LB/RB"),
            (PromptKey.Pause, InputFamily.Gamepad, "Gameplay", "Pause", "<Gamepad>/start", "≡"),
            (PromptKey.Light, InputFamily.Gamepad, "Gameplay", "Light", "<Gamepad>/buttonWest", "X"),
            (PromptKey.Heavy, InputFamily.Gamepad, "Gameplay", "Heavy", "<Gamepad>/buttonNorth", "Y"),
            (PromptKey.Magic, InputFamily.Gamepad, "Gameplay", "Magic", "<Gamepad>/buttonEast", "B"),
            (PromptKey.QuickUse, InputFamily.Gamepad, "Gameplay", "Equipment", "<Gamepad>/rightShoulder", "RB"),
            (PromptKey.Jump, InputFamily.Gamepad, "Gameplay", "Jump", "<Gamepad>/buttonSouth", "A"),
            (PromptKey.Confirm, InputFamily.Keyboard, "Menu", "Confirm", "<Keyboard>/enter", "Enter"),
            (PromptKey.Back, InputFamily.Keyboard, "Menu", "Back", "<Keyboard>/escape", "Esc"),
            (PromptKey.Option, InputFamily.Keyboard, "Menu", "Option", "<Keyboard>/j", "J"),
            (PromptKey.Lock, InputFamily.Keyboard, "Menu", "Lock", "<Keyboard>/k", "K"),
            (PromptKey.Tabs, InputFamily.Keyboard, "Menu", "TabPrevious", "<Keyboard>/q", "Q/E"),
            (PromptKey.Tabs, InputFamily.Keyboard, "Menu", "TabNext", "<Keyboard>/e", "Q/E"),
            (PromptKey.Pause, InputFamily.Keyboard, "Gameplay", "Pause", "<Keyboard>/escape", "Esc"),
            (PromptKey.Light, InputFamily.Keyboard, "Gameplay", "Light", "<Keyboard>/j", "J"),
            (PromptKey.Heavy, InputFamily.Keyboard, "Gameplay", "Heavy", "<Keyboard>/k", "K"),
            (PromptKey.Magic, InputFamily.Keyboard, "Gameplay", "Magic", "<Keyboard>/l", "L"),
            (PromptKey.QuickUse, InputFamily.Keyboard, "Gameplay", "Equipment", "<Keyboard>/i", "I"),
            (PromptKey.Jump, InputFamily.Keyboard, "Gameplay", "Jump", "<Keyboard>/space", "Space"),
        };

        [Test]
        public void Every_prompt_names_a_button_the_asset_really_binds()
        {
            InputActionAsset asset =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(AcceptanceFixture.ControlsAssetPath);
            Assert.That(asset, Is.Not.Null);

            Assert.Multiple(() =>
            {
                foreach (var row in Expected)
                {
                    InputActionMap map = asset.FindActionMap(row.Map);
                    Assert.That(map, Is.Not.Null, row.Map);
                    Assert.That(map.bindings.Any(b => b.action == row.Action && b.path == row.Path), Is.True,
                        $"The asset does not bind {row.Path} to {row.Map}/{row.Action}.");
                    Assert.That(PromptGlyphs.For(row.Family, row.Key).Label, Is.EqualTo(row.Label),
                        $"{row.Family} {row.Key} shows the wrong button.");
                }
            });
        }
    }
}
```

- [ ] **Step 2: Run to see them fail** — `recompile`; compile error, `PromptGlyphs` missing.

- [ ] **Step 3: Write the table**

```csharp
namespace BattleBomb.Core.Players
{
    /// <summary>What a prompt is asking for — a menu role or a fight verb, never a device button.</summary>
    public enum PromptKey
    {
        Move = 0,
        Confirm = 1,
        Back = 2,
        Option = 3,
        Lock = 4,
        Tabs = 5,
        Pause = 6,
        Light = 7,
        Heavy = 8,
        Magic = 9,
        QuickUse = 10,
        Jump = 11,
    }

    /// <summary>Round for a pad's face buttons; square for its system and shoulder buttons and
    /// for every keyboard key — the design's two badge shapes.</summary>
    public enum GlyphShape
    {
        Round = 0,
        Square = 1,
    }

    /// <summary>The badge's fill. Colour itself belongs to the UI; Core only says which.</summary>
    public enum GlyphTone
    {
        Brass = 0,
        Green = 1,
        Red = 2,
        Blue = 3,
        Yellow = 4,
        Key = 5,
    }

    public readonly struct PromptGlyph
    {
        public readonly string Label;
        public readonly GlyphShape Shape;
        public readonly GlyphTone Tone;

        public PromptGlyph(string label, GlyphShape shape, GlyphTone tone)
        {
            Label = label;
            Shape = shape;
            Tone = tone;
        }
    }

    /// <summary>
    /// The picture for each prompt, per device family (D57; UI Pass 01's hint row). Xbox letters
    /// on every controller, by Michael's choice (2026-09-24). An acceptance test keeps this in
    /// step with the input asset, so a rebound action cannot quietly go on showing the old button.
    /// </summary>
    public static class PromptGlyphs
    {
        public static PromptGlyph For(InputFamily family, PromptKey key) =>
            family == InputFamily.Gamepad ? Pad(key) : Keys(key);

        private static PromptGlyph Pad(PromptKey key)
        {
            switch (key)
            {
                case PromptKey.Confirm:
                case PromptKey.Jump:
                    return Face("A", GlyphTone.Green);

                case PromptKey.Back:
                case PromptKey.Magic:
                    return Face("B", GlyphTone.Red);

                case PromptKey.Option:
                case PromptKey.Light:
                    return Face("X", GlyphTone.Blue);

                case PromptKey.Lock:
                case PromptKey.Heavy:
                    return Face("Y", GlyphTone.Yellow);

                case PromptKey.Tabs:
                    return System("LB/RB");

                case PromptKey.QuickUse:
                    return System("RB");

                case PromptKey.Pause:
                    return System("≡");

                default:
                    // The stick: the design draws it as a blank brass disc.
                    return new PromptGlyph(string.Empty, GlyphShape.Round, GlyphTone.Brass);
            }
        }

        private static PromptGlyph Keys(PromptKey key)
        {
            switch (key)
            {
                case PromptKey.Confirm:
                    return Cap("Enter");

                case PromptKey.Back:
                case PromptKey.Pause:
                    return Cap("Esc");

                case PromptKey.Option:
                case PromptKey.Light:
                    return Cap("J");

                case PromptKey.Lock:
                case PromptKey.Heavy:
                    return Cap("K");

                case PromptKey.Tabs:
                    return Cap("Q/E");

                case PromptKey.Magic:
                    return Cap("L");

                case PromptKey.QuickUse:
                    return Cap("I");

                case PromptKey.Jump:
                    return Cap("Space");

                default:
                    return Cap("WASD");
            }
        }

        private static PromptGlyph Face(string label, GlyphTone tone) =>
            new PromptGlyph(label, GlyphShape.Round, tone);

        private static PromptGlyph System(string label) =>
            new PromptGlyph(label, GlyphShape.Square, GlyphTone.Brass);

        private static PromptGlyph Cap(string label) =>
            new PromptGlyph(label, GlyphShape.Square, GlyphTone.Key);
    }
}
```

- [ ] **Step 4: Run both fixtures** — `PromptGlyphsTests` **4 passed**, `PromptGlyphsAcceptanceTests` **1 passed**. Then the whole EditMode suite: green.

- [ ] **Step 5: Commit** — subject `G9: the button pictures, pinned to the input asset`.

---

### Task 10: The badge row

The design's hint row as a reusable widget: a 24px badge (round or squared, coloured per button) and a caption, laid out left to right. Values from the ShopPanel design (UI Pass 01): badge 24×24; key letter Passion One 12px in `#231a2b`; caption Archivo 12px at `rgba(246,239,226,.75)`; 7px badge-to-caption; squared corners 4px; X `#7fb2e8`, Y `#e8c95f`, B `#e0574f`, stick and Start `#c9ab6a`. A is not in the design; its green is the same pastel step, `#8fd27a`.

**Files:**
- Modify: `Assets/_BattleBomb/UI/Chest/UiBuild.cs`
- Create: `Assets/_BattleBomb/UI/Chest/PromptRow.cs`

- [ ] **Step 1: Add the pad colours to `UiBuild`**

After the `Down` colour:

```csharp
        // Pad buttons, from the ShopPanel design's hint row (UI Pass 01). A is not in the design —
        // nothing there needed confirming — so its green is the same pastel step as the others.
        internal static readonly Color PadGreen = Hex("#8fd27a");
        internal static readonly Color PadRed = Hex("#e0574f");
        internal static readonly Color PadBlue = Hex("#7fb2e8");
        internal static readonly Color PadYellow = Hex("#e8c95f");
```

- [ ] **Step 2: Add the rounded-square sprite**

Add `private static Sprite _roundedSquare;` beside the other sprite caches (line ~63).

After `Disc`, add:

```csharp
        /// <summary>
        /// The squared badge: Start, the shoulders, every keyboard key. Nine-sliced, so the design's
        /// 4px corners stay 4px however wide the label makes it — "Enter" is twice the width of "J".
        /// Draw it with <see cref="Image.Type.Sliced"/> and a pixels-per-unit multiplier of
        /// <see cref="RoundedSquareScale"/>.
        /// </summary>
        internal static Sprite RoundedSquare =>
            Polygon(ref _roundedSquare, "UiBuild.RoundedSquare", Rounded(0.18f, 8), border: 24f);

        /// <summary>Shrinks the 24-texel slice border to the design's ~4px corner.</summary>
        internal const float RoundedSquareScale = 4f;

        private static Vector2[] Rounded(float radius, int stepsPerCorner)
        {
            var points = new Vector2[stepsPerCorner * 4];
            Vector2[] centres =
            {
                new Vector2(1f - radius, radius),
                new Vector2(1f - radius, 1f - radius),
                new Vector2(radius, 1f - radius),
                new Vector2(radius, radius),
            };

            int n = 0;
            for (int corner = 0; corner < 4; corner++)
            {
                for (int step = 0; step < stepsPerCorner; step++)
                {
                    float degrees = corner * 90f - 90f + 90f * step / (stepsPerCorner - 1);
                    float angle = degrees * Mathf.Deg2Rad;
                    points[n++] = centres[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
            }

            return points;
        }
```

Change `Polygon`'s signature to `private static Sprite Polygon(ref Sprite cache, string name, Vector2[] points, float border = 0f)` and its `Sprite.Create` call to:

```csharp
            var rect = new Rect(0f, 0f, size, size);
            var pivot = new Vector2(0.5f, 0.5f);
            cache = border > 0f
                ? Sprite.Create(texture, rect, pivot, size, 0, SpriteMeshType.FullRect,
                    new Vector4(border, border, border, border))
                : Sprite.Create(texture, rect, pivot, size);
```

- [ ] **Step 3: Write `PromptRow`**

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Players;
using UnityEngine;
using UnityEngine.UI;

namespace BattleBomb.UI.Chest
{
    /// <summary>One entry in a hint row: which button, and what it does here.</summary>
    internal readonly struct Prompt
    {
        internal readonly PromptKey Key;
        internal readonly string Caption;
        internal readonly bool Gold;

        internal Prompt(PromptKey key, string caption, bool gold = false)
        {
            Key = key;
            Caption = caption;
            Gold = gold;
        }
    }

    /// <summary>
    /// The design's hint row (UI Pass 01, ShopPanel): a 24px badge per button — round for face
    /// buttons, squared for system and shoulder buttons and for keys — coloured per button, with
    /// its caption beside it. Drawn in the buttons of the device the player last pressed (D57).
    /// </summary>
    internal sealed class PromptRow
    {
        internal const float Height = 24f;

        private const float Gap = 7f;
        private const float Spacing = 18f;

        private static readonly Color CaptionColor = new Color(0.965f, 0.937f, 0.886f, 0.75f);

        private readonly RectTransform _root;
        private readonly Text _flash;
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly HashSet<string> _labels = new HashSet<string>();

        private sealed class Entry
        {
            internal RectTransform Root;
            internal Image Badge;
            internal Text Key;
            internal Text Caption;
        }

        internal PromptRow(RectTransform parent)
        {
            _root = UiBuild.Rect("Prompts", parent);
            _flash = UiBuild.Label("Flash", _root, string.Empty, 12, UiBuild.Gold,
                TextAnchor.MiddleLeft, UiBuild.Ui);
            UiBuild.StretchX(_flash.rectTransform, 0f);
            _flash.gameObject.SetActive(false);
        }

        /// <summary>Along the bottom of the parent, between two fractions of its width, inset by
        /// pixels. The row is one line, so only its bottom edge and its ends are chosen.</summary>
        internal void Place(float fromX, float toX, float left, float bottom, float right)
        {
            _root.anchorMin = new Vector2(fromX, 0f);
            _root.anchorMax = new Vector2(toX, 0f);
            _root.pivot = Vector2.zero;
            _root.offsetMin = new Vector2(left, bottom);
            _root.offsetMax = new Vector2(-right, bottom + Height);
        }

        internal void Show(IReadOnlyList<Prompt> prompts, InputFamily family)
        {
            _flash.gameObject.SetActive(false);
            _labels.Clear();

            float limit = _root.rect.width;
            float x = 0f;
            int used = 0;
            for (int i = 0; i < prompts.Count; i++)
            {
                PromptGlyph glyph = PromptGlyphs.For(family, prompts[i].Key);

                // One button, one badge. On a keyboard Escape is Back and Pause at once, and
                // "Esc Back" beside "Esc Leave" would say two things about one key; the first wins.
                if (!_labels.Add(glyph.Label))
                {
                    continue;
                }

                Entry entry = EntryAt(used);
                float width = Draw(entry, glyph, prompts[i]);
                if (used > 0 && limit > 0f && x + width > limit)
                {
                    entry.Root.gameObject.SetActive(false);
                    break;
                }

                entry.Root.anchoredPosition = new Vector2(x, 0f);
                x += width + Spacing;
                used++;
            }

            for (int i = used; i < _entries.Count; i++)
            {
                _entries[i].Root.gameObject.SetActive(false);
            }
        }

        /// <summary>A one-line message in place of the badges — what just happened, briefly.</summary>
        internal void ShowText(string text)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Root.gameObject.SetActive(false);
            }

            _flash.gameObject.SetActive(true);
            _flash.text = text;
        }

        internal static Color ToneColor(GlyphTone tone)
        {
            switch (tone)
            {
                case GlyphTone.Green:
                    return UiBuild.PadGreen;
                case GlyphTone.Red:
                    return UiBuild.PadRed;
                case GlyphTone.Blue:
                    return UiBuild.PadBlue;
                case GlyphTone.Yellow:
                    return UiBuild.PadYellow;
                case GlyphTone.Key:
                    return UiBuild.Bone;
                default:
                    return UiBuild.Brass;
            }
        }

        /// <summary>
        /// A button written into a sentence — "X to grab" — in its badge's colour, for the screens
        /// that are still one block of text: the front door, and the HUD's IMGUI cards.
        /// </summary>
        internal static string Inline(InputFamily family, PromptKey key)
        {
            PromptGlyph glyph = PromptGlyphs.For(family, key);
            return UiBuild.Tint(glyph.Label.Length > 0 ? glyph.Label : "Stick", ToneColor(glyph.Tone));
        }

        private Entry EntryAt(int index)
        {
            while (_entries.Count <= index)
            {
                var entry = new Entry { Root = UiBuild.Rect($"Prompt {_entries.Count}", _root) };
                entry.Badge = UiBuild.Box("Badge", entry.Root, UiBuild.Brass);
                entry.Key = UiBuild.Label("Key", entry.Badge.rectTransform, string.Empty, 12,
                    UiBuild.OnBrass, TextAnchor.MiddleCenter, UiBuild.Display);
                entry.Caption = UiBuild.Label("Caption", entry.Root, string.Empty, 12, CaptionColor,
                    TextAnchor.MiddleLeft, UiBuild.Ui);
                entry.Caption.horizontalOverflow = HorizontalWrapMode.Overflow;
                _entries.Add(entry);
            }

            return _entries[index];
        }

        private static float Draw(Entry entry, in PromptGlyph glyph, in Prompt prompt)
        {
            entry.Root.gameObject.SetActive(true);

            bool round = glyph.Shape == GlyphShape.Round;
            float badge = round ? Height : Mathf.Max(Height, 12f + 7f * glyph.Label.Length);

            entry.Badge.sprite = round ? UiBuild.Disc : UiBuild.RoundedSquare;
            entry.Badge.type = round ? Image.Type.Simple : Image.Type.Sliced;
            entry.Badge.pixelsPerUnitMultiplier = round ? 1f : UiBuild.RoundedSquareScale;
            entry.Badge.color = ToneColor(glyph.Tone);
            UiBuild.Pin(entry.Badge.rectTransform, 0f, 0f, badge, Height);

            entry.Key.text = glyph.Label;
            entry.Key.fontSize = glyph.Label.Length > 2 ? 11 : 12;

            entry.Caption.text = prompt.Caption;
            entry.Caption.color = prompt.Gold ? UiBuild.Gold : CaptionColor;
            float caption = Mathf.Ceil(entry.Caption.preferredWidth);
            UiBuild.Pin(entry.Caption.rectTransform, badge + Gap, 0f, caption + 2f, Height);

            float width = badge + Gap + caption;
            UiBuild.Pin(entry.Root, 0f, 0f, width, Height);
            return width;
        }
    }
}
```

- [ ] **Step 4: Recompile** — expected clean. Nothing uses the row yet; EditMode stays green.

- [ ] **Step 5: Commit** — subject `G10: the design's badge row`.

---

### Task 11: The chest and hero prompts

The chest's text footer becomes the badge row, drawn in the player's own device, rebuilt from the focus every refresh. The hero panel's static footer goes — the screen's row covers both halves. Dead text-strip code with stale button names goes with it.

**Files:**
- Modify: `Assets/_BattleBomb/UI/Chest/ChestScreenHost.cs`
- Modify: `Assets/_BattleBomb/UI/Chest/ChestScreen.cs`
- Modify: `Assets/_BattleBomb/UI/Chest/ChestScreen.Visuals.cs`
- Modify: `Assets/_BattleBomb/UI/Chest/HeroPanel.cs`

- [ ] **Step 1: Let the host report a player's family**

In `ChestScreenHost.cs`, beside `RequestClose`:

```csharp
        /// <summary>Which button pictures this player's screen shows (D57).</summary>
        internal InputFamily FamilyFor(int playerId) =>
            _driver != null ? _driver.Players.FamilyOf(new PlayerId(playerId)) : InputFamily.Keyboard;
```

(`BattleBomb.Core.Players` is already imported there.)

- [ ] **Step 2: Swap the screen's fields**

In `ChestScreen.cs`, delete the field `private Text _hint;` and add:

```csharp
        private PromptRow _promptRow;
        private readonly List<Prompt> _prompts = new List<Prompt>();
        private InputFamily _family;
```

At the top of `Tick`, after the `_bag == null` check and before the swallow block, add:

```csharp
            // The icons follow the hands: a player who picks up a controller mid-menu sees A B X Y
            // on the next step, even before pressing anything the screen reacts to.
            InputFamily family = Host != null ? Host.FamilyFor(_playerId) : InputFamily.Keyboard;
            if (family != _family)
            {
                _family = family;
                Refresh();
            }
```

In `Bind`, before `Build();`, add `_family = Host != null ? Host.FamilyFor(playerId) : InputFamily.Keyboard;`.

- [ ] **Step 3: Build the row instead of the hint label**

In `ChestScreen.Visuals.cs` `Build()`, delete:

```csharp
            _hint = UiBuild.Label("Hint", pad, string.Empty, 12, UiBuild.Muted,
                TextAnchor.LowerLeft, UiBuild.Ui);
            UiBuild.Stretch(_hint.rectTransform);
```

and in its place (still before the `// ── Hero ──` block and its `if (IsShop) return;`), add:

```csharp
            // The hint row belongs to the screen, not to a half: split co-op hides the sack half
            // behind the hero tab, and the row has to stay up either way.
            _promptRow = new PromptRow(root);
            _promptRow.Place(0f, _split ? 1f : 0.5f, 30f, 26f, 27f);
```

In `Refresh()`, replace `_hint.text = _flash.Length > 0 ? UiBuild.Tint(_flash, UiBuild.Gold) : HintLine();` with:

```csharp
            if (_flash.Length > 0)
            {
                _promptRow.ShowText(_flash);
            }
            else
            {
                _promptRow.Show(Prompts(), _family);
            }
```

- [ ] **Step 4: Replace `HintLine` with `Prompts`**

Delete `HintLine()` entirely and add:

```csharp
        /// <summary>
        /// The hint row (UI Pass 01), in the buttons of the device this player last pressed. What
        /// each button does depends on where the cursor is, so the row is rebuilt from the focus
        /// every refresh rather than naming one verb and being wrong on three of four blocks.
        /// </summary>
        private IReadOnlyList<Prompt> Prompts()
        {
            _prompts.Clear();
            _prompts.Add(new Prompt(PromptKey.Move, "Move"));

            // Mid-combine the screen is one question, so the row answers only that one.
            if (_nav.PendingCombine >= 0)
            {
                _prompts.Add(new Prompt(PromptKey.Confirm, "Combine with this"));
                _prompts.Add(new Prompt(PromptKey.Option, "Combine the whole pile", gold: true));
                _prompts.Add(new Prompt(PromptKey.Back, "Cancel"));
                return _prompts;
            }

            switch (_nav.Focus)
            {
                case ChestFocus.Grid:
                    _prompts.Add(new Prompt(PromptKey.Confirm, "Actions"));
                    _prompts.Add(new Prompt(PromptKey.Option, "Sell"));
                    _prompts.Add(new Prompt(PromptKey.Lock, "Lock"));
                    break;

                case ChestFocus.Loadout:
                    _prompts.Add(new Prompt(PromptKey.Confirm, "Upgrade / take off"));
                    _prompts.Add(new Prompt(PromptKey.Lock, "Lock"));
                    break;

                case ChestFocus.Stats:
                    _prompts.Add(new Prompt(PromptKey.Confirm, "Spend a point"));
                    break;

                case ChestFocus.Upgrade:
                    _prompts.Add(new Prompt(PromptKey.Confirm, "Spend"));
                    break;

                case ChestFocus.Stock:
                    _prompts.Add(new Prompt(PromptKey.Confirm, "Buy"));
                    break;

                case ChestFocus.Junk:
                    _prompts.Add(new Prompt(PromptKey.Confirm, "Clear the junk"));
                    break;

                default:
                    _prompts.Add(new Prompt(PromptKey.Confirm, "Choose"));
                    break;
            }

            string tab = TabCaption();
            if (tab.Length > 0)
            {
                // Gold solo, where the other half is easy to miss: the hero panel went a whole
                // pass without anyone finding it (Michael, 2026-08-23).
                _prompts.Add(new Prompt(PromptKey.Tabs, tab, gold: HeroBeside));
            }

            string leave = IsShop ? "Leave" : "Leave the chest";
            _prompts.Add(new Prompt(PromptKey.Back, BackLeaves ? leave : "Back"));
            if (!BackLeaves)
            {
                _prompts.Add(new Prompt(PromptKey.Pause, leave));
            }

            return _prompts;
        }

        /// <summary>Whether B here closes the screen: the top level, where
        /// <see cref="ChestNavigation.Cancel"/> has nothing left to back out of.</summary>
        private bool BackLeaves =>
            _nav.PendingCombine < 0
            && (_nav.Focus == ChestFocus.Grid
                || _nav.Focus == ChestFocus.Filters
                || _nav.Focus == ChestFocus.Tabs
                || (_nav.Focus == ChestFocus.Stock && Buying));

        /// <summary>What LB / RB would switch to here, or nothing where there is one half.</summary>
        private string TabCaption()
        {
            if (_nav.Focus == ChestFocus.Menu || _nav.Focus == ChestFocus.Upgrade)
            {
                return string.Empty;
            }

            if (IsShop)
            {
                return Buying ? "Sell side" : "Buy side";
            }

            if (HasTabs)
            {
                return _nav.Tab == ChestTab.Hero ? "Sack" : "Hero";
            }

            if (HeroBeside)
            {
                return OnLoadout || _nav.Focus == ChestFocus.Stats ? "Your sack" : "Your gear";
            }

            return string.Empty;
        }
```

Add `using BattleBomb.Core.Players;` to `ChestScreen.Visuals.cs` if it is not already there.

- [ ] **Step 5: Delete the dead text-strip code**

Confirm there are no callers first: `grep -n 'TabLine\|FilterLine\|UpgradePanelFor\|_menuRoot\|_menuText' Assets/_BattleBomb/UI/Chest/*.cs` — expected: only their own declarations. Then delete from `ChestScreen.Visuals.cs`: the methods `TabLine()`, `FilterLine(...)`, `UpgradePanelFor(...)`, and the fields `_menuRoot`, `_menuText`. Keep `NameOf`/`ValueOf` — `ShowUpgradeList` uses them. They still name Light, Heavy, and Magic; left in place they are the first thing the next reader would copy.

- [ ] **Step 6: Remove the hero panel's own footer**

In `HeroPanel.cs`: delete the field `private readonly Text _hints;`, the three lines that build it (`_hints = UiBuild.Legible(...)` through `UiBuild.Stretch(_hints.rectTransform);`), and the line `_hints.text = "Stick: move    Light: open the slot    Heavy: back    Esc: leave";`.

- [ ] **Step 7: Recompile and run both suites** — EditMode green; PlayMode expected **21**. Delete `Assets/InitTestScene*`.

- [ ] **Step 8: Look at it (slow, settled — drive it yourself)**

Play mode from `Gameplay.unity`. Use `eval` to open a chest for Player 1 (`driver.OpenScreen(0, BattleBomb.Gameplay.World.InteractionKind.Chest, <the chest WorldInteractable>)`), then `capture_game_view`. Expected: a badge row along the bottom of the sack half — brass disc "Move", key caps "Enter Actions", "J Sell", "K Lock", gold "Q/E Your gear", "Esc Leave the chest" (keyboard family, since nothing has been pressed on a pad). Then set a virtual gamepad's state with `InputSystem.QueueStateEvent` (see the memory's play-mode recipe: Unity foregrounded, `AllDeviceInputAlwaysGoesToGameView`, Game view focused), press A once, and capture again: A/B/X/Y circles, "LB/RB" squared.

If the Start badge (shown when the cursor is one level deep, e.g. after opening the item menu) renders as an empty box, the fonts lack `≡`: change `PromptGlyphs.Pad`'s `PromptKey.Pause` case to `System("MENU")`, change the acceptance row's label to `"MENU"`, rerun both glyph fixtures, and note it in the commit. Then `editor_stop`.

- [ ] **Step 9: Commit** — subject `G11: the chest and hero prompts, in the player's own buttons`. Attach the two captures' descriptions in the body.

---

### Task 12: The other prompts

Settings, results, the front door, and the two in-world cards. The first three get a badge row; the text-block screens and IMGUI cards write buttons inline in their colour.

**Files:**
- Modify: `Assets/_BattleBomb/UI/Chest/SettingsMenu.cs`
- Modify: `Assets/_BattleBomb/UI/Frontend/ResultsScreen.cs`
- Modify: `Assets/_BattleBomb/UI/Frontend/FrontendFlow.cs`
- Modify: `Assets/_BattleBomb/UI/Combat/LootHud.cs`
- Modify: `Assets/_BattleBomb/UI/Combat/ReviveHud.cs`

- [ ] **Step 1: Settings**

Fields: `private PromptRow _promptRow;` and `private readonly List<Prompt> _prompts = new List<Prompt>();`. In `Build()`, after `_body = UiBuild.Label("Text", pad, …);`, add:

```csharp
            _promptRow = new PromptRow(panel);
            _promptRow.Place(0f, 1f, 18f, 12f, 18f);
```

In `Repaint()`, delete `_text.Append("\n\nStick: move   Light: toggle   Pause or Heavy: close");` and after `_body.text = _text.ToString();` add:

```csharp
            _prompts.Clear();
            _prompts.Add(new Prompt(PromptKey.Move, "Move"));
            _prompts.Add(new Prompt(PromptKey.Confirm, "Toggle"));
            _prompts.Add(new Prompt(PromptKey.Back, "Close"));
            _promptRow.Show(_prompts, _driver.Players.FamilyOf(new PlayerId(_owner)));
```

- [ ] **Step 2: Results**

Same two fields. In `Build()`, after `_body = …`, add:

```csharp
            // Left of the pointer button, which owns the bottom-centre of the panel.
            _promptRow = new PromptRow(panel);
            _promptRow.Place(0f, 0.28f, 20f, 16f, 0f);
```

In `Repaint()`, delete `_text.Append("\n\nLight: back to chapters");`, and after the body text is set add:

```csharp
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            InputFamily family = actors.Count > 0
                ? _driver.Players.FamilyOf(actors[0].PlayerId)
                : InputFamily.Keyboard;
            _prompts.Clear();
            _prompts.Add(new Prompt(PromptKey.Confirm, "Back to chapters"));
            _promptRow.Show(_prompts, family);
```

Add `using BattleBomb.UI.Chest;` if `ResultsScreen` does not already import it (it uses `UiBuild`, so it does).

- [ ] **Step 3: The front door**

Same two fields. In `Build()`, after `_body = UiBuild.Label("Text", pad, …);`, add:

```csharp
            _promptRow = new PromptRow(pad);
            _promptRow.Place(0f, 1f, 0f, 0f, 0f);
```

Add the helper:

```csharp
        private InputFamily FamilyOf(int slot) => _input.Players.FamilyOf(new PlayerId(slot));
```

In `Repaint()`:

- Title: delete `_text.Append("\n\nStick: move   Light: choose");`.
- Characters: `"press Light to join"` becomes
  `$"press {PromptRow.Inline(InputFamily.Gamepad, PromptKey.Confirm)} or {PromptRow.Inline(InputFamily.Keyboard, PromptKey.Confirm)} to join"` (a would-be Player 2's device is unknown until they press, so name both);
  `"   (Light: ready)"` becomes `$"   ({PromptRow.Inline(FamilyOf(slot), PromptKey.Confirm)}: ready)"`;
  delete `_text.Append("\nLeft/right: pick   Light: ready   Heavy: back");`.
- Chapters: delete the `_text.Append("\n\nUp/down: chapter   Left/right: tier   Light: ")…Append("   Heavy: back");` statement (the whole chained expression).

At the end of `Repaint()`, after `_body.text = _text.ToString();`, add:

```csharp
            _prompts.Clear();
            switch (_state.Screen)
            {
                case FrontendScreen.Title:
                    _prompts.Add(new Prompt(PromptKey.Move, "Move"));
                    _prompts.Add(new Prompt(PromptKey.Confirm, "Choose"));
                    break;

                case FrontendScreen.Characters:
                    _prompts.Add(new Prompt(PromptKey.Move, "Pick"));
                    _prompts.Add(new Prompt(PromptKey.Confirm, "Ready"));
                    _prompts.Add(new Prompt(PromptKey.Back, "Back"));
                    break;

                case FrontendScreen.Chapters:
                    _prompts.Add(new Prompt(PromptKey.Move, "Chapter and tier"));
                    _prompts.Add(new Prompt(PromptKey.Confirm, _selection.CanLaunch ? "Launch" : "Locked"));
                    _prompts.Add(new Prompt(PromptKey.Back, "Back"));
                    break;
            }

            _promptRow.Show(_prompts, FamilyOf(0));
```

- [ ] **Step 4: The loot card**

In `LootHud.cs`, replace `AnyoneOver` with:

```csharp
        /// <summary>The first standing player close enough to take this drop, or null.</summary>
        private static CharacterActor FirstOver(IReadOnlyList<CharacterActor> players, Vector3 position)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Condition.IsDown)
                {
                    continue;
                }

                Vector3 to = players[i].Position - position;
                to.y = 0f;
                if (to.sqrMagnitude <= SimulationDriver.GrabRadius * SimulationDriver.GrabRadius)
                {
                    return players[i];
                }
            }

            return null;
        }
```

In `OnGUI`, the loop's guard becomes:

```csharp
                CharacterActor over = pickups[i] != null ? FirstOver(players, pickups[i].Position) : null;
                if (over == null)
                {
                    continue;
                }
```

and the last `DrawLine` becomes:

```csharp
                string grab = PromptRow.Inline(_driver.Players.FamilyOf(over.PlayerId), PromptKey.Light);
                DrawLine(new Rect(x, y, 240f, lineHeight), $"{grab} to grab", new Color(0.65f, 0.65f, 0.65f));
```

In the `_style = new GUIStyle(GUI.skin.label) { … }` initializer add `richText = true,`. Add `using BattleBomb.Core.Players;` and `using BattleBomb.UI.Chest;`.

- [ ] **Step 5: The downed marker**

In `ReviveHud.cs`, add `richText = true,` to the style initializer, the same two usings, and:

```csharp
        /// <summary>The buttons of whoever can do the reviving: the first player still standing.</summary>
        private InputFamily FamilyOfReviver(IReadOnlyList<CharacterActor> players, int downed)
        {
            for (int j = 0; j < players.Count; j++)
            {
                if (j != downed && !players[j].Condition.IsDown)
                {
                    return _driver.Players.FamilyOf(players[j].PlayerId);
                }
            }

            return InputFamily.Keyboard;
        }
```

and the label line becomes:

```csharp
                string beat = PromptRow.Inline(FamilyOfReviver(players, i), PromptKey.Light);
                string label = $"P{players[i].PlayerId.Value + 1} DOWN — {beat} on the beat";
```

- [ ] **Step 6: Nothing still names a verb as a button**

`grep -rn '"[^"]*\(Light\|Heavy\|Magic\):' Assets/_BattleBomb/UI` and `grep -rn 'Light to\|Esc / Start\|Heavy: back' Assets/_BattleBomb/UI` — expected: no matches.

- [ ] **Step 7: Recompile and run both suites** — EditMode green; PlayMode **21**. Delete `Assets/InitTestScene*`.

- [ ] **Step 8: Look at the front door** — play mode from `Frontend.unity`, `capture_game_view` on the title (badge row under the menu), `editor_stop`.

- [ ] **Step 9: Commit** — subject `G12: every other prompt in the player's own buttons`.

---

### Task 13: The filter chips no longer sit on the grid

In couch co-op the sack/hero tabs push the filter chips 42px lower, but the grid started at a fixed 128px regardless — so the chips' bottom edge (108 + 30 = 138) ran 10px into the top row. Derive the grid's top from the chips.

**Files:**
- Modify: `Assets/_BattleBomb/UI/Chest/ChestScreen.Visuals.cs`

- [ ] **Step 1: Name the numbers**

Beside `ShopTabBand`, add:

```csharp
        private const float ChipHeight = 30f;

        /// <summary>The room between the chips and the grid's first row. The grid used to start at
        /// a fixed 128 whatever the chips did, so in couch co-op — chips 42px lower under the tabs —
        /// their bottom edge ran 10px into the top row.</summary>
        private const float ChipsToGrid = 32f;

        /// <summary>Where the filter chips start: under the head, or under the tabs as well.</summary>
        private float FilterTop => HasTabs ? 108f : 66f;

        private float GridTop => FilterTop + ChipHeight + ChipsToGrid;
```

- [ ] **Step 2: Use them**

In `BuildFilterRow`: `float top = HasTabs ? 108f : 66f;` → `float top = FilterTop;`; both `30f` heights (chip and sort box `Pin` calls) → `ChipHeight`. In `Build()`: `gridArea.offsetMax = new Vector2(0f, -128f);` → `gridArea.offsetMax = new Vector2(0f, -GridTop);`. Solo and shop are unchanged (66 + 30 + 32 = 128).

- [ ] **Step 3: Measure it (slow, settled — drive it yourself)**

`recompile`. Play mode in `Gameplay.unity` opened on its own (two players, so the chest is split). `eval`: open a chest for player 0, then return the lowest chip's bottom and the first cell's top in world space:

```csharp
var root = UnityEngine.GameObject.Find("Chest Screen P1");
float chipBottom = float.MaxValue, cellTop = float.MinValue;
var corners = new UnityEngine.Vector3[4];
foreach (var rt in root.GetComponentsInChildren<UnityEngine.RectTransform>(true))
{
    if (rt.name.StartsWith("Filter ")) { rt.GetWorldCorners(corners); chipBottom = UnityEngine.Mathf.Min(chipBottom, corners[0].y); }
    if (rt.name == "Cell 0x0") { rt.GetWorldCorners(corners); cellTop = corners[1].y; }
}
return "chip bottom " + chipBottom + " / cell top " + cellTop + " / clear " + (chipBottom > cellTop);
```

Expected: `clear True`. `capture_game_view` for the record. `editor_stop`.

- [ ] **Step 4: Run EditMode** — green.

- [ ] **Step 5: Commit** — subject `G13: the filter chips no longer sit on the grid in couch co-op`.

---

### Task 14: Record it, and Michael's pass

**Files:**
- Modify: `docs/DECISIONS.md` (append D57 before `## Open`)
- Modify: `docs/GAME_DESIGN.md` §3.1
- Modify: `CLAUDE.md` (rule 3), `docs/ARCHITECTURE.md` (wherever rule 3's wording appears)
- Modify: `docs/ROADMAP.md` (§4 Groundwork, §9 debts)

- [ ] **Step 1: Append D57**

```markdown
## D57 — The menu layer and the seats · **Locked** *(amends D17; retires PlayerInput)*

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
```

- [ ] **Step 2: GAME_DESIGN §3.1** — after the verb table's paragraph ending "…chorded inputs are out by construction (D17).", add:

```markdown
**Menus (D57)** read their own buttons, never a combat verb: A confirms, B backs out (Esc on a
keyboard), X sells, Y locks, LB/RB switch tab or mode (Q/E), Start leaves; D-pad and arrows
navigate. X and Y are shortcuts only — everything they do is reachable by choosing the item, then
the verb — so touch needs nothing but taps.
```

- [ ] **Step 3: Rule 3's wording** — in `CLAUDE.md`, rule 3's sentence "only `InputSystemCommandSource` touches a device" becomes "only `InputSystemCommandSource`, with the `SeatInput` it owns, touches a device". Run `grep -n 'InputSystemCommandSource' docs/ARCHITECTURE.md` and make the same change wherever the rule is stated.

- [ ] **Step 4: ROADMAP** — in §4, the Groundwork heading gains `— complete (<date>, <first commit>..<last commit>)`; in §9, mark the "P2" and filter-overlap rows paid.

- [ ] **Step 5: Final gates** — full EditMode and PlayMode runs; report exact counts; delete `Assets/InitTestScene*`; `git status` clean apart from these docs.

- [ ] **Step 6: Commit** — subject `G14: D57 — the menu layer and the seats`.

- [ ] **Step 7: Michael's pass (fast motion — his eyes, not sampling)**

Hand him this checklist and treat his report as the verification:

1. **Solo on the keyboard.** Title → play. Open a chest: the footer shows key caps (Enter, J, K, Q/E, Esc). Esc backs out one level at a time and closes the chest from the top. Esc in the world opens settings; Esc closes them. Closing the chest with Esc does *not* open settings.
2. **Pick up a controller mid-menu.** The footer switches to A/B/X/Y circles the moment you press anything; put it down and press a key, it switches back.
3. **X and Y.** X sells the item under the cursor instantly; Y locks it; X on a locked item refuses; Y again releases it.
4. **Shoulders.** Solo at a chest, LB/RB jump between your sack and your gear. At the shopkeeper they flip Buy/Sell. Start leaves the screen from anywhere, even inside an item's menu.
5. **Mid-combine.** Pick Combine on an item with duplicates: X combines the whole pile, B cancels.
6. **Two controllers on one couch.** Start the title with pad 1; at character select press A on pad 2 — Player 2 joins. In game, each pad moves only its own character. B on pad 2 at character select leaves.
7. **Keyboard + controller couch.** Same as 6 with the keyboard as Player 1.
8. **Labels.** Solo, the health bar reads **P1** — including after going back to chapters and relaunching.
9. **Couch chest.** In split co-op the filter chips sit clear of the first grid row, and the footer stays visible on the Hero tab.
10. **Results.** Finish a stage mashing A: the results screen stays up until you let go and press A again.

Record his verdict in `docs/ROADMAP.md` §4 and in the project status memory.

---

## Self-review (done while writing)

- **Spec coverage.** ROADMAP §4 Groundwork: bridge (Before you start); input switching (Tasks 3–6); button icons (Tasks 9–12); the menu map with the kickoff answers (Tasks 1, 2, 7, 8); D17 amendment (Task 14, as D57 — the log is append-only); both bugs (Task 5 for P2, Task 13 for the overlap); "Done when" checklist (Task 14 Step 7). Added beyond the roadmap's text, each because the map made it necessary: two-pad couch seating (the old pairing could not do it), Escape's double-fire guard, the results screen's release guard, and the held-across-scenes rule.
- **Type consistency.** `MenuPress.{Confirm,Back,Pause,Option,Lock,Tab,Any,AnyHeld}`, `SeatAssignment.{NoDevice,FirstSeatHome,SecondSeatDevice,Joining,Owns,Follow,StandIn}`, `SeatInput.{Own,Sample,Family,LastDeviceId,Dispose}`, `PlayerRegistry.{FamilyOf,LastDeviceOf}`, `ChestNavigation.CycleTab`, `PromptRow.{Place,Show,ShowText,ToneColor,Inline}`, `PromptGlyphs.For`, `ChestScreenHost.FamilyFor` — each defined once and used with the same signature everywhere.
- **Known soft spots, flagged in their steps:** the EditMode count in Task 1 Step 7 (report the real number); binding re-resolution on a live asset (Task 4 Step 6 has the fallback); the `≡` glyph's font coverage (Task 11 Step 8 has the fallback); `RemoveUnusedOverrides` in eval (Task 5 Step 10 says what to drop).
