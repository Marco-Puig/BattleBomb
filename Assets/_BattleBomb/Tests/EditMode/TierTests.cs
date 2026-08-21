using BattleBomb.Core.Chapters;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Movement;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>D50: a tier is a row of multipliers, applied by one pure function. The paper
    /// rows are pinned here; retuning them is editing <see cref="TierSpec.Defaults"/> and
    /// this file together.</summary>
    public sealed class TierTests
    {
        private static StageSpec Stage(int levelStamp = 10, float lootProgress = 2f) => new StageSpec(
            "s1", "Stage", "FixtureStage1", new ArenaSpec[0], levelStamp, lootProgress,
            ElementalMultipliers.Neutral);

        [Test]
        public void The_first_tier_changes_nothing()
        {
            EncounterInputs inputs = EncounterInputs.From(TierSpec.Defaults[0], Stage());

            Assert.That(inputs.LevelStamp, Is.EqualTo(10));
            Assert.That(inputs.LootProgress, Is.EqualTo(2f));
            Assert.That(inputs.LootDifficulty, Is.EqualTo(1f));
            Assert.That(inputs.HealthMultiplier, Is.EqualTo(1f));
            Assert.That(inputs.DamageMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void Higher_tiers_bump_the_level_and_multiply_the_rest()
        {
            EncounterInputs hard = EncounterInputs.From(TierSpec.Defaults[1], Stage());
            EncounterInputs nightmare = EncounterInputs.From(TierSpec.Defaults[2], Stage());

            Assert.That(hard.LevelStamp, Is.EqualTo(20), "+10 levels on the paper row.");
            Assert.That(hard.LootDifficulty, Is.EqualTo(1.5f));
            Assert.That(hard.HealthMultiplier, Is.EqualTo(1.6f));
            Assert.That(nightmare.LevelStamp, Is.EqualTo(35));
            Assert.That(nightmare.LootDifficulty, Is.EqualTo(2.25f));
            Assert.That(nightmare.DamageMultiplier, Is.EqualTo(1.7f));
        }

        [Test]
        public void The_stage_supplies_progress_and_climate_and_the_tier_never_touches_them()
        {
            var climate = ElementalMultipliers.From(new[] { new ElementalMultiplier(new ElementId(1), 1.25f) });
            var stage = new StageSpec("s", "S", "Geo", new ArenaSpec[0], 5, 3f, climate);

            EncounterInputs inputs = EncounterInputs.From(TierSpec.Defaults[2], stage);

            Assert.That(inputs.LootProgress, Is.EqualTo(3f), "Progress is the stage's word (D23).");
            Assert.That(inputs.Climate.For(new ElementId(1)), Is.EqualTo(1.25f), "Climate is the stage's word (D41).");
        }

        [Test]
        public void Tiers_are_named_for_the_player_and_numbered_for_the_code()
        {
            Assert.That(TierSpec.Defaults.Length, Is.EqualTo(3));
            Assert.That(TierSpec.Defaults[0].Name, Is.EqualTo("Normal"));
            Assert.That(TierSpec.Defaults[2].Name, Is.EqualTo("Nightmare"));
        }

        [Test]
        public void Enemy_scaling_multiplies_health_and_attack_damage_and_nothing_else()
        {
            EnemySpec spec = Grunt(maxHealth: 30f, damage: 8f);

            EnemySpec scaled = EnemyScaling.Scale(spec, healthMultiplier: 2f, damageMultiplier: 1.5f);

            Assert.That(scaled.MaxHealth, Is.EqualTo(60f));
            Assert.That(scaled.Tuning.Attack.Damage, Is.EqualTo(12f));
            Assert.That(scaled.Tuning.Attack.ReachX, Is.EqualTo(spec.Tuning.Attack.ReachX), "Reach is not a difficulty dial.");
            Assert.That(scaled.XpReward, Is.EqualTo(spec.XpReward));
        }

        [Test]
        public void Elite_promotion_is_the_same_scaling_with_the_elite_rows()
        {
            EnemySpec spec = Grunt(maxHealth: 30f, damage: 8f);

            EnemySpec elite = EliteSpec.Promote(spec, EliteRules.Default);
            EnemySpec scaled = EnemyScaling.Scale(
                spec, EliteRules.Default.HealthMultiplier, EliteRules.Default.DamageMultiplier);

            Assert.That(elite.MaxHealth, Is.EqualTo(scaled.MaxHealth));
            Assert.That(elite.Tuning.Attack.Damage, Is.EqualTo(scaled.Tuning.Attack.Damage));
        }

        private static EnemySpec Grunt(float maxHealth, float damage)
        {
            var attack = new AttackTuning(
                startupSteps: 20, activeSteps: 6, recoverySteps: 20, damage: damage, reachX: 1.2f,
                depthTolerance: 0.6f, lungeDistance: 0f, maxTargets: 1, knockbackSpeed: 4f,
                launchSpeed: 0f, hitstopSteps: 2, moveSpeedScale: 0f, resolvesOnLanding: false,
                isRadial: false, stunSteps: 0);
            var tuning = new EnemyTuning(
                EnemyArchetype.Grunt, attack, 45, true, 20, ElementId.None, 8f, 4f, 7f, 3f, 90, 0, true);
            return new EnemySpec(tuning, MovementTuning.Default, maxHealth, ElementalMultipliers.Neutral, 1, 10);
        }
    }
}
