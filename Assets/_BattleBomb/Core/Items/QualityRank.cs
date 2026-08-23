namespace BattleBomb.Core.Items
{
    /// <summary>
    /// D33's one ladder — the whole quality arc from trash to trophy. Progress and difficulty
    /// push drops up it; the rank indexes <see cref="QualityTable"/> for everything it grants.
    ///
    /// Nine ranks ship at launch, Nothing through Mythical. <see cref="Godly"/> is authored here
    /// so its name and colour exist and nothing has to be renumbered later, but it is deliberately
    /// outside <see cref="QualityTable.RankCount"/>: no drop rolls it and no combine promotes into
    /// it until that count grows. Which items may reach it is an open design question.
    /// </summary>
    public enum QualityRank
    {
        Nothing = 0,
        Battlescarred = 1,
        Rusty = 2,
        Torn = 3,
        Clean = 4,
        Shiny = 5,
        Pristine = 6,
        Legendary = 7,
        Mythical = 8,

        /// <summary>Reserved for a later release — see the type remarks. Not in the launch ladder.</summary>
        Godly = 9,
    }
}
