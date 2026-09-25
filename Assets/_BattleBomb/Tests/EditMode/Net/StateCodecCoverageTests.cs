using System;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Spatial;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>Every Core struct that crosses the wire comes back whole — every field, private ones too.</summary>
    public sealed class StateCodecCoverageTests
    {
        [Test]
        public void MotorState_travels_whole() =>
            RoundTrip<MotorState>((w, v) => StateCodec.Write(w, v), StateCodec.ReadMotor);

        [Test]
        public void AttackTuning_travels_whole_including_its_private_radial_flag() =>
            RoundTrip<AttackTuning>((w, v) => StateCodec.Write(w, v), StateCodec.ReadAttack);

        [Test]
        public void CombatState_travels_whole() =>
            RoundTrip<CombatState>((w, v) => StateCodec.Write(w, v), StateCodec.ReadCombat);

        [Test]
        public void Health_travels_whole() =>
            RoundTrip<Health>((w, v) => StateCodec.Write(w, v), StateCodec.ReadHealth);

        [Test]
        public void PlayerCondition_travels_whole() =>
            RoundTrip<PlayerCondition>((w, v) => StateCodec.Write(w, v), StateCodec.ReadCondition);

        [Test]
        public void ReviveChannel_travels_whole() =>
            RoundTrip<ReviveChannel>((w, v) => StateCodec.Write(w, v), StateCodec.ReadRevive);

        [Test]
        public void ManaPool_travels_whole() =>
            RoundTrip<ManaPool>((w, v) => StateCodec.Write(w, v), StateCodec.ReadMana);

        [Test]
        public void EnemyState_travels_whole() =>
            RoundTrip<EnemyState>((w, v) => StateCodec.Write(w, v), StateCodec.ReadBrain);

        [Test]
        public void StatusInstance_travels_whole() =>
            RoundTrip<StatusInstance>((w, v) => StateCodec.Write(w, v), StateCodec.ReadStatus);

        [Test]
        public void ProjectileState_travels_whole() =>
            RoundTrip<ProjectileState>((w, v) => StateCodec.Write(w, v), StateCodec.ReadProjectile);

        [Test]
        public void ArenaBounds_travels_whole()
        {
            var bounds = new ArenaBounds(-3.25f, 41.5f, 0.125f);
            var writer = new NetWriter();
            StateCodec.Write(writer, bounds);
            ArenaBounds back = StateCodec.ReadBounds(new NetReader(writer.ToArray()));

            Assert.That(back.MinX, Is.EqualTo(bounds.MinX));
            Assert.That(back.MaxX, Is.EqualTo(bounds.MaxX));
            Assert.That(back.GroundY, Is.EqualTo(bounds.GroundY));
        }

        [Test]
        public void Inverted_bounds_on_the_wire_are_malformed()
        {
            var writer = new NetWriter();
            writer.WriteFloat(5f);
            writer.WriteFloat(1f);
            writer.WriteFloat(0f);
            Assert.Throws<NetFormatException>(() => StateCodec.ReadBounds(new NetReader(writer.ToArray())));
        }

        [Test]
        public void The_coverage_check_itself_catches_a_forgotten_field()
        {
            MotorState full = FieldCoverage.Filled<MotorState>(7);
            var forgetful = new MotorState(full.Position, full.Velocity, full.Facing, full.IsGrounded, full.StepsSinceGrounded, 0);

            Assert.That(FieldCoverage.FirstDifference(full, forgetful, "MotorState"), Is.EqualTo("MotorState.JumpBufferedFor"),
                "The helper passed a value with a field missing — it would pass a broken codec too.");
        }

        private static void RoundTrip<T>(Action<NetWriter, T> write, Func<NetReader, T> read) where T : struct
        {
            for (int seed = 1; seed <= 3; seed++)
            {
                T value = FieldCoverage.Filled<T>(seed);
                var writer = new NetWriter();
                write(writer, value);
                var reader = new NetReader(writer.ToArray());
                T back = read(reader);

                FieldCoverage.AssertSame(value, back, typeof(T).Name);
                Assert.That(reader.Remaining, Is.Zero, $"{typeof(T).Name} read back fewer bytes than it wrote.");
            }
        }
    }
}
