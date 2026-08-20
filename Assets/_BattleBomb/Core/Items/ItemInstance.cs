using System;
using BattleBomb.Core.Combat;
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

        /// <summary>Which pool this consumable refills (D27).</summary>
        public readonly RestoreKind Restores;

        /// <summary>An equipment active's payload (D37), zero on everything passive.</summary>
        public readonly float ActiveWeaponDamageShare;
        public readonly ElementId ActiveElement;
        public readonly float ActiveRadius;
        public readonly int ActiveCooldownSteps;

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
            float consumableHealFraction = 0f,
            RestoreKind restores = RestoreKind.Health,
            float activeWeaponDamageShare = 0f,
            ElementId activeElement = default,
            float activeRadius = 0f,
            int activeCooldownSteps = 0)
        {
            Restores = restores;
            ActiveWeaponDamageShare = activeWeaponDamageShare;
            ActiveElement = activeElement;
            ActiveRadius = activeRadius;
            ActiveCooldownSteps = activeCooldownSteps;
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

        public int AffixCount => Affixes != null ? Affixes.Length : 0;

        public bool IsConsumable => Slot == ItemSlot.Consumable;

        /// <summary>True for the rare equipment piece the quick slot can fire (D37).</summary>
        public bool HasActive => ActiveWeaponDamageShare > 0f;

        /// <summary>A default-constructed instance never went through the generator — the "no item" value.</summary>
        public bool IsEmpty => Affixes == null;

        /// <summary>The whole block this item hands the StatSheet: core stats plus every affix.</summary>
        public GearContribution TotalContribution()
        {
            GearContribution total = CoreStats;
            for (int i = 0; i < AffixCount; i++)
            {
                total += AffixEffects.Contribution(Affixes[i]);
            }

            return total;
        }
    }
}
