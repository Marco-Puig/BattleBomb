using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Movement;
using BattleBomb.Gameplay.Characters;
using UnityEngine;

namespace BattleBomb.Presentation.Enemies
{
    /// <summary>
    /// The brute's ground marker: through its long windup the exact impact box glows on the
    /// ground, heating from ember to hot orange, so the dodge answer — depth or jump — is
    /// readable at real speed even when the body is buried in a crowd. Only the brute earns a
    /// marker: it is the melee hit that must never surprise. The body's own colour ramp lives in
    /// <c>EnemyVisual</c>; this owns nothing but the marker's pixels (§3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TelegraphTell : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Color EmberColor = new Color(0.3f, 0.06f, 0.02f);
        private static readonly Color HotColor = new Color(1f, 0.55f, 0.1f);

        [Tooltip("Enemy whose telegraph is marked. Leave empty to find one on a parent.")]
        [SerializeField] private EnemyActor _enemy;

        private Transform _marker;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;

        private void OnEnable()
        {
            if (_enemy == null)
            {
                _enemy = GetComponentInParent<EnemyActor>();
            }

            if (_marker == null)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "GroundMarker";
                Destroy(marker.GetComponent<Collider>());
                _renderer = marker.GetComponent<MeshRenderer>();
                _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                // Unparented on purpose: EnemyVisual repositions this object's parent in world
                // space every frame, and the marker belongs to the ground, not the body.
                _marker = marker.transform;
                _marker.gameObject.SetActive(false);
            }

            _block = new MaterialPropertyBlock();
        }

        private void OnDisable()
        {
            if (_marker != null)
            {
                Destroy(_marker.gameObject);
                _marker = null;
            }
        }

        private void LateUpdate()
        {
            if (_enemy == null || _marker == null)
            {
                return;
            }

            bool show = _enemy.IsConfigured
                && !_enemy.IsDepleted
                && _enemy.Spec.Tuning.Archetype == EnemyArchetype.Brute
                && (_enemy.Phase == EnemyPhase.Telegraph || _enemy.Phase == EnemyPhase.Active);
            if (_marker.gameObject.activeSelf != show)
            {
                _marker.gameObject.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            AttackTuning attack = _enemy.Spec.Tuning.Attack;
            float direction = _enemy.Facing == Facing.Right ? 1f : -1f;
            _marker.position = _enemy.Position + new Vector3(direction * attack.ReachX * 0.5f, 0.05f, 0f);
            _marker.localScale = new Vector3(attack.ReachX, 0.04f, attack.DepthTolerance * 2f);

            float heat = _enemy.Phase == EnemyPhase.Active ? 1f : _enemy.TelegraphFraction;
            _block.SetColor(BaseColor, Color.Lerp(EmberColor, HotColor, heat));
            _renderer.SetPropertyBlock(_block);
        }
    }
}
