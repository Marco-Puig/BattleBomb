using System;
using BattleBomb.Core.Combat;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    public sealed class DamageCalculatorTests
    {
        private static readonly ElementalMultipliers Neutral = ElementalMultipliers.Neutral;

        [Test]
        public void Neutral_everything_passes_base_damage_through()
        {
            float damage = DamageCalculator.Resolve(10f, Element.Fire, Neutral, Neutral, 1f);

            Assert.That(damage, Is.EqualTo(10f));
        }

        [Test]
        public void Neutral_multipliers_return_one_for_every_element()
        {
            foreach (Element element in (Element[])Enum.GetValues(typeof(Element)))
            {
                Assert.That(Neutral.For(element), Is.EqualTo(1f), element.ToString());
            }
        }

        [Test]
        public void Resistance_scales_only_the_matching_element()
        {
            ElementalMultipliers resistance = new ElementalMultipliers(0.5f, 2f, 1f, 1f);

            Assert.That(DamageCalculator.Resolve(10f, Element.Fire, resistance, Neutral, 1f),
                Is.EqualTo(5f));
            Assert.That(DamageCalculator.Resolve(10f, Element.Water, resistance, Neutral, 1f),
                Is.EqualTo(20f));
            Assert.That(DamageCalculator.Resolve(10f, Element.Earth, resistance, Neutral, 1f),
                Is.EqualTo(10f));
        }

        [Test]
        public void Climate_scales_only_the_matching_element()
        {
            ElementalMultipliers climate = new ElementalMultipliers(1.3f, 1f, 1f, 1f);

            Assert.That(DamageCalculator.Resolve(10f, Element.Fire, Neutral, climate, 1f),
                Is.EqualTo(13f).Within(1e-4f));
            Assert.That(DamageCalculator.Resolve(10f, Element.Electric, Neutral, climate, 1f),
                Is.EqualTo(10f));
        }

        [Test]
        public void The_gear_multiplier_scales_every_element_including_none()
        {
            Assert.That(DamageCalculator.Resolve(10f, Element.None, Neutral, Neutral, 1.5f),
                Is.EqualTo(15f));
            Assert.That(DamageCalculator.Resolve(10f, Element.Earth, Neutral, Neutral, 1.5f),
                Is.EqualTo(15f));
        }

        [Test]
        public void Every_stage_compounds_in_one_resolution()
        {
            ElementalMultipliers resistance = new ElementalMultipliers(0.5f, 1f, 1f, 1f);
            ElementalMultipliers climate = new ElementalMultipliers(1.3f, 1f, 1f, 1f);

            float damage = DamageCalculator.Resolve(10f, Element.Fire, resistance, climate, 1.2f);

            Assert.That(damage, Is.EqualTo(10f * 0.5f * 1.3f * 1.2f).Within(1e-4f));
        }

        [Test]
        public void Elementless_damage_ignores_resistance_and_climate()
        {
            ElementalMultipliers hostile = new ElementalMultipliers(0.1f, 0.1f, 0.1f, 0.1f);

            Assert.That(DamageCalculator.Resolve(10f, Element.None, hostile, hostile, 1f),
                Is.EqualTo(10f));
        }

        [Test]
        public void Damage_never_resolves_negative()
        {
            Assert.That(DamageCalculator.Resolve(-10f, Element.Fire, Neutral, Neutral, 1f),
                Is.EqualTo(0f));
            Assert.That(DamageCalculator.Resolve(10f, Element.Fire, Neutral, Neutral, -2f),
                Is.EqualTo(0f));
        }
    }
}
