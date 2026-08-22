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

        /// <summary>The region skin's element (D22) — authored data, never an enum entry (D38).</summary>
        public readonly ElementId Element;

        /// <summary>Ranged/Caster: projectile flight speed in units per second.</summary>
        public readonly float ProjectileSpeed;

        /// <summary>Ranged/Caster: retreat when the target is nearer than this on X.</summary>
        public readonly float StandoffNearX;

        /// <summary>Ranged/Caster: advance when the target is further than this on X; fire inside it.</summary>
        public readonly float StandoffFarX;

        /// <summary>Melee circles the target at this X distance while waiting its turn (D28).</summary>
        public readonly float HoverDistanceX;

        /// <summary>Steps between depth-strafe direction flips while hovering or peeling. 0 stands still.</summary>
        public readonly int StrafePeriodSteps;

        /// <summary>Steps between possible hops while free to move. 0 never hops.</summary>
        public readonly int HopPulseSteps;

        /// <summary>False for the brute: he never waits for an attack token and never peels off (D28).</summary>
        public readonly bool TakesTurns;

        public EnemyTuning(
            EnemyArchetype archetype,
            AttackTuning attack,
            int cooldownSteps,
            bool interruptible,
            int staggerSteps,
            ElementId element,
            float projectileSpeed,
            float standoffNearX,
            float standoffFarX,
            float hoverDistanceX = 3f,
            int strafePeriodSteps = 90,
            int hopPulseSteps = 0,
            bool takesTurns = true)
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
            HoverDistanceX = hoverDistanceX;
            StrafePeriodSteps = strafePeriodSteps;
            HopPulseSteps = hopPulseSteps;
            TakesTurns = takesTurns;
        }

        /// <summary>Ranged and Caster fight from the standoff and fire projectiles; the rest are melee.</summary>
        public bool FightsAtRange => Archetype == EnemyArchetype.Ranged || Archetype == EnemyArchetype.Caster;
    }
}
