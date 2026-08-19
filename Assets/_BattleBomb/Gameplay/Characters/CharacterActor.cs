using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Players;
using BattleBomb.Core.Spatial;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Gameplay.Characters
{
    /// <summary>
    /// What one player step produced beyond motion: a completed revive (and the health fraction
    /// the reviver's accuracy earned), or a deliberate loot grab (D30). The driver acts on both —
    /// one actor never rewrites another, and only the driver knows which pickup was in reach.
    /// </summary>
    internal readonly struct ActorStepResult
    {
        public readonly int RevivedPartner;
        public readonly float ReviveFraction;
        public readonly bool GrabbedLoot;

        public ActorStepResult(int revivedPartner, float reviveFraction, bool grabbedLoot)
        {
            RevivedPartner = revivedPartner;
            ReviveFraction = reviveFraction;
            GrabbedLoot = grabbedLoot;
        }
    }

    /// <summary>
    /// The simulation's view of one character: it owns the motor state and writes the transform.
    /// <see cref="Step"/> is internal on purpose — Presentation and UI reference this assembly, and
    /// internal is the compiler-level guarantee they can observe but never drive it (§3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterActor : MonoBehaviour
    {
        [SerializeField] private CharacterDefinition _definition;

        [Tooltip("Player slot used when no command source sits on this object.")]
        [SerializeField] private int _playerIndex;

        [Tooltip("Driver this character registers with. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        private IPlayerCommandSource _source;
        private MovementTuning _tuning;
        private CombatKit _kit;
        private MotorState _state;
        private MotorState _previous;
        private CombatState _combat = CombatState.Ready;
        private Vector3 _lungePerStep;
        private int _lungeStepsLeft;
        private bool _attackRooted;
        private Vector3 _strikeMomentum;
        private PlayerCondition _condition;
        private int _hitStaggerSteps;
        private int _hitGraceSteps;
        private ReviveChannel _revive = ReviveChannel.Inactive;
        private float _reviveRequiredProgress = 10f;
        private int _reviveBeatSteps = 45;
        private int _reviveRushSteps = 15;
        private int _revivePumpDecaySteps = 60;
        private float _reviveMinHealthFraction = 0.25f;
        private float _reviveMaxHealthFraction = 0.65f;
        private float _reviveRange = 1.8f;
        private int _reviveGraceSteps = 60;
        private Vector3 _spawnPosition;
        private bool _spawnCaptured;

        /// <summary>
        /// Read live from the command source: PlayerInput assigns its player index after sibling
        /// OnEnable runs, so a snapshot taken there would freeze the wrong id.
        /// </summary>
        public PlayerId PlayerId => _source != null ? _source.PlayerId : new PlayerId(_playerIndex);
        public Vector3 Position => _state.Position;
        public Vector3 PreviousPosition => _previous.Position;
        public Facing Facing => _state.Facing;
        public bool IsGrounded => _state.IsGrounded;

        public AttackPhase CombatPhase => _combat.Phase;
        public AttackTuning CurrentAttack => _combat.CurrentAttack;
        public PlayerCondition Condition => _condition;

        /// <summary>The revive channel this player is running, for the HUD to draw (task 35).</summary>
        public ReviveChannel Revive => _revive;

        /// <summary>Channel progress 0–1, for the HUD's bar — it visibly drains in silence.</summary>
        public float ReviveProgress => _revive.IsActive
            ? Mathf.Clamp01(_revive.Progress / _reviveRequiredProgress)
            : 0f;

        /// <summary>The heartbeat's period, so the HUD's pulse beats on the accuracy clock (D31).</summary>
        public int ReviveBeatSteps => _reviveBeatSteps;

        internal float ReviveRange => _reviveRange;
        public float ChargeFraction => _kit == null || _kit.ChargeThresholdSteps <= 0
            ? 0f
            : Mathf.Clamp01((float)_combat.ChargeSteps / _kit.ChargeThresholdSteps);

        /// <summary>
        /// One fixed step. <paramref name="reviveTarget"/> is the downed partner in revive range
        /// this step (or -1) and <paramref name="lootInReach"/> whether a drop sits in grab range —
        /// both computed by the driver, which alone acts on the result. Contextual Light resolves
        /// in priority order: the revive channel first (D25), then the loot grab (D30), then the
        /// swing.
        /// </summary>
        internal ActorStepResult Step(
            int frame, in PlayerCommand command, in ArenaBounds bounds, float dt,
            int reviveTarget, bool lootInReach)
        {
            float reviveFraction = 0f;
            bool grabbedLoot = false;
            _previous = _state;

            // Staggered or downed, the player's intent goes nowhere — the body still obeys
            // physics (knockback, gravity), it just takes no orders (task 29).
            PlayerCommand effective = _condition.InControl ? command : PlayerCommand.Idle(frame);

            bool frozen = _combat.HitstopSteps > 0;
            int completedRevive = -1;
            if (!frozen)
            {
                // Contextual Light (D17/D25): beside a downed partner the press channels instead
                // of swinging, and only from combat-Ready — a swing or charge in flight keeps its
                // buttons. While the channel runs, attack presses never reach the machine; each
                // press earns by its heartbeat timing, and accuracy prices the revive (D31).
                bool mayChannel = _condition.InControl && _combat.Phase == AttackPhase.Ready;
                _revive = ReviveChannel.Next(
                    _revive,
                    (effective.Pressed & CommandButtons.Light) != 0,
                    reviveTarget,
                    mayChannel,
                    _reviveBeatSteps,
                    _reviveRushSteps,
                    _revivePumpDecaySteps);
                if (_revive.IsComplete(_reviveRequiredProgress))
                {
                    completedRevive = _revive.TargetIndex;
                    reviveFraction = ReviveChannel.RestoredFraction(
                        _revive.AverageAccuracy,
                        _reviveMinHealthFraction, _reviveMaxHealthFraction);
                    _revive = ReviveChannel.Inactive;
                }
            }

            if (_revive.IsActive)
            {
                effective = WithoutAttacks(effective);
            }

            // The deliberate grab (D30): Light beside a drop takes it instead of swinging, but
            // never while a revive channel holds the press — the partner outranks the loot.
            if (!frozen && lootInReach && !_revive.IsActive && completedRevive < 0
                && _condition.InControl && _combat.Phase == AttackPhase.Ready
                && (effective.Pressed & CommandButtons.Light) != 0)
            {
                grabbedLoot = true;
                effective = WithoutLight(effective);
            }

            CombatStepResult combat = CombatMachine.Step(_combat, effective, _kit, _state.IsGrounded);
            _combat = combat.State;
            if (frozen)
            {
                // Hitstop freezes the whole character — the machine above only counted it down.
                return new ActorStepResult(completedRevive, reviveFraction, grabbedLoot);
            }

            _condition = _condition.Step();

            if (combat.AttackStarted)
            {
                BeginAttack(effective, combat.Attack);
            }

            AttackPhase phase = _combat.Phase;
            if (phase == AttackPhase.Ready)
            {
                _attackRooted = false;
                _lungeStepsLeft = 0;
                _state = CharacterMotor.Step(_state, effective, _tuning, bounds, dt);
            }
            else if (_attackRooted)
            {
                // In lunge range the snap owns the body: scoot toward the target, then hold.
                StepLungeMotion(bounds);
            }
            else
            {
                // Out of range nothing roots (Michael's playtest): Lights move freely, Heavies
                // and charging at their reduced speed, and an airborne swing hangs — vertical
                // speed stays zeroed through startup and the hit window, recovery falls normally.
                // The slam is the exception: it dives instead of hanging.
                bool stallGravity = !_state.IsGrounded
                    && (phase == AttackPhase.Startup || phase == AttackPhase.Active)
                    && !_combat.CurrentAttack.ResolvesOnLanding;
                MovementTuning tuning = stallGravity ? WithoutGravity(_tuning) : _tuning;
                _state = CharacterMotor.Step(_state, CombatMove(effective, phase), tuning, bounds, dt);
                StepAirLunge(bounds);
            }

            if (combat.HitWindowOpened && _driver != null)
            {
                _driver.ResolveHits(this, combat.Attack);
            }

            transform.position = _state.Position;
            return new ActorStepResult(completedRevive, reviveFraction, grabbedLoot);
        }

        internal void ApplyHitstop(int steps) => _combat = _combat.WithHitstop(steps);

        /// <summary>
        /// An enemy's landed hit — the seam M3's enemies call (tasks 31–33). Grace and the downed
        /// state swallow it whole; otherwise damage, stagger, the shove, and the victim's hitstop
        /// land together, and a damaging hit interrupts whatever swing was in flight.
        /// </summary>
        internal void ApplyEnemyHit(in HitResult hit)
        {
            if (_condition.IsInvulnerable)
            {
                return;
            }

            _condition = _condition.Hit(hit.Damage, _hitStaggerSteps, _hitGraceSteps);
            if (hit.Damage > 0f)
            {
                _combat = CombatState.Ready;
            }

            ApplyImpulse(hit.Impulse);
            ApplyHitstop(hit.HitstopSteps);
        }

        /// <summary>A completed partner channel stands this player back up (D25); the fraction
        /// is what the reviver's mash pace earned (D29). Grace is this player's own.</summary>
        internal void ApplyRevive(float healthFraction) =>
            _condition = _condition.Revived(healthFraction, _reviveGraceSteps);

        /// <summary>
        /// The attempt-over sandbox reset (task 35): back to the spawn point, full health, clean
        /// combat state. A placeholder by design — the run lifecycle is mode-owned (D4, M7).
        /// </summary>
        internal void ResetForAttempt()
        {
            _condition = PlayerCondition.Fresh(_definition != null ? _definition.MaxHealth : 100f);
            _combat = CombatState.Ready;
            _revive = ReviveChannel.Inactive;
            _lungePerStep = Vector3.zero;
            _lungeStepsLeft = 0;
            _attackRooted = false;
            _state = MotorState.AtRest(_spawnPosition);
            _previous = _state;
            transform.position = _spawnPosition;
        }

        /// <summary>A partner's shove (D21): replaces velocity, never touches health.</summary>
        internal void ApplyImpulse(Vector3 velocity)
        {
            bool launched = velocity.y > 0.01f;
            _state = new MotorState(
                _state.Position,
                velocity,
                _state.Facing,
                launched ? false : _state.IsGrounded,
                launched ? _tuning.CoyoteSteps + 1 : _state.StepsSinceGrounded,
                _state.JumpBufferedFor);
        }

        /// <summary>
        /// The planar speed carried into the current swing, captured before any lunge zeroes it.
        /// Feeds the momentum-knockback bonus — the scripted snap itself never counts as momentum.
        /// </summary>
        internal Vector3 StrikeMomentum => _strikeMomentum;

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

        private void BeginAttack(in PlayerCommand command, in AttackTuning attack)
        {
            _strikeMomentum = new Vector3(_state.Velocity.x, 0f, _state.Velocity.z);

            // The stick aims (D19). Chosen here; a free-moving whiff may still re-aim by moving.
            Facing facing = command.Move.x > 0.01f ? Facing.Right
                : command.Move.x < -0.01f ? Facing.Left
                : _state.Facing;

            _lungePerStep = Vector3.zero;
            _lungeStepsLeft = 0;
            _attackRooted = false;

            Vector3 velocity = _state.Velocity;
            if (!_state.IsGrounded)
            {
                // The air stall: the swing hangs, and recovery resumes the fall from zero.
                // The slam dives at full fall speed instead — the shadow marks where it ends.
                velocity.y = attack.ResolvesOnLanding ? -_tuning.MaxFallSpeed : 0f;
            }

            // The slam never lunges: its aim is the shadow mark (D14), not magnetism.
            Vector3 target = default;
            bool hasTarget = !attack.ResolvesOnLanding && _driver != null
                && _driver.TryPickLungeTarget(this, attack, out target);
            if (hasTarget)
            {
                Vector3 travel = HitResolver.LungeEnd(_state.Position, attack, target) - _state.Position;
                if (travel.sqrMagnitude >= 1e-4f)
                {
                    int steps = Mathf.Max(1, attack.StartupSteps);
                    _lungePerStep = travel / steps;
                    _lungeStepsLeft = steps;
                }

                // Grounded the snap roots; airborne it sheds carried momentum instead, so the
                // scoot pulls at the grounded rate over the grounded range — consistency over
                // overshoot (Michael's retest) — while drift-steering and the fall stay live.
                _attackRooted = _state.IsGrounded;
                velocity = Vector3.zero;
            }

            _state = new MotorState(
                _state.Position, velocity, facing, _state.IsGrounded, _state.StepsSinceGrounded,
                _state.JumpBufferedFor);
        }

        /// <summary>
        /// The air lunge: the grounded snap's travel applied over the free swing — velocity and
        /// stick drift stay live, so the scoot converts the hit without the rooted feel.
        /// </summary>
        private void StepAirLunge(in ArenaBounds bounds)
        {
            if (_lungeStepsLeft <= 0)
            {
                return;
            }

            _lungeStepsLeft -= 1;
            Vector3 position = bounds.ClampHorizontal(_state.Position + _lungePerStep);
            _state = new MotorState(
                position, _state.Velocity, _state.Facing, _state.IsGrounded,
                _state.StepsSinceGrounded, _state.JumpBufferedFor);
        }

        private void StepLungeMotion(in ArenaBounds bounds)
        {
            if (_lungeStepsLeft <= 0)
            {
                return;
            }

            _lungeStepsLeft -= 1;
            Vector3 position = bounds.ClampHorizontal(_state.Position + _lungePerStep);
            _state = new MotorState(
                position, Vector3.zero, _state.Facing, _state.IsGrounded, _state.StepsSinceGrounded,
                _state.JumpBufferedFor);
        }

        private static PlayerCommand WithoutAttacks(in PlayerCommand command) => new PlayerCommand(
            command.Frame,
            command.Move,
            command.Held & ~(CommandButtons.Light | CommandButtons.Heavy),
            command.Pressed & ~(CommandButtons.Light | CommandButtons.Heavy),
            command.Released & ~(CommandButtons.Light | CommandButtons.Heavy));

        private static PlayerCommand WithoutLight(in PlayerCommand command) => new PlayerCommand(
            command.Frame,
            command.Move,
            command.Held & ~CommandButtons.Light,
            command.Pressed & ~CommandButtons.Light,
            command.Released & ~CommandButtons.Light);

        private PlayerCommand CombatMove(in PlayerCommand command, AttackPhase phase)
        {
            float scale = phase == AttackPhase.Charging
                ? _kit.Heavy.MoveSpeedScale
                : _combat.CurrentAttack.MoveSpeedScale;
            return new PlayerCommand(
                command.Frame,
                command.Move * scale,
                command.Held & ~CommandButtons.Jump,
                command.Pressed & ~CommandButtons.Jump,
                command.Released & ~CommandButtons.Jump);
        }

        private static MovementTuning WithoutGravity(in MovementTuning tuning) => new MovementTuning(
            tuning.MaxSpeed,
            tuning.Acceleration,
            tuning.Deceleration,
            tuning.DepthSpeedScale,
            0f,
            tuning.JumpSpeed,
            tuning.MaxFallSpeed,
            tuning.CoyoteSteps,
            tuning.JumpBufferSteps);

        private void Awake()
        {
            _source = GetComponent<IPlayerCommandSource>();
            _tuning = _definition != null ? _definition.ToRuntime() : MovementTuning.Default;
            _kit = _definition != null ? _definition.CombatKitToRuntime() : CombatKit.Default;
            _condition = PlayerCondition.Fresh(_definition != null ? _definition.MaxHealth : 100f);
            _hitStaggerSteps = _definition != null ? _definition.HitStaggerSteps : 15;
            _hitGraceSteps = _definition != null ? _definition.HitGraceSteps : 30;
            _reviveRequiredProgress = _definition != null ? _definition.ReviveRequiredProgress : 10f;
            _reviveBeatSteps = _definition != null ? _definition.ReviveBeatSteps : 45;
            _reviveRushSteps = _definition != null ? _definition.ReviveRushSteps : 15;
            _revivePumpDecaySteps = _definition != null ? _definition.RevivePumpDecaySteps : 60;
            _reviveMinHealthFraction = _definition != null ? _definition.ReviveMinHealthFraction : 0.25f;
            _reviveMaxHealthFraction = _definition != null ? _definition.ReviveMaxHealthFraction : 0.65f;
            _reviveRange = _definition != null ? _definition.ReviveRange : 1.8f;
            _reviveGraceSteps = _definition != null ? _definition.ReviveGraceSteps : 60;
            _state = MotorState.AtRest(transform.position);
            _previous = _state;
        }

        private void OnEnable()
        {
            if (!_spawnCaptured)
            {
                _spawnPosition = transform.position;
                _spawnCaptured = true;
            }

            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (_driver == null)
            {
                Debug.LogError($"{name}: no SimulationDriver in the scene — this character will never move.", this);
                return;
            }

            _driver.Characters.Register(this);
        }

        private void OnDisable()
        {
            if (_driver != null)
            {
                _driver.Characters.Unregister(this);
            }
        }
    }
}
