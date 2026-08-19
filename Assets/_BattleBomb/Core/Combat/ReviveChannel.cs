using System.Collections.Generic;
using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// D25's partner revive as pure state: a downed partner is the game's first interactable, and
    /// Light beside one starts a channel instead of a swing. The channel advances only while the
    /// reviver stays in control, in range, and pointed at the same partner — anything else drops
    /// it back to inactive, and a fresh press restarts it from zero. Completion is the caller's
    /// to read and apply; this struct never touches anyone's health.
    /// </summary>
    public readonly struct ReviveChannel
    {
        /// <summary>Index of the partner being revived, or -1 while no channel runs.</summary>
        public readonly int TargetIndex;

        /// <summary>Steps channelled so far, counting the starting press as the first.</summary>
        public readonly int Steps;

        public ReviveChannel(int targetIndex, int steps)
        {
            TargetIndex = targetIndex;
            Steps = steps > 0 ? steps : 0;
        }

        public bool IsActive => TargetIndex >= 0;

        public bool IsComplete(int requiredSteps) => IsActive && Steps >= requiredSteps;

        public static ReviveChannel Inactive => new ReviveChannel(-1, 0);

        /// <summary>
        /// One step of the channel's whole rulebook: no control or no valid target breaks it, a
        /// target change breaks it (the fresh press restarts), and otherwise it advances — or
        /// starts, when the press arrives with a partner in range.
        /// </summary>
        public static ReviveChannel Next(
            in ReviveChannel current, bool lightPressed, int targetIndex, bool inControl)
        {
            if (!inControl || targetIndex < 0)
            {
                return Inactive;
            }

            if (current.IsActive)
            {
                return current.TargetIndex == targetIndex
                    ? new ReviveChannel(current.TargetIndex, current.Steps + 1)
                    : Inactive;
            }

            return lightPressed ? new ReviveChannel(targetIndex, 1) : Inactive;
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
