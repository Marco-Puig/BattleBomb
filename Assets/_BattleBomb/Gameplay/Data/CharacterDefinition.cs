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

        [Header("Revive (D25)")]
        [Tooltip("Steps this character's revive channel takes when rescuing a partner.")]
        [SerializeField] private int _reviveChannelSteps = 90;

        [Tooltip("Planar range within which Light becomes the revive instead of an attack.")]
        [SerializeField] private float _reviveRange = 1.8f;

        [Tooltip("Health fraction this character stands back up with when revived.")]
        [SerializeField] private float _reviveHealthFraction = 0.5f;

        [Tooltip("Steps of invulnerability granted on being revived.")]
        [SerializeField] private int _reviveGraceSteps = 60;

        public string DisplayName => _displayName;

        public float MaxHealth => Mathf.Max(1f, _maxHealth);

        public int HitStaggerSteps => Mathf.Max(0, _hitStaggerSteps);

        public int HitGraceSteps => Mathf.Max(0, _hitGraceSteps);

        public int ReviveChannelSteps => Mathf.Max(1, _reviveChannelSteps);

        public float ReviveRange => Mathf.Max(0f, _reviveRange);

        public float ReviveHealthFraction => Mathf.Clamp01(_reviveHealthFraction);

        public int ReviveGraceSteps => Mathf.Max(0, _reviveGraceSteps);

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
