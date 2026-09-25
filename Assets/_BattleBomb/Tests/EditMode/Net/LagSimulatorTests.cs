using System.Collections.Generic;
using BattleBomb.Platform.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class LagSimulatorTests
    {
        [Test]
        public void A_message_arrives_half_a_round_trip_later()
        {
            Lagged(new LagProfile("Test", 100f, 0f, 0f), 1u, out LagSimulator sender, out LoopbackTransport receiver);

            sender.Update(0.0);
            sender.Send(LoopbackTransport.HostPeer, NetChannel.Reliable, new byte[] { 7 }, 1);

            sender.Update(0.049);
            Assert.That(LoopbackTransportTests.Drain(receiver), Is.Empty, "Delivered before half the round trip.");

            sender.Update(0.051);
            List<NetEvent> events = LoopbackTransportTests.Drain(receiver);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Payload, Is.EqualTo(new byte[] { 7 }));
        }

        [Test]
        public void Reliable_messages_keep_their_order_through_jitter()
        {
            Lagged(new LagProfile("Test", 100f, 80f, 0f), 42u, out LagSimulator sender, out LoopbackTransport receiver);

            for (int i = 0; i < 50; i++)
            {
                sender.Update(i * 0.001);
                sender.Send(LoopbackTransport.HostPeer, NetChannel.Reliable, new[] { (byte)i }, 1);
            }

            sender.Update(10.0);
            List<NetEvent> events = LoopbackTransportTests.Drain(receiver);
            Assert.That(events.Count, Is.EqualTo(50));
            for (int i = 0; i < 50; i++)
            {
                Assert.That(events[i].Payload[0], Is.EqualTo((byte)i), "Jitter reordered the reliable channel.");
            }
        }

        [Test]
        public void Loss_only_ever_touches_the_unreliable_channel()
        {
            Lagged(new LagProfile("Test", 0f, 0f, 1f), 3u, out LagSimulator sender, out LoopbackTransport receiver);

            sender.Update(0.0);
            sender.Send(LoopbackTransport.HostPeer, NetChannel.Unreliable, new byte[] { 1 }, 1);
            sender.Send(LoopbackTransport.HostPeer, NetChannel.Reliable, new byte[] { 2 }, 1);
            sender.Update(1.0);

            List<NetEvent> events = LoopbackTransportTests.Drain(receiver);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Payload, Is.EqualTo(new byte[] { 2 }));
        }

        [Test]
        public void The_same_seed_loses_the_same_packets()
        {
            Assert.That(Survivors(99u), Is.EqualTo(Survivors(99u)));
            Assert.That(Survivors(99u), Is.Not.EqualTo(Survivors(100u)));
        }

        [Test]
        public void The_bad_profile_is_the_one_the_handoff_names()
        {
            Assert.That(LagProfile.Bad.RoundTripMs, Is.EqualTo(200f));
            Assert.That(LagProfile.Bad.JitterMs, Is.EqualTo(30f));
            Assert.That(LagProfile.Bad.UnreliableLoss, Is.EqualTo(0.02f));
            Assert.That(LagProfile.Normal.RoundTripMs, Is.EqualTo(100f));
            Assert.That(LagProfile.None.IsNone, Is.True);
        }

        private static List<byte> Survivors(uint seed)
        {
            Lagged(new LagProfile("Test", 0f, 0f, 0.5f), seed, out LagSimulator sender, out LoopbackTransport receiver);
            sender.Update(0.0);
            for (int i = 0; i < 64; i++)
            {
                sender.Send(LoopbackTransport.HostPeer, NetChannel.Unreliable, new[] { (byte)i }, 1);
            }

            sender.Update(1.0);
            var survivors = new List<byte>();
            foreach (NetEvent netEvent in LoopbackTransportTests.Drain(receiver))
            {
                survivors.Add(netEvent.Payload[0]);
            }

            return survivors;
        }

        /// <summary>A guest whose sends go through the simulator, and the host that receives them.</summary>
        private static void Lagged(LagProfile profile, uint seed, out LagSimulator sender, out LoopbackTransport receiver)
        {
            LoopbackTransport.CreatePair(out LoopbackTransport host, out LoopbackTransport guest);
            sender = new LagSimulator(guest, profile, seed);
            host.Listen();
            sender.Connect("loopback");
            LoopbackTransportTests.Drain(host);
            LoopbackTransportTests.Drain(sender);
            receiver = host;
        }
    }
}
