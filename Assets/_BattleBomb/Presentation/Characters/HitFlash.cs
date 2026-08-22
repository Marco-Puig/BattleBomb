using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Presentation.Characters
{
    /// <summary>
    /// Blinks the body red when the simulation reports a hit on it, and holds a depleted dummy
    /// grey until it refills. Observes <c>HitLanded</c>; never writes gameplay state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitFlash : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [Tooltip("Driver whose hits are observed. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("Renderer to tint. Leave empty to use the first one below this object.")]
        [SerializeField] private Renderer _renderer;

        [SerializeField] private float _flashSeconds = 0.12f;

        private MaterialPropertyBlock _block;
        private Component _body;
        private Color _restColor = Color.white;
        private float _flashUntil;

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
            }

            Component body = GetComponentInParent<TrainingDummy>();
            _body = body != null ? body : GetComponentInParent<CharacterActor>();
            _block = new MaterialPropertyBlock();
            if (_renderer != null && _renderer.sharedMaterial != null
                && _renderer.sharedMaterial.HasProperty(BaseColor))
            {
                _restColor = _renderer.sharedMaterial.GetColor(BaseColor);
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
        }

        private void OnHit(HitEvent hit)
        {
            if (_body != null && hit.Target == _body)
            {
                _flashUntil = Time.time + _flashSeconds;
            }
        }

        private void LateUpdate()
        {
            if (_renderer == null)
            {
                return;
            }

            Color color = _restColor;
            bool depleted = (_body is TrainingDummy dummy && dummy.IsDepleted)
                || (_body is CharacterActor player && player.Condition.IsDown);
            if (depleted)
            {
                color = new Color(0.32f, 0.32f, 0.32f);
            }

            if (Time.time < _flashUntil)
            {
                color = new Color(1f, 0.25f, 0.2f);
            }

            _block.SetColor(BaseColor, color);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
