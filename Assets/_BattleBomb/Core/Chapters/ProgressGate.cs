using System.Collections.Generic;

namespace BattleBomb.Core.Chapters
{
    /// <summary>
    /// D50's unlock rules, light on purpose: chapter N opens when N−1 is beaten on any tier;
    /// tier T+1 of a chapter opens when it is beaten on tier T. A player may rush the newest
    /// chapter on Normal or grind an early one to Nightmare — both overreach paths are open.
    /// </summary>
    public static class ProgressGate
    {
        /// <summary>
        /// <paramref name="tierCount"/> bounds <paramref name="tierIndex"/>. It comes from the
        /// caller rather than <see cref="TierSpec.Defaults"/> because the tier list is data
        /// (D50 — a fourth tier is a row); the gate must agree with whatever list is in play,
        /// never assume the default three.
        /// </summary>
        public static bool IsUnlocked(
            StoryProgress progress, IReadOnlyList<ChapterSpec> chapters, int chapterIndex, int tierIndex,
            int tierCount)
        {
            if (progress == null || chapters == null
                || chapterIndex < 0 || chapterIndex >= chapters.Count
                || tierIndex < 0 || tierIndex >= tierCount)
            {
                return false;
            }

            if (chapterIndex > 0 && progress.HighestTierBeaten(chapters[chapterIndex - 1].Id) < 1)
            {
                return false;
            }

            return progress.HighestTierBeaten(chapters[chapterIndex].Id) >= tierIndex;
        }
    }
}
