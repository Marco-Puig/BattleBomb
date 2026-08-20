using BattleBomb.Core.Combat;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The status rulebook (task 52, D40): pricing from the applying hit, the two-way same-element
    /// cancel, ticking, renewal, and the clears. Synthetic elements throughout (D38) — nothing
    /// here knows what id 1 is called, so O11 cannot invalidate a single assertion.
    /// </summary>
    public sealed class StatusTests
    {
        private static readonly ElementId Burnish = new ElementId(1);
        private static readonly ElementId Other = new ElementId(2);

        /// <summary>Fire's paper shape: 3 seconds, six ticks, half the applying hit in total.</summary>
        private static readonly StatusSpec Mark = new StatusSpec("Burn", 180, 30, 0.5f);

        [Test]
        public void A_marks_whole_damage_is_its_share_of_the_hit_that_applied_it()
        {
            Mark.Price(100f, 1f, 1f, out int duration, out float perTick);
            var track = new StatusTrack();
            track.Apply(Burnish, duration, Mark.TickSteps, perTick);

            float total = 0f;
            for (int step = 0; step < 200; step++)
            {
                total += track.Step();
            }

            Assert.That(total, Is.EqualTo(50f).Within(1e-3f),
                "A 100-damage hit at a 0.5 share burns for 50 across the whole mark.");
            Assert.That(track.IsEmpty, Is.True, "And then it is over.");
        }

        [Test]
        public void The_source_scales_the_mark_so_an_infusion_is_half_a_cast()
        {
            Mark.Price(100f, 1f, 1f, out _, out float cast);
            Mark.Price(100f, 0.5f, 1f, out _, out float infusion);

            Assert.That(infusion, Is.EqualTo(cast * 0.5f).Within(1e-4f));
        }

        [Test]
        public void Gear_scaling_reaches_statuses_through_the_hit_alone()
        {
            Mark.Price(10f, 1f, 1f, out _, out float weak);
            Mark.Price(40f, 1f, 1f, out _, out float godly);

            Assert.That(godly, Is.EqualTo(weak * 4f).Within(1e-4f),
                "A four-times-harder hit burns four times as hard, with no second tuning axis.");
        }

        [Test]
        public void Resistance_both_shortens_the_mark_and_thins_each_tick()
        {
            Mark.Price(100f, 1f, 0.5f, out int duration, out float perTick);
            var track = new StatusTrack();
            track.Apply(Burnish, duration, Mark.TickSteps, perTick);

            Assert.That(duration, Is.EqualTo(90), "Half resistance, half the duration.");

            float total = 0f;
            for (int step = 0; step < 200; step++)
            {
                total += track.Step();
            }

            // The hit itself was already resisted, so the per-tick number is unchanged here; the
            // shortening is resistance's second bite, and the two compound in a real fight.
            Assert.That(total, Is.EqualTo(25f).Within(1e-3f), "Three ticks instead of six.");
        }

        [Test]
        public void Ticks_land_on_the_cadence_and_never_before_the_first_interval()
        {
            var track = new StatusTrack();
            track.Apply(Burnish, 180, 30, 5f);

            for (int step = 0; step < 29; step++)
            {
                Assert.That(track.Step(), Is.EqualTo(0f), $"step {step} is still burning up to it");
            }

            Assert.That(track.Step(), Is.EqualTo(5f), "The first tick lands one interval in.");
        }

        [Test]
        public void The_same_element_cancels_both_ways_and_never_marks()
        {
            Assert.That(ElementalExchange.SameElement(Burnish, Burnish), Is.True);
            Assert.That(ElementalExchange.DamageScale(Burnish, Burnish), Is.EqualTo(0.5f),
                "Halved, not nulled — a dead matchup is worse than an expensive one.");
            Assert.That(ElementalExchange.CanApplyStatus(Burnish, Burnish), Is.False,
                "A fire being cannot be burned.");
            Assert.That(ElementalExchange.DamageScale(Burnish, Other), Is.EqualTo(1f));
        }

        [Test]
        public void Kinetic_damage_is_never_touched_by_the_cancel()
        {
            var defender = new ElementalDefence(Burnish, ElementalMultipliers.Neutral);

            Assert.That(DamageCalculator.Resolve(
                    10f, ElementId.None, defender, ElementalMultipliers.Neutral, 1f),
                Is.EqualTo(10f),
                "An infused blade against its own element swings like a plain one — never worse.");
            Assert.That(ElementalExchange.CanApplyStatus(ElementId.None, Burnish), Is.False,
                "Kinetic damage marks nothing either.");
        }

        [Test]
        public void A_defenders_identity_and_resistance_both_price_the_hit()
        {
            var defender = new ElementalDefence(Burnish, ElementalMultipliers.Single(Burnish, 0.5f));

            Assert.That(defender.MultiplierFor(Burnish), Is.EqualTo(0.25f).Within(1e-4f),
                "Resisting it and being it compound.");
            Assert.That(defender.MultiplierFor(Other), Is.EqualTo(1f));
        }

        [Test]
        public void The_climate_scales_elemental_damage_whoever_deals_it()
        {
            ElementalMultipliers cold = ElementalMultipliers.Single(Burnish, 1.25f);

            float mine = DamageCalculator.Resolve(10f, Burnish, ElementalDefence.None, cold, 1f);
            float theirs = DamageCalculator.Resolve(10f, Burnish, ElementalDefence.None, cold, 1f);

            Assert.That(mine, Is.EqualTo(12.5f).Within(1e-4f));
            Assert.That(theirs, Is.EqualTo(mine), "D41: symmetric, which is what makes it strategy.");
        }

        [Test]
        public void Renewing_a_mark_never_makes_it_weaker_or_shorter()
        {
            var track = new StatusTrack();
            track.Apply(Burnish, 180, 30, 10f);
            for (int step = 0; step < 60; step++)
            {
                track.Step();
            }

            track.Apply(Burnish, 60, 30, 2f);

            Assert.That(track.Count, Is.EqualTo(1), "One mark per element — never a stack.");
            track.TryGet(Burnish, out StatusInstance status);
            Assert.That(status.DamagePerTick, Is.EqualTo(10f),
                "A weak infusion sustains a strong cast's mark instead of overwriting it.");
            Assert.That(status.RemainingSteps, Is.EqualTo(120));
        }

        [Test]
        public void Two_elements_mark_the_same_target_independently()
        {
            var track = new StatusTrack();
            track.Apply(Burnish, 180, 30, 3f);
            track.Apply(Other, 180, 30, 4f);

            Assert.That(track.Count, Is.EqualTo(2));

            float damage = 0f;
            for (int step = 0; step < 30; step++)
            {
                damage += track.Step();
            }

            Assert.That(damage, Is.EqualTo(7f).Within(1e-4f), "Both ticked together.");
        }

        [Test]
        public void Nothing_and_nowhere_apply_nothing()
        {
            var track = new StatusTrack();

            Assert.That(track.Apply(ElementId.None, 180, 30, 5f), Is.False);
            Assert.That(track.Apply(Burnish, 0, 30, 5f), Is.False, "A zero-length mark is no mark.");
            Assert.That(track.IsEmpty, Is.True);
            Assert.That(track.Step(), Is.EqualTo(0f));
        }

        [Test]
        public void Clearing_ends_every_mark_at_once()
        {
            var track = new StatusTrack();
            track.Apply(Burnish, 180, 30, 3f);
            track.Apply(Other, 180, 30, 3f);

            track.Clear();

            Assert.That(track.IsEmpty, Is.True, "Death and the downed state start clean.");
        }

        [Test]
        public void A_status_tick_drains_health_without_taking_control()
        {
            PlayerCondition condition = PlayerCondition.Fresh(100f);
            condition = condition.Hit(10f, staggerSteps: 20, graceSteps: 40);
            Assert.That(condition.IsInvulnerable, Is.True, "Grace is up.");

            condition = condition.Drained(15f);

            Assert.That(condition.Health.Current, Is.EqualTo(75f),
                "Grace answers hits, never marks — otherwise burns would be waited out for free.");
            Assert.That(condition.StaggerSteps, Is.EqualTo(20), "The tick added no stagger of its own.");
        }

        [Test]
        public void A_mark_can_be_what_finally_downs_a_player()
        {
            PlayerCondition condition = PlayerCondition.Fresh(10f);

            condition = condition.Drained(12f);

            Assert.That(condition.IsDown, Is.True);
            Assert.That(condition.Drained(5f).Health.Current, Is.EqualTo(0f),
                "And nothing burns the downed further.");
        }
    }
}
