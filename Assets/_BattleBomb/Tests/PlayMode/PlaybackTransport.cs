using System.Collections.Generic;
using BattleBomb.Core.Net;
using BattleBomb.Platform.Net;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// A host that is a recording: it welcomes whoever connects, then plays back what a real host
    /// sent, each message on the step it originally went out, at 60 steps a second of real time.
    /// What the guest sends back is kept in <see cref="Sent"/> for a test to read; the recording
    /// itself cannot answer, and does not need to.
    /// </summary>
    internal sealed class PlaybackTransport : INetTransport
    {
        private static readonly NetPeer Host = new NetPeer(1);

        private readonly List<(int Frame, byte[] Payload)> _recording;
        private readonly Queue<NetEvent> _inbox = new Queue<NetEvent>();
        private readonly int _firstFrame;
        private double _start = -1.0;
        private int _next;
        private bool _connected;

        internal PlaybackTransport(List<(int Frame, byte[] Payload)> recording)
        {
            _recording = recording;
            _firstFrame = recording.Count > 0 ? recording[0].Frame : 0;
        }

        internal bool Finished => _next >= _recording.Count;

        /// <summary>Everything the guest sent, in order — the recording cannot answer, but a test can read.</summary>
        internal List<byte[]> Sent { get; } = new List<byte[]>();

        /// <summary>A message the test puts in as the host's, now — an answer a recording cannot know.</summary>
        internal void Deliver(byte[] payload) => _inbox.Enqueue(NetEvent.Data(Host, NetChannel.Reliable, payload));

        public void Listen()
        {
        }

        public void Connect(string address)
        {
            _connected = true;
            _inbox.Enqueue(NetEvent.Connected(Host));
            var writer = new NetWriter();
            HandshakeCodec.WriteWelcome(writer, new WelcomeMessage(1));
            _inbox.Enqueue(NetEvent.Data(Host, NetChannel.Reliable, writer.ToArray()));
        }

        public void Send(NetPeer peer, NetChannel channel, byte[] payload, int length)
        {
            var copy = new byte[length];
            System.Array.Copy(payload, copy, length);
            Sent.Add(copy);
        }

        public void Update(double nowSeconds)
        {
            if (!_connected)
            {
                return;
            }

            if (_start < 0.0)
            {
                _start = nowSeconds;
            }

            double frame = _firstFrame + (nowSeconds - _start) * 60.0;
            while (_next < _recording.Count && _recording[_next].Frame <= frame)
            {
                _inbox.Enqueue(NetEvent.Data(Host, NetChannel.Reliable, _recording[_next].Payload));
                _next++;
            }
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
            if (!_connected)
            {
                return;
            }

            _connected = false;
            _inbox.Enqueue(NetEvent.Disconnected(Host));
        }

        public void Dispose() => _connected = false;
    }
}
