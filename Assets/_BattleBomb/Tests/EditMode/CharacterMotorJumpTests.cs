using BattleBomb.Core.Movement;
using BattleBomb.Core.Players;
using BattleBomb.Core.Spatial;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class CharacterMotorJumpTests
    {
        private const float Dt = 1f / 60f;

        private static readonly MovementTuning Tuning = MovementTuning.Default;
        private static readonly ArenaBounds Arena = new ArenaBounds(-1000f, 1000f);

        private static PlayerCommand Idle => PlayerCommand.Idle(0);

        private static PlayerCommand JumpPress =>
            PlayerCommand.FromState(0, Vector2.zero, CommandButtons.Jump, CommandButtons.None);

        private static PlayerCommand JumpHeld =>
            PlayerCommand.FromState(0, Vector2.zero, CommandButtons.Jump, CommandButtons.Jump);

        private static PlayerCommand JumpRelease =>
            PlayerCommand.FromState(0, Vector2.zero, CommandButtons.None, CommandButtons.Jump);

        private static MotorState Airborne(float height, float verticalSpeed, int stepsSinceGrounded) =>
            new MotorState(new Vector3(0f, height, 0f), new Vector3(0f, verticalSpeed, 0f),
                Facing.Right, false, stepsSinceGrounded, 0);

        [Test]
        public void A_grounded_jump_press_leaves_the_ground()
        {
            MotorState state = MotorState.AtRest(Vector3.zero);

            state = CharacterMotor.Step(state, JumpPress, Tuning, Arena, Dt);

            Assert.That(state.Velocity.y, Is.EqualTo(Tuning.JumpSpeed));
            Assert.That(state.IsGrounded, Is.False);
            Assert.That(state.Position.y, Is.GreaterThan(0f));
        }

        [Test]
        public void A_press_mid_air_does_not_double_jump()
        {
            MotorState state = Airborne(3f, 2f, Tuning.CoyoteSteps + 5);

            state = CharacterMotor.Step(state, JumpPress, Tuning, Arena, Dt);

            Assert.That(state.Velocity.y, Is.EqualTo(2f - Tuning.Gravity * Dt).Within(1e-4f),
                "Only gravity may act — the press must not launch a second jump.");
        }

        [Test]
        public void A_press_inside_the_coyote_window_still_jumps()
        {
            MotorState state = Airborne(2f, 0f, Tuning.CoyoteSteps - 1);

            state = CharacterMotor.Step(state, JumpPress, Tuning, Arena, Dt);

            Assert.That(state.Velocity.y, Is.EqualTo(Tuning.JumpSpeed));
        }

        [Test]
        public void A_press_one_step_past_the_coyote_window_does_not_jump()
        {
            MotorState state = Airborne(2f, 0f, Tuning.CoyoteSteps);

            state = CharacterMotor.Step(state, JumpPress, Tuning, Arena, Dt);

            Assert.That(state.Velocity.y, Is.EqualTo(-Tuning.Gravity * Dt).Within(1e-4f));
        }

        [Test]
        public void A_press_buffered_just_before_landing_fires_on_landing()
        {
            MotorState state = Airborne(0.2f, -10f, 60);

            state = CharacterMotor.Step(state, JumpPress, Tuning, Arena, Dt);
            Assert.That(state.IsGrounded, Is.False, "Still falling on the press step.");

            state = CharacterMotor.Step(state, JumpHeld, Tuning, Arena, Dt);
            Assert.That(state.IsGrounded, Is.True, "This step touches the ground.");

            state = CharacterMotor.Step(state, JumpHeld, Tuning, Arena, Dt);
            Assert.That(state.Velocity.y, Is.EqualTo(Tuning.JumpSpeed),
                "The remembered press fires as soon as the ground allows it.");
            Assert.That(state.IsGrounded, Is.False);
        }

        [Test]
        public void Gravity_accumulates_and_clamps_at_terminal_velocity()
        {
            MotorState state = Airborne(500f, 0f, 100);
            float previous = 0f;

            for (int i = 0; i < 120; i++)
            {
                state = CharacterMotor.Step(state, Idle, Tuning, Arena, Dt);
                Assert.That(state.Velocity.y, Is.LessThanOrEqualTo(previous));
                Assert.That(state.Velocity.y, Is.GreaterThanOrEqualTo(-Tuning.MaxFallSpeed));
                previous = state.Velocity.y;
            }

            Assert.That(state.Velocity.y, Is.EqualTo(-Tuning.MaxFallSpeed));
        }

        [Test]
        public void Landing_snaps_exactly_to_the_ground_with_zero_vertical_velocity()
        {
            MotorState state = Airborne(0.05f, -5f, 30);

            state = CharacterMotor.Step(state, Idle, Tuning, Arena, Dt);

            Assert.That(state.Position.y, Is.EqualTo(Arena.GroundY));
            Assert.That(state.Velocity.y, Is.EqualTo(0f));
            Assert.That(state.IsGrounded, Is.True);
            Assert.That(state.StepsSinceGrounded, Is.EqualTo(0));
        }

        [Test]
        public void Releasing_jump_while_rising_cuts_the_climb()
        {
            MotorState state = Airborne(1f, 10f, 20);

            state = CharacterMotor.Step(state, JumpRelease, Tuning, Arena, Dt);

            float expected = (10f - Tuning.Gravity * Dt) * Tuning.JumpCutMultiplier;
            Assert.That(state.Velocity.y, Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void Releasing_jump_while_falling_changes_nothing()
        {
            MotorState state = Airborne(5f, -5f, 20);

            state = CharacterMotor.Step(state, JumpRelease, Tuning, Arena, Dt);

            Assert.That(state.Velocity.y, Is.EqualTo(-5f - Tuning.Gravity * Dt).Within(1e-4f));
        }

        [Test]
        public void Horizontal_control_still_works_mid_air()
        {
            MotorState state = Airborne(5f, 0f, 10);

            state = CharacterMotor.Step(
                state,
                PlayerCommand.FromState(0, Vector2.right, CommandButtons.None, CommandButtons.None),
                Tuning, Arena, Dt);

            Assert.That(state.Velocity.x, Is.EqualTo(Tuning.Acceleration * Dt).Within(1e-4f));
        }
    }
}
