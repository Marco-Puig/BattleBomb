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

        /// <summary>The lunge stops this far from its target's centre instead of standing inside it.</summary>
        public const float LungeStandoff = 0.75f;

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
        /// The slam's landing burst: every candidate within <c>ReachX</c> of the centre on the
        /// ground plane, facing-free — an AoE crosses depth by construction. Nearest first on
        /// planar distance, capped at <c>MaxTargets</c>. Returns the number of hits.
        /// </summary>
        public static int ResolveRadial(
            Vector3 center,
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
                float distance = PlanarSqrDistance(center, candidates[i]);
                if (distance > attack.ReachX * attack.ReachX
                    || Mathf.Abs(candidates[i].y - center.y) > VerticalTolerance)
                {
                    continue;
                }

                int insertAt = hits.Count;
                for (int h = 0; h < hits.Count; h++)
                {
                    if (distance < PlanarSqrDistance(center, candidates[hits[h]]))
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
        /// The soft lunge's target: the nearest candidate the lunge can actually convert into a hit
        /// — after travelling <see cref="LungeEnd"/> the candidate must sit inside the reach box.
        /// −1 when nothing qualifies. Chosen once, on the step the attack starts, and never
        /// re-picked mid-swing. Without the convertibility check, a flush target the attacker
        /// slightly overran would be skipped for a deep diagonal the lunge cannot reach, dragging
        /// the swing away from a guaranteed hit into a guaranteed whiff.
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
                if (forward < -BehindTolerance || forward > attack.ReachX + attack.LungeDistance)
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

                if (!IsInReach(LungeEnd(attacker, attack, candidate), facing, attack, candidate))
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

        /// <summary>
        /// Where a lunge toward the target ends: the planar gap minus the standoff, capped at the
        /// attack's lunge distance. The picker predicts with this and the mover travels with it, so
        /// they can never disagree.
        /// </summary>
        public static Vector3 LungeEnd(Vector3 attacker, in AttackTuning attack, Vector3 target)
        {
            Vector3 toTarget = target - attacker;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            float travel = Mathf.Min(Mathf.Max(0f, distance - LungeStandoff), attack.LungeDistance);
            if (distance < 1e-4f || travel <= 0f)
            {
                return attacker;
            }

            return attacker + toTarget / distance * travel;
        }

        private static float PlanarSqrDistance(Vector3 a, Vector3 b)
        {
            float dx = b.x - a.x;
            float dz = b.z - a.z;
            return dx * dx + dz * dz;
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
