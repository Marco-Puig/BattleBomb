using UnityEngine;

namespace BattleBomb.Core.Enemies
{
    /// <summary>
    /// The result of one brain step: the new state plus everything the driver acts on. MoveIntent
    /// is stick-shaped (x lateral, y depth) and feeds the same motor players use (D10) — the brain
    /// never moves anything itself.
    /// </summary>
    public readonly struct EnemyStepResult
    {
        public readonly EnemyState State;
        public readonly Vector2 MoveIntent;

        /// <summary>The telegraph opened this step — aim the facing now.</summary>
        public readonly bool AttackStarted;

        /// <summary>First Active step of a melee attack — resolve hits exactly once, here.</summary>
        public readonly bool HitWindowOpened;

        /// <summary>First Active step of a ranged/caster attack — spawn the projectile now.</summary>
        public readonly bool ProjectileFired;

        public EnemyStepResult(
            EnemyState state, Vector2 moveIntent,
            bool attackStarted, bool hitWindowOpened, bool projectileFired)
        {
            State = state;
            MoveIntent = moveIntent;
            AttackStarted = attackStarted;
            HitWindowOpened = hitWindowOpened;
            ProjectileFired = projectileFired;
        }
    }

    /// <summary>
    /// The per-step enemy brain (D22): one pure machine for all four archetypes, differing only by
    /// data. The cycle is Approach → Telegraph → Active → Recovery → Cooldown; a player hit sends
    /// interruptible enemies to Staggered, and the brute simply never flinches. Melee closes on X
    /// and depth under the same §2.2 rules the player lives by; the standoff archetypes hold their
    /// band on X and never close depth — projectiles cross it for them.
    /// </summary>
    public static class EnemyBrain
    {
        /// <summary>Melee stops closing at this fraction of its reach, so targets stay inside it.</summary>
        public const float ApproachFraction = 0.8f;

        public static EnemyStepResult Step(in EnemyState state, in EnemyPerception view, in EnemyTuning tuning)
        {
            if (state.HitstopSteps > 0)
            {
                EnemyState frozen = new EnemyState(state.Phase, state.StepsInPhase, state.HitstopSteps - 1);
                return new EnemyStepResult(frozen, Vector2.zero, false, false, false);
            }

            switch (state.Phase)
            {
                case EnemyPhase.Approach:
                    return StepApproach(state, view, tuning);

                case EnemyPhase.Telegraph:
                    if (state.StepsInPhase >= tuning.Attack.StartupSteps)
                    {
                        bool ranged = tuning.FightsAtRange;
                        return new EnemyStepResult(
                            new EnemyState(EnemyPhase.Active, 1, 0), Vector2.zero,
                            false, hitWindowOpened: !ranged, projectileFired: ranged);
                    }

                    return Advance(state);

                case EnemyPhase.Active:
                    return state.StepsInPhase >= tuning.Attack.ActiveSteps
                        ? Enter(EnemyPhase.Recovery)
                        : Advance(state);

                case EnemyPhase.Recovery:
                    return state.StepsInPhase >= tuning.Attack.RecoverySteps
                        ? Enter(EnemyPhase.Cooldown)
                        : Advance(state);

                case EnemyPhase.Cooldown:
                    if (state.StepsInPhase >= tuning.CooldownSteps)
                    {
                        return new EnemyStepResult(EnemyState.Fresh, Vector2.zero, false, false, false);
                    }

                    // The beat between attacks still chases — it just cannot swing.
                    return new EnemyStepResult(
                        Advance(state).State,
                        view.HasTarget ? ApproachIntent(view, tuning) : Vector2.zero,
                        false, false, false);

                default: // Staggered
                    return state.StepsInPhase >= tuning.StaggerSteps
                        ? new EnemyStepResult(EnemyState.Fresh, Vector2.zero, false, false, false)
                        : Advance(state);
            }
        }

        /// <summary>
        /// A player hit landed: interruptible enemies flinch to Staggered (dropping any attack in
        /// flight); the brute is unchanged — jump and depth are his answer, not the stunlock.
        /// Enemies get no grace, so repeated hits restart the flinch.
        /// </summary>
        public static EnemyState Interrupted(in EnemyState state, in EnemyTuning tuning) =>
            tuning.Interruptible
                ? new EnemyState(EnemyPhase.Staggered, 1, state.HitstopSteps)
                : state;

        private static EnemyStepResult StepApproach(
            in EnemyState state, in EnemyPerception view, in EnemyTuning tuning)
        {
            if (view.HasTarget && InAttackPosition(view, tuning))
            {
                return new EnemyStepResult(
                    new EnemyState(EnemyPhase.Telegraph, 1, 0), Vector2.zero,
                    attackStarted: true, false, false);
            }

            Vector2 intent = view.HasTarget ? ApproachIntent(view, tuning) : Vector2.zero;
            return new EnemyStepResult(Advance(state).State, intent, false, false, false);
        }

        private static bool InAttackPosition(in EnemyPerception view, in EnemyTuning tuning)
        {
            float dx = Mathf.Abs(view.TargetPosition.x - view.SelfPosition.x);
            if (tuning.FightsAtRange)
            {
                // Fires only from the comfort band: a player who dives it forces a retreat
                // before the next shot — closing the gap is the counter to ranged.
                return dx <= tuning.StandoffFarX && dx >= tuning.StandoffNearX;
            }

            float dz = Mathf.Abs(view.TargetPosition.z - view.SelfPosition.z);
            return dx <= tuning.Attack.ReachX && dz <= tuning.Attack.DepthTolerance;
        }

        private static Vector2 ApproachIntent(in EnemyPerception view, in EnemyTuning tuning)
        {
            float dx = view.TargetPosition.x - view.SelfPosition.x;

            if (tuning.FightsAtRange)
            {
                // Hold the band on X; never close depth — projectiles cross it (§2.2).
                float distance = Mathf.Abs(dx);
                if (distance > tuning.StandoffFarX)
                {
                    return new Vector2(Mathf.Sign(dx), 0f);
                }

                return distance < tuning.StandoffNearX
                    ? new Vector2(-Mathf.Sign(dx), 0f)
                    : Vector2.zero;
            }

            float dz = view.TargetPosition.z - view.SelfPosition.z;
            float x = Mathf.Abs(dx) > tuning.Attack.ReachX * ApproachFraction ? Mathf.Sign(dx) : 0f;
            float z = Mathf.Abs(dz) > tuning.Attack.DepthTolerance * ApproachFraction ? Mathf.Sign(dz) : 0f;
            return new Vector2(x, z);
        }

        private static EnemyStepResult Advance(in EnemyState state) => new EnemyStepResult(
            new EnemyState(state.Phase, state.StepsInPhase + 1, 0), Vector2.zero, false, false, false);

        private static EnemyStepResult Enter(EnemyPhase phase) => new EnemyStepResult(
            new EnemyState(phase, 1, 0), Vector2.zero, false, false, false);
    }
}
