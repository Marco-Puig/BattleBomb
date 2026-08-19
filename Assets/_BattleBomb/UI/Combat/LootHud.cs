using System.Collections.Generic;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Loot;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.UI.Combat
{
    /// <summary>
    /// The drop's inspect panel (D30): while someone stands over a token it says what it is —
    /// the placeholder prints its quality roll until M4's items carry real stats — and names the
    /// grab verb. Judging worth is the player's job; grabbing is a deliberate press, so this
    /// panel is the whole decision surface. Purely observational.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LootHud : MonoBehaviour
    {
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

                float x = screen.x - 90f;
                float y = Screen.height - screen.y;
                DrawLine(new Rect(x, y - 22f, 180f, 20f),
                    $"Quality {pickups[i].Quality:F2}", new Color(1f, 0.85f, 0.4f));
                DrawLine(new Rect(x, y, 180f, 18f),
                    "Light to grab", new Color(0.9f, 0.9f, 0.9f));
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
