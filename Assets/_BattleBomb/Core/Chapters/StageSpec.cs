using System;
using BattleBomb.Core.Combat;
using UnityEngine;

namespace BattleBomb.Core.Chapters
{
    /// <summary>
    /// "The game" half of a stage (D48): everything gameplay-relevant, as data. The geometry
    /// scene is a name because Core never loads anything; the Gameplay runner resolves it. The
    /// three numbers M6 left at scene level — the level stamp (D36), loot progress (D23), and
    /// the climate (D41) — live here now, per stage.
    /// </summary>
    public readonly struct StageSpec
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string GeometryScene;
        public readonly ArenaSpec[] Arenas;
        public readonly int LevelStamp;
        public readonly float LootProgress;
        public readonly ElementalMultipliers Climate;

        public StageSpec(
            string id,
            string displayName,
            string geometryScene,
            ArenaSpec[] arenas,
            int levelStamp,
            float lootProgress,
            in ElementalMultipliers climate)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            GeometryScene = geometryScene ?? string.Empty;
            Arenas = arenas ?? Array.Empty<ArenaSpec>();
            LevelStamp = Mathf.Max(1, levelStamp);
            LootProgress = Mathf.Max(0f, lootProgress);
            Climate = climate;
        }

        public int ArenaCount => Arenas.Length;
    }
}
