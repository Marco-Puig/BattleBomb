using System.Collections.Generic;

namespace BattleBomb.Core.Items
{
    /// <summary>
    /// Everything one generated drop needs to know (task 41): the quality score straight from
    /// <c>DropRoll</c>, the progress level for the D36 stamp, the authored catalog and ladder,
    /// and the special-case forcings — a slot for D22's elites, a definition with a quality
    /// floor for D23's bosses.
    /// </summary>
    public readonly struct GenerationContext
    {
        public readonly float QualityScore;
        public readonly int ProgressLevel;
        public readonly IReadOnlyList<ItemSpec> Catalog;
        public readonly QualityTable Table;
        public readonly DropWeights Weights;
        public readonly bool HasForcedSlot;
        public readonly ItemSlot ForcedSlot;
        public readonly bool HasForcedDefinition;
        public readonly int ForcedDefinitionId;
        public readonly QualityRank MinQuality;

        public GenerationContext(
            float qualityScore,
            int progressLevel,
            IReadOnlyList<ItemSpec> catalog,
            in QualityTable table,
            in DropWeights weights)
            : this(qualityScore, progressLevel, catalog, table, weights,
                false, ItemSlot.Helmet, false, 0, QualityRank.Nothing)
        {
        }

        private GenerationContext(
            float qualityScore,
            int progressLevel,
            IReadOnlyList<ItemSpec> catalog,
            in QualityTable table,
            in DropWeights weights,
            bool hasForcedSlot,
            ItemSlot forcedSlot,
            bool hasForcedDefinition,
            int forcedDefinitionId,
            QualityRank minQuality)
        {
            QualityScore = qualityScore;
            ProgressLevel = progressLevel;
            Catalog = catalog;
            Table = table;
            Weights = weights;
            HasForcedSlot = hasForcedSlot;
            ForcedSlot = forcedSlot;
            HasForcedDefinition = hasForcedDefinition;
            ForcedDefinitionId = forcedDefinitionId;
            MinQuality = minQuality;
        }

        /// <summary>D22: an elite drops the slot it visibly wears.</summary>
        public GenerationContext WithForcedSlot(ItemSlot slot) => new GenerationContext(
            QualityScore, ProgressLevel, Catalog, Table, Weights,
            true, slot, HasForcedDefinition, ForcedDefinitionId, MinQuality);

        /// <summary>D23: a boss drops its authored signature at a quality floor.</summary>
        public GenerationContext WithSignature(int definitionId, QualityRank minQuality) => new GenerationContext(
            QualityScore, ProgressLevel, Catalog, Table, Weights,
            HasForcedSlot, ForcedSlot, true, definitionId, minQuality);
    }
}
