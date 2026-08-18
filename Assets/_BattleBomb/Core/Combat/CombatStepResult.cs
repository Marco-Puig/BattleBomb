namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// What one combat step decided. The caller aims and picks the lunge target on
    /// <see cref="AttackStarted"/>, resolves hits exactly once on <see cref="HitWindowOpened"/>,
    /// and withholds the stick from the motor while <see cref="MovementLocked"/>.
    /// </summary>
    public readonly struct CombatStepResult
    {
        public readonly CombatState State;
        public readonly bool AttackStarted;
        public readonly bool HitWindowOpened;
        public readonly bool MovementLocked;

        /// <summary>The attack this step concerns; valid whenever one is in flight.</summary>
        public readonly AttackTuning Attack;

        public CombatStepResult(
            CombatState state,
            bool attackStarted,
            bool hitWindowOpened,
            bool movementLocked,
            AttackTuning attack)
        {
            State = state;
            AttackStarted = attackStarted;
            HitWindowOpened = hitWindowOpened;
            MovementLocked = movementLocked;
            Attack = attack;
        }
    }
}
