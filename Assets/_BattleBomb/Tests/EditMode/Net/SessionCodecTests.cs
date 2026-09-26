using BattleBomb.Core.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>A moment in the host's run that the guest acts on too (D60/D61).</summary>
    public sealed class SessionCodecTests
    {
        [Test]
        public void Every_moment_round_trips()
        {
            foreach (MomentKind moment in new[] { MomentKind.ChapterCompleted, MomentKind.CheckpointReached, MomentKind.StageCompleted })
            {
                var writer = new NetWriter();
                SessionCodec.WriteMoment(writer, moment);
                var reader = new NetReader(writer.ToArray());
                Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.Moment));
                Assert.That(SessionCodec.ReadMoment(reader), Is.EqualTo(moment));
                Assert.That(reader.Remaining, Is.Zero);
            }
        }

        [Test]
        public void An_unknown_moment_is_refused_at_the_door()
        {
            var reader = new NetReader(new byte[] { 99 });
            Assert.Throws<NetFormatException>(() => SessionCodec.ReadMoment(reader));
        }
    }
}
