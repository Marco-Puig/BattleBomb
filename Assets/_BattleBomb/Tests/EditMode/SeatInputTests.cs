using System.Collections.Generic;
using System.Reflection;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Players;
using BattleBomb.Tests.EditMode.Acceptance;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// A seat against virtual controllers (D57). The fixture replaces the machine's real devices
    /// for the length of each test, so only what a test adds exists and only what it presses is down.
    /// </summary>
    public sealed class SeatInputTests : InputTestFixture
    {
        private readonly List<SeatInput> _made = new List<SeatInput>();
        private InputActionAsset _controls;
        private Keyboard _keyboard;
        private Gamepad _padA;
        private Gamepad _padB;
        private int _frame;

        public override void Setup()
        {
            base.Setup();
            _controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AcceptanceFixture.ControlsAssetPath);
            Assert.That(_controls, Is.Not.Null, "The controls asset did not load.");
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _padA = InputSystem.AddDevice<Gamepad>();
            _padB = InputSystem.AddDevice<Gamepad>();
            _frame = 0;
        }

        public override void TearDown()
        {
            for (int i = 0; i < _made.Count; i++)
            {
                _made[i].Dispose();
            }

            _made.Clear();
            base.TearDown();
        }

        /// <summary>A seat that has already taken its first, priming sample.</summary>
        private SeatInput Seat(int seat, SeatAssignment seats)
        {
            var input = new SeatInput(_controls, seat);
            _made.Add(input);
            Next(input, seats);
            return input;
        }

        private PlayerCommand Next(SeatInput seat, SeatAssignment seats)
        {
            seat.Own(seats);
            return seat.Sample(++_frame);
        }

        /// <summary>
        /// A pad reported by the fixture's fake native layer. Only a native device is parked on
        /// the disconnected list when it goes and comes back as the same device under a new id — a
        /// real reconnect; AddDevice's devices are simply removed. The package keeps its test
        /// runtime internal, so it is reached by reflection: if an Input System upgrade renames it,
        /// this fails loudly, which is when reconnects deserve a fresh look anyway.
        /// </summary>
        private int ReportPad()
        {
            object runtime = TestRuntime();
            MethodInfo report = runtime.GetType().GetMethod("ReportNewInputDevice", new[]
            {
                typeof(InputDeviceDescription), typeof(int), typeof(ulong), typeof(string), typeof(string),
            });
            var description = new InputDeviceDescription { deviceClass = nameof(Gamepad), interfaceName = "Test" };
            int id = (int)report.Invoke(runtime, new object[] { description, InputDevice.InvalidDeviceId, 0UL, null, null });
            InputSystem.Update();
            return id;
        }

        private void UnplugPad(InputDevice pad)
        {
            object runtime = TestRuntime();
            runtime.GetType().GetMethod("ReportInputDeviceRemoved", new[] { typeof(InputDevice) })
                .Invoke(runtime, new object[] { pad });
            InputSystem.Update();
        }

        private object TestRuntime() =>
            typeof(InputTestFixture).GetProperty("runtime", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(this);

        [Test]
        public void A_solo_player_answers_to_the_keyboard_and_every_controller()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);

            Press(_padB.buttonSouth);
            Assert.That(Next(one, seats).WasPressed(CommandButtons.Jump), Is.True, "The second pad.");
            Release(_padB.buttonSouth);
            Next(one, seats);

            Press(_keyboard.jKey);
            Assert.That(Next(one, seats).WasPressed(CommandButtons.Light), Is.True, "The keyboard.");
            Release(_keyboard.jKey);
            Next(one, seats);

            Press(_padA.buttonWest);
            Assert.That(Next(one, seats).WasPressed(CommandButtons.Light), Is.True, "The first pad.");
        }

        [Test]
        public void The_prompts_follow_whatever_was_pressed_last()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);

            Press(_padA.buttonNorth);
            Next(one, seats);
            Assert.That(one.Family, Is.EqualTo(InputFamily.Gamepad));
            Assert.That(one.LastDeviceId, Is.EqualTo(_padA.deviceId));
            Release(_padA.buttonNorth);
            Next(one, seats);

            Press(_keyboard.kKey);
            Next(one, seats);
            Assert.That(one.Family, Is.EqualTo(InputFamily.Keyboard));
            Assert.That(one.LastDeviceId, Is.EqualTo(_keyboard.deviceId));
        }

        [Test]
        public void One_press_carries_the_fight_and_the_menu_together()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);

            Press(_padA.buttonSouth);
            PlayerCommand a = Next(one, seats);
            Assert.That(a.WasPressed(CommandButtons.Jump) && a.WasPressed(CommandButtons.Confirm), Is.True,
                "A is Jump in a fight and Confirm on a screen.");
            Release(_padA.buttonSouth);
            Next(one, seats);

            Press(_keyboard.escapeKey);
            PlayerCommand escape = Next(one, seats);
            Assert.That(escape.WasPressed(CommandButtons.Pause) && escape.WasPressed(CommandButtons.Back), Is.True,
                "Escape pauses outside a menu and backs out inside one.");
            Release(_keyboard.escapeKey);
            Next(one, seats);

            Press(_padA.rightShoulder);
            PlayerCommand rb = Next(one, seats);
            Assert.That(rb.WasPressed(CommandButtons.Equipment) && rb.WasPressed(CommandButtons.TabNext), Is.True);
        }

        [Test]
        public void Two_controllers_drive_two_seats_and_never_each_other()
        {
            var seats = new SeatAssignment();
            seats.StandIn(_padB.deviceId);
            SeatInput one = Seat(0, seats);
            SeatInput two = Seat(1, seats);

            Press(_padB.buttonSouth);
            Assert.That(Next(two, seats).WasPressed(CommandButtons.Jump), Is.True);
            Assert.That(Next(one, seats).IsHeld(CommandButtons.Jump), Is.False, "Player 2's pad moved Player 1.");
            Release(_padB.buttonSouth);
            Next(one, seats);
            Next(two, seats);

            Press(_padA.buttonSouth);
            Assert.That(Next(one, seats).WasPressed(CommandButtons.Jump), Is.True);
            Assert.That(Next(two, seats).IsHeld(CommandButtons.Jump), Is.False, "Player 1's pad moved Player 2.");
        }

        [Test]
        public void An_empty_seat_hears_nothing()
        {
            var seats = new SeatAssignment();
            SeatInput two = Seat(1, seats);

            Press(_padA.buttonSouth);
            Press(_keyboard.spaceKey);
            Assert.That(Next(two, seats).Held, Is.EqualTo(CommandButtons.None));
        }

        [Test]
        public void A_controller_plugged_in_later_is_player_ones_on_the_next_sample()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);

            Gamepad late = InputSystem.AddDevice<Gamepad>();
            Next(one, seats);
            Press(late.buttonEast);

            Assert.That(Next(one, seats).WasPressed(CommandButtons.Magic), Is.True);
        }

        [Test]
        public void A_button_already_down_when_the_seat_wakes_is_not_a_press()
        {
            Press(_padA.buttonSouth);
            var input = new SeatInput(_controls, 0);
            _made.Add(input);
            var seats = new SeatAssignment();

            // Button actions skip the initial state check, so a button already down when the seat
            // binds is not even seen as held until it is let go and pressed again.
            Assert.That(Next(input, seats).WasPressed(CommandButtons.Confirm), Is.False,
                "A held across a scene change would confirm the next screen the instant it loaded.");
            Assert.That(Next(input, seats).WasPressed(CommandButtons.Confirm), Is.False,
                "Still held is still not a press.");

            Release(_padA.buttonSouth);
            Next(input, seats);
            Press(_padA.buttonSouth);
            Assert.That(Next(input, seats).WasPressed(CommandButtons.Confirm), Is.True,
                "A fresh press after letting go counts.");
        }

        [Test]
        public void A_seat_is_seated_by_presses_and_remembers_the_last_one()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);
            Assert.That(one.LastDeviceId, Is.EqualTo(SeatAssignment.NoDevice),
                "Nothing pressed yet. The front door seats players by this, so a guess would let a pad " +
                "that streams reports untouched claim a seat.");

            Set(_padA.leftStick, new UnityEngine.Vector2(1f, 0f));
            Next(one, seats);
            Assert.That(one.LastDeviceId, Is.EqualTo(SeatAssignment.NoDevice), "A stick is not a press.");
            Assert.That(one.Family, Is.EqualTo(InputFamily.Gamepad), "The prompts still follow the stick.");
            Set(_padA.leftStick, UnityEngine.Vector2.zero);

            Press(_padB.buttonSouth);
            Next(one, seats);
            Release(_padB.buttonSouth);
            Next(one, seats);
            Next(one, seats);
            Assert.That(one.LastDeviceId, Is.EqualTo(_padB.deviceId),
                "Kept while idle: at character select Player 1 is unhomed whenever this reads nothing.");
        }

        [Test]
        public void A_key_still_held_elsewhere_does_not_take_the_press()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);

            Press(_keyboard.qKey);
            Next(one, seats);
            Press(_padB.buttonSouth);
            Next(one, seats);

            Assert.That(one.LastDeviceId, Is.EqualTo(_padB.deviceId),
                "The pad just pressed, not the key still held: the front door seats Player 2 by this.");
        }

        [Test]
        public void A_device_that_leaves_the_seat_is_forgotten()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);
            Press(_padB.buttonSouth);
            Next(one, seats);
            Release(_padB.buttonSouth);

            seats.StandIn(_padB.deviceId);
            Next(one, seats);

            Assert.That(one.LastDeviceId, Is.EqualTo(SeatAssignment.NoDevice),
                "Pad B is seat two's now, so seat one cannot still claim it.");
        }

        [Test]
        public void A_button_held_through_a_controller_plugging_in_stays_held()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);

            Press(_keyboard.kKey);
            Assert.That(Next(one, seats).WasPressed(CommandButtons.Heavy), Is.True);

            InputSystem.AddDevice<Gamepad>();
            PlayerCommand after = Next(one, seats);

            Assert.That(after.IsHeld(CommandButtons.Heavy), Is.True,
                "A pad plugged in mid-charge must not cut Player 1's Heavy short.");
            Assert.That(after.WasReleased(CommandButtons.Heavy), Is.False);
        }

        [Test]
        public void A_button_held_while_its_device_changes_seats_is_not_the_new_seats_press()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);
            SeatInput two = Seat(1, seats);

            Press(_padB.buttonSouth);
            Next(one, seats);
            Next(two, seats);

            seats.StandIn(_padB.deviceId);
            Assert.That(Next(one, seats).IsHeld(CommandButtons.Jump), Is.False, "Pad B left seat one.");
            Assert.That(Next(two, seats).WasPressed(CommandButtons.Jump), Is.False,
                "A button already down when a pad changes hands is not a join; it takes a fresh press.");

            Release(_padB.buttonSouth);
            Next(two, seats);
            Press(_padB.buttonSouth);
            Assert.That(Next(two, seats).WasPressed(CommandButtons.Jump), Is.True);
        }

        [Test]
        public void A_controller_that_goes_away_leaves_no_device_behind()
        {
            var seats = new SeatAssignment();
            SeatInput one = Seat(0, seats);
            Press(_padB.buttonSouth);
            Next(one, seats);

            InputSystem.RemoveDevice(_padB);

            Assert.That(Next(one, seats).IsHeld(CommandButtons.Jump), Is.False);
            Assert.That(one.LastDeviceId, Is.EqualTo(SeatAssignment.NoDevice));
        }

        [Test]
        public void Player_twos_controller_that_sleeps_at_character_select_keeps_its_seat()
        {
            var pad = (Gamepad)InputSystem.GetDeviceById(ReportPad());

            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, _keyboard.deviceId, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, true, _keyboard.deviceId, pad.deviceId);
            SeatInput one = Seat(0, seats);
            SeatInput two = Seat(1, seats);

            int asleep = pad.deviceId;
            UnplugPad(pad);
            ReportPad();
            Assert.That(pad.added, Is.True, "The pad did not come back as the same device.");
            Assert.That(pad.deviceId, Is.Not.EqualTo(asleep), "The pad came back under its old id.");

            Next(one, seats);
            Next(two, seats);
            Press(pad.buttonSouth);

            Assert.That(Next(two, seats).WasPressed(CommandButtons.Jump), Is.True,
                "Player 2 lost their pad to the reconnect.");
            Assert.That(Next(one, seats).IsHeld(CommandButtons.Jump), Is.False, "Player 1 took Player 2's pad.");
        }

        [Test]
        public void A_controller_that_slept_before_the_seats_were_built_keeps_its_seat()
        {
            var pad = (Gamepad)InputSystem.GetDeviceById(ReportPad());
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, _keyboard.deviceId, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, true, _keyboard.deviceId, pad.deviceId);

            // Asleep across a scene change: the seats that saw it go were destroyed with their scene.
            UnplugPad(pad);
            SeatInput one = Seat(0, seats);
            SeatInput two = Seat(1, seats);
            ReportPad();

            Next(one, seats);
            Next(two, seats);
            Press(pad.buttonSouth);

            Assert.That(Next(two, seats).WasPressed(CommandButtons.Jump), Is.True,
                "Player 2's pad woke in a new scene and was lost to Player 1.");
            Assert.That(Next(one, seats).IsHeld(CommandButtons.Jump), Is.False);
        }
    }
}
