namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// One attack's frame data and consequences, in simulation steps and world units. Authored per
    /// character (§5); flat <see cref="Damage"/> until M4 derives it from the weapon. All values
    /// are paper starting points until the M2 tuning session.
    /// </summary>
    public readonly struct AttackTuning
    {
        public readonly int StartupSteps;
        public readonly int ActiveSteps;
        public readonly int RecoverySteps;
        public readonly float Damage;
        public readonly float ReachX;
        public readonly float DepthTolerance;
        public readonly float LungeDistance;
        public readonly int MaxTargets;
        public readonly float KnockbackSpeed;
        public readonly float LaunchSpeed;
        public readonly int HitstopSteps;

        /// <summary>
        /// Movement speed while this attack whiffs — swings out of lunge range never root
        /// (directed by Michael in the M2 playtest): 1 leaves movement untouched, Heavies dip
        /// below it. Irrelevant once a lunge target is picked; the snap owns motion then.
        /// </summary>
        public readonly float MoveSpeedScale;

        public AttackTuning(
            int startupSteps,
            int activeSteps,
            int recoverySteps,
            float damage,
            float reachX,
            float depthTolerance,
            float lungeDistance,
            int maxTargets,
            float knockbackSpeed,
            float launchSpeed,
            int hitstopSteps,
            float moveSpeedScale)
        {
            StartupSteps = startupSteps;
            ActiveSteps = activeSteps;
            RecoverySteps = recoverySteps;
            Damage = damage;
            ReachX = reachX;
            DepthTolerance = depthTolerance;
            LungeDistance = lungeDistance;
            MaxTargets = maxTargets;
            KnockbackSpeed = knockbackSpeed;
            LaunchSpeed = launchSpeed;
            HitstopSteps = hitstopSteps;
            MoveSpeedScale = moveSpeedScale;
        }

        public int TotalSteps => StartupSteps + ActiveSteps + RecoverySteps;
    }
}
