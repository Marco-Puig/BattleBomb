using System.Text;
using BattleBomb.Core.Saves;

namespace BattleBomb.Core.Net
{
    /// <summary>One player's inventory as the host holds it, at a sack revision.</summary>
    public readonly struct ParticipantMessage
    {
        public readonly int PlayerId;
        public readonly int Revision;

        /// <summary>The sack, wallet and auto flags came too — the guest's own player. Otherwise only what the
        /// player wears, their quick slot and their ledger — the partner, for the guest's copy of them.</summary>
        public readonly bool Full;

        public readonly SaveGame State;

        public ParticipantMessage(int playerId, int revision, bool full, SaveGame state)
        {
            PlayerId = playerId;
            Revision = revision;
            Full = full;
            State = state;
        }
    }

    /// <summary>
    /// A player's inventory on the wire, in the save's own format (a one-character <see cref="SaveGame"/>
    /// through <see cref="SaveCodec"/>), deflated — so a field added to the save travels with no second
    /// codec to keep in step (the argument <c>ItemWire</c> made for single items). Readers take a reader
    /// positioned after the kind byte.
    /// </summary>
    public static class ParticipantCodec
    {
        public static void Write(NetWriter w, int playerId, int revision, bool full, SaveGame state)
        {
            w.WriteByte((byte)NetMessageKind.Participant);
            w.WriteInt(playerId);
            w.WriteInt(revision);
            w.WriteBool(full);
            WriteState(w, state);
        }

        public static ParticipantMessage Read(NetReader r)
        {
            int playerId = r.ReadInt();
            int revision = r.ReadInt();
            bool full = r.ReadBool();
            return new ParticipantMessage(playerId, revision, full, ReadState(r));
        }

        /// <summary>A save, deflated, as a blob — for any message that carries one (the lobby's pick too).</summary>
        public static void WriteState(NetWriter w, SaveGame state)
        {
            byte[] packed = NetDeflate.Pack(Encoding.UTF8.GetBytes(SaveCodec.Encode(state)));
            if (packed.Length > NetProtocol.MaxParticipantBytes)
            {
                throw new NetFormatException($"An inventory of {packed.Length} deflated bytes is too large to send.");
            }

            w.WriteBlob(packed);
        }

        public static SaveGame ReadState(NetReader r)
        {
            byte[] packed = r.ReadBlob(NetProtocol.MaxParticipantBytes);
            string json = Encoding.UTF8.GetString(NetDeflate.Unpack(packed, NetProtocol.MaxParticipantJsonBytes));
            SaveLoad load = SaveCodec.Decode(json);
            if (!load.Ok)
            {
                throw new NetFormatException($"An inventory that is not a save ({load.Reason}).");
            }

            return load.Save;
        }
    }
}
