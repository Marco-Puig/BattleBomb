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

        [Test]
        public void A_stage_says_whether_it_was_placed()
        {
            Assert.That(new LoadStageMessage(0, true, 0f, -1).Placed, Is.False, "A launch stage stands as authored.");
            Assert.That(new LoadStageMessage(1, false, 118.5f, -1).Placed, Is.True, "A stage behind the airlock is always placed.");

            // A late guest's launch stage is wherever the host's airlocks slid it (Task 103).
            var writer = new NetWriter();
            StageCodec.WriteLoad(writer, new LoadStageMessage(1, true, 118.5f, 2, placed: true));
            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            LoadStageMessage back = StageCodec.ReadLoad(reader);

            Assert.That(back.IsLaunch, Is.True);
            Assert.That(back.Placed, Is.True);
            Assert.That(back.FirstArenaMinX, Is.EqualTo(118.5f));
            Assert.That(back.ResumeCheckpointArena, Is.EqualTo(2));
            Assert.That(reader.Remaining, Is.Zero);
        }
    }
}
