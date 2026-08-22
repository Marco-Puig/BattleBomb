namespace BattleBomb.Gameplay.Players
{
    /// <summary>Something a command source registers with. The driver in play; the sampler on
    /// the front door, where there is no simulation but there are still two devices.</summary>
    public interface IPlayerRegistryHost
    {
        PlayerRegistry Players { get; }
    }
}
