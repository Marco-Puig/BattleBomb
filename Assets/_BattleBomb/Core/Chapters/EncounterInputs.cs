using BattleBomb.Core.Combat;

namespace BattleBomb.Core.Chapters
{
    /// <summary>
    /// tier × stage → the numbers the spawner and the loot roll consume (D50). This struct
    /// replaces the driver's <c>_lootProgress</c> field, its hardcoded level stamp, and its
    /// scene-level climate rows outright: everything D23's formula wants is here, and it all
    /// came from data.
    /// </summary>
    public readonly struct EncounterInputs
    {
        public readonly int LevelStamp;
        public readonly float LootProgress;
        public readonly float LootDifficulty;
        public readonly float HealthMultiplier;
        public readonly float DamageMultiplier;
        public readonly ElementalMultipliers Climate;

        public EncounterInputs(
            int levelStamp,
            float lootProgress,
            float lootDifficulty,
            float healthMultiplier,
            float damageMultiplier,
            in ElementalMultipliers climate)
        {
            LevelStamp = levelStamp < 1 ? 1 : levelStamp;
            LootProgress = lootProgress < 0f ? 0f : lootProgress;
            LootDifficulty = lootDifficulty < 0f ? 0f : lootDifficulty;
            HealthMultiplier = healthMultiplier <= 0f ? 1f : healthMultiplier;
            DamageMultiplier = damageMultiplier <= 0f ? 1f : damageMultiplier;
            Climate = climate;
        }

        /// <summary>The bare test scene's numbers: level 1, the M6 loot dial of 2, no climate.</summary>
        public static EncounterInputs Default => new EncounterInputs(1, 2f, 1f, 1f, 1f, ElementalMultipliers.Neutral);

        public static EncounterInputs From(in TierSpec tier, in StageSpec stage) => new EncounterInputs(
            stage.LevelStamp + tier.LevelBump,
            stage.LootProgress,
            tier.LootMultiplier,
            tier.HealthMultiplier,
            tier.DamageMultiplier,
            stage.Climate);
    }
}
