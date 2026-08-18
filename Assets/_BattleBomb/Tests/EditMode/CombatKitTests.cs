using System;
using BattleBomb.Core.Combat;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    public sealed class CombatKitTests
    {
        [Test]
        public void The_default_kit_chains_three_lights()
        {
            CombatKit kit = CombatKit.Default;

            Assert.That(kit.ChainLength, Is.EqualTo(3));
        }

        [Test]
        public void The_launcher_is_the_heavy_ender_after_two_lights()
        {
            CombatKit kit = CombatKit.Default;

            Assert.That(kit.StepAt(0).HasHeavy, Is.False);
            Assert.That(kit.StepAt(1).HasHeavy, Is.False);
            Assert.That(kit.StepAt(2).HasHeavy, Is.True, "L-L-H: the ender lives after two Lights.");
            Assert.That(kit.StepAt(2).OnHeavy.LaunchSpeed, Is.GreaterThan(0f),
                "The ender is a launcher (D19).");
        }

        [Test]
        public void Only_the_launcher_and_the_aerial_pop_launch()
        {
            CombatKit kit = CombatKit.Default;

            for (int i = 0; i < kit.ChainLength; i++)
            {
                Assert.That(kit.StepAt(i).OnLight.LaunchSpeed, Is.EqualTo(0f),
                    $"Light link {i + 1} must not launch.");
            }

            Assert.That(kit.Heavy.LaunchSpeed, Is.EqualTo(0f));
            Assert.That(kit.ChargedHeavy.LaunchSpeed, Is.EqualTo(0f));
            Assert.That(kit.AerialLight.LaunchSpeed, Is.GreaterThan(0f),
                "The pop is a small launch (D19).");
            Assert.That(kit.AerialLight.LaunchSpeed,
                Is.LessThan(kit.StepAt(2).OnHeavy.LaunchSpeed),
                "Deliberately short of juggling: the pop stays under the launcher.");
            Assert.That(kit.AerialHeavy.LaunchSpeed, Is.EqualTo(0f), "The slam knocks away, not up.");
        }

        [Test]
        public void The_slam_resolves_on_landing_and_nothing_else_does()
        {
            CombatKit kit = CombatKit.Default;

            Assert.That(kit.AerialHeavy.ResolvesOnLanding, Is.True);
            Assert.That(kit.AerialHeavy.MaxTargets, Is.GreaterThan(1), "The slam is an AoE (D19).");
            Assert.That(kit.AerialLight.ResolvesOnLanding, Is.False);
            Assert.That(kit.Heavy.ResolvesOnLanding, Is.False);
            Assert.That(kit.ChargedHeavy.ResolvesOnLanding, Is.False);
            for (int i = 0; i < kit.ChainLength; i++)
            {
                Assert.That(kit.StepAt(i).OnLight.ResolvesOnLanding, Is.False);
            }
        }

        [Test]
        public void The_charged_heavy_outdamages_the_standalone_on_a_single_target()
        {
            CombatKit kit = CombatKit.Default;

            Assert.That(kit.ChargedHeavy.Damage, Is.GreaterThan(kit.Heavy.Damage));
            Assert.That(kit.ChargedHeavy.MaxTargets, Is.EqualTo(1),
                "Charging trades the cleave for one big hit (D19).");
        }

        [Test]
        public void Every_authored_attack_has_playable_frame_data()
        {
            CombatKit kit = CombatKit.Default;
            var attacks = new System.Collections.Generic.List<AttackTuning>();
            for (int i = 0; i < kit.ChainLength; i++)
            {
                attacks.Add(kit.StepAt(i).OnLight);
                if (kit.StepAt(i).HasHeavy)
                {
                    attacks.Add(kit.StepAt(i).OnHeavy);
                }
            }

            attacks.Add(kit.Heavy);
            attacks.Add(kit.ChargedHeavy);
            attacks.Add(kit.AerialLight);
            attacks.Add(kit.AerialHeavy);

            foreach (AttackTuning attack in attacks)
            {
                Assert.That(attack.StartupSteps, Is.GreaterThan(0));
                Assert.That(attack.ActiveSteps, Is.GreaterThan(0));
                Assert.That(attack.RecoverySteps, Is.GreaterThan(0));
                Assert.That(attack.Damage, Is.GreaterThan(0f));
                Assert.That(attack.ReachX, Is.GreaterThan(0f));
                Assert.That(attack.DepthTolerance, Is.GreaterThan(0f));
                Assert.That(attack.LungeDistance, Is.GreaterThanOrEqualTo(0f));
                Assert.That(attack.MaxTargets, Is.GreaterThanOrEqualTo(1));
                Assert.That(attack.KnockbackSpeed, Is.GreaterThanOrEqualTo(0f));
                Assert.That(attack.LaunchSpeed, Is.GreaterThanOrEqualTo(0f));
                Assert.That(attack.HitstopSteps, Is.GreaterThanOrEqualTo(0));
                Assert.That(attack.MoveSpeedScale, Is.InRange(0f, 1f),
                    "A whiff can slow movement, never speed it up or reverse it.");
                Assert.That(attack.TotalSteps,
                    Is.EqualTo(attack.StartupSteps + attack.ActiveSteps + attack.RecoverySteps));
            }
        }

        [Test]
        public void Whiffed_lights_move_freely_and_whiffed_heavies_are_slowed()
        {
            CombatKit kit = CombatKit.Default;

            for (int i = 0; i < kit.ChainLength; i++)
            {
                Assert.That(kit.StepAt(i).OnLight.MoveSpeedScale, Is.EqualTo(1f),
                    $"Light link {i + 1} must not hinder movement (Michael's playtest).");
            }

            Assert.That(kit.Heavy.MoveSpeedScale, Is.LessThan(1f));
            Assert.That(kit.ChargedHeavy.MoveSpeedScale, Is.LessThan(1f));
            Assert.That(kit.StepAt(2).OnHeavy.MoveSpeedScale, Is.LessThan(1f),
                "The launcher is heavy-class.");
        }

        [Test]
        public void An_empty_or_missing_chain_throws()
        {
            Assert.Throws<ArgumentException>(() => new CombatKit(
                null, default, default, 1, 1, 1));
            Assert.Throws<ArgumentException>(() => new CombatKit(
                new ComboStep[0], default, default, 1, 1, 1));
        }

        [Test]
        public void Non_positive_timing_windows_throw()
        {
            ComboStep[] steps = { new ComboStep(default(AttackTuning)) };

            Assert.Throws<ArgumentException>(() => new CombatKit(steps, default, default, 0, 1, 1));
            Assert.Throws<ArgumentException>(() => new CombatKit(steps, default, default, 1, 0, 1));
            Assert.Throws<ArgumentException>(() => new CombatKit(steps, default, default, 1, 1, 0));
        }

        [Test]
        public void A_position_outside_the_chain_throws()
        {
            CombatKit kit = CombatKit.Default;

            Assert.Throws<ArgumentOutOfRangeException>(() => kit.StepAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => kit.StepAt(kit.ChainLength));
        }

        [Test]
        public void The_kit_copies_its_chain_so_later_edits_cannot_reach_it()
        {
            ComboStep[] steps =
            {
                new ComboStep(new AttackTuning(1, 1, 1, 5f, 1f, 1f, 0f, 1, 0f, 0f, 0, 1f)),
            };
            CombatKit kit = new CombatKit(steps, default, default, 1, 1, 1);

            steps[0] = new ComboStep(new AttackTuning(9, 9, 9, 99f, 9f, 9f, 9f, 9, 9f, 9f, 9, 1f));

            Assert.That(kit.StepAt(0).OnLight.Damage, Is.EqualTo(5f));
        }
    }
}
