using BattleBomb.Core.Chapters;
using BattleBomb.Gameplay.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>The fixture chapter is the machine's proof, so its authoring is checked like
    /// code: two stages, the shapes Task 78 and the smoke suite rely on, and a runtime struct
    /// that carries every field across.</summary>
    public sealed class StageDefinitionTests
    {
        private const string ChapterPath = "Assets/_BattleBomb/Data/Chapters/Fixture/FixtureChapter.asset";
        private const string TierFolder = "Assets/_BattleBomb/Data/Tiers/";

        [Test]
        public void The_fixture_chapter_has_two_stages_with_the_planned_shape()
        {
            var chapter = AssetDatabase.LoadAssetAtPath<ChapterDefinition>(ChapterPath);
            Assert.That(chapter, Is.Not.Null, "No fixture chapter at " + ChapterPath);

            ChapterSpec spec = chapter.ToRuntime();
            Assert.That(spec.Id, Is.EqualTo("fixture"));
            Assert.That(spec.StageCount, Is.EqualTo(2));

            StageSpec one = spec.Stages[0];
            Assert.That(one.GeometryScene, Is.EqualTo("FixtureStage1"));
            Assert.That(one.ArenaCount, Is.EqualTo(2));
            Assert.That(one.Arenas[0].CheckpointAfter, Is.True, "A mid-stage checkpoint after arena one.");
            Assert.That(one.Arenas[0].ShopkeeperAfter, Is.False);
            Assert.That(one.Arenas[1].CheckpointAfter, Is.True, "The stage's end is an airlock room.");
            Assert.That(one.Arenas[1].ShopkeeperAfter, Is.True);
            Assert.That(one.Arenas[0].WaveCount, Is.EqualTo(2));
            Assert.That(one.Arenas[0].Waves[1].WaitsForPreviousWave, Is.True);
            Assert.That(one.Arenas[1].Waves[1].EnemyIndex, Is.EqualTo(1),
                "The fixture's last fight must draw from roster slot 1, or nothing proves the " +
                "runner's index-to-definition mapping works past the trivial case — an off-by-one " +
                "there would spawn the wrong enemy with every test still green.");

            StageSpec two = spec.Stages[1];
            Assert.That(two.GeometryScene, Is.EqualTo("FixtureStage2"));
            Assert.That(two.ArenaCount, Is.EqualTo(3));
            Assert.That(two.LevelStamp, Is.GreaterThan(one.LevelStamp), "Later stage, higher stamp.");
            Assert.That(two.LootProgress, Is.GreaterThan(one.LootProgress));
            Assert.That(two.Arenas[2].CheckpointAfter, Is.True);
        }

        [Test]
        public void Every_stage_names_a_geometry_scene_that_is_in_the_build()
        {
            var chapter = AssetDatabase.LoadAssetAtPath<ChapterDefinition>(ChapterPath);
            foreach (StageSpec stage in chapter.ToRuntime().Stages)
            {
                bool inBuild = false;
                foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                {
                    if (scene.enabled && scene.path.EndsWith("/" + stage.GeometryScene + ".unity"))
                    {
                        inBuild = true;
                    }
                }

                Assert.That(inBuild, Is.True, $"Stage '{stage.Id}' names scene '{stage.GeometryScene}', which is not an enabled build scene.");
            }
        }

        [Test]
        public void The_three_tier_assets_match_the_paper_rows()
        {
            string[] names = { "Normal", "Hard", "Nightmare" };
            for (int i = 0; i < names.Length; i++)
            {
                var tier = AssetDatabase.LoadAssetAtPath<TierDefinition>(TierFolder + names[i] + ".asset");
                Assert.That(tier, Is.Not.Null, "Missing tier asset " + names[i]);

                TierSpec spec = tier.ToRuntime();
                TierSpec paper = TierSpec.Defaults[i];
                Assert.That(spec.Name, Is.EqualTo(paper.Name));
                Assert.That(spec.HealthMultiplier, Is.EqualTo(paper.HealthMultiplier).Within(0.001f));
                Assert.That(spec.DamageMultiplier, Is.EqualTo(paper.DamageMultiplier).Within(0.001f));
                Assert.That(spec.LevelBump, Is.EqualTo(paper.LevelBump));
                Assert.That(spec.LootMultiplier, Is.EqualTo(paper.LootMultiplier).Within(0.001f));
            }
        }
    }
}
