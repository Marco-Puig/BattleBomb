using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    public sealed class CommandButtonsTests
    {
        [Test]
        public void IsHeld_detects_each_button_in_a_combined_flag_set()
        {
            PlayerCommand command = PlayerCommand.FromState(
                frame: 0,
                move: Vector2.zero,
                held: CommandButtons.Attack | CommandButtons.Dodge,
                previouslyHeld: CommandButtons.None);

            Assert.That(command.IsHeld(CommandButtons.Attack), Is.True);
            Assert.That(command.IsHeld(CommandButtons.Dodge), Is.True);
            Assert.That(command.IsHeld(CommandButtons.Heavy), Is.False);
        }

        [Test]
        public void No_command_holds_the_none_flag()
        {
            PlayerCommand idle = PlayerCommand.Idle(0);
            PlayerCommand active = PlayerCommand.FromState(
                frame: 1,
                move: Vector2.zero,
                held: CommandButtons.Attack,
                previouslyHeld: CommandButtons.None);

            Assert.That(idle.IsHeld(CommandButtons.None), Is.False);
            Assert.That(active.IsHeld(CommandButtons.None), Is.False);
        }
    }
}
