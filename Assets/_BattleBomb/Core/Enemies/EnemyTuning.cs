using BattleBomb.Core.Combat;

namespace BattleBomb.Core.Enemies
{
    /// <summary>
    /// One enemy's authored behaviour (D22): archetype plus numbers, ScriptableObject in and this
    /// struct out, like everything else. The attack reuses <see cref="AttackTuning"/> — its
    /// <c>StartupSteps</c> IS the telegraph, authored long. Movement speed lives in the enemy's
    /// <c>MovementTuning</c>, not here: the brain only emits intent.
    /// </summary>
    public readonly struct EnemyTuning
    {
        public readonly EnemyArchetype Archetype;

        /// <summary>Telegraph (startup), hit window, recovery, reach, damage, knockback — one vocabulary.</summary>
        public readonly AttackTuning Attack;

        /// <summary>The beat between attacks — the player's opening.</summary>
        public readonly int CooldownSteps;

        /// <summary>False for the brute: player hits never flinch him (D26 — dodge, don't interrupt).</summary>
        public readonly bool Interruptible;

        /// <summary>Flinch length when a player hit interrupts an interruptible enemy.</summary>
        public readonly int StaggerSteps;

        /// <summary>The region skin's element (D22). None until M5 gives elements teeth.</summary>
        public readonly Element Element;

        /// <summary>Ranged/Caster: projectile flight speed in units per second.</summary>
        public readonly float ProjectileSpeed;

        /// <summary>Ranged/Caster: retreat when the target is nearer than this on X.</summary>
        public readonly float StandoffNearX;

        /// <summary>Ranged/Caster: advance when the target is further than this on X; fire inside it.</summary>
        public readonly float StandoffFarX;

        public EnemyTuning(
            EnemyArchetype archetype,
            AttackTuning attack,
            int cooldownSteps,
            bool interruptible,
            int staggerSteps,
            Element element,
            float projectileSpeed,
            float standoffNearX,
            float standoffFarX)
        {
            Archetype = archetype;
            Attack = attack;
            CooldownSteps = cooldownSteps;
            Interruptible = interruptible;
            StaggerSteps = staggerSteps;
            Element = element;
            ProjectileSpeed = projectileSpeed;
            StandoffNearX = standoffNearX;
            StandoffFarX = standoffFarX;
        }

        /// <summary>Ranged and Caster fight from the standoff and fire projectiles; the rest are melee.</summary>
        public bool FightsAtRange => Archetype == EnemyArchetype.Ranged || Archetype == EnemyArchetype.Caster;
    }
}
