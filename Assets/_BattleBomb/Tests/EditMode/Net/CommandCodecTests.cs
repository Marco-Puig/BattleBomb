using System;
using System.Collections.Generic;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class CommandCodecTests
    {
        [Test]
        public void Every_command_button_fits_the_sixteen_bits_the_wire_gives_it()
        {
            foreach (CommandButtons button in Enum.GetValues(typeof(CommandButtons)))
            {
                Assert.That((uint)button, Is.LessThan(1u << 16),
                    $"{button} does not fit a ushort. Widen CommandCodec's held field before adding it.");
            }
        }

        [Test]
        public void Remote_players_bring_only_the_fights_verbs()
        {
            const CommandButtons verbs = CommandButtons.Light | CommandButtons.Heavy | CommandButtons.Magic
                | CommandButtons.Equipment | CommandButtons.Jump;
            Assert.That(NetProtocol.RemoteVerbs, Is.EqualTo(verbs));

            // Groundwork's menu layer (D57): none of it may reach the host from a remote player.
            const CommandButtons menus = CommandButtons.Pause | CommandButtons.Confirm | CommandButtons.Back
                | CommandButtons.Option | CommandButtons.Lock | CommandButtons.TabPrevious | CommandButtons.TabNext;
            Assert.That(NetProtocol.RemoteVerbs & menus, Is.EqualTo(CommandButtons.None));
        }

        [Test]
        public void Quantising_the_stick_is_stable()
        {
            var stick = new Vector2(0.123456f, -0.987654f);
            Vector2 once = NetQuantize.Move(stick);
            Vector2 twice = NetQuantize.Move(once);

            Assert.That(twice, Is.EqualTo(once),
                "The guest predicts with the quantised stick and the host simulates with the decoded one; they must be the same number.");
            Assert.That(Mathf.Abs(once.x - stick.x), Is.LessThan(1e-4f));
            Assert.That(NetQuantize.Move(new Vector2(2f, -3f)), Is.EqualTo(new Vector2(1f, -1f)));
        }

        [Test]
        public void Two_players_commands_round_trip_with_the_quantised_stick()
        {
            var one = new List<WireCommand>
            {
                WireCommand.From(PlayerCommand.FromState(10, new Vector2(0.5f, 0f), CommandButtons.Light, CommandButtons.None)),
                WireCommand.From(PlayerCommand.FromState(11, new Vector2(1f, 0f), CommandButtons.None, CommandButtons.Light)),
            };
            var two = new List<WireCommand>
            {
                WireCommand.From(PlayerCommand.FromState(40, new Vector2(-0.3f, 0.7f), CommandButtons.Jump | CommandButtons.Heavy, CommandButtons.None)),
            };

            var decodedOne = RoundTrip(99, one, out int ackOne);
            var decodedTwo = RoundTrip(7, two, out int ackTwo);

            Assert.That(ackOne, Is.EqualTo(99));
            Assert.That(ackTwo, Is.EqualTo(7));
            Assert.That(decodedOne, Is.EqualTo(one));
            Assert.That(decodedTwo, Is.EqualTo(two));
        }

        [Test]
        public void Only_the_newest_four_commands_travel()
        {
            var sent = new List<WireCommand>();
            for (int frame = 1; frame <= 9; frame++)
            {
                sent.Add(new WireCommand(frame, Vector2.zero, CommandButtons.None));
            }

            var decoded = RoundTrip(0, sent, out _);

            Assert.That(decoded.Count, Is.EqualTo(NetProtocol.CommandRedundancy));
            Assert.That(decoded[0].Frame, Is.EqualTo(6));
            Assert.That(decoded[3].Frame, Is.EqualTo(9));
        }

        [Test]
        public void A_packet_claiming_more_than_four_commands_is_malformed()
        {
            var writer = new NetWriter();
            writer.WriteInt(0);
            writer.WriteByte(5);

            // Five whole commands follow, so only the count itself can be what is refused.
            for (int i = 0; i < 5; i++)
            {
                writer.WriteInt(i);
                writer.WriteShort(0);
                writer.WriteShort(0);
                writer.WriteUShort(0);
            }

            Assert.Throws<NetFormatException>(() => CommandCodec.Read(new NetReader(writer.ToArray()), new List<WireCommand>()));
        }

        [Test]
        public void A_button_bit_nothing_defines_is_malformed()
        {
            var writer = new NetWriter();
            writer.WriteInt(0);
            writer.WriteByte(1);
            writer.WriteInt(1);
            writer.WriteShort(0);
            writer.WriteShort(0);
            writer.WriteUShort(1 << 15);
            Assert.Throws<NetFormatException>(() => CommandCodec.Read(new NetReader(writer.ToArray()), new List<WireCommand>()));
        }

        private static List<WireCommand> RoundTrip(int ack, List<WireCommand> commands, out int readAck)
        {
            var writer = new NetWriter();
            CommandCodec.Write(writer, ack, commands);
            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.Commands));
            var decoded = new List<WireCommand>();
            readAck = CommandCodec.Read(reader, decoded);
            Assert.That(reader.Remaining, Is.Zero);
            return decoded;
        }
    }
}
