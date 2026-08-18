using UnityEngine;

namespace BattleBomb.Core.Spatial
{
    /// <summary>
    /// The playable depth band, identical in every area by construction (§2.1, D13). Encoding it as
    /// a project constant makes the never-changes rule impossible to violate per-scene.
    /// </summary>
    public static class DepthBand
    {
        public const float HalfWidth = 3f;
        public const float Min = -HalfWidth;
        public const float Max = HalfWidth;
        public const float Width = HalfWidth * 2f;

        public static float Clamp(float z) => Mathf.Clamp(z, Min, Max);
    }
}
