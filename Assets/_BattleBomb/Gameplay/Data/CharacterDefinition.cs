using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;
using UnityEngine;

namespace BattleBomb.Gameplay.Data
{
    /// <summary>
    /// Authoring format for a character (§5, pillar 3): a data asset in, a plain struct out. Core
    /// only ever sees the <see cref="MovementTuning"/> this produces.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleBomb/Character Definition", fileName = "CharacterDefinition")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Unnamed";

        [Header("Movement")]
        [SerializeField] private float _maxSpeed = 6f;
        [SerializeField] private float _acceleration = 60f;
        [SerializeField] private float _deceleration = 80f;
        [SerializeField] private float _depthSpeedScale = 0.85f;

        [Header("Jumping")]
        [SerializeField] private float _gravity = 45f;
        [SerializeField] private float _jumpSpeed = 12f;
        [SerializeField] private float _maxFallSpeed = 30f;
        [SerializeField] private int _coyoteSteps = 6;
        [SerializeField] private int _jumpBufferSteps = 6;

        [Header("Combat")]
        [Tooltip("The character's combo table. Leave empty to use the default kit.")]
        [SerializeField] private CombatKitDefinition _combatKit;

        [Header("Vitals")]
        [SerializeField] private float _maxHealth = 100f;

        [Tooltip("Steps of lost control after taking a hit.")]
        [SerializeField] private int _hitStaggerSteps = 15;

        [Tooltip("Steps of post-hit invulnerability. Longer than the stagger, so a crowd can never stunlock.")]
        [SerializeField] private int _hitGraceSteps = 30;

        public string DisplayName => _displayName;

        public float MaxHealth => Mathf.Max(1f, _maxHealth);

        public int HitStaggerSteps => Mathf.Max(0, _hitStaggerSteps);

        public int HitGraceSteps => Mathf.Max(0, _hitGraceSteps);

        public CombatKit CombatKitToRuntime() =>
            _combatKit != null ? _combatKit.ToRuntime() : CombatKit.Default;

        public MovementTuning ToRuntime() => new MovementTuning(
            _maxSpeed,
            _acceleration,
            _deceleration,
            _depthSpeedScale,
            _gravity,
            _jumpSpeed,
            _maxFallSpeed,
            _coyoteSteps,
            _jumpBufferSteps);
    }
}
