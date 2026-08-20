using BattleBomb.Core.Combat;
using BattleBomb.Core.Stats;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// One authored item definition as the generator sees it — the `.ToRuntime()` face of the
    /// ScriptableObject, like every other authored thing. The base stat block is what quality
    /// scales; the name is what quality prefixes.
    /// </summary>
    public readonly struct ItemSpec
    {
        public readonly int Id;
        public readonly string Name;
        public readonly ItemSlot Slot;
        public readonly WeaponClass WeaponClass;
        public readonly PetClass PetClass;
        public readonly GearContribution BaseStats;
        public readonly float ShotSpeed;
        public readonly float ConsumableHealFraction;

        /// <summary>Which pool this consumable refills — health potions and mana potions differ
        /// only in this field and their essence name.</summary>
        public readonly RestoreKind Restores;

        /// <summary>An equipment active's damage as a share of the wearer's weapon damage (D37).
        /// Zero for the passive majority.</summary>
        public readonly float ActiveWeaponDamageShare;

        /// <summary>The active's element, and the radius its burst covers.</summary>
        public readonly ElementId ActiveElement;
        public readonly float ActiveRadius;

        /// <summary>The active's own cooldown in steps — a moment, never a rotation (D19/D37).</summary>
        public readonly int ActiveCooldownSteps;

        public ItemSpec(
            int id,
            string name,
            ItemSlot slot,
            GearContribution baseStats,
            WeaponClass weaponClass = WeaponClass.None,
            PetClass petClass = PetClass.None,
            float shotSpeed = 0f,
            float consumableHealFraction = 0f,
            RestoreKind restores = RestoreKind.Health,
            float activeWeaponDamageShare = 0f,
            ElementId activeElement = default,
            float activeRadius = 0f,
            int activeCooldownSteps = 0)
        {
            Id = id;
            Name = name ?? string.Empty;
            Slot = slot;
            WeaponClass = weaponClass;
            PetClass = petClass;
            BaseStats = baseStats;
            ShotSpeed = shotSpeed;
            ConsumableHealFraction = consumableHealFraction;
            Restores = restores;
            ActiveWeaponDamageShare = activeWeaponDamageShare;
            ActiveElement = activeElement;
            ActiveRadius = activeRadius;
            ActiveCooldownSteps = activeCooldownSteps;
        }

        /// <summary>True for the rare equipment piece that does something when pressed (D37).</summary>
        public bool HasActive => ActiveWeaponDamageShare > 0f;
    }
}
