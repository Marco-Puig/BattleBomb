using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleBomb.Core.Chapters;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.World;
using BattleBomb.Gameplay.World.Markers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleBomb.Tests.EditMode.Acceptance
{
    /// <summary>
    /// Where a chapter's data and its geometry have to agree (D48). A stage asset says how many
    /// arenas there are and which of them end in a checkpoint; a stage scene says where those
    /// arenas and rooms physically are. Nothing at runtime reconciles the two — the runner
    /// trusts the asset and reads the scene — so a disagreement is not an exception, it is a
    /// stage that quietly cannot be finished.
    /// </summary>
    /// <remarks>
    /// Every check here is asset inspection, deliberately: MonoBehaviour lifecycle does not run
    /// in EditMode, so this can hold hand-authored chapters to their contract without a single
    /// frame of play. Each failure names the stage and what disagrees with what.
    /// </remarks>
    public sealed class ChapterAuthoringAcceptanceTests
    {
        /// <summary>One authored stage, resolved lazily so the value source stays cheap and the
        /// test name reads as the chapter and stage a human would look for.</summary>
        public sealed class AuthoredStage
        {
            internal string ChapterPath { get; set; }

            internal int Index { get; set; }

            internal ChapterDefinition Chapter =>
                AssetDatabase.LoadAssetAtPath<ChapterDefinition>(ChapterPath);

            internal StageDefinition Definition => Chapter.StageAt(Index);

            public override string ToString() =>
                $"{Path.GetFileNameWithoutExtension(ChapterPath)} stage {Index}";
        }

        private static IEnumerable<AuthoredStage> AuthoredStages()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ChapterDefinition"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ChapterDefinition chapter = AssetDatabase.LoadAssetAtPath<ChapterDefinition>(path);
                if (chapter == null)
                {
                    continue;
                }

                for (int i = 0; i < chapter.Stages.Count; i++)
                {
                    if (chapter.StageAt(i) != null)
                    {
                        yield return new AuthoredStage { ChapterPath = path, Index = i };
                    }
                }
            }
        }

        [Test]
        public void Every_stage_names_a_geometry_scene_that_ships(
            [ValueSource(nameof(AuthoredStages))] AuthoredStage stage)
        {
            string sceneName = stage.Definition.ToRuntime().GeometryScene;
            string path = ScenePathNamed(sceneName);

            Assert.That(path, Is.Not.Null,
                $"{stage} names geometry scene '{sceneName}', and no scene by that name exists. " +
                "The runner resolves it by name at launch, so a typo here is not a compile error — " +
                "it is a stage that never appears and a game that sits there.");

            EditorBuildSettingsScene entry =
                EditorBuildSettings.scenes.FirstOrDefault(s => s.path == path);

            Assert.That(entry, Is.Not.Null,
                $"{stage}'s scene '{sceneName}' is not in Build Settings. LoadSceneAsync returns " +
                "null for a scene that is not in the build, so this stage cannot stream.");
            Assert.That(entry.enabled, Is.True,
                $"{stage}'s scene '{sceneName}' is in Build Settings but disabled, which is the " +
                "same thing as missing as far as the runner is concerned.");
        }

        [Test]
        public void Every_stage_but_the_last_ends_on_a_checkpoint(
            [ValueSource(nameof(AuthoredStages))] AuthoredStage stage)
        {
            StageSpec spec = stage.Definition.ToRuntime();
            bool isLastStage = stage.Index == stage.Chapter.Stages.Count - 1;
            bool endsOnCheckpoint =
                spec.ArenaCount > 0 && spec.Arenas[spec.ArenaCount - 1].CheckpointAfter;

            Assert.That(isLastStage || endsOnCheckpoint, Is.True,
                $"{stage} has another stage after it, but its last arena ends with no checkpoint " +
                "room. That room is the airlock (D48): it is where the next stage streams in " +
                "behind the players, where the chest that pays for the last fight sits (D42), and " +
                "where a wipe puts them back (D49). Without it the stage runs straight from its " +
                "last fight into the next one with none of that.");
        }

        [Test]
        public void Every_stage_scene_has_one_arena_marker_per_authored_arena(
            [ValueSource(nameof(AuthoredStages))] AuthoredStage stage)
        {
            Inspect(stage, (scene, spec) =>
            {
                List<ArenaMarker> arenas = AcceptanceFixture.FindAll<ArenaMarker>(scene);

                Assert.That(arenas.Count, Is.EqualTo(spec.ArenaCount),
                    $"{stage} authors {spec.ArenaCount} arena(s) but its scene has " +
                    $"{arenas.Count} arena marker(s). The asset decides how many fights there " +
                    "are and the scene only says where they happen, so a missing marker is an " +
                    "arena the players can never be clamped to, and a spare one is geometry " +
                    "nothing will ever use.");
            });
        }

        [Test]
        public void Every_authored_checkpoint_has_exactly_one_room_and_no_room_stands_alone(
            [ValueSource(nameof(AuthoredStages))] AuthoredStage stage)
        {
            Inspect(stage, (scene, spec) =>
            {
                List<CheckpointRoomMarker> rooms =
                    AcceptanceFixture.FindAll<CheckpointRoomMarker>(scene);

                for (int i = 0; i < spec.ArenaCount; i++)
                {
                    int markers = rooms.Count(r => r.AfterArena == i);
                    if (spec.Arenas[i].CheckpointAfter)
                    {
                        Assert.That(markers, Is.EqualTo(1),
                            $"{stage} ends arena {i} with a checkpoint, and its scene has " +
                            $"{markers} room marker(s) for it. The runner puts the chest where " +
                            "the marker is; with none there is no chest, and with two there are " +
                            "two chests and only one of them is inside the clamp.");
                    }
                    else
                    {
                        Assert.That(markers, Is.Zero,
                            $"{stage}'s scene has a checkpoint room after arena {i}, but the " +
                            "stage asset does not end that arena with one — so the room will be " +
                            "left empty and the gate past it will not stop there.");
                    }
                }

                int orphans = rooms.Count(r => r.AfterArena < 0 || r.AfterArena >= spec.ArenaCount);
                Assert.That(orphans, Is.Zero,
                    $"{stage} has {orphans} checkpoint room(s) pointing at an arena the stage " +
                    "asset does not have.");
            });
        }

        [Test]
        public void Every_stage_scene_opens_its_first_arena_where_the_runner_streams_it(
            [ValueSource(nameof(AuthoredStages))] AuthoredStage stage)
        {
            Inspect(stage, (scene, spec) =>
            {
                ArenaMarker first = AcceptanceFixture.FindAll<ArenaMarker>(scene)
                    .FirstOrDefault(a => a.Index == 0);

                Assert.That(first, Is.Not.Null, $"{stage}'s scene has no arena 0.");
                Assert.That(first.MinX, Is.EqualTo(StageRunner.AuthoredFirstArenaMinX).Within(0.001f),
                    $"{stage} opens its first arena at x = {first.MinX:0.##}, and stage scenes are " +
                    $"authored to open at {StageRunner.AuthoredFirstArenaMinX:0.##}. The runner " +
                    "measures the real marker when it streams a stage, so this will still line up " +
                    "at runtime — but every hand-authored number in the chapter assumes the " +
                    "convention, and one scene quietly off it is how those numbers stop meaning " +
                    "what they say.");
            });
        }

        [Test]
        public void No_stage_scene_lays_ground_past_its_own_exit(
            [ValueSource(nameof(AuthoredStages))] AuthoredStage stage)
        {
            Inspect(stage, (scene, spec) =>
            {
                StageExitMarker exit = AcceptanceFixture.FindAll<StageExitMarker>(scene).FirstOrDefault();
                Assert.That(exit, Is.Not.Null, $"{stage}'s scene has no exit marker.");

                float groundMaxX = float.MinValue;
                foreach (Renderer renderer in AcceptanceFixture.FindAll<Renderer>(scene))
                {
                    // The same shape the stage-scene fixture calls ground: wide enough to cover
                    // the depth band, with its top surface at the walking plane.
                    if (renderer.bounds.size.z >= 6f && Mathf.Abs(renderer.bounds.max.y) < 0.5f)
                    {
                        groundMaxX = Mathf.Max(groundMaxX, renderer.bounds.max.x);
                    }
                }

                Assert.That(groundMaxX, Is.GreaterThan(float.MinValue),
                    $"{stage}'s scene has no ground-sized renderer at the walking plane.");
                Assert.That(groundMaxX, Is.LessThanOrEqualTo(exit.X + 0.01f),
                    $"{stage} lays ground out to x = {groundMaxX:0.##} while its exit is at " +
                    $"{exit.X:0.##}. The next stage streams in so that its first arena begins at " +
                    "that exit, and its floor comes with it — every unit of overhang is two floors " +
                    "occupying the same plane. They do not blend: they fight, across the whole " +
                    "width of the band, in the checkpoint room the players stand in to sell.");
            });
        }

        private static void Inspect(AuthoredStage stage, System.Action<Scene, StageSpec> check)
        {
            StageSpec spec = stage.Definition.ToRuntime();
            string path = ScenePathNamed(spec.GeometryScene);
            Assert.That(path, Is.Not.Null,
                $"{stage} names geometry scene '{spec.GeometryScene}', which does not exist.");

            Scene scene = AcceptanceFixture.OpenForInspection(path, out bool opened);
            try
            {
                check(scene, spec);
            }
            finally
            {
                AcceptanceFixture.CloseAfterInspection(scene, opened);
            }
        }

        private static string ScenePathNamed(string sceneName) =>
            AssetDatabase.FindAssets("t:Scene")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == sceneName);
    }
}
