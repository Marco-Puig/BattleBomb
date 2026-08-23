using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// "Clear the junk": <see cref="Inventory.PreviewJunk"/> and <see cref="Inventory.SellJunk"/>
    /// must always agree, must never touch a locked piece, a consumable stack, or worn gear, and
    /// must leave anything at or above the threshold alone.
    /// </summary>
    public sealed class JunkSaleTests
    {
        private const int PotionId = 100;

        private static ItemInstance Gear(
            int id, ItemSlot slot = ItemSlot.Helmet,
            QualityRank quality = QualityRank.Rusty, int requiredLevel = 1) => new ItemInstance(
            new ItemIdentity(id, "Test Piece", slot),
            quality, new GearContribution(defence: 0.05f), new AffixRoll[0], requiredLevel);

        private static ItemInstance Potion(QualityRank quality = QualityRank.Rusty) => new ItemInstance(
            new ItemIdentity(PotionId, "Vial of Health", ItemSlot.Consumable),
            quality, GearContribution.Zero, new AffixRoll[0], requiredLevel: 1,
            consumable: new RestorePayload(RestoreKind.Health, 0.35f));

        private static Inventory Sack(int capacity = 6, int stackLimit = 5)
        {
            return new Inventory { Rules = new SackRules(capacity, stackLimit) };
        }

        private static readonly QualityRank TopRank = (QualityRank)(QualityTable.RankCount - 1);

        [Test]
        public void A_preview_reports_the_same_stacks_pieces_and_coins_the_sale_then_pays()
        {
            Inventory inventory = Sack();
            inventory.Add(Gear(1, quality: QualityRank.Battlescarred), currentLevel: 99);
            inventory.Add(Gear(2, quality: QualityRank.Rusty), currentLevel: 99);

            JunkSale preview = inventory.PreviewJunk(QualityRank.Clean);
            JunkSale sale = inventory.SellJunk(QualityRank.Clean);

            Assert.That(sale.Stacks, Is.EqualTo(preview.Stacks));
            Assert.That(sale.Pieces, Is.EqualTo(preview.Pieces));
            Assert.That(sale.Coins, Is.EqualTo(preview.Coins));
        }

        [Test]
        public void A_preview_changes_nothing_in_the_bag()
        {
            Inventory inventory = Sack();
            inventory.Add(Gear(1, quality: QualityRank.Rusty), currentLevel: 99);
            inventory.Add(Gear(2, quality: QualityRank.Torn), currentLevel: 99);

            inventory.PreviewJunk(QualityRank.Clean);

            Assert.That(inventory.SlotsUsed, Is.EqualTo(2));
        }

        [Test]
        public void Everything_strictly_below_the_threshold_sells_and_the_threshold_itself_survives()
        {
            Inventory inventory = Sack();
            inventory.Add(Gear(1, quality: QualityRank.Rusty), currentLevel: 99);
            inventory.Add(Gear(2, quality: QualityRank.Clean), currentLevel: 99);

            JunkSale sale = inventory.SellJunk(QualityRank.Clean);

            Assert.That(sale.Stacks, Is.EqualTo(1));
            Assert.That(inventory.SlotsUsed, Is.EqualTo(1));
            Assert.That(inventory.Items[0].Item.Quality, Is.EqualTo(QualityRank.Clean),
                "The threshold rank is not 'below' it — it survives the clear.");
        }

        [Test]
        public void A_locked_piece_below_the_threshold_is_never_sold()
        {
            Inventory inventory = Sack();
            inventory.Add(Gear(1, quality: QualityRank.Rusty), currentLevel: 99);
            inventory.SetLock(0, true);

            JunkSale sale = inventory.SellJunk(QualityRank.Clean);

            Assert.That(sale.IsEmpty, Is.True);
            Assert.That(inventory.SlotsUsed, Is.EqualTo(1));
        }

        [Test]
        public void A_consumable_stack_below_the_threshold_is_never_sold()
        {
            Inventory inventory = Sack();
            inventory.Add(Potion(QualityRank.Rusty), currentLevel: 1);

            JunkSale sale = inventory.SellJunk(QualityRank.Clean);

            Assert.That(sale.IsEmpty, Is.True);
            Assert.That(inventory.SlotsUsed, Is.EqualTo(1),
                "A potion stack is not 'a piece' for the junk clear, same as auto-sell's target.");
        }

        [Test]
        public void A_stack_pays_per_unit()
        {
            // Gear never stacks past one (ItemStack's own contract), and the one item that
            // does stack — consumables — never qualifies for a junk sale. So "per unit" is
            // exercised here by several qualifying pieces rather than one deep stack; the
            // formula is the same `SellPrice * Count` either way, and Count is 1 per stack.
            Inventory inventory = Sack();
            inventory.Add(Gear(1, quality: QualityRank.Rusty), currentLevel: 99);
            inventory.Add(Gear(2, quality: QualityRank.Battlescarred), currentLevel: 99);

            JunkSale sale = inventory.PreviewJunk(QualityRank.Clean);

            Assert.That(sale.Pieces, Is.EqualTo(2));
            Assert.That(sale.Coins, Is.EqualTo(
                PriceBook.Default.SellPrice(QualityRank.Rusty, 1)
                + PriceBook.Default.SellPrice(QualityRank.Battlescarred, 1)));
        }

        [Test]
        public void A_consumable_stack_deeper_than_one_still_never_qualifies()
        {
            Inventory inventory = Sack();
            for (int i = 0; i < 3; i++)
            {
                inventory.Add(Potion(QualityRank.Rusty), currentLevel: 1);
            }

            inventory.Add(Gear(1, quality: QualityRank.Rusty), currentLevel: 99);
            JunkSale sale = inventory.PreviewJunk(QualityRank.Clean);

            Assert.That(sale.Pieces, Is.EqualTo(1), "The three-deep potion stack never qualifies, so only the gear counts.");
            Assert.That(sale.Coins, Is.EqualTo(PriceBook.Default.SellPrice(QualityRank.Rusty, 1)));
        }

        [Test]
        public void An_empty_bag_returns_an_empty_sale_that_pays_nothing()
        {
            Inventory inventory = Sack();

            JunkSale sale = inventory.SellJunk(TopRank);

            Assert.That(sale.IsEmpty, Is.True);
            Assert.That(sale.Coins, Is.Zero);
        }

        [Test]
        public void A_bag_with_nothing_below_the_threshold_returns_an_empty_sale_that_pays_nothing()
        {
            Inventory inventory = Sack();
            inventory.Add(Gear(1, quality: QualityRank.Legendary), currentLevel: 99);

            JunkSale sale = inventory.SellJunk(QualityRank.Rusty);

            Assert.That(sale.IsEmpty, Is.True);
            Assert.That(inventory.SlotsUsed, Is.EqualTo(1));
        }

        [Test]
        public void Selling_junk_twice_in_a_row_pays_nothing_the_second_time()
        {
            Inventory inventory = Sack();
            inventory.Add(Gear(1, quality: QualityRank.Rusty), currentLevel: 99);
            inventory.Add(Gear(2, quality: QualityRank.Torn), currentLevel: 99);

            inventory.SellJunk(QualityRank.Clean);
            JunkSale second = inventory.SellJunk(QualityRank.Clean);

            Assert.That(second.IsEmpty, Is.True);
            Assert.That(second.Coins, Is.Zero);
        }

        [Test]
        public void Worn_gear_is_not_in_the_sack_and_survives_a_sale_that_clears_everything_below_the_top_rank()
        {
            Inventory inventory = Sack();
            inventory.Add(Gear(1, ItemSlot.Helmet, QualityRank.Rusty), currentLevel: 99);
            inventory.TryEquip(0, currentLevel: 99);

            JunkSale sale = inventory.SellJunk(TopRank);

            Assert.That(sale.IsEmpty, Is.True, "The bag was empty the moment the helmet went on.");
            Assert.That(inventory.Loadout.Worn(ItemSlot.Helmet).IsEmpty, Is.False,
                "Worn gear lives in the loadout, never in the sack, so a junk clear can't reach it.");
        }
    }
}
