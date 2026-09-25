using BattleBomb.Core.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class StageCodecTests
    {
        [Test]
        public void Load_ready_and_hand_over_round_trip()
        {
            var writer = new NetWriter();
            StageCodec.WriteLoad(writer, new LoadStageMessage(1, false, 118.5f, -1));
            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.LoadStage));
            LoadStageMessage load = StageCodec.ReadLoad(reader);
            Assert.That(load.StageIndex, Is.EqualTo(1));
            Assert.That(load.IsLaunch, Is.False);
            Assert.That(load.FirstArenaMinX, Is.EqualTo(118.5f));
            Assert.That(load.ResumeCheckpointArena, Is.EqualTo(-1));

            writer.Reset();
            StageCodec.WriteReady(writer, 1);
            reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.StageReady));
            Assert.That(StageCodec.ReadStage(reader), Is.EqualTo(1));

            writer.Reset();
            StageCodec.WriteHandOver(writer, 1, 4321);
            reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.HandOver));
            Assert.That(StageCodec.ReadHandOver(reader, out int hostFrame), Is.EqualTo(1));
            Assert.That(hostFrame, Is.EqualTo(4321));
        }
    }
}
