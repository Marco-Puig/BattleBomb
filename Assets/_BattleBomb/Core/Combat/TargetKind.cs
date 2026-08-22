namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// What a landed hit is landing on. Partners take knockback and nothing else (D21).
    /// </summary>
    public enum TargetKind
    {
        Enemy = 0,
        Partner = 1,
    }
}
