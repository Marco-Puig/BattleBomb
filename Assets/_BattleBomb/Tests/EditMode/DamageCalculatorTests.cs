using System.Collections.Generic;
using BattleBomb.Core.Combat;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    public sealed class DamageCalculatorTests
    {
        private static readonly ElementalMultipliers Neutral = ElementalMultipliers.Neutral;

        // Synthetic ids (D38): the pipeline is tested without naming a single element, which is
        // the whole point of the roster being data — these tests survive O11 whatever it decides.
        private static readonly ElementId First = new ElementId(1);
        private static readonly ElementId Second = new ElementId(2);
        private static readonly ElementId Unauthored = new ElementId(9);

        private static ElementalMultipliers Table(params (ElementId Element, float Value)[] rows)
        {
            var entries = new List<ElementalMultiplier>(rows.Length);
            foreach ((ElementId element, float value) in rows)
            {
                entries.Add(new ElementalMultiplier(element, value));
            }

            return ElementalMultipliers.From(entries);
        }

        [Test]
        public void Neutral_everything_passes_base_damage_through()
        {
            float damage = DamageCalculator.Resolve(10f, First, Neutral, Neutral, 1f);

            Assert.That(damage, Is.EqualTo(10f));
        }

        [Test]
        public void Neutral_multipliers_return_one_for_every_element()
        {
            Assert.That(Neutral.For(ElementId.None), Is.EqualTo(1f));
            for (int id = 1; id <= 12; id++)
            {
                Assert.That(Neutral.For(new ElementId(id)), Is.EqualTo(1f), $"element {id}");
            }
        }

        [Test]
        public void An_element_the_table_never_mentions_is_neutral()
        {
            ElementalMultipliers resistance = Table((First, 0.5f));

            Assert.That(resistance.For(Unauthored), Is.EqualTo(1f),
                "A table authored before an element existed must not resist it by accident.");
        }

        [Test]
        public void Resistance_scales_only_the_matching_element()
        {
            ElementalMultipliers resistance = Table((First, 0.5f), (Second, 2f));

            Assert.That(DamageCalculator.Resolve(10f, First, resistance, Neutral, 1f),
                Is.EqualTo(5f));
            Assert.That(DamageCalculator.Resolve(10f, Second, resistance, Neutral, 1f),
                Is.EqualTo(20f));
            Assert.That(DamageCalculator.Resolve(10f, Unauthored, resistance, Neutral, 1f),
                Is.EqualTo(10f));
        }

        [Test]
        public void Climate_scales_only_the_matching_element()
        {
            ElementalMultipliers climate = ElementalMultipliers.Single(First, 1.3f);

            Assert.That(DamageCalculator.Resolve(10f, First, Neutral, climate, 1f),
                Is.EqualTo(13f).Within(1e-4f));
            Assert.That(DamageCalculator.Resolve(10f, Second, Neutral, climate, 1f),
                Is.EqualTo(10f));
        }

        [Test]
        public void The_gear_multiplier_scales_every_element_including_none()
        {
            Assert.That(DamageCalculator.Resolve(10f, ElementId.None, Neutral, Neutral, 1.5f),
                Is.EqualTo(15f));
            Assert.That(DamageCalculator.Resolve(10f, Second, Neutral, Neutral, 1.5f),
                Is.EqualTo(15f));
        }

        [Test]
        public void Every_stage_compounds_in_one_resolution()
        {
            ElementalMultipliers resistance = ElementalMultipliers.Single(First, 0.5f);
            ElementalMultipliers climate = ElementalMultipliers.Single(First, 1.3f);

            float damage = DamageCalculator.Resolve(10f, First, resistance, climate, 1.2f);

            Assert.That(damage, Is.EqualTo(10f * 0.5f * 1.3f * 1.2f).Within(1e-4f));
        }

        [Test]
        public void Elementless_damage_ignores_resistance_and_climate()
        {
            ElementalMultipliers hostile = Table((First, 0.1f), (Second, 0.1f));

            Assert.That(DamageCalculator.Resolve(10f, ElementId.None, hostile, hostile, 1f),
                Is.EqualTo(10f));
        }

        [Test]
        public void Damage_never_resolves_negative()
        {
            Assert.That(DamageCalculator.Resolve(-10f, First, Neutral, Neutral, 1f),
                Is.EqualTo(0f));
            Assert.That(DamageCalculator.Resolve(10f, First, Neutral, Neutral, -2f),
                Is.EqualTo(0f));
        }
    }
}
