using System.Collections.Generic;
using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// Pure projectile maths: straight-line flight on the fixed step and a spherical hit test.
    /// At 60 Hz and authored speeds a projectile moves a fraction of the hit radius per step, so
    /// no swept test is needed — revisit only if a projectile ever outruns its own radius.
    /// </summary>
    public static class ProjectileSimulation
    {
        public static ProjectileState Step(in ProjectileState p, float dt) => new ProjectileState(
            p.Position + p.Velocity * dt, p.Velocity, p.Damage, p.Element, p.LifeSteps - 1);

        /// <summary>
        /// The nearest target within <paramref name="radius"/> of the projectile, or −1. The test
        /// is spherical on purpose: a bolt flies at its firing height, so jumping over it works.
        /// </summary>
        public static int HitTest(in ProjectileState p, IReadOnlyList<Vector3> targets, float radius)
        {
            if (targets == null)
            {
                return -1;
            }

            int hit = -1;
            float nearest = radius * radius;
            for (int i = 0; i < targets.Count; i++)
            {
                float distance = (targets[i] - p.Position).sqrMagnitude;
                if (distance <= nearest)
                {
                    nearest = distance;
                    hit = i;
                }
            }

            return hit;
        }
    }
}
