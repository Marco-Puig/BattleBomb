using System;

namespace BattleBomb.Core.Loot
{
    /// <summary>
    /// One run's seeds for the driver's three streams — loot, combat, spawn (D10's streams, kept
    /// separate so an elite never shifts a drop). Drawn once per launch from one number of entropy
    /// and mixed so neighbouring numbers give unrelated runs; the host owns them online (D58).
    /// </summary>
    public readonly struct RunSeeds : IEquatable<RunSeeds>
    {
        public readonly uint Loot;
        public readonly uint Combat;
        public readonly uint Spawn;

        public RunSeeds(uint loot, uint combat, uint spawn)
        {
            Loot = loot;
            Combat = combat;
            Spawn = spawn;
        }

        public static RunSeeds From(uint entropy)
        {
            uint state = entropy;
            uint loot = NonZero(Mix(ref state));
            uint combat = NonZero(Mix(ref state));
            uint spawn = NonZero(Mix(ref state));
            if (combat == loot)
            {
                combat = NonZero(Mix(ref state));
            }

            while (spawn == loot || spawn == combat)
            {
                spawn = NonZero(Mix(ref state));
            }

            return new RunSeeds(loot, combat, spawn);
        }

        public bool Equals(RunSeeds other) => Loot == other.Loot && Combat == other.Combat && Spawn == other.Spawn;

        public override bool Equals(object obj) => obj is RunSeeds other && Equals(other);

        public override int GetHashCode() => unchecked((int)(Loot ^ (Combat * 397u) ^ (Spawn * 7919u)));

        public override string ToString() => $"loot {Loot}, combat {Combat}, spawn {Spawn}";

        /// <summary>A step of a golden-ratio counter through the murmur3 finaliser.</summary>
        private static uint Mix(ref uint state)
        {
            state = unchecked(state + 0x9E3779B9u);
            uint z = state;
            z = unchecked((z ^ (z >> 16)) * 0x85EBCA6Bu);
            z = unchecked((z ^ (z >> 13)) * 0xC2B2AE35u);
            return z ^ (z >> 16);
        }

        /// <summary>Zero is not a valid xorshift state (<see cref="DeterministicRandom"/>).</summary>
        private static uint NonZero(uint seed) => seed != 0u ? seed : 0x6D2B79F5u;
    }
}
