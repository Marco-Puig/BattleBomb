using UnityEngine;

namespace BattleBomb.Gameplay.World.Markers
{
    /// <summary>Where the players stand when a stage begins fresh. A resume or a wipe uses a
    /// checkpoint room's respawn point instead (D49).</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerSpawnMarker : MonoBehaviour
    {
        [Tooltip("Offsets for player slots 1, 2… so two players never spawn inside each other.")]
        [SerializeField] private Vector3[] _slotOffsets = { new Vector3(-0.8f, 0f, 0.5f), new Vector3(0.8f, 0f, -0.5f) };

        public Vector3 PositionFor(int slot)
        {
            if (_slotOffsets == null || _slotOffsets.Length == 0)
            {
                return transform.position;
            }

            return transform.position + _slotOffsets[Mathf.Clamp(slot, 0, _slotOffsets.Length - 1)];
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }
    }
}
