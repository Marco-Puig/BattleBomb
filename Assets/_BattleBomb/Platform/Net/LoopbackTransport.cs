using System;
using System.Collections.Generic;
using BattleBomb.Core.Net;

namespace BattleBomb.Platform.Net
{
    /// <summary>
    /// Two transports joined in memory, for tests: what one sends, the other receives on its next
    /// <see cref="TryReceive"/>, in order and as a copy. No time passes unless a
    /// <see cref="LagSimulator"/> wraps it.
    /// </summary>
    public sealed class LoopbackTransport : INetTransport
    {
        /// <summary>How the guest sees the host, and how the host sees the guest.</summary>
        public static readonly NetPeer HostPeer = new NetPeer(1);
        public static readonly NetPeer GuestPeer = new NetPeer(2);

        private readonly Queue<NetEvent> _inbox = new Queue<NetEvent>();
        private readonly NetPeer _self;
        private LoopbackTransport _partner;
        private bool _listening;
        private bool _connected;

        private LoopbackTransport(NetPeer self)
        {
            _self = self;
        }

        public static void CreatePair(out LoopbackTransport host, out LoopbackTransport guest)
        {
            host = new LoopbackTransport(HostPeer);
            guest = new LoopbackTransport(GuestPeer);
            host._partner = guest;
            guest._partner = host;
        }

        public bool IsConnected => _connected;

        public void Listen() => _listening = true;

        public void Connect(string address)
        {
            if (_connected || !_partner._listening)
            {
                _inbox.Enqueue(NetEvent.Disconnected(_partner._self));
                return;
            }

            _connected = true;
            _partner._connected = true;
            _partner._inbox.Enqueue(NetEvent.Connected(_self));
            _inbox.Enqueue(NetEvent.Connected(_partner._self));
        }

        public void Send(NetPeer peer, NetChannel channel, byte[] payload, int length)
        {
            if (!_connected || peer != _partner._self)
            {
                return;
            }

            if (length > NetProtocol.MaxMessageBytes)
            {
                // The socket's receiver refuses a frame this long and drops the connection
                // (LocalSocketTransport.SplitFrames); under test it must fail exactly as it would there.
                Disconnect(peer);
                return;
            }

            var copy = new byte[length];
            Array.Copy(payload, copy, length);
            _partner._inbox.Enqueue(NetEvent.Data(_self, channel, copy));
        }

        public void Update(double nowSeconds)
        {
        }

        public bool TryReceive(out NetEvent netEvent)
        {
            if (_inbox.Count > 0)
            {
                netEvent = _inbox.Dequeue();
                return true;
            }

            netEvent = default;
            return false;
        }

        public void Disconnect(NetPeer peer)
        {
            if (!_connected || peer != _partner._self)
            {
                return;
            }

            _connected = false;
            _partner._connected = false;
            _partner._inbox.Enqueue(NetEvent.Disconnected(_self));
            _inbox.Enqueue(NetEvent.Disconnected(_partner._self));
        }

        public void Dispose() => Disconnect(_partner._self);
    }
}
