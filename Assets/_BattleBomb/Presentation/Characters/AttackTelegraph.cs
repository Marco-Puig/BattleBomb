using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;
using BattleBomb.Gameplay.Characters;
using UnityEngine;

namespace BattleBomb.Presentation.Characters
{
    /// <summary>
    /// Placeholder swing readability until real animation exists: a flat tinted box covering the
    /// attack's reach exactly while its hit window is open. Reads combat state and owns only its
    /// own box — the simulation never knows the box exists (§3: presentation may not own anything
    /// gameplay-relevant, and this owns nothing but pixels).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AttackTelegraph : MonoBehaviour
    {
        [Tooltip("Actor whose attacks are telegraphed. Leave empty to find one on a parent.")]
        [SerializeField] private CharacterActor _actor;

        [SerializeField] private Color _color = new Color(1f, 0.45f, 0.1f);

        private Transform _box;

        private void OnEnable()
        {
            if (_actor == null)
            {
                _actor = GetComponentInParent<CharacterActor>();
            }

            if (_box == null)
            {
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "Telegraph";
                Destroy(box.GetComponent<Collider>());
                MeshRenderer renderer = box.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", _color);
                renderer.sharedMaterial = material;

                // Unparented on purpose: a child of the interpolated visual would be dragged after
                // this component has already placed it in world space.
                _box = box.transform;
                _box.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (_box != null)
            {
                Destroy(_box.gameObject);
                _box = null;
            }
        }

        private void LateUpdate()
        {
            if (_actor == null || _box == null)
            {
                return;
            }

            bool active = _actor.CombatPhase == AttackPhase.Active;
            if (_box.gameObject.activeSelf != active)
            {
                _box.gameObject.SetActive(active);
            }

            if (!active)
            {
                return;
            }

            AttackTuning attack = _actor.CurrentAttack;
            float direction = _actor.Facing == Facing.Right ? 1f : -1f;
            _box.position = _actor.Position + new Vector3(direction * attack.ReachX * 0.5f, 1f, 0f);
            _box.localScale = new Vector3(attack.ReachX, 0.12f, attack.DepthTolerance * 2f);
        }
    }
}
