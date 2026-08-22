namespace BattleBomb.Platform
{
    /// <summary>
    /// Everything the game asks of a storefront or console back end. Implemented by
    /// <c>SteamPlatformServices</c> and <see cref="NullPlatformServices"/>; a build with Steamworks
    /// entirely absent must still compile and run (§7).
    /// </summary>
    public interface IPlatformServices
    {
        /// <summary>False when running with no back end — callers must degrade, never branch on platform.</summary>
        bool IsAvailable { get; }

        IAchievements Achievements { get; }

        IPlayerIdentity Identity { get; }

        ILeaderboards Leaderboards { get; }

        /// <summary>Where saves go (D52). Local files when no platform is present.</summary>
        ISaveStore Saves { get; }

        /// <summary>Brings the back end up. Returns false if it is unavailable; that is not an error.</summary>
        bool Initialise();

        void Shutdown();
    }
}
