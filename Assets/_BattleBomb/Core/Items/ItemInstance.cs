using System;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Stats;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// One generated item — rolled values stored, never re-derived (save-shaped for M7). Core
    /// stats and affixes are immutable from the drop (D35); investment carries the spend flow
    /// and D43's lock; the required level is D36's gate. Grouped construction is task 61 paying
    /// the M5 close-out's sixteen-parameter debt.
    /// </summary>
    public readonly struct ItemInstance
    {
        public readonly ItemIdentity Identity;
        public readonly QualityRank Quality;
        public readonly GearContribution CoreStats;
        public readonly AffixRoll[] Affixes;
        public readonly int RequiredLevel;
        public readonly ItemInvestment Investment;
        public readonly float ShotSpeed;
        public readonly RestorePayload Consumable;
        public readonly ActivePayload Active;

        public ItemInstance(
            in ItemIdentity identity,
            QualityRank quality,
            in GearContribution coreStats,
            AffixRoll[] affixes,
            int requiredLevel,
            in ItemInvestment investment = default,
            float shotSpeed = 0f,
            in RestorePayload consumable = default,
            in ActivePayload active = default)
        {
            Identity = identity;
            Quality = quality;
            CoreStats = coreStats;
            Affixes = affixes ?? Array.Empty<AffixRoll>();
            RequiredLevel = Math.Max(1, requiredLevel);
            Investment = investment;
            ShotSpeed = shotSpeed;
            Consumable = consumable;
            Active = active;
        }

        public int DefinitionId => Identity.DefinitionId;

        public string DisplayName => Identity.Name ?? string.Empty;

        public ItemSlot Slot => Identity.Slot;

        public WeaponClass WeaponClass => Identity.WeaponClass;

        public PetClass PetClass => Identity.PetClass;

        public int UpgradeCapacity => Investment.Capacity;

        public int UpgradesSpent => Investment.Spent;

        /// <summary>A locked item refuses every selling and combining path (D43).</summary>
        public bool Locked => Investment.Locked;

        public float ConsumableHealFraction => Consumable.Fraction;

        /// <summary>Which pool this consumable refills (D27).</summary>
        public RestoreKind Restores => Consumable.Kind;

        public float ActiveWeaponDamageShare => Active.WeaponDamageShare;

        public ElementId ActiveElement => Active.Element;

        public float ActiveRadius => Active.Radius;

        public int ActiveCooldownSteps => Active.CooldownSteps;

        public int AffixCount => Affixes != null ? Affixes.Length : 0;

        public bool IsConsumable => Slot == ItemSlot.Consumable;

        /// <summary>True for the rare equipment piece the quick slot can fire (D37).</summary>
        public bool HasActive => Active.Exists;

        /// <summary>A default-constructed instance never went through the generator — the "no item" value.</summary>
        public bool IsEmpty => Affixes == null;

        /// <summary>The same item with its lock toggled — everything rolled stays identical.</summary>
        public ItemInstance WithLock(bool locked) => new ItemInstance(
            Identity, Quality, CoreStats, Affixes, RequiredLevel,
            Investment.WithLock(locked), ShotSpeed, Consumable, Active);

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
