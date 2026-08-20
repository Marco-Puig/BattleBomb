using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The four sleeping affixes, awake (task 54): magic damage and range reach the sheet,
    /// elemental resistance aggregates per element and caps, and a weapon's infusion travels to
    /// the hit. Synthetic elements, as everywhere (D38).
    /// </summary>
    public sealed class MagicGearTests
    {
        private static readonly ElementId First = new ElementId(1);
        private static readonly ElementId Second = new ElementId(2);

        private static readonly StatTuning Tuning = StatTuning.Default;

        private static StatSheet Sheet(
            IReadOnlyList<GearContribution> gear,
            IReadOnlyList<ElementalMultiplier> resistances = null) =>
            StatSheet.Build(BaseStats.Zero, Tuning, gear, resistances);

        private static ItemInstance Weapon(params AffixRoll[] affixes) => new ItemInstance(
            definitionId: 1, displayName: "Blade", slot: ItemSlot.Weapon,
            weaponClass: WeaponClass.Sword, petClass: PetClass.None, quality: QualityRank.Shiny,
            coreStats: new GearContribution(weaponDamage: 12f), affixes: affixes,
            requiredLevel: 1, upgradeCapacity: 0);

        /// <summary>Equipping through the real inventory path — Loadout.Swap is internal.</summary>
        private static Loadout Wearing(in ItemInstance item)
        {
            var inventory = new Inventory();
            inventory.Add(item, currentLevel: 99);
            inventory.TryEquip(0, currentLevel: 99);
            return inventory.Loadout;
        }

        [Test]
        public void Magic_damage_and_range_reach_the_sheet_from_any_slot()
        {
            StatSheet sheet = Sheet(new[]
            {
                AffixEffects.Contribution(new AffixRoll(AffixId.MagicDamage, 8f)),
                AffixEffects.Contribution(new AffixRoll(AffixId.MagicRange, 1.2f)),
                AffixEffects.Contribution(new AffixRoll(AffixId.MagicDamage, 5f)),
            });

            Assert.That(sheet.MagicDamage, Is.EqualTo(13f), "Same-stat sources stack additively.");
            Assert.That(sheet.MagicRange, Is.EqualTo(1.2f).Within(1e-4f));
        }

        [Test]
        public void Strength_never_touches_magic()
        {
            var gear = new[] { AffixEffects.Contribution(new AffixRoll(AffixId.MagicDamage, 10f)) };
            var strong = new BaseStats(strength: 50, hp: 0, mana: 0, speed: 0);

            StatSheet weak = StatSheet.Build(BaseStats.Zero, Tuning, gear);
            StatSheet mighty = StatSheet.Build(strong, Tuning, gear);

            Assert.That(mighty.WeaponDamage, Is.GreaterThan(weak.WeaponDamage), "Strength is weapons.");
            Assert.That(mighty.MagicDamage, Is.EqualTo(weak.MagicDamage),
                "D32/D39: the caster build is found in loot, never allocated.");
        }

        [Test]
        public void Elemental_resistance_sums_per_element_and_leaves_the_rest_neutral()
        {
            StatSheet sheet = Sheet(new GearContribution[0], new[]
            {
                new ElementalMultiplier(First, 0.10f),
                new ElementalMultiplier(First, 0.15f),
                new ElementalMultiplier(Second, 0.20f),
            });

            Assert.That(sheet.ElementalResistance.For(First), Is.EqualTo(0.75f).Within(1e-4f),
                "25% off the top across two pieces.");
            Assert.That(sheet.ElementalResistance.For(Second), Is.EqualTo(0.80f).Within(1e-4f));
            Assert.That(sheet.ElementalResistance.For(new ElementId(9)), Is.EqualTo(1f));
        }

        [Test]
        public void No_build_ever_goes_immune_to_an_element()
        {
            var stacked = new List<ElementalMultiplier>();
            for (int i = 0; i < 12; i++)
            {
                stacked.Add(new ElementalMultiplier(First, 0.15f));
            }

            StatSheet sheet = Sheet(new GearContribution[0], stacked);

            Assert.That(sheet.ElementalResistance.For(First),
                Is.EqualTo(1f - Tuning.ElementalResistCap).Within(1e-4f),
                "D40: gear is an answer to a hostile region, never an off-switch.");
        }

        [Test]
        public void Resistance_gear_thins_incoming_elemental_damage()
        {
            StatSheet sheet = Sheet(new GearContribution[0],
                new[] { new ElementalMultiplier(First, 0.25f) });
            var defence = new ElementalDefence(ElementId.None, sheet.ElementalResistance);

            float damage = DamageCalculator.Resolve(
                100f, First, defence, ElementalMultipliers.Neutral, 1f);

            Assert.That(damage, Is.EqualTo(75f).Within(1e-3f));
        }

        [Test]
        public void Resistance_also_shortens_the_mark_it_resists()
        {
            StatSheet sheet = Sheet(new GearContribution[0],
                new[] { new ElementalMultiplier(First, 0.25f) });
            var mark = new StatusSpec("Burn", 180, 30, 0.5f);

            mark.Price(50f, 1f, sheet.ElementalResistance.For(First), out int duration, out _);

            Assert.That(duration, Is.EqualTo(135), "D40: resistance bites twice.");
        }

        [Test]
        public void An_infused_weapon_hands_its_element_to_every_hit()
        {
            Loadout loadout = Wearing(Weapon(new AffixRoll(AffixId.WeaponInfusion, 0.5f, Second)));

            Assert.That(loadout.TryGetInfusion(out ElementId element, out float scale), Is.True);
            Assert.That(element, Is.EqualTo(Second));
            Assert.That(scale, Is.EqualTo(0.5f).Within(1e-4f),
                "The rolled magnitude IS the share of a cast's mark it carries.");
        }

        [Test]
        public void A_plain_weapon_infuses_nothing()
        {
            Loadout loadout = Wearing(Weapon(new AffixRoll(AffixId.CritChance, 0.05f)));

            Assert.That(loadout.TryGetInfusion(out ElementId element, out float scale), Is.False);
            Assert.That(element.IsNone, Is.True);
            Assert.That(scale, Is.EqualTo(0f));
        }

        [Test]
        public void An_infusion_roll_can_never_out_burn_a_cast()
        {
            Loadout loadout = Wearing(Weapon(new AffixRoll(AffixId.WeaponInfusion, 4f, Second)));

            loadout.TryGetInfusion(out _, out float scale);

            Assert.That(scale, Is.EqualTo(1f), "Even a runaway roll caps at a cast's own mark.");
        }

        [Test]
        public void An_infused_hit_marks_the_target_through_the_strike()
        {
            var element = new ElementSpec(First, "First", new StatusSpec("Mark", 180, 30, 0.5f));
            var track = new StatusTrack();

            ElementalStrike.Apply(
                track, element, ElementId.None, ReactionTable.Empty,
                hitDamage: 40f, sourceScale: 0.5f, resistanceMultiplier: 1f);

            Assert.That(track.TryGet(First, out StatusInstance status), Is.True);
            Assert.That(status.DamagePerTick, Is.EqualTo(40f * 0.5f * 0.5f / 6f).Within(1e-4f),
                "Half a cast's share, priced from the swing that carried it.");
        }

        [Test]
        public void Armor_still_never_carries_an_infusion()
        {
            Loadout loadout = Wearing(new ItemInstance(
                definitionId: 2, displayName: "Plate", slot: ItemSlot.Chest,
                weaponClass: WeaponClass.None, petClass: PetClass.None, quality: QualityRank.Shiny,
                coreStats: new GearContribution(defence: 0.1f),
                affixes: new[] { new AffixRoll(AffixId.WeaponInfusion, 0.5f, First) },
                requiredLevel: 1, upgradeCapacity: 0));

            Assert.That(loadout.TryGetInfusion(out _, out _), Is.False,
                "Only the weapon slot infuses, even if an affix somehow landed elsewhere.");
        }
    }
}
