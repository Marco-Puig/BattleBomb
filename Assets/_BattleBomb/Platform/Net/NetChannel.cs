namespace BattleBomb.Platform.Net
{
    /// <summary>Reliable is ordered and always arrives; unreliable may be lost, and newer wins.</summary>
    public enum NetChannel : byte
    {
        Reliable = 0,
        Unreliable = 1,
    }
}
