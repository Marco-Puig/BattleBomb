using System.Collections;
using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Items;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Loot;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Platform;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// A real guest machine fed a hand-built host (Task 93, ahead of Task 95's recorded one): a launch,
    /// snapshots in which two players and an enemy move on known straight paths, and a few events — two
    /// drops announced while the guest is still loading, one of them later taken away, and a partner
    /// hit. The picture must follow the snapshots; a drop stays until a DropRemoved says otherwise,
    /// because snapshots carry no drops at all.
    /// </summary>
    public sealed class GuestReplicaSmokeTests
    {
        private const int Start = 1000;
        private const int Length = 240;

        /// <summary>The launch goes this far ahead of the first snapshot, so the guest's scene load
        /// never eats into what it is meant to draw.</summary>
        private const int LoadMargin = 120;
        private const int EnemyNetId = 7;
        private const int KeptDrop = 1;
        private const int RemovedDrop = 2;
        private const int RemovedAt = Start + 60;
        private const int HitAt = Start + 90;
        private const int TeleportAt = Start + 150;
        private const float TeleportJump = 10f;
        private const float Tolerance = 0.05f;

        /// <summary>The seat a guest plays in — the one NetGuest sends for.</summary>
        private static int GuestOwnPlayerId => GameSession.Find().Net.GuestPlayerId.Value;

        private PlaybackTransport _playback;
        private NetGuest _guest;
        private SimulationDriver _driver;
        private MemorySaveStore _store;
        private CharacterDefinition[] _couch;

        [UnitySetUp]
        public IEnumerator JoinAHandBuiltHost()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            _store = new MemorySaveStore();
            session.Store = _store;
            session.SaveName = "guest-replica";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;

            // The recording's launch picks roster entry 0 for both seats, so the couch holds a copy of
            // it: a distinct reference is the only way to tell the couch came back.
            _couch = new CharacterDefinition[] { Object.Instantiate(session.Roster[0]), null };
            session.Characters[0] = _couch[0];
            session.Characters[1] = _couch[1];

            _playback = new PlaybackTransport(Recording());
            NetSession.FindOrCreate().Join(_playback, "playback");
            for (int i = 0; i < 1500 && _guest == null; i++)
            {
                yield return null;
                _guest = Object.FindAnyObjectByType<NetGuest>();
            }

            Assert.That(_guest, Is.Not.Null, "The guest never loaded the host's run.");
            _driver = Object.FindAnyObjectByType<SimulationDriver>();
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            GameSession session = GameSession.Find();
            if (session != null)
            {
                Object.Destroy(session.gameObject);
            }

            if (_couch != null && _couch[0] != null)
            {
                Object.Destroy(_couch[0]);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator A_drop_stays_until_the_host_says_it_is_gone()
        {
            yield return PlayToTheEnd();

            Assert.That(PickupIds(), Does.Contain(KeptDrop),
                "A drop the host never removed was deleted — snapshots carry no drops at all.");
            Assert.That(PickupIds(), Has.No.Member(RemovedDrop), "A drop the host said had gone is still drawn.");
        }

        [UnityTest]
        public IEnumerator The_guests_picture_follows_the_host_snapshots()
        {
            int compared = 0;
            float worstPlayer = 0f;
            float worstEnemy = 0f;
            for (int guard = 0; guard < 6000 && !Finished(); guard++)
            {
                yield return null;
                float render = _guest.RenderFrame;
                if (render < Start || Mathf.Abs(render - TeleportAt) < NetProtocol.SnapshotEverySteps)
                {
                    continue;
                }

                foreach (CharacterActor actor in _driver.Characters.Ordered)
                {
                    worstPlayer = Mathf.Max(worstPlayer, Mathf.Abs(actor.Position.x - PlayerX(actor.PlayerId.Value, render)));
                }

                foreach (EnemyActor enemy in Object.FindObjectsByType<EnemyActor>(FindObjectsInactive.Exclude))
                {
                    if (enemy.NetId == EnemyNetId)
                    {
                        worstEnemy = Mathf.Max(worstEnemy, Mathf.Abs(enemy.Position.x - EnemyX(render)));
                    }
                }

                compared++;
            }

            Assert.That(compared, Is.GreaterThan(60), "The guest drew almost nothing to compare.");
            Assert.That(_guest.NewestHostFrame, Is.EqualTo(Start + Length), "The guest never received the whole recording.");
            Assert.That(_driver.Characters.Ordered.Count, Is.EqualTo(2), "The guest does not draw both players.");
            Assert.That(worstPlayer, Is.LessThan(Tolerance), $"A player was drawn {worstPlayer:F2} from the host's snapshots.");
            Assert.That(worstEnemy, Is.LessThan(Tolerance), $"The enemy was drawn {worstEnemy:F2} from the host's snapshots.");
            Assert.That(EnemyOnTheGuest(), Is.True, "The host's enemy never appeared on the guest by its id.");
        }

        [UnityTest]
        public IEnumerator A_hit_is_raised_on_the_guest_when_the_picture_reaches_its_step()
        {
            var raised = new List<(float Render, HitEvent Hit)>();
            void Record(HitEvent hit) => raised.Add((_guest.RenderFrame, hit));
            _driver.HitLanded += Record;
            try
            {
                yield return PlayToTheEnd();
            }
            finally
            {
                _driver.HitLanded -= Record;
            }

            Assert.That(raised.Count, Is.EqualTo(1), "The host's one hit was raised on the guest a different number of times.");
            Assert.That(raised[0].Render, Is.GreaterThanOrEqualTo(HitAt), "The hit was raised before the picture reached its step.");
            Assert.That(raised[0].Render, Is.LessThan(HitAt + 2f), "The hit was raised well after the picture reached its step.");
            Assert.That(raised[0].Hit.IsPartner, Is.True);
            var attacker = raised[0].Hit.Attacker as CharacterActor;
            var target = raised[0].Hit.Target as CharacterActor;
            Assert.That(attacker != null && attacker.PlayerId.Value == 0, Is.True, "The hit's attacker is not the guest's Player 1.");
            Assert.That(target != null && target.PlayerId.Value == 1, Is.True, "The hit's target is not the guest's Player 2.");
        }

        [UnityTest]
        public IEnumerator A_teleport_is_drawn_as_one_never_as_a_slide()
        {
            CharacterActor one = null;
            foreach (CharacterActor actor in _driver.Characters.Ordered)
            {
                if (actor.PlayerId.Value == 0)
                {
                    one = actor;
                }
            }

            Assert.That(one, Is.Not.Null);
            float widest = 0f;
            void Sample(int frame) => widest = Mathf.Max(widest, Vector3.Distance(one.PreviousPosition, one.Position));
            _driver.Stepped += Sample;
            try
            {
                yield return PlayToTheEnd();
            }
            finally
            {
                _driver.Stepped -= Sample;
            }

            Assert.That(one.Position.x, Is.GreaterThan(PlayerX(0, TeleportAt) - 1f), "Player 1 never reached the far side of the teleport.");
            Assert.That(widest, Is.LessThan(NetProtocol.ReplicaTeleportDistance),
                $"Player 1 was drawn sliding {widest:F2} between two steps across a teleport.");
        }

        [UnityTest]
        public IEnumerator A_guest_who_leaves_mid_match_writes_no_save()
        {
            yield return AdvanceUntil(() => _guest.RenderFrame >= Start, "The guest never started drawing.");

            GameSession.Find().Net.Leave();
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(NetSession.GameplayScene),
                "A guest who left mid-match was pulled out of the replica scene.");

            Object.FindAnyObjectByType<SaveService>().SaveNow();
            Assert.That(_store.Names(), Is.Empty,
                "A guest who left mid-match wrote the replica's empty stash over their save.");
        }

        [UnityTest]
        public IEnumerator The_guests_couch_comes_back_after_the_match()
        {
            GameSession session = GameSession.Find();
            Assert.That(session.Characters, Is.Not.EqualTo(_couch),
                "The launch never replaced the couch — this case proves nothing.");

            _playback.Disconnect(default);
            yield return AdvanceUntil(() => SceneManager.GetActiveScene().name == NetSession.FrontendScene,
                "The guest never returned to the front door once the host vanished.");

            Assert.That(session.Characters, Is.EqualTo(_couch),
                "The guest's front door lost its couch to the host's picks.");
        }

        [UnityTest]
        public IEnumerator The_guests_own_menu_never_reaches_the_host()
        {
            const CommandButtons South = CommandButtons.Jump | CommandButtons.Confirm;

            CharacterActor own = null;
            foreach (CharacterActor actor in _driver.Characters.Ordered)
            {
                if (actor.PlayerId.Value == GuestOwnPlayerId)
                {
                    own = actor;
                }
            }

            Assert.That(own, Is.Not.Null, $"Player {GuestOwnPlayerId} — the launch's guest seat — never spawned.");
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            _driver.Players.Unregister(own.PlayerId);
            var source = own.gameObject.AddComponent<ScriptedCommandSource>();
            source.Bind(own.PlayerId.Value);
            _driver.Players.Register(source);

            yield return AdvanceUntil(() => _guest.RenderFrame >= Start, "The guest never started drawing.");

            // Stepped fires after SettingsMenu has ticked (it subscribed first, in its own OnEnable),
            // so sampling here catches the menu's state and this step's own raw buttons exactly —
            // unlike reading _driver.Frame between yields, which the editor can advance by more than
            // one step per render frame.
            var openAfter = new Dictionary<int, bool>();
            var raw = new Dictionary<int, CommandButtons>();
            void RecordStep(int frame)
            {
                openAfter[frame] = _driver.MenuPauseHeld;
                raw[frame] = _driver.CommandFor(GuestOwnPlayerId).Held;
            }

            _driver.Stepped += RecordStep;
            try
            {
                // Pause opens the guest's own settings menu the same step it is pressed, and that step
                // has already sent — the send that must go out neutral starts the step after.
                source.Set(Vector2.zero, CommandButtons.Pause);
                yield return AdvanceSteps(1);
                source.Release();
                yield return AdvanceUntil(() => _driver.MenuPauseHeld, "Pause never opened the guest's own settings menu.");

                // AutoEquip is the first row, so a fresh Confirm here only toggles it — Back is what closes.
                source.Set(Vector2.right, South);
                yield return AdvanceSteps(3);
                source.Set(Vector2.right, South | CommandButtons.Back);
                yield return AdvanceSteps(1);
                Assert.That(_driver.MenuPauseHeld, Is.False, "Back never closed the guest's own settings menu.");

                // South stays held across the close (D57's held-across rule) before being let go and pressed fresh.
                source.Set(Vector2.zero, South);
                yield return AdvanceSteps(10);
                source.Release();
                yield return AdvanceSteps(5);
                source.Set(Vector2.zero, CommandButtons.Jump);
                yield return AdvanceSteps(5);
                source.Release();
            }
            finally
            {
                _driver.Stepped -= RecordStep;
            }

            var sent = new Dictionary<int, WireCommand>();
            var batch = new List<WireCommand>();
            foreach (byte[] message in _playback.Sent)
            {
                var reader = new NetReader(message);
                if ((NetMessageKind)reader.ReadByte() != NetMessageKind.Commands)
                {
                    continue;
                }

                CommandCodec.Read(reader, batch);
                foreach (WireCommand command in batch)
                {
                    sent[command.Frame] = command;
                }
            }

            var frames = new List<int>(sent.Keys);
            frames.Sort();

            bool sawDuringOpen = false;
            bool sawBetween = false;
            bool sawFreshJump = false;
            bool inBetweenRun = false;
            foreach (int frame in frames)
            {
                // The send for frame f went out before f's own menu tick, so it saw the menu as it
                // stood after f - 1's tick.
                if (!openAfter.TryGetValue(frame - 1, out bool openAtSend))
                {
                    continue;
                }

                WireCommand command = sent[frame];
                if (openAtSend)
                {
                    sawDuringOpen = true;
                    inBetweenRun = true;
                    Assert.That(command.Held, Is.EqualTo(CommandButtons.None),
                        $"Frame {frame}: the guest's own menu was open and a held button reached the host.");
                    Assert.That(command.Move, Is.EqualTo(Vector2.zero),
                        $"Frame {frame}: the guest's own menu was open and its stick reached the host.");
                    continue;
                }

                if (inBetweenRun)
                {
                    bool rawStillHasJump = raw.TryGetValue(frame, out CommandButtons rawHeld)
                        && (rawHeld & CommandButtons.Jump) != CommandButtons.None;
                    if (rawStillHasJump)
                    {
                        sawBetween = true;
                        Assert.That(command.Held & CommandButtons.Jump, Is.EqualTo(CommandButtons.None),
                            $"Frame {frame}: South held across the menu's close reached the host as a jump.");
                        continue;
                    }

                    inBetweenRun = false;
                }

                if ((command.Held & CommandButtons.Jump) != CommandButtons.None)
                {
                    sawFreshJump = true;
                }
            }

            Assert.That(sawDuringOpen, Is.True, "Nothing was sent while the guest's own menu was open — the case proves nothing.");
            Assert.That(sawBetween, Is.True, "Nothing was sent between the close and the release — the case proves nothing.");
            Assert.That(sawFreshJump, Is.True, "Jump never reached the host once South was fully let go and pressed again.");
        }

        private IEnumerator AdvanceSteps(int steps)
        {
            int target = _driver.Frame + steps;
            for (int guard = 0; guard < 6000 && _driver.Frame < target; guard++)
            {
                yield return null;
            }
        }

        private IEnumerator AdvanceUntil(System.Func<bool> condition, string failure)
        {
            for (int guard = 0; guard < 6000 && !condition(); guard++)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, failure);
        }

        private IEnumerator PlayToTheEnd()
        {
            for (int guard = 0; guard < 6000 && !Finished(); guard++)
            {
                yield return null;
            }

            Assert.That(Finished(), Is.True, "The guest never drew the end of the recording.");
            Assert.That(_guest.NewestHostFrame, Is.EqualTo(Start + Length), "The guest never received the whole recording.");
        }

        private bool Finished() => _playback.Finished && _guest.RenderFrame >= _guest.NewestHostFrame - 1;

        private List<int> PickupIds()
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

        private static bool EnemyOnTheGuest()
        {
            foreach (EnemyActor enemy in Object.FindObjectsByType<EnemyActor>(FindObjectsInactive.Exclude))
            {
                if (enemy.NetId == EnemyNetId)
                {
                    return true;
                }
            }

            return false;
        }

        private static float PlayerX(int playerId, float frame) =>
            playerId == 0
                ? -2f + (frame - Start) * 0.1f + (frame >= TeleportAt ? TeleportJump : 0f)
                : 2f - (frame - Start) * 0.1f;

        private static float EnemyX(float frame) => 4f + (frame - Start) * 0.05f;

        /// <summary>
        /// What a host would have sent, in the order it would have sent it: the launch; both drops
        /// announced at once, so they arrive while the guest is still loading (and must be held); a
        /// snapshot every second step, which never names either drop — snapshots carry no drops; the
        /// removal of the other; and a partner hit.
        /// </summary>
        private static List<(int Frame, byte[] Payload)> Recording()
        {
            var recording = new List<(int Frame, byte[] Payload)>();
            var writer = new NetWriter();
            HandshakeCodec.WriteLaunch(writer, new LaunchMessage("fixture", 0, 0, -1, new[] { 0, 0 }, 1));
            recording.Add((Start - LoadMargin, writer.ToArray()));

            ItemInstance knife = ItemWire.StandIn(QualityRank.Shiny);
            recording.Add((Start - LoadMargin, Events(writer,
                ReplicatedEvent.OfDrop(new DropRecord(KeptDrop, new Vector3(-1f, 0.35f, 1f), knife)).At(Start - LoadMargin),
                ReplicatedEvent.OfDrop(new DropRecord(RemovedDrop, new Vector3(1f, 0.35f, 1f), knife)).At(Start - LoadMargin))));

            for (int frame = Start; frame <= Start + Length; frame += NetProtocol.SnapshotEverySteps)
            {
                if (frame == RemovedAt)
                {
                    recording.Add((frame, Events(writer, ReplicatedEvent.OfDropRemoved(RemovedDrop).At(frame))));
                }

                if (frame == HitAt)
                {
                    recording.Add((frame, Events(writer, ReplicatedEvent.OfHit(new HitRecord(
                        EntityRef.Player(0), EntityRef.Player(1), 0f, Vector3.zero, true, false, false)).At(frame))));
                }

                var world = new WorldSnapshot { HostFrame = frame, AckGuestFrame = -1 };
                world.Players.Add(Player(0, PlayerX(0, frame)));
                world.Players.Add(Player(1, PlayerX(1, frame)));
                world.Enemies.Add(new EnemySnapshot(
                    EnemyNetId, 0, 0, false, -1, MotorState.AtRest(new Vector3(EnemyX(frame), 0f, 2f)), EnemyState.Fresh,
                    Health.FromValues(30f, 30f), -1, 0, null));

                writer.Reset();
                SnapshotCodec.Write(writer, world);
                recording.Add((frame, writer.ToArray()));
            }

            return recording;
        }

        private static PlayerSnapshot Player(int playerId, float x) => new PlayerSnapshot(
            playerId, MotorState.AtRest(new Vector3(x, 0f, 0f)), CombatState.Ready,
            new PlayerCondition(Health.FromValues(100f, 100f), 0, 0), ReviveChannel.Inactive,
            ManaPool.FromValues(50f, 50f), true, null, -1, 0, 0);

        private static byte[] Events(NetWriter writer, params ReplicatedEvent[] events)
        {
            writer.Reset();
            EventCodec.Write(writer, events);
            return writer.ToArray();
        }
    }
}
