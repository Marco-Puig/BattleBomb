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

        public ItemSpec(
            int id,
            string name,
            ItemSlot slot,
            GearContribution baseStats,
            WeaponClass weaponClass = WeaponClass.None,
            PetClass petClass = PetClass.None,
            float shotSpeed = 0f,
            float consumableHealFraction = 0f)
        {
            Id = id;
            Name = name ?? string.Empty;
            Slot = slot;
            WeaponClass = weaponClass;
            PetClass = petClass;
            BaseStats = baseStats;
            ShotSpeed = shotSpeed;
            ConsumableHealFraction = consumableHealFraction;
        }
    }
}
