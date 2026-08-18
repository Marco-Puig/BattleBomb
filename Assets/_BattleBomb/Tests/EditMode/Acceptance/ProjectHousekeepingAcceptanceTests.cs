using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Acceptance
{
    /// <summary>
    /// The URP template's leftovers. Harmless individually; collectively they are how a project ends
    /// up with two of everything and no way to tell which one is live.
    /// </summary>
    public sealed class ProjectHousekeepingAcceptanceTests
    {
        [Test]
        public void The_template_input_asset_is_gone()
        {
            Assert.That(File.Exists(AcceptanceFixture.TemplateControlsPath), Is.False,
                $"{AcceptanceFixture.TemplateControlsPath} still exists. The project has its own " +
                "action asset; two of them means the wrong one gets wired sooner or later.");

            Assert.That(File.Exists(AcceptanceFixture.TemplateControlsPath + ".meta"), Is.False,
                "The template asset's .meta outlived the asset.");
        }

        [Test]
        public void Nothing_still_references_the_template_input_asset()
        {
            List<string> referencing = SearchableFiles()
                .Where(f => File.ReadAllText(f).Contains(AcceptanceFixture.TemplateControlsGuid))
                .Select(f => f.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, string.Empty))
                .ToList();

            Assert.That(referencing, Is.Empty,
                "These still point at the deleted template asset by GUID: " + string.Join(", ", referencing) +
                ". Project-wide input actions live in ProjectSettings — repoint or clear that setting " +
                "before deleting the asset, or the reference dangles.");
        }

        private static IEnumerable<string> SearchableFiles()
        {
            string root = Directory.GetCurrentDirectory();

            IEnumerable<string> settings = Directory.Exists(Path.Combine(root, "ProjectSettings"))
                ? Directory.GetFiles(Path.Combine(root, "ProjectSettings"), "*.asset", SearchOption.TopDirectoryOnly)
                : Enumerable.Empty<string>();

            string[] patterns = { "*.unity", "*.prefab", "*.asset" };
            IEnumerable<string> assets = patterns.SelectMany(p =>
                Directory.GetFiles(Path.Combine(root, "Assets"), p, SearchOption.AllDirectories));

            return settings.Concat(assets);
        }
    }
}
