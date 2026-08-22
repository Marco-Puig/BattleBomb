using System;
using System.Linq;
using System.Reflection;
using BattleBomb.Core.Players;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// Makes the architecture rules machine-checkable rather than aspirational (§8). A failure here is
    /// a design violation, not a flaky test — fix the code, never the assertion.
    /// </summary>
    public sealed class ArchitectureFitnessTests
    {
        private static Assembly Core => typeof(PlayerCommand).Assembly;

        [Test]
        public void Core_contains_no_UnityEngine_Object_types()
        {
            // Covers MonoBehaviour, ScriptableObject, GameObject and Component in one assertion:
            // all of them derive from UnityEngine.Object, and none may exist in Core (§2).
            string[] offenders = Core.GetTypes()
                .Where(t => typeof(UnityEngine.Object).IsAssignableFrom(t))
                .Select(t => t.FullName)
                .ToArray();

            Assert.That(offenders, Is.Empty,
                "Core must be constructible from a plain unit test with no scene loaded. " +
                "Move these types to Gameplay: " + string.Join(", ", offenders));
        }

        [Test]
        public void Core_references_no_other_BattleBomb_assembly()
        {
            string[] offenders = Core.GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(n => n.StartsWith("BattleBomb", StringComparison.Ordinal))
                .ToArray();

            Assert.That(offenders, Is.Empty,
                "Core depends on nothing of ours (§1). Referenced: " + string.Join(", ", offenders));
        }

        [TestCase("BattleBomb.Gameplay", "BattleBomb.Presentation")]
        [TestCase("BattleBomb.Gameplay", "BattleBomb.UI")]
        [TestCase("BattleBomb.Platform", "BattleBomb.Gameplay")]
        [TestCase("BattleBomb.Platform", "BattleBomb.Presentation")]
        [TestCase("BattleBomb.Platform", "BattleBomb.UI")]
        public void Dependency_direction_is_one_way(string assemblyName, string forbiddenReference)
        {
            Assembly assembly = FindAssembly(assemblyName);
            if (assembly == null)
            {
                Assert.Ignore($"{assemblyName} is not loaded — it has no code yet.");
            }

            bool references = assembly.GetReferencedAssemblies()
                .Any(a => a.Name == forbiddenReference);

            Assert.That(references, Is.False,
                $"{assemblyName} must not reference {forbiddenReference} — dependency direction is never negotiated (§1).");
        }

        private static Assembly FindAssembly(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
    }
}
