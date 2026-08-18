using System.Collections.Generic;
using BattleBomb.Core.Movement;
using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// Pure hit geometry. Melee is depth-limited (§2.2) — a candidate several lane units away in Z
    /// is missed no matter how aligned it is in X — and the soft lunge exists precisely to turn a
    /// near-miss in depth into a hit. Callers pass positions; nothing here knows about objects.
    /// </summary>
    public static class HitResolver
    {
        /// <summary>How far behind the attacker's back a flush target still counts.</summary>
        public const float BehindTolerance = 0.35f;

        /// <summary>Vertical slack so a just-launched target stays hittable.</summary>
        public const float VerticalTolerance = 2.5f;

        /// <summary>
        /// Fills <paramref name="hits"/> with the indices of struck candidates, nearest first on
        /// |Δx|, capped at the attack's <c>MaxTargets</c>. Returns the number of hits.
        /// </summary>
        public static int Resolve(
            Vector3 attacker,
            Facing facing,
            in AttackTuning attack,
            IReadOnlyList<Vector3> candidates,
            IList<int> hits)
        {
            hits.Clear();
            if (candidates == null)
            {
                return 0;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (!IsInReach(attacker, facing, attack, candidates[i]))
                {
                    continue;
                }

                float distance = Mathf.Abs(candidates[i].x - attacker.x);
                int insertAt = hits.Count;
                for (int h = 0; h < hits.Count; h++)
                {
                    if (distance < Mathf.Abs(candidates[hits[h]].x - attacker.x))
                    {
                        insertAt = h;
                        break;
                    }
                }

                hits.Insert(insertAt, i);
                if (hits.Count > attack.MaxTargets)
                {
                    hits.RemoveAt(hits.Count - 1);
                }
            }

            return hits.Count;
        }

        /// <summary>
        /// The soft lunge's target: the nearest candidate in front within the attack's reach plus
        /// its lunge distance, on both X and depth. −1 when nothing qualifies. Chosen once, on the
        /// step the attack starts, and never re-picked mid-swing.
        /// </summary>
        public static int LungeTarget(
            Vector3 attacker,
            Facing facing,
            in AttackTuning attack,
            IReadOnlyList<Vector3> candidates)
        {
            if (candidates == null)
            {
                return -1;
            }

            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3 candidate = candidates[i];
                float forward = (candidate.x - attacker.x) * (int)facing;
                if (forward < 0f || forward > attack.ReachX + attack.LungeDistance)
                {
                    continue;
                }

                float depth = Mathf.Abs(candidate.z - attacker.z);
                if (depth > attack.DepthTolerance + attack.LungeDistance)
                {
                    continue;
                }

                if (Mathf.Abs(candidate.y - attacker.y) > VerticalTolerance)
                {
                    continue;
                }

                float distance = forward * forward + depth * depth;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        private static bool IsInReach(Vector3 attacker, Facing facing, in AttackTuning attack, Vector3 candidate)
        {
            float forward = (candidate.x - attacker.x) * (int)facing;
            if (forward < -BehindTolerance || forward > attack.ReachX)
            {
                return false;
            }

            if (Mathf.Abs(candidate.z - attacker.z) > attack.DepthTolerance)
            {
                return false;
            }

            return Mathf.Abs(candidate.y - attacker.y) <= VerticalTolerance;
        }
    }
}
