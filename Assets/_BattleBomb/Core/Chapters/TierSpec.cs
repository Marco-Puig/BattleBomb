using UnityEngine;

namespace BattleBomb.Core.Chapters
{
    /// <summary>
    /// One difficulty row (D50). The player sees the name and nothing else; the numbers are
    /// a dev overlay. Three rows at launch; a fourth is a row. Paper values from HANDOFF-M7.
    /// </summary>
    public readonly struct TierSpec
    {
        public readonly string Name;
        public readonly float HealthMultiplier;
        public readonly float DamageMultiplier;
        public readonly int LevelBump;
        public readonly float LootMultiplier;

        public TierSpec(string name, float healthMultiplier, float damageMultiplier, int levelBump, float lootMultiplier)
        {
            Name = name ?? string.Empty;
            HealthMultiplier = Mathf.Max(0.01f, healthMultiplier);
            DamageMultiplier = Mathf.Max(0.01f, damageMultiplier);
            LevelBump = Mathf.Max(0, levelBump);
            LootMultiplier = Mathf.Max(0.01f, lootMultiplier);
        }

        public static readonly TierSpec[] Defaults =
        {
            new TierSpec("Normal", 1f, 1f, 0, 1f),
            new TierSpec("Hard", 1.6f, 1.3f, 10, 1.5f),
            new TierSpec("Nightmare", 2.5f, 1.7f, 25, 2.25f),
        };
    }
}
