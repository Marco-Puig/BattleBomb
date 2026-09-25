using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class RemoteCommandStreamTests
    {
        [Test]
        public void Nothing_plays_until_the_buffer_holds_its_target_depth()
        {
            var stream = new RemoteCommandStream(target: 2, max: 6);
            stream.Receive(new WireCommand(1, Vector2.right, CommandButtons.None));

            Assert.That(stream.Next(100).Move, Is.EqualTo(Vector2.zero), "Played before the buffer was primed.");

            stream.Receive(new WireCommand(2, Vector2.right, CommandButtons.None));
            Assert.That(stream.Next(101).Move, Is.EqualTo(Vector2.right));
            Assert.That(stream.LastConsumedFrame, Is.EqualTo(1));
        }

        [Test]
        public void A_press_is_one_edge_even_when_its_packet_arrives_twice()
        {
            var stream = Primed();
            stream.Receive(new WireCommand(3, Vector2.zero, CommandButtons.Light));
            stream.Receive(new WireCommand(3, Vector2.zero, CommandButtons.Light));
            stream.Receive(new WireCommand(4, Vector2.zero, CommandButtons.Light));

            PlayerCommand first = stream.Next(12);
            PlayerCommand second = stream.Next(13);

            Assert.That(first.WasPressed(CommandButtons.Light), Is.True);
            Assert.That(second.WasPressed(CommandButtons.Light), Is.False);
            Assert.That(second.IsHeld(CommandButtons.Light), Is.True);
        }

        [Test]
        public void A_starved_step_repeats_what_was_held_and_never_a_press()
        {
            var stream = Primed();
            stream.Receive(new WireCommand(3, Vector2.left, CommandButtons.Jump));
            Assert.That(stream.Next(12).WasPressed(CommandButtons.Jump), Is.True);

            PlayerCommand starved = stream.Next(13);

            Assert.That(starved.IsHeld(CommandButtons.Jump), Is.True);
            Assert.That(starved.Move, Is.EqualTo(Vector2.left));
            Assert.That(starved.Pressed, Is.EqualTo(CommandButtons.None), "A lost packet became a second jump.");
            Assert.That(stream.StarvedSteps, Is.EqualTo(1));
        }

        [Test]
        public void A_button_held_through_a_starved_step_is_not_pressed_again_when_commands_resume()
        {
            var stream = Primed();
            stream.Receive(new WireCommand(3, Vector2.zero, CommandButtons.Heavy));
            stream.Next(12);
            stream.Next(13);

            stream.Receive(new WireCommand(4, Vector2.zero, CommandButtons.Heavy));
            PlayerCommand resumed = stream.Next(14);

            Assert.That(resumed.IsHeld(CommandButtons.Heavy), Is.True);
            Assert.That(resumed.WasPressed(CommandButtons.Heavy), Is.False,
                "Recovering from a lost packet pressed a held button a second time.");
        }

        [Test]
        public void Catching_up_merges_two_steps_and_keeps_both_presses()
        {
            var stream = new RemoteCommandStream(target: 1, max: 2);
            stream.Receive(new WireCommand(1, Vector2.zero, CommandButtons.Light));
            stream.Receive(new WireCommand(2, Vector2.zero, CommandButtons.Heavy));
            stream.Receive(new WireCommand(3, Vector2.zero, CommandButtons.Heavy));
            stream.Receive(new WireCommand(4, Vector2.zero, CommandButtons.Heavy));

            PlayerCommand merged = stream.Next(50);

            Assert.That(merged.WasPressed(CommandButtons.Light), Is.True);
            Assert.That(merged.WasPressed(CommandButtons.Heavy), Is.True);
            Assert.That(merged.WasReleased(CommandButtons.Light), Is.True);
            Assert.That(merged.IsHeld(CommandButtons.Heavy), Is.True);
            Assert.That(stream.MergedSteps, Is.EqualTo(1));
            Assert.That(stream.LastConsumedFrame, Is.EqualTo(2));
        }

        [Test]
        public void Two_remote_players_streams_never_mix()
        {
            var one = Primed();
            var two = Primed();
            one.Receive(new WireCommand(3, Vector2.right, CommandButtons.Light));
            two.Receive(new WireCommand(3, Vector2.left, CommandButtons.None));

            Assert.That(one.Next(12).WasPressed(CommandButtons.Light), Is.True);
            Assert.That(two.Next(12).Move, Is.EqualTo(Vector2.left));
            Assert.That(two.Next(13).Pressed, Is.EqualTo(CommandButtons.None));
        }

        [Test]
        public void Release_forgets_everything_so_a_rejoin_starts_from_frame_zero()
        {
            var stream = Primed();
            stream.Receive(new WireCommand(3, Vector2.right, CommandButtons.Light));
            stream.Next(12);

            stream.Release();
            Assert.That(stream.Next(13).Held, Is.EqualTo(CommandButtons.None));

            stream.Receive(new WireCommand(0, Vector2.up, CommandButtons.None));
            stream.Receive(new WireCommand(1, Vector2.up, CommandButtons.None));
            Assert.That(stream.Next(14).Move, Is.EqualTo(Vector2.up), "A restarted guest's frame 0 was refused as old.");
        }

        [Test]
        public void After_a_release_the_stream_primes_again_and_a_held_button_presses_anew()
        {
            var stream = Primed();
            stream.Receive(new WireCommand(3, Vector2.right, CommandButtons.Light));
            stream.Next(12);

            stream.Release();
            stream.Receive(new WireCommand(0, Vector2.up, CommandButtons.Light));
            Assert.That(stream.Next(13).Move, Is.EqualTo(Vector2.zero), "Played before the rejoined buffer was primed.");

            stream.Receive(new WireCommand(1, Vector2.up, CommandButtons.Light));
            Assert.That(stream.Next(14).WasPressed(CommandButtons.Light), Is.True,
                "A button held before the release was never pressed again.");
        }

        [Test]
        public void Catching_up_starts_past_the_documented_max_not_one_later()
        {
            var atMax = new RemoteCommandStream(target: 2, max: 6);
            var pastMax = new RemoteCommandStream(target: 2, max: 6);
            for (int frame = 1; frame <= 6; frame++)
            {
                atMax.Receive(new WireCommand(frame, Vector2.zero, CommandButtons.None));
            }

            for (int frame = 1; frame <= 7; frame++)
            {
                pastMax.Receive(new WireCommand(frame, Vector2.zero, CommandButtons.None));
            }

            atMax.Next(0);
            pastMax.Next(0);

            Assert.That(atMax.MergedSteps, Is.EqualTo(0), "Six deep is the max, not past it.");
            Assert.That(pastMax.MergedSteps, Is.EqualTo(1), "Seven deep is past the max and must catch up.");
            Assert.That(pastMax.LastConsumedFrame, Is.EqualTo(2));
        }

        [Test]
        public void A_buffer_left_deep_by_a_stall_drains_back_to_its_target()
        {
            var stream = new RemoteCommandStream(target: 2, max: 6);
            int sent = 0;
            while (sent < 6)
            {
                stream.Receive(new WireCommand(++sent, Vector2.zero, CommandButtons.None));
            }

            int steps = 1 + 4 * NetProtocol.InputBufferDrainSteps + 60;
            for (int step = 0; step < steps; step++)
            {
                stream.Receive(new WireCommand(++sent, Vector2.zero, CommandButtons.None));
                stream.Next(step);
            }

            stream.Receive(new WireCommand(++sent, Vector2.zero, CommandButtons.None));
            Assert.That(stream.Buffered, Is.EqualTo(2), "The lag a stall added never drained back to the target.");
            Assert.That(stream.MergedSteps, Is.EqualTo(5), "One catch-up past the max, then one merge per step of lag — and none once at the target.");
        }

        [Test]
        public void A_guest_silent_past_the_cap_lets_go_and_is_not_pressed_again_on_return()
        {
            var stream = new RemoteCommandStream(target: 1, max: 6);
            stream.Receive(new WireCommand(1, Vector2.right, CommandButtons.Heavy));
            stream.Next(0);

            for (int step = 1; step <= NetProtocol.StarvedRepeatSteps; step++)
            {
                PlayerCommand repeated = stream.Next(step);
                Assert.That(repeated.IsHeld(CommandButtons.Heavy), Is.True, "A short gap must repeat what was held.");
                Assert.That(repeated.Move, Is.EqualTo(Vector2.right));
            }

            PlayerCommand letGo = stream.Next(NetProtocol.StarvedRepeatSteps + 1);
            Assert.That(letGo.Move, Is.EqualTo(Vector2.zero), "A silent guest ran on with the last stick heard.");
            Assert.That(letGo.Held, Is.EqualTo(CommandButtons.None));
            Assert.That(letGo.WasReleased(CommandButtons.Heavy), Is.True);
            Assert.That(stream.Next(NetProtocol.StarvedRepeatSteps + 2).Released, Is.EqualTo(CommandButtons.None), "Let go twice.");

            stream.Receive(new WireCommand(2, Vector2.right, CommandButtons.Heavy | CommandButtons.Light));
            PlayerCommand back = stream.Next(NetProtocol.StarvedRepeatSteps + 3);
            Assert.That(back.WasPressed(CommandButtons.Heavy), Is.False, "Held through the silence: coming back is not a new press.");
            Assert.That(back.WasPressed(CommandButtons.Light), Is.True);
            Assert.That(back.Move, Is.EqualTo(Vector2.right));
        }

        [Test]
        public void Sampling_the_same_host_frame_again_consumes_nothing_and_presses_nothing()
        {
            var stream = Primed();
            stream.Receive(new WireCommand(3, Vector2.left, CommandButtons.Light));
            stream.Receive(new WireCommand(4, Vector2.left, CommandButtons.Light));
            Assert.That(stream.Next(12).WasPressed(CommandButtons.Light), Is.True);

            for (int i = 0; i < 5; i++)
            {
                PlayerCommand again = stream.Next(12);
                Assert.That(again.Pressed, Is.EqualTo(CommandButtons.None), "A second sample of one step pressed again.");
                Assert.That(again.IsHeld(CommandButtons.Light), Is.True);
                Assert.That(again.Move, Is.EqualTo(Vector2.left));
            }

            Assert.That(stream.LastConsumedFrame, Is.EqualTo(3), "A repeated sample consumed the next step's command.");
            Assert.That(stream.StarvedSteps, Is.EqualTo(0));
        }

        [Test]
        public void What_piled_up_while_the_host_was_paused_is_dropped_not_replayed()
        {
            var stream = new RemoteCommandStream(target: 2, max: 6);
            stream.Receive(new WireCommand(1, Vector2.zero, CommandButtons.None));
            stream.Receive(new WireCommand(2, Vector2.zero, CommandButtons.None));
            stream.Next(10);

            // Paused: the host samples one step over and over while the guest keeps sending.
            for (int frame = 3; frame <= 40; frame++)
            {
                stream.Receive(new WireCommand(frame, Vector2.left, CommandButtons.None));
                stream.Next(11);
            }

            stream.Next(12);

            Assert.That(stream.LastConsumedFrame, Is.EqualTo(39), "Input from the paused seconds was replayed.");
            Assert.That(stream.MergedSteps, Is.EqualTo(0));
        }

        /// <summary>A stream with frames 1–2 already played through, so the next command is live.</summary>
        private static RemoteCommandStream Primed()
        {
            var stream = new RemoteCommandStream(target: 2, max: 6);
            stream.Receive(new WireCommand(1, Vector2.zero, CommandButtons.None));
            stream.Receive(new WireCommand(2, Vector2.zero, CommandButtons.None));
            stream.Next(10);
            stream.Next(11);
            return stream;
        }
    }
}
