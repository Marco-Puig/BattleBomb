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
            PlayerInput input = _prefab.GetComponent<PlayerInput>();
            Assert.That(input, Is.Not.Null,
                "The player needs a PlayerInput — it is what pairs devices per player and makes co-op work.");

            Assert.That(input.actions, Is.Not.Null, "PlayerInput has no action asset assigned.");
            Assert.That(input.actions.name, Is.EqualTo("BattleBombControls"),
                "The player must use the project's own action asset, not the URP template's.");
            Assert.That(input.defaultActionMap, Is.EqualTo(PlayerActions.Map),
                $"PlayerInput's default map must be '{PlayerActions.Map}'.");
        }

        [Test]
        public void Player_uses_polled_notifications_not_messages()
        {
            PlayerInput input = _prefab.GetComponent<PlayerInput>();

            Assert.That(input.notificationBehavior, Is.EqualTo(PlayerNotifications.InvokeUnityEvents),
                "Commands are sampled once per simulation step, so SendMessage/BroadcastMessage " +
                "notifications are pure overhead.");
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
            Transform proxy = _prefab.transform.Find("ShadowProxy");
            Assert.That(proxy, Is.Not.Null,
                "Expected a child named 'ShadowProxy'. A 2D character casts a flat cutout shadow; the " +
                "invisible 3D proxy is what makes the grounded shadow real (D15).");

            Renderer renderer = proxy.GetComponent<Renderer>();
            Assert.That(renderer, Is.Not.Null, "'ShadowProxy' has no renderer, so it casts nothing.");
            Assert.That(renderer.shadowCastingMode, Is.Not.EqualTo(ShadowCastingMode.Off),
                "The proxy exists solely to cast a shadow.");
        }
    }
}
