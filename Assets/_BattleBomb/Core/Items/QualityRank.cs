namespace BattleBomb.Core.Items
{
    /// <summary>
    /// D33's one ladder, nine ranks — the whole quality arc from trash to trophy. Progress and
    /// difficulty push drops up it; the rank indexes <see cref="QualityTable"/> for everything
    /// it grants.
    /// </summary>
    public enum QualityRank
    {
        Nothing = 0,
        Battlescarred = 1,
        Torn = 2,
        Rusty = 3,
        Shiny = 4,
        Pristine = 5,
        Legendary = 6,
        Mythical = 7,
        Godly = 8,
    }
}
