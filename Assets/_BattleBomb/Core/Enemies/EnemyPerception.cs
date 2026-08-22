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

        /// <summary>
        /// The crowd's turn-taking (D28): false means another enemy holds this target's attack
        /// token, so hover and strafe instead of swinging. The driver grants it; brutes always
        /// receive true because they never wait.
        /// </summary>
        public readonly bool MayAttack;

        public EnemyPerception(Vector3 selfPosition, bool hasTarget, Vector3 targetPosition, bool mayAttack = true)
        {
            SelfPosition = selfPosition;
            HasTarget = hasTarget;
            TargetPosition = targetPosition;
            MayAttack = mayAttack;
        }

        public static EnemyPerception NoTarget(Vector3 selfPosition) =>
            new EnemyPerception(selfPosition, false, default);
    }
}
