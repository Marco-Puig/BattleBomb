using System;
using System.Collections.Generic;
using System.Linq;
using BattleBomb.Gameplay.World.Markers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleBomb.Tests.EditMode.Acceptance
{
    /// <summary>
    /// D48 made mechanical: a stage scene is geometry and markers, nothing else. The moment
    /// someone drops a spawner or a script into a level scene, this fails — gameplay lives in
    /// stage data (rule 2), and endless mode's generator must be able to replace the scene.
    /// </summary>
    /// <remarks>
    /// The test that fails you names the offending object and component, so start there. If
    /// what you added belongs in a level scene, add it to <see cref="Allowed"/>; the rule for
    /// that list is that a component may only <em>look</em> or <em>sound</em>, never decide
    /// anything. A mesh, a light, a probe, an ambient loop: yes. Anything that spawns, tracks,
    /// scores, times, or reacts: no — that is stage data or the runner's job, and putting it
    /// here is the exact mistake D48 exists to prevent. Terrain is deliberately absent: a
    /// side-on brawler on a flat plane has no business with one, and a test asking "really?"
    /// is the right outcome.
    /// </remarks>
    public sealed class StageSceneAcceptanceTests
    {
        private const string StagesFolder = "Assets/_BattleBomb/Scenes/Stages";

        private static readonly Type[] Allowed =
        {
            typeof(Transform),
            typeof(MeshFilter),
            typeof(MeshRenderer),
            typeof(SkinnedMeshRenderer),
            typeof(Collider),
            typeof(Light),
            typeof(ParticleSystem),
            typeof(ParticleSystemRenderer),
            typeof(AudioSource),
            typeof(ReflectionProbe),
            typeof(LODGroup),
            typeof(ArenaMarker),
            typeof(CheckpointRoomMarker),
            typeof(StageExitMarker),
            typeof(PlayerSpawnMarker),
        };

        private static IEnumerable<string> StageScenes() =>
            AssetDatabase.FindAssets("t:Scene", new[] { StagesFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p);

        [Test]
        public void There_is_at_least_one_stage_scene()
        {
            Assert.That(StageScenes().Count(), Is.GreaterThanOrEqualTo(2), "The fixture chapter has two stages.");
        }

        [Test]
        public void Every_stage_scene_holds_only_geometry_and_markers([ValueSource(nameof(StageScenes))] string path)
        {
            Scene scene = AcceptanceFixture.OpenForInspection(path, out bool opened);
            try
            {
                var offenders = new List<string>();
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Component component in root.GetComponentsInChildren<Component>(true))
                    {
                        if (component == null)
                        {
                            offenders.Add(root.name + ": missing script");
                            continue;
                        }

                        Type type = component.GetType();
                        if (!Allowed.Any(a => a.IsAssignableFrom(type)))
                        {
                            offenders.Add(component.gameObject.name + ": " + type.Name);
                        }
                    }
                }

                Assert.That(offenders, Is.Empty,
                    "Stage scenes hold geometry and markers only (D48). Move gameplay into the stage " +
                    "asset and out of: " + string.Join(", ", offenders));
            }
            finally
            {
                AcceptanceFixture.CloseAfterInspection(scene, opened);
            }
        }

        [Test]
        public void Every_stage_scene_has_the_markers_the_runner_needs([ValueSource(nameof(StageScenes))] string path)
        {
            Scene scene = AcceptanceFixture.OpenForInspection(path, out bool opened);
            try
            {
                List<ArenaMarker> arenas = AcceptanceFixture.FindAll<ArenaMarker>(scene);
                Assert.That(arenas.Select(a => a.Index).OrderBy(i => i), Is.EqualTo(Enumerable.Range(0, arenas.Count)),
                    "Arena indices must run 0..n-1 with no gaps.");
                Assert.That(AcceptanceFixture.FindAll<StageExitMarker>(scene).Count, Is.EqualTo(1), "Exactly one exit.");
                Assert.That(AcceptanceFixture.FindAll<PlayerSpawnMarker>(scene).Count, Is.EqualTo(1), "Exactly one stage spawn.");

                foreach (CheckpointRoomMarker room in AcceptanceFixture.FindAll<CheckpointRoomMarker>(scene))
                {
                    Assert.That(room.AfterArena, Is.InRange(0, arenas.Count - 1),
                        $"Checkpoint room '{room.name}' follows arena {room.AfterArena}, which does not exist.");
                }
            }
            finally
            {
                AcceptanceFixture.CloseAfterInspection(scene, opened);
            }
        }

        [Test]
        public void Every_stage_scene_has_ground_under_the_whole_walk([ValueSource(nameof(StageScenes))] string path)
        {
            Scene scene = AcceptanceFixture.OpenForInspection(path, out bool opened);
            try
            {
                List<ArenaMarker> arenas = AcceptanceFixture.FindAll<ArenaMarker>(scene);
                StageExitMarker exit = AcceptanceFixture.FindAll<StageExitMarker>(scene).First();
                float minX = arenas.Min(a => a.MinX);
                float maxX = exit.X;

                Bounds ground = default;
                bool any = false;
                foreach (Renderer renderer in AcceptanceFixture.FindAll<Renderer>(scene))
                {
                    if (renderer.bounds.size.z >= 6f && Mathf.Abs(renderer.bounds.max.y) < 0.5f)
                    {
                        ground = any ? Encapsulate(ground, renderer.bounds) : renderer.bounds;
                        any = true;
                    }
                }

                Assert.That(any, Is.True, "No ground-sized renderer with its top at y = 0.");
                Assert.That(ground.min.x, Is.LessThanOrEqualTo(minX), "Ground starts before the first arena.");
                Assert.That(ground.max.x, Is.GreaterThanOrEqualTo(maxX), "Ground reaches the exit.");
            }
            finally
            {
                AcceptanceFixture.CloseAfterInspection(scene, opened);
            }
        }

        private static Bounds Encapsulate(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }
    }
}
