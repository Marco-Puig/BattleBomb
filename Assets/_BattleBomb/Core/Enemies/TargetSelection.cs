using System.Collections.Generic;
using UnityEngine;

namespace BattleBomb.Core.Enemies
{
    /// <summary>
    /// Which player an enemy fights. Sticky by design: a co-op pair straddling an enemy must not
    /// make it flip-flop every step, so the current target is kept unless it is downed or gone, or
    /// another player is closer by more than the switch margin. Downed players are never targets
    /// (D25). Candidates come from the player registry — never a hardcoded "the player" (D10).
    /// </summary>
    public static class TargetSelection
    {
        /// <summary>Returns the chosen index into <paramref name="targets"/>, or −1 when no one stands.</summary>
        public static int Choose(
            Vector3 self,
            IReadOnlyList<Vector3> targets,
            IReadOnlyList<bool> downed,
            int currentIndex,
            float switchMargin)
        {
            if (targets == null || targets.Count == 0)
            {
                return -1;
            }

            int nearest = -1;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < targets.Count; i++)
            {
                if (downed != null && i < downed.Count && downed[i])
                {
                    continue;
                }

                float distance = PlanarDistance(self, targets[i]);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = i;
                }
            }

            bool currentStands = currentIndex >= 0 && currentIndex < targets.Count
                && !(downed != null && currentIndex < downed.Count && downed[currentIndex]);
            if (!currentStands)
            {
                return nearest;
            }

            float currentDistance = PlanarDistance(self, targets[currentIndex]);
            return nearestDistance + switchMargin < currentDistance ? nearest : currentIndex;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            float dx = b.x - a.x;
            float dz = b.z - a.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
