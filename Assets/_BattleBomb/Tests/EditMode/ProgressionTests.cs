using BattleBomb.Core.Progression;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// D24's endless ladder (task 42): the curve's price, level-ups with overflow, the wall at
    /// 99, and the prestige reset that banks a permanent point while re-locking everything else.
    /// </summary>
    public sealed class ProgressionTests
    {
        private static readonly XpCurve Curve = XpCurve.Default;

        [Test]
        public void Fresh_starts_at_level_one_with_nothing()
        {
            XpLedger ledger = XpLedger.Fresh;

            Assert.That(ledger.Level, Is.EqualTo(1));
            Assert.That(ledger.XpIntoLevel, Is.Zero);
            Assert.That(ledger.UnspentPoints, Is.Zero);
            Assert.That(ledger.Allocations.Total, Is.Zero);
            Assert.That(ledger.PrestigeCount, Is.Zero);
        }

        [Test]
        public void The_curve_grows_polynomially_and_prices_cycles_up()
        {
            Assert.That(Curve.XpToNext(1, 0), Is.EqualTo(40f).Within(1e-3f));
            Assert.That(Curve.XpToNext(4, 0), Is.EqualTo(320f).Within(1e-3f), "4^1.5 is 8");
            Assert.That(Curve.XpToNext(1, 1), Is.EqualTo(50f).Within(1e-3f),
                "the second cycle costs a quarter more");
            Assert.That(Curve.XpToNext(1, 2), Is.EqualTo(62.5f).Within(1e-3f));
        }

        [Test]
        public void Earning_exactly_one_level_banks_one_point()
        {
            XpLedger ledger = XpLedger.Fresh.Earn(Curve.XpToNext(1, 0), Curve);

            Assert.That(ledger.Level, Is.EqualTo(2));
            Assert.That(ledger.XpIntoLevel, Is.Zero);
            Assert.That(ledger.UnspentPoints, Is.EqualTo(1));
        }

        [Test]
        public void Overflow_carries_across_multiple_levels()
        {
            float xp = Curve.XpToNext(1, 0) + Curve.XpToNext(2, 0) + 5f;
            XpLedger ledger = XpLedger.Fresh.Earn(xp, Curve);

            Assert.That(ledger.Level, Is.EqualTo(3));
            Assert.That(ledger.XpIntoLevel, Is.EqualTo(5f).Within(1e-3f));
            Assert.That(ledger.UnspentPoints, Is.EqualTo(2));
        }

        [Test]
        public void Ninety_nine_is_a_wall_until_prestige()
        {
            var nearTop = new XpLedger(98, 0f, 0, BaseStats.Zero, 0);
            XpLedger capped = nearTop.Earn(1e9f, Curve);

            Assert.That(capped.Level, Is.EqualTo(99));
            Assert.That(capped.XpIntoLevel, Is.Zero, "XP at the wall is discarded");
            Assert.That(capped.Earn(500f, Curve).XpIntoLevel, Is.Zero);
            Assert.That(capped.CanPrestige(Curve), Is.True);
        }

        [Test]
        public void Prestige_resets_everything_and_banks_the_permanent_pool()
        {
            var atTop = new XpLedger(99, 0f, 3, new BaseStats(50, 30, 10, 5), 0);
            XpLedger first = atTop.Prestige(Curve);

            Assert.That(first.Level, Is.EqualTo(1));
            Assert.That(first.Allocations.Total, Is.Zero, "the allocations reset with the level");
            Assert.That(first.UnspentPoints, Is.EqualTo(1), "one permanent point, re-allocatable");
            Assert.That(first.PrestigeCount, Is.EqualTo(1));

            var secondTop = new XpLedger(99, 0f, 0, new BaseStats(90, 0, 0, 9), 1);
            XpLedger second = secondTop.Prestige(Curve);

            Assert.That(second.UnspentPoints, Is.EqualTo(2), "the pool is the whole badge count");
            Assert.That(second.PrestigeCount, Is.EqualTo(2));
        }

        [Test]
        public void Prestige_below_the_wall_is_a_no_op()
        {
            var midway = new XpLedger(50, 12f, 4, new BaseStats(20, 10, 10, 9), 0);
            XpLedger after = midway.Prestige(Curve);

            Assert.That(after.Level, Is.EqualTo(50));
            Assert.That(after.UnspentPoints, Is.EqualTo(4));
            Assert.That(after.PrestigeCount, Is.Zero);
        }

        [Test]
        public void Spending_moves_points_into_allocations_and_never_overdraws()
        {
            var ledger = new XpLedger(5, 0f, 2, BaseStats.Zero, 0);

            ledger = ledger.Spend(StatId.Strength).Spend(StatId.Speed);
            Assert.That(ledger.Allocations.Strength, Is.EqualTo(1));
            Assert.That(ledger.Allocations.Speed, Is.EqualTo(1));
            Assert.That(ledger.UnspentPoints, Is.Zero);

            XpLedger overdrawn = ledger.Spend(StatId.Hp);
            Assert.That(overdrawn.Allocations.Hp, Is.Zero, "an empty bank spends nothing");
        }

        [Test]
        public void Negative_or_zero_xp_never_moves_the_ledger()
        {
            XpLedger ledger = XpLedger.Fresh.Earn(-50f, Curve).Earn(0f, Curve);

            Assert.That(ledger.Level, Is.EqualTo(1));
            Assert.That(ledger.XpIntoLevel, Is.Zero);
        }
    }
}
