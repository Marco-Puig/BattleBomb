using System;
using System.Collections.Generic;
using System.IO;
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
            // The file, not the imported asset: the importer quietly gives a duplicated id a fresh
            // random one on every import, which is how Pause sat on Heavy's two ids for a month.
            InputActionAsset asset = InputActionAsset.FromJson(File.ReadAllText(AcceptanceFixture.ControlsAssetPath));
            try
            {
                List<Guid> ids = asset.actionMaps.Select(m => m.id)
                    .Concat(asset.actionMaps.SelectMany(m => m.actions).Select(a => a.id))
                    .Concat(asset.actionMaps.SelectMany(m => m.bindings).Select(b => b.id))
                    .ToList();

                Assert.That(ids, Has.No.Member(Guid.Empty), "A map, action or binding has no id.");

                Guid[] duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
                Assert.That(duplicates, Is.Empty,
                    "Duplicated ids: " + string.Join(", ", duplicates) +
                    ". A copy-pasted id is re-rolled on every import, so anything keyed by it — a saved " +
                    "rebind — silently stops matching.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
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
