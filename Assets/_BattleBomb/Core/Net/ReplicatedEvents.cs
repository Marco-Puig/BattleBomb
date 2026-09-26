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
        ScreenOpened = 4,
        ScreenClosed = 5,
        RackChanged = 6,
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

    /// <summary>A player's chest or shop opening or closing (D42). Menu state, which the guest takes on arrival.</summary>
    public readonly struct ScreenRecord
    {
        public readonly int PlayerId;

        /// <summary>The driver's interaction kind, as its number (the enum is Gameplay's).</summary>
        public readonly int Kind;

        public ScreenRecord(int playerId, int kind)
        {
            PlayerId = playerId;
            Kind = kind;
        }
    }

    /// <summary>A player's shopkeeper rack as it now stands (D43): every piece, in slot order.</summary>
    public readonly struct RackRecord
    {
        public readonly int PlayerId;
        public readonly ItemInstance[] Pieces;

        public RackRecord(int playerId, ItemInstance[] pieces)
        {
            PlayerId = playerId;
            Pieces = pieces ?? System.Array.Empty<ItemInstance>();
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
        public readonly ScreenRecord Screen;
        public readonly RackRecord Rack;

        private ReplicatedEvent(
            ReplicatedEventKind kind, int hostFrame, in HitRecord hit, in DropRecord drop, in ScreenRecord screen, in RackRecord rack)
        {
            Kind = kind;
            HostFrame = hostFrame;
            Hit = hit;
            Drop = drop;
            Screen = screen;
            Rack = rack;
        }

        public static ReplicatedEvent OfHit(in HitRecord hit) =>
            new ReplicatedEvent(ReplicatedEventKind.Hit, 0, hit, default, default, default);

        public static ReplicatedEvent OfDrop(in DropRecord drop) =>
            new ReplicatedEvent(ReplicatedEventKind.DropSpawned, 0, default, drop, default, default);

        /// <summary>A drop that left the host's world — grabbed, swept or cleared by a wipe.</summary>
        public static ReplicatedEvent OfDropRemoved(int netId) =>
            new ReplicatedEvent(
                ReplicatedEventKind.DropRemoved, 0, default, new DropRecord(netId, Vector3.zero, default), default, default);

        /// <summary>A player's screen opening or closing — <paramref name="kind"/> is the driver's interaction kind.</summary>
        public static ReplicatedEvent OfScreen(int playerId, int kind, bool opened) =>
            new ReplicatedEvent(
                opened ? ReplicatedEventKind.ScreenOpened : ReplicatedEventKind.ScreenClosed, 0, default, default,
                new ScreenRecord(playerId, kind), default);

        /// <summary>A player's rack as it now stands.</summary>
        public static ReplicatedEvent OfRack(int playerId, ItemInstance[] pieces) =>
            new ReplicatedEvent(ReplicatedEventKind.RackChanged, 0, default, default, default, new RackRecord(playerId, pieces));

        /// <summary>The same event stamped with the step it happened in.</summary>
        public ReplicatedEvent At(int hostFrame) => new ReplicatedEvent(Kind, hostFrame, Hit, Drop, Screen, Rack);

        /// <summary>Menu state — a screen or a rack — which the guest takes as it arrives rather than when the
        /// picture reaches its step: it is not drawn in the world, and a menu a tenth of a second late is a menu
        /// that feels broken.</summary>
        public bool IsMenuState =>
            Kind == ReplicatedEventKind.ScreenOpened || Kind == ReplicatedEventKind.ScreenClosed
            || Kind == ReplicatedEventKind.RackChanged;
    }
}
