using System;
using System.Collections.Generic;

namespace BattleBomb.Platform
{
    /// <summary>
    /// The implementation used when no storefront is present — editor, tests, and any build without
    /// Steamworks. Everything succeeds locally and nothing throws, so no caller needs a null check (§7).
    /// </summary>
    public sealed class NullPlatformServices : IPlatformServices, IAchievements, IPlayerIdentity, ILeaderboards
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>();

        public bool IsAvailable => false;

        public IAchievements Achievements => this;

        public IPlayerIdentity Identity => this;

        public ILeaderboards Leaderboards => this;

        public bool Initialise() => false;

        public void Shutdown()
        {
        }

        // IAchievements — unlocks are remembered for the session so UI can reflect them.

        public void Unlock(string achievementId)
        {
            if (!string.IsNullOrEmpty(achievementId))
            {
                _unlocked.Add(achievementId);
            }
        }

        public void ReportProgress(string achievementId, int current, int target)
        {
            if (target > 0 && current >= target)
            {
                Unlock(achievementId);
            }
        }

        public bool IsUnlocked(string achievementId) => _unlocked.Contains(achievementId);

        // IPlayerIdentity

        public bool IsSignedIn => false;

        public string UserId => string.Empty;

        public string DisplayName => "Player";

        // ILeaderboards — accepted and discarded; callers see a clean failure, not an exception.

        public void SubmitScore(string boardId, long score, Action<bool> onComplete = null) => onComplete?.Invoke(false);

        public void FetchTop(string boardId, int count, Action<LeaderboardEntry[]> onComplete) =>
            onComplete?.Invoke(Array.Empty<LeaderboardEntry>());
    }
}
