namespace BattleBomb.Core.Players
{
    /// <summary>
    /// Identifies a player slot within a run. Distinct from any device, GameObject, or account id, so
    /// nothing in the simulation is written against one of those (§4).
    /// </summary>
    public readonly struct PlayerId
    {
        public readonly int Value;

        public PlayerId(int value)
        {
            Value = value;
        }

        public static PlayerId One => new PlayerId(0);
        public static PlayerId Two => new PlayerId(1);

        public override bool Equals(object obj) => obj is PlayerId other && other.Value == Value;

        public override int GetHashCode() => Value;

        public override string ToString() => $"P{Value + 1}";

        public static bool operator ==(PlayerId a, PlayerId b) => a.Value == b.Value;

        public static bool operator !=(PlayerId a, PlayerId b) => a.Value != b.Value;
    }
}

