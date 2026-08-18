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

        public CombatStepResult(
            CombatState state,
            bool attackStarted,
            bool hitWindowOpened,
            AttackTuning attack)
        {
            State = state;
            AttackStarted = attackStarted;
            HitWindowOpened = hitWindowOpened;
            Attack = attack;
        }
    }
}
