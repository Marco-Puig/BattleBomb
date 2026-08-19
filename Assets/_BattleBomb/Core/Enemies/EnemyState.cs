using UnityEngine;

namespace BattleBomb.Core.Enemies
{
    public enum EnemyPhase
    {
        /// <summary>Closing, hovering, or strafing toward attack position.</summary>
        Approach = 0,

        /// <summary>The readable windup (the attack's StartupSteps). The player's cue to answer.</summary>
        Telegraph,

        Active,
        Recovery,

        /// <summary>The authored beat between attacks — peels off and circles, but cannot swing.</summary>
        Cooldown,

        /// <summary>Flinching from a player hit. Brutes never enter this.</summary>
        Staggered,
    }

    /// <summary>
    /// One enemy's brain state between steps. A value, like <c>CombatState</c>: the brain returns a
    /// new one every step, so enemies are replayable and testable (D10). The seed desynchronises a
    /// crowd — two enemies with identical perception still strafe and hop on different beats — and
    /// the age is the deterministic clock those beats run on.
    /// </summary>
    public readonly struct EnemyState
    {
        public readonly EnemyPhase Phase;

        /// <summary>Completed steps in the current phase.</summary>
        public readonly int StepsInPhase;

        /// <summary>While above zero the whole enemy is frozen, exactly like the player's hitstop.</summary>
        public readonly int HitstopSteps;

        /// <summary>Per-spawn variation seed (D28): assigned once, never rolled again.</summary>
        public readonly int Seed;

        /// <summary>Unfrozen steps lived — the clock strafe flips and hop pulses run on.</summary>
        public readonly int AgeSteps;

        public EnemyState(EnemyPhase phase, int stepsInPhase, int hitstopSteps, int seed = 0, int ageSteps = 0)
        {
            Phase = phase;
            StepsInPhase = stepsInPhase;
            HitstopSteps = hitstopSteps;
            Seed = seed;
            AgeSteps = ageSteps;
        }

        public static EnemyState Fresh => new EnemyState(EnemyPhase.Approach, 0, 0);

        public static EnemyState Seeded(int seed) => new EnemyState(EnemyPhase.Approach, 0, 0, seed, 0);

        public EnemyState WithHitstop(int steps) =>
            new EnemyState(Phase, StepsInPhase, Mathf.Max(HitstopSteps, steps), Seed, AgeSteps);
    }
}
