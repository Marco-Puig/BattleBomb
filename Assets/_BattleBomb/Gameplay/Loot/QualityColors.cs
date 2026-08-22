using BattleBomb.Core.Items;
using UnityEngine;

namespace BattleBomb.Gameplay.Loot
{
    /// <summary>
    /// One colour per rank of D33's ladder, shared by the drop token and every card that names
    /// an item. Placeholder values — art direction owns the real ramp later.
    /// </summary>
    public static class QualityColors
    {
        public static Color For(QualityRank rank)
        {
            switch (rank)
            {
                case QualityRank.Battlescarred: return new Color(0.62f, 0.50f, 0.38f);
                case QualityRank.Torn: return new Color(0.75f, 0.72f, 0.60f);
                case QualityRank.Rusty: return new Color(0.82f, 0.50f, 0.24f);
                case QualityRank.Shiny: return new Color(1f, 0.90f, 0.32f);
                case QualityRank.Pristine: return new Color(0.45f, 0.90f, 1f);
                case QualityRank.Legendary: return new Color(1f, 0.55f, 0.15f);
                case QualityRank.Mythical: return new Color(0.72f, 0.40f, 0.95f);
                case QualityRank.Godly: return new Color(1f, 0.28f, 0.22f);
                default: return new Color(0.55f, 0.55f, 0.55f);
            }
        }
    }
}
