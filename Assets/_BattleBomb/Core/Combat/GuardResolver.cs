using BattleBomb.Core.Movement;
using UnityEngine;

namespace BattleBomb.Core.Combat
{
    public enum GuardOutcome
    {
        /// <summary>The hit lands in full — not guarding, struck from behind, or Magic.</summary>
        Hit = 0,

        /// <summary>Guarded: no damage, no stagger. A reduced shove may still move the defender.</summary>
        Blocked,

        /// <summary>
        /// Guarded inside the timing window: the defender takes nothing and the attacker is
        /// staggered — every attacker, the brute included. The one interrupt that works on him.
        /// </summary>
        PerfectBlocked,
    }

    /// <summary>
    /// Decides what a guard does to one incoming hit (D19, §2.7). Pure decision — the caller
    /// applies the consequences. Block covers the front only (the same behind-tolerance as
    /// <see cref="HitResolver"/>): positioning still matters while guarding, which is part of
    /// blocking's price alongside inaction and magic vulnerability.
    /// </summary>
    public static class GuardResolver
    {
        public static GuardOutcome Resolve(
            in CombatState defender,
            Facing defenderFacing,
            Vector3 defenderPosition,
            Vector3 attackOrigin,
            HitKind kind,
            int perfectWindowSteps)
        {
            if (kind == HitKind.Magic || defender.Phase != AttackPhase.Guarding)
            {
                return GuardOutcome.Hit;
            }

            float fromFront = (attackOrigin.x - defenderPosition.x) * (int)defenderFacing;
            if (fromFront < -HitResolver.BehindTolerance)
            {
                return GuardOutcome.Hit;
            }

            return defender.StepsInPhase <= perfectWindowSteps
                ? GuardOutcome.PerfectBlocked
                : GuardOutcome.Blocked;
        }
    }
}
