using BattleBomb.Core.Stats;

namespace BattleBomb.Core.Items
{
    /// <summary>What a menu can ask of a player's inventory. Values are wire format: never renumber, only add.</summary>
    public enum PlayerRequestKind : byte
    {
        Equip = 1,
        Unequip = 2,
        Sell = 3,
        SellJunk = 4,
        Lock = 5,
        LockWorn = 6,
        Upgrade = 7,
        UpgradeWorn = 8,
        Combine = 9,
        CombineAll = 10,
        QuickConsumable = 11,
        Allocate = 12,
        Buy = 13,
        CloseScreen = 14,
        SetAutoEquip = 15,
        SetAutoSell = 16,
        DebugGrant = 17,
    }

    /// <summary>Why a request did nothing. Values are wire format: never renumber, only add.</summary>
    public enum RequestRefusal : byte
    {
        None = 0,

        /// <summary>The rules said no — locked, full, too poor, nothing there to do it to.</summary>
        Refused = 1,

        /// <summary>It named a place in a sack that has changed since its player looked.</summary>
        StaleSack = 2,

        /// <summary>No chest or shop is open for this player, or not the one the verb needs.</summary>
        NoScreen = 3,

        /// <summary>An earlier request from the same screen is still waiting for its answer.</summary>
        Busy = 4,
    }

