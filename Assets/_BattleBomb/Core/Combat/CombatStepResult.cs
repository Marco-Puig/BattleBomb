namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// What one combat step decided. The caller aims and picks the lunge target on
    /// <see cref="AttackStarted"/> and resolves hits exactly once on
    /// <see cref="HitWindowOpened"/>. How the body moves during an attack is the caller's call —
    /// rooted snap in lunge range, free otherwise — read from the state's phase.
    /// </summary>
    public readonly struct CombatStepResult
    {
        public readonly CombatState State;
        public readonly bool AttackStarted;
        public readonly bool HitWindowOpened;

        /// <summary>The attack this step concerns; valid whenever one is in flight.</summary>
        public readonly AttackTuning Attack;

        /// <summary>Which cast this step concerns, or None for an ordinary swing (D39).</summary>
        public readonly MagicCastKind Cast;

        /// <summary>
        /// Mana this step committed. Non-zero only on the step a cast starts — the machine decides
        /// affordability, the caller owns the pool and does the spending.
        /// </summary>
        public readonly int ManaSpent;

        /// <summary>The lift a starting leap earned, for the caller to hand to the motor.</summary>
        public readonly float LiftSpeed;

        public CombatStepResult(
            CombatState state,
            bool attackStarted,
            bool hitWindowOpened,
            AttackTuning attack,
            MagicCastKind cast = MagicCastKind.None,
            int manaSpent = 0,
            float liftSpeed = 0f)
        {
            State = state;
            AttackStarted = attackStarted;
            HitWindowOpened = hitWindowOpened;
            Attack = attack;
            Cast = cast;
            ManaSpent = manaSpent;
            LiftSpeed = liftSpeed;
        }

        /// <summary>A cast began this step — the caller spends mana and applies any lift.</summary>
        public bool CastStarted => AttackStarted && Cast != MagicCastKind.None;
    }
}
