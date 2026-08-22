using System.Reflection;
using BattleBomb.Gameplay.Simulation;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode
{
    /// <summary>
    /// The three numbers M6 left on the scene are gone from the driver for good (D48's
    /// consequence). A fallback field "just in case" is how a stage's authored loot progress
    /// silently loses to a forgotten inspector value — so their absence is asserted.
    /// </summary>
    public sealed class EncounterWiringTests
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [TestCase("_lootProgress")]
        [TestCase("_climateRows")]
        [TestCase("_climate")]
        [TestCase("_arena")]
        [TestCase("StoryProgressLevel")]
        public void The_driver_no_longer_carries_the_scene_dial(string member)
        {
            Assert.That(typeof(SimulationDriver).GetField(member, Any), Is.Null,
                $"SimulationDriver still has '{member}'. Stages own it now (D48); delete the field, " +
                "do not default it.");
        }

        [Test]
        public void The_driver_exposes_the_encounter_it_was_given()
        {
            PropertyInfo encounter = typeof(SimulationDriver).GetProperty("Encounter");
            Assert.That(encounter, Is.Not.Null);
            Assert.That(encounter.PropertyType, Is.EqualTo(typeof(Core.Chapters.EncounterInputs)));
        }
    }
}
