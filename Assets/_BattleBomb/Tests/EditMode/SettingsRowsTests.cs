using System.Linq;
using BattleBomb.UI.Chest;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    public sealed class SettingsRowsTests
    {
        [Test]
        public void A_release_has_no_debug_row()
        {
            Assert.That(SettingsRows.For(development: false), Is.EqualTo(new[]
            {
                SettingsRow.AutoEquip, SettingsRow.AutoSell, SettingsRow.ReturnToChapters,
            }));
        }

        [Test]
        public void A_development_build_adds_the_debug_rows_last()
        {
            var release = SettingsRows.For(development: false);
            var development = SettingsRows.For(development: true);

            Assert.That(development.Take(release.Count), Is.EqualTo(release),
                "The rows both flavours share must sit at the same index in both.");
            Assert.That(development.Skip(release.Count), Is.EqualTo(new[]
            {
                SettingsRow.GrantTestLoot, SettingsRow.TierOverlay,
            }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void The_cursor_wraps_both_ways_in_both_flavours(bool development)
        {
            int count = SettingsRows.For(development).Count;

            Assert.That(SettingsRows.Step(0, -1, count), Is.EqualTo(count - 1), "Up from the top did not wrap to the bottom.");
            Assert.That(SettingsRows.Step(count - 1, +1, count), Is.Zero, "Down from the bottom did not wrap to the top.");
            Assert.That(SettingsRows.Step(1, +1, count), Is.EqualTo(2));
            Assert.That(SettingsRows.Step(1, -1, count), Is.EqualTo(0));
        }

        [Test]
        public void The_editor_runs_the_development_layout() =>
            Assert.That(SettingsRows.Current, Is.EqualTo(SettingsRows.For(development: true)));
    }
}
