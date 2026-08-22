using BattleBomb.Core.Simulation;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    public sealed class SimulationClockTests
    {
        private const float Sixtieth = 1f / 60f;

        [Test]
        public void Banks_no_step_until_a_whole_step_of_time_has_passed()
        {
            SimulationClock clock = new SimulationClock(60);

            Assert.That(clock.Accumulate(Sixtieth * 0.5f), Is.EqualTo(0));
            Assert.That(clock.TryConsumeStep(out _), Is.False);
            Assert.That(clock.Accumulate(Sixtieth * 0.5f), Is.EqualTo(1));
        }

        [Test]
        public void A_slow_frame_banks_several_steps()
        {
            SimulationClock clock = new SimulationClock(60);

            Assert.That(clock.Accumulate(Sixtieth * 3f), Is.EqualTo(3));
        }

        [Test]
        public void Backlog_is_capped_so_a_hitch_does_not_spiral()
        {
            SimulationClock clock = new SimulationClock(60, maxStepsPerFrame: 5);

            Assert.That(clock.Accumulate(2f), Is.EqualTo(5), "Two seconds of hitch must not queue 120 steps.");
        }

        [Test]
        public void Consuming_steps_advances_the_frame_counter_once_each()
        {
            SimulationClock clock = new SimulationClock(60);
            clock.Accumulate(Sixtieth * 2f);

            Assert.That(clock.TryConsumeStep(out int first), Is.True);
            Assert.That(clock.TryConsumeStep(out int second), Is.True);
            Assert.That(clock.TryConsumeStep(out _), Is.False);

            Assert.That(first, Is.EqualTo(0));
            Assert.That(second, Is.EqualTo(1));
            Assert.That(clock.Frame, Is.EqualTo(2));
        }

        [Test]
        public void Alpha_reports_the_leftover_fraction_for_interpolation()
        {
            SimulationClock clock = new SimulationClock(60);
            clock.Accumulate(Sixtieth * 1.5f);

            Assert.That(clock.Alpha, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void Reset_drops_banked_time_but_keeps_the_frame_counter()
        {
            SimulationClock clock = new SimulationClock(60);
            clock.Accumulate(Sixtieth * 2f);
            clock.TryConsumeStep(out _);

            clock.Reset();

            Assert.That(clock.TryConsumeStep(out _), Is.False, "Time spent paused must not be simulated.");
            Assert.That(clock.Alpha, Is.EqualTo(0f));
            Assert.That(clock.Frame, Is.EqualTo(1));
        }

        [Test]
        public void Negative_delta_time_is_ignored()
        {
            SimulationClock clock = new SimulationClock(60);

            Assert.That(clock.Accumulate(-1f), Is.EqualTo(0));
            Assert.That(clock.Alpha, Is.EqualTo(0f));
        }

        [Test]
        public void Step_duration_follows_the_configured_rate()
        {
            Assert.That(new SimulationClock(30).StepDuration, Is.EqualTo(1f / 30f).Within(0.0001f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SimulationClock(0));
        }
    }
}
