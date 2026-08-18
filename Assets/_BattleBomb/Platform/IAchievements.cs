namespace BattleBomb.Platform
{
    /// <summary>Achievement unlocks and progress, addressed by a platform-independent id.</summary>
    public interface IAchievements
    {
        void Unlock(string achievementId);

        /// <summary>Reports progress towards an incremental achievement.</summary>
        void ReportProgress(string achievementId, int current, int target);

        bool IsUnlocked(string achievementId);
    }
}
