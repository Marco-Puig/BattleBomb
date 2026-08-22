using UnityEngine;

namespace BattleBomb.Gameplay.World.Markers
{
    /// <summary>
    /// A checkpoint room (D42) in a stage scene: where the chest, an optional shopkeeper, a
    /// training dummy, and the respawn point sit. Whether it has a shopkeeper is the stage
    /// asset's word, not the scene's — the scene only says where things would go.
    /// </summary>
    /// <remarks>
    /// Extents are half-open, <c>[MinX, MaxX)</c>, the same convention <see cref="ArenaMarker"/>
    /// uses: rooms are authored flush against the arenas either side of them, so the boundary
    /// belongs to whatever starts there. <see cref="EntryX"/> deliberately sits well inside the
    /// room rather than on its edge, so "the players have arrived" never turns on which side of
    /// a shared coordinate someone landed.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CheckpointRoomMarker : MonoBehaviour
    {
        [Tooltip("The arena this room follows, by index.")]
        [SerializeField] private int _afterArena;

        [SerializeField] private float _halfWidth = 3f;

        [SerializeField] private Vector3 _chestOffset = new Vector3(-1.5f, 0f, 1.5f);
        [SerializeField] private Vector3 _shopkeeperOffset = new Vector3(1.5f, 0f, 1.5f);
        [SerializeField] private Vector3 _dummyOffset = new Vector3(0f, 0f, -1.5f);
        [SerializeField] private Vector3 _respawnOffset = new Vector3(0f, 0f, 0f);

        public int AfterArena => _afterArena;

        /// <summary>Inclusive lower edge — this room owns x = MinX.</summary>
        public float MinX => transform.position.x - Mathf.Abs(_halfWidth);

        /// <summary>Exclusive upper edge — whatever starts here owns x = MaxX, not this room.</summary>
        public float MaxX => transform.position.x + Mathf.Abs(_halfWidth);

        public Vector3 ChestPosition => transform.position + _chestOffset;

        public Vector3 ShopkeeperPosition => transform.position + _shopkeeperOffset;

        public Vector3 DummyPosition => transform.position + _dummyOffset;

        public Vector3 RespawnPosition => transform.position + _respawnOffset;

        /// <summary>The players are "in" the room once past its first third — deep enough
        /// that the fight behind them is plainly over, shallow enough to feel like arriving.</summary>
        public float EntryX => MinX + Mathf.Abs(_halfWidth) * 0.66f;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 0.9f, 0.9f);
            Gizmos.DrawWireCube(
                new Vector3(transform.position.x, transform.position.y + 0.5f, 0f),
                new Vector3(Mathf.Abs(_halfWidth) * 2f, 1f, 6f));
        }
    }
}
