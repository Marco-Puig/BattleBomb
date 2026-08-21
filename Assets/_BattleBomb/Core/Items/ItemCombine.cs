using BattleBomb.Core.Loot;
using UnityEngine;

namespace BattleBomb.Core.Items
{
    /// <summary>What a combine produced — the reroll, and whether the 2% shot landed.</summary>
    public readonly struct CombineResult
    {
        public readonly bool Combined;
        public readonly ItemInstance Item;
        public readonly bool Promoted;

        public CombineResult(bool combined, in ItemInstance item, bool promoted)
        {
            Combined = combined;
            Item = item;
            Promoted = promoted;
        }

        public static CombineResult Refused => default;
    }

    /// <summary>
    /// D44's gamble. Two of the same item at the same rank are both consumed for one fresh
    /// reroll of it, with a small chance the reroll returns a rank higher. Spent points die with
    /// the inputs — you invest in an item or you gamble it, never both — and the result carries
    /// the higher of the two required levels, so combining never launders gear downhill.
    /// </summary>
    public static class ItemCombine
    {
        /// <summary>The jackpot: how often a reroll comes back one rank up (D44, tunable).</summary>
        public const float PromotionChance = 0.02f;

        /// <summary>
        /// Same definition, same rank, both unlocked, neither a consumable, and not the same
        /// bag slot twice. Quality must match exactly — a Shiny and a Godly never merge.
        /// </summary>
        public static bool CanCombine(in ItemInstance a, in ItemInstance b)
        {
            if (a.IsEmpty || b.IsEmpty || a.IsConsumable || b.IsConsumable)
            {
                return false;
            }

            if (a.Locked || b.Locked)
            {
                return false;
            }

            return a.DefinitionId == b.DefinitionId && a.Quality == b.Quality;
        }

        /// <summary>
        /// Consumes both and rolls one replacement. The promotion draw comes first and always
        /// consumes a number, so the stream advances identically whether or not it lands
        /// (task 41's rule: draw counts never depend on outcomes).
        /// </summary>
        public static DeterministicRandom Combine(
            in DeterministicRandom rng,
            in ItemInstance a,
            in ItemInstance b,
            in GenerationContext context,
            out CombineResult result)
        {
            DeterministicRandom next = rng;
            if (!CanCombine(a, b))
            {
                result = CombineResult.Refused;
                return next;
            }

            next = next.NextFloat(out float promotionDraw);
            bool promoted = promotionDraw < PromotionChance && a.Quality < QualityRank.Godly;
            QualityRank rank = promoted ? a.Quality + 1 : a.Quality;

            next = ItemGenerator.Roll(
                next,
                context.WithExactRoll(a.DefinitionId, rank),
                out ItemInstance rolled);
            if (rolled.IsEmpty)
            {
                result = CombineResult.Refused;
                return next;
            }

            // The higher input's level travels with the result: a reroll is never a way to
            // carry level-40 power into a level-5 character's hands (D36).
            int requiredLevel = Mathf.Max(a.RequiredLevel, b.RequiredLevel);
            var combined = new ItemInstance(
                rolled.Identity,
                rolled.Quality,
                rolled.CoreStats,
                rolled.Affixes,
                requiredLevel,
                // Fresh capacity, nothing spent: whatever either input had invested is gone.
                new ItemInvestment(rolled.UpgradeCapacity),
                rolled.ShotSpeed,
                rolled.Consumable,
                rolled.Active);

            result = new CombineResult(true, combined, promoted);
            return next;
        }
    }
}
