using System;
using BattleBomb.Core.Combat;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    public sealed class HealthTests
    {
        [Test]
        public void Health_starts_full()
        {
            Health health = new Health(50f);

            Assert.That(health.Current, Is.EqualTo(50f));
            Assert.That(health.Max, Is.EqualTo(50f));
            Assert.That(health.IsDepleted, Is.False);
        }

        [Test]
        public void Non_positive_max_health_throws()
        {
            Assert.Throws<ArgumentException>(() => new Health(0f));
            Assert.Throws<ArgumentException>(() => new Health(-10f));
        }

        [Test]
        public void Damage_reduces_and_overkill_clamps_at_zero()
        {
            Health health = new Health(50f).Damaged(20f);
            Assert.That(health.Current, Is.EqualTo(30f));
            Assert.That(health.IsDepleted, Is.False);

            health = health.Damaged(100f);
            Assert.That(health.Current, Is.EqualTo(0f));
            Assert.That(health.IsDepleted, Is.True);
        }

        [Test]
        public void Non_positive_damage_changes_nothing()
        {
            Health health = new Health(50f);

            Assert.That(health.Damaged(0f).Current, Is.EqualTo(50f));
            Assert.That(health.Damaged(-5f).Current, Is.EqualTo(50f),
                "Negative damage must never heal.");
        }

        [Test]
        public void Refilled_restores_full_health()
        {
            Health health = new Health(50f).Damaged(50f);

            Assert.That(health.IsDepleted, Is.True);
            Assert.That(health.Refilled().Current, Is.EqualTo(50f));
        }
    }
}
