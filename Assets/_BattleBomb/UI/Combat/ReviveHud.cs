using System.Collections.Generic;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.UI.Chest;
using UnityEngine;

namespace BattleBomb.UI.Combat
{
    /// <summary>
    /// The downed-state readout (D25/D31): a marker over each fallen player naming the rescue
    /// verb, and while a partner channels, the progress bar plus the heartbeat — a pulse that
    /// swells to its peak exactly when a press earns full accuracy, reading the same clock the
    /// simulation prices with. The wipe banner runs during the attempt-over beat. IMGUI like the
    /// rest of the placeholder UI; purely observational.
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
                    richText = true,
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
                string beat = PromptRow.Inline(FamilyOfReviver(players, i), PromptKey.Light);
                string label = $"P{players[i].PlayerId.Value + 1} DOWN — {beat} on the beat";
                DrawLabel(rect, label, new Color(1f, 0.4f, 0.35f));

                CharacterActor reviver = ChannellingRescuerOf(players, i);
                if (reviver != null)
                {
                    Rect back = new Rect(screen.x - 45f, rect.y + 28f, 90f, 10f);
                    Color restore = GUI.color;
                    GUI.color = new Color(0f, 0f, 0f, 0.6f);
                    GUI.DrawTexture(back, Texture2D.whiteTexture);
                    GUI.color = new Color(0.35f, 0.9f, 0.4f);
                    GUI.DrawTexture(
                        new Rect(back.x + 1f, back.y + 1f, (back.width - 2f) * reviver.ReviveProgress, back.height - 2f),
                        Texture2D.whiteTexture);

                    // The heartbeat: swells to its peak at the accuracy window's centre (D31).
                    float phase = Core.Combat.ReviveChannel.BeatPhase(
                        reviver.Revive.StepsElapsed, reviver.ReviveBeatSteps);
                    float pulse = 1f - 2f * Mathf.Abs(phase - 0.5f);
                    float size = 8f + 16f * pulse;
                    GUI.color = new Color(1f, 0.3f + 0.7f * pulse, 0.3f + 0.5f * pulse);
                    GUI.DrawTexture(
                        new Rect(back.x + back.width + 12f - size * 0.5f, back.y + 5f - size * 0.5f, size, size),
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

        private static CharacterActor ChannellingRescuerOf(IReadOnlyList<CharacterActor> players, int target)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Revive.IsActive && players[i].Revive.TargetIndex == target)
                {
                    return players[i];
                }
            }

            return null;
        }

        /// <summary>The buttons of whoever can do the reviving: the first player still standing.</summary>
        private InputFamily FamilyOfReviver(IReadOnlyList<CharacterActor> players, int downed)
        {
            for (int j = 0; j < players.Count; j++)
            {
                if (j != downed && !players[j].Condition.IsDown)
                {
                    return _driver.Players.FamilyOf(players[j].PlayerId);
                }
            }

            return InputFamily.Keyboard;
        }

        private void DrawLabel(Rect rect, string text, Color color)
        {
            _style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), PromptRow.Plain(text), _style);
            _style.normal.textColor = color;
            GUI.Label(rect, text, _style);
        }
    }
}
