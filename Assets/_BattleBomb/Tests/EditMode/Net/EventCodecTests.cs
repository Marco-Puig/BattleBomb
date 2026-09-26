using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Stats;
using BattleBomb.Platform.Net;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class EventCodecTests
    {
        [Test]
        public void Hits_and_drops_round_trip_with_their_steps()
        {
            var knife = new ItemInstance(
                new ItemIdentity(7, "Knife", ItemSlot.Weapon, default, default), QualityRank.Shiny,
                new GearContribution(weaponDamage: 9f), new AffixRoll[0], 2);
            var sent = new List<ReplicatedEvent>
            {
                ReplicatedEvent.OfHit(new HitRecord(
                    EntityRef.Player(0), EntityRef.Player(1), 0f, new Vector3(1f, 0f, 2f), true, false, false)).At(300),
                ReplicatedEvent.OfHit(new HitRecord(
                    EntityRef.None, EntityRef.Enemy(42), 3.5f, Vector3.one, false, false, true)).At(301),
                ReplicatedEvent.OfDrop(new DropRecord(5, new Vector3(4f, 0.35f, -1f), knife)).At(301),
            };

            var writer = new NetWriter();
            EventCodec.Write(writer, sent);
            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.Events));
            var received = new List<ReplicatedEvent>();
            EventCodec.Read(reader, null, received);

            Assert.That(received.Count, Is.EqualTo(3));
            Assert.That(received[0].HostFrame, Is.EqualTo(300));
            Assert.That(received[0].Hit.Attacker, Is.EqualTo(EntityRef.Player(0)));
            Assert.That(received[0].Hit.Target, Is.EqualTo(EntityRef.Player(1)));
            Assert.That(received[0].Hit.IsPartner, Is.True);
            Assert.That(received[1].Hit.Target, Is.EqualTo(EntityRef.Enemy(42)));
            Assert.That(received[1].Hit.Damage, Is.EqualTo(3.5f));
            Assert.That(received[1].Hit.IsDamageOverTime, Is.True);
            Assert.That(received[2].Kind, Is.EqualTo(ReplicatedEventKind.DropSpawned));
            Assert.That(received[2].Drop.NetId, Is.EqualTo(5));
            Assert.That(received[2].Drop.Item.DefinitionId, Is.EqualTo(7));
            Assert.That(received[2].Drop.Item.Quality, Is.EqualTo(QualityRank.Shiny));
        }

        [Test]
        public void An_event_kind_nothing_defines_is_malformed()
        {
            var writer = new NetWriter();
            writer.WriteUShort(1);
            writer.WriteByte(99);
            writer.WriteInt(0);
            Assert.Throws<NetFormatException>(() => EventCodec.Read(new NetReader(writer.ToArray()), null, new List<ReplicatedEvent>()));
        }

        [Test]
        public void A_burst_past_the_count_bound_arrives_whole_and_in_order()
        {
            var sent = new List<ReplicatedEvent>();
            for (int i = 0; i < NetProtocol.MaxEvents + 88; i++)
            {
                sent.Add(ReplicatedEvent.OfHit(new HitRecord(
                    EntityRef.Player(0), EntityRef.Enemy(i + 1), i, Vector3.zero, false, false, false)).At(1000 + i));
            }

            List<ReplicatedEvent> received = SendAcrossLoopback(sent, out int messages);

            Assert.That(messages, Is.EqualTo(2));
            Assert.That(received.Count, Is.EqualTo(sent.Count));
            for (int i = 0; i < sent.Count; i++)
            {
                Assert.That(received[i].HostFrame, Is.EqualTo(1000 + i));
                Assert.That(received[i].Hit.Target, Is.EqualTo(EntityRef.Enemy(i + 1)));
            }
        }

        [Test]
        public void Drops_past_the_frame_limit_arrive_whole_and_in_order()
        {
            string longName = new string('x', 6500);
            var sent = new List<ReplicatedEvent>();
            for (int i = 0; i < 40; i++)
            {
                var item = new ItemInstance(
                    new ItemIdentity(7, longName + i, ItemSlot.Weapon, default, default), QualityRank.Shiny,
                    new GearContribution(weaponDamage: 9f), new AffixRoll[0], 2);
                sent.Add(ReplicatedEvent.OfDrop(new DropRecord(i + 1, Vector3.zero, item)).At(500));
            }

            List<ReplicatedEvent> received = SendAcrossLoopback(sent, out int messages);

            Assert.That(messages, Is.GreaterThan(1), "Forty large drops fit one frame — the test no longer passes the limit.");
            Assert.That(received.Count, Is.EqualTo(40));
            for (int i = 0; i < 40; i++)
            {
                Assert.That(received[i].Drop.NetId, Is.EqualTo(i + 1));
                Assert.That(received[i].Drop.Item.DisplayName, Is.EqualTo(longName + i));
            }
        }

        [Test]
        public void A_dummy_hit_names_its_stage_as_well_as_its_prop()
        {
            var sent = new List<ReplicatedEvent>
            {
                ReplicatedEvent.OfHit(new HitRecord(
                    EntityRef.Player(0), EntityRef.Dummy(3, 1), 4f, Vector3.zero, false, false, false)).At(40),
            };

            var writer = new NetWriter();
            EventCodec.Write(writer, sent);
            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            var received = new List<ReplicatedEvent>();
            EventCodec.Read(reader, null, received);

            Assert.That(received[0].Hit.Target.DummyStage, Is.EqualTo(3));
            Assert.That(received[0].Hit.Target.DummyProp, Is.EqualTo(1));
            Assert.That(EntityRef.Dummy(0, 1), Is.Not.EqualTo(EntityRef.Dummy(1, 1)),
                "The first dummy of two stages is one ref: a hit could land on the wrong one.");
            Assert.That(EntityRef.Dummy(-1, 2).DummyStage, Is.EqualTo(-1));
            Assert.That(EntityRef.Dummy(-1, 2).DummyProp, Is.EqualTo(2));
        }

        [Test]
        public void A_removed_drop_round_trips_with_its_step()
        {
            var sent = new List<ReplicatedEvent> { ReplicatedEvent.OfDropRemoved(9).At(77) };
            var writer = new NetWriter();
            EventCodec.Write(writer, sent);
            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            var received = new List<ReplicatedEvent>();
            EventCodec.Read(reader, null, received);

            Assert.That(received.Count, Is.EqualTo(1));
            Assert.That(received[0].Kind, Is.EqualTo(ReplicatedEventKind.DropRemoved));
            Assert.That(received[0].Drop.NetId, Is.EqualTo(9));
            Assert.That(received[0].HostFrame, Is.EqualTo(77));
        }

        /// <summary>Through a transport that refuses a frame past the limit the way the socket does, so a
        /// batch that is too big shows up as a dropped connection rather than passing.</summary>
        private static List<ReplicatedEvent> SendAcrossLoopback(List<ReplicatedEvent> sent, out int messages)
        {
            LoopbackTransportTests.Connected(out LoopbackTransport host, out LoopbackTransport guest);
            EventCodec.WriteBatches(sent, new NetWriter(), new NetWriter(),
                w => host.Send(LoopbackTransport.GuestPeer, NetChannel.Reliable, w.Buffer, w.Length));

            var received = new List<ReplicatedEvent>();
            var batch = new List<ReplicatedEvent>();
            messages = 0;
            foreach (NetEvent e in LoopbackTransportTests.Drain(guest))
            {
                Assert.That(e.Kind, Is.EqualTo(NetEventKind.Data), "The connection dropped: a batch was past the frame limit.");
                var reader = new NetReader(e.Payload);
                Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.Events));
                EventCodec.Read(reader, null, batch);
                received.AddRange(batch);
                messages++;
            }

            Assert.That(host.IsConnected, Is.True);
            return received;
        }

        [Test]
        public void Screens_and_racks_round_trip_with_their_steps()
        {
            var knife = new ItemInstance(
                new ItemIdentity(7, "Knife", ItemSlot.Weapon, default, default), QualityRank.Shiny,
                new GearContribution(weaponDamage: 9f), new AffixRoll[0], 2);
            var sent = new List<ReplicatedEvent>
            {
                ReplicatedEvent.OfRack(1, new[] { knife, knife }).At(40),
                ReplicatedEvent.OfScreen(1, 1, true).At(40),
                ReplicatedEvent.OfScreen(0, 0, false).At(41),
                ReplicatedEvent.OfRack(1, new ItemInstance[0]).At(42),
            };

            var writer = new NetWriter();
            EventCodec.Write(writer, sent);
            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            var received = new List<ReplicatedEvent>();
            EventCodec.Read(reader, null, received);

            Assert.That(received.Count, Is.EqualTo(4));
            Assert.That(received[0].Kind, Is.EqualTo(ReplicatedEventKind.RackChanged));
            Assert.That(received[0].Rack.PlayerId, Is.EqualTo(1));
            Assert.That(received[0].Rack.Pieces.Length, Is.EqualTo(2));
            Assert.That(received[0].Rack.Pieces[1].DefinitionId, Is.EqualTo(7));
            Assert.That(received[1].Kind, Is.EqualTo(ReplicatedEventKind.ScreenOpened));
            Assert.That(received[1].Screen.PlayerId, Is.EqualTo(1));
            Assert.That(received[1].Screen.Kind, Is.EqualTo(1));
            Assert.That(received[2].Kind, Is.EqualTo(ReplicatedEventKind.ScreenClosed));
            Assert.That(received[2].HostFrame, Is.EqualTo(41));
            Assert.That(received[3].Rack.Pieces, Is.Empty);
        }
    }
}
