using BattleBomb.Core.Items;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// D43's economy rules: the wallet, and the one price rulebook every coin flows through.
    /// The pinned integers are the paper table made law — retuning them is editing
    /// <see cref="PriceBook.Default"/> and this file together, deliberately.
    /// </summary>
    public sealed class EconomyTests
    {
        // ── The wallet ────────────────────────────────────────────────────────────────

        [Test]
        public void A_wallet_earns_and_spends()
        {
            Wallet wallet = Wallet.Empty.Earned(100).Spent(30);

            Assert.That(wallet.Balance, Is.EqualTo(70));
        }

        [Test]
        public void Affordability_includes_the_exact_balance_and_excludes_debt()
        {
            Wallet wallet = Wallet.Empty.Earned(50);

            Assert.That(wallet.CanAfford(50), Is.True);
            Assert.That(wallet.CanAfford(51), Is.False);
            Assert.That(wallet.CanAfford(-1), Is.False, "A negative price is never a purchase.");
        }

        [Test]
        public void Money_never_goes_negative_and_negative_earnings_mint_nothing()
        {
            Assert.That(Wallet.Empty.Earned(10).Spent(25).Balance, Is.Zero);
            Assert.That(Wallet.Empty.Earned(-10).Balance, Is.Zero);
        }

        // ── Selling: the only faucet ─────────────────────────────────────────────────

        [Test]
        public void The_paper_sell_table_holds()
        {
            PriceBook book = PriceBook.Default;

            Assert.That(book.SellPrice(QualityRank.Rusty, 5), Is.EqualTo(29));
            Assert.That(book.SellPrice(QualityRank.Shiny, 12), Is.EqualTo(71));
            Assert.That(book.SellPrice(QualityRank.Godly, 60), Is.EqualTo(2611));
        }

        [Test]
        public void Each_rank_up_the_ladder_doubles_the_payout()
        {
            PriceBook book = PriceBook.Default;

            float ratio = (float)book.SellPrice(QualityRank.Pristine, 20)
                / book.SellPrice(QualityRank.Shiny, 20);

            Assert.That(ratio, Is.EqualTo(2f).Within(0.05f));
        }

        [Test]
        public void Income_scales_with_level_forever()
        {
            PriceBook book = PriceBook.Default;

            Assert.That(
                book.SellPrice(QualityRank.Godly, 90),
                Is.GreaterThan(book.SellPrice(QualityRank.Godly, 60)),
                "A prestige-deep player's castoffs must keep paying more (D43): the faucet " +
                "inherits the ladder's endless scaling.");
        }

        [Test]
        public void Even_trash_pays_a_coin()
        {
            var freeloader = new PriceBook(0f, 2f, 0.04f, 3f, 2f);

            Assert.That(freeloader.SellPrice(QualityRank.Nothing, 1), Is.EqualTo(1),
                "Every grabbed drop is worth something, or grabbing stops being worth doing.");
        }

        // ── The shop's premium ───────────────────────────────────────────────────────

        [Test]
        public void The_shop_charges_the_premium_over_what_it_pays()
        {
            PriceBook book = PriceBook.Default;

            Assert.That(book.BuyPrice(QualityRank.Rusty, 5), Is.EqualTo(86),
                "3× the raw sell value (28.8), rounded — selling to buy back is a losing trade.");
        }

        // ── The upgrade sink (D44) ───────────────────────────────────────────────────

        [Test]
        public void Upgrade_steps_double_and_sum_to_the_share_of_the_sell_price()
        {
            PriceBook book = PriceBook.Default;
            int sell = book.SellPrice(QualityRank.Godly, 60);

            int[] steps =
            {
                book.UpgradeCost(QualityRank.Godly, 60, capacity: 4, spent: 0),
                book.UpgradeCost(QualityRank.Godly, 60, capacity: 4, spent: 1),
                book.UpgradeCost(QualityRank.Godly, 60, capacity: 4, spent: 2),
                book.UpgradeCost(QualityRank.Godly, 60, capacity: 4, spent: 3),
            };

            Assert.That(steps, Is.EqualTo(new[] { 348, 696, 1393, 2785 }));
            Assert.That(steps[0] + steps[1] + steps[2] + steps[3],
                Is.EqualTo(2 * sell).Within(0.01f * sell),
                "Maxing an item costs about two of its own castoffs, whatever its capacity.");
        }

        [Test]
        public void A_small_capacity_pays_the_same_share_in_bigger_bites()
        {
            PriceBook book = PriceBook.Default;

            int first = book.UpgradeCost(QualityRank.Shiny, 12, capacity: 2, spent: 0);
            int second = book.UpgradeCost(QualityRank.Shiny, 12, capacity: 2, spent: 1);

            Assert.That(first, Is.EqualTo(47));
            Assert.That(second, Is.EqualTo(95));
        }

        [Test]
        public void Exhausted_or_absent_capacity_prices_at_zero()
        {
            PriceBook book = PriceBook.Default;

            Assert.That(book.UpgradeCost(QualityRank.Godly, 60, capacity: 0, spent: 0), Is.Zero);
            Assert.That(book.UpgradeCost(QualityRank.Godly, 60, capacity: 4, spent: 4), Is.Zero);
        }

        [Test]
        public void The_instance_overloads_read_the_item_itself()
        {
            PriceBook book = PriceBook.Default;
            var item = new ItemInstance(
                new ItemIdentity(1, "Shiny Test Blade", ItemSlot.Weapon, WeaponClass.Sword),
                QualityRank.Shiny, new GearContribution(weaponDamage: 20f), new AffixRoll[0],
                requiredLevel: 12, new ItemInvestment(capacity: 2, spent: 1));

            Assert.That(book.SellPrice(item), Is.EqualTo(71));
            Assert.That(book.UpgradeCost(item), Is.EqualTo(95),
                "The second point's price, because one is already spent.");
        }
    }
}
