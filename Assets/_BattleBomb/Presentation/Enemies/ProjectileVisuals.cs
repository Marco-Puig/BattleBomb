using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Presentation.Enemies
{
    /// <summary>
    /// Placeholder bolts for the simulation's projectiles, each with a grounded shadow blob — a
    /// projectile's depth is combat information (D14), and the blob is where that truth lives on
    /// screen. The bolt itself renders raised to chest height while the simulation flies at foot
    /// height, so jumping still clears what the eye tracks. Bolt size scales with damage: the
    /// caster's slow artillery visibly outweighs the archer's poke. Reads the driver's list and
    /// owns only its own pool (§3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileVisuals : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Color BoltColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color BlobColor = new Color(0.1f, 0.1f, 0.1f);

        [Tooltip("Driver whose projectiles are shown. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("How high above its simulated flight line a bolt renders.")]
        [SerializeField] private float _boltHeight = 1f;

        private readonly List<Transform> _bolts = new List<Transform>();
        private readonly List<Transform> _blobs = new List<Transform>();

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < _bolts.Count; i++)
            {
                if (_bolts[i] != null)
                {
                    Destroy(_bolts[i].gameObject);
                }

                if (_blobs[i] != null)
                {
                    Destroy(_blobs[i].gameObject);
                }
            }

            _bolts.Clear();
            _blobs.Clear();
        }

        private void LateUpdate()
        {
            if (_driver == null)
            {
                return;
            }

            IReadOnlyList<ProjectileState> projectiles = _driver.Projectiles;
            while (_bolts.Count < projectiles.Count)
            {
                _bolts.Add(CreateBall("Bolt", BoltColor));
                _blobs.Add(CreateBall("BoltShadow", BlobColor));
            }

            // Straight-line flight, so the render position can lead the fixed step exactly.
            float lead = _driver.Alpha * _driver.StepDuration;
            for (int i = 0; i < _bolts.Count; i++)
            {
                bool live = i < projectiles.Count;
                if (_bolts[i].gameObject.activeSelf != live)
                {
                    _bolts[i].gameObject.SetActive(live);
                    _blobs[i].gameObject.SetActive(live);
                }

                if (!live)
                {
                    continue;
                }

                ProjectileState projectile = projectiles[i];
                Vector3 at = projectile.Position + projectile.Velocity * lead;
                float size = 0.3f + 0.03f * projectile.Damage;
                _bolts[i].position = new Vector3(at.x, at.y + _boltHeight, at.z);
                _bolts[i].localScale = Vector3.one * size;
                _blobs[i].position = new Vector3(at.x, 0.03f, at.z);
                _blobs[i].localScale = new Vector3(size * 1.2f, 0.02f, size * 1.2f);
            }
        }

        private Transform CreateBall(string name, Color color)
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = name;
            Destroy(ball.GetComponent<Collider>());
            MeshRenderer renderer = ball.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, color);
            renderer.SetPropertyBlock(block);
            ball.transform.SetParent(transform, false);
            ball.SetActive(false);
            return ball.transform;
        }
    }
}
