using System.Collections.Generic;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.UI.Combat
{
    /// <summary>
    /// The downed-state readout (D25): a marker over each fallen player naming the rescue verb, a
    /// progress bar while a partner channels, and the wipe banner while the attempt-over beat
    /// runs. IMGUI like the rest of the placeholder UI; purely observational.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReviveHud : MonoBehaviour
    {
        [Tooltip("Driver whose players are read. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("Camera used to project world positions. Leave empty to use the main camera.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Marker text size: the settings menu binds this later.")]
        [SerializeField] private int _fontSize = 18;

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

            IReadOnlyList<CharacterActor> players = _driver.Characters.Ordered;
            if (players.Count == 0)
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

            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].Condition.IsDown)
                {
                    continue;
                }

                Vector3 screen = _camera.WorldToScreenPoint(players[i].Position + Vector3.up * 2.4f);
                if (screen.z <= 0f)
                {
                    continue;
                }

                Rect rect = new Rect(screen.x - 110f, Screen.height - screen.y - 22f, 220f, 26f);
                string label = $"P{players[i].PlayerId.Value + 1} DOWN — mash Light";
                DrawLabel(rect, label, new Color(1f, 0.4f, 0.35f));

                float progress = ChannelProgressFor(players, i);
                if (progress > 0f)
                {
                    Rect back = new Rect(screen.x - 45f, rect.y + 28f, 90f, 10f);
                    Color restore = GUI.color;
                    GUI.color = new Color(0f, 0f, 0f, 0.6f);
                    GUI.DrawTexture(back, Texture2D.whiteTexture);
                    GUI.color = new Color(0.35f, 0.9f, 0.4f);
                    GUI.DrawTexture(
                        new Rect(back.x + 1f, back.y + 1f, (back.width - 2f) * progress, back.height - 2f),
                        Texture2D.whiteTexture);
                    GUI.color = restore;
                }
            }

            if (_driver.AttemptEnding)
            {
                Rect banner = new Rect(0f, Screen.height * 0.4f, Screen.width, 40f);
                int size = _style.fontSize;
                _style.fontSize = size * 2;
                DrawLabel(banner, "WIPED OUT", new Color(1f, 0.35f, 0.3f));
                _style.fontSize = size;
            }
        }

        private static float ChannelProgressFor(IReadOnlyList<CharacterActor> players, int target)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Revive.IsActive && players[i].Revive.TargetIndex == target)
                {
                    return players[i].ReviveProgress;
                }
            }

            return 0f;
        }

        private void DrawLabel(Rect rect, string text, Color color)
        {
            _style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, _style);
            _style.normal.textColor = color;
            GUI.Label(rect, text, _style);
        }
    }
}
