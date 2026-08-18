using BattleBomb.Core.Spatial;
using UnityEngine;

namespace BattleBomb.Gameplay.World
{
    /// <summary>
    /// Scene authoring for <see cref="ArenaBounds"/>. An arena varies only in its X extent — the
    /// gizmo draws the fixed depth band so the rule stays visible while building a level (§2.1).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArenaVolume : MonoBehaviour
    {
        [SerializeField] private float _minX = -10f;
        [SerializeField] private float _maxX = 10f;
        [SerializeField] private float _groundY;

        public ArenaBounds ToRuntime() =>
            new ArenaBounds(Mathf.Min(_minX, _maxX), Mathf.Max(_minX, _maxX), _groundY);

        private void OnDrawGizmos()
        {
            ArenaBounds bounds = ToRuntime();
            Gizmos.color = new Color(0.2f, 0.9f, 0.6f, 0.9f);
            Gizmos.DrawWireCube(bounds.Center, new Vector3(bounds.Width, 0.02f, DepthBand.Width));
        }
    }
}
