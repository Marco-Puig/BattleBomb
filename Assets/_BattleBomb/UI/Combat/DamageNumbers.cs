using System.Collections.Generic;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.UI.Combat
{
    /// <summary>
    /// Floating damage numbers (D20): every landed hit pops its resolved damage at the hit
    /// position, rising and fading. Font size is a serialized parameter from day one so the future
    /// settings menu can bind it — alongside a full disable — without touching this code. Partner
    /// shoves resolve no damage and therefore show nothing (D21). Purely observational.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageNumbers : MonoBehaviour
    {
        [Tooltip("Driver whose hits are observed. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("Camera used to project hit positions. Leave empty to use the main camera.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Number text size (D20): the settings menu binds this later.")]
        [SerializeField] private int _fontSize = 30;

        [SerializeField] private float _lifetimeSeconds = 0.8f;
        [SerializeField] private float _riseUnitsPerSecond = 1.4f;

        private struct Entry
        {
            public Vector3 World;
            public int Amount;
            public float Born;
        }

        private readonly List<Entry> _entries = new List<Entry>();
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

            if (_driver != null)
            {
                _driver.HitLanded += OnHit;
            }
        }

        private void OnDisable()
        {
            if (_driver != null)
            {
                _driver.HitLanded -= OnHit;
            }

            _entries.Clear();
        }

        private void OnHit(HitEvent hit)
        {
            if (hit.Damage <= 0f)
            {
                return;
            }

            _entries.Add(new Entry
            {
                World = hit.Position + Vector3.up * 2.2f,
                Amount = Mathf.Max(1, Mathf.RoundToInt(hit.Damage)),
                Born = Time.time,
            });
        }

        private void OnGUI()
        {
            if (_camera == null || _entries.Count == 0)
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

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                float age = Time.time - _entries[i].Born;
                if (age > _lifetimeSeconds)
                {
                    _entries.RemoveAt(i);
                    continue;
                }

                Vector3 world = _entries[i].World + Vector3.up * (_riseUnitsPerSecond * age);
                Vector3 screen = _camera.WorldToScreenPoint(world);
                if (screen.z <= 0f)
                {
                    continue;
                }

                float alpha = 1f - age / _lifetimeSeconds;
                string text = _entries[i].Amount.ToString();
                Rect rect = new Rect(screen.x - 60f, Screen.height - screen.y - 20f, 120f, 40f);

                _style.normal.textColor = new Color(0f, 0f, 0f, alpha);
                GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, _style);
                _style.normal.textColor = new Color(1f, 0.93f, 0.35f, alpha);
                GUI.Label(rect, text, _style);
            }
        }
    }
}
