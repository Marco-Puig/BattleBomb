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
    /// session-unique network id, a dummy by its stage and prop index (HANDOFF-M8 planning
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

        /// <summary>A dummy by the stage it stands in and its prop index there (planning decision 8): the
        /// first dummy of every stage is the same prop, so the index alone would name two.</summary>
        public static EntityRef Dummy(int stageIndex, int propIndex) =>
            new EntityRef(EntityKind.Dummy, (stageIndex << 16) | (propIndex & 0xFFFF));

        /// <summary>A dummy ref's stage, or -1 when it was hit with no stage loaded.</summary>
        public int DummyStage => Id >> 16;

        public int DummyProp => Id & 0xFFFF;

        public bool Equals(EntityRef other) => Kind == other.Kind && Id == other.Id;

        public override bool Equals(object obj) => obj is EntityRef other && Equals(other);

        public override int GetHashCode() => ((int)Kind * 397) ^ Id;

        public override string ToString() => $"{Kind} {Id}";
    }
}
