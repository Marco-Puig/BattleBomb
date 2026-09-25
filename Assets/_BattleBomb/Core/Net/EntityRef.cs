using System;

namespace BattleBomb.Core.Net
{
    public enum EntityKind : byte
    {
        None = 0,
        Player = 1,
        Enemy = 2,
        Dummy = 3,
    }

    /// <summary>
    /// Something in the world, named the same way on both machines: a player by id, an enemy by its
    /// session-unique network id, a dummy by its prop index in the current stage (HANDOFF-M8 planning
    /// decisions 8 and 9). What a <c>HitEvent</c>'s component references become on the wire.
    /// </summary>
    public readonly struct EntityRef : IEquatable<EntityRef>
    {
        public readonly EntityKind Kind;
        public readonly int Id;

        public EntityRef(EntityKind kind, int id)
        {
            Kind = kind;
            Id = id;
        }

        public static EntityRef None => default;

        public static EntityRef Player(int playerId) => new EntityRef(EntityKind.Player, playerId);

        public static EntityRef Enemy(int netId) => new EntityRef(EntityKind.Enemy, netId);

        public static EntityRef Dummy(int propIndex) => new EntityRef(EntityKind.Dummy, propIndex);

        public bool Equals(EntityRef other) => Kind == other.Kind && Id == other.Id;

        public override bool Equals(object obj) => obj is EntityRef other && Equals(other);

        public override int GetHashCode() => ((int)Kind * 397) ^ Id;

        public override string ToString() => $"{Kind} {Id}";
    }
}
