using BattleBomb.Core.Chapters;
using BattleBomb.Core.Combat;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>The chapter picker's model: a list now, the LittleBigPlanet map later (D48).</summary>
    public sealed class StageSelectionTests
    {
        private static ChapterSpec[] Three() => new[] { Chapter("c1"), Chapter("c2"), Chapter("c3") };

        private static ChapterSpec Chapter(string id) => new ChapterSpec(id, id, new[]
        {
            new StageSpec(id + "-s1", "One", "Geo", new ArenaSpec[0], 1, 1f, ElementalMultipliers.Neutral),
            new StageSpec(id + "-s2", "Two", "Geo", new ArenaSpec[0], 1, 1f, ElementalMultipliers.Neutral),
        });

        [Test]
        public void Selection_starts_on_the_first_chapter_and_tier_and_clamps_at_the_ends()
        {
            var selection = new StageSelection(Three(), TierSpec.Defaults, new StoryProgress());

            Assert.That(selection.ChapterIndex, Is.Zero);
            Assert.That(selection.TierIndex, Is.Zero);

            selection.MoveChapter(-1);
            Assert.That(selection.ChapterIndex, Is.Zero);
            selection.MoveChapter(1);
            selection.MoveChapter(1);
            selection.MoveChapter(1);
            Assert.That(selection.ChapterIndex, Is.EqualTo(2));
            selection.MoveTier(-1);
            Assert.That(selection.TierIndex, Is.Zero);
        }

        [Test]
        public void Locked_entries_can_be_looked_at_but_not_launched()
        {
            var selection = new StageSelection(Three(), TierSpec.Defaults, new StoryProgress());
            selection.MoveChapter(1);

            Assert.That(selection.IsSelectionUnlocked, Is.False);
            Assert.That(selection.CanLaunch, Is.False);

            selection.MoveChapter(-1);
            selection.MoveTier(1);
            Assert.That(selection.TierIndex, Is.EqualTo(1), "You may point at Hard…");
            Assert.That(selection.CanLaunch, Is.False, "…but not start it.");
        }

        [Test]
        public void A_resume_point_selects_its_chapter_and_launches_at_its_stage()
        {
            var progress = new StoryProgress();
            progress.RecordCompletion("c1", 0);
            progress.SetResume("c2", 1, 0);
            var selection = new StageSelection(Three(), TierSpec.Defaults, progress);

            Assert.That(selection.ChapterIndex, Is.EqualTo(1), "Continue lands on the chapter in progress.");
            Assert.That(selection.LaunchStageIndex, Is.EqualTo(1));
            Assert.That(selection.LaunchCheckpointArena, Is.EqualTo(0));

            selection.MoveChapter(-1);
            Assert.That(selection.LaunchStageIndex, Is.Zero, "Any other chapter starts from its first stage.");
            Assert.That(selection.LaunchCheckpointArena, Is.EqualTo(-1));
        }
    }
}
