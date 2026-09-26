namespace BattleBomb.Gameplay.Players
{
    /// <summary>
    /// Marks a command source whose player's hands are on another machine (HANDOFF-M8 planning decision 15).
    /// The registry counts every other source as local — a device, a test script, a replay — because each
    /// of those has its screens on this display.
    /// </summary>
    public interface IRemotePlayerSource
    {
    }
}
