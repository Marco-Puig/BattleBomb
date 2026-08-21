using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Loot;
using BattleBomb.Core.Movement;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// D22's elites, deferred since M3 and landing with the loot loop they exist to feed: a rare
    /// spawn modifier, tougher in exactly two ways, always dropping, at a quality bonus.
    /// </summary>
    public sealed class EliteTests
    {
        private static EnemySpec Grunt() => new EnemySpec(
            new EnemyTuning(
                EnemyArchetype.Grunt,
                new AttackTuning(
                    startupSteps: 30, activeSteps: 6, recoverySteps: 24, damage: 10f,
                    reachX: 1.4f, depthTolerance: 1f, lungeDistance: 0f, maxTargets: 1,
                    knockbackSpeed: 4f, launchSpeed: 0f, hitstopSteps: 3, moveSpeedScale: 1f),
                cooldownSteps: 60,
                interruptible: true,
                staggerSteps: 15,
                element: ElementId.None,
                projectileSpeed: 0f,
                standoffNearX: 0f,
                standoffFarX: 0f),
            MovementTuning.Default,
            maxHealth: 40f,
            resistances: default,
            rank: 2,
            xpReward: 25);

        [Test]
        public void An_elite_is_rare()
        {
            EliteRules rules = EliteRules.Default;
            var rng = new DeterministicRandom(4242u);
            int elites = 0;
            const int spawns = 12000;

            for (int i = 0; i < spawns; i++)
            {
                rng = rules.Roll(rng, out bool isElite);
                if (isElite)
                {
                    elites++;
                }
            }

            Assert.That((float)elites / spawns, Is.EqualTo(rules.Chance).Within(0.015f),
                "About one spawn in twelve — rare enough that spotting one is an event.");
        }

        [Test]
        public void The_spawn_draw_always_costs_one_number()
        {
            var a = new DeterministicRandom(5u);
            var b = new DeterministicRandom(5u);

            a = new EliteRules(0f, 2.5f, 1.3f, 1.08f).Roll(a, out bool never);
            b = new EliteRules(1f, 2.5f, 1.3f, 1.08f).Roll(b, out bool always);

            a.NextFloat(out float afterA);
            b.NextFloat(out float afterB);

            Assert.That(never, Is.False);
            Assert.That(always, Is.True);
            Assert.That(afterA, Is.EqualTo(afterB),
                "Elite or not, the stream lands in the same place (D10).");
        }

        [Test]
        public void An_elite_is_tougher_in_exactly_two_ways()
        {
            EnemySpec grunt = Grunt();
            EnemySpec elite = EliteSpec.Promote(grunt, EliteRules.Default);

            Assert.That(elite.MaxHealth, Is.EqualTo(100f), "40 × 2.5");
            Assert.That(elite.Tuning.Attack.Damage, Is.EqualTo(13f).Within(1e-4f), "10 × 1.3");

            Assert.That(elite.Tuning.Attack.StartupSteps, Is.EqualTo(grunt.Tuning.Attack.StartupSteps),
                "The telegraph is the contract — an elite must stay readable (D22/D28).");
            Assert.That(elite.Tuning.Attack.ReachX, Is.EqualTo(grunt.Tuning.Attack.ReachX));
            Assert.That(elite.Tuning.CooldownSteps, Is.EqualTo(grunt.Tuning.CooldownSteps));
            Assert.That(elite.Tuning.Archetype, Is.EqualTo(grunt.Tuning.Archetype));
            Assert.That(elite.Rank, Is.EqualTo(grunt.Rank), "Rank composes encounters; it is not a stat.");
        }

        [Test]
        public void Promotion_keeps_the_brutes_uninterruptible_stubbornness()
        {
            EnemySpec brute = Grunt();
            EnemySpec elite = EliteSpec.Promote(brute, EliteRules.Default);

            Assert.That(elite.Tuning.Interruptible, Is.EqualTo(brute.Tuning.Interruptible));
            Assert.That(elite.Tuning.TakesTurns, Is.EqualTo(brute.Tuning.TakesTurns));
        }

        [Test]
        public void An_elite_always_drops()
        {
            var rng = new DeterministicRandom(77u);

            for (int i = 0; i < 500; i++)
            {
                rng = DropRoll.Roll(
                    rng, rank: 0, progress: 1f, difficulty: 1f, multipliers: 1f,
                    isElite: true, eliteBonus: 1.08f, out DropDecision decision);
                Assert.That(decision.Dropped, Is.True,
                    "It wears the reward — a kill that produced nothing would be a lie (D22).");
            }
        }

        [Test]
        public void An_ordinary_kill_still_rolls_the_rank_chance()
        {
            var rng = new DeterministicRandom(77u);
            int drops = 0;
            const int kills = 4000;

            for (int i = 0; i < kills; i++)
            {
                rng = DropRoll.Roll(
                    rng, rank: 0, progress: 1f, difficulty: 1f, multipliers: 1f,
                    isElite: false, eliteBonus: 1.08f, out DropDecision decision);
                if (decision.Dropped)
                {
                    drops++;
                }
            }

            Assert.That((float)drops / kills, Is.EqualTo(DropRoll.BaseChance).Within(0.02f),
                "The guarantee is the elite's alone.");
        }

        [Test]
        public void The_elite_bonus_lifts_the_quality_score()
        {
            DropRoll.Roll(
                new DeterministicRandom(9u), rank: 3, progress: 2f, difficulty: 1f,
                multipliers: 1f, isElite: false, eliteBonus: 1.08f, out DropDecision ordinary);
            DropRoll.Roll(
                new DeterministicRandom(9u), rank: 3, progress: 2f, difficulty: 1f,
                multipliers: 1f, isElite: true, eliteBonus: 1.08f, out DropDecision elite);

            Assert.That(elite.Quality, Is.EqualTo(ordinary.Quality * 1.08f).Within(1e-4f),
                "Same seed, same spread — the bonus is the only difference (D23).");
        }
    }
}
