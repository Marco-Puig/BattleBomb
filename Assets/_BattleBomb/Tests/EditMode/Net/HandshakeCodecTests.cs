using BattleBomb.Core.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class HandshakeCodecTests
    {
        [Test]
        public void Hello_welcome_and_refuse_round_trip()
        {
            var writer = new NetWriter();
            HandshakeCodec.WriteHello(writer, new HelloMessage(NetProtocol.Version, "0.8.0"));
            HelloMessage hello = HandshakeCodec.ReadHello(Body(writer, NetMessageKind.Hello));
            Assert.That(hello.Protocol, Is.EqualTo(NetProtocol.Version));
            Assert.That(hello.BuildId, Is.EqualTo("0.8.0"));

            writer.Reset();
            HandshakeCodec.WriteWelcome(writer, new WelcomeMessage(1));
            Assert.That(HandshakeCodec.ReadWelcome(Body(writer, NetMessageKind.Welcome)).GuestPlayerId, Is.EqualTo(1));

            writer.Reset();
            HandshakeCodec.WriteRefuse(writer, "Different versions of the game.");
            Assert.That(HandshakeCodec.ReadRefuse(Body(writer, NetMessageKind.Refuse)), Is.EqualTo("Different versions of the game."));
        }

        [Test]
        public void A_launch_carries_both_seats_and_the_run()
        {
            var writer = new NetWriter();
            HandshakeCodec.WriteLaunch(writer, new LaunchMessage("fixture", 1, 2, 0, new[] { 0, 3 }, 1));
            LaunchMessage launch = HandshakeCodec.ReadLaunch(Body(writer, NetMessageKind.Launch));

            Assert.That(launch.ChapterId, Is.EqualTo("fixture"));
            Assert.That(launch.StageIndex, Is.EqualTo(1));
            Assert.That(launch.TierIndex, Is.EqualTo(2));
            Assert.That(launch.ResumeCheckpointArena, Is.EqualTo(0));
            Assert.That(launch.RosterPicks, Is.EqualTo(new[] { 0, 3 }));
            Assert.That(launch.GuestPlayerId, Is.EqualTo(1));
        }

        [Test]
        public void A_launch_with_more_seats_than_the_game_has_is_malformed()
        {
            var writer = new NetWriter();
            writer.WriteString("fixture");
            writer.WriteInt(0);
            writer.WriteInt(0);
            writer.WriteInt(-1);
            writer.WriteUShort(3);
            Assert.Throws<NetFormatException>(() => HandshakeCodec.ReadLaunch(new NetReader(writer.ToArray())));
        }

        [Test]
        public void A_hello_is_checked_for_protocol_and_build()
        {
            Assert.That(HandshakeCodec.CheckHello(new HelloMessage(NetProtocol.Version, "a"), "a"), Is.Null);
            StringAssert.Contains("protocol", HandshakeCodec.CheckHello(new HelloMessage(NetProtocol.Version + 1, "a"), "a"));
            StringAssert.Contains("version", HandshakeCodec.CheckHello(new HelloMessage(NetProtocol.Version, "b"), "a"));
        }

        private static NetReader Body(NetWriter writer, NetMessageKind expected)
        {
            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(expected));
            return reader;
        }
    }
}
