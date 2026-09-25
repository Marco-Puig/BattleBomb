using System;
using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Net;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Loot;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using BattleBomb.Platform.Net;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// The host's half of a match, in the Gameplay scene: tells the guest which run to load, feeds the
    /// guest's commands into their <see cref="RemoteCommandSource"/>, and sends the world back — a
    /// snapshot every second step and the step's events on the reliable channel (D58). Added by the
    /// binder when the machine boots as a host with a guest connected. It reads the simulation after
    /// each step and never writes to it, apart from the Plan 1 screen guard.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetHost : MonoBehaviour
    {
        private readonly NetWriter _writer = new NetWriter(4096);
        private readonly NetWriter _scratch = new NetWriter(4096);
        private readonly List<WireCommand> _commands = new List<WireCommand>(NetProtocol.CommandRedundancy);
        private readonly List<ReplicatedEvent> _pending = new List<ReplicatedEvent>();
        private readonly List<ReplicatedEvent> _stamped = new List<ReplicatedEvent>();
        private readonly WorldSnapshot _snapshot = new WorldSnapshot();
        private readonly List<DropPickup> _drops = new List<DropPickup>();
        private NetSession _net;
        private GameSession _session;
        private SimulationDriver _driver;
        private StageRunner _runner;
        private RemoteCommandSource _remote;
        private Action<NetWriter> _sendReliable;

        internal void Begin(
            NetSession net, GameSession session, SimulationDriver driver, StageRunner runner, RemoteCommandSource remote)
        {
            _net = net;
            _session = session;
            _driver = driver;
            _runner = runner;
            _remote = remote;
            _sendReliable = w => _net.Send(NetChannel.Reliable, w);
            _net.MessageReceived += OnMessage;
            _net.PeerLeft += OnPeerLeft;
            _driver.Stepped += OnStepped;
            _driver.HitLanded += OnHit;
            _driver.PickupSpawned += OnPickup;
            _driver.PickupRemoved += OnPickupRemoved;
            _driver.MayOpenScreen = id => id != _net.GuestPlayerId.Value;
            SendLaunch();
        }

        private void SendLaunch()
        {
            var picks = new int[_session.Characters.Length];
            for (int i = 0; i < picks.Length; i++)
            {
                // The guest's seat is the host's own hero until the lobby (SessionBinder's stand-in).
                picks[i] = IndexInRoster(_session.Characters[i == _net.GuestPlayerId.Value ? 0 : i]);
            }

            _writer.Reset();
            HandshakeCodec.WriteLaunch(_writer, new LaunchMessage(
                _session.Chapter != null ? _session.Chapter.Id : string.Empty,
                _session.StageIndex, _session.TierIndex, _session.ResumeCheckpointArena,
                picks, _net.GuestPlayerId.Value));
            _net.Send(NetChannel.Reliable, _writer);
        }

        private int IndexInRoster(CharacterDefinition definition)
        {
            if (definition == null)
            {
                return -1;
            }

            for (int i = 0; i < _session.Roster.Length; i++)
            {
                if (_session.Roster[i] == definition)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnMessage(NetMessageKind kind, NetReader reader)
        {
            if (kind != NetMessageKind.Commands || _remote == null)
            {
                return;
            }

            CommandCodec.Read(reader, _commands);
            for (int i = 0; i < _commands.Count; i++)
            {
                // The fight's verbs only: the guest's Start and menu buttons are for the guest's own
                // screens, never the host's (NetProtocol.RemoteVerbs).
                WireCommand command = _commands[i];
                _remote.Stream.Receive(new WireCommand(command.Frame, command.Move, command.Held & NetProtocol.RemoteVerbs));
            }
        }

        private void OnHit(HitEvent hit) =>
            _pending.Add(ReplicatedEvent.OfHit(new HitRecord(
                RefOf(hit.Attacker), RefOf(hit.Target), hit.Damage, hit.Position,
                hit.IsPartner, hit.IsCrit, hit.IsDamageOverTime)));

        private void OnPickup(DropPickup pickup) =>
            _pending.Add(ReplicatedEvent.OfDrop(new DropRecord(pickup.NetId, pickup.Position, pickup.Item)));

        private void OnPickupRemoved(DropPickup pickup)
        {
            // A swept or wiped drop is already destroyed: read its id past Unity's null.
            if (!(pickup is null))
            {
                _pending.Add(ReplicatedEvent.OfDropRemoved(pickup.NetId));
            }
        }

        /// <summary>After every host step: this step's events, then — every second step — the world.</summary>
        private void OnStepped(int frame)
        {
            if (!_net.IsConnected)
            {
                _pending.Clear();
                return;
            }

            if (_pending.Count > 0)
            {
                _stamped.Clear();
                for (int i = 0; i < _pending.Count; i++)
                {
                    _stamped.Add(_pending[i].At(frame));
                }

                _pending.Clear();
                EventCodec.WriteBatches(_stamped, _writer, _scratch, _sendReliable);
            }

            if (frame % NetProtocol.SnapshotEverySteps != 0)
            {
                return;
            }

            Capture(frame);
            _writer.Reset();
            SnapshotCodec.Write(_writer, _snapshot);
            _net.Send(NetChannel.Unreliable, _writer);
        }

        /// <summary>The world as the guest needs it, capped at what one snapshot may carry: past a bound the
        /// rest are left out of this snapshot rather than the host throwing inside its own step.</summary>
        private void Capture(int frame)
        {
            _snapshot.Clear();
            _snapshot.HostFrame = frame;
            _snapshot.AckGuestFrame = _remote != null ? _remote.Stream.LastConsumedFrame : -1;
            _snapshot.Bounds = _driver.Bounds;
            _snapshot.AttemptStepsAllDown = _driver.AttemptStepsAllDown;

            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count && _snapshot.Players.Count < SnapshotCodec.MaxPlayers; i++)
            {
                int id = actors[i].PlayerId.Value;
                int open = _driver.TryGetOpenScreen(id, out InteractionKind kind) ? (int)kind : -1;
                _snapshot.Players.Add(actors[i].CaptureReplica(open, _driver.GrabCountFor(id), _driver.RefusedStepsFor(id)));
            }

            int stage = _runner != null ? _runner.StageIndex : -1;
            IReadOnlyList<ISimTarget> targets = _driver.Targets.Ordered;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] is EnemyActor enemy && enemy.IsConfigured)
                {
                    if (_snapshot.Enemies.Count < NetProtocol.MaxEntities)
                    {
                        _snapshot.Enemies.Add(enemy.CaptureReplica());
                    }
                }
                else if (targets[i] is TrainingDummy dummy && dummy.PropIndex >= 0)
                {
                    if (_snapshot.Dummies.Count < NetProtocol.MaxEntities)
                    {
                        _snapshot.Dummies.Add(dummy.CaptureReplica(stage));
                    }
                }
            }

            IReadOnlyList<ProjectileState> bolts = _driver.Projectiles;
            for (int i = 0; i < bolts.Count && _snapshot.Projectiles.Count < NetProtocol.MaxEntities; i++)
            {
                _snapshot.Projectiles.Add(bolts[i]);
            }

            CaptureDrops();
        }

        /// <summary>Every drop's id — or, past what one snapshot may name, the ones nearest the players: a
        /// guest must never lose the drop at their feet to one left three stages back.</summary>
        private void CaptureDrops()
        {
            _drops.Clear();
            IReadOnlyList<DropPickup> pickups = _driver.Pickups;
            for (int i = 0; i < pickups.Count; i++)
            {
                if (pickups[i] != null)
                {
                    _drops.Add(pickups[i]);
                }
            }

            if (_drops.Count > NetProtocol.MaxEntities)
            {
                _drops.Sort(NearestFirst);
                _drops.RemoveRange(NetProtocol.MaxEntities, _drops.Count - NetProtocol.MaxEntities);
            }

            for (int i = 0; i < _drops.Count; i++)
            {
                _snapshot.DropIds.Add(_drops[i].NetId);
            }
        }

        private int NearestFirst(DropPickup a, DropPickup b) =>
            DistanceToNearestPlayer(a).CompareTo(DistanceToNearestPlayer(b));

        private float DistanceToNearestPlayer(DropPickup drop)
        {
            float nearest = float.MaxValue;
            for (int i = 0; i < _snapshot.Players.Count; i++)
            {
                nearest = Mathf.Min(nearest, (_snapshot.Players[i].Motor.Position - drop.Position).sqrMagnitude);
            }

            return nearest;
        }

        private EntityRef RefOf(Component component)
        {
            switch (component)
            {
                case CharacterActor player:
                    return EntityRef.Player(player.PlayerId.Value);
                case EnemyActor enemy:
                    return EntityRef.Enemy(enemy.NetId);
                case TrainingDummy dummy:
                    return EntityRef.Dummy(_runner != null ? _runner.StageIndex : -1, dummy.PropIndex);
                default:
                    return EntityRef.None;
            }
        }

        /// <summary>The guest is gone: their body stops taking orders rather than running on with the
        /// last stick it heard. What happens to it next is Plan 2's (D61 — the host carries on solo).</summary>
        private void OnPeerLeft()
        {
            if (_remote != null)
            {
                _remote.Stream.Release();
            }
        }

        private void OnDestroy()
        {
            if (_driver != null)
            {
                _driver.Stepped -= OnStepped;
                _driver.HitLanded -= OnHit;
                _driver.PickupSpawned -= OnPickup;
                _driver.PickupRemoved -= OnPickupRemoved;
                _driver.MayOpenScreen = null;
            }

            if (_net == null)
            {
                return;
            }

            _net.MessageReceived -= OnMessage;
            _net.PeerLeft -= OnPeerLeft;

            // The machine is being torn down — results, or return to chapter select. The guest's copy
            // goes back to the front door with it and stays connected for the next launch.
            if (_net.IsConnected)
            {
                _writer.Reset();
                HandshakeCodec.WriteBare(_writer, NetMessageKind.SessionEnd);
                _net.Send(NetChannel.Reliable, _writer);
            }
        }
    }
}
