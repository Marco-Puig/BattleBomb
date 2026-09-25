using System;
using System.Collections.Generic;
using BattleBomb.Core.Players;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using Object = UnityEngine.Object;

namespace BattleBomb.Gameplay.Players
{
    /// <summary>
    /// One couch seat's controls: its own copy of the action asset, bound to exactly the devices
    /// the seat owns (<see cref="SeatAssignment"/>), sampled into a <see cref="PlayerCommand"/>.
    /// With the <see cref="InputSystemCommandSource"/> that owns it, this is the only code that
    /// touches a device (rule 3). It is a plain class so it can be tested against virtual
    /// controllers without a scene.
    /// </summary>
    /// <remarks>
    /// Replaces PlayerInput (D57). PlayerInput paired devices by control scheme, which pinned
    /// Player 1 to the keyboard and Player 2 to the first gamepad: two controllers on one couch
    /// could not both play, and a solo player on a pad was Player 2 on the title screen. Owning
    /// the copy directly lets one seat hold a keyboard and three pads at once.
    /// </remarks>
    public sealed class SeatInput : IDisposable
    {
        private readonly InputActionAsset _actions;
        private readonly int _seat;
        private readonly List<(InputAction action, CommandButtons button)> _buttons =
            new List<(InputAction, CommandButtons)>();
        private readonly List<InputDevice> _owned = new List<InputDevice>();
        private readonly InputAction _move;
        private readonly Dictionary<InputDevice, int> _vanished = new Dictionary<InputDevice, int>();
        private readonly List<(int previous, int returned)> _returned = new List<(int, int)>();

        private CommandButtons _previouslyHeld;
        private bool _primed;
        private InputDevice _lastDevice;
        private InputDevice _lastPressed;

        public SeatInput(InputActionAsset controls, int seat)
        {
            _actions = Object.Instantiate(controls);
            _actions.name = $"{controls.name} (seat {seat})";
            _seat = seat;

            _move = Find(PlayerActions.Map, PlayerActions.Move);
            AddButton(PlayerActions.Map, PlayerActions.Light, CommandButtons.Light);
            AddButton(PlayerActions.Map, PlayerActions.Heavy, CommandButtons.Heavy);
            AddButton(PlayerActions.Map, PlayerActions.Magic, CommandButtons.Magic);
            AddButton(PlayerActions.Map, PlayerActions.Equipment, CommandButtons.Equipment);
            AddButton(PlayerActions.Map, PlayerActions.Jump, CommandButtons.Jump);
            AddButton(PlayerActions.Map, PlayerActions.Pause, CommandButtons.Pause);
            AddButton(PlayerActions.MenuMap, PlayerActions.Confirm, CommandButtons.Confirm);
            AddButton(PlayerActions.MenuMap, PlayerActions.Back, CommandButtons.Back);
            AddButton(PlayerActions.MenuMap, PlayerActions.Option, CommandButtons.Option);
            AddButton(PlayerActions.MenuMap, PlayerActions.Lock, CommandButtons.Lock);
            AddButton(PlayerActions.MenuMap, PlayerActions.TabPrevious, CommandButtons.TabPrevious);
            AddButton(PlayerActions.MenuMap, PlayerActions.TabNext, CommandButtons.TabNext);

            // Owns nothing until the first Own call says otherwise.
            _actions.devices = Array.Empty<InputDevice>();
            _actions.Enable();

            // A pad that went to sleep before this seat was built — in the scene before this
            // one — still carries the id it left with until it wakes, so it is remembered too.
            foreach (InputDevice device in InputSystem.disconnectedDevices)
            {
                _vanished[device] = device.deviceId;
            }

            InputSystem.onDeviceChange += OnDeviceChange;
        }

        /// <summary>
        /// The Input System id of the device this seat last pressed a button on, kept while it
        /// stays in the seat, or <see cref="SeatAssignment.NoDevice"/>. Never a guess: the front
        /// door seats players by it (D57), and a pad that streams reports untouched must not claim
        /// a seat. The prompts' best guess is <see cref="Family"/>'s business.
        /// </summary>
        public int LastDeviceId => _lastPressed != null ? _lastPressed.deviceId : SeatAssignment.NoDevice;

        public InputFamily Family => _lastDevice is Gamepad ? InputFamily.Gamepad : InputFamily.Keyboard;

