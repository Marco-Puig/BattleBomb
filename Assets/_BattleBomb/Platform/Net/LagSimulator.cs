using System;
using System.Collections.Generic;
using BattleBomb.Core.Loot;

namespace BattleBomb.Platform.Net
{
    /// <summary>
    /// Wraps any transport and holds each outgoing message back by half a <see cref="LagProfile"/>
    /// round trip, give or take jitter, dropping some unreliable ones. Seeded, so a test that loses
    /// packets loses the same ones every run. Each machine wraps its own transport, so the two
    /// one-way delays add up to the profile's round trip.
    /// </summary>
    public sealed class LagSimulator : INetTransport
    {
        private readonly INetTransport _inner;
        private readonly List<Pending> _pending = new List<Pending>();
        private DeterministicRandom _random;
        private double _now;
        private double _lastReliableAt;
        private long _order;

        public LagSimulator(INetTransport inner, LagProfile profile, uint seed)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            Profile = profile;
            _random = new DeterministicRandom(seed);
        }

        public LagProfile Profile { get; }

        public INetTransport Inner => _inner;

        public void Listen() => _inner.Listen();

        public void Connect(string address) => _inner.Connect(address);

        public void Send(NetPeer peer, NetChannel channel, byte[] payload, int length)
        {
            if (Profile.IsNone)
            {
                _inner.Send(peer, channel, payload, length);
                return;
            }

            _random = _random.NextFloat(out float lossRoll);
            if (channel == NetChannel.Unreliable && lossRoll < Profile.UnreliableLoss)
            {
                return;
            }

            _random = _random.NextFloat(out float jitterRoll);
            double oneWay = (Profile.RoundTripMs * 0.5 + (jitterRoll - 0.5) * Profile.JitterMs) / 1000.0;
            double at = _now + Math.Max(0.0, oneWay);
            if (channel == NetChannel.Reliable)
            {
                at = Math.Max(at, _lastReliableAt);
                _lastReliableAt = at;
            }

            var copy = new byte[length];
            Array.Copy(payload, copy, length);
            var pending = new Pending(at, _order++, peer, channel, copy);

            int index = _pending.Count;
            while (index > 0 && _pending[index - 1].At > at)
            {
                index--;
            }

            _pending.Insert(index, pending);
        }

        public void Update(double nowSeconds)
        {
            _now = nowSeconds;
            int due = 0;
            while (due < _pending.Count && _pending[due].At <= nowSeconds)
            {
                Pending pending = _pending[due];
                _inner.Send(pending.Peer, pending.Channel, pending.Payload, pending.Payload.Length);
                due++;
            }

            _pending.RemoveRange(0, due);
            _inner.Update(nowSeconds);
        }

        public bool TryReceive(out NetEvent netEvent) => _inner.TryReceive(out netEvent);

        /// <summary>A disconnect drops whatever was still in flight, as a real one does.</summary>
        public void Disconnect(NetPeer peer)
        {
            _pending.Clear();
            _inner.Disconnect(peer);
        }

        public void Dispose()
        {
            _pending.Clear();
            _inner.Dispose();
        }

        private readonly struct Pending
        {
            public readonly double At;
            public readonly long Order;
            public readonly NetPeer Peer;
            public readonly NetChannel Channel;
            public readonly byte[] Payload;

            public Pending(double at, long order, NetPeer peer, NetChannel channel, byte[] payload)
            {
                At = at;
                Order = order;
                Peer = peer;
                Channel = channel;
                Payload = payload;
            }
        }
    }
}
