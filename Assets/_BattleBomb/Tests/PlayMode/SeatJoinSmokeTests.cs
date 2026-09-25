using System.Collections;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Platform;
using BattleBomb.UI.Frontend;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// The front door seating a couch from real (virtual) devices (D57). The smoke suites swap the
    /// device sources out by design, so this is the one place the device path runs end to end.
    /// </summary>
    public sealed class SeatJoinSmokeTests : InputTestFixture
    {
        [UnitySetUp]
        public IEnumerator ClearSessions()
        {
            // A session left by another test would send the front door straight to chapter select.
            foreach (GameSession session in Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None))
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;

            // Made before the scene loads, because the front door reads the save in its OnEnable:
            // left to make its own session it would read the player's real save, and a Continue
            // on the title's first row would change what the first A does.
            GameSession.FindOrCreate().Store = new MemorySaveStore();
        }

        [UnityTearDown]
        public IEnumerator DropTheSession()
        {
            // The next suite loads Gameplay, and a session with nobody seated deactivates every player.
            foreach (GameSession session in Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None))
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Player_two_joins_on_a_second_controller_and_leaves_with_it()
        {
            InputSystem.AddDevice<Keyboard>();
            var first = InputSystem.AddDevice<Gamepad>();
            var second = InputSystem.AddDevice<Gamepad>();

            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;

            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            Assert.That(flow, Is.Not.Null, "The Frontend scene has no FrontendFlow.");
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Title));
            GameSession session = GameSession.Find();
            Assert.That(session, Is.Not.Null, "The front door did not keep the session.");
            Assert.That(session.Store, Is.InstanceOf<MemorySaveStore>(),
                "The front door made a session of its own instead of using the one set up for it.");

            yield return Tap(first.buttonSouth);
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Characters),
                "A on a controller did not start the game from the title.");
            Assert.That(session.Seats.FirstSeatHome, Is.EqualTo(first.deviceId));

            yield return Tap(second.buttonSouth);
            Assert.That(flow.State.IsJoined(1), Is.True, "A on the second controller did not join Player 2.");
            Assert.That(flow.State.IsReady(0), Is.False, "Player 2's join press readied Player 1.");
            Assert.That(session.Seats.SecondSeatDevice, Is.EqualTo(second.deviceId));

            yield return Tap(second.buttonEast);
            Assert.That(flow.State.IsJoined(1), Is.False, "B on Player 2's controller did not leave.");
            Assert.That(session.Seats.SecondSeatDevice, Is.EqualTo(SeatAssignment.NoDevice));
        }

        [UnityTest]
        public IEnumerator A_solo_player_back_at_character_select_on_another_pad_is_still_player_one()
        {
            InputSystem.AddDevice<Keyboard>();
            var first = InputSystem.AddDevice<Gamepad>();
            var second = InputSystem.AddDevice<Gamepad>();

            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;

            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            yield return Tap(first.buttonSouth);
            yield return Tap(first.buttonSouth);
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Chapters), "A alone did not ready Player 1.");

            // Picked up the other pad at chapter select, and backs out with it.
            yield return Tap(second.buttonEast);
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Characters));

            yield return Tap(second.buttonSouth);
            Assert.That(flow.State.IsJoined(1), Is.False, "Player 1's own pad joined a phantom Player 2.");
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Chapters),
                "A on the pad Player 1 came back on did not ready them.");
        }

        [UnityTest]
        public IEnumerator The_front_doors_buttons_answer_only_to_the_pointer()
        {
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;

            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            Assert.That(buttons, Is.Not.Empty, "The front door built no buttons.");
            foreach (Button button in buttons)
            {
                Assert.That(button.navigation.mode, Is.EqualTo(Navigation.Mode.None),
                    $"A click would leave '{button.name}' selected, and every pad's A or Enter would then " +
                    "press it a second time, unseen by the seats (D57).");
            }
        }

        private IEnumerator Tap(ButtonControl button)
        {
            Press(button);
            yield return null;
            yield return null;
            Release(button);
            yield return null;
            yield return null;
        }
    }
}