        /// <summary>
        /// Rebinds to exactly the devices the seats give this one. Cheap when nothing changed, so
        /// it runs before every sample: a controller plugged in mid-game belongs to somebody on the
        /// next step, a seat handed over at character select takes effect the same frame, and a
        /// controller that woke under a new id is handed back to its seat first.
        /// </summary>
        public void Own(SeatAssignment seats)
        {
            for (int i = 0; i < _returned.Count; i++)
            {
                seats.Reclaim(_returned[i].previous, _returned[i].returned);
            }

            _returned.Clear();

            _owned.Clear();
            ReadOnlyArray<InputDevice> devices = InputSystem.devices;
            for (int i = 0; i < devices.Count; i++)
            {
                InputDevice device = devices[i];
                if ((device is Keyboard || device is Gamepad) && seats.Owns(_seat, device.deviceId))
                {
                    _owned.Add(device);
                }
            }

            if (!Matches(_actions.devices, _owned))
            {
                _actions.devices = _owned.ToArray();
            }

            if (_lastDevice != null && !_owned.Contains(_lastDevice))
            {
                _lastDevice = null;
            }

            if (_lastPressed != null && !_owned.Contains(_lastPressed))
            {
                _lastPressed = null;
            }

            if (_lastDevice == null)
            {
                _lastDevice = MostRecentlyUpdated(_owned);
            }
        }

        public PlayerCommand Sample(int frame)
        {
            Vector2 move = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
            if (move.sqrMagnitude > 0.25f)
            {
                Remember(_move);
            }

            // IsPressed alone drops a button held through any change to the seat's devices: the
            // re-resolve keeps the action Performed but clears its pressed flag, which would cut a
            // Heavy charge short the moment a pad is plugged in or pulled out.
            CommandButtons held = CommandButtons.None;
            for (int i = 0; i < _buttons.Count; i++)
            {
                InputAction action = _buttons[i].action;
                if (action.IsPressed() || action.phase == InputActionPhase.Performed)
                {
                    held |= _buttons[i].button;
                }
            }

            // Button actions skip the Input System's initial state check, so a button already down
            // when the seat binds is not seen until pressed again. Priming is the backstop should
            // one ever be: whatever is down on the first sample is held, not pressed.
            CommandButtons before = _primed ? _previouslyHeld : held;
            _primed = true;

            // The device of a fresh press, not of whichever held action came last in the list:
            // someone resting on another pad's shoulder must not take Player 2's join press.
            CommandButtons pressed = held & ~before;
            for (int i = 0; i < _buttons.Count && pressed != CommandButtons.None; i++)
            {
                InputControl control = _buttons[i].action.activeControl;
                if ((pressed & _buttons[i].button) != 0 && control != null)
                {
                    _lastPressed = control.device;
                    _lastDevice = control.device;
                    break;
                }
            }

            PlayerCommand command = PlayerCommand.FromState(frame, move, held, before);
            _previouslyHeld = held;
            return command;
        }

        public void Dispose()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            _actions.Disable();
            if (Application.isPlaying)
            {
                Object.Destroy(_actions);
            }
            else
            {
                Object.DestroyImmediate(_actions);
            }
        }

        /// <summary>
        /// A controller that sleeps and wakes is the same device under a new id, so the id it left
        /// with is kept and, when it returns, handed to the seats on the next <see cref="Own"/>
        /// (D57). Both seats hear it; the second reclaim finds nothing left to move.
        /// </summary>
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Disconnected)
            {
                _vanished[device] = device.deviceId;
            }
            else if (change == InputDeviceChange.Reconnected && _vanished.TryGetValue(device, out int previous))
            {
                _vanished.Remove(device);
                _returned.Add((previous, device.deviceId));
            }
        }

        private void Remember(InputAction action)
        {
            InputControl control = action.activeControl;
            if (control != null)
            {
                _lastDevice = control.device;
            }
        }

        /// <summary>Before anything is pressed, the device touched most recently is the best guess
        /// — a Steam Deck player should see A B X Y on the title, not key caps.</summary>
        private static InputDevice MostRecentlyUpdated(List<InputDevice> devices)
        {
            InputDevice best = null;
            for (int i = 0; i < devices.Count; i++)
            {
                if (best == null || devices[i].lastUpdateTime > best.lastUpdateTime)
                {
                    best = devices[i];
                }
            }

            return best;
        }

        private static bool Matches(ReadOnlyArray<InputDevice>? current, List<InputDevice> wanted)
        {
            if (!current.HasValue || current.Value.Count != wanted.Count)
            {
                return false;
            }

            ReadOnlyArray<InputDevice> devices = current.Value;
            for (int i = 0; i < wanted.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < devices.Count; j++)
                {
                    if (devices[j] == wanted[i])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private void AddButton(string map, string actionName, CommandButtons button)
        {
            InputAction action = Find(map, actionName);
            if (action != null)
            {
                _buttons.Add((action, button));
            }
        }

        private InputAction Find(string map, string actionName)
        {
            InputAction action = _actions.FindAction($"{map}/{actionName}", throwIfNotFound: false);
            if (action == null)
            {
                Debug.LogWarning($"Seat {_seat}: action '{map}/{actionName}' not found — it will read as inactive.");
            }

            return action;
        }
    }
}
