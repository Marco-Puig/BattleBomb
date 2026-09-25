using System.Collections.Generic;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Platform.Net;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// The guest's half of a match, in the Gameplay scene: every local step it sends this machine's
    /// newest commands to the host (with redundancy, D58). Stage B teaches it to draw the host's
    /// world as well.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetGuest : MonoBehaviour
    {
        private readonly NetWriter _writer = new NetWriter(256);
        private readonly List<WireCommand> _recent = new List<WireCommand>(NetProtocol.CommandRedundancy + 1);
        private NetSession _net;
        private SimulationDriver _driver;
        private PlayerId _local;
        private int _latestHostFrame;

        internal void Begin(NetSession net, SimulationDriver driver, PlayerId local)
        {
            _net = net;
            _driver = driver;
            _local = local;
            _driver.ReplicaStepping += OnLocalStep;
            _net.MessageReceived += OnMessage;
        }

        private void OnLocalStep(int frame)
        {
            PlayerCommand command = CommandCodec.Quantized(_driver.CommandFor(_local.Value));
            _recent.Add(WireCommand.From(command));
            if (_recent.Count > NetProtocol.CommandRedundancy)
            {
                _recent.RemoveAt(0);
            }

            _writer.Reset();
            CommandCodec.Write(_writer, _latestHostFrame, _recent);
            _net.Send(NetChannel.Unreliable, _writer);
        }

        private void OnMessage(NetMessageKind kind, NetReader reader)
        {
        }

        private void OnDestroy()
        {
            if (_driver != null)
            {
                _driver.ReplicaStepping -= OnLocalStep;
            }

            if (_net != null)
            {
                _net.MessageReceived -= OnMessage;
                _net.RestoreCouch();
            }
        }
    }
}
