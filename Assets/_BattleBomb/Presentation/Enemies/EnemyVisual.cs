using BattleBomb.Core.Enemies;
using BattleBomb.Core.Movement;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Presentation.Enemies
{
    /// <summary>
    /// The enemy body: an interpolated follow of the simulated actor, a per-archetype placeholder
    /// silhouette (shape and colour, abstract by design — region skins arrive with the story), and
    /// the telegraph tell ramping the body toward its warning colour through the windup. This is
    /// the sole writer of the renderer's tint — rest colour, telegraph ramp, hit flash, and
    /// depleted grey compose in one place, because two tint writers on one renderer flicker (the
    /// ChargeTell lesson). Reads simulation state, never writes it (§3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyVisual : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Color FlashColor = new Color(1f, 0.25f, 0.2f);
        private static readonly Color DepletedColor = new Color(0.32f, 0.32f, 0.32f);

        [Tooltip("Enemy this visual follows. Leave empty to find one on a parent.")]
        [SerializeField] private EnemyActor _enemy;

        [Tooltip("Driver whose interpolation and hits are read. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [SerializeField] private float _flashSeconds = 0.12f;

        private MeshFilter _filter;
        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private bool _applied;
        private EnemyArchetype _archetype;
        private Vector3 _bodyScale = Vector3.one;
        private float _heightOffset = 1f;
        private Color _restColor = Color.white;
        private Color _tellColor = Color.white;
        private float _windupSwell;
        private float _flashUntil;

        private void OnEnable()
        {
            if (_enemy == null)
            {
                _enemy = GetComponentInParent<EnemyActor>();
            }

            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            _filter = GetComponent<MeshFilter>();
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
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
            if (_enemy != null && hit.Target == (Component)_enemy)
            {
                _flashUntil = Time.time + _flashSeconds;
            }
        }

        private void LateUpdate()
        {
            if (_enemy == null || _driver == null || _renderer == null)
            {
                return;
            }

            if (!_applied)
            {
                if (!_enemy.IsConfigured)
                {
                    return;
                }

                ApplySilhouette(_enemy.Spec.Tuning.Archetype);
            }

            transform.position = Vector3.Lerp(_enemy.PreviousPosition, _enemy.Position, _driver.Alpha)
                + Vector3.up * _heightOffset;
            transform.rotation = Quaternion.Euler(0f, _enemy.Facing == Facing.Right ? 0f : 180f, 0f);

            float windup = _enemy.Phase == EnemyPhase.Telegraph ? _enemy.TelegraphFraction : 0f;
            transform.localScale = _bodyScale * (1f + _windupSwell * windup);

            // A mark pulses the body toward its element's colour (D40) — read from the catalog, so
            // a new element is visible the moment its asset exists.
            Color color = _restColor;
            if (_enemy.Statuses.Count > 0)
            {
                Color mark = _driver.ColorOf(_enemy.Statuses.Active[0].Element);
                color = Color.Lerp(color, mark, 0.45f + 0.2f * Mathf.Sin(Time.time * 9f));
            }

            if (windup > 0f)
            {
                color = Color.Lerp(color, _tellColor, windup);
                if (_archetype == EnemyArchetype.Caster && windup > 0.6f)
                {
                    // The caster's hit is the biggest, so its tell ends in a white flicker — the
                    // highest-contrast moment in the fight (D26: the telegraph is the defence).
                    color = Color.Lerp(color, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.time * 26f));
                }
            }

            if (_enemy.IsDepleted)
            {
                color = DepletedColor;
            }

            if (Time.time < _flashUntil)
            {
                color = FlashColor;
            }

            _block.SetColor(BaseColor, color);
            _renderer.SetPropertyBlock(_block);
        }

        private void ApplySilhouette(EnemyArchetype archetype)
        {
            _archetype = archetype;
            PrimitiveType shape;
            switch (archetype)
            {
                case EnemyArchetype.Ranged:
                    shape = PrimitiveType.Cylinder;
                    _bodyScale = new Vector3(0.45f, 1.05f, 0.45f);
                    _heightOffset = 1.05f;
                    _restColor = new Color(0.25f, 0.65f, 0.65f);
                    _tellColor = Color.white;
                    _windupSwell = 0.12f;
                    break;

                case EnemyArchetype.Caster:
                    shape = PrimitiveType.Sphere;
                    _bodyScale = new Vector3(1.1f, 1.1f, 1.1f);
                    _heightOffset = 1.15f;
                    _restColor = new Color(0.55f, 0.35f, 0.78f);
                    _tellColor = new Color(1f, 0.45f, 1f);
                    _windupSwell = 0.28f;
                    break;

                case EnemyArchetype.Brute:
                    shape = PrimitiveType.Cube;
                    _bodyScale = new Vector3(1.5f, 1.9f, 1.1f);
                    _heightOffset = 0.95f;
                    _restColor = new Color(0.45f, 0.3f, 0.2f);
                    _tellColor = new Color(1f, 0.62f, 0.2f);
                    _windupSwell = 0.15f;
                    break;

                default:
                    shape = PrimitiveType.Capsule;
                    _bodyScale = new Vector3(0.8f, 1f, 0.8f);
                    _heightOffset = 1f;
                    _restColor = new Color(0.78f, 0.62f, 0.38f);
                    _tellColor = Color.white;
                    _windupSwell = 0.12f;
                    break;
            }

            if (_filter != null)
            {
                _filter.sharedMesh = PrimitiveMesh(shape);
            }

            transform.localScale = _bodyScale;
            _applied = true;
        }

        private static Mesh PrimitiveMesh(PrimitiveType shape)
        {
            GameObject temp = GameObject.CreatePrimitive(shape);
            temp.SetActive(false);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);
            return mesh;
        }
    }
}
