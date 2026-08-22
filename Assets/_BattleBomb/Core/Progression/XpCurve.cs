using UnityEngine;

namespace BattleBomb.Core.Progression
{
    /// <summary>
    /// D24's authored price of climbing: polynomial growth per level so 99 is a long haul but
    /// never a visually-frozen bar, times a rising multiplier per prestige cycle. Paper values
    /// in <see cref="Default"/>; the authoring asset overrides.
    /// </summary>
    public readonly struct XpCurve
    {
        public readonly float BaseXp;
        public readonly float LevelExponent;
        public readonly float PrestigeCostMultiplier;
        public readonly int MaxLevel;
        public readonly int PointsPerLevel;

        public XpCurve(float baseXp, float levelExponent, float prestigeCostMultiplier, int maxLevel, int pointsPerLevel)
        {
            BaseXp = Mathf.Max(1f, baseXp);
            LevelExponent = Mathf.Max(0f, levelExponent);
            PrestigeCostMultiplier = Mathf.Max(1f, prestigeCostMultiplier);
            MaxLevel = Mathf.Max(2, maxLevel);
            PointsPerLevel = Mathf.Max(1, pointsPerLevel);
        }

        /// <summary>The XP needed to climb from this level to the next, in this prestige cycle.</summary>
        public float XpToNext(int level, int prestigeCount) =>
            BaseXp
            * Mathf.Pow(Mathf.Max(1, level), LevelExponent)
            * Mathf.Pow(PrestigeCostMultiplier, Mathf.Max(0, prestigeCount));

        public static XpCurve Default => new XpCurve(
            baseXp: 40f,
            levelExponent: 1.5f,
            prestigeCostMultiplier: 1.25f,
            maxLevel: 99,
            pointsPerLevel: 1);
    }
}
