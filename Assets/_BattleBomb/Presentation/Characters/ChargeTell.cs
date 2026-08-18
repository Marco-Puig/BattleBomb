using BattleBomb.Core.Combat;
using BattleBomb.Gameplay.Characters;
using UnityEngine;

namespace BattleBomb.Presentation.Characters
{
    /// <summary>
    /// Makes charging legible (D19's only hold): the visual swells and pulses with the charge.
    /// Scale only — <c>HitFlash</c> owns this renderer's tint, and two writers would flicker.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChargeTell : MonoBehaviour
    {
        [Tooltip("Actor whose charge is shown. Leave empty to find one on a parent.")]
        [SerializeField] private CharacterActor _actor;

        [SerializeField] private float _maxSwell = 0.18f;

        private Vector3 _restScale;

        private void OnEnable()
        {
            if (_actor == null)
            {
                _actor = GetComponentInParent<CharacterActor>();
            }

            _restScale = transform.localScale;
        }

        private void LateUpdate()
        {
            if (_actor == null)
            {
                return;
            }

            if (_actor.CombatPhase == AttackPhase.Charging)
            {
                float pulse = 1f + _maxSwell * _actor.ChargeFraction * (0.8f + 0.2f * Mathf.Sin(Time.time * 18f));
                transform.localScale = _restScale * pulse;
            }
            else if (transform.localScale != _restScale)
            {
                transform.localScale = _restScale;
            }
        }
    }
}
