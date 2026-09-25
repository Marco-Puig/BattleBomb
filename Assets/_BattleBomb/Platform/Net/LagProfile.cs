namespace BattleBomb.Platform.Net
{
    /// <summary>
    /// A made-up connection, for testing feel on one machine (HANDOFF-M8 paper numbers). The round
    /// trip is split evenly each way; jitter is spread either side of it; loss only ever hits the
    /// unreliable channel, because a real reliable channel resends until it arrives.
    /// </summary>
    public readonly struct LagProfile
    {
        public readonly string Name;
        public readonly float RoundTripMs;
        public readonly float JitterMs;
        public readonly float UnreliableLoss;

        public LagProfile(string name, float roundTripMs, float jitterMs, float unreliableLoss)
        {
            Name = name;
            RoundTripMs = roundTripMs > 0f ? roundTripMs : 0f;
            JitterMs = jitterMs > 0f ? jitterMs : 0f;
            UnreliableLoss = unreliableLoss < 0f ? 0f : unreliableLoss > 1f ? 1f : unreliableLoss;
        }

        public static LagProfile None => new LagProfile("None", 0f, 0f, 0f);

        /// <summary>A decent home connection.</summary>
        public static LagProfile Normal => new LagProfile("Normal", 100f, 10f, 0f);

        /// <summary>The one to make feel acceptable.</summary>
        public static LagProfile Bad => new LagProfile("Bad", 200f, 30f, 0.02f);

        public bool IsNone => RoundTripMs <= 0f && JitterMs <= 0f && UnreliableLoss <= 0f;

        public override string ToString() => $"{Name} ({RoundTripMs:0} ms ±{JitterMs * 0.5f:0}, {UnreliableLoss:P0} loss)";
    }
}
