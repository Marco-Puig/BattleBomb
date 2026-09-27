using BattleBomb.Core.Chapters;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>An online guest in Player 2's slot at character select (D59): their pick and readiness come from their
    /// own machine, this machine's pads never drive the slot, and nobody launches until they are ready.</summary>
    public sealed class FrontendRemoteSlotTests
    {
        private static FrontendState AtCharacters()
        {
            var state = new FrontendState(3, canContinue: false);
            state.Confirm(0);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Characters));
            return state;
        }

        [Test]
        public void An_online_guest_takes_slot_two_and_this_machines_pads_cannot_drive_it()
        {
            FrontendState state = AtCharacters();
            state.SetRemote(1, true, 2, false);

            Assert.That(state.IsJoined(1), Is.True);
            Assert.That(state.IsRemote(1), Is.True);
            Assert.That(state.PickOf(1), Is.EqualTo(2));

            state.MovePick(1, 1);
            state.Confirm(1);
            state.Back(1);
            Assert.That(state.PickOf(1), Is.EqualTo(2), "A local pad moved the online guest's pick.");
            Assert.That(state.IsReady(1), Is.False, "A local pad readied the online guest.");
            Assert.That(state.IsJoined(1), Is.True, "A local pad unseated the online guest.");
        }

        [Test]
        public void The_host_moves_on_only_when_the_guest_is_ready()
        {
            FrontendState state = AtCharacters();
            state.SetRemote(1, true, 0, false);
            state.Confirm(0);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Characters), "The host moved on without its guest.");

            state.SetRemote(1, true, 0, true);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Chapters));
        }

        [Test]
        public void A_launch_waits_for_a_guest_who_unreadied()
        {
            FrontendState state = AtCharacters();
            state.SetRemote(1, true, 0, true);
            state.Confirm(0);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Chapters));

            state.SetRemote(1, true, 0, false);
            state.Launch(true);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Chapters), "The host launched without its guest.");

            state.SetRemote(1, true, 0, true);
            state.Launch(true);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Launching));
        }

        [Test]
        public void A_guest_who_unreadies_under_a_launch_stops_it_before_anything_loads()
        {
            // A pointer's Launch can land between two looks at the guest (D59): the next look, finding them unready,
            // takes the front door back to the chapters — the host never restores a guest from a pick they took back.
            FrontendState state = AtCharacters();
            state.SetRemote(1, true, 0, true);
            state.Confirm(0);
            state.Launch(true);
            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Launching), "The launch never began, so the case proves nothing.");

            state.SetRemote(1, true, 0, false);

            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Chapters), "The front door launched with its guest unready.");
        }

        [Test]
        public void A_guest_leaving_frees_the_slot_but_never_unseats_a_couch_player()
        {
            FrontendState online = AtCharacters();
            online.SetRemote(1, true, 1, true);
            online.SetRemote(1, false, 0, false);
            Assert.That(online.IsJoined(1), Is.False);
            Assert.That(online.IsRemote(1), Is.False);

            FrontendState couch = AtCharacters();
            couch.Confirm(1);
            couch.SetRemote(1, false, 0, false);
            Assert.That(couch.IsJoined(1), Is.True, "Nobody online left, and the couch's Player 2 was unseated.");
        }

        [Test]
        public void Going_back_to_the_title_keeps_an_online_guest_seated()
        {
            FrontendState state = AtCharacters();
            state.SetRemote(1, true, 0, false);
            state.Back(0);

            Assert.That(state.Screen, Is.EqualTo(FrontendScreen.Title));
            Assert.That(state.IsJoined(1), Is.True, "The title forgot a guest who is still connected.");
        }
    }
}
