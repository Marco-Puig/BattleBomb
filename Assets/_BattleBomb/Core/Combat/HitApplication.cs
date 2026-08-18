using BattleBomb.Core.Movement;
using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// Turns a landed hit into consequences, so Gameplay wires rather than decides. Damage runs the
    /// full §4 pipeline; knockback points away from the attacker on X and Z (a shove works in depth
    /// too); the launcher's <c>LaunchSpeed</c> becomes upward velocity. Partners: knockback only (D21).
    /// </summary>
    public static class HitApplication
    {
        public static HitResult Apply(
            in AttackTuning attack,
            Vector3 attackerPosition,
            Facing attackerFacing,
            Element element,
            float gearMultiplier,
            TargetKind targetKind,
            Vector3 targetPosition,
            in ElementalMultipliers targetResistance,
            in ElementalMultipliers climate)
        {
            Vector3 away = targetPosition - attackerPosition;
            away.y = 0f;
            Vector3 direction = away.sqrMagnitude > 1e-6f
                ? away.normalized
                : new Vector3((int)attackerFacing, 0f, 0f);

            Vector3 impulse = direction * attack.KnockbackSpeed;
            impulse.y = attack.LaunchSpeed;

            if (targetKind == TargetKind.Partner)
            {
                return new HitResult(0f, impulse, 0);
            }

            float damage = DamageCalculator.Resolve(
                attack.Damage, element, targetResistance, climate, gearMultiplier);
            return new HitResult(damage, impulse, attack.HitstopSteps);
        }
    }
}
