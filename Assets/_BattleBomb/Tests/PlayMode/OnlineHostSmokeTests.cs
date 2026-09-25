using System.Collections;
using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
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
        public IEnumerator A_drop_reaches_the_guest_with_its_item_and_is_named_after()
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
            Assert.That(LatestSnapshot().DropIds, Does.Contain(drop.Drop.NetId),
                "Snapshots do not name the drop the guest was told about.");
        }

        [UnityTest]
        public IEnumerator Past_the_snapshot_bound_the_drops_nearest_the_players_are_the_ones_named()
        {
            // One at the guest's feet before the far ones and one after, so keeping the oldest or the
            // newest cuts one of them, and only keeping the nearest names both.
            ItemInstance knife = _driver.RollDebugItem(KnifeDefinitionId, 1f);
            _driver.SpawnDebugDrop(_guestBody.Position, knife);
            int firstAtTheirFeet = _driver.Pickups[_driver.Pickups.Count - 1].NetId;
            Vector3 away = _host.Position + new Vector3(8f, 0f, 0f);
            for (int i = 0; i < NetProtocol.MaxEntities + 20; i++)
            {
                _driver.SpawnDebugDrop(away + new Vector3(0f, 0f, (i % 5) * 0.2f), knife);
            }

            _driver.SpawnDebugDrop(_guestBody.Position, knife);
            int lastAtTheirFeet = _driver.Pickups[_driver.Pickups.Count - 1].NetId;
            int spawnedAt = _driver.Frame;
            yield return Steps(6);

            WorldSnapshot latest = LatestSnapshot();
            Assert.That(latest.HostFrame, Is.GreaterThan(spawnedAt),
                "The host stopped sending snapshots past the bound: a Stepped subscriber threw.");
            Assert.That(latest.DropIds.Count, Is.EqualTo(NetProtocol.MaxEntities));
            Assert.That(latest.DropIds, Does.Contain(firstAtTheirFeet),
                "The oldest drop at the guest's feet was cut to name one farther away.");
            Assert.That(latest.DropIds, Does.Contain(lastAtTheirFeet),
                "The newest drop at the guest's feet was cut to name one farther away.");
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
}
