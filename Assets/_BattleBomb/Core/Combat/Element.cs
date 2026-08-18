namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// The elemental roster (§4). None is elementless kinetic damage — resistances and climates
    /// never touch it.
    /// </summary>
    public enum Element
    {
        None = 0,
        Fire = 1,
        Water = 2,
        Electric = 3,
        Earth = 4,
    }
}
