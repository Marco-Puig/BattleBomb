using BattleBomb.Core.Stats;
using UnityEngine;

namespace BattleBomb.Core.Progression
{
    /// <summary>
    /// One player's place on the endless ladder (D24/D32): level, XP into it, the points banked
    /// and spent, and the prestige count that is both the badge and the permanent point pool.
    /// XP past the level cap is discarded — prestige is the only release. Value-chained like all
    /// Core state.
    /// </summary>
    public readonly struct XpLedger
    {
        public readonly int Level;
        public readonly float XpIntoLevel;
        public readonly int UnspentPoints;
        public readonly BaseStats Allocations;
        public readonly int PrestigeCount;

        public XpLedger(int level, float xpIntoLevel, int unspentPoints, in BaseStats allocations, int prestigeCount)
        {
            Level = Mathf.Max(1, level);
            XpIntoLevel = Mathf.Max(0f, xpIntoLevel);
            UnspentPoints = Mathf.Max(0, unspentPoints);
            Allocations = allocations;
            PrestigeCount = Mathf.Max(0, prestigeCount);
        }

        public static XpLedger Fresh => new XpLedger(1, 0f, 0, BaseStats.Zero, 0);

        public bool CanPrestige(in XpCurve curve) => Level >= curve.MaxLevel;

        public XpLedger Earn(float xp, in XpCurve curve)
        {
            if (xp <= 0f)
            {
                return this;
            }

            int level = Level;
            int unspent = UnspentPoints;
            float pool = XpIntoLevel + xp;

            while (level < curve.MaxLevel)
            {
                float toNext = curve.XpToNext(level, PrestigeCount);
                if (pool < toNext)
                {
                    break;
                }

                pool -= toNext;
                level++;
                unspent += curve.PointsPerLevel;
            }

            if (level >= curve.MaxLevel)
            {
                pool = 0f;
            }

            return new XpLedger(level, pool, unspent, Allocations, PrestigeCount);
        }

        /// <summary>
        /// The reset that is real twice over: level and allocations gone, one more permanent
        /// point banked, and the whole permanent pool handed back to re-allocate from scratch.
        /// D36's gear re-lock reads the new level; below the wall this is a no-op.
        /// </summary>
        public XpLedger Prestige(in XpCurve curve)
        {
            if (!CanPrestige(curve))
            {
                return this;
            }

            int cycles = PrestigeCount + 1;
            return new XpLedger(1, 0f, cycles, BaseStats.Zero, cycles);
        }

        public XpLedger Spend(StatId stat)
        {
            if (UnspentPoints <= 0)
            {
                return this;
            }

            return new XpLedger(Level, XpIntoLevel, UnspentPoints - 1, Allocations.Allocate(stat), PrestigeCount);
        }
    }
}
