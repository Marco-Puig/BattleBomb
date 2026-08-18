using System;

namespace BattleBomb.Platform
{
    /// <summary>
    /// Leaderboard submission and retrieval. Callback-shaped rather than async so callers do not have
    /// to change structure when no back end is present and results arrive immediately.
    /// </summary>
    public interface ILeaderboards
    {
        void SubmitScore(string boardId, long score, Action<bool> onComplete = null);

        void FetchTop(string boardId, int count, Action<LeaderboardEntry[]> onComplete);
    }

    /// <summary>One row of a leaderboard.</summary>
    public readonly struct LeaderboardEntry
    {
        public readonly int Rank;
        public readonly string DisplayName;
        public readonly long Score;

        public LeaderboardEntry(int rank, string displayName, long score)
        {
            Rank = rank;
            DisplayName = displayName;
            Score = score;
        }
    }
}
