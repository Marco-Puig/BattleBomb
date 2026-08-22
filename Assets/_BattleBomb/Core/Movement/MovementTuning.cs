namespace BattleBomb.Core.Movement
{
    /// <summary>
    /// Movement feel constants. Authored per character (§5) and consumed by
    /// <see cref="CharacterMotor"/>; the defaults are paper starting values, set by play from M1 on.
    /// </summary>
    public readonly struct MovementTuning
    {
        public readonly float MaxSpeed;
        public readonly float Acceleration;
        public readonly float Deceleration;
        public readonly float DepthSpeedScale;
        public readonly float Gravity;
        public readonly float JumpSpeed;
        public readonly float MaxFallSpeed;
        public readonly int CoyoteSteps;
        public readonly int JumpBufferSteps;

        public MovementTuning(
            float maxSpeed,
            float acceleration,
            float deceleration,
            float depthSpeedScale,
            float gravity,
            float jumpSpeed,
            float maxFallSpeed,
            int coyoteSteps,
            int jumpBufferSteps)
        {
            MaxSpeed = maxSpeed;
            Acceleration = acceleration;
            Deceleration = deceleration;
            DepthSpeedScale = depthSpeedScale;
            Gravity = gravity;
            JumpSpeed = jumpSpeed;
            MaxFallSpeed = maxFallSpeed;
            CoyoteSteps = coyoteSteps;
            JumpBufferSteps = jumpBufferSteps;
        }

        public static MovementTuning Default => new MovementTuning(
            maxSpeed: 6f,
            acceleration: 60f,
            deceleration: 80f,
            depthSpeedScale: 0.85f,
            gravity: 45f,
            jumpSpeed: 12f,
            maxFallSpeed: 30f,
            coyoteSteps: 6,
            jumpBufferSteps: 6);
    }
}
