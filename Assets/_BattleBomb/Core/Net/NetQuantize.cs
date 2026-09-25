using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// The stick as 16 bits per axis (HANDOFF-M8 paper numbers). Applied on the guest <em>before</em>
    /// it sends or predicts, so the number its own prediction uses is the number the host simulates.
    /// </summary>
    public static class NetQuantize
    {
        public const float AxisScale = 32767f;

        public static short Axis(float value) =>
            (short)Mathf.RoundToInt(Mathf.Clamp(value, -1f, 1f) * AxisScale);

        public static float FromAxis(short value) => Mathf.Clamp(value / AxisScale, -1f, 1f);

        public static Vector2 Move(Vector2 move) =>
            new Vector2(FromAxis(Axis(move.x)), FromAxis(Axis(move.y)));
    }
}
