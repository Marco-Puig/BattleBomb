using System.Collections.Generic;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Combat;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// Sets the guest's world to the host's, one local step at a time: positions interpolated between
    /// the two snapshots around the render frame, everything discrete from the nearer of them. Enemies
    /// and drops appear and vanish as the host's do. A teleport — a respawn, an airlock — is shown as
    /// a teleport, never as a slide across the arena.
    /// </summary>
    internal sealed class ReplicaWorld
    {
        private const float TeleportDistanceSq = NetProtocol.ReplicaTeleportDistance * NetProtocol.ReplicaTeleportDistance;

        private readonly SimulationDriver _driver;
        private readonly StageRunner _runner;
        private readonly EnemySpawner _spawner;
        private readonly GameSession _session;
        private readonly Dictionary<int, EnemyActor> _enemies = new Dictionary<int, EnemyActor>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _gone = new List<int>();
        private readonly HashSet<int> _unspawnable = new HashSet<int>();

        internal ReplicaWorld(SimulationDriver driver, StageRunner runner, EnemySpawner spawner, GameSession session)
        {
            _driver = driver;
            _runner = runner;
            _spawner = spawner;
            _session = session;
        }

        internal void Apply(WorldSnapshot from, WorldSnapshot to, float t, float renderFrame)
        {
            WorldSnapshot near = t < 0.5f ? from : to;
            ApplyPlayers(from, to, t, near);
            ApplyEnemies(from, to, t, near);
            ApplyDummies(from, to, t, near);
            _driver.ApplyReplicaProjectiles(near.Projectiles, (renderFrame - near.HostFrame) * _driver.StepDuration);
            _driver.ApplyReplicaWorld(near.Bounds, near.AttemptStepsAllDown);
        }

        /// <summary>A drop the host announced, placed when the picture reaches the step it appeared in.</summary>
        internal void SpawnDrop(in DropRecord drop) => _driver.ApplyReplicaPickup(drop.NetId, drop.Position, drop.Item);

        /// <summary>A drop the host says has gone. Removal is only ever told: snapshots carry no drops.</summary>
        internal void RemoveDrop(int netId) => _driver.RemoveReplicaPickup(netId);

        internal EnemyActor EnemyByNetId(int netId) =>
            _enemies.TryGetValue(netId, out EnemyActor enemy) && enemy != null ? enemy : null;

        internal CharacterActor PlayerById(int playerId)
        {
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i].PlayerId.Value == playerId)
                {
                    return actors[i];
                }
            }

            return null;
        }

        private void ApplyPlayers(WorldSnapshot from, WorldSnapshot to, float t, WorldSnapshot near)
        {
            for (int i = 0; i < near.Players.Count; i++)
            {
                PlayerSnapshot player = near.Players[i];
                CharacterActor actor = PlayerById(player.PlayerId);
                if (actor == null)
                {
                    continue;
                }

                MotorState motor = player.Motor;
                if (from.TryGetPlayer(player.PlayerId, out PlayerSnapshot a) && to.TryGetPlayer(player.PlayerId, out PlayerSnapshot b))
                {
                    motor = Blend(a.Motor, b.Motor, t);
                }

                actor.ApplyReplica(player.WithMotor(motor));
                // The snapshot still carries each player's open screen; the guest follows the host's screen events.
                _driver.ApplyReplicaPlayerSide(player.PlayerId, player.GrabCount, player.RefusedSteps);
            }
        }

        private void ApplyEnemies(WorldSnapshot from, WorldSnapshot to, float t, WorldSnapshot near)
        {
            _seen.Clear();
            for (int i = 0; i < near.Enemies.Count; i++)
            {
                EnemySnapshot enemy = near.Enemies[i];
                _seen.Add(enemy.NetId);

                EnemyActor actor = EnemyByNetId(enemy.NetId);
                if (actor == null)
                {
                    actor = _spawner != null ? _spawner.ReplicaSpawn(DefinitionOf(enemy), enemy) : null;
                    if (actor == null)
                    {
                        // Said once, not every step: an enemy the guest cannot build is simply not drawn,
                        // and without this the only sign would be empty space where the host has one.
                        if (_unspawnable.Add(enemy.NetId))
                        {
                            Debug.LogWarning(
                                $"Replica: enemy {enemy.NetId} (stage {enemy.StageIndex}, roster {enemy.RosterIndex}) " +
                                "could not be built on the guest, so it is not drawn.");
                        }

                        continue;
                    }

                    _enemies[enemy.NetId] = actor;
                }

                MotorState motor = enemy.Motor;
                if (from.TryGetEnemy(enemy.NetId, out EnemySnapshot a) && to.TryGetEnemy(enemy.NetId, out EnemySnapshot b))
                {
                    motor = Blend(a.Motor, b.Motor, t);
                }

                actor.ApplyReplica(enemy.WithMotor(motor));
            }

            _gone.Clear();
            foreach (KeyValuePair<int, EnemyActor> entry in _enemies)
            {
                if (!_seen.Contains(entry.Key))
                {
                    _gone.Add(entry.Key);
                }
            }

            for (int i = 0; i < _gone.Count; i++)
            {
                if (_spawner != null)
                {
                    _spawner.ReplicaDespawn(_enemies[_gone[i]]);
                }

                _enemies.Remove(_gone[i]);
            }
        }

        private void ApplyDummies(WorldSnapshot from, WorldSnapshot to, float t, WorldSnapshot near)
        {
            if (_runner == null)
            {
                return;
            }

            for (int i = 0; i < near.Dummies.Count; i++)
            {
                DummySnapshot dummy = near.Dummies[i];
                TrainingDummy target = _runner.ReplicaDummy(dummy.StageIndex, dummy.PropIndex);
                if (target == null)
                {
                    continue;
                }

                MotorState motor = dummy.Motor;
                if (from.TryGetDummy(dummy.StageIndex, dummy.PropIndex, out DummySnapshot a)
                    && to.TryGetDummy(dummy.StageIndex, dummy.PropIndex, out DummySnapshot b))
                {
                    motor = Blend(a.Motor, b.Motor, t);
                }

                target.ApplyReplica(new DummySnapshot(dummy.StageIndex, dummy.PropIndex, motor, dummy.Health));
            }
        }

        private EnemyDefinition DefinitionOf(in EnemySnapshot enemy)
        {
            ChapterDefinition chapter = _session != null ? _session.Chapter : null;
            StageDefinition stage = chapter != null ? chapter.StageAt(enemy.StageIndex) : null;
            return stage != null ? stage.EnemyAt(enemy.RosterIndex) : null;
        }

        private static MotorState Blend(in MotorState a, in MotorState b, float t)
        {
            MotorState near = t < 0.5f ? a : b;
            if ((b.Position - a.Position).sqrMagnitude > TeleportDistanceSq)
            {
                return near;
            }

            return new MotorState(
                Vector3.Lerp(a.Position, b.Position, t), Vector3.Lerp(a.Velocity, b.Velocity, t),
                near.Facing, near.IsGrounded, near.StepsSinceGrounded, near.JumpBufferedFor);
        }
    }
}
