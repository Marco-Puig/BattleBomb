using System;

namespace BattleBomb.Platform.Net
{
    /// <summary>
    /// The other end of a connection, as the transport names it — a Steam ID under Steam, a small
    /// fixed number on the local transports. Opaque above the seam: nothing in Gameplay reads it
    /// as anything but "the peer" (D58 — nothing above the seam may assume Steam).
    /// </summary>
    public readonly struct NetPeer : IEquatable<NetPeer>
    {
        public readonly ulong Id;

        public NetPeer(ulong id)
        {
            Id = id;
        }

        public static NetPeer None => default;

        public bool IsNone => Id == 0;

        public bool Equals(NetPeer other) => Id == other.Id;

        public override bool Equals(object obj) => obj is NetPeer other && Equals(other);

        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(NetPeer a, NetPeer b) => a.Id == b.Id;

        public static bool operator !=(NetPeer a, NetPeer b) => a.Id != b.Id;

        public override string ToString() => $"peer {Id}";
    }
}
