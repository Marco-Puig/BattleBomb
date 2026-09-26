using System;
using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Saves;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Items;
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
    /// each step and never writes to it, apart from queueing the guest's menu requests for the step's
    /// first phase (Task 97) and, while a guest is connected, the launch hold and the airlock's wait for
    /// the guest (Task 94).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetHost : MonoBehaviour
    {
        private readonly NetWriter _writer = new NetWriter(4096);
        private readonly NetWriter _scratch = new NetWriter(4096);
        private readonly NetWriter _answer = new NetWriter(64);
        private readonly NetWriter _participant = new NetWriter(16 * 1024);
        private readonly List<(int Sequence, RequestOutcome Outcome)> _answers = new List<(int Sequence, RequestOutcome Outcome)>();
        private readonly Dictionary<int, Watched> _watched = new Dictionary<int, Watched>();
        private readonly List<int> _unwatch = new List<int>();

        /// <summary>A player whose inventory the guest keeps a copy of (Task 99).</summary>
        private sealed class Watched
        {
            internal CharacterActor Actor;
            internal PlayerInventory Bag;
            internal Action Handler;
            internal bool Dirty;
            internal bool Forced;
            internal int SentAt;
        }
        private readonly List<WireCommand> _commands = new List<WireCommand>(NetProtocol.CommandRedundancy);
        private readonly List<ReplicatedEvent> _pending = new List<ReplicatedEvent>();
        private readonly List<ReplicatedEvent> _stamped = new List<ReplicatedEvent>();
        private readonly WorldSnapshot _snapshot = new WorldSnapshot();
        private readonly HashSet<int> _guestReady = new HashSet<int>();
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
            _driver.RemoteRequestAnswered += OnRequestAnswered;
            _driver.ScreenChanged += OnScreenChanged;
            _driver.RackChanged += OnRackChanged;
            if (_runner != null)
            {
                _runner.StageLoadRequested += OnStageLoadRequested;
                _runner.StageHandedOver += OnStageHandedOver;
                _runner.RemoteStageReady = stage => !_net.IsConnected || _guestReady.Contains(stage);
            }

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
            if (kind == NetMessageKind.StageReady)
            {
                OnGuestStageReady(StageCodec.ReadStage(reader));
                return;
            }

            if (kind == NetMessageKind.Request)
            {
                // A guest can only ever act as itself, whatever id it wrote (planning decision 11). Run in
                // the next step's first phase, never here: this is the session's pump, outside the step.
                _driver.QueueRemoteRequest(RequestCodec.ReadRequest(reader).For(_net.GuestPlayerId.Value));
                return;
            }

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

        /// <summary>The answer waits for the end of the step: the guest must have the bag the request changed —
        /// and the rack it bought from — before it hears the answer (all three on the one ordered channel).</summary>
        private void OnRequestAnswered(PlayerRequest request, RequestOutcome outcome)
        {
            if (!_net.IsConnected || request.PlayerId != _net.GuestPlayerId.Value)
            {
                return;
            }

            _answers.Add((request.Sequence, outcome));
            Force(request.PlayerId);
        }

        private void OnScreenChanged(int playerId, InteractionKind kind, bool opened)
        {
            _pending.Add(ReplicatedEvent.OfScreen(playerId, (int)kind, opened));
            if (opened && playerId == _net.GuestPlayerId.Value)
            {
                // The guest's chest opens over the bag as it is now, not as it was a quarter-second ago.
                Force(playerId);
            }
        }

        private void OnRackChanged(int playerId)
        {
            IReadOnlyList<Core.Items.ItemInstance> rack = _driver.RackFor(playerId);
            var pieces = new Core.Items.ItemInstance[rack.Count];
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i] = rack[i];
            }

            _pending.Add(ReplicatedEvent.OfRack(playerId, pieces));
        }

        private void Force(int playerId)
        {
            if (_watched.TryGetValue(playerId, out Watched watched))
            {
                watched.Forced = true;
            }
        }

        /// <summary>Every player in the world gets a watcher the first step they are there — the binder brings the
        /// host up before any player object has enabled — and loses it when they leave.</summary>
        private void WatchParticipants()
        {
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                int id = actors[i].PlayerId.Value;
                if (_watched.ContainsKey(id))
                {
                    continue;
                }

                PlayerInventory bag = actors[i].GetComponent<PlayerInventory>();
                if (bag == null)
                {
                    continue;
                }

                var watched = new Watched { Actor = actors[i], Bag = bag, Dirty = true, Forced = true };
                watched.Handler = () => watched.Dirty = true;
                bag.Changed += watched.Handler;
                _watched[id] = watched;
            }

            _unwatch.Clear();
            foreach (KeyValuePair<int, Watched> entry in _watched)
            {
                if (entry.Value.Actor == null || !entry.Value.Actor.isActiveAndEnabled)
                {
                    _unwatch.Add(entry.Key);
                }
            }

            for (int i = 0; i < _unwatch.Count; i++)
            {
                Unwatch(_unwatch[i]);
            }
        }

        private void Unwatch(int playerId)
        {
            if (!_watched.TryGetValue(playerId, out Watched watched))
            {
                return;
            }

            if (watched.Bag != null)
            {
                watched.Bag.Changed -= watched.Handler;
            }

            _watched.Remove(playerId);
        }

        /// <summary>The guest's whole bag; the partner's worn gear only. Sent when forced, or when changed and not
        /// sent for <see cref="NetProtocol.ParticipantMinSteps"/>.</summary>
        private void FlushParticipants(int frame)
        {
            foreach (KeyValuePair<int, Watched> entry in _watched)
            {
                Watched watched = entry.Value;
                bool due = watched.Forced || (watched.Dirty && frame - watched.SentAt >= NetProtocol.ParticipantMinSteps);
                if (!due)
                {
                    continue;
                }

                SendParticipant(entry.Key, watched);
                watched.Dirty = false;
                watched.Forced = false;
                watched.SentAt = frame;
            }
        }

        private void SendParticipant(int playerId, Watched watched)
        {
            bool full = playerId == _net.GuestPlayerId.Value;
            PlayerInventory bag = watched.Bag;
            var character = new CharacterState(watched.Actor.Element, bag.Ledger, bag.Inventory);
            SaveGame state = SaveMapper.Participant(bag.Stash.Sack, bag.Wallet, character, full);
            _participant.Reset();
            ParticipantCodec.Write(_participant, playerId, bag.Inventory.Sack.Revision, full, state);
            _net.Send(NetChannel.Reliable, _participant);
        }

        private void SendAnswers()
        {
            for (int i = 0; i < _answers.Count; i++)
            {
                _answer.Reset();
                RequestCodec.WriteResult(_answer, _answers[i].Sequence, _answers[i].Outcome);
                _net.Send(NetChannel.Reliable, _answer);
            }

            _answers.Clear();
        }

        /// <summary>After every host step: this step's events, then — every second step — the world.</summary>
        private void OnStepped(int frame)
        {
            if (!_net.IsConnected)
            {
                _pending.Clear();
                _answers.Clear();
                return;
            }

            // Bag first, then what happened, then the answers that read them: one ordered channel (Task 99).
            WatchParticipants();
            FlushParticipants(frame);

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

            SendAnswers();

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
                        _snapshot.Dummies.Add(dummy.CaptureReplica());
                    }
                }
            }

            IReadOnlyList<ProjectileState> bolts = _driver.Projectiles;
            for (int i = 0; i < bolts.Count && _snapshot.Projectiles.Count < NetProtocol.MaxEntities; i++)
            {
                _snapshot.Projectiles.Add(bolts[i]);
            }
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
                    return EntityRef.Dummy(dummy.StageIndex, dummy.PropIndex);
                default:
                    return EntityRef.None;
            }
        }

        private void OnStageLoadRequested(int stage, bool isLaunch, float firstArenaMinX, int resumeCheckpointArena)
        {
            if (isLaunch)
            {
                _guestReady.Clear();
                _driver.HoldForPeer = _net.IsConnected;
            }
            else
            {
                // A stage asked for again is not ready again until the guest says so.
                _guestReady.Remove(stage);
            }

            _writer.Reset();
            StageCodec.WriteLoad(_writer, new LoadStageMessage(stage, isLaunch, firstArenaMinX, resumeCheckpointArena));
            _net.Send(NetChannel.Reliable, _writer);
        }

        private void OnStageHandedOver(int stage)
        {
            _writer.Reset();

            // Raised inside the step that crossed, and the clock has already counted past it.
            StageCodec.WriteHandOver(_writer, stage, _driver.Frame - 1);
            _net.Send(NetChannel.Reliable, _writer);
        }

        private void OnGuestStageReady(int stage)
        {
            _guestReady.Add(stage);
            if (_driver.HoldForPeer && _runner != null && stage == _runner.StageIndex)
            {
                _driver.HoldForPeer = false;

                // What the guest sent while the host waited was pressed at a world that was not running
                // (Task 88's review): start from what they send next, not from a second of replay.
                if (_remote != null)
                {
                    _remote.Stream.Release();
                }
            }

            _runner?.RefreshBounds();
        }

        /// <summary>The guest is gone: their body stops taking orders rather than running on with the
        /// last stick it heard. What happens to it next is Plan 2's (D61 — the host carries on solo).</summary>
        private void OnPeerLeft()
        {
            if (_remote != null)
            {
                _remote.Stream.Release();
            }

            _driver.HoldForPeer = false;
            _runner?.RefreshBounds();
        }

        private void OnDestroy()
        {
            if (_driver != null)
            {
                _driver.Stepped -= OnStepped;
                _driver.HitLanded -= OnHit;
                _driver.PickupSpawned -= OnPickup;
                _driver.PickupRemoved -= OnPickupRemoved;
                _driver.RemoteRequestAnswered -= OnRequestAnswered;
                _driver.ScreenChanged -= OnScreenChanged;
                _driver.RackChanged -= OnRackChanged;
                _driver.HoldForPeer = false;
            }

            if (_runner != null)
            {
                _runner.StageLoadRequested -= OnStageLoadRequested;
                _runner.StageHandedOver -= OnStageHandedOver;
                _runner.RemoteStageReady = null;
            }

            foreach (int id in new List<int>(_watched.Keys))
            {
                Unwatch(id);
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
