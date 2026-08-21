using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Loot;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// D44's two fates for a keeper: deepen it with money, or gamble it against a duplicate.
    /// The promotion rate is pinned by running the gamble thousands of times — a 2% jackpot
    /// that quietly drifted to 20% would ruin the ladder and no single roll would show it.
    /// </summary>
    public sealed class InvestmentTests
    {
        private static readonly ItemSpec[] Catalog =
        {
            new ItemSpec(
                new ItemIdentity(4, "Hunting Knife", ItemSlot.Weapon, WeaponClass.Sword),
                new GearContribution(weaponDamage: 40f, swingSpeedBonus: 0.1f)),
            new ItemSpec(
                new ItemIdentity(1, "Leather Helmet", ItemSlot.Helmet),
                new GearContribution(defence: 0.08f, weight: 4f)),
        };

        private static GenerationContext Context(float score = 3f, int level = 10) =>
            new GenerationContext(score, level, Catalog, QualityTable.Default, DropWeights.Default);

        private static ItemInstance Knife(
            QualityRank quality = QualityRank.Shiny,
            int capacity = 3,
            int requiredLevel = 10,
            params AffixRoll[] affixes) => new ItemInstance(
            new ItemIdentity(4, "Hunting Knife", ItemSlot.Weapon, WeaponClass.Sword),
            quality,
            new GearContribution(weaponDamage: 40f, swingSpeedBonus: 0.1f),
            affixes,
            requiredLevel,
            new ItemInvestment(capacity));

        private static List<UpgradeTarget> TargetsOf(in ItemInstance item)
        {
            var buffer = new List<UpgradeTarget>();
            ItemUpgrade.Targets(item, buffer);
            return buffer;
        }

        // ── Upgrading: the guaranteed half ───────────────────────────────────────────

        [Test]
        public void Only_stats_the_item_already_has_can_be_deepened()
        {
            ItemInstance helmet = new ItemInstance(
                new ItemIdentity(1, "Leather Helmet", ItemSlot.Helmet),
                QualityRank.Shiny, new GearContribution(defence: 0.08f, weight: 4f),
                new[] { new AffixRoll(AffixId.CritChance, 0.05f) },
                requiredLevel: 10, new ItemInvestment(2));

            List<UpgradeTarget> targets = TargetsOf(helmet);

            Assert.That(targets.Count, Is.EqualTo(2), "Defence and the one affix — never weight.");
            Assert.That(targets[0].CoreStat, Is.EqualTo(CoreStatId.Defence));
            Assert.That(targets[1].IsAffix, Is.True);
        }

        [Test]
        public void A_point_raises_the_stat_by_the_step_fraction()
        {
            ItemInstance knife = Knife();

            Assert.That(ItemUpgrade.TryApply(knife, UpgradeTarget.Core(CoreStatId.WeaponDamage),
                out ItemInstance deepened), Is.True);

            Assert.That(deepened.CoreStats.WeaponDamage,
                Is.EqualTo(40f * (1f + ItemUpgrade.StepFraction)).Within(1e-4f));
            Assert.That(deepened.UpgradesSpent, Is.EqualTo(1));
            Assert.That(deepened.CoreStats.SwingSpeedBonus, Is.EqualTo(0.1f),
                "One point goes into one stat; the rest of the item is untouched.");
        }

        [Test]
        public void An_affix_deepens_without_changing_what_it_is()
        {
            ElementId fire = new ElementId(1);
            ItemInstance knife = Knife(affixes: new AffixRoll(AffixId.WeaponInfusion, 0.3f, fire));

            ItemUpgrade.TryApply(knife, UpgradeTarget.Affix(0), out ItemInstance deepened);

            Assert.That(deepened.Affixes[0].Magnitude, Is.EqualTo(0.3f * 1.08f).Within(1e-5f));
            Assert.That(deepened.Affixes[0].Id, Is.EqualTo(AffixId.WeaponInfusion));
            Assert.That(deepened.Affixes[0].Element, Is.EqualTo(fire),
                "Investment deepens the roll; it never re-rolls what the roll was (D35).");
            Assert.That(knife.Affixes[0].Magnitude, Is.EqualTo(0.3f),
                "And the original instance is untouched — items are values.");
        }

        [Test]
        public void Upgrading_never_adds_a_stat_the_item_did_not_roll()
        {
            ItemInstance knife = Knife();

            Assert.That(
                ItemUpgrade.TryApply(knife, UpgradeTarget.Core(CoreStatId.Defence), out _),
                Is.False,
                "A sword has no defence to deepen — the Diablo half stays immutable (D35).");
            Assert.That(ItemUpgrade.TryApply(knife, UpgradeTarget.Affix(0), out _), Is.False,
                "And an affix it never rolled is not a target either.");
        }

        [Test]
        public void Capacity_is_the_hard_gate()
        {
            ItemInstance item = Knife(capacity: 2);

            ItemUpgrade.TryApply(item, UpgradeTarget.Core(CoreStatId.WeaponDamage), out item);
            ItemUpgrade.TryApply(item, UpgradeTarget.Core(CoreStatId.WeaponDamage), out item);

            Assert.That(item.UpgradesSpent, Is.EqualTo(2));
            Assert.That(ItemUpgrade.CanUpgrade(item), Is.False);
            Assert.That(ItemUpgrade.TryApply(item, UpgradeTarget.Core(CoreStatId.WeaponDamage), out _),
                Is.False, "Money can buy the pace, never the total.");
        }

        [Test]
        public void A_maxed_godly_is_a_real_prize_but_never_a_second_roll()
        {
            ItemInstance item = Knife(QualityRank.Godly, capacity: 4);
            for (int i = 0; i < 4; i++)
            {
                ItemUpgrade.TryApply(item, UpgradeTarget.Core(CoreStatId.WeaponDamage), out item);
            }

            float growth = item.CoreStats.WeaponDamage / 40f;

            Assert.That(growth, Is.GreaterThan(1.3f));
            Assert.That(growth, Is.LessThan(1.4f), "About a third up on one stat — not a new item.");
        }

        [Test]
        public void Consumables_have_nothing_to_deepen()
        {
            var potion = new ItemInstance(
                new ItemIdentity(9, "Vial of Health", ItemSlot.Consumable),
                QualityRank.Shiny, GearContribution.Zero, new AffixRoll[0], requiredLevel: 1,
                new ItemInvestment(4), consumable: new RestorePayload(RestoreKind.Health, 0.35f));

            Assert.That(TargetsOf(potion), Is.Empty);
            Assert.That(ItemUpgrade.CanUpgrade(potion), Is.False);
        }

        // ── Combining: the gamble ────────────────────────────────────────────────────

        [Test]
        public void Two_of_the_same_item_at_the_same_rank_combine()
        {
            Assert.That(ItemCombine.CanCombine(Knife(), Knife()), Is.True);
        }

        [Test]
        public void Different_items_or_different_ranks_never_combine()
        {
            ItemInstance helmet = new ItemInstance(
                new ItemIdentity(1, "Leather Helmet", ItemSlot.Helmet),
                QualityRank.Shiny, new GearContribution(defence: 0.08f), new AffixRoll[0],
                requiredLevel: 10, new ItemInvestment(2));

            Assert.That(ItemCombine.CanCombine(Knife(), helmet), Is.False, "different definitions");
            Assert.That(ItemCombine.CanCombine(Knife(QualityRank.Shiny), Knife(QualityRank.Godly)),
                Is.False, "Michael's rule: you can't combine two different quality items.");
        }

        [Test]
        public void A_locked_item_refuses_the_gamble()
        {
            ItemInstance locked = Knife().WithLock(true);

            Assert.That(ItemCombine.CanCombine(locked, Knife()), Is.False);
            Assert.That(ItemCombine.CanCombine(Knife(), locked), Is.False);
        }

        [Test]
        public void The_reroll_comes_back_the_same_item_at_the_input_rank_unless_it_promoted()
        {
            // Seed 7 happens to hit the 2%, which is exactly why this asserts the invariant
            // rather than a hard-coded rank: the reroll is the input rank, or one above it,
            // and never anything else.
            for (uint seed = 1; seed <= 40; seed++)
            {
                ItemCombine.Combine(
                    new DeterministicRandom(seed), Knife(), Knife(), Context(), out CombineResult result);

                Assert.That(result.Combined, Is.True);
                Assert.That(result.Item.DefinitionId, Is.EqualTo(4), "Same item, always.");
                Assert.That(
                    result.Item.Quality,
                    Is.EqualTo(result.Promoted ? QualityRank.Pristine : QualityRank.Shiny),
                    $"seed {seed}");
            }
        }

        [Test]
        public void Spent_points_die_with_the_inputs()
        {
            ItemInstance invested = Knife(capacity: 3);
            ItemUpgrade.TryApply(invested, UpgradeTarget.Core(CoreStatId.WeaponDamage), out invested);
            ItemUpgrade.TryApply(invested, UpgradeTarget.Core(CoreStatId.WeaponDamage), out invested);

            ItemCombine.Combine(
                new DeterministicRandom(11u), invested, Knife(), Context(), out CombineResult result);

            Assert.That(result.Item.UpgradesSpent, Is.Zero,
                "You invest in an item or you gamble it, never both (D44).");
            Assert.That(result.Item.UpgradeCapacity, Is.GreaterThan(0), "Fresh capacity, though.");
        }

        [Test]
        public void The_result_carries_the_higher_required_level()
        {
            ItemCombine.Combine(
                new DeterministicRandom(3u),
                Knife(requiredLevel: 40),
                Knife(requiredLevel: 5),
                Context(level: 5),
                out CombineResult result);

            Assert.That(result.Item.RequiredLevel, Is.EqualTo(40),
                "Combining is never a way to launder high-level gear downhill (D36).");
        }

        [Test]
        public void The_promotion_lands_about_two_percent_of_the_time()
        {
            var rng = new DeterministicRandom(20260821u);
            int promotions = 0;
            const int trials = 20000;

            for (int i = 0; i < trials; i++)
            {
                rng = ItemCombine.Combine(
                    rng, Knife(), Knife(), Context(), out CombineResult result);
                if (result.Promoted)
                {
                    promotions++;
                }
            }

            float rate = (float)promotions / trials;

            Assert.That(rate, Is.EqualTo(ItemCombine.PromotionChance).Within(0.006f),
                $"{promotions} promotions in {trials} combines — the jackpot must stay a jackpot.");
        }

        [Test]
        public void A_promoted_reroll_really_is_one_rank_up()
        {
            var rng = new DeterministicRandom(20260821u);
            bool sawOne = false;

            for (int i = 0; i < 20000 && !sawOne; i++)
            {
                rng = ItemCombine.Combine(
                    rng, Knife(), Knife(), Context(), out CombineResult result);
                if (result.Promoted)
                {
                    sawOne = true;
                    Assert.That(result.Item.Quality, Is.EqualTo(QualityRank.Pristine),
                        "One rank above the Shiny pair that bought it.");
                }
            }

            Assert.That(sawOne, Is.True, "20,000 gambles must produce at least one winner.");
        }

        [Test]
        public void A_godly_pair_can_never_promote_past_the_top_of_the_ladder()
        {
            var rng = new DeterministicRandom(20260821u);

            for (int i = 0; i < 2000; i++)
            {
                rng = ItemCombine.Combine(
                    rng, Knife(QualityRank.Godly), Knife(QualityRank.Godly), Context(99f),
                    out CombineResult result);
                Assert.That(result.Item.Quality, Is.EqualTo(QualityRank.Godly));
                Assert.That(result.Promoted, Is.False);
            }
        }

        // ── Through the inventory, the way the chest screen asks ─────────────────────

        [Test]
        public void Combining_from_the_bag_consumes_both_slots_and_leaves_one()
        {
            var inventory = new Inventory();
            inventory.Add(Knife(), currentLevel: 99);
            inventory.Add(Knife(), currentLevel: 99);

            inventory.TryCombine(
                new DeterministicRandom(5u), 0, 1, Context(), out CombineResult result);

            Assert.That(result.Combined, Is.True);
            Assert.That(inventory.SlotsUsed, Is.EqualTo(1), "Two went in, one came out.");
            Assert.That(inventory.Items[0].Item.DefinitionId, Is.EqualTo(4));
        }

        [Test]
        public void Combining_an_item_with_itself_is_refused()
        {
            var inventory = new Inventory();
            inventory.Add(Knife(), currentLevel: 99);

            inventory.TryCombine(
                new DeterministicRandom(5u), 0, 0, Context(), out CombineResult result);

            Assert.That(result.Combined, Is.False, "One knife is not two knives.");
            Assert.That(inventory.SlotsUsed, Is.EqualTo(1));
        }

        [Test]
        public void Upgrading_reaches_a_worn_piece_without_taking_it_off()
        {
            var inventory = new Inventory();
            inventory.Add(Knife(), currentLevel: 99);
            inventory.TryEquip(0, currentLevel: 99);

            Assert.That(
                inventory.TryUpgradeWorn(ItemSlot.Weapon, 0, UpgradeTarget.Core(CoreStatId.WeaponDamage)),
                Is.True);

            Assert.That(inventory.Loadout.Weapon.CoreStats.WeaponDamage,
                Is.EqualTo(40f * 1.08f).Within(1e-4f));
            Assert.That(inventory.Loadout.Weapon.UpgradesSpent, Is.EqualTo(1),
                "Deepening your best item should never require unequipping it first.");
        }

        [Test]
        public void The_stream_advances_identically_whether_the_jackpot_lands()
        {
            var a = new DeterministicRandom(99u);
            var b = new DeterministicRandom(99u);

            a = ItemCombine.Combine(a, Knife(), Knife(), Context(), out _);
            b = ItemCombine.Combine(b, Knife(), Knife(), Context(), out _);

            a.NextFloat(out float afterA);
            b.NextFloat(out float afterB);

            Assert.That(afterA, Is.EqualTo(afterB),
                "Same inputs, same stream position — determinism is not negotiable (D10).");
        }
    }
}
