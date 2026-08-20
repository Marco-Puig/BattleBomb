using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Stats;
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

        [Header("Revive (D25/D31)")]
        [Tooltip("Progress that completes a revive. A press on the beat earns 2, a rushed mash press 0.5.")]
        [SerializeField] private float _reviveRequiredProgress = 10f;

        [Tooltip("Steps per heartbeat — press at the pulse's peak for full accuracy.")]
        [SerializeField] private int _reviveBeatSteps = 45;

        [Tooltip("A press within this many steps of the last counts as rushed: minimum progress, zero accuracy.")]
        [SerializeField] private int _reviveRushSteps = 15;

        [Tooltip("Quiet steps that drain one sloppy chunk; draining to zero drops the channel.")]
        [SerializeField] private int _revivePumpDecaySteps = 60;

        [Tooltip("Health fraction a pure mash restores.")]
        [SerializeField] private float _reviveMinHealthFraction = 0.25f;

        [Tooltip("Health fraction a perfectly timed revive restores.")]
        [SerializeField] private float _reviveMaxHealthFraction = 0.65f;

        [Tooltip("Planar range within which Light becomes the revive instead of an attack.")]
        [SerializeField] private float _reviveRange = 1.8f;

        [Tooltip("Steps of invulnerability granted on being revived.")]
        [SerializeField] private int _reviveGraceSteps = 60;

        [Header("Stats (D32)")]
        [Tooltip("Max health each allocated HP point adds.")]
        [SerializeField] private float _healthPerPoint = 5f;

        [Tooltip("Mana capacity before any points or gear.")]
        [SerializeField] private float _baseMaxMana = 100f;

        [Tooltip("Mana capacity each allocated Mana point adds.")]
        [SerializeField] private float _manaPerPoint = 5f;

        [Tooltip("Mana per second before gear regen (regen is a gear stat, D32).")]
        [SerializeField] private float _baseManaRegen = 1f;

        [Tooltip("Bare-hands weapon damage — the baseline the authored attack numbers assume.")]
        [SerializeField] private float _unarmedDamage = 10f;

        [Tooltip("Fractional weapon-damage bonus per Strength point (0.01 = +1%).")]
        [SerializeField] private float _damagePerStrengthPoint = 0.01f;

        [Tooltip("Fractional move-speed bonus per Speed point, until the cap.")]
        [SerializeField] private float _moveSpeedPerPoint = 0.005f;

        [Tooltip("The hard velocity cap as a bonus fraction — Speed can never push past it.")]
        [SerializeField] private float _moveSpeedCapBonus = 0.10f;

        [Tooltip("Slow resistance each over-cap Speed point buys.")]
        [SerializeField] private float _slowResistPerOverCapPoint = 0.02f;

        [Tooltip("Ceiling on slow resistance — slows always matter a little.")]
        [SerializeField] private float _slowResistCap = 0.75f;

        [Tooltip("Ceiling on summed defence — the max-tank build's wall (D35).")]
        [SerializeField] private float _defenceCap = 0.70f;

        [Tooltip("Move-speed slow per unit of worn weight. Raised from 0.005 at the mega pass — one chestplate must be felt, not just read.")]
        [SerializeField] private float _slowPerWeight = 0.012f;

        [Tooltip("Damage multiplier a critical hit starts from; crit-damage affixes add to it.")]
        [SerializeField] private float _baseCritDamageMultiplier = 1.5f;

        [Tooltip("Steps the quick-use slot rests after firing (D37).")]
        [SerializeField] private int _quickUseCooldownSteps = 180;

        public string DisplayName => _displayName;

        public float MaxHealth => Mathf.Max(1f, _maxHealth);

        public int HitStaggerSteps => Mathf.Max(0, _hitStaggerSteps);

        public int HitGraceSteps => Mathf.Max(0, _hitGraceSteps);

        public float ReviveRequiredProgress => Mathf.Max(1f, _reviveRequiredProgress);

        public int ReviveBeatSteps => Mathf.Max(2, _reviveBeatSteps);

        public int ReviveRushSteps => Mathf.Max(0, _reviveRushSteps);

        public int RevivePumpDecaySteps => Mathf.Max(0, _revivePumpDecaySteps);

        public float ReviveMinHealthFraction => Mathf.Clamp01(_reviveMinHealthFraction);

        public float ReviveMaxHealthFraction =>
            Mathf.Clamp(_reviveMaxHealthFraction, ReviveMinHealthFraction, 1f);

        public float ReviveRange => Mathf.Max(0f, _reviveRange);

        public int ReviveGraceSteps => Mathf.Max(0, _reviveGraceSteps);

        public int QuickUseCooldownSteps => Mathf.Max(0, _quickUseCooldownSteps);

        public CombatKit CombatKitToRuntime() =>
            _combatKit != null ? _combatKit.ToRuntime() : CombatKit.Default;

        /// <summary>The D32 per-point values, with this character's vitals as the base.</summary>
        public StatTuning ToStatTuning() => new StatTuning(
            MaxHealth,
            _healthPerPoint,
            _baseMaxMana,
            _manaPerPoint,
            _baseManaRegen,
            _unarmedDamage,
            _damagePerStrengthPoint,
            _moveSpeedPerPoint,
            _moveSpeedCapBonus,
            _slowResistPerOverCapPoint,
            _slowResistCap,
            _defenceCap,
            _slowPerWeight,
            _baseCritDamageMultiplier);

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
