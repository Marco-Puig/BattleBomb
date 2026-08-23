using BattleBomb.Core.Items;
using BattleBomb.Core.Loot;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// D44's gamble in bulk. <see cref="Inventory.CombineAll"/> grinds one captured identity —
    /// a definition at a rank — and nothing else: a lock, a consumable, a different item or a
    /// different rank all sit the run out, and a promotion leaves the pool rather than cascading.
    /// The run must also land the loot stream exactly where the same combines made one at a time
    /// would have left it, or a bulk press is a different game than the presses it replaces (D10).
    /// </summary>
    public sealed class CombineAllTests
    {
        private const int KnifeId = 4;
        private const int HelmetId = 1;

        private static readonly ItemSpec[] Catalog =
        {
            new ItemSpec(
                new ItemIdentity(KnifeId, "Hunting Knife", ItemSlot.Weapon, WeaponClass.Sword),
                new GearContribution(weaponDamage: 40f, swingSpeedBonus: 0.1f)),
            new ItemSpec(
                new ItemIdentity(HelmetId, "Leather Helmet", ItemSlot.Helmet),
                new GearContribution(defence: 0.08f, weight: 4f)),
        };

        private static GenerationContext Context(float score = 3f, int level = 10) =>
            new GenerationContext(score, level, Catalog, QualityTable.Default, DropWeights.Default);

        private static ItemInstance Knife(QualityRank quality = QualityRank.Shiny) => new ItemInstance(
            new ItemIdentity(KnifeId, "Hunting Knife", ItemSlot.Weapon, WeaponClass.Sword),
            quality,
            new GearContribution(weaponDamage: 40f, swingSpeedBonus: 0.1f),
            new AffixRoll[0],
            requiredLevel: 10,
            new ItemInvestment(3));

        private static ItemInstance Helmet() => new ItemInstance(
            new ItemIdentity(HelmetId, "Leather Helmet", ItemSlot.Helmet),
            QualityRank.Shiny,
            new GearContribution(defence: 0.08f, weight: 4f),
            new AffixRoll[0],
            requiredLevel: 10,
            new ItemInvestment(3));

        /// <summary>A consumable wearing the knife's own definition id and rank, so the only thing
        /// that can disqualify it is the consumable rule itself.</summary>
        private static ItemInstance Draught() => new ItemInstance(
            new ItemIdentity(KnifeId, "Hunting Draught", ItemSlot.Consumable),
            QualityRank.Shiny,
            GearContribution.Zero,
            new AffixRoll[0],
            requiredLevel: 1,
            consumable: new RestorePayload(RestoreKind.Health, 0.35f));

        private static Inventory PileOf(int knives, QualityRank quality = QualityRank.Shiny)
        {
            var inventory = new Inventory();
            for (int i = 0; i < knives; i++)
            {
                inventory.Add(Knife(quality), currentLevel: 99);
            }

            return inventory;
        }

        /// <summary>How much of the ground identity is still in the bag.</summary>
        private static int PileLeft(Inventory inventory, QualityRank quality = QualityRank.Shiny)
        {
            int count = 0;
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                ItemInstance item = inventory.Items[i].Item;
                if (!item.IsConsumable && item.DefinitionId == KnifeId && item.Quality == quality)
                {
                    count++;
                }
            }

            return count;
        }

        // ── The pile ─────────────────────────────────────────────────────────────────

        [Test]
        public void A_pile_of_duplicates_grinds_down_until_fewer_than_two_of_it_remain()
        {
            Inventory inventory = PileOf(5);

            inventory.CombineAll(new DeterministicRandom(4242u), 0, Context(), out CombineRun run);

            Assert.That(run.IsEmpty, Is.False);
            Assert.That(PileLeft(inventory), Is.LessThan(2),
                "The run stops only when there is no pair left to take.");
            Assert.That(inventory.SlotsUsed, Is.EqualTo(5 - run.Combines),
                "Two go in and one comes out, every single pass.");
        }

        [Test]
        public void A_pile_with_no_promotion_in_it_ends_as_one_piece()
        {
            Inventory inventory = PileOf(4);

            inventory.CombineAll(new DeterministicRandom(4242u), 0, Context(), out CombineRun run);

            Assert.That(run.Promotions, Is.Zero, "This seed loses the 2% shot every time.");
            Assert.That(run.Combines, Is.EqualTo(3), "Four in, three passes, one out.");
            Assert.That(inventory.SlotsUsed, Is.EqualTo(1));
        }

        [Test]
        public void Combines_counts_what_happened_and_promotions_never_outnumber_it()
        {
            for (uint seed = 1u; seed <= 40u; seed++)
            {
                Inventory inventory = PileOf(6);

                inventory.CombineAll(new DeterministicRandom(seed), 0, Context(), out CombineRun run);

                Assert.That(run.Promotions, Is.LessThanOrEqualTo(run.Combines),
                    "A pair that never combined cannot have promoted.");
                Assert.That(inventory.SlotsUsed, Is.EqualTo(6 - run.Combines),
                    "Every counted combine is a pair that really left the bag.");
                Assert.That(PileLeft(inventory), Is.LessThan(2));
            }
        }

        // ── What the run may never reach ─────────────────────────────────────────────

        [Test]
        public void A_locked_duplicate_is_never_consumed()
        {
            Inventory inventory = PileOf(4);
            inventory.SetLock(1, true);
            inventory.SetLock(2, true);

            inventory.CombineAll(new DeterministicRandom(77u), 0, Context(), out CombineRun run);

            Assert.That(run.Combines, Is.EqualTo(1),
                "Only the two unlocked knives were ever eligible for the pile.");
            Assert.That(inventory.SlotsUsed, Is.EqualTo(3));
            Assert.That(inventory.Items[0].Item.Locked, Is.True);
            Assert.That(inventory.Items[1].Item.Locked, Is.True,
                "The lock is absolute, in bulk exactly as it is one at a time (D43).");
        }

        [Test]
        public void A_pile_that_is_all_locked_returns_an_empty_run()
        {
            Inventory inventory = PileOf(3);
            inventory.SetLock(0, true);
            inventory.SetLock(1, true);
            inventory.SetLock(2, true);
            var rng = new DeterministicRandom(77u);

            DeterministicRandom after = inventory.CombineAll(rng, 0, Context(), out CombineRun run);

            Assert.That(run.IsEmpty, Is.True);
            Assert.That(inventory.SlotsUsed, Is.EqualTo(3));
            Assert.That(after.State, Is.EqualTo(rng.State), "A refusal never spends a draw.");
        }

        [Test]
        public void An_unlocked_anchor_whose_every_twin_is_locked_returns_an_empty_run()
        {
            Inventory inventory = PileOf(3);
            inventory.SetLock(1, true);
            inventory.SetLock(2, true);

            inventory.CombineAll(new DeterministicRandom(77u), 0, Context(), out CombineRun run);

            Assert.That(run.IsEmpty, Is.True, "One knife is not two knives.");
            Assert.That(inventory.SlotsUsed, Is.EqualTo(3));
        }

        [Test]
        public void A_consumable_anchor_is_refused_outright()
        {
            var inventory = new Inventory();
            inventory.Add(Draught(), currentLevel: 99);
            inventory.Add(Draught(), currentLevel: 99);
            var rng = new DeterministicRandom(9u);

            DeterministicRandom after = inventory.CombineAll(rng, 0, Context(), out CombineRun run);

            Assert.That(run.IsEmpty, Is.True, "Potions are drunk, never gambled.");
            Assert.That(inventory.Items[0].Count, Is.EqualTo(2), "And the stack is untouched.");
            Assert.That(after.State, Is.EqualTo(rng.State));
        }

        [Test]
        public void A_consumable_is_never_a_partner_even_wearing_the_pile_s_own_identity()
        {
            var inventory = new Inventory();
            inventory.Add(Knife(), currentLevel: 99);
            inventory.Add(Draught(), currentLevel: 99);

            inventory.CombineAll(new DeterministicRandom(9u), 0, Context(), out CombineRun run);

            Assert.That(run.IsEmpty, Is.True,
                "Same definition and same rank, and it still fails — being drinkable is enough.");
            Assert.That(inventory.SlotsUsed, Is.EqualTo(2));
        }

        [Test]
        public void An_anchor_with_no_twin_returns_an_empty_run_and_leaves_the_bag_untouched()
        {
            var inventory = new Inventory();
            inventory.Add(Knife(), currentLevel: 99);
            inventory.Add(Helmet(), currentLevel: 99);
            inventory.Add(Knife(QualityRank.Legendary), currentLevel: 99);
            var rng = new DeterministicRandom(31u);

            DeterministicRandom after = inventory.CombineAll(rng, 0, Context(), out CombineRun run);

            Assert.That(run.IsEmpty, Is.True);
            Assert.That(inventory.SlotsUsed, Is.EqualTo(3));
            Assert.That(after.State, Is.EqualTo(rng.State), "No pair, no draw.");
            Assert.That(inventory.Items[1].Item.DefinitionId, Is.EqualTo(HelmetId));
            Assert.That(inventory.Items[2].Item.Quality, Is.EqualTo(QualityRank.Legendary),
                "A different item and a different rank are not the pile.");
        }

        [Test]
        public void A_run_never_reaches_a_different_definition_or_a_different_rank()
        {
            var inventory = new Inventory();
            inventory.Add(Knife(), currentLevel: 99);
            inventory.Add(Helmet(), currentLevel: 99);
            inventory.Add(Knife(QualityRank.Legendary), currentLevel: 99);
            inventory.Add(Knife(), currentLevel: 99);
            inventory.Add(Knife(), currentLevel: 99);

            inventory.CombineAll(new DeterministicRandom(613u), 0, Context(), out CombineRun run);

            Assert.That(run.IsEmpty, Is.False);
            Assert.That(inventory.Items[0].Item.DefinitionId, Is.EqualTo(HelmetId),
                "The helmet was never in the pile, so nothing ever shifted it.");
            Assert.That(inventory.Items[1].Item.Quality, Is.EqualTo(QualityRank.Legendary),
                "Nor was the Legendary knife — one rank apart is a different item to D44.");
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void An_out_of_range_anchor_is_refused(int anchorIndex)
        {
            Inventory inventory = PileOf(2);
            var rng = new DeterministicRandom(31u);

            DeterministicRandom after = inventory.CombineAll(
                rng, anchorIndex, Context(), out CombineRun run);

            Assert.That(run.IsEmpty, Is.True);
            Assert.That(inventory.SlotsUsed, Is.EqualTo(2),
                "Even with a perfectly good pair sitting in the bag.");
            Assert.That(after.State, Is.EqualTo(rng.State));
        }

        // ── The promotion, and the stream ────────────────────────────────────────────

        [Test]
        public void A_promoted_reroll_leaves_the_pool_and_is_never_ground_down_again()
        {
            Inventory inventory = PileOf(4);

            inventory.CombineAll(new DeterministicRandom(1u), 0, Context(), out CombineRun run);

            Assert.That(run.Promotions, Is.GreaterThanOrEqualTo(1),
                "Seed 1 draws 0.00006 first, well inside D44's 2% — the first pair promotes.");
            Assert.That(run.Combines, Is.EqualTo(2),
                "Four Shiny knives, and the Pristine one that came out is not a Shiny knife: the "
                + "pile ran dry a pass early.");
            Assert.That(PileLeft(inventory), Is.LessThan(2));
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                Assert.That(inventory.Items[i].Item.Quality, Is.LessThanOrEqualTo(QualityRank.Pristine),
                    "The rank up is the payoff, not the start of a cascade.");
            }
        }

        [Test]
        public void A_run_leaves_the_same_bag_and_the_same_stream_as_the_same_combines_by_hand()
        {
            Inventory bulk = PileOf(4);
            DeterministicRandom afterBulk = bulk.CombineAll(
                new DeterministicRandom(4242u), 0, Context(), out CombineRun run);

            Assert.That(run.Promotions, Is.Zero,
                "This seed is chosen for a clean run: a promotion would leave the pool and move "
                + "the pairs the hand-run below picks.");

            Inventory byHand = PileOf(4);
            var rng = new DeterministicRandom(4242u);
            for (int i = 0; i < run.Combines; i++)
            {
                rng = byHand.TryCombine(rng, 0, 1, Context(), out CombineResult result);
                Assert.That(result.Combined, Is.True);
            }

            Assert.That(rng.State, Is.EqualTo(afterBulk.State),
                "One press or three, the loot stream lands in the same place (D10).");
            Assert.That(byHand.SlotsUsed, Is.EqualTo(bulk.SlotsUsed));
            for (int i = 0; i < bulk.Items.Count; i++)
            {
                Assert.That(byHand.Items[i].Item.DefinitionId,
                    Is.EqualTo(bulk.Items[i].Item.DefinitionId));
                Assert.That(byHand.Items[i].Item.Quality, Is.EqualTo(bulk.Items[i].Item.Quality));
                Assert.That(byHand.Items[i].Item.CoreStats.WeaponDamage,
                    Is.EqualTo(bulk.Items[i].Item.CoreStats.WeaponDamage),
                    "Same draws in the same order means the same rolled numbers, not just the "
                    + "same shape of item.");
            }
        }
    }
}
