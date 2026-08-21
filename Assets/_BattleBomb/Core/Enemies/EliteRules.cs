using BattleBomb.Core.Combat;
using BattleBomb.Core.Loot;
using UnityEngine;

namespace BattleBomb.Core.Enemies
{
    /// <summary>
    /// D22's rare per-spawn modifier, as numbers. An elite is any archetype rolled tougher: more
    /// health, a heavier hit, a guaranteed drop at a quality bonus — and it visibly wears what it
    /// will drop, so the fight advertises its own reward.
    /// </summary>
    public readonly struct EliteRules
    {
        /// <summary>How often a spawn comes up elite.</summary>
        public readonly float Chance;

        public readonly float HealthMultiplier;
        public readonly float DamageMultiplier;

        /// <summary>The multiplier D23 applies to the drop's quality score (paper: 5–10%).</summary>
        public readonly float QualityBonus;

        public EliteRules(float chance, float healthMultiplier, float damageMultiplier, float qualityBonus)
        {
            Chance = Mathf.Clamp01(chance);
            HealthMultiplier = Mathf.Max(1f, healthMultiplier);
            DamageMultiplier = Mathf.Max(1f, damageMultiplier);
            QualityBonus = Mathf.Max(1f, qualityBonus);
        }

        /// <summary>HANDOFF-M6's paper numbers: roughly one spawn in twelve.</summary>
        public static EliteRules Default => new EliteRules(
            chance: 1f / 12f,
            healthMultiplier: 2.5f,
            damageMultiplier: 1.3f,
            qualityBonus: 1.08f);

        /// <summary>
        /// The spawn draw. Always consumes exactly one number so the stream advances identically
        /// whether or not this spawn came up elite (D10, task 41's rule).
        /// </summary>
        public DeterministicRandom Roll(in DeterministicRandom rng, out bool isElite)
        {
            DeterministicRandom next = rng.NextFloat(out float draw);
            isElite = draw < Chance;
            return next;
        }
    }

    /// <summary>Turning an ordinary authored enemy into its elite version.</summary>
    public static class EliteSpec
    {
        /// <summary>
        /// The same enemy, tougher. Only health and damage move: an elite is a harder version of
        /// a fight the player already knows how to read, never a different one — the telegraphs,
        /// reach, and rhythm the archetype teaches all survive (D22/D28).
        /// </summary>
        public static EnemySpec Promote(in EnemySpec spec, in EliteRules rules)
        {
            AttackTuning attack = spec.Tuning.Attack;
            var harder = new AttackTuning(
                attack.StartupSteps,
                attack.ActiveSteps,
                attack.RecoverySteps,
                attack.Damage * rules.DamageMultiplier,
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
                spec.MaxHealth * rules.HealthMultiplier,
                spec.Resistances,
                spec.Rank,
                spec.XpReward);
        }
    }
}
