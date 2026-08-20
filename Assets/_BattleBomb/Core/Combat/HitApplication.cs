using BattleBomb.Core.Movement;
using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// Turns a landed hit into consequences, so Gameplay wires rather than decides. Damage runs the
    /// full §4 pipeline; knockback points away from the attacker on X and Z (a shove works in depth
    /// too) and grows with the speed the attacker drove into the target — saturating, so a
    /// speed-stacked build hits harder but never launches an enemy across the screen (Michael's
    /// playtest rule). The launcher's <c>LaunchSpeed</c> becomes upward velocity. Partners:
    /// knockback only (D21).
    /// </summary>
    public static class HitApplication
    {
        /// <summary>The most extra knockback speed momentum can ever add.</summary>
        public const float MomentumBonusCap = 6f;

        /// <summary>Attacker speed into the target at which half the cap is earned.</summary>
        public const float MomentumHalfSaturation = 6f;

        public static HitResult Apply(
            in AttackTuning attack,
            Vector3 attackerPosition,
            Facing attackerFacing,
            Vector3 attackerMomentum,
            ElementId element,
            float gearMultiplier,
            TargetKind targetKind,
            Vector3 targetPosition,
            in ElementalDefence targetDefence,
            in ElementalMultipliers climate)
        {
            Vector3 away = targetPosition - attackerPosition;
            away.y = 0f;
            Vector3 direction = away.sqrMagnitude > 1e-6f
                ? away.normalized
                : new Vector3((int)attackerFacing, 0f, 0f);

            // Only speed driven along the shove counts: a backpedal or depth strafe adds nothing,
            // and vertical speed is never momentum. The lunge snap's scripted travel never reaches
            // here — callers pass the motor velocity carried into the swing.
            Vector3 momentum = attackerMomentum;
            momentum.y = 0f;
            float drive = Mathf.Max(0f, Vector3.Dot(momentum, direction));
            float bonus = MomentumBonusCap * drive / (drive + MomentumHalfSaturation);

            Vector3 impulse = direction * (attack.KnockbackSpeed + bonus);
            impulse.y = attack.LaunchSpeed;

            if (targetKind == TargetKind.Partner)
            {
                return new HitResult(0f, impulse, 0);
            }

            float damage = DamageCalculator.Resolve(
                attack.Damage, element, targetDefence, climate, gearMultiplier);
            return new HitResult(damage, impulse, attack.HitstopSteps);
        }
    }
}
