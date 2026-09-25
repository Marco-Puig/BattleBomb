using System;
using System.Collections.Generic;
using BattleBomb.Core.Players;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// The guest's command packet: the newest host frame it has seen (for Plan 3's prediction) and
    /// its newest <see cref="NetProtocol.CommandRedundancy"/> commands, oldest first.
    /// </summary>
    public static class CommandCodec
    {
        private static readonly uint KnownButtons = AllButtons();

        /// <summary>The command with its stick quantised and its edges kept — what the guest both
        /// sends and (Plan 3) predicts with.</summary>
        public static PlayerCommand Quantized(in PlayerCommand command) => new PlayerCommand(
            command.Frame, NetQuantize.Move(command.Move), command.Held, command.Pressed, command.Released);

        public static void Write(NetWriter writer, int ackHostFrame, IReadOnlyList<WireCommand> newestLast)
        {
            writer.WriteByte((byte)NetMessageKind.Commands);
            writer.WriteInt(ackHostFrame);

            int count = Math.Min(newestLast.Count, NetProtocol.CommandRedundancy);
            writer.WriteByte((byte)count);
            for (int i = newestLast.Count - count; i < newestLast.Count; i++)
            {
                WireCommand command = newestLast[i];
                writer.WriteInt(command.Frame);
                writer.WriteShort(NetQuantize.Axis(command.Move.x));
                writer.WriteShort(NetQuantize.Axis(command.Move.y));
                writer.WriteUShort((ushort)(uint)command.Held);
            }
        }

        /// <summary>Reads a packet positioned after its kind byte. Returns the acknowledged host frame.</summary>
        public static int Read(NetReader reader, List<WireCommand> into)
        {
            into.Clear();
            int ack = reader.ReadInt();
            int count = reader.ReadByte();
            if (count > NetProtocol.CommandRedundancy)
            {
                throw new NetFormatException($"A command packet claiming {count} commands.");
            }

            for (int i = 0; i < count; i++)
            {
                int frame = reader.ReadInt();
                float x = NetQuantize.FromAxis(reader.ReadShort());
                float y = NetQuantize.FromAxis(reader.ReadShort());
                uint held = reader.ReadUShort();
                if ((held & ~KnownButtons) != 0)
                {
                    throw new NetFormatException($"Button bits 0x{held:X4} include ones nothing defines.");
                }

                into.Add(new WireCommand(frame, new Vector2(x, y), (CommandButtons)held));
            }

            return ack;
        }

        private static uint AllButtons()
        {
            uint all = 0;
            foreach (CommandButtons button in Enum.GetValues(typeof(CommandButtons)))
            {
                all |= (uint)button;
            }

            return all;
        }
    }
}
