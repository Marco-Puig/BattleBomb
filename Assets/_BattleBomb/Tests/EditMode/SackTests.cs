using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// D43's carry rules: the cap, stacks as slots, locks, the auto-sell setting, and the
    /// refusal that has no overflow valve. Every edge Michael named is pinned here — a full
    /// sack, an all-locked sack, and a stack at its ceiling.
    /// </summary>
    public sealed class SackTests
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

        private static Inventory Sack(int capacity = 4, int stackLimit = 5)
        {
            return new Inventory { Rules = new SackRules(capacity, stackLimit) };
        }

        private static void Fill(Inventory inventory, int count, QualityRank quality = QualityRank.Rusty)
        {
            for (int i = 0; i < count; i++)
            {
                inventory.Add(Gear(i + 1, quality: quality), currentLevel: 99);
            }
        }

        // ── Slots ────────────────────────────────────────────────────────────────────

        [Test]
        public void A_stack_of_potions_costs_one_slot_however_deep()
        {
            Inventory inventory = Sack();

            for (int i = 0; i < 5; i++)
            {
                inventory.Add(Potion(), currentLevel: 1);
            }

            Assert.That(inventory.SlotsUsed, Is.EqualTo(1));
            Assert.That(inventory.Items[0].Count, Is.EqualTo(5));
        }

        [Test]
        public void Five_of_the_same_item_is_the_ceiling()
        {
            Inventory inventory = Sack();
            for (int i = 0; i < 5; i++)
            {
                inventory.Add(Potion(), currentLevel: 1);
            }

            AddResult sixth = inventory.Add(Potion(), currentLevel: 1);

            Assert.That(sixth.Taken, Is.False, "A sixth identical potion has nowhere to go (D43).");
            Assert.That(inventory.Items[0].Count, Is.EqualTo(5));
            Assert.That(inventory.SlotsUsed, Is.EqualTo(1), "And it never opens a second stack.");
        }

        [Test]
        public void Different_qualities_never_merge_and_take_their_own_slots()
        {
            Inventory inventory = Sack();

            inventory.Add(Potion(QualityRank.Rusty), currentLevel: 1);
            inventory.Add(Potion(QualityRank.Godly), currentLevel: 1);

            Assert.That(inventory.SlotsUsed, Is.EqualTo(2),
                "A Vial and an Elixir heal differently, so they are different items.");
        }

        // ── The cap, and the refusal ─────────────────────────────────────────────────

        [Test]
        public void A_full_sack_refuses_the_pickup_outright()
        {
            Inventory inventory = Sack(capacity: 3);
            Fill(inventory, 3);

            AddResult result = inventory.Add(Gear(99), currentLevel: 99);

            Assert.That(result.Taken, Is.False);
            Assert.That(result.CoinsEarned, Is.Zero);
            Assert.That(inventory.SlotsUsed, Is.EqualTo(3), "No overflow valve exists (D43).");
        }

        [Test]
        public void A_full_sack_with_auto_sell_off_stays_full_even_with_junk_in_it()
        {
            Inventory inventory = Sack(capacity: 2);
            inventory.Add(Gear(1, quality: QualityRank.Battlescarred), currentLevel: 99);
            inventory.Add(Gear(2, quality: QualityRank.Godly), currentLevel: 99);

            Assert.That(inventory.Add(Gear(3), currentLevel: 99).Taken, Is.False,
                "Selling is opt-in — the game never spends the player's loot uninvited.");
        }

        // ── Auto-sell (the setting) ──────────────────────────────────────────────────

        [Test]
        public void Auto_sell_makes_room_by_selling_the_worst_unlocked_piece()
        {
            Inventory inventory = Sack(capacity: 2);
            inventory.AutoSell = true;
            inventory.Add(Gear(1, quality: QualityRank.Godly), currentLevel: 99);
            inventory.Add(Gear(2, quality: QualityRank.Battlescarred), currentLevel: 99);

            AddResult result = inventory.Add(Gear(3, quality: QualityRank.Shiny), currentLevel: 99);

            Assert.That(result.Taken, Is.True);
            Assert.That(result.CoinsEarned, Is.GreaterThan(0), "The junk paid for its own eviction.");
            Assert.That(inventory.SlotsUsed, Is.EqualTo(2));
            Assert.That(inventory.Items[0].Item.Quality, Is.EqualTo(QualityRank.Godly),
                "The keeper survived; the Battlescarred piece was the one sold.");
        }

        [Test]
        public void Auto_sell_never_touches_a_locked_item()
        {
            Inventory inventory = Sack(capacity: 2);
            inventory.AutoSell = true;
            inventory.Add(Gear(1, quality: QualityRank.Battlescarred), currentLevel: 99);
            inventory.Add(Gear(2, quality: QualityRank.Godly), currentLevel: 99);
            inventory.SetLock(0, true);

            AddResult result = inventory.Add(Gear(3, quality: QualityRank.Shiny), currentLevel: 99);

            Assert.That(result.Taken, Is.True);
            Assert.That(inventory.Items[0].Item.Quality, Is.EqualTo(QualityRank.Battlescarred),
                "The lock outranks the quality sort: the Godly piece went instead.");
        }

        [Test]
        public void A_sack_of_locked_items_refuses_even_with_auto_sell_on()
        {
            Inventory inventory = Sack(capacity: 2);
            inventory.AutoSell = true;
            Fill(inventory, 2);
            inventory.SetLock(0, true);
            inventory.SetLock(1, true);

            AddResult result = inventory.Add(Gear(3), currentLevel: 99);

            Assert.That(result.Taken, Is.False,
                "Michael's punishing case: full, all locked, nothing sellable — the drop stays.");
        }

        [Test]
        public void Auto_sell_spends_gear_before_it_spends_a_potion_stack()
        {
            Inventory inventory = Sack(capacity: 2);
            inventory.AutoSell = true;
            for (int i = 0; i < 3; i++)
            {
                inventory.Add(Potion(QualityRank.Battlescarred), currentLevel: 1);
            }

            inventory.Add(Gear(1, quality: QualityRank.Godly), currentLevel: 99);

            inventory.Add(Gear(2, quality: QualityRank.Shiny), currentLevel: 99);

            Assert.That(inventory.Items[0].Item.IsConsumable, Is.True,
                "Three potions are worth more to a player than one spare helmet, whatever " +
                "the quality sort says.");
        }

        // ── Selling and locks by hand ────────────────────────────────────────────────

        [Test]
        public void Selling_pays_the_price_book_and_frees_the_slot()
        {
            Inventory inventory = Sack();
            inventory.Add(Gear(1, quality: QualityRank.Shiny, requiredLevel: 12), currentLevel: 99);

            int coins = inventory.Sell(0);

            Assert.That(coins, Is.EqualTo(PriceBook.Default.SellPrice(QualityRank.Shiny, 12)));
            Assert.That(inventory.SlotsUsed, Is.Zero);
        }

        [Test]
        public void Selling_a_stack_pays_per_unit()
        {
            Inventory inventory = Sack();
            for (int i = 0; i < 4; i++)
            {
                inventory.Add(Potion(), currentLevel: 1);
            }

            int coins = inventory.Sell(0);

            Assert.That(coins, Is.EqualTo(4 * PriceBook.Default.SellPrice(QualityRank.Rusty, 1)));
        }

        [Test]
        public void A_locked_item_refuses_to_be_sold_by_hand_too()
        {
            Inventory inventory = Sack();
            inventory.Add(Gear(1), currentLevel: 99);
            inventory.SetLock(0, true);

            Assert.That(inventory.Sell(0), Is.Zero);
            Assert.That(inventory.SlotsUsed, Is.EqualTo(1), "It is still there — release it first.");
        }

        [Test]
        public void Unlocking_returns_it_to_the_sellable_pile()
        {
            Inventory inventory = Sack();
            inventory.Add(Gear(1), currentLevel: 99);
            inventory.SetLock(0, true);
            inventory.SetLock(0, false);

            Assert.That(inventory.Sell(0), Is.GreaterThan(0));
        }

        // ── The loadout's relationship with a full sack ──────────────────────────────

        [Test]
        public void Taking_gear_off_needs_a_slot_to_put_it_in()
        {
            Inventory inventory = Sack(capacity: 2);
            inventory.Add(Gear(1, ItemSlot.Helmet), currentLevel: 99);
            inventory.TryEquip(0, currentLevel: 99);
            Fill(inventory, 2, QualityRank.Torn);

            Assert.That(inventory.Unequip(ItemSlot.Helmet), Is.False,
                "A full sack has nowhere to hold what you take off.");
        }

        [Test]
        public void Prestige_forces_over_level_gear_home_even_past_the_cap()
        {
            Inventory inventory = Sack(capacity: 2);
            inventory.Add(Gear(1, ItemSlot.Helmet, requiredLevel: 40), currentLevel: 99);
            inventory.TryEquip(0, currentLevel: 99);
            Fill(inventory, 2, QualityRank.Torn);

            int returned = inventory.ReturnOverLevelGear(currentLevel: 1);

            Assert.That(returned, Is.EqualTo(1));
            Assert.That(inventory.SlotsUsed, Is.EqualTo(3),
                "D36's re-lock overflows the sack rather than destroying the gear.");
        }

        [Test]
        public void Swapping_gear_needs_no_extra_room()
        {
            Inventory inventory = Sack(capacity: 2);
            inventory.Add(Gear(1, ItemSlot.Helmet), currentLevel: 99);
            inventory.TryEquip(0, currentLevel: 99);
            inventory.Add(Gear(2, ItemSlot.Helmet, QualityRank.Godly), currentLevel: 99);
            inventory.Add(Gear(3, ItemSlot.Chest), currentLevel: 99);

            Assert.That(inventory.TryEquip(0, currentLevel: 99), Is.True,
                "One goes on as the other comes off — the slot count never moves.");
            Assert.That(inventory.SlotsUsed, Is.EqualTo(2));
        }
    }
}
