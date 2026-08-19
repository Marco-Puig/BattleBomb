using System.Collections.Generic;
using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// D25's partner revive on D31's heartbeat: a pulse beats over the downed partner, and each
    /// Light press earns progress by its timing — on the beat's centre a press is worth
    /// <see cref="PerfectPumpProgress"/> at full accuracy, a rushed press (too soon after the
    /// last) only <see cref="SloppyPumpProgress"/> at zero. The health restored is the average
    /// accuracy across the channel: mashing completes fastest and pays the floor, riding the
    /// beat takes longer and pays the ceiling. Silence drains progress until the channel drops.
    /// It still breaks on lost control, leaving range, or a target change, and always restarts
    /// from zero. Completion is the caller's to read and apply; this struct never touches
    /// anyone's health.
    /// </summary>
    public readonly struct ReviveChannel
    {
        /// <summary>What a rushed press is worth — and the chunk silence drains (D31 paper).</summary>
        public const float SloppyPumpProgress = 0.5f;

        /// <summary>What a press on the beat's centre is worth (D31 paper).</summary>
        public const float PerfectPumpProgress = 2f;

        /// <summary>Index of the partner being revived, or -1 while no channel runs.</summary>
        public readonly int TargetIndex;

        /// <summary>Progress earned so far; the channel completes at the authored requirement.</summary>
        public readonly float Progress;

        /// <summary>Steps since the channel started — the heartbeat's clock.</summary>
        public readonly int StepsElapsed;

        /// <summary>Steps since the last press — the rush check and the decay clock.</summary>
        public readonly int StepsSincePump;

        /// <summary>Summed accuracy of every rated press; the starting press is not rated.</summary>
        public readonly float AccuracySum;

        /// <summary>Rated presses so far.</summary>
        public readonly int Pumps;

        public ReviveChannel(
            int targetIndex, float progress, int stepsElapsed, int stepsSincePump,
            float accuracySum, int pumps)
        {
            TargetIndex = targetIndex;
            Progress = progress > 0f ? progress : 0f;
            StepsElapsed = stepsElapsed > 0 ? stepsElapsed : 0;
            StepsSincePump = stepsSincePump > 0 ? stepsSincePump : 0;
            AccuracySum = accuracySum > 0f ? accuracySum : 0f;
            Pumps = pumps > 0 ? pumps : 0;
        }

        public bool IsActive => TargetIndex >= 0;

        public bool IsComplete(float requiredProgress) => IsActive && Progress >= requiredProgress;

        public float AverageAccuracy => Pumps > 0 ? AccuracySum / Pumps : 0f;

        public static ReviveChannel Inactive => new ReviveChannel(-1, 0f, 0, 0, 0f, 0);

        /// <summary>Where in the beat cycle a given channel step sits, 0–1. The HUD's pulse and
        /// the press accuracy read the same clock, so what the eye sees is what the press earns.</summary>
        public static float BeatPhase(int stepsElapsed, int beatSteps) =>
            beatSteps > 0 ? (stepsElapsed % beatSteps) / (float)beatSteps : 0f;

        /// <summary>1 at the beat's centre (phase 0.5), falling to 0 at its edges.</summary>
        public static float PressAccuracy(int stepsElapsed, int beatSteps) =>
            1f - 2f * Mathf.Abs(BeatPhase(stepsElapsed, beatSteps) - 0.5f);

        /// <summary>
        /// One step of the channel's whole rulebook: no control or no valid target breaks it, a
        /// target change breaks it, a press earns by its timing, and silence decays — one sloppy
        /// chunk lost per <paramref name="pumpDecaySteps"/> quiet steps, dropping the channel at
        /// zero. The starting press opens the channel at a sloppy chunk, unrated.
        /// </summary>
        public static ReviveChannel Next(
            in ReviveChannel current,
            bool lightPressed,
            int targetIndex,
            bool inControl,
            int beatSteps,
            int rushSteps,
            int pumpDecaySteps)
        {
            if (!inControl || targetIndex < 0)
            {
                return Inactive;
            }

            if (!current.IsActive)
            {
                return lightPressed
                    ? new ReviveChannel(targetIndex, SloppyPumpProgress, 1, 0, 0f, 0)
                    : Inactive;
            }

            if (current.TargetIndex != targetIndex)
            {
                return Inactive;
            }

            int steps = current.StepsElapsed + 1;
            if (lightPressed)
            {
                bool rushed = current.StepsSincePump + 1 < rushSteps;
                float accuracy = rushed ? 0f : PressAccuracy(steps, beatSteps);
                float earned = SloppyPumpProgress + accuracy * (PerfectPumpProgress - SloppyPumpProgress);
                return new ReviveChannel(
                    targetIndex, current.Progress + earned, steps, 0,
                    current.AccuracySum + accuracy, current.Pumps + 1);
            }

            int quiet = current.StepsSincePump + 1;
            if (pumpDecaySteps > 0 && quiet >= pumpDecaySteps)
            {
                float drained = current.Progress - SloppyPumpProgress;
                return drained > 0f
                    ? new ReviveChannel(targetIndex, drained, steps, 0, current.AccuracySum, current.Pumps)
                    : Inactive;
            }

            return new ReviveChannel(
                targetIndex, current.Progress, steps, quiet, current.AccuracySum, current.Pumps);
        }

        /// <summary>D31's price: precision buys health. Average accuracy maps linearly between
        /// the authored floor and ceiling.</summary>
        public static float RestoredFraction(float averageAccuracy, float minFraction, float maxFraction) =>
            Mathf.Lerp(minFraction, maxFraction, Mathf.Clamp01(averageAccuracy));

        /// <summary>
        /// The nearest downed partner within planar range, or -1. Never the reviver, never a
        /// standing player; ties break to the lower index so the choice is deterministic (D10).
        /// </summary>
        public static int FindTarget(
            Vector3 self,
            IReadOnlyList<Vector3> positions,
            IReadOnlyList<bool> downed,
            int selfIndex,
            float range)
        {
            int best = -1;
            float bestSq = float.MaxValue;
            float rangeSq = range * range;
            for (int i = 0; i < positions.Count; i++)
            {
                if (i == selfIndex || !downed[i])
                {
                    continue;
                }

                Vector3 to = positions[i] - self;
                to.y = 0f;
                float sq = to.sqrMagnitude;
                if (sq <= rangeSq && sq < bestSq)
                {
                    best = i;
                    bestSq = sq;
                }
            }

            return best;
        }
    }
}
