using BattleBomb.Core.Stats;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// Maps a rolled affix onto the stat block combat reads. Two affixes deliberately map to
    /// nothing here: elemental resistance is per-element, so it aggregates outside this flat block
    /// (<see cref="BattleBomb.Core.Stats.StatSheet"/>), and weapon infusion is an element rather
    /// than a number — the weapon carries it to the hit.
    /// </summary>
    public static class AffixEffects
    {
        public static GearContribution Contribution(in AffixRoll affix)
        {
            switch (affix.Id)
            {
                case AffixId.MagicDamage: return new GearContribution(magicDamage: affix.Magnitude);
                case AffixId.MagicRange: return new GearContribution(magicRange: affix.Magnitude);
                case AffixId.CritChance: return new GearContribution(critChance: affix.Magnitude);
                case AffixId.CritDamage: return new GearContribution(critDamageBonus: affix.Magnitude);
                case AffixId.LifeSteal: return new GearContribution(lifeSteal: affix.Magnitude);
                case AffixId.MaxHealth: return new GearContribution(maxHealthBonus: affix.Magnitude);
                case AffixId.MaxMana: return new GearContribution(maxManaBonus: affix.Magnitude);
                case AffixId.ManaRegen: return new GearContribution(manaRegen: affix.Magnitude);
                case AffixId.ReducedWeight: return new GearContribution(weightReduction: affix.Magnitude);
                case AffixId.KnockbackPower: return new GearContribution(knockbackBonus: affix.Magnitude);
                default: return GearContribution.Zero;
            }
        }
    }
}
