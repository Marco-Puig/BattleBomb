namespace BattleBomb.Core.Items
{
    /// <summary>
    /// The D35 affix pool. The first block is live in M4; the M5-reserved block generates and
    /// displays from day one but contributes nothing until M5 wires elements — the same
    /// shape-exists-early pattern as the damage pipeline.
    /// </summary>
    public enum AffixId
    {
        CritChance = 0,
        CritDamage = 1,
        LifeSteal = 2,
        MaxHealth = 3,
        MaxMana = 4,
        ManaRegen = 5,
        ReducedWeight = 6,
        KnockbackPower = 7,

        MagicDamage = 100,
        MagicRange = 101,
        ElementalResistance = 102,
        WeaponInfusion = 103,
    }
}
