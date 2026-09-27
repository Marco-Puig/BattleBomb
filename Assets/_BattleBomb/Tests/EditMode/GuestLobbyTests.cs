using BattleBomb.Core.Chapters;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>The guest's front door while connected (D59): pick, ready, un-ready, leave.</summary>
    public sealed class GuestLobbyTests
    {
        [Test]
        public void The_pick_wraps_and_holds_still_once_ready()
        {
            var lobby = new GuestLobby(3, 0, false);
            lobby.MovePick(-1);
            Assert.That(lobby.Pick, Is.EqualTo(2));

            lobby.Confirm();
            lobby.MovePick(1);
            Assert.That(lobby.Ready, Is.True);
            Assert.That(lobby.Pick, Is.EqualTo(2), "A ready guest's pick moved.");
        }

        [Test]
        public void Back_unreadies_and_then_leaves()
        {
            var lobby = new GuestLobby(3, 1, true);
            lobby.Back();
            Assert.That(lobby.Ready, Is.False);
            Assert.That(lobby.LeaveRequested, Is.False);

            lobby.Back();
            Assert.That(lobby.LeaveRequested, Is.True);
        }

        [Test]
        public void It_comes_back_as_it_was_left()
        {
            var lobby = new GuestLobby(3, 7, true);
            Assert.That(lobby.Pick, Is.EqualTo(1), "A remembered pick past the roster wraps into it.");
            Assert.That(lobby.Ready, Is.True);
        }
    }
}
