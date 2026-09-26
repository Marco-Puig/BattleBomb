using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>A menu action and its answer cross the wire whole (HANDOFF-M8 planning decision 11).</summary>
    public sealed class RequestCodecTests
    {
        [Test]
        public void A_request_travels_whole()
        {
            for (int seed = 1; seed <= 3; seed++)
            {
                PlayerRequest request = FieldCoverage.Filled<PlayerRequest>(seed);
                var writer = new NetWriter();
                RequestCodec.WriteRequest(writer, request);

                var reader = new NetReader(writer.ToArray());
                Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.Request));
                PlayerRequest back = RequestCodec.ReadRequest(reader);

                FieldCoverage.AssertSame(request, back, nameof(PlayerRequest));
                Assert.That(reader.Remaining, Is.Zero, "The request read back fewer bytes than it wrote.");
            }
        }

        [Test]
        public void An_answer_travels_whole_with_its_sequence()
        {
            for (int seed = 1; seed <= 3; seed++)
            {
                RequestOutcome outcome = FieldCoverage.Filled<RequestOutcome>(seed);
                var writer = new NetWriter();
                RequestCodec.WriteResult(writer, 4000 + seed, outcome);

                var reader = new NetReader(writer.ToArray());
                Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.RequestResult));
                RequestOutcome back = RequestCodec.ReadResult(reader, out int sequence);

                Assert.That(sequence, Is.EqualTo(4000 + seed));
                FieldCoverage.AssertSame(outcome, back, nameof(RequestOutcome));
                Assert.That(reader.Remaining, Is.Zero, "The answer read back fewer bytes than it wrote.");
            }
        }

        [Test]
        public void An_unknown_verb_is_refused_at_the_door()
        {
            var writer = new NetWriter();
            writer.WriteByte((byte)NetMessageKind.Request);
            writer.WriteByte(200);
            for (int i = 0; i < 7; i++)
            {
                writer.WriteInt(0);
            }

            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            Assert.Throws<NetFormatException>(() => RequestCodec.ReadRequest(reader));
        }

        [Test]
        public void An_upgrade_names_its_target_either_way()
        {
            UpgradeTarget affix = PlayerRequest.Upgrade(3, UpgradeTarget.Affix(2)).Target;
            Assert.That(affix.IsAffix, Is.True);
            Assert.That(affix.AffixIndex, Is.EqualTo(2));

            UpgradeTarget core = PlayerRequest.UpgradeWorn(ItemSlot.Weapon, 0, UpgradeTarget.Core(CoreStatId.WeaponDamage)).Target;
            Assert.That(core.IsAffix, Is.False);
            Assert.That(core.CoreStat, Is.EqualTo(CoreStatId.WeaponDamage));
        }

        [Test]
        public void Only_the_verbs_that_name_a_place_in_the_sack_carry_a_revision_check()
        {
            Assert.That(PlayerRequest.Sell(0).NamesASackPlace, Is.True);
            Assert.That(PlayerRequest.Equip(0).NamesASackPlace, Is.True);
            Assert.That(PlayerRequest.Combine(0, 1).NamesASackPlace, Is.True);
            Assert.That(PlayerRequest.CloseScreen().NamesASackPlace, Is.False, "Closing must always work.");
            Assert.That(PlayerRequest.Unequip(ItemSlot.Helmet, 0).NamesASackPlace, Is.False, "A worn slot never moves.");
            Assert.That(PlayerRequest.SetAutoSell(true).NamesASackPlace, Is.False);
        }

        [Test]
        public void Every_verb_says_whether_it_names_a_place_in_the_sack()
        {
            PlayerRequestKind[] named =
            {
                PlayerRequestKind.Equip, PlayerRequestKind.Sell, PlayerRequestKind.Lock,
                PlayerRequestKind.Upgrade, PlayerRequestKind.Combine, PlayerRequestKind.CombineAll,
            };

            foreach (PlayerRequestKind kind in System.Enum.GetValues(typeof(PlayerRequestKind)))
            {
                bool expected = System.Array.IndexOf(named, kind) >= 0;
                Assert.That(new PlayerRequest(kind, 0, 0, 0, 0, 0, 0, 0).NamesASackPlace, Is.EqualTo(expected),
                    expected
                        ? $"{kind} names a place in the sack and goes unguarded."
                        : $"{kind} names no place in the sack and is held to a revision.");
            }
        }
    }
}
