using System.Collections.Generic;
using BattleBomb.Core.Combat;
using UnityEngine;

namespace BattleBomb.Core.Stats
{
    /// <summary>
    /// The aggregated block combat reads (D32/D35): base allocations plus every equipped
    /// contribution, collapsed once. Flat bonuses sum first, percentages apply after, same-stat
    /// sources stack additively. Defence caps, crit and life steal clamp, over-cap Speed becomes
    /// slow resistance. Rebuilt only when the loadout or allocations change — never per step.
    /// </summary>
    public readonly struct StatSheet
    {
        /// <summary>Swing speed can shrink but never collapse a swing to nothing.</summary>
        public const float MinSwingSpeedMultiplier = 0.1f;

        public readonly float MaxHealth;
        public readonly float MaxMana;
        public readonly float ManaRegen;
        public readonly float WeaponDamage;
        public readonly float SwingSpeedMultiplier;
        public readonly float CritChance;
        public readonly float CritDamageMultiplier;
        public readonly float Defence;
        public readonly float MoveSpeedMultiplier;
        public readonly float SlowResist;
        public readonly float WeightSlow;
        public readonly float LifeSteal;
        public readonly float KnockbackMultiplier;

        /// <summary>Flat damage every cast gains from gear (D39) — the whole caster build.</summary>
        public readonly float MagicDamage;

        /// <summary>Units every cast's reach gains from gear.</summary>
        public readonly float MagicRange;

        /// <summary>What incoming elements are worth against this build (D40), 1 being neutral.</summary>
        public readonly ElementalMultipliers ElementalResistance;

        private StatSheet(
            float maxHealth, float maxMana, float manaRegen, float weaponDamage,
            float swingSpeedMultiplier, float critChance, float critDamageMultiplier,
            float defence, float moveSpeedMultiplier, float slowResist, float weightSlow,
            float lifeSteal, float knockbackMultiplier, float magicDamage, float magicRange,
            in ElementalMultipliers elementalResistance)
        {
            MagicDamage = magicDamage;
            MagicRange = magicRange;
            ElementalResistance = elementalResistance;
            MaxHealth = maxHealth;
            MaxMana = maxMana;
            ManaRegen = manaRegen;
            WeaponDamage = weaponDamage;
            SwingSpeedMultiplier = swingSpeedMultiplier;
            CritChance = critChance;
            CritDamageMultiplier = critDamageMultiplier;
            Defence = defence;
            MoveSpeedMultiplier = moveSpeedMultiplier;
            SlowResist = slowResist;
            WeightSlow = weightSlow;
            LifeSteal = lifeSteal;
            KnockbackMultiplier = knockbackMultiplier;
        }

        /// <summary>Move speed with the weight slow applied through slow resistance.</summary>
        public float NetMoveSpeedMultiplier => MoveSpeedMultiplier * (1f - SlowedBy(WeightSlow));

        /// <summary>How much of a raw slow actually lands after resistance (M5's slows reuse this).</summary>
        public float SlowedBy(float rawSlow) => Mathf.Clamp01(rawSlow) * (1f - SlowResist);

        /// <summary>
        /// Builds the sheet. <paramref name="elementalResistances"/> carries per-element
        /// <em>reductions</em> straight off the worn affixes (0.15 meaning "15% less"); they sum
        /// per element and cap, exactly like defence, then become the multipliers combat reads.
        /// They cannot ride inside <see cref="GearContribution"/>, which is one flat block with no
        /// room for a roster that grows (D38).
        /// </summary>
        public static StatSheet Build(
            in BaseStats stats,
            in StatTuning tuning,
            IReadOnlyList<GearContribution> gear,
            IReadOnlyList<ElementalMultiplier> elementalResistances = null)
        {
            float weaponDamage = 0f;
            float swingBonus = 0f;
            float defence = 0f;
            float weight = 0f;
            float weightReduction = 0f;
            float critChance = 0f;
            float critDamageBonus = 0f;
            float lifeSteal = 0f;
            float healthBonus = 0f;
            float manaBonus = 0f;
            float manaRegen = 0f;
            float knockbackBonus = 0f;
            float magicDamage = 0f;
            float magicRange = 0f;

            int count = gear != null ? gear.Count : 0;
            for (int i = 0; i < count; i++)
            {
                GearContribution piece = gear[i];
                weaponDamage += piece.WeaponDamage;
                swingBonus += piece.SwingSpeedBonus;
                defence += piece.Defence;
                weight += piece.Weight;
                weightReduction += piece.WeightReduction;
                critChance += piece.CritChance;
                critDamageBonus += piece.CritDamageBonus;
                lifeSteal += piece.LifeSteal;
                healthBonus += piece.MaxHealthBonus;
                manaBonus += piece.MaxManaBonus;
                manaRegen += piece.ManaRegen;
                knockbackBonus += piece.KnockbackBonus;
                magicDamage += piece.MagicDamage;
                magicRange += piece.MagicRange;
            }

            if (weaponDamage <= 0f)
            {
                weaponDamage = tuning.UnarmedDamage;
            }

            float speedBonus = stats.Speed * tuning.MoveSpeedPerPoint;
            float slowResist = 0f;
            if (speedBonus > tuning.MoveSpeedCapBonus)
            {
                float overCapPoints = (speedBonus - tuning.MoveSpeedCapBonus) / tuning.MoveSpeedPerPoint;
                slowResist = Mathf.Min(tuning.SlowResistCap, overCapPoints * tuning.SlowResistPerOverCapPoint);
                speedBonus = tuning.MoveSpeedCapBonus;
            }

            float netWeight = Mathf.Max(0f, weight * (1f - Mathf.Clamp01(weightReduction)));

            return new StatSheet(
                maxHealth: tuning.BaseMaxHealth + stats.Hp * tuning.HealthPerPoint + healthBonus,
                maxMana: tuning.BaseMaxMana + stats.Mana * tuning.ManaPerPoint + manaBonus,
                manaRegen: tuning.BaseManaRegen + manaRegen,
                weaponDamage: weaponDamage * (1f + stats.Strength * tuning.DamagePerStrengthPoint),
                swingSpeedMultiplier: Mathf.Max(MinSwingSpeedMultiplier, 1f + swingBonus),
                critChance: Mathf.Clamp01(critChance),
                critDamageMultiplier: tuning.BaseCritDamageMultiplier + critDamageBonus,
                defence: Mathf.Clamp(defence, 0f, tuning.DefenceCap),
                moveSpeedMultiplier: 1f + speedBonus,
                slowResist: slowResist,
                weightSlow: netWeight * tuning.SlowPerWeight,
                lifeSteal: Mathf.Clamp01(lifeSteal),
                knockbackMultiplier: 1f + knockbackBonus,
                magicDamage: Mathf.Max(0f, magicDamage),
                magicRange: Mathf.Max(0f, magicRange),
                elementalResistance: BuildResistance(elementalResistances, tuning.ElementalResistCap));
        }

        /// <summary>
        /// Sums the reductions each element collected across the whole loadout, caps them so no
        /// build ever goes immune, and hands back the multipliers the damage pipeline wants.
        /// </summary>
        private static ElementalMultipliers BuildResistance(
            IReadOnlyList<ElementalMultiplier> reductions, float cap)
        {
            if (reductions == null || reductions.Count == 0)
            {
                return ElementalMultipliers.Neutral;
            }

            var summed = new List<ElementalMultiplier>(reductions.Count);
            for (int i = 0; i < reductions.Count; i++)
            {
                ElementId element = reductions[i].Element;
                if (element.IsNone)
                {
                    continue;
                }

                bool merged = false;
                for (int j = 0; j < summed.Count; j++)
                {
                    if (summed[j].Element == element)
                    {
                        summed[j] = new ElementalMultiplier(element, summed[j].Value + reductions[i].Value);
                        merged = true;
                        break;
                    }
                }

                if (!merged)
                {
                    summed.Add(new ElementalMultiplier(element, reductions[i].Value));
                }
            }

            for (int i = 0; i < summed.Count; i++)
            {
                float reduction = Mathf.Clamp(summed[i].Value, 0f, cap);
                summed[i] = new ElementalMultiplier(summed[i].Element, 1f - reduction);
            }

            return ElementalMultipliers.From(summed);
        }
    }
}
