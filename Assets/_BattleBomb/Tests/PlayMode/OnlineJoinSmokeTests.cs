using System.Collections;
using System.Collections.Generic;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Core.Progression;
using BattleBomb.Core.Saves;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Items;
using BattleBomb.Gameplay.Loot;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
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
        private SimulationDriver _driver;
        private StageRunner _runner;
        private CharacterActor _host;
        private ScriptedCommandSource _hostInput;

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

        [UnityTest]
        public IEnumerator A_guest_who_readies_mid_run_waits_for_a_checkpoint_room_and_stands_up_there()
        {
            NetSession net = HostOverLoopback(out LoopbackTransport guestSide);
            yield return LaunchSolo();
            ItemInstance knife = _driver.RollDebugItem(7, 2.2f);
            _driver.SpawnDebugDrop(_host.Position + new Vector3(2f, 0f, 0f), knife);
            int dropId = _driver.Pickups[_driver.Pickups.Count - 1].NetId;

            _guest = HeadlessGuest.Join(guestSide);
            yield return UntilFrames(() => net.GuestReady, "the guest never readied");
            yield return Steps(60);
            Assert.That(_guest.Launch.HasValue, Is.False, "The guest was sent the run mid-fight; it must wait for a checkpoint room.");
            Assert.That(_driver.Characters.Ordered.Count, Is.EqualTo(1));

            yield return PushHostRight(() => _runner.Phase == StagePhase.AtCheckpoint, "the first checkpoint room");
            yield return Until(() => _driver.Characters.Ordered.Count == 2, "the guest never stood up in the room");

            Assert.That(_guest.Launch.HasValue, Is.True);
            Assert.That(_guest.Launch.Value.DropIn, Is.True, "The guest was not told it is dropping into a run already going.");
            LoadStageMessage launchStage = _guest.LoadMessages.Find(load => load.IsLaunch);
            Assert.That(launchStage.Placed, Is.True, "The late guest's launch stage was not placed where the host's is.");

            CharacterActor guestBody = _driver.Characters.Ordered[1];
            Assert.That(guestBody.PlayerId, Is.EqualTo(PlayerId.Two));
            Assert.That(_driver.Players.TryGet(PlayerId.Two, out IPlayerCommandSource source) && source is RemoteCommandSource, Is.True,
                "The late guest's body answers to something other than the wire.");
            Assert.That(Mathf.Abs(guestBody.Position.x - _host.Position.x), Is.LessThan(8f), "The guest stood up somewhere other than the host's room.");
            Assert.That(_driver.IsOnline, Is.True);

            yield return Steps(4);
            Assert.That(DropAnnounced(dropId), Is.True, "The drop already on the ground was never announced to the late guest.");
        }

        [UnityTest]
        public IEnumerator A_guest_still_in_their_lobby_never_holds_the_airlock_shut()
        {
            NetSession net = HostOverLoopback(out LoopbackTransport guestSide);
            yield return LaunchSolo();
            _guest = HeadlessGuest.Join(guestSide);
            _guest.AutoPick = false;
            yield return UntilFrames(() => net.IsConnected, "the guest never connected");

            yield return PushHostRight(() => _runner.StageIndex == 1, "stage two, through the airlock");
            Assert.That(_driver.Characters.Ordered.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator A_guest_dropping_in_stays_unseen_until_the_hosts_world_has_them()
        {
            const int Start = 1000;
            const int Appear = Start + 80;
            var recording = new List<(int Frame, byte[] Payload)>();
            var writer = new NetWriter();
            HandshakeCodec.WriteLaunch(writer, new LaunchMessage("fixture", 0, 0, -1, new[] { 0, 0 }, 1, dropIn: true));
            recording.Add((Start - 120, writer.ToArray()));
            for (int frame = Start; frame <= Start + 200; frame += NetProtocol.SnapshotEverySteps)
            {
                var world = new WorldSnapshot { HostFrame = frame, AckGuestFrame = -1 };
                world.Players.Add(Standing(0, -2f));
                if (frame >= Appear)
                {
                    world.Players.Add(Standing(1, 3f));
                }

                writer.Reset();
                SnapshotCodec.Write(writer, world);
                recording.Add((frame, writer.ToArray()));
            }

            var playback = new PlaybackTransport(recording);
            NetSession.FindOrCreate().Join(playback, "playback");
            NetGuest guest = null;
            for (int i = 0; i < LoadFrameCeiling && guest == null; i++)
            {
                yield return null;
                guest = Object.FindAnyObjectByType<NetGuest>();
            }

            Assert.That(guest, Is.Not.Null, "The guest never loaded the host's run.");
            var driver = Object.FindAnyObjectByType<SimulationDriver>();
            yield return UntilFrames(() => guest.RenderFrame >= Start + 10, "the guest never started drawing");
            Assert.That(driver.Characters.Ordered.Count, Is.EqualTo(1), "The guest's own body stood in the world before the host had it.");
            Assert.That(guest.WaitingToAppear, Is.True);
            Assert.That(Object.FindAnyObjectByType<UI.Combat.NetBanner>().Line, Does.Contain("checkpoint"));

            yield return UntilFrames(() => guest.RenderFrame >= Appear + 4, "the picture never reached the guest's arrival");
            Assert.That(driver.Characters.Ordered.Count, Is.EqualTo(2), "The guest never appeared once the host's world had them.");
            foreach (CharacterActor actor in driver.Characters.Ordered)
            {
                if (actor.PlayerId.Value == 1)
                {
                    Assert.That(actor.Position.x, Is.EqualTo(3f).Within(0.05f), "The guest appeared somewhere other than where the host put them.");
                }
            }

            Assert.That(guest.WaitingToAppear, Is.False);
        }

        [UnityTest]
        public IEnumerator A_guest_dropping_in_keeps_the_bag_the_host_sent_before_it_appeared()
        {
            // The host sends a late guest's whole bag once, as it binds them — while the guest's own body is still hidden
            // here (D59). It must land in that hidden bag: every later copy is the character alone (Task 101a).
            const int Start = 1000;
            const int Appear = Start + 80;
            var recording = new List<(int Frame, byte[] Payload)>();
            var writer = new NetWriter();
            HandshakeCodec.WriteLaunch(writer, new LaunchMessage("fixture", 0, 0, -1, new[] { 0, 0 }, 1, dropIn: true));
            recording.Add((Start - 120, writer.ToArray()));

            var brought = new Inventory();
            brought.Add(new ItemInstance(
                new ItemIdentity(7, "Knife", ItemSlot.Weapon, WeaponClass.Sword), QualityRank.Shiny,
                new GearContribution(weaponDamage: 9f), new AffixRoll[0], requiredLevel: 1), 99);
            writer.Reset();
            ParticipantCodec.Write(writer, 1, 5, true, SaveMapper.Participant(
                brought.Sack, new Wallet(100), new CharacterState(ElementId.None, XpLedger.Fresh, brought), withSack: true));
            byte[] copy = writer.ToArray();
            writer.Reset();
            SessionCodec.WriteMoment(writer, MomentKind.CheckpointReached);
            byte[] checkpoint = writer.ToArray();

            for (int frame = Start; frame <= Start + 200; frame += NetProtocol.SnapshotEverySteps)
            {
                if (frame == Appear)
                {
                    recording.Add((frame, copy));
                }
                else if (frame == Appear + 30)
                {
                    recording.Add((frame, checkpoint));
                }

                var world = new WorldSnapshot { HostFrame = frame, AckGuestFrame = -1 };
                world.Players.Add(Standing(0, -2f));
                if (frame >= Appear)
                {
                    world.Players.Add(Standing(1, 3f));
                }

                writer.Reset();
                SnapshotCodec.Write(writer, world);
                recording.Add((frame, writer.ToArray()));
            }

            NetSession.FindOrCreate().Join(new PlaybackTransport(recording), "playback");
            NetGuest guest = null;
            for (int i = 0; i < LoadFrameCeiling && guest == null; i++)
            {
                yield return null;
                guest = Object.FindAnyObjectByType<NetGuest>();
            }

            Assert.That(guest, Is.Not.Null, "The guest never loaded the host's run.");
            var driver = Object.FindAnyObjectByType<SimulationDriver>();
            yield return UntilFrames(() => guest.RenderFrame >= Appear + 50, "the picture never passed the host's checkpoint");

            PlayerInventory bag = driver.InventoryOf(1);
            Assert.That(bag, Is.Not.Null, "The guest never appeared.");
            Assert.That(bag.Wallet.Balance, Is.EqualTo(100),
                "The whole copy the host sent as it bound the guest was dropped while the guest was hidden: its bag is empty.");
            Assert.That(GameSession.Find().Store.TryRead("online-join", out string text), Is.True,
                "The host's checkpoint never wrote the guest's save.");
            Assert.That(SaveCodec.Decode(text).Save.Coins, Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator A_stage_asked_for_twice_on_the_guest_is_loaded_once()
        {
            const int Start = 1000;
            var recording = new List<(int Frame, byte[] Payload)>();
            var writer = new NetWriter();
            HandshakeCodec.WriteLaunch(writer, new LaunchMessage("fixture", 0, 0, -1, new[] { 0, 0 }, 1));
            recording.Add((Start - 120, writer.ToArray()));
            writer.Reset();
            StageCodec.WriteLoad(writer, new LoadStageMessage(0, true, 0f, -1));
            recording.Add((Start - 120, writer.ToArray()));
            writer.Reset();
            StageCodec.WriteLoad(writer, new LoadStageMessage(1, false, 200f, -1));
            byte[] preload = writer.ToArray();
            for (int frame = Start; frame <= Start + 200; frame += NetProtocol.SnapshotEverySteps)
            {
                if (frame == Start + 10)
                {
                    // The same stage asked for twice, back to back, before either load has landed.
                    recording.Add((frame, preload));
                    recording.Add((frame, preload));
                }

                var world = new WorldSnapshot { HostFrame = frame, AckGuestFrame = -1 };
                world.Players.Add(Standing(0, -2f));
                world.Players.Add(Standing(1, 2f));
                writer.Reset();
                SnapshotCodec.Write(writer, world);
                recording.Add((frame, writer.ToArray()));
            }

            NetSession.FindOrCreate().Join(new PlaybackTransport(recording), "playback");
            NetGuest guest = null;
            for (int i = 0; i < LoadFrameCeiling && guest == null; i++)
            {
                yield return null;
                guest = Object.FindAnyObjectByType<NetGuest>();
            }

            Assert.That(guest, Is.Not.Null);
            yield return UntilFrames(() => guest.RenderFrame >= Start + 150, "the guest never drew the recording");
            for (int i = 0; i < 60; i++)
            {
                yield return null;
            }

            int copies = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == "FixtureStage2" && scene.isLoaded)
                {
                    copies++;
                }
            }

            Assert.That(copies, Is.EqualTo(1), "A stage asked for twice stood in the world twice.");
        }

        [UnityTest]
        public IEnumerator A_hand_over_that_lands_before_its_stage_does_waits_for_it()
        {
            // A guest dropping in loads while the host walks on (D59): the host can hand over to a stage the guest has only
            // just been asked to load. The hand-over waits for that stage, or the guest is left on the stage behind.
            const int Start = 1000;
            var recording = new List<(int Frame, byte[] Payload)>();
            var writer = new NetWriter();
            HandshakeCodec.WriteLaunch(writer, new LaunchMessage("fixture", 0, 0, -1, new[] { 0, 0 }, 1, dropIn: true));
            recording.Add((Start - 120, writer.ToArray()));
            writer.Reset();
            StageCodec.WriteLoad(writer, new LoadStageMessage(0, true, 0f, -1));
            recording.Add((Start - 120, writer.ToArray()));
            writer.Reset();
            StageCodec.WriteLoad(writer, new LoadStageMessage(1, false, 200f, -1));
            byte[] preload = writer.ToArray();
            writer.Reset();
            StageCodec.WriteHandOver(writer, 1, Start + 2);
            byte[] handOver = writer.ToArray();
            for (int frame = Start; frame <= Start + 200; frame += NetProtocol.SnapshotEverySteps)
            {
                if (frame == Start + 20)
                {
                    // The next stage asked for and handed over to at once: it cannot have loaded by the step the
                    // hand-over falls due on.
                    recording.Add((frame, preload));
                    recording.Add((frame, handOver));
                }

                var world = new WorldSnapshot { HostFrame = frame, AckGuestFrame = -1 };
                world.Players.Add(Standing(0, -2f));
                writer.Reset();
                SnapshotCodec.Write(writer, world);
                recording.Add((frame, writer.ToArray()));
            }

            NetSession.FindOrCreate().Join(new PlaybackTransport(recording), "playback");
            NetGuest guest = null;
            for (int i = 0; i < LoadFrameCeiling && guest == null; i++)
            {
                yield return null;
                guest = Object.FindAnyObjectByType<NetGuest>();
            }

            Assert.That(guest, Is.Not.Null, "The guest never loaded the host's run.");
            StageRunner runner = Object.FindAnyObjectByType<StageRunner>();
            yield return UntilFrames(() => guest.RenderFrame >= Start + 150, "the guest never drew the recording");
            Assert.That(runner.StageIndex, Is.EqualTo(1),
                "A hand-over that landed before its stage had loaded was lost: the guest stayed on the stage behind.");
        }

        [UnityTest]
        public IEnumerator A_couch_pair_hosting_has_no_seat_to_offer_mid_run()
        {
            // A couch Player 2 has the second seat for the whole run (D59): no friend may drop in over them, whatever the
            // front door last said.
            NetSession net = HostOverLoopback(out LoopbackTransport guestSide);
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
            flow.State.Confirm(1);
            flow.State.Confirm(0);
            flow.State.Confirm(1);
            yield return UntilFrames(() => flow.State.Screen == FrontendScreen.Chapters, "the couch pair never reached the chapters");
            flow.State.Launch(flow.Selection.CanLaunch);
            yield return UntilFrames(() => SceneManager.GetActiveScene().name == "Gameplay", "the machine never loaded");
            yield return null;
            yield return null;

            Assert.That(Object.FindAnyObjectByType<SimulationDriver>().Characters.Ordered.Count, Is.EqualTo(2),
                "The couch pair did not both launch, so the case proves nothing.");
            Assert.That(Object.FindAnyObjectByType<NetHost>(), Is.Null, "The host's half came up to bind a friend over a couch Player 2.");
            Assert.That(net.IsFull, Is.True, "A couch pair's run offered a friend a seat.");
        }

        [UnityTest]
        public IEnumerator A_guest_still_in_their_lobby_stays_ready_when_the_hosts_run_ends()
        {
            // Only a guest who played spent what they brought (D61): one still waiting in their lobby when the host's run
            // ends is still ready with it, and their front door has nothing new to send.
            NetSession net = HostOverLoopback(out LoopbackTransport guestSide);
            yield return LaunchSolo();
            _guest = HeadlessGuest.Join(guestSide);
            yield return UntilFrames(() => net.GuestReady, "the guest never readied");
            yield return Steps(4);

            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            FrontendFlow flow = null;
            yield return UntilFrames(() => (flow = Object.FindAnyObjectByType<FrontendFlow>()) != null
                && flow.State.Screen == FrontendScreen.Chapters, "the host never came back to its chapters");
            yield return null;
            yield return null;

            Assert.That(net.GuestReady, Is.True, "The host un-readied a guest who never played, and their lobby will never send again.");
            Assert.That(flow.State.IsReady(1), Is.True);
        }

        [UnityTest]
        public IEnumerator A_guest_who_never_appeared_hears_nothing_of_the_chapters_end()
        {
            // A guest sent the run who has not yet stood up in it (D59) is not in the chapter the host finishes: its end must
            // not open their results, nor put the chapter's credit in their save.
            NetSession net = HostOverLoopback(out LoopbackTransport guestSide);
            yield return LaunchSolo();
            _guest = HeadlessGuest.Join(guestSide);
            _guest.AutoReady = false;
            yield return UntilFrames(() => net.GuestReady, "the guest never readied");

            yield return PushHostRight(() => _runner.StageIndex == 1, "stage two, through the airlock");
            yield return PushHostRight(() => _runner.Phase == StagePhase.Complete, "the end of the chapter");
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            Assert.That(_guest.Launch.HasValue, Is.True, "The guest was never sent the run, so the case proves nothing.");
            Assert.That(_driver.Characters.Ordered.Count, Is.EqualTo(1), "The guest was bound, so the case proves nothing.");
            foreach (byte[] message in _guest.Received)
            {
                var reader = new NetReader(message);
                if ((NetMessageKind)reader.ReadByte() == NetMessageKind.Moment)
                {
                    Assert.That(SessionCodec.ReadMoment(reader), Is.Not.EqualTo(MomentKind.ChapterCompleted),
                        "A guest who never appeared was told the host finished the chapter, and would keep its credit.");
                }
            }
        }

        [UnityTest]
        public IEnumerator A_guest_who_unreadies_as_the_run_goes_out_is_sent_back_to_their_lobby()
        {
            // The guest takes their ready back as the host sends them the run (D59): with nothing brought they cannot be
            // bound, so they go back to their lobby to ready again rather than wait unseen for the rest of the run.
            NetSession net = HostOverLoopback(out LoopbackTransport guestSide);
            yield return LaunchSolo();
            _guest = HeadlessGuest.Join(guestSide);
            _guest.AutoReady = false;
            yield return UntilFrames(() => net.GuestReady, "the guest never readied");
            yield return PushHostRight(() => _guest.Launch.HasValue, "the first checkpoint room");

            _guest.SendPick(0, false, null);
            yield return UntilFrames(() => !net.GuestReady, "the guest's un-ready never reached the host");
            foreach (int stage in _guest.LoadRequests)
            {
                _guest.Ready(stage);
            }

            yield return Steps(120);
            Assert.That(_driver.Characters.Ordered.Count, Is.EqualTo(1), "A guest who took back what they brought was bound with nothing.");
            Assert.That(_guest.SessionEnded, Is.True,
                "A guest who un-readied as the run went out was left waiting unseen for a bind that cannot come.");
        }

        private IEnumerator LaunchSolo()
        {
            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
            flow.State.Confirm(0);
            flow.State.Launch(flow.Selection.CanLaunch);
            yield return UntilFrames(() => SceneManager.GetActiveScene().name == "Gameplay", "the machine never loaded");
            yield return null;
            yield return null;

            _driver = Object.FindAnyObjectByType<SimulationDriver>();
            _runner = Object.FindAnyObjectByType<StageRunner>();
            _runner.SpawnsEnabled = false;
            foreach (EnemyActor enemy in Object.FindObjectsByType<EnemyActor>(FindObjectsInactive.Include))
            {
                Object.Destroy(enemy.gameObject);
            }

            yield return UntilFrames(() => _runner.IsStageLoaded, "the stage never streamed in");
            yield return UntilFrames(() => _driver.Frame > 5, "the run never started");
            Assert.That(_driver.Characters.Ordered.Count, Is.EqualTo(1), "A solo launch woke a second player.");

            _host = _driver.Characters.Ordered[0];
            DisableDevices();
            _driver.Players.Unregister(_host.PlayerId);
            _hostInput = _host.gameObject.AddComponent<ScriptedCommandSource>();
            _hostInput.Bind(_host.PlayerId.Value);
            _driver.Players.Register(_hostInput);
        }

        private IEnumerator PushHostRight(System.Func<bool> done, string what)
        {
            _hostInput.Set(Vector2.right, CommandButtons.None);
            int deadline = _driver.Frame + 5000;
            for (int guard = 0; guard < 30000 && _driver.Frame < deadline && !done(); guard++)
            {
                yield return null;
            }

            _hostInput.Release();
            Assert.That(done(), Is.True, $"Pushing right never reached {what}.");
        }

        private IEnumerator Steps(int steps)
        {
            int target = _driver.Frame + steps;
            for (int guard = 0; guard < 30000 && _driver.Frame < target; guard++)
            {
                yield return null;
            }
        }

        private IEnumerator Until(System.Func<bool> condition, string failure)
        {
            int deadline = _driver.Frame + 1200;
            for (int guard = 0; guard < 30000 && _driver.Frame < deadline; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{failure} (waited 1200 steps).");
        }

        private bool DropAnnounced(int netId)
        {
            var batch = new List<ReplicatedEvent>();
            foreach (byte[] message in _guest.Received)
            {
                var reader = new NetReader(message);
                if ((NetMessageKind)reader.ReadByte() != NetMessageKind.Events)
                {
                    continue;
                }

                EventCodec.Read(reader, null, batch);
                foreach (ReplicatedEvent e in batch)
                {
                    if (e.Kind == ReplicatedEventKind.DropSpawned && e.Drop.NetId == netId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static PlayerSnapshot Standing(int playerId, float x) => new PlayerSnapshot(
            playerId, MotorState.AtRest(new Vector3(x, 0f, 0f)), CombatState.Ready,
            new PlayerCondition(Health.FromValues(100f, 100f), 0, 0), ReviveChannel.Inactive,
            ManaPool.FromValues(50f, 50f), true, null, -1, 0, 0);

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
