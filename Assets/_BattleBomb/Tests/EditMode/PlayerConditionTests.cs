using BattleBomb.Core.Combat;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    public sealed class PlayerConditionTests
    {
        private const int Stagger = 15;
        private const int Grace = 30;

        [Test]
        public void Fresh_starts_full_and_in_control()
        {
            PlayerCondition condition = PlayerCondition.Fresh(100f);

            Assert.That(condition.Health.Current, Is.EqualTo(100f));
            Assert.That(condition.InControl, Is.True);
            Assert.That(condition.IsDown, Is.False);
            Assert.That(condition.IsInvulnerable, Is.False);
        }

        [Test]
        public void A_hit_damages_staggers_and_grants_grace()
        {
            PlayerCondition condition = PlayerCondition.Fresh(100f).Hit(20f, Stagger, Grace);

            Assert.That(condition.Health.Current, Is.EqualTo(80f));
            Assert.That(condition.InControl, Is.False, "The stagger takes control.");
            Assert.That(condition.IsInvulnerable, Is.True, "The grace starts immediately.");
        }

        [Test]
        public void A_hit_during_grace_changes_nothing()
        {
            PlayerCondition hit = PlayerCondition.Fresh(100f).Hit(20f, Stagger, Grace);
            PlayerCondition again = hit.Hit(50f, Stagger, Grace);

            Assert.That(again.Health.Current, Is.EqualTo(hit.Health.Current),
                "Grace swallows the whole hit — the anti-stunlock rule.");
            Assert.That(again.StaggerSteps, Is.EqualTo(hit.StaggerSteps));
            Assert.That(again.GraceSteps, Is.EqualTo(hit.GraceSteps));
        }

        [Test]
        public void Control_returns_before_the_grace_ends()
        {
            PlayerCondition condition = PlayerCondition.Fresh(100f).Hit(20f, Stagger, Grace);

            for (int i = 0; i < Stagger; i++)
            {
                condition = condition.Step();
            }

            Assert.That(condition.InControl, Is.True, "The stagger has run out.");
            Assert.That(condition.IsInvulnerable, Is.True,
                "Still protected — grace outlasts stagger so recovery is never a free hit.");

            for (int i = Stagger; i < Grace; i++)
            {
                condition = condition.Step();
            }

            Assert.That(condition.IsInvulnerable, Is.False, "Grace has ended too.");
        }

        [Test]
        public void Depletion_downs_the_player()
        {
            PlayerCondition condition = PlayerCondition.Fresh(50f).Hit(50f, Stagger, Grace);

            Assert.That(condition.IsDown, Is.True);
            Assert.That(condition.InControl, Is.False, "Downed players take no orders (D25).");
            Assert.That(condition.IsInvulnerable, Is.True, "Nothing lands on a downed player.");
        }

        [Test]
        public void A_downed_player_cannot_be_hit_again()
        {
            PlayerCondition down = PlayerCondition.Fresh(50f).Hit(50f, Stagger, Grace);
            PlayerCondition kicked = down.Hit(999f, Stagger, Grace);

            Assert.That(kicked.Health.Current, Is.EqualTo(0f));
            Assert.That(kicked.IsDown, Is.True);
        }

        [Test]
        public void Being_downed_never_expires_on_its_own()
        {
            PlayerCondition condition = PlayerCondition.Fresh(50f).Hit(50f, Stagger, Grace);

            for (int i = 0; i < 600; i++)
            {
                condition = condition.Step();
            }

            Assert.That(condition.IsDown, Is.True, "Only a revive or the attempt ending stands a player up (D25).");
        }

        [Test]
        public void Timers_clamp_at_zero()
        {
            PlayerCondition condition = PlayerCondition.Fresh(100f);

            for (int i = 0; i < 100; i++)
            {
                condition = condition.Step();
            }

            Assert.That(condition.StaggerSteps, Is.EqualTo(0));
            Assert.That(condition.GraceSteps, Is.EqualTo(0));
            Assert.That(condition.InControl, Is.True);
        }
    }
}
