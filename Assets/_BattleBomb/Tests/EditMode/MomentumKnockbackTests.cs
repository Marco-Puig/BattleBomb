using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class MomentumKnockbackTests
    {
        private static readonly AttackTuning Attack = new AttackTuning(
            startupSteps: 1, activeSteps: 1, recoverySteps: 1,
            damage: 10f, reachX: 1.6f, depthTolerance: 1f, lungeDistance: 1.2f,
            maxTargets: 2, knockbackSpeed: 6f, launchSpeed: 0f, hitstopSteps: 4,
            moveSpeedScale: 1f);

        private static readonly ElementalMultipliers Neutral = ElementalMultipliers.Neutral;

        private static float ShoveSpeed(Vector3 momentum, TargetKind kind = TargetKind.Enemy)
        {
            HitResult result = HitApplication.Apply(Attack, Vector3.zero, Facing.Right, momentum,
                Element.None, 1f, kind, new Vector3(1f, 0f, 0f), Neutral, Neutral);
            return new Vector2(result.Impulse.x, result.Impulse.z).magnitude;
        }

        [Test]
        public void A_standing_hit_gets_exactly_the_base_knockback()
        {
            Assert.That(ShoveSpeed(Vector3.zero), Is.EqualTo(Attack.KnockbackSpeed).Within(1e-4f));
        }

        [Test]
        public void Running_into_the_target_knocks_it_back_harder()
        {
            float running = ShoveSpeed(new Vector3(6f, 0f, 0f));

            Assert.That(running, Is.GreaterThan(Attack.KnockbackSpeed),
                "Momentum flows into the shove — the fight keeps moving (Michael).");
        }

        [Test]
        public void The_shove_keeps_its_direction_whatever_the_momentum()
        {
            HitResult result = HitApplication.Apply(Attack, Vector3.zero, Facing.Right,
                new Vector3(9f, 0f, 0f), Element.None, 1f, TargetKind.Enemy,
                new Vector3(1f, 0f, -1f), Neutral, Neutral);

            Assert.That(result.Impulse.x, Is.GreaterThan(0f));
            Assert.That(result.Impulse.z, Is.LessThan(0f), "Still away from the attacker.");
        }

        [Test]
        public void A_backpedal_swing_adds_nothing()
        {
            Assert.That(ShoveSpeed(new Vector3(-6f, 0f, 0f)),
                Is.EqualTo(Attack.KnockbackSpeed).Within(1e-4f),
                "Only speed driven into the target counts.");
        }

        [Test]
        public void A_pure_depth_strafe_adds_nothing()
        {
            Assert.That(ShoveSpeed(new Vector3(0f, 0f, 6f)),
                Is.EqualTo(Attack.KnockbackSpeed).Within(1e-4f));
        }

        [Test]
        public void Falling_speed_is_not_momentum()
        {
            Assert.That(ShoveSpeed(new Vector3(0f, -20f, 0f)),
                Is.EqualTo(Attack.KnockbackSpeed).Within(1e-4f));
        }

        [Test]
        public void Faster_always_shoves_at_least_as_hard()
        {
            float slow = ShoveSpeed(new Vector3(3f, 0f, 0f));
            float run = ShoveSpeed(new Vector3(6f, 0f, 0f));
            float build = ShoveSpeed(new Vector3(12f, 0f, 0f));

            Assert.That(run, Is.GreaterThan(slow));
            Assert.That(build, Is.GreaterThan(run), "A speed build still earns something.");
        }

        [Test]
        public void The_bonus_tapers_instead_of_scaling_linearly()
        {
            float atRun = ShoveSpeed(new Vector3(6f, 0f, 0f)) - Attack.KnockbackSpeed;
            float atDouble = ShoveSpeed(new Vector3(12f, 0f, 0f)) - Attack.KnockbackSpeed;

            Assert.That(atDouble, Is.LessThan(atRun * 2f),
                "Double the speed earns less than double the bonus.");
        }

        [Test]
        public void No_speed_ever_shoves_past_the_cap()
        {
            float absurd = ShoveSpeed(new Vector3(1000f, 0f, 0f));

            Assert.That(absurd,
                Is.LessThan(Attack.KnockbackSpeed + HitApplication.MomentumBonusCap),
                "Stacking every point into speed cannot launch enemies across the screen.");
            Assert.That(absurd,
                Is.GreaterThan(Attack.KnockbackSpeed + HitApplication.MomentumBonusCap * 0.9f),
                "But the curve does approach its cap.");
        }

        [Test]
        public void A_partner_shove_inherits_momentum_too()
        {
            float standing = ShoveSpeed(Vector3.zero, TargetKind.Partner);
            float running = ShoveSpeed(new Vector3(6f, 0f, 0f), TargetKind.Partner);

            Assert.That(standing, Is.EqualTo(Attack.KnockbackSpeed).Within(1e-4f));
            Assert.That(running, Is.GreaterThan(standing), "One physics for every shove (D21).");
        }
    }
}
