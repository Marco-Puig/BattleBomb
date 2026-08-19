namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// A player's hurt state (M3 task 29): health, the stagger that briefly takes control, and the
    /// post-hit grace that stops a crowd from stunlocking anyone to death — players get grace,
    /// enemies never do. Depleted health is the downed state (D25): nothing lands, nothing acts,
    /// until a partner revives or the attempt ends. A value, like <c>MotorState</c>.
    /// </summary>
    public readonly struct PlayerCondition
    {
        public readonly Health Health;

        /// <summary>Steps of lost control remaining; the actor feeds the motor nothing meanwhile.</summary>
        public readonly int StaggerSteps;

        /// <summary>Steps of post-hit invulnerability remaining. Authored to outlast the stagger.</summary>
        public readonly int GraceSteps;

        public PlayerCondition(Health health, int staggerSteps, int graceSteps)
        {
            Health = health;
            StaggerSteps = staggerSteps > 0 ? staggerSteps : 0;
            GraceSteps = graceSteps > 0 ? graceSteps : 0;
        }

        public bool IsDown => Health.IsDepleted;

        public bool InControl => !IsDown && StaggerSteps <= 0;

        /// <summary>Down or in grace: hits pass through entirely — no damage, no shove, no stagger.</summary>
        public bool IsInvulnerable => IsDown || GraceSteps > 0;

        public static PlayerCondition Fresh(float maxHealth) =>
            new PlayerCondition(new Health(maxHealth), 0, 0);

        /// <summary>One simulation step: both timers count down. Hitstop pauses this with everything else.</summary>
        public PlayerCondition Step() =>
            new PlayerCondition(Health, StaggerSteps - 1, GraceSteps - 1);

        /// <summary>
        /// A landed enemy hit. While invulnerable nothing changes; otherwise damage lands, and a
        /// survivor is staggered and granted grace. Depletion downs the player — the stagger is
        /// irrelevant past that point.
        /// </summary>
        /// <summary>
        /// D25's completion: back on their feet at a fraction of max health, with a grace so the
        /// crowd that downed them cannot instantly re-down them.
        /// </summary>
        public PlayerCondition Revived(float healthFraction, int graceSteps) =>
            new PlayerCondition(Health.Refilled(healthFraction), 0, graceSteps);

        public PlayerCondition Hit(float damage, int staggerSteps, int graceSteps)
        {
            if (IsInvulnerable)
            {
                return this;
            }

            Health damaged = Health.Damaged(damage);
            if (damaged.IsDepleted)
            {
                return new PlayerCondition(damaged, 0, 0);
            }

            return new PlayerCondition(damaged, staggerSteps, graceSteps);
        }
    }
}
