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
    }
}
