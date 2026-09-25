using System.Collections.Generic;
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
            (PromptKey.Move, InputFamily.Gamepad, "Gameplay", "Move", "<Gamepad>/leftStick", ""),
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
            (PromptKey.Move, InputFamily.Keyboard, "Gameplay", "Move", "<Keyboard>/w", "WASD"),
        };

        [Test]
        public void Every_prompt_names_a_button_the_asset_really_binds()
        {
            InputActionAsset asset =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(AcceptanceFixture.ControlsAssetPath);
            Assert.That(asset, Is.Not.Null);

            // Every row is checked and every mismatch reported at once. Unity's NUnit has no
            // Assert.Multiple, and a rebind usually breaks several rows together.
            var mismatches = new List<string>();
            foreach (var row in Expected)
            {
                InputActionMap map = asset.FindActionMap(row.Map);
                if (map == null)
                {
                    mismatches.Add($"The asset has no '{row.Map}' map.");
                    continue;
                }

                if (!map.bindings.Any(b => b.action == row.Action && b.path == row.Path))
                {
                    mismatches.Add($"The asset does not bind {row.Path} to {row.Map}/{row.Action}.");
                }

                string label = PromptGlyphs.For(row.Family, row.Key).Label;
                if (label != row.Label)
                {
                    mismatches.Add($"{row.Family} {row.Key} shows '{label}', not '{row.Label}'.");
                }
            }

            Assert.That(mismatches, Is.Empty, string.Join("\n", mismatches));
        }
    }
}
