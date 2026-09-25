using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Core.Spatial;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Gameplay.Characters
{
    /// <summary>
    /// M2's body to hit: health plus the ordinary character motor stepped with idle commands, so
    /// knockback slides, launches, gravity, and arena clamping all reuse motor code. A depleted
    /// dummy stops being a target for a few seconds, then refills — tuning sessions never need a
    /// scene reload. M3's enemies replace this; the registry and hit flow stay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrainingDummy : MonoBehaviour, ISimTarget
    {
        [SerializeField] private float _maxHealth = 50f;

        [Tooltip("Steps a depleted dummy waits before refilling (180 ≈ 3 s at 60 Hz).")]
        [SerializeField] private int _respawnSteps = 180;

        [Tooltip("Driver this dummy registers with. Leave empty to find the one in the scene.")]
        [SerializeField] private SimulationDriver _driver;

        private MovementTuning _tuning = MovementTuning.Default;
        private MotorState _state;
        private MotorState _previous;
        private Health _health;
        private int _hitstopSteps;
        private int _respawnIn;

        public Component Body => this;
        public Vector3 Position => _state.Position;
        public Vector3 PreviousPosition => _previous.Position;
        public Health Health => _health;
        public bool IsDepleted => _health.IsDepleted;

        /// <summary>Which of its stage's props this dummy is — both machines place props from the same
        /// markers in the same order, so the index names the same dummy on each (planning decision 8).
        /// -1 for a dummy placed by hand in a scene.</summary>
        public int PropIndex { get; private set; } = -1;

        internal void SetPropIndex(int index) => PropIndex = index;

        internal void Step(int frame, in ArenaBounds bounds, float dt)
        {
            _previous = _state;
            if (_hitstopSteps > 0)
            {
                _hitstopSteps -= 1;
                return;
            }

            if (_health.IsDepleted)
            {
                _respawnIn -= 1;
                if (_respawnIn <= 0)
                {
                    _health = _health.Refilled();
                }
            }

            _state = CharacterMotor.Step(_state, PlayerCommand.Idle(frame), _tuning, bounds, dt);
            transform.position = _state.Position;
        }

        internal void ApplyHit(in HitResult hit)
        {
            bool wasDepleted = _health.IsDepleted;
            _health = _health.Damaged(hit.Damage);
            if (_health.IsDepleted && !wasDepleted)
            {
                _respawnIn = Mathf.Max(1, _respawnSteps);
            }

            bool launched = hit.Impulse.y > 0.01f;
            _state = new MotorState(
                _state.Position,
                hit.Impulse,
                _state.Facing,
                launched ? false : _state.IsGrounded,
                launched ? _tuning.CoyoteSteps + 1 : _state.StepsSinceGrounded,
                0);
            _hitstopSteps = Mathf.Max(_hitstopSteps, hit.HitstopSteps);
        }

        internal DummySnapshot CaptureReplica(int stageIndex) => new DummySnapshot(stageIndex, PropIndex, _state, _health);

        private void Awake()
        {
            _health = new Health(Mathf.Max(1f, _maxHealth));
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
                Debug.LogError($"{name}: no SimulationDriver in the scene — this dummy is untouchable.", this);
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
