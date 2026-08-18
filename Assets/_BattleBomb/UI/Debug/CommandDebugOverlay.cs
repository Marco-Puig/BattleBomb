using System.Collections.Generic;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.UI.Debug
{
    /// <summary>
    /// Draws each sampled command as one line of text — the on-screen proof that a device press
    /// became a <see cref="PlayerCommand"/>. Reads the driver and mutates nothing (§3).
    /// </summary>
    public sealed class CommandDebugOverlay : MonoBehaviour
    {
        [SerializeField] private SimulationDriver _driver;

        private readonly List<int> _ids = new List<int>();

        private void OnGUI()
        {
            if (_driver == null)
            {
                return;
            }

            _ids.Clear();
            foreach (KeyValuePair<int, PlayerCommand> entry in _driver.Commands)
            {
                _ids.Add(entry.Key);
            }

            _ids.Sort();

            for (int i = 0; i < _ids.Count; i++)
            {
                PlayerCommand command = _driver.Commands[_ids[i]];
                string line =
                    $"{new PlayerId(_ids[i])}  Move ({command.Move.x:0.00}, {command.Move.y:0.00})  Held [{command.Held}]";
                GUI.Label(new Rect(10f, 10f + i * 22f, 640f, 20f), line);
            }
        }
    }
}
