using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class HitApplicationTests
    {
        private static readonly AttackTuning Attack = new AttackTuning(
            startupSteps: 1, activeSteps: 1, recoverySteps: 1,
            damage: 10f, reachX: 1.6f, depthTolerance: 1f, lungeDistance: 1.2f,
            maxTargets: 2, knockbackSpeed: 6f, launchSpeed: 9f, hitstopSteps: 4);

        private static readonly ElementalMultipliers Neutral = ElementalMultipliers.Neutral;

        private static HitResult Apply(TargetKind kind, Vector3 target,
            in ElementalMultipliers resistance, in ElementalMultipliers climate) =>
            HitApplication.Apply(Attack, Vector3.zero, Facing.Right, Element.Fire, 1f,
                kind, target, resistance, climate);

        [Test]
        public void An_enemy_hit_runs_the_full_damage_pipeline()
        {
            ElementalMultipliers resistance = new ElementalMultipliers(0.5f, 1f, 1f, 1f);
            ElementalMultipliers climate = new ElementalMultipliers(1.3f, 1f, 1f, 1f);

            HitResult result = Apply(TargetKind.Enemy, new Vector3(1f, 0f, 0f), resistance, climate);

            Assert.That(result.Damage, Is.EqualTo(10f * 0.5f * 1.3f).Within(1e-4f));
            Assert.That(result.HitstopSteps, Is.EqualTo(Attack.HitstopSteps));
        }

        [Test]
        public void A_partner_hit_shoves_without_damage_or_hitstop()
        {
            HitResult result = Apply(TargetKind.Partner, new Vector3(1f, 0f, 0f), Neutral, Neutral);

            Assert.That(result.Damage, Is.EqualTo(0f), "D21: never damage.");
            Assert.That(result.HitstopSteps, Is.EqualTo(0), "A shove earns no hitstop.");
            Assert.That(result.Impulse.x, Is.GreaterThan(0f), "The shove itself is real.");
        }

        [Test]
        public void Knockback_points_away_from_the_attacker_on_both_axes()
        {
            HitResult result = Apply(TargetKind.Enemy, new Vector3(1f, 0f, -1f), Neutral, Neutral);

            Assert.That(result.Impulse.x, Is.GreaterThan(0f));
            Assert.That(result.Impulse.z, Is.LessThan(0f), "A shove works in depth too.");
            Assert.That(new Vector2(result.Impulse.x, result.Impulse.z).magnitude,
                Is.EqualTo(Attack.KnockbackSpeed).Within(1e-4f));
        }

        [Test]
        public void The_launcher_speed_becomes_upward_velocity()
        {
            HitResult result = Apply(TargetKind.Enemy, new Vector3(1f, 0f, 0f), Neutral, Neutral);

            Assert.That(result.Impulse.y, Is.EqualTo(Attack.LaunchSpeed));
        }

        [Test]
        public void A_target_on_top_of_the_attacker_is_shoved_along_the_facing()
        {
            HitResult result = HitApplication.Apply(Attack, Vector3.zero, Facing.Left, Element.None, 1f,
                TargetKind.Enemy, Vector3.zero, Neutral, Neutral);

            Assert.That(result.Impulse.x, Is.LessThan(0f),
                "With no separation, the shove follows the attacker's facing.");
        }

        [Test]
        public void A_launched_hit_and_the_motor_bring_the_target_back_down()
        {
            HitResult hit = Apply(TargetKind.Enemy, new Vector3(1f, 0f, 0f), Neutral, Neutral);
            var bounds = new BattleBomb.Core.Spatial.ArenaBounds(-100f, 100f);
            var state = MotorState.AtRest(new Vector3(1f, 0f, 0f));
            state = new MotorState(state.Position, hit.Impulse, state.Facing, false,
                BattleBomb.Core.Movement.MovementTuning.Default.CoyoteSteps + 1, 0);

            float peak = 0f;
            int steps = 0;
            while (steps < 600)
            {
                state = CharacterMotor.Step(state, BattleBomb.Core.Players.PlayerCommand.Idle(0),
                    MovementTuning.Default, bounds, 1f / 60f);
                peak = Mathf.Max(peak, state.Position.y);
                steps++;
                if (state.IsGrounded)
                {
                    break;
                }
            }

            Assert.That(peak, Is.GreaterThan(0.5f), "The launch visibly pops the target up.");
            Assert.That(state.IsGrounded, Is.True, "Gravity brings it home without new code.");
            Assert.That(state.Position.y, Is.EqualTo(bounds.GroundY));
        }
    }
}
