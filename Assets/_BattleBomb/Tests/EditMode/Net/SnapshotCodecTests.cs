using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Spatial;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class SnapshotCodecTests
    {
        [Test]
        public void A_world_with_two_players_round_trips()
        {
            WorldSnapshot sent = World();
            var writer = new NetWriter();
            SnapshotCodec.Write(writer, sent);

            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.Snapshot));
            var received = new WorldSnapshot();
            SnapshotCodec.Read(reader, received);

            Assert.That(received.HostFrame, Is.EqualTo(sent.HostFrame));
            Assert.That(received.AckGuestFrame, Is.EqualTo(sent.AckGuestFrame));
            Assert.That(received.AttemptStepsAllDown, Is.EqualTo(sent.AttemptStepsAllDown));
            Assert.That(received.Bounds.MaxX, Is.EqualTo(sent.Bounds.MaxX));
            Assert.That(received.Players.Count, Is.EqualTo(2));
            for (int i = 0; i < 2; i++)
            {
                FieldCoverage.AssertSame(sent.Players[i], received.Players[i], $"Players[{i}]");
            }

            FieldCoverage.AssertSame(sent.Enemies[0], received.Enemies[0], "Enemies[0]");
            FieldCoverage.AssertSame(sent.Dummies[0], received.Dummies[0], "Dummies[0]");
            FieldCoverage.AssertSame(sent.Projectiles[1], received.Projectiles[1], "Projectiles[1]");
            Assert.That(received.DropIds, Is.EqualTo(sent.DropIds));
            Assert.That(reader.Remaining, Is.Zero);
        }

        [Test]
        public void Reading_reuses_the_snapshot_it_is_handed()
        {
            WorldSnapshot sent = World();
            var writer = new NetWriter();
            SnapshotCodec.Write(writer, sent);
            var received = World();
            received.Enemies.Add(received.Enemies[0]);

            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            SnapshotCodec.Read(reader, received);

            Assert.That(received.Enemies.Count, Is.EqualTo(sent.Enemies.Count), "Stale entries survived a read.");
        }

        /// <summary>Planning decision 2 for the entities themselves: every field of every player, enemy,
        /// dummy and bolt, filled by reflection, two of each — read twice into one snapshot, so a list the
        /// read forgets to clear shows up as well as a field the codec forgets.</summary>
        [Test]
        public void Every_entity_travels_whole_and_a_reused_snapshot_keeps_none_of_the_last()
        {
            for (int seed = 1; seed <= 3; seed++)
            {
                var sent = new WorldSnapshot { HostFrame = seed, AckGuestFrame = seed + 1, AttemptStepsAllDown = seed + 2 };
                for (int i = 0; i < 2; i++)
                {
                    int s = seed + 3 * i;
                    sent.Players.Add(FieldCoverage.Filled<PlayerSnapshot>(s));
                    sent.Enemies.Add(FieldCoverage.Filled<EnemySnapshot>(s));
                    sent.Dummies.Add(FieldCoverage.Filled<DummySnapshot>(s));
                    sent.Projectiles.Add(FieldCoverage.Filled<ProjectileState>(s));
                    sent.DropIds.Add(100 * seed + i);
                }

                var writer = new NetWriter();
                SnapshotCodec.Write(writer, sent);
                var received = new WorldSnapshot();
                for (int read = 0; read < 2; read++)
                {
                    var reader = new NetReader(writer.ToArray());
                    reader.ReadByte();
                    SnapshotCodec.Read(reader, received);
                    Assert.That(reader.Remaining, Is.Zero, "The snapshot read back fewer bytes than it wrote.");
                }

                Assert.That(received.HostFrame, Is.EqualTo(sent.HostFrame));
                Assert.That(received.AckGuestFrame, Is.EqualTo(sent.AckGuestFrame));
                Assert.That(received.AttemptStepsAllDown, Is.EqualTo(sent.AttemptStepsAllDown));
                Assert.That(received.DropIds, Is.EqualTo(sent.DropIds));
                Assert.That(received.Players.Count, Is.EqualTo(2), "Stale players survived a read.");
                Assert.That(received.Enemies.Count, Is.EqualTo(2), "Stale enemies survived a read.");
                Assert.That(received.Dummies.Count, Is.EqualTo(2), "Stale dummies survived a read.");
                Assert.That(received.Projectiles.Count, Is.EqualTo(2), "Stale bolts survived a read.");
                for (int i = 0; i < 2; i++)
                {
                    FieldCoverage.AssertSame(sent.Players[i], received.Players[i], $"Players[{i}]");
                    FieldCoverage.AssertSame(sent.Enemies[i], received.Enemies[i], $"Enemies[{i}]");
                    FieldCoverage.AssertSame(sent.Dummies[i], received.Dummies[i], $"Dummies[{i}]");
                    FieldCoverage.AssertSame(sent.Projectiles[i], received.Projectiles[i], $"Projectiles[{i}]");
                }
            }
        }

        [Test]
        public void Lookups_find_players_enemies_and_dummies()
        {
            WorldSnapshot world = World();
            Assert.That(world.TryGetPlayer(1, out PlayerSnapshot two), Is.True);
            Assert.That(two.PlayerId, Is.EqualTo(1));
            Assert.That(world.TryGetEnemy(12, out _), Is.True);
            Assert.That(world.TryGetEnemy(99, out _), Is.False);
            Assert.That(world.TryGetDummy(0, 3, out _), Is.True);
            Assert.That(world.TryGetDummy(1, 3, out _), Is.False, "A dummy of another stage answered.");
        }

        private static WorldSnapshot World()
        {
            var world = new WorldSnapshot
            {
                HostFrame = 1200,
                AckGuestFrame = 1187,
                Bounds = new ArenaBounds(-8f, 24f, 0f),
                AttemptStepsAllDown = 17,
            };

            for (int id = 0; id < 2; id++)
            {
                world.Players.Add(new PlayerSnapshot(
                    id,
                    FieldCoverage.Filled<MotorState>(10 + id),
                    FieldCoverage.Filled<CombatState>(20 + id),
                    FieldCoverage.Filled<PlayerCondition>(30 + id),
                    FieldCoverage.Filled<ReviveChannel>(40 + id),
                    FieldCoverage.Filled<ManaPool>(50 + id),
                    id == 0,
                    new[] { FieldCoverage.Filled<StatusInstance>(60 + id) },
                    id == 1 ? 0 : -1,
                    3 + id,
                    id * 45));
            }

            world.Enemies.Add(new EnemySnapshot(
                12, 0, 1, true, 5,
                FieldCoverage.Filled<MotorState>(70), FieldCoverage.Filled<EnemyState>(71),
                FieldCoverage.Filled<Health>(72), 1, 0, new[] { FieldCoverage.Filled<StatusInstance>(73) }));
            world.Enemies.Add(new EnemySnapshot(
                13, 0, 0, false, -1,
                FieldCoverage.Filled<MotorState>(74), FieldCoverage.Filled<EnemyState>(75),
                FieldCoverage.Filled<Health>(76), -1, 12, new StatusInstance[0]));
            world.Dummies.Add(new DummySnapshot(0, 3, FieldCoverage.Filled<MotorState>(80), FieldCoverage.Filled<Health>(81)));
            world.Projectiles.Add(FieldCoverage.Filled<ProjectileState>(90));
            world.Projectiles.Add(FieldCoverage.Filled<ProjectileState>(91));
            world.DropIds.Add(4);
            world.DropIds.Add(9);
            return world;
        }
    }
}
