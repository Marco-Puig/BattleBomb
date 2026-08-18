using BattleBomb.Platform;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    public sealed class NullPlatformServicesTests
    {
        [Test]
        public void Reports_itself_unavailable_without_throwing()
        {
            IPlatformServices services = new NullPlatformServices();

            Assert.That(services.Initialise(), Is.False);
            Assert.That(services.IsAvailable, Is.False);
            Assert.That(services.Identity.IsSignedIn, Is.False);
            Assert.DoesNotThrow(services.Shutdown);
        }

        [Test]
        public void Achievements_unlock_locally_so_UI_still_has_something_to_show()
        {
            IPlatformServices services = new NullPlatformServices();

            services.Achievements.Unlock("first_blood");
            services.Achievements.ReportProgress("centurion", current: 100, target: 100);
            services.Achievements.ReportProgress("marathon", current: 3, target: 100);

            Assert.That(services.Achievements.IsUnlocked("first_blood"), Is.True);
            Assert.That(services.Achievements.IsUnlocked("centurion"), Is.True);
            Assert.That(services.Achievements.IsUnlocked("marathon"), Is.False);
        }

        [Test]
        public void Leaderboard_calls_complete_with_a_clean_failure()
        {
            IPlatformServices services = new NullPlatformServices();
            bool? submitted = null;
            LeaderboardEntry[] fetched = null;

            services.Leaderboards.SubmitScore("endless", 5000, ok => submitted = ok);
            services.Leaderboards.FetchTop("endless", 10, entries => fetched = entries);

            Assert.That(submitted, Is.False, "Callers must be told it failed, not left waiting.");
            Assert.That(fetched, Is.Not.Null.And.Empty);
        }
    }
}
