using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// D33's ladder and the item anatomy (task 40): the table's shape, the score-to-rank
    /// mapping, the naming rule, and the affix-to-stat mapping with its M5-reserved silence.
    /// </summary>
    public sealed class ItemModelTests
    {
        private static readonly QualityTable Table = QualityTable.Default;

        [Test]
        public void The_default_table_is_monotone_and_capped()
        {
            QualityRow previous = Table.For(QualityRank.Nothing);
            for (int rank = 1; rank < QualityTable.RankCount; rank++)
            {
                QualityRow row = Table.For((QualityRank)rank);
                Assert.That(row.MinScore, Is.GreaterThan(previous.MinScore),
                    "thresholds climb");
                Assert.That(row.StatBudget, Is.GreaterThan(previous.StatBudget),
                    "budgets climb");
                Assert.That(row.UpgradeCapacity, Is.GreaterThanOrEqualTo(previous.UpgradeCapacity),
                    "capacity never shrinks");
                Assert.That(row.AffixMax, Is.LessThanOrEqualTo(4), "four affixes is the ceiling");
                Assert.That(row.AffixMin, Is.LessThanOrEqualTo(row.AffixMax));
                previous = row;
            }

            QualityRow godly = Table.For(QualityRank.Godly);
            Assert.That(godly.AffixMin, Is.EqualTo(3), "a Godly can come up short");
            Assert.That(godly.AffixMax, Is.EqualTo(4));
        }

        [Test]
        public void Scores_map_to_ranks_through_the_thresholds()
        {
            Assert.That(Table.RankFor(0.4f), Is.EqualTo(QualityRank.Nothing));
            Assert.That(Table.RankFor(-1f), Is.EqualTo(QualityRank.Nothing), "never below the floor");
            Assert.That(Table.RankFor(0.7f), Is.EqualTo(QualityRank.Battlescarred));
            Assert.That(Table.RankFor(1.19f), Is.EqualTo(QualityRank.Torn));
            Assert.That(Table.RankFor(1.2f), Is.EqualTo(QualityRank.Rusty));
            Assert.That(Table.RankFor(5f), Is.EqualTo(QualityRank.Godly));
            Assert.That(Table.RankFor(99f), Is.EqualTo(QualityRank.Godly), "the ladder tops out");
        }

        [Test]
        public void Names_wear_the_quality_prefix_except_nothing()
        {
            Assert.That(ItemNaming.Compose(QualityRank.Shiny, "Leather Chestplate"),
                Is.EqualTo("Shiny Leather Chestplate"));
            Assert.That(ItemNaming.Compose(QualityRank.Rusty, "Steel Helmet"),
                Is.EqualTo("Rusty Steel Helmet"));
            Assert.That(ItemNaming.Compose(QualityRank.Nothing, "Hunting Knife"),
                Is.EqualTo("Hunting Knife"), "Nothing-rank items wear their bare name");
        }

        [Test]
        public void Consumables_speak_alchemy_not_gear_quality()
        {
            Assert.That(ItemNaming.Compose(QualityRank.Nothing, ItemSlot.Consumable, "Health"),
                Is.EqualTo("Vial of Health"));
            Assert.That(ItemNaming.Compose(QualityRank.Rusty, ItemSlot.Consumable, "Health"),
                Is.EqualTo("Flask of Health"));
            Assert.That(ItemNaming.Compose(QualityRank.Shiny, ItemSlot.Consumable, "Health"),
                Is.EqualTo("Bottle of Health"));
            Assert.That(ItemNaming.Compose(QualityRank.Mythical, ItemSlot.Consumable, "Health"),
                Is.EqualTo("Philter of Health"));
            Assert.That(ItemNaming.Compose(QualityRank.Godly, ItemSlot.Consumable, "Health"),
                Is.EqualTo("Elixir of Health"));
            Assert.That(ItemNaming.Compose(QualityRank.Godly, ItemSlot.Weapon, "Hunting Knife"),
                Is.EqualTo("Godly Hunting Knife"), "gear keeps the ladder words");
        }

        [Test]
        public void Live_affixes_map_to_their_stat()
        {
            Assert.That(AffixEffects.Contribution(new AffixRoll(AffixId.CritChance, 0.1f)).CritChance,
                Is.EqualTo(0.1f));
            Assert.That(AffixEffects.Contribution(new AffixRoll(AffixId.LifeSteal, 0.05f)).LifeSteal,
                Is.EqualTo(0.05f));
            Assert.That(AffixEffects.Contribution(new AffixRoll(AffixId.MaxHealth, 25f)).MaxHealthBonus,
                Is.EqualTo(25f));
            Assert.That(AffixEffects.Contribution(new AffixRoll(AffixId.ReducedWeight, 0.2f)).WeightReduction,
                Is.EqualTo(0.2f));
        }

        [Test]
        public void The_magic_affixes_contribute_to_the_flat_block()
        {
            Assert.That(AffixEffects.Contribution(new AffixRoll(AffixId.MagicDamage, 10f)).MagicDamage,
                Is.EqualTo(10f));
            Assert.That(AffixEffects.Contribution(new AffixRoll(AffixId.MagicRange, 1.5f)).MagicRange,
                Is.EqualTo(1.5f));
        }

        [Test]
        public void The_two_element_affixes_deliberately_travel_outside_the_flat_block()
        {
            Assert.That(AffixEffects.Contribution(new AffixRoll(AffixId.ElementalResistance, 0.3f)),
                Is.EqualTo(GearContribution.Zero),
                "Resistance is per-element, so the sheet aggregates it separately (D38/D40).");
            Assert.That(AffixEffects.Contribution(new AffixRoll(AffixId.WeaponInfusion, 1f)),
                Is.EqualTo(GearContribution.Zero),
                "Infusion is an element, not a number — the weapon carries it to the hit.");
        }

        [Test]
        public void An_instance_totals_core_stats_plus_affixes()
        {
            var item = new ItemInstance(
                new ItemIdentity(7, "Shiny Hunting Knife", ItemSlot.Weapon, WeaponClass.Sword),
                QualityRank.Shiny,
                new GearContribution(weaponDamage: 40f),
                new[]
                {
                    new AffixRoll(AffixId.CritChance, 0.1f),
                    new AffixRoll(AffixId.MaxHealth, 20f),
                },
                requiredLevel: 5,
                new ItemInvestment(capacity: 3));

            GearContribution total = item.TotalContribution();
            Assert.That(total.WeaponDamage, Is.EqualTo(40f));
            Assert.That(total.CritChance, Is.EqualTo(0.1f));
            Assert.That(total.MaxHealthBonus, Is.EqualTo(20f));
            Assert.That(item.AffixCount, Is.EqualTo(2));
        }

        [Test]
        public void An_instance_normalizes_bad_inputs()
        {
            var item = new ItemInstance(
                new ItemIdentity(1, null, ItemSlot.Chest),
                QualityRank.Torn, GearContribution.Zero, null,
                requiredLevel: 0, new ItemInvestment(capacity: -2));

            Assert.That(item.Affixes, Is.Not.Null.And.Empty);
            Assert.That(item.DisplayName, Is.Empty);
            Assert.That(item.RequiredLevel, Is.EqualTo(1), "level one is the floor");
            Assert.That(item.UpgradeCapacity, Is.Zero);
        }

        [Test]
        public void Locking_flips_nothing_but_the_lock()
        {
            var item = new ItemInstance(
                new ItemIdentity(7, "Shiny Hunting Knife", ItemSlot.Weapon, WeaponClass.Sword),
                QualityRank.Shiny, new GearContribution(weaponDamage: 40f),
                new[] { new AffixRoll(AffixId.CritChance, 0.1f) },
                requiredLevel: 5, new ItemInvestment(capacity: 3, spent: 1));

            ItemInstance locked = item.WithLock(true);

            Assert.That(locked.Locked, Is.True);
            Assert.That(item.Locked, Is.False, "The original is untouched — instances are values.");
            Assert.That(locked.Quality, Is.EqualTo(item.Quality));
            Assert.That(locked.Affixes, Is.SameAs(item.Affixes), "The roll itself never re-rolls.");
            Assert.That(locked.UpgradesSpent, Is.EqualTo(1));
            Assert.That(locked.WithLock(false).Locked, Is.False);
        }

        [Test]
        public void Scaling_grows_the_whole_block()
        {
            var block = new GearContribution(weaponDamage: 10f, defence: 0.1f, weight: 4f);
            GearContribution scaled = block.Scaled(2.5f);

            Assert.That(scaled.WeaponDamage, Is.EqualTo(25f));
            Assert.That(scaled.Defence, Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(scaled.Weight, Is.EqualTo(10f));
        }
    }
}
