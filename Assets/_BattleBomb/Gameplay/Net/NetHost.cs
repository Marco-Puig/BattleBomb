using System.Collections.Generic;
using BattleBomb.Core.Net;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Platform.Net;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// The host's half of a match, in the Gameplay scene: tells the guest which run to load, and
    /// feeds the guest's commands into their <see cref="RemoteCommandSource"/>. Added by the binder
    /// when the machine boots as a host with a guest connected.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetHost : MonoBehaviour
    {
        private readonly NetWriter _writer = new NetWriter(4096);
        private readonly List<WireCommand> _commands = new List<WireCommand>(NetProtocol.CommandRedundancy);
        private NetSession _net;
        private GameSession _session;
        private SimulationDriver _driver;
        private RemoteCommandSource _remote;

        internal void Begin(NetSession net, GameSession session, SimulationDriver driver, RemoteCommandSource remote)
        {
            _net = net;
            _session = session;
            _driver = driver;
            _remote = remote;
            _net.MessageReceived += OnMessage;
            _net.PeerLeft += OnPeerLeft;
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
