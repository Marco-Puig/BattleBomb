using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Combat;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using BattleBomb.Platform.Net;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// The guest's half of a match, in the Gameplay scene: every local step it sends this machine's
    /// newest commands to the host (with redundancy), advances the render clock, sets the world to the
    /// host's snapshots around it, and raises the host's events when the picture reaches them (D58).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetGuest : MonoBehaviour
    {
        private readonly NetWriter _writer = new NetWriter(256);
        private readonly List<WireCommand> _recent = new List<WireCommand>(NetProtocol.CommandRedundancy + 1);
        private readonly SnapshotBuffer _buffer = new SnapshotBuffer();
        private readonly RenderClock _clock = new RenderClock();
        private readonly List<ReplicatedEvent> _incoming = new List<ReplicatedEvent>();
        private readonly List<ReplicatedEvent> _pending = new List<ReplicatedEvent>();
        private readonly MenuGate _menu = new MenuGate();
        private RemotePlayerRequests _requests;
        private NetSession _net;
        private SimulationDriver _driver;
        private StageRunner _runner;
        private ReplicaWorld _world;
        private PlayerId _local;
        private int _handOverStage = -1;
        private int _handOverAt;

        /// <summary>The host frame the picture is showing right now, or -1 before the first snapshot.</summary>
        public float RenderFrame => _clock.Frame;

        public int NewestHostFrame => _buffer.NewestFrame;

        internal void Begin(NetSession net, SimulationDriver driver, PlayerId local)
        {
            _net = net;
            _driver = driver;
            _local = local;
            _runner = FindAnyObjectByType<StageRunner>();
            _world = new ReplicaWorld(_driver, _runner, FindAnyObjectByType<EnemySpawner>(), GameSession.Find());
            _requests = new RemotePlayerRequests(_net, () => _driver.InventoryOf(_local.Value));
            _driver.RequestRoute = id => id == _local.Value ? _requests : null;
            _driver.ReplicaStepping += OnLocalStep;
            _net.MessageReceived += OnMessage;
            if (_runner != null)
            {
                _runner.ReplicaStageReady += OnStageReady;
            }
        }

        private void Start() => _net.ReleaseHeld();

        private void OnLocalStep(int frame)
        {
            PlayerCommand command = CommandCodec.Quantized(_menu.Filter(_driver.CommandFor(_local.Value), _driver.MenuPauseHeld));
            _recent.Add(WireCommand.From(command));
            if (_recent.Count > NetProtocol.CommandRedundancy)
            {
                _recent.RemoveAt(0);
            }

            _writer.Reset();
            CommandCodec.Write(_writer, Mathf.Max(0, _buffer.NewestFrame), _recent);
            _net.Send(NetChannel.Unreliable, _writer);

            if (_buffer.Count == 0)
            {
                return;
            }

            float render = _clock.Advance(_buffer.NewestFrame);
            if (_buffer.TrySample(render, out WorldSnapshot from, out WorldSnapshot to, out float t))
            {
                _world.Apply(from, to, t, render);
            }

            RaiseDue(render);
            if (_handOverStage >= 0 && render >= _handOverAt)
            {
                _runner?.ReplicaHandOver(_handOverStage);
                _handOverStage = -1;
            }

            _buffer.DiscardBefore(render);
        }

        private void OnMessage(NetMessageKind kind, NetReader reader)
        {
            switch (kind)
            {
                case NetMessageKind.Snapshot:
                    WorldSnapshot snapshot = _buffer.Rent();
                    SnapshotCodec.Read(reader, snapshot);
                    _buffer.Add(snapshot);
                    break;

                case NetMessageKind.Events:
                    EventCodec.Read(reader, _driver.ItemSpecs, _incoming);
                    _pending.AddRange(_incoming);
                    break;

                case NetMessageKind.RequestResult:
                    RequestOutcome outcome = RequestCodec.ReadResult(reader, out int sequence);
                    _requests.Answer(sequence, outcome);
                    break;

                case NetMessageKind.LoadStage:
                    LoadStageMessage load = StageCodec.ReadLoad(reader);
                    if (_handOverStage >= 0)
                    {
                        // A load right behind a hand-over replaces the stage that hand-over is waiting on:
                        // take it now rather than lose it. A launch starts the run again, so it goes.
                        if (!load.IsLaunch)
                        {
                            _runner?.ReplicaHandOver(_handOverStage);
                        }

                        _handOverStage = -1;
                    }

                    _runner?.ReplicaLoad(load);
                    break;

                case NetMessageKind.HandOver:
                    // Applied when the picture reaches the host's step, not when it arrives: the guest draws
                    // that step about a tenth of a second later, and the snapshots around it may be lost.
                    _handOverStage = StageCodec.ReadHandOver(reader, out _handOverAt);
                    break;
            }
        }

        private void OnStageReady(int stage)
        {
            _writer.Reset();
            StageCodec.WriteReady(_writer, stage);
            _net.Send(NetChannel.Reliable, _writer);
        }

        /// <summary>Raises every event whose step the picture has reached, in the order they happened.</summary>
        private void RaiseDue(float render)
        {
            int due = 0;
            while (due < _pending.Count && _pending[due].HostFrame <= render)
            {
                ReplicatedEvent e = _pending[due];
                if (e.Kind == ReplicatedEventKind.Hit)
                {
                    Component target = Resolve(e.Hit.Target);
                    if (target != null)
                    {
                        _driver.RaiseReplicatedHit(new HitEvent(
                            Resolve(e.Hit.Attacker), target, e.Hit.Damage, e.Hit.Position,
                            e.Hit.IsPartner, e.Hit.IsCrit, e.Hit.IsDamageOverTime));
                    }
                }
                else if (e.Kind == ReplicatedEventKind.DropSpawned)
                {
                    _world.SpawnDrop(e.Drop);
                }
                else if (e.Kind == ReplicatedEventKind.DropRemoved)
                {
                    _world.RemoveDrop(e.Drop.NetId);
                }

                due++;
            }

            _pending.RemoveRange(0, due);
        }

        private Component Resolve(in EntityRef entity)
        {
            switch (entity.Kind)
            {
                case EntityKind.Player:
                    return _world.PlayerById(entity.Id);
                case EntityKind.Enemy:
                    return _world.EnemyByNetId(entity.Id);
                case EntityKind.Dummy:
                    return _runner != null ? _runner.ReplicaDummy(entity.DummyStage, entity.DummyProp) : null;
                default:
                    return null;
            }
        }

        private void OnDestroy()
        {
            if (_driver != null)
            {
                _driver.ReplicaStepping -= OnLocalStep;
                _driver.RequestRoute = null;
            }

            _requests?.Abandon();

            if (_runner != null)
            {
                _runner.ReplicaStageReady -= OnStageReady;
            }

            if (_net != null)
            {
                _net.MessageReceived -= OnMessage;
                _net.RestoreCouch();
            }
        }
    }
}
