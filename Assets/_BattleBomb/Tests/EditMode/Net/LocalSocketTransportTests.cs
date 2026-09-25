using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BattleBomb.Core.Net;
using BattleBomb.Platform.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>A real TCP pair on localhost. Port 0 lets the OS choose, so a busy 7777 cannot fail it.</summary>
    public sealed class LocalSocketTransportTests
    {
        [Test]
        public void Two_sockets_connect_exchange_and_part()
        {
            var host = new LocalSocketTransport(0);
            var guest = new LocalSocketTransport(0);
            try
            {
                host.Listen();
                guest.Connect($"127.0.0.1:{host.BoundPort}");

                // Both ends: the guest's connect can complete a pump after the host has accepted.
                List<NetEvent> hostEvents = PumpUntil(host, guest, h => h.Count >= 1 && !guest.Peer.IsNone,
                    out List<NetEvent> guestEvents);
                Assert.That(hostEvents[0].Kind, Is.EqualTo(NetEventKind.Connected));
                Assert.That(guestEvents.Exists(e => e.Kind == NetEventKind.Connected), Is.True);

                var big = new byte[70000];
                big[69999] = 42;
                guest.Send(guest.Peer, NetChannel.Reliable, new byte[] { 1, 2, 3 }, 3);
                guest.Send(guest.Peer, NetChannel.Unreliable, big, big.Length);

                hostEvents = PumpUntil(host, guest, h => h.Count >= 2, out _);
                Assert.That(hostEvents[0].Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
                Assert.That(hostEvents[1].Channel, Is.EqualTo(NetChannel.Unreliable));
                Assert.That(hostEvents[1].Payload.Length, Is.EqualTo(70000), "A frame split across reads was not reassembled.");
                Assert.That(hostEvents[1].Payload[69999], Is.EqualTo(42));

                guest.Disconnect(guest.Peer);
                hostEvents = PumpUntil(host, guest, h => h.Count >= 1, out _);
                Assert.That(hostEvents[0].Kind, Is.EqualTo(NetEventKind.Disconnected));
            }
            finally
            {
                guest.Dispose();
                host.Dispose();
            }
        }

        [Test]
        public void A_frame_past_the_limit_drops_the_connection()
        {
            var host = new LocalSocketTransport(0);
            var guest = new LocalSocketTransport(0);
            try
            {
                host.Listen();
                guest.Connect($"127.0.0.1:{host.BoundPort}");
                PumpUntil(host, guest, h => h.Count >= 1 && !guest.Peer.IsNone, out _);

                var big = new byte[NetProtocol.MaxMessageBytes + 1];
                guest.Send(guest.Peer, NetChannel.Reliable, big, big.Length);

                List<NetEvent> hostEvents = PumpUntil(host, guest,
                    h => h.Exists(e => e.Kind == NetEventKind.Disconnected), out _);
                Assert.That(hostEvents.Exists(e => e.Kind == NetEventKind.Data), Is.False,
                    "A frame past the limit was delivered.");
            }
            finally
            {
                guest.Dispose();
                host.Dispose();
            }
        }

        [Test]
        public void Listening_on_a_busy_port_throws_once_and_leaves_nothing_behind()
        {
            var squatter = new TcpListener(IPAddress.Loopback, 0);
            squatter.Start();
            var host = new LocalSocketTransport(((IPEndPoint)squatter.LocalEndpoint).Port);
            try
            {
                Assert.Throws<SocketException>(() => host.Listen());
                Assert.DoesNotThrow(() => host.Update(0.0),
                    "A listener that never started was left behind for every Update to trip over.");
            }
            finally
            {
                host.Dispose();
                squatter.Stop();
            }
        }

        private static List<NetEvent> PumpUntil(
            LocalSocketTransport host, LocalSocketTransport guest,
            System.Func<List<NetEvent>, bool> done, out List<NetEvent> guestEvents)
        {
            var hostEvents = new List<NetEvent>();
            guestEvents = new List<NetEvent>();
            Stopwatch clock = Stopwatch.StartNew();
            while (clock.Elapsed.TotalSeconds < 3.0)
            {
                host.Update(clock.Elapsed.TotalSeconds);
                guest.Update(clock.Elapsed.TotalSeconds);
                while (host.TryReceive(out NetEvent e))
                {
                    hostEvents.Add(e);
                }

                while (guest.TryReceive(out NetEvent e))
                {
                    guestEvents.Add(e);
                }

                if (done(hostEvents))
                {
                    return hostEvents;
                }

                Thread.Sleep(5);
            }

            Assert.Fail($"Timed out: the host saw {hostEvents.Count} event(s).");
            return hostEvents;
        }
    }
}
