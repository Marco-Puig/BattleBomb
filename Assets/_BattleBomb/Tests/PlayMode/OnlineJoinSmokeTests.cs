using System.Collections;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Saves;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Platform;
using BattleBomb.Platform.Net;
using BattleBomb.UI.Frontend;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// D59's joins and D61's leaving (HANDOFF-M8 Stage D): the lobby at character select from both sides, a couch
    /// pair turning a guest away with a reason, and — from Tasks 103 and 104 — dropping in at a checkpoint room and
    /// leaving. Host sides use the headless guest over the loopback; guest sides a recording.
    /// </summary>
    public sealed class OnlineJoinSmokeTests
    {
        private const int LoadFrameCeiling = 1500;

        private HeadlessGuest _guest;

        [UnitySetUp]
        public IEnumerator OpenTheFrontDoor()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "online-join";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;
            DisableDevices();
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            if (_guest != null)
            {
                Object.Destroy(_guest.gameObject);
            }

            GameSession session = GameSession.Find();
            if (session != null)
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator The_host_launches_only_once_its_guest_is_ready_and_the_guest_plays_their_own_pick()
        {
            NetSession net = HostOverLoopback(out LoopbackTransport guestSide);
            _guest = HeadlessGuest.Join(guestSide);
            _guest.AutoPick = false;
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            yield return UntilFrames(() => flow.State.IsRemote(1), "the guest never took Player 2's slot");

            flow.State.Confirm(0);
            flow.State.Confirm(0);
            yield return null;
            yield return null;
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Characters), "The host moved on without its guest.");
            yield return UntilFrames(() => _guest.HostLobby.HasValue && _guest.HostLobby.Value.HostReady,
                "the guest never heard the host was ready");

            _guest.SendPick(0, true, null);
            yield return UntilFrames(() => flow.State.Screen == FrontendScreen.Chapters, "the guest's ready never moved the host on");
            flow.State.Launch(flow.Selection.CanLaunch);
            yield return UntilFrames(() => SceneManager.GetActiveScene().name == "Gameplay", "the machine never loaded");

            yield return UntilFrames(() => _guest.Launch.HasValue, "the guest was never told which run to load");
            Assert.That(_guest.Launch.Value.RosterPicks[1], Is.EqualTo(0), "The launch did not seat the guest's own pick.");
            Assert.That(GameSession.Find().Characters[1], Is.Null, "The guest's hero entered the host's session.");
            Assert.That(net.GuestReady, Is.True);
        }

        [UnityTest]
        public IEnumerator A_couch_pair_turns_a_guest_away_and_both_sides_keep_the_reason()
        {
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
            flow.State.Confirm(1);
            Assert.That(flow.State.IsJoined(1), Is.True);

            NetSession net = HostOverLoopback(out LoopbackTransport guestSide);
            yield return null;
            yield return null;
            _guest = HeadlessGuest.Join(guestSide);
            yield return UntilFrames(() => _guest.Refusal != null, "the full game never turned the guest away");

            Assert.That(_guest.Refusal, Does.Contain("full"));
            yield return UntilFrames(() => net.Peer.IsNone, "the refused guest's connection never closed");
            Assert.That(net.Status, Does.Contain("Refused"), "The host's reason was overwritten when the guest closed.");
            Assert.That(net.IsConnected, Is.False);
        }

        [UnityTest]
        public IEnumerator The_guests_front_door_is_a_lobby_that_sends_its_pick_and_can_leave()
        {
            var playback = new PlaybackTransport(new System.Collections.Generic.List<(int Frame, byte[] Payload)>());
            NetSession net = NetSession.FindOrCreate();
            net.Join(playback, "playback");

            // A click in the frame the join lands, before the front door has looked at the role, is the lobby's too.
            foreach (UnityEngine.UI.Button button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude))
            {
                if (button.name == "Primary")
                {
                    button.onClick.Invoke();
                }
            }

            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            yield return UntilFrames(() => flow.Lobby != null && net.IsConnected, "the guest's front door never became a lobby");

            flow.Lobby.Confirm();
            yield return null;
            yield return null;
            LobbyPick sent = default;
            bool found = false;
            foreach (byte[] message in playback.Sent)
            {
                var reader = new NetReader(message);
                if ((NetMessageKind)reader.ReadByte() == NetMessageKind.LobbyPick)
                {
                    sent = LobbyCodec.ReadPick(reader);
                    found = true;
                }
            }

            Assert.That(found, Is.True, "The guest's lobby sent the host nothing.");
            Assert.That(sent.Ready, Is.True);
            Assert.That(sent.Brought, Is.Not.Null, "A ready guest brought nothing of its own save.");
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Title), "The guest's own front door moved while it was a guest.");

            flow.Lobby.Back();
            flow.Lobby.Back();
            yield return UntilFrames(() => net.Role == NetRole.Offline, "Back from not-ready never left the game");
            yield return null;
            Assert.That(flow.Lobby, Is.Null, "The lobby outlived the connection.");
        }

        [UnityTest]
        public IEnumerator A_second_match_waits_for_the_guests_fresh_pick()
        {
            // D61: after a match the guest's front door sends its pick again, from the save that match wrote. A launch
            // before it arrives would restore the guest from what it brought into the match before — and its next
            // autosave would write that over everything it earned since.
            NetSession net = HostOverLoopback(out LoopbackTransport guestSide);
            _guest = HeadlessGuest.Join(guestSide);
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            yield return UntilFrames(() => flow.State.IsReady(1), "the guest never readied in the lobby");
            flow.State.Confirm(0);
            flow.State.Confirm(0);
            yield return UntilFrames(() => flow.State.Screen == FrontendScreen.Chapters, "the host never reached its chapters");
            flow.State.Launch(flow.Selection.CanLaunch);
            yield return UntilFrames(() => _guest.Launch.HasValue, "the guest was never told which run to load");
            yield return UntilFrames(() => SceneManager.GetActiveScene().name == "Gameplay", "the machine never loaded");
            yield return null;

            // The host goes back to its chapters mid-run: the match ends for both.
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return UntilFrames(() => _guest.SessionEnded, "the guest was never told the match ended");
            yield return UntilFrames(() => (flow = Object.FindAnyObjectByType<FrontendFlow>()) != null
                && flow.State.Screen == FrontendScreen.Chapters, "the host never came back to its chapters");
            yield return null;
            yield return null;

            Assert.That(flow.State.IsReady(1), Is.False, "The guest still stood ready with the save it brought into the last match.");
            flow.State.Launch(flow.Selection.CanLaunch);
            Assert.That(flow.State.Screen, Is.EqualTo(FrontendScreen.Chapters), "The host launched again before the guest's fresh pick arrived.");

            _guest.SendPick(0, true, SaveMapper.Participant(new Inventory().Sack, new Wallet(250),
                new CharacterState(ElementId.None, XpLedger.Fresh, new Inventory()), withSack: true));
            yield return UntilFrames(() => flow.State.IsReady(1), "the guest's fresh pick never readied it");
            Assert.That(net.GuestBrought.Coins, Is.EqualTo(250), "The next match would restore the guest from an old save.");
        }

        private static NetSession HostOverLoopback(out LoopbackTransport guestSide)
        {
            LoopbackTransport.CreatePair(out LoopbackTransport hostSide, out guestSide);
            NetSession net = NetSession.FindOrCreate();
            net.Host(hostSide);
            return net;
        }

        private static void DisableDevices()
        {
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }
        }

        private static IEnumerator UntilFrames(System.Func<bool> condition, string failure)
        {
            for (int guard = 0; guard < LoadFrameCeiling; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{failure} (waited {LoadFrameCeiling} frames).");
        }
    }
}
