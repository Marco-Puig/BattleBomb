using System.Collections.Generic;
using BattleBomb.Core.Spatial;
using UnityEngine;

namespace BattleBomb.Gameplay.World.Markers
{
    /// <summary>
    /// One arena's extent and spawn points, placed in a stage scene (D48). Data only — no
    /// logic, no Update — so a stage scene stays geometry and markers. X values are local to
    /// the marker, so the runner's world offset for a streamed stage comes for free through
    /// the transform.
    /// </summary>
    /// <remarks>
    /// Extents are half-open, <c>[MinX, MaxX)</c>: a coordinate exactly on a boundary belongs
    /// to the region that starts there, never the one that ends there. Arenas and checkpoint
    /// rooms are authored touching — stage 1's arena 0 ends at x = 8 and Room A starts at
    /// x = 8 — so without that rule the seam would belong to both, and every comparison in the
    /// runner would have to guess which side owned it.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ArenaMarker : MonoBehaviour
    {
        [Tooltip("Which arena of the stage asset this is; 0 is the first fight.")]
        [SerializeField] private int _index;

        [SerializeField] private float _halfWidth = 8f;

        [Tooltip("Where waves appear, local to this marker. Empty: the arena's two edges.")]
        [SerializeField] private Vector3[] _spawnPoints = new Vector3[0];

        public int Index => _index;

        /// <summary>Inclusive lower edge — this arena owns x = MinX.</summary>
        public float MinX => transform.position.x - Mathf.Abs(_halfWidth);

        /// <summary>Exclusive upper edge — whatever starts here owns x = MaxX, not this arena.</summary>
        public float MaxX => transform.position.x + Mathf.Abs(_halfWidth);

        public ArenaBounds Bounds => new ArenaBounds(MinX, MaxX, transform.position.y);

        /// <summary>
        /// World-space spawn points, a fresh array per read so a caller may hold on to one.
        /// A wave reads this a handful of times per arena, so the allocation is nothing next
        /// to the aliasing a shared buffer would invite.
        /// </summary>
        public IReadOnlyList<Vector3> SpawnPoints
        {
            get
            {
                if (_spawnPoints == null || _spawnPoints.Length == 0)
                {
                    return new[]
                    {
                        new Vector3(MinX + 1f, transform.position.y, 0f),
                        new Vector3(MaxX - 1f, transform.position.y, 0f),
                    };
                }

                var points = new Vector3[_spawnPoints.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = transform.position + _spawnPoints[i];
                }

                return points;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.5f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(
                new Vector3(transform.position.x, transform.position.y, 0f),
                new Vector3(Mathf.Abs(_halfWidth) * 2f, 0.05f, DepthBand.Width));
        }
    }
}
