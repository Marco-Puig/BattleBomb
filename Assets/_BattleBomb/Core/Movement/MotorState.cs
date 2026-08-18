using UnityEngine;

namespace BattleBomb.Core.Movement
{
    /// <summary>
    /// One character's complete motion state. A plain value so a step is a pure function of it and
    /// determinism is testable by equality (D10).
    /// </summary>
    public readonly struct MotorState
    {
        public readonly Vector3 Position;
        public readonly Vector3 Velocity;
        public readonly Facing Facing;
        public readonly bool IsGrounded;
        public readonly int StepsSinceGrounded;
        public readonly int JumpBufferedFor;

        public MotorState(
            Vector3 position,
            Vector3 velocity,
            Facing facing,
            bool isGrounded,
            int stepsSinceGrounded,
            int jumpBufferedFor)
        {
            Position = position;
            Velocity = velocity;
            Facing = facing;
            IsGrounded = isGrounded;
            StepsSinceGrounded = stepsSinceGrounded;
            JumpBufferedFor = jumpBufferedFor;
        }

        public static MotorState AtRest(Vector3 position, Facing facing = Facing.Right) =>
            new MotorState(position, Vector3.zero, facing, true, 0, 0);
    }
}
