using System;
using System.Collections.Generic;
using BattleBomb.Core.Items;

namespace BattleBomb.Core.Net
{
    /// <summary>One step's (or several steps') events, on the reliable channel. A burst too big for one
    /// message — a boss's hoard of drops — goes as several, in order (<see cref="WriteBatches"/>).</summary>
    public static class EventCodec
    {
        public static void Write(NetWriter w, IReadOnlyList<ReplicatedEvent> events) => Write(w, events, 0, events.Count);

        /// <summary>Events <paramref name="start"/> to <paramref name="start"/> + <paramref name="count"/> as one batch.</summary>
        public static void Write(NetWriter w, IReadOnlyList<ReplicatedEvent> events, int start, int count)
        {
            w.WriteByte((byte)NetMessageKind.Events);
            w.WriteCount(count, NetProtocol.MaxEvents);
            for (int i = start; i < start + count; i++)
            {
                WriteEvent(w, events[i]);
            }
        }

        /// <summary>
        /// Every event, as many batches as it takes — each within <see cref="NetProtocol.MaxEvents"/> and
        /// <see cref="NetProtocol.MaxMessageBytes"/>, past which a transport drops the connection — handed
        /// to <paramref name="send"/> in order. <paramref name="scratch"/> measures each event first.
        /// </summary>
        public static void WriteBatches(
            IReadOnlyList<ReplicatedEvent> events, NetWriter writer, NetWriter scratch, Action<NetWriter> send)
        {
            for (int start = 0; start < events.Count;)
            {
                int count = FitFrom(events, start, scratch);
                writer.Reset();
                Write(writer, events, start, count);
                send(writer);
                start += count;
            }
        }

        /// <summary>Reads a batch positioned after its kind byte. <paramref name="catalog"/> names items
        /// as this build knows them; null keeps the names they were sent with.</summary>
        public static void Read(NetReader r, IReadOnlyList<ItemSpec> catalog, List<ReplicatedEvent> into)
        {
            into.Clear();
            int count = r.ReadCount(NetProtocol.MaxEvents);
            for (int i = 0; i < count; i++)
            {
                var kind = (ReplicatedEventKind)r.ReadByte();
                int frame = r.ReadInt();
                switch (kind)
                {
                    case ReplicatedEventKind.Hit:
                        into.Add(ReplicatedEvent.OfHit(new HitRecord(
                            ReadRef(r), ReadRef(r), r.ReadFloat(), r.ReadVector3(),
                            r.ReadBool(), r.ReadBool(), r.ReadBool())).At(frame));
                        break;

                    case ReplicatedEventKind.DropSpawned:
                        into.Add(ReplicatedEvent.OfDrop(new DropRecord(
                            r.ReadInt(), r.ReadVector3(), ItemWire.Read(r, catalog))).At(frame));
                        break;

                    case ReplicatedEventKind.DropRemoved:
                        into.Add(ReplicatedEvent.OfDropRemoved(r.ReadInt()).At(frame));
                        break;

                    default:
                        throw new NetFormatException($"An event of kind {(byte)kind}.");
                }
            }
        }

        /// <summary>How many events from <paramref name="start"/> fit one message — always at least one:
        /// a single event is far inside the limit, since an item's JSON is bounded by ItemWire.</summary>
        private static int FitFrom(IReadOnlyList<ReplicatedEvent> events, int start, NetWriter scratch)
        {
            scratch.Reset();
            scratch.WriteByte((byte)NetMessageKind.Events);
            scratch.WriteCount(0, NetProtocol.MaxEvents);
            int bytes = scratch.Length;
            int count = 0;
            for (int i = start; i < events.Count && count < NetProtocol.MaxEvents; i++)
            {
                scratch.Reset();
                WriteEvent(scratch, events[i]);
                if (count > 0 && bytes + scratch.Length > NetProtocol.MaxMessageBytes)
                {
                    break;
                }

                bytes += scratch.Length;
                count++;
            }

            return count;
        }

        private static void WriteEvent(NetWriter w, in ReplicatedEvent e)
        {
            w.WriteByte((byte)e.Kind);
            w.WriteInt(e.HostFrame);
            switch (e.Kind)
            {
                case ReplicatedEventKind.Hit:
                    WriteRef(w, e.Hit.Attacker);
                    WriteRef(w, e.Hit.Target);
                    w.WriteFloat(e.Hit.Damage);
                    w.WriteVector3(e.Hit.Position);
                    w.WriteBool(e.Hit.IsPartner);
                    w.WriteBool(e.Hit.IsCrit);
                    w.WriteBool(e.Hit.IsDamageOverTime);
                    break;

                case ReplicatedEventKind.DropSpawned:
                    w.WriteInt(e.Drop.NetId);
                    w.WriteVector3(e.Drop.Position);
                    ItemWire.Write(w, e.Drop.Item);
                    break;

                case ReplicatedEventKind.DropRemoved:
                    w.WriteInt(e.Drop.NetId);
                    break;
            }
        }

        private static void WriteRef(NetWriter w, in EntityRef entity)
        {
            w.WriteByte((byte)entity.Kind);
            w.WriteInt(entity.Id);
        }

        private static EntityRef ReadRef(NetReader r) => new EntityRef((EntityKind)r.ReadByte(), r.ReadInt());
    }
}
