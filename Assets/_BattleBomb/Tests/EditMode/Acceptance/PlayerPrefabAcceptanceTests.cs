using BattleBomb.Gameplay.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace BattleBomb.Tests.EditMode.Acceptance
{
    /// <summary>
    /// What the player prefab must be. Written before the prefab exists; red until it does.
    /// </summary>
    public sealed class PlayerPrefabAcceptanceTests
    {
        private GameObject _prefab;

        [OneTimeSetUp]
        public void LoadPrefab()
        {
            _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AcceptanceFixture.PlayerPrefabPath);
            Assert.That(_prefab, Is.Not.Null, $"No prefab at {AcceptanceFixture.PlayerPrefabPath}.");
        }

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

        [Test]
        public void Player_turns_input_into_commands()
        {
            Assert.That(_prefab.GetComponent<InputSystemCommandSource>(), Is.Not.Null,
                "Without InputSystemCommandSource the player produces no PlayerCommand and the " +
                "simulation cannot see them at all.");
        }

        [Test]
        public void Player_carries_a_shadow_casting_proxy()
        {
            Transform proxy = FindDeep(_prefab.transform, "ShadowProxy");
            Assert.That(proxy, Is.Not.Null,
                "Expected a descendant named 'ShadowProxy'. A 2D character casts a flat cutout shadow; " +
                "the invisible 3D proxy is what makes the grounded shadow real (D15).");

            Renderer renderer = proxy.GetComponent<Renderer>();
            Assert.That(renderer, Is.Not.Null, "'ShadowProxy' has no renderer, so it casts nothing.");
            Assert.That(renderer.shadowCastingMode, Is.Not.EqualTo(ShadowCastingMode.Off),
                "The proxy exists solely to cast a shadow.");
        }

        private static Transform FindDeep(Transform root, string childName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
