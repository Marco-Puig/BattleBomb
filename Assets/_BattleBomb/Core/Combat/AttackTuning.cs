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

        /// <summary>
        /// The slam pattern (D19): startup persists until the attacker touches ground, the actor
        /// descends instead of air-stalling, and the hit resolves radially around the landing
        /// point — <see cref="ReachX"/> is the radius and the grounded shadow is the reticle (D14).
        /// </summary>
        public readonly bool ResolvesOnLanding;

        private readonly bool _radial;

        /// <summary>
        /// Resolves in a circle around the attacker rather than in front of it. The slam is radial
        /// because it lands (D19); M5's aura is radial without waiting for ground, which is what
        /// makes the circle magic's answer to depth.
        /// </summary>
        public bool IsRadial => _radial || ResolvesOnLanding;

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
            float moveSpeedScale,
            bool resolvesOnLanding = false,
            bool isRadial = false)
        {
            _radial = isRadial;
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
            ResolvesOnLanding = resolvesOnLanding;
        }

        public int TotalSteps => StartupSteps + ActiveSteps + RecoverySteps;
    }
}
