using UnityEngine;

namespace BattleBomb.Core.Cameras
{
    /// <summary>
    /// Framing constants for the shared side-on camera. Paper starting values, set by play.
    /// </summary>
    public readonly struct CameraTuning
    {
        public readonly Vector3 OffsetDirection;
        public readonly float BaseDistance;
        public readonly float MinDistance;
        public readonly float MaxDistance;
        public readonly float ComfortWidth;
        public readonly float DistancePerUnitSpread;
        public readonly float FocusHeight;
        public readonly float EdgeInset;

        public CameraTuning(
            Vector3 offsetDirection,
            float baseDistance,
            float minDistance,
            float maxDistance,
            float comfortWidth,
            float distancePerUnitSpread,
            float focusHeight,
            float edgeInset)
        {
            OffsetDirection = offsetDirection.sqrMagnitude > 0f
                ? offsetDirection.normalized
                : new Vector3(0f, 0.45f, -1f).normalized;
            BaseDistance = baseDistance;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            ComfortWidth = comfortWidth;
            DistancePerUnitSpread = distancePerUnitSpread;
            FocusHeight = focusHeight;
            EdgeInset = edgeInset;
        }

        public static CameraTuning Default => new CameraTuning(
            offsetDirection: new Vector3(0f, 0.45f, -1f),
            baseDistance: 14f,
            minDistance: 10f,
            maxDistance: 22f,
            comfortWidth: 8f,
            distancePerUnitSpread: 0.8f,
            focusHeight: 1.4f,
            edgeInset: 4f);
    }
}
