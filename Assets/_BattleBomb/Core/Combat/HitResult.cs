using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// The consequences of one landed hit: resolved damage, the velocity to inject into the
    /// target's body, and hitstop for both parties. Zero damage and zero hitstop for partners (D21).
    /// </summary>
    public readonly struct HitResult
    {
        public readonly float Damage;
        public readonly Vector3 Impulse;
        public readonly int HitstopSteps;

        public HitResult(float damage, Vector3 impulse, int hitstopSteps)
        {
            Damage = damage;
            Impulse = impulse;
            HitstopSteps = hitstopSteps;
        }
    }
}
