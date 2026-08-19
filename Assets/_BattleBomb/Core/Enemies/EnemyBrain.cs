using UnityEngine;

namespace BattleBomb.Core.Enemies
{
    /// <summary>
    /// The result of one brain step: the new state plus everything the driver acts on. MoveIntent
    /// is stick-shaped (x lateral, y depth) and feeds the same motor players use (D10) — the brain
    /// never moves anything itself. JumpRequested becomes a Jump press in the synthesized command.
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

        /// <summary>A hop this step (D28) — liveliness, not pathing. The motor does the rest.</summary>
        public readonly bool JumpRequested;

        public EnemyStepResult(
            EnemyState state, Vector2 moveIntent,
            bool attackStarted, bool hitWindowOpened, bool projectileFired, bool jumpRequested = false)
        {
            State = state;
            MoveIntent = moveIntent;
            AttackStarted = attackStarted;
            HitWindowOpened = hitWindowOpened;
            ProjectileFired = projectileFired;
            JumpRequested = jumpRequested;
        }
    }

    /// <summary>
    /// The per-step enemy brain (D22, livened by D28): one pure machine for all four archetypes,
    /// differing only by data. The cycle is Approach → Telegraph → Active → Recovery → Cooldown;
    /// a player hit sends interruptible enemies to Staggered, and the brute simply never flinches.
    /// The Castle Crashers feel lives in the movement between swings: melee without the attack
    /// token circles the target at its hover distance, everyone who waits strafes depth on a
    /// seeded beat, cooldown peels away instead of standing on the player, the standoff
    /// archetypes drift inside their band, and hops pulse on per-enemy schedules so no two
    /// enemies ever move in lockstep.
    /// </summary>
    public static class EnemyBrain
    {
        /// <summary>Melee stops closing at this fraction of its reach, so targets stay inside it.</summary>
        public const float ApproachFraction = 0.8f;

        /// <summary>Slack around the hover ring before the enemy corrects its distance.</summary>
        public const float HoverSlackX = 1.2f;

        public static EnemyStepResult Step(in EnemyState state, in EnemyPerception view, in EnemyTuning tuning)
        {
            if (state.HitstopSteps > 0)
            {
                EnemyState frozen = new EnemyState(
                    state.Phase, state.StepsInPhase, state.HitstopSteps - 1, state.Seed, state.AgeSteps);
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
                            Enter(EnemyPhase.Active, state), Vector2.zero,
                            false, hitWindowOpened: !ranged, projectileFired: ranged);
                    }

                    return Advance(state);

                case EnemyPhase.Active:
                    return state.StepsInPhase >= tuning.Attack.ActiveSteps
                        ? new EnemyStepResult(Enter(EnemyPhase.Recovery, state), Vector2.zero, false, false, false)
                        : Advance(state);

                case EnemyPhase.Recovery:
                    return state.StepsInPhase >= tuning.Attack.RecoverySteps
                        ? new EnemyStepResult(Enter(EnemyPhase.Cooldown, state), Vector2.zero, false, false, false)
                        : Advance(state);

                case EnemyPhase.Cooldown:
                    if (state.StepsInPhase >= tuning.CooldownSteps)
                    {
                        return new EnemyStepResult(Enter(EnemyPhase.Approach, state), Vector2.zero, false, false, false);
                    }

                    // The beat between attacks moves: turn-takers peel off and circle, the brute
                    // keeps coming, the standoff archetypes keep drifting in their band.
                    return Free(state, view.HasTarget ? CooldownIntent(view, tuning, state) : Vector2.zero,
                        view, tuning);

                default: // Staggered
                    return state.StepsInPhase >= tuning.StaggerSteps
                        ? new EnemyStepResult(Enter(EnemyPhase.Approach, state), Vector2.zero, false, false, false)
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
                ? new EnemyState(EnemyPhase.Staggered, 1, state.HitstopSteps, state.Seed, state.AgeSteps)
                : state;

        private static EnemyStepResult StepApproach(
            in EnemyState state, in EnemyPerception view, in EnemyTuning tuning)
        {
            if (view.HasTarget && (view.MayAttack || !tuning.TakesTurns) && InAttackPosition(view, tuning))
            {
                return new EnemyStepResult(
                    Enter(EnemyPhase.Telegraph, state), Vector2.zero, attackStarted: true, false, false);
            }

            Vector2 intent = Vector2.zero;
            if (view.HasTarget)
            {
                intent = view.MayAttack || !tuning.TakesTurns
                    ? ApproachIntent(view, tuning, state)
                    : HoverIntent(view, tuning, state);
            }

            return Free(state, intent, view, tuning);
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

        private static Vector2 ApproachIntent(in EnemyPerception view, in EnemyTuning tuning, in EnemyState state)
        {
            float dx = view.TargetPosition.x - view.SelfPosition.x;

            if (tuning.FightsAtRange)
            {
                // Hold the band on X and keep drifting in depth — never a statue (D28). Depth is
                // never closed with purpose: projectiles cross it (§2.2).
                float distance = Mathf.Abs(dx);
                float drift = StrafeSign(state, tuning);
                if (distance > tuning.StandoffFarX)
                {
                    return new Vector2(Mathf.Sign(dx), drift);
                }

                return distance < tuning.StandoffNearX
                    ? new Vector2(-Mathf.Sign(dx), drift)   // scatter diagonally when dived
                    : new Vector2(0f, drift);
            }

            float dz = view.TargetPosition.z - view.SelfPosition.z;
            float x = Mathf.Abs(dx) > tuning.Attack.ReachX * ApproachFraction ? Mathf.Sign(dx) : 0f;
            float z = Mathf.Abs(dz) > tuning.Attack.DepthTolerance * ApproachFraction ? Mathf.Sign(dz) : 0f;
            return new Vector2(x, z);
        }

        /// <summary>Waiting melee circles the target: hold the hover ring on X, strafe depth.</summary>
        private static Vector2 HoverIntent(in EnemyPerception view, in EnemyTuning tuning, in EnemyState state)
        {
            float dx = view.TargetPosition.x - view.SelfPosition.x;
            float distance = Mathf.Abs(dx);
            float x = distance < tuning.HoverDistanceX ? -Mathf.Sign(dx)
                : distance > tuning.HoverDistanceX + HoverSlackX ? Mathf.Sign(dx)
                : 0f;
            return new Vector2(x, StrafeSign(state, tuning));
        }

        /// <summary>The peel-off after a swing: back out to the hover ring instead of face-camping.</summary>
        private static Vector2 CooldownIntent(in EnemyPerception view, in EnemyTuning tuning, in EnemyState state)
        {
            if (tuning.FightsAtRange || !tuning.TakesTurns)
            {
                return ApproachIntent(view, tuning, state);
            }

            return HoverIntent(view, tuning, state);
        }

        /// <summary>Alternates the depth-strafe direction on a per-enemy beat, so crowds spread.</summary>
        private static float StrafeSign(in EnemyState state, in EnemyTuning tuning)
        {
            if (tuning.StrafePeriodSteps <= 0)
            {
                return 0f;
            }

            return ((state.Seed + state.AgeSteps / tuning.StrafePeriodSteps) & 1) == 0 ? 1f : -1f;
        }

        /// <summary>A hop lands on this enemy's own beat — never mid-attack, never in lockstep.</summary>
        private static bool HopPulse(in EnemyState state, in EnemyTuning tuning)
        {
            if (tuning.HopPulseSteps <= 0)
            {
                return false;
            }

            int offset = (state.Seed * 37 & int.MaxValue) % tuning.HopPulseSteps;
            return state.AgeSteps % tuning.HopPulseSteps == offset;
        }

        /// <summary>A step in a phase that moves freely: ages, advances, strafes, and may hop.</summary>
        private static EnemyStepResult Free(
            in EnemyState state, Vector2 intent, in EnemyPerception view, in EnemyTuning tuning)
        {
            EnemyState next = new EnemyState(
                state.Phase, state.StepsInPhase + 1, 0, state.Seed, state.AgeSteps + 1);
            bool hop = view.HasTarget && HopPulse(state, tuning);
            return new EnemyStepResult(next, intent, false, false, false, hop);
        }

        private static EnemyStepResult Advance(in EnemyState state) => new EnemyStepResult(
            new EnemyState(state.Phase, state.StepsInPhase + 1, 0, state.Seed, state.AgeSteps + 1),
            Vector2.zero, false, false, false);

        private static EnemyState Enter(EnemyPhase phase, in EnemyState state) =>
            new EnemyState(phase, phase == EnemyPhase.Approach ? 0 : 1, 0, state.Seed, state.AgeSteps + 1);
    }
}
