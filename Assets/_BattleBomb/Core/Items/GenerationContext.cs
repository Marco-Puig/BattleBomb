using System.Collections.Generic;
using BattleBomb.Core.Combat;

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

        /// <summary>The ceiling a roll may reach — Godly for an ordinary drop, and the same rank
        /// as <see cref="MinQuality"/> when a combine (D44) pins the reroll to one rank.</summary>
        public readonly QualityRank MaxQuality;

        /// <summary>
        /// The elements an element-flavoured affix may roll (D38). Empty means none are authored
        /// yet, and those affixes simply stay out of the pool rather than rolling a dud.
        /// </summary>
        public readonly IReadOnlyList<ElementId> Elements;

        public GenerationContext(
            float qualityScore,
            int progressLevel,
            IReadOnlyList<ItemSpec> catalog,
            in QualityTable table,
            in DropWeights weights,
            IReadOnlyList<ElementId> elements = null)
            : this(qualityScore, progressLevel, catalog, table, weights, elements,
                false, ItemSlot.Helmet, false, 0, QualityRank.Nothing, QualityRank.Godly)
        {
        }

        private GenerationContext(
            float qualityScore,
            int progressLevel,
            IReadOnlyList<ItemSpec> catalog,
            in QualityTable table,
            in DropWeights weights,
            IReadOnlyList<ElementId> elements,
            bool hasForcedSlot,
            ItemSlot forcedSlot,
            bool hasForcedDefinition,
            int forcedDefinitionId,
            QualityRank minQuality,
            QualityRank maxQuality)
        {
            MaxQuality = maxQuality;
            QualityScore = qualityScore;
            ProgressLevel = progressLevel;
            Catalog = catalog;
            Table = table;
            Weights = weights;
            Elements = elements;
            HasForcedSlot = hasForcedSlot;
            ForcedSlot = forcedSlot;
            HasForcedDefinition = hasForcedDefinition;
            ForcedDefinitionId = forcedDefinitionId;
            MinQuality = minQuality;
        }

        /// <summary>True when an element-flavoured affix has something to roll.</summary>
        public bool HasElements => Elements != null && Elements.Count > 0;

        /// <summary>D22: an elite drops the slot it visibly wears.</summary>
        public GenerationContext WithForcedSlot(ItemSlot slot) => new GenerationContext(
            QualityScore, ProgressLevel, Catalog, Table, Weights, Elements,
            true, slot, HasForcedDefinition, ForcedDefinitionId, MinQuality, MaxQuality);

        /// <summary>D23: a boss drops its authored signature at a quality floor.</summary>
        public GenerationContext WithSignature(int definitionId, QualityRank minQuality) => new GenerationContext(
            QualityScore, ProgressLevel, Catalog, Table, Weights, Elements,
            HasForcedSlot, ForcedSlot, true, definitionId, minQuality, MaxQuality);

        /// <summary>
        /// D44's combine: this exact definition at this exact rank. Floor and ceiling meet, so
        /// the reroll varies in everything except what it is and how good it is allowed to be.
        /// </summary>
        public GenerationContext WithExactRoll(int definitionId, QualityRank rank) => new GenerationContext(
            QualityScore, ProgressLevel, Catalog, Table, Weights, Elements,
            HasForcedSlot, ForcedSlot, true, definitionId, rank, rank);
    }
}
