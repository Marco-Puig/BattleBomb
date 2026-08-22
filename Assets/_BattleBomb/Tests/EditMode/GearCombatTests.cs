using BattleBomb.Core.Combat;
using BattleBomb.Core.Stats;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The Core pieces task 45 wires into combat: swing speed's kit scaling (planning decision
    /// 5), the mana pool, and the heal/resize rules on Health and PlayerCondition.
    /// </summary>
    public sealed class GearCombatTests
    {
        [Test]
        public void Swing_speed_compresses_the_authored_phases_and_nothing_else()
        {
            CombatKit fast = CombatKit.Default.ScaledBySwingSpeed(1.25f);
            AttackTuning light = fast.StepAt(0).OnLight;

            Assert.That(light.StartupSteps, Is.EqualTo(4), "5 Ã— 0.8 rounds to 4");
            Assert.That(light.ActiveSteps, Is.EqualTo(3));
            Assert.That(light.RecoverySteps, Is.EqualTo(6));
            Assert.That(light.Damage, Is.EqualTo(CombatKit.Default.StepAt(0).OnLight.Damage),
                "damage is the weapon's job, not swing speed's");
            Assert.That(fast.ChargeThresholdSteps, Is.EqualTo(CombatKit.Default.ChargeThresholdSteps),
                "timing promises stay authored");
            Assert.That(fast.ComboWindowSteps, Is.EqualTo(CombatKit.Default.ComboWindowSteps));
        }

        [Test]
        public void Swing_scaling_never_collapses_a_phase_below_one_step()
        {
            CombatKit blur = CombatKit.Default.ScaledBySwingSpeed(10f);
            AttackTuning light = blur.StepAt(0).OnLight;

            Assert.That(light.StartupSteps, Is.GreaterThanOrEqualTo(1));
            Assert.That(light.ActiveSteps, Is.GreaterThanOrEqualTo(1));
            Assert.That(light.RecoverySteps, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void A_neutral_multiplier_returns_the_same_kit()
        {
            CombatKit kit = CombatKit.Default;

            Assert.That(kit.ScaledBySwingSpeed(1f), Is.SameAs(kit));
            Assert.That(kit.ScaledBySwingSpeed(0f), Is.SameAs(kit),
                "a broken multiplier changes nothing rather than freezing the fight");
        }

        [Test]
        public void The_mana_pool_fills_spends_and_resizes()
        {
            ManaPool pool = ManaPool.Full(100f).Spent(40f);
            Assert.That(pool.Current, Is.EqualTo(60f));

            pool = pool.Step(regenPerSecond: 12f, dt: 0.5f);
            Assert.That(pool.Current, Is.EqualTo(66f).Within(1e-4f));

            for (int i = 0; i < 100; i++)
            {
                pool = pool.Step(12f, 0.5f);
            }

            Assert.That(pool.Current, Is.EqualTo(100f), "regen never overfills");

            Assert.That(pool.Spent(500f).Current, Is.Zero, "an overdraw just empties");
            Assert.That(pool.Resized(50f).Current, Is.EqualTo(50f), "shrinking clamps");
            Assert.That(pool.Resized(200f).Current, Is.EqualTo(100f), "growing keeps current");
        }

        [Test]
        public void Heals_never_overfill_and_never_harm()
        {
            Health health = new Health(100f).Damaged(50f);

            Assert.That(health.Healed(20f).Current, Is.EqualTo(70f));
            Assert.That(health.Healed(500f).Current, Is.EqualTo(100f));
            Assert.That(health.Healed(-10f).Current, Is.EqualTo(50f));
        }

        [Test]
        public void Resizing_health_keeps_current_and_clamps()
        {
            Health health = new Health(100f).Damaged(30f);

            Health grown = health.Resized(150f);
            Assert.That(grown.Max, Is.EqualTo(150f));
            Assert.That(grown.Current, Is.EqualTo(70f), "equipping +HP gear never heals");

            Health shrunk = health.Resized(50f);
            Assert.That(shrunk.Max, Is.EqualTo(50f));
            Assert.That(shrunk.Current, Is.EqualTo(50f), "unequipping can wound to the new ceiling");
        }

        [Test]
        public void A_heal_never_stands_the_downed_up()
        {
            PlayerCondition downed = PlayerCondition.Fresh(100f).Hit(150f, 15, 30);
            Assert.That(downed.IsDown, Is.True);

            PlayerCondition after = downed.Healed(50f);
            Assert.That(after.IsDown, Is.True, "the revive is the only way back (D25)");
            Assert.That(after.Health.Current, Is.Zero);
        }

        [Test]
        public void An_arrow_keeps_its_owner_through_flight()
        {
            ProjectileState arrow = ProjectileState.Fired(
                Vector3.zero, Vector3.right * 5f, 10f, 6f, ElementId.None, 60, ownerPlayerId: 1);
            Assert.That(arrow.FromPlayer, Is.True);

            ProjectileState flown = ProjectileSimulation.Step(arrow, 1f / 60f);
            Assert.That(flown.OwnerPlayerId, Is.EqualTo(1), "the owner survives every step");

            ProjectileState bolt = ProjectileState.Fired(
                Vector3.zero, Vector3.right, 8f, 5f, new ElementId(1), 60);
            Assert.That(bolt.FromPlayer, Is.False, "enemy shots stay enemy by default");
        }

        [Test]
        public void Resizing_the_condition_preserves_timers_and_the_downed_state()
        {
            PlayerCondition hurt = PlayerCondition.Fresh(100f).Hit(40f, 15, 30);
            PlayerCondition resized = hurt.Resized(200f);

            Assert.That(resized.Health.Max, Is.EqualTo(200f));
            Assert.That(resized.Health.Current, Is.EqualTo(60f));
            Assert.That(resized.StaggerSteps, Is.EqualTo(hurt.StaggerSteps));
            Assert.That(resized.GraceSteps, Is.EqualTo(hurt.GraceSteps));

            PlayerCondition downed = PlayerCondition.Fresh(100f).Hit(150f, 15, 30);
            Assert.That(downed.Resized(200f).IsDown, Is.True, "a bigger pool never revives");
        }
    }
}
