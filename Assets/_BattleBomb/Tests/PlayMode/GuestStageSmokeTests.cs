using System.Collections;
using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Stats;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using BattleBomb.Gameplay.World.Markers;
using BattleBomb.Platform;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// A real guest machine fed a hand-built host that streams stages (Task 94): the launch stage, the next
    /// one behind the airlock, and the hand-over — with a dummy in the launch stage named by its stage and
    /// prop index. The second half of the recording is written once the guest has the launch stage, so it
    /// names the dummy and the exit line the guest actually has.
    /// </summary>
    public sealed class GuestStageSmokeTests
    {
        private const int Start = 1000;
        private const int LoadMargin = 120;
        private const int SecondPart = Start + 60;
        private const int HitAt = Start + 100;
        private const int DecoyHitAt = Start + 110;
        private const int HandOverAt = Start + 160;
        private const int DummiesUntil = HandOverAt - 12;
        private const int End = Start + 240;
        private const float Tolerance = 0.05f;

        private List<(int Frame, byte[] Payload)> _recording;
        private PlaybackTransport _playback;
        private NetGuest _guest;
        private SimulationDriver _driver;
        private StageRunner _runner;
        private TrainingDummy _dummy;
        private Vector3 _dummyAt;

        [UnitySetUp]
        public IEnumerator JoinAHostThatStreamsStages()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "guest-stage";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;

            _recording = FirstPart();
            _playback = new PlaybackTransport(_recording);
            NetSession.FindOrCreate().Join(_playback, "playback");
            for (int i = 0; i < 1500 && _guest == null; i++)
            {
                yield return null;
                _guest = Object.FindAnyObjectByType<NetGuest>();
            }

            Assert.That(_guest, Is.Not.Null, "The guest never loaded the host's run.");
            _driver = Object.FindAnyObjectByType<SimulationDriver>();
            _runner = Object.FindAnyObjectByType<StageRunner>();
            for (int i = 0; i < 1500 && !_runner.IsStageLoaded; i++)
            {
                yield return null;
            }

            Assert.That(_runner.IsStageLoaded, Is.True, "The guest never streamed the launch stage it was sent.");
            foreach (TrainingDummy dummy in Object.FindObjectsByType<TrainingDummy>(FindObjectsInactive.Exclude))
            {
                if (dummy.gameObject.scene == _runner.StageScene && dummy.PropIndex >= 0
                    && (_dummy == null || dummy.PropIndex < _dummy.PropIndex))
                {
                    _dummy = dummy;
                }
            }

            Assert.That(_dummy, Is.Not.Null, "The launch stage has no dummy for the host to name.");
            _dummyAt = _dummy.transform.position;
            StageExitMarker exit = null;
            foreach (StageExitMarker candidate in Object.FindObjectsByType<StageExitMarker>(FindObjectsInactive.Include))
            {
                if (candidate.gameObject.scene == _runner.StageScene)
                {
                    exit = candidate;
                }
            }

            Assert.That(exit, Is.Not.Null, "The launch stage has no exit line.");
            Assume.That(_guest.NewestHostFrame, Is.LessThan(SecondPart),
                "Set-up ran late: the recording's second half would arrive in one burst and prove less than it should.");
            AddSecondPart(_dummy.PropIndex, exit.X);
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
        public IEnumerator The_guest_streams_the_hosts_stages_and_hands_over_when_its_picture_does()
        {
            float firstRenderOnStageOne = -1f;
            void Sample(int frame)
            {
                if (firstRenderOnStageOne < 0f && _runner.StageIndex == 1)
                {
                    firstRenderOnStageOne = _guest.RenderFrame;
                }
            }

            _driver.Stepped += Sample;
            try
            {
                yield return PlayToTheEnd();
            }
            finally
            {
                _driver.Stepped -= Sample;
            }

            Assert.That(_runner.StageIndex, Is.EqualTo(1), "The guest never took the stage the host handed over.");
            Assert.That(firstRenderOnStageOne, Is.GreaterThanOrEqualTo(HandOverAt),
                "The guest handed over before its picture reached the host's hand-over.");
            Assert.That(firstRenderOnStageOne, Is.LessThan(HandOverAt + 2f), "The guest handed over well after its picture reached it.");
            Assert.That(SceneLoaded("FixtureStage2"), Is.True, "The stage behind the airlock was never streamed on the guest.");
            Assert.That(SceneLoaded("FixtureStage1"), Is.False, "The stage the host left is still loaded on the guest.");
        }

        [UnityTest]
        public IEnumerator A_dummy_is_named_by_its_stage_as_well_as_its_prop()
        {
            var raised = new List<HitEvent>();
            float worst = 0f;
            int compared = 0;
            void Record(HitEvent hit) => raised.Add(hit);
            void Sample(int frame)
            {
                float render = _guest.RenderFrame;
                if (render >= SecondPart + 2 && render <= DummiesUntil - 2)
                {
                    worst = Mathf.Max(worst, Mathf.Abs(_dummy.Position.x - DummyX(render)));
                    compared++;
                }
            }

            _driver.HitLanded += Record;
            _driver.Stepped += Sample;
            try
            {
                yield return PlayToTheEnd();
            }
            finally
            {
                _driver.HitLanded -= Record;
                _driver.Stepped -= Sample;
            }

            Assert.That(compared, Is.GreaterThan(20), "The guest drew almost nothing of the dummy to compare.");
            Assert.That(worst, Is.LessThan(Tolerance),
                $"The guest's dummy was drawn {worst:F2} from where the host's snapshots put it — or another stage's moved it.");
            Assert.That(raised.Count, Is.EqualTo(1),
                "The hit on this stage's dummy never landed, or one on another stage's dummy with the same prop index did.");
            Assert.That(raised[0].Target, Is.SameAs(_dummy));
        }

        [UnityTest]
        public IEnumerator A_stage_load_hard_on_a_hand_over_does_not_strand_it()
        {
            // The next preload right behind a hand-over, as a corridor stage whose gate opens at once
            // would send it. The fixture has two stages, so the next one here is the launch stage again,
            // far along.
            var writer = new NetWriter();
            StageCodec.WriteLoad(writer, new LoadStageMessage(0, false, _dummyAt.x + 400f, -1));
            int after = _recording.FindIndex(entry => entry.Frame > HandOverAt);
            _recording.Insert(after, (HandOverAt + NetProtocol.SnapshotEverySteps, writer.ToArray()));

            yield return PlayToTheEnd();

            Assert.That(_runner.StageIndex, Is.EqualTo(1),
                "A stage load that landed while a hand-over waited for the picture threw the hand-over away.");
        }

        private IEnumerator PlayToTheEnd()
        {
            for (int guard = 0; guard < 6000 && !Finished(); guard++)
            {
                yield return null;
            }

            Assert.That(Finished(), Is.True, "The guest never drew the end of the recording.");
            Assert.That(_guest.NewestHostFrame, Is.EqualTo(End), "The guest never received the whole recording.");
        }

        private bool Finished() => _playback.Finished && _guest.RenderFrame >= _guest.NewestHostFrame - 1;

        private float DummyX(float frame) => _dummyAt.x + (frame - SecondPart) * 0.05f;

        private static bool SceneLoaded(string name)
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

        /// <summary>The launch, the launch stage (both arrive while the guest is still loading and must be
        /// held), and snapshots up to the second part.</summary>
        private List<(int Frame, byte[] Payload)> FirstPart()
        {
            var recording = new List<(int Frame, byte[] Payload)>();
            var writer = new NetWriter();
            HandshakeCodec.WriteLaunch(writer, new LaunchMessage("fixture", 0, 0, -1, new[] { 0, 0 }, 1));
            recording.Add((Start - LoadMargin, writer.ToArray()));
            writer.Reset();
            StageCodec.WriteLoad(writer, new LoadStageMessage(0, true, 0f, -1));
            recording.Add((Start - LoadMargin, writer.ToArray()));
            for (int frame = Start; frame < SecondPart; frame += NetProtocol.SnapshotEverySteps)
            {
                recording.Add((frame, Snapshot(writer, frame, -1)));
            }

            return recording;
        }

        /// <summary>
        /// Written once the guest has the launch stage: the stage behind the airlock at the guest's own exit
        /// line; the dummy moving, and a decoy of the same prop index in the next stage far away; a hit on
        /// each; and the hand-over, sent after its step's snapshot as the host sends it.
        /// </summary>
        private void AddSecondPart(int prop, float exitX)
        {
            var writer = new NetWriter();
            StageCodec.WriteLoad(writer, new LoadStageMessage(1, false, exitX, -1));
            _recording.Add((SecondPart, writer.ToArray()));
            for (int frame = SecondPart; frame <= End; frame += NetProtocol.SnapshotEverySteps)
            {
                if (frame == HitAt || frame == DecoyHitAt)
                {
                    EntityRef target = EntityRef.Dummy(frame == HitAt ? 0 : 1, prop);
                    writer.Reset();
                    EventCodec.Write(writer, new[]
                    {
                        ReplicatedEvent.OfHit(new HitRecord(EntityRef.Player(0), target, 5f, _dummyAt, false, false, false)).At(frame),
                    });
                    _recording.Add((frame, writer.ToArray()));
                }

                // The two snapshots around the hand-over are lost on the way, as unreliable ones can be:
                // the guest must still hand over at the host's step, not at whatever it drew last.
                if (frame != HandOverAt - NetProtocol.SnapshotEverySteps && frame != HandOverAt)
                {
                    _recording.Add((frame, Snapshot(writer, frame, frame <= DummiesUntil ? prop : -1)));
                }

                if (frame == HandOverAt)
                {
                    writer.Reset();
                    StageCodec.WriteHandOver(writer, 1, HandOverAt);
                    _recording.Add((frame, writer.ToArray()));
                }
            }
        }

        private byte[] Snapshot(NetWriter writer, int frame, int dummyProp)
        {
            var world = new WorldSnapshot { HostFrame = frame, AckGuestFrame = -1 };
            world.Players.Add(Player(0, 0f));
            world.Players.Add(Player(1, 1f));
            if (dummyProp >= 0)
            {
                world.Dummies.Add(new DummySnapshot(0, dummyProp,
                    MotorState.AtRest(new Vector3(DummyX(frame), _dummyAt.y, _dummyAt.z)), Health.FromValues(100f, 100f)));
                world.Dummies.Add(new DummySnapshot(1, dummyProp,
                    MotorState.AtRest(_dummyAt + new Vector3(50f, 0f, 0f)), Health.FromValues(100f, 100f)));
            }

            writer.Reset();
            SnapshotCodec.Write(writer, world);
            return writer.ToArray();
        }

        private static PlayerSnapshot Player(int playerId, float x) => new PlayerSnapshot(
            playerId, MotorState.AtRest(new Vector3(x, 0f, 0f)), CombatState.Ready,
            new PlayerCondition(Health.FromValues(100f, 100f), 0, 0), ReviveChannel.Inactive,
            ManaPool.FromValues(50f, 50f), true, null, -1, 0, 0);
    }
}
