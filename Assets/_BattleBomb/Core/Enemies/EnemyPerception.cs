using UnityEngine;

namespace BattleBomb.Core.Enemies
{
    /// <summary>
    /// Everything a brain is allowed to know, handed in by the driver each step. A brain never
    /// touches the scene — if one seems to need more knowledge, this struct grows deliberately.
    /// </summary>
    public readonly struct EnemyPerception
    {
        public readonly Vector3 SelfPosition;
        public readonly bool HasTarget;
        public readonly Vector3 TargetPosition;

        public EnemyPerception(Vector3 selfPosition, bool hasTarget, Vector3 targetPosition)
        {
            SelfPosition = selfPosition;
            HasTarget = hasTarget;
            TargetPosition = targetPosition;
        }

        public static EnemyPerception NoTarget(Vector3 selfPosition) =>
            new EnemyPerception(selfPosition, false, default);
    }
}
