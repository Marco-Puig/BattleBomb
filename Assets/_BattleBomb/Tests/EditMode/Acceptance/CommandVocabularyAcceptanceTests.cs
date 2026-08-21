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
    /// The game's verbs, in one vocabulary, in three places that must agree: the Core enum, the
    /// action-name constants, and the authored input asset. The set is D17's scheme as amended by
    /// D26 (Block cut for the mobile control budget) — no more verbs than the design has.
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
            "Equipment",  // the equipped active item (§5)
            "Jump",       // universal (§2.4)
        };

        /// <summary>
        /// System buttons, deliberately outside the five. They open menus, never act in the
        /// fight, and are never on the mobile thumb surface — so they do not spend D17's budget.
        /// They still travel as commands, because rule 3 has no exceptions.
        /// </summary>
        private static readonly string[] SystemButtons =
        {
            "Pause",      // opens the settings menu (M6)
        };

        private static string[] AllButtons => Buttons.Concat(SystemButtons).ToArray();

        /// <summary>Every non-composite binding the asset must carry, as (path, action).</summary>
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
        };

        [Test]
        public void The_button_enum_holds_exactly_the_designed_verbs()
        {
            string[] actual = Enum.GetNames(typeof(CommandButtons)).OrderBy(n => n).ToArray();
            string[] expected = AllButtons.Concat(new[] { "None" }).OrderBy(n => n).ToArray();

            Assert.That(actual, Is.EqualTo(expected),
                "CommandButtons must match the ability slot table (§3) plus the system buttons. " +
                "Extra combat verbs are input complexity the design does not have (pillar 1); " +
                "missing ones cannot be pressed at all.");
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

            string[] expected = AllButtons.Concat(new[] { "Move", "Map" }).OrderBy(n => n).ToArray();
            Assert.That(constants.Keys.OrderBy(n => n).ToArray(), Is.EqualTo(expected),
                "PlayerActions must name exactly the actions the asset has.");

            foreach (string button in AllButtons.Concat(new[] { "Move" }))
            {
                Assert.That(constants[button], Is.EqualTo(button),
                    "An action constant whose value differs from its name is a trap for the next reader.");
            }
        }

        [Test]
        public void The_input_asset_defines_exactly_those_actions()
        {
            InputActionMap map = GameplayMap();

            string[] actual = map.actions.Select(a => a.name).OrderBy(n => n).ToArray();
            string[] expected = AllButtons.Concat(new[] { "Move" }).OrderBy(n => n).ToArray();

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void The_input_asset_binds_every_action_on_keyboard_and_gamepad()
        {
            InputActionMap map = GameplayMap();
            List<InputBinding> bindings = map.bindings.Where(b => !b.isPartOfComposite).ToList();

            foreach ((string path, string action) in RequiredBindings)
            {
                bool bound = bindings.Any(b => b.path == path && b.action == action);
                Assert.That(bound, Is.True, $"Nothing binds {path} to {action}.");
            }
        }

        [Test]
        public void Movement_keeps_its_WASD_composite()
        {
            InputActionMap map = GameplayMap();
            string[] parts = map.bindings
                .Where(b => b.isPartOfComposite)
                .Select(b => b.path)
                .ToArray();

            foreach (string key in new[] { "<Keyboard>/w", "<Keyboard>/a", "<Keyboard>/s", "<Keyboard>/d" })
            {
                Assert.That(parts, Contains.Item(key));
            }
        }

        [Test]
        public void Every_action_and_binding_has_a_unique_id()
        {
            InputActionMap map = GameplayMap();

            List<Guid> ids = map.actions.Select(a => a.id)
                .Concat(map.bindings.Select(b => b.id))
                .ToList();

            Assert.That(ids, Has.No.Member(Guid.Empty), "An action or binding has no id.");

            Guid[] duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            Assert.That(duplicates, Is.Empty,
                "Duplicated ids: " + string.Join(", ", duplicates) +
                ". Copy-pasted ids silently alias bindings to each other.");
        }

        private static InputActionMap GameplayMap()
        {
            InputActionAsset asset =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(AcceptanceFixture.ControlsAssetPath);
            Assert.That(asset, Is.Not.Null,
                $"{AcceptanceFixture.ControlsAssetPath} is missing or failed to import.");

            InputActionMap map = asset.FindActionMap(PlayerActions.Map);
            Assert.That(map, Is.Not.Null, $"No '{PlayerActions.Map}' action map in the asset.");
            return map;
        }
    }
}
