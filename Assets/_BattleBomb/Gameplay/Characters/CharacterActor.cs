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
        private MotorState _state;
        private MotorState _previous;

        /// <summary>
        /// Read live from the command source: PlayerInput assigns its player index after sibling
        /// OnEnable runs, so a snapshot taken there would freeze the wrong id.
        /// </summary>
        public PlayerId PlayerId => _source != null ? _source.PlayerId : new PlayerId(_playerIndex);
        public Vector3 Position => _state.Position;
        public Vector3 PreviousPosition => _previous.Position;
        public Facing Facing => _state.Facing;
        public bool IsGrounded => _state.IsGrounded;

        internal void Step(int frame, in PlayerCommand command, in ArenaBounds bounds, float dt)
        {
            _previous = _state;
            _state = CharacterMotor.Step(_state, command, _tuning, bounds, dt);
            transform.position = _state.Position;
        }

        private void Awake()
        {
            _source = GetComponent<IPlayerCommandSource>();
            _tuning = _definition != null ? _definition.ToRuntime() : MovementTuning.Default;
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
