namespace BattleBomb.Core.Stats
{
    /// <summary>
    /// The authored per-point values and caps behind the D32 stat language. Paper defaults live
    /// in <see cref="Default"/>; the authoring asset overrides them, code never does.
    /// </summary>
    public readonly struct StatTuning
    {
        public readonly float BaseMaxHealth;
        public readonly float HealthPerPoint;
        public readonly float BaseMaxMana;
        public readonly float ManaPerPoint;
        public readonly float BaseManaRegen;
        public readonly float UnarmedDamage;
        public readonly float DamagePerStrengthPoint;
        public readonly float MoveSpeedPerPoint;
        public readonly float MoveSpeedCapBonus;
        public readonly float SlowResistPerOverCapPoint;
        public readonly float SlowResistCap;
        public readonly float DefenceCap;
        public readonly float SlowPerWeight;
        public readonly float BaseCritDamageMultiplier;

        public StatTuning(
            float baseMaxHealth,
            float healthPerPoint,
            float baseMaxMana,
            float manaPerPoint,
            float baseManaRegen,
            float unarmedDamage,
            float damagePerStrengthPoint,
            float moveSpeedPerPoint,
            float moveSpeedCapBonus,
            float slowResistPerOverCapPoint,
            float slowResistCap,
            float defenceCap,
            float slowPerWeight,
            float baseCritDamageMultiplier)
        {
            BaseMaxHealth = baseMaxHealth;
            HealthPerPoint = healthPerPoint;
            BaseMaxMana = baseMaxMana;
            ManaPerPoint = manaPerPoint;
            BaseManaRegen = baseManaRegen;
            UnarmedDamage = unarmedDamage;
            DamagePerStrengthPoint = damagePerStrengthPoint;
            MoveSpeedPerPoint = moveSpeedPerPoint;
            MoveSpeedCapBonus = moveSpeedCapBonus;
            SlowResistPerOverCapPoint = slowResistPerOverCapPoint;
            SlowResistCap = slowResistCap;
            DefenceCap = defenceCap;
            SlowPerWeight = slowPerWeight;
            BaseCritDamageMultiplier = baseCritDamageMultiplier;
        }

        public static StatTuning Default => new StatTuning(
            baseMaxHealth: 100f,
            healthPerPoint: 5f,
            baseMaxMana: 100f,
            manaPerPoint: 5f,
            baseManaRegen: 1f,
            unarmedDamage: 10f,
            damagePerStrengthPoint: 0.01f,
            moveSpeedPerPoint: 0.005f,
            moveSpeedCapBonus: 0.10f,
            slowResistPerOverCapPoint: 0.02f,
            slowResistCap: 0.75f,
            defenceCap: 0.70f,
            slowPerWeight: 0.005f,
            baseCritDamageMultiplier: 1.5f);
    }
}
