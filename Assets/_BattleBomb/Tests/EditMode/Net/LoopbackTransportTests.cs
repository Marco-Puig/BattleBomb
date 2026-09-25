using System.Collections.Generic;
using BattleBomb.Core.Net;
using BattleBomb.Platform.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class LoopbackTransportTests
    {
        [Test]
        public void Connecting_announces_each_side_to_the_other()
        {
            LoopbackTransport.CreatePair(out LoopbackTransport host, out LoopbackTransport guest);
            host.Listen();
            guest.Connect("loopback");

            Assert.That(Drain(host), Is.EqualTo(new[] { NetEvent.Connected(LoopbackTransport.GuestPeer) }));
            Assert.That(Drain(guest), Is.EqualTo(new[] { NetEvent.Connected(LoopbackTransport.HostPeer) }));
        }

        [Test]
        public void Connecting_to_nobody_listening_fails_as_a_disconnect()
        {
            LoopbackTransport.CreatePair(out LoopbackTransport host, out LoopbackTransport guest);
            guest.Connect("loopback");

            List<NetEvent> events = Drain(guest);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Kind, Is.EqualTo(NetEventKind.Disconnected));
            Assert.That(Drain(host), Is.Empty);
        }

        [Test]
        public void Messages_arrive_in_order_as_copies()
        {
            Connected(out LoopbackTransport host, out LoopbackTransport guest);
            var bytes = new byte[] { 1, 2, 3 };
            guest.Send(LoopbackTransport.HostPeer, NetChannel.Reliable, bytes, 3);
            bytes[0] = 9;
            guest.Send(LoopbackTransport.HostPeer, NetChannel.Unreliable, bytes, 2);

            List<NetEvent> events = Drain(host);
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0].Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(events[0].Channel, Is.EqualTo(NetChannel.Reliable));
            Assert.That(events[1].Payload, Is.EqualTo(new byte[] { 9, 2 }));
            Assert.That(events[1].Peer, Is.EqualTo(LoopbackTransport.GuestPeer));
        }

        [Test]
        public void Disconnecting_tells_both_sides_and_stops_delivery()
        {
            Connected(out LoopbackTransport host, out LoopbackTransport guest);
            host.Disconnect(LoopbackTransport.GuestPeer);
            guest.Send(LoopbackTransport.HostPeer, NetChannel.Reliable, new byte[] { 1 }, 1);

            Assert.That(Drain(host), Is.EqualTo(new[] { NetEvent.Disconnected(LoopbackTransport.GuestPeer) }));
            Assert.That(Drain(guest), Is.EqualTo(new[] { NetEvent.Disconnected(LoopbackTransport.HostPeer) }));
        }

        [Test]
        public void Disconnecting_a_peer_that_is_not_there_does_nothing_as_on_the_socket()
        {
            Connected(out LoopbackTransport host, out LoopbackTransport guest);
            host.Disconnect(NetPeer.None);

            Assert.That(Drain(host), Is.Empty);
            Assert.That(Drain(guest), Is.Empty);
            Assert.That(host.IsConnected, Is.True, "The socket ignores a peer it is not talking to; the loopback must too.");
        }

        [Test]
        public void A_frame_past_the_limit_drops_the_connection_as_the_socket_does()
        {
            Connected(out LoopbackTransport host, out LoopbackTransport guest);
            var big = new byte[NetProtocol.MaxMessageBytes + 1];
            guest.Send(LoopbackTransport.HostPeer, NetChannel.Reliable, big, big.Length);

            Assert.That(Drain(host), Is.EqualTo(new[] { NetEvent.Disconnected(LoopbackTransport.GuestPeer) }),
                "The socket refuses a frame past the limit and drops the connection; the loopback must too.");
            Assert.That(Drain(guest), Is.EqualTo(new[] { NetEvent.Disconnected(LoopbackTransport.HostPeer) }));
        }

        [Test]
        public void A_frame_exactly_at_the_limit_arrives()
        {
            Connected(out LoopbackTransport host, out LoopbackTransport guest);
            var max = new byte[NetProtocol.MaxMessageBytes];
            guest.Send(LoopbackTransport.HostPeer, NetChannel.Reliable, max, max.Length);

            List<NetEvent> events = Drain(host);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Payload.Length, Is.EqualTo(NetProtocol.MaxMessageBytes));
        }

        internal static void Connected(out LoopbackTransport host, out LoopbackTransport guest)
        {
            LoopbackTransport.CreatePair(out host, out guest);
            host.Listen();
            guest.Connect("loopback");
            Drain(host);
            Drain(guest);
        }

        internal static List<NetEvent> Drain(INetTransport transport)
        {
            var events = new List<NetEvent>();
            while (transport.TryReceive(out NetEvent netEvent))
            {
                events.Add(netEvent);
            }

            return events;
        }
    }
}
