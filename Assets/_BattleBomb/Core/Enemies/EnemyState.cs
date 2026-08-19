using UnityEngine;

namespace BattleBomb.Core.Enemies
{
    public enum EnemyPhase
    {
        /// <summary>Closing toward attack position — the only phase that moves with purpose.</summary>
        Approach = 0,

        /// <summary>The readable windup (the attack's StartupSteps). The player's cue to answer.</summary>
        Telegraph,

        Active,
        Recovery,

        /// <summary>The authored beat between attacks — chases, but cannot swing.</summary>
        Cooldown,

        /// <summary>Flinching from a player hit. Brutes never enter this.</summary>
        Staggered,
    }

    /// <summary>
    /// One enemy's brain state between steps. A value, like <c>CombatState</c>: the brain returns a
    /// new one every step, so enemies are replayable and testable (D10).
    /// </summary>
    public readonly struct EnemyState
    {
        public readonly EnemyPhase Phase;

        /// <summary>Completed steps in the current phase.</summary>
        public readonly int StepsInPhase;

        /// <summary>While above zero the whole enemy is frozen, exactly like the player's hitstop.</summary>
        public readonly int HitstopSteps;

        public EnemyState(EnemyPhase phase, int stepsInPhase, int hitstopSteps)
        {
            Phase = phase;
            StepsInPhase = stepsInPhase;
            HitstopSteps = hitstopSteps;
        }

        public static EnemyState Fresh => new EnemyState(EnemyPhase.Approach, 0, 0);

        public EnemyState WithHitstop(int steps) =>
            new EnemyState(Phase, StepsInPhase, Mathf.Max(HitstopSteps, steps));
    }
}
