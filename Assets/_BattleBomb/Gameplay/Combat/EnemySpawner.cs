using System;
using System.Collections.Generic;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Data;
using UnityEngine;

namespace BattleBomb.Gameplay.Combat
{
    /// <summary>
    /// Spawns an authored list of enemies on a simulation-step schedule and tracks its brood.
    /// Nothing procedural — endless mode's generator is a different milestone (D12). Counts steps
    /// through the driver's <c>Stepped</c> event so spawning is part of the simulation, and
    /// <see cref="ResetBrood"/> restarts the encounter for task 35's attempt reset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : MonoBehaviour
    {
        [Serializable]
        private struct SpawnEntry
        {
            public EnemyDefinition Definition;
            public Vector3 Position;

            [Tooltip("Simulation steps after the encounter starts (60 = one second).")]
            public int DelaySteps;
        }

        [Tooltip("Prefab carrying an EnemyActor and its placeholder body.")]
        [SerializeField] private GameObject _enemyPrefab;

        [Tooltip("Driver whose steps schedule the spawns. Leave empty to find the one in the scene.")]
        [SerializeField] private Simulation.SimulationDriver _driver;

        [SerializeField] private List<SpawnEntry> _entries = new List<SpawnEntry>();

        private readonly List<GameObject> _brood = new List<GameObject>();
        private bool[] _spawned;
        private int _elapsedSteps;

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
            _spawned = new bool[_entries.Count];
            _elapsedSteps = 0;
        }

        private void OnStepped(int frame)
        {
            _elapsedSteps += 1;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_spawned[i] || _entries[i].Definition == null || _entries[i].DelaySteps > _elapsedSteps)
                {
                    continue;
                }

                Spawn(i);
            }
        }

        private void Spawn(int index)
        {
            _spawned[index] = true;
            if (_enemyPrefab == null)
            {
                Debug.LogError($"{name}: no enemy prefab assigned — nothing can spawn.", this);
                return;
            }

            SpawnEntry entry = _entries[index];
            GameObject spawned = Instantiate(_enemyPrefab, entry.Position, Quaternion.identity, transform);
            spawned.name = $"{entry.Definition.name} {index + 1:00}";
            EnemyActor actor = spawned.GetComponent<EnemyActor>();
            if (actor != null)
            {
                // The entry index is the D28 variation seed: deterministic, unique per spawn.
                actor.Configure(entry.Definition, index);
            }

            _brood.Add(spawned);
        }

        private void OnEnable()
        {
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<Simulation.SimulationDriver>();
            }

            if (_driver == null)
            {
                Debug.LogError($"{name}: no SimulationDriver in the scene — no encounter will start.", this);
                return;
            }

            _spawned = new bool[_entries.Count];
            _elapsedSteps = 0;
            _driver.Stepped += OnStepped;
            _driver.AttemptReset += ResetBrood;
        }

        private void OnDisable()
        {
            if (_driver != null)
            {
                _driver.Stepped -= OnStepped;
                _driver.AttemptReset -= ResetBrood;
            }
        }
    }
}
