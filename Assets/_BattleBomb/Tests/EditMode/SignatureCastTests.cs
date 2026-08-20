using BattleBomb.Core.Combat;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// D46's Core surface (task 60B): the element owns the press cast, hits can stun, casts can
    /// fire bolts — and none of it is lost in the reconstructions gear and rank scaling perform.
    /// Synthetic values throughout; the four authored elements are pinned at the asset level.
    /// </summary>
    public sealed class SignatureCastTests
    {
        private static AttackTuning Wall(int stunSteps) => new AttackTuning(
            startupSteps: 14, activeSteps: 4, recoverySteps: 16,
            damage: 30f, reachX: 1.6f, depthTolerance: 0.9f, lungeDistance: 0f,
            maxTargets: 3, knockbackSpeed: 2f, launchSpeed: 0f, hitstopSteps: 3,
            moveSpeedScale: 0.4f, stunSteps: stunSteps);

        private static MagicCast Bolt => new MagicCast(
            new AttackTuning(
                startupSteps: 10, activeSteps: 1, recoverySteps: 14,
                damage: 10f, reachX: 0f, depthTolerance: 0.9f, lungeDistance: 0f,
                maxTargets: 1, knockbackSpeed: 2f, launchSpeed: 0f, hitstopSteps: 1,
                moveSpeedScale: 0.4f),
            manaCost: 15, liftSpeed: 0f, delivery: CastDelivery.Projectile, projectileSpeed: 14f);

        [Test]
        public void An_attack_can_carry_a_stun_and_most_carry_none()
        {
            Assert.That(Wall(45).StunSteps, Is.EqualTo(45));
            Assert.That(Wall(-5).StunSteps, Is.EqualTo(0), "Negative stun is no stun.");
            Assert.That(MagicKit.Default.Splash.Attack.StunSteps, Is.EqualTo(0),
                "The paper press cast never stunned and still does not.");
        }

        [Test]
        public void Gear_scaling_keeps_the_stun_the_delivery_and_the_bolt_speed()
        {
            var kit = new MagicKit(
                new MagicCast(Wall(45), 25),
                Bolt,
                MagicKit.Default.Leap);

            MagicKit geared = kit.ScaledByGear(bonusDamage: 10f, bonusRange: 1f);

            Assert.That(geared.Splash.Attack.StunSteps, Is.EqualTo(45),
                "A Godly wall still stuns.");
            Assert.That(geared.Aura.Delivery, Is.EqualTo(CastDelivery.Projectile),
                "The bolt is still a bolt.");
            Assert.That(geared.Aura.ProjectileSpeed, Is.EqualTo(14f));
            Assert.That(geared.Aura.Attack.Damage, Is.EqualTo(20f));
        }

        [Test]
        public void Rank_scaling_keeps_the_stun()
        {
            var kit = new CombatKit(
                new[] { new ComboStep(Wall(45)) },
                heavy: Wall(45), chargedHeavy: Wall(45),
                chargeThresholdSteps: 30, comboWindowSteps: 20, inputBufferSteps: 6,
                aerialLight: Wall(45), aerialHeavy: Wall(45));

            CombatKit faster = kit.ScaledBySwingSpeed(1.5f);

            Assert.That(faster.Heavy.StunSteps, Is.EqualTo(45),
                "Swing speed reshapes frames, never consequences.");
        }

        [Test]
        public void The_element_hands_its_signature_to_the_kit()
        {
            MagicKit paper = MagicKit.Default;
            var ice = new ElementSpec(new ElementId(2), "Ice", default, Bolt);

            MagicKit composed = paper.WithSignature(ice.SignatureCast);

            Assert.That(composed.Splash.Delivery, Is.EqualTo(CastDelivery.Projectile),
                "The press slot is the element's now.");
            Assert.That(composed.Splash.ManaCost, Is.EqualTo(15));
            Assert.That(composed.Aura.Attack.Damage, Is.EqualTo(paper.Aura.Attack.Damage),
                "The aura stays the character's (D46).");
            Assert.That(composed.Leap.LiftSpeed, Is.EqualTo(paper.Leap.LiftSpeed),
                "So does the leap.");
        }

        [Test]
        public void An_element_without_a_signature_changes_nothing()
        {
            MagicKit paper = MagicKit.Default;
            var synthetic = new ElementSpec(new ElementId(9), "Synthetic");

            MagicKit composed = paper.WithSignature(synthetic.SignatureCast);

            Assert.That(composed.Splash.Attack.Damage, Is.EqualTo(paper.Splash.Attack.Damage),
                "Synthetic test elements keep every existing test honest.");
            Assert.That(composed.Splash.Delivery, Is.EqualTo(CastDelivery.Hitbox));
        }
    }
}
