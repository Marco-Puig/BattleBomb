using System.Collections.Generic;
using BattleBomb.Core.Combat;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// D25's partner revive on D31's heartbeat, and the attempt-over beat (task 35): the timing
    /// channel's full rulebook, the accuracy-priced return, and the all-down countdown — every
    /// rule pure and stepped by hand. The beat is 40 here so its centre lands on a whole step.
    /// </summary>
    public sealed class ReviveTests
    {
        private const float Required = 10f;
        private const int Beat = 40;
        private const int Rush = 15;
        private const int Decay = 60;

        private static ReviveChannel Step(in ReviveChannel channel, bool pressed, int target = 1, bool inControl = true) =>
            ReviveChannel.Next(channel, pressed, target, inControl, Beat, Rush, Decay);

        [Test]
        public void A_light_press_beside_a_downed_partner_starts_the_channel()
        {
            ReviveChannel channel = Step(ReviveChannel.Inactive, pressed: true);

            Assert.That(channel.IsActive, Is.True);
            Assert.That(channel.TargetIndex, Is.EqualTo(1));
            Assert.That(channel.Progress, Is.EqualTo(ReviveChannel.SloppyPumpProgress));
            Assert.That(channel.Pumps, Is.Zero, "the starting press is never accuracy-rated");
        }

        [Test]
        public void Without_the_press_nothing_starts()
        {
            ReviveChannel channel = Step(ReviveChannel.Inactive, pressed: false);

            Assert.That(channel.IsActive, Is.False);
        }

        [Test]
        public void Riding_the_beat_pays_the_ceiling()
        {
            ReviveChannel channel = Step(ReviveChannel.Inactive, true);
            for (int k = 0; k < 5; k++)
            {
                while (channel.StepsElapsed < Beat * k + Beat / 2 - 1)
                {
                    channel = Step(channel, false);
                }

                channel = Step(channel, true);
            }

            Assert.That(channel.IsComplete(Required), Is.True, "five perfect chunks complete it");
            Assert.That(channel.AverageAccuracy, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(
                ReviveChannel.RestoredFraction(channel.AverageAccuracy, 0.25f, 0.65f),
                Is.EqualTo(0.65f).Within(1e-4f), "precision buys the max health");
        }

        [Test]
        public void Pure_mashing_completes_fast_but_pays_the_floor()
        {
            ReviveChannel channel = Step(ReviveChannel.Inactive, true);
            for (int i = 0; i < 19; i++)
            {
                channel = Step(channel, true);
            }

            Assert.That(channel.IsComplete(Required), Is.True, "twenty sloppy chunks complete it");
            Assert.That(channel.StepsElapsed, Is.EqualTo(20), "mashing is by far the fastest route");
            Assert.That(channel.AverageAccuracy, Is.Zero, "every rushed press rates zero");
            Assert.That(
                ReviveChannel.RestoredFraction(channel.AverageAccuracy, 0.25f, 0.65f),
                Is.EqualTo(0.25f), "and pays only the floor");
        }

        [Test]
        public void An_off_beat_press_earns_between_the_extremes()
        {
            ReviveChannel channel = Step(ReviveChannel.Inactive, true);
            while (channel.StepsElapsed < 29)
            {
                channel = Step(channel, false);
            }

            channel = Step(channel, true);

            Assert.That(channel.AverageAccuracy, Is.EqualTo(0.5f).Within(1e-4f));
            float expected = ReviveChannel.SloppyPumpProgress
                + ReviveChannel.SloppyPumpProgress
                + 0.5f * (ReviveChannel.PerfectPumpProgress - ReviveChannel.SloppyPumpProgress);
            Assert.That(channel.Progress, Is.EqualTo(expected).Within(1e-4f),
                "an off-beat press earns more than sloppy, less than perfect");
        }

        [Test]
        public void The_beat_clock_peaks_at_centre_and_wraps()
        {
            Assert.That(ReviveChannel.PressAccuracy(Beat / 2, Beat), Is.EqualTo(1f));
            Assert.That(ReviveChannel.PressAccuracy(0, Beat), Is.EqualTo(0f));
            Assert.That(ReviveChannel.PressAccuracy(Beat, Beat), Is.EqualTo(0f));
            Assert.That(ReviveChannel.BeatPhase(Beat + Beat / 2, Beat), Is.EqualTo(0.5f));
        }

        [Test]
        public void The_restored_fraction_clamps_its_accuracy()
        {
            Assert.That(ReviveChannel.RestoredFraction(2f, 0.25f, 0.65f), Is.EqualTo(0.65f));
            Assert.That(ReviveChannel.RestoredFraction(-1f, 0.25f, 0.65f), Is.EqualTo(0.25f));
        }

        [Test]
        public void Silence_drains_progress_until_the_channel_drops()
        {
            ReviveChannel channel = Step(ReviveChannel.Inactive, true);
            while (channel.StepsElapsed < Beat / 2 - 1)
            {
                channel = Step(channel, false);
            }

            channel = Step(channel, true);
            float earned = channel.Progress;

            for (int i = 0; i < Decay; i++)
            {
                channel = Step(channel, false);
            }

            Assert.That(channel.IsActive, Is.True, "a healthy bar survives one drain");
            Assert.That(channel.Progress, Is.LessThan(earned), "but silence costs progress");

            for (int i = 0; i < Decay * 5; i++)
            {
                channel = Step(channel, false);
            }

            Assert.That(channel.IsActive, Is.False, "draining to zero drops the channel");
        }

        [Test]
        public void Losing_control_breaks_the_channel()
        {
            ReviveChannel channel = Step(ReviveChannel.Inactive, true);
            channel = Step(channel, false);

            channel = Step(channel, false, inControl: false);

            Assert.That(channel.IsActive, Is.False);
        }

        [Test]
        public void Leaving_range_breaks_the_channel()
        {
            ReviveChannel channel = Step(ReviveChannel.Inactive, true);
            channel = Step(channel, false);

            channel = Step(channel, false, target: -1);

            Assert.That(channel.IsActive, Is.False);
        }

        [Test]
        public void A_target_change_breaks_the_channel()
        {
            ReviveChannel channel = Step(ReviveChannel.Inactive, true);

            channel = Step(channel, false, target: 0);

            Assert.That(channel.IsActive, Is.False);
        }

        [Test]
        public void A_broken_channel_restarts_from_zero()
        {
            ReviveChannel channel = Step(ReviveChannel.Inactive, true);
            for (int i = 0; i < 6; i++)
            {
                channel = Step(channel, true);
            }

            channel = Step(channel, false, target: -1);
            channel = Step(channel, true);

            Assert.That(channel.Progress, Is.EqualTo(ReviveChannel.SloppyPumpProgress));
            Assert.That(channel.StepsElapsed, Is.EqualTo(1));
            Assert.That(channel.Pumps, Is.Zero);
        }

        [Test]
        public void FindTarget_picks_the_nearest_downed_partner_within_range()
        {
            var positions = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1.2f, 0f, 0f),
                new Vector3(0.6f, 0f, 0.4f),
            };
            var downed = new List<bool> { false, true, true };

            int target = ReviveChannel.FindTarget(positions[0], positions, downed, 0, 1.8f);

            Assert.That(target, Is.EqualTo(2));
        }

        [Test]
        public void FindTarget_never_picks_the_reviver_or_a_standing_player()
        {
            var positions = new List<Vector3> { Vector3.zero, new Vector3(0.5f, 0f, 0f) };
            var downed = new List<bool> { true, false };

            int target = ReviveChannel.FindTarget(positions[0], positions, downed, 0, 1.8f);

            Assert.That(target, Is.EqualTo(-1));
        }

        [Test]
        public void FindTarget_ignores_partners_beyond_range_and_height_never_counts()
        {
            var positions = new List<Vector3>
            {
                Vector3.zero,
                new Vector3(5f, 0f, 0f),
                new Vector3(1f, 4f, 0f),
            };
            var downed = new List<bool> { false, true, true };

            Assert.That(ReviveChannel.FindTarget(positions[0], positions, downed, 0, 1.8f),
                Is.EqualTo(2), "planar distance decides — height is ignored");
            Assert.That(ReviveChannel.FindTarget(positions[0], positions, new List<bool> { false, true, false }, 0, 1.8f),
                Is.EqualTo(-1), "out of range finds nothing");
        }

        [Test]
        public void Revive_stands_the_player_at_half_health_with_grace()
        {
            PlayerCondition downed = PlayerCondition.Fresh(100f).Hit(150f, 15, 30);
            Assert.That(downed.IsDown, Is.True, "the setup must down the player");

            PlayerCondition revived = downed.Revived(0.5f, 60);

            Assert.That(revived.IsDown, Is.False);
            Assert.That(revived.InControl, Is.True);
            Assert.That(revived.Health.Current, Is.EqualTo(50f));
            Assert.That(revived.IsInvulnerable, Is.True, "the revive grace holds");
            Assert.That(revived.GraceSteps, Is.EqualTo(60));
        }

        [Test]
        public void The_partial_refill_clamps_its_fraction()
        {
            Health health = new Health(80f).Damaged(80f);

            Assert.That(health.Refilled(2f).Current, Is.EqualTo(80f));
            Assert.That(health.Refilled(-1f).Current, Is.EqualTo(0f));
            Assert.That(health.Refilled(0.25f).Current, Is.EqualTo(20f));
        }

        [Test]
        public void The_attempt_countdown_runs_only_while_everyone_stays_down()
        {
            AttemptCountdown countdown = default;
            for (int i = 0; i < 60; i++)
            {
                countdown = countdown.Step(true);
            }

            Assert.That(countdown.Triggers(120), Is.False);

            countdown = countdown.Step(false);
            Assert.That(countdown.StepsAllDown, Is.EqualTo(0), "anyone standing resets the beat");

            for (int i = 0; i < 120; i++)
            {
                countdown = countdown.Step(true);
            }

            Assert.That(countdown.Triggers(120), Is.True);
        }

        [Test]
        public void All_down_requires_at_least_one_player()
        {
            Assert.That(AttemptCountdown.AllDown(new List<bool>()), Is.False);
            Assert.That(AttemptCountdown.AllDown(new List<bool> { true, true }), Is.True);
            Assert.That(AttemptCountdown.AllDown(new List<bool> { true, false }), Is.False);
        }
    }
}

