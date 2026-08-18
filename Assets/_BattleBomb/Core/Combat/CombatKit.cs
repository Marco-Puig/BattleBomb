using System;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// A character's authored combo table (D19): the Light chain with its per-position Heavy
    /// enders, the standalone and charged Heavy, and the timing windows. A new combo is a new kit —
    /// data, never code. The defaults are paper values until the M2 tuning session.
    /// </summary>
    public sealed class CombatKit
    {
        private readonly ComboStep[] _steps;

        public readonly AttackTuning Heavy;
        public readonly AttackTuning ChargedHeavy;

        /// <summary>Jump→Light: the pop — a small launch, deliberately short of juggling (D19).</summary>
        public readonly AttackTuning AerialLight;

        /// <summary>Jump→Heavy: the slam — resolves radially where the shadow marks the landing (D19/D14).</summary>
        public readonly AttackTuning AerialHeavy;

        public readonly int ChargeThresholdSteps;
        public readonly int ComboWindowSteps;
        public readonly int InputBufferSteps;

        public CombatKit(
            ComboStep[] steps,
            AttackTuning heavy,
            AttackTuning chargedHeavy,
            int chargeThresholdSteps,
            int comboWindowSteps,
            int inputBufferSteps,
            AttackTuning aerialLight = default,
            AttackTuning aerialHeavy = default)
        {
            if (steps == null || steps.Length == 0)
            {
                throw new ArgumentException("A combo chain needs at least one position.", nameof(steps));
            }

            if (chargeThresholdSteps <= 0 || comboWindowSteps <= 0 || inputBufferSteps <= 0)
            {
                throw new ArgumentException("Timing windows must be positive step counts.");
            }

            _steps = (ComboStep[])steps.Clone();
            Heavy = heavy;
            ChargedHeavy = chargedHeavy;
            AerialLight = aerialLight;
            AerialHeavy = aerialHeavy;
            ChargeThresholdSteps = chargeThresholdSteps;
            ComboWindowSteps = comboWindowSteps;
            InputBufferSteps = inputBufferSteps;
        }

        public int ChainLength => _steps.Length;

        public ComboStep StepAt(int comboIndex)
        {
            if (comboIndex < 0 || comboIndex >= _steps.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(comboIndex));
            }

            return _steps[comboIndex];
        }

        public static CombatKit Default => new CombatKit(
            new[]
            {
                new ComboStep(new AttackTuning(
                    startupSteps: 5, activeSteps: 4, recoverySteps: 8,
                    damage: 5f, reachX: 1.6f, depthTolerance: 1f, lungeDistance: 1.2f,
                    maxTargets: 2, knockbackSpeed: 5f, launchSpeed: 0f, hitstopSteps: 2,
                    moveSpeedScale: 1f)),
                new ComboStep(new AttackTuning(
                    startupSteps: 4, activeSteps: 4, recoverySteps: 9,
                    damage: 6f, reachX: 1.6f, depthTolerance: 1f, lungeDistance: 1.2f,
                    maxTargets: 2, knockbackSpeed: 5f, launchSpeed: 0f, hitstopSteps: 2,
                    moveSpeedScale: 1f)),
                new ComboStep(
                    new AttackTuning(
                        startupSteps: 6, activeSteps: 4, recoverySteps: 14,
                        damage: 8f, reachX: 1.7f, depthTolerance: 1f, lungeDistance: 1.4f,
                        maxTargets: 2, knockbackSpeed: 9f, launchSpeed: 0f, hitstopSteps: 2,
                        moveSpeedScale: 1f),
                    new AttackTuning(
                        startupSteps: 8, activeSteps: 4, recoverySteps: 16,
                        damage: 8f, reachX: 1.6f, depthTolerance: 1f, lungeDistance: 1.2f,
                        maxTargets: 2, knockbackSpeed: 3f, launchSpeed: 9f, hitstopSteps: 3,
                        moveSpeedScale: 0.8f)),
            },
            heavy: new AttackTuning(
                startupSteps: 12, activeSteps: 6, recoverySteps: 16,
                damage: 12f, reachX: 2f, depthTolerance: 1.1f, lungeDistance: 2f,
                maxTargets: 4, knockbackSpeed: 9f, launchSpeed: 0f, hitstopSteps: 3,
                moveSpeedScale: 0.8f),
            chargedHeavy: new AttackTuning(
                startupSteps: 6, activeSteps: 6, recoverySteps: 20,
                damage: 26f, reachX: 2.2f, depthTolerance: 1.1f, lungeDistance: 2.4f,
                maxTargets: 1, knockbackSpeed: 12f, launchSpeed: 0f, hitstopSteps: 4,
                moveSpeedScale: 0.8f),
            chargeThresholdSteps: 30,
            comboWindowSteps: 12,
            inputBufferSteps: 12,
            aerialLight: new AttackTuning(
                startupSteps: 4, activeSteps: 3, recoverySteps: 8,
                damage: 4f, reachX: 1.6f, depthTolerance: 1f, lungeDistance: 1.2f,
                maxTargets: 2, knockbackSpeed: 2f, launchSpeed: 5f, hitstopSteps: 2,
                moveSpeedScale: 1f),
            aerialHeavy: new AttackTuning(
                startupSteps: 4, activeSteps: 2, recoverySteps: 12,
                damage: 14f, reachX: 2.2f, depthTolerance: 1.1f, lungeDistance: 0f,
                maxTargets: 4, knockbackSpeed: 10f, launchSpeed: 0f, hitstopSteps: 5,
                moveSpeedScale: 0.3f, resolvesOnLanding: true));
    }
}
