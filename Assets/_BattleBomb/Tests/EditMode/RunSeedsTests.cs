using System.Collections.Generic;
using BattleBomb.Core.Loot;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    public sealed class RunSeedsTests
    {
        [Test]
        public void The_same_draw_gives_the_same_seeds()
        {
            Assert.That(RunSeeds.From(12345u), Is.EqualTo(RunSeeds.From(12345u)));
        }

        [Test]
        public void Neighbouring_draws_give_unrelated_seeds()
        {
            RunSeeds a = RunSeeds.From(1u);
            RunSeeds b = RunSeeds.From(2u);
            Assert.That(a.Loot, Is.Not.EqualTo(b.Loot));
            Assert.That(a.Combat, Is.Not.EqualTo(b.Combat));
            Assert.That(a.Spawn, Is.Not.EqualTo(b.Spawn));
        }

        [Test]
        public void The_three_streams_are_never_zero_and_never_share_a_seed()
        {
            var seen = new HashSet<uint>();
            for (uint draw = 0; draw < 2000; draw++)
            {
                RunSeeds seeds = RunSeeds.From(draw);
                Assert.That(seeds.Loot, Is.Not.Zero);
                Assert.That(seeds.Combat, Is.Not.Zero);
                Assert.That(seeds.Spawn, Is.Not.Zero);
                Assert.That(seeds.Loot != seeds.Combat && seeds.Combat != seeds.Spawn && seeds.Loot != seeds.Spawn, Is.True,
                    $"Draw {draw} gave two streams one seed — they would roll in lockstep.");
                seen.Add(seeds.Loot);
            }

            Assert.That(seen.Count, Is.GreaterThan(1990), "Loot seeds collide far more often than chance.");
        }
    }
}
