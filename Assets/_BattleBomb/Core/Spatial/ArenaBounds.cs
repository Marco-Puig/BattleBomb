using System;
using UnityEngine;

namespace BattleBomb.Core.Spatial
{
    /// <summary>
    /// The playable volume of one arena: an X extent and ground height of its own, plus the fixed
    /// depth band. Y is the jump axis, not a bound (§2.1).
    /// </summary>
    public readonly struct ArenaBounds
    {
        public ArenaBounds(float minX, float maxX, float groundY = 0f)
        {
            if (maxX < minX)
            {
                throw new ArgumentException($"maxX ({maxX}) must not be less than minX ({minX}).");
            }

            MinX = minX;
            MaxX = maxX;
            GroundY = groundY;
        }

        public float MinX { get; }
        public float MaxX { get; }
        public float GroundY { get; }

        public float MinZ => DepthBand.Min;
        public float MaxZ => DepthBand.Max;
        public float Width => MaxX - MinX;

        public Vector3 Center => new Vector3((MinX + MaxX) * 0.5f, GroundY, 0f);

        public static ArenaBounds Default => new ArenaBounds(-10f, 10f);

        public Vector3 ClampHorizontal(Vector3 position) =>
            new Vector3(Mathf.Clamp(position.x, MinX, MaxX), position.y, DepthBand.Clamp(position.z));

        public bool Contains(Vector3 position) =>
            position.x >= MinX && position.x <= MaxX &&
            position.z >= MinZ && position.z <= MaxZ;
    }
}
