namespace BattleBomb.Core.Net
{
    /// <summary>The first byte of every message. Values are wire format: never renumber, only add.</summary>
    public enum NetMessageKind : byte
    {
        Hello = 1,
        Welcome = 2,
        Refuse = 3,
        Commands = 4,
        Launch = 5,
        Snapshot = 6,
        Events = 7,
        LoadStage = 8,
        StageReady = 9,
        HandOver = 10,
        KeepAlive = 11,
        SessionEnd = 12,
        Bye = 13,
        Request = 14,
        RequestResult = 15,
        Participant = 16,
    }
}
