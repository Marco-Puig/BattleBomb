using BattleBomb.Core.Loot;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The D23 drop seam (task 36): the project's first randomness must replay exactly from a
    /// seed, and the roll's chance, quality compounding, and stream stability are the contract
    /// M4/M6 build on.
    /// </summary>
    public sealed class LootTests
    {
        [Test]
        public void The_same_seed_replays_the_same_sequence()
        {
            DeterministicRandom a = new DeterministicRandom(1234u);
            DeterministicRandom b = new DeterministicRandom(1234u);

            for (int i = 0; i < 100; i++)
            {
                a = a.Next(out uint fromA);
                b = b.Next(out uint fromB);
                Assert.That(fromA, Is.EqualTo(fromB), $"diverged at draw {i}");
            }
        }

        [Test]
        public void Different_seeds_diverge()
        {
            new DeterministicRandom(1u).Next(out uint fromOne);
            new DeterministicRandom(2u).Next(out uint fromTwo);

            Assert.That(fromOne, Is.Not.EqualTo(fromTwo));
        }

        [Test]
        public void A_zero_seed_still_produces_a_working_stream()
        {
            DeterministicRandom rng = new DeterministicRandom(0u);
            Assert.That(rng.State, Is.Not.Zero);

            rng.Next(out uint value);
            Assert.That(value, Is.Not.Zero);
        }

        [Test]
        public void Float_draws_stay_inside_the_unit_interval()
        {
            DeterministicRandom rng = new DeterministicRandom(99u);
            for (int i = 0; i < 1000; i++)
            {
                rng = rng.NextFloat(out float value);
                Assert.That(value, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f), $"draw {i} escaped");
            }
        }

        [Test]
        public void Chance_scales_with_rank_and_clamps()
        {
            Assert.That(DropRoll.Chance(0), Is.EqualTo(0.10f));
            Assert.That(DropRoll.Chance(3), Is.EqualTo(0.25f));
            Assert.That(DropRoll.Chance(20), Is.EqualTo(DropRoll.ChanceCap));
            Assert.That(DropRoll.Chance(-5), Is.EqualTo(DropRoll.BaseChance), "negative rank never discounts below base");
        }

        [Test]
        public void Identical_inputs_roll_identically()
        {
            DeterministicRandom rng = new DeterministicRandom(777u);

            DeterministicRandom afterA = DropRoll.Roll(rng, 2, 1f, 1f, 1f, false, 1.5f, out DropDecision a);
            DeterministicRandom afterB = DropRoll.Roll(rng, 2, 1f, 1f, 1f, false, 1.5f, out DropDecision b);

            Assert.That(a.Dropped, Is.EqualTo(b.Dropped));
            Assert.That(a.Quality, Is.EqualTo(b.Quality));
            Assert.That(afterA.State, Is.EqualTo(afterB.State));
        }

        [Test]
        public void The_roll_consumes_the_same_randomness_whether_or_not_anything_drops()
        {
            DeterministicRandom rng = new DeterministicRandom(31u);

            DeterministicRandom afterLikely = DropRoll.Roll(rng, 20, 1f, 1f, 1f, false, 1f, out _);
            DeterministicRandom afterUnlikely = DropRoll.Roll(rng, 0, 1f, 1f, 1f, false, 1f, out _);

            Assert.That(afterLikely.State, Is.EqualTo(afterUnlikely.State),
                "the stream must advance identically so later rolls never depend on earlier outcomes");
        }

        [Test]
        public void Quality_compounds_every_input_and_the_elite_bonus_applies_only_to_elites()
        {
            DeterministicRandom rng = FindDroppingState(new DeterministicRandom(5u), 20);

            DropRoll.Roll(rng, 20, 1f, 1f, 1f, false, 2f, out DropDecision baseline);
            DropRoll.Roll(rng, 20, 2f, 1f, 1f, false, 2f, out DropDecision progressed);
            DropRoll.Roll(rng, 20, 1f, 3f, 1f, false, 2f, out DropDecision harder);
            DropRoll.Roll(rng, 20, 1f, 1f, 1.5f, false, 2f, out DropDecision boosted);
            DropRoll.Roll(rng, 20, 2f, 3f, 1.5f, false, 2f, out DropDecision compounded);
            DropRoll.Roll(rng, 20, 1f, 1f, 1f, true, 2f, out DropDecision elite);

            Assert.That(baseline.Dropped, Is.True, "the setup must find a dropping state");
            Assert.That(progressed.Quality, Is.EqualTo(baseline.Quality * 2f).Within(1e-4f));
            Assert.That(harder.Quality, Is.EqualTo(baseline.Quality * 3f).Within(1e-4f));
            Assert.That(boosted.Quality, Is.EqualTo(baseline.Quality * 1.5f).Within(1e-4f));
            Assert.That(compounded.Quality, Is.EqualTo(baseline.Quality * 9f).Within(1e-3f));
            Assert.That(elite.Quality, Is.EqualTo(baseline.Quality * 2f).Within(1e-4f),
                "the elite bonus multiplies only when the kill was an elite");
        }

        [Test]
        public void Nothing_dropped_carries_no_quality()
        {
            DeterministicRandom rng = FindNonDroppingState(new DeterministicRandom(11u), 0);

            DropRoll.Roll(rng, 0, 5f, 5f, 5f, true, 5f, out DropDecision decision);

            Assert.That(decision.Dropped, Is.False, "the setup must find a non-dropping state");
            Assert.That(decision.Quality, Is.Zero);
        }

        [Test]
        public void Drops_land_near_the_authored_rate()
        {
            DeterministicRandom rng = new DeterministicRandom(1u);
            int dropped = 0;
            for (int i = 0; i < 2000; i++)
            {
                rng = DropRoll.Roll(rng, 0, 1f, 1f, 1f, false, 1f, out DropDecision decision);
                if (decision.Dropped)
                {
                    dropped++;
                }
            }

            Assert.That(dropped, Is.InRange(120, 280), "rank 0 pays out near its authored 10%");
        }

        private static DeterministicRandom FindDroppingState(DeterministicRandom rng, int rank)
        {
            for (int i = 0; i < 100; i++)
            {
                DropRoll.Roll(rng, rank, 1f, 1f, 1f, false, 1f, out DropDecision decision);
                if (decision.Dropped)
                {
                    return rng;
                }

                rng = rng.Next(out _);
            }

            Assert.Fail("no dropping state found in 100 tries");
            return rng;
        }

        private static DeterministicRandom FindNonDroppingState(DeterministicRandom rng, int rank)
        {
            for (int i = 0; i < 100; i++)
            {
                DropRoll.Roll(rng, rank, 1f, 1f, 1f, false, 1f, out DropDecision decision);
                if (!decision.Dropped)
                {
                    return rng;
                }

                rng = rng.Next(out _);
            }

            Assert.Fail("no non-dropping state found in 100 tries");
            return rng;
        }
    }
}
