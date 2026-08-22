namespace BattleBomb.Core.Stats
{
    /// <summary>
    /// The four base stats a player allocates level points into (D32). Everything else a build
    /// carries is a direct gear stat, never an attribute.
    /// </summary>
    public enum StatId
    {
        Strength = 0,
        Hp = 1,
        Mana = 2,
        Speed = 3,
    }
}
