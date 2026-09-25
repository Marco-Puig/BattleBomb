using BattleBomb.Core.Players;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// What a guest's own menu keeps off the wire. While the menu is open the guest sends a neutral
    /// command: its presses and its stick are the menu's. Once it closes, a button still held from
    /// the menu counts only after it has been let go (D57's held-across rule), so the South that
    /// closes the menu is not a jump on the host. The stick goes out again at once — it is a
    /// position, not a press.
    /// </summary>
    public sealed class MenuGate
    {
        private CommandButtons _swallowed;
        private CommandButtons _sent;

        public PlayerCommand Filter(PlayerCommand command, bool menuOpen)
        {
            Vector2 move = command.Move;
            CommandButtons held;
            if (menuOpen)
            {
                _swallowed = command.Held;
                move = Vector2.zero;
                held = CommandButtons.None;
            }
            else
            {
                _swallowed &= command.Held;
                held = command.Held & ~_swallowed;
            }

            PlayerCommand sent = new PlayerCommand(command.Frame, move, held, held & ~_sent, _sent & ~held);
            _sent = held;
            return sent;
        }
    }
}
