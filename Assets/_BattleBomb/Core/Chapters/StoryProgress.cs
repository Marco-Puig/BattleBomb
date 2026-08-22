using System.Collections.Generic;
using UnityEngine;

namespace BattleBomb.Core.Chapters
{
    /// <summary>
    /// Story mode's own progress namespace (D4, D51): per chapter, how many tiers are beaten,
    /// and the one resume point. Mutable and Core — the save mapper copies it in and out.
    /// </summary>
    public sealed class StoryProgress
    {
        private readonly Dictionary<string, int> _tiersBeaten = new Dictionary<string, int>();

        public string ResumeChapterId { get; private set; } = string.Empty;
        public int ResumeStageIndex { get; private set; }
        public int ResumeCheckpointArena { get; private set; } = -1;

        public bool HasResume => ResumeChapterId.Length > 0;

        public IEnumerable<KeyValuePair<string, int>> TiersBeaten => _tiersBeaten;

        /// <summary>Tiers beaten on this chapter: beating tier index 2 means three.</summary>
        public int HighestTierBeaten(string chapterId) =>
            chapterId != null && _tiersBeaten.TryGetValue(chapterId, out int beaten) ? beaten : 0;

        public void RecordCompletion(string chapterId, int tierIndex)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                return;
            }

            int beaten = Mathf.Max(0, tierIndex) + 1;
            if (beaten > HighestTierBeaten(chapterId))
            {
                _tiersBeaten[chapterId] = beaten;
            }

            if (ResumeChapterId == chapterId)
            {
                ClearResume();
            }
        }

        public void SetResume(string chapterId, int stageIndex, int checkpointArena)
        {
            ResumeChapterId = chapterId ?? string.Empty;
            ResumeStageIndex = Mathf.Max(0, stageIndex);
            ResumeCheckpointArena = Mathf.Max(-1, checkpointArena);
        }

        public void ClearResume()
        {
            ResumeChapterId = string.Empty;
            ResumeStageIndex = 0;
            ResumeCheckpointArena = -1;
        }

        /// <summary>The save mapper's way in: a beaten count straight from the file.</summary>
        public void RestoreTiersBeaten(string chapterId, int beaten)
        {
            if (!string.IsNullOrEmpty(chapterId) && beaten > 0)
            {
                _tiersBeaten[chapterId] = beaten;
            }
        }
    }
}
