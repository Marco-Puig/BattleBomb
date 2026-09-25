using System;

namespace BattleBomb.Platform.Net
{
    public enum NetEventKind
    {
        Connected = 0,
        Disconnected = 1,
        Data = 2,
    }

    /// <summary>One thing a transport received. <see cref="Payload"/> is the receiver's own copy.</summary>
    public readonly struct NetEvent : IEquatable<NetEvent>
    {
        public readonly NetEventKind Kind;
        public readonly NetPeer Peer;
        public readonly NetChannel Channel;
        public readonly byte[] Payload;

        private NetEvent(NetEventKind kind, NetPeer peer, NetChannel channel, byte[] payload)
        {
            Kind = kind;
            Peer = peer;
            Channel = channel;
            Payload = payload;
        }

        public static NetEvent Connected(NetPeer peer) =>
            new NetEvent(NetEventKind.Connected, peer, NetChannel.Reliable, null);

        public static NetEvent Disconnected(NetPeer peer) =>
            new NetEvent(NetEventKind.Disconnected, peer, NetChannel.Reliable, null);

        public static NetEvent Data(NetPeer peer, NetChannel channel, byte[] payload) =>
            new NetEvent(NetEventKind.Data, peer, channel, payload);

        public bool Equals(NetEvent other) =>
            Kind == other.Kind && Peer == other.Peer && Channel == other.Channel && Payload == other.Payload;

        public override bool Equals(object obj) => obj is NetEvent other && Equals(other);

        public override int GetHashCode() => ((int)Kind * 397) ^ Peer.GetHashCode();

        public override string ToString() => $"{Kind} {Peer} {Channel} {(Payload != null ? Payload.Length : 0)}B";
    }
}
