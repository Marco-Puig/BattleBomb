namespace BattleBomb.Core.Stats
{
    /// <summary>
    /// One equipped item's resolved stat contribution — core stats and affixes already collapsed
    /// into plain numbers (D35). Task 40's item model produces these; <see cref="StatSheet"/>
    /// consumes them without knowing what an item is.
    /// </summary>
    public readonly struct GearContribution
    {
        public readonly float WeaponDamage;
        public readonly float SwingSpeedBonus;
        public readonly float Defence;
        public readonly float Weight;
        public readonly float CritChance;
        public readonly float CritDamageBonus;
        public readonly float LifeSteal;
        public readonly float MaxHealthBonus;
        public readonly float MaxManaBonus;
        public readonly float ManaRegen;
        public readonly float WeightReduction;
        public readonly float KnockbackBonus;

        public GearContribution(
            float weaponDamage = 0f,
            float swingSpeedBonus = 0f,
            float defence = 0f,
            float weight = 0f,
            float critChance = 0f,
            float critDamageBonus = 0f,
            float lifeSteal = 0f,
            float maxHealthBonus = 0f,
            float maxManaBonus = 0f,
            float manaRegen = 0f,
            float weightReduction = 0f,
            float knockbackBonus = 0f)
        {
            WeaponDamage = weaponDamage;
            SwingSpeedBonus = swingSpeedBonus;
            Defence = defence;
            Weight = weight;
            CritChance = critChance;
            CritDamageBonus = critDamageBonus;
            LifeSteal = lifeSteal;
            MaxHealthBonus = maxHealthBonus;
            MaxManaBonus = maxManaBonus;
            ManaRegen = manaRegen;
            WeightReduction = weightReduction;
            KnockbackBonus = knockbackBonus;
        }

        public static GearContribution Zero => default;

        /// <summary>Every field scaled — how a quality budget grows a definition's base block.</summary>
        public GearContribution Scaled(float factor) => new GearContribution(
            WeaponDamage * factor,
            SwingSpeedBonus * factor,
            Defence * factor,
            Weight * factor,
            CritChance * factor,
            CritDamageBonus * factor,
            LifeSteal * factor,
            MaxHealthBonus * factor,
            MaxManaBonus * factor,
            ManaRegen * factor,
            WeightReduction * factor,
            KnockbackBonus * factor);

        public static GearContribution operator +(in GearContribution a, in GearContribution b) =>
            new GearContribution(
                a.WeaponDamage + b.WeaponDamage,
                a.SwingSpeedBonus + b.SwingSpeedBonus,
                a.Defence + b.Defence,
                a.Weight + b.Weight,
                a.CritChance + b.CritChance,
                a.CritDamageBonus + b.CritDamageBonus,
                a.LifeSteal + b.LifeSteal,
                a.MaxHealthBonus + b.MaxHealthBonus,
                a.MaxManaBonus + b.MaxManaBonus,
                a.ManaRegen + b.ManaRegen,
                a.WeightReduction + b.WeightReduction,
                a.KnockbackBonus + b.KnockbackBonus);
    }
}
