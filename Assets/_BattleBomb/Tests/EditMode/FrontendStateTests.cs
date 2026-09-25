using BattleBomb.Core.Chapters;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>Title → characters → chapters, Castle Crashers style (D51): Player 2 joins at
    /// character select, and nobody launches until everyone present is ready.</summary>
    public sealed class FrontendStateTests
    {
        [Test]
        public void The_title_offers_continue_only_when_there_is_something_to_continue()
        {
            Assert.That(new FrontendState(rosterCount: 4, canContinue: false).TitleOptions, Is.EqualTo(1));
            Assert.That(new FrontendState(4, canContinue: true).TitleOptions, Is.EqualTo(2));
        }

        [Test]
        public void Starting_goes_to_character_select_with_player_one_present()
        {
            var state = new FrontendState(4, false);

            state.Confirm(slot: 0);

            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Characters));
            Assert.That(state.IsJoined(0), Is.True);
            Assert.That(state.IsJoined(1), Is.False);
        }

        [Test]
        public void Player_two_joins_with_a_press_and_picks_independently()
        {
            var state = new FrontendState(4, false);
            state.Confirm(0);

            state.Confirm(1);
            Assert.That(state.IsJoined(1), Is.True, "A press on the second device joins.");
            Assert.That(state.IsReady(1), Is.False, "Joining is not readying.");

            state.MovePick(1, 1);
            state.MovePick(1, 1);
            state.MovePick(0, -1);
            Assert.That(state.PickOf(1), Is.EqualTo(2));
            Assert.That(state.PickOf(0), Is.EqualTo(3), "Left from the first wraps to the last.");
        }

        [Test]
        public void Launching_waits_for_everyone_present_to_be_ready()
        {
            var state = new FrontendState(4, false);
            state.Confirm(0);
            state.Confirm(1);

            state.Confirm(0);
            Assert.That(state.IsReady(0), Is.True);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Characters), "Player two is still choosing.");

            state.Confirm(1);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Chapters));
        }

        [Test]
        public void Alone_one_ready_press_is_enough()
        {
            var state = new FrontendState(4, false);
            state.Confirm(0);
            state.Confirm(0);

            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Chapters));
        }

        [Test]
        public void Back_unreadies_then_leaves_then_returns_to_the_title()
        {
            var state = new FrontendState(4, false);
            state.Confirm(0);
            state.Confirm(1);
            state.Confirm(1);

            state.Back(1);
            Assert.That(state.IsReady(1), Is.False);
            state.Back(1);
            Assert.That(state.IsJoined(1), Is.False, "A second back leaves the couch.");

            state.Back(0);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Title));
        }

        [Test]
        public void Backing_out_to_the_title_lets_player_two_go_too()
        {
            var state = new FrontendState(4, false);
            state.Confirm(0);
            state.Confirm(1);
            state.Confirm(1);

            state.Back(0);

            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Title));
            Assert.That(state.IsJoined(1), Is.False,
                "The title forgets the seats (D57); a Player 2 still joined would come back seated on nobody's press.");
            Assert.That(state.IsReady(1), Is.False);
        }

        [Test]
        public void From_chapters_back_returns_to_characters_and_launch_needs_an_unlocked_pick()
        {
            var state = new FrontendState(4, false);
            state.Confirm(0);
            state.Confirm(0);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Chapters));

            state.Back(0);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Characters));
            Assert.That(state.IsReady(0), Is.False, "Coming back un-readies, so the pick can change.");

            state.Confirm(0);
            state.Launch(canLaunch: false);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Chapters), "A locked pick does not launch.");
            state.Launch(canLaunch: true);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Launching));
        }

        [Test]
        public void Continue_is_remembered_so_the_picker_can_land_on_the_resume_point()
        {
            var state = new FrontendState(4, canContinue: true);
            state.MoveTitle(1);
            state.Confirm(0);

            Assert.That(state.ChoseContinue, Is.False, "Row 1 is Start when Continue is offered first.");

            state = new FrontendState(4, canContinue: true);
            state.Confirm(0);
            Assert.That(state.ChoseContinue, Is.True);
        }
    }
}
