using System.Collections.Generic;
using BattleBomb.Core.Stats;

namespace BattleBomb.Core.Items
{
    /// <summary>The core stats a capacity point may deepen (D35's slot promise, minus the tax).</summary>
    public enum CoreStatId
    {
        None = 0,
        WeaponDamage = 1,
        SwingSpeed = 2,
        Defence = 3,
        ShotSpeed = 4,
    }

    /// <summary>
    /// One stat on one item that a capacity point can raise: a core stat, or the affix at this
    /// index. Never a stat the item does not already have — investment deepens the roll, it
    /// never adds to it (D35/D44).
    /// </summary>
    public readonly struct UpgradeTarget
    {
        public readonly CoreStatId CoreStat;
        public readonly int AffixIndex;

        private UpgradeTarget(CoreStatId coreStat, int affixIndex)
        {
            CoreStat = coreStat;
            AffixIndex = affixIndex;
        }

        public static UpgradeTarget Core(CoreStatId stat) => new UpgradeTarget(stat, -1);

        public static UpgradeTarget Affix(int index) => new UpgradeTarget(CoreStatId.None, index);

        public bool IsAffix => AffixIndex >= 0;

        public bool IsNothing => !IsAffix && CoreStat == CoreStatId.None;
    }

    /// <summary>
    /// D44's guaranteed half. A capacity point raises one stat the item already has by
    /// <see cref="StepFraction"/> of its current value; the money cost doubles per point
    /// (<see cref="PriceBook"/>), so capacity gates the total and money gates the pace.
    /// </summary>
    public static class ItemUpgrade
    {
        /// <summary>What one point adds, as a share of the stat's current value.</summary>
        public const float StepFraction = 0.08f;

        /// <summary>
        /// Every stat this item can deepen, in a stable order: core stats first, then affixes in
        /// roll order. Consumables and stats the item rolled at zero never appear.
        /// </summary>
        public static int Targets(in ItemInstance item, IList<UpgradeTarget> buffer)
        {
            if (buffer == null)
            {
                return 0;
            }

            buffer.Clear();
            if (item.IsEmpty || item.IsConsumable)
            {
                return 0;
            }

            GearContribution core = item.CoreStats;
            if (core.WeaponDamage > 0f)
            {
                buffer.Add(UpgradeTarget.Core(CoreStatId.WeaponDamage));
            }

            if (core.SwingSpeedBonus > 0f)
            {
                buffer.Add(UpgradeTarget.Core(CoreStatId.SwingSpeed));
            }

            if (core.Defence > 0f)
            {
                buffer.Add(UpgradeTarget.Core(CoreStatId.Defence));
            }

            if (item.ShotSpeed > 0f)
            {
                buffer.Add(UpgradeTarget.Core(CoreStatId.ShotSpeed));
            }

            for (int i = 0; i < item.AffixCount; i++)
            {
                buffer.Add(UpgradeTarget.Affix(i));
            }

            return buffer.Count;
        }

        /// <summary>Capacity left to spend — the gate money cannot open.</summary>
        public static bool CanUpgrade(in ItemInstance item) =>
            !item.IsEmpty && !item.IsConsumable && item.Investment.Remaining > 0;

        /// <summary>
        /// Spends one point into this stat. Returns false and leaves the item alone when the
        /// capacity is gone or the target is not a stat this item has — the caller has already
        /// taken the money, so a refusal must never silently swallow it.
        /// </summary>
        public static bool TryApply(in ItemInstance item, in UpgradeTarget target, out ItemInstance upgraded)
        {
            upgraded = item;
            if (!CanUpgrade(item) || target.IsNothing)
            {
                return false;
            }

            GearContribution core = item.CoreStats;
            AffixRoll[] affixes = item.Affixes;
            float shotSpeed = item.ShotSpeed;

            if (target.IsAffix)
            {
                if (target.AffixIndex >= item.AffixCount)
                {
                    return false;
                }

                affixes = (AffixRoll[])item.Affixes.Clone();
                AffixRoll roll = affixes[target.AffixIndex];
                affixes[target.AffixIndex] = new AffixRoll(
                    roll.Id, Raised(roll.Magnitude), roll.Element);
            }
            else
            {
                switch (target.CoreStat)
                {
                    case CoreStatId.WeaponDamage when core.WeaponDamage > 0f:
                        core = WithWeaponDamage(core, Raised(core.WeaponDamage));
                        break;
                    case CoreStatId.SwingSpeed when core.SwingSpeedBonus > 0f:
                        core = WithSwingSpeed(core, Raised(core.SwingSpeedBonus));
                        break;
                    case CoreStatId.Defence when core.Defence > 0f:
                        core = WithDefence(core, Raised(core.Defence));
                        break;
                    case CoreStatId.ShotSpeed when shotSpeed > 0f:
                        shotSpeed = Raised(shotSpeed);
                        break;
                    default:
                        return false;
                }
            }

            upgraded = new ItemInstance(
                item.Identity,
                item.Quality,
                core,
                affixes,
                item.RequiredLevel,
                item.Investment.WithSpent(item.UpgradesSpent + 1),
                shotSpeed,
                item.Consumable,
                item.Active);
            return true;
        }

        private static float Raised(float value) => value * (1f + StepFraction);

        private static GearContribution WithWeaponDamage(in GearContribution block, float value) =>
            new GearContribution(
                value, block.SwingSpeedBonus, block.Defence, block.Weight, block.CritChance,
                block.CritDamageBonus, block.LifeSteal, block.MaxHealthBonus, block.MaxManaBonus,
                block.ManaRegen, block.WeightReduction, block.KnockbackBonus, block.MagicDamage,
                block.MagicRange);

        private static GearContribution WithSwingSpeed(in GearContribution block, float value) =>
            new GearContribution(
                block.WeaponDamage, value, block.Defence, block.Weight, block.CritChance,
                block.CritDamageBonus, block.LifeSteal, block.MaxHealthBonus, block.MaxManaBonus,
                block.ManaRegen, block.WeightReduction, block.KnockbackBonus, block.MagicDamage,
                block.MagicRange);

        private static GearContribution WithDefence(in GearContribution block, float value) =>
            new GearContribution(
                block.WeaponDamage, block.SwingSpeedBonus, value, block.Weight, block.CritChance,
                block.CritDamageBonus, block.LifeSteal, block.MaxHealthBonus, block.MaxManaBonus,
                block.ManaRegen, block.WeightReduction, block.KnockbackBonus, block.MagicDamage,
                block.MagicRange);
    }
}
