using System;
using System.Collections.Generic;
using System.Net.Sockets;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Session;
using BattleBomb.Platform.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// The connection, owned by the object that survives every scene (HANDOFF-M8 planning decision
    /// 5): the role, the transport, the peer, the handshake, keep-alives and the drop timer. Runs
    /// ahead of the driver each frame so a guest's commands are buffered before the host samples
    /// them. It moves bytes and scenes; it decides nothing about the game — that is
    /// <see cref="NetHost"/> and <see cref="NetGuest"/>, which live in the Gameplay scene.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    [DisallowMultipleComponent]
    public sealed class NetSession : MonoBehaviour
    {
        public const string GameplayScene = "Gameplay";
        public const string FrontendScene = "Frontend";

        private readonly NetWriter _writer = new NetWriter(512);
        private INetTransport _transport;
        private double _lastReceived;
        private double _lastSent;
        private bool _welcomed;
        private readonly System.Collections.Generic.List<byte[]> _held = new System.Collections.Generic.List<byte[]>();
        private bool _holding;

        /// <summary>This machine's own couch, held while the host's launch fills the session's seats
        /// and put back when the match ends: the session outlives the match, and the front door would
        /// otherwise seat the host's heroes here as local players.</summary>
        private CharacterDefinition[] _couch;

        public NetRole Role { get; private set; }

        public NetPeer Peer { get; private set; }

        /// <summary>The handshake is done: the host has welcomed a guest, or the guest was welcomed.</summary>
        public bool IsConnected => Role != NetRole.Offline && _welcomed && !Peer.IsNone;

        /// <summary>The guest's seat. Fixed while D11 caps the game at two: the host is Player 1.</summary>
        public PlayerId GuestPlayerId => PlayerId.Two;

        /// <summary>One line for the development panel.</summary>
        public string Status { get; private set; } = "Offline";

        public string LastRefusal { get; private set; }

        /// <summary>Nothing heard for a second — the "connection problem" banner's condition.</summary>
        public bool HasProblem => IsConnected && Now - _lastReceived > NetProtocol.ProblemAfterSeconds;

        /// <summary>Every message past the handshake, still positioned after its kind byte.</summary>
        public event Action<NetMessageKind, NetReader> MessageReceived;

        public event Action PeerJoined;

        public event Action PeerLeft;

        private static double Now => Time.unscaledTimeAsDouble;

        public static NetRole RoleOf(GameSession session)
        {
            NetSession net = session != null ? session.Net : null;
            return net != null ? net.Role : NetRole.Offline;
        }

        public static NetSession FindOrCreate()
        {
            GameSession session = GameSession.FindOrCreate();
            NetSession net = session.Net;
            return net != null ? net : session.gameObject.AddComponent<NetSession>();
        }

        public void Host(INetTransport transport)
        {
            Close();
            if (transport == null)
            {
                throw new ArgumentNullException(nameof(transport));
            }

            try
            {
                transport.Listen();
            }
            catch (SocketException)
            {
                // Another copy is already hosting here. The role is committed only once something is
                // listening, so this machine stays offline and the panel says why.
                transport.Dispose();
                Status = $"Port {NetProtocol.DevPort} is busy";
                return;
            }

            _transport = transport;
            Role = NetRole.Host;
            Status = "Hosting — waiting for a guest";
        }

        public void Join(INetTransport transport, string address)
        {
            Close();
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            Role = NetRole.Guest;
            Status = $"Joining {address}";
            _transport.Connect(address);
        }

        /// <summary>The lag profiles the development panel cycles through, by index.</summary>
        public static readonly IReadOnlyList<string> LocalLagNames = Array.AsReadOnly(new[] { "None", "Normal", "Bad" });

        /// <summary>Development: host on this machine's local socket (two editors, HANDOFF-M8 Task 90).</summary>
        public void HostLocal(int lagIndex) =>
            Host(WrapLocal(new LocalSocketTransport(NetProtocol.DevPort), lagIndex));

        /// <summary>Development: join a host on this machine's local socket.</summary>
        public void JoinLocal(int lagIndex) =>
            Join(WrapLocal(new LocalSocketTransport(0), lagIndex), $"127.0.0.1:{NetProtocol.DevPort}");

        private static INetTransport WrapLocal(INetTransport transport, int lagIndex)
        {
            LagProfile profile = lagIndex == 1 ? LagProfile.Normal : lagIndex == 2 ? LagProfile.Bad : LagProfile.None;
            return profile.IsNone ? transport : new LagSimulator(transport, profile, 1u);
        }

        /// <summary>A clean leave: the other side hears it at once rather than waiting out the drop timer.</summary>
        public void Leave()
        {
            if (!Peer.IsNone)
            {
                _writer.Reset();
                HandshakeCodec.WriteBare(_writer, NetMessageKind.Bye);
                Send(NetChannel.Reliable, _writer);
            }

            Close();
        }

        public void Send(NetChannel channel, NetWriter writer)
        {
            if (_transport == null || Peer.IsNone)
            {
                return;
            }

            _transport.Send(Peer, channel, writer.Buffer, writer.Length);
            _lastSent = Now;
        }

        private void Update()
        {
            if (_transport == null)
            {
                return;
            }

            _transport.Update(Now);
            while (_transport != null && _transport.TryReceive(out NetEvent netEvent))
            {
                Handle(netEvent);
            }

            if (_transport == null || Peer.IsNone)
            {
                return;
            }

            if (Now - _lastReceived > NetProtocol.DropAfterSeconds)
            {
                Status = "The connection went silent.";
                _transport.Disconnect(Peer);
                return;
            }

            if (_welcomed && Now - _lastSent > NetProtocol.KeepAliveSeconds)
            {
                _writer.Reset();
                HandshakeCodec.WriteBare(_writer, NetMessageKind.KeepAlive);
                Send(NetChannel.Unreliable, _writer);
            }
        }

        private void Handle(in NetEvent netEvent)
        {
            switch (netEvent.Kind)
            {
                case NetEventKind.Connected:
                    if (!Peer.IsNone)
                    {
                        // D11: one guest. A second connection is turned away at the door.
                        _transport.Disconnect(netEvent.Peer);
                        return;
                    }

                    Peer = netEvent.Peer;
                    _lastReceived = Now;
                    if (Role == NetRole.Guest)
                    {
                        _writer.Reset();
                        HandshakeCodec.WriteHello(_writer, new HelloMessage(NetProtocol.Version, Application.version));
                        Send(NetChannel.Reliable, _writer);
                        Status = "Saying hello";
                    }

                    break;

                case NetEventKind.Disconnected:
                    if (!Peer.IsNone && netEvent.Peer != Peer)
                    {
                        return;
                    }

                    Lost("left");
                    break;

                case NetEventKind.Data:
                    if (netEvent.Peer != Peer)
                    {
                        return;
                    }

                    _lastReceived = Now;
                    Dispatch(netEvent.Payload);
                    break;
            }
        }

        private void Dispatch(byte[] payload)
        {
            var reader = new NetReader(payload);
            try
            {
                var kind = (NetMessageKind)reader.ReadByte();
                switch (kind)
                {
                    case NetMessageKind.Hello when Role == NetRole.Host && !_welcomed:
                        Greet(HandshakeCodec.ReadHello(reader));
                        return;

                    case NetMessageKind.Welcome when Role == NetRole.Guest && !_welcomed:
                        HandshakeCodec.ReadWelcome(reader);
                        _welcomed = true;
                        Status = "Joined — waiting for the host to launch";
                        PeerJoined?.Invoke();
                        return;

                    case NetMessageKind.Refuse when Role == NetRole.Guest:
                        LastRefusal = HandshakeCodec.ReadRefuse(reader);
                        Close();
                        Status = $"Refused: {LastRefusal}";
                        return;

                    case NetMessageKind.KeepAlive:
                        return;

                    case NetMessageKind.Bye:
                        Lost("left");
                        return;

                    case NetMessageKind.Launch when Role == NetRole.Guest && _welcomed:
                        FollowLaunch(HandshakeCodec.ReadLaunch(reader));
                        return;

                    case NetMessageKind.SessionEnd when Role == NetRole.Guest && _welcomed:
                        ReturnToFrontend();
                        return;
                }

                if (!_welcomed)
                {
                    return;
                }

                if (_holding)
                {
                    _held.Add(payload);
                    return;
                }

                MessageReceived?.Invoke(kind, reader);
            }
            catch (NetFormatException e)
            {
                Debug.LogWarning($"{name}: a malformed message from {Peer} — {e.Message}. Dropping the connection.", this);
                _transport?.Disconnect(Peer);
            }
        }

        private void Greet(in HelloMessage hello)
        {
            string refusal = HandshakeCodec.CheckHello(hello, Application.version);
            if (refusal != null)
            {
                _writer.Reset();
                HandshakeCodec.WriteRefuse(_writer, refusal);
                Send(NetChannel.Reliable, _writer);
                Status = $"Refused a guest: {refusal}";
                _transport.Disconnect(Peer);
                return;
            }

            _welcomed = true;
            _writer.Reset();
            HandshakeCodec.WriteWelcome(_writer, new WelcomeMessage(GuestPlayerId.Value));
            Send(NetChannel.Reliable, _writer);
            Status = "A guest joined";
            PeerJoined?.Invoke();
        }

        /// <summary>The guest loads the run the host just launched. The chapter and heroes are found by
        /// id and roster index in what this machine's front door put on the session.</summary>
        private void FollowLaunch(in LaunchMessage launch)
        {
            GameSession session = GetComponent<GameSession>();
            ChapterDefinition chapter = null;
            for (int i = 0; i < session.Chapters.Length; i++)
            {
                if (session.Chapters[i] != null && session.Chapters[i].Id == launch.ChapterId)
                {
                    chapter = session.Chapters[i];
                }
            }

            if (chapter == null)
            {
                Debug.LogError($"{name}: the host launched chapter '{launch.ChapterId}', which this build does not have.", this);
                Leave();
                return;
            }

            if (_couch == null)
            {
                _couch = (CharacterDefinition[])session.Characters.Clone();
            }

            session.Chapter = chapter;
            session.StageIndex = launch.StageIndex;
            session.TierIndex = launch.TierIndex;
            session.ResumeCheckpointArena = launch.ResumeCheckpointArena;
            for (int i = 0; i < session.Characters.Length; i++)
            {
                int pick = i < launch.RosterPicks.Length ? launch.RosterPicks[i] : -1;
                session.Characters[i] = pick >= 0 && pick < session.Roster.Length ? session.Roster[pick] : null;
            }

            Status = "Playing as the guest";
            _held.Clear();
            _holding = true;
            SceneManager.LoadScene(GameplayScene, LoadSceneMode.Single);
        }

        /// <summary>The guest's match is over, however it ended.</summary>
        internal void RestoreCouch()
        {
            if (_couch == null)
            {
                return;
            }

            GameSession session = GetComponent<GameSession>();
            Array.Copy(_couch, session.Characters, Math.Min(_couch.Length, session.Characters.Length));
            _couch = null;
        }

        /// <summary>
        /// Plays back what arrived while the guest's machine was loading. Called by
        /// <see cref="NetGuest"/> in <c>Start</c> — after every object in the new scene has enabled, so
        /// the stage runner a <c>LoadStage</c> reaches is ready to take it.
        /// </summary>
        public void ReleaseHeld()
        {
            _holding = false;
            if (_held.Count == 0)
            {
                return;
            }

            byte[][] held = _held.ToArray();
            _held.Clear();
            for (int i = 0; i < held.Length; i++)
            {
                Dispatch(held[i]);
            }
        }

        private void ReturnToFrontend()
        {
            _held.Clear();
            _holding = false;
            RestoreCouch();
            if (SceneManager.GetActiveScene().name != FrontendScene)
            {
                SceneManager.LoadScene(FrontendScene, LoadSceneMode.Single);
            }
        }

        private void Lost(string why)
        {
            bool wasJoined = _welcomed;
            Peer = NetPeer.None;
            _welcomed = false;

            if (Role == NetRole.Guest)
            {
                Close();
                Status = $"The host {why}.";
                if (wasJoined)
                {
                    PeerLeft?.Invoke();
                    ReturnToFrontend();
                }

                return;
            }

            Status = $"Hosting — the guest {why}; waiting for another";
            if (wasJoined)
            {
                PeerLeft?.Invoke();
            }
        }

        private void Close()
        {
            _held.Clear();
            _holding = false;
            _transport?.Dispose();
            _transport = null;
            Role = NetRole.Offline;
            Peer = NetPeer.None;
            _welcomed = false;
            Status = "Offline";
        }

        private void OnDestroy() => Leave();
    }
}
