using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Loot;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The §5.3 generator (task 41): replay determinism, the score-to-rank mapping with the boss
    /// floor, affix counts and uniqueness, the elite and boss forcings, the level stamp, and the
    /// one-roll defence/weight correlation that makes tanky rolls heavy rolls.
    /// </summary>
    public sealed class ItemGeneratorTests
    {
        private static readonly ItemSpec[] Catalog =
        {
            new ItemSpec(new ItemIdentity(1, "Leather Helmet", ItemSlot.Helmet), new GearContribution(defence: 0.08f, weight: 4f)),
            new ItemSpec(new ItemIdentity(2, "Leather Chestplate", ItemSlot.Chest), new GearContribution(defence: 0.12f, weight: 6f)),
            new ItemSpec(new ItemIdentity(3, "Leather Boots", ItemSlot.Boots), new GearContribution(defence: 0.06f, weight: 3f)),
            new ItemSpec(new ItemIdentity(4, "Hunting Knife", ItemSlot.Weapon, WeaponClass.Sword), new GearContribution(weaponDamage: 40f)),
            new ItemSpec(new ItemIdentity(5, "Hunting Bow", ItemSlot.Weapon, WeaponClass.Bow), new GearContribution(weaponDamage: 28f), shotSpeed: 14f),
            new ItemSpec(new ItemIdentity(6, "Health", ItemSlot.Consumable), GearContribution.Zero, consumable: new RestorePayload(RestoreKind.Health, 0.35f)),
            new ItemSpec(new ItemIdentity(7, "Lucky Charm", ItemSlot.Equipment), new GearContribution(critChance: 0.03f)),
            new ItemSpec(new ItemIdentity(8, "Terrier", ItemSlot.Pet, petClass: PetClass.StatBoost), new GearContribution(maxHealthBonus: 15f)),
        };

        /// <summary>Two synthetic elements (D38) — the generator never learns their names.</summary>
        private static readonly ElementId[] Elements = { new ElementId(1), new ElementId(2) };

        private static GenerationContext Context(float score, int level = 1) =>
            new GenerationContext(score, level, Catalog, QualityTable.Default, DropWeights.Default, Elements);

        [Test]
        public void The_same_seed_and_context_replay_the_identical_item()
        {
            var rng = new DeterministicRandom(1234u);
            DeterministicRandom afterFirst = ItemGenerator.Roll(rng, Context(1.3f, 12), out ItemInstance first);
            DeterministicRandom afterSecond = ItemGenerator.Roll(rng, Context(1.3f, 12), out ItemInstance second);

            Assert.That(second.DisplayName, Is.EqualTo(first.DisplayName));
            Assert.That(second.Quality, Is.EqualTo(first.Quality));
            Assert.That(second.RequiredLevel, Is.EqualTo(first.RequiredLevel));
            Assert.That(second.CoreStats, Is.EqualTo(first.CoreStats));
            Assert.That(second.AffixCount, Is.EqualTo(first.AffixCount));
            for (int i = 0; i < first.AffixCount; i++)
            {
                Assert.That(second.Affixes[i].Id, Is.EqualTo(first.Affixes[i].Id));
                Assert.That(second.Affixes[i].Magnitude, Is.EqualTo(first.Affixes[i].Magnitude));
                Assert.That(second.Affixes[i].Element, Is.EqualTo(first.Affixes[i].Element));
            }

            Assert.That(afterSecond.State, Is.EqualTo(afterFirst.State), "the stream advanced identically");
        }

        [Test]
        public void A_chained_stream_spreads_drops_across_slots()
        {
            var slots = new HashSet<ItemSlot>();
            var rng = new DeterministicRandom(1u);
            for (int i = 0; i < 50; i++)
            {
                rng = ItemGenerator.Roll(rng, Context(1f), out ItemInstance item);
                slots.Add(item.Slot);
            }

            Assert.That(slots.Count, Is.GreaterThan(2),
                "fifty drops off one stream — the real usage — must spread across slots");
        }

        [Test]
        public void An_empty_catalog_yields_the_empty_item_and_no_draws()
        {
            var rng = new DeterministicRandom(7u);
            var context = new GenerationContext(1f, 1, new ItemSpec[0], QualityTable.Default, DropWeights.Default);

            DeterministicRandom after = ItemGenerator.Roll(rng, context, out ItemInstance item);

            Assert.That(item.IsEmpty, Is.True);
            Assert.That(after.State, Is.EqualTo(rng.State), "the stream never moved");
        }

        [Test]
        public void The_boss_forcing_binds_the_definition_and_floors_the_quality()
        {
            GenerationContext context = Context(0.4f, 8).WithSignature(4, QualityRank.Legendary);
            ItemGenerator.Roll(new DeterministicRandom(42u), context, out ItemInstance item);

            Assert.That(item.DefinitionId, Is.EqualTo(4));
            Assert.That(item.Quality, Is.EqualTo(QualityRank.Legendary),
                "a trash score still respects the signature floor");
            Assert.That(item.DisplayName, Is.EqualTo("Legendary Hunting Knife"));
        }

        [Test]
        public void Godly_weapons_roll_three_or_four_affixes_and_both_happen()
        {
            var counts = new HashSet<int>();
            var rng = new DeterministicRandom(9u);
            GenerationContext context = Context(99f).WithForcedSlot(ItemSlot.Weapon);
            for (int i = 0; i < 300; i++)
            {
                rng = ItemGenerator.Roll(rng, context, out ItemInstance item);
                Assert.That(item.Quality, Is.EqualTo(QualityRank.Godly));
                Assert.That(item.AffixCount, Is.InRange(3, 4));
                counts.Add(item.AffixCount);
            }

            Assert.That(counts, Does.Contain(3), "a Godly can come up short");
            Assert.That(counts, Does.Contain(4));
        }

        [Test]
        public void A_nothing_drop_carries_no_affixes_and_no_capacity()
        {
            ItemGenerator.Roll(new DeterministicRandom(11u), Context(0.1f).WithForcedSlot(ItemSlot.Chest), out ItemInstance item);

            Assert.That(item.Quality, Is.EqualTo(QualityRank.Nothing));
            Assert.That(item.AffixCount, Is.Zero);
            Assert.That(item.UpgradeCapacity, Is.Zero);
            Assert.That(item.DisplayName, Is.EqualTo("Leather Chestplate"), "Nothing wears the bare name");
        }

        [Test]
        public void Affixes_never_duplicate_on_one_item()
        {
            var rng = new DeterministicRandom(21u);
            GenerationContext context = Context(99f).WithForcedSlot(ItemSlot.Weapon);
            for (int i = 0; i < 200; i++)
            {
                rng = ItemGenerator.Roll(rng, context, out ItemInstance item);
                var seen = new HashSet<AffixId>();
                for (int a = 0; a < item.AffixCount; a++)
                {
                    Assert.That(seen.Add(item.Affixes[a].Id), Is.True,
                        "an affix appeared twice on " + item.DisplayName);
                }
            }
        }

        [Test]
        public void The_elite_forcing_binds_the_slot()
        {
            var rng = new DeterministicRandom(5u);
            GenerationContext context = Context(1.5f).WithForcedSlot(ItemSlot.Pet);
            for (int i = 0; i < 100; i++)
            {
                rng = ItemGenerator.Roll(rng, context, out ItemInstance item);
                Assert.That(item.Slot, Is.EqualTo(ItemSlot.Pet));
                Assert.That(item.PetClass, Is.EqualTo(PetClass.StatBoost));
            }
        }

        [Test]
        public void The_required_level_stamps_from_progress()
        {
            ItemGenerator.Roll(new DeterministicRandom(3u), Context(1f, 40), out ItemInstance deep);
            Assert.That(deep.RequiredLevel, Is.EqualTo(40), "level-40 content asks for level 40");

            ItemGenerator.Roll(new DeterministicRandom(3u), Context(1f, 0), out ItemInstance floor);
            Assert.That(floor.RequiredLevel, Is.EqualTo(1));
        }

        [Test]
        public void Consumables_roll_clean_of_affixes_and_potency_follows_the_container()
        {
            ItemGenerator.Roll(new DeterministicRandom(17u), Context(99f).WithForcedSlot(ItemSlot.Consumable), out ItemInstance elixir);

            Assert.That(elixir.IsConsumable, Is.True);
            Assert.That(elixir.AffixCount, Is.Zero, "a potion is a potion");
            Assert.That(elixir.DisplayName, Is.EqualTo("Elixir of Health"));
            Assert.That(elixir.ConsumableHealFraction, Is.EqualTo(0.875f).Within(1e-4f),
                "the Godly budget scales the heal");

            ItemGenerator.Roll(new DeterministicRandom(17u), Context(0.1f).WithForcedSlot(ItemSlot.Consumable), out ItemInstance vial);
            Assert.That(vial.DisplayName, Is.EqualTo("Vial of Health"));
            Assert.That(vial.ConsumableHealFraction, Is.EqualTo(0.175f).Within(1e-4f),
                "a Vial is a sip");
        }

        [Test]
        public void Infusion_stays_on_weapons_and_element_affixes_carry_an_element()
        {
            var rng = new DeterministicRandom(31u);
            GenerationContext armor = Context(99f).WithForcedSlot(ItemSlot.Chest);
            for (int i = 0; i < 300; i++)
            {
                rng = ItemGenerator.Roll(rng, armor, out ItemInstance item);
                for (int a = 0; a < item.AffixCount; a++)
                {
                    AffixRoll affix = item.Affixes[a];
                    Assert.That(affix.Id, Is.Not.EqualTo(AffixId.WeaponInfusion),
                        "infusion is weapon-only (D35)");
                    if (affix.Id == AffixId.ElementalResistance)
                    {
                        Assert.That(affix.Element, Is.Not.EqualTo(ElementId.None));
                        Assert.That(Elements, Contains.Item(affix.Element),
                            "An element affix only ever names an authored element (D38).");
                    }
                }
            }
        }

        [Test]
        public void With_no_elements_authored_the_element_affixes_never_roll()
        {
            var rng = new DeterministicRandom(31u);
            var context = new GenerationContext(
                99f, 1, Catalog, QualityTable.Default, DropWeights.Default)
                .WithForcedSlot(ItemSlot.Weapon);

            for (int i = 0; i < 200; i++)
            {
                rng = ItemGenerator.Roll(rng, context, out ItemInstance item);
                for (int a = 0; a < item.AffixCount; a++)
                {
                    Assert.That(item.Affixes[a].Id, Is.Not.EqualTo(AffixId.WeaponInfusion),
                        "An infusion with no element to infuse would be a dead affix.");
                    Assert.That(item.Affixes[a].Id, Is.Not.EqualTo(AffixId.ElementalResistance));
                }
            }
        }

        [Test]
        public void The_budget_grows_core_stats_up_the_ladder()
        {
            GenerationContext trash = Context(0.1f).WithSignature(4, QualityRank.Nothing);
            GenerationContext godly = Context(99f).WithSignature(4, QualityRank.Nothing);

            ItemGenerator.Roll(new DeterministicRandom(2u), trash, out ItemInstance low);
            ItemGenerator.Roll(new DeterministicRandom(2u), godly, out ItemInstance high);

            Assert.That(low.Quality, Is.EqualTo(QualityRank.Nothing));
            Assert.That(high.Quality, Is.EqualTo(QualityRank.Godly));
            Assert.That(high.CoreStats.WeaponDamage, Is.GreaterThan(low.CoreStats.WeaponDamage * 2f),
                "the same knife, worlds apart");
        }

        [Test]
        public void Defence_and_weight_share_one_roll_so_tanky_is_heavy()
        {
            var rng = new DeterministicRandom(13u);
            GenerationContext context = Context(2f).WithForcedSlot(ItemSlot.Helmet);
            for (int i = 0; i < 20; i++)
            {
                rng = ItemGenerator.Roll(rng, context, out ItemInstance item);
                float ratio = item.CoreStats.Defence / item.CoreStats.Weight;
                Assert.That(ratio, Is.EqualTo(0.08f / 4f).Within(1e-5f),
                    "one spread scales both, so the correlation is exact");
            }
        }
    }
}
