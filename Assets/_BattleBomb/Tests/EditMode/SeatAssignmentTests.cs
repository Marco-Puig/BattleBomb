using BattleBomb.Core.Chapters;
using BattleBomb.Core.Players;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// Who owns which device (D57). One rule — Player 2 owns the device they joined with, Player 1
    /// owns everything else — plus character select, where the empty seat listens for a join.
    /// </summary>
    public sealed class SeatAssignmentTests
    {
        private const int Keyboard = 1;
        private const int PadA = 5;
        private const int PadB = 9;

        [Test]
        public void A_fresh_assignment_gives_player_one_every_device()
        {
            var seats = new SeatAssignment();

            Assert.That(seats.Owns(0, Keyboard), Is.True);
            Assert.That(seats.Owns(0, PadA), Is.True);
            Assert.That(seats.Owns(0, PadB), Is.True);
            Assert.That(seats.Owns(1, PadA), Is.False, "Nobody sits in seat two yet.");
        }

        [Test]
        public void At_character_select_player_one_is_held_to_the_device_that_started()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, secondJoined: false, PadA, SeatAssignment.NoDevice);

            Assert.That(seats.FirstSeatHome, Is.EqualTo(PadA));
            Assert.That(seats.Owns(0, PadA), Is.True);
            Assert.That(seats.Owns(0, PadB), Is.False,
                "A press on another pad is a join, so it cannot also be Player 1's.");
            Assert.That(seats.Owns(1, PadB), Is.True);
            Assert.That(seats.Owns(1, Keyboard), Is.True);
            Assert.That(seats.Owns(1, PadA), Is.False);
        }

        [Test]
        public void Joining_on_another_device_seats_player_two_on_it()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, PadA, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, true, PadA, PadB);

            Assert.That(seats.SecondSeatDevice, Is.EqualTo(PadB));
            Assert.That(seats.Owns(1, PadB), Is.True);
            Assert.That(seats.Owns(1, Keyboard), Is.False, "Player 2 owns only what they joined with.");
            Assert.That(seats.Owns(0, PadB), Is.False);
            Assert.That(seats.Owns(0, Keyboard), Is.True, "Player 1 gets everything else back.");
        }

        [Test]
        public void Player_two_leaving_frees_their_device()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, PadA, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, true, PadA, PadB);
            seats.Follow(FrontendScreen.Characters, false, PadA, PadB);

            Assert.That(seats.SecondSeatDevice, Is.EqualTo(SeatAssignment.NoDevice));
            Assert.That(seats.Owns(1, PadB), Is.True, "Still at character select, so it can rejoin.");
        }

        [Test]
        public void Leaving_character_select_alone_hands_player_one_every_device()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, PadA, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Chapters, false, PadA, SeatAssignment.NoDevice);

            Assert.That(seats.Joining, Is.False);
            Assert.That(seats.Owns(0, PadB), Is.True);
            Assert.That(seats.Owns(0, Keyboard), Is.True);
            Assert.That(seats.Owns(1, PadB), Is.False);
        }

        [Test]
        public void A_couch_keeps_its_seats_past_character_select()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, Keyboard, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, true, Keyboard, PadA);
            seats.Follow(FrontendScreen.Chapters, true, Keyboard, PadA);

            Assert.That(seats.Owns(1, PadA), Is.True);
            Assert.That(seats.Owns(0, PadB), Is.True, "A third pad belongs to Player 1.");
            Assert.That(seats.Owns(0, PadA), Is.False);
        }

        [Test]
        public void The_title_forgets_both_seats()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, PadA, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, true, PadA, PadB);
            seats.Follow(FrontendScreen.Title, false, PadA, PadB);

            Assert.That(seats.FirstSeatHome, Is.EqualTo(SeatAssignment.NoDevice));
            Assert.That(seats.SecondSeatDevice, Is.EqualTo(SeatAssignment.NoDevice));
            Assert.That(seats.Owns(0, PadB), Is.True, "On the title anyone can start.");
        }

        [Test]
        public void A_join_on_player_ones_own_device_seats_nobody()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, PadA, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, true, PadA, PadA);

            Assert.That(seats.SecondSeatDevice, Is.EqualTo(SeatAssignment.NoDevice));
        }

        [Test]
        public void Player_one_is_homed_on_their_first_press_when_they_started_by_pointer()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, SeatAssignment.NoDevice, SeatAssignment.NoDevice);
            Assert.That(seats.Owns(1, PadB), Is.False, "No home yet, so nothing can be a join.");

            seats.Follow(FrontendScreen.Characters, false, Keyboard, SeatAssignment.NoDevice);
            Assert.That(seats.FirstSeatHome, Is.EqualTo(Keyboard));
            Assert.That(seats.Owns(1, PadB), Is.True);
        }

        [Test]
        public void No_device_is_owned_by_nobody()
        {
            var seats = new SeatAssignment();

            Assert.That(seats.Owns(0, SeatAssignment.NoDevice), Is.False);
            Assert.That(seats.Owns(1, SeatAssignment.NoDevice), Is.False);
            Assert.That(seats.Owns(2, PadA), Is.False, "There are two seats.");
        }

        [Test]
        public void With_no_front_door_player_two_stands_in_on_the_first_controller()
        {
            var seats = new SeatAssignment();
            seats.StandIn(PadA);

            Assert.That(seats.Owns(1, PadA), Is.True);
            Assert.That(seats.Owns(0, PadA), Is.False);
            Assert.That(seats.Owns(0, Keyboard), Is.True);

            seats.StandIn(SeatAssignment.NoDevice);
            Assert.That(seats.Owns(0, PadA), Is.True, "No controller: Player 1 has everything.");
        }

        [Test]
        public void Coming_back_to_character_select_on_another_device_homes_player_one_on_it()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, Keyboard, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Chapters, false, PadA, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, false, PadA, SeatAssignment.NoDevice);

            Assert.That(seats.FirstSeatHome, Is.EqualTo(PadA));
            Assert.That(seats.Owns(0, PadA), Is.True,
                "A solo player who picked up a pad at chapter select and backed out is still Player 1 on it.");
            Assert.That(seats.Owns(1, Keyboard), Is.True, "The keyboard they put down is now a join.");
        }

        [Test]
        public void Player_one_losing_their_home_device_at_character_select_is_not_locked_out()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, PadA, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, false, SeatAssignment.NoDevice, SeatAssignment.NoDevice);

            Assert.That(seats.Owns(0, PadB), Is.True,
                "With the home pad gone, a pads-only couch could neither ready nor back out.");
            Assert.That(seats.Owns(1, PadB), Is.False, "With no home, nothing can be a join.");

            seats.Follow(FrontendScreen.Characters, false, PadB, SeatAssignment.NoDevice);
            Assert.That(seats.FirstSeatHome, Is.EqualTo(PadB));
        }

        [Test]
        public void A_seated_player_two_keeps_their_device_whatever_their_seat_reports()
        {
            var seats = new SeatAssignment();
            seats.Follow(FrontendScreen.Characters, false, PadA, SeatAssignment.NoDevice);
            seats.Follow(FrontendScreen.Characters, true, PadA, PadB);
            seats.Follow(FrontendScreen.Characters, true, PadA, Keyboard);
            seats.Follow(FrontendScreen.Chapters, true, PadA, SeatAssignment.NoDevice);

            Assert.That(seats.SecondSeatDevice, Is.EqualTo(PadB));
        }
    }
}
