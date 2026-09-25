using BattleBomb.Core.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class RenderClockTests
    {
        [Test]
        public void The_first_advance_starts_the_delay_behind_the_newest()
        {
            var clock = new RenderClock(delaySteps: 6);
            Assert.That(clock.IsRunning, Is.False);
            Assert.That(clock.Advance(100), Is.EqualTo(94f));
        }

        [Test]
        public void In_step_with_the_host_it_advances_one_step_per_step()
        {
            var clock = new RenderClock(delaySteps: 6);
            clock.Advance(100);
            for (int newest = 101; newest < 200; newest++)
            {
                clock.Advance(newest);
            }

            Assert.That(clock.Frame, Is.EqualTo(193f).Within(0.01f));
        }

        [Test]
        public void Behind_it_hurries_and_ahead_it_waits_but_never_by_more_than_a_quarter_step()
        {
            var behind = new RenderClock(delaySteps: 6);
            behind.Advance(100);
            float before = behind.Frame;
            behind.Advance(120);
            float stride = behind.Frame - before;
            Assert.That(stride, Is.GreaterThan(1f));
            Assert.That(stride, Is.LessThanOrEqualTo(1.25f + 1e-4f));

            // Ahead: the newest stalls (it never goes backwards), so the gap grows until the ease reaches
            // its quarter-step limit.
            var ahead = new RenderClock(delaySteps: 20);
            ahead.Advance(100);
            float slowest = float.MaxValue;
            for (int i = 0; i < 10; i++)
            {
                before = ahead.Frame;
                ahead.Advance(100);
                slowest = System.Math.Min(slowest, ahead.Frame - before);
            }

            Assert.That(slowest, Is.LessThan(1f));
            Assert.That(slowest, Is.GreaterThanOrEqualTo(0.75f - 1e-4f));
        }

        [Test]
        public void It_never_draws_past_the_newest_snapshot()
        {
            var clock = new RenderClock(delaySteps: 0);
            clock.Advance(50);
            for (int i = 0; i < 20; i++)
            {
                clock.Advance(50);
            }

            Assert.That(clock.Frame, Is.LessThanOrEqualTo(50f));
        }

        [Test]
        public void A_long_stall_snaps_rather_than_crawling_to_catch_up()
        {
            var clock = new RenderClock(delaySteps: 6);
            clock.Advance(100);
            Assert.That(clock.Advance(400), Is.EqualTo(394f));
        }
    }
}
