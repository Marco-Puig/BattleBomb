using BattleBomb.Gameplay.Characters;
using UnityEngine;

namespace BattleBomb.Presentation.Characters
{
    /// <summary>
    /// Lays the body flat while its player is down and stands it back up on revive, rotating the
    /// mesh around its own centre so the fallen body stays exactly on the simulation's position —
    /// the spot a rescuer must actually reach (D25). Owns only this child's local transform;
    /// nothing else writes it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DownedPose : MonoBehaviour
    {
        [Tooltip("Player whose downed state is shown. Leave empty to find one on a parent.")]
        [SerializeField] private CharacterActor _actor;

        [Tooltip("Seconds the fall (and the stand-up) takes.")]
        [SerializeField] private float _fallSeconds = 0.25f;

        [Tooltip("Height of the mesh centre once flat — roughly the lying body's radius.")]
        [SerializeField] private float _fallenHeight = 0.45f;

        private Vector3 _restLocalPosition;
        private Quaternion _restLocalRotation;
        private float _blend;

        private void OnEnable()
        {
            if (_actor == null)
            {
                _actor = GetComponentInParent<CharacterActor>();
            }

            _restLocalPosition = transform.localPosition;
            _restLocalRotation = transform.localRotation;
        }

        private void LateUpdate()
        {
            if (_actor == null)
            {
                return;
            }

            float target = _actor.Condition.IsDown ? 1f : 0f;
            _blend = Mathf.MoveTowards(_blend, target, Time.deltaTime / Mathf.Max(0.01f, _fallSeconds));

            Vector3 fallen = new Vector3(_restLocalPosition.x, _fallenHeight, _restLocalPosition.z);
            transform.localPosition = Vector3.Lerp(_restLocalPosition, fallen, _blend);
            transform.localRotation = _restLocalRotation * Quaternion.Euler(0f, 0f, 90f * _blend);
        }
    }
}
