using System.Collections.Generic;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// D32's stat language and D35's aggregation rules (task 39): base points, the Speed cap and
    /// its over-cap slow-resistance, additive gear stacking, and every clamp — pure math, stepped
    /// by hand against paper tuning.
    /// </summary>
    public sealed class StatSheetTests
    {
        private static readonly StatTuning Tuning = StatTuning.Default;

        private static StatSheet Build(in BaseStats stats, params GearContribution[] gear) =>
            StatSheet.Build(stats, Tuning, gear);

        [Test]
        public void Empty_gear_equals_the_tuning_baselines()
        {
            StatSheet sheet = Build(BaseStats.Zero);

            Assert.That(sheet.MaxHealth, Is.EqualTo(100f));
            Assert.That(sheet.MaxMana, Is.EqualTo(100f));
            Assert.That(sheet.ManaRegen, Is.EqualTo(1f));
            Assert.That(sheet.WeaponDamage, Is.EqualTo(10f), "bare hands still hit");
            Assert.That(sheet.SwingSpeedMultiplier, Is.EqualTo(1f));
            Assert.That(sheet.CritChance, Is.Zero);
            Assert.That(sheet.CritDamageMultiplier, Is.EqualTo(1.5f));
            Assert.That(sheet.Defence, Is.Zero);
            Assert.That(sheet.MoveSpeedMultiplier, Is.EqualTo(1f));
            Assert.That(sheet.SlowResist, Is.Zero);
            Assert.That(sheet.WeightSlow, Is.Zero);
            Assert.That(sheet.LifeSteal, Is.Zero);
            Assert.That(sheet.KnockbackMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void Hp_and_mana_points_grow_their_pools()
        {
            StatSheet sheet = Build(new BaseStats(0, 10, 4, 0));

            Assert.That(sheet.MaxHealth, Is.EqualTo(150f));
            Assert.That(sheet.MaxMana, Is.EqualTo(120f));
        }

        [Test]
        public void Strength_multiplies_weapon_damage()
        {
            Assert.That(Build(new BaseStats(10, 0, 0, 0)).WeaponDamage, Is.EqualTo(11f).Within(1e-4f),
                "unarmed damage scales too");
            Assert.That(
                Build(new BaseStats(10, 0, 0, 0), new GearContribution(weaponDamage: 40f)).WeaponDamage,
                Is.EqualTo(44f).Within(1e-4f));
        }

        [Test]
        public void Gear_health_and_mana_bonuses_add_flat()
        {
            StatSheet sheet = Build(
                new BaseStats(0, 2, 0, 0),
                new GearContribution(maxHealthBonus: 25f, maxManaBonus: 10f, manaRegen: 0.5f),
                new GearContribution(maxHealthBonus: 15f));

            Assert.That(sheet.MaxHealth, Is.EqualTo(150f));
            Assert.That(sheet.MaxMana, Is.EqualTo(110f));
            Assert.That(sheet.ManaRegen, Is.EqualTo(1.5f));
        }

        [Test]
        public void Speed_below_the_cap_raises_velocity_exactly()
        {
            StatSheet sheet = Build(new BaseStats(0, 0, 0, 10));

            Assert.That(sheet.MoveSpeedMultiplier, Is.EqualTo(1.05f).Within(1e-4f));
            Assert.That(sheet.SlowResist, Is.Zero);
        }

        [Test]
        public void Speed_at_the_cap_clamps_with_no_resist_yet()
        {
            StatSheet sheet = Build(new BaseStats(0, 0, 0, 20));

            Assert.That(sheet.MoveSpeedMultiplier, Is.EqualTo(1.10f).Within(1e-4f));
            Assert.That(sheet.SlowResist, Is.Zero);
        }

        [Test]
        public void Points_past_the_cap_become_slow_resistance()
        {
            StatSheet sheet = Build(new BaseStats(0, 0, 0, 30));

            Assert.That(sheet.MoveSpeedMultiplier, Is.EqualTo(1.10f).Within(1e-4f),
                "the hard cap never breaks");
            Assert.That(sheet.SlowResist, Is.EqualTo(0.20f).Within(1e-4f),
                "ten over-cap points at 2% each");
        }

        [Test]
        public void Slow_resistance_has_its_own_cap()
        {
            StatSheet sheet = Build(new BaseStats(0, 0, 0, 200));

            Assert.That(sheet.SlowResist, Is.EqualTo(0.75f), "slows always matter a little");
        }

        [Test]
        public void Defence_sums_across_pieces_and_caps()
        {
            StatSheet two = Build(BaseStats.Zero,
                new GearContribution(defence: 0.3f), new GearContribution(defence: 0.3f));
            Assert.That(two.Defence, Is.EqualTo(0.6f).Within(1e-4f));

            StatSheet three = Build(BaseStats.Zero,
                new GearContribution(defence: 0.3f),
                new GearContribution(defence: 0.3f),
                new GearContribution(defence: 0.3f));
            Assert.That(three.Defence, Is.EqualTo(0.70f), "the tank ceiling holds");
        }

        [Test]
        public void Weight_sums_into_a_slow_and_reduction_shrinks_it()
        {
            StatSheet sheet = Build(BaseStats.Zero,
                new GearContribution(weight: 10f),
                new GearContribution(weight: 10f, weightReduction: 0.25f));

            Assert.That(sheet.WeightSlow, Is.EqualTo(0.075f).Within(1e-4f),
                "twenty units reduced a quarter, at half a percent each");
        }

        [Test]
        public void Net_speed_composes_weight_slow_through_resistance()
        {
            StatSheet sheet = Build(new BaseStats(0, 0, 0, 30),
                new GearContribution(weight: 15f));

            Assert.That(sheet.WeightSlow, Is.EqualTo(0.075f).Within(1e-4f));
            Assert.That(sheet.SlowedBy(sheet.WeightSlow), Is.EqualTo(0.06f).Within(1e-4f),
                "20% resist shaves the slow");
            Assert.That(sheet.NetMoveSpeedMultiplier, Is.EqualTo(1.10f * 0.94f).Within(1e-4f));
        }

        [Test]
        public void Offensive_affixes_stack_additively()
        {
            StatSheet sheet = Build(BaseStats.Zero,
                new GearContribution(critChance: 0.10f, critDamageBonus: 0.2f, lifeSteal: 0.05f, knockbackBonus: 0.2f),
                new GearContribution(critChance: 0.15f, critDamageBonus: 0.3f, lifeSteal: 0.05f, knockbackBonus: 0.3f));

            Assert.That(sheet.CritChance, Is.EqualTo(0.25f).Within(1e-4f));
            Assert.That(sheet.CritDamageMultiplier, Is.EqualTo(2.0f).Within(1e-4f));
            Assert.That(sheet.LifeSteal, Is.EqualTo(0.10f).Within(1e-4f));
            Assert.That(sheet.KnockbackMultiplier, Is.EqualTo(1.5f).Within(1e-4f));
        }

        [Test]
        public void Crit_chance_and_life_steal_clamp_to_one()
        {
            StatSheet sheet = Build(BaseStats.Zero,
                new GearContribution(critChance: 0.8f, lifeSteal: 0.7f),
                new GearContribution(critChance: 0.8f, lifeSteal: 0.7f));

            Assert.That(sheet.CritChance, Is.EqualTo(1f));
            Assert.That(sheet.LifeSteal, Is.EqualTo(1f));
        }

        [Test]
        public void Swing_speed_stacks_and_never_collapses()
        {
            StatSheet fast = Build(BaseStats.Zero,
                new GearContribution(swingSpeedBonus: 0.10f),
                new GearContribution(swingSpeedBonus: 0.15f));
            Assert.That(fast.SwingSpeedMultiplier, Is.EqualTo(1.25f).Within(1e-4f));

            StatSheet floored = Build(BaseStats.Zero, new GearContribution(swingSpeedBonus: -2f));
            Assert.That(floored.SwingSpeedMultiplier, Is.EqualTo(StatSheet.MinSwingSpeedMultiplier));
        }

        [Test]
        public void Allocation_is_immutable_and_totalled()
        {
            BaseStats stats = BaseStats.Zero.Allocate(StatId.Strength, 3).Allocate(StatId.Speed);

            Assert.That(stats.Strength, Is.EqualTo(3));
            Assert.That(stats.Speed, Is.EqualTo(1));
            Assert.That(stats.Total, Is.EqualTo(4));
            Assert.That(BaseStats.Zero.Total, Is.Zero, "the source never changed");
        }
    }
}
