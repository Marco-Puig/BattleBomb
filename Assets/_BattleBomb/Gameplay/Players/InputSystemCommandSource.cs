using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleBomb.Gameplay.Players
{
    /// <summary>
    /// One couch seat's input, turned into <see cref="PlayerCommand"/>s. With the
    /// <see cref="SeatInput"/> it owns, this is the only code in the project that touches an input
    /// device — everything downstream reads commands (§4, rule 3).
    /// </summary>
    /// <remarks>
    /// The seat is authored, and it is the player's id. It used to be <c>PlayerInput.playerIndex</c>,
    /// which Unity allocates across every PlayerInput alive — including the front door's, which
    /// claimed indices first — so a solo player could come out labelled "P2" (HANDOFF-M7). Which
    /// devices the seat owns comes from the session's <see cref="SeatAssignment"/> (D57).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class InputSystemCommandSource : MonoBehaviour, IPlayerCommandSource, IInputDeviceReport
    {
        [Tooltip("Driver this player registers with. Leave empty to find the driver in the scene, " +
                 "or the command sampler when there is no simulation (the front door).")]
        [SerializeField] private SimulationDriver _driver;

        [Tooltip("The project's controls. Each seat reads its own copy.")]
        [SerializeField] private InputActionAsset _controls;

        [Tooltip("0 for Player 1, 1 for Player 2. This is the player's id, so it is what every " +
                 "P1/P2 label reads.")]
        [SerializeField] private int _seat;

        private readonly SeatAssignment _standIn = new SeatAssignment();
        private IPlayerRegistryHost _host;
        private SeatInput _input;
        private GameSession _session;
        private bool _lookedForSession;
        private int _speakAs = -1;

        public PlayerId PlayerId => new PlayerId(_speakAs >= 0 ? _speakAs : _seat);

        public InputFamily Family => _input != null ? _input.Family : InputFamily.Keyboard;

        public int LastDeviceId => _input != null ? _input.LastDeviceId : SeatAssignment.NoDevice;

        public PlayerCommand Sample(int frame)
        {
            if (_input == null)
            {
                return PlayerCommand.Idle(frame);
            }

            _input.Own(Seats());
            return _input.Sample(frame);
        }

        /// <summary>
        /// Online (HANDOFF-M8 planning decision 18): the device rules of one seat, speaking as
        /// another player. The guest's machine gives its one local player seat 0's rule — every
        /// device is theirs — while they are Player 2 in the host's game. Called before
        /// <c>OnEnable</c> (the binder runs first), because the seat's controls are built there.
        /// </summary>
        internal void UseSeat(int seat, int speakAs)
        {
            _seat = seat;
            _speakAs = speakAs;
        }

        private void OnEnable()
        {
            if (_controls == null)
            {
                Debug.LogError($"{name}: no controls asset — this player will contribute nothing.", this);
                return;
            }

            // Built here rather than in Awake: a mid-play recompile re-runs OnEnable but not Awake.
            _input = new SeatInput(_controls, _seat);
            _lookedForSession = false;

            _host = _driver != null ? _driver : FindHost();
            if (_host == null)
            {
                Debug.LogError($"{name}: nothing to register with — commands will never be sampled.", this);
                return;
            }

            _host.Players.Register(this);
        }

        private void OnDisable()
        {
            // Unregistered through the host it registered with, not the one in the scene now:
            // a scene change can swap the host out from under a source that outlives it.
            _host?.Players.Unregister(PlayerId);
            _host = null;
            _input?.Dispose();
            _input = null;
        }

        /// <summary>
        /// The session's seats, looked for on the first sample rather than in OnEnable: the front
        /// door creates the session in its own OnEnable, which may run after this one. With no
        /// session at all — the Gameplay scene opened on its own — Player 2 stands in on the first
        /// controller, which is how that scene has always behaved.
        /// </summary>
        private SeatAssignment Seats()
        {
            if (!_lookedForSession)
            {
                _session = GameSession.Find();
                _lookedForSession = true;
            }

            if (_session != null)
            {
                return _session.Seats;
            }

            _standIn.StandIn(FirstGamepad());
            return _standIn;
        }

        private static int FirstGamepad()
        {
            int first = SeatAssignment.NoDevice;
            foreach (Gamepad pad in Gamepad.all)
            {
                if (first == SeatAssignment.NoDevice || pad.deviceId < first)
                {
                    first = pad.deviceId;
                }
            }

            return first;
        }

        private static IPlayerRegistryHost FindHost()
        {
            SimulationDriver driver = FindAnyObjectByType<SimulationDriver>();
            if (driver != null)
            {
                return driver;
            }

            return FindAnyObjectByType<CommandSampler>();
        }
    }
}
