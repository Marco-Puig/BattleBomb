using System;
using BattleBomb.Core.Stats;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// One generated item — rolled values stored, never re-derived (save-shaped for M7). Core
    /// stats and affixes are immutable from the drop (D35); upgrade capacity waits for M6's
    /// spend flow; the required level is D36's lock.
    /// </summary>
    public readonly struct ItemInstance
    {
        private static readonly AffixRoll[] NoAffixes = Array.Empty<AffixRoll>();

        public readonly int DefinitionId;
        public readonly string DisplayName;
        public readonly ItemSlot Slot;
        public readonly WeaponClass WeaponClass;
        public readonly PetClass PetClass;
        public readonly QualityRank Quality;
        public readonly GearContribution CoreStats;
        public readonly AffixRoll[] Affixes;
        public readonly int RequiredLevel;
        public readonly int UpgradeCapacity;
        public readonly int UpgradesSpent;
        public readonly float ShotSpeed;
        public readonly float ConsumableHealFraction;

        public ItemInstance(
            int definitionId,
            string displayName,
            ItemSlot slot,
            WeaponClass weaponClass,
            PetClass petClass,
            QualityRank quality,
            in GearContribution coreStats,
            AffixRoll[] affixes,
            int requiredLevel,
            int upgradeCapacity,
            int upgradesSpent = 0,
            float shotSpeed = 0f,
            float consumableHealFraction = 0f)
        {
            DefinitionId = definitionId;
            DisplayName = displayName ?? string.Empty;
            Slot = slot;
            WeaponClass = weaponClass;
            PetClass = petClass;
            Quality = quality;
            CoreStats = coreStats;
            Affixes = affixes ?? NoAffixes;
            RequiredLevel = Math.Max(1, requiredLevel);
            UpgradeCapacity = Math.Max(0, upgradeCapacity);
            UpgradesSpent = Math.Max(0, upgradesSpent);
            ShotSpeed = shotSpeed;
            ConsumableHealFraction = consumableHealFraction;
        }

        public int AffixCount => Affixes.Length;

        public bool IsConsumable => Slot == ItemSlot.Consumable;

        /// <summary>The whole block this item hands the StatSheet: core stats plus every affix.</summary>
        public GearContribution TotalContribution()
        {
            GearContribution total = CoreStats;
            for (int i = 0; i < Affixes.Length; i++)
            {
                total += AffixEffects.Contribution(Affixes[i]);
            }

            return total;
        }
    }
}
