using System.Collections.Generic;
using UnityEngine;

namespace BattleBomb.Core.Movement
{
    /// <summary>
    /// The soft crowding seam deferred from M1, due now that crowds exist (task 37): overlapping
    /// bodies push gently apart on X and Z so a group never stacks into one point — never Y, and
    /// never the physics engine. The push is capped low so it reads as crowding, not force, and a
    /// fast-moving body receives no push at all: knockback and the launcher must never feel
    /// cushioned. Pairs resolve in list order, so the caller's deterministic ordering is the
    /// whole determinism story (D10).
    /// </summary>
    public static class BodySeparation
    {
        /// <summary>Personal-space radius per body; two bodies overlap under twice this.</summary>
        public const float PersonalRadius = 0.45f;

        /// <summary>Largest nudge one step may apply — crowding pace, roughly 1.8 units/second.</summary>
        public const float MaxPushPerStep = 0.03f;

        /// <summary>At or above this speed a body is in flight (shove, launch) and receives nothing.</summary>
        public const float FastSpeed = 4.5f;

        /// <summary>
        /// Fills <paramref name="displacements"/> with one planar nudge per body — many are zero.
        /// Fast bodies still push others aside; they just take no push themselves. A perfectly
        /// stacked pair splits along X, lower index left, so even the degenerate case is
        /// deterministic.
        /// </summary>
        public static void Resolve(
            IReadOnlyList<Vector3> positions,
            IReadOnlyList<Vector3> velocities,
            List<Vector3> displacements)
        {
            displacements.Clear();
            for (int i = 0; i < positions.Count; i++)
            {
                displacements.Add(Vector3.zero);
            }

            for (int i = 0; i < positions.Count; i++)
            {
                for (int j = i + 1; j < positions.Count; j++)
                {
                    float dx = positions[j].x - positions[i].x;
                    float dz = positions[j].z - positions[i].z;
                    float distance = Mathf.Sqrt(dx * dx + dz * dz);
                    float overlap = PersonalRadius * 2f - distance;
                    if (overlap <= 0f)
                    {
                        continue;
                    }

                    Vector3 direction = distance > 1e-4f
                        ? new Vector3(dx / distance, 0f, dz / distance)
                        : Vector3.right;
                    float push = Mathf.Min(MaxPushPerStep, overlap * 0.5f);

                    if (velocities[i].magnitude < FastSpeed)
                    {
                        displacements[i] -= direction * push;
                    }

                    if (velocities[j].magnitude < FastSpeed)
                    {
                        displacements[j] += direction * push;
                    }
                }
            }
        }
    }
}
