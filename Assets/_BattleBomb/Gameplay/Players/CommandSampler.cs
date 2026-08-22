using System.Collections.Generic;
using BattleBomb.Core.Players;
using UnityEngine;

namespace BattleBomb.Gameplay.Players
{
    /// <summary>
    /// Samples every registered command source once per render frame, for menus in scenes
    /// without a simulation (the front door). Rule 3 holds there too: the menu reads
    /// <see cref="PlayerCommand"/>s, never a device.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CommandSampler : MonoBehaviour, IPlayerRegistryHost
    {
        private readonly PlayerRegistry _players = new PlayerRegistry();
        private readonly Dictionary<int, PlayerCommand> _commands = new Dictionary<int, PlayerCommand>();
        private int _frame;

        public PlayerRegistry Players => _players;

        public int Frame => _frame;

        public PlayerCommand CommandFor(int playerIdValue) =>
            _commands.TryGetValue(playerIdValue, out PlayerCommand command) ? command : PlayerCommand.Idle(_frame);

        private void Update()
        {
            _frame++;
            _players.SampleAll(_frame, _commands);
        }
    }
}
