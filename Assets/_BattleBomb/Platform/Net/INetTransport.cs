using System;

namespace BattleBomb.Platform.Net
{
    /// <summary>
    /// Everything the game asks of a network (HANDOFF-M8 planning decision 3). A host listens for
    /// one peer; a guest connects to an address the transport understands. Time is handed in, never
    /// read, so a lag simulator and a test can drive it. Implementations: <see cref="LoopbackTransport"/>
    /// (tests), <see cref="LocalSocketTransport"/> (two editors), and Steam's (Plan 3).
    /// </summary>
    public interface INetTransport : IDisposable
    {
        void Listen();

        void Connect(string address);

        /// <summary>Sends <paramref name="length"/> bytes of <paramref name="payload"/>. The
        /// transport copies what it needs; the caller may reuse the buffer at once.</summary>
        void Send(NetPeer peer, NetChannel channel, byte[] payload, int length);

        /// <summary>Pumps the network. Call once per frame, before draining <see cref="TryReceive"/>.</summary>
        void Update(double nowSeconds);

        bool TryReceive(out NetEvent netEvent);

        void Disconnect(NetPeer peer);
    }
}
