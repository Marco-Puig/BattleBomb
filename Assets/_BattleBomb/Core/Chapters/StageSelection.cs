using System.Collections.Generic;
using UnityEngine;

namespace BattleBomb.Core.Chapters
{
    /// <summary>
    /// The chapter picker's truth (D48): which chapter and tier are pointed at, whether they
    /// may launch, and where launching starts. M7 draws this as a list; the map Michael wants
    /// is another view over the same object.
    /// </summary>
    public sealed class StageSelection
    {
        private readonly IReadOnlyList<ChapterSpec> _chapters;
        private readonly IReadOnlyList<TierSpec> _tiers;
        private readonly StoryProgress _progress;

        public StageSelection(IReadOnlyList<ChapterSpec> chapters, IReadOnlyList<TierSpec> tiers, StoryProgress progress)
        {
            _chapters = chapters ?? new ChapterSpec[0];
            _tiers = tiers ?? TierSpec.Defaults;
            _progress = progress ?? new StoryProgress();
            ChapterIndex = 0;
            if (_progress.HasResume)
            {
                for (int i = 0; i < _chapters.Count; i++)
                {
                    if (_chapters[i].Id == _progress.ResumeChapterId)
                    {
                        ChapterIndex = i;
                        break;
                    }
                }
            }
        }

        public int ChapterIndex { get; private set; }
        public int TierIndex { get; private set; }

        public int ChapterCount => _chapters.Count;
        public int TierCount => _tiers.Count;

        public ChapterSpec SelectedChapter => _chapters.Count > 0 ? _chapters[ChapterIndex] : default;
        public TierSpec SelectedTier => _tiers.Count > 0 ? _tiers[TierIndex] : default;

        public bool IsUnlocked(int chapterIndex, int tierIndex) =>
            ProgressGate.IsUnlocked(_progress, _chapters, chapterIndex, tierIndex, _tiers.Count);

        public bool IsSelectionUnlocked => IsUnlocked(ChapterIndex, TierIndex);

        public bool CanLaunch => _chapters.Count > 0 && IsSelectionUnlocked && SelectedChapter.StageCount > 0;

        /// <summary>The chapter in progress resumes; any other starts from its first stage.</summary>
        public bool IsResuming => _progress.HasResume && SelectedChapter.Id == _progress.ResumeChapterId;

        public int LaunchStageIndex => IsResuming
            ? Mathf.Clamp(_progress.ResumeStageIndex, 0, Mathf.Max(0, SelectedChapter.StageCount - 1))
            : 0;

        public int LaunchCheckpointArena => IsResuming ? _progress.ResumeCheckpointArena : -1;

        public void MoveChapter(int delta) =>
            ChapterIndex = Mathf.Clamp(ChapterIndex + delta, 0, Mathf.Max(0, _chapters.Count - 1));

        public void MoveTier(int delta) =>
            TierIndex = Mathf.Clamp(TierIndex + delta, 0, Mathf.Max(0, _tiers.Count - 1));
    }
}
