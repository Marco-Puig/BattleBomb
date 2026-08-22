using BattleBomb.Core.Combat;

namespace BattleBomb.Core.Enemies
{
    /// <summary>Health and attack damage scaled, everything else untouched — the one place
    /// elites (D22) and tiers (D50) both go to get tougher, so they cannot drift apart.</summary>
    public static class EnemyScaling
    {
        public static EnemySpec Scale(in EnemySpec spec, float healthMultiplier, float damageMultiplier)
        {
            AttackTuning attack = spec.Tuning.Attack;
            var harder = new AttackTuning(
                attack.StartupSteps,
                attack.ActiveSteps,
                attack.RecoverySteps,
                attack.Damage * damageMultiplier,
                attack.ReachX,
                attack.DepthTolerance,
                attack.LungeDistance,
                attack.MaxTargets,
                attack.KnockbackSpeed,
                attack.LaunchSpeed,
                attack.HitstopSteps,
                attack.MoveSpeedScale,
                attack.ResolvesOnLanding,
                attack.IsRadial,
                attack.StunSteps);

            EnemyTuning tuning = spec.Tuning;
            var promoted = new EnemyTuning(
                tuning.Archetype,
                harder,
                tuning.CooldownSteps,
                tuning.Interruptible,
                tuning.StaggerSteps,
                tuning.Element,
                tuning.ProjectileSpeed,
                tuning.StandoffNearX,
                tuning.StandoffFarX,
                tuning.HoverDistanceX,
                tuning.StrafePeriodSteps,
                tuning.HopPulseSteps,
                tuning.TakesTurns);

            return new EnemySpec(
                promoted,
                spec.Movement,
                spec.MaxHealth * healthMultiplier,
                spec.Resistances,
                spec.Rank,
                spec.XpReward);
        }
    }
}
