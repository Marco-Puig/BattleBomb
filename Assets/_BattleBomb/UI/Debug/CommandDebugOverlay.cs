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

        private GUIStyle _style;

        private void OnGUI()
        {
            if (_driver == null)
            {
                return;
            }

            int fontSize = Mathf.Max(14, Screen.height / 45);
            if (_style == null || _style.fontSize != fontSize)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = fontSize };
                _style.normal.textColor = Color.white;
            }

            _ids.Clear();
            foreach (KeyValuePair<int, PlayerCommand> entry in _driver.Commands)
            {
                _ids.Add(entry.Key);
            }

            _ids.Sort();

            if (_ids.Count == 0)
            {
                return;
            }

            float row = fontSize + 8f;
            float width = fontSize * 30f;
            GUI.Box(new Rect(8f, 8f, width, 12f + row * _ids.Count), GUIContent.none);

            for (int i = 0; i < _ids.Count; i++)
            {
                PlayerCommand command = _driver.Commands[_ids[i]];
                string line =
                    $"{new PlayerId(_ids[i])}  Move ({command.Move.x:0.00}, {command.Move.y:0.00})  Held [{command.Held}]";
                GUI.Label(new Rect(16f, 14f + i * row, width - 16f, row), line, _style);
            }
        }
    }
}
