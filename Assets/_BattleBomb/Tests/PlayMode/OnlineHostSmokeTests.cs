using System.Collections;
using System.Collections.Generic;
using BattleBomb.Core.Chapters;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Loot;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using BattleBomb.Gameplay.World.Markers;
using BattleBomb.Platform;
using BattleBomb.Platform.Net;
using BattleBomb.UI.Chest;
using BattleBomb.UI.Frontend;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// D45's tripwire, online (HANDOFF-M8): the real Gameplay scene hosted, with Player 2 played by a
    /// headless guest over the loopback transport. It asks whether anything is completely broken —
    /// never how it feels; that is Michael's, with two editors (Task 96). Paced by
    /// <see cref="SimulationDriver.Frame"/>, as its siblings are, and with a partner in it always.
    /// </summary>
    public sealed class OnlineHostSmokeTests
    {
        private const int PatienceSteps = 1200;
        private const int FrameCeiling = 30000;
        private const int LoadFrameCeiling = 1500;
        private const string MachineScene = "Gameplay";
        private const int KnifeDefinitionId = 7;

        private SimulationDriver _driver;
        private StageRunner _runner;
        private CharacterActor _host;
        private CharacterActor _guestBody;
        private ScriptedCommandSource _hostInput;
        private HeadlessGuest _guest;

        [UnitySetUp]
        public IEnumerator HostWithAGuest()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "online-smoke";

            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            LoopbackTransport.CreatePair(out LoopbackTransport hostSide, out LoopbackTransport guestSide);
            NetSession net = NetSession.FindOrCreate();
            net.Host(hostSide);
            _guest = HeadlessGuest.Join(guestSide);
            yield return UntilFrames(() => net.IsConnected && _guest.IsWelcomed, "the handshake never finished");

            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
            flow.State.Confirm(0);
            flow.State.Launch(flow.Selection.CanLaunch);
            yield return UntilFrames(() => SceneManager.GetActiveScene().name == MachineScene, "the machine never loaded");
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
            yield return UntilFrames(() => _driver.Frame > 5, "the launch hold never released — the host is still waiting for its guest");

            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            Assert.That(actors.Count, Is.EqualTo(2), "A connected guest must wake the second seat (D59).");
            _host = actors[0];
            _guestBody = actors[1];

            // The host's own devices come out of the loop, as in every smoke suite; the guest's seat
            // already answers only to the wire.
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            _driver.Players.Unregister(_host.PlayerId);
            _hostInput = _host.gameObject.AddComponent<ScriptedCommandSource>();
            _hostInput.Bind(_host.PlayerId.Value);
            _driver.Players.Register(_hostInput);
            yield return null;
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
        public IEnumerator The_guest_is_told_which_run_to_load()
        {
            Assert.That(_guest.Launch.HasValue, Is.True, "The host never sent the guest a Launch.");
            Assert.That(_guest.Launch.Value.ChapterId, Is.EqualTo("fixture"));
            Assert.That(_guest.Launch.Value.GuestPlayerId, Is.EqualTo(1));
            Assert.That(_guest.Launch.Value.RosterPicks[1], Is.GreaterThanOrEqualTo(0),
                "The guest's seat went out with no hero in it.");
            yield break;
        }

        [UnityTest]
        public IEnumerator The_second_seat_answers_to_the_wire_not_to_a_device()
        {
            Assert.That(_driver.Players.TryGet(PlayerId.Two, out IPlayerCommandSource source), Is.True,
                "Nothing is registered for Player 2.");
            Assert.That(source, Is.InstanceOf<RemoteCommandSource>(),
                "Player 2 is still a local device — the host's own pad would drive the guest's body.");
            Assert.That(_guestBody.PlayerId, Is.EqualTo(PlayerId.Two));
            yield break;
        }

        [UnityTest]
        public IEnumerator The_guests_stick_moves_player_two_and_nobody_else()
        {
            Vector3 hostStart = _host.Position;
            Vector3 guestStart = _guestBody.Position;

            _guest.Move = Vector2.right;
            yield return Steps(60);
            _guest.Move = Vector2.zero;
            yield return Steps(10);

            Assert.That(_guestBody.Position.x, Is.GreaterThan(guestStart.x + 0.5f),
                "A second of the guest's stick moved Player 2 nowhere.");
            Assert.That(Vector3.Distance(_host.Position, hostStart), Is.LessThan(0.5f),
                "The guest's stick moved the host.");
        }

        [UnityTest]
        public IEnumerator A_guests_jump_is_one_jump()
        {
            _guest.Held = CommandButtons.Jump;
            yield return Steps(4);
            _guest.Held = CommandButtons.None;

            bool leftTheGround = false;
            for (int i = 0; i < 60 && !leftTheGround; i++)
            {
                yield return Steps(1);
                leftTheGround |= !_guestBody.IsGrounded;
            }

            Assert.That(leftTheGround, Is.True, "The guest pressed Jump and Player 2 never left the ground.");
            yield return Until(() => _guestBody.IsGrounded, "Player 2 never landed");
        }

        [UnityTest]
        public IEnumerator The_guests_start_and_confirm_never_reach_the_hosts_menus()
        {
            SettingsMenu settings = Object.FindAnyObjectByType<SettingsMenu>();
            Assert.That(settings, Is.Not.Null, "The gameplay scene has no SettingsMenu.");

            _guest.Held = CommandButtons.Pause | CommandButtons.Confirm;
            yield return Steps(4);
            _guest.Held = CommandButtons.None;
            yield return Steps(4);

            Assert.That(settings.IsOpen, Is.False, "The guest's Start opened the host's settings.");
            Assert.That(_driver.PausedForScreen, Is.False, "The guest's Start paused the host's world.");
        }

        [UnityTest]
        public IEnumerator A_guest_who_leaves_stops_driving_their_body()
        {
            _guest.Move = Vector2.left;
            yield return Steps(20);
            _guest.Leave();
            yield return Steps(40);

            Vector3 settled = _guestBody.Position;
            yield return Steps(30);
            Assert.That(Vector3.Distance(_guestBody.Position, settled), Is.LessThan(0.05f),
                "Player 2 kept running on the last stick heard after the guest left.");
        }

        [UnityTest]
        public IEnumerator A_guest_who_leaves_is_let_go_at_once_not_after_the_silence_cap()
        {
            _guest.Move = Vector2.left;
            yield return Steps(20);
            _guest.Leave();
            yield return Steps(5);

            Assert.That(_driver.CommandFor(PlayerId.Two.Value).Move, Is.EqualTo(Vector2.zero),
                "Player 2 ran on after the guest said goodbye — only the silence cap would have stopped them.");
        }

        [UnityTest]
        public IEnumerator The_stand_in_hero_never_enters_the_session_that_outlives_the_match()
        {
            Assert.That(_guestBody.gameObject.activeSelf, Is.True);
            Assert.That(GameSession.Find().Characters[1], Is.Null,
                "The guest's stand-in was written into the session; the front door would seat it as a local Player 2.");
            yield break;
        }

        [UnityTest]
        public IEnumerator Snapshots_carry_both_players_as_the_host_sees_them()
        {
            _guest.Move = Vector2.right;
            yield return Steps(40);
            _guest.Move = Vector2.zero;
            yield return Steps(6);

            WorldSnapshot latest = LatestSnapshot();
            Assert.That(latest, Is.Not.Null, "No snapshot reached the guest.");
            Assert.That(latest.HostFrame % NetProtocol.SnapshotEverySteps, Is.Zero);
            Assert.That(latest.Players.Count, Is.EqualTo(2));
            Assert.That(latest.TryGetPlayer(1, out PlayerSnapshot two), Is.True);
            Assert.That(Mathf.Abs(two.Motor.Position.x - _guestBody.Position.x), Is.LessThan(0.5f),
                "The snapshot's Player 2 is not where the host has them.");
            Assert.That(latest.AckGuestFrame, Is.GreaterThan(0), "The host never acknowledged a guest command.");
        }

        [UnityTest]
        public IEnumerator A_partner_shove_reaches_the_guest_as_a_hit_between_players()
        {
            float side = _host.Position.x <= _guestBody.Position.x ? -1f : 1f;
            yield return WalkHostTo(_guestBody.Position + new Vector3(side * 1.4f, 0f, 0f));

            // Face the partner before swinging: the walk may have ended facing away.
            _hostInput.Set(new Vector2(-side * 0.2f, 0f), CommandButtons.None);
            yield return Steps(2);
            _hostInput.Set(Vector2.zero, CommandButtons.Heavy);
            yield return Steps(3);
            _hostInput.Release();
            yield return Steps(40);

            bool found = false;
            foreach (ReplicatedEvent e in AllEvents())
            {
                found |= e.Kind == ReplicatedEventKind.Hit && e.Hit.IsPartner
                    && e.Hit.Attacker.Equals(EntityRef.Player(0)) && e.Hit.Target.Equals(EntityRef.Player(1));
            }

            Assert.That(found, Is.True, "The host's swing shoved Player 2 and the guest never heard about it.");
        }

        [UnityTest]
        public IEnumerator A_drop_reaches_the_guest_with_its_item()
        {
            ItemInstance knife = _driver.RollDebugItem(KnifeDefinitionId, 1f);
            int spawnedAt = _driver.Frame;
            _driver.SpawnDebugDrop(_host.Position + new Vector3(3f, 0f, 0f), knife);
            yield return Steps(6);

            ReplicatedEvent drop = default;
            foreach (ReplicatedEvent e in AllEvents())
            {
                if (e.Kind == ReplicatedEventKind.DropSpawned)
                {
                    drop = e;
                }
            }

            Assert.That(drop.Kind, Is.EqualTo(ReplicatedEventKind.DropSpawned), "The drop never reached the guest.");
            Assert.That(drop.Drop.Item.DefinitionId, Is.EqualTo(KnifeDefinitionId));
            Assert.That(drop.Drop.NetId, Is.GreaterThan(0));
            Assert.That(drop.HostFrame, Is.EqualTo(spawnedAt),
                "The drop crossed without the step it appeared in; the guest would place it against the wrong snapshot.");
        }

        [UnityTest]
        public IEnumerator A_grabbed_drop_reaches_the_guest_as_gone()
        {
            ItemInstance knife = _driver.RollDebugItem(KnifeDefinitionId, 1f);
            _driver.SpawnDebugDrop(_host.Position, knife);
            int netId = _driver.Pickups[_driver.Pickups.Count - 1].NetId;
            yield return Steps(40);

            _hostInput.Set(Vector2.zero, CommandButtons.Light);
            yield return Steps(3);
            _hostInput.Release();
            yield return Steps(10);

            Assert.That(PickupIdsOnTheHost(), Has.No.Member(netId), "The host never grabbed the drop at its feet.");
            Assert.That(RemovedDropIds(), Does.Contain(netId), "The drop was grabbed and the guest was never told it had gone.");
        }

        [UnityTest]
        public IEnumerator A_drop_swept_away_reaches_the_guest_as_gone()
        {
            ItemInstance knife = _driver.RollDebugItem(KnifeDefinitionId, 1f);
            _driver.SpawnDebugDrop(_host.Position + new Vector3(5f, 0f, 0f), knife);
            DropPickup drop = _driver.Pickups[_driver.Pickups.Count - 1];
            int netId = drop.NetId;
            Object.Destroy(drop.gameObject);
            yield return Steps(6);

            Assert.That(RemovedDropIds(), Does.Contain(netId), "A drop left the host's world and the guest was never told.");
        }

        [UnityTest]
        public IEnumerator A_wipe_announces_every_drop_it_clears_as_gone()
        {
            ItemInstance knife = _driver.RollDebugItem(KnifeDefinitionId, 1f);
            _driver.SpawnDebugDrop(_host.Position + new Vector3(5f, 0f, 0f), knife);
            int netId = _driver.Pickups[_driver.Pickups.Count - 1].NetId;
            yield return Steps(2);

            _driver.DebugDownPlayers();
            yield return Steps(140);

            Assert.That(PickupIdsOnTheHost(), Has.No.Member(netId), "The wipe never cleared the drop.");
            Assert.That(RemovedDropIds(), Does.Contain(netId), "A wipe cleared a drop and the guest was never told.");
        }

        [UnityTest]
        public IEnumerator Hundreds_of_drops_keep_the_host_sending_and_none_is_announced_gone()
        {
            ItemInstance knife = _driver.RollDebugItem(KnifeDefinitionId, 1f);
            for (int i = 0; i < NetProtocol.MaxEntities + 20; i++)
            {
                _driver.SpawnDebugDrop(_host.Position + new Vector3(8f, 0f, (i % 5) * 0.2f), knife);
            }

            int spawnedAt = _driver.Frame;
            yield return Steps(10);

            Assert.That(LatestSnapshot().HostFrame, Is.GreaterThan(spawnedAt),
                "The host stopped sending snapshots past hundreds of drops: a Stepped subscriber threw.");
            Assert.That(RemovedDropIds(), Is.Empty, "A drop never removed was announced as gone.");
        }

        [UnityTest]
        public IEnumerator The_airlock_waits_for_the_guest_to_have_the_next_stage()
        {
            _guest.AutoReady = false;
            StageExitMarker exit = Object.FindObjectsByType<StageExitMarker>(FindObjectsInactive.Include)[0];
            foreach (StageExitMarker candidate in Object.FindObjectsByType<StageExitMarker>(FindObjectsInactive.Include))
            {
                if (candidate.gameObject.scene == _runner.StageScene)
                {
                    exit = candidate;
                }
            }

            yield return PushBothRight(() => _guest.LoadRequests.Contains(1) && _host.Position.x >= exit.X - 0.05f
                && _guestBody.Position.x >= exit.X - 0.05f, 4000, "both players to stage one's exit");

            // The host's own copy of the next stage must be in, or the wait below would prove nothing.
            yield return UntilFrames(() => SceneIsLoaded("FixtureStage2"), "the host never streamed the next stage");
            LoadStageMessage request = _guest.LoadMessages.Find(load => load.StageIndex == 1);
            Assert.That(request.IsLaunch, Is.False);
            Assert.That(request.FirstArenaMinX, Is.EqualTo(exit.X).Within(0.01f),
                "The guest was not told to put the next stage where the host's exit line is.");

            // At the line with the guest not ready: the clamp must not reach past it, and no hand-over.
            yield return PushBothRight(() => false, 240, null);
            Assert.That(_runner.StageIndex, Is.Zero, "The host walked through the airlock before the guest had the stage behind it.");
            Assert.That(_host.Position.x, Is.LessThanOrEqualTo(exit.X + 0.01f),
                "The clamp opened into a stage the guest had not loaded.");

            _guest.Ready(1);
            yield return PushBothRight(() => _runner.StageIndex == 1, 1200, "the hand-over once the guest was ready");

            // The hand-over was sent this frame; the headless guest reads its inbox in its own Update,
            // which may already have run.
            yield return Steps(2);
            bool handedOver = false;
            foreach (byte[] message in _guest.Received)
            {
                var reader = new NetReader(message);
                handedOver |= (NetMessageKind)reader.ReadByte() == NetMessageKind.HandOver && StageCodec.ReadStage(reader) == 1;
            }

            Assert.That(handedOver, Is.True, "The guest was never told the stage was handed over.");
        }

        [UnityTest]
        public IEnumerator Every_dummy_names_the_stage_it_was_placed_in()
        {
            yield return Steps(4);
            int dummies = 0;
            foreach (TrainingDummy dummy in Object.FindObjectsByType<TrainingDummy>(FindObjectsInactive.Exclude))
            {
                if (dummy.PropIndex >= 0)
                {
                    dummies++;
                    Assert.That(dummy.StageIndex, Is.EqualTo(_runner.StageIndex), "A dummy does not carry the stage it stands in.");
                }
            }

            Assert.That(dummies, Is.GreaterThan(0), "The launch stage has no dummy to check.");
            WorldSnapshot latest = LatestSnapshot();
            Assert.That(latest.Dummies.Count, Is.EqualTo(dummies));
            foreach (DummySnapshot dummy in latest.Dummies)
            {
                Assert.That(dummy.StageIndex, Is.EqualTo(_runner.StageIndex), "A snapshot named a dummy with a stage it does not stand in.");
            }
        }

        [UnityTest]
        public IEnumerator A_remote_player_two_walks_the_fixture_chapter_to_its_end()
        {
            yield return PushBothRight(() => _runner.StageIndex == 1, 5000, "stage two, through the airlock");
            yield return PushBothRight(() => _runner.Phase == StagePhase.Complete, 5000, "the end of the chapter");

            Assert.That(_guest.LoadRequests, Is.EqualTo(new[] { 0, 1 }),
                "The guest was not asked to stream exactly the chapter's two stages.");
            Assert.That(_guest.Received.Exists(m => new NetReader(m).ReadByte() == (byte)NetMessageKind.HandOver), Is.True,
                "The guest was never told about the hand-over.");
            Assert.That(_driver.Characters.Ordered.Count, Is.EqualTo(2), "Player 2 did not finish the chapter with the host.");
        }

        private static bool SceneIsLoaded(string name)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == name && scene.isLoaded)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Both players push right — the host by script, the guest over the wire — until done,
        /// or the step budget runs out (null <paramref name="what"/> means running out is the point).</summary>
        private IEnumerator PushBothRight(System.Func<bool> done, int steps, string what)
        {
            _guest.Move = Vector2.right;
            _hostInput.Set(Vector2.right, CommandButtons.None);
            int deadline = _driver.Frame + steps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline; guard++)
            {
                if (done())
                {
                    break;
                }

                yield return null;
            }

            _guest.Move = Vector2.zero;
            _hostInput.Release();
            if (what != null && !done())
            {
                Assert.Fail($"Pushing right never reached {what} (host x={_host.Position.x:F2}, guest x={_guestBody.Position.x:F2}).");
            }
        }

        private List<int> PickupIdsOnTheHost()
        {
            var ids = new List<int>();
            foreach (DropPickup pickup in _driver.Pickups)
            {
                if (pickup != null)
                {
                    ids.Add(pickup.NetId);
                }
            }

            return ids;
        }

        private List<int> RemovedDropIds()
        {
            var ids = new List<int>();
            foreach (ReplicatedEvent e in AllEvents())
            {
                if (e.Kind == ReplicatedEventKind.DropRemoved)
                {
                    ids.Add(e.Drop.NetId);
                }
            }

            return ids;
        }

        private WorldSnapshot LatestSnapshot()
        {
            for (int i = _guest.Received.Count - 1; i >= 0; i--)
            {
                var reader = new NetReader(_guest.Received[i]);
                if ((NetMessageKind)reader.ReadByte() == NetMessageKind.Snapshot)
                {
                    var snapshot = new WorldSnapshot();
                    SnapshotCodec.Read(reader, snapshot);
                    return snapshot;
                }
            }

            return null;
        }

        private List<ReplicatedEvent> AllEvents()
        {
            var all = new List<ReplicatedEvent>();
            var batch = new List<ReplicatedEvent>();
            foreach (byte[] message in _guest.Received)
            {
                var reader = new NetReader(message);
                if ((NetMessageKind)reader.ReadByte() == NetMessageKind.Events)
                {
                    EventCodec.Read(reader, _driver.ItemSpecs, batch);
                    all.AddRange(batch);
                }
            }

            return all;
        }

        private IEnumerator WalkHostTo(Vector3 target)
        {
            int deadline = _driver.Frame + PatienceSteps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline; guard++)
            {
                Vector3 to = target - _host.Position;
                to.y = 0f;
                if (to.magnitude <= 0.3f)
                {
                    _hostInput.Release();
                    yield return Steps(2);
                    yield break;
                }

                _hostInput.Set(new Vector2(Mathf.Clamp(to.x, -1f, 1f), Mathf.Clamp(to.z, -1f, 1f)), CommandButtons.None);
                yield return null;
            }

            _hostInput.Release();
            Assert.Fail($"The host never reached {target} — stopped {Vector3.Distance(target, _host.Position):F2} away.");
        }

        private IEnumerator Steps(int steps)
        {
            int target = _driver.Frame + steps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < target; guard++)
            {
                yield return null;
            }
        }

        private IEnumerator Until(System.Func<bool> condition, string failure)
        {
            int deadline = _driver.Frame + PatienceSteps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{failure} (waited {PatienceSteps} steps).");
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

    /// <summary>The host's first step waits for the guest (HANDOFF-M8 planning decision 10).</summary>
    public sealed class OnlineLaunchHoldSmokeTests
    {
        [UnityTest]
        public IEnumerator The_host_holds_its_first_step_until_the_guest_has_loaded()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "online-hold";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;

            LoopbackTransport.CreatePair(out LoopbackTransport hostSide, out LoopbackTransport guestSide);
            NetSession net = NetSession.FindOrCreate();
            net.Host(hostSide);
            HeadlessGuest guest = HeadlessGuest.Join(guestSide);
            guest.AutoReady = false;
            try
            {
                for (int i = 0; i < 300 && !(net.IsConnected && guest.IsWelcomed); i++)
                {
                    yield return null;
                }

                FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
                flow.State.Confirm(0);
                flow.State.Confirm(0);
                flow.State.Launch(flow.Selection.CanLaunch);
                for (int i = 0; i < 1500 && SceneManager.GetActiveScene().name != "Gameplay"; i++)
                {
                    yield return null;
                }

                var driver = Object.FindAnyObjectByType<SimulationDriver>();
                var runner = Object.FindAnyObjectByType<StageRunner>();
                for (int i = 0; i < 1500 && !runner.IsStageLoaded; i++)
                {
                    yield return null;
                }

                int menuSteps = 0;
                void CountMenuStep() => menuSteps++;
                driver.MenuStepped += CountMenuStep;
                for (int i = 0; i < 120; i++)
                {
                    yield return null;
                }

                driver.MenuStepped -= CountMenuStep;
                Assert.That(driver.Frame, Is.Zero, "The host started the run without its guest.");
                Assert.That(menuSteps, Is.GreaterThan(0),
                    "The hold froze the host's menus too: a guest that never loads would leave no way out.");
                Assert.That(guest.LoadRequests, Does.Contain(0), "The guest was never asked to load the launch stage.");

                guest.Ready(0);
                for (int i = 0; i < 600 && driver.Frame <= 10; i++)
                {
                    yield return null;
                }

                Assert.That(driver.Frame, Is.GreaterThan(10), "The guest reported ready and the host never started.");

                RemoteCommandSource remote = Object.FindAnyObjectByType<RemoteCommandSource>();
                Assert.That(remote, Is.Not.Null);
                Assert.That(remote.Stream.Buffered, Is.LessThanOrEqualTo(NetProtocol.InputBufferMax),
                    "What the guest sent during the hold is being replayed at double speed.");
            }
            finally
            {
                Object.Destroy(guest.gameObject);
                Object.Destroy(GameSession.FindOrCreate().gameObject);
            }
        }
    }
}
