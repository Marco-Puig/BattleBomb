namespace BattleBomb.Core.Loot
{
    /// <summary>
    /// The project's first randomness (task 36, planning decision 11): a tiny seeded xorshift,
    /// a value like every other Core state — drawing returns the next state, so identical seeds
    /// replay identical streams on every machine (D10). Unity's Random stays forbidden in Core;
    /// Gameplay owns the seed and injects this wherever chance is needed.
    /// </summary>
    public readonly struct DeterministicRandom
    {
        public readonly uint State;

        /// <summary>Zero is not a valid xorshift state, so it silently becomes a fixed non-zero one.</summary>
        public DeterministicRandom(uint seed)
        {
            State = seed != 0u ? seed : 2463534242u;
        }

        public DeterministicRandom Next(out uint value)
        {
            uint x = State;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            value = x;
            return new DeterministicRandom(x);
        }

        /// <summary>A draw in [0, 1).</summary>
        public DeterministicRandom NextFloat(out float value)
        {
            DeterministicRandom next = Next(out uint raw);
            value = (raw >> 8) * (1f / 16777216f);
            return next;
        }
    }
}
