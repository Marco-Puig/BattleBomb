using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// Pre-M8 fix F1's tripwires: whatever the simulation removes is gone from the step it is removed
    /// in, not from the end of the frame. The driver runs up to five steps a frame; on Unity 6 a
    /// <c>Destroy</c> runs <c>OnDisable</c> — where every actor, dummy and interactable leaves its
    /// registry — at once, though the object itself lingers to the frame's end. These cases pin that:
    /// if an upgrade ever deferred it again, they go red here instead of as a hitch on M8's host.
    /// Every case forces five steps a frame — the life of any machine under 60 fps — and checks from
    /// inside the step.
    /// </summary>
    public sealed class SameStepRemovalSmokeTests
    {
        private const string SceneName = "Gameplay";
        private const float FiveStepsAFrame = 5f / 60f;
        private const int PatienceSteps = 3000;
        private const int FrameCeiling = 20000;

        private readonly List<ScriptedCommandSource> _inputs = new List<ScriptedCommandSource>();
        private readonly List<string> _violations = new List<string>();
        private SimulationDriver _driver;
        private StageRunner _runner;
        private CharacterActor _player;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            _driver = Object.FindAnyObjectByType<SimulationDriver>();
            _runner = Object.FindAnyObjectByType<StageRunner>();
            Assert.That(_driver, Is.Not.Null, "The gameplay scene has no SimulationDriver.");
            Assert.That(_runner, Is.Not.Null, "The gameplay scene has no StageRunner.");

            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            _inputs.Clear();
            _violations.Clear();
            foreach (CharacterActor actor in _driver.Characters.Ordered)
            {
                _driver.Players.Unregister(actor.PlayerId);
                var input = actor.gameObject.AddComponent<ScriptedCommandSource>();
                input.Bind(actor.PlayerId.Value);
                _driver.Players.Register(input);
                _inputs.Add(input);
            }

            _player = _driver.Characters.Ordered[0];
            yield return UntilFrames(() => _runner.IsStageLoaded, "the fixture stage never streamed in");
            Time.captureDeltaTime = FiveStepsAFrame;
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.captureDeltaTime = 0f;
            foreach (ScriptedCommandSource input in _inputs)
            {
                if (input != null)
                {
                    input.Release();
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator A_cleared_attempts_enemies_are_gone_from_the_step_it_resets()
        {
            yield return Until(() => Enemies().Count > 0, "no wave ever spawned");

            var lastSeen = new List<EnemyActor>();
            var oldBrood = new List<EnemyActor>();
            bool reset = false;
            int resetFrame = -1;

            System.Action onReset = () =>
            {
                reset = true;
                resetFrame = _driver.Frame;
                oldBrood.AddRange(lastSeen);
            };
            System.Action<int> onStep = frame =>
            {
                if (reset)
                {
                    foreach (EnemyActor enemy in Enemies())
                    {
                        if (oldBrood.Contains(enemy))
                        {
                            _violations.Add($"step {frame}: {enemy.name}, cleared by the reset, is still a target");
                        }
                    }
                }

                lastSeen.Clear();
                lastSeen.AddRange(Enemies());
            };
            System.Action<HitEvent> onHit = hit =>
            {
                if (reset && hit.Attacker is EnemyActor enemy && oldBrood.Contains(enemy))
                {
                    _violations.Add($"{enemy.name}, cleared by the reset, landed a hit after it");
                }
            };

            _driver.AttemptReset += onReset;
            _driver.Stepped += onStep;
            _driver.HitLanded += onHit;
            try
            {
                _driver.DebugDownPlayers();
                yield return Until(() => reset && _driver.Frame > resetFrame + 20, "the attempt never reset");
            }
            finally
            {
                _driver.AttemptReset -= onReset;
                _driver.Stepped -= onStep;
                _driver.HitLanded -= onHit;
            }

            Assert.That(oldBrood, Is.Not.Empty, "No enemy existed when the attempt reset, so nothing was tested.");
            Assert.That(_violations, Is.Empty, string.Join("\n", _violations));
        }

        [UnityTest]
        public IEnumerator A_despawned_corpse_is_gone_from_the_step_it_despawns()
        {
            yield return Until(() => Enemies().Count > 0, "no wave ever spawned");

            var deaths = new List<Vector3>();
            int kills = 0;
            System.Action<EnemyDeath> onDeath = death =>
            {
                deaths.Add(death.Position);
                kills++;
            };
            System.Action<int> onStep = frame =>
            {
                foreach (Vector3 at in deaths)
                {
                    foreach (EnemyActor enemy in Enemies())
                    {
                        Vector3 delta = enemy.Position - at;
                        if (enemy.IsDepleted && new Vector2(delta.x, delta.z).sqrMagnitude < 1e-6f)
                        {
                            _violations.Add($"step {frame}: the corpse at {at} is still a target after it despawned");
                        }
                    }
                }

                deaths.Clear();
            };

            _driver.EnemyDied += onDeath;
            _driver.Stepped += onStep;
            try
            {
                yield return FightUntil(() => kills > 0);
            }
            finally
            {
                _driver.EnemyDied -= onDeath;
                _driver.Stepped -= onStep;
            }

            Assert.That(kills, Is.GreaterThan(0), "Player one never killed anything, so nothing was tested.");
            Assert.That(_violations, Is.Empty, string.Join("\n", _violations));
        }

        [UnityTest]
        public IEnumerator Every_corpse_whose_beat_ends_in_a_step_is_settled_in_that_step_in_order()
        {
            yield return Until(() => Living().Count >= 3, "no wave of three ever spawned");

            // Depleted together, between two frames, so all their dying beats end in the same step.
            List<EnemyActor> doomed = Living();
            MethodInfo deplete = typeof(EnemyActor).GetMethod(
                "ApplyStatusDamage", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(deplete, Is.Not.Null, "EnemyActor has no ApplyStatusDamage to deplete it with.");
            foreach (EnemyActor enemy in doomed)
            {
                deplete.Invoke(enemy, new object[] { 1e6f });
            }

            var settled = new List<EnemyActor>();
            var steps = new List<int>();
            System.Action<EnemyDeath> onDeath = death =>
            {
                // The corpse being settled is still registered while its death is announced.
                EnemyActor match = null;
                float best = 0.01f;
                foreach (EnemyActor enemy in doomed)
                {
                    if (enemy == null || settled.Contains(enemy))
                    {
                        continue;
                    }

                    Vector3 delta = enemy.Position - death.Position;
                    if (delta.sqrMagnitude < best)
                    {
                        best = delta.sqrMagnitude;
                        match = enemy;
                    }
                }

                settled.Add(match);
                steps.Add(_driver.Frame);
            };

            _driver.EnemyDied += onDeath;
            try
            {
                yield return Until(() => settled.Count >= doomed.Count, "the doomed wave never finished dying");
            }
            finally
            {
                _driver.EnemyDied -= onDeath;
            }

            Assert.That(steps, Is.All.EqualTo(steps[0]),
                "Corpses whose beats ended together were settled across several steps: the walk skipped " +
                "the enemy behind each despawned corpse.");
            Assert.That(settled, Is.EqualTo(doomed),
                "The corpses were not settled in registry order, so the loot stream drew out of order.");
        }

        [UnityTest]
        public IEnumerator A_left_stages_dummies_are_gone_from_the_step_it_is_left()
        {
            _runner.SpawnsEnabled = false;
            foreach (EnemyActor enemy in Object.FindObjectsByType<EnemyActor>(FindObjectsInactive.Include))
            {
                Object.Destroy(enemy.gameObject);
            }

            Scene left = default;
            bool handedOver = false;
            int dummiesLeft = 0;
            System.Action<int> onCompleted = stage =>
            {
                left = _runner.StageScene;
                handedOver = true;
                foreach (ISimTarget target in _driver.Targets.Ordered)
                {
                    if (target is TrainingDummy dummy && dummy.gameObject.scene == left)
                    {
                        dummiesLeft++;
                    }
                }
            };
            System.Action<int> onStep = frame =>
            {
                if (!handedOver)
                {
                    return;
                }

                foreach (ISimTarget target in _driver.Targets.Ordered)
                {
                    if (target is TrainingDummy dummy && dummy.gameObject.scene == left)
                    {
                        _violations.Add($"step {frame}: {dummy.name} from the stage left behind is still a target");
                    }
                }
            };

            _runner.StageCompleted += onCompleted;
            _driver.Stepped += onStep;
            try
            {
                yield return PushEveryoneRight(() => _runner.StageIndex == 1, "stage two, through the airlock");
                yield return Steps(10);
            }
            finally
            {
                _runner.StageCompleted -= onCompleted;
                _driver.Stepped -= onStep;
            }

            Assert.That(dummiesLeft, Is.GreaterThan(0), "The stage left behind had no dummy, so nothing was tested.");
            Assert.That(_violations, Is.Empty, string.Join("\n", _violations));
        }

        private List<EnemyActor> Enemies()
        {
            var enemies = new List<EnemyActor>();
            foreach (ISimTarget target in _driver.Targets.Ordered)
            {
                if (target is EnemyActor enemy && enemy.IsConfigured)
                {
                    enemies.Add(enemy);
                }
            }

            return enemies;
        }

        private List<EnemyActor> Living()
        {
            var living = new List<EnemyActor>();
            foreach (EnemyActor enemy in Enemies())
            {
                if (!enemy.IsDepleted)
                {
                    living.Add(enemy);
                }
            }

            return living;
        }

        /// <summary>Player one walks to the nearest living enemy and swings until something dies.</summary>
        private IEnumerator FightUntil(System.Func<bool> done)
        {
            ScriptedCommandSource input = _inputs[0];
            int deadline = _driver.Frame + PatienceSteps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline && !done(); guard++)
            {
                EnemyActor target = null;
                float best = float.MaxValue;
                foreach (EnemyActor enemy in Enemies())
                {
                    float d = (enemy.Position - _player.Position).sqrMagnitude;
                    if (!enemy.IsDepleted && d < best)
                    {
                        best = d;
                        target = enemy;
                    }
                }

                if (target == null)
                {
                    input.Release();
                }
                else
                {
                    Vector3 to = target.Position - _player.Position;
                    to.y = 0f;
                    bool swing = to.magnitude <= 1.2f && (_driver.Frame / 6) % 2 == 0;
                    Vector2 move = to.magnitude > 1.2f
                        ? new Vector2(Mathf.Clamp(to.x, -1f, 1f), Mathf.Clamp(to.z, -1f, 1f))
                        : new Vector2(Mathf.Sign(to.x) * 0.05f, 0f);
                    input.Set(move, swing ? CommandButtons.Light : CommandButtons.None);
                }

                yield return null;
            }

            input.Release();
        }

        private IEnumerator PushEveryoneRight(System.Func<bool> done, string what)
        {
            foreach (ScriptedCommandSource input in _inputs)
            {
                input.Set(Vector2.right, CommandButtons.None);
            }

            int deadline = _driver.Frame + PatienceSteps * 2;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline && !done(); guard++)
            {
                yield return null;
            }

            foreach (ScriptedCommandSource input in _inputs)
            {
                input.Release();
            }

            Assert.That(done(), Is.True, $"Pushing right never reached {what} (x={_player.Position.x:F2}).");
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
            for (int guard = 0; guard < 1500; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{failure} (waited 1500 frames).");
        }
    }
}
