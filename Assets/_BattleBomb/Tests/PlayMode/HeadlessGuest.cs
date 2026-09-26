using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Core.Simulation;
using BattleBomb.Platform.Net;
using UnityEngine;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// A guest with no scene: it speaks the protocol over a transport, sends the stick and buttons a
    /// test sets at the simulation's rate, and keeps every message the host sends. Two Gameplay scenes
    /// cannot share one process — their wiring finds "the" driver — so the automated guest has none
    /// (HANDOFF-M8, Testing).
    /// </summary>
    internal sealed class HeadlessGuest : MonoBehaviour
    {
        private readonly NetWriter _writer = new NetWriter(256);
        private readonly List<WireCommand> _recent = new List<WireCommand>();
        private readonly SimulationClock _clock = new SimulationClock();
        private INetTransport _transport;
        private NetPeer _host;
        private CommandButtons _previous;
        private int _frame;

        internal bool IsWelcomed { get; private set; }

        internal LaunchMessage? Launch { get; private set; }

        internal bool SessionEnded { get; private set; }

        /// <summary>Every message the host sent after the handshake, in arrival order, with its kind byte.</summary>
        internal List<byte[]> Received { get; } = new List<byte[]>();

        internal int SentCommands { get; private set; }

        /// <summary>The newest host frame this guest has seen a snapshot for — what it acknowledges.</summary>
        internal int LatestHostFrame { get; set; }

        internal Vector2 Move { get; set; }

        internal CommandButtons Held { get; set; }

        /// <summary>False stops the command stream, as a frozen or hitching guest would.</summary>
        internal bool Sending { get; set; } = true;

        /// <summary>Answer every <c>LoadStage</c> with <c>StageReady</c> at once — a guest with no scene
        /// loads instantly. Off, the test decides when with <see cref="Ready"/>.</summary>
        internal bool AutoReady { get; set; } = true;

        internal List<int> LoadRequests { get; } = new List<int>();

        /// <summary>Every <c>LoadStage</c> in full — where the host asked for each stage to go.</summary>
        internal List<LoadStageMessage> LoadMessages { get; } = new List<LoadStageMessage>();

        /// <summary>Every answer the host sent to a request, in order.</summary>
        internal List<(int Sequence, RequestOutcome Outcome)> Results { get; } = new List<(int Sequence, RequestOutcome Outcome)>();

        internal void SendRequest(in PlayerRequest request)
        {
            _writer.Reset();
            RequestCodec.WriteRequest(_writer, request);
            _transport.Send(_host, NetChannel.Reliable, _writer.Buffer, _writer.Length);
        }

        /// <summary>Where the host's clock was when each message arrived — set by a recording test.</summary>
        internal System.Func<int> Clock { get; set; }

        /// <summary>Every message after the handshake, with the host step it arrived on, for playback.</summary>
        internal List<(int Frame, byte[] Payload)> Recorded { get; } = new List<(int Frame, byte[] Payload)>();

        internal void Ready(int stage)
        {
            _writer.Reset();
            StageCodec.WriteReady(_writer, stage);
            _transport.Send(_host, NetChannel.Reliable, _writer.Buffer, _writer.Length);
        }

        internal static HeadlessGuest Join(INetTransport transport)
        {
            var go = new GameObject("Headless Guest");
            DontDestroyOnLoad(go);
            var guest = go.AddComponent<HeadlessGuest>();
            guest._transport = transport;
            transport.Connect("loopback");
            return guest;
        }

        internal void Leave()
        {
            _writer.Reset();
            HandshakeCodec.WriteBare(_writer, NetMessageKind.Bye);
            _transport.Send(_host, NetChannel.Reliable, _writer.Buffer, _writer.Length);
            _transport.Disconnect(_host);
        }

        internal void SendRaw(NetChannel channel, NetWriter writer) =>
            _transport.Send(_host, channel, writer.Buffer, writer.Length);

        private void Update()
        {
            _transport.Update(Time.unscaledTimeAsDouble);
            while (_transport.TryReceive(out NetEvent netEvent))
            {
                Handle(netEvent);
            }

            if (!IsWelcomed || !Launch.HasValue)
            {
                return;
            }

            _clock.Accumulate(Time.deltaTime);
            while (_clock.TryConsumeStep(out _))
            {
                if (!Sending)
                {
                    continue;
                }

                var command = PlayerCommand.FromState(_frame++, Move, Held, _previous);
                _previous = Held;
                _recent.Add(WireCommand.From(command));
                if (_recent.Count > NetProtocol.CommandRedundancy)
                {
                    _recent.RemoveAt(0);
                }

                _writer.Reset();
                CommandCodec.Write(_writer, LatestHostFrame, _recent);
                _transport.Send(_host, NetChannel.Unreliable, _writer.Buffer, _writer.Length);
                SentCommands++;
            }
        }

        private void Handle(in NetEvent netEvent)
        {
            switch (netEvent.Kind)
            {
                case NetEventKind.Connected:
                    _host = netEvent.Peer;
                    _writer.Reset();
                    HandshakeCodec.WriteHello(_writer, new HelloMessage(NetProtocol.Version, Application.version));
                    _transport.Send(_host, NetChannel.Reliable, _writer.Buffer, _writer.Length);
                    break;

                case NetEventKind.Disconnected:
                    _host = NetPeer.None;
                    IsWelcomed = false;
                    break;

                case NetEventKind.Data:
                    if (IsWelcomed)
                    {
                        Recorded.Add((Clock != null ? Clock() : 0, netEvent.Payload));
                    }

                    var reader = new NetReader(netEvent.Payload);
                    var kind = (NetMessageKind)reader.ReadByte();
                    if (kind == NetMessageKind.Welcome)
                    {
                        IsWelcomed = true;
                    }
                    else if (kind == NetMessageKind.Launch)
                    {
                        Launch = HandshakeCodec.ReadLaunch(reader);
                        Received.Add(netEvent.Payload);
                    }
                    else if (kind == NetMessageKind.RequestResult)
                    {
                        RequestOutcome outcome = RequestCodec.ReadResult(reader, out int sequence);
                        Results.Add((sequence, outcome));
                        Received.Add(netEvent.Payload);
                    }
                    else if (kind == NetMessageKind.SessionEnd)
                    {
                        SessionEnded = true;
                        Received.Add(netEvent.Payload);
                    }
                    else if (kind == NetMessageKind.LoadStage)
                    {
                        LoadStageMessage load = StageCodec.ReadLoad(reader);
                        int stage = load.StageIndex;
                        LoadRequests.Add(stage);
                        LoadMessages.Add(load);
                        Received.Add(netEvent.Payload);
                        if (AutoReady)
                        {
                            Ready(stage);
                        }
                    }
                    else if (kind != NetMessageKind.KeepAlive)
                    {
                        Received.Add(netEvent.Payload);
                    }

                    break;
            }
        }

        private void OnDestroy() => _transport?.Dispose();
    }
}
