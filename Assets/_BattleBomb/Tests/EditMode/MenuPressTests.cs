using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// How a screen reads one step's buttons (D57). The rule worth pinning is Escape's: it is
    /// bound to Back and to Pause at once, and a menu must hear only the Back.
    /// </summary>
    public sealed class MenuPressTests
    {
        private static PlayerCommand Pressed(CommandButtons buttons) =>
            PlayerCommand.FromState(1, Vector2.zero, buttons, CommandButtons.None);

        [Test]
        public void Escape_is_a_back_not_a_pause()
        {
            MenuPress press = MenuPress.From(Pressed(CommandButtons.Back | CommandButtons.Pause));

            Assert.That(press.Back, Is.True);
            Assert.That(press.Pause, Is.False,
                "Escape backs out one level. Read as Pause too, it would close the whole screen.");
        }

        [Test]
        public void Start_alone_is_a_pause()
        {
            MenuPress press = MenuPress.From(Pressed(CommandButtons.Pause));

            Assert.That(press.Pause, Is.True);
            Assert.That(press.Back, Is.False);
        }

        [Test]
        public void Each_menu_button_reads_as_its_own_intent()
        {
            Assert.That(MenuPress.From(Pressed(CommandButtons.Confirm)).Confirm, Is.True);
            Assert.That(MenuPress.From(Pressed(CommandButtons.Option)).Option, Is.True);
            Assert.That(MenuPress.From(Pressed(CommandButtons.Lock)).Lock, Is.True);
            Assert.That(MenuPress.From(Pressed(CommandButtons.TabNext)).Tab, Is.EqualTo(1));
            Assert.That(MenuPress.From(Pressed(CommandButtons.TabPrevious)).Tab, Is.EqualTo(-1));
        }

        [Test]
        public void Both_shoulders_at_once_go_nowhere()
        {
            MenuPress press = MenuPress.From(Pressed(CommandButtons.TabNext | CommandButtons.TabPrevious));

            Assert.That(press.Tab, Is.Zero);
        }

        [Test]
        public void The_fight_verbs_mean_nothing_to_a_menu()
        {
            MenuPress press = MenuPress.From(Pressed(
                CommandButtons.Light | CommandButtons.Heavy | CommandButtons.Magic
                | CommandButtons.Jump | CommandButtons.Equipment));

            Assert.That(press.Any, Is.False,
                "A menu reads its own buttons. The fight's verbs share physical buttons with them, " +
                "but a screen that read Light would sell on the press that opened it.");
        }

        [Test]
        public void A_held_button_is_not_a_new_press()
        {
            PlayerCommand held = PlayerCommand.FromState(
                2, Vector2.zero, CommandButtons.Confirm, CommandButtons.Confirm);

            Assert.That(MenuPress.From(held).Confirm, Is.False);
        }

        [Test]
        public void Anything_held_keeps_a_fresh_screen_waiting()
        {
            PlayerCommand holdingLight = PlayerCommand.FromState(
                1, Vector2.zero, CommandButtons.Light, CommandButtons.Light);

            Assert.That(MenuPress.AnyHeld(holdingLight), Is.True);
            Assert.That(MenuPress.AnyHeld(PlayerCommand.Idle(1)), Is.False);
        }
    }
}
