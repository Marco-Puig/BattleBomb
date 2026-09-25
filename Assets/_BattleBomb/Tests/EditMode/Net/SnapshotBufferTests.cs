using BattleBomb.Core.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class SnapshotBufferTests
    {
        [Test]
        public void Snapshots_are_kept_in_frame_order_and_once()
        {
            var buffer = new SnapshotBuffer();
            Assert.That(buffer.Add(At(buffer, 20)), Is.True);
            Assert.That(buffer.Add(At(buffer, 10)), Is.True);
            Assert.That(buffer.Add(At(buffer, 20)), Is.False, "A duplicate was kept.");

            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer.NewestFrame, Is.EqualTo(20));
        }

        [Test]
        public void Sampling_between_two_snapshots_gives_the_pair_and_how_far_along()
        {
            var buffer = new SnapshotBuffer();
            buffer.Add(At(buffer, 10));
            buffer.Add(At(buffer, 12));
            buffer.Add(At(buffer, 14));

            Assert.That(buffer.TrySample(11f, out WorldSnapshot from, out WorldSnapshot to, out float t), Is.True);
            Assert.That(from.HostFrame, Is.EqualTo(10));
            Assert.That(to.HostFrame, Is.EqualTo(12));
            Assert.That(t, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void Sampling_outside_the_buffer_holds_its_edge_rather_than_guessing()
        {
            var buffer = new SnapshotBuffer();
            buffer.Add(At(buffer, 10));
            buffer.Add(At(buffer, 12));

            buffer.TrySample(4f, out WorldSnapshot from, out WorldSnapshot to, out float t);
            Assert.That(from.HostFrame, Is.EqualTo(10));
            Assert.That(to.HostFrame, Is.EqualTo(10));

            buffer.TrySample(30f, out from, out to, out t);
            Assert.That(from.HostFrame, Is.EqualTo(12));
            Assert.That(to.HostFrame, Is.EqualTo(12));
            Assert.That(t, Is.Zero);
        }

        [Test]
        public void Discarding_keeps_the_snapshot_the_picture_starts_from_and_recycles_the_rest()
        {
            var buffer = new SnapshotBuffer();
            WorldSnapshot first = At(buffer, 10);
            buffer.Add(first);
            buffer.Add(At(buffer, 12));
            buffer.Add(At(buffer, 14));

            buffer.DiscardBefore(13f);

            Assert.That(buffer.Count, Is.EqualTo(2));
            buffer.TrySample(13f, out WorldSnapshot from, out _, out _);
            Assert.That(from.HostFrame, Is.EqualTo(12));
            Assert.That(buffer.Rent(), Is.SameAs(first), "A discarded snapshot was not recycled.");
        }

        [Test]
        public void The_buffer_never_grows_past_its_capacity()
        {
            var buffer = new SnapshotBuffer();
            for (int frame = 0; frame < SnapshotBuffer.Capacity * 3; frame += 2)
            {
                buffer.Add(At(buffer, frame));
            }

            Assert.That(buffer.Count, Is.EqualTo(SnapshotBuffer.Capacity));
        }

        private static WorldSnapshot At(SnapshotBuffer buffer, int frame)
        {
            WorldSnapshot snapshot = buffer.Rent();
            snapshot.HostFrame = frame;
            return snapshot;
        }
    }
}
