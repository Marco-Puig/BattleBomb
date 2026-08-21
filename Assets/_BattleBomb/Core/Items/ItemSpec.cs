using BattleBomb.Core.Combat;
using BattleBomb.Core.Stats;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// One authored item definition as the generator sees it — the `.ToRuntime()` face of the
    /// ScriptableObject, like every other authored thing. The base stat block is what quality
    /// scales; the identity's name is what quality prefixes.
    /// </summary>
    public readonly struct ItemSpec
    {
        public readonly ItemIdentity Identity;
        public readonly GearContribution BaseStats;
        public readonly float ShotSpeed;
        public readonly RestorePayload Consumable;
        public readonly ActivePayload Active;

        public ItemSpec(
            in ItemIdentity identity,
            in GearContribution baseStats,
            float shotSpeed = 0f,
            in RestorePayload consumable = default,
            in ActivePayload active = default)
        {
            Identity = identity;
            BaseStats = baseStats;
            ShotSpeed = shotSpeed;
            Consumable = consumable;
            Active = active;
        }

        public int Id => Identity.DefinitionId;

        public string Name => Identity.Name ?? string.Empty;

        public ItemSlot Slot => Identity.Slot;

        public WeaponClass WeaponClass => Identity.WeaponClass;

        public PetClass PetClass => Identity.PetClass;

        public float ConsumableHealFraction => Consumable.Fraction;

        /// <summary>Which pool this consumable refills — health potions and mana potions differ
        /// only in this payload and their essence name.</summary>
        public RestoreKind Restores => Consumable.Kind;

        /// <summary>True for the rare equipment piece that does something when pressed (D37).</summary>
        public bool HasActive => Active.Exists;
    }
}
