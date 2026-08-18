using System;
using System.Collections.Generic;
using System.Linq;
using BattleBomb.Gameplay.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BattleBomb.Tests.EditMode.Acceptance
{
    /// <summary>
    /// Two players in the scene, and something on screen proving their commands arrive.
    /// </summary>
    public sealed class PlayersInSceneAcceptanceTests
    {
        private const string OverlayTypeName = "BattleBomb.UI.Debug.CommandDebugOverlay";
        private const string UiAssemblyName = "BattleBomb.UI";

        private Scene _scene;
        private bool _openedByTest;

        [OneTimeSetUp]
        public void OpenScene()
        {
            Assert.That(System.IO.File.Exists(AcceptanceFixture.GameplayScenePath), Is.True,
                $"No scene at {AcceptanceFixture.GameplayScenePath}.");

            _scene = AcceptanceFixture.OpenForInspection(AcceptanceFixture.GameplayScenePath, out _openedByTest);
        }

        [OneTimeTearDown]
        public void CloseScene() => AcceptanceFixture.CloseAfterInspection(_scene, _openedByTest);

        [Test]
        public void Two_players_are_present_and_both_come_from_the_prefab()
        {
            List<InputSystemCommandSource> sources = AcceptanceFixture.FindAll<InputSystemCommandSource>(_scene);

            Assert.That(sources.Count, Is.EqualTo(2),
                $"Expected two players in the scene, found {sources.Count}. Co-op is the frame (pillar 4) " +
                "— one player proves nothing about the two-player path.");

            foreach (InputSystemCommandSource source in sources)
            {
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(source.gameObject);
                Assert.That(path, Is.EqualTo(AcceptanceFixture.PlayerPrefabPath),
                    $"'{source.name}' is not an instance of the player prefab. Hand-built copies drift " +
                    "from the prefab the moment either changes.");
            }
        }

        [Test]
        public void The_two_players_are_paired_to_different_devices()
        {
            string[] schemes = AcceptanceFixture.FindAll<PlayerInput>(_scene)
                .Select(p => p.defaultControlScheme)
                .OrderBy(s => s)
                .ToArray();

            Assert.That(schemes, Is.EqualTo(new[] { "Gamepad", "Keyboard" }),
                "One player defaults to Keyboard and the other to Gamepad. Left unset, both PlayerInputs " +
                "bind every device and one keyboard drives both characters.");
        }

        [Test]
        public void A_debug_overlay_exists_in_the_UI_assembly()
        {
            Type overlay = FindOverlayType();

            Assert.That(overlay, Is.Not.Null,
                $"No type {OverlayTypeName} in assembly {UiAssemblyName}. It is the only thing that " +
                "shows a command actually arrived.");
            Assert.That(typeof(MonoBehaviour).IsAssignableFrom(overlay), Is.True,
                "The overlay is a scene component.");
        }

        [Test]
        public void The_overlay_is_in_the_scene_and_wired_to_the_driver()
        {
            List<Component> overlays = AcceptanceFixture.FindAllByTypeName(_scene, OverlayTypeName);

            Assert.That(overlays.Count, Is.EqualTo(1),
                $"Expected exactly one {OverlayTypeName} in the scene, found {overlays.Count}.");

            SerializedObject serialized = new SerializedObject(overlays[0]);
            SerializedProperty driver = serialized.FindProperty("_driver");

            Assert.That(driver, Is.Not.Null,
                "The overlay has no serialized '_driver' field — it must be handed the driver, not hunt " +
                "for one through a singleton.");
            Assert.That(driver.objectReferenceValue, Is.Not.Null,
                "The overlay's driver reference is empty, so it will render nothing. Wire it in the scene.");
        }

        private static Type FindOverlayType() =>
            AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == UiAssemblyName)
                ?.GetType(OverlayTypeName);
    }
}
