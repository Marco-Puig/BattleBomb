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
        public void Every_debug_member_is_inside_a_development_only_region()
        {
            string[] lines = File.ReadAllLines(MenuPath);
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

                foreach (string member in DebugOnly)
                {
                    Assert.That(developmentDepth > 0 || !lines[i].Contains(member), Is.True,
                        $"{MenuPath}:{i + 1} mentions {member} outside a DEVELOPMENT_BUILD || UNITY_EDITOR region, " +
                        "so it compiles into a release.");
                }
            }
        }
    }
}
