using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BattleBomb.Core.Net;

namespace BattleBomb.Platform.Net
{
    /// <summary>
    /// TCP on localhost, for two editors on one machine under Multiplayer Play Mode (HANDOFF-M8
    /// planning decision 4). TCP because a development transport gets its reliable channel for
    /// free; the unreliable channel rides the same stream, and a <see cref="LagSimulator"/> supplies
    /// the loss a real one would have. Each message is framed as a 4-byte length, a channel byte,
    /// and the payload. One peer at a time: a host accepts the next one only once the last has gone.
    /// </summary>
    public sealed class LocalSocketTransport : INetTransport
    {
        private const int HeaderBytes = 5;

        /// <summary>A peer that stops reading (a paused editor) fills the socket's buffer, and a
        /// blocking write would then freeze this thread; past this it counts as a lost connection.</summary>
        private const int SendTimeoutMs = 1000;

        private static readonly NetPeer TheHost = new NetPeer(1);
        private static readonly NetPeer TheGuest = new NetPeer(2);

        private readonly int _port;
        private readonly Queue<NetEvent> _inbox = new Queue<NetEvent>();
        private readonly byte[] _header = new byte[HeaderBytes];
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private Task _connecting;
        private byte[] _received = new byte[64 * 1024];
        private int _receivedLength;

        public LocalSocketTransport(int port)
        {
            _port = port;
        }

        /// <summary>The port actually listened on — the OS's choice when constructed with 0.</summary>
        public int BoundPort => _listener != null ? ((IPEndPoint)_listener.LocalEndpoint).Port : _port;

        /// <summary>Who is on the other end right now, or <see cref="NetPeer.None"/>.</summary>
        public NetPeer Peer { get; private set; }

        public void Listen()
        {
            // Kept only once started: a busy port throws here, and a listener that never started must
            // not be left behind for every Update to call Pending on.
            var listener = new TcpListener(IPAddress.Loopback, _port);
            try
            {
                listener.Start(1);
            }
            catch
            {
                listener.Stop();
                throw;
            }

            _listener = listener;
        }

        public void Connect(string address)
        {
            int colon = address != null ? address.LastIndexOf(':') : -1;
            if (colon <= 0 || !int.TryParse(address.Substring(colon + 1), out int port))
            {
                throw new ArgumentException($"'{address}' is not host:port.", nameof(address));
            }

            _client = new TcpClient { NoDelay = true, SendTimeout = SendTimeoutMs };
            _connecting = _client.ConnectAsync(address.Substring(0, colon), port);
        }

        public void Update(double nowSeconds)
        {
            if (_listener != null && _listener.Pending())
            {
                if (_stream != null)
                {
                    // One peer at a time: anyone else is turned away now, not left in the backlog to
                    // surface as a phantom join once the first peer leaves.
                    _listener.AcceptTcpClient().Close();
                }
                else
                {
                    _client = _listener.AcceptTcpClient();
                    _client.NoDelay = true;
                    _client.SendTimeout = SendTimeoutMs;
                    _stream = _client.GetStream();
                    Peer = TheGuest;
                    _inbox.Enqueue(NetEvent.Connected(Peer));
                }
            }

            if (_connecting != null && _connecting.IsCompleted)
            {
                bool ok = !_connecting.IsFaulted && !_connecting.IsCanceled && _client != null && _client.Connected;
                _connecting = null;
                if (ok)
                {
                    _stream = _client.GetStream();
                    Peer = TheHost;
                    _inbox.Enqueue(NetEvent.Connected(Peer));
                }
                else
                {
                    Close();
                    _inbox.Enqueue(NetEvent.Disconnected(TheHost));
                }
            }

            if (_stream != null)
            {
                Pump();
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

        public void Send(NetPeer peer, NetChannel channel, byte[] payload, int length)
        {
            if (_stream == null || peer != Peer)
            {
                return;
            }

            _header[0] = (byte)length;
            _header[1] = (byte)(length >> 8);
            _header[2] = (byte)(length >> 16);
            _header[3] = (byte)(length >> 24);
            _header[4] = (byte)channel;
            try
            {
                _stream.Write(_header, 0, HeaderBytes);
                _stream.Write(payload, 0, length);
            }
            catch (Exception e) when (IsSocketFailure(e))
            {
                Lost();
            }
        }

        public void Disconnect(NetPeer peer)
        {
            if (_stream == null || peer != Peer)
            {
                return;
            }

            Lost();
        }

        public void Dispose()
        {
            Close();
            _listener?.Stop();
            _listener = null;
        }

        private void Pump()
        {
            try
            {
                while (_stream != null && _stream.DataAvailable)
                {
                    if (_receivedLength == _received.Length)
                    {
                        Array.Resize(ref _received, _received.Length * 2);
                    }

                    int read = _stream.Read(_received, _receivedLength, _received.Length - _receivedLength);
                    if (read <= 0)
                    {
                        Lost();
                        return;
                    }

                    _receivedLength += read;
                    SplitFrames();
                }

                if (_client != null && _client.Client.Poll(0, SelectMode.SelectRead) && _client.Client.Available == 0)
                {
                    Lost();
                }
            }
            catch (Exception e) when (IsSocketFailure(e))
            {
                Lost();
            }
        }

        private void SplitFrames()
        {
            int offset = 0;
            while (_stream != null && _receivedLength - offset >= HeaderBytes)
            {
                int length = _received[offset] | (_received[offset + 1] << 8)
                    | (_received[offset + 2] << 16) | (_received[offset + 3] << 24);
                if (length < 0 || length > NetProtocol.MaxMessageBytes)
                {
                    Lost();
                    return;
                }

                if (_receivedLength - offset - HeaderBytes < length)
                {
                    break;
                }

                var channel = (NetChannel)_received[offset + 4];
                var payload = new byte[length];
                Array.Copy(_received, offset + HeaderBytes, payload, 0, length);
                _inbox.Enqueue(NetEvent.Data(Peer, channel, payload));
                offset += HeaderBytes + length;
            }

            if (offset > 0 && _stream != null)
            {
                Array.Copy(_received, offset, _received, 0, _receivedLength - offset);
                _receivedLength -= offset;
            }
        }

        private void Lost()
        {
            NetPeer lost = Peer;
            Close();
            _inbox.Enqueue(NetEvent.Disconnected(lost));
        }

        private void Close()
        {
            _stream?.Dispose();
            _client?.Close();
            _stream = null;
            _client = null;
            _connecting = null;
            _receivedLength = 0;
            Peer = NetPeer.None;
        }

        private static bool IsSocketFailure(Exception e) =>
            e is IOException || e is SocketException || e is ObjectDisposedException || e is InvalidOperationException;
    }
}