    /// <summary>
    /// One menu action, as a value (HANDOFF-M8 planning decision 11): who, which verb, up to four
    /// integer arguments, the sequence its sender numbered it with, and the sack revision its player
    /// was looking at. The same value runs on the couch at once and crosses the wire from a guest, so a
    /// menu has exactly one way to change anything.
    /// </summary>
    public readonly struct PlayerRequest
    {
        public readonly PlayerRequestKind Kind;
        public readonly int PlayerId;
        public readonly int Sequence;
        public readonly int Revision;
        public readonly int A;
        public readonly int B;
        public readonly int C;
        public readonly int D;

        public PlayerRequest(PlayerRequestKind kind, int playerId, int sequence, int revision, int a, int b, int c, int d)
        {
            Kind = kind;
            PlayerId = playerId;
            Sequence = sequence;
            Revision = revision;
            A = a;
            B = b;
            C = c;
            D = d;
        }

        private static PlayerRequest Of(PlayerRequestKind kind, int a = 0, int b = 0, int c = 0, int d = 0) =>
            new PlayerRequest(kind, -1, 0, 0, a, b, c, d);

        public static PlayerRequest Equip(int bagIndex) => Of(PlayerRequestKind.Equip, bagIndex);

        public static PlayerRequest Unequip(ItemSlot slot, int equipmentIndex) =>
            Of(PlayerRequestKind.Unequip, (int)slot, equipmentIndex);

        public static PlayerRequest Sell(int bagIndex) => Of(PlayerRequestKind.Sell, bagIndex);

        public static PlayerRequest SellJunk(QualityRank below) => Of(PlayerRequestKind.SellJunk, (int)below);

        public static PlayerRequest Lock(int bagIndex, bool locked) => Of(PlayerRequestKind.Lock, bagIndex, locked ? 1 : 0);

        public static PlayerRequest LockWorn(ItemSlot slot, int equipmentIndex, bool locked) =>
            Of(PlayerRequestKind.LockWorn, (int)slot, equipmentIndex, locked ? 1 : 0);

        public static PlayerRequest Upgrade(int bagIndex, in UpgradeTarget target) =>
            Of(PlayerRequestKind.Upgrade, bagIndex, (int)target.CoreStat, target.AffixIndex);

        public static PlayerRequest UpgradeWorn(ItemSlot slot, int equipmentIndex, in UpgradeTarget target) =>
            Of(PlayerRequestKind.UpgradeWorn, (int)slot, equipmentIndex, (int)target.CoreStat, target.AffixIndex);

        public static PlayerRequest Combine(int firstIndex, int secondIndex) =>
            Of(PlayerRequestKind.Combine, firstIndex, secondIndex);

        public static PlayerRequest CombineAll(int anchorIndex) => Of(PlayerRequestKind.CombineAll, anchorIndex);

        public static PlayerRequest QuickConsumable(int definitionId) => Of(PlayerRequestKind.QuickConsumable, definitionId);

        public static PlayerRequest Allocate(StatId stat) => Of(PlayerRequestKind.Allocate, (int)stat);

        public static PlayerRequest Buy(int rackSlot) => Of(PlayerRequestKind.Buy, rackSlot);

        public static PlayerRequest CloseScreen() => Of(PlayerRequestKind.CloseScreen);

        public static PlayerRequest SetAutoEquip(bool on) => Of(PlayerRequestKind.SetAutoEquip, on ? 1 : 0);

        public static PlayerRequest SetAutoSell(bool on) => Of(PlayerRequestKind.SetAutoSell, on ? 1 : 0);

        public static PlayerRequest DebugGrant() => Of(PlayerRequestKind.DebugGrant);

        /// <summary>The same request, as this player's. The host always stamps a remote player's own id
        /// over whatever arrived: a guest can only ever act as itself.</summary>
        public PlayerRequest For(int playerId) => new PlayerRequest(Kind, playerId, Sequence, Revision, A, B, C, D);

        public PlayerRequest WithSequence(int sequence) => new PlayerRequest(Kind, PlayerId, sequence, Revision, A, B, C, D);

        public PlayerRequest WithRevision(int revision) => new PlayerRequest(Kind, PlayerId, Sequence, revision, A, B, C, D);

        /// <summary>The verbs that name a place in the sack by its index — the ones a stale view could aim at
        /// the wrong item, and so the ones the revision guards. A worn slot, a definition id, a stat or a
        /// setting cannot move under the player's cursor; a rack slot moves only when its own player buys,
        /// and a guest's next action waits for that answer.</summary>
        public bool NamesASackPlace =>
            Kind == PlayerRequestKind.Equip || Kind == PlayerRequestKind.Sell || Kind == PlayerRequestKind.Lock
            || Kind == PlayerRequestKind.Upgrade || Kind == PlayerRequestKind.Combine || Kind == PlayerRequestKind.CombineAll;

        /// <summary>What an Upgrade (B, C) or an UpgradeWorn (C, D) deepens.</summary>
        public UpgradeTarget Target => Kind == PlayerRequestKind.UpgradeWorn ? TargetOf(C, D) : TargetOf(B, C);

        private static UpgradeTarget TargetOf(int coreStat, int affixIndex) =>
            affixIndex >= 0 ? UpgradeTarget.Affix(affixIndex) : UpgradeTarget.Core((CoreStatId)coreStat);
    }

    /// <summary>
    /// What a request did. The numbers mean what the verb says: Sell — A coins; SellJunk — A stacks, B
    /// pieces, C coins; Combine — A is 1 when it promoted; CombineAll — A combines, B promotions; Buy and
    /// the upgrades — A the price paid. A screen redraws from its bag's own change, never from these.
    /// </summary>
    public readonly struct RequestOutcome
    {
        public readonly bool Ok;
        public readonly RequestRefusal Refusal;
        public readonly int A;
        public readonly int B;
        public readonly int C;

        public RequestOutcome(bool ok, RequestRefusal refusal, int a, int b, int c)
        {
            Ok = ok;
            Refusal = refusal;
            A = a;
            B = b;
            C = c;
        }

        public static RequestOutcome Done(int a = 0, int b = 0, int c = 0) =>
            new RequestOutcome(true, RequestRefusal.None, a, b, c);

        public static RequestOutcome No(RequestRefusal why) =>
            new RequestOutcome(false, why == RequestRefusal.None ? RequestRefusal.Refused : why, 0, 0, 0);

        public static RequestOutcome From(bool ok) => ok ? Done() : No(RequestRefusal.Refused);
    }
}
