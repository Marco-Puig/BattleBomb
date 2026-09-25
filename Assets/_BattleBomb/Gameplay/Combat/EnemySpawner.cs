using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
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

        /// <summary>Network ids. Unlike <see cref="_serial"/> this never resets: a wipe or an airlock
        /// must not hand a new enemy an id an old one still holds on the guest's screen.</summary>
        private int _nextNetId;

        /// <summary>
        /// Clears the arena: every enemy still standing goes, and the variation seed starts over.
        /// Called by the attempt reset (task 35) and by the stage runner at a launch and at every
        /// airlock hand-over, so the D28 seed is an offset into one stage rather than a counter
        /// that climbs for as long as the machine has been running.
        /// </summary>
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
            _serial = 0;
        }

        /// <summary>
        /// Spawns one wave across the arena's points, cycling through them. Returns how many
        /// actually appeared, which is what the stage run counts as alive. Disabled (the smoke
        /// suites quiet the arena this way) it spawns nothing and says so.
        /// </summary>
        internal int SpawnWave(
            EnemyDefinition definition, int count, IReadOnlyList<Vector3> points,
            float healthMultiplier, float damageMultiplier, int stageIndex = -1, int rosterIndex = -1)
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

            // The dead are already gone from the world; drop them from the list too, or a
            // chapter's worth of waves leaves it holding hundreds of nulls to walk on a reset.
            for (int i = _brood.Count - 1; i >= 0; i--)
            {
                if (_brood[i] == null)
                {
                    _brood.RemoveAt(i);
                }
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
                    actor.SetOrigin(++_nextNetId, stageIndex, rosterIndex);
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

        /// <summary>
        /// The guest's copy of an enemy the host spawned: the same prefab and archetype, configured at
        /// face value — health, tier and elite toughness all arrive in every snapshot, so none of them
        /// is recomputed here. An elite wears a stand-in carrying only its piece's quality, which is
        /// all its tint reads; the real piece drops from the host.
        /// </summary>
        internal EnemyActor ReplicaSpawn(EnemyDefinition definition, in EnemySnapshot snapshot)
        {
            if (_enemyPrefab == null || definition == null)
            {
                return null;
            }

            GameObject go = Instantiate(_enemyPrefab, snapshot.Motor.Position, Quaternion.identity, transform);
            go.name = $"{definition.name} {snapshot.NetId:000}";
            EnemyActor actor = go.GetComponent<EnemyActor>();
            if (actor == null)
            {
                Destroy(go);
                return null;
            }

            ItemInstance carried = snapshot.CarriedQuality >= 0
                ? ItemWire.StandIn((QualityRank)snapshot.CarriedQuality)
                : default;
            actor.Configure(definition, 0, snapshot.IsElite, carried, 1f, 1f);
            actor.SetOrigin(snapshot.NetId, snapshot.StageIndex, snapshot.RosterIndex);
            _brood.Add(go);
            return actor;
        }

        /// <summary>Gone from the host's world: gone from the guest's, at once — deactivated first so it
        /// leaves every registry now, not at the end of the frame.</summary>
        internal void ReplicaDespawn(EnemyActor actor)
        {
            if (actor == null)
            {
                return;
            }

            _brood.Remove(actor.gameObject);
            actor.gameObject.SetActive(false);
            Destroy(actor.gameObject);
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
