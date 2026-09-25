using System.Collections;
using BattleBomb.Core.Loot;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Platform;
using BattleBomb.UI.Frontend;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>Pre-M8 fix F3: a launch rolls its own; a bare machine keeps the authored seeds.</summary>
    public sealed class RunSeedsSmokeTests
    {
        /// <summary>What <c>Gameplay.unity</c> authors on the driver: loot 1, combat 2, spawn 3.</summary>
        private static readonly RunSeeds Authored = new RunSeeds(1u, 2u, 3u);

        [UnityTearDown]
        public IEnumerator Close()
        {
            GameSession session = GameSession.Find();
            if (session != null)
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator A_bare_machine_keeps_the_authored_seeds()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            SceneManager.LoadScene("Gameplay", LoadSceneMode.Single);
            yield return null;
            yield return null;

            Assert.That(Object.FindAnyObjectByType<SimulationDriver>().Seeds, Is.EqualTo(Authored),
                "With no session the smoke suites must roll exactly what they always have.");
        }

        [UnityTest]
        public IEnumerator Each_launch_rolls_its_own_and_the_machine_uses_them()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "seeds";

            yield return LaunchFromTheFrontDoor(firstTime: true);
            RunSeeds first = Object.FindAnyObjectByType<SimulationDriver>().Seeds;
            Assert.That(session.Seeds.HasValue, Is.True, "The launch drew no seeds onto the session.");
            Assert.That(first, Is.EqualTo(session.Seeds.Value), "The machine is not using the session's seeds.");
            Assert.That(first, Is.Not.EqualTo(Authored), "A launched run still rolls the authored constants.");

            yield return LaunchFromTheFrontDoor(firstTime: false);
            RunSeeds second = Object.FindAnyObjectByType<SimulationDriver>().Seeds;
            Assert.That(second, Is.Not.EqualTo(first), "Two launches rolled the same run.");
        }

        /// <summary>Title → characters → chapters → launch the first time; straight to chapters after,
        /// where the front door lands a player coming back from a run.</summary>
        private static IEnumerator LaunchFromTheFrontDoor(bool firstTime)
        {
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            if (firstTime)
            {
                flow.State.Confirm(0);
                flow.State.Confirm(0);
            }

            flow.State.Launch(flow.Selection.CanLaunch);
            for (int i = 0; i < 1500 && SceneManager.GetActiveScene().name != "Gameplay"; i++)
            {
                yield return null;
            }

            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Gameplay"), "The launch never reached the machine.");
        }
    }
}
