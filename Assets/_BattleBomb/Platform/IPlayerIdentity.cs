namespace BattleBomb.Platform
{
    /// <summary>Who the local player is according to the platform, if anyone.</summary>
    public interface IPlayerIdentity
    {
        bool IsSignedIn { get; }

        /// <summary>Platform-scoped id, empty when signed out.</summary>
        string UserId { get; }

        /// <summary>Display name, or a local fallback when signed out.</summary>
        string DisplayName { get; }
    }
}
