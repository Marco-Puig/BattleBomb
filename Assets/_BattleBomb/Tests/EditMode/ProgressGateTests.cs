using BattleBomb.Core.Chapters;
using BattleBomb.Core.Combat;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>D50's gate: light on purpose, so players can overreach — but never past the frontier.</summary>
    public sealed class ProgressGateTests
    {
        private static ChapterSpec[] Three() => new[]
        {
            Chapter("c1"), Chapter("c2"), Chapter("c3"),
        };

        private static ChapterSpec Chapter(string id) => new ChapterSpec(id, id.ToUpper(), new[]
        {
            new StageSpec(id + "-s1", "One", "Geo", new ArenaSpec[0], 1, 1f, ElementalMultipliers.Neutral),
        });

        [Test]
        public void The_first_chapter_on_the_first_tier_is_always_open()
        {
            var progress = new StoryProgress();

            Assert.That(ProgressGate.IsUnlocked(progress, Three(), 0, 0, TierSpec.Defaults.Length), Is.True);
            Assert.That(ProgressGate.IsUnlocked(progress, Three(), 0, 1, TierSpec.Defaults.Length), Is.False, "Hard waits on a Normal clear.");
            Assert.That(ProgressGate.IsUnlocked(progress, Three(), 1, 0, TierSpec.Defaults.Length), Is.False, "Chapter two waits on chapter one.");
        }

        [Test]
        public void Beating_a_chapter_on_any_tier_opens_the_next_chapters_first_tier()
        {
            var progress = new StoryProgress();
            progress.RecordCompletion("c1", tierIndex: 0);

            Assert.That(ProgressGate.IsUnlocked(progress, Three(), 1, 0, TierSpec.Defaults.Length), Is.True);
            Assert.That(ProgressGate.IsUnlocked(progress, Three(), 1, 1, TierSpec.Defaults.Length), Is.False);
            Assert.That(ProgressGate.IsUnlocked(progress, Three(), 2, 0, TierSpec.Defaults.Length), Is.False, "Not two chapters ahead.");
        }

        [Test]
        public void Beating_a_tier_opens_the_next_tier_of_the_same_chapter_only()
        {
            var progress = new StoryProgress();
            progress.RecordCompletion("c1", 0);
            progress.RecordCompletion("c1", 1);

            Assert.That(ProgressGate.IsUnlocked(progress, Three(), 0, 2, TierSpec.Defaults.Length), Is.True, "Nightmare on chapter one.");
            Assert.That(ProgressGate.IsUnlocked(progress, Three(), 1, 1, TierSpec.Defaults.Length), Is.False, "Chapter two is still on Normal.");
        }

        [Test]
        public void Completion_never_goes_backwards_and_skipping_a_tier_counts_the_ones_below()
        {
            var progress = new StoryProgress();
            progress.RecordCompletion("c1", 2);
            progress.RecordCompletion("c1", 0);

            Assert.That(progress.HighestTierBeaten("c1"), Is.EqualTo(3), "Tier index 2 beaten means three tiers.");
            Assert.That(ProgressGate.IsUnlocked(progress, Three(), 0, 2, TierSpec.Defaults.Length), Is.True);
        }

        [Test]
        public void Nothing_outside_the_catalog_is_ever_unlocked()
        {
            var progress = new StoryProgress();

            Assert.That(ProgressGate.IsUnlocked(progress, Three(), 3, 0, TierSpec.Defaults.Length), Is.False);
            Assert.That(ProgressGate.IsUnlocked(progress, Three(), -1, 0, TierSpec.Defaults.Length), Is.False);
            Assert.That(ProgressGate.IsUnlocked(progress, Three(), 0, -1, TierSpec.Defaults.Length), Is.False);
            Assert.That(ProgressGate.IsUnlocked(progress, Three(), 0, TierSpec.Defaults.Length, TierSpec.Defaults.Length), Is.False);
        }

        [Test]
        public void The_bound_follows_the_callers_tier_count_not_the_default_three()
        {
            var progress = new StoryProgress();
            ChapterSpec[] chapters = Three();
            progress.RecordCompletion("c1", 0);
            progress.RecordCompletion("c1", 1);

            Assert.That(ProgressGate.IsUnlocked(progress, chapters, 0, 2, tierCount: 2), Is.False,
                "Beaten enough for index 2 under the default three, but a two-tier list has no index 2.");

            progress.RecordCompletion("c1", 2);
            Assert.That(ProgressGate.IsUnlocked(progress, chapters, 0, 3, tierCount: 4), Is.True,
                "A four-tier list allows index 3, which the default three-row bound would have rejected.");
            Assert.That(ProgressGate.IsUnlocked(progress, chapters, 0, 4, tierCount: 4), Is.False,
                "Still bounded by the length of the list actually in play.");
        }

        [Test]
        public void The_resume_point_is_one_place_and_completing_a_chapter_clears_it()
        {
            var progress = new StoryProgress();
            progress.SetResume("c2", stageIndex: 1, checkpointArena: 0);

            Assert.That(progress.HasResume, Is.True);
            Assert.That(progress.ResumeChapterId, Is.EqualTo("c2"));
            Assert.That(progress.ResumeStageIndex, Is.EqualTo(1));
            Assert.That(progress.ResumeCheckpointArena, Is.EqualTo(0));

            progress.RecordCompletion("c2", 0);
            Assert.That(progress.HasResume, Is.False, "A finished chapter has nowhere to resume.");
        }
    }
}
