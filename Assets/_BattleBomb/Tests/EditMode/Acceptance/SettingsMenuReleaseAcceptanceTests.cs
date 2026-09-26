using System.IO;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Acceptance
{
    /// <summary>
    /// Pre-M8 fix F2: nothing that grants loot, or shows the tier numbers, compiles into a release.
    /// The editor always defines <c>UNITY_EDITOR</c>, so no test run ever compiles the release
    /// flavour; this reads the menu's source instead and checks each debug-only member is only ever
    /// mentioned inside a <c>DEVELOPMENT_BUILD || UNITY_EDITOR</c> region.
    /// </summary>
    public sealed class SettingsMenuReleaseAcceptanceTests
    {
        private const string MenuPath = "Assets/_BattleBomb/UI/Chest/SettingsMenu.cs";

        private static readonly string[] DebugOnly =
        {
            "GrantTestLoot(", "RollDebugItem(", "GrantCoins(", "DebugGrantQuality", "TierOverlay", "SettingsRow.GrantTestLoot",
        };

        [Test]
        public void Every_debug_member_is_inside_a_development_only_region() =>
            AssertDevelopmentOnly(MenuPath, DebugOnly);

        /// <summary>The grant itself moved into the request runner with the settings online (HANDOFF-M8 Task 100),
        /// so a guest's grant runs on the host — and it must stay out of a release there too.</summary>
        [Test]
        public void The_runners_debug_grant_is_inside_a_development_only_region() =>
            AssertDevelopmentOnly(
                "Assets/_BattleBomb/Gameplay/Items/PlayerRequestRunner.cs",
                new[] { "RollDebugItem(", "GrantCoins(", "DebugGrantQuality" });

        private static void AssertDevelopmentOnly(string path, string[] members)
        {
            string[] lines = File.ReadAllLines(path);
            int developmentDepth = 0;
            int depth = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("#if"))
                {
                    depth++;
                    if (line.Contains("DEVELOPMENT_BUILD") && line.Contains("UNITY_EDITOR"))
                    {
                        developmentDepth = developmentDepth == 0 ? depth : developmentDepth;
                    }

                    continue;
                }

                if (line.StartsWith("#else") && developmentDepth == depth)
                {
                    developmentDepth = 0;
                    continue;
                }

                if (line.StartsWith("#endif"))
                {
                    if (developmentDepth == depth)
                    {
                        developmentDepth = 0;
                    }

                    depth--;
                    continue;
                }

                if (line.StartsWith("///") || line.StartsWith("//"))
                {
                    continue;
                }

                foreach (string member in members)
                {
                    Assert.That(developmentDepth > 0 || !lines[i].Contains(member), Is.True,
                        $"{path}:{i + 1} mentions {member} outside a DEVELOPMENT_BUILD || UNITY_EDITOR region, " +
                        "so it compiles into a release.");
                }
            }
        }
    }
}
