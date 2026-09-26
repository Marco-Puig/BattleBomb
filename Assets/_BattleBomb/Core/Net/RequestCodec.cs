using BattleBomb.Core.Items;

namespace BattleBomb.Core.Net
{
    /// <summary>A menu action and its answer on the reliable channel. Readers take a reader positioned after
    /// the kind byte.</summary>
    public static class RequestCodec
    {
        public static void WriteRequest(NetWriter w, in PlayerRequest r)
        {
            w.WriteByte((byte)NetMessageKind.Request);
            w.WriteByte((byte)r.Kind);
            w.WriteInt(r.PlayerId);
            w.WriteInt(r.Sequence);
            w.WriteInt(r.Revision);
            w.WriteInt(r.A);
            w.WriteInt(r.B);
            w.WriteInt(r.C);
            w.WriteInt(r.D);
        }

        public static PlayerRequest ReadRequest(NetReader r)
        {
            var kind = (PlayerRequestKind)r.ReadByte();
            if (kind < PlayerRequestKind.Equip || kind > PlayerRequestKind.DebugGrant)
            {
                throw new NetFormatException($"A request of kind {(byte)kind}.");
            }

            return new PlayerRequest(kind, r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt());
        }

        public static void WriteResult(NetWriter w, int sequence, in RequestOutcome o)
        {
            w.WriteByte((byte)NetMessageKind.RequestResult);
            w.WriteInt(sequence);
            w.WriteBool(o.Ok);
            w.WriteByte((byte)o.Refusal);
            w.WriteInt(o.A);
            w.WriteInt(o.B);
            w.WriteInt(o.C);
        }

        public static RequestOutcome ReadResult(NetReader r, out int sequence)
        {
            sequence = r.ReadInt();
            bool ok = r.ReadBool();
            var refusal = (RequestRefusal)r.ReadByte();
            if (refusal > RequestRefusal.Busy)
            {
                throw new NetFormatException($"A refusal of kind {(byte)refusal}.");
            }

            return new RequestOutcome(ok, refusal, r.ReadInt(), r.ReadInt(), r.ReadInt());
        }
    }
}
