using System.Collections.Generic;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleBomb.Gameplay.Players
{
    /// <summary>
    /// Translates one player's Input System actions into <see cref="PlayerCommand"/>s. This is the
    /// only place in the project permitted to touch an input device — everything downstream reads
    /// commands (§4).
    /// </summary>
    /// <remarks>
    /// Actions come from the sibling <see cref="PlayerInput"/>, which clones the action asset per
    /// player and pairs it to that player's devices. That is what makes local co-op work without any
    /// code knowing how many players there are.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class InputSystemCommandSource : MonoBehaviour, IPlayerCommandSource
    {
        [Tooltip("Driver this player registers with. Leave empty to find the driver in the scene, " +
                 "or the command sampler when there is no simulation (the front door).")]
        [SerializeField] private SimulationDriver _driver;

        /// <summary>What this source is actually registered with. Usually the driver, but the
        /// front door has no simulation and still has two devices, so anything that owns a
        /// <see cref="PlayerRegistry"/> will do (task 80).</summary>
        private IPlayerRegistryHost _host;

        private readonly List<(InputAction action, CommandButtons button)> _buttons =
            new List<(InputAction, CommandButtons)>();

        private PlayerInput _playerInput;
        private InputAction _move;
        private CommandButtons _previouslyHeld;

        public PlayerId PlayerId => new PlayerId(_playerInput != null ? Mathf.Max(0, _playerInput.playerIndex) : 0);

        public PlayerCommand Sample(int frame)
        {
            Vector2 move = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;

            CommandButtons held = CommandButtons.None;
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i].action.IsPressed())
                {
                    held |= _buttons[i].button;
                }
            }

            PlayerCommand command = PlayerCommand.FromState(frame, move, held, _previouslyHeld);
            _previouslyHeld = held;
            return command;
        }

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            InputActionAsset actions = _playerInput.actions;

            if (actions == null)
            {
                Debug.LogError($"{name}: PlayerInput has no action asset — this player will contribute nothing.", this);
                return;
            }

            _move = Find(actions, PlayerActions.Move);
            AddButton(actions, PlayerActions.Light, CommandButtons.Light);
            AddButton(actions, PlayerActions.Heavy, CommandButtons.Heavy);
            AddButton(actions, PlayerActions.Magic, CommandButtons.Magic);
            AddButton(actions, PlayerActions.Equipment, CommandButtons.Equipment);
            AddButton(actions, PlayerActions.Jump, CommandButtons.Jump);
            AddButton(actions, PlayerActions.Pause, CommandButtons.Pause);
        }

        private void OnEnable()
        {
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
            _previouslyHeld = CommandButtons.None;
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

        private void AddButton(InputActionAsset actions, string actionName, CommandButtons button)
        {
            InputAction action = Find(actions, actionName);
            if (action != null)
            {
                _buttons.Add((action, button));
            }
        }

        private InputAction Find(InputActionAsset actions, string actionName)
        {
            InputAction action = actions.FindAction($"{PlayerActions.Map}/{actionName}", throwIfNotFound: false);
            if (action == null)
            {
                Debug.LogWarning($"{name}: action '{PlayerActions.Map}/{actionName}' not found — it will read as inactive.", this);
            }

            return action;
        }
    }
}
