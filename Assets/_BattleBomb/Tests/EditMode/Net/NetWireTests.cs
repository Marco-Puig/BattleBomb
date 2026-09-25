using BattleBomb.Core.Net;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class NetWireTests
    {
        [Test]
        public void Every_primitive_round_trips()
        {
            var writer = new NetWriter(4);
            writer.WriteByte(200);
            writer.WriteBool(true);
            writer.WriteBool(false);
            writer.WriteShort(-12345);
            writer.WriteUShort(54321);
            writer.WriteInt(-123456789);
            writer.WriteUInt(3000000000u);
            writer.WriteFloat(-1.5e-7f);
            writer.WriteVector2(new Vector2(0.25f, -8f));
            writer.WriteVector3(new Vector3(1f, 2.5f, -3.75f));
            writer.WriteString("Fire ↑ héros");

            var reader = new NetReader(writer.ToArray());
            Assert.That(reader.ReadByte(), Is.EqualTo(200));
            Assert.That(reader.ReadBool(), Is.True);
            Assert.That(reader.ReadBool(), Is.False);
            Assert.That(reader.ReadShort(), Is.EqualTo(-12345));
            Assert.That(reader.ReadUShort(), Is.EqualTo(54321));
            Assert.That(reader.ReadInt(), Is.EqualTo(-123456789));
            Assert.That(reader.ReadUInt(), Is.EqualTo(3000000000u));
            Assert.That(reader.ReadFloat(), Is.EqualTo(-1.5e-7f));
            Assert.That(reader.ReadVector2(), Is.EqualTo(new Vector2(0.25f, -8f)));
            Assert.That(reader.ReadVector3(), Is.EqualTo(new Vector3(1f, 2.5f, -3.75f)));
            Assert.That(reader.ReadString(), Is.EqualTo("Fire ↑ héros"));
            Assert.That(reader.Remaining, Is.Zero);
        }

        [Test]
        public void A_writer_grows_past_its_first_capacity()
        {
            var writer = new NetWriter(16);
            for (int i = 0; i < 1000; i++)
            {
                writer.WriteInt(i);
            }

            var reader = new NetReader(writer.Buffer, writer.Length);
            for (int i = 0; i < 1000; i++)
            {
                Assert.That(reader.ReadInt(), Is.EqualTo(i));
            }
        }

        [Test]
        public void Reading_past_the_end_is_a_format_error_not_a_crash()
        {
            var writer = new NetWriter();
            writer.WriteUShort(7);

            var reader = new NetReader(writer.ToArray());
            Assert.Throws<NetFormatException>(() => reader.ReadInt());
        }

        [Test]
        public void A_count_over_its_bound_is_refused_on_both_sides()
        {
            var writer = new NetWriter();
            Assert.Throws<NetFormatException>(() => writer.WriteCount(9, 8));

            writer.WriteUShort(9);
            var reader = new NetReader(writer.ToArray());
            Assert.Throws<NetFormatException>(() => reader.ReadCount(8));

            var atBound = new NetWriter();
            atBound.WriteCount(8, 8);
            Assert.That(new NetReader(atBound.ToArray()).ReadCount(8), Is.EqualTo(8),
                "A count exactly at its bound is allowed.");
        }

        [Test]
        public void A_string_longer_than_the_readers_limit_is_refused()
        {
            var writer = new NetWriter();
            writer.WriteString(new string('x', 300));

            var reader = new NetReader(writer.ToArray());
            Assert.Throws<NetFormatException>(() => reader.ReadString(256));
        }

        [Test]
        public void A_bool_byte_other_than_zero_or_one_is_malformed()
        {
            var reader = new NetReader(new byte[] { 2 });
            Assert.Throws<NetFormatException>(() => reader.ReadBool());
        }

        [Test]
        public void Reset_reuses_the_buffer_from_the_start()
        {
            var writer = new NetWriter();
            writer.WriteInt(1);
            byte[] before = writer.Buffer;
            writer.Reset();
            writer.WriteByte(9);

            Assert.That(writer.Buffer, Is.SameAs(before), "Reset allocated a new buffer.");
            Assert.That(writer.Length, Is.EqualTo(1));
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 9 }));
        }
    }
}
