using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class MenuGateTests
    {
        private const CommandButtons South = CommandButtons.Jump | CommandButtons.Confirm;

        private CommandButtons _previous;
        private int _frame;

        [SetUp]
        public void Begin()
        {
            _previous = CommandButtons.None;
            _frame = 0;
        }

        [Test]
        public void With_no_menu_a_command_goes_out_as_it_came()
        {
            var gate = new MenuGate();
            PlayerCommand first = Step(new Vector2(0.5f, 0f), CommandButtons.Jump);
            AssertSame(first, gate.Filter(first, false));
            PlayerCommand second = Step(Vector2.zero, CommandButtons.None);
            AssertSame(second, gate.Filter(second, false));
        }

        [Test]
        public void With_no_menu_the_stick_goes_out_exactly_as_it_came()
        {
            var command = new PlayerCommand(0, new Vector2(0.8f, 0.6000001f), CommandButtons.None, CommandButtons.None, CommandButtons.None);

            Assert.That(new MenuGate().Filter(command, false).Move, Is.EqualTo(command.Move),
                "The gate touched a stick it had no reason to.");
        }

        [Test]
        public void While_the_menu_is_open_only_neutral_goes_out()
        {
            var gate = new MenuGate();
            PlayerCommand sent = gate.Filter(Step(new Vector2(0f, 1f), South | CommandButtons.Light), true);

            Assert.That(sent.Move, Is.EqualTo(Vector2.zero), "The stick that moves the menu cursor walked the player.");
            Assert.That(sent.Held, Is.EqualTo(CommandButtons.None), "A menu press went out as a held button.");
            Assert.That(sent.Pressed, Is.EqualTo(CommandButtons.None));
        }

        [Test]
        public void The_button_that_closes_the_menu_counts_only_after_it_is_let_go()
        {
            var gate = new MenuGate();
            gate.Filter(Step(Vector2.zero, South), true);

            PlayerCommand still = gate.Filter(Step(Vector2.zero, South), false);
            Assert.That(still.Held, Is.EqualTo(CommandButtons.None), "A South held across the close went out as held.");
            Assert.That(still.Pressed, Is.EqualTo(CommandButtons.None));

            gate.Filter(Step(Vector2.zero, CommandButtons.None), false);
            PlayerCommand again = gate.Filter(Step(Vector2.zero, South), false);
            Assert.That(again.WasPressed(CommandButtons.Jump), Is.True, "Once let go, the next press must be a press.");
        }

        [Test]
        public void A_button_first_pressed_after_the_close_counts_at_once()
        {
            var gate = new MenuGate();
            gate.Filter(Step(Vector2.zero, CommandButtons.Back), true);

            PlayerCommand sent = gate.Filter(Step(Vector2.zero, CommandButtons.Back | CommandButtons.Jump), false);
            Assert.That(sent.WasPressed(CommandButtons.Jump), Is.True);
            Assert.That(sent.IsHeld(CommandButtons.Back), Is.False, "Back is still the menu's until it is let go.");
        }

        [Test]
        public void The_stick_goes_out_again_as_soon_as_the_menu_closes()
        {
            var gate = new MenuGate();
            gate.Filter(Step(Vector2.right, CommandButtons.None), true);

            Assert.That(gate.Filter(Step(Vector2.right, CommandButtons.None), false).Move, Is.EqualTo(Vector2.right));
        }

        private PlayerCommand Step(Vector2 move, CommandButtons held)
        {
            PlayerCommand command = PlayerCommand.FromState(_frame++, move, held, _previous);
            _previous = held;
            return command;
        }

        private static void AssertSame(PlayerCommand expected, PlayerCommand actual)
        {
            Assert.That(actual.Frame, Is.EqualTo(expected.Frame));
            Assert.That(actual.Move, Is.EqualTo(expected.Move));
            Assert.That(actual.Held, Is.EqualTo(expected.Held));
            Assert.That(actual.Pressed, Is.EqualTo(expected.Pressed));
            Assert.That(actual.Released, Is.EqualTo(expected.Released));
        }
    }
}
