using BattleBomb.Core.Players;
using BattleBomb.Core.Spatial;
using UnityEngine;

namespace BattleBomb.Core.Movement
{
    /// <summary>
    /// The character motor: a pure function of (state, command, tuning, bounds, dt). There is no
    /// physics engine in the loop — the only collision is the arena clamp and the ground plane.
    /// Obstacles arrive with M3 through an explicit Core seam, never a Rigidbody (HANDOFF-M1 §4).
    /// </summary>
    public static class CharacterMotor
    {
        private const float FacingThreshold = 0.01f;

        public static MotorState Step(in MotorState state, in PlayerCommand command,
                                      in MovementTuning tuning, in ArenaBounds bounds, float dt)
        {
            int buffered = Mathf.Max(0, state.JumpBufferedFor - 1);
            if (command.WasPressed(CommandButtons.Jump))
            {
                buffered = tuning.JumpBufferSteps;
            }

            int stepsSinceGrounded = state.IsGrounded ? 0 : state.StepsSinceGrounded + 1;
            bool grounded = state.IsGrounded;
            float vy = state.Velocity.y;

            if (buffered > 0 && stepsSinceGrounded <= tuning.CoyoteSteps)
            {
                vy = tuning.JumpSpeed;
                buffered = 0;
                grounded = false;
                stepsSinceGrounded = tuning.CoyoteSteps + 1;
            }
            else
            {
                vy = grounded ? 0f : Mathf.Max(vy - tuning.Gravity * dt, -tuning.MaxFallSpeed);
            }

            float desiredX = command.Move.x * tuning.MaxSpeed;
            float desiredZ = command.Move.y * tuning.MaxSpeed * tuning.DepthSpeedScale;
            float rate = command.Move.sqrMagnitude > 0f ? tuning.Acceleration : tuning.Deceleration;
            float vx = Mathf.MoveTowards(state.Velocity.x, desiredX, rate * dt);
            float vz = Mathf.MoveTowards(state.Velocity.z, desiredZ, rate * dt);

            Vector3 position = state.Position + new Vector3(vx, 0f, vz) * dt;
            Vector3 clamped = bounds.ClampHorizontal(position);
            if (clamped.x != position.x)
            {
                vx = 0f;
            }

            if (clamped.z != position.z)
            {
                vz = 0f;
            }

            float y = state.Position.y + vy * dt;
            if (y <= bounds.GroundY)
            {
                y = bounds.GroundY;
                vy = 0f;
                grounded = true;
                stepsSinceGrounded = 0;
            }
            else
            {
                grounded = false;
            }

            Facing facing = command.Move.x > FacingThreshold ? Facing.Right
                          : command.Move.x < -FacingThreshold ? Facing.Left
                          : state.Facing;

            return new MotorState(
                new Vector3(clamped.x, y, clamped.z),
                new Vector3(vx, vy, vz),
                facing,
                grounded,
                stepsSinceGrounded,
                buffered);
        }
    }
}
