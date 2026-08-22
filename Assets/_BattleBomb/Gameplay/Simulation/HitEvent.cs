using UnityEngine;

namespace BattleBomb.Gameplay.Simulation
{
    /// <summary>
    /// One landed hit, announced for Presentation and UI to observe — flashes, hitstop framing,
    /// and damage numbers (D20). Partner shoves carry zero damage and show no number (D21).
    /// The attacker may be a player, an enemy, or null for a projectile.
    /// </summary>
    public readonly struct HitEvent
    {
        public readonly Component Attacker;
        public readonly Component Target;
        public readonly float Damage;
        public readonly Vector3 Position;
        public readonly bool IsPartner;

        /// <summary>The combat stream rolled a critical (M4) — presentation may shout about it.</summary>
        public readonly bool IsCrit;

        /// <summary>A status tick rather than a strike (M5) — no attacker, and styled apart.</summary>
        public readonly bool IsDamageOverTime;

        public HitEvent(
            Component attacker, Component target, float damage, Vector3 position, bool isPartner,
            bool isCrit = false, bool isDamageOverTime = false)
        {
            Attacker = attacker;
            Target = target;
            Damage = damage;
            Position = position;
            IsPartner = isPartner;
            IsCrit = isCrit;
            IsDamageOverTime = isDamageOverTime;
        }
    }
}
