using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Movement;
using UnityEngine;

namespace BattleBomb.Gameplay.Data
{
    /// <summary>
    /// Authoring format for one enemy (D22): archetype behaviour tuning, vitals, resistances, and
    /// the D23 reward inputs — a data asset in, an <see cref="EnemySpec"/> out. A region's roster
    /// is a set of these; a new enemy is never a new class (pillar 3). Placeholder assets stay
    /// abstractly named (§9): Grunt, Ranged, Caster, Brute.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleBomb/Enemy Definition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private EnemyArchetype _archetype = EnemyArchetype.Grunt;

        [Tooltip("The region skin's element (D22) — an authored asset, empty for kinetic-only (D38).")]
        [SerializeField] private ElementDefinition _element;

        [Header("Attack")]
        [Tooltip("StartupSteps IS the telegraph — author it long enough to read (D22).")]
        [SerializeField] private AttackSpec _attack = new AttackSpec();

        [Tooltip("The beat between attacks — the player's opening.")]
        [SerializeField] private int _cooldownSteps = 45;

        [Tooltip("Off for the brute: player hits never flinch him (D26 — dodge, don't interrupt).")]
        [SerializeField] private bool _interruptible = true;

        [Tooltip("Flinch length when a player hit interrupts.")]
        [SerializeField] private int _staggerSteps = 20;

        [Header("Ranged / Caster")]
        [SerializeField] private float _projectileSpeed = 8f;

        [Tooltip("Retreat when the target is nearer than this on X.")]
        [SerializeField] private float _standoffNearX = 4f;

        [Tooltip("Advance when further than this on X; fire from inside it.")]
        [SerializeField] private float _standoffFarX = 7f;

        [Header("Liveliness (D28)")]
        [Tooltip("Melee circles the target at this X distance while waiting its attack turn.")]
        [SerializeField] private float _hoverDistanceX = 3f;

        [Tooltip("Steps between depth-strafe direction flips while circling. 0 stands still.")]
        [SerializeField] private int _strafePeriodSteps = 90;

        [Tooltip("Steps between possible hops while free to move. 0 never hops.")]
        [SerializeField] private int _hopPulseSteps = 0;

        [Tooltip("Off for the brute: he never waits for an attack token and never peels off.")]
        [SerializeField] private bool _takesTurns = true;

        [Header("Movement")]
        [SerializeField] private float _maxSpeed = 3f;
        [SerializeField] private float _acceleration = 40f;
        [SerializeField] private float _deceleration = 60f;
        [SerializeField] private float _depthSpeedScale = 0.85f;

        [Tooltip("Hop launch speed; only matters when hop pulses are authored. 0 disables jumping.")]
        [SerializeField] private float _jumpSpeed = 0f;

        [Header("Vitals and rewards")]
        [SerializeField] private float _maxHealth = 30f;

        [Tooltip("D22's composition ladder position; feeds D23's drop chance. A number for now.")]
        [SerializeField] private int _rank = 1;

        [Tooltip("D24 payload on the death event; nothing consumes it until M4's ladder.")]
        [SerializeField] private int _xpReward = 10;

        [Header("Elemental resistances (§4 — one row per element that is not neutral)")]
        [SerializeField] private ElementMultiplierSpec[] _resistances = new ElementMultiplierSpec[0];

        public EnemySpec ToRuntime() => new EnemySpec(
            new EnemyTuning(
                _archetype,
                _attack.ToRuntime(),
                Mathf.Max(1, _cooldownSteps),
                _interruptible,
                Mathf.Max(1, _staggerSteps),
                _element != null ? _element.Id : ElementId.None,
                Mathf.Max(0.1f, _projectileSpeed),
                Mathf.Max(0f, _standoffNearX),
                Mathf.Max(_standoffNearX, _standoffFarX),
                Mathf.Max(0f, _hoverDistanceX),
                Mathf.Max(0, _strafePeriodSteps),
                Mathf.Max(0, _hopPulseSteps),
                _takesTurns),
            new MovementTuning(
                Mathf.Max(0.1f, _maxSpeed),
                Mathf.Max(1f, _acceleration),
                Mathf.Max(1f, _deceleration),
                Mathf.Clamp01(_depthSpeedScale),
                gravity: 45f,
                jumpSpeed: Mathf.Max(0f, _jumpSpeed),
                maxFallSpeed: 30f,
                coyoteSteps: 1,
                jumpBufferSteps: 1),
            Mathf.Max(1f, _maxHealth),
            ElementMultiplierSpec.ToTable(_resistances),
            Mathf.Max(1, _rank),
            Mathf.Max(0, _xpReward));
    }
}
