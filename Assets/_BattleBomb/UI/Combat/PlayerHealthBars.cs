using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.UI.Combat
{
    /// <summary>
    /// Minimal per-player health readout: one bar per registered player in PlayerId order, with
    /// the downed state spelled out. Bar and text sizes are serialized from day one so the future
    /// settings menu can bind them (the DamageNumbers pattern, D20). Purely observational.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealthBars : MonoBehaviour
    {
        [Tooltip("Driver whose players are read. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("Bar text size: the settings menu binds this later.")]
        [SerializeField] private int _fontSize = 16;

        [SerializeField] private float _barWidth = 240f;
        [SerializeField] private float _barHeight = 22f;

        private GUIStyle _style;

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }
        }

        private void OnGUI()
        {
            if (_driver == null)
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
                    alignment = TextAnchor.MiddleLeft,
                };
            }

            _style.fontSize = Mathf.Max(8, _fontSize);

            float x = (Screen.width - _barWidth) * 0.5f;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerCondition condition = players[i].Condition;
                float fraction = condition.Health.Max > 0f
                    ? Mathf.Clamp01(condition.Health.Current / condition.Health.Max)
                    : 0f;

                Rect back = new Rect(x, 10f + i * (_barHeight + 6f), _barWidth, _barHeight);
                Color restore = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.55f);
                GUI.DrawTexture(back, Texture2D.whiteTexture);
                if (fraction > 0f)
                {
                    GUI.color = Color.Lerp(
                        new Color(0.85f, 0.2f, 0.15f), new Color(0.25f, 0.8f, 0.3f), fraction);
                    GUI.DrawTexture(
                        new Rect(back.x + 2f, back.y + 2f, (back.width - 4f) * fraction, back.height - 4f),
                        Texture2D.whiteTexture);
                }

                GUI.color = restore;

                string label = condition.IsDown
                    ? $"P{players[i].PlayerId.Value + 1}  DOWN"
                    : $"P{players[i].PlayerId.Value + 1}  {Mathf.CeilToInt(condition.Health.Current)}/{Mathf.CeilToInt(condition.Health.Max)}";
                _style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
                GUI.Label(new Rect(back.x + 9f, back.y + 1f, back.width, back.height), label, _style);
                _style.normal.textColor = condition.IsDown ? new Color(1f, 0.45f, 0.4f) : Color.white;
                GUI.Label(new Rect(back.x + 8f, back.y, back.width, back.height), label, _style);
            }
        }
    }
}
