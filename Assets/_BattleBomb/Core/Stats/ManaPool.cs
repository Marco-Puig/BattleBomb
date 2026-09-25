using UnityEngine;

namespace BattleBomb.Core.Stats
{
    /// <summary>
    /// The mana pool D32 sizes and D19's Magic will spend in M5 — from M4 it exists, fills from
    /// gear regen, and shows its shape. A value, like every Core state.
    /// </summary>
    public readonly struct ManaPool
    {
        public readonly float Max;
        public readonly float Current;

        private ManaPool(float max, float current)
        {
            Max = max;
            Current = current;
        }

        public static ManaPool Full(float max)
        {
            float ceiling = Mathf.Max(0f, max);
            return new ManaPool(ceiling, ceiling);
        }

        /// <summary>A pool exactly as another machine had it (M8's wire).</summary>
        public static ManaPool FromValues(float max, float current) => new ManaPool(max, current);

        public float Fraction => Max > 0f ? Current / Max : 0f;

        /// <summary>One fixed step of regeneration; never overfills.</summary>
        public ManaPool Step(float regenPerSecond, float dt) =>
            new ManaPool(Max, Mathf.Min(Max, Current + Mathf.Max(0f, regenPerSecond) * dt));

        /// <summary>M5's casts pull from here; an overdraw simply empties the pool.</summary>
        public ManaPool Spent(float amount) =>
            new ManaPool(Max, Mathf.Max(0f, Current - Mathf.Max(0f, amount)));

        /// <summary>A mana potion's refill (D27); never overfills.</summary>
        public ManaPool Restored(float amount) =>
            new ManaPool(Max, Mathf.Min(Max, Current + Mathf.Max(0f, amount)));

        /// <summary>The stat sheet resized the pool; current mana is kept, clamped.</summary>
        public ManaPool Resized(float newMax)
        {
            float max = Mathf.Max(0f, newMax);
            return new ManaPool(max, Mathf.Min(Current, max));
        }
    }
}
