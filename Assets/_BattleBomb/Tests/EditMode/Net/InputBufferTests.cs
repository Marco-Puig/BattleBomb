using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class InputBufferTests
    {
        [Test]
        public void Out_of_order_arrivals_come_out_in_frame_order()
        {
            var buffer = new InputBuffer();
            buffer.Add(At(3));
            buffer.Add(At(1));
            buffer.Add(At(2));

            Assert.That(Take(buffer), Is.EqualTo(1));
            Assert.That(Take(buffer), Is.EqualTo(2));
            Assert.That(Take(buffer), Is.EqualTo(3));
        }

        [Test]
        public void A_redundant_copy_is_kept_once()
        {
            var buffer = new InputBuffer();
            Assert.That(buffer.Add(At(5)), Is.True);
            Assert.That(buffer.Add(At(5)), Is.False);
            Assert.That(buffer.Count, Is.EqualTo(1));
        }

        [Test]
        public void Overlapping_redundant_packets_keep_each_frame_once()
        {
            var buffer = new InputBuffer();
            for (int frame = 1; frame <= 4; frame++)
            {
                buffer.Add(At(frame));
            }

            for (int frame = 2; frame <= 5; frame++)
            {
                buffer.Add(At(frame));
            }

            Assert.That(buffer.Count, Is.EqualTo(5), "A copy that was not the newest queued frame was kept twice.");
            for (int frame = 1; frame <= 5; frame++)
            {
                Assert.That(Take(buffer), Is.EqualTo(frame));
            }
        }

        [Test]
        public void A_command_older_than_one_already_taken_is_ignored()
        {
            var buffer = new InputBuffer();
            buffer.Add(At(5));
            Take(buffer);

            Assert.That(buffer.Add(At(5)), Is.False);
            Assert.That(buffer.Add(At(4)), Is.False);
            Assert.That(buffer.LastTakenFrame, Is.EqualTo(5));
        }

        [Test]
        public void A_flood_is_bounded_by_dropping_the_oldest()
        {
            var buffer = new InputBuffer();
            for (int frame = 1; frame <= InputBuffer.MaxQueued + 10; frame++)
            {
                buffer.Add(At(frame));
            }

            Assert.That(buffer.Count, Is.EqualTo(InputBuffer.MaxQueued));
            Assert.That(Take(buffer), Is.EqualTo(11));
        }

        private static WireCommand At(int frame) => new WireCommand(frame, Vector2.zero, CommandButtons.None);

        private static int Take(InputBuffer buffer)
        {
            Assert.That(buffer.TryTake(out WireCommand command), Is.True);
            return command.Frame;
        }
    }
}
