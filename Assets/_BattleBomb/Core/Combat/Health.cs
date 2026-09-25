using System;
using UnityEngine;

namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// A value, like every other Core state: damage returns a new instance. Starts full.
    /// </summary>
    public readonly struct Health
    {
        public readonly float Max;
        public readonly float Current;

        public Health(float max)
        {
            if (max <= 0f)
            {
                throw new ArgumentException("Max health must be positive.", nameof(max));
            }

            Max = max;
            Current = max;
        }

        /// <summary>A pool exactly as another machine had it (M8's wire). No clamping: the host's
        /// numbers are the truth (D58).</summary>
        public static Health FromValues(float max, float current) => new Health(max, current);

        private Health(float max, float current)
        {
            Max = max;
            Current = current;
        }

        public bool IsDepleted => Current <= 0f;

        public Health Damaged(float amount) =>
            amount <= 0f ? this : new Health(Max, Mathf.Max(0f, Current - amount));

        public Health Refilled() => new Health(Max, Max);

        /// <summary>A partial refill — the revive's half-health return (D25). Fraction is clamped.</summary>
        public Health Refilled(float fraction) =>
            new Health(Max, Mathf.Clamp01(fraction) * Max);

        /// <summary>Life steal and potions (M4): heals never overfill and never harm.</summary>
        public Health Healed(float amount) =>
            amount <= 0f ? this : new Health(Max, Mathf.Min(Max, Current + amount));

        /// <summary>
        /// The stat sheet changed the pool's size (D32/D35): current health is kept, clamped to
        /// the new ceiling — equipping +HP gear never heals, unequipping it can wound.
        /// </summary>
        public Health Resized(float newMax)
        {
            float max = Mathf.Max(1f, newMax);
            return new Health(max, Mathf.Min(Current, max));
        }
    }
}
