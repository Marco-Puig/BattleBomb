using System.Collections.Generic;
using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// D25's partner revive with D29's skill: each Light press beside the downed partner is one
    /// pump, the revive completes after an authored pump count, and the pace those pumps landed
    /// at decides the restored health — the Castle Crashers CPR feel. Going quiet drains pumps
    /// until the channel drops. It still breaks on lost control, leaving range, or a target
    /// change, and always restarts from zero. Completion is the caller's to read and apply; this
    /// struct never touches anyone's health.
    /// </summary>
    public readonly struct ReviveChannel
    {
        /// <summary>Index of the partner being revived, or -1 while no channel runs.</summary>
        public readonly int TargetIndex;

        /// <summary>Light presses landed so far, counting the starting press.</summary>
        public readonly int Pumps;

        /// <summary>Steps since the channel started — the pace that prices the revive (D29).</summary>
        public readonly int StepsElapsed;

        /// <summary>Steps since the last pump — the decay clock.</summary>
        public readonly int StepsSincePump;

        public ReviveChannel(int targetIndex, int pumps, int stepsElapsed, int stepsSincePump)
        {
            TargetIndex = targetIndex;
            Pumps = pumps > 0 ? pumps : 0;
            StepsElapsed = stepsElapsed > 0 ? stepsElapsed : 0;
            StepsSincePump = stepsSincePump > 0 ? stepsSincePump : 0;
        }

        public bool IsActive => TargetIndex >= 0;

        public bool IsComplete(int requiredPumps) => IsActive && Pumps >= requiredPumps;

        public static ReviveChannel Inactive => new ReviveChannel(-1, 0, 0, 0);

        /// <summary>
        /// One step of the channel's whole rulebook: no control or no valid target breaks it, a
        /// target change breaks it, a press pumps it, silence decays it — one pump lost per
        /// <paramref name="pumpDecaySteps"/> quiet steps, and draining to zero drops the channel.
        /// </summary>
        public static ReviveChannel Next(
            in ReviveChannel current,
            bool lightPressed,
            int targetIndex,
            bool inControl,
            int pumpDecaySteps)
        {
            if (!inControl || targetIndex < 0)
            {
                return Inactive;
            }

            if (!current.IsActive)
            {
                return lightPressed ? new ReviveChannel(targetIndex, 1, 1, 0) : Inactive;
            }

            if (current.TargetIndex != targetIndex)
            {
                return Inactive;
            }

            int steps = current.StepsElapsed + 1;
            if (lightPressed)
            {
                return new ReviveChannel(targetIndex, current.Pumps + 1, steps, 0);
            }

            int quiet = current.StepsSincePump + 1;
            if (pumpDecaySteps > 0 && quiet >= pumpDecaySteps)
            {
                int pumps = current.Pumps - 1;
                return pumps > 0
                    ? new ReviveChannel(targetIndex, pumps, steps, 0)
                    : Inactive;
            }

            return new ReviveChannel(targetIndex, current.Pumps, steps, quiet);
        }

        /// <summary>
        /// D29's price: the health fraction a completed revive restores, from the pace it was
        /// pumped at. At or under <paramref name="fastSteps"/> the max fraction; at or past
        /// <paramref name="slowSteps"/> the min; linear between.
        /// </summary>
        public static float RestoredFraction(
            int stepsElapsed, int fastSteps, int slowSteps, float minFraction, float maxFraction)
        {
            float pace = Mathf.InverseLerp(fastSteps, slowSteps, stepsElapsed);
            return Mathf.Lerp(maxFraction, minFraction, pace);
        }

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
