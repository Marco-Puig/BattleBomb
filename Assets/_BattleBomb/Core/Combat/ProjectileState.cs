using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// One projectile in flight: a value stepped by the driver, like every other Core state. Aimed
    /// at the target's position on the step it fires — no homing — so movement and jump dodge it
    /// (§2.2, D26: the telegraph and your legs are the defence). Crossing depth freely is the
    /// point: it is ranged's mechanical identity.
    /// </summary>
    public readonly struct ProjectileState
    {
        public readonly Vector3 Position;
        public readonly Vector3 Velocity;
        public readonly float Damage;
        public readonly Element Element;

        /// <summary>Steps left before the projectile despawns unhit.</summary>
        public readonly int LifeSteps;

        /// <summary>The firing player's id, or -1 for an enemy shot (M4's bow). Owner decides
        /// which side the flight tests against — arrows hunt enemies, bolts hunt players.</summary>
        public readonly int OwnerPlayerId;

        public ProjectileState(
            Vector3 position, Vector3 velocity, float damage, Element element, int lifeSteps,
            int ownerPlayerId = -1)
        {
            Position = position;
            Velocity = velocity;
            Damage = damage;
            Element = element;
            LifeSteps = lifeSteps;
            OwnerPlayerId = ownerPlayerId;
        }

        public bool IsExpired => LifeSteps <= 0;

        public bool FromPlayer => OwnerPlayerId >= 0;

        /// <summary>Aims from the muzzle at where the target is right now, depth included.</summary>
        public static ProjectileState Fired(
            Vector3 origin, Vector3 target, float speed, float damage, Element element, int lifeSteps,
            int ownerPlayerId = -1)
        {
            Vector3 toTarget = target - origin;
            Vector3 direction = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : Vector3.right;
            return new ProjectileState(origin, direction * speed, damage, element, lifeSteps, ownerPlayerId);
        }
    }
}
