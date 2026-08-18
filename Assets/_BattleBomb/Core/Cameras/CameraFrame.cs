using UnityEngine;

namespace BattleBomb.Core.Cameras
{
    /// <summary>
    /// One computed framing: what the camera looks at and how far back it sits.
    /// </summary>
    public readonly struct CameraFrame
    {
        public readonly Vector3 Focus;
        public readonly float Distance;

        public CameraFrame(Vector3 focus, float distance)
        {
            Focus = focus;
            Distance = distance;
        }

        public Vector3 PositionFor(in CameraTuning tuning) => Focus + tuning.OffsetDirection * Distance;
    }
}
