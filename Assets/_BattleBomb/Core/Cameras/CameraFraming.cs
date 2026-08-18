using System.Collections.Generic;
using BattleBomb.Core.Spatial;
using UnityEngine;

namespace BattleBomb.Core.Cameras
{
    /// <summary>
    /// Frames every target on a fixed side-on axis. Focus Z is always 0: the depth band never
    /// changes (§2.1), so the camera never tracks depth — that stability is a feature.
    /// </summary>
    public static class CameraFraming
    {
        public static CameraFrame Compute(IReadOnlyList<Vector3> targets,
                                          in CameraTuning tuning, in ArenaBounds bounds)
        {
            float focusHeight = bounds.GroundY + tuning.FocusHeight;

            if (targets == null || targets.Count == 0)
            {
                return new CameraFrame(new Vector3(bounds.Center.x, focusHeight, 0f), tuning.BaseDistance);
            }

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            for (int i = 0; i < targets.Count; i++)
            {
                minX = Mathf.Min(minX, targets[i].x);
                maxX = Mathf.Max(maxX, targets[i].x);
            }

            float spread = maxX - minX;
            float distance = Mathf.Clamp(
                tuning.BaseDistance + Mathf.Max(0f, spread - tuning.ComfortWidth) * tuning.DistancePerUnitSpread,
                tuning.MinDistance,
                tuning.MaxDistance);

            float focusX = (minX + maxX) * 0.5f;
            float lo = bounds.MinX + tuning.EdgeInset;
            float hi = bounds.MaxX - tuning.EdgeInset;
            focusX = lo > hi ? bounds.Center.x : Mathf.Clamp(focusX, lo, hi);

            return new CameraFrame(new Vector3(focusX, focusHeight, 0f), distance);
        }
    }
}
