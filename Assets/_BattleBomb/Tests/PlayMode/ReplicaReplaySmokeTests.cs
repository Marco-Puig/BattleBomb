using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BattleBomb.Core.Net;
using BattleBomb.Gameplay.Characters;
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
    /// D45's tripwire for the mirror (HANDOFF-M8 Task 95): a hosted fight is recorded — what the host
    /// sent and where everything truly was — then replayed into a guest machine, whose picture must
    /// match the truth at the frame it is drawing. Tolerances allow for drawing between snapshots two
    /// steps apart; a forgotten field, a wrong id, or a stuck replica is far outside them.
    /// </summary>
    public sealed class ReplicaReplaySmokeTests
    {
        private const int RecordSteps = 600;
        private const float PlayerTolerance = 0.3f;
        private const float EnemyTolerance = 0.5f;

        /// <summary>The <c>Stepped</c> subscribers a guest is known to carry, each vetted: it either
        /// only reads, or returns early on a replica (the runner). A new one fails this until someone
        /// decides which it is (HANDOFF-M8 standing watch).</summary>
        private static readonly string[] VettedGuestSubscribers =
        {
            "StageRunner", "ChestScreenHost", "SettingsMenu", "ResultsScreen", "CastTell",
        };

        private readonly Dictionary<int, Truth> _truth = new Dictionary<int, Truth>();
        private readonly HashSet<int> _jumps = new HashSet<int>();

        [UnityTest]
        public IEnumerator A_replayed_host_run_draws_what_the_host_simulated()
        {
            List<(int Frame, byte[] Payload)> recording = null;
            yield return Record(r => recording = r);
            Assert.That(recording.Count, Is.GreaterThan(RecordSteps / 4), "Almost nothing was recorded.");

            GameSession fresh = GameSession.FindOrCreate();
            fresh.Store = new MemorySaveStore();
            fresh.SaveName = "replay-guest";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var playback = new PlaybackTransport(recording);
            NetSession.FindOrCreate().Join(playback, "playback");

            NetGuest guest = null;
            for (int i = 0; i < 1500 && guest == null; i++)
            {
                yield return null;
                guest = Object.FindAnyObjectByType<NetGuest>();
            }

            Assert.That(guest, Is.Not.Null, "The guest never loaded the host's run.");
            var driver = Object.FindAnyObjectByType<SimulationDriver>();

            AssertSteppedSubscribersAreVetted(driver);

            int compared = 0;
            int enemiesCompared = 0;
            var drawn = new HashSet<int>();
            float worstPlayer = 0f;
            float worstEnemy = 0f;
            for (int guard = 0; guard < 6000 && !(playback.Finished && guest.RenderFrame >= guest.NewestHostFrame - 1); guard++)
            {
                yield return null;
                float render = guest.RenderFrame;
                if (render < 0f || NearAJump(render) || !TryTruthAt(render, out Vector3 one, out Vector3 two, out Dictionary<int, Vector3> enemies))
                {
                    continue;
                }

                foreach (CharacterActor actor in driver.Characters.Ordered)
                {
                    Vector3 expected = actor.PlayerId.Value == 0 ? one : two;
                    worstPlayer = Mathf.Max(worstPlayer, Planar(actor.Position - expected));
                }

                foreach (EnemyActor enemy in Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None))
                {
                    if (enemy.NetId > 0 && enemies.TryGetValue(enemy.NetId, out Vector3 at))
                    {
                        worstEnemy = Mathf.Max(worstEnemy, Planar(enemy.Position - at));
                        enemiesCompared++;
                    }
                }

                drawn.Add(Mathf.FloorToInt(render));
                compared++;
            }

            Assert.That(playback.Finished && guest.RenderFrame >= guest.NewestHostFrame - 1, Is.True,
                "The replica never drew the end of the recording: a picture stuck on one step compares only with itself.");
            Assert.That(drawn.Count, Is.GreaterThan(RecordSteps / 4), "The replica drew too few of the host's steps to prove it follows them.");
            Assert.That(driver.Characters.Ordered.Count, Is.EqualTo(2), "The replica does not hold both players.");
            Assert.That(enemiesCompared, Is.GreaterThan(0), "No replica enemy was ever matched to the truth by its id.");
            AssertSteppedSubscribersAreVetted(driver);
            Assert.That(compared, Is.GreaterThan(100), "The replica drew almost nothing worth comparing.");
            Assert.That(worstPlayer, Is.LessThan(PlayerTolerance), $"A replica player was {worstPlayer:F2} from the truth.");
            Assert.That(worstEnemy, Is.LessThan(EnemyTolerance), $"A replica enemy was {worstEnemy:F2} from the truth.");
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            foreach (HeadlessGuest guest in Object.FindObjectsByType<HeadlessGuest>(FindObjectsSortMode.None))
            {
                Object.Destroy(guest.gameObject);
            }

            GameSession session = GameSession.Find();
            if (session != null)
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;
        }

        /// <summary>A hosted fight with spawns on and nobody pressing anything but the guest's stick:
        /// enemies walk, swing, stagger players, maybe wipe them — everything a snapshot must carry.</summary>
        private IEnumerator Record(System.Action<List<(int Frame, byte[] Payload)>> done)
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "replay-host";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            LoopbackTransport.CreatePair(out LoopbackTransport hostSide, out LoopbackTransport guestSide);
            NetSession.FindOrCreate().Host(hostSide);
            HeadlessGuest guest = HeadlessGuest.Join(guestSide);
            for (int i = 0; i < 300 && !guest.IsWelcomed; i++)
            {
                yield return null;
            }

            SimulationDriver driver = null;
            guest.Clock = () => driver != null ? driver.Frame : 0;

            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
            flow.State.Confirm(0);
            flow.State.Launch(flow.Selection.CanLaunch);
            for (int i = 0; i < 1500 && SceneManager.GetActiveScene().name != "Gameplay"; i++)
            {
                yield return null;
            }

            driver = Object.FindAnyObjectByType<SimulationDriver>();
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            driver.Stepped += frame =>
            {
                Truth now = Truth.Of(driver);
                if (_truth.TryGetValue(frame - 1, out Truth before) && now.JumpedFrom(before))
                {
                    _jumps.Add(frame);
                }

                _truth[frame] = now;
            };
            guest.Move = new Vector2(0.4f, 0.2f);

            int stop = 0;
            for (int i = 0; i < 20000 && (stop == 0 || driver.Frame < stop); i++)
            {
                if (stop == 0 && driver.Frame > 0)
                {
                    stop = driver.Frame + RecordSteps;
                }

                yield return null;
            }

            done(new List<(int Frame, byte[] Payload)>(guest.Recorded));
            Object.Destroy(guest.gameObject);
            Object.Destroy(GameSession.Find().gameObject);
            yield return null;
        }

        private bool TryTruthAt(float render, out Vector3 one, out Vector3 two, out Dictionary<int, Vector3> enemies)
        {
            int from = Mathf.FloorToInt(render);
            one = default;
            two = default;
            enemies = null;
            if (!_truth.TryGetValue(from, out Truth a) || !_truth.TryGetValue(from + 1, out Truth b))
            {
                return false;
            }

            float t = render - from;
            one = Vector3.Lerp(a.One, b.One, t);
            two = Vector3.Lerp(a.Two, b.Two, t);
            enemies = new Dictionary<int, Vector3>();
            foreach (KeyValuePair<int, Vector3> entry in a.Enemies)
            {
                enemies[entry.Key] = b.Enemies.TryGetValue(entry.Key, out Vector3 next)
                    ? Vector3.Lerp(entry.Value, next, t)
                    : entry.Value;
            }

            return true;
        }

        /// <summary>A step in which something moved further than its tolerance — a teleport, a respawn
        /// after a wipe, a hard knockback — is drawn between snapshots two steps apart and read from the
        /// truth between two steps: both right, and up to half that move apart for a frame or two. Those
        /// frames prove nothing either way; <c>GuestReplicaSmokeTests</c> pins the snap itself.</summary>
        private bool NearAJump(float render)
        {
            int from = Mathf.FloorToInt(render);
            for (int frame = from - NetProtocol.SnapshotEverySteps; frame <= from + 1 + NetProtocol.SnapshotEverySteps; frame++)
            {
                if (_jumps.Contains(frame))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertSteppedSubscribersAreVetted(SimulationDriver driver)
        {
            FieldInfo backing = typeof(SimulationDriver).GetField("Stepped", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(backing, Is.Not.Null, "SimulationDriver.Stepped is no longer a field-like event; update this check.");
            var stepped = backing.GetValue(driver) as System.Delegate;
            if (stepped == null)
            {
                return;
            }

            foreach (System.Delegate handler in stepped.GetInvocationList())
            {
                string type = handler.Target != null ? handler.Target.GetType().Name : handler.Method.DeclaringType.Name;
                Assert.That(VettedGuestSubscribers, Does.Contain(type),
                    $"{type} subscribes to Stepped on a guest. Decide whether it changes the simulation: if it " +
                    "does, it must be host-only or return early on a replica; then add it to VettedGuestSubscribers.");
            }
        }

        /// <summary>Distance on the ground plane — the replica's height between snapshots is the one
        /// thing interpolation legitimately shaves off an arc.</summary>
        private static float Planar(Vector3 delta) => new Vector2(delta.x, delta.z).magnitude;

        private readonly struct Truth
        {
            public readonly Vector3 One;
            public readonly Vector3 Two;
            public readonly Dictionary<int, Vector3> Enemies;

            private Truth(Vector3 one, Vector3 two, Dictionary<int, Vector3> enemies)
            {
                One = one;
                Two = two;
                Enemies = enemies;
            }

            public static Truth Of(SimulationDriver driver)
            {
                Vector3 one = default;
                Vector3 two = default;
                foreach (CharacterActor actor in driver.Characters.Ordered)
                {
                    if (actor.PlayerId.Value == 0)
                    {
                        one = actor.Position;
                    }
                    else
                    {
                        two = actor.Position;
                    }
                }

                var enemies = new Dictionary<int, Vector3>();
                foreach (ISimTarget target in driver.Targets.Ordered)
                {
                    if (target is EnemyActor enemy && enemy.NetId > 0)
                    {
                        enemies[enemy.NetId] = enemy.Position;
                    }
                }

                return new Truth(one, two, enemies);
            }

            public bool JumpedFrom(Truth before)
            {
                if (Planar(One - before.One) > PlayerTolerance || Planar(Two - before.Two) > PlayerTolerance)
                {
                    return true;
                }

                foreach (KeyValuePair<int, Vector3> entry in Enemies)
                {
                    if (before.Enemies.TryGetValue(entry.Key, out Vector3 was) && Planar(entry.Value - was) > EnemyTolerance)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
