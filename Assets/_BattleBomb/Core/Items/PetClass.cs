namespace BattleBomb.Core.Items
{
    /// <summary>
    /// D34's three pet classes. M4 ships StatBoost end to end; Attacker (an ally brain) and
    /// Unique (bespoke hooks) are data now, behaviour later — their own design moments.
    /// </summary>
    public enum PetClass
    {
        None = 0,
        Attacker = 1,
        StatBoost = 2,
        Unique = 3,
    }
}
