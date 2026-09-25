using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Items;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Core.Spatial;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Gameplay.Characters
{
    /// <summary>
    /// One spawned enemy: the Core brain choosing intent, the same motor players use obeying it
    /// (D10), and health in the target registry so player attacks find it. The dummy pattern
    /// promoted. Depleted enemies stand inert until task 36 gives them a real death.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyActor : MonoBehaviour, ISimTarget
    {
        private const float TargetSwitchMargin = 1.5f;

        /// <summary>Steps a corpse lingers before despawning (task 36) — the fade is presentation's.</summary>
        private const int DeathBeatSteps = 45;

        [Tooltip("Authored archetype this enemy runs. The spawner assigns it on spawn.")]
        [SerializeField] private EnemyDefinition _definition;

        [Tooltip("Driver this enemy registers with. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        private EnemySpec _spec;
        private bool _configured;
        private MotorState _state;
        private MotorState _previous;
        private EnemyState _brain = EnemyState.Fresh;
        private Health _health;
        private int _targetIndex = -1;
        private Vector3 _strikeMomentum;
        private int _depletedSteps;
        private bool _deathReported;
        private bool _isElite;
        private ItemInstance _carriedDrop;

        public Component Body => this;
        public Vector3 Position => _state.Position;
        public Vector3 PreviousPosition => _previous.Position;
        public Facing Facing => _state.Facing;
        public Health Health => _health;
        public bool IsDepleted => !_configured || _health.IsDepleted;
        public bool IsConfigured => _configured;
        public EnemySpec Spec => _spec;

        /// <summary>D22's rare modifier: tougher, and it drops what it wears.</summary>
        public bool IsElite => _isElite;

        /// <summary>This enemy's id on the wire: unique for the whole launch and never reused
        /// (HANDOFF-M8 planning decision 8). Zero for an enemy placed by hand in a scene.</summary>
        public int NetId { get; private set; }

        /// <summary>The stage and roster slot this enemy was spawned from — how the guest finds the
        /// same authored archetype.</summary>
        internal int OriginStage { get; private set; } = -1;

        internal int OriginRoster { get; private set; } = -1;

        internal void SetOrigin(int netId, int stageIndex, int rosterIndex)
        {
            NetId = netId;
            OriginStage = stageIndex;
            OriginRoster = rosterIndex;
        }

        /// <summary>The piece an elite is wearing — Presentation tints its armor this item's
        /// quality colour, and the kill hands over exactly this.</summary>
        public ItemInstance CarriedDrop => _carriedDrop;
        public EnemyPhase Phase => _brain.Phase;

        /// <summary>How far into the telegraph, 0–1 — the tell Presentation ramps on (task 34).</summary>
        public float TelegraphFraction => _brain.Phase == EnemyPhase.Telegraph
            && _spec.Tuning.Attack.StartupSteps > 0
                ? Mathf.Clamp01((float)_brain.StepsInPhase / _spec.Tuning.Attack.StartupSteps)
                : 0f;

        internal Vector3 StrikeMomentum => _strikeMomentum;
        internal ElementalMultipliers Resistances => _spec.Resistances;

        /// <summary>Resistances plus its own element — what every damage resolution asks for (D40).</summary>
        internal ElementalDefence Defence => _spec.Defence;

        /// <summary>The elements currently marking it (D40). Presentation reads; the driver ticks.</summary>
        public StatusTrack Statuses { get; } = new StatusTrack();
        internal int TargetIndex => _targetIndex;

        /// <summary>A melee turn-taker competes for the per-target attack token (D28).</summary>
        internal bool TakesMeleeTurns =>
            _configured && !_spec.Tuning.FightsAtRange && _spec.Tuning.TakesTurns;

        /// <summary>Holding an attack token right now — mid-telegraph or mid-swing.</summary>
        internal bool IsAttacking =>
            _brain.Phase == EnemyPhase.Telegraph || _brain.Phase == EnemyPhase.Active;

        /// <summary>
        /// The spawner assigns the authored archetype right after instantiating. An elite (D22)
        /// arrives already promoted and already carrying the piece it will drop — the fight
        /// advertises its own reward, so the reward has to exist before the fight does.
        /// </summary>
        internal void Configure(
            EnemyDefinition definition, int seed = 0,
            bool isElite = false, in ItemInstance carriedDrop = default,
            float healthMultiplier = 1f, float damageMultiplier = 1f)
        {
            _definition = definition;
            _spec = EnemyScaling.Scale(definition.ToRuntime(), healthMultiplier, damageMultiplier);
            _isElite = isElite;
            _carriedDrop = carriedDrop;
            if (isElite)
            {
                // The tier toughens first, then the elite toughens that (D22 on top of D50).
                _spec = EliteSpec.Promote(_spec, EliteRules.Default);
            }

            _health = new Health(_spec.MaxHealth);
            _brain = EnemyState.Seeded(seed);
            _targetIndex = -1;
            _state = MotorState.AtRest(transform.position);
            _previous = _state;
            _configured = true;
        }

        /// <summary>Returns true when a melee attack started this step, so the driver's token
        /// count stays honest within the step (D28).</summary>
        internal bool Step(
            int frame,
            IReadOnlyList<Vector3> playerPositions,
            IReadOnlyList<bool> playerDowned,
            bool mayAttack,
            in ArenaBounds bounds,
            float dt)
        {
            _previous = _state;
            if (!_configured)
            {
                return false;
            }

            if (_health.IsDepleted)
            {
                // The dying beat: still physical, so a killing blow's knockback lands and
                // settles before the driver despawns the corpse and rolls the drop (task 36).
                _depletedSteps += 1;
                _state = CharacterMotor.Step(_state, PlayerCommand.Idle(frame), _spec.Movement, bounds, dt);
                transform.position = _state.Position;
                return false;
            }

            bool frozen = _brain.HitstopSteps > 0;
            if (frozen)
            {
                _brain = EnemyBrain.Step(_brain, EnemyPerception.NoTarget(_state.Position), _spec.Tuning).State;
                return false;
            }

            _targetIndex = TargetSelection.Choose(
                _state.Position, playerPositions, playerDowned, _targetIndex, TargetSwitchMargin);
            EnemyPerception view = _targetIndex >= 0
                ? new EnemyPerception(_state.Position, true, playerPositions[_targetIndex], mayAttack)
                : EnemyPerception.NoTarget(_state.Position);

            EnemyStepResult result = EnemyBrain.Step(_brain, view, _spec.Tuning);
            _brain = result.State;

            if (result.AttackStarted && _targetIndex >= 0)
            {
                _strikeMomentum = new Vector3(_state.Velocity.x, 0f, _state.Velocity.z);
                Facing facing = playerPositions[_targetIndex].x >= _state.Position.x
                    ? Facing.Right
                    : Facing.Left;
                _state = new MotorState(
                    _state.Position, _state.Velocity, facing, _state.IsGrounded,
                    _state.StepsSinceGrounded, _state.JumpBufferedFor);
            }

            CommandButtons held = result.JumpRequested ? CommandButtons.Jump : CommandButtons.None;
            float slow = Statuses.MoveScale;
            PlayerCommand intent = PlayerCommand.FromState(
                frame, slow < 1f ? result.MoveIntent * slow : result.MoveIntent, held,
                CommandButtons.None);
            _state = CharacterMotor.Step(_state, intent, _spec.Movement, bounds, dt);

            if (result.HitWindowOpened && _driver != null)
            {
                _driver.ResolveEnemyMelee(this, _spec.Tuning.Attack);
            }

            if (result.ProjectileFired && _targetIndex >= 0 && _driver != null)
            {
                _driver.SpawnProjectile(this, playerPositions[_targetIndex]);
            }

            transform.position = _state.Position;
            return result.AttackStarted && !_spec.Tuning.FightsAtRange;
        }

        /// <summary>A player's landed hit: pipeline damage, knockback, hitstop, and the flinch.</summary>
        internal void ApplyHit(in HitResult hit)
        {
            _health = _health.Damaged(hit.Damage);

            bool launched = hit.Impulse.y > 0.01f;
            _state = new MotorState(
                _state.Position,
                hit.Impulse,
                _state.Facing,
                launched ? false : _state.IsGrounded,
                launched ? _spec.Movement.CoyoteSteps + 1 : _state.StepsSinceGrounded,
                0);

            _brain = _brain.WithHitstop(hit.HitstopSteps);
            _brain = EnemyBrain.Interrupted(_brain, _spec.Tuning);
        }

        /// <summary>
        /// One status tick (D40): health only. It never flinches the enemy and never interrupts a
        /// telegraph — a burning brute still lands his swing, which is what keeps the mark a
        /// damage source rather than a stunlock.
        /// </summary>
        internal void ApplyStatusDamage(float damage)
        {
            if (damage <= 0f || _health.IsDepleted)
            {
                return;
            }

            _health = _health.Damaged(damage);
            if (_health.IsDepleted)
            {
                Statuses.Clear();
            }
        }

        /// <summary>
        /// A reaction's hold (D41). Unlike a hit's flinch this ignores the brute's uninterruptible
        /// flag — earning a reaction is a deliberate two-element setup, and it should beat the one
        /// enemy who shrugs off everything else.
        /// </summary>
        internal void ApplyStun(int steps)
        {
            if (steps <= 0 || _health.IsDepleted)
            {
                return;
            }

            _brain = EnemyBrain.Interrupted(_brain, _spec.Tuning);
            _brain = _brain.WithHitstop(steps);
        }

        internal void ApplyHitstop(int steps) => _brain = _brain.WithHitstop(steps);

        internal Vector3 Velocity => _state.Velocity;

        /// <summary>The crowding nudge (task 37): position only — velocity is never cushioned.</summary>
        internal void ApplySeparation(Vector3 push, in ArenaBounds bounds)
        {
            if (push.sqrMagnitude < 1e-12f)
            {
                return;
            }

            Vector3 position = bounds.ClampHorizontal(_state.Position + push);
            _state = new MotorState(
                position, _state.Velocity, _state.Facing, _state.IsGrounded,
                _state.StepsSinceGrounded, _state.JumpBufferedFor);
            transform.position = _state.Position;
        }

        /// <summary>
        /// True exactly once, when the dying beat has run out — the driver's cue to announce the
        /// death, roll the drop, and despawn (task 36).
        /// </summary>
        internal bool ConsumeDeath()
        {
            if (_deathReported || !_configured || !_health.IsDepleted || _depletedSteps < DeathBeatSteps)
            {
                return false;
            }

            _deathReported = true;
            return true;
        }

        internal EnemySnapshot CaptureReplica() => new EnemySnapshot(
            NetId, OriginStage, OriginRoster, _isElite,
            _carriedDrop.IsEmpty ? -1 : (int)_carriedDrop.Quality,
            _state, _brain, _health, _targetIndex, _depletedSteps, Statuses.ToArray());

        private void Awake()
        {
            if (_definition != null)
            {
                Configure(_definition);
            }
            else
            {
                _state = MotorState.AtRest(transform.position);
                _previous = _state;
            }
        }

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_driver == null)
            {
                Debug.LogError($"{name}: no SimulationDriver in the scene — this enemy will never act.", this);
                return;
            }

            _driver.Targets.Register(this);
        }

        private void OnDisable()
        {
            if (_driver != null)
            {
                _driver.Targets.Unregister(this);
            }
        }
    }
}
