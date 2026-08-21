using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Players;
using UnityEngine;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// A command source the smoke suite drives by hand. It is the same interface a device, a
    /// replay, or a remote peer would use (§4), which is exactly the point: the test presses
    /// buttons through the real pipe rather than reaching past it, so a wiring break between the
    /// command and the simulation is a break the test can see.
    /// </summary>
    internal sealed class ScriptedCommandSource : MonoBehaviour, IPlayerCommandSource
    {
        private PlayerId _id;
        private Vector2 _move;
        private CommandButtons _held;
        private CommandButtons _previous;

        public PlayerId PlayerId => _id;

        internal void Bind(int playerIdValue) => _id = new PlayerId(playerIdValue);

        /// <summary>Hold these buttons and this stick until told otherwise.</summary>
        internal void Set(Vector2 move, CommandButtons held)
        {
            _move = move;
            _held = held;
        }

        internal void Release() => Set(Vector2.zero, CommandButtons.None);

        public PlayerCommand Sample(int frame)
        {
            PlayerCommand command = PlayerCommand.FromState(frame, _move, _held, _previous);
            _previous = _held;
            return command;
        }
    }
}
