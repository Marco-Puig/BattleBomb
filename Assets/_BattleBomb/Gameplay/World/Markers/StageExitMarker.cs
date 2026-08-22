using UnityEngine;

namespace BattleBomb.Gameplay.World.Markers
{
    /// <summary>The line past which the stage is left behind (D48's airlock). The next stage
    /// streams in so that its first arena begins here.</summary>
    [DisallowMultipleComponent]
    public sealed class StageExitMarker : MonoBehaviour
    {
        public float X => transform.position.x;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.2f, 0.4f, 0.9f);
            Gizmos.DrawLine(
                new Vector3(transform.position.x, 0f, -3f),
                new Vector3(transform.position.x, 3f, 3f));
        }
    }
}
