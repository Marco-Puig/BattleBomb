using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class PlayerCommandTests
    {
        [Test]
        public void FromState_derives_press_edge_from_previous_step()
        {
            PlayerCommand command = PlayerCommand.FromState(
                frame: 7,
                move: Vector2.zero,
                held: CommandButtons.Light | CommandButtons.Jump,
                previouslyHeld: CommandButtons.Light);

            Assert.That(command.WasPressed(CommandButtons.Jump), Is.True);
            Assert.That(command.WasPressed(CommandButtons.Light), Is.False, "Light was already held.");
            Assert.That(command.Released, Is.EqualTo(CommandButtons.None));
            Assert.That(command.Frame, Is.EqualTo(7));
        }

        [Test]
        public void FromState_derives_release_edge_from_previous_step()
        {
            PlayerCommand command = PlayerCommand.FromState(
                frame: 3,
                move: Vector2.zero,
                held: CommandButtons.None,
                previouslyHeld: CommandButtons.Heavy);

            Assert.That(command.WasReleased(CommandButtons.Heavy), Is.True);
            Assert.That(command.IsHeld(CommandButtons.Heavy), Is.False);
        }

        [Test]
        public void FromState_clamps_movement_to_unit_length()
        {
            PlayerCommand command = PlayerCommand.FromState(3, new Vector2(1f, 1f), CommandButtons.None, CommandButtons.None);

            Assert.That(command.Move.magnitude, Is.EqualTo(1f).Within(0.0001f),
                "Diagonal input must not out-run cardinal input.");
        }

        [Test]
        public void FromState_leaves_sub_unit_movement_untouched()
        {
            Vector2 partial = new Vector2(0.4f, -0.2f);

            PlayerCommand command = PlayerCommand.FromState(0, partial, CommandButtons.None, CommandButtons.None);

            Assert.That(command.Move, Is.EqualTo(partial), "Analogue precision must survive.");
        }

        [Test]
        public void Idle_carries_no_intent()
        {
            PlayerCommand command = PlayerCommand.Idle(12);

            Assert.That(command.Move, Is.EqualTo(Vector2.zero));
            Assert.That(command.Held, Is.EqualTo(CommandButtons.None));
            Assert.That(command.Pressed, Is.EqualTo(CommandButtons.None));
            Assert.That(command.Released, Is.EqualTo(CommandButtons.None));
            Assert.That(command.Frame, Is.EqualTo(12));
        }
    }
}
