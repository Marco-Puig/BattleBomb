namespace BattleBomb.Core.Items
{
    /// <summary>
    /// Where an item lives when it isn't in the bag (D34): the five gear slots, the worn
    /// equipment pair, and the stacking consumables the quick-use slot fires.
    /// </summary>
    public enum ItemSlot
    {
        Helmet = 0,
        Chest = 1,
        Boots = 2,
        Weapon = 3,
        Pet = 4,
        Equipment = 5,
        Consumable = 6,
    }
}
