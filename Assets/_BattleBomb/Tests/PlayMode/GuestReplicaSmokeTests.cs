using System.Collections;
using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Items;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Loot;
using BattleBomb.Gameplay.Net;
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
    /// hit. The picture must follow the snapshots; a drop no snapshot names must stay, because since
    /// Task 92's caps only a DropRemoved says a drop is gone.
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

        private PlaybackTransport _playback;
        private NetGuest _guest;
        private SimulationDriver _driver;

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
            session.Store = new MemorySaveStore();
            session.SaveName = "guest-replica";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;

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

            yield return null;
        }

        [UnityTest]
        public IEnumerator A_drop_no_snapshot_names_stays_and_a_removed_one_goes()
        {
            yield return PlayToTheEnd();

            Assert.That(PickupIds(), Does.Contain(KeptDrop),
                "A drop no snapshot named was deleted: since the snapshot caps, being left out is not being gone.");
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
        /// snapshot every second step that never names the kept drop, as though a cap left it out; the
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
                if (frame < RemovedAt)
                {
                    world.DropIds.Add(RemovedDrop);
                }

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
