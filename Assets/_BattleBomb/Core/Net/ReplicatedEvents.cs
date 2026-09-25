using BattleBomb.Core.Items;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>Values are wire format: never renumber, only add.</summary>
    public enum ReplicatedEventKind : byte
    {
        Hit = 1,
        DropSpawned = 2,
        DropRemoved = 3,
    }

    /// <summary>A landed hit, with its two parties as entity references (HANDOFF-M8 planning decision 9).</summary>
    public readonly struct HitRecord
    {
        public readonly EntityRef Attacker;
        public readonly EntityRef Target;
        public readonly float Damage;
        public readonly Vector3 Position;
        public readonly bool IsPartner;
        public readonly bool IsCrit;
        public readonly bool IsDamageOverTime;

        public HitRecord(
            EntityRef attacker, EntityRef target, float damage, Vector3 position,
            bool isPartner, bool isCrit, bool isDamageOverTime)
        {
            Attacker = attacker;
            Target = target;
            Damage = damage;
            Position = position;
            IsPartner = isPartner;
            IsCrit = isCrit;
            IsDamageOverTime = isDamageOverTime;
        }
    }

    /// <summary>A drop appearing, with the item it carries — sent once; snapshots carry no drops.</summary>
    public readonly struct DropRecord
    {
        public readonly int NetId;
        public readonly Vector3 Position;
        public readonly ItemInstance Item;

        public DropRecord(int netId, Vector3 position, in ItemInstance item)
        {
            NetId = netId;
            Position = position;
            Item = item;
        }
    }

    /// <summary>Something that happened once, at a host step. The guest raises it when its picture
    /// reaches that step, so a hit spark lands on the frame the hit is drawn.</summary>
    public readonly struct ReplicatedEvent
    {
        public readonly ReplicatedEventKind Kind;
        public readonly int HostFrame;
        public readonly HitRecord Hit;
        public readonly DropRecord Drop;

        private ReplicatedEvent(ReplicatedEventKind kind, int hostFrame, in HitRecord hit, in DropRecord drop)
        {
            Kind = kind;
            HostFrame = hostFrame;
            Hit = hit;
            Drop = drop;
        }

        public static ReplicatedEvent OfHit(in HitRecord hit) =>
            new ReplicatedEvent(ReplicatedEventKind.Hit, 0, hit, default);

        public static ReplicatedEvent OfDrop(in DropRecord drop) =>
            new ReplicatedEvent(ReplicatedEventKind.DropSpawned, 0, default, drop);

        /// <summary>A drop that left the host's world — grabbed, swept or cleared by a wipe.</summary>
        public static ReplicatedEvent OfDropRemoved(int netId) =>
            new ReplicatedEvent(ReplicatedEventKind.DropRemoved, 0, default, new DropRecord(netId, Vector3.zero, default));

        /// <summary>The same event stamped with the step it happened in.</summary>
        public ReplicatedEvent At(int hostFrame) => new ReplicatedEvent(Kind, hostFrame, Hit, Drop);
    }
}
