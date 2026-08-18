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
        public float ChargeFraction => _kit == null || _kit.ChargeThresholdSteps <= 0
            ? 0f
            : Mathf.Clamp01((float)_combat.ChargeSteps / _kit.ChargeThresholdSteps);

        internal void Step(int frame, in PlayerCommand command, in ArenaBounds bounds, float dt)
        {
            _previous = _state;

            bool frozen = _combat.HitstopSteps > 0;
            CombatStepResult combat = CombatMachine.Step(_combat, command, _kit, _state.IsGrounded);
            _combat = combat.State;
            if (frozen)
            {
                // Hitstop freezes the whole character — the machine above only counted it down.
                return;
            }

            if (combat.AttackStarted)
            {
                BeginAttack(command, combat.Attack);
            }

            AttackPhase phase = _combat.Phase;
            if (phase == AttackPhase.Ready)
            {
                _attackRooted = false;
                _lungeStepsLeft = 0;
                _state = CharacterMotor.Step(_state, command, _tuning, bounds, dt);
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
                _state = CharacterMotor.Step(_state, CombatMove(command, phase), tuning, bounds, dt);
                StepAirLunge(bounds);
            }

            if (combat.HitWindowOpened && _driver != null)
            {
                _driver.ResolveHits(this, combat.Attack);
            }

            transform.position = _state.Position;
        }

        internal void ApplyHitstop(int steps) => _combat = _combat.WithHitstop(steps);

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

        private void BeginAttack(in PlayerCommand command, in AttackTuning attack)
        {
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

                if (_state.IsGrounded)
                {
                    _attackRooted = true;
                    velocity = Vector3.zero;
                }

                // Airborne the same scoot rides on top of the free swing instead of rooting:
                // momentum is kept and the stick still drifts, so overshooting is possible
                // (Michael's playtest — accepted).
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
            _state = MotorState.AtRest(transform.position);
            _previous = _state;
        }

        private void OnEnable()
        {
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
