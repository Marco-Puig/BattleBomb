using System.Collections.Generic;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Data;
using UnityEngine;

namespace BattleBomb.Gameplay.Combat
{
    /// <summary>
    /// Puts enemies into the world when the stage runner says a wave is due (D48) and tracks
    /// its brood so an attempt reset can clear it (task 35). It decides nothing: what spawns,
    /// how many, where, and how tough all arrive as arguments from <see cref="World.StageRunner"/>,
    /// which got them from <see cref="Core.Chapters.StageRun"/> and the tier.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : MonoBehaviour
    {
        [Tooltip("Prefab carrying an EnemyActor and its placeholder body.")]
        [SerializeField] private GameObject _enemyPrefab;

        [Tooltip("Driver the elite roll and the reset come from. Leave empty to find the one in the scene.")]
        [SerializeField] private Simulation.SimulationDriver _driver;

        private readonly List<GameObject> _brood = new List<GameObject>();
        private int _serial;

        internal void ResetBrood()
        {
            for (int i = 0; i < _brood.Count; i++)
            {
                if (_brood[i] != null)
                {
                    Destroy(_brood[i]);
                }
            }

            _brood.Clear();
        }

        /// <summary>
        /// Spawns one wave across the arena's points, cycling through them. Returns how many
        /// actually appeared, which is what the stage run counts as alive. Disabled (the smoke
        /// suites quiet the arena this way) it spawns nothing and says so.
        /// </summary>
        internal int SpawnWave(
            EnemyDefinition definition, int count, IReadOnlyList<Vector3> points,
            float healthMultiplier, float damageMultiplier)
        {
            if (!enabled || definition == null || count <= 0 || points == null || points.Count == 0)
            {
                return 0;
            }

            if (_enemyPrefab == null)
            {
                Debug.LogError($"{name}: no enemy prefab assigned — nothing can spawn.", this);
                return 0;
            }

            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 at = points[i % points.Count];
                at.z += ((i / points.Count) % 3 - 1) * 1.2f;
                _serial++;

                GameObject go = Instantiate(_enemyPrefab, at, Quaternion.identity, transform);
                go.name = $"{definition.name} {_serial:000}";
                EnemyActor actor = go.GetComponent<EnemyActor>();
                if (actor != null)
                {
                    // The elite draw happens here, before the body exists to be looked at (D22).
                    bool isElite = _driver.RollEliteSpawn(out Core.Items.ItemInstance carried);
                    actor.Configure(definition, _serial, isElite, carried, healthMultiplier, damageMultiplier);
                    if (isElite)
                    {
                        go.name += " (Elite)";
                    }
                }

                _brood.Add(go);
                spawned++;
            }

            return spawned;
        }

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<Simulation.SimulationDriver>();
            }

            if (_driver == null)
            {
                Debug.LogError($"{name}: no SimulationDriver in the scene — no wave can spawn.", this);
                return;
            }

            _driver.AttemptReset += ResetBrood;
        }

        private void OnDisable()
        {
            if (_driver != null)
            {
                _driver.AttemptReset -= ResetBrood;
            }
        }
    }
}
