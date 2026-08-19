using System.Collections.Generic;
using System.Linq;
using BattleBomb.Gameplay.Simulation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleBomb.Tests.EditMode.Acceptance
{
    /// <summary>
    /// What the gameplay scene must contain. Written before the scene work; red until it lands.
    /// </summary>
    public sealed class GameplaySceneAcceptanceTests
    {
        /// <summary>
        /// Everything allowed at the root of the scene. A name not on this list is scratch work that
        /// was left behind — delete the object. Extending this list is an escalation, not a fix.
        /// </summary>
        private static readonly string[] AllowedRootObjects =
        {
            "Ground",
            "Directional Light",
            "Global Volume",
            "Simulation",
            "Main Camera",
            "Player 1",
            "Player 2",
            "Debug Overlay",
            "Enemies",   // M3 (HANDOFF-M3 task 33): the spawner and its brood live under one root
        };

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
        public void Scene_is_in_build_settings_and_enabled()
        {
            EditorBuildSettingsScene entry = EditorBuildSettings.scenes
                .FirstOrDefault(s => s.path == AcceptanceFixture.GameplayScenePath);

            Assert.That(entry, Is.Not.Null, "The gameplay scene is not in Build Settings.");
            Assert.That(entry.enabled, Is.True, "The gameplay scene is in Build Settings but disabled.");
        }

        [Test]
        public void Scene_contains_no_leftover_scratch_objects()
        {
            string[] unexpected = AcceptanceFixture.RootNames(_scene)
                .Where(n => !AllowedRootObjects.Contains(n))
                .ToArray();

            Assert.That(unexpected, Is.Empty,
                "Objects left in the scene that nothing asked for: " + string.Join(", ", unexpected) +
                ". Probes, proofs and composers are scratch work — delete them when the check they " +
                "supported is finished.");
        }

        [Test]
        public void Exactly_one_camera_renders_the_scene()
        {
            List<Camera> enabled = AcceptanceFixture.FindAll<Camera>(_scene)
                .Where(c => c.enabled && c.gameObject.activeInHierarchy)
                .ToList();

            Assert.That(enabled.Count, Is.EqualTo(1),
                "Found " + enabled.Count + " active cameras (" +
                string.Join(", ", enabled.Select(c => c.name)) +
                "). Two cameras rendering the same scene is never intentional.");
        }

        [Test]
        public void The_camera_is_tagged_and_perspective()
        {
            Camera camera = TheCamera();

            Assert.That(camera.gameObject.CompareTag("MainCamera"), Is.True,
                "The rendering camera must be tagged MainCamera so Camera.main resolves.");
            Assert.That(camera.orthographic, Is.False,
                "The world is 3D (D15) — an orthographic camera throws away the depth cue D14 depends on.");
        }

        [Test]
        public void The_camera_looks_down_the_depth_axis_from_above()
        {
            Camera camera = TheCamera();
            Transform t = camera.transform;
            float pitch = t.eulerAngles.x > 180f ? t.eulerAngles.x - 360f : t.eulerAngles.x;

            Assert.That(pitch, Is.InRange(10f, 25f),
                $"Camera pitch is {pitch:0.#}°. A side-on brawler camera looks slightly down at the " +
                "play area — roughly 15–20°. Zero pitch shows the ground plane edge-on.");
            Assert.That(t.position.y, Is.InRange(3f, 12f),
                $"Camera height is {t.position.y:0.#}. It should sit above the action, not stand in it.");
            Assert.That(t.position.z, Is.LessThanOrEqualTo(-8f),
                "The camera sits back on -Z looking into the depth band.");
            Assert.That(t.forward.z, Is.GreaterThan(0f), "The camera must face +Z.");
        }

        [Test]
        public void Ground_covers_the_play_area_and_the_whole_depth_band()
        {
            GameObject ground = _scene.GetRootGameObjects().FirstOrDefault(o => o.name == "Ground");
            Assert.That(ground, Is.Not.Null, "No object named 'Ground' at the scene root.");

            Renderer renderer = ground.GetComponentInChildren<Renderer>();
            Assert.That(renderer, Is.Not.Null, "'Ground' has no renderer, so there is nothing to stand on.");

            Bounds bounds = renderer.bounds;
            Assert.That(bounds.size.x, Is.GreaterThanOrEqualTo(16f),
                $"Ground is {bounds.size.x:0.#} units wide; the camera frames about 16.");
            Assert.That(bounds.size.z, Is.GreaterThanOrEqualTo(6f),
                $"Ground is {bounds.size.z:0.#} units deep; the depth band is 6 and never changes (§2.1).");
            Assert.That(Mathf.Abs(bounds.center.y), Is.LessThan(0.5f),
                "The ground surface sits at y = 0 — the motor treats that as the ground plane.");
        }

        [Test]
        public void A_directional_light_casts_shadows()
        {
            List<Light> lights = AcceptanceFixture.FindAll<Light>(_scene)
                .Where(l => l.enabled && l.gameObject.activeInHierarchy)
                .ToList();

            Assert.That(lights.Count, Is.EqualTo(1), "Expected exactly one light in the scene.");
            Assert.That(lights[0].type, Is.EqualTo(LightType.Directional));
            Assert.That(lights[0].shadows, Is.Not.EqualTo(LightShadows.None),
                "Grounded shadows are the primary spatial cue (D14, §2.3), not decoration.");
        }

        [Test]
        public void One_simulation_driver_runs_the_scene()
        {
            List<SimulationDriver> drivers = AcceptanceFixture.FindAll<SimulationDriver>(_scene);

            Assert.That(drivers.Count, Is.EqualTo(1),
                "Exactly one SimulationDriver drives a scene — two would step the world twice.");
        }

        private Camera TheCamera()
        {
            Camera camera = AcceptanceFixture.FindAll<Camera>(_scene)
                .FirstOrDefault(c => c.enabled && c.gameObject.activeInHierarchy);

            Assert.That(camera, Is.Not.Null, "No active camera in the scene.");
            return camera;
        }
    }
}
