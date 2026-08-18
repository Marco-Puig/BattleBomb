using BattleBomb.Core.Movement;
using BattleBomb.Core.Players;
using BattleBomb.Core.Spatial;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class CharacterMotorTests
    {
        private const float Dt = 1f / 60f;

        private static readonly MovementTuning Tuning = MovementTuning.Default;
        private static readonly ArenaBounds OpenArena = new ArenaBounds(-1000f, 1000f);

        private static PlayerCommand Cmd(Vector2 move,
                                         CommandButtons held = CommandButtons.None,
                                         CommandButtons previous = CommandButtons.None) =>
            PlayerCommand.FromState(0, move, held, previous);

        [Test]
        public void Accelerates_toward_max_speed_and_never_exceeds_it()
        {
            MotorState state = MotorState.AtRest(Vector3.zero);

            for (int i = 0; i < 120; i++)
            {
                state = CharacterMotor.Step(state, Cmd(Vector2.right), Tuning, OpenArena, Dt);
                Assert.That(state.Velocity.x, Is.LessThanOrEqualTo(Tuning.MaxSpeed));
            }

            Assert.That(state.Velocity.x, Is.EqualTo(Tuning.MaxSpeed));
        }

        [Test]
        public void Decelerates_to_exactly_zero_and_stays_there()
        {
            MotorState state = MotorState.AtRest(Vector3.zero);
            for (int i = 0; i < 30; i++)
            {
                state = CharacterMotor.Step(state, Cmd(Vector2.right), Tuning, OpenArena, Dt);
            }

            for (int i = 0; i < 10; i++)
            {
                state = CharacterMotor.Step(state, Cmd(Vector2.zero), Tuning, OpenArena, Dt);
            }

            Assert.That(state.Velocity.x, Is.EqualTo(0f));

            Vector3 restingPosition = state.Position;
            state = CharacterMotor.Step(state, Cmd(Vector2.zero), Tuning, OpenArena, Dt);
            Assert.That(state.Position, Is.EqualTo(restingPosition));
        }

        [Test]
        public void Depth_speed_is_the_scaled_fraction_of_lateral_speed_for_equal_input()
        {
            MotorState lateral = MotorState.AtRest(Vector3.zero);
            MotorState depth = MotorState.AtRest(new Vector3(0f, 0f, DepthBand.Min));

            for (int i = 0; i < 20; i++)
            {
                lateral = CharacterMotor.Step(lateral, Cmd(Vector2.right), Tuning, OpenArena, Dt);
                depth = CharacterMotor.Step(depth, Cmd(Vector2.up), Tuning, OpenArena, Dt);
            }

            Assert.That(lateral.Velocity.x, Is.EqualTo(Tuning.MaxSpeed));
            Assert.That(depth.Velocity.z, Is.EqualTo(Tuning.MaxSpeed * Tuning.DepthSpeedScale));
        }

        [Test]
        public void Facing_flips_on_lateral_input_and_survives_pure_depth_input()
        {
            MotorState state = MotorState.AtRest(Vector3.zero);

            state = CharacterMotor.Step(state, Cmd(Vector2.left), Tuning, OpenArena, Dt);
            Assert.That(state.Facing, Is.EqualTo(Facing.Left));

            state = CharacterMotor.Step(state, Cmd(Vector2.up), Tuning, OpenArena, Dt);
            Assert.That(state.Facing, Is.EqualTo(Facing.Left));

            state = CharacterMotor.Step(state, Cmd(Vector2.right), Tuning, OpenArena, Dt);
            Assert.That(state.Facing, Is.EqualTo(Facing.Right));
        }

        [Test]
        public void Hitting_a_wall_zeroes_velocity_on_that_axis_only()
        {
            ArenaBounds tight = new ArenaBounds(-2f, 2f);
            MotorState state = MotorState.AtRest(new Vector3(1.9f, 0f, 0f));

            for (int i = 0; i < 25; i++)
            {
                state = CharacterMotor.Step(state, Cmd(new Vector2(1f, 1f)), Tuning, tight, Dt);
            }

            Assert.That(state.Position.x, Is.EqualTo(2f));
            Assert.That(state.Velocity.x, Is.EqualTo(0f));
            Assert.That(state.Velocity.z, Is.GreaterThan(0f));
        }

        [Test]
        public void Position_never_leaves_the_arena()
        {
            ArenaBounds tight = new ArenaBounds(-2f, 2f);
            MotorState state = MotorState.AtRest(Vector3.zero);

            for (int i = 0; i < 300; i++)
            {
                state = CharacterMotor.Step(state, Cmd(new Vector2(1f, 1f)), Tuning, tight, Dt);
                Assert.That(tight.Contains(state.Position), Is.True,
                    $"Escaped the arena at step {i}: {state.Position}");
            }
        }

        [Test]
        public void Identical_inputs_produce_identical_states()
        {
            MotorState first = Run();
            MotorState second = Run();

            Assert.That(second, Is.EqualTo(first),
                "The motor must be deterministic — a replayed command stream is a remote player (D10).");
        }

        private static MotorState Run()
        {
            MotorState state = MotorState.AtRest(new Vector3(0.5f, 0f, -1f));

            for (int i = 0; i < 120; i++)
            {
                Vector2 move = i < 40 ? Vector2.right
                             : i < 80 ? new Vector2(-0.3f, 0.7f)
                             : Vector2.zero;
                CommandButtons held = i >= 20 && i < 30 ? CommandButtons.Jump : CommandButtons.None;
                CommandButtons previous = i >= 21 && i < 31 ? CommandButtons.Jump : CommandButtons.None;

                state = CharacterMotor.Step(
                    state, PlayerCommand.FromState(i, move, held, previous), Tuning, OpenArena, Dt);
            }

            return state;
        }
    }
}
