using System.Collections.Generic;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Loot;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.UI.Items;
using UnityEngine;

namespace BattleBomb.UI.Combat
{
    /// <summary>
    /// The drop's item card (D30): while someone stands over a drop it shows the real thing —
    /// name in its quality colour, core stats, every affix, the level requirement — and names
    /// the grab verb. Judging worth is the player's job; grabbing is a deliberate press, so this
    /// panel is the whole decision surface. Purely observational.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LootHud : MonoBehaviour
    {
        private readonly List<string> _lines = new List<string>();

        [Tooltip("Driver whose drops are read. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("Camera used to project drop positions. Leave empty to use the main camera.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Panel text size: the settings menu binds this later.")]
        [SerializeField] private int _fontSize = 14;

        private GUIStyle _style;

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void OnGUI()
        {
            if (_driver == null || _camera == null)
            {
                return;
            }

            IReadOnlyList<DropPickup> pickups = _driver.Pickups;
            if (pickups.Count == 0)
            {
                return;
            }

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
            }

            _style.fontSize = Mathf.Max(8, _fontSize);

            IReadOnlyList<CharacterActor> players = _driver.Characters.Ordered;
            for (int i = 0; i < pickups.Count; i++)
            {
                if (pickups[i] == null || !AnyoneOver(players, pickups[i].Position))
                {
                    continue;
                }

                Vector3 screen = _camera.WorldToScreenPoint(pickups[i].Position + Vector3.up * 1.1f);
                if (screen.z <= 0f)
                {
                    continue;
                }

                var item = pickups[i].Item;
                ItemText.BuildLines(item, _lines);

                float lineHeight = _fontSize + 5f;
                float x = screen.x - 120f;
                float y = Screen.height - screen.y - (_lines.Count + 1) * lineHeight;

                DrawLine(new Rect(x, y, 240f, lineHeight + 2f),
                    item.DisplayName, QualityColors.For(item.Quality));
                y += lineHeight + 2f;
                for (int line = 0; line < _lines.Count; line++)
                {
                    DrawLine(new Rect(x, y, 240f, lineHeight), _lines[line],
                        new Color(0.92f, 0.92f, 0.92f));
                    y += lineHeight;
                }

                DrawLine(new Rect(x, y, 240f, lineHeight),
                    "Light to grab", new Color(0.65f, 0.65f, 0.65f));
            }
        }

        private static bool AnyoneOver(IReadOnlyList<CharacterActor> players, Vector3 position)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Condition.IsDown)
                {
                    continue;
                }

                Vector3 to = players[i].Position - position;
                to.y = 0f;
                if (to.sqrMagnitude <= SimulationDriver.GrabRadius * SimulationDriver.GrabRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawLine(Rect rect, string text, Color color)
        {
            _style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, _style);
            _style.normal.textColor = color;
            GUI.Label(rect, text, _style);
        }
    }
}
